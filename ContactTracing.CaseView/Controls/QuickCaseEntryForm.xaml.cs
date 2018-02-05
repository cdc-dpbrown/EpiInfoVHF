using System;
using System.Collections.Generic;
using System.Data;
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
using Epi.Data;
using Epi.Fields;

namespace ContactTracing.CaseView.Controls
{
    /// <summary>
    /// Interaction logic for QuickCaseEntryForm.xaml
    /// </summary>
    public partial class QuickCaseEntryForm : UserControl
    {
        public event EventHandler Closed;

        private CaseViewModel CaseVM { get; set; }

        public QuickCaseEntryForm(CaseViewModel caseVM)
        {
            InitializeComponent();
            CaseVM = caseVM;
        }

        private void LoadDropDownLists()
        {
            IDbDriver db = DataHelper.Project.CollectedData.GetDatabase();
            Query selectQuery = db.CreateQuery("SELECT [epidemiologycasedefinition] FROM [codeepicasedef1]");
            DataTable dt = db.Select(selectQuery);

            cmbEpiCaseClass.Items.Add(String.Empty);
            foreach(DataRow row in dt.Rows) 
            {
                cmbEpiCaseClass.Items.Add(row[0].ToString());
            }

            selectQuery = db.CreateQuery("SELECT [finallabclass] FROM [codefinallabclass1]");
            dt = db.Select(selectQuery);

            cmbFinalLabClass.Items.Add(String.Empty);
            foreach(DataRow row in dt.Rows) 
            {
                cmbFinalLabClass.Items.Add(row[0].ToString());
            }



            cmbFeverTempSource.Items.Add(String.Empty);
            cmbFeverTempSource.Items.Add(Properties.Resources.FeverTempSourceAxillary);
            cmbFeverTempSource.Items.Add(Properties.Resources.FeverTempSourceOral);

            cmbFeverTempSourceFinal.Items.Add(String.Empty);
            cmbFeverTempSourceFinal.Items.Add(Properties.Resources.FeverTempSourceAxillary);
            cmbFeverTempSourceFinal.Items.Add(Properties.Resources.FeverTempSourceOral);
        }

