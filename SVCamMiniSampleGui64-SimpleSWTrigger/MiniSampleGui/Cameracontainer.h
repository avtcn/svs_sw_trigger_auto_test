
#ifndef CAMERACONTAINER_H
#define CAMERACONTAINER_H

#include"src\ds_commonwin.h"
#include "Camera.h"

class CameraContainer
{
public:
    CameraContainer();
    ~CameraContainer();
    vector<SV_DEVICE_INFO *>  devInfoList;
    vector<Camera * > sv_cam_list;
    vector<SV_SYSTEM_HANDLE > sv_system_hdl_list;
    vector<SV_INTERFACE_HANDLE > sv_interface_hdl_list;
    vector<SV_DEVICE_HANDLE > sv_device_hdl_list;


    SV_RETURN closeCameracontainer();
    size_t  devicediscovery();


    bool  initSDK();
    void EnumDevices(unsigned int timeout, SV_SYSTEM_HANDLE  sv_cam_sys_hdl);

};
#endif
