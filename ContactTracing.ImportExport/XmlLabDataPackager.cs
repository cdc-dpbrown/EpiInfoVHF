using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Xml;
using Epi;
using Epi.Data;
using Epi.Fields;
using Epi.ImportExport.Filters;

namespace ContactTracing.ImportExport
{
    /// <summary>
    /// A class used to turn the data for an Epi Info 7 form (and the data for any forms related to it) into Xml format. Row filtering is supported through the use of the
    /// Filters property, and fields data can be erased from the Xml by using the FieldsToNull property. Call the PackageForm() public method to start the packaging
    /// process; an XmlDocument object will be returned, representing the form's data and any data from descendant forms.
    /// </summary>
    public class XmlLabDataPackager
    {
        #region Events
        public event SetProgressBarDelegate UpdateProgress;
        public event SimpleEventHandler ResetProgress;
        public event UpdateStatusEventHandler StatusChanged;
        #endregion // Events

        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sourceForm">The form within the project whose data will be packaged.</param>
        /// <param name="packageName">The name of the data package.</param>
        public XmlLabDataPackager(View sourceForm, string packageName)
        {
            #region Input Validation
            if (sourceForm == null) { throw new ArgumentNullException("sourceForm"); }
            if (string.IsNullOrEmpty(packageName)) { throw new ArgumentNullException("packageName"); }
            #endregion // Input Validation

            SourceForm = sourceForm;
            SourceProject = SourceForm.Project;
            PackageName = packageName;
            GridColumnsToNull = new Dictionary<string, List<string>>();
            FieldsToNull = new Dictionary<string, List<string>>();
            ParentIdList = new List<string>();
            CurrentDistanceFromRoot = 0;
            PreviousDistanceFromRoot = 0;
        }
        #endregion // Constructors

        #region Properties
        /// <summary>
        /// Gets/sets the source form within the project that will be used for the packaging routine.
        /// </summary>
        public View SourceForm { get; private set; }

        /// <summary>
        /// Gets/sets the name of the package.
        /// </summary>
        public string PackageName { get; private set; }

        /// <summary>
        /// Gets/sets the results of the packaging process
        /// </summary>
        public Epi.ImportExport.ProjectPackagers.ExportInfo ExportInfo { get; private set; }

        /// <summary>
        /// Gets/sets the list of fields whose data should be erased during the packaging process. The dictionary key is the name of the form; the list of strings represent the field names within the form that should be erased.
        /// </summary>
        public Dictionary<string, List<string>> FieldsToNull { get; set; }

        /// <summary>
        /// Gets/sets the list of filters to apply to each form. The dictionary key is the name of the form and the RowFilters object represents the group of filters to be applied to that form.
        /// </summary>
        public Dictionary<string, RowFilters> Filters { get; set; }

        /// <summary>
        /// Gets/sets the list of grid columns whose data should be erased during the packaging process
        /// </summary>
        public Dictionary<string, List<string>> GridColumnsToNull { get; set; }

        /// <summary>
        /// Gets/sets the source project for the packaging routine
        /// </summary>
        private Project SourceProject { get; set; }

        /// <summary>
        /// Gets/sets a dictionary of GUID string and XmlElement key value pairs.
        /// </summary>
        private Dictionary<string, XmlElement> IdList { get; set; }

        /// <summary>
        /// Gets/sets a list of GUIDs for the parent form for whichever form is currently being processed.
        /// </summary>
        private List<string> ParentIdList { get; set; }

        /// <summary>
        /// Gets/sets the distance away from the root (parent) node of the form being packaged.
        /// </summary>
        /// <remarks>
        /// For example, the parent form would have a distance of 0. A form related to the parent (child form) would have a distance 
        /// of 1. A form related to a form related to the parent (grandchild form) would have a distance of 2. This property is used
        /// to ensure that Guid lists don't get refreshed when moving to another related form that is the same distance as a related
        /// form that was processed previously.
        /// </remarks>
        private int CurrentDistanceFromRoot { get; set; }

        /// <summary>
        /// Gets/sets the distance away from the root (parent) node of the form that was packaged prior to the current form.
        /// </summary>        
        private int PreviousDistanceFromRoot { get; set; }
        #endregion // Properties

