
// MiniSample DisplayDlg.cpp : implementation file
//

#include "stdafx.h"
#include "MiniSampleGui.h"
#include "MiniSampleGuiDlg.h"
#include "afxdialogex.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif


#define _CRTDBG_MAP_ALLOC
#include <stdlib.h>
#include <crtdbg.h>

#ifdef SVGENSDK_DYNAMIC_LOAD
//#define SVS_SDK_DLL „Path to SVGenSDK64.dll// use this define to load specific SDK Dll path otherwise it will load Dll on SVS_SDK_BIN_64 environmental variable
#include "sv_gen_sdk_dynamic.h"
#endif

std::wstring s2ws(const std::string& s)
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

std::string string_format(const std::string fmt_str, ...) {
    int final_n, n = ((int)fmt_str.size()) * 2; /* Reserve two times as much as the length of the fmt_str */
    std::unique_ptr<char[]> formatted;
    va_list ap;
    while(1) {
        formatted.reset(new char[n]); /* Wrap the plain char array into the unique_ptr */
        strcpy(&formatted[0], fmt_str.c_str());
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



// CAboutDlg dialog used for App About

class CAboutDlg : public CDialogEx
{
public:
    CAboutDlg();

    // Dialog Data
    enum { IDD = IDD_ABOUTBOX };

protected:
    virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support

    // Implementation
protected:
    DECLARE_MESSAGE_MAP()
};

CAboutDlg::CAboutDlg() : CDialogEx(CAboutDlg::IDD)
{
}

void CAboutDlg::DoDataExchange(CDataExchange* pDX)
{
    CDialogEx::DoDataExchange(pDX);
}

BEGIN_MESSAGE_MAP(CAboutDlg, CDialogEx)

END_MESSAGE_MAP()


// CMiniSampleDisplayDlg dialog

unsigned long __stdcall   DisplayThreadfunction(void * context)
{
    // Process only the currently selected camera.
    CMiniSampleDisplayDlg  *svCam = (CMiniSampleDisplayDlg*)context;
    if (svCam != NULL)

        svCam->startAcqThread();
    return 0;
}


int CMiniSampleDisplayDlg::startAcqThread()
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

        EnterCriticalSection(&csacquisition);
        SV_RETURN ret = currentCam->grab(&bufferInfo);
        LeaveCriticalSection(&csacquisition);

        if (SV_ERROR_ABORT == ret)
            break;
        else if (SV_ERROR_SUCCESS != ret)
            continue;
        // Save images
        if (SaveImg)
        {
            if (bufferInfo.pImagePtr == NULL)
            {
                //delete bufferInfo;
                continue;
            }
            if ( save_list.size() < save_img_buffer_count)
            {
                SV_BUFFER_INFO *save_bufferInfo = new SV_BUFFER_INFO();
                save_bufferInfo->pImagePtr = new  uint8_t[bufferInfo.iImageSize];

                save_bufferInfo->iSizeX = bufferInfo.iSizeX;
                save_bufferInfo->iSizeY = bufferInfo.iSizeY;
                save_bufferInfo->iImageId = bufferInfo.iImageId;
                save_bufferInfo->iPixelType = bufferInfo.iPixelType;
                save_bufferInfo->iImageSize = bufferInfo.iImageSize;
                save_bufferInfo->iTimeStamp = bufferInfo.iTimeStamp;
                memcpy(save_bufferInfo->pImagePtr, bufferInfo.pImagePtr, bufferInfo.iImageSize);
                save_list.push_back(save_bufferInfo);
            }

        }

        // display images		else
        else {
            MSG msg;
            while (PeekMessage(&msg, NULL, WM_DISPLAY_IMAGE, WM_DISPLAY_IMAGE, PM_REMOVE))
            {
                continue;
            }

            PostMessage(WM_DISPLAY_IMAGE, 0, (LONG_PTR)(&bufferInfo));
        }
    }

    if (bufferInfo.pImagePtr)
    {
        delete[] bufferInfo.pImagePtr;
        bufferInfo.pImagePtr = NULL;
    }

    SetEvent(m_acquisitionstopThread);


    return  0;
}

