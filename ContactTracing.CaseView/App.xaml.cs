using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;

using ContactTracing.Core;


namespace ContactTracing.CaseView
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        public static string ApplicationRegion;


        public App()
            : base()
        {
            string culture = ContactTracing.CaseView.Properties.Settings.Default.Culture;
            ApplicationViewModel.Instance.CurrentRegion = ContactTracing.CaseView.Properties.Settings.Default.Region;        

            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);

                CaseView.Properties.Resources.Culture = System.Threading.Thread.CurrentThread.CurrentCulture;

            }
            catch (Exception ex)
            {



                MessageBox.Show(ex.Message);

            }

            //   this.DispatcherUnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(OnDispatcherUnhandledException);
        }


    
        //public static ApplicationViewModel ApplicationViewModel
        //{
        //    get
        //    {
        //        return   ApplicationViewModel.Instance;
        //    }
        //}

        public static void ChangeCulture(string newCultureText)
        {
            CultureInfo newCulture = new CultureInfo(newCultureText);

            Thread.CurrentThread.CurrentCulture = newCulture;
            Thread.CurrentThread.CurrentUICulture = newCulture;

            CaseView.Properties.Resources.Culture = System.Threading.Thread.CurrentThread.CurrentCulture;

            var oldWindow = Application.Current.MainWindow;

            // "Refresh" the ResourceDictionary   
            ResourceDictionary myResourceDictionary = new ResourceDictionary();
            Application.Current.Resources.MergedDictionaries.Clear();
            myResourceDictionary.Source = new Uri("themes/Generic.xaml", UriKind.Relative);
            Application.Current.Resources.MergedDictionaries.Add(myResourceDictionary);


            Application.Current.MainWindow = new MainWindow();
            Application.Current.MainWindow.Show();

            oldWindow.Close();
        }
        void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("An unhandled exception has occurred in the application (" + e.Exception.Message + "). Do you want to keep working?", "Exception", MessageBoxButton.YesNo, MessageBoxImage.Error);
            if (result == MessageBoxResult.Yes)
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = false;
                Application curApp = Application.Current;
                curApp.Shutdown();
            }
        }
    }
}
