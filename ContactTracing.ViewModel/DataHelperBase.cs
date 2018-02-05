using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows.Input;
using Epi;
using Epi.Data;
using Epi.Fields;
using ContactTracing.Core;
using ContactTracing.ViewModel.Events;

namespace ContactTracing.ViewModel
{
    public abstract class DataHelperBase : ObservableObject, IDataHelper
    {
        private System.Windows.Shell.TaskbarItemProgressState _taskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
        private double _taskbarProgressValue = 0.0;
        protected string _outbreakName = String.Empty;
        private ObservableCollection<LabResultViewModel> _labResultCollection = new ObservableCollection<LabResultViewModel>();
        private int _changeset = 0;
        private string _vhfDbVersion = "0.8.0.0";
        private string _syncStatus = String.Empty;

        private bool _sudanTestsDetected = false;
        private bool _ebolaTestsDetected = false;
        private bool _marburgTestsDetected = false;
        private bool _bundibugyoTestsDetected = false;
        private bool _cchfTestsDetected = false;
        private bool _riftTestsDetected = false;
        private bool _lassaTestsDetected = false;
        private string _loadStatus = String.Empty;
        private string _upgradeStatus = String.Empty;
        private Core.Enums.VirusTestTypes _virusTestType = Core.Enums.VirusTestTypes.Ebola;
        protected internal readonly object _taskbarValueLock = new object();

        private bool _isShowingDatabaseUpgrade = false;

        public bool IsShowingDatabaseUpgrade
        {
            get
            {
                return this._isShowingDatabaseUpgrade;
            }
            set
            {
                this._isShowingDatabaseUpgrade = value;
                RaisePropertyChanged("IsShowingDatabaseUpgrade");
            }
        }

        public View CaseForm { get; set; }
        public View LabForm { get; set; }
        public VhfProject Project { get; set; }
        public IDbDriver Database { get; protected set; }
        public string SyncStatus
        {
            get
            {
                return this._syncStatus;
            }
            set
            {
                this._syncStatus = value;
                RaisePropertyChanged("SyncStatus");
            }
        }

        public DateTime? OutbreakDate { get; protected set; }
        public string IDPrefix { get; protected set; }
        public string IDSeparator { get; protected set; }
        public string IDPattern { get; protected set; }
        public Core.Enums.VirusTestTypes VirusTestType
        {
            get
            {
                return _virusTestType;
            }
            set
            {
                _virusTestType = value;
                RaisePropertyChanged("VirusTestType");
            }
        }
        public string Country { get; protected set; }
        public bool IsShortForm { get; set; } //17040
        public int ContactFormType { get; protected set; }

        public static string SampleInterpretConfirmedAcute = String.Empty;
        public static string SampleInterpretConfirmedConvalescent = String.Empty;
        public static string SampleInterpretNotCase = String.Empty;
        public static string SampleInterpretIndeterminate = String.Empty;
        public static string SampleInterpretNegativeNeedsFollowUp = String.Empty;

        public static string PCRPositive = String.Empty;
        public static string PCRNegative = String.Empty;
        public static string PCRIndeterminate = String.Empty;
        public static string PCRNotAvailable = String.Empty;

        public static string SampleTypeWholeBlood = String.Empty;
        public static string SampleTypeSerum = String.Empty;
        public static string SampleTypeHeartBlood = String.Empty;
        public static string SampleTypeSkin = String.Empty;
        public static string SampleTypeOther = String.Empty;
        public static string SampleTypeSalivaSwab = String.Empty;

        /// <summary>
        /// Gets/sets the changeset number (applicable for multi-user data sync)
        /// </summary>
        public int Changeset
        {
            get
            {
                return this._changeset;
            }
            protected set
            {
                if (value < Changeset)
                {
                    throw new Core.Exceptions.InvalidChangesetException("New changeset number cannot be less than the current changeset.");
                }
                else if (value != Changeset)
                {
                    this._changeset = value;
                    RaisePropertyChanged("Changeset");
                }
            }
        }

        /// <summary>
        /// Gets/sets the current name of the outbreak
        /// </summary>
        public string OutbreakName
        {
            get
            {
                return this._outbreakName;
            }
            set
            {
                if (this.OutbreakName != value)
                {
                    this._outbreakName = value;
                    RaisePropertyChanged("OutbreakName");
                }
            }
        }

        /// <summary>
        /// The latest version of the Vhf app that has connected to the server
        /// </summary>
        public string VhfDbVersion
        {
            get
            {
                return this._vhfDbVersion;
            }
            set
            {
                if (this.VhfDbVersion != value)
                {
                    this._vhfDbVersion = value;
                    RaisePropertyChanged("VhfDbVersion");
                }
            }
        }

        public bool SudanTestsDetected
        {
            get
            {
                return this._sudanTestsDetected;
            }
            protected set
            {
                this._sudanTestsDetected = value;
                RaisePropertyChanged("SudanTestsDetected");
            }
        }
        public bool EbolaTestsDetected
        {
            get
            {
                return this._ebolaTestsDetected;
            }
            protected set
            {
                this._ebolaTestsDetected = value;
                RaisePropertyChanged("EbolaTestsDetected");
            }
        }
        public bool MarburgTestsDetected
        {
            get
            {
                return this._marburgTestsDetected;
            }
            protected set
            {
                this._marburgTestsDetected = value;
                RaisePropertyChanged("MarburgTestsDetected");
            }
        }
        public bool BundibugyoTestsDetected
        {
            get
            {
                return this._bundibugyoTestsDetected;
            }
            protected set
            {
                this._bundibugyoTestsDetected = value;
                RaisePropertyChanged("BundibugyoTestsDetected");
            }
        }
        public bool CCHFTestsDetected
        {
            get
            {
                return this._cchfTestsDetected;
            }
            protected set
            {
                this._cchfTestsDetected = value;
                RaisePropertyChanged("CCHFTestsDetected");
            }
        }
        public bool RiftTestsDetected
        {
            get
            {
                return this._riftTestsDetected;
            }
            protected set
            {
                this._riftTestsDetected = value;
                RaisePropertyChanged("RiftTestsDetected");
            }
        }
        public bool LassaTestsDetected
        {
            get
            {
                return this._lassaTestsDetected;
            }
            protected set
            {
                this._lassaTestsDetected = value;
                RaisePropertyChanged("LassaTestsDetected");
            }
        }

