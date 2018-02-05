using ContactTracing.ViewModel;
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
    /// Interaction logic for Occupations.xaml
    /// </summary>
    public partial class Occupations : UserControl
    {
        public Occupations()
        {
            InitializeComponent();
            this.IsVisibleChanged += Occupations_IsVisibleChanged;

        }

        void Occupations_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            SwapTextBoxesAndComboBoxes();
        }

        private void cmbTransportType_LostFocus(object sender, RoutedEventArgs e)
        {
            ComboBox cmbTransportType = (ComboBox)sender;

            if (cmbTransportType.Text.Replace(" ", "").Length == 0)
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
            if (vm != null && vm.TransportTypes != null && vm.TransportTypes.Count > 0)
            {
                foreach (string hospital in vm.TransportTypes)
                {
                    if (hospital.Equals(cmbTransportType.Text))
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
                cmbTransportType.Text = "";
                MessageBox.Show("Invalid Transportation Type", null, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cmbOtherOccupationSpecify_LostFocus(object sender, RoutedEventArgs e)
        {
            ComboBox cmbTransportType = (ComboBox)sender;

            if (cmbTransportType.Text.Replace(" ", "").Length == 0)
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
            if (vm != null && vm.OtherOccupDetails != null && vm.OtherOccupDetails.Count > 0)
            {
                foreach (string hospital in vm.OtherOccupDetails)
                {
                    if (hospital.Equals(cmbTransportType.Text))
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
                cmbTransportType.Text = "";
                MessageBox.Show("Invalid Occupation", null, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SwapTextBoxesAndComboBoxes()
        {
            EpiDataHelper vm = Application.Current.MainWindow.DataContext as EpiDataHelper;
            if (vm != null && vm.CaseForm != null)
            {
                #region TransporterType
                if (vm.CaseForm.Fields["TransporterType"] is Epi.Fields.DDLFieldOfLegalValues || vm.CaseForm.Fields["TransporterType"] is Epi.Fields.DDLFieldOfCodes
                    || vm.CaseForm.Fields["TransporterType"] is Epi.Fields.DDLFieldOfCommentLegal)
                {
                    cmbTransportType.Visibility = System.Windows.Visibility.Visible;
                    txtTransportType.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    cmbTransportType.Visibility = System.Windows.Visibility.Collapsed;
                    txtTransportType.Visibility = System.Windows.Visibility.Visible;
                }
                #endregion

                #region OtherOccupDetail
                if (vm.CaseForm.Fields["OtherOccupDetail"] is Epi.Fields.DDLFieldOfLegalValues || vm.CaseForm.Fields["OtherOccupDetail"] is Epi.Fields.DDLFieldOfCodes
                    || vm.CaseForm.Fields["OtherOccupDetail"] is Epi.Fields.DDLFieldOfCommentLegal)
                {
                    cmbOtherOccupationSpecify.Visibility = System.Windows.Visibility.Visible;
                    txtOtherOccupationSpecify.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    cmbOtherOccupationSpecify.Visibility = System.Windows.Visibility.Collapsed;
                    txtOtherOccupationSpecify.Visibility = System.Windows.Visibility.Visible;
                }
                #endregion

                #region BusinessType
                if (vm.CaseForm.Fields["BusinessType"] is Epi.Fields.DDLFieldOfLegalValues || vm.CaseForm.Fields["BusinessType"] is Epi.Fields.DDLFieldOfCodes
                    || vm.CaseForm.Fields["BusinessType"] is Epi.Fields.DDLFieldOfCommentLegal)
                {
                    cmbBusinessType.Visibility = System.Windows.Visibility.Visible;
                    txtBusinessType.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    cmbBusinessType.Visibility = System.Windows.Visibility.Collapsed;
                    txtBusinessType.Visibility = System.Windows.Visibility.Visible;
                }
                #endregion
            }
        }
    }
}
