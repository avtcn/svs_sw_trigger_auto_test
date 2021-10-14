

#include "CameraContainer.h"


CameraContainer::CameraContainer()
{

}

CameraContainer::~CameraContainer()
{
    closeCameracontainer();
}

SV_RETURN CameraContainer::closeCameracontainer()
{
    DSDeleteContainer(sv_cam_list);
    //printf("%s Close all interfaces. \n", __FUNCTION__);
    // Close all interfaces and free all the resources allocated by this module

    for (vector<SV_INTERFACE_HANDLE >::iterator it = sv_interface_hdl_list.begin(); it != sv_interface_hdl_list.end(); ++it)
        if (*it)
        {
            SVInterfaceClose((*it));
        }

    sv_interface_hdl_list.clear();

    //printf("%s Close system module. \n", __FUNCTION__);
    //Close system module and free all the resources
    for (vector<SV_SYSTEM_HANDLE >::iterator it = sv_system_hdl_list.begin(); it != sv_system_hdl_list.end(); ++it)
        if (*it)
        {
            SVSystemClose((*it));
        }

    SVLibClose();

    sv_system_hdl_list.clear();

    // Delete and clear all elements in the device info list
    DSDeleteContainer(devInfoList);


    return 0;
}

bool  CameraContainer::initSDK()
{

    string ctiPath;
    string genicamPath;
    string genicamCachePath;
    string clProtocolPath;

    char buffer[1024] = { 0 };

#ifdef _WIN64
    int res = GetEnvironmentVariableA("GENICAM_GENTL64_PATH", buffer, sizeof(buffer));
    if (0 == res)
        return false;
#else
    int res = GetEnvironmentVariableA("GENICAM_GENTL32_PATH", buffer, sizeof(buffer));
    if (0 == res)
        return false;
#endif

    ctiPath = string(buffer);

    memset(buffer, 0, sizeof(buffer));
    res = GetEnvironmentVariableA("SVS_GENICAM_ROOT", buffer, sizeof(buffer));
    if (0 == res)
        return false;

    genicamPath = string(buffer);

    memset(buffer, 0, sizeof(buffer));
    res = GetEnvironmentVariableA("SVS_GENICAM_CACHE", buffer, sizeof(buffer));
    if (0 == res)
        return false;

    genicamCachePath = string(buffer);

    memset(buffer, 0, sizeof(buffer));
    res = GetEnvironmentVariableA("SVS_GENICAM_CLPROTOCOL", buffer, sizeof(buffer));
    if (0 == res)
        return false;

    clProtocolPath = string(buffer);

    SV_RETURN ret = SVLibInit(ctiPath.c_str(), genicamPath.c_str(), genicamCachePath.c_str(), clProtocolPath.c_str());
    if (SV_ERROR_SUCCESS != ret)
    {
        printf("SVLibInit Failed! :%d", ret);
        return false;
    }

    return true;
}


size_t  CameraContainer::devicediscovery()
{

    SV_RETURN  ret = SV_ERROR_SUCCESS;
    //Open the System module for each of GenTLProducers.
    uint32_t tlCount = 0;

    if ( sv_cam_list.size() ==0 )
        ret = SVLibSystemGetCount(&tlCount);

    for (uint32_t i = 0; i < tlCount; i++)
    {
        SV_TL_INFO tlInfo = { 0 };
        ret = SVLibSystemGetInfo(i, &tlInfo);
        if (SV_ERROR_SUCCESS != ret)
        {
            continue;
        }

        if (0 != _stricmp("SVS-VISTEK GmbH", tlInfo.vendor))
            if (0 != _stricmp("CXP", tlInfo.tlType))
                continue;

        if (0 == _stricmp("CL", tlInfo.tlType))
            continue;

        SV_SYSTEM_HANDLE  sv_cam_sys_hdl = NULL;



        int ret = SVLibSystemOpen(i, &sv_cam_sys_hdl);
        if (SV_ERROR_SUCCESS != ret)
        {


            continue;
        }

        sv_system_hdl_list.push_back(sv_cam_sys_hdl);

    }


    for (uint32_t i = 0; i < sv_system_hdl_list.size(); i++)
        EnumDevices(1000, sv_system_hdl_list.at(i));

    return devInfoList.size();
}

