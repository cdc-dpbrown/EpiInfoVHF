using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Xml;
using Epi;
using Epi.Data;
using Epi.Fields;
using Epi.ImportExport.Filters;
using Epi.ImportExport.ProjectPackagers;

namespace ContactTracing.ImportExport
{
    /// <summary>
    /// A class used to import data from an Epi Info 7 data package (represented as Xml) into the specified Epi Info 7 form and any of its descendant forms.
    /// </summary>
    /// <remarks>
    /// The ImportInfo object contains information about what was imported and can be accessed after the import 
    /// process has been completed.
    /// </remarks>
    public class XmlLabDataUnpackager
    {
        #region Events
        public event SetProgressBarDelegate UpdateProgress;
        public event SimpleEventHandler ResetProgress;
        public event UpdateStatusEventHandler StatusChanged;
        public event UpdateStatusEventHandler MessageGenerated;
        public event EventHandler ImportFinished;
        #endregion // Events

        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="destinationForm">The form that will receive the incoming data</param>
        /// <param name="xmlDataPackage">The data package in Xml format</param>
        public XmlLabDataUnpackager(View destinationForm, XmlDocument xmlDataPackage)
        {
            #region Input Validation
            if (destinationForm == null) { throw new ArgumentNullException("sourceForm"); }
            if (xmlDataPackage == null) { throw new ArgumentNullException("xmlDataPackage"); }
            #endregion // Input Validation

            DestinationForm = destinationForm;
            DestinationProject = DestinationForm.Project;
            XmlDataPackage = xmlDataPackage;
            Update = true;
            Append = true;
        }
        #endregion // Constructors

        #region Properties
        /// <summary>
        /// Gets/sets the results of the unpackaging process
        /// </summary>
        public Epi.ImportExport.ProjectPackagers.ImportInfo ImportInfo { get; private set; }
        /// <summary>
        /// Gets/sets the source form within the project that will be used for the packaging routine.
        /// </summary>
        public View DestinationForm { get; private set; }

        /// <summary>
        /// Gets/sets whether to append unmatched records during the import.
        /// </summary>
        public bool Append { get; set; }

        /// <summary>
        /// Gets/sets whether to update matching records during the import.
        /// </summary>
        public bool Update { get; set; }

        /// <summary>
        /// Gets/sets the source project for the packaging routine
        /// </summary>
        private Project DestinationProject { get; set; }

        /// <summary>
        /// Gets/sets the XmlDataPackage that will be used to import data
        /// </summary>
        private XmlDocument XmlDataPackage { get; set; }

        /// <summary>
        /// Gets/sets the name of the current package being imported.
        /// </summary>
        private string PackageName { get; set; }

        /// <summary>
        /// Gets/sets a connection to the database
        /// </summary>
        /// <remarks>
        /// Intended only for use with Ole-based databases for performance reasons; keeping this connection
        /// open and using it for the DB calls is much faster as opposed to relying on the data drivers, which 
        /// open and close the connection each time they are used.
        /// </remarks>
        private IDbConnection Conn { get; set; }
        #endregion // Properties

