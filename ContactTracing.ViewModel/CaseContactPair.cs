using System;
using System.Xml.Linq;
using System.Data;
using Epi.Data;
using System.Windows.Input;
using ContactTracing.Core;

namespace ContactTracing.ViewModel
{
    /// <summary>
    /// A case and someone that case may have come into contact with.
    /// </summary>
    public class CaseContactPairViewModel : ObservableObject
    {
        #region Members
        private CaseViewModel _caseVM;
        private ContactViewModel _contactVM;
        private int? _contactType = 0;
        private DateTime _dateLastContact;
        #endregion // Members

        #region Properties
        public CaseViewModel CaseVM
        {
            get
            {
                return this._caseVM;
            }
            set
            {
                if (_caseVM != value)
                {
                    _caseVM = value;
                    RaisePropertyChanged("CaseVM");
                }
            }
        }
        public ContactViewModel ContactVM
        {
            get
            {
                return this._contactVM;
            }
            set
            {
                if (_contactVM != value)
                {
                    _contactVM = value;
                    RaisePropertyChanged("ContactVM");
                }
            }
        }

        #region Case Properties
        public bool IsActive
        {
            get { return true; }
        }
        public string CaseID
        {
            get { return CaseVM.ID; }
        }
        public string CaseSurname
        {
            get { return CaseVM.Surname; }
        }
        public string CaseOtherNames
        {
            get { return CaseVM.OtherNames; }
        }
        public string CaseRecordStatus
        {
            get { return CaseVM.RecordStatus; }
        }
        public double? CaseAge
        {
            get { return CaseVM.Age; }
        }
        public double? CaseAgeYears
        {
            get { return CaseVM.AgeYears; }
        }
        public DateTime? CaseDateOnset
        {
            get { return CaseVM.DateOnset; }
        }
        public string CaseVillage
        {
            get { return CaseVM.Village; }
        }
        public string CaseSubCounty
        {
            get { return CaseVM.SubCounty; }
        }
        public string CaseDistrict
        {
            get { return CaseVM.District; }
        }
        public string CaseDistrictOnset
        {
            get { return CaseVM.DistrictOnset; }
        }
        public DateTime? CaseDateIsolationCurrent
        {
            get { return CaseVM.DateIsolationCurrent; }
        }
        public DateTime? CaseDateDischargeIso
        {
            get { return CaseVM.DateDischargeIso; }
        }
        public DateTime? CaseDateDeath
        {
            get { return CaseVM.DateDeath; }
        }
        public string CaseCurrentStatus
        {
            get { return CaseVM.CurrentStatus; }
        }
        //public string CaseCurrentStatusLocalized
        //{
        //    get { return CaseVM.LocalizedCurrentStatus; }
        //}
        public string CaseIsolationCurrent
        {
            get { return CaseVM.IsolationCurrent; }
        }
        #endregion //Case Properties

        #region Contact Properties
        public string ContactSurname
        {
            get { return ContactVM.Surname; }
        }
        public string ContactOtherNames
        {
            get { return ContactVM.OtherNames; }
        }
        public string ContactGender
        {
            get { return ContactVM.Gender; }
        }
        public double? ContactAge
        {
            get { return ContactVM.Age; }
        }
        public double? ContactAgeYears
        {
            get { return ContactVM.AgeYears; }
        }
        public string ContactVillage
        {
            get { return ContactVM.Village; }
        }
        public string ContactSubCounty
        {
            get { return ContactVM.SubCounty; }
        }
        public string ContactDistrict
        {
            get { return ContactVM.District; }
        }
        public string ContactRiskLevel
        {
            get { return ContactVM.RiskLevel; }
        }
        public string ContactHeadOfHousehold
        {
            get { return ContactVM.HeadOfHousehold; }
        }
        public string ContactLC1Chairman
        {
            get { return ContactVM.LC1Chairman; }
        }
        public string ContactPhone
        {
            get { return ContactVM.Phone; }
        }
        public string ContactHCW
        {
            get { return ContactVM.HCW; }
        }
        public string ContactHCWFacility
        {
            get { return ContactVM.HCWFacility; }
        }
        //public bool ContactIsCase
        //{
        //    get { return ContactVM.IsCase; }
        //}

        public bool ContactIsActive
        {
            get { return ContactVM.IsActive; }
        }
        #endregion // Contact Properties