UINT  SaveThreadfunction(LPVOID pParamt)
{

    // Process only the currently selected camera.
    CMiniSampleDisplayDlg  *svCam = (CMiniSampleDisplayDlg*)pParamt;
    svCam->save_img_count = 0;
    while (!svCam->SaveTerminated)
    {
        if (svCam->save_list.size() < svCam->save_img_buffer_count|| !svCam->SaveImg)
        {
            Sleep(20);
            continue;
        }

        for (unsigned j = 0; j < svCam->save_list.size(); j++)
        {
            if (svCam->SaveTerminated)
                break;

            SV_BUFFER_INFO* buff_inf = svCam->save_list.at(j);
            string  filename = string("Image//") + svCam->currentCam->devInfo.displayName  + std::to_string(buff_inf->iImageId) + "_" + std::to_string(buff_inf->iTimeStamp) + "_.png";
            SV_RETURN  ret = SVUtilSaveImageToPNGFile(*buff_inf, filename.c_str());
            if (ret == SV_ERROR_SUCCESS)
            {
                svCam->save_img_count++;
                const CString  strValue(to_string(svCam->save_img_count).c_str()) ;
                LPCWSTR wstrValue = static_cast<LPCWSTR>(strValue);
                svCam->m_saved_frame_counter.SetWindowTextW(wstrValue);
            }
        }

        for (unsigned j = 0; j < svCam->save_list.size(); j++)
        {
            SV_BUFFER_INFO*  buff = svCam->save_list.at(j);
            delete[]  buff->pImagePtr;
            buff->pImagePtr = NULL;
            delete buff;
            buff = NULL;
        }

        svCam->save_list.clear();
    }

    SetEvent(svCam->m_SaveStopThread);

    return 0;
}


CMiniSampleDisplayDlg::CMiniSampleDisplayDlg(CWnd* pParent /*=NULL*/)
    : CDialogEx(CMiniSampleDisplayDlg::IDD, pParent)
{

    m_hIcon = AfxGetApp()->LoadIcon(IDR_MAINFRAME);

    sdk_init_done = false;
}




CMiniSampleDisplayDlg::~CMiniSampleDisplayDlg()
{
    if (m_bAutoTrigger)
        KillTimer(1);
}

void CMiniSampleDisplayDlg::DoDataExchange(CDataExchange* pDX)
{
    CDialogEx::DoDataExchange(pDX);

    DDX_Control(pDX, IDC_COMBO1, m_cameras);
    DDX_Control(pDX, IDC_CAM_FOUND, m_cam_found);
    DDX_Control(pDX, IDC_CHECK1, m_aoi);
    DDX_Control(pDX, IDC_COMBO2, m_auto_white_balance);
    DDX_Control(pDX, IDC_COMBO3, m_piv_mode);
    DDX_Control(pDX, IDC_CHECK2, m_save_img);

    DDX_Control(pDX, IDC_FRAME, m_frame_counter);
    DDX_Control(pDX, IDC_SAVED_FRAME, m_saved_frame_counter);
    DDX_Control(pDX, ID_LABEL_JT_SVS_API_DETAILS, m_SVSCamDLL_Details);
}


BEGIN_MESSAGE_MAP(CMiniSampleDisplayDlg, CDialogEx)
ON_WM_SYSCOMMAND()
ON_WM_PAINT()
ON_WM_QUERYDRAGICON()
ON_BN_CLICKED(IDC_CHECK1, &CMiniSampleDisplayDlg::OnBnClickedCheck1)
ON_BN_CLICKED(IDC_BUTTON_START, &CMiniSampleDisplayDlg::OnBnClickedButtonStart)
ON_BN_CLICKED(IDC_BUTTON_STOP, &CMiniSampleDisplayDlg::OnBnClickedButtonStop)
ON_BN_CLICKED(IDC_BUTTON1, &CMiniSampleDisplayDlg::OnBnClickedButton1)
ON_CBN_SELCHANGE(IDC_COMBO1, &CMiniSampleDisplayDlg::OnCbnSelchangeCombo1)


ON_BN_CLICKED(IDOK, &CMiniSampleDisplayDlg::OnBnClickedOk)
ON_CBN_SELCHANGE(IDC_COMBO2, &CMiniSampleDisplayDlg::OnCbnSelchangeCombo2)
ON_CBN_SELCHANGE(IDC_COMBO3, &CMiniSampleDisplayDlg::OnCbnSelchangeCombo3)

ON_WM_CLOSE()
ON_BN_CLICKED(IDC_CHECK2, &CMiniSampleDisplayDlg::OnBnClickedSave)

ON_MESSAGE(WM_DISPLAY_IMAGE, WMDisplayImage)

