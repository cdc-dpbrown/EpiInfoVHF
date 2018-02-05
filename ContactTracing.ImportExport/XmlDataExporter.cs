using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Epi;
using Epi.Data;
using Epi.Fields;
using Epi.ImportExport.Filters;
using ContactTracing.Core;
using ContactTracing.Core.Enums;

namespace ContactTracing.ImportExport
{
    public class XmlDataExporter
    {
        #region Members
        private DateTime _endDate = DateTime.MinValue;
        private readonly VhfProject _project;
        private readonly bool _includeContacts = true;
        private readonly RecordProcessingScope _scope = RecordProcessingScope.Undeleted;
        private readonly double _majorProgressIncrement = 1.0;
        protected HashSet<string> _caseGuids = null;
        protected List<string> _contactGuids = null;
        protected const string LAST_SAVE_TIME = " ((LastSaveTime >= @StartDate AND LastSaveTime <= @EndDate) OR LastSaveTime IS NULL) ";
        protected readonly RegionEnum _region = ApplicationViewModel.Instance.CurrentRegion;
        #endregion Members

        #region Properties
        public double MajorIncrement { get { return _majorProgressIncrement; } }
        protected string _database = "Access";
        private Dictionary<string, List<string>> FieldsToNull { get; set; }
        public bool DeIdentifyData { get; set; }
        protected double MajorProgress { get; set; }
        protected double MinorProgress { get; set; }
        public Dictionary<string, SyncFileFilters> Filters { get; set; }
        public VhfProject Project { get { return _project; } }
        public bool IncludeContacts { get { return _includeContacts; } }
        public RecordProcessingScope Scope { get { return _scope; } }
        public Epi.ImportExport.ProjectPackagers.ExportInfo ExportInfo { get; private set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate
        {
            get
            {
                return _endDate;
            }
            set
            {
                _endDate = new DateTime(value.Year, value.Month, value.Day, 23, 59, 59); // make sure end date captures all times on the given date
            }
        }
        protected int ContactFormId
        {
            get
            {
                return _project.Views[Core.Constants.CONTACT_FORM_NAME].Id;
            }
        }

        #endregion Properties

        #region Events
        public event SetProgressBarDelegate MajorProgressChanged;
        public event SetProgressBarDelegate MinorProgressChanged;
        public event UpdateStatusEventHandler MinorStatusChanged;
        #endregion Events

        #region Constructors
        public XmlDataExporter(VhfProject project, bool includeContacts, RecordProcessingScope recordProcessingScope)
        {
            ExportInfo = new Epi.ImportExport.ProjectPackagers.ExportInfo();

            _project = project;
            _includeContacts = includeContacts;
            _scope = recordProcessingScope;

            double totalMajorItems = 2;

            if (!_includeContacts)
            {
                totalMajorItems--;
            }

            Filters = new Dictionary<string, SyncFileFilters>();

            FieldsToNull = new Dictionary<string, List<string>>();

            foreach (View form in Project.Views)
            {
                FieldsToNull.Add(form.Name, new List<string>());
                Filters.Add(form.Name, new SyncFileFilters(Project.CollectedData.GetDatabase()));

                if (!_includeContacts && form.Name.Equals(Core.Constants.CONTACT_FORM_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                totalMajorItems++; // 1 for each form
            }

            StartDate = new DateTime(2000, 1, 1);
            EndDate = DateTime.Today.AddDays(3);

            _majorProgressIncrement = 100 / totalMajorItems;
        }
        #endregion Constructors

        #region Public Methods
        /// <summary>
        /// Gets all fields that will be excluded from the exported data set for a given form
        /// </summary>
        /// <param name="formName">The form to which the excluded fields belong</param>
        /// <returns>IEnumerable of string</returns>
        public IEnumerable<string> GetFieldsToNull(string formName)
        {
            return FieldsToNull[formName].AsEnumerable();
        }

        /// <summary>
        /// Adds multiple fields whose data should be excluded from the data package
        /// </summary>
        /// <param name="fieldNames">The list of field names to exclude</param>
        /// <param name="formName">The name of the form on which the fields are present</param>
        public void AddFieldsToNull(List<string> fieldNames, string formName)
        {
            // pre
            Contract.Requires(fieldNames != null);
            Contract.Requires(!String.IsNullOrEmpty(formName));

            if (!FieldsToNull.ContainsKey(formName))
            {
                FieldsToNull.Add(formName, new List<string>());
            }

            foreach (string fieldName in fieldNames)
            {
                if (!FieldsToNull[formName].Contains(fieldName))
                {
                    FieldsToNull[formName].Add(fieldName);
                }
            }
        }

        /// <summary>
        /// Adds a field whose data should be excluded from the data package
        /// </summary>
        /// <param name="fieldName">The name of the field to exclude</param>
        /// <param name="formName">The name of the form on which the field is present</param>
        public void AddFieldToNull(string fieldName, string formName)
        {
            // pre
            Contract.Requires(!String.IsNullOrEmpty(fieldName));
            Contract.Requires(!String.IsNullOrEmpty(formName));

            if (FieldsToNull.ContainsKey(formName))
            {
                if (!FieldsToNull[formName].Contains(fieldName))
                {
                    FieldsToNull[formName].Add(fieldName);
                }
            }
            else
            {
                FieldsToNull.Add(formName, new List<string>() { fieldName });
            }
        }

        /// <summary>
        /// Adds a field whose data should be excluded from the data package
        /// </summary>
        /// <param name="field">The field whose data should be excluded</param>
        public void AddFieldToNull(IField field)
        {
            // pre
            Contract.Requires(field != null);

            AddFieldToNull(field.Name, field.GetView().Name);
        }

        public virtual void WriteTo(string fileName)
        {
            ExportInfo.UserID = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString();
            ExportInfo.ExportInitiated = DateTime.Now;

            var sw = new Stopwatch();
            sw.Start();

            MajorProgress = 0;

            Epi.ApplicationIdentity appId = new Epi.ApplicationIdentity(typeof(Configuration).Assembly);
            System.Reflection.Assembly vhfAssembly = System.Reflection.Assembly.GetExecutingAssembly();

            DateTime dt = DateTime.UtcNow;
            CultureInfo invariantCulture = CultureInfo.InvariantCulture;
            string dateDisplayValue = string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0:s}", dt);

            using (FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                XmlWriterSettings settings = new XmlWriterSettings() { Async = false, Encoding = Encoding.Default, CheckCharacters = false, NewLineOnAttributes = false, Indent = true };

                using (XmlWriter writer = XmlWriter.Create(stream, settings))
                {
                    writer.WriteStartElement("DataPackage");
                    writer.WriteAttributeString("Version", appId.Version);
                    writer.WriteAttributeString("VhfVersion", vhfAssembly.GetName().Version.ToString());
                    writer.WriteAttributeString("CreatedUtc", dateDisplayValue);
                    writer.WriteAttributeString("CreatedDate", dt.ToString(invariantCulture.DateTimeFormat.ShortDatePattern));
                    writer.WriteAttributeString("Name", "sync");
                    writer.WriteAttributeString("Id", System.Guid.NewGuid().ToString());
                    writer.WriteAttributeString("SourceDbType", _database);
                    writer.WriteAttributeString("StartDate", StartDate.ToString(invariantCulture.DateTimeFormat.ShortDatePattern));
                    writer.WriteAttributeString("EndDate", EndDate.ToString(invariantCulture.DateTimeFormat.ShortDatePattern));

                    if (_region == RegionEnum.International)
                    {
                        writer.WriteAttributeString("Region", "International");
                    }
                    else
                    {
                        writer.WriteAttributeString("Region", "USA");
                    }

                    foreach (View form in Project.Views)
                    {
                        ExportInfo.RecordsPackaged.Add(form, 0);

                        if (!_includeContacts && form.Name.Equals(Core.Constants.CONTACT_FORM_NAME, StringComparison.OrdinalIgnoreCase)) 
                        {
                            continue;
                        }
                        WriteFormData(writer, form);
                        MajorProgress += _majorProgressIncrement;
                        OnMajorProgressChanged();
                        ExportInfo.FormsProcessed++;
                    }

                    WriteLinksData(writer);
                    MajorProgress += _majorProgressIncrement;
                    OnMajorProgressChanged();

                    if (_includeContacts)
                    {
                        WriteFollowUpsData(writer);
                        MajorProgress += _majorProgressIncrement;
                        OnMajorProgressChanged();
                    }

                    writer.WriteEndElement();
                }
            }

            OnMinorStatusChanged("All records written.");

            sw.Stop();
            Debug.Print(sw.Elapsed.TotalMilliseconds.ToString());

            ExportInfo.ExportCompleted = DateTime.Now;
            ExportInfo.TimeElapsed = ExportInfo.ExportCompleted - ExportInfo.ExportInitiated;
            ExportInfo.Succeeded = true;
        }
        #endregion Public Methods

        protected void OnMinorStatusChanged(string message)
        {
            if (MinorStatusChanged != null)
            {
                MinorStatusChanged(message);
            }
        }

        protected void OnMajorProgressChanged()
        {
            if (MajorProgressChanged != null)
            {
                MajorProgressChanged(MajorProgress);
            }
        }

        protected void OnMinorProgressChanged()
        {
            if (MinorProgressChanged != null)
            {
                MinorProgressChanged(MinorProgress);
            }
        }

        protected virtual Query GetFormSelectQuery(View form, bool selectAll = true)
        {
            IDbDriver db = form.Project.CollectedData.GetDatabase();

            string selectQueryText = String.Empty;
            Query selectQuery = null;

            SyncFileFilters filters = null;

            int count = 0;
            if (Filters != null && Filters.ContainsKey(form.Name))
            {
                foreach (Epi.ImportExport.IRowFilterCondition fc in Filters[form.Name])
                {
                    count++;
                }
            }

            if (count > 0)
            {
                filters = Filters[form.Name];
                filters.RecordProcessingScope = _scope;
                selectQuery = filters.GetGuidSelectQuery(form);

                List<QueryParameter> paramsToAdd = selectQuery.Parameters;
                if (selectAll)
                {
                    selectQuery = db.CreateQuery(selectQuery.SqlStatement.Replace("[t].[GlobalRecordId], [t].[FKEY], [t].[RECSTATUS]", "*") + " ORDER BY [t].[GlobalRecordId]");
                }
                else
                {
                    selectQuery = db.CreateQuery(selectQuery.SqlStatement.Replace("[t].[GlobalRecordId], [t].[FKEY], [t].[RECSTATUS]",
                        "[t].[GlobalRecordId], [t].[FKEY], [t].[RECSTATUS], [FirstSaveLogonName], [LastSaveLogonName], [FirstSaveTime], [LastSaveTime] ") + " ORDER BY [t].[GlobalRecordId]");
                }
                selectQuery.Parameters = paramsToAdd;
                //selectQuery.Parameters.Add(new QueryParameter("@StartDate", DbType.DateTime, StartDate));
                //selectQuery.Parameters.Add(new QueryParameter("@EndDate", DbType.DateTime, EndDate));
            }
            else
            {
                string recStatusClause = "RECSTATUS = 1";

                if (_scope == Epi.RecordProcessingScope.Both)
                {
                    recStatusClause = "RECSTATUS >= 0";
                }
                else if (_scope == Epi.RecordProcessingScope.Deleted)
                {
                    recStatusClause = "RECSTATUS = 0";
                }

                if (selectAll)
                {
                    selectQueryText = "SELECT * " + form.FromViewSQL + " WHERE " + recStatusClause + " ORDER BY [t].[GlobalRecordId]";
                }
                else
                {
                    selectQueryText = "SELECT [t].[GlobalRecordId], [FKEY], [RECSTATUS], [FirstSaveLogonName], [LastSaveLogonName], [FirstSaveTime], [LastSaveTime] " + form.FromViewSQL + " WHERE " + recStatusClause + " ORDER BY [t].[GlobalRecordId]";
                }
                selectQuery = db.CreateQuery(selectQueryText);
                //selectQuery.Parameters.Add(new QueryParameter("@StartDate", DbType.DateTime, StartDate));
                //selectQuery.Parameters.Add(new QueryParameter("@EndDate", DbType.DateTime, EndDate));
            }

            return selectQuery;
        }

        protected virtual void WriteFormMetadata(XmlWriter writer, View form)
        {
            writer.WriteStartElement("FieldMetadata");

            #region Field metadata

            foreach (Field field in form.Fields)
            {
                if (field is IDataField && field is RenderableField)
                {
                    RenderableField renderableField = field as RenderableField;
                    if (renderableField != null)
                    {
                        writer.WriteStartElement("FieldInfo");

                        writer.WriteAttributeString("Name", renderableField.Name);
                        writer.WriteAttributeString("FieldType", renderableField.FieldType.ToString());
                        writer.WriteAttributeString("Page", renderableField.Page.Position.ToString());

                        writer.WriteEndElement();
                    }
                }
            }

            #endregion // Field metadata

            writer.WriteEndElement(); // end FieldMetadata element
        }

        protected virtual void WriteFormPagedData(XmlWriter writer, View form)
        {
            MinorProgress = 0;
            OnMinorProgressChanged();

            List<string> fieldsToNull = FieldsToNull[form.Name];

            writer.WriteStartElement("Form");

            writer.WriteAttributeString("Name", form.Name);
            writer.WriteAttributeString("Pages", form.Pages.Count.ToString());
            writer.WriteAttributeString("IsRelatedForm", form.IsRelatedView.ToString());

            WriteFormMetadata(writer, form);

            writer.WriteStartElement("Data");

            IDbDriver db = Project.CollectedData.GetDatabase();

            using (OleDbConnection conn = new OleDbConnection(db.ConnectionString))
            {
                conn.Open();

                double totalRecords = -1;

                using (OleDbCommand command = new OleDbCommand("SELECT COUNT(*) FROM " + form.TableName, conn))
                {
                    totalRecords = Convert.ToDouble(command.ExecuteScalar());
                }

                double inc = 100 / totalRecords;

                //List<string> guids = new List<string>();
                HashSet<string> guids = new HashSet<string>();

                Query guidQuery = GetFormSelectQuery(form, false);
                using (IDataReader guidReader = db.ExecuteReader(guidQuery))
                {
                    int i = 0;
                    while (guidReader.Read())
                    {
                        string globalRecordId = guidReader["GlobalRecordId"].ToString();
                        //guids[i] = globalRecordId;
                        guids.Add(globalRecordId);
                        i++;
                    }
                }

                if (form.Name.Equals(Core.Constants.CASE_FORM_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    _caseGuids = guids;
                }

                HashSet<string> guidsWritten = new HashSet<string>();

                foreach (Page page in form.Pages)
                {
                    Query selectQuery = db.CreateQuery("SELECT * FROM " + page.TableName + " p INNER JOIN " + form.TableName + " f ON p.GlobalRecordId = f.GlobalRecordId");
                    using (OleDbCommand command = new OleDbCommand(selectQuery.SqlStatement, conn))
                    {
                        using (IDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string globalRecordId = reader["p.GlobalRecordId"].ToString();

                                if (!guids.Contains(globalRecordId)) 
                                {
                                    continue;
                                }

                                string lastSaveTimeStr = String.Empty;
                                long? lastSaveTimeLong = null;
                                DateTime? lastSaveTime = null;

                                if (reader["LastSaveTime"] != DBNull.Value)
                                {
                                    lastSaveTime = Convert.ToDateTime(reader["LastSaveTime"]);
                                    lastSaveTimeLong = lastSaveTime.Value.Ticks;
                                    lastSaveTimeStr = lastSaveTimeLong.ToString();

                                    if (lastSaveTime < StartDate || lastSaveTime > EndDate)
                                    {
                                        MinorProgress += inc;
                                        OnMinorProgressChanged();
                                        OnMinorStatusChanged(String.Format("{0} exported for " + form.Name + "...", (MinorProgress / 100).ToString("P0")));

                                        continue;
                                    }
                                }

                                if (!guidsWritten.Contains(globalRecordId))
                                {
                                    guidsWritten.Add(globalRecordId);
                                }

                                writer.WriteStartElement("Record");
                                writer.WriteAttributeString("Id", globalRecordId);
                                writer.WriteAttributeString("FKEY", reader["FKEY"] == DBNull.Value ? String.Empty : reader["FKEY"].ToString());
                                writer.WriteAttributeString("FirstSaveUserId", reader["FirstSaveLogonName"].ToString());
                                writer.WriteAttributeString("LastSaveUserId", reader["LastSaveLogonName"].ToString());
                                if (reader["FirstSaveTime"] != DBNull.Value)
                                {
                                    writer.WriteAttributeString("FirstSaveTime", Convert.ToDateTime(reader["FirstSaveTime"]).Ticks.ToString());
                                }
                                else
                                {
                                    writer.WriteAttributeString("FirstSaveTime", String.Empty);
                                }
                                writer.WriteAttributeString("LastSaveTime", lastSaveTimeStr);
                                writer.WriteAttributeString("RecStatus", reader["RecStatus"].ToString());

                                foreach (RenderableField field in page.Fields)
                                {
                                    if (field == null || fieldsToNull.Contains(field.Name) || !(field is IDataField))
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        if (reader[field.Name] != DBNull.Value && !String.IsNullOrEmpty(reader[field.Name].ToString()))
                                        {
                                            writer.WriteStartElement(field.Name);
                                            //writer.WriteAttributeString("Name", field.Name);
                                            WriteFieldValue(writer, reader, field);
                                            writer.WriteEndElement();
                                        }
                                    }
                                }

                                writer.WriteEndElement();
                            }
                        }
                    }
                }

                ExportInfo.RecordsPackaged[form] = guidsWritten.Count;
            }

            writer.WriteEndElement(); // data element
            writer.WriteEndElement(); // form element
        }

        protected virtual void WriteFormData(XmlWriter writer, View form)
        {
            if (form.Fields.DataFields.Count > Core.Constants.EXPORT_FIELD_LIMIT && Project.CollectedData.GetDatabase() is Epi.Data.Office.OleDbDatabase)
            {
                // OleDB can't handle a SELECT * with a lot of fields, so do page-by-page processing instead
                WriteFormPagedData(writer, form);
                return;
            }

            MinorProgress = 0;
            OnMinorProgressChanged();

            List<string> fieldsToNull = FieldsToNull[form.Name];

            writer.WriteStartElement("Form");

            writer.WriteAttributeString("Name", form.Name);
            writer.WriteAttributeString("Pages", form.Pages.Count.ToString());
            writer.WriteAttributeString("IsRelatedForm", form.IsRelatedView.ToString());

            WriteFormMetadata(writer, form);

            writer.WriteStartElement("Data");

            IDbDriver db = Project.CollectedData.GetDatabase();

            Query selectQuery = GetFormSelectQuery(form);

            List<string> labGuids = null;

            OnMinorStatusChanged("Applying filters for " + form.Name + "...");

            if (Filters.ContainsKey(Core.Constants.CASE_FORM_NAME) && Filters[Core.Constants.CASE_FORM_NAME].Count() > 0 && _caseGuids != null)
            {
                if (form.Name.Equals(Core.Constants.CONTACT_FORM_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    _contactGuids = new List<string>();

                    Query guidSelectQuery = db.CreateQuery("SELECT C.GlobalRecordId, M.FromRecordGuid FROM (ContactEntryForm C INNER JOIN metaLinks M ON C.GlobalRecordId = M.ToRecordGuid) WHERE M.ToViewId = @ToViewId AND M.FromViewId = 1");
                    guidSelectQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, ContactFormId));

                    using (IDataReader reader = db.ExecuteReader(guidSelectQuery))
                    {
                        while (reader.Read())
                        {
                            string caseGuid = reader["FromRecordGuid"].ToString();

                            if (_caseGuids.Contains(caseGuid))
                            {
                                _contactGuids.Add(reader["GlobalRecordId"].ToString());
                            }
                        }
                    }
                }
                else if (form.Name.Equals(Core.Constants.LAB_FORM_NAME))
                {
                    labGuids = new List<string>();

                    Query guidSelectQuery = db.CreateQuery("SELECT L.GlobalRecordId, L.FKEY FROM (CaseInformationForm C INNER JOIN LaboratoryResultsForm L ON C.GlobalRecordId = L.FKEY)");

                    using (IDataReader reader = db.ExecuteReader(guidSelectQuery))
                    {
                        while (reader.Read())
                        {
                            string labGuid = reader["GlobalRecordId"].ToString();
                            string caseGuid = reader["FKEY"].ToString();

                            if (_caseGuids.Contains(caseGuid))
                            {
                                labGuids.Add(reader["GlobalRecordId"].ToString());
                            }
                        }
                    }
                }
            }

            OnMinorStatusChanged("Getting total row counts for " + form.Name + "...");

            string recStatusClause = "RECSTATUS = 1";

            if (Scope == Epi.RecordProcessingScope.Both)
            {
                recStatusClause = "RECSTATUS >= 0";
            }
            else if (Scope == Epi.RecordProcessingScope.Deleted)
            {
                recStatusClause = "RECSTATUS = 0";
            }

            Query countQuery = db.CreateQuery("SELECT COUNT(*) FROM " + form.TableName + " WHERE " + recStatusClause + " AND ((LastSaveTime >= @StartDate AND LastSaveTime <= @EndDate) OR LastSaveTime IS NULL)");
            countQuery.Parameters.Add(new QueryParameter("@StartDate", DbType.DateTime, StartDate));
            countQuery.Parameters.Add(new QueryParameter("@EndDate", DbType.DateTime, EndDate));
            double totalRecords = Convert.ToDouble(db.ExecuteScalar(countQuery));

            double inc = 100 / totalRecords;

            bool isCaseForm = form.Name.Equals(Core.Constants.CASE_FORM_NAME, StringComparison.OrdinalIgnoreCase);
            if (isCaseForm) { _caseGuids = new HashSet<string>(); }

            using (IDataReader reader = db.ExecuteReader(selectQuery))
            {
                //int i = 1;

                while (reader.Read())
                {
                    string recordGuid = reader["t.GlobalRecordId"].ToString();
                    string lastSaveTimeStr = String.Empty;
                    long? lastSaveTimeLong = null;
                    DateTime? lastSaveTime = null;

                    if (reader["LastSaveTime"] != DBNull.Value)
                    {
                        lastSaveTime = Convert.ToDateTime(reader["LastSaveTime"]);
                        lastSaveTimeLong = lastSaveTime.Value.Ticks;
                        lastSaveTimeStr = lastSaveTimeLong.ToString();

                        if (lastSaveTime < StartDate || lastSaveTime > EndDate)
                        {
                            MinorProgress += inc;
                            OnMinorProgressChanged();
                            OnMinorStatusChanged(String.Format("{0} of records exported from " + form.Name + "...", (MinorProgress / 100).ToString("P0")));

                            if (isCaseForm)
                            {
                                // we want to add the GUID here so related records (e.g. contacts and labs) don't get excluded because
                                // their case wasn't in the date range.
                                _caseGuids.Add(recordGuid);
                            }

                            continue;
                        }
                    }

                    if (form.Name.Equals(Core.Constants.CONTACT_FORM_NAME, StringComparison.OrdinalIgnoreCase) &&_contactGuids != null && !_contactGuids.Contains(recordGuid)) { continue; }
                    if (form.Name.Equals(Core.Constants.LAB_FORM_NAME, StringComparison.OrdinalIgnoreCase) && labGuids != null && !labGuids.Contains(recordGuid)) { continue; }

                    writer.WriteStartElement("Record");
                    writer.WriteAttributeString("Id", recordGuid);
                    writer.WriteAttributeString("FKEY", reader["FKEY"] == DBNull.Value ? String.Empty : reader["FKEY"].ToString());
                    writer.WriteAttributeString("FirstSaveUserId", reader["FirstSaveLogonName"].ToString());
                    writer.WriteAttributeString("LastSaveUserId", reader["LastSaveLogonName"].ToString());

                    if (reader["FirstSaveTime"] != DBNull.Value)
                    {
                        writer.WriteAttributeString("FirstSaveTime", Convert.ToDateTime(reader["FirstSaveTime"]).Ticks.ToString());
                    }
                    else
                    {
                        writer.WriteAttributeString("FirstSaveTime", String.Empty);
                    }

                    writer.WriteAttributeString("LastSaveTime", lastSaveTimeStr);
                    writer.WriteAttributeString("RecStatus", reader["RecStatus"].ToString());

                    foreach (IDataField dataField in form.Fields.DataFields)
                    {
                        RenderableField field = dataField as RenderableField;

                        if (field == null || dataField is UniqueKeyField || fieldsToNull.Contains(field.Name))
                        {
                            continue;
                        }
                        else
                        {
                            if (reader[field.Name] != DBNull.Value && !String.IsNullOrEmpty(reader[field.Name].ToString()))
                            {
                                writer.WriteStartElement(field.Name);
                                WriteFieldValue(writer, reader, field);
                                writer.WriteEndElement();
                            }
                        }
                    }

                    writer.WriteEndElement(); // record

                    ExportInfo.RecordsPackaged[form]++;
                    
                    MinorProgress += inc;
                    OnMinorProgressChanged();

                    //OnMinorStatusChanged(String.Format("{1} ({0}) of records exported from " + form.Name + "...", i.ToString(), (MinorProgress / 100).ToString("P0")));
                    OnMinorStatusChanged(String.Format("{0} of records exported from " + form.Name + "...", (MinorProgress / 100).ToString("P0")));

                    //i++;
                }
            }

            writer.WriteEndElement(); // data element

            writer.WriteEndElement(); // form element
        }