        private void LoadCaseData()
        {
            CaseViewModel c = CaseVM;

            if (String.IsNullOrEmpty(c.EpiCaseClassification)) 
            {
                cmbEpiCaseClass.SelectedIndex = 0;
            }
            else if (c.EpiCaseDef == Core.Enums.EpiCaseClassification.NotCase)
            {
                cmbEpiCaseClass.SelectedIndex = 1;
            }
            else if (c.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed)
            {
                cmbEpiCaseClass.SelectedIndex = 2;
            }
            else if (c.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable)
            {
                cmbEpiCaseClass.SelectedIndex = 3;
            }
            else if (c.EpiCaseDef == Core.Enums.EpiCaseClassification.Suspect)
            {
                cmbEpiCaseClass.SelectedIndex = 4;
            }
            else if (c.EpiCaseDef == Core.Enums.EpiCaseClassification.Excluded)
            {
                cmbEpiCaseClass.SelectedIndex = 5;
            }
            else
            {
                MessageBox.Show("Incorrect language detected. Closing.");
                if (this.Closed != null)
                {
                    Closed(this, new EventArgs());
                }
                return;
            }

            switch (c.SymptomFeverTempSource)
            {
                case "1":
                    cmbFeverTempSource.SelectedIndex = 1;
                    break;
                case "2":
                    cmbFeverTempSource.SelectedIndex = 2;
                    break;
                default:
                    cmbFeverTempSource.SelectedIndex = 0;
                    break;
            }

            switch (c.SymptomFeverTempSourceFinal)
            {
                case "1":
                    cmbFeverTempSourceFinal.SelectedIndex = 1;
                    break;
                case "2":
                    cmbFeverTempSourceFinal.SelectedIndex = 2;
                    break;
                default:
                    cmbFeverTempSourceFinal.SelectedIndex = 0;
                    break;
            }

            if (c.FinalLabClass == Core.Enums.FinalLabClassification.None)
            {
                cmbFinalLabClass.SelectedIndex = 0;
            }
            else if (c.FinalLabClass == Core.Enums.FinalLabClassification.NotCase)
            {
                cmbFinalLabClass.SelectedIndex = 1;
            }
            else if (c.FinalLabClass == Core.Enums.FinalLabClassification.ConfirmedAcute)
            {
                cmbFinalLabClass.SelectedIndex = 2;
            }
            else if (c.FinalLabClass == Core.Enums.FinalLabClassification.ConfirmedConvalescent)
            {
                cmbFinalLabClass.SelectedIndex = 3;
            }
            else if (c.FinalLabClass == Core.Enums.FinalLabClassification.Indeterminate)
            {
                cmbFinalLabClass.SelectedIndex = 4;
            }
            else if (c.FinalLabClass == Core.Enums.FinalLabClassification.NeedsFollowUpSample)
            {
                cmbFinalLabClass.SelectedIndex = 5;
            }
            else
            {
                MessageBox.Show("Incorrect language detected. Closing.");
                if (this.Closed != null)
                {
                    Closed(this, new EventArgs());
                }
                return;
            }

            switch(c.AgeUnit)
            {
                case AgeUnits.Months:
                    checkboxMonths.IsChecked = true;
                    break;
                case AgeUnits.Years:
                    checkboxYears.IsChecked = true;
                    break;
            }

            if (c.Gender == Core.Enums.Gender.Male) checkboxMale.IsChecked = true;
            if (c.Gender == Core.Enums.Gender.Female) checkboxFemale.IsChecked = true;

            //txtPhone.Text = c.PhoneNumber;
            //txtPhoneOwner.Text = c.PhoneOwner;

            switch (c.StatusReport)
            {
                case "1":
                    checkboxDead.IsChecked = true;
                    break;
                case "2":
                    checkboxAlive.IsChecked = true;
                    break;
            }

            switch (c.HospitalizedCurrent)
            {
                case "1":
                    checkboxHospCurrentYes.IsChecked = true;
                    break;
                case "2":
                    checkboxHospCurrentNo.IsChecked = true;
                    break;
            }

            switch (c.IsolationCurrent)
            {
                case "1":
                    checkboxCurrentlyInIsoYes.IsChecked = true;
                    break;
                case "2":
                    checkboxCurrentlyInIsoNo.IsChecked = true;
                    break;
            }

            switch (c.HospitalizedPast)
            {
                case "1":
                    checkboxPrevHospYes.IsChecked = true;
                    break;
                case "2":
                    checkboxPrevHospNo.IsChecked = true;
                    break;
                case "3":
                    checkboxPrevHospUnk.IsChecked = true;
                    break;
            }

            switch (c.IsolationPast1)
            {
                case "1":
                    checkboxHospIsoYes1.IsChecked = true;
                    break;
                case "2":
                    checkboxHospIsoNo1.IsChecked = true;
                    break;
            }

            switch (c.IsolationPast2)
            {
                case "1":
                    checkboxHospIsoYes2.IsChecked = true;
                    break;
                case "2":
                    checkboxHospIsoNo2.IsChecked = true;
                    break;
            }

            switch (c.HadContact)
            {
                case "1":
                    checkboxKnownContactYes.IsChecked = true;
                    break;
                case "2":
                    checkboxKnownContactNo.IsChecked = true;
                    break;
                case "3":
                    checkboxKnownContactUnk.IsChecked = true;
                    break;
            }

            switch (c.ContactStatus1)
            {
                case "1":
                    checkboxContactAlive1.IsChecked = false;
                    checkboxContactDead1.IsChecked = true;
                    break;
                case "2":
                    checkboxContactAlive1.IsChecked = true;
                    checkboxContactDead1.IsChecked = false;
                    break;
                case "3":
                    checkboxContactAlive1.IsChecked = true;
                    checkboxContactDead1.IsChecked = true;
                    break;
            }

            switch (c.ContactStatus2)
            {
                case "1":
                    checkboxContactAlive2.IsChecked = false;
                    checkboxContactDead2.IsChecked = true;
                    break;
                case "2":
                    checkboxContactAlive2.IsChecked = true;
                    checkboxContactDead2.IsChecked = false;
                    break;
                case "3":
                    checkboxContactAlive2.IsChecked = true;
                    checkboxContactDead2.IsChecked = true;
                    break;
            }

            switch (c.ContactStatus3)
            {
                case "1":
                    checkboxContactAlive3.IsChecked = false;
                    checkboxContactDead3.IsChecked = true;
                    break;
                case "2":
                    checkboxContactAlive3.IsChecked = true;
                    checkboxContactDead3.IsChecked = false;
                    break;
                case "3":
                    checkboxContactAlive3.IsChecked = true;
                    checkboxContactDead3.IsChecked = true;
                    break;
            }

            if (c.TypesOfContact1.Contains("1")) checkboxContact1Type1.IsChecked = true;
            if (c.TypesOfContact1.Contains("2")) checkboxContact1Type2.IsChecked = true;
            if (c.TypesOfContact1.Contains("3")) checkboxContact1Type3.IsChecked = true;
            if (c.TypesOfContact1.Contains("4")) checkboxContact1Type4.IsChecked = true;

            if (c.TypesOfContact2.Contains("1")) checkboxContact2Type1.IsChecked = true;
            if (c.TypesOfContact2.Contains("2")) checkboxContact2Type2.IsChecked = true;
            if (c.TypesOfContact2.Contains("3")) checkboxContact2Type3.IsChecked = true;
            if (c.TypesOfContact2.Contains("4")) checkboxContact2Type4.IsChecked = true;

            if (c.TypesOfContact3.Contains("1")) checkboxContact3Type1.IsChecked = true;
            if (c.TypesOfContact3.Contains("2")) checkboxContact3Type2.IsChecked = true;
            if (c.TypesOfContact3.Contains("3")) checkboxContact3Type3.IsChecked = true;
            if (c.TypesOfContact3.Contains("4")) checkboxContact3Type4.IsChecked = true;

            switch (c.AttendFuneral)
            {
                case "1":
                    checkboxAttendFuneralYes.IsChecked = true;
                    break;
                case "2":
                    checkboxAttendFuneralNo.IsChecked = true;
                    break;
                case "3":
                    checkboxAttendFuneralUnk.IsChecked = true;
                    break;
            }

            switch (c.Travel)
            {
                case "1":
                    checkboxTravelYes.IsChecked = true;
                    break;
                case "2":
                    checkboxTravelNo.IsChecked = true;
                    break;
                case "3":
                    checkboxTravelUnk.IsChecked = true;
                    break;
            }

            switch (c.HospitalBeforeIll)
            {
                case "1":
                    checkboxHospBeforeIllYes.IsChecked = true;
                    break;
                case "2":
                    checkboxHospBeforeIllNo.IsChecked = true;
                    break;
                case "3":
                    checkboxHospBeforeIllUnk.IsChecked = true;
                    break;
            }

            switch (c.TraditionalHealer)
            {
                case "1":
                    checkboxTradHealerYes.IsChecked = true;
                    break;
                case "2":
                    checkboxTradHealerNo.IsChecked = true;
                    break;
                case "3":
                    checkboxTradHealerUnk.IsChecked = true;
                    break;
            }

            switch (c.Animals)
            {
                case "1":
                    checkboxAnimalsYes.IsChecked = true;
                    break;
                case "2":
                    checkboxAnimalsNo.IsChecked = true;
                    break;
                case "3":
                    checkboxAnimalsUnk.IsChecked = true;
                    break;
            }

            switch (c.BittenTick)
            {
                case "1":
                    checkboxBittenTickYes.IsChecked = true;
                    break;
                case "2":
                    checkboxBittenTickNo.IsChecked = true;
                    break;
                case "3":
                    checkboxBittenTickUnk.IsChecked = true;
                    break;
            }

            //if (c.FinalStatus == Core.Enums.AliveDead.Alive) { checkboxFinalStatusAlive.IsChecked = true; }
            //else if (c.FinalStatus == Core.Enums.AliveDead.Dead) { checkboxFinalStatusDead.IsChecked = true; }
            //else if (c.FinalStatus == String.Empty) { checkboxFinalStatusAlive.IsChecked = false; checkboxFinalStatusDead.IsChecked = false; }

            switch (c.BleedUnexplainedEver)
            {
                case "1":
                    checkboxUnexBleedYes.IsChecked = true;
                    break;
                case "2":
                    checkboxUnexBleedNo.IsChecked = true;
                    break;
                case "3":
                    checkboxUnexBleedUnk.IsChecked = true;
                    break;
            }

            switch (c.PlaceDeath)
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
            //txtContactNameSoureCase1.Text = c.ContactName1;
            //txtContactNameSoureCase2.Text = c.ContactName3;
            //txtContactNameSoureCase3.Text = c.ContactName3;

            //txtContactRelationToPatient1.Text = c.ContactRelation1;
            //txtContactRelationToPatient2.Text = c.ContactRelation2;
            //txtContactRelationToPatient3.Text = c.ContactRelation3;

            //txtContactVillage1.Text = c.ContactRelation1;
            //txtContactVillage2.Text = c.ContactRelation2;
            //txtContactVillage3.Text = c.ContactRelation3;

            //txtContactDistrict1.Text = c.ContactDistrict1;
            //txtContactDistrict2.Text = c.ContactDistrict2;
            //txtContactDistrict3.Text = c.ContactDistrict3;

            //dateDeath.SelectedDate = c.DateDeath;
            //if (c.DateDeath.HasValue) dateDeath.DisplayDate = c.DateDeath.Value;

            //txtHeadHousehold.Text = c.HeadOfHousehold;
            //cmbVillageRes.Text = c.Village;
            //cmbParishRes.Text = c.Parish;
            //cmbDistrictRes.Text = c.District;
            //cmbSCRes.Text = c.SubCounty;
            //cmbCountryRes.Text = c.Country;

            //checkboxFarmer.IsChecked = c.OccupationFarmer;
            //checkboxButcher.IsChecked = c.OccupationButcher;
            //checkboxHunter.IsChecked = c.OccupationHunter;
            //checkboxMiner.IsChecked = c.OccupationMiner;
            //checkboxReligious.IsChecked = c.OccupationReligious;
            //checkboxHousewife.IsChecked = c.OccupationHousewife;
            //checkboxStudent.IsChecked = c.OccupationStudent;
            //checkboxChild.IsChecked = c.OccupationChild;
            //checkboxBusinessman.IsChecked = c.OccupationBusinessman;
            //checkboxTransporter.IsChecked = c.OccupationTransporter;
            //checkboxHCW.IsChecked = c.IsHCW;
            //checkboxTradHealer.IsChecked = c.OccupationTraditionalHealer;
            //checkboxOther.IsChecked = c.OccupationOther;

            //txtOtherOccupationSpecify.Text = c.OccupationOtherSpecify;
            //txtBusinessType.Text = c.OccupationBusinessSpecify;
            //txtTransportType.Text = c.OccupationTransporterSpecify;

            //cmbVillageOnset.Text = c.VillageOnset;
            ////cmbParishOnset.Text = c.Parish;
            //cmbDistrictOnset.Text = c.DistrictOnset;
            //cmbSCOnset.Text = c.SubCountyOnset;
            //cmbCountryOnset.Text = c.CountryOnset;

            //if (c.Latitude.HasValue) txtLat.Text = c.Latitude.ToString();
            //if (c.Longitude.HasValue) txtLong.Text = c.Longitude.ToString();

            //dateRes1.SelectedDate = c.DateOnsetLocalStart;
            //dateRes2.SelectedDate = c.DateOnsetLocalEnd;

            //foreach (string s in cmbEpiCaseClass.Items)
            //{
            //    int position = s.IndexOf('-');
            //    if (position >= 0)
            //    {
            //        string label = s.Substring(position + 1, s.Length - position - 1).ToLower();
            //        if (c.EpiCaseDef.ToLower().Equals(label))
            //        {
            //            cmbEpiCaseClass.SelectedItem = s;
            //            break;
            //        }
            //    }
            //}
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DataHelper.ReloadAllCaseData(CaseVM);
            if (this.Closed != null)
            {
                Closed(this, new EventArgs());
            }
        }

