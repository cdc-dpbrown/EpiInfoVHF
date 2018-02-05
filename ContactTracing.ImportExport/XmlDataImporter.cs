using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Epi;
using Epi.Data;
using Epi.Fields;
using Epi.ImportExport.Filters;
using ContactTracing.Core;
using ContactTracing.Core.Enums;

namespace ContactTracing.ImportExport
{
    public class XmlDataImporter
    {
        #region Members
        private readonly VhfProject _project;
        //private readonly bool _includeContacts = true;
        private readonly RecordProcessingScope _scope = RecordProcessingScope.Undeleted;
        //private readonly double _majorProgressIncrement = 1.0;
        protected string _sourceDbType = String.Empty;
        protected readonly RegionEnum _region = ApplicationViewModel.Instance.CurrentRegion;
        #endregion // Members

        #region Events
        public event SetProgressBarDelegate MajorProgressChanged;
        public event SetProgressBarDelegate MinorProgressChanged;
        #endregion // Events

        #region Properties
        protected double MajorProgress { get; set; }
        protected double MinorProgress { get; set; }
        public VhfProject Project { get { return _project; } }
        public RecordProcessingScope Scope { get { return _scope; } }
        public bool Update { get; set; }
        public bool Append { get; set; }

        /// <summary>
        /// Gets/sets whether to soft-delete records during the import.
        /// </summary>
        public bool Delete { get; set; }

        /// <summary>
        /// Gets/sets whether to undo soft-deletions of records during the import.
        /// </summary>
        public bool Undelete { get; set; }
        #endregion // Properties

        #region Constructors
        public XmlDataImporter(VhfProject project, RecordProcessingScope scope)
        {
            _project = project;
            _scope = scope;
            Update = true;
            Append = true;
        }
        #endregion // Constructors

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

