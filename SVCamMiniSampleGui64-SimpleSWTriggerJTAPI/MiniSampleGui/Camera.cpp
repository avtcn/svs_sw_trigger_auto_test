
#include "Camera.h"
#include <stdio.h>


Camera::Camera(SV_DEVICE_INFO _devInfo)
{
    InitializeCriticalSection(&cscopybufferinfo);

    devInfo = _devInfo;
    if (0 == _stricmp("GEV", _devInfo.tlType))
        sv_cam_type = TL_GEV;

    if (0 == _stricmp("U3V", _devInfo.tlType))
        sv_cam_type = TL_U3V;

    CamWidth = 0;
    CamHeight = 0;

    hDevice = NULL;
    hRemoteDev = NULL;

}
Camera::~Camera()
{

    StreamAcquisitionStop();
    closeConnection();

    hDevice = NULL;
    hRemoteDev = NULL;
    is_opened = false;
    isStreaming = false;

    // Uninitilize local copy of buffer info
    EnterCriticalSection(&cscopybufferinfo);
    if (NULL == copyBufferInfo.pImagePtr)
    {
        delete[] copyBufferInfo.pImagePtr;
        copyBufferInfo.pImagePtr = NULL;
    } 
    LeaveCriticalSection(&cscopybufferinfo);

    DeleteCriticalSection(&cscopybufferinfo);
}

SV_RETURN   Camera::openConnection()
{
    if (is_opened)
        return 0;

    SV_RETURN ret = SV_ERROR_SUCCESS;

    //Open the device with device id (devInfo.uid) connected to the interface (devInfo.hParentIF)
    ret = SVInterfaceDeviceOpen(devInfo.hParentIF, devInfo.uid, SV_DEVICE_ACCESS_CONTROL, &hDevice, &hRemoteDev);

    printf("open connection\n");

    if (ret == SV_ERROR_SUCCESS)
        is_opened = true;
    else
        return ret;

    char streamId0[SV_STRING_SIZE] = { 0 };
    size_t streamId0Size = SV_STRING_SIZE;

    // retrieve the stream ID
    ret = SVDeviceGetStreamId(hDevice, 0, streamId0, &streamId0Size);

    if (SV_ERROR_SUCCESS != ret)
    {
        printf(":%s SVDeviceGetStreamId Failed!:%d\n", __FUNCTION__, ret);
        return ret;
    }

    //open the Streaming channel with the retrieved stream ID
    ret = SVDeviceStreamOpen(hDevice, streamId0, &hDS);
    if (SV_ERROR_SUCCESS != ret)
    {
        printf(":%s SVDeviceStreamOpen Failed!:%d\n", __FUNCTION__, ret);
        return ret;
    }

    return ret;
}

SV_RETURN Camera::closeConnection()
{
    printf("close connection\n");
    StreamAcquisitionStop();
    SVStreamClose(hDS);
    SV_RETURN ret = SV_ERROR_SUCCESS;
    ret = SVDeviceClose(hDevice);
    hDevice = NULL;
    return ret;
}




uint64_t  Camera::getPayloadsize()
{

    // retrieve the effective payload size from stream if it exists if not retrieve it from the remote device.
    uint64_t payloadSizeFinal = 0;
    SV_DS_INFO dsInfo = { 0 };
    SV_RETURN ret = SVStreamGetInfo(hDS, &dsInfo);
    SV_FEATURE_HANDLE hFeature = NULL;
    if (SV_ERROR_SUCCESS == ret)
    {
        if (dsInfo.isPayloadSizeDefined)
            payloadSizeFinal = dsInfo.payloadSize;
    }

    if (0 == payloadSizeFinal)
    {
        int64_t payloadSizeDS = 0;
        SVFeatureGetByName(hDS, "PayloadSize", &hFeature);
        SVFeatureGetValueInt64(hDS, hFeature, &payloadSizeDS);
        if (payloadSizeDS > 0)
            payloadSizeFinal = payloadSizeDS;
        else
        {
            int64_t payloadSizeRemoteDev = 0;

            //retrieve the payload size to allocate the buffers
            SVFeatureGetByName(hRemoteDev, "PayloadSize", &hFeature);
            SVFeatureGetValueInt64(hRemoteDev, hFeature, &payloadSizeRemoteDev);
            payloadSizeFinal = payloadSizeRemoteDev;
        }
    }
    return payloadSizeFinal;

}