        private void checkboxMale_Checked(object sender, RoutedEventArgs e)
        {
            checkboxFemale.IsChecked = false;
        }

        private void checkboxFemale_Checked(object sender, RoutedEventArgs e)
        {
            checkboxMale.IsChecked = false;
        }

        private void cmbDistrictRes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (KeyValuePair<string, List<string>> district in DataHelper.DistrictsSubCounties)
            {
                if (cmbDistrictRes.SelectedItem.ToString() == district.Key)
                {
                    foreach (string sc in district.Value)
                    {
                        cmbSCRes.Items.Add(sc);
                    }
                }
            }
        }

        public EpiDataHelper DataHelper
        {
            get { return this.DataContext as EpiDataHelper; }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();

            //if (cmbEpiCaseClass.SelectedIndex == -1)
            //{
            //    MessageBox.Show("Epi case classification must not be empty.", "Required field", MessageBoxButton.OK, MessageBoxImage.Information);
            //    cmbEpiCaseClass.Focus();
            //    return;
            //}

            //CaseViewModel c = CaseVM;

            //string ageUnit = String.Empty;

            //if (checkboxMonths.IsChecked == true) { c.AgeUnit = AgeUnits.Months; ageUnit = Properties.Resources.AgeUnitMonths; }
            //else if (checkboxYears.IsChecked == true) { c.AgeUnit = AgeUnits.Years; ageUnit = Properties.Resources.AgeUnitYears; }
            //else { c.AgeUnit = null; }
            
            //string queryGender = String.Empty;

            //if (checkboxMale.IsChecked == true) { c.Sex = "1"; queryGender = "1"; }
            //else if (checkboxFemale.IsChecked == true) { c.Sex = "2"; queryGender = "2"; }
            //else c.Sex = String.Empty;

            //if (checkboxAlive.IsChecked == true) { c.StatusReport = "2"; }
            //else if (checkboxDead.IsChecked == true) { c.StatusReport = "1"; }
            //else c.StatusReport = String.Empty;

            //if (checkboxHospCurrentYes.IsChecked == true) { c.HospitalizedCurrent = "1"; }
            //else if (checkboxHospCurrentNo.IsChecked == true) { c.HospitalizedCurrent = "2"; }
            //else c.HospitalizedCurrent = String.Empty;

            //if (checkboxCurrentlyInIsoYes.IsChecked == true) { c.IsolationCurrent = "1"; }
            //else if (checkboxCurrentlyInIsoNo.IsChecked == true) { c.IsolationCurrent = "2"; }
            //else c.HospitalizedCurrent = String.Empty;

            //if (checkboxPrevHospYes.IsChecked == true) { c.HospitalizedPast = "1"; }
            //else if (checkboxPrevHospNo.IsChecked == true) { c.HospitalizedPast = "2"; }
            //else if (checkboxPrevHospUnk.IsChecked == true) { c.HospitalizedCurrent = String.Empty; }

            //if (checkboxHospIsoYes1.IsChecked == true) { c.IsolationPast1 = "1"; }
            //else if (checkboxHospIsoNo1.IsChecked == true) { c.IsolationPast1 = "2"; }
            //else c.IsolationPast1 = String.Empty;


            //if (checkboxHospIsoYes2.IsChecked == true) { c.IsolationPast2 = "1"; }
            //else if (checkboxHospIsoNo2.IsChecked == true) { c.IsolationPast2 = "2"; }
            //else c.IsolationPast2 = String.Empty;

            //if (checkboxKnownContactYes.IsChecked == true) { c.HadContact = "1"; }
            //else if (checkboxKnownContactNo.IsChecked == true) { c.HadContact = "2"; }
            //else c.HadContact = String.Empty;



            //if (c.DateDeath.HasValue && !c.DateDeath2.HasValue)
            //{
            //    c.DateDeath2 = c.DateDeath;
            //    c.FinalCaseStatus = "1"; // dead
            //    c.CurrentStatus = Properties.Resources.Dead;
            //}

            //DateTime dtNow = DateTime.Now;
            //string userID = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString();
            //string guid = c.RecordId;

            //IDbDriver db = DataHelper.Project.CollectedData.GetDatabase();
            //Query updateQuery = db.CreateQuery("UPDATE [" + DataHelper.CaseForm.TableName + "] SET " +
            //    "[LastSaveLogonName] = @LastSaveLogonName, " +
            //    "[LastSaveTime] = @LastSaveTime " +
            //    "WHERE [GlobalRecordId] = @GlobalRecordId");
            //updateQuery.Parameters.Add(new QueryParameter("@LastSaveLogonName", DbType.String, userID));
            //updateQuery.Parameters.Add(new QueryParameter("@LastSaveTime", DbType.DateTime2, dtNow));
            //updateQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, guid.ToString()));
            //db.ExecuteNonQuery(updateQuery);

            //Field field = DataHelper.CaseForm.Fields["ID"];
            //Epi.Page page = (field as RenderableField).Page;

            //if (page == null)
            //{
            //    MessageBox.Show("There was a problem saving this case record. The page cannot be null.", "Fail", MessageBoxButton.OK, MessageBoxImage.Error);
            //    return;
            //}

            //updateQuery = db.CreateQuery("UPDATE [" + page.TableName + "] SET " +
            //        "[ID] = @ID, " +
            //        "[OrigID] = @OrigID, " +
            //        "[EpiCaseDef] = @EpiCaseDef, " +
            //        "[DateReport] = @DateReport, " +
            //        "[Surname] = @Surname, " +
            //        "[OtherNames] = @OtherNames, " +
            //        "[Age] = @Age, " +
            //        "[AgeUnit] = @AgeUnit, " +
            //        "[Gender] = @Gender, " +
            //        "[PhoneNumber] = @PhoneNumber, " +
            //        "[PhoneOwner] = @PhoneOwner, " +
            //        "[StatusReport] = @StatusReport, " +
            //        "[DateDeath] = @DateDeath, " +
            //        "[HeadHouse] = @HeadHouse, " +
            //        "[VillageRes] = @VillageRes " +

            //        "[VillageRes] = @VillageRes " +
            //        "[ParishRes] = @ParishRes " +
            //        "[CountryRes] = @CountryRes " +
            //        "[DistrictRes] = @DistrictRes " +
            //        "[SCRes] = @SCRes " +
            //        "[Farmer] = @Farmer " +
            //        "[Butcher] = @Butcher " +
            //        "[Hunter] = @Hunter " +
            //        "[Miner] = @Miner " +

            //        "[Religiousleader] = @Religiousleader " +
            //        "[Housewife] = @Housewife " +
            //        "[Student] = @Student " +
            //        "[Child] = @Child " +
            //        "[TraditionalHealer] = @TraditionalHealer " +
            //        "[Business] = @Business " +
            //        "[Transporter] = @Transporter " +
            //        "[HCW] = @HCW " +
            //        "[OtherOccup] = @OtherOccup " +
            //        "[BusinessType] = @BusinessType " +
            //        "[TransporterType] = @TransporterType " +
            //        "[HCWPosition] = @HCWPosition " +
            //        "[HCWFacility] = @HCWFacility " +
            //        "[OtherOccupDetail] = @OtherOccupDetail " +

            //        "[VillageOnset] = @VillageOnset " +
            //        "[ParishOnset] = @ParishOnset " +
            //        "[CountryOnset] = @CountryOnset " +
            //        "[DistrictOnset] = @DistrictOnset " +

            //        "[Latitude] = @Latitude " +
            //        "[Longitude] = @Longitude " +

            //        "WHERE [GlobalRecordId] = @GlobalRecordId");

            //updateQuery.Parameters.Add(new QueryParameter("@ID", DbType.String, c.ID));
            //updateQuery.Parameters.Add(new QueryParameter("@OrigID", DbType.String, c.OriginalID));
            //updateQuery.Parameters.Add(new QueryParameter("@EpiCaseDef", DbType.String, c.EpiCaseDef));
            ////if (contact.AgeYears.HasValue)
            ////{
            ////    updateQuery.Parameters.Add(new QueryParameter("@Age", DbType.Double, contact.AgeYears));
            ////}
            ////else
            ////{
            ////    updateQuery.Parameters.Add(new QueryParameter("@Age", DbType.Double, DBNull.Value));
            ////}
            ////updateQuery.Parameters.Add(new QueryParameter("@AgeUnit", DbType.String, ageUnit));
            ////updateQuery.Parameters.Add(new QueryParameter("@ContactHeadHouse", DbType.String, contact.HeadOfHousehold));
            ////updateQuery.Parameters.Add(new QueryParameter("@VillageRes", DbType.String, contact.Village));
            ////updateQuery.Parameters.Add(new QueryParameter("@DistrictRes", DbType.String, contact.District));
            ////updateQuery.Parameters.Add(new QueryParameter("@SCRes", DbType.String, contact.SubCounty));
            ////updateQuery.Parameters.Add(new QueryParameter("@LC1", DbType.String, contact.LC1Chairman));
            ////updateQuery.Parameters.Add(new QueryParameter("@PhoneNumber", DbType.String, contact.Phone));
            ////updateQuery.Parameters.Add(new QueryParameter("@ContactHCW", DbType.String, queryHCW));
            ////updateQuery.Parameters.Add(new QueryParameter("@ContactHCWFacility", DbType.String, contact.HCWFacility));
            ////updateQuery.Parameters.Add(new QueryParameter("@ContactRiskLevel", DbType.String, contact.RiskLevel));
            ////updateQuery.Parameters.Add(new QueryParameter("@ContactFinalOutcome", DbType.String, contact.FinalOutcome));
            //updateQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, guid.ToString()));
            //db.ExecuteNonQuery(updateQuery);

            //DataHelper.UpdateOrAddCase.Execute(CaseVM.RecordId);

            //if (this.Closed != null)
            //{
            //    Closed(this, new EventArgs());
            //}
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            if (this.Closed != null)
            {
                Closed(this, new EventArgs());
            }
        }

