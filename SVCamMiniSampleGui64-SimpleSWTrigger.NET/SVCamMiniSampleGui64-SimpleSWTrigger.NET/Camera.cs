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
        public long SensorWidth = 0;
        public long SensorHeight = 0;
        public long RoiOffsetX = 0;
        public long RoiOffsetY = 0;

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

            GetSensorDimensions(out SensorWidth, out SensorHeight);
            CamWidth = SensorWidth;
            CamHeight = SensorHeight;
            RoiOffsetX = 0;
            RoiOffsetY = 0;

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

        public void GetSensorDimensions(out long width, out long height)
        {
            width = 0;
            height = 0;
            if (hRemoteDev == IntPtr.Zero)
                return;

            IntPtr hFeature = IntPtr.Zero;
            if (myApi.SVS_FeatureGetByName(hRemoteDev, "WidthMax", ref hFeature) == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS && hFeature != IntPtr.Zero)
                myApi.SVS_FeatureGetValueInt64(hRemoteDev, hFeature, ref width);
            if (width == 0 && myApi.SVS_FeatureGetByName(hRemoteDev, "SensorWidth", ref hFeature) == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS && hFeature != IntPtr.Zero)
                myApi.SVS_FeatureGetValueInt64(hRemoteDev, hFeature, ref width);
            if (width == 0 && myApi.SVS_FeatureGetByName(hRemoteDev, "Width", ref hFeature) == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS && hFeature != IntPtr.Zero)
                myApi.SVS_FeatureGetValueInt64(hRemoteDev, hFeature, ref width);

            hFeature = IntPtr.Zero;
            if (myApi.SVS_FeatureGetByName(hRemoteDev, "HeightMax", ref hFeature) == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS && hFeature != IntPtr.Zero)
                myApi.SVS_FeatureGetValueInt64(hRemoteDev, hFeature, ref height);
            if (height == 0 && myApi.SVS_FeatureGetByName(hRemoteDev, "SensorHeight", ref hFeature) == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS && hFeature != IntPtr.Zero)
                myApi.SVS_FeatureGetValueInt64(hRemoteDev, hFeature, ref height);
            if (height == 0 && myApi.SVS_FeatureGetByName(hRemoteDev, "Height", ref hFeature) == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS && hFeature != IntPtr.Zero)
                myApi.SVS_FeatureGetValueInt64(hRemoteDev, hFeature, ref height);

            if (width > 0) SensorWidth = width;
            if (height > 0) SensorHeight = height;
        }

        /// <summary>
        /// Configure camera Region of Interest (ROI) using standard GenICam features Width/Height/OffsetX/OffsetY.
        /// Ported from SetCameraROI in SVCamMiniSample64-ROI-Exp-Mono8.
        /// When center is true, the ROI is centered on the sensor.
        /// </summary>
        public SVcamApi.SVSCamApiReturn SetCameraROI(long roiWidth, long roiHeight, bool center = true)
        {
            if (hRemoteDev == IntPtr.Zero)
                return SVcamApi.SVSCamApiReturn.SV_ERROR_UNKNOWN;

            // 1. Retrieve the sensor limits to compute the ROI size
            long sensorWidth = SensorWidth;
            long sensorHeight = SensorHeight;
            if (sensorWidth <= 0 || sensorHeight <= 0)
            {
                GetSensorDimensions(out sensorWidth, out sensorHeight);
            }
            if (sensorWidth <= 0) sensorWidth = 200;
            if (sensorHeight <= 0) sensorHeight = 200;

            // 2. Retrieve increment each ROI feature requires from the sensor
            long widthInc = 1;
            long heightInc = 1;
            long offsetXInc = 1;
            long offsetYInc = 1;

            string[] roiFeatureNames = new string[] { "Width", "Height", "OffsetX", "OffsetY" };
            for (int i = 0; i < roiFeatureNames.Length; i++)
            {
                IntPtr hFeat = IntPtr.Zero;
                if (myApi.SVS_FeatureGetByName(hRemoteDev, roiFeatureNames[i], ref hFeat) == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS && hFeat != IntPtr.Zero)
                {
                    var featInfo = new SVcamApi._SV_FEATURE_INFO();
                    if (myApi.SVS_FeatureGetInfo(hRemoteDev, hFeat, ref featInfo) == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS)
                    {
                        if (featInfo.intInc > 0)
                        {
                            switch (i)
                            {
                                case 0: widthInc = featInfo.intInc; break;
                                case 1: heightInc = featInfo.intInc; break;
                                case 2: offsetXInc = featInfo.intInc; break;
                                case 3: offsetYInc = featInfo.intInc; break;
                            }
                        }
                    }
                }
            }

            // Clamp ROI width and height within sensor boundaries
            if (roiWidth > sensorWidth) roiWidth = sensorWidth;
            if (roiHeight > sensorHeight) roiHeight = sensorHeight;

            // Snap the size down to the increment the sensor requires
            if (widthInc > 1)
                roiWidth = (roiWidth / widthInc) * widthInc;
            if (heightInc > 1)
                roiHeight = (roiHeight / heightInc) * heightInc;

            // Center the ROI on the sensor (offsets snapped to their increment)
            long offsetX = 0;
            long offsetY = 0;
            if (center)
            {
                offsetX = ((sensorWidth - roiWidth) / 2) / offsetXInc * offsetXInc;
                offsetY = ((sensorHeight - roiHeight) / 2) / offsetYInc * offsetYInc;
            }

            // Helper to set Int64 feature with refresh/retry mirroring C++ setROIValue
            Func<string, long, SVcamApi.SVSCamApiReturn> setROIValue = (featureName, value) =>
            {
                IntPtr hF = IntPtr.Zero;
                var retVal = myApi.SVS_FeatureGetByName(hRemoteDev, featureName, ref hF);
                if (retVal != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS || hF == IntPtr.Zero)
                    return retVal != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS ? retVal : SVcamApi.SVSCamApiReturn.SV_ERROR_UNKNOWN;

                var fInfo = new SVcamApi._SV_FEATURE_INFO();
                myApi.SVS_FeatureGetInfo(hRemoteDev, hF, ref fInfo);
                retVal = myApi.SVS_FeatureSetValueInt64(hRemoteDev, hF, value);
                if (retVal != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS)
                {
                    myApi.SVS_FeatureGetInfo(hRemoteDev, hF, ref fInfo);
                    retVal = myApi.SVS_FeatureSetValueInt64(hRemoteDev, hF, value);
                }
                return retVal;
            };

            // Unlock TLParams
            IntPtr hTLParams = IntPtr.Zero;
            myApi.SVS_FeatureGetByName(hRemoteDev, "TLParamsLocked", ref hTLParams);
            if (hTLParams != IntPtr.Zero)
                myApi.SVS_FeatureSetValueInt64(hRemoteDev, hTLParams, 0);

            // 1. Reset OffsetX and OffsetY to 0 first to avoid out-of-range bounds check when expanding width/height
            setROIValue("OffsetX", 0);
            setROIValue("OffsetY", 0);

            // 2. Set Width and Height
            var ret = setROIValue("Width", roiWidth);
            if (ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS)
                Console.WriteLine($"SetCameraROI: setting Width failed! ret:{(int)ret}");

            ret = setROIValue("Height", roiHeight);
            if (ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS)
                Console.WriteLine($"SetCameraROI: setting Height failed! ret:{(int)ret}");

            // 3. Set desired OffsetX and OffsetY
            ret = setROIValue("OffsetX", offsetX);
            if (ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS)
                Console.WriteLine($"SetCameraROI: setting OffsetX failed! ret:{(int)ret}");

            ret = setROIValue("OffsetY", offsetY);
            if (ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS)
                Console.WriteLine($"SetCameraROI: setting OffsetY failed! ret:{(int)ret}");

            // Lock TLParams
            if (hTLParams != IntPtr.Zero)
                myApi.SVS_FeatureSetValueInt64(hRemoteDev, hTLParams, 1);

            // Verify that the camera really applied the requested ROI
            long appliedWidth = 0, appliedHeight = 0, appliedOffsetX = 0, appliedOffsetY = 0;
            IntPtr hApplied = IntPtr.Zero;
            if (myApi.SVS_FeatureGetByName(hRemoteDev, "Width", ref hApplied) == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS && hApplied != IntPtr.Zero)
                myApi.SVS_FeatureGetValueInt64(hRemoteDev, hApplied, ref appliedWidth);

            hApplied = IntPtr.Zero;
            if (myApi.SVS_FeatureGetByName(hRemoteDev, "Height", ref hApplied) == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS && hApplied != IntPtr.Zero)
                myApi.SVS_FeatureGetValueInt64(hRemoteDev, hApplied, ref appliedHeight);

            hApplied = IntPtr.Zero;
            if (myApi.SVS_FeatureGetByName(hRemoteDev, "OffsetX", ref hApplied) == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS && hApplied != IntPtr.Zero)
                myApi.SVS_FeatureGetValueInt64(hRemoteDev, hApplied, ref appliedOffsetX);

            hApplied = IntPtr.Zero;
            if (myApi.SVS_FeatureGetByName(hRemoteDev, "OffsetY", ref hApplied) == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS && hApplied != IntPtr.Zero)
                myApi.SVS_FeatureGetValueInt64(hRemoteDev, hApplied, ref appliedOffsetY);

            if (appliedWidth > 0 && appliedHeight > 0)
            {
                CamWidth = appliedWidth;
                CamHeight = appliedHeight;
            }
            else
            {
                CamWidth = roiWidth;
                CamHeight = roiHeight;
            }
            RoiOffsetX = appliedOffsetX;
            RoiOffsetY = appliedOffsetY;

            Console.WriteLine($"SetCameraROI: Camera applied {CamWidth}x{CamHeight} at OffsetX:{appliedOffsetX} OffsetY:{appliedOffsetY} (Target: {roiWidth}x{roiHeight} at {offsetX},{offsetY}, Sensor: {sensorWidth}x{sensorHeight}, center={center})");

            return SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS;
        }

        /// <summary>
        /// Retrieve current camera ROI / resolution and offset settings (Width, Height, OffsetX, OffsetY).
        /// </summary>
        public void GetCameraROI(out long width, out long height, out long offsetX, out long offsetY)
        {
            width = CamWidth;
            height = CamHeight;
            offsetX = RoiOffsetX;
            offsetY = RoiOffsetY;

            if (hRemoteDev == IntPtr.Zero)
                return;

            IntPtr hFeat = IntPtr.Zero;
            if (myApi.SVS_FeatureGetByName(hRemoteDev, "Width", ref hFeat) == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS && hFeat != IntPtr.Zero)
                myApi.SVS_FeatureGetValueInt64(hRemoteDev, hFeat, ref width);

            hFeat = IntPtr.Zero;
            if (myApi.SVS_FeatureGetByName(hRemoteDev, "Height", ref hFeat) == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS && hFeat != IntPtr.Zero)
                myApi.SVS_FeatureGetValueInt64(hRemoteDev, hFeat, ref height);

            hFeat = IntPtr.Zero;
            if (myApi.SVS_FeatureGetByName(hRemoteDev, "OffsetX", ref hFeat) == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS && hFeat != IntPtr.Zero)
                myApi.SVS_FeatureGetValueInt64(hRemoteDev, hFeat, ref offsetX);

            hFeat = IntPtr.Zero;
            if (myApi.SVS_FeatureGetByName(hRemoteDev, "OffsetY", ref hFeat) == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS && hFeat != IntPtr.Zero)
                myApi.SVS_FeatureGetValueInt64(hRemoteDev, hFeat, ref offsetY);

            CamWidth = width;
            CamHeight = height;
            RoiOffsetX = offsetX;
            RoiOffsetY = offsetY;
        }

        public static void GetCameraROI(Camera cam, out long width, out long height, out long offsetX, out long offsetY)
        {
            if (cam != null)
            {
                cam.GetCameraROI(out width, out height, out offsetX, out offsetY);
            }
            else
            {
                width = 0;
                height = 0;
                offsetX = 0;
                offsetY = 0;
            }
        }

        public static SVcamApi.SVSCamApiReturn SetCameraROI(Camera cam, long roiWidth, long roiHeight, bool center = true)
        {
            return cam != null ? cam.SetCameraROI(roiWidth, roiHeight, center) : SVcamApi.SVSCamApiReturn.SV_ERROR_UNKNOWN;
        }

        public SVcamApi.SVSCamApiReturn setAreaOfInterest(long sizeX, long sizeY, long offsetX, long offsetY)
        {
            if (offsetX == 0 && offsetY == 0 && (sizeX < SensorWidth || sizeY < SensorHeight) && sizeX == 200 && sizeY == 200)
            {
                return SetCameraROI(sizeX, sizeY, center: true);
            }
            return SetCameraROI(sizeX, sizeY, center: false);
        }

        /// <summary>
        /// Set camera pixel/image format (e.g. "Mono8").
        /// The camera streaming must be stopped when changing the pixel format.
        /// Corresponds to SetCameraPixelFormat in SVCamMiniSample64-ROI-Exp-Mono8.
        /// </summary>
        public SVcamApi.SVSCamApiReturn SetCameraPixelFormat(string pixelFormat)
        {
            if (hRemoteDev == IntPtr.Zero)
                return SVcamApi.SVSCamApiReturn.SV_ERROR_UNKNOWN;

            IntPtr hFeature = IntPtr.Zero;
            SVcamApi.SVSCamApiReturn ret = myApi.SVS_FeatureGetByName(hRemoteDev, "PixelFormat", ref hFeature);
            if (ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS || hFeature == IntPtr.Zero)
            {
                Console.WriteLine($"SetCameraPixelFormat: Feature 'PixelFormat' not found! ret:{(int)ret}");
                return ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS ? ret : SVcamApi.SVSCamApiReturn.SV_ERROR_UNKNOWN;
            }

            ret = myApi.SVS_FeatureSetValueEnum(hRemoteDev, hFeature, pixelFormat);
            if (ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS)
            {
                Console.WriteLine($"SetCameraPixelFormat: Failed to set PixelFormat to '{pixelFormat}'! ret:{(int)ret}");
                return ret;
            }

            string appliedFormat = null;
            if (myApi.SVS_FeatureGetValueEnum(hRemoteDev, hFeature, ref appliedFormat, (uint)SVcamApi.DefineConstants.SV_STRING_SIZE) == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS && !string.IsNullOrEmpty(appliedFormat))
            {
                appliedFormat = appliedFormat.TrimEnd('\0');
                Console.WriteLine($"SetCameraPixelFormat: PixelFormat successfully set to '{appliedFormat}'");
            }
            else
            {
                Console.WriteLine($"SetCameraPixelFormat: PixelFormat set to '{pixelFormat}'");
            }

            return SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS;
        }

        public SVcamApi.SVSCamApiReturn SetPixelFormat(string pixelFormat) => SetCameraPixelFormat(pixelFormat);

        public static SVcamApi.SVSCamApiReturn SetCameraPixelFormat(Camera cam, string pixelFormat)
        {
            return cam != null ? cam.SetCameraPixelFormat(pixelFormat) : SVcamApi.SVSCamApiReturn.SV_ERROR_UNKNOWN;
        }

        public SVcamApi.SVSCamApiReturn GetCameraPixelFormat(out string pixelFormat)
        {
            pixelFormat = string.Empty;
            if (hRemoteDev == IntPtr.Zero)
                return SVcamApi.SVSCamApiReturn.SV_ERROR_UNKNOWN;

            IntPtr hFeature = IntPtr.Zero;
            SVcamApi.SVSCamApiReturn ret = myApi.SVS_FeatureGetByName(hRemoteDev, "PixelFormat", ref hFeature);
            if (ret == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS && hFeature != IntPtr.Zero)
            {
                ret = myApi.SVS_FeatureGetValueEnum(hRemoteDev, hFeature, ref pixelFormat, (uint)SVcamApi.DefineConstants.SV_STRING_SIZE);
                if (pixelFormat != null) pixelFormat = pixelFormat.TrimEnd('\0');
            }
            return ret;
        }

        public SVcamApi.SVSCamApiReturn GetPixelFormat(out string pixelFormat) => GetCameraPixelFormat(out pixelFormat);

        /// <summary>
        /// Set camera exposure time in microseconds (e.g. 10000.0 us for 10 ms).
        /// Corresponds to SetCameraExposureTime in SVCamMiniSample64-ROI-Exp-Mono8.
        /// </summary>
        public SVcamApi.SVSCamApiReturn SetCameraExposureTime(double exposureTimeUs)
        {
            if (hRemoteDev == IntPtr.Zero)
                return SVcamApi.SVSCamApiReturn.SV_ERROR_UNKNOWN;

            // Ensure manual exposure mode if auto-exposure features exist
            IntPtr hAutoExp = IntPtr.Zero;
            if (myApi.SVS_FeatureGetByName(hRemoteDev, "ExposureAuto", ref hAutoExp) == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS && hAutoExp != IntPtr.Zero)
            {
                myApi.SVS_FeatureSetValueEnum(hRemoteDev, hAutoExp, "Off");
            }

            IntPtr hExpMode = IntPtr.Zero;
            if (myApi.SVS_FeatureGetByName(hRemoteDev, "ExposureMode", ref hExpMode) == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS && hExpMode != IntPtr.Zero)
            {
                myApi.SVS_FeatureSetValueEnum(hRemoteDev, hExpMode, "Timed");
            }

            IntPtr hFeature = IntPtr.Zero;
            SVcamApi.SVSCamApiReturn ret = myApi.SVS_FeatureGetByName(hRemoteDev, "ExposureTime", ref hFeature);
            if (ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS || hFeature == IntPtr.Zero)
            {
                // Try fallback for SFNC 1.x cameras (e.g. Allied Vision Manta / Prosilica)
                ret = myApi.SVS_FeatureGetByName(hRemoteDev, "ExposureTimeAbs", ref hFeature);
            }
            if (ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS || hFeature == IntPtr.Zero)
            {
                // Try fallback for cameras that use ExposureTime_us
                ret = myApi.SVS_FeatureGetByName(hRemoteDev, "ExposureTime_us", ref hFeature);
            }

            if (ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS || hFeature == IntPtr.Zero)
            {
                Console.WriteLine($"SetCameraExposureTime: Feature 'ExposureTime' not found! ret:{(int)ret}");
                return ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS ? ret : SVcamApi.SVSCamApiReturn.SV_ERROR_UNKNOWN;
            }

            ret = myApi.SVS_FeatureSetValueFloat(hRemoteDev, hFeature, exposureTimeUs);
            if (ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS)
            {
                Console.WriteLine($"SetCameraExposureTime: Failed to set ExposureTime to {exposureTimeUs:F1} us! ret:{(int)ret}");
                return ret;
            }

            double appliedExp = 0.0;
            if (myApi.SVS_FeatureGetValueFloat(hRemoteDev, hFeature, ref appliedExp) == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS)
            {
                Console.WriteLine($"SetCameraExposureTime: ExposureTime successfully set to {appliedExp:F1} us ({appliedExp / 1000.0:F2} ms)");
            }
            else
            {
                Console.WriteLine($"SetCameraExposureTime: ExposureTime set to {exposureTimeUs:F1} us ({exposureTimeUs / 1000.0:F2} ms)");
            }

            return SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS;
        }

        public SVcamApi.SVSCamApiReturn SetExposureTime(double exposureTimeUs) => SetCameraExposureTime(exposureTimeUs);

        public static SVcamApi.SVSCamApiReturn SetCameraExposureTime(Camera cam, double exposureTimeUs)
        {
            return cam != null ? cam.SetCameraExposureTime(exposureTimeUs) : SVcamApi.SVSCamApiReturn.SV_ERROR_UNKNOWN;
        }

        public SVcamApi.SVSCamApiReturn GetCameraExposureTime(out double exposureTimeUs)
        {
            exposureTimeUs = 0.0;
            if (hRemoteDev == IntPtr.Zero)
                return SVcamApi.SVSCamApiReturn.SV_ERROR_UNKNOWN;

            IntPtr hFeature = IntPtr.Zero;
            SVcamApi.SVSCamApiReturn ret = myApi.SVS_FeatureGetByName(hRemoteDev, "ExposureTime", ref hFeature);
            if (ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS || hFeature == IntPtr.Zero)
                ret = myApi.SVS_FeatureGetByName(hRemoteDev, "ExposureTimeAbs", ref hFeature);
            if (ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS || hFeature == IntPtr.Zero)
                ret = myApi.SVS_FeatureGetByName(hRemoteDev, "ExposureTime_us", ref hFeature);

            if (ret == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS && hFeature != IntPtr.Zero)
            {
                ret = myApi.SVS_FeatureGetValueFloat(hRemoteDev, hFeature, ref exposureTimeUs);
            }
            return ret;
        }

        public SVcamApi.SVSCamApiReturn GetExposureTime(out double exposureTimeUs) => GetCameraExposureTime(out exposureTimeUs);

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
