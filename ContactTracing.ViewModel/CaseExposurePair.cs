using System;
using System.Data;
using Epi.Data;
using System.Windows.Input;
using ContactTracing.Core;

namespace ContactTracing.ViewModel
{
    /// <summary>
    /// A case and another case that was exposed.
    /// </summary>
    public class CaseExposurePairViewModel : ObservableObject
    {
        private CaseViewModel _sourceCaseVM;
        private CaseViewModel _exposedCaseVM;
        private int? _contactType = 0;
        private DateTime _dateLastContact = DateTime.Today;

        public CaseViewModel SourceCaseVM
        {
            get
            {
                return this._sourceCaseVM;
            }
            set
            {
                if (_sourceCaseVM != value)
                {
                    _sourceCaseVM = value;
                    RaisePropertyChanged("SourceCaseVM");
                }
            }
        }
        public CaseViewModel ExposedCaseVM
        {
            get
            {
                return this._exposedCaseVM;
            }
            set
            {
                if (_exposedCaseVM != value)
                {
                    _exposedCaseVM = value;
                    RaisePropertyChanged("ExposedCaseVM");
                }
            }
        }

        public string ID
        {
            get { return ExposedCaseVM.ID; }
        }
        public string Surname
        {
            get { return ExposedCaseVM.Surname; }
        }
        public string OtherNames
        {
            get { return ExposedCaseVM.OtherNames; }
        }
        public string RecordStatus
        {
            get { return ExposedCaseVM.RecordStatus; }
        }
        public double? Age
        {
            get { return ExposedCaseVM.Age; }
        }
        public double? AgeYears
        {
            get { return ExposedCaseVM.AgeYears; }
        }
        public DateTime? DateOnset
        {
            get { return ExposedCaseVM.DateOnset; }
        }
        public string Village
        {
            get { return ExposedCaseVM.Village; }
        }
        public string SubCounty
        {
            get { return ExposedCaseVM.SubCounty; }
        }
        public string District
        {
            get { return ExposedCaseVM.District; }
        }
        public string DistrictOnset
        {
            get { return ExposedCaseVM.DistrictOnset; }
        }
        public DateTime? DateIsolationCurrent
        {
            get { return ExposedCaseVM.DateIsolationCurrent; }
        }
        public DateTime? DateDischargeIso
        {
            get { return ExposedCaseVM.DateDischargeIso; }
        }
        public DateTime? DateDeath
        {
            get { return ExposedCaseVM.DateDeath; }
        }
        public string CurrentStatus
        {
            get { return ExposedCaseVM.CurrentStatus; }
        }
        public string IsolationCurrent
        {
            get { return ExposedCaseVM.IsolationCurrent; }
        }

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
                }
            }
        }
        public bool IsActive { get { return true; } }
        public bool IsTentative { get; set; }
        public bool IsContactDateEstimated { get; set; }

        public string Relationship { get; set; }
        //public int? ContactType { get; set; }

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

        public string ContactTypeString
        {
            get
            {
                if (ContactType != null)
                {
                    Epi.WordBuilder wb = new Epi.WordBuilder(",");
                    string bits = Convert.ToString(ContactType.Value, 2);
                    switch (bits.Length)
                    {
                        case 1:
                            bits = "000" + bits;
                            break;
                        case 2:
                            bits = "00" + bits;
                            break;
                        case 3:
                            bits = "0" + bits;
                            break;
                    }

                    if (bits[0].Equals('1'))
                    {
                        wb.Add("1");
                    }
                    if (bits[1].Equals('1'))
                    {
                        wb.Add("2");
                    }
                    if (bits[2].Equals('1'))
                    {
                        wb.Add("3");
                    }
                    if (bits[3].Equals('1'))
                    {
                        wb.Add("4");
                    }

                    return wb.ToString();
                }
                else
                {
                    return String.Empty;
                }
            }
        }

        public Query GenerateInsertQuery(IDbDriver database, int toViewId, int fromViewId, bool isForContactDuplicatedInCaseList = false)
        {
            Query insertQuery = database.CreateQuery("INSERT INTO [metaLinks] (FromRecordGuid, ToRecordGuid, FromViewId, ToViewId, [" + ContactTracing.Core.Constants.LAST_CONTACT_DATE_COLUMN_NAME + "], ContactType, RelationshipType, Tentative, IsEstimatedContactDate) VALUES (" +
                    "@CurrentCaseGuid, @ContactGuid, @FromViewId, @ToViewId, @LastContactDate, @ContactType, @Relationship, @Tentative, @IsEstimatedContactDate)");

            if (isForContactDuplicatedInCaseList)
            {
                insertQuery.Parameters.Add(new QueryParameter("@CurrentCaseGuid", DbType.String, ExposedCaseVM.RecordId));
                insertQuery.Parameters.Add(new QueryParameter("@ContactGuid", DbType.String, SourceCaseVM.RecordId));
            }
            else
            {
                insertQuery.Parameters.Add(new QueryParameter("@CurrentCaseGuid", DbType.String, SourceCaseVM.RecordId));
                insertQuery.Parameters.Add(new QueryParameter("@ContactGuid", DbType.String, ExposedCaseVM.RecordId));
            }
            insertQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, fromViewId));
            insertQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, toViewId));
            insertQuery.Parameters.Add(new QueryParameter("@LastContactDate", DbType.DateTime, DateLastContact));
            
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
            if (IsTentative)
            {
                insertQuery.Parameters.Add(new QueryParameter("@Tentative", DbType.Byte, 1));
            }
            else
            {
                insertQuery.Parameters.Add(new QueryParameter("@Tentative", DbType.Byte, 0));
            }

            insertQuery.Parameters.Add(new QueryParameter("@IsEstimatedContactDate", DbType.Boolean, IsContactDateEstimated));

            return insertQuery;
        }

        public Query GenerateInsertQueryForConversion(IDbDriver database, int toViewId, int fromViewId)
        {
            Query insertQuery = database.CreateQuery("INSERT INTO [metaLinks] (FromRecordGuid, ToRecordGuid, FromViewId, ToViewId, [" + ContactTracing.Core.Constants.LAST_CONTACT_DATE_COLUMN_NAME + "], ContactType, RelationshipType, Tentative, IsEstimatedContactDate) VALUES (" +
                    "@CurrentCaseGuid, @ContactGuid, @FromViewId, @ToViewId, @LastContactDate, @ContactType, @Relationship, @Tentative, @IsEstimatedContactDate)");
            insertQuery.Parameters.Add(new QueryParameter("@CurrentCaseGuid", DbType.String, ExposedCaseVM.RecordId));
            insertQuery.Parameters.Add(new QueryParameter("@ContactGuid", DbType.String, SourceCaseVM.RecordId));
            insertQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, fromViewId));
            insertQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, toViewId));
            insertQuery.Parameters.Add(new QueryParameter("@LastContactDate", DbType.DateTime, DateLastContact));

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
            if (IsTentative)
            {
                insertQuery.Parameters.Add(new QueryParameter("@Tentative", DbType.Byte, 1));
            }
            else
            {
                insertQuery.Parameters.Add(new QueryParameter("@Tentative", DbType.Byte, 0));
            }

            insertQuery.Parameters.Add(new QueryParameter("@IsEstimatedContactDate", DbType.Boolean, IsContactDateEstimated));

            return insertQuery;
        }

        public Query GenerateUpdateQuery(IDbDriver database, int toViewId, int fromViewId)
        {
            Query updateQuery = database.CreateQuery("UPDATE [metaLinks] SET " +
            "[LastContactDate] = @LastContactDate, " +
            "[ContactType] = @ContactType, " +
            "[RelationshipType] = @RelationshipType, " +
            "[IsEstimatedContactDate] = @IsEstimatedContactDate, " +
            "[Tentative] = @Tentative " +
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
            switch(IsTentative) 
            {
                case true:
                    updateQuery.Parameters.Add(new QueryParameter("@Tentative", DbType.Byte, 1));
                    break;
                case false:
                    updateQuery.Parameters.Add(new QueryParameter("@Tentative", DbType.Byte, 0));
                    break;
            }

            updateQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, toViewId));
            updateQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, fromViewId));
            updateQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, ExposedCaseVM.RecordId));
            updateQuery.Parameters.Add(new QueryParameter("@FromRecordGuid", DbType.String, SourceCaseVM.RecordId));

            return updateQuery;
        }

        public Query GenerateDeleteQuery(IDbDriver database, int toViewId, int fromViewId)
        {
            string queryString = "DELETE * FROM [metaLinks] WHERE " +
                "[FromRecordGuid] = @FromRecordGuid AND [ToRecordGuid] = @ToRecordGuid AND [FromViewId] = @FromViewId AND [ToViewId] = @ToViewId";
            if (database.ToString().ToLower().Contains("sql"))
            {
                queryString = "DELETE FROM [metaLinks] WHERE " +
                "[FromRecordGuid] = @FromRecordGuid AND [ToRecordGuid] = @ToRecordGuid AND [FromViewId] = @FromViewId AND [ToViewId] = @ToViewId";
            }

            Query deleteQuery = database.CreateQuery(queryString);
            deleteQuery.Parameters.Add(new QueryParameter("@FromRecordGuid", DbType.String, SourceCaseVM.RecordId));
            deleteQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, ExposedCaseVM.RecordId));
            deleteQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, fromViewId));
            deleteQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, toViewId));
            return deleteQuery;
        }

        private void UpdatePair(CaseExposurePairViewModel updatedPair)
        {
            this.Relationship = updatedPair.Relationship;
            this.ContactType = updatedPair.ContactType;
            this.IsContactDateEstimated = updatedPair.IsContactDateEstimated;
            this.IsTentative = updatedPair.IsTentative;
            this.DateLastContact = updatedPair.DateLastContact;
        }

        #region Commands
        void UpdateExecute(CaseExposurePairViewModel updatedPair)
        {
            UpdatePair(updatedPair);
        }

        public ICommand Update { get { return new RelayCommand<CaseExposurePairViewModel>(UpdateExecute); } }
        #endregion
    }
}