ON_BN_CLICKED(IDC_BUTTON_SOFTTRIGGER, &CMiniSampleDisplayDlg::OnBnClickedButtonSofttrigger)
ON_BN_CLICKED(IDC_BTN_OPEN_SVS_CAMERA, &CMiniSampleDisplayDlg::OnBnClickedBtnOpenSvsCamera)
ON_BN_CLICKED(IDC_BTN_SOFTWARE_TRIGGER_ONE_PHOTO, &CMiniSampleDisplayDlg::OnBnClickedBtnSoftwareTriggerOnePhoto)
ON_BN_CLICKED(IDC_BTN_CLOSE_SVS_CAMERA, &CMiniSampleDisplayDlg::OnBnClickedBtnCloseSvsCamera)
ON_BN_CLICKED(IDC_BTN_SW_TRIGGER_TIMER_1FPS, &CMiniSampleDisplayDlg::OnBnClickedBtnSwTriggerTimer1fps)
ON_WM_TIMER()
ON_BN_CLICKED(IDC_BTN_JT_API_SVS_CAMERA_OPEN, &CMiniSampleDisplayDlg::OnBnClickedBtnJtApiSvsCameraOpen)
ON_BN_CLICKED(IDC_BTN_JT_API_SVS_CAMERA_CAPTURE_PHOTO_SAVE, &CMiniSampleDisplayDlg::OnBnClickedBtnJtApiSvsCameraCapturePhotoSave)
ON_BN_CLICKED(IDC_BTN_JT_API_SVS_CAMERA_CLOSE, &CMiniSampleDisplayDlg::OnBnClickedBtnJtApiSvsCameraClose)
END_MESSAGE_MAP()



// CMiniSampleDisplayDlg message handlers
BOOL CMiniSampleDisplayDlg::OnInitDialog()
{
    InitializeCriticalSection(&csacquisition);
    currentCam = NULL;
    CDialogEx::OnInitDialog();




    // Add "About..." menu item to system menu.

    // IDM_ABOUTBOX must be in the system command range.
    ASSERT((IDM_ABOUTBOX & 0xFFF0) == IDM_ABOUTBOX);
    ASSERT(IDM_ABOUTBOX < 0xF000);

    CMenu* pSysMenu = GetSystemMenu(FALSE);
    if (pSysMenu != NULL)
    {
        BOOL bNameValid;
        CString strAboutMenu;
        bNameValid = strAboutMenu.LoadString(IDS_ABOUTBOX);
        ASSERT(bNameValid);
        if (!strAboutMenu.IsEmpty())
        {
            pSysMenu->AppendMenu(MF_SEPARATOR);
            pSysMenu->AppendMenu(MF_STRING, IDM_ABOUTBOX, strAboutMenu);
        }
    }

    // Set the icon for this dialog.  The framework does this automatically
    //  when the application's main window is not a dialog
    SetIcon(m_hIcon, TRUE);			// Set big icon
    SetIcon(m_hIcon, FALSE);		// Set small icon
    cam_container = new CameraContainer();

    m_acquisitionstopThread = CreateEvent(NULL, false, false, NULL);
    m_SaveStopThread = CreateEvent(NULL, false, false,  NULL);

    // number of image to be saved befor grabing the next one.
    save_img_buffer_count = 2;
    acqTerminated = false;
    SaveTerminated = false;
    isStoping = false;

    m_save_thread = AfxBeginThread(SaveThreadfunction, this);
    m_thread = CreateThread(NULL, 0, DisplayThreadfunction, (void *)this, 0, NULL);

    m_auto_white_balance.AddString(L"Reset");
    m_auto_white_balance.AddString(L"Continuous");
    m_auto_white_balance.AddString(L"Once");
    m_auto_white_balance.AddString(L"Off");


    m_piv_mode.AddString(L"Off");
    m_piv_mode.AddString(L"ON");

    m_aoi.SetCheck(0);

    m_nSWTriggerCount = 0;

    //ShowWindow(SW_SHOW);
    //displg.ShowWindow(SW_SHOWMAXIMIZED);

    return TRUE;  // return TRUE  unless you set the focus to a control

}

void CMiniSampleDisplayDlg::OnSysCommand(UINT nID, LPARAM lParam)
{
    if ((nID & 0xFFF0) == IDM_ABOUTBOX)
    {
        CAboutDlg dlgAbout;
        dlgAbout.DoModal();
    }
    else
    {
        CDialogEx::OnSysCommand(nID, lParam);
    }
}


// If you add a minimize button to your dialog, you will need the code below
//  to draw the icon.  For MFC applications using the document/view model,
//  this is automatically done for you by the framework.

