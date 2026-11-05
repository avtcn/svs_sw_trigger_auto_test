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
- **AOI (Area of Interest)**:
  - Toggle between 200x200 AOI and full sensor resolution.
- **White Balance & PIV Mode**:
  - White balance options: Reset, Continuous, Once, Off.
  - PIV Mode: Off, On.

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
