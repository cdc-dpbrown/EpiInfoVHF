using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using Epi;
using ContactTracing.ViewModel;

namespace ContactTracing.CaseView.Controls
{
    /// <summary>
    /// Interaction logic for ImportFromLab.xaml
    /// </summary>
    public partial class ImportFromLab : UserControl
    {
        public string FileName { get; set; }

        public event RoutedEventHandler Closed;

        public ImportFromLab()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".pkg7";
            dlg.Filter = "Contact Tracing Data Package|*.pkg7| Contact Tracing Raw Xml|*.xml"; // Filter files by extension

            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results 
            if (result == true)
            {
                FileName = dlg.FileName;
                txtFileName.Text = FileName;
            }
        }

        void xmlUP_ImportFinished(object sender, EventArgs e)
        {
            //if (sender is XmlDataUnpackager)
            //{
            //}
        }

        /// <summary>
        /// Provides callback to the UI thread for setting the progress bar value.
        /// </summary>
        private void CallbackSetProgressBar(double value)
        {
            this.Dispatcher.BeginInvoke(new SetProgressBarDelegate(SetProgressBarValue), value);
        }

        /// <summary>
        /// Provides callback to the UI thread for resetting the progress bar.
        /// </summary>
        private void CallbackResetProgressBar()
        {
            this.Dispatcher.BeginInvoke(new SetProgressBarDelegate(SetProgressBarValue), 1);
        }

        private void CallbackSetStatusMessage(string message)
        {
            //this.Dispatcher.BeginInvoke(new SetStatusDelegate(SetStatusMessage), message);
        }

        private void CallbackAddStatusMessage(string message)
        {
            this.Dispatcher.BeginInvoke(new UpdateStatusEventHandler(AddNotificationStatusMessage), message);
        }

        private void CallbackIncrementProgressBar(double value)
        {
            this.Dispatcher.BeginInvoke(new SetProgressBarDelegate(IncrementProgressBarValue), value);
        }

        /// <summary>
        /// Sets the status message of the import form
        /// </summary>
        /// <param name="message">The status message to display</param>
        private void SetStatusMessage(string message)
        {
            //txtProgress.Text = message;
        }

        /// <summary>
        /// Sets the progress bar to a given value
        /// </summary>
        /// <param name="value">The value by which to increment</param>
        private void SetProgressBarValue(double value)
        {
            progressBar.IsIndeterminate = false;
            if (value > progressBar.Maximum)
            {
                progressBar.IsIndeterminate = true;
            }
            else
            {
                progressBar.Value = value;
            }
        }

        /// <summary>
        /// Increments the progress bar by a given value
        /// </summary>
        /// <param name="value">The value by which to increment</param>
        private void IncrementProgressBarValue(double value)
        {
            //progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Value = progressBar.Value + value;
        }

        /// <summary>
        /// Adds a status message to the status list box
        /// </summary>
        /// <param name="statusMessage"></param>
        private void AddNotificationStatusMessage(string statusMessage)
        {
            string message = DateTime.Now + ": " + statusMessage;
            //lbxStatus.Items.Add(message);
            //Logger.Log(message);
        }

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            if (FileName == null)
                return;
            XmlDocument xmlDataPackage = new XmlDocument();
            xmlDataPackage.XmlResolver = null;

            this.btnBrowse.IsEnabled = false;
            this.btnClose.IsEnabled = false;
            this.btnImport.IsEnabled = false;

            this.Cursor = Cursors.Wait;

            if (FileName.EndsWith(".xml"))
            {
                xmlDataPackage.Load(FileName);
            }
            else
            {
                try
                {
                    string str = Configuration.DecryptFileToString(FileName, "", "", "", 1000);
                    str = str.Remove(0, 24);
                    string plainText = Epi.ImportExport.ImportExportHelper.UnZip(str);
                    xmlDataPackage.LoadXml(plainText);
                }
                catch (System.Security.Cryptography.CryptographicException ex)
                {
                    Epi.Windows.MsgBox.ShowException(ex);
                }
            }

            if (this.DataContext != null)
            {
                EpiDataHelper dataHelper = this.DataContext as EpiDataHelper;
                if (dataHelper != null && dataHelper.CaseForm != null && dataHelper.IsCountryUS == false && dataHelper.IsDataSyncing == false)
                {
                    View caseForm = dataHelper.CaseForm;
                    PackageInfo info = new PackageInfo(caseForm, xmlDataPackage);
                    BackgroundWorker worker = new BackgroundWorker();
                    worker.DoWork += new DoWorkEventHandler(worker_DoWork);
                    worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
                    worker.RunWorkerAsync(info);
                }
                else
                {
                    throw new InvalidOperationException("DataHelper or DataHelper's case form property cannot be null.");
                }
            }
        }

        void ImportCompleted()
        {
            MessageBox.Show("Import complete. Click OK to refresh the database.", "Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            if (this.DataContext != null)
            {
                EpiDataHelper dataHelper = this.DataContext as EpiDataHelper;
                if (dataHelper != null)
                {
                    dataHelper.RepopulateCollections();
                }
            }
            MessageBox.Show("Database refreshed. Click OK to continue.", "Complete", MessageBoxButton.OK, MessageBoxImage.Information);

            this.btnBrowse.IsEnabled = true;
            this.btnClose.IsEnabled = true;
            this.btnImport.IsEnabled = true;

            this.Cursor = Cursors.Arrow;

            if (Closed != null)
            {
                RoutedEventArgs args = new RoutedEventArgs(ImportFromLab.CloseClickEvent);
                Closed(this, args);
            }
        }

        private class PackageInfo
        {
            public View CaseForm { get; set; }
            public XmlDocument XmlDataPackage { get; set; }

            public PackageInfo(View caseForm, XmlDocument xmlDataPackage)
            {
                CaseForm = caseForm;
                XmlDataPackage = xmlDataPackage;
            }
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.Dispatcher.BeginInvoke(new SimpleEventHandler(ImportCompleted));
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            PackageInfo info = e.Argument as PackageInfo;
            if (info != null)
            {
                ContactTracing.ImportExport.XmlLabDataUnpackager xmlUP = new ContactTracing.ImportExport.XmlLabDataUnpackager(info.CaseForm, info.XmlDataPackage);
                xmlUP.StatusChanged += new UpdateStatusEventHandler(CallbackSetStatusMessage);
                xmlUP.UpdateProgress += new SetProgressBarDelegate(CallbackSetProgressBar);
                xmlUP.ResetProgress += new SimpleEventHandler(CallbackResetProgressBar);
                xmlUP.MessageGenerated += new UpdateStatusEventHandler(CallbackAddStatusMessage);
                xmlUP.ImportFinished += new EventHandler(xmlUP_ImportFinished);
                xmlUP.Append = true;
                xmlUP.Update = true;
                xmlUP.Unpackage();
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            if (Closed != null)
            {
                RoutedEventArgs args = new RoutedEventArgs(ImportFromLab.CloseClickEvent);
                Closed(this, args);
            }
        }

        public static readonly RoutedEvent CloseClickEvent = EventManager.RegisterRoutedEvent(
        "Close", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ImportFromLab));

        public event RoutedEventHandler Close
        {
            add { AddHandler(CloseClickEvent, value); }
            remove { RemoveHandler(CloseClickEvent, value); }
        }
    }
}