        #region Public Methods
        /// <summary>
        /// Initiates form packaging and returns the corresponding package in Xml format.
        /// </summary>
        /// <remarks>
        /// The returned Xml is not encrypted and not compressed. The calling object is responsible for that, should it be desired.
        /// </remarks>
        public XmlDocument PackageForm()
        {
            if (StatusChanged != null) { StatusChanged("Starting package processing..."); }

            ExportInfo = new Epi.ImportExport.ProjectPackagers.ExportInfo();
            ExportInfo.UserID = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString();
            ExportInfo.ExportInitiated = DateTime.Now;
            ExportInfo.FormsProcessed = 0;

            CheckForProblems(); // check to see if problems exist in the data or metadata that is about to be exported

            IdList = new Dictionary<string, XmlElement>();
            XmlDocument xmlDataPackage = new XmlDocument();
            XmlElement root = xmlDataPackage.CreateElement("DataPackage");
            CreateRootAttributes(xmlDataPackage, root);

            // Process the parent form first, before getting to any relational forms...
            root.AppendChild(CreateXmlFormElement(xmlDataPackage, SourceForm));
            ExportInfo.FormsProcessed++;

            // ... now that we've processed the parent, we want to process each related form. But we want to process
            // those related forms in order, based on how far away they are from the parent (the root). This is necessary
            // so that we can take the GUIDs from the last parent processed and use them to delete orphans.
            List<View> formsToProcess = new List<View>();
            SortedDictionary<int, List<View>> forms = new SortedDictionary<int, List<View>>();

            // Iterate over all the views in the source project...
            foreach (View form in SourceProject.Views)
            {
                // Don't process the parent, since we already did that above
                if (form.Name == SourceForm.Name) { continue; }

                // If the form is a descendant of the source form...
                if (Epi.ImportExport.ImportExportHelper.IsFormDescendant(form, SourceForm))
                {
                    // Include this form in the dictionary of forms to process
                    // Note: We're sorting these so that the forms are generated in top-to-bottom order in the Xml
                    int level = Epi.ImportExport.ImportExportHelper.GetFormDescendantLevel(form, SourceForm, 0);
                    if (!forms.ContainsKey(level))
                    {
                        forms.Add(level, new List<View>());
                    }
                    forms[level].Add(form);
                }
            }

            foreach (KeyValuePair<int, List<View>> kvp in forms)
            {
                foreach (View form in kvp.Value)
                {
                    CurrentDistanceFromRoot = kvp.Key;
                    root.AppendChild(CreateXmlFormElement(xmlDataPackage, form));
                    PreviousDistanceFromRoot = kvp.Key;
                    ExportInfo.FormsProcessed++;
                }
            }

            xmlDataPackage.AppendChild(root);

            if (StatusChanged != null) { StatusChanged(PackagerStrings.PACKAGE_CREATED); }

            return xmlDataPackage;
        }
        #endregion // Public Methods

