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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using Epi;

namespace ContactTracing.ImportView
{
    /// <summary>
    /// Interaction logic for ImportWindow.xaml
    /// </summary>
    public partial class ImportWindow : Window
    {
        private delegate void ExceptionMessagingHandler(Exception ex);

        public ImportWindow()
        {
            InitializeComponent();

            string[] args = Environment.GetCommandLineArgs();
            
            ImportWindowViewModel vm;

            if (args.Length == 2)
            {
                string projectPath = args[1];

                vm = new ImportWindowViewModel(projectPath);
                this.DataContext = vm;
            }
            else
            {
                vm = new ImportWindowViewModel();
                this.DataContext = vm;
            }

            vm.SyncProblemsDetected += vm_SyncProblemsDetected;
        }

        private void vm_SyncProblemsDetected(object sender, EventArgs e)
        {
            if (sender is Exception)
            {
                Exception ex = sender as Exception;
                if (ex != null)
                {
                    this.Dispatcher.BeginInvoke(new ExceptionMessagingHandler(DisplaySyncProblems), ex);
                }
            }
            else
            {
                this.Dispatcher.BeginInvoke(new SimpleEventHandler(DisplaySyncProblems));
            }
        }

        private void DisplaySyncProblems()
        {
            MessageBox.Show("Problems were detected during this data sync operation.");
        }

        private void DisplaySyncProblems(Exception ex)
        {
            MessageBox.Show("Problems were detected during this data sync operation. Exception message: " + ex.Message);
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.AutoUpgradeEnabled = true;
            openFileDialog.DefaultExt = "ecs";
            openFileDialog.Filter = "Epi Info VHF Case Sync File (*.ecs)|*.ecs";

            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            System.Windows.Forms.DialogResult result = openFileDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                tboxFileName.Text = openFileDialog.FileName;
            }
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
                }

                tboxProjectFileName.Text = openFileDialog.FileName;
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
