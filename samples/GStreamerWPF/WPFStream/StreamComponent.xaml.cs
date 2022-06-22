using D3D11Scene;
using Gst;
using GStreamerD3D.Samples.WPF.D3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WPFStream {
    /// <summary>
    /// Interaction logic for StreamComponent.xaml
    /// </summary>
    public partial class StreamComponent : UserControl {

        private D3DImageEx _d3DImageEx;
        private D3D11TestScene _D3D11Scene;
        private Playback _playback;

        private bool _rtps = false;

        private const bool _enableSoftwareFallback = false;

        public static readonly DependencyProperty RtpsProperty = DependencyProperty.Register("Rtps", typeof(bool), typeof(StreamComponent), new PropertyMetadata(false, new PropertyChangedCallback(RtpsPropertyChanged)));

        public bool Rtps {
            get { return (bool)GetValue(RtpsProperty); }
            set { SetValue(RtpsProperty, value); }
        }

        public static void RtpsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            StreamComponent streamComponent = (StreamComponent)d;
            streamComponent.Reload();
        }

        public static readonly DependencyProperty PipelineProperty = DependencyProperty.Register("Pipeline", typeof(string), typeof(StreamComponent));

        public string Pipeline {
            get { return (string)GetValue(PipelineProperty); }
            set { SetValue(PipelineProperty, value); }
        }

        public StreamComponent() {
            InitializeComponent();
            Utilities.DetectGstPath();
            Loaded += Control_Loaded;
        }

        private void Control_Loaded(object sender, RoutedEventArgs e) {
            Reload();
        }

        private void Reload() {
            
            if (_playback != null)
            {
                _playback.Cleanup();
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

            _playback = new Playback(IntPtr.Zero, Rtps);
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
            _playback.Cleanup();
            _D3D11Scene.Dispose();
        }
    }
}

