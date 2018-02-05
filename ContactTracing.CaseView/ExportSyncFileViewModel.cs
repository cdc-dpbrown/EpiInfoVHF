using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;
using System.Xml.Linq;
using Epi;
using Epi.Data;
using Epi.ImportExport;
using Epi.ImportExport.Filters;
using Epi.ImportExport.ProjectPackagers;
using ContactTracing.Core;
using ContactTracing.Core.Enums;

namespace ContactTracing.CaseView
{
    public sealed class ExportSyncFileViewModel : ObservableObject
    {
        #region Members
        private readonly string _currentUser = String.Empty;
        private readonly string _macAddress = String.Empty;
        private string _filterJoinType = String.Empty;
        private string _filterOperator1 = String.Empty;
        private string _filterOperator2 = String.Empty;
        private string _filterField1 = String.Empty;
        private string _filterField2 = String.Empty;
        private string _filterValue1 = String.Empty;
        private string _filterValue2 = String.Empty;
        private object _sendMessageLock = new object();

        private bool _isDisplaying = false;
        private System.Windows.Shell.TaskbarItemProgressState _taskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
        private double _majorProgressValue = 0.0;
        private double _minorProgressValue = 0.0;
        private string _syncStatus = String.Empty;
        private string _overallSyncStatus = String.Empty;
        private string _timeElapsed = String.Empty;
        private readonly VhfProject _project;
        private bool _isDataSyncing = false;
        private bool _isShowingExportProgress = false;
        private string _syncFilePath = String.Empty;
        private bool _includeCases = true;
        private bool _includeCasesAndContacts = true;
        private bool _deIdentifyData = false;
        private string _projectFilePath = String.Empty;
        private string _recordScope = "Both";
        private string _recordsExported = String.Empty;
        private double _increment = 0.0;
        private string _days = "18250";
        private DateTime _startDate = DateTime.MinValue;
        private DateTime _endDate = DateTime.MaxValue;
        private bool _applyFilters = false;
        private bool _applyLastSaveFilter = false;
        private bool _hasExportErrors = false;
        #endregion // Members

        #region Events
        public event EventHandler SyncProblemsDetected;
        #endregion // Events

        #region Properties

        public bool HasExportErrors { get { return _hasExportErrors; } set { _hasExportErrors = value; RaisePropertyChanged("HasExportErrors"); } }
        public bool ApplyFilters { get { return _applyFilters; } set { _applyFilters = value; RaisePropertyChanged("ApplyFilters"); } }
        public bool ApplyLastSaveFilter { get { return _applyLastSaveFilter; } set { _applyLastSaveFilter = value; RaisePropertyChanged("ApplyLastSaveFilter"); } }
        
        public string FilterJoinType
        {
            get { return _filterJoinType; }
            set
            {
                _filterJoinType = value; RaisePropertyChanged("FilterJoinType");
                if (String.IsNullOrEmpty(FilterJoinType))
                {
                    FilterField2 = String.Empty;
                    FilterOperator2 = String.Empty;
                    FilterValue2 = String.Empty;
                }
            }
        }

        public string FilterOperator1 { get { return _filterOperator1; } set { _filterOperator1 = value; RaisePropertyChanged("FilterOperator1"); } }
        public string FilterOperator2 { get { return _filterOperator2; } set { _filterOperator2 = value; RaisePropertyChanged("FilterOperator2"); } }

        public string FilterField1 { get { return _filterField1; } set { _filterField1 = value; RaisePropertyChanged("FilterField1"); } }
        public string FilterField2 { get { return _filterField2; } set { _filterField2 = value; RaisePropertyChanged("FilterField2"); } }

        public string FilterValue1 { get { return _filterValue1; } set { _filterValue1 = value; RaisePropertyChanged("FilterValue1"); } }
        public string FilterValue2 { get { return _filterValue2; } set { _filterValue2 = value; RaisePropertyChanged("FilterValue2"); } }

