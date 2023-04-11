using D3D11Scene;
using Gst;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace GStreamerD3D11
{
    public partial class GStreamerCanvas : UserControl, IDisposable
    {

        private D3DImageEx _D3DImageEx;
        private D3D11TestScene _D3D11Scene;


        public static DependencyProperty EnabledProperty = DependencyProperty.Register("Enabled", typeof(bool), typeof(GStreamerCanvas), new PropertyMetadata(false, new PropertyChangedCallback(EnabledPropertyChanged)));

        public bool Enabled
        {
            get { return (bool)GetValue(EnabledProperty); }
            set { SetValue(EnabledProperty, value); }
        }

        public static void EnabledPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GStreamerCanvas streamComponent = (GStreamerCanvas)d;
            streamComponent.Reload();
        }

        public GStreamerCanvas()
        {
            InitializeComponent();
            Loaded += Control_Loaded;
            Unloaded += Control_Unloaded;
            Microsoft.Win32.SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
        }

        private void SystemEvents_SessionSwitch(object sender, Microsoft.Win32.SessionSwitchEventArgs e)
        {
            if (e.Reason == Microsoft.Win32.SessionSwitchReason.SessionUnlock ||
                e.Reason == Microsoft.Win32.SessionSwitchReason.SessionLogon)
            {
                Reload();
            }
            else
            {
                DisposeScene();
            }
        }

        private void Control_Unloaded(object sender, RoutedEventArgs e)
        {
            DisposeScene();
        }

        private void Control_Loaded(object sender, RoutedEventArgs e)
        {
            Reload();
        }

        private void Reload()
        {
            if (!Enabled)
            {
                return;
            }

            _D3DImageEx = new D3DImageEx();
            _D3D11Scene = new D3D11TestScene(1920, 1080);

            d3dScene.Source = _D3DImageEx;

            /* Set the backbuffer, which is a ID3D11Texture2D pointer */
            var renderTarget = _D3D11Scene.GetRenderTarget();
            var backBuffer = _D3DImageEx.CreateBackBuffer(D3DResourceTypeEx.ID3D11Texture2D, renderTarget);
            var enableSoftwareFallback = false;
            _D3DImageEx.Lock();
            _D3DImageEx.SetBackBuffer(D3DResourceType.IDirect3DSurface9, backBuffer, enableSoftwareFallback);
            _D3DImageEx.Unlock();

            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        public void BeginDraw(Element sink, GLib.SignalArgs args)
        {
            if (_D3D11Scene != null)
            {
                var sharedHandle = _D3D11Scene.GetSharedHandle();
                _ = sink.Emit("draw", sharedHandle, (UInt32)2, (UInt64)0, (UInt64)0);
            }
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            InvalidateD3DImage();
        }

        private void InvalidateD3DImage()
        {
            _D3DImageEx.Lock();

            _D3DImageEx.AddDirtyRect(new Int32Rect()
            {
                X = 0,
                Y = 0,
                Height = _D3DImageEx.PixelHeight,
                Width = _D3DImageEx.PixelWidth
            });

            _D3DImageEx.Unlock();
        }


        public void Dispose()
        {
            Microsoft.Win32.SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
            DisposeScene();
        }

        private void DisposeScene()
        {
            if (_D3DImageEx != null)
            {
                _D3D11Scene.Dispose();
            }
        }
    }
}

