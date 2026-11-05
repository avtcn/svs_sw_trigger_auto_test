using System;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

namespace SVCamApi
{
    public class Camera
    {
        public SVcamApi._SV_DEVICE_INFO devInfo;
        public SVcamApi myApi;

        public IntPtr hDevice = IntPtr.Zero;
        public IntPtr hRemoteDev = IntPtr.Zero;
        public IntPtr hStream = IntPtr.Zero;

        public volatile bool is_opened = false;
        public volatile bool isStreaming = false;

        private readonly object m_acquisitionLock = new object();

        public long CamWidth = 0;
        public long CamHeight = 0;

        public SVcamApi._SV_BUFFER_INFO bufferInfoDest = new SVcamApi._SV_BUFFER_INFO();
        public SVcamApi._SV_BUFFER_INFO bufferInfosrc = new SVcamApi._SV_BUFFER_INFO();

        private uint m_dsBufCount = 0;

        // Software trigger buffer synchronization (mirroring C++ copyBufferInfo / cscopybufferinfo)
        private readonly object cscopybufferinfo = new object();
        private SVcamApi._SV_BUFFER_INFO copyBufferInfo = new SVcamApi._SV_BUFFER_INFO();
        private byte[] copyBufferData = null;
        private volatile bool bCopyBufferReady = false;

        public string DisplayName => devInfo.displayName?.TrimEnd('\0') ?? "Camera";
        public string Model => devInfo.model?.TrimEnd('\0') ?? "Camera";

        public Camera(SVcamApi._SV_DEVICE_INFO _devinfo, SVcamApi _myApi)
        {
            devInfo = _devinfo;
            myApi = _myApi;
            bufferInfoDest.pImagePtr = IntPtr.Zero;
            bufferInfosrc.pImagePtr = IntPtr.Zero;
        }

        ~Camera()
        {
            if (isStreaming)
            {
                StreamAcquisitionStop();
            }
            closeConnection();
        }

        public SVcamApi.SVSCamApiReturn openConnection()
        {
            if (is_opened)
                return SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS;

            SVcamApi.SVSCamApiReturn ret = myApi.SVS_InterfaceDeviceOpen(
                devInfo.hParentIF,
                devInfo.uid,
                SVcamApi.SV_DEVICE_ACCESS_FLAGS_LIST.SV_DEVICE_ACCESS_CONTROL,
                ref hDevice,
                ref hRemoteDev);

            if (ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS)
                return ret;

            is_opened = true;

            // Open data stream channel
            string streamId0 = null;
            uint streamId0Size = 512;
            ret = myApi.SVS_DeviceGetStreamId(hDevice, 0, ref streamId0, ref streamId0Size);
            if (ret == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS)
            {
                ret = myApi.SVS_DeviceStreamOpen(hDevice, streamId0, ref hStream);
            }

            return ret;
        }

        public SVcamApi.SVSCamApiReturn closeConnection()
        {
            lock (m_acquisitionLock)
            {
                if (isStreaming)
                {
                    StreamAcquisitionStop();
                }

                if (hStream != IntPtr.Zero)
                {
                    myApi.SVS_SVStreamClose(hStream);
                    hStream = IntPtr.Zero;
                }

                if (hDevice != IntPtr.Zero)
                {
                    myApi.SVS_DeviceClose(hDevice);
                    hDevice = IntPtr.Zero;
                }

                hRemoteDev = IntPtr.Zero;
                is_opened = false;

                lock (cscopybufferinfo)
                {
                    copyBufferData = null;
                    bCopyBufferReady = false;
                }

                return SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS;
            }
        }

