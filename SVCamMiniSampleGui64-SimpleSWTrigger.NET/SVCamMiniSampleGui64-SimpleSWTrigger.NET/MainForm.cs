using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Windows.Forms;

namespace SVCamApi
{
    public partial class MainForm : Form
    {
        private CameraContainer cam_container;
        private Camera currentCam = null;

        private bool m_bCameraOpened = false;
        private long m_nSWTriggerCount = 0;

        private bool m_bAutoTrigger = false;
        private long m_nAutoTriggerCount = 0;
        private string strAutoTriggerText = "Software Trigger Timer 1fps";

        private bool SaveImg = false;
        private int save_img_count = 0;
        private int save_img_buffer_count = 2;

        private readonly Queue<Tuple<SVcamApi._SV_BUFFER_INFO, byte[]>> save_queue = new Queue<Tuple<SVcamApi._SV_BUFFER_INFO, byte[]>>();
        private readonly object save_lock = new object();
        private readonly object csacquisition = new object();

        private Thread m_acqThread = null;
        private Thread m_saveThread = null;
        private volatile bool acqTerminated = false;
        private volatile bool saveTerminated = false;
        private volatile bool isStoping = false;

        private DisplayForm displayForm = null;

        private static readonly ColorPalette s_grayPalette;

        static MainForm()
        {
            using (Bitmap dummy = new Bitmap(1, 1, PixelFormat.Format8bppIndexed))
            {
                s_grayPalette = dummy.Palette;
                for (int i = 0; i < 256; i++)
                {
                    s_grayPalette.Entries[i] = Color.FromArgb(i, i, i);
                }
            }
        }

        public MainForm()
        {
            InitializeComponent();

            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SVS.ico");
                if (File.Exists(iconPath))
                {
                    Icon = new Icon(iconPath);
                }
            }
            catch { }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Dialogue title with commit id and build timestamp
            string commitId = GetGitCommitId();
            string buildDate = DateTime.Now.ToString("MMM d yyyy");
            string buildTime = DateTime.Now.ToString("HH:mm:ss");
            Text = $"SVCamControl.NET [Commit: {commitId} | Built: {buildDate} {buildTime}]";

            cam_container = new CameraContainer();

            // Create and show Display window in background (modeless, maximized)
            displayForm = new DisplayForm();
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SVS.ico");
                if (File.Exists(iconPath))
                {
                    displayForm.Icon = new Icon(iconPath);
                }
            }
            catch { }
            displayForm.WindowState = FormWindowState.Maximized;
            displayForm.Show();

            // Bring MainForm to front on top of Display
            BringToFront();
            TopMost = true;

            // Start background acquisition and save threads
            acqTerminated = false;
            saveTerminated = false;
            isStoping = false;

            m_saveThread = new Thread(SaveThreadFunction) { IsBackground = true, Name = "SaveThread" };
            m_saveThread.Start();

            m_acqThread = new Thread(AcqThreadFunction) { IsBackground = true, Name = "AcqThread" };
            m_acqThread.Start();

            // Default combobox settings
            if (cmbWhiteBalance.Items.Count > 0)
                cmbWhiteBalance.SelectedIndex = 3; // "Off"
            if (cmbPivMode.Items.Count > 0)
                cmbPivMode.SelectedIndex = 0; // "Off"

            chkAOI.Checked = false;
            rbSwTrigger.Checked = true;
            m_nSWTriggerCount = 0;
            m_bCameraOpened = false;

            UpdateUIControlsState();
            UpdateModeTips();

