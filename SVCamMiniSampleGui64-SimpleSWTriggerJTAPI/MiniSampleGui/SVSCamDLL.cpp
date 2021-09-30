// SVSCamDLL.cpp: implementation of the SVSCamDLL class.
//
//////////////////////////////////////////////////////////////////////

#include "stdafx.h"
#include "SVSCamDLL.h"
#include <string> 

using   namespace   std;

#ifdef _DEBUG
#undef THIS_FILE
static char THIS_FILE[]=__FILE__;
#define new DEBUG_NEW
#endif

std::wstring s2ws2(const std::string& s)
{
    int len;
    int slength = (int)s.length() + 1;
    len = MultiByteToWideChar(CP_ACP, 0, s.c_str(), slength, 0, 0);
    wchar_t* buf = new wchar_t[len];
    MultiByteToWideChar(CP_ACP, 0, s.c_str(), slength, buf, len);
    std::wstring r(buf);
    delete[] buf;
    return r;
}

#include <stdarg.h>  // For va_start, etc.
#include <memory>    // For std::unique_ptr

std::string string_format2(const std::string fmt_str, ...) {
    int final_n, n = ((int)fmt_str.size()) * 2; /* Reserve two times as much as the length of the fmt_str */
    std::unique_ptr<char[]> formatted;
    va_list ap;
    while(1) {
        formatted.reset(new char[n]); /* Wrap the plain char array into the unique_ptr */
        strcpy_s(&formatted[0], fmt_str.size(), fmt_str.c_str());
        va_start(ap, fmt_str);
        final_n = vsnprintf(&formatted[0], n, fmt_str.c_str(), ap);
        va_end(ap);
        if (final_n < 0 || final_n >= n)
            n += abs(final_n - n + 1);
        else
            break;
    }
    return std::string(formatted.get());
}

std::wstring string_format_ws(const std::string fmt_str, ...) {
    int final_n, n = ((int)fmt_str.size()) * 3; /* Reserve two times as much as the length of the fmt_str */
    std::unique_ptr<char[]> formatted;
    va_list ap;
    while(1) {
        formatted.reset(new char[n]); /* Wrap the plain char array into the unique_ptr */
        strcpy_s(&formatted[0], fmt_str.size(), fmt_str.c_str());
        va_start(ap, fmt_str);
        final_n = vsnprintf(&formatted[0], n, fmt_str.c_str(), ap);
        va_end(ap);
        if (final_n < 0 || final_n >= n)
            n += abs(final_n - n + 1);
        else
            break;
    }
    std::string strNew = std::string(formatted.get());
    
    return s2ws2(strNew);
}



//////////////////////////////////////////////////////////////////////
// Construction/Destruction
//////////////////////////////////////////////////////////////////////

unsigned long __stdcall GrabSVSCameraPhotosThreadfunction(void* context)
{
    OutputDebugString(L"\nThread Started: To running ...\n");

    // Process only the currently selected camera.
    SVSCamDLL* svCam = (SVSCamDLL*)context;
    if (svCam != NULL)
    {
        svCam->startAcqThread(); 
    }
    return 0;
}

SVSCamDLL::SVSCamDLL()
{
    //InitializeCriticalSection(&csacquisition);

    sdk_init_done = false;

    if(cam_container == NULL)
        cam_container = new CameraContainer();

    currentCam = NULL; 

    isStoping = false;

    // Thread in background to capture every photo triggered by software signal
    acqTerminated = false;
    m_acquisitionstopThread = CreateEvent(NULL, false, false, NULL);
    m_thread = CreateThread(NULL, 0, GrabSVSCameraPhotosThreadfunction, (void *)this, 0, NULL);

}