        public SVcamApi.SVSCamApiReturn StreamAcquisitionStart(uint bufcount = 4)
        {
            lock (m_acquisitionLock)
            {
                if (hStream == IntPtr.Zero)
                    return SVcamApi.SVSCamApiReturn.SV_ERROR_UNKNOWN;

                if (isStreaming)
                    return SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS;

                SVcamApi.SVSCamApiReturn ret = SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS;
                IntPtr hFeature = IntPtr.Zero;
                long payloadSize = 0;

                myApi.SVS_FeatureGetByName(hStream, SVcamApi.CameraFeature.PayloadSize, ref hFeature);
                if (hFeature != IntPtr.Zero)
                    myApi.SVS_FeatureGetValueInt64(hStream, hFeature, ref payloadSize);

                if (payloadSize == 0 && hRemoteDev != IntPtr.Zero)
                {
                    hFeature = IntPtr.Zero;
                    myApi.SVS_FeatureGetByName(hRemoteDev, SVcamApi.CameraFeature.PayloadSize, ref hFeature);
                    if (hFeature != IntPtr.Zero)
                        myApi.SVS_FeatureGetValueInt64(hRemoteDev, hFeature, ref payloadSize);
                }

                IntPtr imagptr = IntPtr.Zero;
                for (uint i = 0; i < bufcount; i++)
                {
                    IntPtr hBuffer = IntPtr.Zero;
                    ret = myApi.SVS_StreamAllocAndAnnounceBuffer(hStream, (uint)payloadSize, imagptr, ref hBuffer);
                    if (ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS)
                        continue;

                    ret = myApi.SVS_StreamQueueBuffer(hStream, hBuffer);
                    if (ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS)
                        continue;
                }

                myApi.SVS_StreamFlushQueue(hStream, SVcamApi.SV_ACQ_QUEUE_TYPE_LIST.SV_ACQ_QUEUE_ALL_TO_INPUT);
                ret = myApi.SVS_StreamAcquisitionStart(hStream, SVcamApi.SV_ACQ_START_FLAGS_LIST.SV_ACQ_START_FLAGS_DEFAULT, SVcamApi.DefineConstants.INFINIT);

                if (ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS)
                {
                    for (uint i = 0; i < bufcount; i++)
                    {
                        IntPtr hBuffer = IntPtr.Zero;
                        ret = myApi.SVS_StreamGetBufferId(hStream, 0, ref hBuffer);
                        IntPtr pBuffer = IntPtr.Zero;
                        IntPtr imagptr1 = IntPtr.Zero;
                        if (hBuffer != IntPtr.Zero)
                        {
                            myApi.SVS_StreamRevokeBuffer(hStream, hBuffer, ref pBuffer, ref imagptr1);
                        }
                    }
                    return ret;
                }

                hFeature = IntPtr.Zero;
                myApi.SVS_FeatureGetByName(hRemoteDev, SVcamApi.CameraFeature.AcquisitionStart, ref hFeature);
                if (hFeature != IntPtr.Zero)
                    myApi.SVS_FeatureCommandExecute(hRemoteDev, hFeature, 1000);

                hFeature = IntPtr.Zero;
                myApi.SVS_FeatureGetByName(hRemoteDev, SVcamApi.CameraFeature.TLParamsLocked, ref hFeature);
                if (hFeature != IntPtr.Zero)
                    myApi.SVS_FeatureSetValueInt64(hRemoteDev, hFeature, 1);

                m_dsBufCount = bufcount;
                isStreaming = true;
                return SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS;
            }
        }

