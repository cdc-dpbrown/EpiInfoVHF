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

namespace ContactTracing.CaseView.Controls
{
    /// <summary>
    /// Interaction logic for UnexplainedBleeding.xaml
    /// </summary>
    public partial class UnexplainedBleeding : UserControl
    {
        public UnexplainedBleeding()
        {
            InitializeComponent();
            this.IsVisibleChanged += UnexplainedBleeding_IsVisibleChanged;
        }

        void UnexplainedBleeding_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            SwapTextBoxesAndComboBoxes();
        }

        private void cmbBleedOtherComment_LostFocus(object sender, RoutedEventArgs e)
        {
            ComboBox cmbBleedOtherComment = (ComboBox)sender;

            if (cmbBleedOtherComment.Text.Replace(" ", "").Length == 0)
                return;

            EpiDataHelper vm = null;
            FrameworkElement startElement = this.Parent as FrameworkElement;
            while (startElement.Parent != null)
            {
                FrameworkElement parentElement = startElement.Parent as FrameworkElement;
                if (parentElement.DataContext is EpiDataHelper)
                {
                    vm = parentElement.DataContext as EpiDataHelper;
                    break;
                }

                startElement = parentElement;
            }

            if (vm == null) return;

            //EpiDataHelper vm = (((FrameworkElement)this.Parent).Parent as FrameworkElement).DataContext as EpiDataHelper;
            bool match = false;
            if (vm != null && vm.BleedOtherComments != null && vm.BleedOtherComments.Count > 0)
            {
                foreach (string comment in vm.BleedOtherComments)
                {
                    if (comment.Equals(cmbBleedOtherComment.Text))
                    {
                        match = true;
                        break;
                    }
                }
            }
            else
                match = true;

            if (!match)
            {
                cmbBleedOtherComment.Text = "";
                MessageBox.Show("Invalid Symptom", null, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SwapTextBoxesAndComboBoxes()
        {
            EpiDataHelper vm = Application.Current.MainWindow.DataContext as EpiDataHelper;
            if (vm != null && vm.CaseForm != null)
            {
                #region Bleed other comments
                if (vm.CaseForm.Fields["BleedOtherComment"] is Epi.Fields.DDLFieldOfLegalValues || vm.CaseForm.Fields["BleedOtherComment"] is Epi.Fields.DDLFieldOfCodes
                    || vm.CaseForm.Fields["BleedOtherComment"] is Epi.Fields.DDLFieldOfCommentLegal)
                {
                    cmbBleedOtherComment.Visibility = System.Windows.Visibility.Visible;
                    txtBleedOtherComment.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    cmbBleedOtherComment.Visibility = System.Windows.Visibility.Collapsed;
                    txtBleedOtherComment.Visibility = System.Windows.Visibility.Visible;
                }
                #endregion

            }
        }
    }
}
