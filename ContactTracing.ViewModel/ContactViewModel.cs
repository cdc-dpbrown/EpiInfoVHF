using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows.Input;
using ContactTracing.Core;

namespace ContactTracing.ViewModel
{
    public class ContactViewModel : ObservableObject
    {
        #region Members
        //public Contact Contact;
        private bool _isLocked = false;
        private FollowUpWindowViewModel _followUpWindowViewModel;
        private string _recordId = String.Empty;
        private string _surname = String.Empty;
        private string _otherNames = String.Empty;
        private string _gender = String.Empty;
        private double? _age;
        private double? _ageYears;
        private int _uniqueKey = 0;
        private AgeUnits? _ageUnit = null;
        private DateTime? _firstSaveTime = null;
        private DateTime? _dateOfLastContact = null;
        private CaseViewModel _lastSourceCase = null;
        private string _relationshipToLastSourceCase = String.Empty;
        private string _lastSourceCaseContactTypes = String.Empty;
        private string _village = String.Empty;
        private string _subCounty = String.Empty;
        private string _district = String.Empty;
        private string _parish = String.Empty;
        private string _country = String.Empty;
        private string _riskLevel = String.Empty;
        private string _headOfHousehold = String.Empty;
        private string _LC1Chairman = String.Empty;
        private string _phone = String.Empty;
        private string _HCW = String.Empty;
        private string _HCWFacility = String.Empty;
        private string _Team = String.Empty;
        private string _finalOutcome = String.Empty;
        private bool _isCase = false;
        private bool _isActive = true;
        private string _contactID = String.Empty;
        #endregion // Members

        #region Static Members
        public static string Male = String.Empty;
        public static string Female = String.Empty;
        #endregion // Static Members

        #region Properties

        private string _contactStateID;

        public string ContactStateID
        {
            get { return _contactStateID; }
            set { _contactStateID = value;
            RaisePropertyChanged("ContactStateID");
            }
        }

        private string _contactCDCID;

        public string ContactCDCID
        {
            get { return _contactCDCID; }
            set { _contactCDCID = value;
            RaisePropertyChanged("ContactCDCID");
            }
        }

        private string _publicHealthAction;

        public string PublicHealthAction
        {
            get { return _publicHealthAction; }
            set { _publicHealthAction = value; }
        }



        public string ContactID
        {
            get
            {
                return this._contactID;
            }
            private set
            {
                this._contactID = value;
                RaisePropertyChanged("ContactID");
            }
        }