        #region Private Methods
        /// <summary>
        /// Checks for problems in the source project
        /// </summary>
        private void CheckForProblems()
        {
            IDbDriver driver = SourceProject.CollectedData.GetDatabase();

            // Check #1 - Make sure the base table exists and that it has a Global Record Id field, Record status field, and Unique key field.
            DataTable dt = driver.GetTableData(SourceForm.TableName, "GlobalRecordId, RECSTATUS, UniqueKey");
            int baseTableRowCount = dt.Rows.Count;

            // Check #2a - Make sure GlobalRecordId is a string.
            if (!dt.Columns[0].DataType.ToString().Equals("System.String"))
            {
                ExportInfo.Succeeded = false;
                ExportInfo.AddError(ImportExportSharedStrings.ERROR_PACKAGER_CHECK_INVALID_GUID_COLUMN, String.Empty);
                throw new ApplicationException(ImportExportSharedStrings.ERROR_PACKAGER_CHECK_INVALID_GUID_COLUMN);
            }

            // Check #2b - Make sure RECSTATUS is a number
            if (!(dt.Columns[1].DataType.ToString().Equals("System.Byte") || dt.Columns[1].DataType.ToString().Equals("System.Int16") || dt.Columns[1].DataType.ToString().Equals("System.Int32")))
            {
                ExportInfo.Succeeded = false;
                ExportInfo.AddError(ImportExportSharedStrings.ERROR_PACKAGER_CHECK_INVALID_RECSTATUS_COLUMN, String.Empty);
                throw new ApplicationException(ImportExportSharedStrings.ERROR_PACKAGER_CHECK_INVALID_RECSTATUS_COLUMN);
            }

            // Check #3 - Make sure GlobalRecordId values haven't been replaced with something that isn't actually a GUID. 
            //      For performance reasons only the first few values are checked.
            if (baseTableRowCount >= 1)
            {
                string value = dt.Rows[0][0].ToString();
                System.Guid guid = new Guid(value);

                if (baseTableRowCount >= 30)
                {
                    for (int i = 0; i < 30; i++)
                    {
                        value = dt.Rows[i][0].ToString();
                        guid = new Guid(value);
                    }
                }
            }

            // Check #4a - See if global record ID values are distinct on the base table.
            Query selectDistinctQuery = driver.CreateQuery("SELECT DISTINCT [GlobalRecordId] FROM [" + SourceForm.TableName + "]");
            DataTable distinctTable = driver.Select(selectDistinctQuery);
            if (distinctTable.Rows.Count != baseTableRowCount)
            {
                ExportInfo.Succeeded = false;
                ExportInfo.AddError(ImportExportSharedStrings.ERROR_PACKAGER_CHECK_GUID_NOT_UNIQUE, String.Empty);
                throw new ApplicationException(ImportExportSharedStrings.ERROR_PACKAGER_CHECK_GUID_NOT_UNIQUE);
            }

            // Check #4b - See if global record ID values are distinct on each page table.
            foreach (Page page in SourceForm.Pages)
            {
                selectDistinctQuery = driver.CreateQuery("SELECT DISTINCT [GlobalRecordId] FROM [" + page.TableName + "]");
                distinctTable = driver.Select(selectDistinctQuery);
                if (distinctTable.Rows.Count != baseTableRowCount)
                {
                    ExportInfo.Succeeded = false;
                    ExportInfo.AddError(ImportExportSharedStrings.ERROR_PACKAGER_CHECK_GUID_NOT_UNIQUE_PAGE, String.Empty);
                    throw new ApplicationException(string.Format(ImportExportSharedStrings.ERROR_PACKAGER_CHECK_GUID_NOT_UNIQUE_PAGE, page.TableName));
                }
            }

            // Check #5 - Make sure RECSTATUS has valid values.
            selectDistinctQuery = driver.CreateQuery("SELECT DISTINCT [RecStatus] FROM [" + SourceForm.TableName + "]");
            distinctTable = driver.Select(selectDistinctQuery);
            foreach (DataRow row in distinctTable.Rows)
            {
                if (!row[0].ToString().Equals("1") && !row[0].ToString().Equals("0"))
                {
                    ExportInfo.Succeeded = false;
                    ExportInfo.AddError(ImportExportSharedStrings.ERROR_PACKAGER_CHECK_RECSTATUS_VALUES_INVALID, String.Empty);
                    throw new ApplicationException(ImportExportSharedStrings.ERROR_PACKAGER_CHECK_RECSTATUS_VALUES_INVALID);
                }
            }

            // Check #7 - Should never get here because the UI should prevent it, but do a check just in case
            if (SourceForm.IsRelatedView == true)
            {
                ExportInfo.Succeeded = false;
                ExportInfo.AddError(ImportExportSharedStrings.ERROR_PACKAGER_CHECK_RELATED_FORM, String.Empty);
                throw new ApplicationException(ImportExportSharedStrings.ERROR_PACKAGER_CHECK_RELATED_FORM);
            }

            distinctTable = null;
            selectDistinctQuery = null;
            driver.Dispose();
            driver = null;
        }

        /// <summary>
        /// Creates attributes for the root element
        /// </summary>
        /// <param name="xmlDataPackage">The Xml Data Package document</param>
        /// <param name="root">The root element of the document</param>
        private void CreateRootAttributes(XmlDocument xmlDataPackage, XmlElement root)
        {
            #region Input Validation
            if (xmlDataPackage == null) { throw new ArgumentNullException("xmlDataPackage"); }
            if (root == null) { throw new ArgumentNullException("root"); }
            #endregion // Input Validation

            Epi.ApplicationIdentity appId = new Epi.ApplicationIdentity(typeof(Configuration).Assembly);

            // Append timestamp in UTC so that people sending packages across time zones don't need to worry about what zone it was packaged in.
            DateTime dt = DateTime.UtcNow;
            string dateDisplayValue = string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0:s}", dt);