        public string ContactRecordId { get; set; }
        public DateTime DateLastContact
        {
            get
            {
                return this._dateLastContact;
            }
            set
            {
                if (_dateLastContact != value)
                {
                    _dateLastContact = new DateTime(value.Year, value.Month, value.Day, 0, 0, 0);
                    RaisePropertyChanged("DateLastContact");
                    RaisePropertyChanged("DateFirstFollowUp");
                    RaisePropertyChanged("DateLastFollowUp");
                }
            }
        }
        public DateTime DateFirstFollowUp 
        {
            get
            {
                return DateLastContact.AddDays(1);
            }
        }
        public DateTime DateLastFollowUp
        {
            get
            {
                return DateLastContact.AddDays(Core.Common.DaysInWindow);
            }
        }
        public string Relationship { get; set; }
        public bool IsContactDateEstimated { get; set; }
        public int? ContactType
        {
            get
            {
                return this._contactType;
            }
            set
            {
                this._contactType = value;
            }
        }

        public string Team { get; set; }

        public string ContactTypeString
        {
            get
            {
                if (ContactType != null)
                {
                    return Core.Common.ContactTypeConverter(ContactType.Value);
                    //Epi.WordBuilder wb = new Epi.WordBuilder(",");
                    //string bits = Convert.ToString(ContactType.Value, 2);
                    //switch (bits.Length)
                    //{
                    //    case 1:
                    //        bits = "000" + bits;
                    //        break;
                    //    case 2:
                    //        bits = "00" + bits;
                    //        break;
                    //    case 3:
                    //        bits = "0" + bits;
                    //        break;
                    //}

                    //if (bits[0].Equals('1'))
                    //{
                    //    wb.Add("1");
                    //}
                    //if (bits[1].Equals('1'))
                    //{
                    //    wb.Add("2");
                    //}
                    //if (bits[2].Equals('1'))
                    //{
                    //    wb.Add("3");
                    //}
                    //if (bits[3].Equals('1'))
                    //{
                    //    wb.Add("4");
                    //}

                    //return wb.ToString();
                }
                else
                {
                    return String.Empty;
                }
            }
        }

        #endregion // Properties

        #region Methods
        private void UpdatePair(CaseContactPairViewModel updatedPair)
        {
            this.Relationship = updatedPair.Relationship;
            this.ContactType = updatedPair.ContactType;
            this.IsContactDateEstimated = updatedPair.IsContactDateEstimated;
            this.DateLastContact = updatedPair.DateLastContact;
            //if (this.ContactVM.FollowUpWindowViewModel.WindowStartDate < this.DateLastContact)
            //{
            //    this.ContactVM.FollowUpWindowViewModel.WindowStartDate = this.DateLastContact;
            //}
            if (this.ContactVM.FollowUpWindowViewModel.WindowStartDate < this.DateFirstFollowUp)
            {
                this.ContactVM.FollowUpWindowViewModel.WindowStartDate = this.DateFirstFollowUp;
            }
        }