void CMiniSampleDisplayDlg::OnPaint()
{
    if (IsIconic())
    {
        CPaintDC dc(this); // device context for painting

        SendMessage(WM_ICONERASEBKGND, reinterpret_cast<WPARAM>(dc.GetSafeHdc()), 0);

        // Center icon in client rectangle
        int cxIcon = GetSystemMetrics(SM_CXICON);
        int cyIcon = GetSystemMetrics(SM_CYICON);
        CRect rect;
        GetClientRect(&rect);
        int x = (rect.Width() - cxIcon + 1) / 2;
        int y = (rect.Height() - cyIcon + 1) / 2;

        // Draw the icon
        dc.DrawIcon(x, y, m_hIcon);
    }
    else
    {
        CDialogEx::OnPaint();
    }
}

// The system calls this function to obtain the cursor to display while the user drags
//  the minimized window.
HCURSOR CMiniSampleDisplayDlg::OnQueryDragIcon()
{
    return static_cast<HCURSOR>(m_hIcon);
}


void CMiniSampleDisplayDlg::OnBnClickedCheck1()
{

    if (currentCam == NULL)
        return;

    OnBnClickedButtonStop();
    if (m_aoi.GetCheck())
    {
        int ret = currentCam->setAreaOfInterest(200, 200, 0, 0);
        if (ret != 0)
            m_aoi.SetCheck(0);
    }
    else
    {
        int  ret = currentCam->setAreaOfInterest(currentCam->CamWidth, currentCam->CamHeight, 0, 0);
        if (ret != 0)
            m_aoi.SetCheck(1);
    }
    OnBnClickedButtonStart();
}



void CMiniSampleDisplayDlg::OnBnClickedButton2()
{
    // TODO: Add your control notification handler code here
}


void CMiniSampleDisplayDlg::OnBnClickedButtonStart()
{
    if (currentCam == NULL)
        return;

    BeginWaitCursor();
    currentCam->StreamAcquisitionStart(4);
    EndWaitCursor();
    m_aoi.EnableWindow(false);

}


void CMiniSampleDisplayDlg::OnBnClickedButtonStop()
{
    if (currentCam == NULL)
        return;

    isStoping = true;

    BeginWaitCursor();


    EnterCriticalSection(&csacquisition);
    currentCam->StreamAcquisitionStop();
    LeaveCriticalSection(&csacquisition);
    EndWaitCursor();

    isStoping = false;

    m_aoi.EnableWindow(true);


    MSG msg;
    while (PeekMessage(&msg, NULL, WM_DISPLAY_IMAGE, WM_DISPLAY_IMAGE, PM_REMOVE))
    {

        continue;
    }


}


void CMiniSampleDisplayDlg::OnBnClickedButton1()
{


    BeginWaitCursor();

    SV_RETURN ret = SV_ERROR_SUCCESS;

    if (!sdk_init_done)
    {
        if (!cam_container->initSDK())
        {
            m_cam_found.SetWindowTextW(L" initialisation failed !!");
            EndWaitCursor();
            return;
        }
        sdk_init_done = true;
    }


    int  CamAnz = (int)cam_container->devicediscovery();
    string  st1 = "Cameras found: ";
    int currentIndex = m_cameras.GetCurSel();
    m_cameras.ResetContent();

    if (CamAnz != 0)
    {
        for (vector<Camera *>::iterator it = cam_container->sv_cam_list.begin(); it != cam_container->sv_cam_list.end(); ++it)
            if (*it)
            {
                const CString  strValue((*it)->devInfo.model);
                LPCWSTR ws = static_cast<LPCWSTR>(strValue);
                m_cameras.AddString(ws);
            }

        string st2 = to_string(CamAnz);
        st1.append(st2);
        m_cam_found.SetWindowTextW(CString(st1.c_str()));
    }
    else
        m_cam_found.SetWindowTextW(L" NO Cameras found !!");
    m_cameras.SetCurSel(currentIndex);
    EndWaitCursor();
}