        protected virtual void WriteFieldValue(XmlWriter writer, IDataReader reader, RenderableField field)
        {
            switch (field.FieldType)
            {
                case MetaFieldType.Date:
                case MetaFieldType.DateTime:
                case MetaFieldType.Time:
                    //writer.WriteString(Convert.ToDateTime(reader[field.Name]).Ticks.ToString());
                    writer.WriteString(Convert.ToDateTime(reader[field.Name]).ToString(CultureInfo.InvariantCulture.DateTimeFormat.ShortDatePattern));
                    break;
                case MetaFieldType.Checkbox:
                    writer.WriteString(Convert.ToBoolean(reader[field.Name]).ToString());
                    break;
                case MetaFieldType.CommentLegal:
                case MetaFieldType.LegalValues:
                case MetaFieldType.Codes:
                case MetaFieldType.Text:
                case MetaFieldType.TextUppercase:
                case MetaFieldType.PhoneNumber:
                case MetaFieldType.UniqueRowId:
                case MetaFieldType.ForeignKey:
                case MetaFieldType.GlobalRecordId:
                case MetaFieldType.Multiline:
                    writer.WriteString(reader[field.Name].ToString());
                    break;
                case MetaFieldType.Number:
                    writer.WriteString(Convert.ToDouble(reader[field.Name]).ToString(System.Globalization.CultureInfo.InvariantCulture));
                    break;
                case MetaFieldType.YesNo:
                case MetaFieldType.RecStatus:
                    writer.WriteString(reader[field.Name].ToString());
                    break;
                case MetaFieldType.GUID:
                    writer.WriteString(reader[field.Name].ToString());
                    break;
                case MetaFieldType.Option:
                    throw new ApplicationException(ImportExportSharedStrings.UNRECOGNIZED_FIELD_TYPE);
                case MetaFieldType.Image:
                    throw new ApplicationException(ImportExportSharedStrings.UNRECOGNIZED_FIELD_TYPE);
                default:
                    throw new ApplicationException(ImportExportSharedStrings.UNRECOGNIZED_FIELD_TYPE);
            }
        }

