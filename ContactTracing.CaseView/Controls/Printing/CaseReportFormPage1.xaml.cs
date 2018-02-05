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
using ContactTracing.Core;
using ContactTracing.ViewModel;

namespace ContactTracing.CaseView.Controls.Printing
{
    /// <summary>
    /// Interaction logic for CaseReportFormPage1.xaml
    /// </summary>
    public partial class CaseReportFormPage1 : UserControl
    {
        public CaseReportFormPage1()
        {
            InitializeComponent();
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            CaseViewModel caseVM = (this.DataContext as CaseViewModel);

            if (caseVM != null)
            {

                txtCaseID.Text = caseVM.ID;
                txtCaseID2.Text = caseVM.OriginalID;

                txtSurname.Text = caseVM.Surname;
                txtOtherNames.Text = caseVM.OtherNames;
                txtAge.Text = caseVM.Age.HasValue ? caseVM.Age.Value.ToString() : String.Empty;

                if (caseVM.AgeUnit == AgeUnits.Years)
                {
                    checkboxYears.IsChecked = true;
                }
                if (caseVM.AgeUnit == AgeUnits.Months)
                {
                    checkboxMonths.IsChecked = true;
                }

                if (caseVM.Gender == Core.Enums.Gender.Male)
                {
                    checkboxMale.IsChecked = true;
                }
                if (caseVM.Gender == Core.Enums.Gender.Female)
                {
                    checkboxFemale.IsChecked = true;
                }

                txtPhone.Text = caseVM.PhoneNumber;
                txtPhoneOwner.Text = caseVM.PhoneOwner;

                if (caseVM.StatusReport == "2")
                {
                    checkboxAlive.IsChecked = true;
                }
                if (caseVM.StatusReport == "1")
                {
                    checkboxDead.IsChecked = true;
                }

                dateDeath.DataContext = caseVM.DateDeath;

                txtHeadHousehold.Text = caseVM.HeadOfHousehold;
                txtVillageRes.Text = caseVM.Village;
                txtDistrictRes.Text = caseVM.District;
                txtCountryRes.Text = caseVM.Country;
                txtSCRes.Text = caseVM.SubCounty;
                txtParishRes.Text = caseVM.Parish;

                checkboxChild.IsChecked = caseVM.OccupationChild;
                checkboxBusinessman.IsChecked = caseVM.OccupationBusinessman;
                checkboxHousewife.IsChecked = caseVM.OccupationHousewife;
                checkboxFarmer.IsChecked = caseVM.OccupationFarmer;
                checkboxButcher.IsChecked = caseVM.OccupationButcher;
                checkboxHunter.IsChecked = caseVM.OccupationHunter;
                checkboxMiner.IsChecked = caseVM.OccupationMiner;
                checkboxReligious.IsChecked = caseVM.OccupationReligious;
                checkboxStudent.IsChecked = caseVM.OccupationStudent;
                checkboxTradHealer.IsChecked = caseVM.OccupationTraditionalHealer;
                checkboxTransporter.IsChecked = caseVM.OccupationTransporter;
                checkboxOther.IsChecked = caseVM.OccupationOther;

                checkboxHCW.IsChecked = caseVM.IsHCW;
                txtHCWFacility.Text = caseVM.OccupationHCWFacility;
                txtHCWPosition.Text = caseVM.OccupationHCWPosition;
                txtOtherOccupationSpecify.Text = caseVM.OccupationOtherSpecify;
                txtTransportType.Text = caseVM.OccupationTransporterSpecify;
                txtBusinessType.Text = caseVM.OccupationBusinessSpecify;

                txtVillageOnset.Text = caseVM.VillageOnset;
                txtDistrictOnset.Text = caseVM.DistrictOnset;
                txtSCOnset.Text = caseVM.SubCountyOnset;

                txtLat.Text = caseVM.Latitude.HasValue ? caseVM.Latitude.Value.ToString() : String.Empty;
                txtLong.Text = caseVM.Longitude.HasValue ? caseVM.Longitude.Value.ToString() : String.Empty;

                dateRes1.DataContext = caseVM.DateOnsetLocalStart;
                dateRes2.DataContext = caseVM.DateOnsetLocalEnd;

                dateInitialSymptomOnset.DataContext = caseVM.DateOnset;

                ynuAbdPainFinal.DataContext = caseVM.SymptomAbdPain;
                ynuAnorexiaFinal.DataContext = caseVM.SymptomAnorexia;
                ynuChestPainFinal.DataContext = caseVM.SymptomChestPain;
                ynuConfusedFinal.DataContext = caseVM.SymptomConfused;
                ynuConjunctivitisFinal.DataContext = caseVM.SymptomConjunctivitis;
                ynuCoughFinal.DataContext = caseVM.SymptomCough;
                ynuDiarrheaFinal.DataContext = caseVM.SymptomDiarrhea;
                ynuDiffBreatheFinal.DataContext = caseVM.SymptomDiffBreathe;
                ynuDiffSwallowFinal.DataContext = caseVM.SymptomDiffSwallow;
                ynuFatigueFinal.DataContext = caseVM.SymptomFatigue;
                ynuFeverFinal.DataContext = caseVM.SymptomFever;
                ynuHeadacheFinal.DataContext = caseVM.SymptomHeadache;
                ynuHiccupsFinal.DataContext = caseVM.SymptomHiccups;
                ynuJaundiceFinal.DataContext = caseVM.SymptomJaundice;
                ynuJointPainFinal.DataContext = caseVM.SymptomJointPain;
                ynuMusclePainFinal.DataContext = caseVM.SymptomMusclePain;
                ynuPainEyesFinal.DataContext = caseVM.SymptomPainEyes;
                ynuRashFinal.DataContext = caseVM.SymptomRash;
                ynuSoreThroatFinal.DataContext = caseVM.SymptomSoreThroat;
                ynuUnconsciousFinal.DataContext = caseVM.SymptomUnconscious;
                ynuVomitingFinal.DataContext = caseVM.SymptomVomiting;

                ynuBleedUnexplained.DataContext = caseVM.SymptomUnexplainedBleeding;
                ynuBleedGums.DataContext = caseVM.SymptomBleedGums;
                ynuBleedInjectionSite.DataContext = caseVM.SymptomBleedInjectionSite;
                ynuNoseBleed.DataContext = caseVM.SymptomNoseBleed;
                ynuBloodyStool.DataContext = caseVM.SymptomBloodyStool;
                ynuBloodVomit.DataContext = caseVM.SymptomHematemesis;
                ynuDigestedBlood.DataContext = caseVM.SymptomBloodVomit;
                ynuCoughBlood.DataContext = caseVM.SymptomCoughBlood;
                ynuBleedVagina.DataContext = caseVM.SymptomBleedVagina;
                ynuBruising.DataContext = caseVM.SymptomBleedSkin;
                ynuBloodInUrine.DataContext = caseVM.SymptomBleedUrine;
                ynuOtherHemorrhagic.DataContext = caseVM.SymptomOtherHemo;
                ynuOtherNonHemorrhagic.DataContext = caseVM.SymptomOtherNonHemorrhagic;

                switch (caseVM.HospitalizedCurrent)
                {
                    case "1":
                        checkboxHospCurrentYes.IsChecked = true;
                        break;
                    case "2":
                        checkboxHospCurrentNo.IsChecked = true;
                        break;
                }

                switch (caseVM.HospitalizedPast)
                {
                    case "1":
                        checkboxPrevHospYes.IsChecked = true;
                        break;
                    case "2":
                        checkboxPrevHospNo.IsChecked = true;
                        break;
                    default:
                        checkboxPrevHospUnk.IsChecked = true;
                        break;
                }

                dateHospitalAdmitted.DataContext = caseVM.DateHospitalCurrentAdmit;
                txtSpecifyHospitalAdmitted.Text = caseVM.CurrentHospital;

                txtVillageHosp.Text = caseVM.VillageHosp;
                txtDistrictHosp.Text = caseVM.DistrictHosp;
                txtSCHosp.Text = caseVM.SubCountyHosp;
                txtCountryHosp.Text = caseVM.CountryHosp;

                switch (caseVM.IsolationCurrent)
                {
                    case "1":
                        checkboxCurrentlyInIsoYes.IsChecked = true;
                        break;
                    case "2":
                        checkboxCurrentlyInIsoNo.IsChecked = true;
                        break;
                }

                dateIsolated.DataContext = caseVM.DateIsolationCurrent;

                dateHospFrom1.DataContext = caseVM.DateHospitalPastStart1;
                dateHospFrom2.DataContext = caseVM.DateHospitalPastStart2;
                dateHospTo1.DataContext = caseVM.DateHospitalPastEnd1;
                dateHospTo2.DataContext = caseVM.DateHospitalPastEnd2;

                tblockHealthFacilityName1.Text = caseVM.HospitalPast1;
                tblockHealthFacilityName2.Text = caseVM.HospitalPast2;

                tblockHospVillage1.Text = caseVM.HospitalVillage1;
                tblockHospVillage2.Text = caseVM.HospitalVillage2;

                tblockHospDistrict1.Text = caseVM.HospitalDistrict1;
                tblockHospDistrict2.Text = caseVM.HospitalDistrict2;

                switch (caseVM.IsolationPast1)
                {
                    case "1":
                        checkboxHospIsoYes1.IsChecked = true;
                        break;
                    case "2":
                        checkboxHospIsoNo1.IsChecked = true;
                        break;
                }

                switch (caseVM.IsolationPast2)
                {
                    case "1":
                        checkboxHospIsoYes2.IsChecked = true;
                        break;
                    case "2":
                        checkboxHospIsoNo2.IsChecked = true;
                        break;
                }
            }
        }
    }
}
