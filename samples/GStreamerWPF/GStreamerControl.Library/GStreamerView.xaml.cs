using D3D11Scene;
using Gst;
using GStreamerD3D.Samples.WPF.D3D11;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace GStreamerControl.Library {
    /// <summary>
    /// Interaction logic for StreamComponent.xaml
    /// </summary>
    public partial class GStreamerView : UserControl, IDisposable {

        private D3DImageEx _d3DImageEx;
        private D3D11TestScene _D3D11Scene;
        private Playback _playback;

        private const bool _enableSoftwareFallback = false;

        public static readonly DependencyProperty RtspProperty = DependencyProperty.Register("Rtsp", typeof(bool), typeof(GStreamerView), new PropertyMetadata(false, new PropertyChangedCallback(RtspPropertyChanged)));

        public bool Rtsp {
            get { return (bool)GetValue(RtspProperty); }
            set { SetValue(RtspProperty, value); }
        }

        public static void RtspPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            GStreamerView streamComponent = (GStreamerView)d;
            streamComponent.Reload();
        }

        public static DependencyProperty EnabledProperty = DependencyProperty.Register("Enabled", typeof(bool), typeof(GStreamerView), new PropertyMetadata(false, new PropertyChangedCallback(EnabledPropertyChanged)));

        public bool Enabled {
            get { return (bool)GetValue(EnabledProperty); }
            set { SetValue(EnabledProperty, value); }
        }

        public static void EnabledPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GStreamerView streamComponent = (GStreamerView)d;
            streamComponent.Reload();
        }

        public static readonly DependencyProperty PipelineStringProperty = DependencyProperty.Register("PipelineString", typeof(string), typeof(GStreamerView), new PropertyMetadata("", new PropertyChangedCallback(PipelineStringPropertyChanged)));

        public string PipelineString {
            get { return (string)GetValue(PipelineStringProperty); }
            set { SetValue(PipelineStringProperty, value); }
        }

        public static void PipelineStringPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GStreamerView streamComponent = (GStreamerView)d;
            streamComponent.Reload();
        }

        public GStreamerView() {
            InitializeComponent();
            Utilities.DetectGstPath();
            Loaded += Control_Loaded;
            Unloaded += GStreamerView_Unloaded;
        }

        private void GStreamerView_Unloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }

        private void Control_Loaded(object sender, RoutedEventArgs e) {
            Reload();
        }

        private void Reload() {
            
            if (_playback != null)
            {
                _playback.Cleanup();
            }

            if (!Enabled)
            {
                return;
            }


            _d3DImageEx = new D3DImageEx();
            _D3D11Scene = new D3D11TestScene(1920, 1080);

            d3dScene.Source = _d3DImageEx;

            /* Set the backbuffer, which is a ID3D11Texture2D pointer */
            var renderTarget = _D3D11Scene.GetRenderTarget();
            var backBuffer = _d3DImageEx.CreateBackBuffer(D3DResourceTypeEx.ID3D11Texture2D, renderTarget);

            _d3DImageEx.Lock();
            _d3DImageEx.SetBackBuffer(D3DResourceType.IDirect3DSurface9, backBuffer, _enableSoftwareFallback);
            _d3DImageEx.Unlock();

            _playback = new Playback(IntPtr.Zero, Rtsp, PipelineString);
            _playback.OnDrawSignalReceived += VideoSink_OnBeginDraw;

            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        private void VideoSink_OnBeginDraw(Element sink, GLib.SignalArgs args) {
            var sharedHandle = _D3D11Scene.GetSharedHandle();
            _ = sink.Emit("draw", sharedHandle, (UInt32)2, (UInt64)0, (UInt64)0);
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e) {
            InvalidateD3DImage();
        }

        private void InvalidateD3DImage() {
            _d3DImageEx.Lock();

            _d3DImageEx.AddDirtyRect(new Int32Rect() {
                X = 0,
                Y = 0,
                Height = _d3DImageEx.PixelHeight,
                Width = _d3DImageEx.PixelWidth
            });

            _d3DImageEx.Unlock();
        }


        public void Dispose() {
            if (_playback != null)
            {
                _playback.Cleanup();
            }
            if (_d3DImageEx != null)
            {
                _D3D11Scene.Dispose();
            }
        }

       
    }
}

