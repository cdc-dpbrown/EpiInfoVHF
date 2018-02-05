#region Using
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Text;
using System.Threading.Tasks;
using Epi;
using Epi.Core;
using Epi.Fields;
using Epi.Data;
#endregion // Using

namespace ContactTracing.ImportExport
{
    /// <summary>
    /// 
    /// A class used to handle importing (merging) data from another Epi Info 7 form.
    /// -- E. Knudsen, 2012
    /// 
    /// Repurposed for VHF 12-24-2014
    /// -- E. Knudsen, 2014
    /// 
    /// </summary>
    public class FormDataImporter : IDisposable
    {
        #region Private Members
        private readonly Project _sourceProject;
        private readonly Project _destinationProject;
        private readonly View _sourceView;
        private readonly View _destinationView;
        private readonly IDbDriver _sourceProjectDataDriver;
        private readonly IDbDriver _destinationProjectDataDriver;
        private List<View> _formsToProcess;
        private Query _selectQuery;
        private int _progress = 0;
        private List<string> _sourceGUIDs;
        private List<string> _optionFieldsAsStrings; // option fields prior to certain updates may be string or Int16.
        private const double DIFF_TOLERANCE = 1.7;
        #endregion // Private Members

        #region Events
        public event SetMaxProgressBarValueDelegate SetMaxProgressBarValue;
        public event SetProgressBarDelegate SetProgressBar;
        public event UpdateStatusEventHandler SetStatus;
        public event UpdateStatusEventHandler AddStatusMessage;
        public event CheckForCancellationHandler CheckForCancellation;
        #endregion // Events

        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        public FormDataImporter(Project sourceProject, Project destinationProject, View destinationView, List<View> viewsToProcess)
        {
            _formsToProcess = viewsToProcess;
            _sourceProject =  sourceProject;
            _destinationProject = destinationProject;
            _sourceView =  _sourceProject.Views[destinationView.Name];
            _destinationView =  destinationView;
            if (_destinationProject != null &&  _sourceProject != null)
            {
                _destinationProjectDataDriver = _destinationProject.CollectedData.GetDbDriver();
                _sourceProjectDataDriver =  _sourceProject.CollectedData.GetDbDriver();
            }

            _optionFieldsAsStrings = new List<string>();
        }
        #endregion // Constructors

        #region Public Properties
        /// <summary>
        /// Gets/sets whether to update existing records
        /// </summary>
        public bool Update { get; set; }

        /// <summary>
        /// Gets/sets whether to append unmatched records
        /// </summary>
        public bool Append { get; set; }

        /// <summary>
        /// Sets the columns to null;
        /// </summary>
        public Dictionary<string, List<string>> ColumnsToNull { get; set; }

        /// <summary>
        /// Sets the grid columns to null;
        /// </summary>
        public Dictionary<string, List<string>> GridColumnsToNull { get; set; }

        /// <summary>
        /// Gets/sets the select query used to filter records during the copying process.
        /// </summary>
        public Query SelectQuery
        {
            get
            {
                return this._selectQuery;
            }
            set
            {
                if (!value.SqlStatement.ToLower().Trim().StartsWith("select"))
                {
                    throw new ArgumentException(ImportExportSharedStrings.ERROR_INVALID_SELECT_QUERY);
                }
                else
                {
                    this._selectQuery = value;
                }
            }
        }
        #endregion Public Properties

        #region Public Methods
        /// <summary>
        /// Releases all resources used by the form data importer
        /// </summary>
        public void Dispose() // Implements IDisposable.Dispose
        {
        }

        /// <summary>
        /// Imports data from one project to another
        /// </summary>
        public void ImportFormData()
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            Query destinationSelectQuery = _destinationProjectDataDriver.CreateQuery("SELECT [GlobalRecordId] FROM [" + _destinationView.TableName + "]");
            List<string> destinationGUIDList = new List<string>();

            using (IDataReader destReader = _destinationProjectDataDriver.ExecuteReader(destinationSelectQuery))
            {
                while (destReader.Read())
                {
                    destinationGUIDList.Add(destReader[0].ToString());
                }
            }

            PopulateSourceGUIDs();

            int maxProgress = 100;

            if (SelectQuery == null)
            {
                int recordCount = _sourceView.GetRecordCount();
                int gridRowCount = 0;

                foreach (GridField gridField in _sourceView.Fields.GridFields)
                {
                    using (IDataReader reader = _sourceProjectDataDriver.GetTableDataReader(gridField.TableName))
                    {
                        while (reader.Read())
                        {
                            gridRowCount++;
                        }
                    }
                }

                maxProgress = recordCount * (_sourceView.Pages.Count + 1);
                maxProgress = maxProgress + gridRowCount;

                foreach (View form in _formsToProcess)
                {
                    maxProgress = maxProgress + (form.GetRecordCount() * (form.Pages.Count + 1));

                    foreach (GridField gridField in form.Fields.GridFields)
                    {
                        using (IDataReader reader = _sourceProjectDataDriver.GetTableDataReader(gridField.TableName))
                        {
                            while (reader.Read())
                            {
                                gridRowCount++;
                            }
                        }
                    }

                    maxProgress = maxProgress + gridRowCount;
                }

                using (IDataReader linksReader = _sourceProjectDataDriver.GetTableDataReader("metaLinks"))
                {
                    int linksRowCount = 0;
                    while (linksReader.Read())
                    {
                        linksRowCount++;
                    }

                    maxProgress = maxProgress + linksRowCount;
                }

                using (IDataReader fuReader = _sourceProjectDataDriver.GetTableDataReader("metaHistory"))
                {
                    int fuRowCount = 0;
                    while (fuReader.Read())
                    {
                        fuRowCount++;
                    }

                    maxProgress = maxProgress + fuRowCount;
                }
            }
            else
            {
                // This is only a rough estimate
                int recordCount = _sourceGUIDs.Count;
                maxProgress = recordCount * (_sourceView.Pages.Count + 1);
                OnSetStatusMessage(string.Format(ImportExportSharedStrings.ROW_FILTERS_IN_EFFECT, recordCount.ToString()));
            }

            CheckIfFormsAreAlike();

            OnSetMaxProgressValue(maxProgress);

            ProcessBaseTable(_sourceView, _destinationView, destinationGUIDList);
            ProcessPages(_sourceView, _destinationView, destinationGUIDList);
            //ProcessGridFields(_sourceView, _destinationView);
            ProcessRelatedForms(_sourceView, _destinationView, _formsToProcess);
            ProcessMetaLinks(_sourceView, _destinationView);
            ProcessMetaHistory(_sourceView, _destinationView);

            //Parallel.Invoke(
            //    () =>
            //    {
            //        ProcessBaseTable(_sourceView, _destinationView, destinationGUIDList);
            //        ProcessPages(_sourceView, _destinationView, destinationGUIDList);
            //        //ProcessGridFields(_sourceView, _destinationView);
            //        ProcessRelatedForms(_sourceView, _destinationView, _formsToProcess);
            //    },
            //    () =>
            //    {
            //        ProcessMetaLinks(_sourceView, _destinationView);
            //        ProcessMetaHistory(_sourceView, _destinationView);
            //    }
            //);

            sw.Stop();
            System.Diagnostics.Debug.Print("Finished ImportFormData. " + sw.Elapsed.TotalMilliseconds + "ms elapsed.");
        }
        #endregion // Public Methods

        #region Private Methods