        protected virtual void WriteLinksData(XmlWriter writer)
        {
            MinorProgress = 0;
            OnMinorProgressChanged();

            writer.WriteStartElement("Links");

            string selectQueryText = "SELECT * FROM metaLinks";
            IDbDriver db = Project.CollectedData.GetDatabase();
            Query selectQuery = db.CreateQuery(selectQueryText);

            CultureInfo format = CultureInfo.InvariantCulture;

            double totalRecords = Convert.ToDouble(db.ExecuteScalar(db.CreateQuery("SELECT COUNT(*) FROM metaLinks")));
            double inc = 100 / totalRecords;

            bool filter = _contactGuids != null;
            string contactFormId = Project.Views[Core.Constants.CONTACT_FORM_NAME].Id.ToString();

            using (IDataReader reader = db.ExecuteReader(selectQuery))
            {
                while (reader.Read())
                {
                    string fromRecordGuid = reader["FromRecordGuid"].ToString();
                    string toRecordGuid = reader["ToRecordGuid"].ToString();
                    string toViewId = reader["ToViewId"].ToString();

                    if (!_includeContacts && toViewId == contactFormId)
                    {
                        continue;
                    }

                    if (filter && toViewId == contactFormId && !_contactGuids.Contains(toRecordGuid)) 
                    { 
                        continue; 
                    }

                    writer.WriteStartElement("Link");

                    #region Link Fields

                    writer.WriteStartElement("FromRecordGuid");
                    writer.WriteString(fromRecordGuid);
                    writer.WriteEndElement();

                    writer.WriteStartElement("ToRecordGuid");
                    writer.WriteString(toRecordGuid);
                    writer.WriteEndElement();

                    writer.WriteStartElement("FromViewId");
                    writer.WriteString(reader["FromViewId"].ToString());
                    writer.WriteEndElement();

                    writer.WriteStartElement("ToViewId");
                    writer.WriteString(toViewId);
                    writer.WriteEndElement();

                    writer.WriteStartElement("LastContactDate");
                    //writer.WriteString(Convert.ToDateTime(reader["LastContactDate"]).Ticks.ToString());
                    writer.WriteString(Convert.ToDateTime(reader["LastContactDate"]).ToString(format.DateTimeFormat.ShortDatePattern));
                    writer.WriteEndElement();

                    if (!String.IsNullOrEmpty(reader["ContactType"].ToString()))
                    {
                        writer.WriteStartElement("ContactType");
                        writer.WriteString(reader["ContactType"].ToString());
                        writer.WriteEndElement();
                    }

                    if (!String.IsNullOrEmpty(reader["RelationshipType"].ToString()))
                    {
                        writer.WriteStartElement("RelationshipType");
                        writer.WriteString(reader["RelationshipType"].ToString());
                        writer.WriteEndElement();
                    }

                    if (!String.IsNullOrEmpty(reader["Tentative"].ToString()))
                    {
                        writer.WriteStartElement("Tentative");
                        writer.WriteString(reader["Tentative"].ToString());
                        writer.WriteEndElement();
                    }

                    writer.WriteStartElement("IsEstimatedContactDate");
                    writer.WriteString(reader["IsEstimatedContactDate"].ToString());
                    writer.WriteEndElement();

                    for (int i = 1; i <= 21; i++)
                    {
                        string dayName = "Day" + i.ToString();
                        string dayNotesName = dayName + "Notes";

                        if (!String.IsNullOrEmpty(reader[dayName].ToString()))
                        {
                            writer.WriteStartElement(dayName);
                            writer.WriteString(reader[dayName].ToString());
                            writer.WriteEndElement();
                        }

                        if (!String.IsNullOrEmpty(reader[dayNotesName].ToString()))
                        {
                            writer.WriteStartElement(dayNotesName);
                            writer.WriteString(reader[dayNotesName].ToString());
                            writer.WriteEndElement();
                        }
                    }

                    writer.WriteStartElement("LinkId");
                    writer.WriteString(reader["LinkId"].ToString());
                    writer.WriteEndElement();

                    #endregion // Link Fields

                    writer.WriteEndElement();

                    MinorProgress += inc;
                    OnMinorProgressChanged();

                    OnMinorStatusChanged(String.Format("{0} of relationship records exported...", (MinorProgress / 100).ToString("P0")));
                }
            }

            writer.WriteEndElement();
        }