        /// <summary>
        /// Use to convert a contact's sources to a case's source cases, when that contact is converted to a case.
        /// </summary>
        /// <param name="database"></param>
        /// <param name="toViewId"></param>
        /// <param name="fromViewId"></param>
        /// <returns></returns>
        public Query GenerateInsertQueryForConversion(IDbDriver database, int caseFormId)
        {
            Query insertQuery = database.CreateQuery("INSERT INTO [metaLinks] (FromRecordGuid, ToRecordGuid, FromViewId, ToViewId, [" + ContactTracing.Core.Constants.LAST_CONTACT_DATE_COLUMN_NAME + "], IsEstimatedContactDate, ContactType, RelationshipType) VALUES (" +
                    "@CurrentCaseGuid, @ContactGuid, @FromViewId, @ToViewId, @LastContactDate, @IsEstimatedContactDate, @ContactType, @Relationship)");
            insertQuery.Parameters.Add(new QueryParameter("@CurrentCaseGuid", DbType.String, ContactRecordId));
            insertQuery.Parameters.Add(new QueryParameter("@ContactGuid", DbType.String, CaseVM.RecordId));
            insertQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, caseFormId));
            insertQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, caseFormId));
            insertQuery.Parameters.Add(new QueryParameter("@LastContactDate", DbType.DateTime, DateLastContact));
            insertQuery.Parameters.Add(new QueryParameter("@IsEstimatedContactDate", DbType.Boolean, IsContactDateEstimated));
            if (ContactType.HasValue)
            {
                insertQuery.Parameters.Add(new QueryParameter("@ContactType", DbType.Byte, ContactType));
            }
            else
            {
                insertQuery.Parameters.Add(new QueryParameter("@ContactType", DbType.Byte, DBNull.Value));
            }
            if (string.IsNullOrEmpty(Relationship))
            {
                insertQuery.Parameters.Add(new QueryParameter("@Relationship", DbType.String, DBNull.Value));
            }
            else
            {
                insertQuery.Parameters.Add(new QueryParameter("@Relationship", DbType.String, Relationship));
            }
            return insertQuery;
        }

        public Query GenerateInsertQuery(IDbDriver database, int toViewId, int fromViewId)
        {
            Query insertQuery = database.CreateQuery("INSERT INTO [metaLinks] (FromRecordGuid, ToRecordGuid, FromViewId, ToViewId, [" + ContactTracing.Core.Constants.LAST_CONTACT_DATE_COLUMN_NAME + "], IsEstimatedContactDate, ContactType, RelationshipType) VALUES (" +
                    "@CurrentCaseGuid, @ContactGuid, @FromViewId, @ToViewId, @LastContactDate, @IsEstimatedContactDate, @ContactType, @Relationship)");
            insertQuery.Parameters.Add(new QueryParameter("@CurrentCaseGuid", DbType.String, CaseVM.RecordId));
            insertQuery.Parameters.Add(new QueryParameter("@ContactGuid", DbType.String, ContactRecordId));
            insertQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, fromViewId));
            insertQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, toViewId));
            insertQuery.Parameters.Add(new QueryParameter("@LastContactDate", DbType.DateTime, DateLastContact));
            insertQuery.Parameters.Add(new QueryParameter("@IsEstimatedContactDate", DbType.Boolean, IsContactDateEstimated));
            if (ContactType.HasValue)
            {
                insertQuery.Parameters.Add(new QueryParameter("@ContactType", DbType.Byte, ContactType));
            }
            else
            {
                insertQuery.Parameters.Add(new QueryParameter("@ContactType", DbType.Byte, DBNull.Value));
            }
            if (string.IsNullOrEmpty(Relationship))
            {
                insertQuery.Parameters.Add(new QueryParameter("@Relationship", DbType.String, DBNull.Value));
            }
            else
            {
                insertQuery.Parameters.Add(new QueryParameter("@Relationship", DbType.String, Relationship));
            }
            return insertQuery;
        }

        public Query GenerateUpdateQuery(IDbDriver database, int toViewId, int fromViewId)
        {
            Query updateQuery = database.CreateQuery("UPDATE [metaLinks] SET " +
            "[LastContactDate] = @LastContactDate, " +
            "[ContactType] = @ContactType, " +
            "[RelationshipType] = @RelationshipType, " +
            "[IsEstimatedContactDate] = @IsEstimatedContactDate " +
            "WHERE " +
            "[ToViewId] = @ToViewId AND " +
            "[FromViewId] = @FromViewId AND " +
            "[ToRecordGuid] = @ToRecordGuid AND " +
            "[FromRecordGuid] = @FromRecordGuid");

            updateQuery.Parameters.Add(new QueryParameter("@LastContactDate", DbType.DateTime, DateLastContact));
            if (ContactType.HasValue)
            {
                updateQuery.Parameters.Add(new QueryParameter("@ContactType", DbType.Byte, ContactType));
            }
            else
            {
                updateQuery.Parameters.Add(new QueryParameter("@ContactType", DbType.Byte, DBNull.Value));
            }
            if (string.IsNullOrEmpty(Relationship))
            {
                updateQuery.Parameters.Add(new QueryParameter("@RelationshipType", DbType.String, DBNull.Value));
            }
            else
            {
                updateQuery.Parameters.Add(new QueryParameter("@RelationshipType", DbType.String, Relationship));
            }

            updateQuery.Parameters.Add(new QueryParameter("@IsEstimatedContactDate", DbType.Boolean, IsContactDateEstimated));

            updateQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, toViewId));
            updateQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, fromViewId));
            updateQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, ContactVM.RecordId));
            updateQuery.Parameters.Add(new QueryParameter("@FromRecordGuid", DbType.String, CaseVM.RecordId));

            return updateQuery;
        }

        public XElement Serialize(int toViewId, int fromViewId)
        {
            XElement recordElement =
                new XElement("Record");

            recordElement.Add(
                new XElement("FromRecordGuid", CaseVM.RecordId),
                new XElement("ToRecordGuid", ContactVM.RecordId),
                new XElement("FromViewId", fromViewId),
                new XElement("ToViewId", toViewId),
                new XElement("LastContactDate", this.DateLastContact),
                new XElement("ContactType", this.ContactType),
                new XElement("RelationshipType", this.Relationship),
                new XElement("IsEstimatedContactDate", this.IsContactDateEstimated)
                );

            return recordElement;
        }
        #endregion // Methods

        #region Commands
        void UpdateExecute(CaseContactPairViewModel updatedPair)
        {
            UpdatePair(updatedPair);
        }

        public ICommand Update { get { return new RelayCommand<CaseContactPairViewModel>(UpdateExecute); } }
        #endregion
    }
}