void CameraContainer::EnumDevices(unsigned int timeout, SV_SYSTEM_HANDLE  sv_cam_sys_hdl)
{


    //Updates the internal list of available interfaces.
    SV_RETURN ret = SVSystemUpdateInterfaceList(sv_cam_sys_hdl, NULL, timeout);
    if (SV_ERROR_SUCCESS != ret)
        return;

    {
        uint32_t numInterface = 0;
        //Queries the number of available interfaces on this System module.
        ret = SVSystemGetNumInterfaces(sv_cam_sys_hdl, &numInterface);
        if (SV_ERROR_SUCCESS != ret)
            return;
        for (uint32_t i = 0; i < numInterface; i++)
        {
            char interfaceId[SV_STRING_SIZE] = { 0 };
            size_t interfaceIdSize = sizeof(interfaceId);

            //Queries the ID of the interface at iIndex in the internal interface list .
            ret = SVSystemGetInterfaceId(sv_cam_sys_hdl, i, interfaceId, &interfaceIdSize);
            if (SV_ERROR_SUCCESS != ret)
            {
                continue;
            }
            SV_INTERFACE_INFO interfaceInfo = { 0 };
            ret = SVSystemInterfaceGetInfo(sv_cam_sys_hdl, interfaceId, &interfaceInfo);
            if (SV_ERROR_SUCCESS != ret)
            {
                continue;
            }

            SV_INTERFACE_HANDLE hInterface = NULL;
            // Queries the information about the interface on this System module without opening the interface.
            ret = SVSystemInterfaceOpen(sv_cam_sys_hdl, interfaceId, &hInterface);
            if (SV_ERROR_SUCCESS != ret)
            {
                continue;
            }
            sv_interface_hdl_list.push_back(hInterface);
        }
    }


    for (uint32_t i = 0; i < sv_interface_hdl_list.size(); i++)
    {
        //Updates the internal list of available devices on this interface.


        ret = SVInterfaceUpdateDeviceList(sv_interface_hdl_list.at(i), NULL, 5000);
        if (SV_ERROR_SUCCESS != ret)
        {
            continue;
        }
        {
            uint32_t numDevices = 0;
            //Queries the number of available devices on this interface
            ret = SVInterfaceGetNumDevices(sv_interface_hdl_list.at(i), &numDevices);
            if (SV_ERROR_SUCCESS != ret || 0 == numDevices)
            {
                SVInterfaceClose(sv_interface_hdl_list.at(i));
                continue;
            }

            // Get device info for all available devices and add it to the device info list.
            for (uint32_t j = 0; j < numDevices; j++)
            {
                char deviceId[SV_STRING_SIZE] = { 0 };
                size_t deviceIdSize = sizeof(deviceId);
                ret = SVInterfaceGetDeviceId(sv_interface_hdl_list.at(i), j, deviceId, &deviceIdSize);
                if (SV_ERROR_SUCCESS != ret)
                {
                    continue;
                }

                SV_DEVICE_INFO * devInfo = new   SV_DEVICE_INFO();
                ret = SVInterfaceDeviceGetInfo(sv_interface_hdl_list.at(i), deviceId, devInfo);
                if (SV_ERROR_SUCCESS != ret)
                {
                    delete devInfo;
                    continue;
                }

                // check if the camera is already in the list
                Camera *cam = new Camera(*devInfo);
                for (unsigned  j = 0; j < sv_cam_list.size(); j++)
                {
                    if (0 == _stricmp(cam->devInfo.serialNumber, sv_cam_list.at(j)->devInfo.serialNumber))
                    {
                        delete  devInfo;
                        delete  cam;
                        cam = NULL;
                        break;
                    }
                }
                if (cam != NULL)
                {
                    sv_cam_list.push_back(cam);
                    devInfoList.push_back(devInfo);

                }
            }
        }


    }

    return;
}