        public int UniqueKey
        {
            get { return this._uniqueKey; }
            set
            {
                this._uniqueKey = value;
                this.ContactID = "C-" + value.ToString("D4");
                RaisePropertyChanged("UniqueKey");
                RaisePropertyChanged("ContactID");
            }
        }
        public string IsCaseSymbol
        {
            get
            {
                if (IsCase)
                {
                    return "‼";
                }
                else
                {
                    return String.Empty;
                }
            }
        }
        public bool IsLocked
        {
            get
            {
                return _isLocked;
            }
            set
            {
                if (_isLocked != value)
                {
                    _isLocked = value;
                    RaisePropertyChanged("IsLocked");
                }
            }
        }
        public FollowUpWindowViewModel FollowUpWindowViewModel
        {
            get
            {
                return _followUpWindowViewModel;
            }
            set
            {
                if (_followUpWindowViewModel != value)
                {
                    _followUpWindowViewModel = value;
                    RaisePropertyChanged("FollowUpWindowViewModel");
                }
            }
        }
        public string RecordId
        {
            get { return _recordId; }
            set
            {
                if (_recordId != value)
                {
                    _recordId = value;
                    RaisePropertyChanged("RecordId");
                }
            }
        }
        public string Surname
        {
            get { return _surname; }
            set
            {
                if (_surname != value)
                {
                    _surname = value;
                    RaisePropertyChanged("Surname");
                }
            }
        }
        public string OtherNames
        {
            get { return _otherNames; }
            set
            {
                if (_otherNames != value)
                {
                    _otherNames = value;
                    RaisePropertyChanged("OtherNames");
                }
            }
        }        
        public string Gender
        {
            get { return _gender; }
            set
            {
                if (_gender != value)
                {
                    _gender = value;
                    RaisePropertyChanged("Gender");
                }
            }
        }
        public string GenderAbbreviation
        {
            get 
            {
                if (this.Gender.Equals(ContactViewModel.Female)) { return "F"; }
                else if (this.Gender.Equals(ContactViewModel.Male)) { return "M"; }
                else return String.Empty;
            }
        }
        public double? Age
        {
            get { return this._age; }
            set
            {
                this._age = value;

                if (AgeUnit == AgeUnits.Years)
                {
                    AgeYears = Math.Round(this.Age.Value, 2);
                }
                else if (AgeUnit == AgeUnits.Months && Age.HasValue)
                {
                    double newAge = (Age.Value / 12);
                    AgeYears = Math.Round(newAge, 2);
                }
                //else if (AgeUnit == null) { throw new ApplicationException("Age unit cannot be null in Age setter."); } // form updated to force AgeUnit if Age has a value, so this is no longer needed
                else if (CaseViewModel.IsCountryUS)//17224 // making use of CaseViewModel property coz, contact is always bound to Case. And this value should be same for the whole app not just CaseViewModel
                {
                    AgeYears = this.Age;
                }
                else
                {
                    AgeYears = null;
                }

                RaisePropertyChanged("Age");
                RaisePropertyChanged("AgeYears");
            }
        }
        public double? AgeYears
        {
            get
            {
                return _ageYears;
            }
            private set
            {
                this._ageYears = value;
            }
        }
        public AgeUnits? AgeUnit
        {
            get { return _ageUnit; }
            set
            {
                if (_ageUnit != value)
                {
                    _ageUnit = value;
                    RaisePropertyChanged("AgeUnit");
                }
            }
        }
        public DateTime? FirstSaveTime
        {
            get { return _firstSaveTime; }
            set
            {
                if (_firstSaveTime != value)
                {
                    _firstSaveTime = value;
                    RaisePropertyChanged("FirstSaveTime");
                }
            }
        }
        public DateTime? DateOfLastContact
        {
            get { return _dateOfLastContact; }
            set
            {
                if (_dateOfLastContact != value)
                {
                    _dateOfLastContact = value;
                    RaisePropertyChanged("DateOfLastContact");
                    RaisePropertyChanged("DateOfLastFollowUp");
                    RaisePropertyChanged("IsWithin21DayWindow");
                }
            }
        }
        public CaseViewModel LastSourceCase
        {
            get
            {
                return _lastSourceCase;
            }
            set
            {
                if (_lastSourceCase != value)
                {
                    _lastSourceCase = value;
                    RaisePropertyChanged("LastSourceCase");
                }
            }
        }
        public string RelationshipToLastSourceCase
        {
            get
            {
                return _relationshipToLastSourceCase;
            }
            set
            {
                if (_relationshipToLastSourceCase != value)
                {
                    _relationshipToLastSourceCase = value;
                    RaisePropertyChanged("RelationshipToLastSourceCase");
                }
            }
        }
        public string LastSourceCaseContactTypes
        {
            get
            {
                return _lastSourceCaseContactTypes;
            }
            set
            {
                if (_lastSourceCaseContactTypes != value)
                {
                    _lastSourceCaseContactTypes = value;
                    RaisePropertyChanged("LastSourceCaseContactTypes");
                }
            }
        }
        public DateTime? DateOfLastFollowUp
        {
            get 
            {
                if (DateOfLastContact.HasValue)
                {
                    return DateOfLastContact.Value.AddDays(Core.Common.DaysInWindow);
                }
                else
                {
                    return null;
                }
            }
        }
        public string Village
        {
            get { return _village; }
            set
            {
                if (_village != value)
                {
                    _village = value;
                    RaisePropertyChanged("Village");
                }
            }
        }
        public string SubCounty
        {
            get { return _subCounty; }
            set
            {
                if (_subCounty != value)
                {
                    _subCounty = value;
                    RaisePropertyChanged("SubCounty");
                }
            }
        }
        public string District
        {
            get { return _district; }
            set
            {
                if (_district != value)
                {
                    _district = value;
                    RaisePropertyChanged("District");
                }
            }
        }
        public string Parish
        {
            get { return _parish; }
            set
            {
                if (_parish != value)
                {
                    _parish = value;
                    RaisePropertyChanged("Parish");
                }
            }
        }
        public string Country
        {
            get { return _country; }
            set
            {
                if (_country != value)
                {
                    _country = value;
                    RaisePropertyChanged("Country");
                }
            }
        }
        public string RiskLevel
        {
            get { return _riskLevel; }
            set
            {
                if (_riskLevel != value)
                {
                    _riskLevel = value;
                    RaisePropertyChanged("RiskLevel");
                }
            }
        }
        public string HeadOfHousehold
        {
            get { return _headOfHousehold; }
            set
            {
                if (_headOfHousehold != value)
                {
                    _headOfHousehold = value;
                    RaisePropertyChanged("HeadOfHousehold");
                }
            }
        }
        public string LC1Chairman
        {
            get { return _LC1Chairman; }
            set
            {
                if (_LC1Chairman != value)
                {
                    _LC1Chairman = value;
                    RaisePropertyChanged("LC1Chairman");
                }
            }
        }
        public string Phone
        {
            get { return _phone; }
            set
            {
                if (_phone != value)
                {
                    _phone = value;
                    RaisePropertyChanged("Phone");
                }
            }
        }
        public string HCW
        {
            get { return _HCW; }
            set
            {
                if (_HCW != value)
                {
                    _HCW = value;
                    RaisePropertyChanged("HCW");
                }
            }
        }
        public string HCWFacility
        {
            get { return _HCWFacility; }
            set
            {
                if (_HCWFacility != value)
                {
                    _HCWFacility = value;
                    RaisePropertyChanged("HCWFacility");
                }
            }
        }
        public string Team
        {
            get { return _Team; }
            set
            {
                if (_Team != value)
                {
                    _Team = value;
                    RaisePropertyChanged("Team");
                }
            }
        }
        public string FinalOutcome
        {
            get { return _finalOutcome; }
            set
            {
                if (_finalOutcome != value)
                {
                    _finalOutcome = value;
                    RaisePropertyChanged("FinalOutcome");
                    RaisePropertyChanged("FinalOutcomeDisplay");
                    RaisePropertyChanged("HasFinalOutcome");
                }
            }
        }
        public string FinalOutcomeDisplay
        {
            get 
            {
                switch (FinalOutcome)
                {
                    case "1":
                        return "Discharged from follow-up";
                    case "2":
                        return "Developed symptoms & isolated";
                    case "3":
                        return "Dropped from follow-up";
                    default:
                        return String.Empty;
                }
            }
        }
        public bool HasFinalOutcome
        {
            get
            {
                return !(String.IsNullOrEmpty(FinalOutcome));
            }
        }
        public bool IsCase
        {
            get { return _isCase; }
            set
            {
                if (_isCase != value)
                {
                    _isCase = value;
                    RaisePropertyChanged("IsCase");
                    RaisePropertyChanged("IsCaseSymbol");
                }
            }
        }
        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    RaisePropertyChanged("IsActive");
                }
            }
        }
        public bool IsWithin21DayWindow
        {
            get
            {
                DateTime today = DateTime.Today;
                if(today >= this.DateOfLastContact.Value.AddDays(1) && today <= (new DateTime(DateOfLastFollowUp.Value.Year, DateOfLastFollowUp.Value.Month, DateOfLastFollowUp.Value.Day, 23, 59, 59))) 
                {
                    return true;
                }
                return false;
            }
        }

        private DateTime? contactDOB = null;

        public DateTime? ContactDOB
        {
            get { return contactDOB; }
            set { contactDOB = value; }
        }

        private string contactAddress = string.Empty;

        public string ContactAddress
        {
            get { return contactAddress; }
            set { contactAddress = value; }
        }

        private string contactZip = string.Empty;

        public string ContactZip
        {
            get { return contactZip; }
            set { contactZip = value; }
        }

        private string contactEmail = string.Empty;

        public string ContactEmail
        {
            get { return contactEmail; }
            set { contactEmail = value; }
        }

        private string contactHCWPosition = string.Empty;

        public string ContactHCWPosition
        {
            get { return contactHCWPosition; }
            set { contactHCWPosition = value; }
        }

        private string contactHCWDistrict = string.Empty;

        public string ContactHCWDistrict
        {
            get { return contactHCWDistrict; }
            set { contactHCWDistrict = value; }
        }

        private string contactHCWSC = string.Empty;

        public string ContactHCWSC
        {
            get { return contactHCWSC; }
            set { contactHCWSC = value; }
        }

        private string contactHCWVillage = string.Empty;

        public string ContactHCWVillage
        {
            get { return contactHCWVillage; }
            set { contactHCWVillage = value; }
        }


        #endregion // Properties

        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        public ContactViewModel()
        {
            IsLocked = false;
            Surname = String.Empty;
            ContactCDCID = string.Empty;
            ContactStateID = string.Empty;
            PublicHealthAction = string.Empty;
            OtherNames = String.Empty;
            Gender = String.Empty;
            RiskLevel = String.Empty;
            HeadOfHousehold = String.Empty;
            Village = String.Empty;
            SubCounty = String.Empty;
            District = String.Empty;
            Country = String.Empty;
            Parish = String.Empty;
            LC1Chairman = String.Empty;
            Phone = String.Empty;
            HCW = String.Empty;
            HCWFacility = String.Empty;
            Team = String.Empty;
            FinalOutcome = String.Empty;
            IsActive = true;
            IsCase = false;
        }

        public ContactViewModel(ContactViewModel newContact)
        {
            UpdateContact(newContact);
        }

        //public ContactViewModel(CaseContactPairViewModel caseContactPair, int uniqueKey)
        //{
        //    Contact = new Contact();
        //    CreateContactFromCase(caseContactPair, uniqueKey);
        //}

        public ContactViewModel(CaseViewModel caseVM, int uniqueKey)
        {
            CreateContactFromCase(caseVM, uniqueKey);
        }
        #endregion // Constructors

        #region Methods

        public override string ToString()
        {
            return ContactID + " : " + Surname + ", " + OtherNames + " : " + GenderAbbreviation;
        }

        private void CreateContactFromCase(CaseViewModel caseVM, int uniqueKey)
        {
            this.AgeUnit = caseVM.AgeUnit;
            this.Age = caseVM.Age;
            this.HeadOfHousehold = caseVM.HeadOfHousehold;
            this.District = caseVM.District;
            if (caseVM.Gender == Core.Enums.Gender.Female)
            {
                Gender = Female;
            }
            else if (caseVM.Gender == Core.Enums.Gender.Male)
            {
                Gender = Male;
            }
            else
            {
                Gender = String.Empty;
            }
            //this.Gender = caseVM.Gender;
            this.IsCase = true;
            this.OtherNames = caseVM.OtherNames;
            this.RecordId = caseVM.RecordId;
            this.UniqueKey = uniqueKey;
            this.SubCounty = caseVM.SubCounty;
            this.Surname = caseVM.Surname;
            this.ContactStateID = caseVM.ID;
            this.ContactCDCID = caseVM.OriginalID;
            this.Village = caseVM.Village;
            this.Country = caseVM.Country;
            this.Parish = caseVM.Parish;
            this.ContactDOB = caseVM.DOB;
            this.ContactAddress = caseVM.AddressRes;
            this.ContactZip = caseVM.ZipRes;
            this.Phone = caseVM.PhoneNumber;
            this.ContactEmail = caseVM.Email;
            this.HCW = caseVM.IsHCW.ToString().Trim();
            this.ContactHCWPosition = caseVM.OccupationHCWPosition;
            this.HCWFacility = caseVM.OccupationHCWFacility;
            this.ContactHCWDistrict = caseVM.OccupationHCWDistrict;
            this.ContactHCWSC = caseVM.OccupationHCWSC;
            this.ContactHCWVillage = caseVM.OccupationHCWVillage;
        }

        public void UpdateContact(ContactViewModel updatedContact)
        {
            this.AgeUnit = updatedContact.AgeUnit;
            this.Age = updatedContact.Age;
            this.DateOfLastContact = updatedContact.DateOfLastContact;
            this.FirstSaveTime = updatedContact.FirstSaveTime;
            this.LastSourceCase = updatedContact.LastSourceCase;
            this.District = updatedContact.District;
            this.Gender = updatedContact.Gender;
            this.LC1Chairman = updatedContact.LC1Chairman;
            this.HeadOfHousehold = updatedContact.HeadOfHousehold;
            this.HCW = updatedContact.HCW;
            this.HCWFacility = updatedContact.HCWFacility;
            this.Team = updatedContact.Team;
            this.IsCase = updatedContact.IsCase;
            this.OtherNames = updatedContact.OtherNames;
            this.RecordId = updatedContact.RecordId;
            this.SubCounty = updatedContact.SubCounty;
            this.Surname = updatedContact.Surname;
            this.ContactCDCID = updatedContact.ContactCDCID;
            this.ContactStateID = updatedContact.ContactStateID;
            this.PublicHealthAction = updatedContact.PublicHealthAction;
            this.UniqueKey = updatedContact.UniqueKey;
            this.IsActive = updatedContact.IsActive;
            this.Phone = updatedContact.Phone;
            this.RiskLevel = updatedContact.RiskLevel;
            this.Village = updatedContact.Village;
            this.Country = updatedContact.Country;
            this.Parish = updatedContact.Parish;
            this.HCW = updatedContact.HCW;
            this.ContactHCWPosition = updatedContact.ContactHCWPosition;
            this.HCWFacility = updatedContact.HCWFacility;
            this.ContactHCWDistrict = updatedContact.ContactHCWDistrict;
            this.ContactHCWSC = updatedContact.ContactHCWSC;
            this.ContactHCWVillage = updatedContact.ContactHCWVillage;
        }

        public void UpdateContactFormDataOnly(ContactViewModel updatedContact)
        {
            this.AgeUnit = updatedContact.AgeUnit;
            this.Age = updatedContact.Age;
            this.District = updatedContact.District;
            this.Gender = updatedContact.Gender;
            this.LC1Chairman = updatedContact.LC1Chairman;
            this.HeadOfHousehold = updatedContact.HeadOfHousehold;
            this.HCW = updatedContact.HCW;
            this.HCWFacility = updatedContact.HCWFacility;
            this.Team = updatedContact.Team;
            this.OtherNames = updatedContact.OtherNames;
            this.SubCounty = updatedContact.SubCounty;
            this.Surname = updatedContact.Surname;
            this.Phone = updatedContact.Phone;
            this.ContactCDCID = updatedContact.ContactCDCID;
            this.ContactStateID = updatedContact.ContactStateID;
            this.PublicHealthAction = updatedContact.PublicHealthAction;
            this.RiskLevel = updatedContact.RiskLevel;
            this.Village = updatedContact.Village;
            this.Country = updatedContact.Country;
            this.Parish = updatedContact.Parish;
            this.HCW = updatedContact.HCW;
            this.ContactHCWPosition = updatedContact.ContactHCWPosition;
            this.HCWFacility = updatedContact.HCWFacility;
            this.ContactHCWDistrict = updatedContact.ContactHCWDistrict;
            this.ContactHCWSC = updatedContact.ContactHCWSC;
            this.ContactHCWVillage = updatedContact.ContactHCWVillage;
        }

        //public XElement Serialize()
        //{
        //    XElement recordElement =
        //        new XElement("Record",
        //            new XAttribute("GlobalRecordId", this.RecordId.ToString()));

        //    if(this.FirstSaveTime.HasValue) 
        //    {
        //        recordElement.Add(new XAttribute("FirstSaveTime", this.FirstSaveTime.Value.Ticks.ToString()));
        //    }
        //    else 
        //    {
        //        recordElement.Add(new XAttribute("FirstSaveTime", String.Empty));
        //    }

        //    string age = String.Empty;
        //    if (Age.HasValue)
        //    {
        //        age = Age.Value.ToString();
        //    }

        //    object ageUnit = String.Empty;
        //    if (AgeUnit.HasValue)
        //    {
        //        ageUnit = (int)AgeUnit.Value;
        //    }

        //    recordElement.Add(
        //        new XElement("Surname", this.Surname),
        //        new XElement("OtherNames", this.OtherNames),
        //        new XElement("Age", age),
        //        new XElement("AgeUnit", ageUnit),
        //        new XElement("Gender", this.Gender),
        //        new XElement("RiskLevel", this.RiskLevel),
        //        new XElement("HeadOfHousehold", this.HeadOfHousehold),
        //        new XElement("Village", this.Village),
        //        new XElement("SubCounty", this.SubCounty),
        //        new XElement("District", this.District),
        //        new XElement("LC1Chairman", this.LC1Chairman),
        //        new XElement("Phone", this.Phone),
        //        new XElement("HCW", this.HCW),
        //        new XElement("HCWFacility", this.HCWFacility),
        //        new XElement("FinalOutcome", this.FinalOutcome)
        //        );

        //    return recordElement;
        //}
        #endregion // Methods

        #region Deprecated / old code
        ///// <summary>
        ///// Updates a contact record in the database with the corresponding in-memory dataset
        ///// </summary>
        ///// <param name="contactVM">The contact to update</param>
        //private void UpdateContactInDatabase(Contact contact)
        //{
        //    string contactBaseTableName = ContactForm.TableName;
        //    IDbDriver db = this.Database;

        //    DateTime dtNow = DateTime.Now;
        //    dtNow = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, dtNow.Hour, dtNow.Minute, dtNow.Second, 0);

        //    string userID = CurrentUser;

        //    Query updateQuery = db.CreateQuery("UPDATE [" + contactBaseTableName + "] SET " +
        //        "[LastSaveLogonName] = @LastSaveLogonName, " +
        //        "[LastSaveTime] = @LastSaveTime " +
        //        "WHERE [GlobalRecordId] = @GlobalRecordId");
        //    updateQuery.Parameters.Add(new QueryParameter("@LastSaveLogonName", DbType.String, userID));
        //    updateQuery.Parameters.Add(new QueryParameter("@LastSaveTime", DbType.DateTime, dtNow));
        //    updateQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, contact.RecordId));

        //    int rows = db.ExecuteNonQuery(updateQuery);

        //    if (rows == 0)
        //    {
        //        throw new InvalidOperationException("Warning: Could not update data for this contact.");
        //    }

        //    string queryGender = String.Empty;

        //    if (contact.Gender == Core.Enums.Gender.Male) { queryGender = "1"; }
        //    else if (contact.Gender == Core.Enums.Gender.Female) { queryGender = "2"; }

        //    string queryHCW = String.Empty;

        //    if (contact.HCW == Properties.Resources.Yes) { queryHCW = "1"; }
        //    else if (contact.HCW == Properties.Resources.No) { queryHCW = "2"; }

        //    updateQuery = db.CreateQuery("UPDATE [" + ContactForm.Pages[0].TableName + "] SET " +
        //        "[ContactSurname] = @Surname, " +
        //        "[ContactOtherNames] = @OtherNames, " +
        //        "[ContactGender] = @Gender, " +
        //        "[ContactAge] = @Age, " +
        //        "[ContactAgeUnit] = @AgeUnit, " +
        //        "[ContactHeadHouse] = @HeadHouse, " +
        //        "[ContactVillage] = @VillageRes, " +
        //        "[ContactDistrict] = @DistrictRes, " +
        //        "[ContactSC] = @SCRes, " +
        //        "[LC1] = @LC1, " +
        //        "[ContactPhone] = @PhoneNumber, " +
        //        "[ContactHCW] = @HCW, " +
        //        "[ContactHCWFacility] = @HCWFacility, " +
        //        "[RiskLevel] = @RiskLevel, " +
        //        "[FinalOutcome] = @FinalOutcome " +

        //        "WHERE [GlobalRecordId] = @GlobalRecordId");

        //    updateQuery.Parameters.Add(new QueryParameter("@Surname", DbType.String, contact.Surname));
        //    updateQuery.Parameters.Add(new QueryParameter("@OtherNames", DbType.String, contact.OtherNames));
        //    updateQuery.Parameters.Add(new QueryParameter("@Gender", DbType.String, queryGender));
        //    if (contact.AgeYears.HasValue)
        //    {
        //        updateQuery.Parameters.Add(new QueryParameter("@Age", DbType.Double, contact.AgeYears));
        //    }
        //    else
        //    {
        //        updateQuery.Parameters.Add(new QueryParameter("@Age", DbType.Double, DBNull.Value));
        //    }

        //    if (contact.AgeUnit.HasValue)
        //    {
        //        updateQuery.Parameters.Add(new QueryParameter("@AgeUnit", DbType.String, contact.AgeUnit));
        //    }
        //    else
        //    {
        //        updateQuery.Parameters.Add(new QueryParameter("@AgeUnit", DbType.String, String.Empty));
        //    }

        //    updateQuery.Parameters.Add(new QueryParameter("@ContactHeadHouse", DbType.String, contact.HeadOfHousehold));
        //    updateQuery.Parameters.Add(new QueryParameter("@VillageRes", DbType.String, contact.Village));
        //    updateQuery.Parameters.Add(new QueryParameter("@DistrictRes", DbType.String, contact.District));
        //    updateQuery.Parameters.Add(new QueryParameter("@SCRes", DbType.String, contact.SubCounty));
        //    updateQuery.Parameters.Add(new QueryParameter("@LC1", DbType.String, contact.LC1Chairman));
        //    updateQuery.Parameters.Add(new QueryParameter("@PhoneNumber", DbType.String, contact.Phone));
        //    updateQuery.Parameters.Add(new QueryParameter("@ContactHCW", DbType.String, queryHCW));
        //    updateQuery.Parameters.Add(new QueryParameter("@ContactHCWFacility", DbType.String, contact.HCWFacility));
        //    updateQuery.Parameters.Add(new QueryParameter("@ContactRiskLevel", DbType.String, contact.RiskLevel));
        //    updateQuery.Parameters.Add(new QueryParameter("@ContactFinalOutcome", DbType.String, contact.FinalOutcome));
        //    updateQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, contact.RecordId));

        //    db.ExecuteNonQuery(updateQuery);
        //}

        ///// <summary>
        ///// Inserts a new contact record in the database with the corresponding in-memory dataset
        ///// </summary>
        ///// <param name="contactVM">The contact to add</param>
        //private void InsertContactInDatabase(Contact contact)
        //{
        //    string contactFormTableName = ContactForm.TableName;

        //    DateTime dtNow = DateTime.Now;
        //    dtNow = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, dtNow.Hour, dtNow.Minute, dtNow.Second, 0);

        //    string userID = CurrentUser;

        //    contact.FirstSaveTime = dtNow;

        //    Query insertQuery = Database.CreateQuery("INSERT INTO [" + ContactForm.TableName + "] (GlobalRecordId, RECSTATUS, FirstSaveLogonName, LastSaveLogonName, FirstSaveTime, LastSaveTime) VALUES (" +
        //        "@GlobalRecordId, @RECSTATUS, @FirstSaveLogonName, @LastSaveLogonName, @FirstSaveTime, @LastSaveTime)");
        //    insertQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, contact.RecordId));
        //    insertQuery.Parameters.Add(new QueryParameter("@RECSTATUS", DbType.Byte, 1));
        //    insertQuery.Parameters.Add(new QueryParameter("@FirstSaveLogonName", DbType.String, userID));
        //    insertQuery.Parameters.Add(new QueryParameter("@LastSaveLogonName", DbType.String, userID));
        //    insertQuery.Parameters.Add(new QueryParameter("@FirstSaveTime", DbType.DateTime, dtNow));
        //    insertQuery.Parameters.Add(new QueryParameter("@LastSaveTime", DbType.DateTime, dtNow));
        //    Database.ExecuteNonQuery(insertQuery);

        //    string queryGender = String.Empty;

        //    if (contact.Gender == Core.Enums.Gender.Male) { queryGender = "1"; }
        //    else if (contact.Gender == Core.Enums.Gender.Female) { queryGender = "2"; }

        //    string queryHCW = String.Empty;

        //    if (contact.HCW == Properties.Resources.Yes) { queryHCW = "1"; }
        //    else if (contact.HCW == Properties.Resources.No) { queryHCW = "2"; }

        //    foreach (Epi.Page page in ContactForm.Pages)
        //    {
        //        // contact form has only one page, so we can get away with this code for the time being.
        //        insertQuery = Database.CreateQuery("INSERT INTO [" + page.TableName + "] (GlobalRecordId, ContactSurname, ContactOtherNames, " +
        //            "ContactGender, ContactAge, ContactAgeUnit, ContactHeadHouse, ContactVillage, ContactDistrict, ContactSC, LC1, " +
        //            "ContactPhone, ContactHCW, ContactHCWFacility, RiskLevel, FinalOutcome) VALUES (" +
        //        "@GlobalRecordId, @ContactSurname, @ContactOtherNames, @ContactGender, @ContactAge, @ContactAgeUnit, @ContactHeadHouse, @ContactVillage, @ContactDistrict, " +
        //        "@ContactSC, @LC1, @ContactPhone, @ContactHCW, @ContactHCWFacility, @ContactRiskLevel, @ContactFinalOutcome)");
        //        insertQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, contact.RecordId));
        //        insertQuery.Parameters.Add(new QueryParameter("@ContactSurname", DbType.String, contact.Surname));
        //        insertQuery.Parameters.Add(new QueryParameter("@ContactOtherNames", DbType.String, contact.OtherNames));
        //        insertQuery.Parameters.Add(new QueryParameter("@ContactGender", DbType.String, queryGender));
        //        if (contact.AgeYears.HasValue)
        //        {
        //            insertQuery.Parameters.Add(new QueryParameter("@ContactAge", DbType.String, contact.AgeYears));
        //        }
        //        else
        //        {
        //            insertQuery.Parameters.Add(new QueryParameter("@ContactAge", DbType.String, DBNull.Value));
        //        }
        //        insertQuery.Parameters.Add(new QueryParameter("@ContactAgeUnit", DbType.String, contact.AgeUnit));
        //        insertQuery.Parameters.Add(new QueryParameter("@ContactHeadHouse", DbType.String, contact.HeadOfHousehold));
        //        insertQuery.Parameters.Add(new QueryParameter("@ContactVillage", DbType.String, contact.Village));
        //        insertQuery.Parameters.Add(new QueryParameter("@ContactDistrict", DbType.String, contact.District));
        //        insertQuery.Parameters.Add(new QueryParameter("@ContactSC", DbType.String, contact.SubCounty));
        //        insertQuery.Parameters.Add(new QueryParameter("@LC1", DbType.String, contact.LC1Chairman));
        //        insertQuery.Parameters.Add(new QueryParameter("@ContactPhone", DbType.String, contact.Phone));
        //        insertQuery.Parameters.Add(new QueryParameter("@ContactHCW", DbType.String, queryHCW));
        //        insertQuery.Parameters.Add(new QueryParameter("@ContactHCWFacility", DbType.String, contact.HCWFacility));
        //        insertQuery.Parameters.Add(new QueryParameter("@ContactRiskLevel", DbType.String, contact.RiskLevel));
        //        insertQuery.Parameters.Add(new QueryParameter("@ContactFinalOutcome", DbType.String, contact.FinalOutcome));
        //        Database.ExecuteNonQuery(insertQuery);
        //    }
        //}
        #endregion // Deprecated / old code

        #region Commands
        void UpdateExecute(ContactViewModel updatedContact)
        {
            UpdateContact(updatedContact);
        }

        public ICommand Update { get { return new RelayCommand<ContactViewModel>(UpdateExecute); } }
        #endregion
    }
}
