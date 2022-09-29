# WPF GStreamer component

This repo was forked from [gstreamer-d3d11-overlay](https://github.com/berglie/gstreamer-d3d11-overlay), and tweaked to build a reusable WPF component that can be used by a `d3d11videosink` to render video inside a WPF application. 

## Usage 
By building and using the nuget package in samples/GStreamerControl.Library, you can add a GStreamerView component to your application and then connect the videosink to the View like in Playback.cs in the sample:
```
    sink["draw-on-shared-texture"] = true;
    sink.Connect("begin-draw", VideoSink_OnBeginDraw);
```
See the sample application in `samples/GStreamerWPF/GStreamerControl.Demo.Direct` for details.