        private void ProcessMetaLinks(View sourceView, View  _destinationView)
        {
// #if DEBUG
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
// #endif
            IDbDriver sourceDb = _sourceProjectDataDriver;
            IDbDriver destinationDb = _destinationProjectDataDriver;

            using (IDbConnection conn = new OleDbConnection(_destinationProject.CollectedData.GetDatabase().GetConnection().ConnectionString))
            {
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }

                using (IDataReader sourceReader = _sourceProjectDataDriver.GetTableDataReader("metaLinks"))
                {
                    while (sourceReader.Read())
                    {
                        if (OnCheckForCancellation())
                        {
                            OnAddStatusMessage(ImportExportSharedStrings.IMPORT_CANCELLED);
                            return;
                        }

                        Query insertQuery = destinationDb.CreateQuery("INSERT INTO metaLinks (ContactType, FromRecordGuid, ToRecordGuid, FromViewId, ToViewId, LastContactDate, IsEstimatedContactDate, RelationshipType, Tentative, Day1, Day2, Day3, Day4, Day5, Day6, Day7, Day8, Day9, Day10, Day11, Day12, Day13, Day14, Day15, Day16, Day17, Day18, Day19, Day20, Day21, Day1Notes, Day2Notes, Day3Notes, Day4Notes, Day5Notes, Day6Notes, Day7Notes, Day8Notes, Day9Notes, Day10Notes, Day11Notes, Day12Notes, Day13Notes, Day14Notes, Day15Notes, Day16Notes, Day17Notes, Day18Notes, Day19Notes, Day20Notes, Day21Notes) VALUES (" +
                            "@ContactType, @FromRecordGuid, @ToRecordGuid, @FromViewId, @ToViewId, @LastContactDate, @IsEstimatedContactDate, @RelationshipType, @Tentative, " +
                            "@Day1, @Day2, @Day3, @Day4, @Day5, @Day6, @Day7, @Day8, @Day9, " +
                            "@Day10, @Day11, @Day12, @Day13, @Day14, @Day15, @Day16, @Day17, @Day18, " +
                            "@Day19, @Day20, @Day21, " +
                            "@Day1Notes, @Day2Notes, @Day3Notes, @Day4Notes, @Day5Notes, @Day6Notes, @Day7Notes, @Day8Notes, @Day9, " +
                            "@Day10Notes, @Day11Notes, @Day12Notes, @Day13Notes, @Day14Notes, @Day15Notes, @Day16Notes, @Day17Notes, @Day18, " +
                            "@Day19Notes, @Day20Notes, @Day21Notes " +
                            ")"
                            );
                        insertQuery.Parameters.Add(new QueryParameter("@ContactType", DbType.Int32, sourceReader.GetValue(sourceReader.GetOrdinal("ContactType"))));
                        insertQuery.Parameters.Add(new QueryParameter("@FromRecordGuid", DbType.String, sourceReader.GetValue(sourceReader.GetOrdinal("FromRecordGuid"))));
                        insertQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, sourceReader.GetValue(sourceReader.GetOrdinal("ToRecordGuid"))));
                        insertQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, sourceReader.GetValue(sourceReader.GetOrdinal("FromViewId"))));
                        insertQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, sourceReader.GetValue(sourceReader.GetOrdinal("ToViewId"))));
                        insertQuery.Parameters.Add(new QueryParameter("@LastContactDate", DbType.DateTime, sourceReader.GetValue(sourceReader.GetOrdinal("LastContactDate"))));
                        insertQuery.Parameters.Add(new QueryParameter("@IsEstimatedContactDate", DbType.Boolean, sourceReader.GetValue(sourceReader.GetOrdinal("IsEstimatedContactDate"))));
                        insertQuery.Parameters.Add(new QueryParameter("@RelationshipType", DbType.String, sourceReader.GetValue(sourceReader.GetOrdinal("RelationshipType"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Tentative", DbType.Byte, sourceReader.GetValue(sourceReader.GetOrdinal("Tentative"))));

                        insertQuery.Parameters.Add(new QueryParameter("@Day1", DbType.Byte, sourceReader.GetValue(sourceReader.GetOrdinal("Day1"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day2", DbType.Byte, sourceReader.GetValue(sourceReader.GetOrdinal("Day2"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day3", DbType.Byte, sourceReader.GetValue(sourceReader.GetOrdinal("Day3"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day4", DbType.Byte, sourceReader.GetValue(sourceReader.GetOrdinal("Day4"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day5", DbType.Byte, sourceReader.GetValue(sourceReader.GetOrdinal("Day5"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day6", DbType.Byte, sourceReader.GetValue(sourceReader.GetOrdinal("Day6"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day7", DbType.Byte, sourceReader.GetValue(sourceReader.GetOrdinal("Day7"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day8", DbType.Byte, sourceReader.GetValue(sourceReader.GetOrdinal("Day8"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day9", DbType.Byte, sourceReader.GetValue(sourceReader.GetOrdinal("Day9"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day10", DbType.Byte, sourceReader.GetValue(sourceReader.GetOrdinal("Day10"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day11", DbType.Byte, sourceReader.GetValue(sourceReader.GetOrdinal("Day11"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day12", DbType.Byte, sourceReader.GetValue(sourceReader.GetOrdinal("Day12"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day13", DbType.Byte, sourceReader.GetValue(sourceReader.GetOrdinal("Day13"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day14", DbType.Byte, sourceReader.GetValue(sourceReader.GetOrdinal("Day14"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day15", DbType.Byte, sourceReader.GetValue(sourceReader.GetOrdinal("Day15"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day16", DbType.Byte, sourceReader.GetValue(sourceReader.GetOrdinal("Day16"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day17", DbType.Byte, sourceReader.GetValue(sourceReader.GetOrdinal("Day17"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day18", DbType.Byte, sourceReader.GetValue(sourceReader.GetOrdinal("Day18"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day19", DbType.Byte, sourceReader.GetValue(sourceReader.GetOrdinal("Day19"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day20", DbType.Byte, sourceReader.GetValue(sourceReader.GetOrdinal("Day20"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day21", DbType.Byte, sourceReader.GetValue(sourceReader.GetOrdinal("Day21"))));

                        insertQuery.Parameters.Add(new QueryParameter("@Day1Notes", DbType.String, sourceReader.GetValue(sourceReader.GetOrdinal("Day1Notes"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day2Notes", DbType.String, sourceReader.GetValue(sourceReader.GetOrdinal("Day2Notes"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day3Notes", DbType.String, sourceReader.GetValue(sourceReader.GetOrdinal("Day3Notes"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day4Notes", DbType.String, sourceReader.GetValue(sourceReader.GetOrdinal("Day4Notes"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day5Notes", DbType.String, sourceReader.GetValue(sourceReader.GetOrdinal("Day5Notes"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day6Notes", DbType.String, sourceReader.GetValue(sourceReader.GetOrdinal("Day6Notes"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day7Notes", DbType.String, sourceReader.GetValue(sourceReader.GetOrdinal("Day7Notes"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day8Notes", DbType.String, sourceReader.GetValue(sourceReader.GetOrdinal("Day8Notes"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day9Notes", DbType.String, sourceReader.GetValue(sourceReader.GetOrdinal("Day9Notes"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day10Notes", DbType.String, sourceReader.GetValue(sourceReader.GetOrdinal("Day10Notes"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day11Notes", DbType.String, sourceReader.GetValue(sourceReader.GetOrdinal("Day11Notes"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day12Notes", DbType.String, sourceReader.GetValue(sourceReader.GetOrdinal("Day12Notes"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day13Notes", DbType.String, sourceReader.GetValue(sourceReader.GetOrdinal("Day13Notes"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day14Notes", DbType.String, sourceReader.GetValue(sourceReader.GetOrdinal("Day14Notes"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day15Notes", DbType.String, sourceReader.GetValue(sourceReader.GetOrdinal("Day15Notes"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day16Notes", DbType.String, sourceReader.GetValue(sourceReader.GetOrdinal("Day16Notes"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day17Notes", DbType.String, sourceReader.GetValue(sourceReader.GetOrdinal("Day17Notes"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day18Notes", DbType.String, sourceReader.GetValue(sourceReader.GetOrdinal("Day18Notes"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day19Notes", DbType.String, sourceReader.GetValue(sourceReader.GetOrdinal("Day19Notes"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day20Notes", DbType.String, sourceReader.GetValue(sourceReader.GetOrdinal("Day20Notes"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Day21Notes", DbType.String, sourceReader.GetValue(sourceReader.GetOrdinal("Day21Notes"))));

                        //destinationDb.ExecuteNonQuery(insertQuery);
                        using (IDbCommand command = GetCommand(insertQuery.SqlStatement, conn, insertQuery.Parameters))
                        {
                            object obj = command.ExecuteNonQuery();
                        }

                        OnSetProgress(1);
                    }
                }
            }

// #if DEBUG
            sw.Stop();
            System.Diagnostics.Debug.Print("Finished ProcessMetaLinks. " + sw.Elapsed.TotalMilliseconds + "ms elapsed.");
// #endif
        }

        private void ProcessMetaHistory(View sourceView, View  _destinationView)
        {
// #if DEBUG
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
// #endif
            IDbDriver sourceDb = _sourceProjectDataDriver;
            IDbDriver destinationDb = _destinationProjectDataDriver;

            if (!destinationDb.TableExists("metaHistory"))
            {
                List<TableColumn> columnList = new List<TableColumn>();
                columnList.Add(new TableColumn("ContactGUID", GenericDbColumnType.Guid, true));
                columnList.Add(new TableColumn("FollowUpDate", GenericDbColumnType.Date, true));
                columnList.Add(new TableColumn("StatusOnDate", GenericDbColumnType.Int16, true));
                columnList.Add(new TableColumn("Note", GenericDbColumnType.StringLong, true));
                columnList.Add(new TableColumn("Temp1", GenericDbColumnType.Double, true));
                columnList.Add(new TableColumn("Temp2", GenericDbColumnType.Double, true));
                destinationDb.CreateTable("metaHistory", columnList);
            }

            if (!sourceDb.TableExists("metaHistory"))
            {
                // metaHistory doesn't exist in the source database, so skip it
                return;
            }

            using (OleDbConnection conn = new OleDbConnection(_destinationProject.CollectedData.GetDatabase().GetConnection().ConnectionString))
            {
                conn.Open();

                using (IDataReader sourceReader = _sourceProjectDataDriver.GetTableDataReader("metaHistory"))
                {
                    while (sourceReader.Read())
                    {
                        if (OnCheckForCancellation())
                        {
                            OnAddStatusMessage(ImportExportSharedStrings.IMPORT_CANCELLED);
                            return;
                        }

                        Query insertQuery = destinationDb.CreateQuery("INSERT INTO metaHistory (ContactGUID, [FollowUpDate], [StatusOnDate], [Note], [Temp1], [Temp2]) VALUES (" +
                            "@ContactGUID, @FollowUpDate, @StatusOnDate, @Note, @Temp1, @Temp2 " +
                            ")"
                            );
                        insertQuery.Parameters.Add(new QueryParameter("@ContactGUID", DbType.Guid, sourceReader.GetValue(sourceReader.GetOrdinal("ContactGUID"))));
                        insertQuery.Parameters.Add(new QueryParameter("@FollowUpDate", DbType.DateTime, sourceReader.GetValue(sourceReader.GetOrdinal("FollowUpDate"))));
                        insertQuery.Parameters.Add(new QueryParameter("@StatusOnDate", DbType.Int16, sourceReader.GetValue(sourceReader.GetOrdinal("StatusOnDate"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Note", DbType.String, sourceReader.GetValue(sourceReader.GetOrdinal("Note"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Temp1", DbType.Double, sourceReader.GetValue(sourceReader.GetOrdinal("Temp1"))));
                        insertQuery.Parameters.Add(new QueryParameter("@Temp2", DbType.Double, sourceReader.GetValue(sourceReader.GetOrdinal("Temp2"))));

                        //destinationDb.ExecuteNonQuery(insertQuery);
                        using (IDbCommand command = GetCommand(insertQuery.SqlStatement, conn, insertQuery.Parameters))
                        {
                            object obj = command.ExecuteNonQuery();
                        }

                        OnSetProgress(1);
                    }
                }
            }

// #if DEBUG
            sw.Stop();
            System.Diagnostics.Debug.Print("Finished ProcessMetaHistory. " + sw.Elapsed.TotalMilliseconds + "ms elapsed.");
// #endif
        }

        /// <summary>
        /// Checks to see if two given descendant forms are alike
        /// </summary>
        private void CheckIfDescendantFormsAreAlike(View sourceChildForm, View destChildForm)
        {
            //OnAddStatusMessage(string.Format(ImportExportSharedStrings.IMPORT_CHECK_DESCENDANT_FORM_START, sourceChildForm.Name));

            //int warningCount = 0;

            //if (sourceChildForm.Pages.Count != destChildForm.Pages.Count)
            //{
            //    throw new ApplicationException(string.Format(ImportExportSharedStrings.ERROR_DESCENDANT_PAGE_COUNT_DIFFERENT, sourceChildForm.Name));
            //}

            //if (!string.IsNullOrEmpty(destChildForm.TableName) && _destinationProjectDataDriver.TableExists(destChildForm.TableName))
            //{
            //    if (!sourceChildForm.TableName.Equals(destChildForm.TableName))
            //    {
            //        throw new ApplicationException(string.Format(ImportExportSharedStrings.ERROR_DESCENDANT_FORM_TABLE_NAMES_DIFFER, sourceChildForm.Name));
            //    }
            //}

            //foreach (Field sourceField in sourceChildForm.Fields)
            //{
            //    if (destChildForm.Fields.Contains(sourceField.Name))
            //    {
            //        Field destinationField = destChildForm.Fields[sourceField.Name];

            //        if (destinationField.FieldType != sourceField.FieldType)
            //        {
            //            throw new ApplicationException(string.Format(ImportExportSharedStrings.ERROR_DESCENDANT_FIELD_MISMATCH_DEST, destinationField.Name, sourceChildForm.Name));
            //        }
            //        else
            //        {
            //            if (destinationField is IDataField && destinationField is RenderableField && sourceField is IDataField && sourceField is RenderableField)
            //            {
            //                RenderableField rfDstField = destinationField as RenderableField;
            //                RenderableField rfSrcField = sourceField as RenderableField;

            //                if (rfDstField.Page.Position != rfSrcField.Page.Position)
            //                {
            //                    throw new ApplicationException(string.Format(ImportExportSharedStrings.ERROR_DESCENDANT_FIELD_PAGE_ORDER_MISMATCH, rfSrcField.Name, sourceChildForm.Name));
            //                }
            //            }
            //            if (destinationField is GridField && sourceField is GridField)
            //            {
            //                CheckIfGridsAreAlike(destinationField as GridField, sourceField as GridField);
            //            }
            //        }
            //    }
            //    else
            //    {
            //        OnAddStatusMessage(string.Format(ImportExportSharedStrings.ERROR_DESCENDANT_FIELD_NOT_FOUND, sourceField.Name, sourceChildForm.Name));
            //        warningCount++;
            //    }

            //    if (sourceField is ImageField)
            //    {
            //        OnAddStatusMessage(string.Format(ImportExportSharedStrings.ERROR_DESCENDANT_IMAGE_FIELD, sourceField.Name, sourceChildForm.Name));
            //    }
            //}

            //foreach (Field destinationField in destChildForm.Fields)
            //{
            //    if (!sourceChildForm.Fields.Contains(destinationField.Name))
            //    {
            //        OnAddStatusMessage(string.Format(ImportExportSharedStrings.ERROR_DESCENDANT_FIELD_NOT_FOUND_SOURCE, destinationField.Name, sourceChildForm.Name));
            //        warningCount++;
            //    }
            //}

            //// sanity check, especially for projects imported from Epi Info 3, where the forms may have untold amounts of corruption and errors
            //foreach (Field sourceField in sourceChildForm.Fields)
            //{
            //    if (!Util.IsFirstCharacterALetter(sourceField.Name))
            //    {
            //        throw new ApplicationException(string.Format(ImportExportSharedStrings.ERROR_FIELD_NAME_INVALID, sourceField.Name));
            //        //errorCount++;
            //    }
            //    if (Epi.Data.Services.AppData.Instance.IsReservedWord(sourceField.Name) && (sourceField.Name.ToLower() != "uniquekey" && sourceField.Name.ToLower() != "recstatus" && sourceField.Name.ToLower() != "fkey"))
            //    {
            //        //AddWarningMessage("The field name for " + sourceField.Name + " in the source form is a reserved word. Problems may be encountered during the import.");
            //        throw new ApplicationException(string.Format(ImportExportSharedStrings.ERROR_FIELD_NAME_RESERVED_WORD, sourceField.Name));
            //    }
            //}

            //if (!_sourceProjectDataDriver.TableExists(sourceChildForm.TableName))
            //{
            //    throw new ApplicationException(string.Format(SharedStrings.DATA_TABLE_NOT_FOUND, sourceChildForm.Name));
            //}

            //if (warningCount > ((double)destChildForm.Fields.Count / DIFF_TOLERANCE)) // User may have selected to import the wrong form with this many differences?
            //{
            //    throw new ApplicationException(ImportExportSharedStrings.ERROR_TOO_MANY_DIFFERENCES);
            //}

            //OnAddStatusMessage(string.Format(ImportExportSharedStrings.IMPORT_CHECK_DESCENDANT_FORM_END, sourceChildForm.Name));
        }

        /// <summary>
        /// Checks to see if two given grid fields are alike
        /// </summary>
        private void CheckIfGridsAreAlike(GridField sourceGridField, GridField destGridField)
        {
            //OnAddStatusMessage(string.Format(ImportExportSharedStrings.IMPORT_CHECK_GRID_START, sourceGridField.Name));

            foreach (GridColumnBase dgc in destGridField.Columns)
            {
                bool foundInSource = false;
                foreach (GridColumnBase sgc in sourceGridField.Columns)
                {
                    if (dgc.Name == sgc.Name)
                    {
                        if (dgc.GridColumnType != sgc.GridColumnType)
                        {
                            throw new ApplicationException(string.Format(ImportExportSharedStrings.ERROR_GRID_COLUMN_MISMATCH, destGridField.Name, dgc.Name, dgc.GridColumnType.ToString(), sgc.GridColumnType.ToString()));
                        }
                        else
                        {
                            foundInSource = true;
                            break;
                        }
                    }
                }

                if (!foundInSource)
                {
                    OnAddStatusMessage(string.Format(ImportExportSharedStrings.ERROR_GRID_COLUMN_MISSING, dgc.Name, destGridField.Name));
                }
            }

            //OnAddStatusMessage(string.Format(ImportExportSharedStrings.IMPORT_CHECK_GRID_END, sourceGridField.Name));
        }

        /// <summary>
        /// Checks to see whether or not the two forms (source and destination) are alike enough for the import to proceed.
        /// </summary>        
        private void CheckIfFormsAreAlike()
        {
            //int warningCount = 0;

            //OnAddStatusMessage(string.Format(ImportExportSharedStrings.IMPORT_CHECK_FORM_START, _sourceView.Name));

            //if (_sourceView.Pages.Count != _destinationView.Pages.Count)
            //{
            //    throw new ApplicationException(ImportExportSharedStrings.ERROR_PAGE_COUNT_DIFFERENT);
            //}

            //if (!_sourceProject.Views.Contains(_destinationView.Name))
            //{
            //    throw new ApplicationException(ImportExportSharedStrings.ERROR_FORM_NAMES_DIFFER);
            //}

            //foreach (View otherSourceView in  _sourceProject.Views)
            //{
            //    if (Epi.ImportExport.ImportExportHelper.IsFormDescendant(otherSourceView, _sourceView) && otherSourceView != _sourceView)
            //    {
            //        // the view is a descendant form
            //        if (_destinationProject.Views.Contains(otherSourceView.Name))
            //        {
            //            CheckIfDescendantFormsAreAlike(otherSourceView, _destinationProject.Views[otherSourceView.Name]);
            //        }
            //        else
            //        {
            //            throw new ApplicationException(ImportExportSharedStrings.ERROR_DESCENDANT_FORM_NAMES_DIFFER);
            //        }
            //    }
            //}

            //if (!string.IsNullOrEmpty(_destinationView.TableName) && _destinationProjectDataDriver.TableExists(_destinationView.TableName))
            //{
            //    if (!_sourceView.TableName.Equals(_destinationView.TableName))
            //    {
            //        throw new ApplicationException(ImportExportSharedStrings.ERROR_FORM_TABLE_NAMES_DIFFER);
            //    }
            //}

            //foreach (Field sourceField in _sourceView.Fields)
            //{
            //    if (_destinationView.Fields.Contains(sourceField.Name))
            //    {
            //        Field destinationField =  _destinationView.Fields[sourceField.Name];

            //        if (destinationField.FieldType != sourceField.FieldType)
            //        {
            //            throw new ApplicationException(string.Format(ImportExportSharedStrings.ERROR_FIELD_MISMATCH_DEST, destinationField.Name));
            //        }
            //        else
            //        {
            //            if (destinationField is IDataField && destinationField is RenderableField && sourceField is IDataField && sourceField is RenderableField)
            //            {
            //                RenderableField rfDstField = destinationField as RenderableField;
            //                RenderableField rfSrcField = sourceField as RenderableField;

            //                if (rfDstField.Page.Position != rfSrcField.Page.Position)
            //                {
            //                    throw new ApplicationException(string.Format(ImportExportSharedStrings.ERROR_FIELD_PAGE_ORDER_MISMATCH, rfSrcField.Name));
            //                }
            //            }
            //            if (destinationField is GridField && sourceField is GridField)
            //            {
            //                CheckIfGridsAreAlike(destinationField as GridField, sourceField as GridField);
            //            }
            //        }
            //    }
            //    else
            //    {
            //        OnAddStatusMessage(string.Format(ImportExportSharedStrings.ERROR_FIELD_NOT_FOUND, sourceField.Name));
            //        warningCount++;
            //    }

            //    if (sourceField is ImageField)
            //    {
            //        OnAddStatusMessage(string.Format(ImportExportSharedStrings.ERROR_IMAGE_FIELD, sourceField.Name));
            //    }
            //}

            //foreach (Field destinationField in  _destinationView.Fields)
            //{
            //    if (!_sourceView.Fields.Contains(destinationField.Name))
            //    {
            //        OnAddStatusMessage(string.Format(ImportExportSharedStrings.ERROR_FIELD_NOT_FOUND_SOURCE, destinationField.Name));
            //        warningCount++;
            //    }
            //}

            //// sanity check, especially for projects imported from Epi Info 3, where the forms may have untold amounts of corruption and errors
            //foreach (Field sourceField in _sourceView.Fields)
            //{
            //    if (!Util.IsFirstCharacterALetter(sourceField.Name))
            //    {
            //        throw new ApplicationException(string.Format(ImportExportSharedStrings.ERROR_FIELD_NAME_INVALID, sourceField.Name));
            //        //errorCount++;
            //    }
            //    if (Epi.Data.Services.AppData.Instance.IsReservedWord(sourceField.Name) && (sourceField.Name.ToLower() != "uniquekey" && sourceField.Name.ToLower() != "recstatus" && sourceField.Name.ToLower() != "fkey"))
            //    {
            //        //AddWarningMessage("The field name for " + sourceField.Name + " in the source form is a reserved word. Problems may be encountered during the import.");
            //        throw new ApplicationException(string.Format(ImportExportSharedStrings.ERROR_FIELD_NAME_RESERVED_WORD, sourceField.Name));
            //    }
            //}

            //if (!_sourceProjectDataDriver.TableExists(_sourceView.TableName))
            //{
            //    throw new ApplicationException(string.Format(SharedStrings.DATA_TABLE_NOT_FOUND, _sourceView.Name));
            //}

            //if (warningCount > ((double)_destinationView.Fields.Count / DIFF_TOLERANCE)) // User may have selected to import the wrong form with this many differences?
            //{
            //    throw new ApplicationException(ImportExportSharedStrings.ERROR_TOO_MANY_DIFFERENCES);
            //}

            OnAddStatusMessage(string.Format(ImportExportSharedStrings.IMPORT_CHECK_FORM_END, _sourceView.Name));
        }

        /// <summary>
        /// Populates the list of source GUIDs that should be used to do the import.
        /// </summary>
        private void PopulateSourceGUIDs()
        {
            _sourceGUIDs = new List<string>();

            if (SelectQuery != null)
            {
                using (IDataReader reader = _sourceProjectDataDriver.ExecuteReader(SelectQuery))
                {
                    while (reader.Read())
                    {
                        _sourceGUIDs.Add(reader[0].ToString());
                    }
                }
            }
            else
            {
                _sourceGUIDs = null;
            }
        }

        /// <summary>
        /// Populates the list of source GUIDs that should be used to do the import on a specific view.
        /// </summary>
        /// <param name="relatedView">The related view to process</param>
        private void PopulateSourceGUIDs(View relatedView)
        {
            #region Input Validation
            //if (!relatedView.IsRelatedView)
            //{
            //    throw new ArgumentException(ImportExportSharedStrings.ERROR_FORM_NOT_DESCENDENT);
            //}
            #endregion // Input Validation

            if (SelectQuery == null)
            {
                _sourceGUIDs = null;
                return;
            }

            List<string> parentGUIDs = new List<string>();

            if (relatedView.ParentView == null) return;

            View parentView = relatedView.ParentView;

            _sourceGUIDs = new List<string>();
            Query query = _destinationProjectDataDriver.CreateQuery("SELECT [GlobalRecordId] FROM [" + parentView.TableName + "]");
            IDataReader parentReader = null;

            if (parentView.Name ==  _destinationView.Name)
            {
                parentReader =  _sourceProjectDataDriver.ExecuteReader(SelectQuery);
            }
            else
            {
                parentReader = _destinationProjectDataDriver.ExecuteReader(query);
            }

            while (parentReader.Read())
            {
                parentGUIDs.Add(parentReader[0].ToString());
            }

            parentReader.Close();
            parentReader.Dispose();
            parentReader = null;

            foreach (string GUID in parentGUIDs)
            {
                Query childQuery =  _sourceProjectDataDriver.CreateQuery("SELECT [FKEY], [GlobalRecordId] FROM [" + relatedView.TableName + "] WHERE [FKEY] = @FKEY");
                childQuery.Parameters.Add(new QueryParameter("@FKEY", DbType.String, GUID));
                using (IDataReader reader = _sourceProjectDataDriver.ExecuteReader(childQuery))
                {
                    while (reader.Read())
                    {
                        string FKEY = reader[0].ToString();
                        string childGUID = reader[1].ToString();
                        _sourceGUIDs.Add(childGUID);
                    }
                }
            }
        }

        /// <summary>
        /// Processes a form's base table
        /// </summary>
        /// <param name="sourceView">The source form</param>
        /// <param name="destinationView">The destination form</param>
        /// <param name="destinationGUIDList">The list of GUIDs that exist in the destination</param>
        private void ProcessBaseTable(View sourceView, View  _destinationView, List<string> destinationGUIDList)
        {

// #if DEBUG
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
// #endif
            sourceView.LoadFirstRecord();
            OnAddStatusMessage(ImportExportSharedStrings.PROCESSING_BASE_TABLE);

            int recordsInserted = 0;
            int recordsUpdated = 0;

            string sourceTable = sourceView.TableName;
            string destinationTable =  _destinationView.TableName;

            _optionFieldsAsStrings = new List<string>();
            // Check for string-based option fields.
            foreach (Field f in  _destinationView.Fields)
            {
                if (f is OptionField)
                {
                    DataTable dt = _destinationProjectDataDriver.GetTopTwoTable((f as OptionField).Page.TableName);
                    if (dt.Columns[f.Name].DataType.ToString().Equals("System.String"))
                    {
                        _optionFieldsAsStrings.Add(f.Name);
                    }
                }
            }

            using (IDbConnection conn = new OleDbConnection(_destinationProject.CollectedData.GetDatabase().GetConnection().ConnectionString))
            {
                conn.Open();

                try
                {
                    List<string> newGUIDList = new List<string>();
                    using (IDataReader sourceReader = _sourceProjectDataDriver.GetTableDataReader(sourceView.TableName))
                    {
                        while (sourceReader.Read())
                        {
                            object recordStatus = sourceReader["RECSTATUS"];
                            object firstSaveLogonName = sourceReader["FirstSaveLogonName"];
                            object lastSaveLogonName = sourceReader["LastSaveLogonName"];
                            object firstSaveTime = sourceReader["FirstSaveTime"];
                            object lastSaveTime = sourceReader["LastSaveTime"];

                            QueryParameter paramRecordStatus = new QueryParameter("@RECSTATUS", DbType.Int32, recordStatus);
                            QueryParameter paramFirstSaveLogonName = new QueryParameter("@FirstSaveLogonName", DbType.String, firstSaveLogonName);
                            QueryParameter paramLastSaveLogonName = new QueryParameter("@LastSaveLogonName", DbType.String, lastSaveLogonName);

                            QueryParameter paramFirstSaveTime = new QueryParameter("@FirstSaveTime", DbType.DateTime, DBNull.Value);
                            QueryParameter paramLastSaveTime = new QueryParameter("@LastSaveTime", DbType.DateTime, DBNull.Value);

                            if (firstSaveTime != null && firstSaveTime != DBNull.Value)
                            {
                                paramFirstSaveTime = new QueryParameter("@FirstSaveTime", DbType.DateTime, firstSaveTime);
                            }

                            if (lastSaveTime != null && lastSaveTime != DBNull.Value)
                            {
                                paramLastSaveTime = new QueryParameter("@LastSaveTime", DbType.DateTime, lastSaveTime);
                            }

                            //if (importWorker.CancellationPending)
                            //{
                            //    this.BeginInvoke(new SetStatusDelegate(AddStatusMessage), "Import cancelled.");
                            //    return;
                            //}

                            if (OnCheckForCancellation())
                            {
                                OnAddStatusMessage(ImportExportSharedStrings.IMPORT_CANCELLED);
                                return;
                            }

                            WordBuilder fieldNames = new WordBuilder(StringLiterals.COMMA);
                            WordBuilder fieldValues = new WordBuilder(StringLiterals.COMMA);
                            List<QueryParameter> fieldValueParams = new List<QueryParameter>();

                            string GUID = sourceReader["GlobalRecordId"].ToString();

                            if (_sourceGUIDs != null && !_sourceGUIDs.Contains(GUID))
                            {
                                continue;
                            }

                            fieldNames.Append("GlobalRecordId");
                            fieldValues.Append("@GlobalRecordId");

                            string FKEY = sourceReader["FKEY"].ToString();
                            QueryParameter paramFkey = new QueryParameter("@FKEY", DbType.String, FKEY); // don't add this yet
                            QueryParameter paramGUID = new QueryParameter("@GlobalRecordId", DbType.String, GUID);
                            fieldValueParams.Add(paramGUID);

                            #region UPDATE
                            //if (destinationGUIDList.Contains(GUID))
                            //{
                            //    if (Update)
                            //    {
                            //        // UPDATE matching records
                            //        string updateHeader = string.Empty;
                            //        string whereClause = string.Empty;
                            //        fieldValueParams = new List<QueryParameter>();
                            //        StringBuilder sb = new StringBuilder();

                            //        // Build the Update statement which will be reused
                            //        sb.Append(SqlKeyWords.UPDATE);
                            //        sb.Append(StringLiterals.SPACE);
                            //        sb.Append(_destinationProjectDataDriver.InsertInEscape(destinationTable));
                            //        sb.Append(StringLiterals.SPACE);
                            //        sb.Append(SqlKeyWords.SET);
                            //        sb.Append(StringLiterals.SPACE);

                            //        updateHeader = sb.ToString();

                            //        sb.Remove(0, sb.ToString().Length);

                            //        // Build the WHERE caluse which will be reused
                            //        sb.Append(SqlKeyWords.WHERE);
                            //        sb.Append(StringLiterals.SPACE);
                            //        sb.Append(_destinationProjectDataDriver.InsertInEscape(ColumnNames.GLOBAL_RECORD_ID));
                            //        sb.Append(StringLiterals.EQUAL);
                            //        sb.Append("'");
                            //        sb.Append(GUID);
                            //        sb.Append("'");
                            //        whereClause = sb.ToString();

                            //        sb.Remove(0, sb.ToString().Length);

                            //        //if (sourceView.ForeignKeyFieldExists)
                            //        if (!string.IsNullOrEmpty(FKEY))
                            //        {
                            //            sb.Append(StringLiterals.LEFT_SQUARE_BRACKET);
                            //            sb.Append("FKEY");
                            //            sb.Append(StringLiterals.RIGHT_SQUARE_BRACKET);
                            //            sb.Append(StringLiterals.EQUAL);

                            //            sb.Append(StringLiterals.COMMERCIAL_AT);
                            //            sb.Append("FKEY");
                            //            fieldValueParams.Add(paramFkey);

                            //            Query updateQuery = _destinationProjectDataDriver.CreateQuery(updateHeader + StringLiterals.SPACE + sb.ToString() + StringLiterals.SPACE + whereClause);
                            //            updateQuery.Parameters = fieldValueParams;

                            //            _destinationProjectDataDriver.ExecuteNonQuery(updateQuery);

                            //            sb.Remove(0, sb.ToString().Length);
                            //            fieldValueParams.Clear();

                            //            recordsUpdated++;
                            //        }
                            //    }
                            //}
                            //else
                            //{
                            #endregion UPDATE
                            if (Append)
                                {
                                    if (!String.IsNullOrEmpty(FKEY))
                                    {
                                        fieldNames.Append("FKEY");
                                        fieldValues.Append("@FKEY");
                                        fieldValueParams.Add(paramFkey);
                                    }
                                    fieldNames.Append("RECSTATUS");
                                    fieldValues.Append("@RECSTATUS");
                                    fieldValueParams.Add(paramRecordStatus);

                                    fieldNames.Append("FirstSaveLogonName");
                                    fieldValues.Append("@FirstSaveLogonName");
                                    fieldValueParams.Add(paramFirstSaveLogonName);

                                    fieldNames.Append("LastSaveLogonName");
                                    fieldValues.Append("@LastSaveLogonName");
                                    fieldValueParams.Add(paramLastSaveLogonName);

                                    // Concatenate the query clauses into one SQL statement.
                                    StringBuilder sb = new StringBuilder();
                                    sb.Append(" insert into ");
                                    sb.Append(_destinationProjectDataDriver.InsertInEscape(destinationTable));
                                    sb.Append(StringLiterals.SPACE);
                                    sb.Append(Util.InsertInParantheses(fieldNames.ToString()));
                                    sb.Append(" values (");
                                    sb.Append(fieldValues.ToString());
                                    sb.Append(") ");
                                    Query insertQuery = _destinationProjectDataDriver.CreateQuery(sb.ToString());
                                    insertQuery.Parameters = fieldValueParams;

                                    //_destinationProjectDataDriver.ExecuteNonQuery(insertQuery);

                                    using (IDbCommand command = GetCommand(insertQuery.SqlStatement, conn, insertQuery.Parameters)) 
                                    {
                                        object obj = command.ExecuteNonQuery();
                                    }

                                    recordsInserted++;
                                }
                            //}
                            OnSetProgress(1);
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnAddStatusMessage(string.Format(ImportExportSharedStrings.ERROR_WITH_MESSAGE, ex.Message));
                }
                finally
                {
                }
            }

            if (Update && Append)
            {
                OnAddStatusMessage(string.Format(ImportExportSharedStrings.IMPORT_TABLE_UPDATED_AND_APPENDED, destinationTable, recordsInserted.ToString(), recordsUpdated.ToString()));
            }
            else if (Update && !Append)
            {
                OnAddStatusMessage(string.Format(ImportExportSharedStrings.IMPORT_TABLE_UPDATED, destinationTable, recordsUpdated.ToString()));
            }
            else if (!Update && Append)
            {
                OnAddStatusMessage(string.Format(ImportExportSharedStrings.IMPORT_TABLE_APPENDED, destinationTable, recordsInserted.ToString()));
            }

// #if DEBUG
            sw.Stop();
            System.Diagnostics.Debug.Print("Finished ProcessBaseTable (" + sourceView.Name + "). " + sw.Elapsed.TotalMilliseconds + "ms elapsed.");
// #endif
        }

        /// <summary>
        /// Processes all of the fields on a given form, page-by-page, except for the fields on the base table.
        /// </summary>
        /// <param name="sourceView">The source form</param>
        /// <param name="destinationView">The destination form</param>
        /// <param name="destinationGUIDList">The list of GUIDs that exist in the destination</param>
        private void ProcessPages(View sourceView, View  _destinationView, List<string> destinationGUIDList)
        {
// #if DEBUG
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
// #endif
            List<string> fieldsToSkip = new List<string>();
            foreach (Field sourceField in sourceView.Fields)
            {
                bool found = false;
                foreach (Field destinationField in _destinationView.Fields)
                {
                    if (destinationField.Name.ToLower().Equals(sourceField.Name.ToLower()))
                    {
                        found = true;
                    }
                }
                if (!found)
                {
                    fieldsToSkip.Add(sourceField.Name);
                }
            }

            if (ColumnsToNull != null && ColumnsToNull.ContainsKey(sourceView.Name))
            {
                List<string> toNull = ColumnsToNull[sourceView.Name];

                foreach (string s in toNull)
                {
                    if (!fieldsToSkip.Contains(s))
                    {
                        fieldsToSkip.Add(s);
                    }
                }
            }

            for (int i = 0; i < sourceView.Pages.Count; i++)
            //Parallel.ForEach(sourceView.Pages, p =>
            {
                using (IDbConnection conn = new OleDbConnection(_destinationProject.CollectedData.GetDatabase().GetConnection().ConnectionString))
                {
                    if (conn.State != ConnectionState.Open)
                    {
                        conn.Open();
                    }

                    //sourceView.LoadFirstRecord();
                    //OnAddStatusMessage(string.Format(ImportExportSharedStrings.PROCESSING_PAGE, (i + 1).ToString(), sourceView.Pages.Count.ToString()));

                    int recordsInserted = 0;
                    int recordsUpdated = 0;

                    // TPL implementation

                    //Page sourcePage = p; 
                    //Page destinationPage = _destinationView.Pages[0];

                    //foreach (Page page in _destinationView.Pages)
                    //{
                    //    if (page.Name.Equals(sourcePage.Name, StringComparison.OrdinalIgnoreCase))
                    //    {
                    //        destinationPage = page;
                    //    }
                    //}

                    Page sourcePage = sourceView.Pages[i];
                    Page destinationPage = _destinationView.Pages[i];

                    try
                    {
                        using (IDataReader sourceReader = _sourceProjectDataDriver.GetTableDataReader(sourcePage.TableName))
                        {
                            while (sourceReader.Read())
                            {
                                //if (importWorker.CancellationPending)
                                //{
                                //    this.BeginInvoke(new SetStatusDelegate(AddStatusMessage), "Import cancelled.");
                                //    return;
                                //}
                                if (OnCheckForCancellation())
                                {
                                    OnAddStatusMessage(ImportExportSharedStrings.IMPORT_CANCELLED);
                                    return;
                                }

                                WordBuilder fieldNames = new WordBuilder(StringLiterals.COMMA);
                                WordBuilder fieldValues = new WordBuilder(StringLiterals.COMMA);
                                List<QueryParameter> fieldValueParams = new List<QueryParameter>();
                                string GUID = sourceReader["GlobalRecordId"].ToString();

                                if (_sourceGUIDs != null && !_sourceGUIDs.Contains(GUID))
                                {
                                    continue;
                                }
                                #region UPDATE
                                //if (Update && destinationGUIDList.Contains(GUID))
                                //{
                                //    #region UPDATE
                                //    // UPDATE matching records
                                //    string updateHeader = string.Empty;
                                //    string whereClause = string.Empty;
                                //    fieldValueParams = new List<QueryParameter>();
                                //    StringBuilder sb = new StringBuilder();
                                //    int columnIndex = 0;

                                //    // Build the Update statement which will be reused
                                //    sb.Append(SqlKeyWords.UPDATE);
                                //    sb.Append(StringLiterals.SPACE);
                                //    sb.Append(_destinationProjectDataDriver.InsertInEscape(destinationPage.TableName));
                                //    sb.Append(StringLiterals.SPACE);
                                //    sb.Append(SqlKeyWords.SET);
                                //    sb.Append(StringLiterals.SPACE);

                                //    updateHeader = sb.ToString();

                                //    sb.Remove(0, sb.ToString().Length);

                                //    // Build the WHERE caluse which will be reused
                                //    sb.Append(SqlKeyWords.WHERE);
                                //    sb.Append(StringLiterals.SPACE);
                                //    sb.Append(_destinationProjectDataDriver.InsertInEscape(ColumnNames.GLOBAL_RECORD_ID));
                                //    sb.Append(StringLiterals.EQUAL);
                                //    sb.Append("'");
                                //    sb.Append(GUID);
                                //    sb.Append("'");
                                //    whereClause = sb.ToString();

                                //    sb.Remove(0, sb.ToString().Length);

                                //    int fieldsInQuery = 0;
                                //    // Now build the field update statements in 100 field chunks
                                //    foreach (RenderableField renderableField in sourcePage.Fields)
                                //    {
                                //        if (renderableField is GridField || renderableField is GroupField || renderableField is ImageField || fieldsToSkip.Contains(renderableField.Name)) // TODO: Someday, allow image fields
                                //        {
                                //            continue;
                                //        }
                                //        else if (renderableField is IDataField)
                                //        {
                                //            IDataField dataField = (IDataField)renderableField;
                                //            if (dataField.FieldType != MetaFieldType.UniqueKey && dataField is RenderableField)
                                //            {
                                //                columnIndex += 1;

                                //                //if (dataField.CurrentRecordValueObject == null)
                                //                if (sourceReader[renderableField.Name] == DBNull.Value || String.IsNullOrEmpty(sourceReader[renderableField.Name].ToString()))
                                //                {
                                //                    //sb.Append(SqlKeyWords.NULL);
                                //                }
                                //                else
                                //                {
                                //                    switch (dataField.FieldType)
                                //                    {
                                //                        case MetaFieldType.Date:
                                //                        case MetaFieldType.DateTime:
                                //                        case MetaFieldType.Time:
                                //                            fieldValueParams.Add(new QueryParameter("@" + renderableField.Name, DbType.DateTime, Convert.ToDateTime(sourceReader[renderableField.Name])));
                                //                            break;
                                //                        case MetaFieldType.Checkbox:
                                //                            fieldValueParams.Add(new QueryParameter("@" + renderableField.Name, DbType.Boolean, Convert.ToBoolean(sourceReader[renderableField.Name])));
                                //                            break;
                                //                        case MetaFieldType.CommentLegal:
                                //                        case MetaFieldType.LegalValues:
                                //                        case MetaFieldType.Codes:
                                //                        case MetaFieldType.Text:
                                //                        case MetaFieldType.TextUppercase:
                                //                        case MetaFieldType.PhoneNumber:
                                //                        case MetaFieldType.UniqueRowId:
                                //                        case MetaFieldType.ForeignKey:
                                //                        case MetaFieldType.GlobalRecordId:
                                //                        case MetaFieldType.Multiline:
                                //                            fieldValueParams.Add(new QueryParameter("@" + renderableField.Name, DbType.String, sourceReader[renderableField.Name]));
                                //                            break;
                                //                        case MetaFieldType.Number:
                                //                        case MetaFieldType.RecStatus:
                                //                        case MetaFieldType.YesNo:
                                //                            fieldValueParams.Add(new QueryParameter("@" + renderableField.Name, DbType.Single, sourceReader[renderableField.Name]));
                                //                            break;
                                //                        case MetaFieldType.GUID:
                                //                            fieldValueParams.Add(new QueryParameter("@" + renderableField.Name, DbType.Guid, sourceReader[renderableField.Name]));
                                //                            break;
                                //                        case MetaFieldType.Option:
                                //                            if (_optionFieldsAsStrings.Contains(renderableField.Name))
                                //                            {
                                //                                fieldValueParams.Add(new QueryParameter("@" + renderableField.Name, DbType.String, sourceReader[renderableField.Name]));
                                //                            }
                                //                            else
                                //                            {
                                //                                fieldValueParams.Add(new QueryParameter("@" + renderableField.Name, DbType.Int16, sourceReader[renderableField.Name]));
                                //                            }
                                //                            break;
                                //                        case MetaFieldType.Image:
                                //                            OnAddStatusMessage(String.Format(ImportExportSharedStrings.WARNING_FIELD_NOT_IMPORTED, renderableField.Name));
                                //                            continue;
                                //                        default:
                                //                            throw new ApplicationException(ImportExportSharedStrings.UNRECOGNIZED_FIELD_TYPE);
                                //                    }
                                //                    sb.Append(StringLiterals.LEFT_SQUARE_BRACKET);
                                //                    sb.Append(((Epi.INamedObject)dataField).Name);
                                //                    sb.Append(StringLiterals.RIGHT_SQUARE_BRACKET);
                                //                    sb.Append(StringLiterals.EQUAL);

                                //                    sb.Append(StringLiterals.COMMERCIAL_AT);
                                //                    sb.Append(((Epi.INamedObject)dataField).Name);
                                //                    sb.Append(StringLiterals.COMMA);
                                //                }
                                //            }

                                //            if ((columnIndex % 100) == 0 && columnIndex > 0)
                                //            {
                                //                if (sb.ToString().LastIndexOf(StringLiterals.COMMA).Equals(sb.ToString().Length - 1))
                                //                {
                                //                    sb.Remove(sb.ToString().LastIndexOf(StringLiterals.COMMA), 1);
                                //                }

                                //                Query updateQuery = _destinationProjectDataDriver.CreateQuery(updateHeader + StringLiterals.SPACE + sb.ToString() + StringLiterals.SPACE + whereClause);
                                //                updateQuery.Parameters = fieldValueParams;

                                //                _destinationProjectDataDriver.ExecuteNonQuery(updateQuery);

                                //                columnIndex = 0;
                                //                sb.Remove(0, sb.ToString().Length);
                                //                fieldValueParams.Clear();
                                //            }
                                //        }
                                //        fieldsInQuery++;
                                //    }

                                //    if (fieldsInQuery == 0)
                                //    {
                                //        continue;
                                //    }

                                //    if (sb.Length > 0)
                                //    {
                                //        if (sb.ToString().LastIndexOf(StringLiterals.COMMA).Equals(sb.ToString().Length - 1))
                                //        {
                                //            int startIndex = sb.ToString().LastIndexOf(StringLiterals.COMMA);
                                //            if (startIndex >= 0)
                                //            {
                                //                sb.Remove(startIndex, 1);
                                //            }
                                //        }

                                //        Query updateQuery = _destinationProjectDataDriver.CreateQuery(updateHeader + StringLiterals.SPACE + sb.ToString() + StringLiterals.SPACE + whereClause);
                                //        updateQuery.Parameters = fieldValueParams;

                                //        _destinationProjectDataDriver.ExecuteNonQuery(updateQuery);

                                //        columnIndex = 0;
                                //        sb.Remove(0, sb.ToString().Length);
                                //        fieldValueParams.Clear();
                                //    }

                                //    recordsUpdated++;
                                //    #endregion // UPDATE
                                //}
                                #endregion  // UPDATE
                                //else if (Append && !destinationGUIDList.Contains(GUID))
                                //{
                                    fieldNames.Append("GlobalRecordId");
                                    fieldValues.Append("@GlobalRecordId");
                                    fieldValueParams.Add(new QueryParameter("@GlobalRecordId", DbType.String, GUID));

                                    int fieldsInQuery = 0;
                                    // INSERT unmatched records
                                    foreach (RenderableField renderableField in sourcePage.Fields)
                                    {
                                        IDataField dataField = renderableField as IDataField;
                                        if (dataField != null) 
                                        {
                                            if (dataField is UniqueKeyField)
                                            {
                                                continue;
                                            }
                                            else
                                            {
                                                if (sourceReader[renderableField.Name] == DBNull.Value || String.IsNullOrEmpty(sourceReader[renderableField.Name].ToString()))
                                                //if (dataField.CurrentRecordValueObject == null)
                                                {
                                                    //fieldValues.Append(" null "); // TODO: Check to make sure we shouldn't be using this
                                                }
                                                else
                                                {
                                                    String fieldName = ((Epi.INamedObject)dataField).Name;
                                                    //fieldValueParams.Add(dataField.CurrentRecordValueAsQueryParameter);
                                                    switch (dataField.FieldType)
                                                    {
                                                        case MetaFieldType.Date:
                                                        case MetaFieldType.DateTime:
                                                        case MetaFieldType.Time:
                                                            fieldValueParams.Add(new QueryParameter("@" + fieldName, DbType.DateTime, Convert.ToDateTime(sourceReader[fieldName])));
                                                            break;
                                                        case MetaFieldType.Checkbox:
                                                            fieldValueParams.Add(new QueryParameter("@" + fieldName, DbType.Boolean, Convert.ToBoolean(sourceReader[fieldName])));
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
                                                            fieldValueParams.Add(new QueryParameter("@" + fieldName, DbType.String, sourceReader[fieldName]));
                                                            break;
                                                        case MetaFieldType.Number:
                                                        case MetaFieldType.YesNo:
                                                        case MetaFieldType.RecStatus:
                                                            fieldValueParams.Add(new QueryParameter("@" + fieldName, DbType.Single, sourceReader[fieldName]));
                                                            break;
                                                        case MetaFieldType.GUID:
                                                            fieldValueParams.Add(new QueryParameter("@" + fieldName, DbType.Guid, sourceReader[fieldName]));
                                                            break;
                                                        case MetaFieldType.Option:
                                                            if (_optionFieldsAsStrings.Contains(renderableField.Name))
                                                            {
                                                                fieldValueParams.Add(new QueryParameter("@" + renderableField.Name, DbType.String, sourceReader[fieldName]));
                                                            }
                                                            else
                                                            {
                                                                fieldValueParams.Add(new QueryParameter("@" + renderableField.Name, DbType.Int16, sourceReader[fieldName]));
                                                            }
                                                            break;
                                                        case MetaFieldType.Image:
                                                            OnAddStatusMessage(string.Format(ImportExportSharedStrings.WARNING_FIELD_NOT_IMPORTED, renderableField.Name));
                                                            continue;
                                                        default:
                                                            throw new ApplicationException(ImportExportSharedStrings.UNRECOGNIZED_FIELD_TYPE);
                                                    }
                                                    fieldNames.Append(_destinationProjectDataDriver.InsertInEscape(((Epi.INamedObject)dataField).Name));
                                                    fieldValues.Append("@" + fieldName);
                                                }
                                            }
                                        }
                                        else // if (renderableField is GridField || renderableField is GroupField /* || fieldsToSkip.Contains(renderableField.Name)*/)
                                        {
                                            continue;
                                        }
                                        fieldsInQuery++;
                                    }

                                    if (fieldsInQuery == 0)
                                    {
                                        continue;
                                    }

                                    // Concatenate the query clauses into one SQL statement.
                                    StringBuilder sb = new StringBuilder();
                                    sb.Append(" insert into ");
                                    sb.Append(_destinationProjectDataDriver.InsertInEscape(destinationPage.TableName));
                                    sb.Append(StringLiterals.SPACE);
                                    sb.Append(Util.InsertInParantheses(fieldNames.ToString()));
                                    sb.Append(" values (");
                                    sb.Append(fieldValues.ToString());
                                    sb.Append(") ");
                                    Query insertQuery = _destinationProjectDataDriver.CreateQuery(sb.ToString());
                                    insertQuery.Parameters = fieldValueParams;

                                    //_destinationProjectDataDriver.ExecuteNonQuery(insertQuery);

                                    using (IDbCommand command = GetCommand(insertQuery.SqlStatement, conn, insertQuery.Parameters))
                                    {
                                        object obj = command.ExecuteNonQuery();
                                    }

                                    recordsInserted++;
                                //}
                                //this.BeginInvoke(new SetProgressBarDelegate(IncrementProgressBarValue), 1);
                                    OnSetProgress(1);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        OnAddStatusMessage(string.Format(ImportExportSharedStrings.ERROR_WITH_MESSAGE, ex.Message));
                    }
                    finally
                    {
                    }

                    if (Update && Append)
                    {
                        OnAddStatusMessage(string.Format(ImportExportSharedStrings.IMPORT_PAGE_UPDATED_AND_APPENDED, destinationPage.Name, recordsInserted.ToString(), recordsUpdated.ToString()));
                    }
                    else if (Update && !Append)
                    {
                        OnAddStatusMessage(string.Format(ImportExportSharedStrings.IMPORT_PAGE_UPDATED, destinationPage.Name, recordsUpdated.ToString()));
                    }
                    else if (!Update && Append)
                    {
                        OnAddStatusMessage(string.Format(ImportExportSharedStrings.IMPORT_PAGE_APPENDED, destinationPage.Name, recordsInserted.ToString()));
                    }
                }
            }
            //});

// #if DEBUG
            sw.Stop();
            System.Diagnostics.Debug.Print("Finished ProcessPages(" + sourceView.Name + "). " + sw.Elapsed.TotalMilliseconds + "ms elapsed.");
// #endif
        }

        /// <summary>
        /// Processes all related forms
        /// </summary>
        /// <param name="sourceView">The source form</param>
        /// <param name="destinationView">The destination form</param>
        /// <param name="viewsToProcess">The list of forms to be processed</param>
        private void ProcessRelatedForms(View sourceView, View  _destinationView, List<View> viewsToProcess)
        {
// #if DEBUG
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
// #endif
            foreach (View view in viewsToProcess)
            //Parallel.ForEach(viewsToProcess, view =>
            {
                if (!_destinationProjectDataDriver.TableExists(view.TableName))
                {
                    _destinationProject.CollectedData.CreateDataTableForView(view, 1);
                    OnAddStatusMessage(string.Format(ImportExportSharedStrings.CREATED_DATA_TABLE_FOR_FORM, view.Name));
                }

                //Query selectQuery = _destinationProjectDataDriver.CreateQuery("SELECT [GlobalRecordId] FROM [" + view.TableName + "]");
                //IDataReader destReader = _destinationProjectDataDriver.ExecuteReader(selectQuery);
                List<string> destinationRelatedGUIDList = new List<string>();

                View relatedDestinationView = _destinationProject.Views[view.Name];

                //while (destReader.Read())
                //{
                //    destinationRelatedGUIDList.Add(destReader[0].ToString());
                //}

                //destReader.Close();
                //destReader.Dispose();

                //PopulateSourceGUIDs(view);

                ProcessBaseTable(view, relatedDestinationView, destinationRelatedGUIDList);
                ProcessPages(view, relatedDestinationView, destinationRelatedGUIDList);
                //ProcessGridFields(view, relatedDestinationView);

                // Do not process related forms again, that's all being done in this loop

            }

// #if DEBUG
            sw.Stop();
            System.Diagnostics.Debug.Print("Finished ProcessRelatedForms. " + sw.Elapsed.TotalMilliseconds + "ms elapsed.");
// #endif
        }

        /// <summary>
        /// Processes all of the grid fields on a given form
        /// </summary>
        /// <param name="sourceView">The source form</param>
        /// <param name="destinationView">The destination form</param>        
        private void ProcessGridFields(View sourceView, View  _destinationView)
        {
            foreach (GridField gridField in sourceView.Fields.GridFields)
            {
                List<string> gridGUIDList = new List<string>();

                if (_destinationView.Fields.GridFields.Contains(gridField.Name))
                {
                    int recordsUpdated = 0;
                    int recordsInserted = 0;
                    OnAddStatusMessage(string.Format(ImportExportSharedStrings.PROCESSING_GRID, gridField.Name));

                    try
                    {
                        List<string> gridColumnsToSkip = new List<string>();

                        string destinationGridTableName =  _destinationView.Fields.GridFields[gridField.Name].TableName;
                        GridField destinationGridField =  _destinationView.Fields.GridFields[gridField.Name];
                        IDataReader destinationGridTableReader = _destinationProjectDataDriver.GetTableDataReader(destinationGridTableName);

                        foreach (GridColumnBase gridColumn in gridField.Columns)
                        {
                            bool found = false;
                            foreach (GridColumnBase destinationGridColumn in destinationGridField.Columns)
                            {
                                if (destinationGridColumn.Name.ToLower().Equals(gridColumn.Name.ToLower()))
                                {
                                    found = true;
                                }
                            }
                            if (!found)
                            {
                                gridColumnsToSkip.Add(gridColumn.Name);
                            }
                        }

                        string gridReference = sourceView.Name + ":" + gridField.Name;

                        if (GridColumnsToNull != null && GridColumnsToNull.ContainsKey(gridReference))
                        {
                            List<string> toNull = GridColumnsToNull[gridReference];

                            foreach (string s in toNull)
                            {
                                if (!gridColumnsToSkip.Contains(s))
                                {
                                    gridColumnsToSkip.Add(s);
                                }
                            }
                        }

                        while (destinationGridTableReader.Read())
                        {
                            gridGUIDList.Add(destinationGridTableReader["UniqueRowId"].ToString());
                        }

                        destinationGridTableReader.Close();
                        destinationGridTableReader.Dispose();

                        IDataReader sourceGridTableReader =  _sourceProjectDataDriver.GetTableDataReader(gridField.TableName);
                        while (sourceGridTableReader.Read())
                        {
                            string GUID = sourceGridTableReader["UniqueRowId"].ToString();
                            string FKEY = sourceGridTableReader["FKEY"].ToString();

                            if (_sourceGUIDs != null && !_sourceGUIDs.Contains(FKEY))
                            {
                                continue;
                            }

                            if (gridGUIDList.Contains(GUID) && Update)
                            {
                                int columns = 0;
                                StringBuilder sb = new StringBuilder();
                                List<QueryParameter> fieldValueParams = new List<QueryParameter>();
                                sb.Append("UPDATE " + _destinationProjectDataDriver.InsertInEscape(destinationGridTableName) + " SET ");
                                foreach (GridColumnBase gridColumn in gridField.Columns)
                                {
                                    object data = sourceGridTableReader[gridColumn.Name];
                                    if (data == null || string.IsNullOrEmpty(data.ToString()) || data == DBNull.Value)
                                    {
                                        continue; // don't update current data with null values (according to product requirements)
                                    }
                                    else if (gridColumnsToSkip.Contains(gridColumn.Name))
                                    {
                                        continue; // don't try and update a grid row that may not exist
                                    }

                                    sb.Append(_destinationProjectDataDriver.InsertInEscape(gridColumn.Name));
                                    sb.Append(StringLiterals.EQUAL);
                                    sb.Append("@" + gridColumn.Name);
                                    switch (gridColumn.GridColumnType)
                                    {
                                        case MetaFieldType.Date:
                                        case MetaFieldType.DateTime:
                                        case MetaFieldType.Time:
                                            //sb.Append(destinationProjectDataDriver.FormatDateTime((DateTime)data));                                        
                                            fieldValueParams.Add(new QueryParameter("@" + gridColumn.Name, DbType.DateTime, data));
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
                                            //sb.Append("'" + data.ToString().Replace("'", "''") + "'");
                                            fieldValueParams.Add(new QueryParameter("@" + gridColumn.Name, DbType.String, data));
                                            break;
                                        case MetaFieldType.Number:
                                        case MetaFieldType.RecStatus:
                                            fieldValueParams.Add(new QueryParameter("@" + gridColumn.Name, DbType.Single, data));
                                            break;
                                        default:
                                            throw new ApplicationException(string.Format(ImportExportSharedStrings.ERROR_GRID_COLUMN, gridColumn.Name));
                                    }
                                    sb.Append(StringLiterals.COMMA);
                                    columns++;
                                }

                                if (columns == 0)
                                {
                                    continue;
                                }

                                sb.Length = sb.Length - 1;

                                sb.Append(" WHERE ");
                                sb.Append("[UniqueRowId] = ");
                                sb.Append("'" + GUID + "'");

                                Query updateQuery = _destinationProjectDataDriver.CreateQuery(sb.ToString());
                                updateQuery.Parameters = fieldValueParams;
                                _destinationProjectDataDriver.ExecuteNonQuery(updateQuery);
                                recordsUpdated++;
                                OnSetProgress(1);
                                //this.BeginInvoke(new SetProgressBarDelegate(IncrementProgressBarValue), 1);

                            }
                            else if (!gridGUIDList.Contains(GUID) && Append)
                            {
                                int columns = 0;
                                List<QueryParameter> fieldValueParams = new List<QueryParameter>();
                                WordBuilder fieldNames = new WordBuilder(",");
                                WordBuilder fieldValues = new WordBuilder(",");
                                foreach (GridColumnBase gridColumn in gridField.Columns)
                                {
                                    object data = sourceGridTableReader[gridColumn.Name];
                                    if (data == null || string.IsNullOrEmpty(data.ToString()) || data == DBNull.Value)
                                    {
                                        continue; // don't update current data with null values (according to product requirements)
                                    }
                                    else if (gridColumnsToSkip.Contains(gridColumn.Name))
                                    {
                                        continue; // don't try and update a grid row that may not exist
                                    }

                                    fieldNames.Add(_destinationProjectDataDriver.InsertInEscape(gridColumn.Name));
                                    fieldValues.Add("@" + gridColumn.Name);

                                    switch (gridColumn.GridColumnType)
                                    {
                                        case MetaFieldType.Date:
                                        case MetaFieldType.DateTime:
                                        case MetaFieldType.Time:
                                            fieldValueParams.Add(new QueryParameter("@" + gridColumn.Name, DbType.DateTime, data));
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
                                            fieldValueParams.Add(new QueryParameter("@" + gridColumn.Name, DbType.String, data));
                                            break;
                                        case MetaFieldType.Number:
                                        case MetaFieldType.RecStatus:
                                            fieldValueParams.Add(new QueryParameter("@" + gridColumn.Name, DbType.Single, data));
                                            break;
                                        default:
                                            throw new ApplicationException(string.Format(ImportExportSharedStrings.ERROR_GRID_COLUMN, gridColumn.Name));
                                    }
                                    columns++;
                                }

                                if (columns == 0)
                                {
                                    continue;
                                }

                                StringBuilder sb = new StringBuilder();
                                sb.Append("INSERT INTO " + _destinationProjectDataDriver.InsertInEscape(destinationGridTableName));
                                sb.Append(StringLiterals.SPACE);
                                sb.Append(Util.InsertInParantheses(fieldNames.ToString()));
                                sb.Append(" values (");
                                sb.Append(fieldValues.ToString());
                                sb.Append(") ");
                                Query insertQuery = _destinationProjectDataDriver.CreateQuery(sb.ToString());
                                insertQuery.Parameters = fieldValueParams;

                                _destinationProjectDataDriver.ExecuteNonQuery(insertQuery);
                                OnSetProgress(1);
                                //this.BeginInvoke(new SetProgressBarDelegate(IncrementProgressBarValue), 1);
                                recordsInserted++;
                            }
                            OnSetProgress(1);
                        } // end while source data reader reads

                        sourceGridTableReader.Close();
                        sourceGridTableReader.Dispose();

                        if (Update && Append)
                        {
                            OnAddStatusMessage(string.Format(ImportExportSharedStrings.IMPORT_GRID_UPDATED_AND_APPENDED, gridField.Name, recordsInserted.ToString(), recordsUpdated.ToString()));
                        }
                        else if (Update && !Append)
                        {
                            OnAddStatusMessage(string.Format(ImportExportSharedStrings.IMPORT_GRID_UPDATED, gridField.Name, recordsUpdated.ToString()));
                        }
                        else if (!Update && Append)
                        {
                            OnAddStatusMessage(string.Format(ImportExportSharedStrings.IMPORT_GRID_APPENDED, gridField.Name, recordsInserted.ToString()));
                        }
                    }
                    catch (Exception ex)
                    {
                        OnAddStatusMessage(string.Format(ImportExportSharedStrings.ERROR_WITH_MESSAGE, ex.Message));
                    }
                } // end if contains
            }
        }
        #endregion // Private Methods

        #region Protected Methods
        /// <summary>
        /// Checks for cancellation
        /// </summary>
        protected virtual bool OnCheckForCancellation()
        {
            if (CheckForCancellation != null)
            {
                return CheckForCancellation();
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Adds a status message
        /// </summary>
        /// <param name="message">The message</param>
        protected virtual void OnAddStatusMessage(string message)
        {
            if (AddStatusMessage != null)
            {
                AddStatusMessage(message);
            }
        }

        /// <summary>
        /// Sets status message
        /// </summary>
        /// <param name="message">The message</param>
        protected virtual void OnSetStatusMessage(string message)
        {
            if (SetStatus != null)
            {
                SetStatus(message);
            }
        }

        /// <summary>
        /// Sets progess bar value
        /// </summary>
        /// <param name="progress">The message</param>
        protected virtual void OnSetProgress(double progress)
        {
            if (SetProgressBar != null)
            {
                _progress = _progress + (int)progress;
                SetProgressBar(_progress);
            }
        }

        /// <summary>
        /// Sets max attainable progess bar value
        /// </summary>
        /// <param name="maxProgress">The max value to set</param>
        protected virtual void OnSetMaxProgressValue(double maxProgress)
        {
            if (SetMaxProgressBarValue != null)
            {
                SetMaxProgressBarValue(maxProgress);
            }
        }

        /// <summary>
        /// Returns a native equivalent of a DbParameter
        /// </summary>
        /// <returns>Native equivalent of a DbParameter</returns>
        protected OleDbParameter ConvertToNativeParameter(QueryParameter parameter)
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
        protected OleDbType CovertToNativeDbType(DbType dbType)
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

        /// <summary>
        /// Gets a new command using an existing connection
        /// </summary>
        /// <param name="sqlStatement">The query to be executed against the database</param>
        /// <param name="connection">Parameters for the query to be executed</param>
        /// <param name="parameters">An OleDb command object</param>
        /// <returns></returns>
        protected IDbCommand GetCommand(string sqlStatement, IDbConnection connection, List<QueryParameter> parameters)
        {
            #region Input Validation
            if (String.IsNullOrEmpty(sqlStatement))
            {
                throw new ArgumentNullException("sqlStatement");
            }
            if (parameters == null)
            {
                throw new ArgumentNullException("parameters");
            }
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
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
        #endregion // Protected Methods
    }
}