SV_GVSP_PIXEL_TYPE  Camera::Get32bitPixelTyp(SV_GVSP_PIXEL_TYPE pixeltyp)
{

    // check 16bitValue
    switch (pixeltyp)
    {
    // Mono buffer format defines
    case 0x0101:   return   SV_GVSP_PIX_MONO8;

    case 0x0102:   return   SV_GVSP_PIX_MONO10;
    case 0x0103:   return     SV_GVSP_PIX_MONO12;
    case 0x0105:   return    SV_GVSP_PIX_MONO16;
        // Bayer buffer format defines
    case 0x0311:   return    SV_GVSP_PIX_BAYGR8;
    case 0x0321:   return    SV_GVSP_PIX_BAYRG8;
    case 0x0331:   return   SV_GVSP_PIX_BAYGB8;
    case 0x0341:   return     SV_GVSP_PIX_BAYBG8;

    case 0x0312:   return     SV_GVSP_PIX_BAYGR10;
    case 0x0322:   return     SV_GVSP_PIX_BAYRG10;
    case 0x0332:   return     SV_GVSP_PIX_BAYGB10;
    case 0x0342:   return     SV_GVSP_PIX_BAYBG10;

    case 0x0313:   return    SV_GVSP_PIX_BAYGR12;
    case 0x0323:   return   SV_GVSP_PIX_BAYRG12;
    case 0x0333:   return    SV_GVSP_PIX_BAYGB12;
    case 0x0343:   return   SV_GVSP_PIX_BAYBG12;

    case 0x0315:   return    SV_GVSP_PIX_BAYGR16;
    case 0x0325:   return     SV_GVSP_PIX_BAYRG16;
    case 0x0335:   return     SV_GVSP_PIX_BAYGB16;
    case 0x0345:   return     SV_GVSP_PIX_BAYBG16;

        // Mono buffer format defines
    case  (int)SV_GVSP_PIX_MONO8:
    case  (int)SV_GVSP_PIX_MONO10:
    case  (int)SV_GVSP_PIX_MONO12:
    case  (int)SV_GVSP_PIX_MONO16:
    case  (int)SV_GVSP_PIX_MONO12_PACKED:

        // Bayer buffer format defines
    case  (int)SV_GVSP_PIX_BAYGR8:
    case (int)SV_GVSP_PIX_BAYRG8:
    case (int)SV_GVSP_PIX_BAYGB8:
    case (int)SV_GVSP_PIX_BAYBG8:
    case (int)SV_GVSP_PIX_BAYGR10:
    case (int)SV_GVSP_PIX_BAYRG10:
    case (int)SV_GVSP_PIX_BAYGB10:
    case (int)SV_GVSP_PIX_BAYBG10:
    case  (int)SV_GVSP_PIX_BAYGR12:
    case (int)SV_GVSP_PIX_BAYRG12:
    case (int)SV_GVSP_PIX_BAYGB12:
    case (int)SV_GVSP_PIX_BAYBG12:
    case (int)SV_GVSP_PIX_BAYGR12_PACKED:
    case (int)SV_GVSP_PIX_BAYRG12_PACKED:
    case (int)SV_GVSP_PIX_BAYGB12_PACKED:
    case (int)SV_GVSP_PIX_BAYBG12_PACKED:
    case (int)SV_GVSP_PIX_BAYGR16:
    case  (int)SV_GVSP_PIX_BAYRG16:
    case (int)SV_GVSP_PIX_BAYGB16:
    case (int)SV_GVSP_PIX_BAYBG16:
        return (SV_GVSP_PIXEL_TYPE)pixeltyp;
    default:

        return SV_GVSP_PIX_UNKNOWN;

    }

}
SV_RETURN Camera::StreamAcquisitionStart(unsigned int bufcount)
{
    if (isStreaming)
        return 0;

    dsBufcount = bufcount;
    SV_FEATURE_HANDLE hFeature = NULL;
    SV_RETURN ret = SV_ERROR_UNKNOWN;
    uint64_t payloadSize =  getPayloadsize();

    // allocate buffers with the retrieved payload size.
    for (uint32_t i = 0; i<bufcount; i++)
    {

        SV_BUFFER_HANDLE hBuffer = NULL;
        ret = SVStreamAllocAndAnnounceBuffer(hDS, payloadSize, NULL, &hBuffer);
        // allocate memory to the Data Stream associated with the hStream
        //ret = SVStreamAnnounceBuffer(hDS, buffer, (uint32_t)payloadSize, NULL, &hBuffer);
        if (SV_ERROR_SUCCESS != ret)
        {
            printf(":%s SVStreamAnnounceBuffer[%d] Failed!:%d\n", __FUNCTION__, i, ret);

            continue;
        }
        ret = SVStreamQueueBuffer(hDS, hBuffer);
    }

    ret = SVStreamFlushQueue(hDS, SV_ACQ_QUEUE_ALL_TO_INPUT);

    if (SV_ERROR_SUCCESS == ret)
        ret = SVStreamAcquisitionStart(hDS, SV_ACQ_START_FLAGS_DEFAULT, GENTL_INFINITE);

    if (SV_ERROR_SUCCESS != ret)
    {
        printf(":%s SVStreamAcquisitionStart Failed!:%d\n", __FUNCTION__, ret);

        for (uint32_t i = 0; i<bufcount; i++)
        {
            SV_BUFFER_HANDLE hBuffer = NULL;
            SVStreamGetBufferId(hDS, 0, &hBuffer);
            if (hBuffer)
            {
                SVStreamRevokeBuffer(hDS, hBuffer, NULL, NULL);
            }
        }
    }

    hFeature = NULL;
    uint32_t ExecuteTimeout = 1000;
    ret = SVFeatureGetByName(hRemoteDev, "AcquisitionStart", &hFeature);

    if (SV_ERROR_SUCCESS == ret)
        ret = SVFeatureCommandExecute(hRemoteDev, hFeature, ExecuteTimeout);

    hFeature = NULL;
    //Some of features is locked and are not enabled on the feature tree during Streaming (Ex: AOI, binning, Fliping..)
    if (SV_ERROR_SUCCESS == ret)
        ret = SVFeatureGetByName(hRemoteDev, "TLParamsLocked", &hFeature);
    if (SV_ERROR_SUCCESS == ret)
        ret = SVFeatureSetValueInt64(hRemoteDev, hFeature, 1);

    if (ret == SV_ERROR_SUCCESS)
        isStreaming = true;

    return ret;
}