void CMiniSampleDisplayDlg::OnCbnSelchangeCombo1()
{

    int sl = m_cameras.GetCurSel();
    currentCam = cam_container->sv_cam_list.at(sl);
    currentCam->openConnection();
    //currentCam->SetTriggerMode(false);
    currentCam->SetTriggerMode(true);    // enable software trigger by default  
    currentCam->setAcquisitionMode(2);

    // Get  Camera Information
    currentCam->CamHeight = 0;
    currentCam->CamWidth = 0;
    SV_RETURN ret = SV_ERROR_SUCCESS;
    SV_FEATURE_HANDLE hFeature = NULL;

    // Get The Camera Size
    SVFeatureGetByName(currentCam->hRemoteDev, "SensorWidth", &hFeature);
    SVFeatureGetValueInt64(currentCam->hRemoteDev, hFeature, &currentCam->CamWidth);

    SVFeatureGetByName(currentCam->hRemoteDev, "SensorHeight", &hFeature);
    SVFeatureGetValueInt64(currentCam->hRemoteDev, hFeature, &currentCam->CamHeight);

    //  configure frame rate and Exposure
    SVFeatureGetByName(currentCam->hRemoteDev, "AcquisitionFrameRate", &hFeature);
    SVFeatureSetValueFloat(currentCam->hRemoteDev, hFeature, 5);

    SVFeatureGetByName(currentCam->hRemoteDev, "ExposureTime", &hFeature);
    SVFeatureSetValueFloat(currentCam->hRemoteDev, hFeature, 50000);


    bool isPivmod = false;


    ret = currentCam->GetPivMode(&isPivmod);
    if (ret == SV_ERROR_SUCCESS)
    {
        if (isPivmod)
        {
            m_piv_mode.SetCurSel(1);
            piv_mode_nabled = true;
        }

        else
        {
            m_piv_mode.SetCurSel(0);
            piv_mode_nabled = false;
        }
    }
    else
    {
        m_piv_mode.EnableWindow(false);
        piv_mode_nabled = false;
    }


    int ChkBox = m_save_img.GetCheck();
    CString str;

    if (ChkBox == BST_UNCHECKED)
        SaveImg = false;
    else if (ChkBox == BST_CHECKED)
        SaveImg = true;;



}


LRESULT CMiniSampleDisplayDlg::WMDisplayImage(WPARAM WParam, LPARAM LParam)
{

    MSG msg;
    while (PeekMessage(&msg, NULL, WM_DISPLAY_IMAGE, WM_DISPLAY_IMAGE, PM_REMOVE))
    {
        continue;
    }
    // Obtain image information structure
    SV_BUFFER_INFO *   ImageInfo = (SV_BUFFER_INFO *)LParam;
    if (ImageInfo == NULL)
        return 0;

    if (ImageInfo->pImagePtr == NULL)
        return 0;

    if (ImageInfo)
    {

        // update the displayed image id
        const CString  strValue(to_string(ImageInfo->iImageId).c_str());
        LPCWSTR wstrValue = static_cast<LPCWSTR>(strValue);
        m_frame_counter.SetWindowTextW(wstrValue);

    }


    // Check if a RGB image( Bayer buffer format) arrived
    bool isImgRGB = false;
    int pDestLength = (int)(ImageInfo->iSizeX * ImageInfo->iSizeY);

    //  Bayer buffer format(up id: 8)
    if ((ImageInfo->iPixelType & SV_GVSP_PIX_ID_MASK) >= 8)
    {
        isImgRGB = true;
        pDestLength = 3 * pDestLength;
    }

    size_t sizeX = ImageInfo->iSizeX;
    size_t sizeY = ImageInfo->iSizeY;


    // check if the pDestLength has been changed and allocate a  buffer if needed
    if (isImgRGB)
    {
        if (currentCamSize != pDestLength)
        {
            if ((NULL != displg.ImageData_RGB))
            {
                GlobalFree(displg.ImageData_RGB);
                displg.ImageData_RGB = (unsigned char *)GlobalAlloc(GMEM_FIXED, pDestLength);
            }
            currentCamSize = pDestLength;
        }

        if ((NULL == displg.ImageData_RGB))
        {

            displg.ImageData_RGB = (unsigned char *)GlobalAlloc(GMEM_FIXED, pDestLength);
        }
    }
    else
    {

        if (currentCamSize != pDestLength)
        {
            if ((NULL != displg.ImageData_MONO))
            {
                GlobalFree(displg.ImageData_MONO);
                displg.ImageData_MONO = (unsigned char *)GlobalAlloc(GMEM_FIXED, pDestLength);
            }

            currentCamSize = pDestLength;
        }

        if ((NULL == displg.ImageData_MONO))
            displg.ImageData_MONO = (unsigned char *)GlobalAlloc(GMEM_FIXED, pDestLength);
    }



    // 8 bit Format
    if ((ImageInfo->iPixelType & SV_GVSP_PIX_EFFECTIVE_PIXELSIZE_MASK) == SV_GVSP_PIX_OCCUPY8BIT)
    {
        if (isImgRGB)
        {
            // Convert to 24 bit and display image
            SVUtilBufferBayerToRGB(*ImageInfo, displg.ImageData_RGB, pDestLength);

            displg.ShowImageRGB(sizeX, sizeY, displg.ImageData_RGB);
        }
        else
        {
            displg.ShowImageMono(sizeX, sizeY, ImageInfo->pImagePtr);
        }
    }

    // 12 bit Format
    // Check if a conversion of a 12-bit image is needed
    if ((ImageInfo->iPixelType & SV_GVSP_PIX_EFFECTIVE_PIXELSIZE_MASK) == SV_GVSP_PIX_OCCUPY12BIT)
    {
        if (isImgRGB)
        {
            // Convert to 24 bit and display image
            SVUtilBufferBayerToRGB(*ImageInfo, displg.ImageData_RGB, pDestLength);
            displg.ShowImageRGB(sizeX, sizeY, displg.ImageData_RGB);
        }
        else
        {
            // Convert to 8 bit and display image
            SVUtilBuffer12BitTo8Bit(*ImageInfo, displg.ImageData_MONO, pDestLength);
            displg.ShowImageMono(sizeX, sizeY, displg.ImageData_MONO);

        }
    }


    // 16 bit Format
    // Check if a conversion of a 16-bit image is needed
    if ((ImageInfo->iPixelType & SV_GVSP_PIX_EFFECTIVE_PIXELSIZE_MASK) == SV_GVSP_PIX_OCCUPY16BIT)
    {
        if (isImgRGB)
        {
            // Convert to 24 bit and display image
            SVUtilBufferBayerToRGB(*ImageInfo, displg.ImageData_RGB, (int)pDestLength);
            displg.ShowImageRGB(sizeX, sizeY, displg.ImageData_RGB);
        }
        else
        {
            // Convert to 8 bit and display image
            SVUtilBuffer16BitTo8Bit(*ImageInfo, displg.ImageData_MONO, (int)pDestLength);
            displg.ShowImageMono(sizeX, sizeY, displg.ImageData_MONO);
        }
    }




    while (PeekMessage(&msg, NULL, WM_DISPLAY_IMAGE, WM_DISPLAY_IMAGE, PM_REMOVE))
    {
        continue;
    }

    return 0;
}



