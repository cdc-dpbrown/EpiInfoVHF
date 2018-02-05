using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
using System.Xml.Linq;
using Epi;
using Epi.Data;
using ContactTracing.Core;
using ContactTracing.ImportExport;

namespace ContactTracing.SqlToMdbCopier
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private VhfProject _project;

        public MainWindow()
        {
            InitializeComponent();
            LoadConfig();
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

        private void btnProjectBrowse_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.AutoUpgradeEnabled = true;
            openFileDialog.DefaultExt = "prj";
            openFileDialog.Filter = "VHF Project File (*.prj)|*.prj";

            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            System.Windows.Forms.DialogResult result = openFileDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                if (!String.IsNullOrEmpty(openFileDialog.FileName))
                {
                    XDocument doc = XDocument.Load(openFileDialog.FileName);
                    string dataDriver = doc.Element("Project").Element("CollectedData").Element("Database").Attribute("dataDriver").Value;

                    if (!dataDriver.Equals("Epi.Data.SqlServer.SqlDBFactory, Epi.Data.SqlServer", StringComparison.OrdinalIgnoreCase))
                    {
                        MessageBox.Show("Only projects using Microsoft SQL Server are supported.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    _project = new VhfProject(openFileDialog.FileName);
                }

                tboxProjectFileName.Text = openFileDialog.FileName;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (_project == null)
            {
                MessageBox.Show("No project is selected. Please try again.");
            }

            this.Cursor = Cursors.Wait;

            string guid = Guid.NewGuid().ToString();

            Project newProject = ContactTracing.ImportExport.ImportExportHelper.CreateNewOutbreak("Sierra Leone",
                "en-US", @"Projects\VHF\vhf_" + _project.OutbreakName + "_" + guid + ".prj",
                @"Projects\VHF\vhf_" + _project.OutbreakName + "_" + guid + ".mdb",
                _project.OutbreakDate.Ticks.ToString(),
                _project.OutbreakName);

            Epi.View caseForm = _project.Views[Core.Constants.CASE_FORM_NAME];

            ContactTracing.ImportExport.FormCopier formCopier = new ImportExport.FormCopier(_project, newProject, caseForm);
            formCopier.Copy();

            this.Cursor = Cursors.Arrow;
        }
    }
}
