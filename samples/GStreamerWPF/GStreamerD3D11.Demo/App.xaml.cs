using System;
using System.Windows;

namespace GStreamerD3DSampleCore
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        protected override void OnStartup(StartupEventArgs e)
        {
            // Set up a simple configuration that logs on the console.
            log4net.Config.BasicConfigurator.Configure();

            base.OnStartup(e);

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(TopLevelExceptionHandler);
        }

        private void TopLevelExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;

            _log.Fatal(e.Message, e);
            MessageBox.Show(e.ToString());
            Environment.Exit(0);
        }
    }
}
