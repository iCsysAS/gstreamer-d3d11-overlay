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
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;

namespace GStreamerD3DSampleCore
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private Stopwatch timer = new Stopwatch();
        private long lastTime = 0;
        private double average = 0;
        private int countMeasure = 0;
        public MainWindow()
        {
            InitializeComponent();

            Loaded += Window_Loaded;

            timer.Start();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            CompositionTarget.Rendering += CompositionTarget_Rendering;

        }

        private void CompositionTarget_Rendering(object sender, EventArgs e) {
            long newTime = timer.ElapsedMilliseconds;
            average = (average * countMeasure + newTime - lastTime) / (countMeasure + 1);
            countMeasure++;
            Console.WriteLine($"elapsed {newTime - lastTime}ms; average {average}ms");
            lastTime = newTime;
        }

        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            stream1.Dispose();
            stream2.Dispose();
            Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        public void Dispose() {
            timer.Stop();
        }
    }
}