void CMiniSampleDisplayDlg::OnBnClickedOk()
{
    BeginWaitCursor();

    PostMessage(WM_CLOSE, 0, NULL);
    EndWaitCursor();

}

void CMiniSampleDisplayDlg::OnBnClickedCheck4()
{
    if (currentCam == NULL)
        return;
    SV_FEATURE_HANDLE hFeature = NULL;
    SV_RETURN ret = SV_ERROR_UNKNOWN;

    //retrieve the payload size to allocate the buffers
    ret = SVFeatureGetByName(currentCam->hRemoteDev, "TriggerMode", &hFeature);

    bool trigger_on = false;
    if (m_trigger_mode.GetCheck() !=0)
        bool trigger_on = true;

    if (trigger_on)
    {
        SVFeatureSetValueEnum(currentCam->hRemoteDev, hFeature, "On");
    }
    else

        SVFeatureSetValueEnum(currentCam->hRemoteDev, hFeature, "Off");
}

void CMiniSampleDisplayDlg::OnCbnSelchangeCombo2()
{
    if (currentCam == NULL)
        return;

    int sl = m_auto_white_balance.GetCurSel();
    currentCam->SetAutoWhitebalance(sl);
}

void CMiniSampleDisplayDlg::OnCbnSelchangeCombo3()
{
    BeginWaitCursor();
    if (currentCam == NULL)
        return;

    currentCam->StreamAcquisitionStop();


    int sl = this->m_piv_mode.GetCurSel();
    if (sl == 0)
    {
        currentCam->SetPivMode(false);
        piv_mode_nabled = false;
    }
    else
    {
        currentCam->SetPivMode(true);
        piv_mode_nabled = true;
    }
    currentCam->StreamAcquisitionStart(4);
    EndWaitCursor();
}

void CMiniSampleDisplayDlg::OnClose()
{
    if (!acqTerminated)
    {

        acqTerminated = true;
        WaitForSingleObject(m_acquisitionstopThread, INFINITE);
        ResetEvent(m_acquisitionstopThread);
        CloseHandle(m_thread);

        MSG msg;
        while (PeekMessage(&msg, NULL, WM_DISPLAY_IMAGE, WM_DISPLAY_IMAGE, PM_REMOVE))
        {
            continue;
        }
    }



    if (!SaveTerminated)
    {
        SaveTerminated = true;
        WaitForSingleObject(m_SaveStopThread, INFINITE);
        PostThreadMessage(m_save_thread->m_nThreadID, 0, 0, 0);
        ResetEvent(m_SaveStopThread);

        for (unsigned j = 0; j < save_list.size(); j++)
        {
            SV_BUFFER_INFO*  buff = save_list.at(j);
            delete[]  buff->pImagePtr;
            buff->pImagePtr = NULL;
            delete buff;
            buff = NULL;
        }

        if (save_list.size() != 0)
            save_list.clear();
    }

    if (cam_container)
        delete  cam_container;

    DeleteCriticalSection(&csacquisition);

    CDialogEx::OnClose();
}

