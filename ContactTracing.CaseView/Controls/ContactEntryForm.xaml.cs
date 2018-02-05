using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
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
using Epi;
using Epi.Data;

namespace ContactTracing.CaseView.Controls
{
    /// <summary>
    /// Interaction logic for ContactEntryForm.xaml
    /// </summary>   
    public partial class ContactEntryForm : UserControl
    {
        public event EventHandler Closed;

        private bool IsViewingRelationshipInfo { get; set; }

        private ContactViewModel ContactVM;
        private CaseViewModel CaseVM;
        private IDbDriver Database { get; set; }
        private string ContactFormTableName { get; set; }

        public CaseContactPairViewModel CaseContactPair { get; private set; }

        private bool _isNewContact = false;

        enum SmartQuery
        {
            InsertColumnList,
            InsertValuesList
        }


        public ContactEntryForm(EpiDataHelper dataHelper, CaseViewModel c, bool isSuperUser = false)
        {
            InitializeComponent();
            this.DataContext = dataHelper;
            panelFinalOutcome.IsEnabled = false;
            IsNewContact = true;
            CaseVM = c;
            Construct();
        }

        public ContactEntryForm(EpiDataHelper dataHelper, ContactViewModel contact, bool isSuperUser = false)
        {
            InitializeComponent();
            this.DataContext = dataHelper;
            panelFinalOutcome.IsEnabled = isSuperUser;
            IsNewContact = false;
            ContactVM = contact;
            Construct();
        }