        public SVcamApi.SVSCamApiReturn StreamAcquisitionStop()
        {
            lock (m_acquisitionLock)
            {
                if (hStream == IntPtr.Zero || !isStreaming)
                    return SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS;

                isStreaming = false;

                IntPtr hFeature = IntPtr.Zero;
                myApi.SVS_FeatureGetByName(hRemoteDev, SVcamApi.CameraFeature.AcquisitionStop, ref hFeature);
                if (hFeature != IntPtr.Zero)
                    myApi.SVS_FeatureCommandExecute(hRemoteDev, hFeature, 1000);

                hFeature = IntPtr.Zero;
                myApi.SVS_FeatureGetByName(hRemoteDev, SVcamApi.CameraFeature.TLParamsLocked, ref hFeature);
                if (hFeature != IntPtr.Zero)
                    myApi.SVS_FeatureSetValueInt64(hRemoteDev, hFeature, 0);

                myApi.SVS_StreamAcquisitionStop(hStream, SVcamApi.SV_ACQ_STOP_FLAGS_LIST.SV_ACQ_STOP_FLAGS_KILL);
                myApi.SVS_StreamFlushQueue(hStream, SVcamApi.SV_ACQ_QUEUE_TYPE_LIST.SV_ACQ_QUEUE_INPUT_TO_OUTPUT);
                myApi.SVS_StreamFlushQueue(hStream, SVcamApi.SV_ACQ_QUEUE_TYPE_LIST.SV_ACQ_QUEUE_OUTPUT_DISCARD);

                for (uint i = 0; i < m_dsBufCount; i++)
                {
                    IntPtr hBuffer = IntPtr.Zero;
                    var ret = myApi.SVS_StreamGetBufferId(hStream, 0, ref hBuffer);
                    IntPtr pBuffer = IntPtr.Zero;
                    IntPtr imagptr1 = IntPtr.Zero;
                    if (hBuffer != IntPtr.Zero)
                    {
                        myApi.SVS_StreamRevokeBuffer(hStream, hBuffer, ref pBuffer, ref imagptr1);
                    }
                }

                m_dsBufCount = 0;
                return SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS;
            }
        }

        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        public SVcamApi.SVSCamApiReturn grab(ref SVcamApi._SV_BUFFER_INFO outInfo, out byte[] imageBytes, uint timeout = 100)
        {
            imageBytes = null;
            if (hStream == IntPtr.Zero || !isStreaming)
                return SVcamApi.SVSCamApiReturn.SV_ERROR_UNKNOWN;

            lock (m_acquisitionLock)
            {
                if (hStream == IntPtr.Zero || !isStreaming)
                    return SVcamApi.SVSCamApiReturn.SV_ERROR_UNKNOWN;

                IntPtr hBuffer = IntPtr.Zero;
                IntPtr pData = IntPtr.Zero;

                SVcamApi.SVSCamApiReturn ret = myApi.SVS_StreamWaitForNewBuffer(hStream, ref pData, ref hBuffer, timeout);
                if (!isStreaming || hStream == IntPtr.Zero)
                {
                    return SVcamApi.SVSCamApiReturn.SV_ERROR_ABORT;
                }

                if (ret == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS)
                {
                    try
                    {
                        ret = myApi.SVS_StreamBufferGetInfo(hStream, hBuffer, ref bufferInfosrc);
                        if (ret == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS && bufferInfosrc.pImagePtr != IntPtr.Zero)
                        {
                            outInfo = bufferInfosrc;
                            int imgSize = (int)bufferInfosrc.iImageSize.ToInt64();
                            if (imgSize > 0)
                            {
                                imageBytes = new byte[imgSize];
                                Marshal.Copy(bufferInfosrc.pImagePtr, imageBytes, 0, imgSize);

                                lock (cscopybufferinfo)
                                {
                                    copyBufferInfo = bufferInfosrc;
                                    copyBufferData = new byte[imgSize];
                                    Array.Copy(imageBytes, copyBufferData, imgSize);
                                    bCopyBufferReady = true;
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        imageBytes = null;
                    }

                    if (isStreaming && hStream != IntPtr.Zero && hBuffer != IntPtr.Zero)
                    {
                        try
                        {
                            myApi.SVS_StreamQueueBuffer(hStream, hBuffer);
                        }
                        catch { }
                    }
                }
                else
                {
                    lock (cscopybufferinfo)
                    {
                        bCopyBufferReady = false;
                    }
                }

                return ret;
            }
        }

        // Feature controls
        public SVcamApi.SVSCamApiReturn SetTriggerMode(bool enable)
        {
            IntPtr hFeature = IntPtr.Zero;
            var ret = myApi.SVS_FeatureGetByName(hRemoteDev, "TriggerMode", ref hFeature);
            if (ret == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS && hFeature != IntPtr.Zero)
            {
                ret = myApi.SVS_FeatureSetValueEnum(hRemoteDev, hFeature, enable ? "On" : "Off");
            }
            return ret;
        }

        public SVcamApi.SVSCamApiReturn GetTriggerMode(out bool enable)
        {
            enable = false;
            IntPtr hFeature = IntPtr.Zero;
            var ret = myApi.SVS_FeatureGetByName(hRemoteDev, "TriggerMode", ref hFeature);
            if (ret == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS && hFeature != IntPtr.Zero)
            {
                string val = null;
                ret = myApi.SVS_FeatureGetValueEnum(hRemoteDev, hFeature, ref val, (uint)SVcamApi.DefineConstants.SV_STRING_SIZE);
                enable = string.Equals(val?.TrimEnd('\0'), "On", StringComparison.OrdinalIgnoreCase);
            }
            return ret;
        }

        public SVcamApi.SVSCamApiReturn SetTriggerSource(string sourceName)
        {
            IntPtr hFeature = IntPtr.Zero;
            var ret = myApi.SVS_FeatureGetByName(hRemoteDev, "TriggerSource", ref hFeature);
            if (ret == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS && hFeature != IntPtr.Zero)
            {
                ret = myApi.SVS_FeatureSetValueEnum(hRemoteDev, hFeature, sourceName);
            }
            return ret;
        }

        public SVcamApi.SVSCamApiReturn GetTriggerSource(out string sourceName)
        {
            sourceName = string.Empty;
            IntPtr hFeature = IntPtr.Zero;
            var ret = myApi.SVS_FeatureGetByName(hRemoteDev, "TriggerSource", ref hFeature);
            if (ret == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS && hFeature != IntPtr.Zero)
            {
                ret = myApi.SVS_FeatureGetValueEnum(hRemoteDev, hFeature, ref sourceName, (uint)SVcamApi.DefineConstants.SV_STRING_SIZE);
                if (sourceName != null) sourceName = sourceName.TrimEnd('\0');
            }
            return ret;
        }

        public SVcamApi.SVSCamApiReturn SetAutoWhitebalance(int index)
        {
            IntPtr hFeature = IntPtr.Zero;
            var ret = myApi.SVS_FeatureGetByName(hRemoteDev, "BalanceWhiteAuto", ref hFeature);
            if (ret == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS && hFeature != IntPtr.Zero)
            {
                string mode = "Off";
                switch (index)
                {
                    case 0: mode = "Reset"; break;
                    case 1: mode = "Continuous"; break;
                    case 2: mode = "Once"; break;
                    case 3: mode = "Off"; break;
                }
                ret = myApi.SVS_FeatureSetValueEnum(hRemoteDev, hFeature, mode);
            }
            return ret;
        }

        public SVcamApi.SVSCamApiReturn SetPivMode(bool enable)
        {
            IntPtr hFeature = IntPtr.Zero;
            var ret = myApi.SVS_FeatureGetByName(hRemoteDev, "PIVMode", ref hFeature);
            if (ret == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS && hFeature != IntPtr.Zero)
            {
                ret = myApi.SVS_FeatureSetValueEnum(hRemoteDev, hFeature, enable ? "On" : "Off");
            }
            return ret;
        }

        public SVcamApi.SVSCamApiReturn setAcquisitionMode(int index)
        {
            IntPtr hFeature = IntPtr.Zero;
            var ret = myApi.SVS_FeatureGetByName(hRemoteDev, "AcquisitionMode", ref hFeature);
            if (ret == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS && hFeature != IntPtr.Zero)
            {
                string mode = "Continuous";
                switch (index)
                {
                    case 0: mode = "SingleFrame"; break;
                    case 1: mode = "MultiFrame"; break;
                    case 2: mode = "Continuous"; break;
                }
                ret = myApi.SVS_FeatureSetValueEnum(hRemoteDev, hFeature, mode);
            }
            return ret;
        }

        public SVcamApi.SVSCamApiReturn setAreaOfInterest(long sizeX, long sizeY, long offsetX, long offsetY)
        {
            IntPtr hFeature = IntPtr.Zero;
            myApi.SVS_FeatureGetByName(hRemoteDev, "TLParamsLocked", ref hFeature);
            if (hFeature != IntPtr.Zero)
                myApi.SVS_FeatureSetValueInt64(hRemoteDev, hFeature, 0);

            hFeature = IntPtr.Zero;
            myApi.SVS_FeatureGetByName(hRemoteDev, "Width", ref hFeature);
            if (hFeature != IntPtr.Zero)
                myApi.SVS_FeatureSetValueInt64(hRemoteDev, hFeature, sizeX);

            hFeature = IntPtr.Zero;
            myApi.SVS_FeatureGetByName(hRemoteDev, "Height", ref hFeature);
            if (hFeature != IntPtr.Zero)
                myApi.SVS_FeatureSetValueInt64(hRemoteDev, hFeature, sizeY);

            hFeature = IntPtr.Zero;
            myApi.SVS_FeatureGetByName(hRemoteDev, "OffsetX", ref hFeature);
            if (hFeature != IntPtr.Zero)
                myApi.SVS_FeatureSetValueInt64(hRemoteDev, hFeature, offsetX);

            hFeature = IntPtr.Zero;
            myApi.SVS_FeatureGetByName(hRemoteDev, "OffsetY", ref hFeature);
            if (hFeature != IntPtr.Zero)
                myApi.SVS_FeatureSetValueInt64(hRemoteDev, hFeature, offsetY);

            hFeature = IntPtr.Zero;
            myApi.SVS_FeatureGetByName(hRemoteDev, "TLParamsLocked", ref hFeature);
            if (hFeature != IntPtr.Zero)
                myApi.SVS_FeatureSetValueInt64(hRemoteDev, hFeature, 1);

            return SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS;
        }

        // Interfaces to software trigger buffer copy
        public bool EnableSoftwareTriggerMode()
        {
            SetTriggerMode(true);
            SetTriggerSource("Software");
            return true;
        }

        public bool EmitSoftwareTriggerSignal()
        {
            lock (cscopybufferinfo)
            {
                bCopyBufferReady = false;
            }

            IntPtr hFeature = IntPtr.Zero;
            var ret = myApi.SVS_FeatureGetByName(hRemoteDev, "TriggerSoftware", ref hFeature);
            if (ret == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS && hFeature != IntPtr.Zero)
            {
                myApi.SVS_FeatureCommandExecute(hRemoteDev, hFeature, 1000);
            }
            return true;
        }

        public bool CheckSoftwareTriggerResult()
        {
            lock (cscopybufferinfo)
            {
                return bCopyBufferReady;
            }
        }

        public bool GetSoftwareTriggerResult(out SVcamApi._SV_BUFFER_INFO bufferInfo, out byte[] bufferData)
        {
            lock (cscopybufferinfo)
            {
                if (!bCopyBufferReady || copyBufferData == null)
                {
                    bufferInfo = new SVcamApi._SV_BUFFER_INFO();
                    bufferData = null;
                    return false;
                }

                bufferInfo = copyBufferInfo;
                bufferData = new byte[copyBufferData.Length];
                Array.Copy(copyBufferData, bufferData, copyBufferData.Length);
                return true;
            }
        }
    }
}
