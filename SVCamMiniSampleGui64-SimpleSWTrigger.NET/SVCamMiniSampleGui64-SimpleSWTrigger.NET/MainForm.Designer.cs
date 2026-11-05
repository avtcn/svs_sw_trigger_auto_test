namespace SVCamApi
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.btnDiscovery = new System.Windows.Forms.Button();
            this.lblCamFound = new System.Windows.Forms.Label();
            this.lblSelectCamera = new System.Windows.Forms.Label();
            this.cmbCameras = new System.Windows.Forms.ComboBox();
            this.chkAOI = new System.Windows.Forms.CheckBox();
            this.chkAutoSave = new System.Windows.Forms.CheckBox();
            this.lblAutoWhiteBalance = new System.Windows.Forms.Label();
            this.cmbWhiteBalance = new System.Windows.Forms.ComboBox();
            this.lblPivMode = new System.Windows.Forms.Label();
            this.cmbPivMode = new System.Windows.Forms.ComboBox();
            this.lblFrameIdPrompt = new System.Windows.Forms.Label();
            this.lblFrameId = new System.Windows.Forms.Label();
            this.lblFpsPrompt = new System.Windows.Forms.Label();
            this.lblFps = new System.Windows.Forms.Label();
            this.lblExposurePrompt = new System.Windows.Forms.Label();
            this.lblExposure = new System.Windows.Forms.Label();
            this.lblPixelFormatPrompt = new System.Windows.Forms.Label();
            this.lblPixelFormat = new System.Windows.Forms.Label();
            this.lblResolutionPrompt = new System.Windows.Forms.Label();
            this.lblResolution = new System.Windows.Forms.Label();
            this.lblRoiOffsetPrompt = new System.Windows.Forms.Label();
            this.lblRoiOffset = new System.Windows.Forms.Label();
            this.lblSavedPrompt = new System.Windows.Forms.Label();
            this.lblSavedFrame = new System.Windows.Forms.Label();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnSoftTrigger = new System.Windows.Forms.Button();
            this.lineSeparator = new System.Windows.Forms.Panel();
            this.grpTriggerMode = new System.Windows.Forms.GroupBox();
            this.rbHwTrigger = new System.Windows.Forms.RadioButton();
            this.rbSwTrigger = new System.Windows.Forms.RadioButton();
            this.rbFreeRun = new System.Windows.Forms.RadioButton();
            this.btnOpenSvsCamera = new System.Windows.Forms.Button();
            this.lblModeTips = new System.Windows.Forms.Label();
            this.btnSoftwareTrigger = new System.Windows.Forms.Button();
            this.btnSoftwareTriggerTimer = new System.Windows.Forms.Button();
            this.btnCloseSvsCamera = new System.Windows.Forms.Button();
            this.btnShowDisplay = new System.Windows.Forms.Button();
            this.btnQuit = new System.Windows.Forms.Button();
            this.timer1fps = new System.Windows.Forms.Timer(this.components);
            this.timerFps = new System.Windows.Forms.Timer(this.components);
            this.grpTriggerMode.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnDiscovery
            // 
            this.btnDiscovery.Location = new System.Drawing.Point(24, 16);
            this.btnDiscovery.Name = "btnDiscovery";
            this.btnDiscovery.Size = new System.Drawing.Size(280, 32);
            this.btnDiscovery.TabIndex = 0;
            this.btnDiscovery.Text = "Discovery";
            this.btnDiscovery.UseVisualStyleBackColor = true;
            this.btnDiscovery.Click += new System.EventHandler(this.btnDiscovery_Click);
            // 
            // lblCamFound
            // 
            this.lblCamFound.Location = new System.Drawing.Point(24, 52);
            this.lblCamFound.Name = "lblCamFound";
            this.lblCamFound.Size = new System.Drawing.Size(280, 20);
            this.lblCamFound.TabIndex = 1;
            this.lblCamFound.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblSelectCamera
            // 
            this.lblSelectCamera.AutoSize = true;
            this.lblSelectCamera.Location = new System.Drawing.Point(24, 76);
            this.lblSelectCamera.Name = "lblSelectCamera";
            this.lblSelectCamera.Size = new System.Drawing.Size(76, 13);
            this.lblSelectCamera.TabIndex = 2;
            this.lblSelectCamera.Text = "Select Camera";
            // 
            // cmbCameras
            // 
            this.cmbCameras.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCameras.FormattingEnabled = true;
            this.cmbCameras.Location = new System.Drawing.Point(24, 96);
            this.cmbCameras.Name = "cmbCameras";
            this.cmbCameras.Size = new System.Drawing.Size(180, 21);
            this.cmbCameras.TabIndex = 3;
            this.cmbCameras.SelectedIndexChanged += new System.EventHandler(this.cmbCameras_SelectedIndexChanged);
            // 
            // chkAOI
            // 
            this.chkAOI.AutoSize = true;
            this.chkAOI.Location = new System.Drawing.Point(24, 135);
            this.chkAOI.Name = "chkAOI";
            this.chkAOI.Size = new System.Drawing.Size(100, 17);
            this.chkAOI.TabIndex = 4;
            this.chkAOI.Text = "ROI: 200*200";
            this.chkAOI.UseVisualStyleBackColor = true;
            this.chkAOI.CheckedChanged += new System.EventHandler(this.chkAOI_CheckedChanged);
            // 
            // chkAutoSave
            // 
            this.chkAutoSave.AutoSize = true;
            this.chkAutoSave.Enabled = false;
            this.chkAutoSave.Location = new System.Drawing.Point(170, 135);
            this.chkAutoSave.Name = "chkAutoSave";
            this.chkAutoSave.Size = new System.Drawing.Size(81, 17);
            this.chkAutoSave.TabIndex = 5;
            this.chkAutoSave.Text = "Auto Save ";
            this.chkAutoSave.UseVisualStyleBackColor = true;
            this.chkAutoSave.CheckedChanged += new System.EventHandler(this.chkAutoSave_CheckedChanged);
            // 
            // lblAutoWhiteBalance
            // 
            this.lblAutoWhiteBalance.AutoSize = true;
            this.lblAutoWhiteBalance.Enabled = false;
            this.lblAutoWhiteBalance.Location = new System.Drawing.Point(24, 175);
            this.lblAutoWhiteBalance.Name = "lblAutoWhiteBalance";
            this.lblAutoWhiteBalance.Size = new System.Drawing.Size(95, 13);
            this.lblAutoWhiteBalance.TabIndex = 6;
            this.lblAutoWhiteBalance.Text = "AutoWhitebalance";
            // 
            // cmbWhiteBalance
            // 
            this.cmbWhiteBalance.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbWhiteBalance.Enabled = false;
            this.cmbWhiteBalance.FormattingEnabled = true;
            this.cmbWhiteBalance.Items.AddRange(new object[] {
            "Reset",
            "Continuous",
            "Once",
            "Off"});
            this.cmbWhiteBalance.Location = new System.Drawing.Point(24, 195);
            this.cmbWhiteBalance.Name = "cmbWhiteBalance";
            this.cmbWhiteBalance.Size = new System.Drawing.Size(125, 21);
            this.cmbWhiteBalance.TabIndex = 7;
            this.cmbWhiteBalance.SelectedIndexChanged += new System.EventHandler(this.cmbWhiteBalance_SelectedIndexChanged);
            // 
            // lblPivMode
            // 
            this.lblPivMode.AutoSize = true;
            this.lblPivMode.Enabled = false;
            this.lblPivMode.Location = new System.Drawing.Point(170, 175);
            this.lblPivMode.Name = "lblPivMode";
            this.lblPivMode.Size = new System.Drawing.Size(53, 13);
            this.lblPivMode.TabIndex = 8;
            this.lblPivMode.Text = "PIV Mode";
            // 
            // cmbPivMode
            // 
            this.cmbPivMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPivMode.Enabled = false;
            this.cmbPivMode.FormattingEnabled = true;
            this.cmbPivMode.Items.AddRange(new object[] {
            "Off",
            "ON"});
            this.cmbPivMode.Location = new System.Drawing.Point(170, 195);
            this.cmbPivMode.Name = "cmbPivMode";
            this.cmbPivMode.Size = new System.Drawing.Size(125, 21);
            this.cmbPivMode.TabIndex = 9;
            this.cmbPivMode.SelectedIndexChanged += new System.EventHandler(this.cmbPivMode_SelectedIndexChanged);
            // 
            // lblFrameIdPrompt
            // 
            this.lblFrameIdPrompt.AutoSize = true;
            this.lblFrameIdPrompt.Location = new System.Drawing.Point(24, 255);
            this.lblFrameIdPrompt.Name = "lblFrameIdPrompt";
            this.lblFrameIdPrompt.Size = new System.Drawing.Size(53, 13);
            this.lblFrameIdPrompt.TabIndex = 10;
            this.lblFrameIdPrompt.Text = "Frame ID:";
            // 
            // lblFrameId
            // 
            this.lblFrameId.AutoSize = true;
            this.lblFrameId.Location = new System.Drawing.Point(95, 255);
            this.lblFrameId.Name = "lblFrameId";
            this.lblFrameId.Size = new System.Drawing.Size(13, 13);
            this.lblFrameId.TabIndex = 11;
            this.lblFrameId.Text = "0";
            // 
            // lblFpsPrompt
            // 
            this.lblFpsPrompt.AutoSize = true;
            this.lblFpsPrompt.Location = new System.Drawing.Point(170, 255);
            this.lblFpsPrompt.Name = "lblFpsPrompt";
            this.lblFpsPrompt.Size = new System.Drawing.Size(29, 13);
            this.lblFpsPrompt.TabIndex = 26;
            this.lblFpsPrompt.Text = "FPS:";
            // 
            // lblFps
            // 
            this.lblFps.AutoSize = true;
            this.lblFps.Location = new System.Drawing.Point(205, 255);
            this.lblFps.Name = "lblFps";
            this.lblFps.Size = new System.Drawing.Size(43, 13);
            this.lblFps.TabIndex = 27;
            this.lblFps.Text = "0.0 fps";
            // 
            // lblExposurePrompt
            // 
            this.lblExposurePrompt.AutoSize = true;
            this.lblExposurePrompt.Location = new System.Drawing.Point(24, 280);
            this.lblExposurePrompt.Name = "lblExposurePrompt";
            this.lblExposurePrompt.Size = new System.Drawing.Size(54, 13);
            this.lblExposurePrompt.TabIndex = 28;
            this.lblExposurePrompt.Text = "Exposure:";
            // 
            // lblExposure
            // 
            this.lblExposure.AutoSize = true;
            this.lblExposure.Location = new System.Drawing.Point(105, 280);
            this.lblExposure.Name = "lblExposure";
            this.lblExposure.Size = new System.Drawing.Size(10, 13);
            this.lblExposure.TabIndex = 29;
            this.lblExposure.Text = "-";
            // 
            // lblPixelFormatPrompt
            // 
            this.lblPixelFormatPrompt.AutoSize = true;
            this.lblPixelFormatPrompt.Location = new System.Drawing.Point(24, 305);
            this.lblPixelFormatPrompt.Name = "lblPixelFormatPrompt";
            this.lblPixelFormatPrompt.Size = new System.Drawing.Size(67, 13);
            this.lblPixelFormatPrompt.TabIndex = 30;
            this.lblPixelFormatPrompt.Text = "Pixel Format:";
            // 
            // lblPixelFormat
            // 
            this.lblPixelFormat.AutoSize = true;
            this.lblPixelFormat.Location = new System.Drawing.Point(105, 305);
            this.lblPixelFormat.Name = "lblPixelFormat";
            this.lblPixelFormat.Size = new System.Drawing.Size(10, 13);
            this.lblPixelFormat.TabIndex = 31;
            this.lblPixelFormat.Text = "-";
            // 
            // lblResolutionPrompt
            // 
            this.lblResolutionPrompt.AutoSize = true;
            this.lblResolutionPrompt.Location = new System.Drawing.Point(24, 330);
            this.lblResolutionPrompt.Name = "lblResolutionPrompt";
            this.lblResolutionPrompt.Size = new System.Drawing.Size(60, 13);
            this.lblResolutionPrompt.TabIndex = 32;
            this.lblResolutionPrompt.Text = "Resolution:";
            // 
            // lblResolution
            // 
            this.lblResolution.AutoSize = true;
            this.lblResolution.Location = new System.Drawing.Point(105, 330);
            this.lblResolution.Name = "lblResolution";
            this.lblResolution.Size = new System.Drawing.Size(10, 13);
            this.lblResolution.TabIndex = 33;
            this.lblResolution.Text = "-";
            // 
            // lblRoiOffsetPrompt
            // 
            this.lblRoiOffsetPrompt.AutoSize = true;
            this.lblRoiOffsetPrompt.Location = new System.Drawing.Point(24, 355);
            this.lblRoiOffsetPrompt.Name = "lblRoiOffsetPrompt";
            this.lblRoiOffsetPrompt.Size = new System.Drawing.Size(62, 13);
            this.lblRoiOffsetPrompt.TabIndex = 34;
            this.lblRoiOffsetPrompt.Text = "ROI Offset:";
            // 
            // lblRoiOffset
            // 
            this.lblRoiOffset.AutoSize = true;
            this.lblRoiOffset.Location = new System.Drawing.Point(105, 355);
            this.lblRoiOffset.Name = "lblRoiOffset";
            this.lblRoiOffset.Size = new System.Drawing.Size(10, 13);
            this.lblRoiOffset.TabIndex = 35;
            this.lblRoiOffset.Text = "-";
            // 
            // lblSavedPrompt
            // 
            this.lblSavedPrompt.AutoSize = true;
            this.lblSavedPrompt.Location = new System.Drawing.Point(24, 380);
            this.lblSavedPrompt.Name = "lblSavedPrompt";
            this.lblSavedPrompt.Size = new System.Drawing.Size(41, 13);
            this.lblSavedPrompt.TabIndex = 12;
            this.lblSavedPrompt.Text = "Saved:";
            this.lblSavedPrompt.Visible = false;
            // 
            // lblSavedFrame
            // 
            this.lblSavedFrame.AutoSize = true;
            this.lblSavedFrame.Location = new System.Drawing.Point(105, 380);
            this.lblSavedFrame.Name = "lblSavedFrame";
            this.lblSavedFrame.Size = new System.Drawing.Size(0, 13);
            this.lblSavedFrame.TabIndex = 13;
            this.lblSavedFrame.Visible = false;
            // 
            // btnStop
            // 
            this.btnStop.Location = new System.Drawing.Point(24, 225);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(75, 23);
            this.btnStop.TabIndex = 14;
            this.btnStop.Text = "Stop";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Visible = false;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(110, 225);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(75, 23);
            this.btnStart.TabIndex = 15;
            this.btnStart.Text = "Start";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Visible = false;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // btnSoftTrigger
            // 
            this.btnSoftTrigger.Location = new System.Drawing.Point(195, 225);
            this.btnSoftTrigger.Name = "btnSoftTrigger";
            this.btnSoftTrigger.Size = new System.Drawing.Size(85, 23);
            this.btnSoftTrigger.TabIndex = 16;
            this.btnSoftTrigger.Text = "Soft Trigger";
            this.btnSoftTrigger.UseVisualStyleBackColor = true;
            this.btnSoftTrigger.Visible = false;
            this.btnSoftTrigger.Click += new System.EventHandler(this.btnSoftTrigger_Click);
            // 
            // lineSeparator
            // 
            this.lineSeparator.BackColor = System.Drawing.SystemColors.ControlDark;
            this.lineSeparator.Location = new System.Drawing.Point(325, 12);
            this.lineSeparator.Name = "lineSeparator";
            this.lineSeparator.Size = new System.Drawing.Size(1, 385);
            this.lineSeparator.TabIndex = 17;
            // 
            // grpTriggerMode
            // 
            this.grpTriggerMode.Controls.Add(this.rbHwTrigger);
            this.grpTriggerMode.Controls.Add(this.rbSwTrigger);
            this.grpTriggerMode.Controls.Add(this.rbFreeRun);
            this.grpTriggerMode.Location = new System.Drawing.Point(345, 12);
            this.grpTriggerMode.Name = "grpTriggerMode";
            this.grpTriggerMode.Size = new System.Drawing.Size(330, 95);
            this.grpTriggerMode.TabIndex = 18;
            this.grpTriggerMode.TabStop = false;
            this.grpTriggerMode.Text = "JT SVS Trigger Mode Selection:";
            // 
            // rbHwTrigger
            // 
            this.rbHwTrigger.AutoSize = true;
            this.rbHwTrigger.Location = new System.Drawing.Point(16, 68);
            this.rbHwTrigger.Name = "rbHwTrigger";
            this.rbHwTrigger.Size = new System.Drawing.Size(138, 17);
            this.rbHwTrigger.TabIndex = 2;
            this.rbHwTrigger.Text = "Hardware Trigger Mode";
            this.rbHwTrigger.UseVisualStyleBackColor = true;
            this.rbHwTrigger.CheckedChanged += new System.EventHandler(this.rbHwTrigger_CheckedChanged);
            // 
            // rbSwTrigger
            // 
            this.rbSwTrigger.AutoSize = true;
            this.rbSwTrigger.Checked = true;
            this.rbSwTrigger.Location = new System.Drawing.Point(16, 44);
            this.rbSwTrigger.Name = "rbSwTrigger";
            this.rbSwTrigger.Size = new System.Drawing.Size(134, 17);
            this.rbSwTrigger.TabIndex = 1;
            this.rbSwTrigger.TabStop = true;
            this.rbSwTrigger.Text = "Software Trigger Mode";
            this.rbSwTrigger.UseVisualStyleBackColor = true;
            this.rbSwTrigger.CheckedChanged += new System.EventHandler(this.rbSwTrigger_CheckedChanged);
            // 
            // rbFreeRun
            // 
            this.rbFreeRun.AutoSize = true;
            this.rbFreeRun.Location = new System.Drawing.Point(16, 20);
            this.rbFreeRun.Name = "rbFreeRun";
            this.rbFreeRun.Size = new System.Drawing.Size(95, 17);
            this.rbFreeRun.TabIndex = 0;
            this.rbFreeRun.Text = "FreeRun Mode";
            this.rbFreeRun.UseVisualStyleBackColor = true;
            this.rbFreeRun.CheckedChanged += new System.EventHandler(this.rbFreeRun_CheckedChanged);
            // 
            // btnOpenSvsCamera
            // 
            this.btnOpenSvsCamera.Location = new System.Drawing.Point(345, 115);
            this.btnOpenSvsCamera.Name = "btnOpenSvsCamera";
            this.btnOpenSvsCamera.Size = new System.Drawing.Size(200, 28);
            this.btnOpenSvsCamera.TabIndex = 19;
            this.btnOpenSvsCamera.Text = "Open SVS Camera";
            this.btnOpenSvsCamera.UseVisualStyleBackColor = true;
            this.btnOpenSvsCamera.Click += new System.EventHandler(this.btnOpenSvsCamera_Click);
            // 
            // lblModeTips
            // 
            this.lblModeTips.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblModeTips.ForeColor = System.Drawing.Color.Navy;
            this.lblModeTips.Location = new System.Drawing.Point(345, 148);
            this.lblModeTips.Name = "lblModeTips";
            this.lblModeTips.Size = new System.Drawing.Size(330, 32);
            this.lblModeTips.TabIndex = 20;
            // 
            // btnSoftwareTrigger
            // 
            this.btnSoftwareTrigger.Location = new System.Drawing.Point(345, 185);
            this.btnSoftwareTrigger.Name = "btnSoftwareTrigger";
            this.btnSoftwareTrigger.Size = new System.Drawing.Size(330, 28);
            this.btnSoftwareTrigger.TabIndex = 21;
            this.btnSoftwareTrigger.Text = "Software Trigger";
            this.btnSoftwareTrigger.UseVisualStyleBackColor = true;
            this.btnSoftwareTrigger.Click += new System.EventHandler(this.btnSoftwareTrigger_Click);
            // 
            // btnSoftwareTriggerTimer
            // 
            this.btnSoftwareTriggerTimer.Location = new System.Drawing.Point(345, 220);
            this.btnSoftwareTriggerTimer.Name = "btnSoftwareTriggerTimer";
            this.btnSoftwareTriggerTimer.Size = new System.Drawing.Size(330, 28);
            this.btnSoftwareTriggerTimer.TabIndex = 22;
            this.btnSoftwareTriggerTimer.Text = "Software Trigger Timer 1fps";
            this.btnSoftwareTriggerTimer.UseVisualStyleBackColor = true;
            this.btnSoftwareTriggerTimer.Click += new System.EventHandler(this.btnSoftwareTriggerTimer_Click);
            // 
            // btnCloseSvsCamera
            // 
            this.btnCloseSvsCamera.Location = new System.Drawing.Point(345, 255);
            this.btnCloseSvsCamera.Name = "btnCloseSvsCamera";
            this.btnCloseSvsCamera.Size = new System.Drawing.Size(200, 28);
            this.btnCloseSvsCamera.TabIndex = 23;
            this.btnCloseSvsCamera.Text = "Close SVS Camera";
            this.btnCloseSvsCamera.UseVisualStyleBackColor = true;
            this.btnCloseSvsCamera.Click += new System.EventHandler(this.btnCloseSvsCamera_Click);
            // 
            // btnShowDisplay
            // 
            this.btnShowDisplay.Location = new System.Drawing.Point(345, 360);
            this.btnShowDisplay.Name = "btnShowDisplay";
            this.btnShowDisplay.Size = new System.Drawing.Size(120, 26);
            this.btnShowDisplay.TabIndex = 24;
            this.btnShowDisplay.Text = "Show Display";
            this.btnShowDisplay.UseVisualStyleBackColor = true;
            this.btnShowDisplay.Click += new System.EventHandler(this.btnShowDisplay_Click);
            // 
            // btnQuit
            // 
            this.btnQuit.Location = new System.Drawing.Point(595, 360);
            this.btnQuit.Name = "btnQuit";
            this.btnQuit.Size = new System.Drawing.Size(80, 26);
            this.btnQuit.TabIndex = 25;
            this.btnQuit.Text = "Quit";
            this.btnQuit.UseVisualStyleBackColor = true;
            this.btnQuit.Click += new System.EventHandler(this.btnQuit_Click);
            // 
            // timer1fps
            // 
            this.timer1fps.Interval = 1000;
            this.timer1fps.Tick += new System.EventHandler(this.timer1fps_Tick);
            // 
            // timerFps
            // 
            this.timerFps.Interval = 250;
            this.timerFps.Tick += new System.EventHandler(this.timerFps_Tick);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(704, 412);
            this.Controls.Add(this.btnQuit);
            this.Controls.Add(this.btnShowDisplay);
            this.Controls.Add(this.btnCloseSvsCamera);
            this.Controls.Add(this.btnSoftwareTriggerTimer);
            this.Controls.Add(this.btnSoftwareTrigger);
            this.Controls.Add(this.lblModeTips);
            this.Controls.Add(this.btnOpenSvsCamera);
            this.Controls.Add(this.grpTriggerMode);
            this.Controls.Add(this.lineSeparator);
            this.Controls.Add(this.btnSoftTrigger);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.lblSavedFrame);
            this.Controls.Add(this.lblSavedPrompt);
            this.Controls.Add(this.lblRoiOffset);
            this.Controls.Add(this.lblRoiOffsetPrompt);
            this.Controls.Add(this.lblResolution);
            this.Controls.Add(this.lblResolutionPrompt);
            this.Controls.Add(this.lblPixelFormat);
            this.Controls.Add(this.lblPixelFormatPrompt);
            this.Controls.Add(this.lblExposure);
            this.Controls.Add(this.lblExposurePrompt);
            this.Controls.Add(this.lblFps);
            this.Controls.Add(this.lblFpsPrompt);
            this.Controls.Add(this.lblFrameId);
            this.Controls.Add(this.lblFrameIdPrompt);
            this.Controls.Add(this.cmbPivMode);
            this.Controls.Add(this.lblPivMode);
            this.Controls.Add(this.cmbWhiteBalance);
            this.Controls.Add(this.lblAutoWhiteBalance);
            this.Controls.Add(this.chkAutoSave);
            this.Controls.Add(this.chkAOI);
            this.Controls.Add(this.cmbCameras);
            this.Controls.Add(this.lblSelectCamera);
            this.Controls.Add(this.lblCamFound);
            this.Controls.Add(this.btnDiscovery);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SVCamControl";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.grpTriggerMode.ResumeLayout(false);
            this.grpTriggerMode.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Button btnDiscovery;
        private System.Windows.Forms.Label lblCamFound;
        private System.Windows.Forms.Label lblSelectCamera;
        private System.Windows.Forms.ComboBox cmbCameras;
        private System.Windows.Forms.CheckBox chkAOI;
        private System.Windows.Forms.CheckBox chkAutoSave;
        private System.Windows.Forms.Label lblAutoWhiteBalance;
        private System.Windows.Forms.ComboBox cmbWhiteBalance;
        private System.Windows.Forms.Label lblPivMode;
        private System.Windows.Forms.ComboBox cmbPivMode;
        private System.Windows.Forms.Label lblFrameIdPrompt;
        private System.Windows.Forms.Label lblFrameId;
        private System.Windows.Forms.Label lblFpsPrompt;
        private System.Windows.Forms.Label lblFps;
        private System.Windows.Forms.Label lblExposurePrompt;
        private System.Windows.Forms.Label lblExposure;
        private System.Windows.Forms.Label lblPixelFormatPrompt;
        private System.Windows.Forms.Label lblPixelFormat;
        private System.Windows.Forms.Label lblResolutionPrompt;
        private System.Windows.Forms.Label lblResolution;
        private System.Windows.Forms.Label lblRoiOffsetPrompt;
        private System.Windows.Forms.Label lblRoiOffset;
        private System.Windows.Forms.Label lblSavedPrompt;
        private System.Windows.Forms.Label lblSavedFrame;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnSoftTrigger;
        private System.Windows.Forms.Panel lineSeparator;
        private System.Windows.Forms.GroupBox grpTriggerMode;
        private System.Windows.Forms.RadioButton rbHwTrigger;
        private System.Windows.Forms.RadioButton rbSwTrigger;
        private System.Windows.Forms.RadioButton rbFreeRun;
        private System.Windows.Forms.Button btnOpenSvsCamera;
        private System.Windows.Forms.Label lblModeTips;
        private System.Windows.Forms.Button btnSoftwareTrigger;
        private System.Windows.Forms.Button btnSoftwareTriggerTimer;
        private System.Windows.Forms.Button btnCloseSvsCamera;
        private System.Windows.Forms.Button btnShowDisplay;
        private System.Windows.Forms.Button btnQuit;
        private System.Windows.Forms.Timer timer1fps;
        private System.Windows.Forms.Timer timerFps;
    }
}