SV_RETURN Camera::StreamAcquisitionStop()
{

    if (!isStreaming)
        return 0;

    SV_FEATURE_HANDLE hFeature = NULL;
    uint32_t ExecuteTimeout = 1000;
    SV_RETURN ret = SVFeatureGetByName(hRemoteDev, "AcquisitionStop", &hFeature);

    if (SV_ERROR_SUCCESS == ret)
        ret = SVFeatureCommandExecute(hRemoteDev, hFeature, ExecuteTimeout);

    hFeature = NULL;
    if (SV_ERROR_SUCCESS == ret)
        ret = SVFeatureGetByName(hRemoteDev, "TLParamsLocked", &hFeature);

    if (SV_ERROR_SUCCESS == ret)
        ret = SVFeatureSetValueInt64(hRemoteDev, hFeature, 0);

    if (SV_ERROR_SUCCESS == ret)
        ret = SVStreamAcquisitionStop(hDS, SV_ACQ_STOP_FLAGS_DEFAULT);


    if (SV_ERROR_SUCCESS == ret)
        ret = SVStreamFlushQueue(hDS, SV_ACQ_QUEUE_INPUT_TO_OUTPUT);

    if (SV_ERROR_SUCCESS == ret)
        ret = SVStreamFlushQueue(hDS, SV_ACQ_QUEUE_OUTPUT_DISCARD);

    uint32_t bufferIdx = 0;

    for (uint32_t i = 0; i< dsBufcount; i++)
    {
        SV_BUFFER_HANDLE hBuffer = NULL;

        int ret = SVStreamGetBufferId(hDS, 0, &hBuffer);

        if (hBuffer)
        {
            ret = SVStreamRevokeBuffer(hDS, hBuffer, NULL, NULL);

        }
    }

    if (ret == SV_ERROR_SUCCESS)
        isStreaming = false;

    return  ret;
}