        private void Construct()
        {
            IsViewingRelationshipInfo = false;
            tblockDistrictRes.Text = DataHelper.Adm1 + ":";
            tblockSCRes.Text = DataHelper.Adm2 + ":";
            tblockParishRes.Text = DataHelper.Adm3 + ":";
            tblockVillageRes.Text = DataHelper.Adm4 + ":";

            Database = DataHelper.Project.CollectedData.GetDatabase();
            bool hasTeamColumn = false;
            foreach (Epi.Page page in DataHelper.ContactForm.Pages)
            {
                hasTeamColumn = Database.ColumnExists(page.TableName, "Team");
                if (hasTeamColumn)
                    break;
            }
            if (hasTeamColumn)
            {
                txtTeam.Visibility = System.Windows.Visibility.Visible;
                tbTeam.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                txtTeam.Visibility = System.Windows.Visibility.Collapsed;
                tbTeam.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        public EpiDataHelper DataHelper
        {
            get { return this.DataContext as EpiDataHelper; }
        }

        private bool IsNewContact
        {
            get { return _isNewContact; }
            set
            {
                _isNewContact = value;
                if (value)
                {
                    tblockSourceCase.Visibility = Visibility.Collapsed;
                    gridSourceCase.Visibility = Visibility.Collapsed;

                    tblockRelationship.Visibility = Visibility.Visible;
                    panelRelationship.Visibility = Visibility.Visible;

                    tblockContactID.Visibility = System.Windows.Visibility.Collapsed;
                    txtContactID.Visibility = System.Windows.Visibility.Collapsed;

                    //rectangleSplitter.Visibility = System.Windows.Visibility.Visible;

                    col1.MaxWidth = 560;
                    //tblockIns1.MaxWidth = 440;
                    //tblockIns2.MaxWidth = 440;
                    //tblockIns3.MaxWidth = 440;
                }
                else
                {
                    tblockSourceCase.Visibility = Visibility.Visible;
                    gridSourceCase.Visibility = Visibility.Visible;

                    //tblockRelationship.Visibility = Visibility.Collapsed;
                    //panelRelationship.Visibility = Visibility.Collapsed;

                    tblockContactID.Visibility = System.Windows.Visibility.Visible;
                    txtContactID.Visibility = System.Windows.Visibility.Visible;

                    //rectangleSplitter.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
        }

        #region Event Handlers
        private void checkboxMale_Checked(object sender, RoutedEventArgs e)
        {
            checkboxFemale.IsChecked = false;
        }

        private void checkboxFemale_Checked(object sender, RoutedEventArgs e)
        {
            checkboxMale.IsChecked = false;
        }

        private void checkboxHCWYes_Checked(object sender, RoutedEventArgs e)
        {
            checkboxHCWNo.IsChecked = false;
        }

        private void checkboxHCWNo_Checked(object sender, RoutedEventArgs e)
        {
            checkboxHCWYes.IsChecked = false;
            txtHCWFacility.Text = String.Empty;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (ContactVM != null)
            {
                DataHelper.SendMessageForLockContact(ContactVM);
            }

            if (CaseVM != null)
            {
                DataHelper.SendMessageForLockCase(CaseVM);
            }


            SwapTextBoxesAndComboBoxes();

        }

        /// <summary>
        /// Hides combo boxes if the source field in Epi Info 7 is a text field; if the source field is a drop-down list in Epi Info, however, then this makes sure the comboboxes are displayed instead
        /// </summary>
        private void SwapTextBoxesAndComboBoxes()
        {
            EpiDataHelper vm = ((FrameworkElement)this.Parent).DataContext as EpiDataHelper;
            if (vm != null && vm.ContactForm != null)
            {


                #region  Village
                if (vm.CaseForm.Fields["VillageRes"] is Epi.Fields.DDLFieldOfLegalValues || vm.CaseForm.Fields["VillageRes"] is Epi.Fields.DDLFieldOfCodes)
                {

                    cmbVillage.Visibility = System.Windows.Visibility.Visible;
                    txtVillage.Visibility = System.Windows.Visibility.Collapsed;


                }
                else
                {

                    cmbVillage.Visibility = System.Windows.Visibility.Collapsed;
                    txtVillage.Visibility = System.Windows.Visibility.Visible;


                }
                #endregion

                #region  District
                if (vm.CaseForm.Fields["Districtres"] is Epi.Fields.DDLFieldOfLegalValues || vm.CaseForm.Fields["Districtres"] is Epi.Fields.DDLFieldOfCodes)
                {

                    cmbDistrict.Visibility = System.Windows.Visibility.Visible;
                    txtDistrict.Visibility = System.Windows.Visibility.Collapsed;


                }
                else
                {

                    cmbDistrict.Visibility = System.Windows.Visibility.Collapsed;
                    txtDistrict.Visibility = System.Windows.Visibility.Visible;


                }
                #endregion //  District


                #region     Parish  
                if (vm.CaseForm.Fields["ParishRes"] is Epi.Fields.DDLFieldOfLegalValues || vm.CaseForm.Fields["ParishRes"] is Epi.Fields.DDLFieldOfCodes)
                {

                    cmbParish.Visibility = System.Windows.Visibility.Visible;

                    txtParish.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    cmbParish.Visibility = System.Windows.Visibility.Collapsed;

                    txtParish.Visibility = System.Windows.Visibility.Visible;
                }
                #endregion

                #region    Sub County
                if (vm.CaseForm.Fields["SCRes"] is Epi.Fields.DDLFieldOfLegalValues || vm.CaseForm.Fields["SCRes"] is Epi.Fields.DDLFieldOfCodes)
                {

                    cmbSubCounty.Visibility = System.Windows.Visibility.Visible;

                    txtSubCounty.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    cmbSubCounty.Visibility = System.Windows.Visibility.Collapsed;

                    txtSubCounty.Visibility = System.Windows.Visibility.Visible;
                }
                #endregion

                #region    ContactHCWFacility
                if (vm.ContactForm.Fields["ContactHCWFacility"] is Epi.Fields.DDLFieldOfLegalValues || vm.ContactForm.Fields["ContactHCWFacility"] is Epi.Fields.DDLFieldOfCodes
                    || vm.ContactForm.Fields["ContactHCWFacility"] is Epi.Fields.DDLFieldOfCommentLegal)
                {

                    cmbHCWfacility.Visibility = System.Windows.Visibility.Visible;

                    txtHCWFacility.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    cmbHCWfacility.Visibility = System.Windows.Visibility.Collapsed;

                    txtHCWFacility.Visibility = System.Windows.Visibility.Visible;
                }
                #endregion

                #region    Team

                if (vm.ContactForm.Fields.Exists("Team"))
                {


                    if (vm.ContactForm.Fields["Team"] is Epi.Fields.DDLFieldOfLegalValues || vm.ContactForm.Fields["Team"] is Epi.Fields.DDLFieldOfCodes
                        || vm.ContactForm.Fields["Team"] is Epi.Fields.DDLFieldOfCommentLegal)
                    {

                        cmbTeam.Visibility = System.Windows.Visibility.Visible;

                        txtTeam.Visibility = System.Windows.Visibility.Collapsed;
                    }
                    else
                    {
                        cmbTeam.Visibility = System.Windows.Visibility.Collapsed;

                        txtTeam.Visibility = System.Windows.Visibility.Visible;
                    }
                }
                #endregion

            }
        }

        private void cmbDistrict_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataHelper.DistrictsSubCounties != null && DataHelper.DistrictsSubCounties.Count > 0)
            {
                cmbSubCounty.Items.Clear();
                foreach (KeyValuePair<string, List<string>> district in DataHelper.DistrictsSubCounties)
                {
                    if (cmbDistrict.SelectedItem != null && cmbDistrict.SelectedItem.ToString() == district.Key)
                    {
                        foreach (string sc in district.Value)
                        {
                            cmbSubCounty.Items.Add(sc);
                        }
                    }
                }
            }
        }

        private void cmbHCWfacility_LostFocus(object sender, RoutedEventArgs e)
        {
            ComboBox cmbHCWfacility = (ComboBox)sender;

            if (cmbHCWfacility.Text.Replace(" ", "").Length == 0)
                return;

            EpiDataHelper vm = ((FrameworkElement)this.Parent).DataContext as EpiDataHelper;
            bool match = false;
            if (vm != null && vm.ContactHCWFacilities != null && vm.ContactHCWFacilities.Count > 0)
            {
                foreach (string hospital in vm.ContactHCWFacilities)
                {
                    if (hospital.Equals(cmbHCWfacility.Text))
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
                cmbHCWfacility.Text = "";
                MessageBox.Show("Invalid Hospital", null, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cmbTeam_LostFocus(object sender, RoutedEventArgs e)
        {
            ComboBox cmbTeam = (ComboBox)sender;

            if (cmbTeam.Text.Replace(" ", "").Length == 0)
                return;

            EpiDataHelper vm = ((FrameworkElement)this.Parent).DataContext as EpiDataHelper;
            bool match = false;
            if (vm != null && vm.ContactTeamCollection != null && vm.ContactTeamCollection.Count > 0)
            {
                foreach (string hospital in vm.ContactTeamCollection)
                {
                    if (hospital.Equals(cmbTeam.Text))
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
                cmbTeam.Text = "";
                MessageBox.Show("Invalid Team", null, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void checkboxFO_Discharged_Checked(object sender, RoutedEventArgs e)
        {
            checkboxFO_Isolated.IsChecked = false;
            checkboxFO_Dropped.IsChecked = false;
        }

        private void checkboxFO_Isolated_Checked(object sender, RoutedEventArgs e)
        {
            checkboxFO_Discharged.IsChecked = false;
            checkboxFO_Dropped.IsChecked = false;
        }

        private void checkboxFO_Dropped_Checked(object sender, RoutedEventArgs e)
        {
            checkboxFO_Isolated.IsChecked = false;
            checkboxFO_Discharged.IsChecked = false;
        }
        #endregion // Event Handlers

        public void ClearCaseData()
        {
            txtCaseID.Text = String.Empty;
            txtCaseName.Text = String.Empty;
            txtDateContact.Text = String.Empty;
        }

        public void ClearContactData()
        {
            txtSurname.Text = String.Empty;
            txtOtherNames.Text = String.Empty;
            checkboxMale.IsChecked = false;
            checkboxFemale.IsChecked = false;

            txtAge.Text = String.Empty;

            txtHeadHousehold.Text = String.Empty;

            cmbVillage.Text = String.Empty;
            cmbDistrict.Text = String.Empty;
            cmbSubCounty.Text = String.Empty;

            txtDistrict.Text = string.Empty;
            txtSubCounty.Text = string.Empty;

            txtParish.Text = "";


            txtLC1Chairman.Text = String.Empty;
            txtPhoneNumber.Text = String.Empty;

            checkboxHCWYes.IsChecked = false;
            checkboxHCWNo.IsChecked = false;

            txtHCWFacility.Text = String.Empty;
            txtTeam.Text = String.Empty;
            txtRiskLevel.Text = String.Empty;

            checkboxFO_Discharged.IsChecked = false;
            checkboxFO_Isolated.IsChecked = false;
            checkboxFO_Dropped.IsChecked = false;
        }

        public void LoadCaseData(CaseViewModel c)
        {
            if (!IsNewContact)
            {
            }
        }

        public void LoadContactData(ContactViewModel contact)
        {
            if (!IsNewContact) { txtContactID.Text = contact.ContactID; }
            txtSurname.Text = contact.Surname;
            txtOtherNames.Text = contact.OtherNames;
            if (contact.Gender == Properties.Resources.Male) checkboxMale.IsChecked = true;
            if (contact.Gender == Properties.Resources.Female) checkboxFemale.IsChecked = true;

            if (contact.Age.HasValue) txtAge.Text = contact.Age.Value.ToString();
            if (contact.AgeUnit.HasValue)
            {
                switch (contact.AgeUnit.Value)
                {
                    case AgeUnits.Months:
                        cmbAgeUnit.Text = Properties.Resources.AgeUnitMonths;
                        break;
                    case AgeUnits.Years:
                        cmbAgeUnit.Text = Properties.Resources.AgeUnitYears;
                        break;
                }
            }
            else
            {
                cmbAgeUnit.SelectedIndex = -1;
            }

            txtHeadHousehold.Text = contact.HeadOfHousehold;

            cmbVillage.Text = contact.Village;

            txtVillage.Text = contact.Village;



            cmbDistrict.Text = contact.District;
            txtDistrict.Text = contact.District;

            cmbSubCounty.Text = contact.SubCounty;
            txtSubCounty.Text = contact.SubCounty;

            txtParish.Text = contact.Parish;


            txtLC1Chairman.Text = contact.LC1Chairman;
            txtPhoneNumber.Text = contact.Phone;

            if (contact.HCW == ContactTracing.CaseView.Properties.Resources.Yes) checkboxHCWYes.IsChecked = true;
            if (contact.HCW == ContactTracing.CaseView.Properties.Resources.No) checkboxHCWNo.IsChecked = true;

            txtHCWFacility.Text = contact.HCWFacility;
            cmbHCWfacility.Text = contact.HCWFacility;
            txtTeam.Text = contact.Team;
            cmbTeam.Text = contact.Team;
            txtRiskLevel.Text = contact.RiskLevel;

            if (contact.FinalOutcome == "1") checkboxFO_Discharged.IsChecked = true;
            if (contact.FinalOutcome == "2") checkboxFO_Isolated.IsChecked = true;
            if (contact.FinalOutcome == "3") checkboxFO_Dropped.IsChecked = true;

            if (!IsNewContact)
            {
                txtCaseID.Text = contact.LastSourceCase.ID;
                txtCaseName.Text = contact.LastSourceCase.Surname + " " + contact.LastSourceCase.OtherNames;
                txtDateContact.Text = contact.DateOfLastContact.Value.ToShortDateString();
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (IsViewingRelationshipInfo && IsNewContact)
            {
                IsViewingRelationshipInfo = false;
                svPrimary.Visibility = System.Windows.Visibility.Visible;
                panelRelationship.Visibility = System.Windows.Visibility.Collapsed;
                panelRelationshipInfo.Visibility = System.Windows.Visibility.Collapsed;
                return;
            }

            ReleaseLocks();

            if (this.Closed != null)
            {
                Closed(this, new EventArgs());
            }
        }


        private bool SaveRecord()
        {
            // TODO:  Now that the code to autmatically add "Team" amd "ContactParish" is added to EpiDataHelper
            // the coditional code in this function should be removed    
            //    
            if (IsNewContact && !dateContact.SelectedDate.HasValue)
            {
                MessageBox.Show("Must have a date of last contact to proceed.");
                return false;
            }
            if (txtAge.Text.Trim().Length > 0 && cmbAgeUnit.SelectedIndex <= 0) // 0 is currently the 'blank' option
            {
                MessageBox.Show("Cannot save a record that has an age without an age unit.");
                return false;
            }

            ContactViewModel contact = ContactVM; // new ContactViewModel();
            //ContactVM = contact;

            DateTime dtNow = DateTime.Now;
            dtNow = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, dtNow.Hour, dtNow.Minute, dtNow.Second, 0);

            try
            {
                if (IsNewContact)
                {
                    contact = new ContactViewModel();
                    ContactVM = contact;
                }

                contact.Surname = txtSurname.Text;
                contact.OtherNames = txtOtherNames.Text;

                string queryGender = String.Empty;

                if (checkboxMale.IsChecked == true) { contact.Gender = Properties.Resources.Male; queryGender = "1"; }
                else if (checkboxFemale.IsChecked == true) { contact.Gender = Properties.Resources.Female; queryGender = "2"; }
                else contact.Gender = String.Empty;

                string ageUnit = cmbAgeUnit.Text;

                if (cmbAgeUnit.SelectedIndex >= 0)
                {
                    if (ageUnit.Equals(Properties.Resources.AgeUnitMonths, StringComparison.OrdinalIgnoreCase))
                    {
                        contact.AgeUnit = AgeUnits.Months;
                    }
                    else if (ageUnit.Equals(Properties.Resources.AgeUnitYears, StringComparison.OrdinalIgnoreCase))
                    {
                        contact.AgeUnit = AgeUnits.Years;
                    }
                }
                //switch ()
                //{
                //    case AgeUnits.Months:
                //        ageUnit = Properties.Resources.AgeUnitMonths;
                //        break;
                //    case AgeUnits.Years:
                //        ageUnit = Properties.Resources.AgeUnitYears;
                //        break;
                //}

                if (!String.IsNullOrEmpty(txtAge.Text.Trim()))
                {
                    contact.Age = double.Parse(txtAge.Text.Trim());
                }
                else
                {
                    contact.AgeUnit = null;
                    contact.Age = null;
                }

                contact.HeadOfHousehold = txtHeadHousehold.Text;

                EpiDataHelper vm = ((FrameworkElement)this.Parent).DataContext as EpiDataHelper;


                // Parish         
                if (vm.CaseForm.Fields["Parishres"] is Epi.Fields.DDLFieldOfLegalValues ||
                    vm.CaseForm.Fields["Parishres"] is Epi.Fields.DDLFieldOfCodes)
                {
                    contact.Parish = cmbParish.Text;
                }
                else
                {
                    contact.Parish = txtParish.Text;                 
                }


                //  SubCounty         
                if (vm.CaseForm.Fields["SCRes"] is Epi.Fields.DDLFieldOfLegalValues ||
                    vm.CaseForm.Fields["SCRes"] is Epi.Fields.DDLFieldOfCodes)
                {
                    contact.SubCounty = cmbSubCounty.Text;
                }
                else
                {
                    contact.SubCounty = txtSubCounty.Text;
                }

                // District 
                if (vm.CaseForm.Fields["Districtres"] is Epi.Fields.DDLFieldOfLegalValues ||
                    vm.CaseForm.Fields["Districtres"] is Epi.Fields.DDLFieldOfCodes)
                {
                    contact.District = cmbDistrict.Text;                 
                }
                else
                {
                    contact.District = txtDistrict.Text;
                }

                // Team    
                if (vm.ContactForm.Fields.Exists("Team"))
                {
                    if (vm.ContactForm.Fields["Team"] is Epi.Fields.DDLFieldOfLegalValues || vm.ContactForm.Fields["Team"] is Epi.Fields.DDLFieldOfCodes)
                    {
                        contact.Team = cmbTeam.Text;
                    }
                    else
                    {
                        contact.Team = txtTeam.Text;
                    }
                }


                if (vm.ContactForm.Fields["ContactHCWFacility"] is Epi.Fields.DDLFieldOfLegalValues || vm.ContactForm.Fields["ContactHCWFacility"] is Epi.Fields.DDLFieldOfCodes)
                {
                    contact.HCWFacility = cmbHCWfacility.Text;
                }
                else
                {
                    contact.HCWFacility = txtHCWFacility.Text;
                }

                contact.Village = txtVillage.Text;

                contact.LC1Chairman = txtLC1Chairman.Text;
                contact.Phone = txtPhoneNumber.Text;

                string queryHCW = String.Empty;

                if (checkboxHCWYes.IsChecked == true) { contact.HCW = Properties.Resources.Yes; queryHCW = "1"; }
                else if (checkboxHCWNo.IsChecked == true) { contact.HCW = Properties.Resources.No; ; queryHCW = "2"; }
                else contact.HCW = String.Empty;

       

                if (checkboxFO_Discharged.IsChecked == true) contact.FinalOutcome = "1";
                else if (checkboxFO_Isolated.IsChecked == true) contact.FinalOutcome = "2";
                else if (checkboxFO_Dropped.IsChecked == true) contact.FinalOutcome = "3";
                else contact.FinalOutcome = String.Empty;

                Database = DataHelper.Project.CollectedData.GetDatabase();
                ContactFormTableName = DataHelper.ContactForm.TableName;

                string userID = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString();

                Guid guid = System.Guid.NewGuid();
                if (IsNewContact)
                {
                    contact.FirstSaveTime = dtNow;

                    Query insertQuery = Database.CreateQuery("INSERT INTO [" + DataHelper.ContactForm.TableName + "] (GlobalRecordId, RECSTATUS, FirstSaveLogonName, LastSaveLogonName, FirstSaveTime, LastSaveTime) VALUES (" +
                        "@GlobalRecordId, @RECSTATUS, @FirstSaveLogonName, @LastSaveLogonName, @FirstSaveTime, @LastSaveTime)");
                    insertQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, guid.ToString()));
                    insertQuery.Parameters.Add(new QueryParameter("@RECSTATUS", DbType.Byte, 1));
                    insertQuery.Parameters.Add(new QueryParameter("@FirstSaveLogonName", DbType.String, userID));
                    insertQuery.Parameters.Add(new QueryParameter("@LastSaveLogonName", DbType.String, userID));
                    insertQuery.Parameters.Add(new QueryParameter("@FirstSaveTime", DbType.DateTime, dtNow));
                    insertQuery.Parameters.Add(new QueryParameter("@LastSaveTime", DbType.DateTime, dtNow));

                    using (IDbTransaction transaction = Database.OpenTransaction())
                    {
                        Database.ExecuteNonQuery(insertQuery, transaction);

                        foreach (Epi.Page page in DataHelper.ContactForm.Pages)
                        {
                            bool hasTeamColumn = Database.ColumnExists(page.TableName, "Team");
                            bool hasContactParishColumn = Database.ColumnExists(page.TableName, "ContactParish");


                            string columnListText = "(GlobalRecordId, ContactSurname, ContactOtherNames, " +
                                "ContactGender, ContactAge, ContactAgeUnit, ContactHeadHouse, ContactVillage, ContactDistrict, ContactSC, LC1, " +
                                "ContactPhone, ContactHCW, ContactHCWFacility, Team, ContactParish, RiskLevel, FinalOutcome   )";

                            //                      columnListText = checkForNewColumns(SmartQuery.InsertColumnList, columnListText);

                            string columnValuesText = "( @GlobalRecordId, @ContactSurname, @ContactOtherNames, @ContactGender, @ContactAge, @ContactAgeUnit, @ContactHeadHouse, @ContactVillage, @ContactDistrict, " +
                                                        "@ContactSC, @LC1, @ContactPhone, @ContactHCW, @ContactHCWFacility, @Team, @ContactParish, @ContactRiskLevel, @ContactFinalOutcome     )";

                            //                     columnValuesText = checkForNewColumns(SmartQuery.InsertValuesList, columnValuesText);


                            // contact form has only one page, so we can get away with this code for the time being.
                            insertQuery = Database.CreateQuery("INSERT INTO [" + page.TableName + "] " +
                                columnListText + " VALUES " + columnValuesText);

                            //  Not used due to new function          
                            //if (hasTeamColumn)
                            //    insertQuery = Database.CreateQuery("INSERT INTO [" + page.TableName + "] (GlobalRecordId, ContactSurname, ContactOtherNames, " +
                            //        "ContactGender, ContactAge, ContactAgeUnit, ContactHeadHouse, ContactVillage, ContactDistrict, ContactSC, LC1, " +
                            //        "ContactPhone, ContactHCW, ContactHCWFacility, Team, RiskLevel, FinalOutcome, ContactParish ) VALUES (" +
                            //    "@GlobalRecordId, @ContactSurname, @ContactOtherNames, @ContactGender, @ContactAge, @ContactAgeUnit, @ContactHeadHouse, @ContactVillage, @ContactDistrict, " +
                            //    "@ContactSC, @LC1, @ContactPhone, @ContactHCW, @ContactHCWFacility, @ContactTeam, @ContactRiskLevel, @ContactFinalOutcome, @ContactParish )");        

                            insertQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, guid.ToString()));
                            insertQuery.Parameters.Add(new QueryParameter("@ContactSurname", DbType.String, contact.Surname));
                            insertQuery.Parameters.Add(new QueryParameter("@ContactOtherNames", DbType.String, contact.OtherNames));
                            insertQuery.Parameters.Add(new QueryParameter("@ContactGender", DbType.String, queryGender));
                            if (contact.AgeYears.HasValue)
                            {
                                insertQuery.Parameters.Add(new QueryParameter("@ContactAge", DbType.Double, contact.AgeYears));
                            }
                            else
                            {
                                insertQuery.Parameters.Add(new QueryParameter("@ContactAge", DbType.String, DBNull.Value));
                            }
                            insertQuery.Parameters.Add(new QueryParameter("@ContactAgeUnit", DbType.String, ageUnit));
                            insertQuery.Parameters.Add(new QueryParameter("@ContactHeadHouse", DbType.String, contact.HeadOfHousehold));
                            insertQuery.Parameters.Add(new QueryParameter("@ContactVillage", DbType.String, contact.Village));
                            insertQuery.Parameters.Add(new QueryParameter("@ContactDistrict", DbType.String, contact.District));
                            //     insertQuery.Parameters.Add(new QueryParameter("@ContactParish", DbType.String, contact.Parish));
                            insertQuery.Parameters.Add(new QueryParameter("@ContactSC", DbType.String, contact.SubCounty));
                            insertQuery.Parameters.Add(new QueryParameter("@LC1", DbType.String, contact.LC1Chairman));
                            insertQuery.Parameters.Add(new QueryParameter("@ContactPhone", DbType.String, contact.Phone));
                            insertQuery.Parameters.Add(new QueryParameter("@ContactHCW", DbType.String, queryHCW));
                            insertQuery.Parameters.Add(new QueryParameter("@ContactHCWFacility", DbType.String, contact.HCWFacility));
                            //  if (hasTeamColumn)
                            insertQuery.Parameters.Add(new QueryParameter("@Team", DbType.String, contact.Team));

                            //  if (hasContactParishColumn)
                            insertQuery.Parameters.Add(new QueryParameter("@ContactParish", DbType.String, contact.Parish));


                            insertQuery.Parameters.Add(new QueryParameter("@ContactRiskLevel", DbType.String, contact.RiskLevel));
                            insertQuery.Parameters.Add(new QueryParameter("@ContactFinalOutcome", DbType.String, contact.FinalOutcome));
                            Database.ExecuteNonQuery(insertQuery, transaction);
                        }

                        try
                        {
                            transaction.Commit();
                        }
                        catch (Exception ex0)
                        {
                            Epi.Logger.Log(String.Format(DateTime.Now + ":  " + "Contact INSERT Commit Exception Type: {0}", ex0.GetType()));
                            Epi.Logger.Log(String.Format(DateTime.Now + ":  " + "Contact INSERT Commit Exception Message: {0}", ex0.Message));
                            Epi.Logger.Log(String.Format(DateTime.Now + ":  " + "Contact INSERT Commit Rollback started..."));
                            DbLogger.Log("Contact record insertion failed on commit. Transaction rolled back. Exception: " + ex0.Message);
                            try
                            {
                                transaction.Rollback();
                                Epi.Logger.Log(String.Format(DateTime.Now + ":  " + "Contact INSERT Commit Rollback was successful."));
                            }
                            catch (Exception ex1)
                            {
                                DbLogger.Log("Contact record insertion failed on commit rollback. Exception: " + ex1.Message);
                            }
                        }

                        Database.CloseTransaction(transaction);
                    }

                    DbLogger.Log(String.Format(
                    "Inserted contact : GUID = {0}",
                        contact.RecordId));
                }
                else
                {
                    guid = new Guid(contact.RecordId);
                    bool hasTeamColumn = false; bool hasContactParishColumn = false;

                    //  Check for team  
                    foreach (Epi.Page page in DataHelper.ContactForm.Pages)
                    {
                        hasTeamColumn = Database.ColumnExists(page.TableName, "Team");
                        if (hasTeamColumn)
                            break;
                    }

                    //  Check for ContactParish    
                    foreach (Epi.Page page in DataHelper.ContactForm.Pages)
                    {
                        hasContactParishColumn = Database.ColumnExists(page.TableName, "ContactParish");
                        if (hasContactParishColumn)
                            break;
                    }

                    Query updateQuery = Database.CreateQuery("UPDATE [" + DataHelper.ContactForm.TableName + "] SET " +
                        "[LastSaveLogonName] = @LastSaveLogonName, " +
                        "[LastSaveTime] = @LastSaveTime " +
                        "WHERE [GlobalRecordId] = @GlobalRecordId");
                    updateQuery.Parameters.Add(new QueryParameter("@LastSaveLogonName", DbType.String, userID));
                    updateQuery.Parameters.Add(new QueryParameter("@LastSaveTime", DbType.DateTime, dtNow));
                    updateQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, guid.ToString()));
                    int rows = Database.ExecuteNonQuery(updateQuery);

                    if (rows == 0)
                    {
                        MessageBox.Show("Warning: Could not update data for this contact.");
                    }


                    updateQuery = Database.CreateQuery("UPDATE [" + DataHelper.ContactForm.Pages[0].TableName + "] SET " +
                        "[ContactSurname] = @Surname, " +
                        "[ContactOtherNames] = @OtherNames, " +
                        "[ContactGender] = @Gender, " +
                        "[ContactAge] = @Age, " +
                        "[ContactAgeUnit] = @AgeUnit, " +
                        "[ContactHeadHouse] = @HeadHouse, " +
                        "[ContactVillage] = @VillageRes, " +
                        "[ContactDistrict] = @DistrictRes, " +
                        "[ContactSC] = @SCRes, " +
                        "[LC1] = @LC1, " +
                        "[ContactPhone] = @PhoneNumber, " +
                        "[ContactHCW] = @HCW, " +
                        "[ContactHCWFacility] = @HCWFacility, " +
                        "[RiskLevel] = @RiskLevel, " +
                        "[FinalOutcome] = @FinalOutcome, " +
                        //(hasContactParishColumn == true ? "[ContactParish] = @ParishRes,   " : " ") +
                        //(hasTeamColumn == true ? "[Team] = @Team  " : " ") +
                        "[ContactParish] = @ParishRes,   " + 
                        "[Team] = @Team  "  +    
                        "WHERE [GlobalRecordId] = @GlobalRecordId");

          
                    updateQuery.Parameters.Add(new QueryParameter("@Surname", DbType.String, contact.Surname));
                    updateQuery.Parameters.Add(new QueryParameter("@OtherNames", DbType.String, contact.OtherNames));
                    updateQuery.Parameters.Add(new QueryParameter("@Gender", DbType.String, queryGender));
                    if (contact.AgeYears.HasValue)
                    {
                        updateQuery.Parameters.Add(new QueryParameter("@Age", DbType.Double, contact.AgeYears));
                    }
                    else
                    {
                        updateQuery.Parameters.Add(new QueryParameter("@Age", DbType.Double, DBNull.Value));
                    }
                    updateQuery.Parameters.Add(new QueryParameter("@AgeUnit", DbType.String, ageUnit));
                    updateQuery.Parameters.Add(new QueryParameter("@HeadHouse", DbType.String, contact.HeadOfHousehold));
                    updateQuery.Parameters.Add(new QueryParameter("@VillageRes", DbType.String, contact.Village));
                    updateQuery.Parameters.Add(new QueryParameter("@DistrictRes", DbType.String, contact.District));
                    updateQuery.Parameters.Add(new QueryParameter("@SCRes", DbType.String, contact.SubCounty));
                    updateQuery.Parameters.Add(new QueryParameter("@LC1", DbType.String, contact.LC1Chairman));
                    updateQuery.Parameters.Add(new QueryParameter("@PhoneNumber", DbType.String, contact.Phone));
                    updateQuery.Parameters.Add(new QueryParameter("@HCW", DbType.String, queryHCW));
                    updateQuery.Parameters.Add(new QueryParameter("@HCWFacility", DbType.String, contact.HCWFacility));
                    updateQuery.Parameters.Add(new QueryParameter("@RiskLevel", DbType.String, contact.RiskLevel));
                    updateQuery.Parameters.Add(new QueryParameter("@FinalOutcome", DbType.String, contact.FinalOutcome));
                    if (hasContactParishColumn)
                        updateQuery.Parameters.Add(new QueryParameter("@ParishRes", DbType.String, contact.Parish));
                    if (hasTeamColumn)
                        updateQuery.Parameters.Add(new QueryParameter("@Team", DbType.String, contact.Team));
                    updateQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, guid.ToString()));
                    Database.ExecuteNonQuery(updateQuery);

                    DataHelper.SendMessageForUpdateContact(guid.ToString());
                    ReleaseLocks();

                    if (String.IsNullOrEmpty(contact.FinalOutcome))
                    {
                        contact.IsActive = true;
                    }
                    else
                    {
                        contact.IsActive = false;
                    }

                    DbLogger.Log(String.Format(
                        "Updated contact : GUID = {0}",
                            contact.RecordId));
                }

                //DataHelper.UpdateOrAddContact.Execute(CaseContactPair);

                if (IsNewContact)
                {
                    StringBuilder sb = new StringBuilder();
                    if (cbxCon1.IsChecked == true) sb.Append("1");
                    else sb.Append("0");
                    if (cbxCon2.IsChecked == true) sb.Append("1");
                    else sb.Append("0");
                    if (cbxCon3.IsChecked == true) sb.Append("1");
                    else sb.Append("0");
                    if (cbxCon4.IsChecked == true) sb.Append("1");
                    else sb.Append("0");

                    // use relationship info, save case-contact pair AND contact
                    CaseContactPair = new CaseContactPairViewModel();
                    CaseContactPair.Relationship = txtRelationship.Text;
                    CaseContactPair.ContactRecordId = guid.ToString();
                    CaseContactPair.CaseVM = CaseVM;
                    CaseContactPair.ContactType = Convert.ToInt32(sb.ToString(), 2);
                    CaseContactPair.IsContactDateEstimated = cbxEstimated.IsChecked == true ? true : false;
                    CaseContactPair.DateLastContact = dateContact.SelectedDate.Value;
                    CaseContactPair.ContactVM = contact;
                    CaseContactPair.ContactVM.DateOfLastContact = dateContact.SelectedDate.Value;
                    CaseContactPair.ContactVM.RecordId = guid.ToString();

                    CaseContactPair.ContactVM.FollowUpWindowViewModel = new FollowUpWindowViewModel(CaseContactPair.DateLastContact, ContactVM, CaseVM);
                    DataHelper.UpdateOrAddContact.Execute(CaseContactPair);
                    DataHelper.UpdateCaseContactLink.Execute(CaseContactPair);

                    //DataHelper.SendMessageForAddContact(contact.RecordId);
                    // I removed the 'add' send message since it gets called in the 'UpdateOrAdd' call above

                    BackgroundWorker worker = new BackgroundWorker();
                    worker.DoWork += worker_DoWork;
                    worker.RunWorkerCompleted += worker_RunWorkerCompleted;
                    worker.RunWorkerAsync(CaseContactPair);
                }
                return true;
            }
            catch (Exception ex)
            {
                ReleaseLocks();

                string newRecordString = "existing";
                if (IsNewContact)
                {
                    newRecordString = "new";
                }

                if (ex.InnerException == null)
                {
                    MessageBox.Show(dtNow.ToShortDateString() + "\n\nThere was a problem saving this record (" + newRecordString + "). Exception message: " + ex.Message);
                }
                else
                {
                    MessageBox.Show(dtNow.ToShortDateString() + "\n\nThere was a problem saving this record (" + newRecordString + "). Outer exception message: " + ex.Message + ".\n\nInner exception message: " + ex.InnerException.Message);
                }
                return false;
            }
        }
        struct ColumnStatus
        {
            public string ColumnName { get; set; }
            public bool ColumnExists { get; set; }
        }

        private string checkForNewColumns(SmartQuery smartQuery, string baseText)
        {

            bool hasTeamColumn = false; bool hasContactParishColumn = false;

            List<ColumnStatus> newColumns = new List<ColumnStatus>();
            List<ColumnStatus> newColumnsOut = new List<ColumnStatus>();


            StringCollection newColsColl = ContactTracing.CaseView.Properties.Settings.Default.NewColumns;


            foreach (string item in newColsColl)
            {


                ColumnStatus cs = new ColumnStatus();
                cs.ColumnName = item;
                cs.ColumnExists = false;
                newColumns.Add(cs);

            }

            //  hard coded    
            //newColumns.Add(new ColumnStatus()
            //{
            //    ColumnName = "Team",
            //    ColumnExists = false
            //});
            //newColumns.Add(new ColumnStatus()
            //{
            //    ColumnName = "ContactParish",
            //    ColumnExists = false
            //});


            foreach (ColumnStatus column in newColumns)
            {

                foreach (Epi.Page page in DataHelper.ContactForm.Pages)
                {
                    ColumnStatus cs = new ColumnStatus();
                    cs.ColumnExists = Database.ColumnExists(page.TableName, column.ColumnName);
                    cs.ColumnName = column.ColumnName;
                    newColumnsOut.Add(cs);
                    if (cs.ColumnExists)
                        break;
                }

            }



            if (newColumnsOut.Count == 0)
            {
                return "";
            }


            switch (smartQuery)
            {
                case SmartQuery.InsertColumnList:
                    foreach (ColumnStatus cs in newColumnsOut)
                    {
                        int pos = baseText.IndexOf(")");
                        if (cs.ColumnExists)
                            baseText = baseText.Insert(pos, "    " + "," + cs.ColumnName);
                    }

                    break;
                case SmartQuery.InsertValuesList:

                    foreach (ColumnStatus cs in newColumnsOut)
                    {
                        int pos = baseText.IndexOf(")");
                        if (cs.ColumnExists)
                            baseText = baseText.Insert(pos, "    " + ",@" + cs.ColumnName);
                    }

                    break;
                default:
                    break;
            }


            return baseText;


        }

  
        private string checkForNewColumns(string column, string context)
        {
            throw new NotImplementedException();
        }

        private void ReleaseLocks()
        {
            if (ContactVM != null)
            {
                DataHelper.SendMessageForUnlockContact(ContactVM);
            }

            if (CaseVM != null)
            {
                DataHelper.SendMessageForUnlockCase(CaseVM);
            }
        }

        public void ContactIdUpdated(KeyValuePair<int, CaseContactPairViewModel> kvp)
        {
            CaseContactPairViewModel ccp = kvp.Value;
            int uniqueKey = kvp.Key;

            ccp.ContactVM.UniqueKey = uniqueKey;

            ReleaseLocks();

            if (ccp.ContactVM != ContactVM)
            {
                DataHelper.SendMessageForUnlockContact(ccp.ContactVM);
            }

            if (ccp.CaseVM != CaseVM)
            {
                DataHelper.SendMessageForUnlockCase(ccp.CaseVM);
            }
        }

        public delegate void ContactIdUpdatedEventHandler(KeyValuePair<int, CaseContactPairViewModel> kvp);

        public void ForceRepopulation()
        {
            MessageBox.Show("There was a problem obtaining the contact ID number. Please contact the application developer. Click OK to refresh the database.");
            DataHelper.RepopulateCollections();
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result is KeyValuePair<int, CaseContactPairViewModel>)
            {
                KeyValuePair<int, CaseContactPairViewModel> kvp =
                    (KeyValuePair<int, CaseContactPairViewModel>)e.Result;
                this.Dispatcher.BeginInvoke(new ContactIdUpdatedEventHandler(ContactIdUpdated), kvp);
            }
            else if (e.Result is Exception)
            {
                this.Dispatcher.BeginInvoke(new SimpleEventHandler(ForceRepopulation));
            }
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                CaseContactPairViewModel ccp = e.Argument as CaseContactPairViewModel;

                if (ccp != null)
                {
                    Query selectQuery = Database.CreateQuery("SELECT UniqueKey FROM " + ContactFormTableName + " WHERE [GlobalRecordId] = @GlobalRecordId");
                    selectQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, ccp.ContactVM.RecordId));

                    KeyValuePair<int, CaseContactPairViewModel> kvp = new KeyValuePair<int, CaseContactPairViewModel>(Convert.ToInt32(Database.Select(selectQuery).Rows[0]["UniqueKey"]),
                        ccp);
                    e.Result = kvp;
                }
            }
            catch (Exception ex)
            {
                e.Result = ex;
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!IsViewingRelationshipInfo && IsNewContact)
            {
                if (!String.IsNullOrEmpty(txtAge.Text) && cmbAgeUnit.SelectedIndex == -1)
                {
                    MessageBox.Show("Please specify an age unit.", "Age unit needed", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                IsViewingRelationshipInfo = true;
                svPrimary.Visibility = System.Windows.Visibility.Collapsed;
                panelRelationship.Visibility = System.Windows.Visibility.Visible;
                panelRelationshipInfo.Visibility = System.Windows.Visibility.Visible;
                return;
            }

            if (!SaveRecord()) return;

            if (this.Closed != null)
            {
                Closed(this, new EventArgs());
            }
        }

        //private List<string> FindMatches()
        //{
        //    List<string> wordList = new List<string>();

        //    string gender = String.Empty;
        //    if (checkboxFemale.IsChecked == true) gender = Properties.Resources.Female;
        //    if (checkboxMale.IsChecked == true) gender = Properties.Resources.Male;
        //    string word = txtSurname.Text + " " + txtOtherNames.Text + " " + gender;

        //    foreach(ContactViewModel c in DataHelper.ContactCollection) 
        //    {
        //        wordList.Add(c.Surname + " " + c.OtherNames + " " + c.Gender);
        //    }

        //    List<string> foundWords = Core.Common.Search(
        //        word,
        //        wordList,
        //        0.77);

        //    return foundWords;
        //}

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            if (this.Closed != null)
            {
                MessageBoxResult result = MessageBox.Show("Do you want to save this record?", "Save", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    if (!SaveRecord()) return;
                    ReleaseLocks();
                    Closed(this, new EventArgs());
                }
                else if (result == MessageBoxResult.No)
                {
                    ReleaseLocks();
                    Closed(this, new EventArgs());
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }
        }

        private void txtAge_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            int value = -1;
            bool success = int.TryParse(e.Text, out value);

            if (!(e.Text.Equals(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator) ||
                success))
            {
                e.Handled = true;
            }
            //System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("[0-9]"); //regex that matches disallowed text
            //if (!regex.IsMatch(e.Text))
            //{
            //    e.Handled = true;
            //}
        }

        private void txtAge_LostFocus(object sender, RoutedEventArgs e)
        {
            double value = -1;
            bool success = double.TryParse(txtAge.Text, out value);
            if (!success)
            {
                if (!String.IsNullOrEmpty(txtAge.Text.Trim()))
                {
                    MessageBox.Show("An invalid value has been detected in the Age field. Please try again.");
                    txtAge.Text = String.Empty;
                }
            }
            else
            {
                if (value < 0)
                {
                    MessageBox.Show("An invalid value has been detected in the Age field. Please try again.");
                    txtAge.Text = String.Empty;
                }
            }
        }
    }
}