SVSCamDLL::~SVSCamDLL()
{
    if (!acqTerminated)
    {
        acqTerminated = true;
        WaitForSingleObject(m_acquisitionstopThread, INFINITE);
        ResetEvent(m_acquisitionstopThread);
        CloseHandle(m_thread);
        m_thread = NULL;
    }

    if (cam_container) {
        delete  cam_container;
        cam_container = NULL;
    }

    // Release allocated memory
    if (m_newBuffer.pImagePtr)
    {
        delete[] m_newBuffer.pImagePtr;
        m_newBuffer.pImagePtr = NULL;
    }

    //DeleteCriticalSection(&csacquisition);
}

int SVSCamDLL::Open()
{
    // Dicovery all SVS cameras and open the first one 
    SV_RETURN ret = SV_ERROR_SUCCESS;


    if (!sdk_init_done)
    {
        if (!cam_container->initSDK())
        {
            //m_cam_found.SetWindowTextW(L" initialisation failed !!");
            return -1;
        }
        sdk_init_done = true;
    }


    int  CamAnz = (int)cam_container->devicediscovery();
    string  st1 = "Cameras found: ";

    if (CamAnz != 0)
    {
        for (vector<Camera *>::iterator it = cam_container->sv_cam_list.begin(); it != cam_container->sv_cam_list.end(); ++it)
            if (*it)
            {
                const CString  strValue((*it)->devInfo.model);
                LPCWSTR ws = static_cast<LPCWSTR>(strValue);
            }

        string st2 = to_string(CamAnz);
        st1.append(st2);
    }
    else
    {
        //m_cam_found.SetWindowTextW(L" NO Cameras found !!");
    }

    // Open the first camera
    // Set to software trigger mode
    currentCam = cam_container->sv_cam_list.at(0);
    ASSERT(currentCam);

    ret = currentCam->openConnection();
    ASSERT(SV_ERROR_SUCCESS == ret);


    ret = currentCam->SetTriggerMode(true);    // enable software trigger by default  
    ASSERT(SV_ERROR_SUCCESS == ret);
    ret = currentCam->setAcquisitionMode(2);   // Continous mode
    ASSERT(SV_ERROR_SUCCESS == ret);


    // Get  Camera Information
    currentCam->CamHeight = 0;
    currentCam->CamWidth = 0;

    ret = SV_ERROR_SUCCESS;
    SV_FEATURE_HANDLE hFeature = NULL;

    // Get The Camera Size
    SVFeatureGetByName(currentCam->hRemoteDev, "SensorWidth", &hFeature);
    SVFeatureGetValueInt64(currentCam->hRemoteDev, hFeature, &currentCam->CamWidth);

    SVFeatureGetByName(currentCam->hRemoteDev, "SensorHeight", &hFeature);
    SVFeatureGetValueInt64(currentCam->hRemoteDev, hFeature, &currentCam->CamHeight);

    //  configure frame rate and Exposure
    SVFeatureGetByName(currentCam->hRemoteDev, "AcquisitionFrameRate", &hFeature);
    SVFeatureSetValueFloat(currentCam->hRemoteDev, hFeature, 15);       // 15fps

    SVFeatureGetByName(currentCam->hRemoteDev, "ExposureTime", &hFeature);
    SVFeatureSetValueFloat(currentCam->hRemoteDev, hFeature, 30000);    // exposure time 30ms

    // Start capturing thread
    ret = currentCam->StreamAcquisitionStart(4);
    ASSERT(ret == SV_ERROR_SUCCESS);

    // OnInitDialog() ==>  DisplayThreadfunction() ==> svCam->startAcqThread() 

    return 0;
}

void SVSCamDLL::Close()
{
    // Close the camera 
    if (currentCam == NULL)
        return;


    // 
    isStoping = true;

    //EnterCriticalSection(&csacquisition);
    currentCam->StreamAcquisitionStop();
    //currentCam->closeConnection();
    //LeaveCriticalSection(&csacquisition);

    isStoping = false;


    if (m_newBuffer.pImagePtr)
    {
        delete[] m_newBuffer.pImagePtr;
        m_newBuffer.pImagePtr = NULL;
    } 


}