        public ObservableCollection<string> FilterableFields { get; private set; }
        public ObservableCollection<string> FilterOperators { get; private set; }
        public ObservableCollection<string> FilterJoinTypes { get; private set; }
        public DateTime StartDate { get { return _startDate; } set { _startDate = value; RaisePropertyChanged("StartDate"); } }
        public DateTime EndDate { get { return _endDate; } set { _endDate = value; RaisePropertyChanged("EndDate"); } }
        public bool IsDisplaying
        {
            get
            {
                return _isDisplaying;
            }
            set
            {
                _isDisplaying = value;
                RaisePropertyChanged("IsDisplaying");
            }
        }
        public Dictionary<string, RowFilters> Filters { get; set; }
        public RecordProcessingScope RecordProcessingScope { get; set; }
        private IDbDriver Database
        {
            get
            {
                return _project.CollectedData.GetDatabase();
            }
        }
        public string Days
        {
            get
            {
                return _days;
            }
            set
            {
                _days = value;
                RaisePropertyChanged("Days");
            }
        }
        public string RecordsExported
        {
            get
            {
                return _recordsExported;
            }
            set
            {
                _recordsExported = value;
                RaisePropertyChanged("RecordsExported");
            }
        }
        public string RecordScope
        {
            get
            {
                return _recordScope;
            }
            set
            {
                _recordScope = value;
                RaisePropertyChanged("RecordScope");
            }
        }
        private View CaseForm { get; set; }
        private View LabForm { get; set; }
        private View ContactForm { get; set; }
        public bool IsCountryUS { get; set; }
        private int CaseFormId { get; set; }
        private int ContactFormId { get; set; }
        private int LabFormId { get; set; }
        public bool IsWaitingOnOtherClients { get; set; }
        public bool DeIdentifyData
        {
            get
            {
                return _deIdentifyData;
            }
            set
            {
                _deIdentifyData = value;
                RaisePropertyChanged("DeIdentifyData");
            }
        }
        public string SyncFilePath
        {
            get
            {
                return _syncFilePath;
            }
            set
            {
                _syncFilePath = value;
                RaisePropertyChanged("SyncFilePath");
            }
        }
        public bool IncludeCasesOnly
        {
            get
            {
                return _includeCases;
            }
            set
            {
                _includeCases = value;
                RaisePropertyChanged("IncludeCasesOnly");
            }
        }
        public bool IncludeCasesAndContacts
        {
            get
            {
                return _includeCasesAndContacts;
            }
            set
            {
                _includeCasesAndContacts = value;
                RaisePropertyChanged("IncludeCasesAndContacts");
            }
        }
        public bool IsShowingExportProgress
        {
            get
            {
                return _isShowingExportProgress;
            }
            protected internal set
            {
                if (_isShowingExportProgress != value)
                {
                    _isShowingExportProgress = value;
                    RaisePropertyChanged("IsShowingExportProgress");
                }
            }
        }
        public bool IsDataSyncing
        {
            get
            {
                return _isDataSyncing;
            }
            internal set
            {
                if (_isDataSyncing != value)
                {
                    _isDataSyncing = value;
                    RaisePropertyChanged("IsDataSyncing");
                }
            }
        }
        public string MajorSyncStatus
        {
            get
            {
                return this._overallSyncStatus;
            }
            set
            {
                this._overallSyncStatus = value;
                RaisePropertyChanged("MajorSyncStatus");
            }
        }
        public string TimeElapsed
        {
            get
            {
                return this._timeElapsed;
            }
            set
            {
                this._timeElapsed = value;
                RaisePropertyChanged("TimeElapsed");
            }
        }

        public string MinorSyncStatus
        {
            get
            {
                return this._syncStatus;
            }
            set
            {
                this._syncStatus = value;
                RaisePropertyChanged("MinorSyncStatus");
            }
        }

        /// <summary>
        /// Gets/sets the current progress value for the progress bar that shows up in the export panel
        /// </summary>
        public double MinorProgressValue
        {
            get
            {
                return this._minorProgressValue;
            }
            set
            {
                if (this.MinorProgressValue != value)
                {
                    this._minorProgressValue = value;
                    RaisePropertyChanged("MinorProgressValue");
                }
            }
        }

        /// <summary>
        /// Gets/sets the current progress value for the progress bar that shows up in the app's taskbar icon.
        /// </summary>
        public double MajorProgressValue
        {
            get
            {
                return this._majorProgressValue;
            }
            set
            {
                if (this.MajorProgressValue != value)
                {
                    this._majorProgressValue = value;
                    RaisePropertyChanged("MajorProgressValue");
                }
            }
        }