        /// <summary>
        /// Gets/sets the current progress value for the progress bar that shows up in the app's taskbar icon.
        /// </summary>
        public double TaskbarProgressValue
        {
            get
            {
                lock (_taskbarValueLock)
                {
                    return this._taskbarProgressValue;
                }
            }
            set
            {
                lock (_taskbarValueLock)
                {
                    if (this.TaskbarProgressValue != value)
                    {
                        this._taskbarProgressValue = value;
                        RaisePropertyChanged("TaskbarProgressValue");
                    }
                }
            }
        }

        public ObservableCollection<LabResultViewModel> LabResultCollection
        {
            get
            {
                return this._labResultCollection;
            }
            protected set
            {
                if (this._labResultCollection != value)
                {
                    this._labResultCollection = value;
                    RaisePropertyChanged("LabResultCollection");
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

        protected bool IsMicrosoftSQLDatabase
        {
            get
            {
                return (Database.ToString().ToLower().Contains("sql"));
            }
        }

        public string LoadStatus
        {
            get
            {
                return this._loadStatus;
            }
            set
            {
                if (_loadStatus != value)
                {
                    this._loadStatus = value;
                    RaisePropertyChanged("LoadStatus");
                }
            }
        }

        public string UpgradeStatus
        {
            get
            {
                return this._upgradeStatus;
            }
            set
            {
                if (_upgradeStatus != value)
                {
                    this._upgradeStatus = value;
                    RaisePropertyChanged("UpgradeStatus");
                }
            }
        }

        protected virtual void UpgradeDatabase()
        {
            if (!Database.TableExists("metaHistory"))
            {
                IsShowingDatabaseUpgrade = true;

                IDbTransaction transaction = Database.OpenTransaction();

                if (IsMicrosoftSQLDatabase)
                {
                    Query createTableQuery = Database.CreateQuery("CREATE TABLE dbo.metaHistory (" +
                                "ContactGUID uniqueidentifier, " +
                                "FollowUpDate datetime, " +
                                "StatusOnDate smallint, " +
                                "[Note] ntext, " +
                                "Temp1 float, " +
                                "Temp2 float " +
                                    ");");
                    Database.ExecuteNonQuery(createTableQuery, transaction);
                }
                else
                {
                    Query createTableQuery = Database.CreateQuery("CREATE TABLE metaHistory (" +
                                "ContactGUID GUID, " +
                                "FollowUpDate datetime, " +
                                "StatusOnDate SHORT, " +
                                "[Note] MEMO, " +
                                "Temp1 double, " +
                                "Temp2 double " +
                                    ");");
                    Database.ExecuteNonQuery(createTableQuery, transaction);
                }

                try
                {
                    UpgradeFollowUpData(transaction);
                    transaction.Commit();
                    DbLogger.Log("MetaHistory upgrade succeeded.");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    DbLogger.Log("MetaHistory upgrade failed. Transaction rolled back. Exception: " + ex.Message);
                }
                finally
                {
                    IsShowingDatabaseUpgrade = false;
                    transaction.Dispose();
                }
            }
        }

        protected virtual void UpgradeFollowUpData(IDbTransaction transaction)
        {
            List<string> columnNames = new List<string>();
            columnNames.Add("ToRecordGuid");
            columnNames.Add("LastContactDate");
            columnNames.Add("Day1");
            columnNames.Add("Day2");
            columnNames.Add("Day3");
            columnNames.Add("Day4");
            columnNames.Add("Day5");
            columnNames.Add("Day6");
            columnNames.Add("Day7");
            columnNames.Add("Day8");
            columnNames.Add("Day9");
            columnNames.Add("Day10");
            columnNames.Add("Day11");
            columnNames.Add("Day12");
            columnNames.Add("Day13");
            columnNames.Add("Day14");
            columnNames.Add("Day15");
            columnNames.Add("Day16");
            columnNames.Add("Day17");
            columnNames.Add("Day18");
            columnNames.Add("Day19");
            columnNames.Add("Day20");
            columnNames.Add("Day21");
            columnNames.Add("Day1Notes");
            columnNames.Add("Day2Notes");
            columnNames.Add("Day3Notes");
            columnNames.Add("Day4Notes");
            columnNames.Add("Day5Notes");
            columnNames.Add("Day6Notes");
            columnNames.Add("Day7Notes");
            columnNames.Add("Day8Notes");
            columnNames.Add("Day9Notes");
            columnNames.Add("Day10Notes");
            columnNames.Add("Day11Notes");
            columnNames.Add("Day12Notes");
            columnNames.Add("Day13Notes");
            columnNames.Add("Day14Notes");
            columnNames.Add("Day15Notes");
            columnNames.Add("Day16Notes");
            columnNames.Add("Day17Notes");
            columnNames.Add("Day18Notes");
            columnNames.Add("Day19Notes");
            columnNames.Add("Day20Notes");
            columnNames.Add("Day21Notes");
            DataTable linksTable = Database.GetTableData("metaLinks", columnNames);

            int total = linksTable.Rows.Count;

            TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;

            for (int i = 0; i < total; i++)
            {
                double progress = (double)i / (double)total;
                TaskbarProgressValue = progress;
                UpgradeStatus = String.Format("A database upgrade is in progress. Please do not close the VHF application until the upgrade has been completed.");

                DataRow row = linksTable.Rows[i];
                for (int j = 2; j < 23; j++)
                {
                    string rowJValue = row[j].ToString();

                    if (!String.IsNullOrEmpty(rowJValue))
                    {
                        string guid = row[0].ToString();
                        string status = rowJValue;

                        string contactGuid = row[0].ToString();
                        System.DateTime followUpDate = ((System.DateTime)row[1]).Add(new System.TimeSpan(j - 1, 0, 0, 0));
                        Int16 statusOnDate = Convert.ToInt16(row[j]);
                        string note = row[j + 21].ToString();

                        Query insertQuery = Database.CreateQuery("INSERT INTO [metaHistory] (ContactGUID, FollowUpDate, StatusOnDate, [Note]) VALUES (" +
                                "@ContactGuid, @FollowUpDate, @StatusOnDate, @Note)");
                        insertQuery.Parameters.Add(new QueryParameter("@ContactGuid", DbType.String, contactGuid));
                        insertQuery.Parameters.Add(new QueryParameter("@FollowUpDate", DbType.DateTime, followUpDate));
                        insertQuery.Parameters.Add(new QueryParameter("@StatusOnDate", DbType.Int16, statusOnDate));
                        insertQuery.Parameters.Add(new QueryParameter("@Note", DbType.String, note));

                        try
                        {
                            Database.ExecuteNonQuery(insertQuery, transaction);
                        }
                        catch (System.Data.OleDb.OleDbException)
                        { 

                        }

                    }
                }
            }

            TaskbarProgressValue = 0;
            TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
            UpgradeStatus = String.Empty;
        }

        public abstract void ClearCollections();
        protected abstract bool PopulateCollections(bool initialLoad = false);
        public virtual void RepopulateCollections(bool initialLoad = false) { }

        public virtual void SetupDatabase(bool showSetupScreen = true)
        {
            #region Create Columns
            if (!Database.ColumnExists("metaLinks", ContactTracing.Core.Constants.LAST_CONTACT_DATE_COLUMN_NAME))
            {
                List<Epi.Data.TableColumn> tcList = new List<Epi.Data.TableColumn>();

                tcList.Add(new Epi.Data.TableColumn(ContactTracing.Core.Constants.LAST_CONTACT_DATE_COLUMN_NAME, GenericDbColumnType.Date, true));
                tcList.Add(new Epi.Data.TableColumn("ContactType", GenericDbColumnType.Int32, true));
                tcList.Add(new Epi.Data.TableColumn("RelationshipType", GenericDbColumnType.String, true));
                tcList.Add(new Epi.Data.TableColumn("Tentative", GenericDbColumnType.Byte, true));
                tcList.Add(new Epi.Data.TableColumn("IsEstimatedContactDate", GenericDbColumnType.Boolean, true));

                tcList.Add(new Epi.Data.TableColumn("Day1", GenericDbColumnType.Byte, true));
                tcList.Add(new Epi.Data.TableColumn("Day2", GenericDbColumnType.Byte, true));
                tcList.Add(new Epi.Data.TableColumn("Day3", GenericDbColumnType.Byte, true));
                tcList.Add(new Epi.Data.TableColumn("Day4", GenericDbColumnType.Byte, true));
                tcList.Add(new Epi.Data.TableColumn("Day5", GenericDbColumnType.Byte, true));
                tcList.Add(new Epi.Data.TableColumn("Day6", GenericDbColumnType.Byte, true));
                tcList.Add(new Epi.Data.TableColumn("Day7", GenericDbColumnType.Byte, true));
                tcList.Add(new Epi.Data.TableColumn("Day8", GenericDbColumnType.Byte, true));
                tcList.Add(new Epi.Data.TableColumn("Day9", GenericDbColumnType.Byte, true));
                tcList.Add(new Epi.Data.TableColumn("Day10", GenericDbColumnType.Byte, true));
                tcList.Add(new Epi.Data.TableColumn("Day11", GenericDbColumnType.Byte, true));
                tcList.Add(new Epi.Data.TableColumn("Day12", GenericDbColumnType.Byte, true));
                tcList.Add(new Epi.Data.TableColumn("Day13", GenericDbColumnType.Byte, true));
                tcList.Add(new Epi.Data.TableColumn("Day14", GenericDbColumnType.Byte, true));
                tcList.Add(new Epi.Data.TableColumn("Day15", GenericDbColumnType.Byte, true));
                tcList.Add(new Epi.Data.TableColumn("Day16", GenericDbColumnType.Byte, true));
                tcList.Add(new Epi.Data.TableColumn("Day17", GenericDbColumnType.Byte, true));
                tcList.Add(new Epi.Data.TableColumn("Day18", GenericDbColumnType.Byte, true));
                tcList.Add(new Epi.Data.TableColumn("Day19", GenericDbColumnType.Byte, true));
                tcList.Add(new Epi.Data.TableColumn("Day20", GenericDbColumnType.Byte, true));
                tcList.Add(new Epi.Data.TableColumn("Day21", GenericDbColumnType.Byte, true));

                tcList.Add(new Epi.Data.TableColumn("Day1Notes", GenericDbColumnType.String, true));
                tcList.Add(new Epi.Data.TableColumn("Day2Notes", GenericDbColumnType.String, true));
                tcList.Add(new Epi.Data.TableColumn("Day3Notes", GenericDbColumnType.String, true));
                tcList.Add(new Epi.Data.TableColumn("Day4Notes", GenericDbColumnType.String, true));
                tcList.Add(new Epi.Data.TableColumn("Day5Notes", GenericDbColumnType.String, true));
                tcList.Add(new Epi.Data.TableColumn("Day6Notes", GenericDbColumnType.String, true));
                tcList.Add(new Epi.Data.TableColumn("Day7Notes", GenericDbColumnType.String, true));
                tcList.Add(new Epi.Data.TableColumn("Day8Notes", GenericDbColumnType.String, true));
                tcList.Add(new Epi.Data.TableColumn("Day9Notes", GenericDbColumnType.String, true));
                tcList.Add(new Epi.Data.TableColumn("Day10Notes", GenericDbColumnType.String, true));
                tcList.Add(new Epi.Data.TableColumn("Day11Notes", GenericDbColumnType.String, true));
                tcList.Add(new Epi.Data.TableColumn("Day12Notes", GenericDbColumnType.String, true));
                tcList.Add(new Epi.Data.TableColumn("Day13Notes", GenericDbColumnType.String, true));
                tcList.Add(new Epi.Data.TableColumn("Day14Notes", GenericDbColumnType.String, true));
                tcList.Add(new Epi.Data.TableColumn("Day15Notes", GenericDbColumnType.String, true));
                tcList.Add(new Epi.Data.TableColumn("Day16Notes", GenericDbColumnType.String, true));
                tcList.Add(new Epi.Data.TableColumn("Day17Notes", GenericDbColumnType.String, true));
                tcList.Add(new Epi.Data.TableColumn("Day18Notes", GenericDbColumnType.String, true));
                tcList.Add(new Epi.Data.TableColumn("Day19Notes", GenericDbColumnType.String, true));
                tcList.Add(new Epi.Data.TableColumn("Day20Notes", GenericDbColumnType.String, true));
                tcList.Add(new Epi.Data.TableColumn("Day21Notes", GenericDbColumnType.String, true));

                foreach (Epi.Data.TableColumn tableColumn in tcList)
                {
                    Database.AddColumn("metaLinks", tableColumn);
                }
            }

            //Task task = Task.Factory.StartNew(() => 
            //{ 
                

            //}, System.Threading.CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.FromCurrentSynchronizationContext());
            
            

            

            if (!Database.ColumnExists("metaDbInfo", "OutbreakName"))
            {
                List<Epi.Data.TableColumn> tcList = new List<Epi.Data.TableColumn>();
                tcList.Add(new Epi.Data.TableColumn("OutbreakName", GenericDbColumnType.String, true));
                tcList.Add(new Epi.Data.TableColumn("OutbreakDate", GenericDbColumnType.DateTime, true));
                tcList.Add(new Epi.Data.TableColumn("IDPrefix", GenericDbColumnType.String, true));
                tcList.Add(new Epi.Data.TableColumn("IDSeparator", GenericDbColumnType.String, true));
                tcList.Add(new Epi.Data.TableColumn("IDPattern", GenericDbColumnType.String, true));
                tcList.Add(new Epi.Data.TableColumn("Virus", GenericDbColumnType.String, true));
                tcList.Add(new Epi.Data.TableColumn("PrimaryCountry", GenericDbColumnType.String, true));
                tcList.Add(new Epi.Data.TableColumn("VhfVersion", GenericDbColumnType.String, 32, false));
                tcList.Add(new Epi.Data.TableColumn("Culture", GenericDbColumnType.String, 5, false));
                tcList.Add(new Epi.Data.TableColumn("Adm1", GenericDbColumnType.String, 48, true));
                tcList.Add(new Epi.Data.TableColumn("Adm2", GenericDbColumnType.String, 48, true));
                tcList.Add(new Epi.Data.TableColumn("Adm3", GenericDbColumnType.String, 48, true));
                tcList.Add(new Epi.Data.TableColumn("Adm4", GenericDbColumnType.String, 48, true));
                tcList.Add(new Epi.Data.TableColumn("ContactFormType", GenericDbColumnType.Int32, true));
                tcList.Add(new Epi.Data.TableColumn("IsShortForm", GenericDbColumnType.Boolean, false)); //17040 // changed to disallow nulls

                foreach (Epi.Data.TableColumn tableColumn in tcList)
                {
                    try
                    {
                        Database.AddColumn("metaDbInfo", tableColumn);
                    }
                    catch (Exception)
                    {
                        // do nothing
                    }
                }

                try
                {
                    Query contactFormTypeUpdateQuery = Database.CreateQuery("UPDATE metaDbInfo SET ContactFormType = 1");
                    Database.ExecuteNonQuery(contactFormTypeUpdateQuery);

                    if (CaseForm.Fields.Contains("DistrictRes") && CaseForm.Fields.Contains("SCRes") && CaseForm.Fields.Contains("ParishRes") && CaseForm.Fields.Contains("VillageRes"))
                    {
                        RenderableField field1 = (CaseForm.Fields["DistrictRes"] as RenderableField);
                        RenderableField field2 = (CaseForm.Fields["SCRes"] as RenderableField);
                        RenderableField field3 = (CaseForm.Fields["ParishRes"] as RenderableField);
                        RenderableField field4 = (CaseForm.Fields["VillageRes"] as RenderableField);

                        if (field1 != null && field2 != null && field3 != null && field4 != null && field1.PromptText != null && field2.PromptText != null && field3.PromptText != null && field4.PromptText != null)
                        {
                            string label1 = field1.PromptText.TrimEnd(':').Trim();
                            string label2 = field2.PromptText.TrimEnd(':').Trim();
                            string label3 = field3.PromptText.TrimEnd(':').Trim();
                            string label4 = field4.PromptText.TrimEnd(':').Trim();

                            Query updateQuery = Database.CreateQuery("UPDATE metaDbInfo SET [Adm1] = @Adm1, [Adm2] = @Adm2, [Adm3] = @Adm3, [Adm4] = @Adm4");
                            updateQuery.Parameters.Add(new QueryParameter("@Adm1", DbType.String, label1));
                            updateQuery.Parameters.Add(new QueryParameter("@Adm2", DbType.String, label2));
                            updateQuery.Parameters.Add(new QueryParameter("@Adm3", DbType.String, label3));
                            updateQuery.Parameters.Add(new QueryParameter("@Adm4", DbType.String, label4));
                            Database.ExecuteNonQuery(updateQuery);
                        }
                        else
                        {
                            throw new InvalidOperationException("DistrictRes, SCRes, ParishRes, and VillageRes must all exist on the case report form. One or more of these fields are missing.");
                        }
                    }
                }
                catch (Exception)
                {
                    // do nothing
                }
            }

            //RunInitialSetup(showSetupScreen);
            #endregion // Create Columns
        }

        private string GetPCRResult(string value)
        {
            string result = String.Empty;
            switch (value)
            {
                case "1":
                    result = DataHelperBase.PCRPositive;
                    break;
                case "2":
                    result = DataHelperBase.PCRNegative;
                    break;
                case "3":
                    result = DataHelperBase.PCRIndeterminate;
                    break;
                case "4":
                    result = DataHelperBase.PCRNotAvailable;
                    break;
            }

            return result;
        }

        protected virtual string ConvertFinalLabClassificationCode(string code)
        {
            switch (code)
            {
                case "0":
                    return "Not a Case";
                case "1":
                    return "Confirmed Acute";
                case "2":
                    return "Confirmed Convalescent";
                case "3":
                    return "Indeterminate";
                case "4":
                    return "Needs follow-up sample";
            }

            return String.Empty;
        }

        protected LabResultViewModel CreateLabResultFromGuid(string guid)
        {
            LabResultViewModel r = new LabResultViewModel(LabForm);

            string labQueryText = "SELECT * " +
                LabForm.FromViewSQL + " " +
                "WHERE [t.GlobalRecordId] = @GlobalRecordId";

            if (Database.ToString().ToLower().Contains("sql"))
            {
                labQueryText = "SELECT * " +
                LabForm.FromViewSQL + " " +
                "WHERE t.GlobalRecordId = @GlobalRecordId";
            }

            Query selectQuery = Database.CreateQuery(labQueryText);
            selectQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, guid));
            DataTable dt = Database.Select(selectQuery);

            if (dt != null && dt.Columns.Contains("t.GlobalRecordId"))
            {
                dt.Columns["t.GlobalRecordId"].ColumnName = "GlobalRecordId";
            }

            if (dt.Rows.Count == 1)
            {
                DataRow row = dt.Rows[0];
                string fkey = row["FKEY"].ToString();

                string caseQueryText = "SELECT * " +
                    CaseForm.FromViewSQL + " " +
                    "WHERE [t.GlobalRecordId] = @FKEY";

                if (Database.ToString().ToLower().Contains("sql"))
                {
                    caseQueryText = "SELECT * " +
                    CaseForm.FromViewSQL + " " +
                    "WHERE t.GlobalRecordId = @FKEY";
                }

                selectQuery = Database.CreateQuery(caseQueryText);
                selectQuery.Parameters.Add(new QueryParameter("@FKEY", DbType.String, fkey));
                DataTable caseDt = Database.Select(selectQuery);

                LoadResultData(row, r, caseDt.DefaultView);
            }
            else
            {
                r = null;
            }

            return r;
        }

        protected void LoadResultData(DataRow row, LabResultViewModel labResultVM, DataView caseView = null)
        {
            if (caseView != null && caseView.Table.Columns.Contains("t.GlobalRecordId"))
            {
                caseView.Table.Columns["t.GlobalRecordId"].ColumnName = "GlobalRecordId";
                labResultVM.RecordId = row["GlobalRecordId"].ToString();
            }
            else
            {
                if (Database.ToString().ToLower().Contains("sql"))
                {
                    labResultVM.RecordId = row["GlobalRecordId"].ToString();
                }
                else
                {
                    labResultVM.RecordId = row["t.GlobalRecordId"].ToString();
                }
            }

            labResultVM.LabCaseID = row["ID"].ToString();
            if (!String.IsNullOrEmpty(row["DateSampleCollected"].ToString()))
            {
                labResultVM.DateSampleCollected = DateTime.Parse(row["DateSampleCollected"].ToString());
            }
            if (!String.IsNullOrEmpty(row["DateSampleTested"].ToString()))
            {
                labResultVM.DateSampleTested = DateTime.Parse(row["DateSampleTested"].ToString());
            }
            if (!String.IsNullOrEmpty(row["DaysAcute"].ToString()))
            {
                labResultVM.DaysAcute = Int32.Parse(row["DaysAcute"].ToString());
            }
            labResultVM.FieldLabSpecimenID = row["FieldLabSpecID"].ToString();

            switch (row["SampleInterpret"].ToString())
            {
                case "1":
                    labResultVM.SampleInterpretation = DataHelperBase.SampleInterpretConfirmedAcute;
                    break;
                case "2":
                    labResultVM.SampleInterpretation = DataHelperBase.SampleInterpretConfirmedConvalescent;
                    break;
                case "3":
                    labResultVM.SampleInterpretation = DataHelperBase.SampleInterpretNotCase;
                    break;
                case "4":
                    labResultVM.SampleInterpretation = DataHelperBase.SampleInterpretIndeterminate;
                    break;
                case "5":
                    labResultVM.SampleInterpretation = DataHelperBase.SampleInterpretNegativeNeedsFollowUp;
                    break;
            }

            switch (row["Malariat"].ToString())
            {
                case "1":
                    labResultVM.MalariaRapidTest = DataHelperBase.PCRPositive;
                    break;
                case "2":
                    labResultVM.MalariaRapidTest = DataHelperBase.PCRNegative;
                    break;
                case "3":
                    labResultVM.MalariaRapidTest = DataHelperBase.PCRNotAvailable;
                    break;
            }

            switch (row["SampleType"].ToString())
            {
                case "1":
                    labResultVM.SampleType = DataHelperBase.SampleTypeWholeBlood;
                    break;
                case "2":
                    labResultVM.SampleType = DataHelperBase.SampleTypeSerum;//"Serum";
                    break;
                case "3":
                    labResultVM.SampleType = DataHelperBase.SampleTypeHeartBlood; //"Post-mortem heart blood";
                    break;
                case "4":
                    labResultVM.SampleType = DataHelperBase.SampleTypeSkin;//"Skin biopsy";
                    break;
                case "5":
                    labResultVM.SampleType = DataHelperBase.SampleTypeOther;//"Other";
                    break;
            }

            labResultVM.UVRIVSPBLogNumber = row["UGSPBLog"].ToString();

            labResultVM.SUDVPCR = GetPCRResult(row["SUDVNPPCR"].ToString());
            labResultVM.SUDVPCR2 = GetPCRResult(row["SUDVPCR2"].ToString());
            labResultVM.SUDVAg = GetPCRResult(row["SUDVAg"].ToString());
            labResultVM.SUDVIgM = GetPCRResult(row["SUDVIgM"].ToString());
            labResultVM.SUDVIgG = GetPCRResult(row["SUDVIgG"].ToString());

            labResultVM.BDBVPCR = GetPCRResult(row["BDBVNPPCR"].ToString());
            labResultVM.BDBVPCR2 = GetPCRResult(row["BDBVVP40PCR"].ToString());
            labResultVM.BDBVAg = GetPCRResult(row["BDBVAg"].ToString());
            labResultVM.BDBVIgM = GetPCRResult(row["BDBVIgM"].ToString());
            labResultVM.BDBVIgG = GetPCRResult(row["BDBVIgG"].ToString());

            labResultVM.EBOVPCR = GetPCRResult(row["EBOVPCR1"].ToString());
            labResultVM.EBOVPCR2 = GetPCRResult(row["EBOVPCR2"].ToString());
            labResultVM.EBOVAg = GetPCRResult(row["EBOVAg"].ToString());
            labResultVM.EBOVIgM = GetPCRResult(row["EBOVIgM"].ToString());
            labResultVM.EBOVIgG = GetPCRResult(row["EBOVIgG"].ToString());

            labResultVM.MARVPCR = GetPCRResult(row["MARVPolPCR"].ToString());
            labResultVM.MARVPCR2 = GetPCRResult(row["MARVVP40PCR"].ToString());
            labResultVM.MARVAg = GetPCRResult(row["MARVAg"].ToString());
            labResultVM.MARVIgM = GetPCRResult(row["MARVIgM"].ToString());
            labResultVM.MARVIgG = GetPCRResult(row["MARVIgG"].ToString());

            labResultVM.CCHFPCR = GetPCRResult(row["CCHFPCR1"].ToString());
            labResultVM.CCHFPCR2 = GetPCRResult(row["CCHFPCR2"].ToString());
            labResultVM.CCHFAg = GetPCRResult(row["CCHFAg"].ToString());
            labResultVM.CCHFIgM = GetPCRResult(row["CCHFIgM"].ToString());
            labResultVM.CCHFIgG = GetPCRResult(row["CCHFIgG"].ToString());

            labResultVM.RVFPCR = GetPCRResult(row["RVFPCR1"].ToString());
            labResultVM.RVFPCR2 = GetPCRResult(row["RVFPCR2"].ToString());
            labResultVM.RVFAg = GetPCRResult(row["RVFAg"].ToString());
            labResultVM.RVFIgM = GetPCRResult(row["RVFIgM"].ToString());
            labResultVM.RVFIgG = GetPCRResult(row["RVFIgG"].ToString());

            labResultVM.LHFPCR = GetPCRResult(row["LASPCR1"].ToString());
            labResultVM.LHFPCR2 = GetPCRResult(row["LASPCR2"].ToString());
            labResultVM.LHFAg = GetPCRResult(row["LASAg"].ToString());
            labResultVM.LHFIgM = GetPCRResult(row["LASIgM"].ToString());
            labResultVM.LHFIgG = GetPCRResult(row["LASIgG"].ToString());

            if (caseView != null)
            {
                caseView.RowFilter = "[GlobalRecordId] = '" + row["FKEY"].ToString() + "'";

                foreach (DataRowView caseRowView in caseView)
                {
                    labResultVM.Surname = caseRowView["Surname"].ToString();
                    labResultVM.OtherNames = caseRowView["OtherNames"].ToString();
                    labResultVM.Village = caseRowView["VillageRes"].ToString();
                    labResultVM.District = caseRowView["DistrictRes"].ToString();

                    if (caseRowView["Age"] != DBNull.Value)
                    {
                        labResultVM.Age = Convert.ToDouble(caseRowView["Age"]);
                    }

                    string genderCode = caseRowView["Gender"].ToString();
                    switch (genderCode)
                    {
                        case "1":
                            labResultVM.Gender = "Male";
                            break;
                        case "2":
                            labResultVM.Gender = "Female";
                            break;
                        default:
                            labResultVM.Gender = String.Empty;
                            break;
                    }

                    labResultVM.FinalLabClassification = ConvertFinalLabClassificationCode(caseRowView["FinalLabClass"].ToString());
                    labResultVM.UniqueKey = Int32.Parse(caseRowView["UniqueKey"].ToString());
                    labResultVM.CaseID = caseRowView["ID"].ToString();
                    labResultVM.CaseRecordGuid = caseRowView["GlobalRecordId"].ToString();
                    if (!String.IsNullOrEmpty(caseRowView["DateOnset"].ToString()))
                    {
                        labResultVM.DateOnset = DateTime.Parse(caseRowView["DateOnset"].ToString());
                    }

                    if (!String.IsNullOrEmpty(caseRowView["DateDeath"].ToString()))
                    {
                        labResultVM.DateDeath = DateTime.Parse(caseRowView["DateDeath"].ToString());
                    }
                    break;
                }
            }
            if (LabResultViewModel.IsCountryUS)
            {
                labResultVM.LabSampleTest = row["LabSampleTest"].ToString();
                labResultVM.FacilityLabSubmit = row["FacilityLabSubmit"].ToString();
                labResultVM.PersonLabSubmit = row["PersonLabSubmit"].ToString();
                labResultVM.PhoneLabSubmit = row["PhoneLabSubmit"].ToString();
                labResultVM.EmailLabSubmit = row["EmailLabSubmit"].ToString();
            }
        }

        public void FillInOutbreakData(string outbreakName, string idPrefix, string idSeparator, DateTime? outbreakDate, string pattern, string virus, string country, bool isShortForm, int contactType = 1) //17040
        {
            Query updateQuery = Database.CreateQuery("UPDATE metaDbInfo SET OutbreakName = @OutbreakName, OutbreakDate = @OutbreakDate, IDPrefix = @IDPrefix, IDSeparator = @IDSeparator, IDPattern = @IDPattern, Virus = @Virus, PrimaryCountry = @PrimaryCountry, IsShortForm = @IsShortForm, ContactFormType = @ContactFormType");
            updateQuery.Parameters.Add(new QueryParameter("@OutbreakName", DbType.String, outbreakName));
            updateQuery.Parameters.Add(new QueryParameter("@OutbreakDate", DbType.DateTime, outbreakDate));
            updateQuery.Parameters.Add(new QueryParameter("@IDPrefix", DbType.String, idPrefix));
            updateQuery.Parameters.Add(new QueryParameter("@IDSeparator", DbType.String, idSeparator));
            updateQuery.Parameters.Add(new QueryParameter("@IDPattern", DbType.String, pattern));
            updateQuery.Parameters.Add(new QueryParameter("@Virus", DbType.String, virus));
            updateQuery.Parameters.Add(new QueryParameter("@PrimaryCountry", DbType.String, country));
            updateQuery.Parameters.Add(new QueryParameter("@IsShortForm", DbType.Boolean, isShortForm)); //17040
            updateQuery.Parameters.Add(new QueryParameter("@ContactFormType", DbType.Int16, contactType)); 

            switch (virus)
            {
                case "Sudan":
                    VirusTestType = Core.Enums.VirusTestTypes.Sudan;
                    break;
                case "Ebola":
                    VirusTestType = Core.Enums.VirusTestTypes.Ebola;
                    break;
                case "Marburg":
                    VirusTestType = Core.Enums.VirusTestTypes.Marburg;
                    break;
                case "Bundibugyo":
                    VirusTestType = Core.Enums.VirusTestTypes.Bundibugyo;
                    break;
                case "CCHF":
                    VirusTestType = Core.Enums.VirusTestTypes.CCHF;
                    Common.DaysInWindow = 14;
                    break;
                case "Rift":
                    VirusTestType = Core.Enums.VirusTestTypes.Rift;
                    break;
                case "Lassa":
                    VirusTestType = Core.Enums.VirusTestTypes.Lassa;
                    break;
            }

            if (VirusTestType.Equals(Core.Enums.VirusTestTypes.CCHF))
            {
                Core.Common.DaysInWindow = 14;
            }

            Database.ExecuteNonQuery(updateQuery);
        }

        protected virtual void RunInitialSetup(bool showSetupScreen)
        {
            //if (InitialSetupRun != null)
            //{
            //    InitialSetupRun(this, new EventArgs());
            //}
        }

        protected virtual void SortCases() { }

        protected virtual DataTable GetCasesTable()
        {
            DataTable casesTable = new DataTable("casesTable");
            casesTable.CaseSensitive = true;
            casesTable = ContactTracing.Core.Common.JoinPageTables(Database, CaseForm);
            return casesTable;
        }

        protected virtual DataTable GetLabTable()
        {
            Query selectQuery = Database.CreateQuery("SELECT * " +
                LabForm.FromViewSQL);
            return Database.Select(selectQuery);
        }

        public virtual bool ExportCases(string fileName)
        {
            DataTable casesTable = new DataTable("casesTable");
            casesTable.CaseSensitive = true;
            casesTable = ContactTracing.Core.Common.JoinPageTables(Database, CaseForm);

            bool exportResult = ExportView(casesTable.DefaultView, fileName);

            return exportResult;
        }

        protected bool LoadConfig()
        {
            string configFilePath = Configuration.DefaultConfigurationPath;
            bool configurationOk = true;
            try
            {
                string directoryName = System.IO.Path.GetDirectoryName(configFilePath);
                if (!Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }

                FileInfo fi = new FileInfo(configFilePath);

                if (fi.Exists == false || fi.Length == 0)
                {
                    Configuration defaultConfig = Configuration.CreateDefaultConfiguration();
                    Configuration.Save(defaultConfig);
                }

                Configuration.Load(configFilePath);
            }
            catch (Epi.ConfigurationException)
            {
            }
            catch (Exception ex)
            {
                configurationOk = String.IsNullOrEmpty(ex.Message);
            }

            return configurationOk;
        }

        /// <summary>
        /// Initiates an export of the data to the specified file.
        /// </summary>
        protected bool ExportView(DataView dv, string fileName, bool usePromptText = false, bool includeGlobalRecordId = false)
        {
            SyncStatus = String.Format("Writing header information...");

            DataTable table = dv.Table;//dv.ToTable(false);

            if (includeGlobalRecordId == false)
            {
                if (table.Columns.Contains("GlobalRecordId")) table.Columns.Remove("GlobalRecordId");
            }
            if (table.Columns.Contains("RECSTATUS")) table.Columns.Remove("RECSTATUS");
            if (table.Columns.Contains("UniqueKey")) table.Columns.Remove("UniqueKey");
            if (table.Columns.Contains("ID1")) table.Columns.Remove("ID1");
            if (table.Columns.Contains("ID2")) table.Columns.Remove("ID2");

            WordBuilder wb = new WordBuilder(",");
            StreamWriter sw = null;

            try
            {
                sw = new StreamWriter(fileName, false, Encoding.Unicode);

                sw.WriteLine("sep=,");

                foreach (DataColumn dc in table.Columns)
                {                     

                     string csvColumnName = dc.ColumnName;

                    if (usePromptText)
                    {
                        IDataField field = null;
                        if (CaseForm.Fields.DataFields.Contains(dc.ColumnName))
                        {
                            field = CaseForm.Fields[dc.ColumnName] as IDataField;
                        }
                        else if (LabForm.Fields.DataFields.Contains(dc.ColumnName))
                        {
                            field = CaseForm.Fields[dc.ColumnName] as IDataField;
                        }

                        if (field != null)
                        {
                                                     
                            csvColumnName = "\"" + field.PromptText.Replace("\n", String.Empty).Replace("\r", String.Empty).Replace("\"", "\"\"") + "\"";

                            if (csvColumnName.Trim().EndsWith(":"))
                            {
                                csvColumnName = csvColumnName.Substring(0, csvColumnName.Length - 1);
                            }
                        }
                    }

                    wb.Add(csvColumnName);
                }

                sw.WriteLine(wb.ToString());
                int rowsExported = 0;
                int totalRows = 0;

                //if (useTabOrder || !exportAllFields)
                //{
                totalRows = table.Rows.Count;
                foreach (DataRow row in table.Rows)
                {
                    wb = new WordBuilder(",");
                    for (int i = 0; i < table.Columns.Count; i++)
                    {
                        string rowValue = row[i].ToString().Replace("\r\n", " ").Replace("\n", " ");
                        if (rowValue.Contains(",") || rowValue.Contains(",") || rowValue.Contains("\"") || rowValue.Contains(" "))
                        {
                            rowValue = rowValue.Replace("\"", "\"\"");
                            rowValue = Util.InsertIn(rowValue, "\"");
                        }
                        if(CaseViewModel.IsCountryUS)
                        {
                            if (table.Columns[i].DataType.ToString() == "System.DateTime" && !string.IsNullOrEmpty( rowValue))
                            {
                              rowValue=  DateTime.Parse(row[i].ToString()).ToShortDateString();
                            }
                        }
                        wb.Add(rowValue);
                    }
                    sw.WriteLine(wb);
                    rowsExported++;
                    if (rowsExported % 50 == 0)
                    {
                        SyncStatus = String.Format("{0} rows exported.", rowsExported);
                        //this.Dispatcher.BeginInvoke(new SetGadgetStatusHandler(RequestUpdateStatusMessage), string.Format(SharedStrings.DASHBOARD_EXPORT_PROGRESS, rowsExported.ToString(), totalRows.ToString()), (double)rowsExported);
                        //RequestUpdateStatusMessage(string.Format(SharedStrings.DASHBOARD_EXPORT_PROGRESS, rowsExported.ToString(), totalRows.ToString()), (double)rowsExported);
                        //SetProgressAndStatus(string.Format(SharedStrings.DASHBOARD_EXPORT_PROGRESS, rowsExported.ToString(), totalRows.ToString()), (double)rowsExported);
                        //OnSetStatusMessageAndProgressCount(string.Format(SharedStrings.DASHBOARD_EXPORT_PROGRESS, rowsExported.ToString(), totalRows.ToString()), (double)rowsExported);
                    }
                }

                //this.Dispatcher.BeginInvoke(new SetStatusDelegate(SetStatusMessage), string.Format(SharedStrings.DASHBOARD_EXPORT_SUCCESS, rowsExported.ToString()));                
                //OnSetStatusMessage(string.Format(SharedStrings.DASHBOARD_EXPORT_SUCCESS, rowsExported.ToString()));
            }

            catch (Exception)
            {
                //OnSetStatusMessage(ex.Message);
                //this.Dispatcher.BeginInvoke(new SetStatusDelegate(SetErrorMessage), ex.Message);
                return false;
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                    sw = null;
                }
            }
            return true;
        }
    }
}