// Store a copy of photo buffer in local variable
BYTE* SVSCamDLL::GetImageData()
{
    SV_RETURN ret = currentCam->GetSoftwareTriggerResult(&m_newBuffer);
    if (ret)
    {
        return m_newBuffer.pImagePtr;
    }
    else 
        return NULL;
}

std::string
fixedWidth2(uint64_t value, int width)
{
    char buffer[100];
    snprintf( buffer, sizeof(buffer), "%.*lld", width, value );
    return buffer;
}

void SVSCamDLL::SoftTrigger()
{
    currentCam->EmitSoftwareTriggerSignal();
}


bool SVSCamDLL::SoftTriggerAndSavePhoto()
{
    // Emit software trigger command
    // Joe: Software trigger for JT Demo
    SV_FEATURE_HANDLE hFeature = NULL;
    SV_RETURN ret = SV_ERROR_SUCCESS;

    OutputDebugString(L"SoftTriggerAndSavePhoto(): SoftTrigger signal emitted ...\n");

    SoftTrigger(); 

    // Check and Wait for the new photo 
    bool bReady = WaitTrigerFinish();

    if (bReady)
    {
        // Get photo copy and save ti to bitmap file
        SV_BUFFER_INFO newbufferInfo = { 0 }; 
        SV_RETURN ret = currentCam->GetSoftwareTriggerResult(&newbufferInfo);
        if (ret) 
        {
            string  filename = string("Image//") + currentCam->devInfo.displayName + "_"
                + fixedWidth2(newbufferInfo.iImageId, 8) + "_" + std::to_string(newbufferInfo.iTimeStamp) + ".bmp";
            SV_RETURN  ret = SVUtilSaveImageToFile(newbufferInfo, filename.c_str(), SV_IMAGE_FILE_BMP);

            string strOutput("SoftTriggerAndSavePhoto(): Save new photo: ");
            strOutput += filename + "\n";
            OutputDebugString(CA2W(strOutput.c_str()));
        }

        // Must release the memory  allocated inside SVSCamDLL for new received photo data  !!!!!!!
        if (newbufferInfo.pImagePtr)
        {
            delete[] newbufferInfo.pImagePtr;
            newbufferInfo.pImagePtr = NULL;
        } 
    } 
    else
    {
        // Time out and no photo captured.
        // ....
        return false;
    }

	
	return true;
}

// default time out is 1000ms
BOOL SVSCamDLL::WaitTrigerFinish()
{
    bool bReady = false;
    int delayCount = 100;
    while (!(bReady = currentCam->CheckSoftwareTriggerResult())) {
        Sleep(10);
        if (delayCount-- <= 0)
            return bReady;
    }
	
	return bReady;
}




int SVSCamDLL::startAcqThread()
{
    SV_RETURN ret = SV_ERROR_SUCCESS;
    SV_BUFFER_INFO bufferInfo = { 0 };
    while (!acqTerminated)
    {
        if (currentCam == NULL)
        {
            Sleep(20);
            continue;
        }

        if (!currentCam->isStreaming)
        {
            Sleep(20);
            continue;
        }

        if (isStoping )
        {
            Sleep(20);
            continue;
        }

        //EnterCriticalSection(&csacquisition);
        SV_RETURN ret = currentCam->grab(&bufferInfo);
        //LeaveCriticalSection(&csacquisition);

        if (SV_ERROR_ABORT == ret)
        {
            OutputDebugString(L"Warning: Grab returned SV_ERROR_ABORT ... Skipped it ....\n");
            //break;
        }
        else if (SV_ERROR_SUCCESS != ret)
        {
            continue;
        }

        /*
        if (SaveImg)
        {
            Sleep(20); 
        }

        // display images		else
        else 
        */
        {
            Sleep(20);
        }
    }

    // Release resource before quiting the grab thread
    if (bufferInfo.pImagePtr)
    {
        delete[] bufferInfo.pImagePtr;
        bufferInfo.pImagePtr = NULL;
    }

    SetEvent(m_acquisitionstopThread);

    OutputDebugString(L"\nThread Finished: To exit!!!!!!\n");
    return  0;
}

