void CMiniSampleDisplayDlg::OnBnClickedSave()
{
	int ChkBox = m_save_img.GetCheck();
	CString str;

	if (ChkBox == BST_UNCHECKED)
		SaveImg = false;
	else if (ChkBox == BST_CHECKED)
		SaveImg = true;
}

void CMiniSampleDisplayDlg::OnBnClickedButtonSofttrigger()
{
    // Joe: Software trigger for JT Demo
    SV_FEATURE_HANDLE hFeature = NULL;
    SV_RETURN ret = SV_ERROR_SUCCESS;

    hFeature = NULL;
    uint32_t ExecuteTimeout = 1000;
    ret = SVFeatureGetByName(currentCam->hRemoteDev, "TriggerSoftware", &hFeature);

    if (SV_ERROR_SUCCESS == ret)
        ret = SVFeatureCommandExecute(currentCam->hRemoteDev, hFeature, ExecuteTimeout);

}

void CMiniSampleDisplayDlg::OnBnClickedBtnOpenSvsCamera()
{
    // Dicovery all SVS cameras and open the first one 
    SV_RETURN ret = SV_ERROR_SUCCESS;

    if (!sdk_init_done)
    {
        if (!cam_container->initSDK())
        {
            m_cam_found.SetWindowTextW(L" initialisation failed !!");
            EndWaitCursor();
            return;
        }
        sdk_init_done = true;
    }


    int  CamAnz = (int)cam_container->devicediscovery();
    string  st1 = "Cameras found: ";
    int currentIndex = m_cameras.GetCurSel();
    m_cameras.ResetContent();

    if (CamAnz != 0)
    {
        for (vector<Camera *>::iterator it = cam_container->sv_cam_list.begin(); it != cam_container->sv_cam_list.end(); ++it)
            if (*it)
            {
                const CString  strValue((*it)->devInfo.model);
                LPCWSTR ws = static_cast<LPCWSTR>(strValue);
                m_cameras.AddString(ws);
            }

        string st2 = to_string(CamAnz);
        st1.append(st2);
        m_cam_found.SetWindowTextW(CString(st1.c_str()));
    }
    else
        m_cam_found.SetWindowTextW(L" NO Cameras found !!");
    m_cameras.SetCurSel(currentIndex);

    // Open the first camera
    // Set to software trigger mode
    currentCam = cam_container->sv_cam_list.at(0);
    currentCam->openConnection();
    currentCam->SetTriggerMode(true);    // enable software trigger by default  
    currentCam->setAcquisitionMode(2);   // Continous mode

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
    SVFeatureSetValueFloat(currentCam->hRemoteDev, hFeature, 15);

    SVFeatureGetByName(currentCam->hRemoteDev, "ExposureTime", &hFeature);
    SVFeatureSetValueFloat(currentCam->hRemoteDev, hFeature, 30000);

    // Start capturing thread
    currentCam->StreamAcquisitionStart(4);

    // OnInitDialog() ==>  DisplayThreadfunction() ==> svCam->startAcqThread() 

    return;
}


std::string
fixedWidth( int value, int width )
{
    char buffer[100];
    snprintf( buffer, sizeof(buffer), "%.*d", width, value );
    return buffer;
}

void CMiniSampleDisplayDlg::OnBnClickedBtnSoftwareTriggerOnePhoto()
{
    // Emit software trigger command
    // Joe: Software trigger for JT Demo
    SV_FEATURE_HANDLE hFeature = NULL;
    SV_RETURN ret = SV_ERROR_SUCCESS;

    hFeature = NULL;
    uint32_t ExecuteTimeout = 1000;
    ret = SVFeatureGetByName(currentCam->hRemoteDev, "TriggerSoftware", &hFeature);

    if (SV_ERROR_SUCCESS == ret)
        ret = SVFeatureCommandExecute(currentCam->hRemoteDev, hFeature, ExecuteTimeout);

    m_nSWTriggerCount++;
    std::string strMsg = string_format("sw trigger %08d", m_nSWTriggerCount);
    
    m_cam_found.SetWindowTextW(s2ws(strMsg).c_str());

    // TODO: Check and Wait for the new photo 
    bool bReady = false;
    while (!(bReady = currentCam->CheckSoftwareTriggerResult())) {
        Sleep(10);
    }

    if (bReady)
    {
        SV_BUFFER_INFO newbufferInfo = { 0 }; 
        SV_RETURN ret = currentCam->GetSoftwareTriggerResult(&newbufferInfo);
        if (ret) 
        {
            string  filename = string("Image//") + currentCam->devInfo.displayName + "_"
                + fixedWidth(newbufferInfo.iImageId, 8) + "_" + std::to_string(newbufferInfo.iTimeStamp) + ".bmp";
            SV_RETURN  ret = SVUtilSaveImageToFile(newbufferInfo, filename.c_str(), SV_IMAGE_FILE_BMP);
        }

        if (newbufferInfo.pImagePtr)
        {
            delete[] newbufferInfo.pImagePtr;
            newbufferInfo.pImagePtr = NULL;
        } 
    } 

}


