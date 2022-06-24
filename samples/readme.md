## Build and run the sample WPF Core project on Windows

- Download and install `x64` runtime `gstreamer` binaries [here](https://gstreamer.freedesktop.org/data/pkg/windows/1.20.3/msvc/gstreamer-1.0-msvc-x86_64-1.20.3.msi).
- Add the `bin` folder of the binaries to your `PATH` variable. For example, add `"D:\gstreamer\1.0\msvc_x86_64\bin"` as a path entry (on Windows, `Advanced System Settings` -> `Advanced` tab -> `Environment Variables` button -> Edit `Path` in `System Variables` section).
- Install the `"gstreamer-d3d11-overlay\samples\package\x86_64\GstSharp.1.20.2.1.nupkg"` package to your offline package source with the following command-line (FYI, this package was built for `x86` by `https://github.com/iCsysAS/gstreamer-builder/actions/runs/2330817965`)

`nuget.exe push x86_64\GstSharp.1.20.2.1.nupkg -source "Microsoft Visual Studio Offline Packages"`

- Open the `"gstreamer-d3d11-overlay\samples\GStreamerWPF\GStreamerD3DSample.sln"` solution with Visual Studio.
- Set `GStreamerD3DSampleCore` as Startup project. Select `x64` as build architecture.
- Build the solution. The NuGet restore operation should succeed using both local source and `nuget.org`. No build error at this point.
- Change the stream URL in `Playback.cs` to your liking.
- Start the application and watch the stream getting rendered inside the main window.

### Usual problem during build:

- C1083: Cannot open include file 'ctype.h' or LNK1104: cannot open file ucrtd.lib => go in Control Panel > Uninstall a program and uninstall all the Windows Software Development Kit you have. Then install the Windows SDK here : https://developer.microsoft.com/en-US/windows/downloads/windows-sdk/ and relaunch Visual Studio it should work
