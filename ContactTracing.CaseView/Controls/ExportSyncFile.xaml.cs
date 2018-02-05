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
using Epi;
using Epi.ImportExport.Filters;

namespace ContactTracing.CaseView.Controls
{
    /// <summary>
    /// Interaction logic for ExportSyncFile.xaml
    /// </summary>
    public partial class ExportSyncFile : UserControl
    {
        private ExportSyncFileViewModel DataSyncFileViewModel
        {
            get
            {
                return ((this.DataContext) as ExportSyncFileViewModel);
            }
        }
        public ExportSyncFile()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog openFileDialog = new System.Windows.Forms.SaveFileDialog();
            openFileDialog.AutoUpgradeEnabled = true;
            openFileDialog.OverwritePrompt = true;
            openFileDialog.DefaultExt = "ecs";
            openFileDialog.Filter = "Epi Info VHF Sync File (*.sync)|*.sync";

            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            System.Windows.Forms.DialogResult result = openFileDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                tboxFileName.Text = openFileDialog.FileName;
                DataSyncFileViewModel.ShowExportOptions = true;
            }
        }

        private void checkboxFilterData_Unchecked(object sender, RoutedEventArgs e)
        {
            cmbLogicalOperator.SelectedIndex = -1;
            cmbVariableName1.SelectedIndex = -1;
            cmbVariableName2.SelectedIndex = -1;
            tboxValue1.Text = String.Empty;
            tboxValue2.Text = String.Empty;
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
