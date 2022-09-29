using System;
using System.Windows;
using System.Windows.Media;
using System.Diagnostics;
using GStreamerD3D.Samples.WPF.D3D11;
using System.Windows.Interop;

namespace GStreamerD3DSampleCore
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        private Playback _playback;

        public MainWindow()
        {
            InitializeComponent();

            _playback = new Playback(IntPtr.Zero);
            _playback.OnDrawSignalReceived += stream1.BeginDraw;
            _playback.OnDrawSignalReceived += stream2.BeginDraw;
        }

        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            stream1.Dispose();
            stream2.Dispose();
            Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        public void Dispose() {
            _playback.Cleanup();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //grid.Children.Clear();
        }
    }
}
