
#ifndef CAMERA_H
#define CAMERA_H

#include "sv_gen_sdk.h"
using namespace std;

class  Camera
{

public:
    Camera(SV_DEVICE_INFO _devInfo);
    ~Camera();
    SV_DEVICE_INFO devInfo;
    SV_DEVICE_HANDLE hDevice;
    SV_TL_TYPE sv_cam_type;
    SV_STREAM_HANDLE hDS;

    bool is_opened;
    unsigned int dsBufcount;
    bool isStreaming;
    int64_t CamWidth;
    int64_t CamHeight;

    SV_REMOTE_DEVICE_HANDLE hRemoteDev;
public:

    SV_RETURN  Camera::setAreaOfInterest(
            int64_t SizeX,
            int64_t SizeY,
            int64_t OffsetX,
            int64_t OffsetY);

    SV_RETURN openConnection();
    SV_RETURN closeConnection();
    SV_RETURN StreamAcquisitionStart(unsigned int bufcount);
    SV_RETURN StreamAcquisitionStop();
    SV_RETURN grab(SV_BUFFER_INFO  *bufferInfo );

    SV_RETURN setAcquisitionMode(int index );
    SV_RETURN SetAutoWhitebalance( int index );
    SV_RETURN  GetAutoWhitebalance(int *index);

    SV_RETURN SetTriggerMode(bool enable);
    SV_RETURN GetTriggerMode(bool *enable);

    SV_RETURN SetPivMode(bool enable);
    SV_RETURN GetPivMode(bool *enable);
    SV_GVSP_PIXEL_TYPE  Get32bitPixelTyp(SV_GVSP_PIXEL_TYPE pixeltyp);
    uint64_t  getPayloadsize();


};
#endif