        #region Relationships and Follow-ups
        protected virtual void ImportLinkData(XElement linksElement)
        {
            var sw = new Stopwatch();
            sw.Start();

            MinorProgress = 0;

            double inc = 100.0 / (double)linksElement.Elements().Count();

            IDbDriver db = Project.CollectedData.GetDatabase();

            HashSet<string> guids = new HashSet<string>();
            HashSet<string> records = new HashSet<string>();

            CultureInfo format = CultureInfo.InvariantCulture;

            using (IDbConnection conn = db.GetConnection())
            {
                conn.Open();
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM [metaLinks]";
                    using (IDataReader reader = command.ExecuteReader())// db.ExecuteReader(selectQuery))
                    {
                        while (reader.Read())
                        {
                            guids.Add(reader.GetString(reader.GetOrdinal("FromRecordGuid")) + ";" + reader.GetString(reader.GetOrdinal("ToRecordGuid")) + ";" + reader.GetInt32(reader.GetOrdinal("ToViewId")).ToString() + ";" +
                                reader.GetInt32(reader.GetOrdinal("FromViewId")).ToString());

                            WordBuilder wb = new WordBuilder(";");

                            for (int i = 0; i < reader.FieldCount; i++) 
                            {
                                string columnName = reader.GetName(i);
                                string val = reader[i].ToString();

                                if (!String.IsNullOrEmpty(val) && !columnName.Equals("LinkId", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (!columnName.Equals(Core.Constants.LAST_CONTACT_DATE_COLUMN_NAME, StringComparison.OrdinalIgnoreCase))
                                    {
                                        wb.Add(reader[i].ToString());
                                    }
                                    else
                                    {
                                        DateTime dt = reader.GetDateTime(i);
                                        wb.Add(dt.ToString(format.DateTimeFormat.ShortDatePattern));
                                    }
                                }
                            }

                            records.Add(wb.ToString());
                        }
                    }
                }
                conn.Close();
            }

            using (IDbConnection conn = db.GetConnection())
            {
                conn.Open();

                foreach (XElement link in linksElement.Elements())
                {
                    string fromRecordGuid = link.Element("FromRecordGuid").Value;
                    string toRecordGuid = link.Element("ToRecordGuid").Value;
                    string fromViewId = link.Element("FromViewId").Value;
                    string toViewId = link.Element("ToViewId").Value;

                    bool found = false;

                    string comparer = fromRecordGuid + ";" + toRecordGuid + ";" + toViewId + ";" + fromViewId;

                    if (guids.Contains(comparer))
                    {
                        found = true;
                    }

                    if (found && Update)
                    {
                        WordBuilder wb = new WordBuilder(";");

                        foreach (XElement field in link.Elements()) 
                        {
                            if (field.Name.ToString().Equals("LinkId", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                            wb.Add(field.Value);
                        }

                        string comparer2 = wb.ToString();

                        if (!records.Contains(comparer2))
                        {
                            // update
                            UpdateLink(conn, link, db, fromRecordGuid, toRecordGuid, Int32.Parse(fromViewId), Int32.Parse(toViewId));
                        }
                    }
                    else if (!found && Append)
                    {
                        InsertLink(conn, link, db, fromRecordGuid, toRecordGuid, Int32.Parse(fromViewId), Int32.Parse(toViewId));
                    }

                    MinorProgress = MinorProgress + inc;
                    OnMinorProgressChanged();
                }

                conn.Close();
            }

            sw.Stop();
            Debug.Print("ImportLinkData Complete. Elapsed: " + sw.Elapsed.TotalMilliseconds + " ms.");
        }

        protected virtual void InsertFollowUp(IDbConnection conn, XElement followUp, IDbDriver db, string contactGuid, DateTime followUpDate)
        {
            WordBuilder wbFieldNames = new WordBuilder(", ");
            WordBuilder wbParamNames = new WordBuilder(", ");
            List<QueryParameter> parameters = new List<QueryParameter>();

            foreach (XElement field in followUp.Elements())
            {
                string fieldName = field.Name.ToString();

                if (fieldName.Equals("ContactGUID", StringComparison.OrdinalIgnoreCase) ||
                    fieldName.Equals("FollowUpDate", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                wbFieldNames.Add("[" + fieldName + "]");
                wbParamNames.Add("@" + fieldName);

                if (fieldName.Equals("StatusOnDate", StringComparison.OrdinalIgnoreCase))
                {
                    parameters.Add(new QueryParameter("@" + fieldName, DbType.Int32, Int32.Parse(field.Value)));
                }
                else if (fieldName.Equals("Note", StringComparison.OrdinalIgnoreCase))
                {
                    parameters.Add(new QueryParameter("@" + fieldName, DbType.String, field.Value));
                }
                else if (fieldName.Equals("Temp1", StringComparison.OrdinalIgnoreCase) || fieldName.Equals("Temp2", StringComparison.OrdinalIgnoreCase))
                {
                    parameters.Add(new QueryParameter("@" + fieldName, DbType.Double, Double.Parse(field.Value, CultureInfo.InvariantCulture)));
                }
            }

            string queryText = "INSERT INTO [metaHistory] (ContactGUID, FollowUpDate, " + wbFieldNames.ToString() + ") VALUES (@ContactGUID, @FollowUpDate, " + wbParamNames.ToString() + ")";
            if (parameters.Count == 0)
            {
                queryText = "INSERT INTO [metaHistory] (ContactGUID, FollowUpDate) VALUES (@ContactGUID, @FollowUpDate)";
            }

            Query insertQuery = db.CreateQuery(queryText);

            insertQuery.Parameters.Add(new QueryParameter("@ContactGUID", DbType.Guid, new Guid(contactGuid)));
            insertQuery.Parameters.Add(new QueryParameter("@FollowUpDate", DbType.DateTime, followUpDate));

            foreach (QueryParameter p in parameters)
            {
                insertQuery.Parameters.Add(p);
            }

            using (IDbCommand command = GetCommand(insertQuery.SqlStatement, conn, insertQuery.Parameters))
            {
                object obj = command.ExecuteNonQuery();
            }
        }

        protected virtual void InsertLink(IDbConnection conn, XElement link, IDbDriver db, string fromRecordGuid, string toRecordGuid, int fromViewId, int toViewId)
        {
            WordBuilder wbFieldNames = new WordBuilder(", ");
            WordBuilder wbParamNames = new WordBuilder(", ");
            List<QueryParameter> parameters = new List<QueryParameter>();

            bool addEstimatedContactDate = true;

            foreach (XElement field in link.Elements())
            {
                string fieldName = field.Name.ToString();

                if (fieldName.Equals("FromRecordGuid", StringComparison.OrdinalIgnoreCase) ||
                    fieldName.Equals("ToRecordGuid", StringComparison.OrdinalIgnoreCase) ||
                    fieldName.Equals("FromViewId", StringComparison.OrdinalIgnoreCase) ||
                    fieldName.Equals("ToViewId", StringComparison.OrdinalIgnoreCase) ||
                    fieldName.Equals("LinkId", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                wbFieldNames.Add(fieldName);
                wbParamNames.Add("@" + fieldName);

                if (fieldName.Equals("LastContactDate", StringComparison.OrdinalIgnoreCase))
                {
                    //parameters.Add(new QueryParameter("@" + fieldName, DbType.DateTime, new DateTime(long.Parse(field.Value))));
                    parameters.Add(new QueryParameter("@" + fieldName, DbType.DateTime, DateTime.Parse(field.Value, CultureInfo.InvariantCulture)));
                }
                else if (fieldName.Equals("ContactType", StringComparison.OrdinalIgnoreCase))
                {
                    parameters.Add(new QueryParameter("@" + fieldName, DbType.Int32, Int32.Parse(field.Value)));
                }
                else if (fieldName.Equals("RelationshipType", StringComparison.OrdinalIgnoreCase))
                {
                    parameters.Add(new QueryParameter("@" + fieldName, DbType.String, field.Value));
                }
                else if (fieldName.Equals("Tentative", StringComparison.OrdinalIgnoreCase))
                {
                    parameters.Add(new QueryParameter("@" + fieldName, DbType.Byte, Byte.Parse(field.Value)));
                }
                else if (fieldName.Equals("IsEstimatedContactDate", StringComparison.OrdinalIgnoreCase))
                {
                    parameters.Add(new QueryParameter("@" + fieldName, DbType.Boolean, Boolean.Parse(field.Value)));
                    addEstimatedContactDate = false;
                }
                else if (fieldName.StartsWith("Day", StringComparison.OrdinalIgnoreCase) && fieldName.EndsWith("Notes", StringComparison.OrdinalIgnoreCase))
                {
                    parameters.Add(new QueryParameter("@" + fieldName, DbType.String, field.Value));
                }
                else if (fieldName.StartsWith("Day", StringComparison.OrdinalIgnoreCase))
                {
                    parameters.Add(new QueryParameter("@" + fieldName, DbType.Int32, Int32.Parse(field.Value)));
                }
            }

            if (addEstimatedContactDate)
            {
                wbFieldNames.Add("IsEstimatedContactDate");
                wbParamNames.Add("@IsEstimatedContactDate");
                parameters.Add(new QueryParameter("@IsEstimatedContactDate", DbType.Boolean, false));
            }

            Query insertQuery = db.CreateQuery("INSERT INTO [metaLinks] (FromRecordGuid, ToRecordGuid, FromViewId, ToViewId, " + wbFieldNames.ToString() + ") VALUES (@FromRecordGuid, @ToRecordGuid, @FromViewId, @ToViewId, " + wbParamNames.ToString() + ")");
            insertQuery.Parameters.Add(new QueryParameter("@FromRecordGuid", DbType.String, fromRecordGuid));
            insertQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, toRecordGuid));
            insertQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, fromViewId));
            insertQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, toViewId));

            foreach (QueryParameter parameter in parameters)
            {
                insertQuery.Parameters.Add(parameter);
            }

            using (IDbCommand command = GetCommand(insertQuery.SqlStatement, conn, insertQuery.Parameters))
            {
                object obj = command.ExecuteNonQuery();
            }
        }

