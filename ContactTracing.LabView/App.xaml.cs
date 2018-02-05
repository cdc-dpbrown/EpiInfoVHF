using System;
using System.Configuration;
using System.Globalization;
using System.Threading;
using System.Windows;

namespace ContactTracing.LabView
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
            : base()
        {
            string culture = ContactTracing.LabView.Properties.Settings.Default.Culture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);

            LabView.Properties.Resources.Culture = System.Threading.Thread.CurrentThread.CurrentCulture;

            //this.DispatcherUnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(OnDispatcherUnhandledException);
        }
    }
}
