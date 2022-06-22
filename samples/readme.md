## Build and run the sample WPF Core project on Windows

- Download and install `x86` runtime `gstreamer` binaries [here](https://gstreamer.freedesktop.org/data/pkg/windows/1.20.2/msvc/gstreamer-1.0-msvc-x86-1.20.2.msi).
- Add the `bin` folder of the binaries to your `PATH` variable. For example, add `"D:\gstreamer\1.0\msvc_x86\bin"` as a path entry (on Windows, `Advanced System Settings` -> `Advanced` tab -> `Environment Variables` button -> Edit `Path` in `System Variables` section).
- Install the `"gstreamer-d3d11-overlay\samples\package\GstSharp.1.20.2.1.nupkg"` package to your offline package source with the following command-line (FYI, this package was built for `x86` by `https://github.com/iCsysAS/gstreamer-builder/actions/runs/2330817965`)

`nuget.exe push GstSharp.1.20.2.1.nupkg -source "Microsoft Visual Studio Offline Packages"`

- Open the `"gstreamer-d3d11-overlay\samples\GStreamerWPF\GStreamerD3DSample.sln"` solution with Visual Studio.
- Set `GStreamerD3DSampleCore` as Startup project. Select `x86` as build architecture.
- Build the solution. The NuGet restore operation should succeed using both local source and `nuget.org`. No build error at this point.
- Change the stream URL in `Playback.cs` to your liking.
- Start the application and watch the stream getting rendered inside the main window.

usual problem during build :

- C1083 : Cannot open include file 'ctype.h' => search in our folder C:\Program Files (x86)\Windows Kits\10\Include\10.0.19041.0\ucrt and check if ctype.h is in the folder then select D3D11Scene and click on Project > Properties > VC++ Directories and add the folder you find to 'Include Directories'. Do the same with Interop.D3DImageEx
if the folder doesn't exists you may need to install it (Visual Studio Installer > Visual Studio Build Tools > and add SDK Windows 10 (10.0.19041.0), maybe an other version work)
- LNK1104 : cannot open file ucrtd.lib => search in our folder C:\Program Files (x86)\Windows Kits\10\Lib\10.0.19041.0\ucrt\x86 and check if ucrtd.lib is in the folder then select D3D11Scene and click on Project > Properties > VC++ Directories and add the folder you find to 'Library Directories'. Do the same with Interop.D3DImageEx