        private void txtAge_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("[0-9]"); //regex that matches disallowed text
            if (!regex.IsMatch(e.Text))
            {
                e.Handled = true;
            }
        }

        private void checkboxYears_Checked(object sender, RoutedEventArgs e)
        {
            checkboxMonths.IsChecked = false;
        }

        private void checkboxMonths_Checked(object sender, RoutedEventArgs e)
        {
            checkboxYears.IsChecked = false;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDropDownLists();
            LoadCaseData();
        }

        private void SetCurrentStatus()
        {
            bool? Alive1 = null;
            bool? Alive2 = null;

            if(checkboxAlive.IsChecked == true) Alive1 = true;
            if(checkboxDead.IsChecked == true) Alive1 = false;

            if(checkboxFinalStatusAlive.IsChecked == true) Alive2 = true;
            if(checkboxFinalStatusDead.IsChecked == true) Alive2 = false;

            if (Alive1 == null && Alive2 == null)
            {
                txtCurrentStatus.Text = String.Empty;
            }
            if (Alive1 == null && Alive2 == true)
            {
                txtCurrentStatus.Text = Properties.Resources.Alive;
            }
            if (Alive1 == null && Alive2 == false)
            {
                txtCurrentStatus.Text = Properties.Resources.Dead;
            }


            if (Alive1 == true && Alive2 == null)
            {
                txtCurrentStatus.Text = Properties.Resources.Alive;
            }
            if (Alive1 == true && Alive2 == true)
            {
                txtCurrentStatus.Text = Properties.Resources.Alive;
            }
            if (Alive1 == true && Alive2 == false)
            {
                txtCurrentStatus.Text = Properties.Resources.Dead;
            }



            if (Alive1 == false && Alive2 == null)
            {
                txtCurrentStatus.Text = Properties.Resources.Dead;
                checkboxFinalStatusDead.IsChecked = true;
            }
            if (Alive1 == false && Alive2 == true)
            {
                txtCurrentStatus.Text = Properties.Resources.Dead;
            }
            if (Alive1 == false && Alive2 == false)
            {
                txtCurrentStatus.Text = Properties.Resources.Dead;
            }



    //        IF StatusReport = (.) AND FinalStatus = (.) THEN
    //    ASSIGN StatusAsOfCurrentDate = (.)
    //END-IF

    //IF StatusReport = (.) AND FinalStatus = "2" THEN
    //    ASSIGN StatusAsOfCurrentDate = "Vivant"
    //END-IF

    //IF StatusReport = (.) AND FinalStatus = "1" THEN
    //    ASSIGN StatusAsOfCurrentDate = "Décédé"
    //END-IF


    //IF StatusReport = "2" AND FinalStatus = (.) THEN
    //    ASSIGN StatusAsOfCurrentDate = "Vivant"
    //END-IF

    //IF StatusReport = "2" AND FinalStatus = "2" THEN
    //    ASSIGN StatusAsOfCurrentDate = "Vivant"
    //END-IF

    //IF StatusReport = "2" AND FinalStatus = "1" THEN
    //    ASSIGN StatusAsOfCurrentDate = "Décédé"
    //END-IF


    //IF StatusReport = "1" AND FinalStatus = (.) THEN
    //    ASSIGN StatusAsOfCurrentDate = "Décédé"
    //END-IF

    //IF StatusReport = "1" AND FinalStatus = "2" THEN
    //    ASSIGN StatusAsOfCurrentDate = "Décédé"
    //END-IF

    //IF StatusReport = "1" AND FinalStatus = "1" THEN
    //    ASSIGN StatusAsOfCurrentDate = "Décédé"
    //END-IF

        }

        private void checkboxAlive_Checked(object sender, RoutedEventArgs e)
        {
            checkboxDead.IsChecked = false;
            dateDeath.SelectedDate = null;
            SetCurrentStatus();
        }

        private void checkboxDead_Checked(object sender, RoutedEventArgs e)
        {
            checkboxAlive.IsChecked = false;
            checkboxFinalStatusDead.IsChecked = true;
            SetCurrentStatus();
        }

        private void checkboxDead_Unchecked(object sender, RoutedEventArgs e)
        {
            dateDeath.SelectedDate = null;
            checkboxDateDeathEstimated.IsChecked = false;
            SetCurrentStatus();
        }

        private void checkboxAlive_Unchecked(object sender, RoutedEventArgs e)
        {
            SetCurrentStatus();
        }

        private void checkboxHospCurrentYes_Checked(object sender, RoutedEventArgs e)
        {
            checkboxHospCurrentNo.IsChecked = false;
        }

        private void checkboxHospCurrentNo_Checked(object sender, RoutedEventArgs e)
        {
            checkboxHospCurrentYes.IsChecked = false;
        }

        private void checkboxCurrentlyInIsoYes_Checked(object sender, RoutedEventArgs e)
        {
            checkboxCurrentlyInIsoNo.IsChecked = false;
        }

        private void checkboxCurrentlyInIsoNo_Checked(object sender, RoutedEventArgs e)
        {
            checkboxCurrentlyInIsoYes.IsChecked = false;
        }

        private void checkboxKnownContactYes_Checked(object sender, RoutedEventArgs e)
        {
            checkboxKnownContactNo.IsChecked = false;
            checkboxKnownContactUnk.IsChecked = false;
        }

        private void checkboxKnownContactNo_Checked(object sender, RoutedEventArgs e)
        {
            checkboxKnownContactYes.IsChecked = false;
            checkboxKnownContactUnk.IsChecked = false;
        }

        private void checkboxKnownContactUnk_Checked(object sender, RoutedEventArgs e)
        {
            checkboxKnownContactNo.IsChecked = false;
            checkboxKnownContactYes.IsChecked = false;
        }

        private void checkboxContactAlive1_Checked(object sender, RoutedEventArgs e)
        {
            checkboxContactDead1.IsChecked = false;
            dpContactDateDeath1.SelectedDate = null;
        }

        private void checkboxContactDead1_Checked(object sender, RoutedEventArgs e)
        {
            checkboxContactAlive1.IsChecked = false;
        }

        private void checkboxContactAlive2_Checked(object sender, RoutedEventArgs e)
        {
            checkboxContactDead2.IsChecked = false;
            dpContactDateDeath2.SelectedDate = null;
        }

        private void checkboxContactDead2_Checked(object sender, RoutedEventArgs e)
        {
            checkboxContactAlive2.IsChecked = false;
        }

        private void checkboxContactAlive3_Checked(object sender, RoutedEventArgs e)
        {
            checkboxContactDead3.IsChecked = false;
            dpContactDateDeath3.SelectedDate = null;
        }

        private void checkboxContactDead3_Checked(object sender, RoutedEventArgs e)
        {
            checkboxContactAlive3.IsChecked = false;
        }

        private void checkboxAttendFuneralYes_Checked(object sender, RoutedEventArgs e)
        {
            checkboxAttendFuneralNo.IsChecked = false;
            checkboxAttendFuneralUnk.IsChecked = false;
        }

        private void checkboxAttendFuneralNo_Checked(object sender, RoutedEventArgs e)
        {
            checkboxAttendFuneralYes.IsChecked = false;
            checkboxAttendFuneralUnk.IsChecked = false;
        }

        private void checkboxAttendFuneralUnk_Checked(object sender, RoutedEventArgs e)
        {
            checkboxAttendFuneralNo.IsChecked = false;
            checkboxAttendFuneralYes.IsChecked = false;
        }

        private void checkboxTravelYes_Checked(object sender, RoutedEventArgs e)
        {
            checkboxTravelNo.IsChecked = false;
            checkboxTravelUnk.IsChecked = false;
        }

        private void checkboxTravelNo_Checked(object sender, RoutedEventArgs e)
        {
            checkboxTravelYes.IsChecked = false;
            checkboxTravelUnk.IsChecked = false;
        }

        private void checkboxTravelUnk_Checked(object sender, RoutedEventArgs e)
        {
            checkboxTravelNo.IsChecked = false;
            checkboxTravelYes.IsChecked = false;
        }

        private void checkboxHospBeforeIllYes_Checked(object sender, RoutedEventArgs e)
        {
            checkboxHospBeforeIllNo.IsChecked = false;
            checkboxHospBeforeIllUnk.IsChecked = false;
        }

        private void checkboxHospBeforeIllNo_Checked(object sender, RoutedEventArgs e)
        {
            checkboxHospBeforeIllYes.IsChecked = false;
            checkboxHospBeforeIllUnk.IsChecked = false;
        }

        private void checkboxHospBeforeIllUnk_Checked(object sender, RoutedEventArgs e)
        {
            checkboxHospBeforeIllNo.IsChecked = false;
            checkboxHospBeforeIllYes.IsChecked = false;
        }

        private void checkboxTradHealerYes_Checked(object sender, RoutedEventArgs e)
        {
            checkboxTradHealerNo.IsChecked = false;
            checkboxTradHealerUnk.IsChecked = false;
        }

        private void checkboxTradHealerNo_Checked(object sender, RoutedEventArgs e)
        {
            checkboxTradHealerYes.IsChecked = false;
            checkboxTradHealerUnk.IsChecked = false;
        }

        private void checkboxTradHealerUnk_Checked(object sender, RoutedEventArgs e)
        {
            checkboxTradHealerYes.IsChecked = false;
            checkboxTradHealerNo.IsChecked = false;
        }

        private void checkboxAnimalsYes_Checked(object sender, RoutedEventArgs e)
        {
            checkboxAnimalsNo.IsChecked = false;
            checkboxAnimalsUnk.IsChecked = false;
        }

        private void checkboxAnimalsNo_Checked(object sender, RoutedEventArgs e)
        {
            checkboxAnimalsYes.IsChecked = false;
            checkboxAnimalsUnk.IsChecked = false;
        }

        private void checkboxAnimalsUnk_Checked(object sender, RoutedEventArgs e)
        {
            checkboxAnimalsNo.IsChecked = false;
            checkboxAnimalsYes.IsChecked = false;
        }

        private void checkboxBittenTickYes_Checked(object sender, RoutedEventArgs e)
        {
            checkboxBittenTickNo.IsChecked = false;
            checkboxBittenTickUnk.IsChecked = false;
        }

        private void checkboxBittenTickNo_Checked(object sender, RoutedEventArgs e)
        {
            checkboxBittenTickYes.IsChecked = false;
            checkboxBittenTickUnk.IsChecked = false;
        }

        private void checkboxBittenTickUnk_Checked(object sender, RoutedEventArgs e)
        {
            checkboxBittenTickNo.IsChecked = false;
            checkboxBittenTickYes.IsChecked = false;
        }

        private void checkboxPatient_Checked(object sender, RoutedEventArgs e)
        {
            checkboxProxy.IsChecked = false;
            txtProxyName.Text = String.Empty;
            txtProxyRelation.Text = String.Empty;
        }

        private void checkboxProxy_Checked(object sender, RoutedEventArgs e)
        {
            checkboxPatient.IsChecked = false;
        }

        private void checkboxFinalStatusAlive_Checked(object sender, RoutedEventArgs e)
        {
            checkboxFinalStatusDead.IsChecked = false;
            SetCurrentStatus();
        }

        private void checkboxFinalStatusDead_Checked(object sender, RoutedEventArgs e)
        {
            checkboxFinalStatusAlive.IsChecked = false;
            SetCurrentStatus();
        }

        private void checkboxFinalStatusAlive_Unchecked(object sender, RoutedEventArgs e)
        {
            SetCurrentStatus();
        }

        private void checkboxFinalStatusDead_Unchecked(object sender, RoutedEventArgs e)
        {
            SetCurrentStatus();
        }

        private void checkboxUnexBleedYes_Checked(object sender, RoutedEventArgs e)
        {
            checkboxUnexBleedNo.IsChecked = false;
            checkboxUnexBleedUnk.IsChecked = false;
        }

        private void checkboxUnexBleedNo_Checked(object sender, RoutedEventArgs e)
        {
            checkboxUnexBleedYes.IsChecked = false;
            checkboxUnexBleedUnk.IsChecked = false;
            txtOtherNonHemo.Text = String.Empty;
        }

        private void checkboxUnexBleedUnk_Checked(object sender, RoutedEventArgs e)
        {
            checkboxUnexBleedNo.IsChecked = false;
            checkboxUnexBleedYes.IsChecked = false;
            txtOtherNonHemo.Text = String.Empty;
        }

        private void checkboxDeathCommunity_Checked(object sender, RoutedEventArgs e)
        {
            checkboxDeathHospital.IsChecked = false;
            checkboxDeathOther.IsChecked = false;
        }

        private void checkboxDeathHospital_Checked(object sender, RoutedEventArgs e)
        {
            checkboxDeathCommunity.IsChecked = false;
            checkboxDeathOther.IsChecked = false;
        }

        private void checkboxDeathOther_Checked(object sender, RoutedEventArgs e)
        {
            checkboxDeathHospital.IsChecked = false;
            checkboxDeathCommunity.IsChecked = false;
        }

        private void txtCaseID_LostFocus(object sender, RoutedEventArgs e)
        {
            //if (!String.IsNullOrEmpty(txtCaseID.Text))
            //{
            //    foreach (CaseViewModel c in DataHelper.CaseCollection)
            //    {
            //        if (CaseVM != c)
            //        {
            //            if (txtCaseID.Text.Equals(c.ID))
            //            {
            //                MessageBox.Show("Another case already has an ID value of " + txtCaseID.Text + ".", "Duplicate ID", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            //                //txtCaseID.Focus();
            //                e.Handled = true;
            //                return;
            //            }
            //        }
            //    }

            //    string ID = txtCaseID.Text;
            //    string errorMessage = String.Empty;

            //    if (!ID.Contains(CaseViewModel.IDSeparator))
            //    {
            //        errorMessage = "The case ID field is incorrectly formatted. The separator character '" + CaseViewModel.IDSeparator + "' is required.";
            //    }
            //    else
            //    {
            //        int position = ID.IndexOf(CaseViewModel.IDSeparator);
            //        string prefix = ID.Substring(0, position);
            //        int prefixLength = prefix.Length;

            //        string pattern = ID.Substring(position + 1);

            //        double caseNumber = -1;
            //        bool success = double.TryParse(pattern, out caseNumber);

            //        if (prefixLength != CaseViewModel.IDPrefix.Length)
            //        {
            //            errorMessage = "The ID prefix has an incorrect length. Please ensure you are using the ID format " + CaseViewModel.IDPrefix + CaseViewModel.IDSeparator + CaseViewModel.IDPattern;
            //        }
            //        else if (prefix != CaseViewModel.IDPrefix)
            //        {
            //            errorMessage = "The ID prefix is incorrect. Please ensure you are using the ID format " + CaseViewModel.IDPrefix + CaseViewModel.IDSeparator + CaseViewModel.IDPattern;
            //        }
            //        else if (!success)
            //        {
            //            errorMessage = "The ID suffix must be a valid number. Please ensure you are using the ID format " + CaseViewModel.IDPrefix + CaseViewModel.IDSeparator + CaseViewModel.IDPattern;
            //        }
            //        else if (pattern.Length != CaseViewModel.IDPattern.Length)
            //        {
            //            errorMessage = "The ID suffix does not have a valid number of characters. Please ensure you are using the ID format " + CaseViewModel.IDPrefix + CaseViewModel.IDSeparator + CaseViewModel.IDPattern;
            //        }
            //    }
            //    if (String.IsNullOrEmpty(ID))
            //    {
            //        errorMessage = "First Name is required";
            //    }

            //    if (!String.IsNullOrEmpty(errorMessage))
            //    {
            //        MessageBox.Show(errorMessage, "Invalid ID", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            //        txtCaseID.Focus();
            //        e.Handled = true;
            //        return;
            //    }
            //}
        }

        private void txtCaseID_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (!String.IsNullOrEmpty(txtCaseID.Text))
            {
                foreach (CaseViewModel c in DataHelper.CaseCollection)
                {
                    if (CaseVM != c)
                    {
                        if (txtCaseID.Text.Equals(c.ID))
                        {
                            MessageBox.Show("Another case already has an ID value of " + txtCaseID.Text + ".", "Duplicate ID", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                            e.Handled = true;
                            return;
                        }
                    }
                }

                //string ID = txtCaseID.Text;
                //string errorMessage = String.Empty;

                //if (!ID.Contains(CaseViewModel.IDSeparator))
                //{
                //    errorMessage = "The case ID field is incorrectly formatted. The separator character '" + CaseViewModel.IDSeparator + "' is required.";
                //}
                //else
                //{
                //    int position = ID.IndexOf(CaseViewModel.IDSeparator);
                //    string prefix = ID.Substring(0, position);
                //    int prefixLength = prefix.Length;

                //    string pattern = ID.Substring(position + 1);

                //    double caseNumber = -1;
                //    bool success = double.TryParse(pattern, out caseNumber);

                //    //if (prefixLength != CaseViewModel.IDPrefix.Length)
                //    //{
                //    //    errorMessage = "The ID prefix has an incorrect length. Please ensure you are using the ID format " + CaseViewModel.IDPrefix + CaseViewModel.IDSeparator + CaseViewModel.IDPattern;
                //    //}
                //    //else if (prefix != CaseViewModel.IDPrefix)
                //    //{
                //    //    errorMessage = "The ID prefix is incorrect. Please ensure you are using the ID format " + CaseViewModel.IDPrefix + CaseViewModel.IDSeparator + CaseViewModel.IDPattern;
                //    //}
                //    //else if (!success)
                //    //{
                //    //    errorMessage = "The ID suffix must be a valid number. Please ensure you are using the ID format " + CaseViewModel.IDPrefix + CaseViewModel.IDSeparator + CaseViewModel.IDPattern;
                //    //}
                //    //else if (pattern.Length != CaseViewModel.IDPattern.Length)
                //    //{
                //    //    errorMessage = "The ID suffix does not have a valid number of characters. Please ensure you are using the ID format " + CaseViewModel.IDPrefix + CaseViewModel.IDSeparator + CaseViewModel.IDPattern;
                //    //}
                //}
                //if (String.IsNullOrEmpty(ID))
                //{
                //    errorMessage = "First Name is required";
                //}

                //if (!String.IsNullOrEmpty(errorMessage))
                //{
                //    MessageBox.Show(errorMessage, "Invalid ID", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                //    e.Handled = true;
                //    return;
                //}
            }
        }

        private void cBleedUnexplainedY_Checked(object sender, RoutedEventArgs e)
        {
            //cBleedGumsN.IsChecked = false;
            //cBleedInjectionSiteN.IsChecked = false;
            //cNoseBleedN.IsChecked = false;
            //cBloodyStoolN.IsChecked = false;
            //cBloodVomitN.IsChecked = false;
            //cDigestedBloodN.IsChecked = false;
            //cCoughBloodN.IsChecked = false;
            //cBleedVaginaN.IsChecked = false;
            //cBruisingN.IsChecked = false;
            //cBloodInUrineN.IsChecked = false;
            //cOtherHemorrhagicN.IsChecked = false;
        }

        private void cBleedUnexplainedN_Checked(object sender, RoutedEventArgs e)
        {
            cBleedGumsN.IsChecked = true; 
            cBleedInjectionSiteN.IsChecked = true; 
            cNoseBleedN.IsChecked = true;
            cBloodyStoolN.IsChecked = true;
            cBloodVomitN.IsChecked = true;
            cDigestedBloodN.IsChecked = true;
            cCoughBloodN.IsChecked = true;
            if (checkboxFemale.IsChecked == true)
            {
                cBleedVaginaN.IsChecked = true;
            }
            cBruisingN.IsChecked = true;
            cBloodInUrineN.IsChecked = true;
            cOtherHemorrhagicN.IsChecked = true;
        }

        private void cBleedUnexplainedY_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void cBleedUnexplainedN_Unchecked(object sender, RoutedEventArgs e)
        {
            cBleedGumsN.IsChecked = false;
            cBleedInjectionSiteN.IsChecked = false;
            cNoseBleedN.IsChecked = false;
            cBloodyStoolN.IsChecked = false;
            cBloodVomitN.IsChecked = false;
            cDigestedBloodN.IsChecked = false;
            cCoughBloodN.IsChecked = false;
            cBleedVaginaN.IsChecked = false;
            cBruisingN.IsChecked = false;
            cBloodInUrineN.IsChecked = false;
            cOtherHemorrhagicN.IsChecked = false;
        }
    }
}
