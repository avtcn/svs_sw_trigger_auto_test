using System;
using System.Collections.Generic;

namespace SVCamApi
{
    public class CameraContainer
    {
        public SVcamApi myApi;
        public List<Camera> sv_cam_list = new List<Camera>();
        public List<IntPtr> sv_cam_sys_hdl_list = new List<IntPtr>();
        public List<IntPtr> sv_interface_hdl_list = new List<IntPtr>();
        public bool sdk_init_done = false;

        public CameraContainer()
        {
            myApi = new SVcamApi();
        }

        ~CameraContainer()
        {
            closeCameraContainer();
        }

        public bool initSDK()
        {
            if (sdk_init_done)
                return true;

            bool is64Env = IntPtr.Size == 8;
            string svGenicamGentl = Environment.GetEnvironmentVariable(is64Env ? "GENICAM_GENTL64_PATH" : "GENICAM_GENTL32_PATH");
            string svGenicamRoot = Environment.GetEnvironmentVariable("SVS_GENICAM_ROOT");
            string svGenicamCache = Environment.GetEnvironmentVariable("SVS_GENICAM_CACHE");
            string svCLProtocol = Environment.GetEnvironmentVariable("SVS_GENICAM_CLPROTOCOL");

            if (string.IsNullOrEmpty(svGenicamGentl) || string.IsNullOrEmpty(svGenicamRoot))
            {
                return false;
            }

            var ret = myApi.SVS_LibInit(svGenicamGentl, svGenicamRoot, svGenicamCache, svCLProtocol);
            if (ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS)
            {
                return false;
            }

            sdk_init_done = true;
            return true;
        }

        public int devicediscovery()
        {
            if (!sdk_init_done)
            {
                if (!initSDK())
                    return 0;
            }

            // Close existing cameras and interfaces before rediscover
            closeCameraContainer(keepSdk: true);

            uint tlCount = 0;
            var ret = myApi.SVS_LibSystemGetCount(ref tlCount);
            if (ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS)
                return 0;

            uint timeout = 1000;
            bool bChanged = false;

            for (uint i = 0; i < tlCount; i++)
            {
                var tlInfo = new SVcamApi._SV_TL_INFO();
                ret = myApi.SVS_LibSystemGetInfo(i, ref tlInfo);
                if (ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS)
                    continue;

                string tlType = tlInfo.tlType?.TrimEnd('\0');
                if (string.Equals(tlType, "CL", StringComparison.OrdinalIgnoreCase))
                    continue;

                IntPtr sv_cam_sys_hdl = IntPtr.Zero;
                ret = myApi.SVS_LibSystemOpen(i, ref sv_cam_sys_hdl);
                if (ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS || sv_cam_sys_hdl == IntPtr.Zero)
                    continue;

                sv_cam_sys_hdl_list.Add(sv_cam_sys_hdl);
                myApi.SVS_SystemUpdateInterfaceList(sv_cam_sys_hdl, ref bChanged, timeout);

                uint numInterface = 0;
                ret = myApi.SVS_SystemGetNumInterfaces(sv_cam_sys_hdl, ref numInterface);
                if (ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS)
                    continue;

                for (uint j = 0; j < numInterface; j++)
                {
                    uint interfaceIdSize = 512;
                    string interfaceId = null;
                    ret = myApi.SVS_SystemGetInterfaceId(sv_cam_sys_hdl, j, ref interfaceId, ref interfaceIdSize);
                    if (ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS || string.IsNullOrEmpty(interfaceId))
                        continue;

                    var interfaceInfo = new SVcamApi._SV_INTERFACE_INFO();
                    ret = myApi.SVS_SystemInterfaceGetInfo(sv_cam_sys_hdl, interfaceId, ref interfaceInfo);
                    if (ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS)
                        continue;

                    IntPtr hInterface = IntPtr.Zero;
                    ret = myApi.SVS_SystemInterfaceOpen(sv_cam_sys_hdl, interfaceId, ref hInterface);
                    if (ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS || hInterface == IntPtr.Zero)
                        continue;

                    sv_interface_hdl_list.Add(hInterface);

                    ret = myApi.SVS_InterfaceUpdateDeviceList(hInterface, ref bChanged, 5000);
                    if (ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS)
                        continue;

                    uint numDevices = 0;
                    ret = myApi.SVS_InterfaceGetNumDevices(hInterface, ref numDevices);
                    if (ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS)
                        continue;

                    for (uint k = 0; k < numDevices; k++)
                    {
                        uint deviceIdSize = 512;
                        string deviceId = null;
                        ret = myApi.SVS_InterfaceGetDeviceId(hInterface, k, ref deviceId, ref deviceIdSize);
                        if (ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS || string.IsNullOrEmpty(deviceId))
                            continue;

                        var devInfo = new SVcamApi._SV_DEVICE_INFO();
                        ret = myApi.SVS_InterfaceDeviceGetInfo(hInterface, deviceId, ref devInfo);
                        if (ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS)
                            continue;

                        sv_cam_list.Add(new Camera(devInfo, myApi));
                    }
                }
            }

            return sv_cam_list.Count;
        }

        public void closeCameraContainer(bool keepSdk = false)
        {
            foreach (var cam in sv_cam_list)
            {
                cam.closeConnection();
            }
            sv_cam_list.Clear();

            foreach (var hIf in sv_interface_hdl_list)
            {
                if (hIf != IntPtr.Zero)
                    myApi.SVS_InterfaceClose(hIf);
            }
            sv_interface_hdl_list.Clear();

            foreach (var hSys in sv_cam_sys_hdl_list)
            {
                if (hSys != IntPtr.Zero)
                    myApi.SVS_SystemClose(hSys);
            }
            sv_cam_sys_hdl_list.Clear();

            if (!keepSdk && sdk_init_done)
            {
                myApi.SVS_LibClose();
                sdk_init_done = false;
            }
        }
    }
}