        protected virtual void UpdateFollowUp(IDbConnection conn, XElement followUp, IDbDriver db, string contactGuid, DateTime followUpDate)
        {
            WordBuilder wb = new WordBuilder(", ");
            List<QueryParameter> parameters = new List<QueryParameter>();

            foreach (XElement field in followUp.Elements())
            {
                string fieldName = field.Name.ToString();

                if (fieldName.Equals("ContactGUID", StringComparison.OrdinalIgnoreCase) ||
                    fieldName.Equals("FollowUpDate", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                wb.Add("[" + fieldName + "] = @" + fieldName);

                if (fieldName.Equals("StatusOnDate", StringComparison.OrdinalIgnoreCase))
                {
                    parameters.Add(new QueryParameter("@" + fieldName, DbType.Int32, Int32.Parse(field.Value)));
                }
                else if (fieldName.Equals("Note", StringComparison.OrdinalIgnoreCase))
                {
                    parameters.Add(new QueryParameter("@" + fieldName, DbType.String, field.Value));
                }
                else if (fieldName.Equals("Temp1", StringComparison.OrdinalIgnoreCase) || fieldName.Equals("Temp2", StringComparison.OrdinalIgnoreCase))
                {
                    parameters.Add(new QueryParameter("@" + fieldName, DbType.Double, Double.Parse(field.Value, CultureInfo.InvariantCulture)));
                }
            }

            if (parameters.Count == 0)
            {
                return;
            }

            Query updateQuery = db.CreateQuery("UPDATE [metaHistory] SET " + wb.ToString() + " WHERE [ContactGUID] = @ContactGUID AND [FollowUpDate] = @FollowUpDate");

            foreach (QueryParameter p in parameters)
            {
                updateQuery.Parameters.Add(p);
            }

            updateQuery.Parameters.Add(new QueryParameter("@ContactGUID", DbType.Guid, new Guid(contactGuid)));
            updateQuery.Parameters.Add(new QueryParameter("@FollowUpDate", DbType.DateTime, followUpDate));

            using (IDbCommand command = GetCommand(updateQuery.SqlStatement, conn, updateQuery.Parameters))
            {
                object obj = command.ExecuteNonQuery();
            }
        }

        protected virtual void UpdateLink(IDbConnection conn, XElement link, IDbDriver db, string fromRecordGuid, string toRecordGuid, int fromViewId, int toViewId)
        {
            WordBuilder wb = new WordBuilder(", ");
            List<QueryParameter> parameters = new List<QueryParameter>();

            foreach (XElement field in link.Elements())
            {
                string fieldName = field.Name.ToString();

                if(fieldName.Equals("FromRecordGuid", StringComparison.OrdinalIgnoreCase) || 
                    fieldName.Equals("ToRecordGuid", StringComparison.OrdinalIgnoreCase) || 
                    fieldName.Equals("FromViewId", StringComparison.OrdinalIgnoreCase) ||
                    fieldName.Equals("ToViewId", StringComparison.OrdinalIgnoreCase) ||
                    fieldName.Equals("LinkId", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                wb.Add(fieldName + " = @" + fieldName);

                if (fieldName.Equals("LastContactDate", StringComparison.OrdinalIgnoreCase))
                {
                    //parameters.Add(new QueryParameter("@" + fieldName, DbType.DateTime, new DateTime(long.Parse(field.Value))));
                    parameters.Add(new QueryParameter("@" + fieldName, DbType.DateTime, DateTime.Parse(field.Value, CultureInfo.InvariantCulture)));
                }
                else if (fieldName.Equals("ContactType", StringComparison.OrdinalIgnoreCase))
                {
                    parameters.Add(new QueryParameter("@" + fieldName, DbType.Int32, Int32.Parse(field.Value)));
                }
                else if (fieldName.Equals("RelationshipType", StringComparison.OrdinalIgnoreCase))
                {
                    parameters.Add(new QueryParameter("@" + fieldName, DbType.String, field.Value));
                }
                else if (fieldName.Equals("Tentative", StringComparison.OrdinalIgnoreCase))
                {
                    parameters.Add(new QueryParameter("@" + fieldName, DbType.Byte, Byte.Parse(field.Value)));
                }
                else if (fieldName.Equals("IsEstimatedContactDate", StringComparison.OrdinalIgnoreCase))
                {
                    parameters.Add(new QueryParameter("@" + fieldName, DbType.Boolean, Boolean.Parse(field.Value)));
                }
                else if (fieldName.StartsWith("Day", StringComparison.OrdinalIgnoreCase) && fieldName.EndsWith("Notes", StringComparison.OrdinalIgnoreCase))
                {
                    parameters.Add(new QueryParameter("@" + fieldName, DbType.String, field.Value));
                }
                else if (fieldName.StartsWith("Day", StringComparison.OrdinalIgnoreCase))
                {
                    parameters.Add(new QueryParameter("@" + fieldName, DbType.Int32, Int32.Parse(field.Value)));
                }
            }

            if (parameters.Count == 0)
            {
                return;
            }

            Query updateQuery = db.CreateQuery("UPDATE [metaLinks] SET " + wb.ToString() + " WHERE [ToRecordGuid] = @ToRecordGuid AND [FromRecordGuid] = @FromRecordGuid AND [ToViewId] = @ToViewId AND " +
                    "[FromViewId] = @FromViewId");

            foreach (QueryParameter p in parameters)
            {
                updateQuery.Parameters.Add(p);
            }

            updateQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, toRecordGuid));
            updateQuery.Parameters.Add(new QueryParameter("@FromRecordGuid", DbType.String, fromRecordGuid));
            updateQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, toViewId));
            updateQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, fromViewId));

            //if (Project.CollectedDataDriver.ToLower().Contains("epi.data.office"))
            //{
                using (IDbCommand command = GetCommand(updateQuery.SqlStatement, conn, updateQuery.Parameters))
                {
                    object obj = command.ExecuteNonQuery();
                }
            