void CMiniSampleDisplayDlg::OnBnClickedBtnCloseSvsCamera()
{
    // Close the camera

    if (currentCam == NULL)
        return;

    isStoping = true;

    EnterCriticalSection(&csacquisition);
    currentCam->StreamAcquisitionStop();
    LeaveCriticalSection(&csacquisition);

    isStoping = false;

    // Tips
    m_cam_found.SetWindowTextW(L"SVS Camera Closed !!");
    m_nSWTriggerCount = 0;

    if (m_newBuffer.pImagePtr)
    {
        delete[] m_newBuffer.pImagePtr;
        m_newBuffer.pImagePtr = NULL;
    } 

    MSG msg;
    while (PeekMessage(&msg, NULL, WM_DISPLAY_IMAGE, WM_DISPLAY_IMAGE, PM_REMOVE))
    { 
        continue;
    }

}


void CMiniSampleDisplayDlg::OnBnClickedBtnSwTriggerTimer1fps()
{
    // Joe: 1 fps timer
    if (false == m_bAutoTrigger) {
        m_bAutoTrigger = true;
        SetTimer(1, 1000, NULL);
        m_nAutoTriggerCount = 0;
        GetDlgItemText(IDC_BTN_SW_TRIGGER_TIMER_1FPS, strAutoTriggerText);
    }
    else
    {
        KillTimer(1); 
        m_bAutoTrigger = false;
        m_nAutoTriggerCount = 0;
        SetDlgItemText(IDC_BTN_SW_TRIGGER_TIMER_1FPS, strAutoTriggerText);
    }
}


void CMiniSampleDisplayDlg::OnTimer(UINT_PTR nIDEvent)
{
    // Simulate software trigger 
    PostMessage(WM_COMMAND, MAKEWPARAM(IDC_BTN_SOFTWARE_TRIGGER_ONE_PHOTO, BN_CLICKED), NULL);  

    m_nAutoTriggerCount++;
    CString strNewText; 
    strNewText.Format(L"%s-%08d", strAutoTriggerText, m_nAutoTriggerCount);
    SetDlgItemText(IDC_BTN_SW_TRIGGER_TIMER_1FPS, strNewText);

    CDialogEx::OnTimer(nIDEvent);
}









// SVSCamDLL.h/cpp example



void CMiniSampleDisplayDlg::OnBnClickedBtnJtApiSvsCameraOpen()
{
    m_nPhotoCounter = 0;
    int ret = m_SVSCamDLLInst.Open();
    if (0 == ret)
        m_SVSCamDLL_Details.SetWindowText(L"Camera Opened Successfully!");
    else
        m_SVSCamDLL_Details.SetWindowText(L"Camera Opened Faied!");

    OutputDebugString(CA2W("OnBnClickedBtnJtApiSvsCameraOpen() called ...\n"));
}


void CMiniSampleDisplayDlg::OnBnClickedBtnJtApiSvsCameraCapturePhotoSave()
{
    m_nPhotoCounter++;

    bool bRet = m_SVSCamDLLInst.SoftTriggerAndSavePhoto();

    if (bRet) {
        const CString  strValue(to_string(m_nPhotoCounter).c_str());
        LPCWSTR wstrValue = static_cast<LPCWSTR>(strValue);
        m_SVSCamDLL_Details.SetWindowTextW(wstrValue);
    }
    else {
        m_SVSCamDLL_Details.SetWindowText(L"Camera Captured Faied!"); 
        OutputDebugString(CA2W("OnBnClickedBtnJtApiSvsCameraCapturePhotoSave() failed ...\n"));
    }
}


void CMiniSampleDisplayDlg::OnBnClickedBtnJtApiSvsCameraClose()
{
    m_SVSCamDLLInst.Close();
    m_SVSCamDLL_Details.SetWindowText(L"Camera Closed Successfully!"); 
    m_nPhotoCounter = 0;

    OutputDebugString(CA2W("OnBnClickedBtnJtApiSvsCameraClose() called ...\n"));
}