            XmlAttribute version = xmlDataPackage.CreateAttribute("Version"); // The version of Epi Info 7 that was used to create it
            XmlAttribute created = xmlDataPackage.CreateAttribute("Created"); // The date/time the package creation started
            XmlAttribute pakName = xmlDataPackage.CreateAttribute("Name"); // The name of the package (should mirror the file name but without a filename-based timestamp)
            XmlAttribute guidStr = xmlDataPackage.CreateAttribute("Id"); // Unique ID value for the package; may be useful to avoid importing the same package twice depending on how system designers have set up their import mechanisms

            version.Value = appId.Version;
            created.Value = dateDisplayValue;
            pakName.Value = PackageName;
            guidStr.Value = System.Guid.NewGuid().ToString();

            root.Attributes.Append(version);
            root.Attributes.Append(created);
            root.Attributes.Append(pakName);
            root.Attributes.Append(guidStr);
        }

        /// <summary>
        /// Creates an XmlElement representing an Epi Info 7 view.
        /// </summary>
        /// <param name="xmlDataPackage">The data package xml document that the XmlElement should be added to</param>
        /// <param name="form">The form to be serialized</param>        
        /// <returns>XmlElement; represents the view in Xml format, suitable for use in data packaging</returns>
        private XmlElement CreateXmlFormElement(XmlDocument xmlDataPackage, View form)
        {
            #region Input Validation
            if (xmlDataPackage == null) { throw new ArgumentNullException("xmlDataPackage"); }
            if (form == null) { throw new ArgumentNullException("form"); }
            #endregion // Input Validation

            XmlElement formElement = xmlDataPackage.CreateElement("Form");

            XmlAttribute name = xmlDataPackage.CreateAttribute("Name");
            XmlAttribute pages = xmlDataPackage.CreateAttribute("Pages"); // The page count is here so we can check to see if the # of pages match between the Xml document and the destination project during import
            XmlAttribute related = xmlDataPackage.CreateAttribute("IsRelatedForm");

            name.Value = form.Name;
            pages.Value = form.Pages.Count.ToString();
            related.Value = form.IsRelatedView.ToString();

            formElement.Attributes.Append(name);
            formElement.Attributes.Append(pages);
            formElement.Attributes.Append(related);

            if (form.IsRelatedView)
            {
                // This attribute points to the name of the parent form
                XmlAttribute parent = xmlDataPackage.CreateAttribute("ParentForm");
                parent.Value = form.ParentView.Name;
                formElement.Attributes.Append(parent);

                // This attribute sets the 'descendent level' of the form. For example, a parent form has a level of 0. A
                // child of that parent has a level 1. A grandchild has a level of 2. And so on.
                XmlAttribute level = xmlDataPackage.CreateAttribute("DescendantLevel");
                level.Value = Epi.ImportExport.ImportExportHelper.GetFormDescendantLevel(form, SourceForm, 0).ToString();
                formElement.Attributes.Append(level);
            }

            formElement.AppendChild(CreateXmlFormMetadataElement(xmlDataPackage, form));
            formElement.AppendChild(CreateXmlFormDataElement(xmlDataPackage, form));

            return formElement;
        }

        /// <summary>
        /// Creates an XmlElement representing an Epi Info 7 view's metadata.
        /// </summary>
        /// <param name="xmlDataPackage">The data package xml document that the XmlElement should be added to</param>
        /// <param name="form">The form whose metadata will be serialized</param>
        /// <returns>XmlElement; represents the view's metadata in Xml format, suitable for use in data packaging</returns>
        private XmlElement CreateXmlFormMetadataElement(XmlDocument xmlDataPackage, View form)
        {
            #region Input Validation
            if (xmlDataPackage == null) { throw new ArgumentNullException("xmlDataPackage"); }
            if (form == null) { throw new ArgumentNullException("form"); }
            #endregion // Input Validation

            if (StatusChanged != null) { StatusChanged(string.Format(PackagerStrings.ADDING_FIELD_METADATA, form.Name)); }

            XmlElement fields = xmlDataPackage.CreateElement("FieldMetadata");

            foreach (Field field in form.Fields)
            {
                if (field is IDataField && field is RenderableField)
                {
                    RenderableField renderableField = field as RenderableField;

                    if (renderableField != null)
                    {
                        XmlElement fieldInfo = xmlDataPackage.CreateElement("FieldInfo");

                        XmlAttribute name = xmlDataPackage.CreateAttribute("Name");
                        XmlAttribute type = xmlDataPackage.CreateAttribute("FieldType");
                        XmlAttribute page = xmlDataPackage.CreateAttribute("Page"); // records page position, NOT page id and NOT page name

                        name.Value = renderableField.Name;
                        type.Value = renderableField.FieldType.ToString();
                        page.Value = renderableField.Page.Position.ToString();

                        fieldInfo.Attributes.Append(name);
                        fieldInfo.Attributes.Append(type);
                        fieldInfo.Attributes.Append(page);

                        fields.AppendChild(fieldInfo);
                    }
                }
            }

            return fields;
        }