            //}
            //else
            //{
            //    db.ExecuteNonQuery(updateQuery);
            //}
        }

        protected virtual void ImportFollowUpData(XElement followUpsElement)
        {
            var sw = new Stopwatch();
            sw.Start();

            MinorProgress = 0;

            double inc = 100.0 / (double)followUpsElement.Elements().Count();

            IDbDriver db = Project.CollectedData.GetDatabase();

            using (IDbConnection conn = db.GetConnection())
            {
                conn.Open();

                Query selectQuery = db.CreateQuery("SELECT * FROM [metaHistory]");

                HashSet<string> guids = new HashSet<string>();
                HashSet<string> records = new HashSet<string>();

                CultureInfo format = CultureInfo.InvariantCulture;

                using (IDataReader reader = db.ExecuteReader(selectQuery))
                {
                    while (reader.Read())
                    {
                        //FollowUpRow row = new FollowUpRow();

                        //row.ContactGUID = new Guid(reader["ContactGUID"].ToString());// new System.Guid(reader.GetString(reader.GetOrdinal("ContactGUID")));
                        //row.FollowUpDate = reader.GetDateTime(reader.GetOrdinal("FollowUpDate"));

                        //followups.Add(row);
                        guids.Add(reader["ContactGUID"].ToString() + ";" + reader.GetDateTime(reader.GetOrdinal("FollowUpDate")).ToString(format.DateTimeFormat.ShortDatePattern));

                        WordBuilder wb = new WordBuilder(";");

                        wb.Add(reader["ContactGUID"].ToString());
                        wb.Add(reader.GetDateTime(reader.GetOrdinal("FollowUpDate")).ToString(format.DateTimeFormat.ShortDatePattern));

                        if (reader["StatusOnDate"] != DBNull.Value)
                        {
                            wb.Add(reader["StatusOnDate"].ToString());
                        }
                        else 
                        {
                            wb.Add(String.Empty);
                        }

                        wb.Add(reader["Note"].ToString());

                        if (reader["Temp1"] != DBNull.Value)
                        {
                            wb.Add(Convert.ToDouble(reader["Temp1"]).ToString(System.Globalization.CultureInfo.InvariantCulture));
                        }
                        else 
                        {
                            wb.Add(String.Empty);
                        }

                        if (reader["Temp2"] != DBNull.Value)
                        {
                            wb.Add(Convert.ToDouble(reader["Temp2"]).ToString(System.Globalization.CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            wb.Add(String.Empty);
                        }

                        records.Add(wb.ToString());
                    }
                }

                foreach (XElement followUp in followUpsElement.Elements())
                {
                    string contactGuid = followUp.Element("ContactGUID").Value;
                    DateTime followUpDate = DateTime.Parse(followUp.Element("FollowUpDate").Value, CultureInfo.InvariantCulture);

                    bool found = false;

                    string comparer = contactGuid + ";" + followUpDate.ToString(format.DateTimeFormat.ShortDatePattern);

                    if(guids.Contains(comparer)) 
                    {
                        found = true;
                    }

                    //foreach (FollowUpRow row in followups)
                    //{
                    //    if (row.ContactGUID.ToString().Equals(contactGuid, StringComparison.OrdinalIgnoreCase) && row.FollowUpDate == followUpDate)
                    //    {
                    //        found = true;
                    //        break;
                    //    }
                    //}

                    if (found && Update)
                    {
                        // update

                        WordBuilder wb = new WordBuilder(";");

                        wb.Add(contactGuid);
                        wb.Add(followUpDate.ToString(format.DateTimeFormat.ShortDatePattern));

                        if (followUp.Element("StatusOnDate") != null)
                        {
                            wb.Add(followUp.Element("StatusOnDate").Value);
                        }
                        else
                        {
                            wb.Add(String.Empty);
                        }

                        if (followUp.Element("Note") != null)
                        {
                            wb.Add(followUp.Element("Note").Value);
                        }
                        else
                        {
                            wb.Add(String.Empty);
                        }

                        if (followUp.Element("Temp1") != null)
                        {
                            wb.Add(followUp.Element("Temp1").Value);
                        }
                        else
                        {
                            wb.Add(String.Empty);
                        }

                        if (followUp.Element("Temp2") != null)
                        {
                            wb.Add(followUp.Element("Temp2").Value);
                        }
                        else
                        {
                            wb.Add(String.Empty);
                        }

                        string comparer2 = wb.ToString();

                        if (!records.Contains(comparer2))
                        {
                            UpdateFollowUp(conn, followUp, db, contactGuid, followUpDate);
                        }
                    }
                    else if (!found && Append)
                    {
                        InsertFollowUp(conn, followUp, db, contactGuid, followUpDate);
                    }

                    MinorProgress = MinorProgress + inc;
                    OnMinorProgressChanged();
                }
            }

            sw.Stop();
            Debug.Print("ImportFollowUpData Complete. Elapsed: " + sw.Elapsed.TotalMilliseconds + " ms.");
        }
        #endregion // Relationships and Follow-ups

        protected virtual void ImportFormDataElements(XElement formElement)
        {
            string formName = formElement.Attribute("Name").Value.ToString();
            if (!Project.Views.Contains(formName)) return;

            var sw = new Stopwatch();
            sw.Start();

            MinorProgress = 0;
            XElement dataElement = formElement.Element("Data");
            IDbDriver db = Project.CollectedData.GetDatabase();

            using (IDbConnection conn = db.GetConnection())
            {
                conn.Open();

                View form = Project.Views[formName];
                Dictionary<string, bool> guids = new Dictionary<string, bool>();

                #region Get record count
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = "SELECT GlobalRecordId FROM " + form.TableName;

                    int totalCount = 0;
                    using (IDataReader guidReader = command.ExecuteReader()) // db.ExecuteReader(selectGuidQuery))
                    {
                        while (guidReader.Read())
                        {
                            guids.Add(guidReader["GlobalRecordId"].ToString(), true); // true = originally in source pre-import
                            totalCount++;
                        }
                    }
                }
                double inc = 100.0 / (double)dataElement.Elements().Count();
                #endregion // Get record count

                bool isMdb = false;
                if (_sourceDbType.Equals("Access", StringComparison.OrdinalIgnoreCase))
                {
                    isMdb = true;
                }

                //HashSet<string> guidsToExclude = new HashSet<string>();

                if (isMdb && form.Fields.DataFields.Count > Core.Constants.EXPORT_FIELD_LIMIT)
                {
                    XElement data = RebuildRecords(dataElement);

                    inc = 100.0 / (double)data.Elements().Count();

                    foreach (XElement record in data.Elements())
                    {
                        ImportFormRecordElement(record, db, conn, guids, form, inc);
                    }
                }
                else
                {
                    foreach (XElement record in dataElement.Elements())
                    {
                        ImportFormRecordElement(record, db, conn, guids, form, inc);
                    }
                }
            }

            sw.Stop();
            Debug.Print("ImportFormDataElements (" + formName +") Complete. Elapsed: " + sw.Elapsed.TotalMilliseconds + " ms.");
        }

        private XElement RebuildRecords(XElement oldData)
        {
            XElement newData = new XElement("Data");

            Dictionary<string, XElement> recordDictionary = new Dictionary<string, XElement>();

            foreach (XElement record in oldData.Elements("Record"))
            {
                string guid = record.Attribute("Id").Value;

                if (recordDictionary.ContainsKey(guid))
                {
                    foreach (XElement field in record.Elements())
                    {
                        recordDictionary[guid].Add(new XElement(field.Name, field.Value));
                    }
                }
                else
                {
                    XElement newElement = new XElement("Record");

                    newElement.Add(new XAttribute("Id", guid),
                            new XAttribute("FKEY", record.Attribute("FKEY").Value),
                            new XAttribute("FirstSaveUserId", record.Attribute("FirstSaveUserId").Value),
                            new XAttribute("LastSaveUserId", record.Attribute("LastSaveUserId").Value),
                            new XAttribute("FirstSaveTime", record.Attribute("FirstSaveTime").Value),
                            new XAttribute("LastSaveTime", record.Attribute("LastSaveTime").Value),
                            new XAttribute("RecStatus", record.Attribute("RecStatus").Value));

                    foreach (XElement field in record.Elements())
                    {
                        newElement.Add(new XElement(field.Name, field.Value));
                    }

                    recordDictionary.Add(guid, newElement);
                }
            }

            foreach (KeyValuePair<string, XElement> kvp in recordDictionary)
            {
                newData.Add(kvp.Value);
            }

            return newData;
        }

        protected virtual void ImportFormRecordElement(XElement record, IDbDriver db, IDbConnection conn, Dictionary<string, bool> guids, View form, double inc)
        {
            #region Get base table data
            string globalRecordId = record.Attribute("Id").Value.ToString();
            string foreignKey = record.Attribute("FKEY").Value.ToString();
            string firstSaveUserId = record.Attribute("FirstSaveUserId").Value.ToString();
            string lastSaveUserId = record.Attribute("LastSaveUserId").Value.ToString();

            string firstSaveTimeStr = String.Empty;
            if (record.Attribute("FirstSaveTime") != null)
            {
                firstSaveTimeStr = record.Attribute("FirstSaveTime").Value;
            }

            string lastSaveTimeStr = String.Empty;
            if (record.Attribute("LastSaveTime") != null)
            {
                lastSaveTimeStr = record.Attribute("LastSaveTime").Value;
            }

            string recStatus = record.Attribute("RecStatus").Value.ToString();

            if ((recStatus == "1" && Scope == RecordProcessingScope.Deleted) ||
                (recStatus == "0" && Scope == RecordProcessingScope.Undeleted))
            {
                return;// continue;
            }

            DateTime? firstSaveTime = null;
            DateTime? lastSaveTime = null;

            if (!String.IsNullOrEmpty(firstSaveTimeStr))
            {
                firstSaveTime = new DateTime(long.Parse(firstSaveTimeStr));
            }

            if (!String.IsNullOrEmpty(lastSaveTimeStr))
            {
                lastSaveTime = new DateTime(long.Parse(lastSaveTimeStr));
            }
            #endregion // Get base table data

            if ((Update && guids.ContainsKey(globalRecordId)) || (guids.ContainsKey(globalRecordId) && guids[globalRecordId] == false)) // the false is for records spread over multiple Xml entities
            {
                WordBuilder wb = new WordBuilder(", ");
                List<QueryParameter> parameters = new List<QueryParameter>();

                if (lastSaveTime.HasValue)
                {
                    wb.Add("LastSaveTime = @LastSaveTime");
                    parameters.Add(new QueryParameter("@LastSaveTime", DbType.DateTime, lastSaveTime));
                }

                wb.Add("LastSaveLogonName = @LastSaveLogonName");
                parameters.Add(new QueryParameter("@LastSaveLogonName", DbType.String, lastSaveUserId));

                if ((recStatus == "1" && Undelete) || (recStatus == "0" && Delete))
                {
                    wb.Add("RecStatus = @RecStatus");
                    parameters.Add(new QueryParameter("@RecStatus", DbType.String, recStatus));
                }

                parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, globalRecordId));

                Query updateBaseTableQuery = db.CreateQuery("UPDATE " + form.TableName + " SET " + wb.ToString() + " WHERE GlobalRecordId = @GlobalRecordId");
                foreach (QueryParameter p in parameters)
                {
                    updateBaseTableQuery.Parameters.Add(p);
                }

                using (IDbCommand command = GetCommand(updateBaseTableQuery.SqlStatement, conn, updateBaseTableQuery.Parameters))
                {
                    object obj = command.ExecuteNonQuery();
                }

                UpdateRecord(conn, db, form, record, globalRecordId);
            }
            else if (Append && !guids.ContainsKey(globalRecordId))
            {
                bool inserted = CreateNewRow(conn, form, record, globalRecordId, foreignKey, recStatus, firstSaveUserId, lastSaveUserId, firstSaveTime, lastSaveTime);

                guids.Add(globalRecordId, false);
            }

            MinorProgress = MinorProgress + inc;
            OnMinorProgressChanged();
        }

        protected void UpdatePageData(IDbConnection conn, View form, Page page, IDbDriver db, XElement record, string globalRecordId)
        {
            WordBuilder wbFieldNames = new WordBuilder(", ");
            List<QueryParameter> parameters = new List<QueryParameter>();

            if (!record.HasElements)
            {
                return;
            }

            foreach (RenderableField field in page.Fields)
            {
                if (field is IDataField && record.Element(field.Name) != null)
                {
                    wbFieldNames.Add(field.Name + " = @" + field.Name);
                    parameters.Add(
                        GetQueryParameterForField(field,
                            FormatFieldData(form, field.Name, record.Element(field.Name).Value),
                            form,
                            page));
                }
            }

            if (parameters.Count == 0)
            {
                return;
            }

            Query updateQuery = db.CreateQuery("UPDATE " + page.TableName + " SET " + wbFieldNames.ToString() + " WHERE GlobalRecordId = @GlobalRecordId");

            foreach (QueryParameter parameter in parameters)
            {
                updateQuery.Parameters.Add(parameter);
            }
            
            updateQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, globalRecordId));

            using (IDbCommand command = GetCommand(updateQuery.SqlStatement, conn, updateQuery.Parameters))
            {
                object obj = command.ExecuteNonQuery();
            }
        }

        protected virtual void UpdateRecord(IDbConnection conn, IDbDriver db, View form, XElement record, string globalRecordId)
        {
            foreach (Page page in form.Pages)
            {
                UpdatePageData(conn, form, page, db, record, globalRecordId);
            }
        }

        protected virtual bool CreateNewRow(IDbConnection conn, View form, XElement record, string guid, string fkey = "", string recStatus = "1", string firstSaveId = "", string lastSaveId = "", DateTime? firstSaveTime = null, DateTime? lastSaveTime = null)
        {
            #region Input Validation
            if (string.IsNullOrEmpty(guid)) { throw new ArgumentNullException("guid"); }
            #endregion // Input Validation

            IDbDriver db = Project.CollectedData.GetDatabase();
            StringBuilder sb = new StringBuilder();
            sb.Append(" insert into ");
            sb.Append(db.InsertInEscape(form.TableName));
            sb.Append(StringLiterals.SPACE);
            sb.Append(StringLiterals.SPACE);

            WordBuilder fields = new WordBuilder(",");
            fields.Append("[GlobalRecordId]");

            if (!String.IsNullOrEmpty(fkey)) { fields.Append("[FKEY]"); }
            if (!String.IsNullOrEmpty(recStatus)) { fields.Append("[RecStatus]"); }
            if (!String.IsNullOrEmpty(firstSaveId)) { fields.Append("[FirstSaveLogonName]"); }
            if (!String.IsNullOrEmpty(lastSaveId)) { fields.Append("[LastSaveLogonName]"); }
            if (firstSaveTime.HasValue)
            {
                firstSaveTime = new DateTime(firstSaveTime.Value.Year,
                firstSaveTime.Value.Month,
                firstSaveTime.Value.Day,
                firstSaveTime.Value.Hour,
                firstSaveTime.Value.Minute,
                firstSaveTime.Value.Second);
                fields.Append("[FirstSaveTime]");
            }
            if (lastSaveTime.HasValue)
            {
                lastSaveTime = new DateTime(lastSaveTime.Value.Year,
                lastSaveTime.Value.Month,
                lastSaveTime.Value.Day,
                lastSaveTime.Value.Hour,
                lastSaveTime.Value.Minute,
                lastSaveTime.Value.Second);
                fields.Append("[LastSaveTime]");
            }

            sb.Append("(" + fields.ToString() + ")");
            sb.Append(" values (");

            List<QueryParameter> parameters = new List<QueryParameter>();
            WordBuilder values = new WordBuilder(",");
            values.Append("'" + guid + "'");

            if (!String.IsNullOrEmpty(fkey))
            {
                values.Append("@FKEY");
                parameters.Add(new QueryParameter("@FKEY", DbType.String, fkey));
            }
            if (!String.IsNullOrEmpty(recStatus))
            {
                values.Append("@RecStatus");
                parameters.Add(new QueryParameter("@RecStatus", DbType.Int32, Convert.ToInt32(recStatus)));
            }
            if (!String.IsNullOrEmpty(firstSaveId))
            {
                values.Append("@FirstSaveLogonName");
                parameters.Add(new QueryParameter("@FirstSaveLogonName", DbType.String, firstSaveId));
            }
            if (!String.IsNullOrEmpty(lastSaveId))
            {
                values.Append("@LastSaveLogonName");
                parameters.Add(new QueryParameter("@LastSaveLogonName", DbType.String, lastSaveId));
            }
            if (firstSaveTime.HasValue)
            {
                values.Append("@FirstSaveTime");
                parameters.Add(new QueryParameter("@FirstSaveTime", DbType.DateTime, firstSaveTime));
            }
            if (lastSaveTime.HasValue)
            {
                values.Append("@LastSaveTime");
                parameters.Add(new QueryParameter("@LastSaveTime", DbType.DateTime, lastSaveTime));
            }

            sb.Append(values.ToString());
            sb.Append(") ");
            Epi.Data.Query insertQuery = db.CreateQuery(sb.ToString());
            insertQuery.Parameters = parameters;

            IDbCommand baseTableCommand = GetCommand(insertQuery.SqlStatement, conn, insertQuery.Parameters);
            object baseObj = baseTableCommand.ExecuteNonQuery();

            foreach (Page page in form.Pages)
            {
                WordBuilder wbFieldNames = new WordBuilder(", ");
                WordBuilder wbParamNames = new WordBuilder(", ");
                List<QueryParameter> pageInsertParameters = new List<QueryParameter>();

                foreach (RenderableField field in page.Fields)
                {
                    if (field is IDataField && record.Element(field.Name) != null)
                    {
                        wbFieldNames.Add(field.Name);
                        wbParamNames.Add("@" + field.Name);

                        pageInsertParameters.Add(
                            GetQueryParameterForField(field,
                                FormatFieldData(form, field.Name, record.Element(field.Name).Value),
                                form,
                                page));
                    }
                }

                string queryText = "INSERT INTO " + page.TableName + " (" + wbFieldNames.ToString() + ", GlobalRecordId) VALUES (" + wbParamNames.ToString() + ", @GlobalRecordId)";

                if (wbParamNames.Count == 0)
                {
                    queryText = "INSERT INTO " + page.TableName + " (GlobalRecordId) VALUES (@GlobalRecordId)";
                }

                Query pageInsertQuery = db.CreateQuery(queryText);

                foreach (QueryParameter parameter in pageInsertParameters)
                {
                    pageInsertQuery.Parameters.Add(parameter);
                }

                pageInsertQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, guid));

                using (IDbCommand command = GetCommand(pageInsertQuery.SqlStatement, conn, pageInsertQuery.Parameters))
                {
                    object obj = command.ExecuteNonQuery();
                }
            }

            return true;
        }

        protected QueryParameter GetQueryParameterForField(Field field, object fieldValue, View destinationForm, Page sourcePage)
        {
            if (!(
                field is GroupField ||
                field is RelatedViewField ||
                field is UniqueKeyField ||
                field is RecStatusField ||
                field is GlobalRecordIdField ||
                fieldValue == null ||
                String.IsNullOrEmpty(fieldValue.ToString()
                )))
            {
                string fieldName = field.Name;
                switch (field.FieldType)
                {
                    case MetaFieldType.Date:
                    case MetaFieldType.DateTime:
                    case MetaFieldType.Time:
                        DateTime dt = DateTime.Parse(fieldValue.ToString(), CultureInfo.InvariantCulture);
                        return new QueryParameter("@" + fieldName, DbType.DateTime, dt);
                    case MetaFieldType.Checkbox:
                        return new QueryParameter("@" + fieldName, DbType.Boolean, Convert.ToBoolean(fieldValue));
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
                    case MetaFieldType.GUID:
                        return new QueryParameter("@" + fieldName, DbType.String, fieldValue);
                    case MetaFieldType.Number:
                    case MetaFieldType.YesNo:
                    case MetaFieldType.RecStatus:
                        return new QueryParameter("@" + fieldName, DbType.Single, Convert.ToDouble(fieldValue, System.Globalization.CultureInfo.InvariantCulture));
                    case MetaFieldType.Image:
                        return new QueryParameter("@" + fieldName, DbType.Binary, Convert.FromBase64String(fieldValue.ToString()));
                    case MetaFieldType.Option:
                        return new QueryParameter("@" + fieldName, DbType.Single, fieldValue);
                    default:
                        throw new ApplicationException("Not a supported field type");
                }
            }
            return null;
        }

        protected object FormatFieldData(View destinationForm, string fieldName, object value)
        {
            if (destinationForm.Fields.Contains(fieldName))
            {
                Field field = destinationForm.Fields[fieldName];

                if (field is CheckBoxField)
                {
                    if (value.ToString().Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        value = true;
                    }
                    else if (value.ToString().Equals("false", StringComparison.OrdinalIgnoreCase))
                    {
                        value = false;
                    }
                }

                if (field is YesNoField)
                {
                    if (value.ToString().Equals("1", StringComparison.OrdinalIgnoreCase))
                    {
                        value = 1;
                    }
                    else if (value.ToString().Equals("0", StringComparison.OrdinalIgnoreCase))
                    {
                        value = 0;
                    }
                }

                if (field is NumberField && !string.IsNullOrEmpty(value.ToString()))
                {
                    double result = -1;
                    if (double.TryParse(value.ToString(), out result))
                    {
                        value = result;
                    }
                }
            }

            return value;
        }

        public virtual void Import(string fileName)
        {
            var sw = new Stopwatch();
            sw.Start();

            MajorProgress = 0;

            Epi.ApplicationIdentity appId = new Epi.ApplicationIdentity(typeof(Configuration).Assembly);
            DateTime dt = DateTime.UtcNow;
            string dateDisplayValue = string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0:s}", dt);

            RegionEnum syncFileRegion = RegionEnum.International;

            using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                XmlReaderSettings settings = new XmlReaderSettings() { Async = false, CheckCharacters = false };

                using (XmlReader reader = XmlReader.Create(stream, settings))
                {
                    while (reader.Read())
                    {
                        if (reader.Name.Equals("DataPackage", StringComparison.OrdinalIgnoreCase))
                        {
                            _sourceDbType = reader.GetAttribute("SourceDbType");
                            
                            try
                            {
                                if (reader.GetAttribute("VhfVersion").StartsWith("0.9.4.", StringComparison.OrdinalIgnoreCase)) syncFileRegion = RegionEnum.International;
                                else if (reader.GetAttribute("Region").Equals("International", StringComparison.OrdinalIgnoreCase)) syncFileRegion = RegionEnum.International;
                                else if (reader.GetAttribute("Region").Equals("USA", StringComparison.OrdinalIgnoreCase)) syncFileRegion = RegionEnum.USA;
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                syncFileRegion = RegionEnum.None;
                            }
                            catch (NullReferenceException)
                            {
                                syncFileRegion = RegionEnum.None;
                            }

                            if (syncFileRegion != _region)
                            {
                                throw new InvalidOperationException("The region the selected .sync file was created with is incompatible with the current application settings.");
                            }

                            break;
                        }
                    }

                    reader.MoveToContent();
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            if (reader.Name.Equals("Form", StringComparison.OrdinalIgnoreCase))
                            {
                                XElement el = XNode.ReadFrom(reader) as XElement;
                                if (el != null)
                                {
                                    ImportFormDataElements(el);
                                }
                            }
                            else if (reader.Name.Equals("ContactFollowUps", StringComparison.OrdinalIgnoreCase))
                            {
                                XElement el = XNode.ReadFrom(reader) as XElement;
                                if (el != null)
                                {
                                    ImportFollowUpData(el);
                                }
                            }
                            else if (reader.Name.Equals("Links", StringComparison.OrdinalIgnoreCase))
                            {
                                XElement el = XNode.ReadFrom(reader) as XElement;
                                if (el != null)
                                {
                                    ImportLinkData(el);
                                }
                            }
                        }
                    }
                }
            }

            sw.Stop();
            System.Diagnostics.Debug.Print("Import finished. Total seconds: " + sw.Elapsed.TotalSeconds.ToString());
        }

        #region Database Stuff
        /// <summary>
        /// Returns a native equivalent of a DbParameter
        /// </summary>
        /// <returns>Native equivalent of a DbParameter</returns>
        protected virtual OleDbParameter ConvertToAccessParameter(QueryParameter parameter)
        {
            if (parameter.DbType.Equals(DbType.Guid))
            {
                parameter.Value = new Guid(parameter.Value.ToString());
            }

            OleDbParameter param = new OleDbParameter(parameter.ParameterName, CovertToAccessDbType(parameter.DbType), parameter.Size, parameter.Direction, parameter.IsNullable, parameter.Precision, parameter.Scale, parameter.SourceColumn, parameter.SourceVersion, parameter.Value);
            return param;
        }

        /// <summary>
        /// Returns a native equivalent of a DbParameter
        /// </summary>
        /// <returns>Native equivalent of a DbParameter</returns>
        protected SqlParameter ConvertToSqlParameter(QueryParameter parameter)
        {
            if (parameter.DbType.Equals(DbType.Guid))
            {
                parameter.Value = new Guid(parameter.Value.ToString());
            }

            return new SqlParameter(parameter.ParameterName, CovertToSqlDbType(parameter.DbType), parameter.Size, parameter.Direction, parameter.IsNullable, parameter.Precision, parameter.Scale, parameter.SourceColumn, parameter.SourceVersion, parameter.Value);
        }

        /// <summary>
        /// Gets the Access version of a generic DbType
        /// </summary>
        /// <returns>Access version of the generic DbType</returns>
        protected SqlDbType CovertToSqlDbType(DbType dbType)
        {
            switch (dbType)
            {
                case DbType.AnsiString:
                    return SqlDbType.VarChar;
                case DbType.AnsiStringFixedLength:
                    return SqlDbType.Char;
                case DbType.Binary:
                    return SqlDbType.Binary;
                case DbType.Boolean:
                    return SqlDbType.Bit;
                case DbType.Byte:
                    return SqlDbType.TinyInt;
                case DbType.Currency:
                    return SqlDbType.Money;
                case DbType.Date:
                    return SqlDbType.DateTime;
                case DbType.DateTime:
                    return SqlDbType.DateTime;
                case DbType.DateTime2:
                    return SqlDbType.DateTime2;
                case DbType.DateTimeOffset:
                    return SqlDbType.DateTimeOffset;
                case DbType.Decimal:
                    return SqlDbType.Decimal;
                case DbType.Double:
                    return SqlDbType.Float;
                case DbType.Guid:
                    return SqlDbType.UniqueIdentifier;
                case DbType.Int16:
                    return SqlDbType.SmallInt;
                case DbType.Int32:
                    return SqlDbType.Int;
                case DbType.Int64:
                    return SqlDbType.BigInt;
                case DbType.Object:
                    return SqlDbType.Binary;
                case DbType.SByte:
                    return SqlDbType.TinyInt;
                case DbType.Single:
                    return SqlDbType.Real;
                case DbType.String:
                    return SqlDbType.NVarChar;
                case DbType.StringFixedLength:
                    return SqlDbType.NChar;
                case DbType.Time:
                    return SqlDbType.DateTime;
                case DbType.UInt16:
                    return SqlDbType.SmallInt;
                case DbType.UInt32:
                    return SqlDbType.Int;
                case DbType.UInt64:
                    return SqlDbType.BigInt;
                case DbType.VarNumeric:
                    return SqlDbType.Decimal;
                default:
                    return SqlDbType.VarChar;
            }
        }

        /// <summary>
        /// Gets the Access version of a generic DbType
        /// </summary>
        /// <returns>Access version of the generic DbType</returns>
        protected virtual OleDbType CovertToAccessDbType(DbType dbType)
        {
            switch (dbType)
            {
                case DbType.AnsiString:
                    return OleDbType.VarChar;
                case DbType.AnsiStringFixedLength:
                    return OleDbType.Char;
                case DbType.Binary:
                    return OleDbType.Binary;
                case DbType.Boolean:
                    return OleDbType.Boolean;
                case DbType.Byte:
                    return OleDbType.UnsignedTinyInt;
                case DbType.Currency:
                    return OleDbType.Currency;
                case DbType.Date:
                    return OleDbType.DBDate;
                case DbType.DateTime:
                case DbType.DateTime2:
                    return OleDbType.DBTimeStamp;
                case DbType.Decimal:
                    return OleDbType.Decimal;
                case DbType.Double:
                    return OleDbType.Double;
                case DbType.Guid:
                    return OleDbType.Guid;
                case DbType.Int16:
                    return OleDbType.SmallInt;
                case DbType.Int32:
                    return OleDbType.Integer;
                case DbType.Int64:
                    return OleDbType.BigInt;
                case DbType.Object:
                    //  return OleDbType.VarChar;
                    return OleDbType.Binary;
                case DbType.SByte:
                    return OleDbType.TinyInt;
                case DbType.Single:
                    return OleDbType.Single;
                case DbType.String:
                    return OleDbType.VarWChar;
                case DbType.StringFixedLength:
                    return OleDbType.WChar;
                case DbType.Time:
                    return OleDbType.DBTimeStamp;
                case DbType.UInt16:
                    return OleDbType.UnsignedSmallInt;
                case DbType.UInt32:
                    return OleDbType.UnsignedInt;
                case DbType.UInt64:
                    return OleDbType.UnsignedBigInt;
                case DbType.VarNumeric:
                    return OleDbType.VarNumeric;
                default:
                    return OleDbType.VarChar;
            }
        }

        protected virtual IDbCommand GetCommand(string sqlStatement, IDbConnection connection, List<QueryParameter> parameters)
        {

            #region Input Validation
            if (string.IsNullOrEmpty(sqlStatement))
            {
                throw new ArgumentNullException("sqlStatement");
            }
            if (parameters == null)
            {
                throw new ArgumentNullException("parameters");
            }
            #endregion

            IDbCommand command = connection.CreateCommand();
            command.CommandText = sqlStatement;

            IDbDriver db = Project.CollectedData.GetDatabase();

            if (db.ToString().ToLower().Contains("sql") && !(db is Epi.Data.Office.OleDbDatabase))
            {
                foreach (QueryParameter parameter in parameters)
                {
                    command.Parameters.Add(this.ConvertToSqlParameter(parameter));
                }
            }
            else
            {
                foreach (QueryParameter parameter in parameters)
                {
                    command.Parameters.Add(this.ConvertToAccessParameter(parameter));
                }
            }

            return command;
        }
        #endregion // Database Stuff
    }
}