        /// <summary>
        /// Gets/sets the current progress state for the progress bar that shows up in the app's taskbar icon.
        /// </summary>
        public System.Windows.Shell.TaskbarItemProgressState TaskbarProgressState
        {
            get
            {
                return this._taskbarProgressState;
            }
            set
            {
                if (this._taskbarProgressState != value)
                {
                    this._taskbarProgressState = value;
                    RaisePropertyChanged("TaskbarProgressState");
                }
            }
        }
        public bool _ShowExportOptions;
        public bool ShowExportOptions { get { return _ShowExportOptions; } set { _ShowExportOptions = value; RaisePropertyChanged("ShowExportOptions"); } }


        #endregion // Properties

        #region Constructors

        public ExportSyncFileViewModel(VhfProject project, string currentUser, string macAddress, bool isCountryUS)
            : base()
        {
            IncludeCasesAndContacts = true;
            IncludeCasesOnly = false;
            _project = project;
            _currentUser = currentUser;
            _macAddress = macAddress;
            IsCountryUS = isCountryUS;
            ShowExportOptions = false;

            CaseForm = _project.Views[Core.Constants.CASE_FORM_NAME];
            ContactForm = _project.Views[Core.Constants.CONTACT_FORM_NAME];
            LabForm = _project.Views[Core.Constants.LAB_FORM_NAME];

            CaseFormId = CaseForm.Id;
            ContactFormId = ContactForm.Id;
            LabFormId = LabForm.Id;

            StartDate = new DateTime(2000, 1, 1);
            EndDate = DateTime.Today.AddDays(3);

            FilterableFields = new ObservableCollection<string>() { "DistrictRes", "DistrictOnset", "SCRes", "SCOnset", "ID" };
            FilterOperators = new ObservableCollection<string>() { "equals", "contains" };
            FilterJoinTypes = new ObservableCollection<string>() { "", "and", "or" };

            FilterJoinType = String.Empty;
            FilterField1 = String.Empty;
            FilterField2 = String.Empty;
            FilterOperator1 = String.Empty;
            FilterOperator2 = String.Empty;
            FilterValue1 = String.Empty;
            FilterValue2 = String.Empty;

            Filters = new Dictionary<string, RowFilters>();

            foreach (View form in project.Views)
            {
                Filters.Add(form.Name, new RowFilters(Database));
            }
        }
        #endregion // Constructors

        #region Methods

        void exporter_MinorProgressChanged(double progress)
        {
            MinorProgressValue = progress / 100;
        }

        void exporter_MajorProgressChanged(double progress)
        {
            MajorProgressValue = progress / 100;
        }

        void exporter_MinorStatusChanged(string message)
        {
            MinorSyncStatus = message;
        }

        private bool SendMessage(string description, string recordId, ServerUpdateType updateType)
        {
            bool success = false;

            if (Database.ToString().ToLower().Contains("sql"))
            {
                System.Guid guid = System.Guid.NewGuid();
                string guidString = guid.ToString();
                DateTime now = DateTime.Now;

                lock (_sendMessageLock)
                {
                    using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Database.ConnectionString + ";Connection Timeout=10"))
                    {
                        conn.Open();

                        System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
                        Version thisVersion = a.GetName().Version;

                        using (System.Data.SqlClient.SqlCommand insertCommand = new System.Data.SqlClient.SqlCommand("INSERT INTO Changesets (ChangesetID, UpdateType, UserID, MACADDR, Description, DestinationRecordID, CheckinDate, VhfVersion) VALUES (" +
                            "@ChangesetID, @UpdateType, @UserID, @MACADDR, @Description, @DestinationRecordID, @CheckinDate, @VhfVersion)", conn))
                        {

                            insertCommand.Parameters.Add("@ChangesetID", SqlDbType.NVarChar).Value = guidString;
                            insertCommand.Parameters.Add("@UpdateType", SqlDbType.Int).Value = (int)updateType;
                            insertCommand.Parameters.Add("@UserID", SqlDbType.NVarChar).Value = _currentUser;
                            insertCommand.Parameters.Add("@MACADDR", SqlDbType.NVarChar).Value = _macAddress;
                            insertCommand.Parameters.Add("@Description", SqlDbType.NVarChar).Value = description;
                            insertCommand.Parameters.Add("@DestinationRecordID", SqlDbType.NVarChar).Value = recordId;
                            insertCommand.Parameters.Add("@CheckinDate", SqlDbType.DateTime2).Value = now;
                            insertCommand.Parameters.Add("@VhfVersion", SqlDbType.NVarChar).Value = thisVersion.ToString();

                            int records = insertCommand.ExecuteNonQuery();

                            if (records == 1)
                            {
                                success = true;
                            }
                        }

                        conn.Close();
                    }
                }
            }
            else
            {
                success = true;
            }

