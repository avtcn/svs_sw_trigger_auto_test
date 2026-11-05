# SVCamMiniSampleGui64-SimpleSWTrigger.NET

A C#/.NET Windows Forms clone of the C++ MFC application `SVCamMiniSampleGui64-SimpleSWTrigger`, based on the SVS-VISTEK SVCam .NET SDK wrapper (`SVCamApi.cs`).

## Overview

This project provides feature-for-feature parity with the C++ MFC version `SVCamMiniSampleGui64-SimpleSWTrigger`:
- **Real-time Live Camera Display**: Independent `DisplayForm` supporting full-screen / maximized viewing with aspect-ratio preservation and format handling for Mono 8-bit, Mono 12-bit, Mono 16-bit, and Bayer/RGB.
- **Topmost Control Window**: `MainForm` (`SVCamControl.NET`) hosting all camera discovery, acquisition parameters, and trigger controls.
- **Trigger Modes Switching**:
  1. **FreeRun Mode**: `TriggerMode = Off`. Continuous free-running acquisition with real-time frame counter.
  2. **Software Trigger Mode**: `TriggerMode = On`, `TriggerSource = Software`. Manual single-shot trigger and automatic periodic trigger.
  3. **Hardware Trigger Mode**: `TriggerMode = On`, `TriggerSource = Line1` (fallback `Line0`). External TTL hardware trigger input.
- **One-Click Software Trigger & Capture**:
  - Emits software trigger command to camera (`TriggerSoftware`).
  - Waits for image capture synchronization.
  - Automatically saves captured images to `Image/<displayName>_<iImageId:D8>_<iTimeStamp>.bmp`.
- **Automated 1 FPS Software Trigger Loop**:
  - Toggles a 1-second interval timer simulating automated software triggers.
  - Displays trigger iteration counter on button text (e.g., `Software Trigger Timer 1fps-00000001`).
- **Continuous Auto-Save**:
  - Checkbox `"Auto Save "` queues streaming frames to background worker thread.
  - Saves continuous frames to `Image/<displayName><iImageId>_<iTimeStamp>_.png`.
- **Real-Time Received FPS, Frame ID & Parameter Display**:
  - Displays real-time frame ID alongside actual measured frame rate received by the application (`Frame ID: xxxx  FPS: yyy fps`).
  - Displays current camera parameters directly below the Frame ID part:
    - `Exposure:` e.g. `10.00 ms (10000 us)`
    - `Pixel Format:` e.g. `Mono8`
    - `Resolution:` e.g. `200 x 200`
    - `ROI Offset:` e.g. `OffsetX: 1948, OffsetY: 1400`
  - Live display window title updates dynamically with resolution and frame ID (`Display - <width>x<height> (Frame: <id>)`).
  - Uses high-resolution sliding-window timestamp measurement with automatic stall detection.
- **Pixel Format & Exposure Configuration (Ported from `SVCamMiniSample64-ROI-Exp-Mono8`)**:
  - Automatically sets image format to `Mono8` via `SetCameraPixelFormat("Mono8")` before acquisition start.
  - Automatically sets exposure time to 10 ms (`10000.0` µs) via `SetCameraExposureTime(10000.0)`.
  - Ensures manual exposure mode (`ExposureAuto = Off`, `ExposureMode = Timed`).
  - Supports feature fallback order: `ExposureTime` -> `ExposureTimeAbs` (SFNC 1.x) -> `ExposureTime_us`.
- **ROI (Region of Interest - Centered 200x200, Ported from `SVCamMiniSample64-ROI-Exp-Mono8`)**:
  - Renamed from AOI to `ROI: 200*200` in the GUI.
  - Centered within the full sensor dimensions:
    - Queries sensor limits (`WidthMax` / `SensorWidth`) and feature increments (`widthInc`, `heightInc`, `offsetXInc`, `offsetYInc`).
    - Snaps width and height to increments and calculates centered offsets: `OffsetX = ((sensorWidth - roiWidth) / 2 / offsetXInc) * offsetXInc`, `OffsetY = ((sensorHeight - roiHeight) / 2 / offsetYInc) * offsetYInc`.
    - Coordinates sequence: resets `OffsetX = 0, OffsetY = 0` first, sets `Width/Height`, then sets centered `OffsetX/OffsetY`.
  - Can be toggled on/off **both before opening the camera and dynamically while streaming**:
    - Pre-open toggle: checking before clicking "Open SVS Camera" opens the camera directly into centered 200x200 ROI.
    - Dynamic toggle: toggling while streaming seamlessly stops acquisition, sets ROI, and restarts with matching buffer sizes.
- **Explicit Camera Open Control**:
  - Selecting FreeRun mode or selecting a camera does **not** automatically open the camera or start acquisition.
  - Camera connection and streaming only begin when the user explicitly clicks the `"Open SVS Camera"` button.
- **Auto-Save & Auxiliary Controls**:
  - `Auto Save` checkbox and background continuous saving thread are disabled.
  - `AutoWhitebalance` and `PIV Mode` controls are disabled.

## Architecture & Code Mapping

| C++ MFC (`SVCamMiniSampleGui64-SimpleSWTrigger`) | C#/.NET (`SVCamMiniSampleGui64-SimpleSWTrigger.NET`) | Description |
|--------------------------------------------------|-------------------------------------------------------|-------------|
| `sv_gen_sdk_dynamic.h`, `sv_gen_sdk.h`           | `SVCamApi.cs`                                         | SVS GenICam SDK P/Invoke and utility wrapper |
| `Camera.h` / `Camera.cpp`                        | `Camera.cs`                                           | Camera connection, streaming, software trigger buffer sync |
| `CameraContainer.h` / `Cameracontainer.cpp`       | `CameraContainer.cs`                                  | SDK initialization, GenTL interface & device discovery |
| `MiniSampleGuiDlg.h` / `MiniSampleGuiDlg.cpp`    | `MainForm.cs` & `MainForm.Designer.cs`                | Main dialog with controls, threads, timer and events |
| `Display.h` / `Display.cpp`                      | `DisplayForm.cs` & `DisplayForm.Designer.cs`          | Modeless live video display window |

## Build Requirements

1. **Operating System**: Windows 10 / 11 (64-bit)
2. **Framework**: .NET Framework 4.8
3. **IDE / Tools**: Visual Studio 2019 / 2022 Community (or MSBuild)
4. **Environment Variables**: Installed with SVCam Kit / SVCapture (e.g. `SVS_GENICAM_ROOT`, `GENICAM_GENTL64_PATH`)

## How to Build

### Using MSBuild:
```powershell
# Build Release x64
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "SVCamMiniSampleGui64-SimpleSWTrigger.NET.sln" /p:Configuration=Release /p:Platform=x64

# Build Debug x64
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "SVCamMiniSampleGui64-SimpleSWTrigger.NET.sln" /p:Configuration=Debug /p:Platform=x64
```

### Using Visual Studio:
1. Open `SVCamMiniSampleGui64-SimpleSWTrigger.NET.sln` in Visual Studio 2022.
2. Select Configuration `Release` or `Debug`, Platform `x64`.
3. Press `Ctrl + Shift + B` to build.
4. Output executable will be in `x64\Release\SVCamMiniSampleGui64-SimpleSWTrigger.NET.exe` or `x64\Debug\SVCamMiniSampleGui64-SimpleSWTrigger.NET.exe`.
