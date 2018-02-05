using System;
using System.Collections.Generic;
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
using ContactTracing.ViewModel;

namespace ContactTracing.CaseView.Controls.Printing
{
    /// <summary>
    /// Interaction logic for CaseReportFormPage3.xaml
    /// </summary>
    public partial class CaseReportFormPage3 : UserControl
    {
        public CaseReportFormPage3()
        {
            InitializeComponent();
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            CaseViewModel caseVM = (this.DataContext as CaseViewModel);

            if (caseVM != null)
            {
                if (caseVM.FinalStatus.Equals(Core.Enums.AliveDead.Alive))
                {
                    checkboxFinalStatusDead.IsChecked = false;
                    checkboxFinalStatusAlive.IsChecked = true;
                }
                else if (caseVM.FinalStatus.Equals(Core.Enums.AliveDead.Dead))
                {
                    checkboxFinalStatusDead.IsChecked = true;
                    checkboxFinalStatusAlive.IsChecked = false;
                }
                else
                {
                    checkboxFinalStatusDead.IsChecked = false;
                    checkboxFinalStatusAlive.IsChecked = false;
                }

                switch (caseVM.BleedUnexplainedEver)
                {
                    case "1":
                    case "Yes":
                        checkboxUnexBleedYes.IsChecked = true;
                        checkboxUnexBleedNo.IsChecked = false;
                        checkboxUnexBleedUnk.IsChecked = false;
                        break;
                    case "2":
                    case "No":
                        checkboxUnexBleedYes.IsChecked = false;
                        checkboxUnexBleedNo.IsChecked = true;
                        checkboxUnexBleedUnk.IsChecked = false;
                        break;
                    case "3":
                    case "Unk":
                        checkboxUnexBleedYes.IsChecked = false;
                        checkboxUnexBleedNo.IsChecked = false;
                        checkboxUnexBleedUnk.IsChecked = true;
                        break;
                    default:
                        checkboxUnexBleedYes.IsChecked = false;
                        checkboxUnexBleedNo.IsChecked = false;
                        checkboxUnexBleedUnk.IsChecked = false;
                        break;
                }

                switch (caseVM.PlaceDeath)
                {
                    case "1":
                        checkboxDeathCommunity.IsChecked = true;
                        break;
                    case "2":
                        checkboxDeathHospital.IsChecked = true;
                        break;
                    case "3":
                        checkboxDeathOther.IsChecked = true;
                        break;
                }

                txtUnexBleedSpecify.Text = caseVM.SpecifyBleeding;
                txtHospitalDischargedName.Text = caseVM.HospitalDischarge;
                txtHospitalDischargedDistrict.Text = caseVM.HospitalDischargeDistrict;

                txtPlaceDeathVillage.Text = caseVM.VillageDeath;
                txtPlaceDeathSC.Text = caseVM.SubCountyDeath;
                txtPlaceDeathDistrict.Text = caseVM.DistrictDeath;

                txtPlaceFuneralVillage.Text = caseVM.VillageFuneral;
                txtPlaceFuneralSC.Text = caseVM.SubCountyFuneral;
                txtPlaceFuneralDistrict.Text = caseVM.DistrictFuneral;

                dateInfoCompleted.DataContext = caseVM.DateOutcomeInfoCompleted;
                dateDischargeIso.DataContext = caseVM.DateDischargeIso;
                dateDischargeHospital.DataContext = caseVM.DateDischargeHospital;
                dateDeath.DataContext = caseVM.DateDeath2;
                dateFuneral.DataContext = caseVM.DistrictFuneral;

                ynuAbdPainFinal.DataContext = caseVM.SymptomAbdPainFinal;
                ynuAnorexiaFinal.DataContext = caseVM.SymptomAnorexiaFinal;
                ynuChestPainFinal.DataContext = caseVM.SymptomChestPainFinal;
                ynuConfusedFinal.DataContext = caseVM.SymptomConfusedFinal;
                ynuConjunctivitisFinal.DataContext = caseVM.SymptomConjunctivitisFinal;
                ynuCoughFinal.DataContext = caseVM.SymptomCoughFinal;
                ynuDiarrheaFinal.DataContext = caseVM.SymptomDiarrheaFinal;
                ynuDiffBreatheFinal.DataContext = caseVM.SymptomDiffBreatheFinal;
                ynuDiffSwallowFinal.DataContext = caseVM.SymptomDiffSwallowFinal;
                ynuFatigueFinal.DataContext = caseVM.SymptomFatigueFinal;
                ynuFeverFinal.DataContext = caseVM.SymptomFeverFinal;
                ynuHeadacheFinal.DataContext = caseVM.SymptomHeadacheFinal;
                ynuHiccupsFinal.DataContext = caseVM.SymptomHiccupsFinal;
                ynuJaundiceFinal.DataContext = caseVM.SymptomJaundiceFinal;
                ynuJointPainFinal.DataContext = caseVM.SymptomJointPainFinal;
                ynuMusclePainFinal.DataContext = caseVM.SymptomMusclePainFinal;
                ynuPainEyesFinal.DataContext = caseVM.SymptomPainEyesFinal;
                ynuRashFinal.DataContext = caseVM.SymptomRashFinal;
                ynuSoreThroatFinal.DataContext = caseVM.SymptomSoreThroatFinal;
                ynuUnconsciousFinal.DataContext = caseVM.SymptomUnconsciousFinal;
                ynuVomitingFinal.DataContext = caseVM.SymptomVomitingFinal;

                ynuOtherHemorrhagicFinal.DataContext = caseVM.SymptomOtherHemoFinal;

                txtOtherHemorrhagicFinalSpecify.Text = caseVM.SymptomOtherHemoFinalSpecify;

                if (caseVM.SymptomFeverTemp.HasValue) txtFeverTempFinal.Text = caseVM.SymptomFeverTemp.Value.ToString("F1");

                switch (caseVM.SymptomFeverTempSource)
                {
                    case "1":
                        txtFeverTempSourceFinal.Text = "Axillary";
                        break;
                    case "2":
                        txtFeverTempSourceFinal.Text = "Oral";
                        break;
                }

                txtCaseID.Text = caseVM.ID;
                txtCaseName.Text = caseVM.OtherNames + " " + caseVM.Surname;
            }
        }
    }
}