            return success;
        }

        private void SendMessageForAwaitAll()
        {
            SendMessage("Exporting data to sync file", String.Empty, ServerUpdateType.LockAllClientIsRefreshing);
        }

        private void SendMessageForUnAwaitAll()
        {
            SendMessage("Done exporting data to sync file", String.Empty, ServerUpdateType.UnlockAllClientRefreshComplete);
        }

        public void CreateCaseSyncFileStart(ContactTracing.ImportExport.SyncFileFilters filters /*string fileName, bool includeCases, bool includeCaseExposures, bool includeContacts, Epi.ImportExport.Filters.RowFilters filters, bool deIdentifyData, Epi.RecordProcessingScope recordProcessingScope*/)
        {
            if (IsWaitingOnOtherClients)
            {
                return;
            }

            if (String.IsNullOrEmpty(SyncFilePath.Trim()))
            {
                throw new ArgumentNullException("fileName");
            }

            bool success = true;
            IsDataSyncing = true;
            RecordsExported = String.Empty;

            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            #region Remove extraneous data

            //int rows = 0;

            MinorSyncStatus = "Deleting extraneous page table rows...";
            IDbDriver db = _project.CollectedData.GetDatabase();

            if (db.ToString().ToLower().Contains("sql"))
            {
                using (IDbTransaction transaction = db.OpenTransaction())
                {
                    foreach (View form in _project.Views)
                    {
                        Query formDeleteDuplicateQuery = Database.CreateQuery("WITH cte AS (SELECT *, ROW_NUMBER() OVER(PARTITION BY GlobalRecordId ORDER BY GlobalRecordId) 'RowRank' FROM " + form.TableName + ") " +
                            "DELETE FROM cte " +
                            "WHERE RowRank > 1");
                        Database.ExecuteNonQuery(formDeleteDuplicateQuery, transaction);

                        foreach (Page page in form.Pages)
                        {
                            Query deleteQuery = db.CreateQuery("DELETE FROM " + form.Name + " WHERE GlobalRecordId NOT IN (SELECT GlobalRecordId FROM " + page.TableName + ")");
                            db.ExecuteNonQuery(deleteQuery, transaction);
                            //if (rows > 0)
                            //{
                            //    // report ??
                            //}

                            Query pageDeleteQuery = db.CreateQuery("DELETE FROM " + page.TableName + " WHERE GlobalRecordId NOT IN (SELECT GlobalRecordId FROM " + form.Name + ")");
                            db.ExecuteNonQuery(deleteQuery, transaction);
                            //if (rows > 0)
                            //{
                            //    // report ??
                            //}

                            Query pageDeleteDuplicateQuery = Database.CreateQuery("WITH cte AS (SELECT *, ROW_NUMBER() OVER(PARTITION BY GlobalRecordId ORDER BY GlobalRecordId) 'RowRank' FROM " + page.TableName + ") " +
                            "DELETE FROM cte " +
                            "WHERE RowRank > 1");
                            Database.ExecuteNonQuery(pageDeleteDuplicateQuery, transaction);
                        }
                    }

                    Query linksDeleteQuery = db.CreateQuery("DELETE FROM metaLinks WHERE ToViewId = @ToViewId AND ToRecordGuid NOT IN (SELECT GlobalRecordId FROM " + ContactForm.TableName + ")");
                    linksDeleteQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, ContactFormId));
                    db.ExecuteNonQuery(linksDeleteQuery, transaction);

                    if (db.TableExists("metaHistory"))
                    {
                        Query historyDeleteQuery = db.CreateQuery("DELETE FROM metaHistory WHERE ContactGUID NOT IN (SELECT GlobalRecordId FROM " + ContactForm.TableName + ")");
                        db.ExecuteNonQuery(historyDeleteQuery, transaction);
                    }

                    try
                    {
                        transaction.Commit();
                    }
                    catch (Exception ex0)
                    {
                        Epi.Logger.Log(String.Format(DateTime.Now + ":  " + "DB cleanup exception Type: {0}", ex0.GetType()));
                        Epi.Logger.Log(String.Format(DateTime.Now + ":  " + "DB cleanup exception Message: {0}", ex0.Message));
                        Epi.Logger.Log(String.Format(DateTime.Now + ":  " + "DB cleanup rollback started..."));
                        DbLogger.Log("Database cleanup failed on commit. Exception: " + ex0.Message);

                        try
                        {
                            transaction.Rollback();
                            Epi.Logger.Log(String.Format(DateTime.Now + ":  " + "DB cleanup rollback was successful."));
                        }
                        catch (Exception ex1)
                        {
                            DbLogger.Log("Database cleanup rollback failed. Exception: " + ex1.Message);
                        }
                    }

                    db.CloseTransaction(transaction);
                }
            }
            #endregion // Remove extraneous data

            RecordsExported = String.Empty;
            IsDataSyncing = true;
            IsShowingExportProgress = true;

            SendMessageForAwaitAll();

            DbLogger.Log(String.Format("Initiated process 'export sync file' - IncludeCasesAndContacts = {0}", IncludeCasesAndContacts));

            Task.Factory.StartNew(
                () =>
                {
                    success = CreateCaseSyncFile(filters /*fileName, includeCases, includeCaseExposures, includeContacts, filters, deIdentifyData, recordProcessingScope*/);
                },
                System.Threading.CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).ContinueWith(
                    delegate
                    {
                        SendMessageForUnAwaitAll();

                        TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                        MajorProgressValue = 0;

                        IsDataSyncing = false;

                        MinorProgressValue = 0;

                        stopwatch.Stop();
                        MinorSyncStatus = String.Empty;

                        if (success)
                        {
                            HasExportErrors = false;
                            MajorSyncStatus = "Finished exporting data to sync file.";
                            TimeElapsed = "Elapsed time: " + stopwatch.Elapsed.TotalMinutes.ToString("F1") + " minutes.";
                            DbLogger.Log(String.Format("Completed process 'export sync file' successfully - elapsed time = {0} ms", stopwatch.Elapsed.TotalMilliseconds.ToString()));
                        }
                        else
                        {
                            HasExportErrors = true;
                            MajorSyncStatus = "There was a problem exporting the data.";
                            DbLogger.Log(String.Format("Completed process 'export sync file' with errors"));
                        }

                        CommandManager.InvalidateRequerySuggested();

                    }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        #endregion // Methods

        #region Commands

        public ICommand CancelCommand { get { return new RelayCommand(CancelCommandExecute, CanExecuteCancelCommand); } }
        private void CancelCommandExecute()
        {
            IsShowingExportProgress = false;
            SyncFilePath = String.Empty;
            IncludeCasesAndContacts = true;
            DeIdentifyData = false;
            IsDisplaying = false;
        }

        private bool CanExecuteCancelCommand()
        {
            if (IsDataSyncing)
            {
                return false;
            }

            return true;
        }

        public ICommand StartExportCommand { get { return new RelayCommand(StartExportCommandExecute, CanExecuteStartExportCommand); } }
        private void StartExportCommandExecute()
        {
            switch (RecordScope)
            {
                case "Deleted":
                    RecordProcessingScope = RecordProcessingScope.Deleted;
                    break;
                case "Both":
                    RecordProcessingScope = RecordProcessingScope.Both;
                    break;
                case "Active":
                default:
                    RecordProcessingScope = RecordProcessingScope.Undeleted;
                    break;
            }

            Epi.ImportExport.Filters.ConditionJoinTypes op = Epi.ImportExport.Filters.ConditionJoinTypes.And;

            if (FilterJoinType != null && FilterJoinType.Equals("or", StringComparison.OrdinalIgnoreCase))
            {
                op = Epi.ImportExport.Filters.ConditionJoinTypes.Or;
            }

            ContactTracing.ImportExport.SyncFileFilters filters = new ContactTracing.ImportExport.SyncFileFilters(Database, op);

            if (ApplyFilters == true)
            {
                #region Check to see if user's filtering options make sense

                //if (String.IsNullOrEmpty(varName1) && String.IsNullOrEmpty(value1))
                //{
                //    MessageBox.Show("Neither a variable nor a value have been selected for the first condition. Please ensure both a variable and a value are present before proceeding.", "Missing filter information", MessageBoxButton.OK, MessageBoxImage.Error);
                //    return;
                //}

                //if (!String.IsNullOrEmpty(varName1) && String.IsNullOrEmpty(value1))
                //{
                //    MessageBox.Show("A variable has been selected for the first condition, but no value has been specified. Please specify a value and try again.", "No value specified", MessageBoxButton.OK, MessageBoxImage.Error);
                //    return;
                //}

                //if (!String.IsNullOrEmpty(value1) && String.IsNullOrEmpty(varName1))
                //{
                //    MessageBox.Show("A value has been selected for the first condition, but no variable has been specified. Please specify a variable on which to filter and try again.", "No variable specified", MessageBoxButton.OK, MessageBoxImage.Error);
                //    return;
                //}

                //if (cmbLogicalOperator.SelectedIndex == 1 && String.IsNullOrEmpty(varName2) && String.IsNullOrEmpty(value2))
                //{
                //    MessageBox.Show("Neither a variable nor a value have been selected for the second condition. Please ensure both a variable and a value are present before proceeding.", "Missing filter information", MessageBoxButton.OK, MessageBoxImage.Error);
                //    return;
                //}

                //if (cmbLogicalOperator.SelectedIndex == 1 && !String.IsNullOrEmpty(varName2) && String.IsNullOrEmpty(value2))
                //{
                //    MessageBox.Show("A variable has been selected for the second condition, but no value has been specified. Please specify a value and try again.", "No value specified", MessageBoxButton.OK, MessageBoxImage.Error);
                //    return;
                //}

                //if (cmbLogicalOperator.SelectedIndex == 1 && !String.IsNullOrEmpty(value2) && String.IsNullOrEmpty(varName2))
                //{
                //    MessageBox.Show("A value has been selected for the second condition, but no variable has been specified. Please specify a variable on which to filter and try again.", "No variable specified", MessageBoxButton.OK, MessageBoxImage.Error);
                //    return;
                //}

                #endregion

                if (!String.IsNullOrEmpty(FilterField1) && !String.IsNullOrEmpty(FilterValue1))
                {
                    if (FilterOperator1.Equals("equals", StringComparison.OrdinalIgnoreCase))
                    {
                        TextRowFilterCondition tfc = new TextRowFilterCondition("[" + FilterField1 + "] = @" + FilterField1 + "", "" + FilterField1 + "", "@" + FilterField1 + "", FilterValue1);
                        tfc.Description = "" + FilterField1 + " equals " + FilterValue1;
                        filters.Add(tfc);
                    }
                    else
                    {
                        string tempFilterValue1 = "%" + FilterValue1 + "%";
                        TextRowFilterCondition tfc = new TextRowFilterCondition("[" + FilterField1 + "] LIKE @" + FilterField1 + "", "" + FilterField1 + "", "@" + FilterField1 + "", tempFilterValue1);
                        tfc.Description = "" + FilterField1 + " contains " + tempFilterValue1;
                        tfc.ConditionOperator = ConditionOperators.Contains;
                        filters.Add(tfc);
                    }
                }

                if (!String.IsNullOrEmpty(FilterField2) && !String.IsNullOrEmpty(FilterValue2))
                {
                    if (FilterOperator2.Equals("equals", StringComparison.OrdinalIgnoreCase))
                    {
                        TextRowFilterCondition tfc = new TextRowFilterCondition("[" + FilterField2 + "] = @" + FilterField2 + "", "" + FilterField2 + "", "@" + FilterField2 + "", FilterValue2);
                        tfc.Description = "" + FilterField2 + " equals " + FilterValue2;
                        filters.Add(tfc);
                    }
                    else
                    {
                        string tempFilterValue2 = "%" + FilterValue2 + "%";
                        TextRowFilterCondition tfc = new TextRowFilterCondition("[" + FilterField2 + "] LIKE @" + FilterField2 + "", "" + FilterField2 + "", "@" + FilterField2 + "", tempFilterValue2);
                        tfc.Description = "" + FilterField2 + " contains " + tempFilterValue2;
                        tfc.ConditionOperator = ConditionOperators.Contains;
                        filters.Add(tfc);
                    }
                }
            }

            CreateCaseSyncFileStart(filters);
        }

        private bool CreateCaseSyncFile(ContactTracing.ImportExport.SyncFileFilters filters/*string fileName, bool includeCases, bool includeCaseExposures, bool includeContacts, Epi.ImportExport.Filters.RowFilters filters, bool deIdentifyData, Epi.RecordProcessingScope recordProcessingScope*/)
        {
            MajorProgressValue = 0;

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            MajorSyncStatus = "Exporting data to " + SyncFilePath + "...";

            ContactTracing.ImportExport.XmlDataExporter exporter; // = new ContactTracing.ImportExport.XmlSqlDataExporter(Project, includeContacts, recordProcessingScope);

            if (Database.ToString().ToLower().Contains("sql") && !(Database is Epi.Data.Office.OleDbDatabase))
            {
                exporter = new ContactTracing.ImportExport.XmlSqlDataExporter(_project, IncludeCasesAndContacts, RecordProcessingScope);
            }
            else
            {
                exporter = new ContactTracing.ImportExport.XmlDataExporter(_project, IncludeCasesAndContacts, RecordProcessingScope);
            }

            if (filters != null)
            {
                if (exporter.Filters.ContainsKey(Core.Constants.CASE_FORM_NAME))
                {
                    exporter.Filters[Core.Constants.CASE_FORM_NAME] = filters;
                }
                else
                {
                    exporter.Filters.Add(Core.Constants.CASE_FORM_NAME, filters);
                }
            }

            string tempXmlFileName = SyncFilePath + ".xml";

            exporter.MinorProgressChanged += exporter_MinorProgressChanged;
            exporter.MajorProgressChanged += exporter_MajorProgressChanged;
            exporter.MinorStatusChanged += exporter_MinorStatusChanged;

            if (ApplyLastSaveFilter)
            {
                exporter.StartDate = StartDate;
                exporter.EndDate = EndDate;
            }

            if (DeIdentifyData)
            {
                if (!IsCountryUS)
                {
                    exporter.AddFieldsToNull(new List<string> { "Surname", "OtherNames", "PhoneNumber", "PhoneOwner", "HeadHouse", "ContactName1", "ContactName2", "ContactName3", "FuneralName1", "FuneralName2", "HospitalBeforeIllPatient", "TradHealerName", "InterviewerName", "InterviewerPhone", "InterviwerEmail", "ProxyName" }, CaseForm.Name);
                    exporter.AddFieldsToNull(new List<string> { "SurnameLab", "OtherNameLab" }, LabForm.Name);
                    exporter.AddFieldsToNull(new List<string> { "ContactSurname", "ContactOtherNames", "ContactHeadHouse", "ContactPhone", "LC1" }, ContactForm.Name);
                }
                else
                {
                    exporter.AddFieldsToNull(new List<string> { "Surname", "OtherNames", "PhoneNumber", "PhoneOwner", "HeadHouse", "ContactName1", "ContactName2", "ContactName3", "FuneralName1", "FuneralName2", "HospitalBeforeIllPatient", "TradHealerName", "InterviewerName", "InterviewerPhone", "InterviwerEmail", "ProxyName", "DOB", "Email", "AddressRes", "AddressOnset", "ProxyPhone", "ProxyEmail" }, CaseForm.Name);
                    exporter.AddFieldsToNull(new List<string> { "SurnameLab", "OtherNameLab", "PersonLabSubmit", "PhoneLabSubmit", "EmailLabSubmit" }, LabForm.Name);
                    exporter.AddFieldsToNull(new List<string> { "ContactSurname", "ContactOtherNames", "ContactHeadHouse", "ContactPhone", "LC1", "ContactDOB", "ContactAddress", "ContactEmail" }, ContactForm.Name);
                }
            }

            _increment = exporter.MajorIncrement;

            bool success = false;

            try
            {
                exporter.WriteTo(tempXmlFileName);
                success = true;
            }
            catch (Exception ex)
            {
                RecordsExported = "Export failed during write operation: " + ex.Message;
            }
            finally
            {
                exporter.MinorProgressChanged -= exporter_MinorProgressChanged;
                exporter.MajorProgressChanged -= exporter_MajorProgressChanged;
                exporter.MinorStatusChanged -= exporter_MinorStatusChanged;
            }

            if (!success)
            {
                MajorSyncStatus = "There was a problem exporting data.";
                sw.Stop();
                System.IO.File.Delete(tempXmlFileName);
                return success;
            }

            string casesExported = exporter.ExportInfo.RecordsPackaged[CaseForm].ToString();
            string contactsExported = exporter.ExportInfo.RecordsPackaged[ContactForm].ToString();
            string labsExported = exporter.ExportInfo.RecordsPackaged[LabForm].ToString();

            RecordsExported = "Exported " + casesExported + " cases, " + contactsExported + " contacts, " +
                labsExported + " lab results.";

            DbLogger.Log(String.Format("Process 'export sync file' reports {0} case records, {1} contact records, and {2} lab records were written to disk.", casesExported, contactsExported, labsExported));

            MajorSyncStatus = "Compressing data...";

            try
            {
                System.IO.FileInfo fi = new System.IO.FileInfo(tempXmlFileName);
                Epi.ImportExport.ImportExportHelper.CompressDataPackage(fi);
            }
            catch (Exception ex)
            {
                RecordsExported = "Export failed during file compression: " + ex.Message;
                return false;
            }

            string tempGzFileName = tempXmlFileName + ".gz";

            MajorSyncStatus = "Encrypting data...";

            try
            {
                Epi.Configuration.EncryptFile(tempGzFileName, SyncFilePath, "vQ@6L'<J3?)~5=vQnwh(2ic;>.<=dknF&/TZ4Uu!$78", String.Empty, String.Empty, 1000);
            }
            catch (Exception ex)
            {
                RecordsExported = "Export failed during file encryption: " + ex.Message;
                return false;
            }

            MajorSyncStatus = "Deleting temporary files...";

            try
            {
                System.IO.File.Delete(tempXmlFileName);
                System.IO.File.Delete(tempGzFileName);
            }
            catch (Exception ex)
            {
                RecordsExported = "Warning: Temporary files could not be cleaned. Message: " + ex.Message;
            }

            sw.Stop();
            System.Diagnostics.Debug.Print("Export completed in " + sw.Elapsed.TotalMilliseconds + " ms.");

            return success;
            //SyncStatus = "Export completed in " + sw.Elapsed.TotalSeconds.ToString("F1") + " seconds.";
        }

        private bool CanExecuteStartExportCommand()
        {
            if (ApplyFilters)
            {
                if (!String.IsNullOrEmpty(FilterField1) && (String.IsNullOrEmpty(FilterOperator1) || String.IsNullOrEmpty(FilterValue1))) { return false; }
                if (!String.IsNullOrEmpty(FilterField2) && (String.IsNullOrEmpty(FilterOperator2) || String.IsNullOrEmpty(FilterValue2))) { return false; }

                if (!String.IsNullOrEmpty(FilterOperator1) && (String.IsNullOrEmpty(FilterField1) || (String.IsNullOrEmpty(FilterValue1)))) { return false; }
                if (!String.IsNullOrEmpty(FilterOperator2) && (String.IsNullOrEmpty(FilterField2) || (String.IsNullOrEmpty(FilterValue2)))) { return false; }

                if (!String.IsNullOrEmpty(FilterValue1) && (String.IsNullOrEmpty(FilterField1) || (String.IsNullOrEmpty(FilterOperator1)))) { return false; }
                if (!String.IsNullOrEmpty(FilterValue2) && (String.IsNullOrEmpty(FilterField2) || (String.IsNullOrEmpty(FilterOperator2)))) { return false; }

                if (!String.IsNullOrEmpty(FilterField1) && !String.IsNullOrEmpty(FilterField2) && String.IsNullOrEmpty(FilterJoinType)) { return false; }
            }

            return (!String.IsNullOrEmpty(SyncFilePath) && _project != null);
        }

        public ICommand StopExportCommand { get { return new RelayCommand(StopExportCommandExecute, CanExecuteStopExportCommand); } }
        private void StopExportCommandExecute()
        {
            IsShowingExportProgress = false;
        }

        private bool CanExecuteStopExportCommand()
        {
            return !IsDataSyncing;
        }
        #endregion // Commands
    }
}