        /// <summary>
        /// Creates an XmlElement representing an Epi Info 7 view's data.
        /// </summary>
        /// <param name="xmlDataPackage">The data package xml document that the XmlElement should be added to</param>
        /// <param name="form">The form whose data will be serialized</param>
        /// <returns>XmlElement; represents the form's data in Xml format, suitable for use in data packaging</returns>
        private XmlElement CreateXmlFormDataElement(XmlDocument xmlDataPackage, View form)
        {
            #region Input Validation
            if (xmlDataPackage == null) { throw new ArgumentNullException("xmlDataPackage"); }
            if (form == null) { throw new ArgumentNullException("form"); }
            #endregion // Input Validation

            XmlElement data = xmlDataPackage.CreateElement("Data");

            if (StatusChanged != null) { StatusChanged(string.Format(PackagerStrings.GUID_LIST_SETUP, form.Name)); }
            if (ResetProgress != null) { ResetProgress(); }

            /* This seems like an usual set of steps to just iterate over the data. The problem is that we can't "just
             * iterate over the data" - the data is split up page tables, with one table representing one page on the
             * form. While a JOIN might be able to bring everything together into one table, it might not - for example,
             * if there are >255 fields after the JOIN, an OleDb exception will be thrown.
             * 
             * To get around this issue: The code first iterates over the rows in the BASE TABLE, obtaining the GUID 
             * values for each. The GUIDs and their corresponding XmlElement go into a dictionary.
             * 
             * Later, each row in each page is iterated over; as the GUIDs for each page table are accessed, the corresponding 
             * XmlElement is pulled from the dictionary. Field data is added to it for each field that has data. In this
             * manner, it doesn't matter that each row is technically accessed out-of-order because they'll still show up
             * in-order in the resulting Xml.
             * 
             * Filtering adds another layer of complexity. To filter, a JOIN operation is needed so that the filters can
             * be applied across all those tables, since the fields in the filter may be across different tables. The
             * RowFilter class provides a way to handle this; we simply get the query from that object and apply it to the
             * reader. Only GUIDs that match the filter are added to the dictionary of guids.
             */

            // We need to exclude records from child forms that may now be orphaned as a result of a filter applied to the parent            
            if (form.IsRelatedView && PreviousDistanceFromRoot < CurrentDistanceFromRoot)
            {
                ParentIdList.Clear();
                foreach (KeyValuePair<string, XmlElement> kvp in IdList) { ParentIdList.Add(kvp.Key); }
            }

            IdList.Clear(); // Very important, this needs to be re-set in case we've already processed a form (this is a class level variable)

            if (!ExportInfo.RecordsPackaged.ContainsKey(form))
            {
                ExportInfo.RecordsPackaged.Add(form, 0);
            }

            bool filterThisForm = false;
            RowFilters filters = null;
            Query selectQuery = null;

            if (Filters != null && Filters.ContainsKey(form.Name) && Filters[form.Name].Count() > 0)
            {
                filterThisForm = true;
                filters = Filters[form.Name];
                selectQuery = filters.GetGuidSelectQuery(form);
            }

            double totalRecords = 0;

            string queryText = String.Empty;
            List<QueryParameter> parameters = new List<QueryParameter>();

            if (form.Name.StartsWith("Lab"))
            {
                queryText = "SELECT " +
                "[ID], " + 
                "[FieldLabSpecID], " + 
                form.TableName + ".GlobalRecordId AS GlobalRecordId, " + 
                form.TableName + ".FKEY as FKEY, " + 
                form.TableName + ".RecStatus as RecStatus, " +

                form.TableName + ".FirstSaveLogonName AS FirstSaveLogonName, " +
                form.TableName + ".FirstSaveTime AS FirstSaveTime, " +
                form.TableName + ".LastSaveLogonName AS LastSaveLogonName, " +
                form.TableName + ".LastSaveTime AS LastSaveTime " +
                "FROM " + form.Pages[0].TableName + " " +
                "INNER JOIN " + form.TableName + " ON " + form.Pages[0].TableName + ".GlobalRecordId = " + form.TableName + ".GlobalRecordId";
                    
            }
            else
            {
                queryText = "SELECT " +
                "[ID], " +                
                form.TableName + ".GlobalRecordId AS GlobalRecordId, " +
                form.TableName + ".FKEY as FKEY, " +
                form.TableName + ".RecStatus as RecStatus, " +

                form.TableName + ".FirstSaveLogonName AS FirstSaveLogonName, " +
                form.TableName + ".FirstSaveTime AS FirstSaveTime, " +
                form.TableName + ".LastSaveLogonName AS LastSaveLogonName, " +
                form.TableName + ".LastSaveTime AS LastSaveTime " +
                "FROM " + form.Pages[0].TableName + " " +
                "INNER JOIN " + form.TableName + " ON " + form.Pages[0].TableName + ".GlobalRecordId = " + form.TableName + ".GlobalRecordId ";
                if(filterThisForm) 
                {
                    queryText = queryText + "WHERE ";
                    if (filters != null)
                    {
                        foreach (Epi.ImportExport.IRowFilterCondition rowFC in filters)
                        {
                            queryText = queryText + rowFC.Sql;
                            parameters.Add(rowFC.Parameter);
                        }
                    }
                }
            }

            Query pageOneQuery = SourceProject.CollectedData.GetDatabase().CreateQuery(queryText);
            pageOneQuery.Parameters = parameters;
            DataTable caseGuidTable = SourceProject.CollectedData.GetDatabase().Select(pageOneQuery);

            using (IDataReader guidReader = SourceProject.CollectedData.ExecuteReader(pageOneQuery)) //= filterThisForm ? SourceProject.CollectedData.GetDatabase().ExecuteReader(selectQuery) : SourceProject.CollectedData.GetDatabase().GetTableDataReader(form.TableName))
            {
                while (guidReader.Read())
                {
                    string caseId = String.Empty;
                    string guid = guidReader["GlobalRecordId"].ToString();
                    string fieldLabSpecId = String.Empty;

                    if (form.Name.StartsWith("Lab"))
                    {
                        fieldLabSpecId = guidReader["FieldLabSpecID"].ToString();
                    }

                    string fkey = guidReader["FKEY"].ToString();
                    string recstatus = guidReader["RECSTATUS"].ToString();
                    string firstSaveUserId = String.Empty;
                    DateTime? firstSaveTime = null;
                    string lastSaveUserId = String.Empty;
                    DateTime? lastSaveTime = null;

                    foreach (DataRow row in caseGuidTable.Rows)
                    {
                        if (row["GlobalRecordId"].ToString() == guid)
                        {
                            caseId = row["ID"].ToString();
                            break;
                        }
                    }

                    if (guidReader.FieldCount > 3)
                    {
                        try
                        {
                            firstSaveUserId = guidReader["FirstSaveLogonName"].ToString();
                            if (guidReader["FirstSaveTime"] != DBNull.Value)
                            {
                                firstSaveTime = (DateTime)guidReader["FirstSaveTime"];
                            }
                            lastSaveUserId = guidReader["LastSaveLogonName"].ToString();
                            if (guidReader["LastSaveTime"] != DBNull.Value)
                            {
                                lastSaveTime = (DateTime)guidReader["LastSaveTime"];
                            }
                        }
                        catch (IndexOutOfRangeException)
                        {
                            // just ignore, probably an older upgraded project
                        }
                    }

                    if (recstatus.Equals("1")) // only include undeleted records
                    {
                        if (!form.IsRelatedView || ParentIdList.Contains(fkey))
                        {
                            XmlElement record = xmlDataPackage.CreateElement("Record");
                            XmlAttribute id = xmlDataPackage.CreateAttribute("Id");
                            id.Value = guid;
                            record.Attributes.Append(id);

                            XmlAttribute caseIdAtr = xmlDataPackage.CreateAttribute("CaseId");
                            caseIdAtr.Value = caseId;
                            record.Attributes.Append(caseIdAtr);
                            
                            if (!string.IsNullOrEmpty(fieldLabSpecId))
                            {
                                XmlAttribute fieldLabId = xmlDataPackage.CreateAttribute("FieldLabSpecId");
                                fieldLabId.Value = fieldLabSpecId;
                                record.Attributes.Append(fieldLabId);
                            }
                            if (!string.IsNullOrEmpty(fkey))
                            {
                                XmlAttribute foreignKey = xmlDataPackage.CreateAttribute("Fkey");
                                foreignKey.Value = fkey;
                                record.Attributes.Append(foreignKey);
                            }
                            if (!string.IsNullOrEmpty(firstSaveUserId))
                            {
                                XmlAttribute firstSaveId = xmlDataPackage.CreateAttribute("FirstSaveUserId");
                                firstSaveId.Value = firstSaveUserId;
                                record.Attributes.Append(firstSaveId);
                            }
                            if (!string.IsNullOrEmpty(lastSaveUserId))
                            {
                                XmlAttribute lastSaveId = xmlDataPackage.CreateAttribute("LastSaveUserId");
                                lastSaveId.Value = lastSaveUserId;
                                record.Attributes.Append(lastSaveId);
                            }
                            if (firstSaveTime.HasValue)
                            {
                                XmlAttribute firstSaveDateTime = xmlDataPackage.CreateAttribute("FirstSaveTime");
                                firstSaveDateTime.Value = firstSaveTime.Value.Ticks.ToString();
                                record.Attributes.Append(firstSaveDateTime);
                            }
                            if (lastSaveTime.HasValue)
                            {
                                XmlAttribute lastSaveDateTime = xmlDataPackage.CreateAttribute("LastSaveTime");
                                lastSaveDateTime.Value = lastSaveTime.Value.Ticks.ToString();
                                record.Attributes.Append(lastSaveDateTime);
                            }
                            IdList.Add(guid, record);
                            totalRecords++;

                            ExportInfo.TotalRecordsPackaged++;
                            ExportInfo.RecordsPackaged[form]++;
                        }
                    }
                }
            }

            totalRecords = totalRecords * form.Pages.Count;
            int processedRecords = 0;

            if (StatusChanged != null) { StatusChanged(string.Format(PackagerStrings.ADDING_FIELD_DATA, form.Name)); }

            foreach (Page page in form.Pages)
            {
                using (IDataReader reader = SourceProject.CollectedData.GetDatabase().GetTableDataReader(page.TableName))
                {
                    while (reader.Read())
                    {
                        string guid = reader["GlobalRecordId"].ToString();

                        if (IdList.ContainsKey(guid))
                        {
                            XmlElement element = IdList[guid];

                            foreach (Field field in page.Fields)
                            {
                                if (field is IDataField && field is RenderableField && !(field is GridField) && !(FieldsToNull.ContainsKey(form.Name) && FieldsToNull[form.Name].Contains(field.Name)))
                                {
                                    RenderableField renderableField = field as RenderableField;
                                    if (renderableField != null)
                                    {
                                        XmlElement fieldData = xmlDataPackage.CreateElement("Field");

                                        XmlAttribute name = xmlDataPackage.CreateAttribute("Name");
                                        name.Value = renderableField.Name;
                                        fieldData.Attributes.Append(name);

                                        string value = reader[field.Name].ToString();

                                        if (!string.IsNullOrEmpty(value))
                                        {
                                            if (field is DateTimeField)
                                            {
                                                DateTime dt = Convert.ToDateTime(value);
                                                fieldData.InnerText = dt.Ticks.ToString();
                                            }
                                            else if (field is ImageField)
                                            {
                                                value = Convert.ToBase64String((Byte[])reader[field.Name]);
                                                fieldData.InnerText = value;
                                            }
                                            else
                                            {
                                                fieldData.InnerText = value;
                                            }
                                        }
                                        element.AppendChild(fieldData);
                                        data.AppendChild(element);
                                    }
                                }
                            }
                        }
                        processedRecords++;
                        double progress = (((double)processedRecords) / ((double)totalRecords)) * 100;
                        if (UpdateProgress != null) { UpdateProgress(progress); }
                    }
                }
            }

            return data;
        }
        #endregion // Private Methods
    }
}