SV_RETURN   Camera::grab(SV_BUFFER_INFO  *m_InfoConverted )
{
    SV_BUFFER_INFO  origInfo =  { 0 };
    origInfo.pImagePtr = NULL;
    void * data = NULL;

    SV_BUFFER_HANDLE bufhandle = NULL;
    SV_RETURN ret = SVStreamWaitForNewBuffer(hDS, &data, &bufhandle, 1000);



    if (SV_ERROR_SUCCESS == ret)
    {

        // sv_buffer_hdl_list.push_back(bufhandle);

        ret = SVStreamBufferGetInfo(hDS, bufhandle, &origInfo);
        if (SV_ERROR_SUCCESS != ret)
            return ret;

        if (NULL == m_InfoConverted->pImagePtr)
            m_InfoConverted->pImagePtr = new  uint8_t[origInfo.iImageSize];


        m_InfoConverted->iSizeX = origInfo.iSizeX;
        m_InfoConverted->iSizeY = origInfo.iSizeY;
        m_InfoConverted->iImageId = origInfo.iImageId;
        m_InfoConverted->iPixelType = Get32bitPixelTyp((SV_GVSP_PIXEL_TYPE)origInfo.iPixelType);
        m_InfoConverted->iImageSize = origInfo.iImageSize;
        m_InfoConverted->iTimeStamp = origInfo.iTimeStamp;


        if (m_InfoConverted->pImagePtr != NULL)
            memcpy(m_InfoConverted->pImagePtr, origInfo.pImagePtr, origInfo.iImageSize);

        // ------------------------------------------------------------------------------------------ //
        // Joe: copy the latest buffer info local variable
        EnterCriticalSection(&cscopybufferinfo);
        // Local copy buffer info
        if (NULL == copyBufferInfo.pImagePtr)
            copyBufferInfo.pImagePtr = new  uint8_t[origInfo.iImageSize]; 

        copyBufferInfo.iSizeX = origInfo.iSizeX;
        copyBufferInfo.iSizeY = origInfo.iSizeY;
        copyBufferInfo.iImageId = origInfo.iImageId;
        copyBufferInfo.iPixelType = Get32bitPixelTyp((SV_GVSP_PIXEL_TYPE)origInfo.iPixelType);
        copyBufferInfo.iImageSize = origInfo.iImageSize;
        copyBufferInfo.iTimeStamp = origInfo.iTimeStamp; 
        if (copyBufferInfo.pImagePtr != NULL)
            memcpy(copyBufferInfo.pImagePtr, origInfo.pImagePtr, origInfo.iImageSize); 
        bCopyBufferReady = true;
        LeaveCriticalSection(&cscopybufferinfo);


        ret = SVStreamQueueBuffer(hDS, bufhandle);
    }
    else
    {
        EnterCriticalSection(&cscopybufferinfo);
        bCopyBufferReady = false;
        LeaveCriticalSection(&cscopybufferinfo);
    }
    return ret;
}



SV_RETURN  Camera::setAreaOfInterest(
        int64_t SizeX,
        int64_t SizeY,
        int64_t OffsetX,
        int64_t OffsetY)
{

    SV_RETURN ret = SV_ERROR_SUCCESS;
    SV_FEATURE_HANDLE hFeature = NULL;

    ret = SVFeatureGetByName(hRemoteDev, "TLParamsLocked", &hFeature);
    if (ret != SV_ERROR_SUCCESS)
        return ret;

    if (SV_ERROR_SUCCESS == ret)
    {
        int64_t  islocked = 1;
        ret = SVFeatureGetValueInt64(hRemoteDev, hFeature, &islocked);
        if (islocked == 1)
            return  SV_ERROR_ACCESS_DENIED;
    }

    hFeature = NULL;
    ret = SVFeatureGetByName(hRemoteDev, "Width", &hFeature);
    if (ret != SV_ERROR_SUCCESS)
        return ret;

    ret = SVFeatureSetValueInt64(hRemoteDev, hFeature, SizeX);
    if (ret != SV_ERROR_SUCCESS)
        return ret;

    hFeature = NULL;
    ret = ret = SVFeatureGetByName(hRemoteDev, "Height", &hFeature);
    if (ret != SV_ERROR_SUCCESS)
        return ret;

    ret = SVFeatureSetValueInt64(hRemoteDev, hFeature, SizeY);
    if (ret != SV_ERROR_SUCCESS)
        return ret;

    hFeature = NULL;
    if (ret != SV_ERROR_SUCCESS)
        return ret;

    ret = SVFeatureGetByName(hRemoteDev, "OffsetX", &hFeature);
    if (ret != SV_ERROR_SUCCESS)
        return ret;

    ret = SVFeatureSetValueInt64(hRemoteDev, hFeature, OffsetX);
    if (ret != SV_ERROR_SUCCESS)
        return ret;

    hFeature = NULL;
    ret = SVFeatureGetByName(hRemoteDev, "OffsetY", &hFeature);
    if (ret != SV_ERROR_SUCCESS)
        return ret;

    ret = SVFeatureSetValueInt64(hRemoteDev, hFeature, OffsetY);
    if (ret != SV_ERROR_SUCCESS)
        return ret;

    return ret;

}