        protected virtual void WriteFollowUpsData(XmlWriter writer)
        {
            MinorProgress = 0;
            OnMinorProgressChanged();

            string selectQueryText = "SELECT * FROM metaHistory";
            IDbDriver db = Project.CollectedData.GetDatabase();

            CultureInfo format = CultureInfo.InvariantCulture;

            if(!db.TableExists("metaHistory")) { return; }

            writer.WriteStartElement("ContactFollowUps");

            Query selectQuery = db.CreateQuery(selectQueryText);

            double totalRecords = Convert.ToDouble(db.ExecuteScalar(db.CreateQuery("SELECT COUNT(*) FROM metaHistory")));
            double inc = 100 / totalRecords;

            bool filter = _contactGuids != null;
            string contactFormId = Project.Views[Core.Constants.CONTACT_FORM_NAME].Id.ToString();

            using (IDataReader reader = db.ExecuteReader(selectQuery))
            {
                while (reader.Read())
                {
                    string contactGuid = reader["ContactGUID"].ToString();

                    if (filter && !_contactGuids.Contains(contactGuid))
                    {
                        continue;
                    }

                    writer.WriteStartElement("ContactFollowUp");

                    #region Followup Fields

                    writer.WriteStartElement("ContactGUID");
                    writer.WriteString(contactGuid);
                    writer.WriteEndElement();

                    writer.WriteStartElement("FollowUpDate");
                    writer.WriteString(Convert.ToDateTime(reader["FollowUpDate"]).ToString(format.DateTimeFormat.ShortDatePattern));
                    writer.WriteEndElement();

                    if (!String.IsNullOrEmpty(reader["StatusOnDate"].ToString()))
                    {
                        writer.WriteStartElement("StatusOnDate");
                        writer.WriteString(reader["StatusOnDate"].ToString());
                        writer.WriteEndElement();
                    }

                    if (!String.IsNullOrEmpty(reader["Note"].ToString()))
                    {
                        writer.WriteStartElement("Note");
                        writer.WriteString(reader["Note"].ToString());
                        writer.WriteEndElement();
                    }

                    if (!String.IsNullOrEmpty(reader["Temp1"].ToString()))
                    {
                        writer.WriteStartElement("Temp1");
                        writer.WriteString(reader["Temp1"] == DBNull.Value ? String.Empty : Convert.ToDouble(reader["Temp1"]).ToString(System.Globalization.CultureInfo.InvariantCulture));
                        writer.WriteEndElement();
                    }

                    if (!String.IsNullOrEmpty(reader["Temp2"].ToString()))
                    {
                        writer.WriteStartElement("Temp2");
                        writer.WriteString(reader["Temp2"] == DBNull.Value ? String.Empty : Convert.ToDouble(reader["Temp2"]).ToString(System.Globalization.CultureInfo.InvariantCulture));
                        writer.WriteEndElement();
                    }

                    #endregion // Followup Fields

                    writer.WriteEndElement();

                    MinorProgress += inc;
                    OnMinorProgressChanged();

                    OnMinorStatusChanged(String.Format("{0} of contact tracing records exported...", (MinorProgress / 100).ToString("P0")));
                }
            }

            writer.WriteEndElement();
        }
    }
}