        #region Public Methods
        /// <summary>
        /// Unpackages the specified XmlDocument and imports the data into the specified Epi Info 7 form (and any descendant forms).
        /// </summary>
        public void Unpackage()
        {
            ImportInfo = new Epi.ImportExport.ProjectPackagers.ImportInfo();
            ImportInfo.UserID = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString();
            ImportInfo.ImportInitiated = DateTime.Now;

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            using (Conn = DestinationProject.CollectedData.GetDatabase().GetConnection())
            {
                Conn.Open();
                CheckForProblems();

                foreach (XmlNode node in XmlDataPackage.ChildNodes)
                {
                    if (node.Name.ToLower().Equals("datapackage"))
                    {
                        PackageName = node.Attributes["Name"].Value;
                        if (StatusChanged != null) { StatusChanged(string.Format(UnpackagerStrings.IMPORT_INITIATED, PackageName, ImportInfo.UserID)); }
                        if (MessageGenerated != null) { MessageGenerated(string.Format(UnpackagerStrings.IMPORT_INITIATED, PackageName, ImportInfo.UserID)); }

                        foreach (XmlNode dpNode in node.ChildNodes)
                        {
                            if (dpNode.Name.ToLower().Equals("form"))
                            {
                                List<PackageCaseFieldData> records = new List<PackageCaseFieldData>();

                                XmlNode formNode = dpNode;
                                View form = DestinationProject.Views[formNode.Attributes["Name"].Value.ToString()];

                                if (formNode.ChildNodes.Count == 2)
                                {
                                    XmlNode metaDataNode = formNode.ChildNodes[0];
                                    XmlNode dataNode = formNode.ChildNodes[1];

                                    Dictionary<string, Page> pageDictionary = new Dictionary<string, Page>();

                                    foreach (XmlElement fieldMetadataElement in metaDataNode.ChildNodes)
                                    {
                                        string fieldName = fieldMetadataElement.Attributes["Name"].InnerText;
                                        foreach (Page page in form.Pages)
                                        {
                                            if (page.Fields.Contains(fieldName))
                                            {
                                                pageDictionary.Add(fieldName, page);
                                                break;
                                            }
                                        }
                                    }

                                    foreach (XmlElement recordElement in dataNode.ChildNodes)
                                    {
                                        if (recordElement.Name.Equals("Record"))
                                        {
                                            string guid = recordElement.Attributes[0].Value.ToString();
                                            string caseId = recordElement.Attributes["CaseId"].Value.ToString();
                                            string labId = String.Empty;
                                            if (form.Name.StartsWith("Lab"))
                                            {
                                                try
                                                {
                                                    labId = recordElement.Attributes["FieldLabSpecId"].Value.ToString();
                                                }
                                                catch (NullReferenceException)
                                                {
                                                    labId = String.Empty;
                                                    continue;
                                                }
                                            }

                                            foreach (XmlNode fieldNode in recordElement.ChildNodes)
                                            {
                                                string fieldName = String.Empty;
                                                if (fieldNode.Name.Equals("Field"))
                                                {
                                                    fieldName = fieldNode.Attributes[0].Value;

                                                    if (pageDictionary.ContainsKey(fieldName)) // needed in case a field exists in the package but not on the form
                                                    {
                                                        Page destinationPage = pageDictionary[fieldName];

                                                        object fieldValue = FormatFieldData(fieldName, fieldNode.InnerText);
                                                        PackageCaseFieldData fieldData = new PackageCaseFieldData();
                                                        fieldData.FieldName = fieldName;
                                                        fieldData.FieldValue = fieldValue;
                                                        fieldData.RecordGUID = guid;
                                                        fieldData.RecordCaseId = caseId;
                                                        fieldData.RecordLabId = labId;
                                                        fieldData.Page = destinationPage;
                                                        records.Add(fieldData);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                ImportInfo.FormsProcessed++;
                                if (StatusChanged != null) { StatusChanged(string.Format(UnpackagerStrings.IMPORT_START_FORM, PackageName, form.Name)); }
                                if (MessageGenerated != null) { MessageGenerated(string.Format(UnpackagerStrings.IMPORT_START_FORM, PackageName, form.Name)); }
                                if (form.Name.StartsWith("Lab"))
                                {
                                    ImportLabRecords(form, formNode, records);
                                }
                                else
                                {
                                    ImportCaseRecords(form, formNode, records);
                                }
                                if (StatusChanged != null) { StatusChanged(string.Format(UnpackagerStrings.IMPORT_END_FORM, PackageName, form.Name, ImportInfo.RecordsUpdated[form].ToString(), ImportInfo.RecordsAppended[form].ToString())); }
                                if (MessageGenerated != null) { MessageGenerated(string.Format(UnpackagerStrings.IMPORT_END_FORM, PackageName, form.Name, ImportInfo.RecordsUpdated[form].ToString(), ImportInfo.RecordsAppended[form].ToString())); }
                            }
                        }
                    }
                }
            }

            if (StatusChanged != null) { StatusChanged(string.Format(UnpackagerStrings.IMPORT_END, PackageName)); }
            if (MessageGenerated != null) { MessageGenerated(string.Format(UnpackagerStrings.IMPORT_END, PackageName)); }

            sw.Stop();
            ImportInfo.TimeElapsed = sw.Elapsed;
            ImportInfo.ImportCompleted = DateTime.Now;
            ImportInfo.Succeeded = true;

            if (ImportFinished != null) { ImportFinished(this, new EventArgs()); }
        }
        #endregion // Public Methods

        #region Private Methods
        /// <summary>
        /// Checks for problems in the destination form and the packaged data, and for any problematic
        /// iscrepancies in the metadata.
        /// </summary>
        private void CheckForProblems()
        {
            XmlNodeList xnList = XmlDataPackage.SelectNodes("/DataPackage/Form");
            foreach (XmlNode xn in xnList)
            {
                XmlNode fieldMetaDataNode = xn.FirstChild;

                if (fieldMetaDataNode != null)
                {
                    foreach (XmlNode fieldInfoNode in fieldMetaDataNode.ChildNodes)
                    {
                        string fieldName = fieldInfoNode.Attributes["Name"].Value.ToString();
                        string fieldType = fieldInfoNode.Attributes["FieldType"].Value.ToString();
                        string fieldPage = fieldInfoNode.Attributes["Page"].Value.ToString();

                        if (DestinationForm.Fields.Contains(fieldName))
                        {
                            Field field = DestinationForm.Fields[fieldName];
                            string t = field.FieldType.ToString();

                            if (!fieldType.Equals(t))
                            {
                                ImportInfo.Succeeded = false;
                                string message = string.Format(ImportExportSharedStrings.UNPACKAGE_PROBLEM_CHECK_ERROR_E3013, fieldName, DestinationForm.Name, fieldType, t);
                                ImportInfo.AddError(message, "3013");
                                throw new ApplicationException(message);
                            }
                        }
                        else
                        {
                            string message = string.Format(ImportExportSharedStrings.UNPACKAGE_PROBLEM_CHECK_ERROR_E3014, fieldName, DestinationForm.Name);
                            ImportInfo.AddError(message, "3014");
                            if (MessageGenerated != null) { MessageGenerated(message); }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Begins the process of importing data into each of the page tables on the form
        /// </summary>
        /// <param name="form">The form that will receive the data</param>
        /// <param name="records">The data to be imported</param>
        /// <param name="destinationGuids">A dictionary of GUIDs in the destination project; the key represents the GUID itself and the value (either true or false) represents whether or not to process that record</param>
        private void ImportCaseRecordsToPageTables(View form, List<PackageCaseFieldData> records, Dictionary<string, bool> destinationCaseIds, Dictionary<string, string> caseGuidPairs)
        {
            if (records.Count == 0) { return; }

            if (Conn.State != ConnectionState.Open) { Conn.Open(); }

            IDbDriver db = DestinationProject.CollectedData.GetDatabase();
            if (db == null)
            {
                throw new InvalidOperationException("Data driver cannot be null in ImportCaseRecordsToPageTables method.");
            }

            DataTable destinationTable = ContactTracing.Core.Common.JoinPageTables(db, form);

            if (ResetProgress != null) { ResetProgress(); }
            double total = records.Count;

            Page previousPage = null;
            string lastGuid = String.Empty;
            List<string> fieldsInQuery = new List<string>();

            WordBuilder setFieldText = new WordBuilder(StringLiterals.COMMA);
            List<QueryParameter> fieldValueParams = new List<QueryParameter>();

            PackageCaseFieldData lastRecord = new PackageCaseFieldData();
            lastRecord.FieldName = "__--LastRecord--__";
            lastRecord.RecordGUID = String.Empty;
            records.Add(lastRecord);

            for (int i = 0; i < records.Count; i++)
            {
                PackageCaseFieldData fieldData = records[i];

                if (i % 200 == 0)
                {
                    if (StatusChanged != null) { StatusChanged(string.Format(UnpackagerStrings.IMPORT_FIELD_PROGRESS, (i / total).ToString("P0"))); }
                    if (UpdateProgress != null) { UpdateProgress((i / total) * 100); }
                }

                //string guid = fieldData.RecordGUID;
                string caseId = fieldData.RecordCaseId;
                bool isLast = fieldData.Equals(lastRecord);
                Page currentPage = fieldData.Page;

                if ((previousPage != currentPage && previousPage != null && previousPage.TableName != null) || isLast || fieldsInQuery.Contains(fieldData.FieldName))
                {
                    // run the update with the fields we currently have...

                    string updateHeader = String.Empty;
                    string whereClause = String.Empty;
                    StringBuilder sb = new StringBuilder();

                    // Build the Update statement which will be reused
                    sb.Append(SqlKeyWords.UPDATE);
                    sb.Append(StringLiterals.SPACE);
                    sb.Append(db.InsertInEscape(previousPage.TableName));
                    sb.Append(StringLiterals.SPACE);
                    sb.Append(SqlKeyWords.SET);
                    sb.Append(StringLiterals.SPACE);

                    updateHeader = sb.ToString();

                    sb.Remove(0, sb.ToString().Length);

                    // Build the WHERE caluse which will be reused
                    sb.Append(SqlKeyWords.WHERE);
                    sb.Append(StringLiterals.SPACE);
                    sb.Append(db.InsertInEscape(ColumnNames.GLOBAL_RECORD_ID));
                    sb.Append(StringLiterals.EQUAL);
                    sb.Append("'");
                    sb.Append(lastGuid);
                    sb.Append("'");

                    //if (destinationTable.Columns.Contains(fieldData.FieldName) && form.Name.StartsWith("Case"))
                    //{
                    //    string columnType = destinationTable.Columns[fieldData.FieldName].DataType.ToString();
                    //    if (columnType == "System.String")
                    //    {
                    //        sb.Append(" AND (");
                    //        sb.Append(fieldData.FieldName);
                    //        sb.Append(" is null");
                    //        sb.Append(" OR ");
                    //        sb.Append(fieldData.FieldName);
                    //        sb.Append(" = '')");
                    //    }
                    //    else
                    //    {
                    //        sb.Append(" AND ");
                    //        sb.Append(fieldData.FieldName);
                    //        sb.Append(" is null");
                    //    }
                    //    whereClause = sb.ToString();
                    //}

                    whereClause = sb.ToString();

                    sb.Remove(0, sb.ToString().Length);

                    sb.Append(StringLiterals.LEFT_SQUARE_BRACKET);
                    sb.Append(fieldData.FieldName);
                    sb.Append(StringLiterals.RIGHT_SQUARE_BRACKET);
                    sb.Append(StringLiterals.EQUAL);

                    sb.Append(StringLiterals.COMMERCIAL_AT);
                    sb.Append(fieldData.FieldName);

                    if (fieldsInQuery.Count > 0 && fieldValueParams.Count > 0)
                    {
                        Query updateQuery = db.CreateQuery(updateHeader + StringLiterals.SPACE + setFieldText.ToString() + StringLiterals.SPACE + whereClause);
                        updateQuery.Parameters = fieldValueParams;

                        if (DestinationProject.CollectedDataDriver.ToLower().Contains("epi.data.office"))
                        {
                            IDbCommand command = GetCommand(updateQuery.SqlStatement, Conn, updateQuery.Parameters);
                            object obj = command.ExecuteNonQuery();
                        }
                        else
                        {
                            db.ExecuteNonQuery(updateQuery);
                        }
                    }

                    setFieldText = new WordBuilder(StringLiterals.COMMA);
                    fieldValueParams = new List<QueryParameter>();
                    fieldsInQuery = new List<string>();

                    if (isLast) { break; }
                }

                if (destinationCaseIds.ContainsKey(caseId) && destinationCaseIds[caseId] == true)
                {
                    DataRow currentRow = null;

                    foreach (DataRow row in destinationTable.Rows)
                    {
                        if (row["ID"].ToString().Equals(caseId))
                        {
                            currentRow = row;
                            break; // should never have duplicates
                        }
                    }

                    if (currentRow != null)
                    {
                        if (String.IsNullOrEmpty(currentRow[fieldData.FieldName].ToString().Trim()) || (fieldData.FieldName == "FinalLabClass") || (fieldData.FieldName == "ID" && !String.IsNullOrEmpty(currentRow[fieldData.FieldName].ToString().Trim())))
                        {
                            QueryParameter parameter = GetQueryParameterForField(fieldData, form, fieldData.Page);
                            fieldsInQuery.Add(fieldData.FieldName);
                            if (parameter != null)
                            {
                                setFieldText.Append(db.InsertInEscape(fieldData.FieldName) + " = " + "@" + fieldData.FieldName);
                                fieldValueParams.Add(parameter);
                            }
                            lastGuid = caseGuidPairs[caseId];
                            previousPage = currentPage;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Begins the process of importing data into each of the page tables on the form
        /// </summary>
        /// <param name="form">The form that will receive the data</param>
        /// <param name="records">The data to be imported</param>
        /// <param name="destinationGuids">A dictionary of GUIDs in the destination project; the key represents the GUID itself and the value (either true or false) represents whether or not to process that record</param>
        private void ImportLabRecordsToPageTables(View labForm, 
            List<PackageCaseFieldData> labRecords, 
            Dictionary<string, bool> destinationLabIds, 
            Dictionary<string, string> labGuidPairs)
        {
            if (labRecords.Count == 0) { return; }

            if (Conn.State != ConnectionState.Open) { Conn.Open(); }

            IDbDriver db = DestinationProject.CollectedData.GetDatabase();
            if (db == null)
            {
                throw new InvalidOperationException("Data driver cannot be null in ImportRecordsToPageTables method.");
            }

            if (ResetProgress != null) { ResetProgress(); }
            double total = labRecords.Count;

            Page previousPage = null;
            string lastLabId = String.Empty;
            List<string> fieldsInQuery = new List<string>();

            WordBuilder setFieldText = new WordBuilder(StringLiterals.COMMA);
            List<QueryParameter> fieldValueParams = new List<QueryParameter>();

            PackageCaseFieldData lastRecord = new PackageCaseFieldData();
            lastRecord.FieldName = "__--LastRecord--__";
            lastRecord.RecordGUID = String.Empty;
            labRecords.Add(lastRecord);

            for (int i = 0; i < labRecords.Count; i++)
            {
                PackageCaseFieldData fieldData = labRecords[i];

                if (i % 200 == 0)
                {
                    if (StatusChanged != null) { StatusChanged(string.Format(UnpackagerStrings.IMPORT_FIELD_PROGRESS, (i / total).ToString("P0"))); }
                    if (UpdateProgress != null) { UpdateProgress((i / total) * 100); }
                }

                //string guid = fieldData.RecordGUID;
                string labId = fieldData.RecordLabId;
                bool isLast = fieldData.Equals(lastRecord);
                Page currentPage = fieldData.Page;

                if ((previousPage != currentPage && previousPage != null) || isLast || fieldsInQuery.Contains(fieldData.FieldName))
                {
                    // run the update with the fields we currently have...

                    string updateHeader = String.Empty;
                    string whereClause = String.Empty;
                    StringBuilder sb = new StringBuilder();

                    // Build the Update statement which will be reused
                    sb.Append(SqlKeyWords.UPDATE);
                    sb.Append(StringLiterals.SPACE);
                    sb.Append(db.InsertInEscape(previousPage.TableName));
                    sb.Append(StringLiterals.SPACE);
                    sb.Append(SqlKeyWords.SET);
                    sb.Append(StringLiterals.SPACE);

                    updateHeader = sb.ToString();

                    sb.Remove(0, sb.ToString().Length);

                    // Build the WHERE caluse which will be reused
                    sb.Append(SqlKeyWords.WHERE);
                    sb.Append(StringLiterals.SPACE);
                    sb.Append("[" + ContactTracing.Core.Constants.FIELD_LAB_SPEC_COLUMN_NAME + "]");
                    sb.Append(StringLiterals.EQUAL);
                    sb.Append("'");
                    sb.Append(lastLabId);
                    sb.Append("'");
                    whereClause = sb.ToString();

                    sb.Remove(0, sb.ToString().Length);

                    sb.Append(StringLiterals.LEFT_SQUARE_BRACKET);
                    sb.Append(fieldData.FieldName);
                    sb.Append(StringLiterals.RIGHT_SQUARE_BRACKET);
                    sb.Append(StringLiterals.EQUAL);

                    sb.Append(StringLiterals.COMMERCIAL_AT);
                    sb.Append(fieldData.FieldName);

                    if (fieldsInQuery.Count > 0 && fieldValueParams.Count > 0)
                    {
                        Query updateQuery = db.CreateQuery(updateHeader + StringLiterals.SPACE + setFieldText.ToString() + StringLiterals.SPACE + whereClause);
                        updateQuery.Parameters = fieldValueParams;

                        if (DestinationProject.CollectedDataDriver.ToLower().Contains("epi.data.office"))
                        {
                            IDbCommand command = GetCommand(updateQuery.SqlStatement, Conn, updateQuery.Parameters);
                            object obj = command.ExecuteNonQuery();
                        }
                        else
                        {
                            db.ExecuteNonQuery(updateQuery);
                        }
                    }

                    setFieldText = new WordBuilder(StringLiterals.COMMA);
                    fieldValueParams = new List<QueryParameter>();
                    fieldsInQuery = new List<string>();

                    if (isLast) { break; }
                }

                //if (destinationCaseIds.ContainsKey(caseId) && destinationCaseIds[caseId] == true)
                if (destinationLabIds.ContainsKey(labId) && destinationLabIds[labId] == true)
                {
                    QueryParameter parameter = GetQueryParameterForField(fieldData, labForm, fieldData.Page);
                    fieldsInQuery.Add(fieldData.FieldName);
                    if (parameter != null)
                    {
                        setFieldText.Append(db.InsertInEscape(fieldData.FieldName) + " = " + "@" + fieldData.FieldName);
                        fieldValueParams.Add(parameter);
                    }
                    lastLabId = labId;
                    previousPage = currentPage;
                }
            }

            string queryText = "SELECT " +
                "[ID], " +
                "[" + ContactTracing.Core.Constants.FIELD_LAB_SPEC_COLUMN_NAME + "], " +
                labForm.TableName + ".GlobalRecordId AS GlobalRecordId, " +
                labForm.TableName + ".FKEY as FKEY, " +
                labForm.TableName + ".RecStatus as RecStatus " +
                "FROM " + labForm.Pages[0].TableName + " " +
                "INNER JOIN " + labForm.TableName + " ON " + labForm.Pages[0].TableName + ".GlobalRecordId = " + labForm.TableName + ".GlobalRecordId" + " " +
                "WHERE ([" + ContactTracing.Core.Constants.FIELD_LAB_SPEC_COLUMN_NAME + "] is null OR [" + ContactTracing.Core.Constants.FIELD_LAB_SPEC_COLUMN_NAME + "] = '')";

            Query selectQuery = db.CreateQuery(queryText);
            DataTable deletionCandidateTable = db.Select(selectQuery);
            List<string> guidsToRemove = new List<string>();
            foreach (DataRow row in deletionCandidateTable.Rows)
            {
                foreach (PackageCaseFieldData fieldData in labRecords)
                {
                    if (fieldData.RecordCaseId == row["ID"].ToString())
                    {
                        guidsToRemove.Add(row["GlobalRecordId"].ToString());
                    }
                }
            }

            // Delete lab records that have been entered by the epis
            foreach (string guid in guidsToRemove)
            {
                Query deleteQuery = db.CreateQuery("DELETE * FROM " + labForm.TableName + " WHERE [GlobalRecordId] = @GUID");
                deleteQuery.Parameters.Add(new QueryParameter("@GUID", DbType.String, guid));
                db.ExecuteNonQuery(deleteQuery);
                foreach (Page page in labForm.Pages)
                {
                    deleteQuery = db.CreateQuery("DELETE * FROM " + page.TableName + " WHERE [GlobalRecordId] = @GUID");
                    deleteQuery.Parameters.Add(new QueryParameter("@GUID", DbType.String, guid));
                    db.ExecuteNonQuery(deleteQuery);
                }
            }

            // Update foreign keys
            Dictionary<string, string> caseGuidPairs = new Dictionary<string, string>();
            View caseForm = this.DestinationProject.Views[Core.Constants.CASE_FORM_NAME];
            //DataTable caseTable = Common.JoinPageTables(db, this.DestinationProject.Views["CaseReportForm"]);
            queryText = "SELECT " +
                "[ID], " +                
                caseForm.TableName + ".GlobalRecordId AS GlobalRecordId, " +
                caseForm.TableName + ".FKEY as FKEY, " +
                caseForm.TableName + ".RecStatus as RecStatus " +                
                "FROM " + caseForm.Pages[0].TableName + " " +
                "INNER JOIN " + caseForm.TableName + " ON " + caseForm.Pages[0].TableName + ".GlobalRecordId = " + caseForm.TableName + ".GlobalRecordId";

            selectQuery = db.CreateQuery(queryText);
            DataTable caseTable = db.Select(selectQuery);
            foreach (DataRow row in caseTable.Rows)
            {
                caseGuidPairs.Add(row["ID"].ToString(), row["GlobalRecordId"].ToString());
            }

            foreach (KeyValuePair<string, string> kvp in caseGuidPairs)
            {
                string caseId = kvp.Key; // the case ID, currently the only thing that matches between records in the two tables
                string fkey = kvp.Value; // the guid from the case table becomes the FKEY in the lab table, hence the fkey var name
                string guid = String.Empty; // the guid that we'll need to use in the lab table for the UPDATE query

                foreach (PackageCaseFieldData fieldData in labRecords)
                {
                    if (fieldData.RecordCaseId == caseId) // so we found a lab record that has a matching case ID value
                    {
                        // find the lab record's global record id
                        foreach (KeyValuePair<string, string> labKvp in labGuidPairs)
                        {
                            if (labKvp.Key == fieldData.RecordLabId)
                            {
                                guid = labKvp.Value;
                            }
                        }

                        // set the lab record's FKEY value to the GUID of the corresponding case
                        Query updateQuery = db.CreateQuery("UPDATE " + labForm.TableName + " SET [FKEY] = @FKEY WHERE [GlobalRecordId] = @GlobalRecordId");
                        updateQuery.Parameters.Add(new QueryParameter("@FKEY", DbType.String, fkey));
                        updateQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, guid));
                        db.ExecuteNonQuery(updateQuery);
                        //break;
                    }
                }
            }
        }

        /// <summary>
        /// Begins the process of importing records from the data package into the destination form
        /// </summary>
        /// <param name="form">The form that will receive the data</param>
        /// <param name="formNode">The XmlNode representing the form</param>
        /// <param name="records">The data to be imported</param>
        private void ImportLabRecords(View labForm, XmlNode formNode, List<PackageCaseFieldData> records)
        {
            ImportInfo.RecordsAppended.Add(labForm, 0);
            ImportInfo.RecordsUpdated.Add(labForm, 0);

            IDbDriver destinationDb = DestinationProject.CollectedData.GetDatabase();
            Dictionary<string, bool> destinationGuids = new Dictionary<string, bool>();
            Dictionary<string, bool> destinationCaseIds = new Dictionary<string, bool>();
            Dictionary<string, bool> destinationLabIds = new Dictionary<string, bool>();

            Dictionary<string, string> labGuidPairs = new Dictionary<string, string>();
            //Dictionary<string, string> caseLabPairs = new Dictionary<string, string>();

            using (IDataReader tableReader = destinationDb.GetTableDataReader(labForm.Pages[0].TableName))
            {
                while (tableReader.Read())
                {
                    //destinationCaseIds.Add(tableReader["ID"].ToString(), true);
                    destinationGuids.Add(tableReader["GlobalRecordId"].ToString(), true);

                    if (!String.IsNullOrEmpty(tableReader["FieldLabSpecID"].ToString()))
                    {
                        destinationLabIds.Add(tableReader["FieldLabSpecID"].ToString(), true);
                    }
                    //caseLabPairs.Add(tableReader["ID"].ToString(), tableReader["FieldLabSpecID"].ToString());
                }
            }

            foreach (XmlNode recordNode in formNode.ChildNodes[1].ChildNodes)
            {
                if (recordNode.Name.ToLower().Equals("record"))
                {
                    string guid = String.Empty;
                    string caseId = String.Empty;
                    string fieldLabSpecId = String.Empty;
                    string fkey = String.Empty;
                    string firstSaveId = String.Empty;
                    string lastSaveId = String.Empty;
                    DateTime? firstSaveTime = null;
                    DateTime? lastSaveTime = null;

                    foreach (XmlAttribute attrib in recordNode.Attributes)
                    {
                        if (attrib.Name.ToLower().Equals("id")) guid = attrib.Value;
                        if (attrib.Name.ToLower().Equals("caseid")) caseId = attrib.Value;
                        if (attrib.Name.ToLower().Equals("fieldlabspecid")) fieldLabSpecId = attrib.Value;
                        if (attrib.Name.ToLower().Equals("fkey")) fkey = attrib.Value;
                        if (attrib.Name.ToLower().Equals("firstsaveuserid")) firstSaveId = attrib.Value;
                        if (attrib.Name.ToLower().Equals("lastsaveuserid")) lastSaveId = attrib.Value;
                        if (attrib.Name.ToLower().Equals("firstsavetime")) firstSaveTime = new DateTime(Convert.ToInt64(attrib.Value));
                        if (attrib.Name.ToLower().Equals("lastsavetime")) lastSaveTime = new DateTime(Convert.ToInt64(attrib.Value));
                    }

                    if (!destinationLabIds.ContainsKey(fieldLabSpecId))
                    {
                        if (Append)
                        {
                            if (!String.IsNullOrEmpty(fieldLabSpecId))
                            {
                                destinationLabIds.Add(fieldLabSpecId, true);
                                CreateNewBlankLabRow(labForm, guid, caseId, fieldLabSpecId, fkey, firstSaveId, lastSaveId, firstSaveTime, lastSaveTime);
                                //caseGuidPairs.Add(caseId, guid);
                                labGuidPairs.Add(fieldLabSpecId, guid);
                                ImportInfo.TotalRecordsAppended++;
                                ImportInfo.RecordsAppended[labForm]++;
                            }
                        }
                    }
                    else
                    {
                        if (!Update)
                        {
                            //destinationGuids[guid] = false;
                            destinationLabIds[fieldLabSpecId] = false;
                        }
                        else
                        {
                            if (!String.IsNullOrEmpty(fieldLabSpecId))
                            {
                                ImportInfo.TotalRecordsUpdated++;
                                ImportInfo.RecordsUpdated[labForm]++;
                                //caseGuidPairs.Add(caseId, guid);
                                labGuidPairs.Add(fieldLabSpecId, guid);
                                if (!destinationLabIds.ContainsKey(fieldLabSpecId))
                                {
                                    destinationLabIds.Add(fieldLabSpecId, true);
                                }
                            }
                        }
                    }
                }
            }

            ImportLabRecordsToPageTables(labForm, records, destinationLabIds, labGuidPairs);
        }

        /// <summary>
        /// Begins the process of importing records from the data package into the destination form
        /// </summary>
        /// <param name="form">The form that will receive the data</param>
        /// <param name="formNode">The XmlNode representing the form</param>
        /// <param name="records">The data to be imported</param>
        private void ImportCaseRecords(View form, XmlNode formNode, List<PackageCaseFieldData> records)
        {
            ImportInfo.RecordsAppended.Add(form, 0);
            ImportInfo.RecordsUpdated.Add(form, 0);

            IDbDriver destinationDb = DestinationProject.CollectedData.GetDatabase();
            Dictionary<string, bool> destinationGuids = new Dictionary<string, bool>();
            Dictionary<string, bool> originalDestinationGuids = new Dictionary<string, bool>();
            Dictionary<string, bool> destinationCaseIds = new Dictionary<string, bool>();
            Dictionary<string, bool> destinationLabIds = new Dictionary<string, bool>();
            
            Dictionary<string, string> caseGuidPairs = new Dictionary<string, string>();
            Dictionary<string, string> caseLabPairs = new Dictionary<string, string>();

            using (IDataReader tableReader = destinationDb.GetTableDataReader(form.Pages[0].TableName))
            {
                while (tableReader.Read())
                {
                    destinationCaseIds.Add(tableReader["ID"].ToString(), true);
                    destinationGuids.Add(tableReader["GlobalRecordId"].ToString(), true);
                    originalDestinationGuids.Add(tableReader["GlobalRecordId"].ToString(), true);
                    string caseID = tableReader["ID"].ToString();
                    if (!String.IsNullOrEmpty(caseID))
                    {
                        caseGuidPairs.Add(caseID, tableReader["GlobalRecordId"].ToString());
                    }
                }
            }

            foreach (XmlNode recordNode in formNode.ChildNodes[1].ChildNodes)
            {
                if (recordNode.Name.ToLower().Equals("record"))
                {
                    string guid = String.Empty;
                    string caseId = String.Empty;
                    string fkey = String.Empty;
                    string firstSaveId = String.Empty;
                    string lastSaveId = String.Empty;
                    DateTime? firstSaveTime = null;
                    DateTime? lastSaveTime = null;

                    foreach (XmlAttribute attrib in recordNode.Attributes)
                    {
                        if (attrib.Name.ToLower().Equals("id")) guid = attrib.Value;
                        if (attrib.Name.ToLower().Equals("caseid")) caseId = attrib.Value;
                        if (attrib.Name.ToLower().Equals("fkey")) fkey = attrib.Value;
                        if (attrib.Name.ToLower().Equals("firstsaveuserid")) firstSaveId = attrib.Value;
                        if (attrib.Name.ToLower().Equals("lastsaveuserid")) lastSaveId = attrib.Value;
                        if (attrib.Name.ToLower().Equals("firstsavetime")) firstSaveTime = new DateTime(Convert.ToInt64(attrib.Value));
                        if (attrib.Name.ToLower().Equals("lastsavetime")) lastSaveTime = new DateTime(Convert.ToInt64(attrib.Value));
                    }

                    if(!destinationCaseIds.ContainsKey(caseId) || String.IsNullOrEmpty(caseId))
                    {
                        if (Append)
                        {
                            if (!destinationGuids.ContainsKey(guid))
                            {
                                destinationGuids.Add(guid, true);
                            }
                            if (!destinationCaseIds.ContainsKey(caseId))
                            {
                                destinationCaseIds.Add(caseId, true);
                            }

                            if (!originalDestinationGuids.ContainsKey(guid))
                            {
                                CreateNewBlankCaseRow(form, guid, caseId, fkey, firstSaveId, lastSaveId, firstSaveTime, lastSaveTime);
                            }
                            if (!caseGuidPairs.ContainsKey(caseId))
                            {
                                caseGuidPairs.Add(caseId, guid);
                            }
                            ImportInfo.TotalRecordsAppended++;
                            ImportInfo.RecordsAppended[form]++;
                        }
                    }
                    else
                    {
                        if (!Update)
                        {
                            destinationCaseIds[caseId] = false;
                        }
                        else
                        {
                            ImportInfo.TotalRecordsUpdated++;
                            ImportInfo.RecordsUpdated[form]++;
                            if (!caseGuidPairs.ContainsKey(caseId))
                            {
                                caseGuidPairs.Add(caseId, guid);
                            }
                            destinationCaseIds[caseId] = true;
                            //destinationCaseIds.Add(caseId, true);
                        }
                    }
                }
            }

            ImportCaseRecordsToPageTables(form, records, /*destinationGuids*/ destinationCaseIds, caseGuidPairs);
        }
        
        /// <summary>
        /// Returns a native equivalent of a DbParameter
        /// </summary>
        /// <returns>Native equivalent of a DbParameter</returns>
        private OleDbParameter ConvertToNativeParameter(QueryParameter parameter)
        {
            if (parameter.DbType.Equals(DbType.Guid))
            {
                parameter.Value = new Guid(parameter.Value.ToString());
            }

            OleDbParameter param = new OleDbParameter(parameter.ParameterName, CovertToNativeDbType(parameter.DbType), parameter.Size, parameter.Direction, parameter.IsNullable, parameter.Precision, parameter.Scale, parameter.SourceColumn, parameter.SourceVersion, parameter.Value);
            return param;
        }

        /// <summary>
        /// Gets the Access version of a generic DbType
        /// </summary>
        /// <returns>Access version of the generic DbType</returns>
        private OleDbType CovertToNativeDbType(DbType dbType)
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

        /// <summary>
        /// Gets a new command using an existing connection
        /// </summary>
        /// <param name="sqlStatement">The query to be executed against the database</param>
        /// <param name="connection">Parameters for the query to be executed</param>
        /// <param name="parameters">An OleDb command object</param>
        /// <returns></returns>
        private IDbCommand GetCommand(string sqlStatement, IDbConnection connection, List<QueryParameter> parameters)
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

            foreach (QueryParameter parameter in parameters)
            {
                command.Parameters.Add(this.ConvertToNativeParameter(parameter));
            }

            return command;
        }
        
        /// <summary>
        /// Creates a new blank row for a given form's base table and all of its page tables.
        /// </summary>
        /// <param name="form">The form where the row should be added.</param>
        /// <param name="guid">The Guid value to use for the row.</param>
        /// <param name="fkey">The foreign key for the row.</param>
        /// <param name="firstSaveId">The user ID of the first person that saved this record.</param>
        /// <param name="firstSaveTime">The time when the record was first saved.</param>
        /// <param name="lastSaveId">The user ID of the last person that saved this record.</param>
        /// <param name="lastSaveTime">The time when the record was last saved.</param>
        private void CreateNewBlankCaseRow(View form, string guid, string caseId, string fkey = "", string firstSaveId = "", string lastSaveId = "", DateTime? firstSaveTime = null, DateTime? lastSaveTime = null)
        {
            #region Input Validation
            if (string.IsNullOrEmpty(guid)) { throw new ArgumentNullException("guid"); }
            if (form == null) { throw new ArgumentNullException("form"); }
            #endregion // Input Validation

            if (Conn.State != ConnectionState.Open)
            {
                Conn.Open();
            }

            IDbDriver db = DestinationProject.CollectedData.GetDatabase();
            StringBuilder sb = new StringBuilder();
            sb.Append(" insert into ");
            sb.Append(db.InsertInEscape(form.TableName));
            sb.Append(StringLiterals.SPACE);
            sb.Append(StringLiterals.SPACE);

            WordBuilder fields = new WordBuilder(",");
            fields.Append("[GlobalRecordId]");

            if (!string.IsNullOrEmpty(fkey)) { fields.Append("[FKEY]"); }
            if (!string.IsNullOrEmpty(firstSaveId)) { fields.Append("[FirstSaveLogonName]"); }
            if (!string.IsNullOrEmpty(lastSaveId)) { fields.Append("[LastSaveLogonName]"); }
            if (firstSaveTime.HasValue) { fields.Append("[FirstSaveTime]"); }
            if (lastSaveTime.HasValue) { fields.Append("[LastSaveTime]"); }

            sb.Append("(" + fields.ToString() + ")");
            sb.Append(" values (");

            List<QueryParameter> parameters = new List<QueryParameter>();
            WordBuilder values = new WordBuilder(",");
            values.Append("'" + guid + "'");

            if (!string.IsNullOrEmpty(fkey))
            {
                values.Append("@FKEY");
                parameters.Add(new QueryParameter("@FKEY", DbType.String, fkey));
            }
            if (!string.IsNullOrEmpty(firstSaveId))
            {
                values.Append("@FirstSaveLogonName");
                parameters.Add(new QueryParameter("@FirstSaveLogonName", DbType.String, firstSaveId));
            }
            if (!string.IsNullOrEmpty(lastSaveId))
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

            if (DestinationProject.CollectedDataDriver.ToLower().Contains("epi.data.office"))
            {
                IDbCommand command = GetCommand(insertQuery.SqlStatement, Conn, insertQuery.Parameters);
                object obj = command.ExecuteNonQuery();
            }
            else
            {
                db.ExecuteNonQuery(insertQuery);
            }

            foreach (Page page in form.Pages)
            {
                sb = new StringBuilder();
                sb.Append(" insert into ");
                sb.Append(db.InsertInEscape(page.TableName));
                sb.Append(StringLiterals.SPACE);
                sb.Append(StringLiterals.SPACE);
                if (page.Fields.Contains("ID"))
                {
                    sb.Append("([GlobalRecordId], [ID])");
                }
                else
                {
                    sb.Append("([GlobalRecordId])");
                }
                sb.Append(" values (");
                if (page.Fields.Contains("ID"))
                {
                    sb.Append("'" + guid + "', '" + caseId + "'");
                }
                else
                {
                    sb.Append("'" + guid + "'");
                }
                sb.Append(") ");
                insertQuery = db.CreateQuery(sb.ToString());
                if (DestinationProject.CollectedDataDriver.ToLower().Contains("epi.data.office"))
                {
                    IDbCommand command = GetCommand(insertQuery.SqlStatement, Conn, insertQuery.Parameters);
                    object obj = command.ExecuteNonQuery();
                }
                else
                {
                    db.ExecuteNonQuery(insertQuery);
                }
            }
        }

        /// <summary>
        /// Creates a new blank row for a given form's base table and all of its page tables.
        /// </summary>
        /// <param name="form">The form where the row should be added.</param>
        /// <param name="guid">The Guid value to use for the row.</param>
        /// <param name="fkey">The foreign key for the row.</param>
        /// <param name="firstSaveId">The user ID of the first person that saved this record.</param>
        /// <param name="firstSaveTime">The time when the record was first saved.</param>
        /// <param name="lastSaveId">The user ID of the last person that saved this record.</param>
        /// <param name="lastSaveTime">The time when the record was last saved.</param>
        private void CreateNewBlankLabRow(View labForm, string guid, string caseId, string labId, string fkey = "", string firstSaveId = "", string lastSaveId = "", DateTime? firstSaveTime = null, DateTime? lastSaveTime = null)
        {
            #region Input Validation
            if (string.IsNullOrEmpty(guid)) { throw new ArgumentNullException("guid"); }
            if (labForm == null) { throw new ArgumentNullException("labForm"); }
            #endregion // Input Validation

            if (Conn.State != ConnectionState.Open)
            {
                Conn.Open();
            }

            IDbDriver db = DestinationProject.CollectedData.GetDatabase();
            StringBuilder sb = new StringBuilder();
            sb.Append(" insert into ");
            sb.Append(db.InsertInEscape(labForm.TableName));
            sb.Append(StringLiterals.SPACE);
            sb.Append(StringLiterals.SPACE);

            WordBuilder fields = new WordBuilder(",");
            fields.Append("[GlobalRecordId]");

            if (!string.IsNullOrEmpty(fkey)) { fields.Append("[FKEY]"); }
            if (!string.IsNullOrEmpty(firstSaveId)) { fields.Append("[FirstSaveLogonName]"); }
            if (!string.IsNullOrEmpty(lastSaveId)) { fields.Append("[LastSaveLogonName]"); }
            if (firstSaveTime.HasValue) { fields.Append("[FirstSaveTime]"); }
            if (lastSaveTime.HasValue) { fields.Append("[LastSaveTime]"); }

            sb.Append("(" + fields.ToString() + ")");
            sb.Append(" values (");

            List<QueryParameter> parameters = new List<QueryParameter>();
            WordBuilder values = new WordBuilder(",");
            values.Append("'" + guid + "'");

            if (!string.IsNullOrEmpty(fkey))
            {
                values.Append("@FKEY");
                parameters.Add(new QueryParameter("@FKEY", DbType.String, fkey));
            }
            if (!string.IsNullOrEmpty(firstSaveId))
            {
                values.Append("@FirstSaveLogonName");
                parameters.Add(new QueryParameter("@FirstSaveLogonName", DbType.String, firstSaveId));
            }
            if (!string.IsNullOrEmpty(lastSaveId))
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

            if (DestinationProject.CollectedDataDriver.ToLower().Contains("epi.data.office"))
            {
                IDbCommand command = GetCommand(insertQuery.SqlStatement, Conn, insertQuery.Parameters);
                object obj = command.ExecuteNonQuery();
            }
            else
            {
                db.ExecuteNonQuery(insertQuery);
            }

            foreach (Page page in labForm.Pages)
            {
                sb = new StringBuilder();
                sb.Append(" insert into ");
                sb.Append(db.InsertInEscape(page.TableName));
                sb.Append(StringLiterals.SPACE);
                sb.Append(StringLiterals.SPACE);
                if (page.Fields.Contains("ID") && page.Fields.Contains("FieldLabSpecID"))
                {
                    sb.Append("([GlobalRecordId], [ID], [FieldLabSpecID])");
                }
                else
                {
                    sb.Append("([GlobalRecordId])");
                }
                sb.Append(" values (");
                if (page.Fields.Contains("ID") && page.Fields.Contains("FieldLabSpecID"))
                {
                    sb.Append("'" + guid + "', '" + caseId + "', '" + labId + "'");
                }
                else
                {
                    sb.Append("'" + guid + "'");
                }
                sb.Append(") ");
                insertQuery = db.CreateQuery(sb.ToString());
                if (DestinationProject.CollectedDataDriver.ToLower().Contains("epi.data.office"))
                {
                    IDbCommand command = GetCommand(insertQuery.SqlStatement, Conn, insertQuery.Parameters);
                    object obj = command.ExecuteNonQuery();
                }
                else
                {
                    db.ExecuteNonQuery(insertQuery);
                }
            }
        }

        /// <summary>
        /// Gets the appropriate query parameter for a given field.
        /// </summary>
        /// <param name="fieldData">The field data to use to generate the parameter.</param>
        /// <param name="destinationForm">The form on which the field resides.</param>
        /// <param name="sourcePage">The page on the form in which the field resides.</param>
        /// <returns>QueryParameter</returns>
        private QueryParameter GetQueryParameterForField(PackageCaseFieldData fieldData, View destinationForm, Page sourcePage)
        {
            Field dataField = destinationForm.Fields[fieldData.FieldName];
            if (!(
                dataField is GroupField ||
                dataField is RelatedViewField ||
                dataField is UniqueKeyField ||
                dataField is RecStatusField ||
                dataField is GlobalRecordIdField ||
                fieldData.FieldValue == null ||
                string.IsNullOrEmpty(fieldData.FieldValue.ToString()
                )))
            {
                String fieldName = ((Epi.INamedObject)dataField).Name;
                switch (dataField.FieldType)
                {
                    case MetaFieldType.Date:
                    case MetaFieldType.DateTime:
                    case MetaFieldType.Time:
                        DateTime dt = new DateTime(Convert.ToInt64(fieldData.FieldValue));
                        return new QueryParameter("@" + fieldName, DbType.DateTime, dt);
                    case MetaFieldType.Checkbox:
                        return new QueryParameter("@" + fieldName, DbType.Boolean, Convert.ToBoolean(fieldData.FieldValue));
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
                        return new QueryParameter("@" + fieldName, DbType.String, fieldData.FieldValue);
                    case MetaFieldType.Number:
                    case MetaFieldType.YesNo:
                    case MetaFieldType.RecStatus:
                        return new QueryParameter("@" + fieldName, DbType.Single, fieldData.FieldValue);
                    case MetaFieldType.Image:
                        //throw new ApplicationException("Not a supported field type");
                        return new QueryParameter("@" + fieldName, DbType.Binary, Convert.FromBase64String(fieldData.FieldValue.ToString()));
                    case MetaFieldType.Option:
                        return new QueryParameter("@" + fieldName, DbType.Single, fieldData.FieldValue);
                    //this.BeginInvoke(new SetStatusDelegate(AddWarningMessage), "The data for " + fieldName + " was not imported. This field type is not supported.");
                    default:
                        throw new ApplicationException("Not a supported field type");
                }
            }

            return null;
        }
        
        /// <summary>
        /// Formats field data
        /// </summary>
        /// <param name="fieldName">The name of the field whose data should be formatted</param>
        /// <param name="value">The value that needs to be formatted</param>
        /// <returns>The formatted value</returns>
        private object FormatFieldData(string fieldName, object value)
        {
            if (DestinationForm.Fields.Contains(fieldName))
            {
                Field field = DestinationForm.Fields[fieldName];

                if (field is CheckBoxField)
                {
                    if (value.ToString().ToLower().Equals("true"))
                    {
                        value = true;
                    }
                    else if (value.ToString().ToLower().Equals("false"))
                    {
                        value = false;
                    }
                }

                if (field is YesNoField)
                {
                    if (value.ToString().ToLower().Equals("1"))
                    {
                        value = 1;
                    }
                    else if (value.ToString().ToLower().Equals("0"))
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
        #endregion // Private Methods
    }
}
