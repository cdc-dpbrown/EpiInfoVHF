using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using ContactTracing.ViewModel;

namespace ContactTracing.CaseView.Controls
{
    /// <summary>
    /// Interaction logic for ShortCaseEntryForm.xaml
    /// </summary>
    public partial class ShortCaseEntryForm : UserControl
    {
        public ShortCaseEntryForm()
        {
            InitializeComponent();
        }
        //Populates drop down(s) - VHF-243
        private void PopulateDropDown(EpiDataHelper vm)
        {
            //Populate cmbSCRes
            if (cmbDistrictRes.Visibility == System.Windows.Visibility.Visible &&
                cmbSCRes.Visibility == System.Windows.Visibility.Visible)
            {
                if (vm != null && vm.DistrictsSubCounties != null && vm.DistrictsSubCounties.Count > 0)
                {
                    string scText = cmbSCRes.Text;
                    cmbSCRes.Items.Clear();

                    if (cmbDistrictRes.SelectedItem != null)
                    {
                        string selectedDistrict = cmbDistrictRes.SelectedItem.ToString();

                        foreach (KeyValuePair<string, List<string>> district in vm.DistrictsSubCounties)
                        {
                            if (district.Key.Equals(selectedDistrict, StringComparison.OrdinalIgnoreCase))
                            {
                                ComboBox cmbDistrict = (ComboBox)cmbDistrictRes;

                                foreach (string sc in district.Value)
                                {
                                    cmbSCRes.Items.Add(sc);
                                }
                            }
                        }
                        if (cmbSCRes.Items.Contains(scText))
                        {
                            cmbSCRes.Text = scText;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// VHF-253
        /// Sets the item source for SC Combox
        /// </summary>
        /// <param name="comboBox"></param>
        /// <param name="vm"></param>
        private void SetSCItemSource(ComboBox comboBox, EpiDataHelper vm)
        {

            foreach (var item in vm.SubCounties)
            {
                comboBox.Items.Add(item);
            }
        }

        private void cmbDistrictRes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            EpiDataHelper vm = ((FrameworkElement)this.Parent).DataContext as EpiDataHelper;
            if (!vm.IsShortFormOpened || this.DataContext == null)//VHF-256, VHF-278
            {
                return;
            }
            //if (vm != null && vm.DistrictsSubCounties != null && vm.DistrictsSubCounties.Count > 0)
            //{
            //    cmbSCRes.Items.Clear();
            //    cmbSCRes.Text = "";
            //    foreach (KeyValuePair<string, List<string>> district in vm.DistrictsSubCounties)
            //    {
            //        ComboBox cmbDistrict = (ComboBox)sender;
            //        if (cmbDistrict.SelectedItem != null)
            //        {
            //            if (cmbDistrict.SelectedItem.ToString() == district.Key)
            //            {
            //                foreach (string sc in district.Value)
            //                {
            //                    cmbSCRes.Items.Add(sc);
            //                }
            //            }
            //        }
            //    }
            //}
            PopulateDropDown(vm);
        }

        private void cmbDistrictOnset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EpiDataHelper vm = ((FrameworkElement)this.Parent).DataContext as EpiDataHelper;
            if (vm != null && vm.DistrictsSubCounties != null && vm.DistrictsSubCounties.Count > 0)
            {
                cmbSCOnset.Items.Clear();
                foreach (KeyValuePair<string, List<string>> district in vm.DistrictsSubCounties)
                {
                    if (cmbDistrictOnset.SelectedItem != null)
                    {
                        if (cmbDistrictOnset.SelectedItem.ToString() == district.Key)
                        {
                            foreach (string sc in district.Value)
                            {
                                cmbSCOnset.Items.Add(sc);
                            }
                        }
                    }
                }
            }
        }

        private void cmbDistrictFuneral_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EpiDataHelper vm = ((FrameworkElement)this.Parent).DataContext as EpiDataHelper;
            if (vm != null && vm.DistrictsSubCounties != null && vm.DistrictsSubCounties.Count > 0)
            {
                cmbSCFuneral.Items.Clear();
                foreach (KeyValuePair<string, List<string>> district in vm.DistrictsSubCounties)
                {
                    if (cmbDistrictFuneral.SelectedItem != null)
                    {
                        if (cmbDistrictFuneral.SelectedItem.ToString() == district.Key)
                        {
                            foreach (string sc in district.Value)
                            {
                                cmbSCFuneral.Items.Add(sc);
                            }
                        }
                    }
                }
            }
        }

        private void tboxAge_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            double result;
            CultureInfo culture = System.Threading.Thread.CurrentThread.CurrentUICulture;

            if (e.Text != culture.NumberFormat.NumberDecimalSeparator)
            {
                bool success = Double.TryParse(e.Text, System.Globalization.NumberStyles.Any, culture, out result);

                if (!success)
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null && e.OldValue != null && (bool)(e.NewValue) == false && (bool)(e.OldValue) == true)
            {
                CaseViewModel c = this.DataContext as CaseViewModel;
                if (c != null && c.IsEditing)
                {
                    c.CancelEditModeCommand.Execute(null);
                }

                svMain.ScrollToTop();
            }

            SwapTextBoxesAndComboBoxes();
        }

        /// <summary>
        /// Hides combo boxes if the source field in Epi Info 7 is a text field; if the source field is a drop-down list in Epi Info, however, then this makes sure the comboboxes are displayed instead
        /// </summary>
        private void SwapTextBoxesAndComboBoxes()
        {
            EpiDataHelper vm = ((FrameworkElement)this.Parent).DataContext as EpiDataHelper;
            if (vm != null && vm.CaseForm != null)
            {
                #region Country
                if (vm.CaseForm.Fields["CountryRes"] is Epi.Fields.DDLFieldOfLegalValues || vm.CaseForm.Fields["CountryRes"] is Epi.Fields.DDLFieldOfCodes)
                {
                    cmbCountryRes.Visibility = System.Windows.Visibility.Visible;
                    cmbCountryOnset.Visibility = System.Windows.Visibility.Visible;
                    txtCountryRes.Visibility = System.Windows.Visibility.Collapsed;
                    txtCountryOnset.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    cmbCountryRes.Visibility = System.Windows.Visibility.Collapsed;
                    cmbCountryOnset.Visibility = System.Windows.Visibility.Collapsed;
                    txtCountryRes.Visibility = System.Windows.Visibility.Visible;
                    txtCountryOnset.Visibility = System.Windows.Visibility.Visible;
                }
                #endregion // Country

                #region District
                if (vm.CaseForm.Fields["DistrictRes"] is Epi.Fields.DDLFieldOfLegalValues || vm.CaseForm.Fields["DistrictRes"] is Epi.Fields.DDLFieldOfCodes)
                {
                    cmbDistrictRes.Visibility = System.Windows.Visibility.Visible;
                    cmbDistrictRes1.Visibility = System.Windows.Visibility.Visible;
                    cmbDistrictOnset.Visibility = System.Windows.Visibility.Visible;
                    cmbDistrictHosp.Visibility = System.Windows.Visibility.Visible;
                    cmbHospitalDistrict1.Visibility = System.Windows.Visibility.Visible;
                    cmbContactDistrict1.Visibility = System.Windows.Visibility.Visible;
                    cmbContactDistrict2.Visibility = System.Windows.Visibility.Visible;
                    cmbFuneralDistrict1.Visibility = System.Windows.Visibility.Visible;
                    cmbTravelDistrict.Visibility = System.Windows.Visibility.Visible;
                    cmbHospitalDischargeDistrict.Visibility = System.Windows.Visibility.Visible;
                    cmbDistrictFuneral.Visibility = System.Windows.Visibility.Visible;
                    txtDistrictRes.Visibility = System.Windows.Visibility.Collapsed;
                    txtDistrictRes1.Visibility = System.Windows.Visibility.Collapsed;
                    txtDistrictOnset.Visibility = System.Windows.Visibility.Collapsed;
                    txtDistrictHosp.Visibility = System.Windows.Visibility.Collapsed;
                    txtHospitalDistrict1.Visibility = System.Windows.Visibility.Collapsed;
                    txtContactDistrict1.Visibility = System.Windows.Visibility.Collapsed;
                    txtContactDistrict2.Visibility = System.Windows.Visibility.Collapsed;
                    txtFuneralDistrict1.Visibility = System.Windows.Visibility.Collapsed;
                    txtTravelDistrict.Visibility = System.Windows.Visibility.Collapsed;
                    txtHospitalDischargeDistrict.Visibility = System.Windows.Visibility.Collapsed;
                    txtDistrictFuneral.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    cmbDistrictRes.Visibility = System.Windows.Visibility.Collapsed;
                    cmbDistrictRes1.Visibility = System.Windows.Visibility.Collapsed;
                    cmbDistrictOnset.Visibility = System.Windows.Visibility.Collapsed;
                    cmbDistrictHosp.Visibility = System.Windows.Visibility.Collapsed;
                    cmbHospitalDistrict1.Visibility = System.Windows.Visibility.Collapsed;
                    cmbContactDistrict1.Visibility = System.Windows.Visibility.Collapsed;
                    cmbContactDistrict2.Visibility = System.Windows.Visibility.Collapsed;
                    cmbFuneralDistrict1.Visibility = System.Windows.Visibility.Collapsed;
                    cmbTravelDistrict.Visibility = System.Windows.Visibility.Collapsed;
                    cmbHospitalDischargeDistrict.Visibility = System.Windows.Visibility.Collapsed;
                    cmbDistrictFuneral.Visibility = System.Windows.Visibility.Collapsed;
                    txtDistrictRes.Visibility = System.Windows.Visibility.Visible;
                    txtDistrictRes1.Visibility = System.Windows.Visibility.Visible;
                    txtDistrictOnset.Visibility = System.Windows.Visibility.Visible;
                    txtDistrictHosp.Visibility = System.Windows.Visibility.Visible;
                    txtHospitalDistrict1.Visibility = System.Windows.Visibility.Visible;
                    txtContactDistrict1.Visibility = System.Windows.Visibility.Visible;
                    txtContactDistrict2.Visibility = System.Windows.Visibility.Visible;
                    txtFuneralDistrict1.Visibility = System.Windows.Visibility.Visible;
                    txtTravelDistrict.Visibility = System.Windows.Visibility.Visible;
                    txtHospitalDischargeDistrict.Visibility = System.Windows.Visibility.Visible;
                    txtDistrictFuneral.Visibility = System.Windows.Visibility.Visible;
                }
                #endregion // District

                #region Sub-County
                if (vm.CaseForm.Fields["SCRes"] is Epi.Fields.DDLFieldOfLegalValues || vm.CaseForm.Fields["SCRes"] is Epi.Fields.DDLFieldOfCodes)
                {
                    cmbSCRes.Visibility = System.Windows.Visibility.Visible;
                    cmbSCOnset.Visibility = System.Windows.Visibility.Visible;
                    cmbSCFuneral.Visibility = System.Windows.Visibility.Visible;
                    if (cmbSCRes.Items.Count == 0)
                    {
                        SetSCItemSource(cmbSCRes, vm);
                    }
                    if (cmbSCOnset.Items.Count == 0)
                    {
                        SetSCItemSource(cmbSCOnset, vm);
                    }
                    if (cmbSCFuneral.Items.Count == 0)
                    {
                        SetSCItemSource(cmbSCFuneral, vm);
                    }
                    txtSCRes.Visibility = System.Windows.Visibility.Collapsed;
                    txtSCOnset.Visibility = System.Windows.Visibility.Collapsed;
                    txtSCFuneral.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    cmbSCRes.Visibility = System.Windows.Visibility.Collapsed;
                    cmbSCOnset.Visibility = System.Windows.Visibility.Collapsed;
                    cmbSCFuneral.Visibility = System.Windows.Visibility.Collapsed;
                    txtSCRes.Visibility = System.Windows.Visibility.Visible;
                    txtSCOnset.Visibility = System.Windows.Visibility.Visible;
                    txtSCFuneral.Visibility = System.Windows.Visibility.Visible;
                }
                #endregion // Sub-County

                #region Parish
                if (vm.CaseForm.Fields.Contains("ParishRes"))//17178
                {
                    if (vm.CaseForm.Fields["ParishRes"] is Epi.Fields.DDLFieldOfLegalValues || vm.CaseForm.Fields["ParishRes"] is Epi.Fields.DDLFieldOfCodes)
                    {
                        cmbParishRes.Visibility = System.Windows.Visibility.Visible;
                        txtParishRes.Visibility = System.Windows.Visibility.Collapsed;
                    }
                    else
                    {
                        cmbParishRes.Visibility = System.Windows.Visibility.Collapsed;
                        txtParishRes.Visibility = System.Windows.Visibility.Visible;
                    }
                }

                #endregion // Parish

                #region Village
                if (vm.CaseForm.Fields["VillageRes"] is Epi.Fields.DDLFieldOfLegalValues || vm.CaseForm.Fields["VillageRes"] is Epi.Fields.DDLFieldOfCodes)
                {
                    cmbVillageRes.Visibility = System.Windows.Visibility.Visible;
                    cmbVillageOnset.Visibility = System.Windows.Visibility.Visible;
                    cmbContactVillage1.Visibility = System.Windows.Visibility.Visible;
                    cmbContactVillage2.Visibility = System.Windows.Visibility.Visible;
                    cmbVillageFuneral.Visibility = System.Windows.Visibility.Visible;
                    cmbHospitalVillage1.Visibility = System.Windows.Visibility.Visible;
                    txtVillageRes.Visibility = System.Windows.Visibility.Collapsed;
                    txtVillageOnset.Visibility = System.Windows.Visibility.Collapsed;
                    txtContactVillage1.Visibility = System.Windows.Visibility.Collapsed;
                    txtContactVillage2.Visibility = System.Windows.Visibility.Collapsed;
                    txtVillageFuneral.Visibility = System.Windows.Visibility.Collapsed;
                    txtHospitalVillage1.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    cmbVillageRes.Visibility = System.Windows.Visibility.Collapsed;
                    cmbVillageOnset.Visibility = System.Windows.Visibility.Collapsed;
                    cmbContactVillage1.Visibility = System.Windows.Visibility.Collapsed;
                    cmbContactVillage2.Visibility = System.Windows.Visibility.Collapsed;
                    cmbVillageFuneral.Visibility = System.Windows.Visibility.Collapsed;
                    cmbHospitalVillage1.Visibility = System.Windows.Visibility.Collapsed;
                    txtVillageRes.Visibility = System.Windows.Visibility.Visible;
                    txtVillageOnset.Visibility = System.Windows.Visibility.Visible;
                    txtContactVillage1.Visibility = System.Windows.Visibility.Visible;
                    txtContactVillage2.Visibility = System.Windows.Visibility.Visible;
                    txtVillageFuneral.Visibility = System.Windows.Visibility.Visible;
                    txtHospitalVillage1.Visibility = System.Windows.Visibility.Visible;
                }
                #endregion // Village

                #region HCWPosition
                if (vm.CaseForm.Fields["hcwposition"] is Epi.Fields.DDLFieldOfLegalValues || vm.CaseForm.Fields["hcwposition"] is Epi.Fields.DDLFieldOfCodes
                    || vm.CaseForm.Fields["hcwposition"] is Epi.Fields.DDLFieldOfCommentLegal)
                {
                    cmbhcwposition.Visibility = System.Windows.Visibility.Visible;
                    txthcwposition.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    cmbhcwposition.Visibility = System.Windows.Visibility.Collapsed;
                    txthcwposition.Visibility = System.Windows.Visibility.Visible;
                }
                #endregion

                #region HCWFacility
                if (vm.CaseForm.Fields["hcwfacility"] is Epi.Fields.DDLFieldOfLegalValues || vm.CaseForm.Fields["hcwfacility"] is Epi.Fields.DDLFieldOfCodes
                    || vm.CaseForm.Fields["hcwfacility"] is Epi.Fields.DDLFieldOfCommentLegal)
                {
                    cmbhcwfacility.Visibility = System.Windows.Visibility.Visible;
                    txthcwfacility.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    cmbhcwfacility.Visibility = System.Windows.Visibility.Collapsed;
                    txthcwfacility.Visibility = System.Windows.Visibility.Visible;
                }
                #endregion

                #region CurrentHospital
                if (vm.CaseForm.Fields["HospitalCurrent"] is Epi.Fields.DDLFieldOfLegalValues || vm.CaseForm.Fields["HospitalCurrent"] is Epi.Fields.DDLFieldOfCodes
                    || vm.CaseForm.Fields["HospitalCurrent"] is Epi.Fields.DDLFieldOfCommentLegal)
                {
                    cmbCurrentHospital.Visibility = System.Windows.Visibility.Visible;
                    txtCurrentHospital.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    cmbCurrentHospital.Visibility = System.Windows.Visibility.Collapsed;
                    txtCurrentHospital.Visibility = System.Windows.Visibility.Visible;
                }
                #endregion

                #region Hospital Past
                if (vm.CaseForm.Fields["HospitalPast1"] is Epi.Fields.DDLFieldOfLegalValues || vm.CaseForm.Fields["HospitalPast1"] is Epi.Fields.DDLFieldOfCodes
                    || vm.CaseForm.Fields["HospitalPast1"] is Epi.Fields.DDLFieldOfCommentLegal)
                {
                    cmbCurrentHospital2.Visibility = System.Windows.Visibility.Visible;
                    txtCurrentHospital2.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    cmbCurrentHospital2.Visibility = System.Windows.Visibility.Collapsed;
                    txtCurrentHospital2.Visibility = System.Windows.Visibility.Visible;
                }
                #endregion

                #region SymptOtherComment
                if (vm.CaseForm.Fields["SymptOtherComment"] is Epi.Fields.DDLFieldOfLegalValues || vm.CaseForm.Fields["SymptOtherComment"] is Epi.Fields.DDLFieldOfCodes
                    || vm.CaseForm.Fields["SymptOtherComment"] is Epi.Fields.DDLFieldOfCommentLegal)
                {
                    cmbSymptOtherComment.Visibility = System.Windows.Visibility.Visible;
                    txtSymptOtherComment.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    cmbSymptOtherComment.Visibility = System.Windows.Visibility.Collapsed;
                    txtSymptOtherComment.Visibility = System.Windows.Visibility.Visible;
                }
                #endregion

                #region Contact Relation
                if (vm.CaseForm.Fields["ContactRelation1"] is Epi.Fields.DDLFieldOfLegalValues || vm.CaseForm.Fields["ContactRelation1"] is Epi.Fields.DDLFieldOfCodes
                    || vm.CaseForm.Fields["ContactRelation1"] is Epi.Fields.DDLFieldOfCommentLegal)
                {
                    cmbContactRelation1.Visibility = System.Windows.Visibility.Visible;
                    txtContactRelation1.Visibility = System.Windows.Visibility.Collapsed;
                    cmbContactRelation2.Visibility = System.Windows.Visibility.Visible;
                    txtContactRelation2.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    cmbContactRelation1.Visibility = System.Windows.Visibility.Collapsed;
                    txtContactRelation1.Visibility = System.Windows.Visibility.Visible;
                    cmbContactRelation2.Visibility = System.Windows.Visibility.Collapsed;
                    txtContactRelation2.Visibility = System.Windows.Visibility.Visible;
                }
                #endregion

                #region Funeral Relation
                if (vm.CaseForm.Fields["FuneralRelation1"] is Epi.Fields.DDLFieldOfLegalValues || vm.CaseForm.Fields["FuneralRelation1"] is Epi.Fields.DDLFieldOfCodes
                    || vm.CaseForm.Fields["FuneralRelation1"] is Epi.Fields.DDLFieldOfCommentLegal)
                {
                    cmbFuneralRelation1.Visibility = System.Windows.Visibility.Visible;
                    txtFuneralRelation1.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    cmbFuneralRelation1.Visibility = System.Windows.Visibility.Collapsed;
                    txtFuneralRelation1.Visibility = System.Windows.Visibility.Visible;
                }
                #endregion
                #region Interview Position
                if (vm.CaseForm.Fields["InterviewerPosition"] is Epi.Fields.DDLFieldOfLegalValues || vm.CaseForm.Fields["InterviewerPosition"] is Epi.Fields.DDLFieldOfCodes
                    || vm.CaseForm.Fields["InterviewerPosition"] is Epi.Fields.DDLFieldOfCommentLegal)
                {
                    cmbInterviewerPosition.Visibility = System.Windows.Visibility.Visible;
                    txtInterviewerPosition.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    cmbInterviewerPosition.Visibility = System.Windows.Visibility.Collapsed;
                    txtInterviewerPosition.Visibility = System.Windows.Visibility.Visible;
                }
                #endregion

                #region Interview Health Facility
                if (vm.CaseForm.Fields["InterviewerHealthFacility"] is Epi.Fields.DDLFieldOfLegalValues ||
                    vm.CaseForm.Fields["InterviewerHealthFacility"] is Epi.Fields.DDLFieldOfCodes
                    || vm.CaseForm.Fields["InterviewerHealthFacility"] is Epi.Fields.DDLFieldOfCommentLegal)
                {
                    cmbInterviewerHealthFacility.Visibility = System.Windows.Visibility.Visible;
                    txtInterviewerHealthFacility.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    cmbInterviewerHealthFacility.Visibility = System.Windows.Visibility.Collapsed;
                    txtInterviewerHealthFacility.Visibility = System.Windows.Visibility.Visible;
                }
                #endregion

                #region Proxy Relation
                if (vm.CaseForm.Fields["ProxyRelation"] is Epi.Fields.DDLFieldOfLegalValues || vm.CaseForm.Fields["ProxyRelation"] is Epi.Fields.DDLFieldOfCodes
                    || vm.CaseForm.Fields["ProxyRelation"] is Epi.Fields.DDLFieldOfCommentLegal)
                {
                    cmbProxyRelation.Visibility = System.Windows.Visibility.Visible;
                    txtProxyRelation.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    cmbProxyRelation.Visibility = System.Windows.Visibility.Collapsed;
                    txtProxyRelation.Visibility = System.Windows.Visibility.Visible;
                }
                #endregion

                #region Hosiptal Discharge
                if (vm.CaseForm.Fields["HospitalDischarge"] is Epi.Fields.DDLFieldOfLegalValues || vm.CaseForm.Fields["HospitalDischarge"] is Epi.Fields.DDLFieldOfCodes
                    || vm.CaseForm.Fields["HospitalDischarge"] is Epi.Fields.DDLFieldOfCommentLegal)
                {
                    cmbHospitalDischarge.Visibility = System.Windows.Visibility.Visible;
                    txtHospitalDischarge.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    cmbHospitalDischarge.Visibility = System.Windows.Visibility.Collapsed;
                    txtHospitalDischarge.Visibility = System.Windows.Visibility.Visible;
                }
                #endregion

                #region Other Hemo Final Specify
                if (vm.CaseForm.Fields["OtherHemoFinalSpecify"] is Epi.Fields.DDLFieldOfLegalValues || vm.CaseForm.Fields["OtherHemoFinalSpecify"] is Epi.Fields.DDLFieldOfCodes
                    || vm.CaseForm.Fields["OtherHemoFinalSpecify"] is Epi.Fields.DDLFieldOfCommentLegal)
                {
                    cmbOtherHemoFinalSpecify.Visibility = System.Windows.Visibility.Visible;
                    txtOtherHemoFinalSpecify.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    cmbOtherHemoFinalSpecify.Visibility = System.Windows.Visibility.Collapsed;
                    txtOtherHemoFinalSpecify.Visibility = System.Windows.Visibility.Visible;
                }
                #endregion
            }
        }

        private void cmbDistrictRes_LostFocus(object sender, RoutedEventArgs e)
        {
            ComboBox cmbDistrict = (ComboBox)sender;

            if (cmbDistrict.Text.Replace(" ", "").Length == 0)
                return;

            EpiDataHelper vm = ((FrameworkElement)this.Parent).DataContext as EpiDataHelper;
            bool match = false;
            if (vm != null && vm.Districts != null && vm.Districts.Count > 0)
            {
                foreach (string district in vm.Districts)
                {
                    if (district.Equals(cmbDistrict.Text))
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
                cmbDistrict.Text = "";
                MessageBox.Show("Invalid Location", null, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cmbSymptOtherComment_LostFocus(object sender, RoutedEventArgs e)
        {
            ComboBox cmbSymptOtherComment = (ComboBox)sender;

            if (cmbSymptOtherComment.Text.Replace(" ", "").Length == 0)
                return;

            EpiDataHelper vm = ((FrameworkElement)this.Parent).DataContext as EpiDataHelper;
            bool match = false;
            if (vm != null && vm.SymptOtherComments != null && vm.SymptOtherComments.Count > 0)
            {
                foreach (string comment in vm.SymptOtherComments)
                {
                    if (comment.Equals(cmbSymptOtherComment.Text))
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
                cmbSymptOtherComment.Text = "";
                MessageBox.Show("Invalid Symptom", null, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cmbInterviewerPosition_LostFocus(object sender, RoutedEventArgs e)
        {
            ComboBox cmbInterviewerPosition = (ComboBox)sender;

            if (cmbInterviewerPosition.Text.Replace(" ", "").Length == 0)
                return;

            EpiDataHelper vm = ((FrameworkElement)this.Parent).DataContext as EpiDataHelper;
            bool match = false;
            if (vm != null && vm.InterviewerPositions != null && vm.InterviewerPositions.Count > 0)
            {
                foreach (string position in vm.InterviewerPositions)
                {
                    if (position.Equals(cmbInterviewerPosition.Text))
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
                cmbInterviewerPosition.Text = "";
                MessageBox.Show("Invalid Position", null, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cmbInterviewerHealthFacility_LostFocus(object sender, RoutedEventArgs e)
        {
            ComboBox cmbInterviewerHealthFacility = (ComboBox)sender;

            if (cmbInterviewerHealthFacility.Text.Replace(" ", "").Length == 0)
                return;

            EpiDataHelper vm = ((FrameworkElement)this.Parent).DataContext as EpiDataHelper;
            bool match = false;
            if (vm != null && vm.InterviewerHealthFacilities != null && vm.InterviewerHealthFacilities.Count > 0)
            {
                foreach (string facility in vm.InterviewerHealthFacilities)
                {
                    if (facility.Equals(cmbInterviewerHealthFacility.Text))
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
                cmbInterviewerHealthFacility.Text = "";
                MessageBox.Show("Invalid Facility", null, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cmbContactRelation_LostFocus(object sender, RoutedEventArgs e)
        {
            ComboBox cmbContactRelation = (ComboBox)sender;

            if (cmbContactRelation.Text.Replace(" ", "").Length == 0)
                return;

            EpiDataHelper vm = ((FrameworkElement)this.Parent).DataContext as EpiDataHelper;
            bool match = false;
            if (vm != null && vm.ContactRelations != null && vm.ContactRelations.Count > 0)
            {
                foreach (string relation in vm.ContactRelations)
                {
                    if (relation.Equals(cmbContactRelation.Text))
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
                cmbContactRelation.Text = "";
                MessageBox.Show("Invalid Relationship", null, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cmbProxyRelation_LostFocus(object sender, RoutedEventArgs e)
        {
            ComboBox cmbProxyRelation = (ComboBox)sender;

            if (cmbProxyRelation.Text.Replace(" ", "").Length == 0)
                return;

            EpiDataHelper vm = ((FrameworkElement)this.Parent).DataContext as EpiDataHelper;
            bool match = false;
            if (vm != null && vm.ProxyRelations != null && vm.ProxyRelations.Count > 0)
            {
                foreach (string relation in vm.ProxyRelations)
                {
                    if (relation.Equals(cmbProxyRelation.Text))
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
                cmbProxyRelation.Text = "";
                MessageBox.Show("Invalid Relationship", null, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cmbHospitalDischarge_LostFocus(object sender, RoutedEventArgs e)
        {
            ComboBox cmbHospitalDischarge = (ComboBox)sender;

            if (cmbHospitalDischarge.Text.Replace(" ", "").Length == 0)
                return;

            EpiDataHelper vm = ((FrameworkElement)this.Parent).DataContext as EpiDataHelper;
            bool match = false;
            if (vm != null && vm.HospitalDischarges != null && vm.HospitalDischarges.Count > 0)
            {
                foreach (string hospital in vm.HospitalDischarges)
                {
                    if (hospital.Equals(cmbHospitalDischarge.Text))
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
                cmbHospitalDischarge.Text = "";
                MessageBox.Show("Invalid Hospital", null, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cmbCurrentHospital_LostFocus(object sender, RoutedEventArgs e)
        {
            ComboBox cmbHospital = (ComboBox)sender;

            if (cmbHospital.Text.Replace(" ", "").Length == 0)
                return;

            EpiDataHelper vm = ((FrameworkElement)this.Parent).DataContext as EpiDataHelper;
            bool match = false;
            if (vm != null && vm.CurrentHospitals != null && vm.CurrentHospitals.Count > 0)
            {
                foreach (string hospital in vm.CurrentHospitals)
                {
                    if (hospital.Equals(cmbHospital.Text))
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
                cmbHospital.Text = "";
                MessageBox.Show("Invalid Hospital", null, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cmbCurrentHospital2_LostFocus(object sender, RoutedEventArgs e)
        {
            ComboBox cmbHospital = (ComboBox)sender;

            if (cmbHospital.Text.Replace(" ", "").Length == 0)
                return;

            EpiDataHelper vm = ((FrameworkElement)this.Parent).DataContext as EpiDataHelper;
            bool match = false;
            if (vm != null && vm.HospitalsPast != null && vm.HospitalsPast.Count > 0)
            {
                foreach (string hospital in vm.HospitalsPast)
                {
                    if (hospital.Equals(cmbHospital.Text))
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
                cmbHospital.Text = "";
                MessageBox.Show("Invalid Hospital", null, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cmbhcwposition_LostFocus(object sender, RoutedEventArgs e)
        {
            ComboBox cmbhcwposition = (ComboBox)sender;

            if (cmbhcwposition.Text.Replace(" ", "").Length == 0)
                return;

            EpiDataHelper vm = ((FrameworkElement)this.Parent).DataContext as EpiDataHelper;
            bool match = false;
            if (vm != null && vm.HcwPositions != null && vm.HcwPositions.Count > 0)
            {
                foreach (string position in vm.HcwPositions)
                {
                    if (position.Equals(cmbhcwposition.Text))
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
                cmbhcwposition.Text = "";
                MessageBox.Show("Invalid Position", null, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cmbhcwfacility_LostFocus(object sender, RoutedEventArgs e)
        {
            ComboBox cmbhcwfacility = (ComboBox)sender;

            if (cmbhcwfacility.Text.Replace(" ", "").Length == 0)
                return;

            EpiDataHelper vm = ((FrameworkElement)this.Parent).DataContext as EpiDataHelper;
            bool match = false;
            if (vm != null && vm.HcwFacilities != null && vm.HcwFacilities.Count > 0)
            {
                foreach (string facility in vm.HcwFacilities)
                {
                    if (facility.Equals(cmbhcwfacility.Text))
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
                cmbhcwfacility.Text = "";
                MessageBox.Show("Invalid Facility", null, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cmbSCRes_LostFocus(object sender, RoutedEventArgs e)
        {
            if (cmbSCRes.Text.Replace(" ", "").Length == 0)
                return;

            EpiDataHelper vm = ((FrameworkElement)this.Parent).DataContext as EpiDataHelper;

            PopulateDropDown(vm); //VHF-243

            bool match = false;
            ComboBox cmbDistrict = (ComboBox)sender;
            string districtContent = cmbDistrictRes.Text;// issue#17127
            if (vm != null && vm.DistrictsSubCounties != null && vm.DistrictsSubCounties.Count > 0)
            {
                foreach (KeyValuePair<string, List<string>> district in vm.DistrictsSubCounties)
                {
                    if (districtContent.Length > 0) //cmbDistrict.SelectedItem != null VHF-243
                    {
                        if (districtContent == district.Key) //cmbDistrict.SelectedItem.ToString() issue#17127
                        {
                            foreach (string sc in district.Value)
                            {
                                if (sc.Equals(cmbSCRes.Text))
                                {
                                    match = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            else
                match = true;

            if (!match)
            {
                cmbSCRes.Text = "";
                MessageBox.Show("Invalid Location", null, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cmbSCOnset_LostFocus(object sender, RoutedEventArgs e)
        {
            if (cmbSCOnset.Text.Replace(" ", "").Length == 0)
                return;

            EpiDataHelper vm = ((FrameworkElement)this.Parent).DataContext as EpiDataHelper;

            PopulateDropDown(vm); //VHF-243

            bool match = false;
            ComboBox cmbDistrict = (ComboBox)sender;
            string districtContent = cmbDistrictOnset.Text;// issue#17127
            if (vm != null && vm.DistrictsSubCounties != null && vm.DistrictsSubCounties.Count > 0)
            {
                foreach (KeyValuePair<string, List<string>> district in vm.DistrictsSubCounties)
                {
                    if (districtContent.Length > 0) //cmbDistrict.SelectedItem != null VHF-243
                    {
                        if (districtContent == district.Key) //cmbDistrict.SelectedItem.ToString() issue#17127
                        {
                            foreach (string sc in district.Value)
                            {
                                if (sc.Equals(cmbSCOnset.Text))
                                {
                                    match = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            else
                match = true;

            if (!match)
            {
                cmbSCOnset.Text = "";
                MessageBox.Show("Invalid Location", null, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cmbSCFuneral_LostFocus(object sender, RoutedEventArgs e)
        {
            if (cmbSCFuneral.Text.Replace(" ", "").Length == 0)
                return;

            EpiDataHelper vm = ((FrameworkElement)this.Parent).DataContext as EpiDataHelper;

            PopulateDropDown(vm); //VHF-243

            bool match = false;
            ComboBox cmbDistrict = (ComboBox)sender;
            string districtContent = cmbDistrictFuneral.Text;// issue#17127
            if (vm != null && vm.DistrictsSubCounties != null && vm.DistrictsSubCounties.Count > 0)
            {
                foreach (KeyValuePair<string, List<string>> district in vm.DistrictsSubCounties)
                {
                    if (districtContent.Length > 0) //cmbDistrict.SelectedItem != null VHF-243
                    {
                        if (districtContent == district.Key) //cmbDistrict.SelectedItem.ToString() issue#17127
                        {
                            foreach (string sc in district.Value)
                            {
                                if (sc.Equals(cmbSCFuneral.Text))
                                {
                                    match = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            else
                match = true;

            if (!match)
            {
                cmbSCFuneral.Text = "";
                MessageBox.Show("Invalid Location", null, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cmbParishRes_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void cmbVillageRes_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void ComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (((ComboBox)sender).Text.Replace(" ", "").Length == 0)
                return;

            string fieldText = ((ComboBox)sender).Text;
            EpiDataHelper vm = ((FrameworkElement)this.Parent).DataContext as EpiDataHelper;
            bool match = false;
            if (vm != null && vm.Countries != null && vm.Countries.Count > 0)
            {
                foreach (string district in vm.Countries)
                {
                    if (district.Equals(((ComboBox)sender).Text))
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
                ((ComboBox)sender).Text = "";
                MessageBox.Show("Invalid Country", null, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        //VHF-256 - Turns IsShortFormOpened flag off.
        private void btnSaveAndClose_Click(object sender, RoutedEventArgs e)
        {
            EpiDataHelper vm = ((FrameworkElement)this.Parent).DataContext as EpiDataHelper;
            vm.IsShortFormOpened = false;
        }

        private void txtID_TextChanged(object sender, TextChangedEventArgs e)
        {
            bool idIsAlreadyUsed = false;

            EpiDataHelper vm = ((FrameworkElement)this.Parent).DataContext as EpiDataHelper;

            foreach (CaseViewModel foreachCase in vm.CaseCollection)
            {
                if (foreachCase.ID.Equals(txtID.Text, StringComparison.OrdinalIgnoreCase))
                {
                    if (foreachCase != this.DataContext as CaseViewModel)
                    {
                        idIsAlreadyUsed = true;
                        break;
                    }
                }
            }

            if (idIsAlreadyUsed)
            {
                txtID.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                txtID.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                ToolTip tip = new ToolTip();

                //TextBlock tbHeader = new TextBlock();
                //tbHeader.Text = "Duplicate ID";
                //tbHeader.FontSize = 14;
                //tbHeader.FontWeight = FontWeights.SemiBold;
                //tbHeader.TextWrapping = TextWrapping.WrapWithOverflow;
                //tbHeader.Margin = new Thickness(0, 0, 0, 4);

                TextBlock tb = new TextBlock();
                tb.Text = Properties.Resources.ErrorDuplicateIDs;
                tb.TextWrapping = TextWrapping.WrapWithOverflow;

                StackPanel sp = new StackPanel();
                sp.Width = 200;
                //sp.Children.Add(tbHeader);
                sp.Children.Add(tb);

                tip.Content = sp;
                txtID.ToolTip = tip;
            }
            else
            {
                txtID.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
                txtID.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                txtID.ToolTip = null;
            }
        }
    }
}