SV_RETURN Camera::SetAutoWhitebalance(int index)
{
    SV_FEATURE_HANDLE hFeature = NULL;
    SV_RETURN ret = SV_ERROR_UNKNOWN;


    ret = SVFeatureGetByName(hRemoteDev, "BalanceWhiteAuto", &hFeature);
    if (SV_ERROR_SUCCESS == ret)

        switch (index)
        {
        case  0:
            SVFeatureSetValueEnum(hRemoteDev, hFeature, "Reset");
            break;
        case  1:
            SVFeatureSetValueEnum(hRemoteDev, hFeature, "Continuous");
            break;
        case  2:
            SVFeatureSetValueEnum(hRemoteDev, hFeature, "Once");
            break;
        case  3:
            SVFeatureSetValueEnum(hRemoteDev, hFeature, "Off");
            break;
        }
    return ret;
}

SV_RETURN Camera::GetAutoWhitebalance(int *index)
{
    SV_FEATURE_HANDLE hFeature = NULL;
    SV_RETURN ret = SV_ERROR_UNKNOWN;

    char buff[512] = {};
    ret = SVFeatureGetByName(hRemoteDev, "BalanceWhiteAuto", &hFeature);
    if (SV_ERROR_SUCCESS == ret)
        ret = SVFeatureGetValueEnum(hRemoteDev, hFeature, buff, 512);

    if (0 == _stricmp("Reset", buff))
        *index = 0;
    if (0 == _stricmp("Continuous", buff))
        *index = 1;
    if (0 == _stricmp("Once", buff))
        *index = 2;
    if (0 == _stricmp("On", buff))
        *index = 3;

    return ret;
}


SV_RETURN Camera::SetPivMode(bool enable)
{
    SV_FEATURE_HANDLE hFeature = NULL;
    SV_RETURN ret = SV_ERROR_UNKNOWN;

    //retrieve the payload size to allocate the buffers
    ret = SVFeatureGetByName(hRemoteDev, "PIVMode", &hFeature);
    if (ret != SV_ERROR_SUCCESS)
        return ret;

    if (enable)
    {
        SVFeatureSetValueEnum(hRemoteDev, hFeature, "On");
        if (ret != SV_ERROR_SUCCESS)
            return ret;
    }
    else
    {
        SVFeatureSetValueEnum(hRemoteDev, hFeature, "Off");
        if (ret != SV_ERROR_SUCCESS)
            return ret;
    }
    return ret;

}

SV_RETURN Camera::GetPivMode(bool *enable)
{
    SV_FEATURE_HANDLE hFeature = NULL;
    SV_RETURN ret = SV_ERROR_UNKNOWN;
    //retrieve the payload size to allocate the buffers
    ret = SVFeatureGetByName(hRemoteDev, "PIVMode", &hFeature);
    if (ret != SV_ERROR_SUCCESS)
        return ret;

    char buff[512] = {};
    ret = SVFeatureGetValueEnum(hRemoteDev, hFeature, buff, 512);
    if (ret != SV_ERROR_SUCCESS)
        return ret;
    if (0 == _stricmp("On", buff))
        *enable = true;
    else
        *enable = false;

    return ret;

}

