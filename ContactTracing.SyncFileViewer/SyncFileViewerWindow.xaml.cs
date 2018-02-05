using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
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
using System.Xml.Linq;
using Epi;

namespace ContactTracing.SyncFileViewer
{
    /// <summary>
    /// Interaction logic for SyncFileViewerWindow.xaml
    /// </summary>
    public partial class SyncFileViewerWindow : Window
    {
        public SyncFileViewerWindow()
        {
            InitializeComponent();
            LoadConfig();
        }

        public void LoadSyncFile()
        {
            this.DataContext = new SyncFileViewerViewModel();

            SyncFileViewerViewModel vm = DataContext as SyncFileViewerViewModel;

            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.AutoUpgradeEnabled = true;
            openFileDialog.CheckFileExists = true;
            openFileDialog.DefaultExt = "sync";
            openFileDialog.Filter = "Epi Info VHF Sync File (*.sync)|*.sync";

            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            System.Windows.Forms.DialogResult result = openFileDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string fileName = openFileDialog.FileName;

                vm.SyncFileName = fileName;

                XElement doc;

                try
                {

                    
                    string uncompressedText = String.Empty;
                    
                    Epi.Configuration.DecryptFile(fileName, fileName + ".gz", "vQ@6L'<J3?)~5=vQnwh(2ic;>.<=dknF&/TZ4Uu!$78", "", "", 1000);
                    FileInfo fi = new FileInfo(fileName + ".gz");
                    Epi.ImportExport.ImportExportHelper.DecompressDataPackage(fi);

                    uncompressedText = File.ReadAllText(fileName + ".mdb", Encoding.Default);
                    File.Delete(fileName + ".mdb");
                    File.Delete(fileName + ".gz");

                    //File.WriteAllText(fileName + ".test.txt", uncompressedText, Encoding.Default);

                    uncompressedText = uncompressedText.Replace("<?xml version=\"1.0\" encoding=\"Windows-1252\"?>\r\n", String.Empty);

                    File.WriteAllText(fileName + ".xml", uncompressedText, Encoding.Default);

                    using (StreamReader reader = new StreamReader(fileName + ".xml"))
                    {
                        XmlReaderSettings settings = new XmlReaderSettings();
                        settings.CheckCharacters = false;
                        XmlReader xmlReader = XmlReader.Create(reader, settings);

                        doc = XElement.Load(xmlReader);
                        //xmlString = fileName + ".xml";
                    }
                    File.Delete(fileName + ".xml");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                //SyncFileViewerViewModel vm = this.DataContext as SyncFileViewerViewModel;
                //if (vm != null)
                //{
                    vm.LoadSyncFile(doc);
                //}
            }
        }

        private bool LoadConfig()
        {
            string configFilePath = Configuration.DefaultConfigurationPath;
            bool configurationOk = true;
            try
            {
                string directoryName = System.IO.Path.GetDirectoryName(configFilePath);
                if (!Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }

                if (!File.Exists(configFilePath))
                {
                    Configuration defaultConfig = Configuration.CreateDefaultConfiguration();
                    Configuration.Save(defaultConfig);
                }

                Configuration.Load(configFilePath);
            }
            catch (Epi.ConfigurationException)
            {
            }
            catch (Exception ex)
            {
                configurationOk = String.IsNullOrEmpty(ex.Message);
            }
            return configurationOk;
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            LoadSyncFile();
        }
    }
}
