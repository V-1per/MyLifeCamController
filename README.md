# My LifeCam Controller

**English** · [Português](README.pt.md)

A Windows app that controls every setting of a webcam (focus, exposure, zoom, brightness, white balance…). It was made for the **Microsoft LifeCam Studio** but works with any USB (UVC) webcam.

It came from a real problem: the LifeCam Studio's autofocus keeps "hunting" and the image goes blurry every now and then. With this app you turn off **Auto** for focus, set the value by hand, and it stays saved.

## Download

Get `MyLifeCamController.exe` from the [latest release](https://github.com/V-1per/MyLifeCamController/releases/latest). It's a single file, no installer needed. It requires the [.NET 9 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0).

## Features

- Lists the connected cameras and shows **every control the camera supports**, with a slider and an **Auto** checkbox when an automatic mode exists.
- Live **preview** (if another app is using the camera, the controls still work).
- Shows in real time what the automatic modes are doing (for example, the focus value changing).
- **Saves your changes automatically** per camera and re-applies them when you open the app.
- **Apply when Windows starts**: the LifeCam goes back to autofocus when you restart the PC; this option re-applies your settings at every sign-in.
- **Driver settings…** button that opens the driver's own settings window.
- **Reset to defaults** puts everything back to the factory values.
- **English / Português**: pick the language at the top of the window (by default it follows the Windows language).

## Requirements

- Windows 10/11
- [.NET 9 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) (to run)
- .NET 9 SDK (only to build)

## Build and run

```powershell
dotnet build -c Release
dotnet run --project src/MyLifeCamController -c Release
```

Create a single executable in `publish/`:

```powershell
dotnet publish src/MyLifeCamController -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish
```

> Turn on "Apply my settings when Windows starts" from the executable in the folder where it will stay: the startup entry stores the executable's path.

## Tests

```powershell
dotnet test                                  # all
dotnet test --filter Category!=Hardware      # only the ones that don't need a camera
```

The hardware tests use the real camera: they read the controls and change the brightness for a moment, restoring the original value at the end. If there is no camera, they pass without doing anything.

## Command line

| Command | What it does |
| --- | --- |
| `MyLifeCamController.exe` | Opens the window. |
| `MyLifeCamController.exe --apply` | Applies the saved settings to every connected camera, without a window (waits up to 30 s for the camera to appear). Writes a log to `%APPDATA%\MyLifeCamController\apply.log`. |

## How it works

The app talks directly to the driver through the DirectShow interfaces `IAMCameraControl` (focus, exposure, zoom, pan, tilt…) and `IAMVideoProcAmp` (brightness, contrast, white balance…). The COM interop is hand-written in `src/MyLifeCamController.Core/DirectShow/Interop.cs`, with no external libraries.

```
src/
  MyLifeCamController.Core/   UI-free logic: interop, camera, preview, settings
  MyLifeCamController/        WinForms app (window, control rows, --apply mode, startup)
tests/
  MyLifeCamController.Tests/  xUnit tests (pure logic + real camera)
```

Settings are stored in `%APPDATA%\MyLifeCamController\settings.json`. Only the controls you changed are saved.

## Known limitations

- Some apps (Discord, browsers) may turn autofocus back on when they start using the camera. If that happens, open the app and your settings are applied again.
- Settings are saved per USB port: if you plug the camera into a different port, Windows sees it as a different device.

## Inspiration

- [WebCam-Settings](https://github.com/Simon311/WebCam-Settings): the driver's native settings window
- [CameraControlLib](https://github.com/marcellarius/CameraControlLib): control via DirectShow
- [cfg-cam](https://github.com/SuslikV/cfg-cam): saving and restoring settings