SV_RETURN Camera::setAcquisitionMode(int index )
{
    SV_FEATURE_HANDLE hFeature = NULL;
    SV_RETURN ret = SV_ERROR_UNKNOWN;

    //retrieve the payload size to allocate the buffers
    ret = SVFeatureGetByName(hRemoteDev, "AcquisitionMode", &hFeature);
    if (SV_ERROR_SUCCESS == ret)
        switch (index)
        {
        case  0 :
            SVFeatureSetValueEnum(hRemoteDev, hFeature, "SingleFrame");
            break;
        case  1:
            SVFeatureSetValueEnum(hRemoteDev, hFeature, "MultiFrame");
            break;
        case  2:
            SVFeatureSetValueEnum(hRemoteDev, hFeature, "Continuous");
            break;
        }
    return ret;
}

SV_RETURN Camera::SetTriggerMode(bool enable)
{

    SV_FEATURE_HANDLE hFeature = NULL;
    SV_RETURN ret = SV_ERROR_UNKNOWN;

    //retrieve the payload size to allocate the buffers
    ret = SVFeatureGetByName(hRemoteDev, "TriggerMode", &hFeature);
    if (SV_ERROR_SUCCESS != ret)
        return  ret;

    if (enable)
        ret = SVFeatureSetValueEnum(hRemoteDev, hFeature, "On");
    else
        ret = SVFeatureSetValueEnum(hRemoteDev, hFeature, "Off");

    return ret;
}

SV_RETURN Camera::GetTriggerMode(bool *enable)
{
    SV_FEATURE_HANDLE hFeature = NULL;
    SV_RETURN ret = SV_ERROR_UNKNOWN;

    //retrieve the payload size to allocate the buffers
    ret = SVFeatureGetByName(hRemoteDev, "TriggerMode", &hFeature);
    if (SV_ERROR_SUCCESS != ret)
        return  ret;

    *enable = false;
    char* triggerMode = new char[512];
    ret = SVFeatureGetValueEnum(hRemoteDev, hFeature, triggerMode, 512);
    if (0 == _stricmp("On", triggerMode))
        *enable = true;
    delete triggerMode;

    return ret;
}


bool Camera::EnableSoftwareTriggerMode()
{
    SetTriggerMode(true);    // enable software trigger by default  
    return true;
}


bool Camera::EmitSoftwareTriggerSignal()
{
    // Emit software trigger command
    // Joe: Software trigger for JT Demo
    SV_FEATURE_HANDLE hFeature = NULL;
    SV_RETURN ret = SV_ERROR_SUCCESS;

    EnterCriticalSection(&cscopybufferinfo);
    bCopyBufferReady = false;
    LeaveCriticalSection(&cscopybufferinfo);

    hFeature = NULL;
    uint32_t ExecuteTimeout = 1000;
    ret = SVFeatureGetByName(hRemoteDev, "TriggerSoftware", &hFeature);

    if (SV_ERROR_SUCCESS == ret)
        ret = SVFeatureCommandExecute(hRemoteDev, hFeature, ExecuteTimeout);

    return true;
}

bool Camera::CheckSoftwareTriggerResult()
{
    bool bReady = false;

    EnterCriticalSection(&cscopybufferinfo);
    bReady = bCopyBufferReady;
    LeaveCriticalSection(&cscopybufferinfo);

    return bReady; 
}

bool Camera::GetSoftwareTriggerResult(SV_BUFFER_INFO* bufferInfo)
{
    // Local copy buffer info
    EnterCriticalSection(&cscopybufferinfo);
    if (NULL == bufferInfo->pImagePtr)
        bufferInfo->pImagePtr = new  uint8_t[copyBufferInfo.iImageSize]; 

    bufferInfo->iSizeX = copyBufferInfo.iSizeX;
    bufferInfo->iSizeY = copyBufferInfo.iSizeY;
    bufferInfo->iImageId = copyBufferInfo.iImageId;
    bufferInfo->iPixelType = Get32bitPixelTyp((SV_GVSP_PIXEL_TYPE)copyBufferInfo.iPixelType);
    bufferInfo->iImageSize = copyBufferInfo.iImageSize;
    bufferInfo->iTimeStamp = copyBufferInfo.iTimeStamp;
    if (copyBufferInfo.pImagePtr != NULL)
        memcpy(bufferInfo->pImagePtr, copyBufferInfo.pImagePtr, copyBufferInfo.iImageSize);

    LeaveCriticalSection(&cscopybufferinfo);


    return true; 
}