            // Automatically call Discovery once when app starts up
            btnDiscovery_Click(this, EventArgs.Empty);
        }

        private string GetGitCommitId()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("git", "rev-parse --short HEAD")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (Process proc = Process.Start(psi))
                {
                    string output = proc.StandardOutput.ReadToEnd().Trim();
                    proc.WaitForExit(1000);
                    if (!string.IsNullOrEmpty(output))
                        return output;
                }
            }
            catch { }

            return "a2887ca";
        }

        private void btnDiscovery_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                if (!cam_container.sdk_init_done)
                {
                    if (!cam_container.initSDK())
                    {
                        lblCamFound.Text = " initialisation failed !!";
                        return;
                    }
                }

                int camCount = cam_container.devicediscovery();
                int currentIndex = cmbCameras.SelectedIndex;
                cmbCameras.Items.Clear();

                if (camCount > 0)
                {
                    foreach (var cam in cam_container.sv_cam_list)
                    {
                        cmbCameras.Items.Add(cam.Model);
                    }

                    lblCamFound.Text = $"Cameras found: {camCount}";
                    if (currentIndex < 0 || currentIndex >= camCount)
                        currentIndex = 0;
                    cmbCameras.SelectedIndex = currentIndex;
                }
                else
                {
                    lblCamFound.Text = " NO Cameras found !!";
                }
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void cmbCameras_SelectedIndexChanged(object sender, EventArgs e)
        {
            int sl = cmbCameras.SelectedIndex;
            if (sl >= 0 && sl < cam_container.sv_cam_list.Count)
            {
                currentCam = cam_container.sv_cam_list[sl];
                currentCam.openConnection();
                currentCam.SetTriggerMode(true);
                currentCam.setAcquisitionMode(2); // Continuous
            }
        }

        private void btnOpenSvsCamera_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                if (!cam_container.sdk_init_done)
                {
                    if (!cam_container.initSDK())
                    {
                        lblCamFound.Text = " initialisation failed !!";
                        return;
                    }
                }

                int camCount = cam_container.devicediscovery();
                int currentIndex = cmbCameras.SelectedIndex;
                cmbCameras.Items.Clear();

                if (camCount > 0)
                {
                    foreach (var cam in cam_container.sv_cam_list)
                    {
                        cmbCameras.Items.Add(cam.Model);
                    }

                    lblCamFound.Text = $"Cameras found: {camCount}";
                    if (currentIndex < 0 || currentIndex >= camCount)
                        currentIndex = 0;
                    cmbCameras.SelectedIndex = currentIndex;
                }
                else
                {
                    lblCamFound.Text = " NO Cameras found !!";
                    return;
                }

                if (cam_container.sv_cam_list.Count == 0)
                {
                    lblCamFound.Text = " NO Cameras found !!";
                    return;
                }

                currentCam = cam_container.sv_cam_list[currentIndex];
                currentCam.openConnection();

                // Configure mode based on radio selections:
                if (rbFreeRun.Checked)
                {
                    currentCam.SetTriggerMode(false);
                    lblCamFound.Text = "Camera Opened (FreeRun Mode)";
                }
                else if (rbHwTrigger.Checked)
                {
                    currentCam.SetTriggerMode(true);
                    var sret = currentCam.SetTriggerSource("Line1");
                    if (sret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS)
                        currentCam.SetTriggerSource("Line0");
                    lblCamFound.Text = "Camera Opened (Hardware Trigger Mode)";
                }
                else // rbSwTrigger
                {
                    currentCam.SetTriggerMode(true);
                    currentCam.SetTriggerSource("Software");
                    lblCamFound.Text = "Camera Opened (Software Trigger Mode)";
                }

                currentCam.setAcquisitionMode(2); // Continuous mode

                // Get camera dimensions
                currentCam.CamHeight = 0;
                currentCam.CamWidth = 0;

                IntPtr hFeature = IntPtr.Zero;
                cam_container.myApi.SVS_FeatureGetByName(currentCam.hRemoteDev, "SensorWidth", ref hFeature);
                if (hFeature != IntPtr.Zero)
                    cam_container.myApi.SVS_FeatureGetValueInt64(currentCam.hRemoteDev, hFeature, ref currentCam.CamWidth);

                hFeature = IntPtr.Zero;
                cam_container.myApi.SVS_FeatureGetByName(currentCam.hRemoteDev, "SensorHeight", ref hFeature);
                if (hFeature != IntPtr.Zero)
                    cam_container.myApi.SVS_FeatureGetValueInt64(currentCam.hRemoteDev, hFeature, ref currentCam.CamHeight);

                // Configure frame rate and Exposure
                hFeature = IntPtr.Zero;
                cam_container.myApi.SVS_FeatureGetByName(currentCam.hRemoteDev, "AcquisitionFrameRate", ref hFeature);
                if (hFeature != IntPtr.Zero)
                    cam_container.myApi.SVS_FeatureSetValueFloat(currentCam.hRemoteDev, hFeature, 15.0);

                hFeature = IntPtr.Zero;
                cam_container.myApi.SVS_FeatureGetByName(currentCam.hRemoteDev, "ExposureTime", ref hFeature);
                if (hFeature != IntPtr.Zero)
                    cam_container.myApi.SVS_FeatureSetValueFloat(currentCam.hRemoteDev, hFeature, 30000.0);

                // Start stream acquisition with 4 buffers
                lock (csacquisition)
                {
                    currentCam.StreamAcquisitionStart(4);
                }

                m_bCameraOpened = true;
                UpdateUIControlsState();
                UpdateModeTips();
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void btnCloseSvsCamera_Click(object sender, EventArgs e)
        {
            if (currentCam == null)
                return;

            // Stop auto trigger timer if running
            if (m_bAutoTrigger)
            {
                timer1fps.Stop();
                m_bAutoTrigger = false;
                m_nAutoTriggerCount = 0;
                btnSoftwareTriggerTimer.Text = strAutoTriggerText;
            }

            isStoping = true;
            Cursor = Cursors.WaitCursor;
            try
            {
                lock (csacquisition)
                {
                    currentCam.StreamAcquisitionStop();
                }
            }
            finally
            {
                isStoping = false;
                Cursor = Cursors.Default;
            }

            lblCamFound.Text = "SVS Camera Closed !!";
            m_bCameraOpened = false;
            UpdateUIControlsState();
            lblModeTips.Text = "";
            m_nSWTriggerCount = 0;
        }

        private void btnSoftwareTrigger_Click(object sender, EventArgs e)
        {
            if (currentCam == null || !currentCam.is_opened)
                return;

            currentCam.EmitSoftwareTriggerSignal();
            m_nSWTriggerCount++;
            string strMsg = $"sw trigger {m_nSWTriggerCount:D8}";
            lblCamFound.Text = strMsg;

            // Wait for photo result (poll with timeout)
            Stopwatch sw = Stopwatch.StartNew();
            bool bReady = false;
            while (!bReady && sw.ElapsedMilliseconds < 2000)
            {
                bReady = currentCam.CheckSoftwareTriggerResult();
                if (!bReady)
                {
                    Application.DoEvents();
                    Thread.Sleep(10);
                }
            }

            if (bReady)
            {
                if (currentCam.GetSoftwareTriggerResult(out var newbufferInfo, out var newBufferData))
                {
                    Directory.CreateDirectory("Image");
                    string filename = Path.Combine("Image", $"{currentCam.DisplayName}_{newbufferInfo.iImageId:D8}_{newbufferInfo.iTimeStamp}.bmp");
                    SaveBufferToBmp(newbufferInfo, newBufferData, filename);

                    // Update display
                    if (displayForm != null && !displayForm.IsDisposed)
                    {
                        displayForm.UpdateFrame(newbufferInfo, newBufferData, cam_container.myApi);
                    }
                }
            }
        }

        private void btnSoftwareTriggerTimer_Click(object sender, EventArgs e)
        {
            if (!m_bAutoTrigger)
            {
                m_bAutoTrigger = true;
                timer1fps.Start();
                m_nAutoTriggerCount = 0;
                strAutoTriggerText = "Software Trigger Timer 1fps";
            }
            else
            {
                timer1fps.Stop();
                m_bAutoTrigger = false;
                m_nAutoTriggerCount = 0;
                btnSoftwareTriggerTimer.Text = strAutoTriggerText;
            }
        }

        private void timer1fps_Tick(object sender, EventArgs e)
        {
            btnSoftwareTrigger_Click(this, EventArgs.Empty);

            m_nAutoTriggerCount++;
            btnSoftwareTriggerTimer.Text = $"{strAutoTriggerText}-{m_nAutoTriggerCount:D8}";
        }

        private void rbFreeRun_CheckedChanged(object sender, EventArgs e)
        {
            if (rbFreeRun.Checked)
            {
                if (currentCam != null && currentCam.is_opened)
                {
                    currentCam.SetTriggerMode(false);
                    lblCamFound.Text = "Switched to FreeRun Mode";
                    UpdateModeTips();
                }
                UpdateUIControlsState();
            }
        }

        private void rbSwTrigger_CheckedChanged(object sender, EventArgs e)
        {
            if (rbSwTrigger.Checked)
            {
                if (currentCam != null && currentCam.is_opened)
                {
                    currentCam.SetTriggerMode(true);
                    currentCam.SetTriggerSource("Software");
                    lblCamFound.Text = "Switched to Software Trigger Mode";
                    UpdateModeTips();
                }
                UpdateUIControlsState();
            }
        }

        private void rbHwTrigger_CheckedChanged(object sender, EventArgs e)
        {
            if (rbHwTrigger.Checked)
            {
                if (currentCam != null && currentCam.is_opened)
                {
                    currentCam.SetTriggerMode(true);
                    var ret = currentCam.SetTriggerSource("Line1");
                    if (ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS)
                        currentCam.SetTriggerSource("Line0");
                    lblCamFound.Text = "Switched to Hardware Trigger Mode";
                    UpdateModeTips();
                }
                UpdateUIControlsState();
            }
        }

        private void UpdateModeTips()
        {
            if (currentCam == null || !currentCam.is_opened)
            {
                lblModeTips.Text = "";
                return;
            }

            if (rbFreeRun.Checked)
            {
                lblModeTips.Text = "Now camera in freerun mode, you can see frames counter increasing ...";
            }
            else if (rbHwTrigger.Checked)
            {
                lblModeTips.Text = "Please provide external 5V TTL signal to LINE1 pin to trigger camera ...";
            }
            else // rbSwTrigger
            {
                lblModeTips.Text = "Please click software trigger button to capture one frame ...";
            }
        }

        private void UpdateUIControlsState()
        {
            btnOpenSvsCamera.Enabled = !m_bCameraOpened;
            btnCloseSvsCamera.Enabled = m_bCameraOpened;

            bool bEnableSwTrigger = m_bCameraOpened && rbSwTrigger.Checked;
            btnSoftwareTrigger.Enabled = bEnableSwTrigger;
            btnSoftwareTriggerTimer.Enabled = bEnableSwTrigger;

            if (!bEnableSwTrigger && m_bAutoTrigger)
            {
                timer1fps.Stop();
                m_bAutoTrigger = false;
                m_nAutoTriggerCount = 0;
                btnSoftwareTriggerTimer.Text = strAutoTriggerText;
            }
        }

        private void chkAOI_CheckedChanged(object sender, EventArgs e)
        {
            if (currentCam == null)
                return;

            isStoping = true;
            Cursor = Cursors.WaitCursor;
            try
            {
                lock (csacquisition)
                {
                    currentCam.StreamAcquisitionStop();
                    if (chkAOI.Checked)
                    {
                        var ret = currentCam.setAreaOfInterest(200, 200, 0, 0);
                        if (ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS)
                            chkAOI.Checked = false;
                    }
                    else
                    {
                        var ret = currentCam.setAreaOfInterest(currentCam.CamWidth, currentCam.CamHeight, 0, 0);
                        if (ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS)
                            chkAOI.Checked = true;
                    }
                    currentCam.StreamAcquisitionStart(4);
                }
                chkAOI.Enabled = false;
            }
            finally
            {
                isStoping = false;
                Cursor = Cursors.Default;
            }
        }

        private void chkAutoSave_CheckedChanged(object sender, EventArgs e)
        {
            SaveImg = chkAutoSave.Checked;
        }

        private void cmbWhiteBalance_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (currentCam == null)
                return;
            currentCam.SetAutoWhitebalance(cmbWhiteBalance.SelectedIndex);
        }

        private void cmbPivMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (currentCam == null)
                return;

            isStoping = true;
            Cursor = Cursors.WaitCursor;
            try
            {
                lock (csacquisition)
                {
                    currentCam.StreamAcquisitionStop();
                    currentCam.SetPivMode(cmbPivMode.SelectedIndex == 1);
                    currentCam.StreamAcquisitionStart(4);
                }
            }
            finally
            {
                isStoping = false;
                Cursor = Cursors.Default;
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (currentCam == null)
                return;

            Cursor = Cursors.WaitCursor;
            try
            {
                lock (csacquisition)
                {
                    currentCam.StreamAcquisitionStart(4);
                }
                chkAOI.Enabled = false;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (currentCam == null)
                return;

            isStoping = true;
            Cursor = Cursors.WaitCursor;
            try
            {
                lock (csacquisition)
                {
                    currentCam.StreamAcquisitionStop();
                }
                chkAOI.Enabled = true;
            }
            finally
            {
                isStoping = false;
                Cursor = Cursors.Default;
            }
        }

        private void btnSoftTrigger_Click(object sender, EventArgs e)
        {
            if (currentCam == null || !currentCam.is_opened)
                return;

            currentCam.EmitSoftwareTriggerSignal();
        }

        private void btnShowDisplay_Click(object sender, EventArgs e)
        {
            if (displayForm == null || displayForm.IsDisposed)
            {
                displayForm = new DisplayForm();
            }
            displayForm.Show();
            displayForm.WindowState = FormWindowState.Normal;
            displayForm.BringToFront();
        }

        private void btnQuit_Click(object sender, EventArgs e)
        {
            Close();
        }

        // Background Acquisition Loop
        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        private void AcqThreadFunction()
        {
            SVcamApi._SV_BUFFER_INFO bufferInfo = new SVcamApi._SV_BUFFER_INFO();

            while (!acqTerminated)
            {
                if (currentCam == null || !currentCam.isStreaming || isStoping)
                {
                    Thread.Sleep(20);
                    continue;
                }

                SVcamApi.SVSCamApiReturn ret = SVcamApi.SVSCamApiReturn.SV_ERROR_UNKNOWN;
                byte[] imageBytes = null;

                try
                {
                    lock (csacquisition)
                    {
                        if (currentCam == null || !currentCam.isStreaming || isStoping)
                        {
                            Thread.Sleep(20);
                            continue;
                        }

                        ret = currentCam.grab(ref bufferInfo, out imageBytes, 100);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"AcqThread exception: {ex.Message}");
                    Thread.Sleep(20);
                    continue;
                }

                if (ret == SVcamApi.SVSCamApiReturn.SV_ERROR_ABORT)
                    break;
                if (ret != SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS || imageBytes == null)
                    continue;

                // Update frame id label on UI
                long imageId = (long)bufferInfo.iImageId;
                if (!IsDisposed && IsHandleCreated)
                {
                    BeginInvoke(new Action(() =>
                    {
                        if (!IsDisposed)
                            lblFrameId.Text = imageId.ToString();
                    }));
                }

                if (SaveImg)
                {
                    lock (save_lock)
                    {
                        if (save_queue.Count < save_img_buffer_count)
                        {
                            save_queue.Enqueue(Tuple.Create(bufferInfo, imageBytes));
                        }
                    }
                }
                else
                {
                    if (displayForm != null && !displayForm.IsDisposed)
                    {
                        displayForm.UpdateFrame(bufferInfo, imageBytes, cam_container.myApi);
                    }
                }
            }
        }

        // Background Image Save Loop
        private void SaveThreadFunction()
        {
            save_img_count = 0;
            while (!saveTerminated)
            {
                Tuple<SVcamApi._SV_BUFFER_INFO, byte[]> item = null;
                lock (save_lock)
                {
                    if (save_queue.Count > 0 && SaveImg)
                    {
                        item = save_queue.Dequeue();
                    }
                }

                if (item == null)
                {
                    Thread.Sleep(20);
                    continue;
                }

                try
                {
                    Directory.CreateDirectory("Image");
                    string filename = Path.Combine("Image", $"{currentCam.DisplayName}{item.Item1.iImageId}_{item.Item1.iTimeStamp}_.png");
                    SaveBufferToPng(item.Item1, item.Item2, filename);

                    save_img_count++;
                    int count = save_img_count;
                    if (!IsDisposed && IsHandleCreated)
                    {
                        BeginInvoke(new Action(() =>
                        {
                            if (!IsDisposed)
                                lblSavedFrame.Text = count.ToString();
                        }));
                    }
                }
                catch { }
            }
        }

        private void SaveBufferToBmp(SVcamApi._SV_BUFFER_INFO info, byte[] data, string filepath)
        {
            try
            {
                GCHandle pin = GCHandle.Alloc(data, GCHandleType.Pinned);
                try
                {
                    info.pImagePtr = pin.AddrOfPinnedObject();
                    var ret = cam_container.myApi.SVS_UtilSaveImageToFile(info, filepath, SVcamApi.SV_IMAGE_FILE_TYPE.SV_IMAGE_FILE_BMP);
                    if (ret == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS && File.Exists(filepath))
                        return;
                }
                finally
                {
                    pin.Free();
                }
            }
            catch { }

            // GDI+ Fallback
            SaveBufferWithGdi(info, data, filepath, ImageFormat.Bmp);
        }

        private void SaveBufferToPng(SVcamApi._SV_BUFFER_INFO info, byte[] data, string filepath)
        {
            try
            {
                GCHandle pin = GCHandle.Alloc(data, GCHandleType.Pinned);
                try
                {
                    info.pImagePtr = pin.AddrOfPinnedObject();
                    var ret = cam_container.myApi.SVS_UtilSaveImageToPNGFile(info, filepath);
                    if (ret == SVcamApi.SVSCamApiReturn.SV_ERROR_SUCCESS && File.Exists(filepath))
                        return;
                }
                finally
                {
                    pin.Free();
                }
            }
            catch { }

            // GDI+ Fallback
            SaveBufferWithGdi(info, data, filepath, ImageFormat.Png);
        }

        private void SaveBufferWithGdi(SVcamApi._SV_BUFFER_INFO info, byte[] data, string filepath, ImageFormat format)
        {
            int width = (int)info.iSizeX.ToInt64();
            int height = (int)info.iSizeY.ToInt64();
            if (width <= 0 || height <= 0 || data == null || data.Length == 0)
                return;

            bool isBayer = (info.iPixelType & (ulong)SVcamApi.DefineConstants.SV_GVSP_PIX_ID_MASK) >= 8;
            ulong pixelOccupy = info.iPixelType & (ulong)SVcamApi.DefineConstants.SV_GVSP_PIX_EFFECTIVE_PIXELSIZE_MASK;

            if (isBayer)
            {
                byte[] rgb = new byte[width * height * 3];
                GCHandle pin = GCHandle.Alloc(data, GCHandleType.Pinned);
                try
                {
                    info.pImagePtr = pin.AddrOfPinnedObject();
                    cam_container.myApi.SVS_UtilBufferBayerToRGB(info, ref rgb[0], rgb.Length);
                }
                finally
                {
                    pin.Free();
                }

                using (Bitmap bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb))
                {
                    BitmapData bd = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                    try
                    {
                        int rowBytes = width * 3;
                        if (bd.Stride == rowBytes)
                        {
                            Marshal.Copy(rgb, 0, bd.Scan0, rgb.Length);
                        }
                        else
                        {
                            for (int y = 0; y < height; y++)
                            {
                                IntPtr destRow = new IntPtr(bd.Scan0.ToInt64() + y * bd.Stride);
                                Marshal.Copy(rgb, y * rowBytes, destRow, rowBytes);
                            }
                        }
                    }
                    finally
                    {
                        bmp.UnlockBits(bd);
                    }
                    bmp.Save(filepath, format);
                }
            }
            else if (pixelOccupy == (ulong)SVcamApi.DefineConstants.SV_GVSP_PIX_OCCUPY12BIT ||
                     pixelOccupy == (ulong)SVcamApi.DefineConstants.SV_GVSP_PIX_OCCUPY16BIT)
            {
                byte[] mono = new byte[width * height];
                GCHandle pin = GCHandle.Alloc(data, GCHandleType.Pinned);
                try
                {
                    info.pImagePtr = pin.AddrOfPinnedObject();
                    if (pixelOccupy == (ulong)SVcamApi.DefineConstants.SV_GVSP_PIX_OCCUPY12BIT)
                        cam_container.myApi.SVS_UtilBuffer12BitTo8Bit(info, ref mono[0], mono.Length);
                    else
                        cam_container.myApi.SVS_UtilBuffer16BitTo8Bit(info, ref mono[0], mono.Length);
                }
                finally
                {
                    pin.Free();
                }

                using (Bitmap bmp = Create8bppBitmap(width, height, mono))
                {
                    bmp.Save(filepath, format);
                }
            }
            else
            {
                using (Bitmap bmp = Create8bppBitmap(width, height, data))
                {
                    bmp.Save(filepath, format);
                }
            }
        }

        private Bitmap Create8bppBitmap(int width, int height, byte[] pixelData)
        {
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            bmp.Palette = s_grayPalette;
            BitmapData bd = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
            try
            {
                if (bd.Stride == width)
                {
                    Marshal.Copy(pixelData, 0, bd.Scan0, width * height);
                }
                else
                {
                    for (int y = 0; y < height; y++)
                    {
                        IntPtr destRow = new IntPtr(bd.Scan0.ToInt64() + y * bd.Stride);
                        Marshal.Copy(pixelData, y * width, destRow, width);
                    }
                }
            }
            finally
            {
                bmp.UnlockBits(bd);
            }
            return bmp;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (m_bAutoTrigger)
            {
                timer1fps.Stop();
                m_bAutoTrigger = false;
            }

            acqTerminated = true;
            saveTerminated = true;
            isStoping = true;

            if (m_acqThread != null && m_acqThread.IsAlive)
            {
                m_acqThread.Join(500);
            }

            if (m_saveThread != null && m_saveThread.IsAlive)
            {
                m_saveThread.Join(500);
            }

            lock (csacquisition)
            {
                if (currentCam != null && currentCam.isStreaming)
                {
                    currentCam.StreamAcquisitionStop();
                }

                if (cam_container != null)
                {
                    cam_container.closeCameraContainer();
                }
            }

            if (displayForm != null && !displayForm.IsDisposed)
            {
                displayForm.Dispose();
                displayForm = null;
            }
        }
    }
}
