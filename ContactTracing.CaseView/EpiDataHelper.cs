using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Timers;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using Epi;
using Epi.Data;
using Epi.Fields;
using ContactTracing.Core;
using ContactTracing.Core.Enums;
using ContactTracing.Core.Data;
using ContactTracing.ViewModel;
using ContactTracing.ViewModel.Collections;
using ContactTracing.ViewModel.Events;
using NPOI.HSSF.UserModel;
using System.IO;
using System.Text;
using ContactTracing.CaseView.Converters;

// TODO: Class needs a total and complete re-write to better adhere to MVVM, to re-design data model, and for other purposes
// Note Oct 6 2014 - this refactoring to MVVM is finally underway as of a few days ago - still much more to do

// Big things: 10.6.14
//  - Searching was implemented with the idea we'd be looking at a typical Ebola outbreak, e.g. 200-300 cases. With 2014 West Africa we're instead looking at 2000+ cases per site, perhaps more. Search implementation needs to change dramatically.
//  - Way too much SQL code, all of this should be moved to VhfProject.cs in Core
//  - Probably no need for ViewModel csproj
//  - Lot of update/add Commands were meant in the context of updating stuff in-memory. However, this naming is confusing
//  - Contact tracing is a total mess due to early design phase which I've never been able to refactor/undo because of time constraints

namespace ContactTracing.CaseView
{
    /// <summary>
    /// Data helper class for the Epidemiology (Case Management) menu app; currently acts as the view model
    /// </summary>
    public partial class EpiDataHelper : DataHelperBase
    {
        #region Members
        private ExportSyncFileViewModel _exportViewModel;
        private SqlToMdbCopierViewModel _sqlToMdbCopierViewModel;
        private ObservableCollection<CaseContactPairViewModel> _currentContactLinkCollection = new ObservableCollection<CaseContactPairViewModel>();
        private ObservableCollection<CaseContactPairViewModel> _currentCaseLinkCollection = new ObservableCollection<CaseContactPairViewModel>();
        private ObservableCollection<CaseExposurePairViewModel> _currentExposureCollection = new ObservableCollection<CaseExposurePairViewModel>();
        private ObservableCollection<CaseExposurePairViewModel> _currentSourceCaseCollection = new ObservableCollection<CaseExposurePairViewModel>();
        private ObservableCollection<DailyCheckViewModel> _prevFollowUpCollection = new ObservableCollection<DailyCheckViewModel>();
        private ObservableCollection<XYColumnChartData> _epiCurveDataPointCollectionCP = new ObservableCollection<XYColumnChartData>();
        private ObservableCollection<XYColumnChartData> _epiCurveDataPointCollectionCPS = new ObservableCollection<XYColumnChartData>();
        private ObservableCollection<XYColumnChartData> _epiCurveDataPointCollectionDeathsCPS = new ObservableCollection<XYColumnChartData>();
        private ObservableCollection<XYColumnChartData> _ageGroupDataPointCollectionCP = new ObservableCollection<XYColumnChartData>();
        private ObservableCollection<Issue> _issueCollection = new ObservableCollection<Issue>();
        private string _searchCasesText = String.Empty;
        private string _searchExistingCasesText = String.Empty;
        private string _searchIsoCasesText = String.Empty;
        private string _searchContactsText = String.Empty;
        private string _searchExistingContactsText = String.Empty;
        private short _pollRate = 0;
        protected internal readonly object _caseCollectionLock = new object();
        protected internal readonly object _contactCollectionLock = new object();
        protected internal readonly object _dailyCollectionLock = new object();
        protected internal readonly object _prevCollectionLock = new object();
        protected internal readonly object _epiCurveCollectionLock = new object();
        protected internal readonly object _sendMessageLock = new object();
        protected internal readonly object _locationCollectionLock = new object();
        protected internal readonly object _contactTeamsCollectionLock = new object();
        protected internal readonly object _facilitiesCollectionLock = new object();
        private bool _isCheckingServerForUpdates = false;
        private bool _isConnected = true;
        private bool _isMultiUser = true;
        private bool _isLoadingProjectData = false;
        private bool _isWaitingOnOtherClients = false;
        private bool _isSendingServerUpdates = false;
        private bool _isDataSyncing = false;
        private bool _isShowingDataExporter = false;
        private string _showingDataExporterText = string.Empty;
        private bool _isShowingCaseReportForm = false;
        private bool _isShowingError = false;
        private bool _isExportingData = false;
        private string _errorMessage = String.Empty;
        private string _errorMessageDetail = String.Empty;
        private System.Data.OleDb.OleDbConnection _oleDbConnection = null;
        
        private Timer _updateTimer = new Timer();
        private bool _isEditingAdminBoundaryFieldTypes = false;

        private string _separator = "-";

        private string _prefix = "";
        // private string _outbreakName = "New Outbreak";
        private string _projectName = "NewProject";

        private RegionEnum _applicationRegion = ApplicationViewModel.Instance.CurrentRegion;                //  Properties.Settings.Default.Region;


        private string _adm1 = Properties.Resources.Adm1Default;
        private string _adm2 = Properties.Resources.Adm2Default;
        private string _adm3 = Properties.Resources.Adm3Default;
        private string _adm4 = Properties.Resources.Adm4Default;


        private List<string> _contactTeams = new List<string>();
        private List<string> _facilities = new List<string>();
        private List<string> _allDistricts = new List<string>();
        private List<string> _allSC = new List<string>();
        private List<string> _allVillages = new List<string>();
        private Dictionary<int, Boundry> _boundryAggregation = new Dictionary<int, Boundry>();
        private Dictionary<int, string> _boundryAtLevel = new Dictionary<int, string>();
        private int _filterSelectedBoundry = -1;
        internal readonly string CurrentUser = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString();
        #endregion // Members

        #region Properties

        /// <summary>
        /// Property created for VHF-260
        /// </summary>
        private CaseViewModel _caseOpenedInShortForm = null;

        public CaseViewModel CaseOpenedInShortForm
        {
            get { return _caseOpenedInShortForm; }
            set
            {
                _caseOpenedInShortForm = value;
                RaisePropertyChanged("CaseOpenedInShortForm");
            }
        }


        public SqlToMdbCopierViewModel SqlToMdbCopierViewModel
        {
            get
            {
                return _sqlToMdbCopierViewModel;
            }
            set
            {
                _sqlToMdbCopierViewModel = value;
                RaisePropertyChanged("SqlToMdbCopierViewModel");
            }
        }

        public ExportSyncFileViewModel ExportSyncFileViewModel
        {
            get
            {
                return _exportViewModel;
            }
            set
            {
                _exportViewModel = value;
                RaisePropertyChanged("ExportSyncFileViewModel");
            }
        }

        public string Separator
        {
            get { return _separator; }
            set { _separator = value; }
        }


        public RegionEnum ApplicationRegion
        {
            get { return _applicationRegion; }
            set
            {
                _applicationRegion = value;
                RaisePropertyChanged("ApplicationRegion");
            }
        }

        private string _applicationCulture = ContactTracing.CaseView.Properties.Settings.Default.Culture;

        public string ApplicationCulture
        {
            get { return _applicationCulture; }
            set { _applicationCulture = value; }
        }


        public string Prefix
        {
            get { return _prefix; }
            set { _prefix = value; }
        }

        //public string OutbreakName
        //{
        //    get
        //    {
        //        return _outbreakName;
        //    }
        //    set
        //    {
        //        _outbreakName = value;
        //    }
        //}

        public string ProjectName
        {
            get
            {
                return _projectName;
            }
            set
            {

                _projectName = value;
            }
        }
        public string Adm1
        {
            get
            {
                //17178, 17223
                //if (IsCountryUS)
                //{
                //    return this._adm1 + " (" + Properties.Resources.Residence + ")";
                //}
                return this._adm1;
            }
            set
            {
                if (!Adm1.Equals(value))
                {
                    _adm1 = value;
                    RaisePropertyChanged("Adm1");
                    RaisePropertyChanged("Adm1Onset");

                    if (Database != null)
                    {
                        Query updateQuery = Database.CreateQuery("UPDATE metaDbInfo SET [Adm1] = @Adm1");
                        updateQuery.Parameters.Add(new QueryParameter("@Adm1", DbType.String, _adm1));//State(Residence) issue fix - referring private field instead of Adm1
                        Database.ExecuteNonQuery(updateQuery);

                        UpdateAdministrativeBoundariesAsync();
                    }
                }
            }
        }

        public string Adm1Onset { get { return this._adm1 + " (" + Properties.Resources.Onset + ")"; } }

        public string Adm2
        {
            get { return this._adm2; }
            set
            {
                if (!Adm2.Equals(value))
                {
                    _adm2 = value;
                    RaisePropertyChanged("Adm2");

                    if (Database != null)
                    {
                        Query updateQuery = Database.CreateQuery("UPDATE metaDbInfo SET [Adm2] = @Adm2");
                        updateQuery.Parameters.Add(new QueryParameter("@Adm2", DbType.String, Adm2));
                        Database.ExecuteNonQuery(updateQuery);

                        UpdateAdministrativeBoundariesAsync();
                    }
                }
            }
        }
        public string Adm3
        {
            get { return this._adm3; }
            set
            {
                if (!Adm3.Equals(value))
                {
                    _adm3 = value;
                    RaisePropertyChanged("Adm3");

                    if (Database != null)
                    {
                        Query updateQuery = Database.CreateQuery("UPDATE metaDbInfo SET [Adm3] = @Adm3");
                        updateQuery.Parameters.Add(new QueryParameter("@Adm3", DbType.String, Adm3));
                        Database.ExecuteNonQuery(updateQuery);

                        UpdateAdministrativeBoundariesAsync();
                    }
                }
            }
        }
        public string Adm4
        {
            get { return this._adm4; }
            set
            {
                if (!Adm4.Equals(value))
                {
                    _adm4 = value;
                    RaisePropertyChanged("Adm4");

                    if (Database != null)
                    {
                        Query updateQuery = Database.CreateQuery("UPDATE metaDbInfo SET [Adm4] = @Adm4");
                        updateQuery.Parameters.Add(new QueryParameter("@Adm4", DbType.String, Adm4));
                        Database.ExecuteNonQuery(updateQuery);

                        UpdateAdministrativeBoundariesAsync();
                    }
                }
            }
        }

        public int FilterSelectedBoundry
        {
            get { return _filterSelectedBoundry; }
            set { _filterSelectedBoundry = value; }
        }
        public View ContactForm { get; set; }
        public DataTable LabTable { get; protected internal set; }
        public bool IsSuperUser { get; protected internal set; }

        private bool _isCountryUS;

        public bool IsCountryUS
        {
            get { return _isCountryUS; }
            set
            {
                _isCountryUS = value;
                if (_isCountryUS)
                {
                    ShowColumn = false;
                }
                else
                {
                    ShowColumn = true;
                }
                RaisePropertyChanged("IsCountryUS");
            }
        }

        //Property used to hide columns for US - false will hide columns.
        public bool _ShowColumn;
        public bool ShowColumn { get { return _ShowColumn; } set { _ShowColumn = value; RaisePropertyChanged("ShowColumn"); } }



        public bool IsDataSyncing
        {
            get
            {
                return _isDataSyncing;
            }
            protected internal set
            {
                if (_isDataSyncing != value)
                {
                    _isDataSyncing = value;
                    RaisePropertyChanged("IsDataSyncing");
                }
            }
        }

        public bool IsShowingDataExporter
        {
            get
            {
                return _isShowingDataExporter;
            }
            protected internal set
            {
                if (_isShowingDataExporter != value)
                {
                    _isShowingDataExporter = value;
                    RaisePropertyChanged("IsShowingDataExporter");
                }
            }
        }

        public string ShowingDataExporterText
        {
            get
            {
                return _showingDataExporterText;
            }
            protected internal set
            {
                if (_showingDataExporterText != value)
                {
                    _showingDataExporterText = value;
                    RaisePropertyChanged("ShowingDataExporterText");
                }
            }
        }

        public bool IsShowingCaseReportForm
        {
            get
            {
                return _isShowingCaseReportForm;
            }
            protected internal set
            {
                if (_isShowingCaseReportForm != value)
                {
                    _isShowingCaseReportForm = value;
                    RaisePropertyChanged("IsShowingCaseReportForm");
                }
            }
        }

        public bool IsShowingError
        {
            get
            {
                return _isShowingError;
            }
            protected internal set
            {
                if (_isShowingError != value)
                {
                    _isShowingError = value;
                    RaisePropertyChanged("IsShowingError");
                }
            }
        }

        public bool IsExportingData
        {
            get
            {
                return _isExportingData;
            }
            protected internal set
            {
                if (_isExportingData != value)
                {
                    _isExportingData = value;
                    RaisePropertyChanged("IsExportingData");
                }
            }
        }

        public bool IsEditingAdminBoundaryFieldTypes
        {
            get
            {
                return _isEditingAdminBoundaryFieldTypes;
            }
            private set
            {
                if (IsEditingAdminBoundaryFieldTypes != value)
                {
                    _isEditingAdminBoundaryFieldTypes = value;
                    RaisePropertyChanged("IsEditingAdminBoundaryFieldTypes");
                }
            }
        }

        /// <summary>
        ///VHF-256
        /// Property created to prevent SelectionChangedEvent of ComboBox on ShortCaseEntryForm to incorrectly update data in CaseViewModel.
        /// EventHandler Code doesn't get executed when form is not loaded.
        /// </summary>
        private bool _isShortFormOpened = false;

        public bool IsShortFormOpened
        {
            get { return _isShortFormOpened; }
            set { _isShortFormOpened = value; }
        }

        public ICollectionView CaseCollectionView { get; set; }
        public ICollectionView ContactCollectionView { get; set; }
        public ICollectionView IsolatedCollectionView { get; set; }
        public ICollectionView CasesWithoutContactsCollectionView { get; set; }
        public ICollectionView ExistingCaseCollectionView { get; set; }
        public ICollectionView ExistingContactCollectionView { get; set; }

        public Dictionary<string, List<string>> DistrictsSubCounties { get; set; }
        public ObservableCollection<string> SubCounties { get; set; }
        public ObservableCollection<string> Districts { get; set; }
        public ObservableCollection<string> Villages { get; set; }
        public ObservableCollection<string> Countries { get; set; }
        public ObservableCollection<string> HcwPositions { get; set; }
        public ObservableCollection<string> TransportTypes { get; set; }
        public ObservableCollection<string> HospitalsPast { get; set; }
        public ObservableCollection<string> ContactRelations { get; set; }
        public ObservableCollection<string> FuneralRelations { get; set; }
        public ObservableCollection<string> OtherOccupDetails { get; set; }
        public ObservableCollection<string> BusinessTypes { get; set; }
        public ObservableCollection<string> BleedOtherComments { get; set; }
        public ObservableCollection<string> SymptOtherComments { get; set; }
        public ObservableCollection<string> HcwFacilities { get; set; }
        public ObservableCollection<string> CurrentHospitals { get; set; }
        public ObservableCollection<string> InterviewerPositions { get; set; }
        public ObservableCollection<string> InterviewerHealthFacilities { get; set; }
        public ObservableCollection<string> ProxyRelations { get; set; }
        public ObservableCollection<string> HospitalDischarges { get; set; }
        public ObservableCollection<string> OtherHemoFinalSpecifyCollection { get; set; }

        public ObservableCollection<string> ContactHCWFacilities { get; set; }
        public ObservableCollection<string> ContactTeamCollection { get; set; }

        public string MacAddress { get; set; }
        protected string CaseDataSelectCommand { get; set; }
        protected string ContactDataSelectCommand { get; set; }

        public int CaseFormId { get; protected internal set; } // the ViewId for the case form as represented in the metaViews table in the database
        public int ContactFormId { get; protected internal set; } // the ViewId for the contact form as represented in the metaViews table in the database
        public int LabFormId { get; protected internal set; }
        protected bool RefreshOnTick { get; set; }

        public List<string> Facilities
        {
            get
            {
                if (_facilities == null)
                {
                    _facilities = new List<string>();
                }

                if (_facilities.Count == 0 && ContactForm != null)
                {
                    bool hasfacilityColumn = false;
                    string facilityName;
                    _facilities.Add(string.Empty);

                    foreach (Page page in ContactForm.Pages)
                    {
                        string columnName = "ContactHCWFacility";
                        if (Database != null)
                            hasfacilityColumn = Database.ColumnExists(page.TableName, columnName);

                        if (hasfacilityColumn)
                        {
                            Query selectQuery = Database.CreateQuery("SELECT [" + columnName + "] FROM [" + page.TableName + "]");
                            DataTable facilityTable = Database.Select(selectQuery);

                            List<string> facilityNames = new List<string>();

                            foreach (DataRow row in facilityTable.Rows)
                            {
                                if (row[columnName] != DBNull.Value && row[columnName] is string)
                                {
                                    facilityName = (string)row[columnName];
                                    if (_facilities.Contains(facilityName) == false)
                                    {
                                        _facilities.Add(facilityName);
                                    }
                                }
                            }
                        }
                    }
                }

                return _facilities;
            }
        }

        public List<string> ContactTeams
        {
            get
            {
                if (_contactTeams == null)
                {
                    _contactTeams = new List<string>();
                }

                if (_contactTeams.Count == 0 && ContactForm != null)
                {
                    bool hasTeamColumn = false;
                    string teamName;
                    _contactTeams.Add(string.Empty);

                    foreach (Page page in ContactForm.Pages)
                    {
                        if (Database != null)
                            hasTeamColumn = Database.ColumnExists(page.TableName, "Team");

                        if (hasTeamColumn)
                        {
                            Query selectQuery = Database.CreateQuery("SELECT [Team] FROM [" + page.TableName + "]");
                            DataTable teamTable = Database.Select(selectQuery);

                            List<string> teamNames = new List<string>();

                            foreach (DataRow row in teamTable.Rows)
                            {
                                if (row["Team"] != DBNull.Value && row["Team"] is string)
                                {
                                    teamName = (string)row["Team"];
                                    if (_contactTeams.Contains(teamName) == false)
                                    {
                                        _contactTeams.Add(teamName);
                                    }
                                }
                            }
                        }
                    }
                }

                return _contactTeams;
            }
        }

        public List<string> AllVillages
        {
            get
            {
                if (_allVillages == null)
                {
                    _allVillages = new List<string>();
                }

                if (_allVillages.Count == 0 && ContactForm != null)
                {
                    bool hasTeamColumn;
                    string teamName;
                    _allVillages.Add(string.Empty);

                    foreach (Page page in ContactForm.Pages)
                    {
                        hasTeamColumn = Database.ColumnExists(page.TableName, "ContactVillage");

                        if (hasTeamColumn)
                        {
                            Query selectQuery = Database.CreateQuery("SELECT [ContactVillage] FROM [" + page.TableName + "]");
                            DataTable teamTable = Database.Select(selectQuery);

                            List<string> teamNames = new List<string>();

                            foreach (DataRow row in teamTable.Rows)
                            {
                                if (row["ContactVillage"] != DBNull.Value && row["ContactVillage"] is string)
                                {
                                    teamName = (string)row["ContactVillage"];
                                    if (_allVillages.Contains(teamName) == false)
                                    {
                                        _allVillages.Add(teamName);
                                    }
                                }
                            }
                        }
                    }
                }

                return _allVillages;
            }
        }

        public List<string> AllSubcounties
        {
            get
            {
                if (_allSC == null)
                {
                    _allSC = new List<string>();
                }

                if (_allSC.Count == 0 && ContactForm != null)
                {
                    bool hasTeamColumn;
                    string teamName;
                    _allSC.Add(string.Empty);

                    foreach (Page page in ContactForm.Pages)
                    {
                        hasTeamColumn = Database.ColumnExists(page.TableName, "ContactSC");

                        if (hasTeamColumn)
                        {
                            Query selectQuery = Database.CreateQuery("SELECT [ContactSC] FROM [" + page.TableName + "]");
                            DataTable teamTable = Database.Select(selectQuery);

                            List<string> teamNames = new List<string>();

                            foreach (DataRow row in teamTable.Rows)
                            {
                                if (row["ContactSC"] != DBNull.Value && row["ContactSC"] is string)
                                {
                                    teamName = (string)row["ContactSC"];
                                    if (_allSC.Contains(teamName) == false)
                                    {
                                        _allSC.Add(teamName);
                                    }
                                }
                            }
                        }
                    }
                }

                return _allSC;
            }
        }

        public List<string> AllDistricts
        {
            get
            {
                if (_allDistricts == null)
                {
                    _allDistricts = new List<string>();
                }

                if (_allDistricts.Count == 0 && ContactForm != null)
                {
                    bool hasTeamColumn;
                    string teamName;
                    _allDistricts.Add(string.Empty);

                    foreach (Page page in ContactForm.Pages)
                    {
                        hasTeamColumn = Database.ColumnExists(page.TableName, "ContactDistrict");

                        if (hasTeamColumn)
                        {
                            Query selectQuery = Database.CreateQuery("SELECT [ContactDistrict] FROM [" + page.TableName + "]");
                            DataTable teamTable = Database.Select(selectQuery);

                            List<string> teamNames = new List<string>();

                            foreach (DataRow row in teamTable.Rows)
                            {
                                if (row["ContactDistrict"] != DBNull.Value && row["ContactDistrict"] is string)
                                {
                                    teamName = (string)row["ContactDistrict"];
                                    if (_allDistricts.Contains(teamName) == false)
                                    {
                                        _allDistricts.Add(teamName);
                                    }
                                }
                            }
                        }
                    }
                }

                return _allDistricts;
            }
        }

        public List<string> ColumnValues(string columnName)
        {
            List<string> columnValues = new List<string>();

            if (ContactForm != null)
            {
                bool hasColumn;

                foreach (Page page in ContactForm.Pages)
                {
                    hasColumn = Database.ColumnExists(page.TableName, columnName);

                    if (hasColumn)
                    {
                        Query selectQuery = Database.CreateQuery("SELECT DISTINCT [" + columnName + "] FROM [" + page.TableName + "] ORDER BY " + columnName);
                        DataTable teamTable = Database.Select(selectQuery);
                        columnValues = teamTable.Rows.OfType<DataRow>().Select(dr => dr.Field<string>(columnName)).ToList();
                    }
                }
            }

            return columnValues;
        }

        public Dictionary<int, Boundry> BoundaryAggregation
        {
            get
            {
                if (_boundryAggregation == null)
                {
                    _boundryAggregation = new Dictionary<int, Boundry>();
                }

                if (Country == "USA")
                {
                    if (_boundryAggregation.Count == 0)
                    {
                        _boundryAggregation.Add(3, new Boundry(_adm1, "ContactDistrict", "ContactVM.District", caseObjectResolution: "District"));//STATE
                        _boundryAggregation.Add(2, new Boundry(_adm2, "ContactSC", "ContactVM.SubCounty", caseObjectResolution: "SubCounty"));//COUNTY
                        _boundryAggregation.Add(0, new Boundry(_adm4, "ContactVillage", "ContactVM.Village", caseObjectResolution: "Village"));//CITY
                        _boundryAggregation.Add(-1, new Boundry("Zip Code", "ContactZipCode", "ContactVM.ContactZip", caseObjectResolution: "ZipRes"));
                        _boundryAggregation.Add(-2, new Boundry("Address", "ContactAddress", "ContactVM.ContactAddress", caseObjectResolution: "AddressRes"));
                    }
                }
                else
                {
                    if (_boundryAggregation.Count == 0)
                    {
                        _boundryAggregation.Add(3, new Boundry(_adm1, "ContactDistrict", "ContactVM.District"));
                        _boundryAggregation.Add(2, new Boundry(_adm2, "ContactSC", "ContactVM.SubCounty"));
                        _boundryAggregation.Add(0, new Boundry(_adm4, "ContactVillage", "ContactVM.Village"));
                    }
                }




                return _boundryAggregation;
            }
        }

        public List<string> BoundriesAtLevel
        {
            get
            {

                if (FilterSelectedBoundry == -1)
                {
                    return new List<string>();
                }
                else if (FilterSelectedBoundry == 0)
                {
                    return AllVillages;
                }
                else if (FilterSelectedBoundry == 2)
                {
                    return AllSubcounties;
                }
                else if (FilterSelectedBoundry == 3)
                {
                    return AllDistricts;
                }
                else
                {
                    return new List<string>();
                }
            }
        }

        public string ErrorMessage
        {
            get
            {
                return this._errorMessage;
            }
            set
            {
                if (_errorMessage != value)
                {
                    this._errorMessage = value;
                    RaisePropertyChanged("ErrorMessage");
                }
            }
        }

        public string ErrorMessageDetail
        {
            get
            {
                return this._errorMessageDetail;
            }
            set
            {
                this._errorMessageDetail = value;
                RaisePropertyChanged("ErrorMessageDetail");
            }
        }

        public bool IsLoadingProjectData
        {
            get
            {
                return _isLoadingProjectData;
            }
            protected internal set
            {
                if (IsLoadingProjectData != value)
                {
                    _isLoadingProjectData = value;
                    RaisePropertyChanged("IsLoadingProjectData");
                }
            }
        }


        public bool IsSendingServerUpdates
        {
            get
            {
                return _isSendingServerUpdates;
            }
            protected internal set
            {
                if (IsSendingServerUpdates != value)
                {
                    _isSendingServerUpdates = value;
                    RaisePropertyChanged("IsSendingServerUpdates");
                }
            }
        }
        public bool IsCheckingServerForUpdates
        {
            get
            {
                return _isCheckingServerForUpdates;
            }
            protected internal set
            {
                if (IsCheckingServerForUpdates != value)
                {
                    _isCheckingServerForUpdates = value;
                    RaisePropertyChanged("IsCheckingServerForUpdates");
                }
            }
        }

        /// <summary>
        /// Gets/sets whether the data connection model supports multiple users connecting to the database simultaneously
        /// </summary>
        public bool IsMultiUser
        {
            get
            {
                return _isMultiUser;
            }
            protected internal set
            {
                if (IsMultiUser != value)
                {
                    _isMultiUser = value;
                    RaisePropertyChanged("IsMultiUser");
                }
            }
        }
        
        public bool IsConnected
        {
            get
            {
                return _isConnected;
            }
            protected internal set
            {
                if (IsConnected != value)
                {
                    if (!IsConnected)
                    {
                        PollRate = ContactTracing.Core.Constants.DISCONNECT_POLL_RATE;
                    }
                    else
                    {
                        PollRate = ContactTracing.Core.Constants.DEFAULT_POLL_RATE;
                    }

                    _isConnected = value;
                    RaisePropertyChanged("IsConnected");
                }
            }
        }
        public bool IsWaitingOnOtherClients
        {
            get
            {
                return _isWaitingOnOtherClients;
            }
            protected internal set
            {
                if (IsWaitingOnOtherClients != value)
                {
                    _isWaitingOnOtherClients = value;
                    RaisePropertyChanged("IsWaitingOnOtherClients");
                }
            }
        }

        protected List<Issue> IssueList { get; set; }

        /// <summary>
        /// Gets/sets the timer object that handles server polling for updates
        /// </summary>
        public Timer UpdateTimer
        {
            get
            {
                return this._updateTimer;
            }
            protected internal set
            {
                this._updateTimer = value;
            }
        }

        /// <summary>
        /// Gets/sets the rate at which the client application checks the server for updates (milliseconds)
        /// </summary>
        protected short PollRate
        {
            get
            {
                return this._pollRate;
            }
            set
            {
                this._pollRate = value;
                if (this.UpdateTimer != null)
                {
                    if (PollRate == 0)
                    {
                        this.UpdateTimer.Stop();
                    }
                    else
                    {
                        this.UpdateTimer.Interval = PollRate;
                        this.UpdateTimer.Start();
                    }
                }
            }
        }

        public string SearchContactsText
        {
            get
            {
                return this._searchContactsText;
            }
            set
            {
                if (this._searchContactsText != value)
                {
                    this._searchContactsText = value;
                    RaisePropertyChanged("SearchContactsText");
                    SearchContacts.Execute(SearchContactsText);
                }
            }
        }
        public string SearchExistingContactsText
        {
            get
            {
                return this._searchExistingContactsText;
            }
            set
            {
                if (this._searchExistingContactsText != value)
                {
                    this._searchExistingContactsText = value;
                    RaisePropertyChanged("SearchExistingContactsText");
                    SearchExistingContacts.Execute(SearchExistingContactsText);
                }
            }
        }
        public string SearchIsoCasesText
        {
            get
            {
                return this._searchIsoCasesText;
            }
            set
            {
                if (this._searchIsoCasesText != value)
                {
                    this._searchIsoCasesText = value;
                    RaisePropertyChanged("SearchIsoCasesText");
                    SearchIsoCases.Execute(SearchIsoCasesText);
                }
            }
        }
        public string SearchCasesText
        {
            get
            {
                return this._searchCasesText;
            }
            set
            {
                if (this._searchCasesText != value)
                {
                    this._searchCasesText = value;
                    RaisePropertyChanged("SearchCasesText");
                    SearchCases.Execute(SearchCasesText);
                }
            }
        }
        public string SearchExistingCasesText
        {
            get
            {
                return this._searchExistingCasesText;
            }
            set
            {
                if (this._searchExistingCasesText != value)
                {
                    this._searchExistingCasesText = value;
                    RaisePropertyChanged("SearchExistingCasesText");
                    SearchExistingCases.Execute(SearchExistingCasesText);
                }
            }
        }

        public ObservableCollection<Issue> IssueCollection
        {
            get
            {
                return this._issueCollection;
            }
            protected internal set
            {
                if (this._issueCollection != value)
                {
                    this._issueCollection = value;
                    RaisePropertyChanged("IssueCollection");
                }
            }
        }

        public ObservableCollection<XYColumnChartData> EpiCurveDataPointCollectionCP
        {
            get
            {
                return this._epiCurveDataPointCollectionCP;
            }
            protected internal set
            {
                if (this._epiCurveDataPointCollectionCP != value)
                {
                    this._epiCurveDataPointCollectionCP = value;
                    RaisePropertyChanged("EpiCurveDataPointCollectionCP");
                }
            }
        }
        public ObservableCollection<XYColumnChartData> AgeGroupDataPointCollectionCP
        {
            get
            {
                return this._ageGroupDataPointCollectionCP;
            }
            protected internal set
            {
                if (this._ageGroupDataPointCollectionCP != value)
                {
                    this._ageGroupDataPointCollectionCP = value;
                    RaisePropertyChanged("AgeGroupDataPointCollectionCP");
                }
            }
        }
        public ObservableCollection<XYColumnChartData> EpiCurveDataPointCollectionCPS
        {
            get
            {
                return this._epiCurveDataPointCollectionCPS;
            }
            protected internal set
            {
                if (this._epiCurveDataPointCollectionCPS != value)
                {
                    this._epiCurveDataPointCollectionCPS = value;
                    RaisePropertyChanged("EpiCurveDataPointCollectionCPS");
                }
            }
        }
        public ObservableCollection<XYColumnChartData> EpiCurveDataPointCollectionDeathsCPS
        {
            get
            {
                return this._epiCurveDataPointCollectionDeathsCPS;
            }
            protected internal set
            {
                if (this._epiCurveDataPointCollectionDeathsCPS != value)
                {
                    this._epiCurveDataPointCollectionDeathsCPS = value;
                    RaisePropertyChanged("EpiCurveDataPointCollectionDeathsCPS");
                }
            }
        }

        public CaseCollectionMaster CaseCollection { get; set; }
        public ContactCollectionMaster ContactCollection { get; set; }
        public ContactLinkCollectionMaster ContactLinkCollection { get; set; }
        public DailyFollowUpCollectionMaster DailyFollowUpCollection { get; set; }
        public DataTable metaLinksDataTable { get; set; }

        public ObservableCollection<CaseContactPairViewModel> CurrentContactLinkCollection
        {
            get
            {
                return this._currentContactLinkCollection;
            }
            protected internal set
            {
                if (this._currentContactLinkCollection != value)
                {
                    this._currentContactLinkCollection = value;
                    RaisePropertyChanged("CurrentContactLinkCollection");
                }
            }
        }
        public ObservableCollection<CaseContactPairViewModel> CurrentCaseLinkCollection
        {
            get
            {
                return this._currentCaseLinkCollection;
            }
            protected internal set
            {
                if (this._currentCaseLinkCollection != value)
                {
                    this._currentCaseLinkCollection = value;
                    RaisePropertyChanged("CurrentCaseLinkCollection");
                }
            }
        }
        public ObservableCollection<CaseExposurePairViewModel> CurrentExposureCollection
        {
            get
            {
                return this._currentExposureCollection;
            }
            protected internal set
            {
                if (this._currentExposureCollection != value)
                {
                    this._currentExposureCollection = value;
                    RaisePropertyChanged("CurrentExposureCollection");
                }
            }
        }
        public ObservableCollection<CaseExposurePairViewModel> CurrentSourceCaseCollection
        {
            get
            {
                return this._currentSourceCaseCollection;
            }
            protected internal set
            {
                if (this._currentSourceCaseCollection != value)
                {
                    this._currentSourceCaseCollection = value;
                    RaisePropertyChanged("CurrentSourceCaseCollection");
                }
            }
        }
        public ObservableCollection<DailyCheckViewModel> PrevFollowUpCollection
        {
            get
            {
                return this._prevFollowUpCollection;
            }
            protected internal set
            {
                if (this._prevFollowUpCollection != value)
                {
                    this._prevFollowUpCollection = value;
                    RaisePropertyChanged("PrevFollowUpCollection");
                }
            }
        }
        #endregion // Properties

        #region Constructors
        public EpiDataHelper()
        {

            ApplicationViewModel.RegionChanged += ApplicationViewModel_RegionChanged;
            LoadConfig();
            TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
            CaseFormId = -1;
            ContactFormId = -1;
            LabFormId = -1;
            RefreshOnTick = false;
            IssueList = new List<Issue>();
            IsDataSyncing = false;
            IsConnected = true;
            IsShowingError = false;

            CaseCollection = new CaseCollectionMaster();
            ContactCollection = new ContactCollectionMaster();
            ContactLinkCollection = new ContactLinkCollectionMaster();
            DailyFollowUpCollection = new DailyFollowUpCollectionMaster();
            EpiCurveDataPointCollectionCP = new ObservableCollection<XYColumnChartData>();
            EpiCurveDataPointCollectionCPS = new ObservableCollection<XYColumnChartData>();
            EpiCurveDataPointCollectionDeathsCPS = new ObservableCollection<XYColumnChartData>();
            Districts = new ObservableCollection<string>();
            Countries = new ObservableCollection<string>();
            HcwPositions = new ObservableCollection<string>();
            TransportTypes = new ObservableCollection<string>();
            HospitalsPast = new ObservableCollection<string>();
            ContactRelations = new ObservableCollection<string>();
            FuneralRelations = new ObservableCollection<string>();
            OtherOccupDetails = new ObservableCollection<string>();
            BusinessTypes = new ObservableCollection<string>();
            BleedOtherComments = new ObservableCollection<string>();
            SymptOtherComments = new ObservableCollection<string>();
            HcwFacilities = new ObservableCollection<string>();
            CurrentHospitals = new ObservableCollection<string>();
            InterviewerPositions = new ObservableCollection<string>();
            InterviewerHealthFacilities = new ObservableCollection<string>();
            ProxyRelations = new ObservableCollection<string>();
            HospitalDischarges = new ObservableCollection<string>();
            OtherHemoFinalSpecifyCollection = new ObservableCollection<string>();
            SubCounties = new ObservableCollection<string>();
            DistrictsSubCounties = new Dictionary<string, List<string>>();
            ContactHCWFacilities = new ObservableCollection<string>();
            ContactTeamCollection = new ObservableCollection<string>();

            CaseCollection.CaseAdded += CaseCollection_CaseAdded;
            CaseCollection.CaseUpdated += CaseCollection_CaseUpdated;
            CaseCollection.CaseViewerClosed += CaseCollection_CaseViewerClosed;
            CaseCollection.CaseSwitchToLegacyEnter += CaseCollection_CaseSwitchToLegacyEnter;

            //BindingOperations.CollectionRegistering += BindingOperations_CollectionRegistering;

            BindingOperations.EnableCollectionSynchronization(CaseCollection, _caseCollectionLock);
            BindingOperations.EnableCollectionSynchronization(ContactCollection, _contactCollectionLock);
            BindingOperations.EnableCollectionSynchronization(DailyFollowUpCollection, _dailyCollectionLock);
            BindingOperations.EnableCollectionSynchronization(PrevFollowUpCollection, _prevCollectionLock);

            BindingOperations.EnableCollectionSynchronization(CurrentContactLinkCollection, _contactCollectionLock);
            BindingOperations.EnableCollectionSynchronization(CurrentCaseLinkCollection, _caseCollectionLock);
            BindingOperations.EnableCollectionSynchronization(ContactLinkCollection, _contactCollectionLock);
            BindingOperations.EnableCollectionSynchronization(CurrentSourceCaseCollection, _caseCollectionLock);

            BindingOperations.EnableCollectionSynchronization(EpiCurveDataPointCollectionCP, _epiCurveCollectionLock);
            BindingOperations.EnableCollectionSynchronization(EpiCurveDataPointCollectionCPS, _epiCurveCollectionLock);
            BindingOperations.EnableCollectionSynchronization(EpiCurveDataPointCollectionDeathsCPS, _epiCurveCollectionLock);

            BindingOperations.EnableCollectionSynchronization(LabResultCollection, _caseCollectionLock);

            BindingOperations.EnableCollectionSynchronization(Countries, _locationCollectionLock);
            BindingOperations.EnableCollectionSynchronization(HcwPositions, _caseCollectionLock);
            BindingOperations.EnableCollectionSynchronization(TransportTypes, _caseCollectionLock);
            BindingOperations.EnableCollectionSynchronization(HospitalsPast, _caseCollectionLock);
            BindingOperations.EnableCollectionSynchronization(ContactRelations, _caseCollectionLock);
            BindingOperations.EnableCollectionSynchronization(FuneralRelations, _caseCollectionLock);
            BindingOperations.EnableCollectionSynchronization(OtherOccupDetails, _caseCollectionLock);
            BindingOperations.EnableCollectionSynchronization(BusinessTypes, _caseCollectionLock);
            BindingOperations.EnableCollectionSynchronization(BleedOtherComments, _caseCollectionLock);
            BindingOperations.EnableCollectionSynchronization(SymptOtherComments, _caseCollectionLock);
            BindingOperations.EnableCollectionSynchronization(HcwFacilities, _caseCollectionLock);
            BindingOperations.EnableCollectionSynchronization(CurrentHospitals, _caseCollectionLock);
            BindingOperations.EnableCollectionSynchronization(InterviewerPositions, _caseCollectionLock);
            BindingOperations.EnableCollectionSynchronization(InterviewerHealthFacilities, _caseCollectionLock);
            BindingOperations.EnableCollectionSynchronization(ProxyRelations, _caseCollectionLock);
            BindingOperations.EnableCollectionSynchronization(OtherHemoFinalSpecifyCollection, _locationCollectionLock);
            BindingOperations.EnableCollectionSynchronization(HospitalDischarges, _locationCollectionLock);
            BindingOperations.EnableCollectionSynchronization(Districts, _locationCollectionLock);
            BindingOperations.EnableCollectionSynchronization(SubCounties, _locationCollectionLock);
            BindingOperations.EnableCollectionSynchronization(DistrictsSubCounties, _locationCollectionLock);

            BindingOperations.EnableCollectionSynchronization(ContactHCWFacilities, _contactTeamsCollectionLock);
            BindingOperations.EnableCollectionSynchronization(ContactTeamCollection, _contactTeamsCollectionLock);
            BindingOperations.EnableCollectionSynchronization(ContactTeams, _contactTeamsCollectionLock);
            BindingOperations.EnableCollectionSynchronization(Facilities, _facilitiesCollectionLock);

            base._outbreakName = "New Outbreak";
            this.UpdateTimer.Elapsed += UpdateTimer_Elapsed;


            LabResultCollection.Clear();
        }


        void ApplicationViewModel_RegionChanged(object sender, Core.Events.RegionChangedEventArgs e)
        {
            this.ApplicationRegion = e.RegionEnum;
        }

        void CaseCollection_CaseSwitchToLegacyEnter(object sender, EventArgs e)
        {
            CaseViewModel c = sender as CaseViewModel;

            if (c != null && CaseSwitchToLegacyEnter != null)
            {
                CaseSwitchToLegacyEnter(c, new EventArgs());
            }
        }

        void CaseCollection_CaseViewerClosed(object sender, EventArgs e)
        {
            CaseViewModel c = sender as CaseViewModel;
            if (c != null)
            {
                SendMessageForUnlockCase(c);
                IsShowingCaseReportForm = false;
            }
        }

        private void CaseCollection_CaseUpdated(object sender, CaseChangedArgs e)
        {
            #region Input Validation
            if (sender == null)
            {
                throw new ArgumentNullException("sender");
            }
            #endregion Input Validation

            if (CaseCollection == null)
            {
                return;
            }

            CaseViewModel c = e.ChangedCase;
            SendMessageForUpdateCase(c.RecordId);

            EpiCaseClassification originalClassification = e.PreviousCaseClassification;
            EpiCaseClassification updatedClassification = c.EpiCaseDef;

            string originalID = c.OriginalCaseID;
            string updatedID = c.ID;

            bool idUpdated = false;

            if (!originalID.Equals(updatedID, StringComparison.OrdinalIgnoreCase))
            {
                idUpdated = true;
            }
            if (updatedClassification == EpiCaseClassification.NotCase)
            {
                ContactViewModel contactVM = GetContactVM(c.RecordId);
                if (contactVM != null)
                {
                    string prevContactFinalOutcome = contactVM.FinalOutcome;
                    bool prevContactIsActive = contactVM.IsActive;

                    if (contactVM.IsWithin21DayWindow == true)
                    {
                        foreach (CaseViewModel caseVM in CaseCollection
                            .Where(x => x != c)
                            .Where(x => x.Contacts.Contains(contactVM))
                            .Where(x => x.EpiCaseDef != EpiCaseClassification.NotCase))
                        {
                            contactVM.FinalOutcome = String.Empty;
                            contactVM.IsActive = true;

                            CheckCaseContactForDailyFollowUp(c, contactVM, DateTime.Now);
                            SortFollowUps(DailyFollowUpCollection);
                            break;
                        }
                    }
                    else
                    {
                        contactVM.FinalOutcome = "1";
                        contactVM.IsActive = false;

                        lock (_dailyCollectionLock)
                        {
                            DailyCheckViewModel dcToRemove = null;
                            foreach (DailyCheckViewModel dcVM in this.DailyFollowUpCollection)
                            {
                                if (dcVM.ContactVM.Equals(contactVM))
                                {
                                    dcToRemove = dcVM;
                                }
                            }

                            if (dcToRemove != null)
                            {
                                DailyFollowUpCollection.Remove(dcToRemove);
                            }
                        }
                    }

                    if (prevContactIsActive != contactVM.IsActive ||
                        !prevContactFinalOutcome.Equals(contactVM.FinalOutcome, StringComparison.OrdinalIgnoreCase))
                    {
                        SetContactFinalStatus(contactVM);
                    }
                }

                foreach (ContactViewModel iContact in c.Contacts)
                {
                    string prevContactFinalOutcome = iContact.FinalOutcome;
                    bool prevContactIsActive = iContact.IsActive;

                    bool wasExposedByOtherCases = false;
                    // if the contact has no other cases that exposed them...
                    foreach (CaseViewModel caseVM in CaseCollection)
                    {
                        if (caseVM != c && caseVM.Contacts.Contains(iContact) && !(caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.NotCase))
                        {
                            wasExposedByOtherCases = true;
                            break;
                        }
                    }

                    if (!wasExposedByOtherCases)
                    {
                        // then DROP FROM FOLLOW UP
                        iContact.IsActive = false;
                        iContact.FinalOutcome = "3";

                        if (prevContactIsActive != iContact.IsActive ||
                            !prevContactFinalOutcome.Equals(iContact.FinalOutcome, StringComparison.OrdinalIgnoreCase))
                        {
                            SetContactFinalStatus(iContact);
                            RaisePropertyChanged("ContactCollection");

                            lock (_dailyCollectionLock)
                            {
                                DailyCheckViewModel dcToRemove = null;
                                foreach (DailyCheckViewModel dcVM in this.DailyFollowUpCollection)
                                {
                                    if (dcVM.ContactVM.Equals(iContact))
                                    {
                                        dcToRemove = dcVM;
                                    }
                                }

                                if (dcToRemove != null)
                                {
                                    DailyFollowUpCollection.Remove(dcToRemove);
                                }
                            }
                        }
                    }
                }
            }
            // the case already exists, it was 'not a case' before we saved it, and now it's confirmed/probable/suspect
            else if (originalClassification == EpiCaseClassification.NotCase &&
                (c.EpiCaseDef == EpiCaseClassification.Confirmed || c.EpiCaseDef == EpiCaseClassification.Probable || c.EpiCaseDef == EpiCaseClassification.Suspect||c.EpiCaseDef == EpiCaseClassification.Excluded ||c.EpiCaseDef == EpiCaseClassification.PUI))
            {
                if (c.IsContact)
                {
                    ContactViewModel contactVM = ContactCollection[c.RecordId];

                    if (contactVM.IsActive == true)
                    {
                        //if (contactVM.IsWithin21DayWindow)
                        //{
                        contactVM.IsActive = false;
                        contactVM.FinalOutcome = "2";
                        SetContactFinalStatus(contactVM);
                        //}
                    }
                }
                foreach (ContactViewModel contactVM in c.Contacts)
                {
                    string prevContactFinalOutcome = contactVM.FinalOutcome;
                    bool prevContactIsActive = contactVM.IsActive;

                    bool wasExposedByOtherCases = false;
                    // if the contact has no other cases that exposed them...
                    foreach (CaseViewModel caseVM in CaseCollection)
                    {
                        if (caseVM != c && caseVM.Contacts.Contains(contactVM) && !(caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.NotCase))
                        {
                            wasExposedByOtherCases = true;
                            break;
                        }
                    }

                    if (!wasExposedByOtherCases && contactVM.FinalOutcome == "3" && contactVM.IsActive == false &&
                        (contactVM.FollowUpWindowViewModel.FollowUpVisits[Core.Common.DaysInWindow - 1].Status == null ||
                        contactVM.FollowUpWindowViewModel.FollowUpVisits[Core.Common.DaysInWindow - 1].Status.Value == ContactDailyStatus.NotRecorded ||
                        contactVM.FollowUpWindowViewModel.FollowUpVisits[Core.Common.DaysInWindow - 1].Status.Value == ContactDailyStatus.NotSeen))
                    {
                        contactVM.IsActive = true;
                        contactVM.FinalOutcome = String.Empty;

                        if (prevContactIsActive != contactVM.IsActive ||
                            !prevContactFinalOutcome.Equals(contactVM.FinalOutcome, StringComparison.OrdinalIgnoreCase))
                        {
                            SetContactFinalStatus(contactVM);
                            RaisePropertyChanged("ContactCollection");
                        }

                        CheckCaseContactForDailyFollowUp(c, contactVM, DateTime.Now);
                        SortFollowUps(DailyFollowUpCollection);
                    }
                }
            }
            UpdateDatesOfLastContactForSourceCase(c);

            // VHF-383
            foreach (ContactViewModel contact in c.Contacts)
            {
                Query selectQuery = Database.CreateQuery("SELECT * FROM [metaLinks] WHERE [ToViewId] = @ToViewId AND [FromViewId] = @FromViewId AND [ToRecordGuid] = @ToRecordGuid ORDER BY [LastContactDate] DESC");
                selectQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, ContactFormId));
                selectQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, CaseFormId));
                selectQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, contact.RecordId));
                DataTable linksTable = Database.Select(selectQuery);

                foreach (DataRow row in linksTable.Rows)
                {
                    string recordId = row["FromRecordGuid"].ToString();

                    CaseViewModel iCase = GetCaseVM(recordId);

                    if (iCase != null && iCase.EpiCaseDef != EpiCaseClassification.NotCase)
                    {
                        LoadContactLinkData(row, contact, iCase, WindowGenerationMode.Shift);
                        break;
                    }
                }
            }

            DataTable dt = GetLabTableforCase(c.RecordId);
            if (dt.Rows.Count >= 1)
            {
                DataRow rowView = dt.Rows[0];
                if (rowView["SUDVNPPCR"].ToString() == "1" ||
                                            rowView["SUDVPCR2"].ToString() == "1" ||
                                            rowView["BDBVNPPCR"].ToString() == "1" ||
                                            rowView["BDBVVP40PCR"].ToString() == "1" ||
                                            rowView["EBOVPCR1"].ToString() == "1" ||
                                            rowView["EBOVPCR2"].ToString() == "1" ||
                                            rowView["MARVPolPCR"].ToString() == "1" ||
                                            rowView["MARVVP40PCR"].ToString() == "1"
                                            )
                {
                    c.LastSamplePCRResult = ContactTracing.CaseView.Properties.Resources.Positive;
                }
                else
                {
                    c.LastSamplePCRResult = ContactTracing.CaseView.Properties.Resources.Negative;
                }
                LabTable = GetLabTable();
                DataRow[] rows = LabTable.Select("FKEY = '" + c.RecordId + "'", "DateSampleCollected DESC", DataViewRowState.CurrentRows);
                if (rows.Length >= 1)
                {
                    DataRow rowview = rows[0];
                    if (!String.IsNullOrEmpty(rowview["DateSampleCollected"].ToString()))
                    {
                        c.DateLastLabSampleCollected = DateTime.Parse(rowview["DateSampleCollected"].ToString());
                    }
                }    
            }

            if (idUpdated)
            {
                //if (IsShowingCaseReportForm)
                //{
                    SortCases();
                //}
                //else
                //{
                //    //SortCases();

                //    int currentViewPosition = CaseCollectionView.CurrentPosition;
                //    CaseCollection.SortSingleItemByID(c);
                //    int newViewPosition = CaseCollectionView.CurrentPosition;

                //    if (newViewPosition != currentViewPosition)
                //    {
                //        CaseCollectionView.MoveCurrentTo(c);
                //    }
                //}
            }
        }

        private void CaseCollection_CaseAdded(object sender, CaseAddedArgs e)
        {
            //throw new NotImplementedException();
            //AddCase(e.AddedCase);
            SendMessageForAddCase(e.AddedCase.RecordId);
        }
        #endregion // Constructors

        #region Commands

        private bool CanExecuteRepopulateCollectionsCommand(bool canExecute)
        {
            if (IsLoadingProjectData || IsSendingServerUpdates || IsDataSyncing || IsWaitingOnOtherClients || IsExportingData || !IsConnected ||
                (CaseCollectionView != null && CaseCollectionView.CurrentItem != null && (((CaseViewModel)(CaseCollectionView.CurrentItem)).IsEditing || ((CaseViewModel)(CaseCollectionView.CurrentItem)).IsSaving)
                ))
            {
                //canExecute = false;
                return false;
            }
            else
            {
                //canExecute = true;
                return true;
            }
        }

        private bool CanShowCaseFormCommand()
        {
            return CanExecuteRepopulateCollectionsCommand(true);
        }

        private bool CanExecuteMoveToNextCaseRecordCommand()
        {
            if (CaseForm == null) return false;
            if (CaseCollectionView == null) return false;
            if (CaseCollectionView.CurrentPosition == (CaseCollectionView.Cast<CaseViewModel>().Count() - 1)) return false;

            CaseViewModel currentRecord = CaseCollectionView.CurrentItem as CaseViewModel;
            if (currentRecord == null)
            {
                return false;
            }
            else
            {
                if (currentRecord.HasErrors)
                {
                    return false;
                }
                return currentRecord.FieldValueChanges.Count == 0;
            }
        }

        private bool CanExecuteMoveToPreviousCaseRecordCommand()
        {
            if (CaseForm == null) return false;
            if (CaseCollectionView == null) return false;
            if (CaseCollectionView.CurrentPosition == 0) return false;

            CaseViewModel currentRecord = CaseCollectionView.CurrentItem as CaseViewModel;
            if (currentRecord == null)
            {
                return false;
            }
            else
            {
                if (currentRecord.HasErrors)
                {
                    return false;
                }
                return currentRecord.FieldValueChanges.Count == 0;
            }
        }

        private bool CanExecuteMoveToFirstCaseRecordCommand()
        {
            return CanExecuteMoveToPreviousCaseRecordCommand();
        }

        private bool CanExecuteMoveToLastCaseRecordCommand()
        {
            return CanExecuteMoveToNextCaseRecordCommand();
        }


        public ICommand AddCaseShortFormCommand { get { return new RelayCommand(AddCaseShortFormCommandExecute, CanShowCaseFormCommand); } }
        protected void AddCaseShortFormCommandExecute()
        {
            //if (this.Country != "Liberia" && this.Country != "Sierra Leone" && this.Country != "Guinea")
            //{
            //    return;
            //}

            if (!this.IsShortForm) //17040
            {
                return;
            }
            CaseViewModel c = new CaseViewModel(CaseForm, LabForm);
            if (CaseViewModel.Months.Equals(string.Empty))
            {
                CaseViewModel.SampleLabel = Properties.Resources.Sample;

                CaseViewModel.PlaceDeathCommunityValue = Properties.Resources.PlaceDeathCommunity;
                CaseViewModel.PlaceDeathHospitalValue = Properties.Resources.PlaceDeathHospital;
                CaseViewModel.PlaceDeathOtherValue = Properties.Resources.PlaceDeathOther;

                CaseViewModel.RecComplete = ContactTracing.CaseView.Properties.Resources.RecComplete;
                CaseViewModel.RecNoCRF = ContactTracing.CaseView.Properties.Resources.RecNoCRF;
                CaseViewModel.RecMissCRF = ContactTracing.CaseView.Properties.Resources.RecMissCRF;
                CaseViewModel.RecPendingLab = ContactTracing.CaseView.Properties.Resources.RecPendingLab;
                CaseViewModel.RecPendingOutcome = ContactTracing.CaseView.Properties.Resources.RecPendingOutcome;

                CaseViewModel.Years = Properties.Resources.AgeUnitYears;
                CaseViewModel.Months = Properties.Resources.AgeUnitMonths;

                ContactViewModel.Male = Properties.Resources.Male;
                ContactViewModel.Female = Properties.Resources.Female;

                CaseViewModel.Male = Properties.Resources.Male;
                CaseViewModel.Female = Properties.Resources.Female;

                CaseViewModel.Dead = Properties.Resources.Dead;
                CaseViewModel.Alive = Properties.Resources.Alive;

                CaseViewModel.MaleAbbr = Properties.Resources.MaleSymbol;
                CaseViewModel.FemaleAbbr = Properties.Resources.FemaleSymbol;

                EpiDataHelper.SampleInterpretConfirmedAcute = Properties.Resources.SampleInterpretationConfirmedAcute;
                EpiDataHelper.SampleInterpretConfirmedConvalescent = Properties.Resources.SampleInterpretationConfirmedConvalescent;
                EpiDataHelper.SampleInterpretNotCase = Properties.Resources.SampleInterpretationNotCase;
                EpiDataHelper.SampleInterpretIndeterminate = Properties.Resources.SampleInterpretationIndeterminate;
                EpiDataHelper.SampleInterpretNegativeNeedsFollowUp = Properties.Resources.SampleInterpretationNegativeNeedFollowUp;

                EpiDataHelper.PCRPositive = Properties.Resources.Positive;
                EpiDataHelper.PCRNegative = Properties.Resources.Negative;
                EpiDataHelper.PCRIndeterminate = Properties.Resources.AnalysisClassIndeterminate;
                EpiDataHelper.PCRNotAvailable = "n/a";

                EpiDataHelper.SampleTypeWholeBlood = Properties.Resources.SampleTypeWholeBlood;
                EpiDataHelper.SampleTypeSerum = Properties.Resources.SampleTypeSerum;
                EpiDataHelper.SampleTypeHeartBlood = Properties.Resources.SampleTypeHeartBlood;
                EpiDataHelper.SampleTypeSkin = Properties.Resources.SampleTypeSkin;
                EpiDataHelper.SampleTypeOther = Properties.Resources.SampleTypeOther;
                EpiDataHelper.SampleTypeSalivaSwab = Properties.Resources.SampleTypeSalivaSwab;
            }
            CaseCollection.Add(c);
            CaseCollectionView.MoveCurrentTo(c);
            c.IsEditing = true;
            CaseOpenedInShortForm = c; //VHF-260
            this.IsShortFormOpened = true; //VHF-256
        }

        //VHF-260
        public ICommand ToggleShortCaseReportFormCommand { get { return new RelayCommand<object>(ToggleShortCaseReportFormCommandExecute); } }
        protected void ToggleShortCaseReportFormCommandExecute(object vm)
        {
            //if (this.Country != "Liberia" && this.Country != "Sierra Leone" && this.Country != "Guinea"), CanShowCaseFormCommand
            //CanShowCaseFormCommand();
            if (!IsShortForm) //17040
            {
                return;
            }

            CaseViewModel cvm = vm as CaseViewModel;

            if (cvm != null)
            {
                cvm.IsEditing = !cvm.IsEditing;
                IsShowingCaseReportForm = cvm.IsEditing;

                if (cvm.IsEditing == false)
                {
                    SendMessageForUnlockCase(cvm);
                }
                else
                {
                    SendMessageForLockCase(cvm);
                }
                CaseOpenedInShortForm = cvm;
            }
        }

        public ICommand ErrorAcceptCommand { get { return new RelayCommand(ErrorAcceptCommandExecute); } }
        protected void ErrorAcceptCommandExecute()
        {
            IsShowingError = false;
            ErrorMessage = String.Empty;
            ErrorMessageDetail = String.Empty;
        }

        private bool CanExecuteRemoveCaselessContactsCommand()
        {
            if (IsWaitingOnOtherClients ||
                IsShowingDataExporter ||
                IsSendingServerUpdates ||
                IsLoadingProjectData ||
                IsExportingData ||
                IsDataSyncing ||
                !IsConnected)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public ICommand RemoveCaselessContactsCommand { get { return new RelayCommand(RemoveCaselessContactsCommandExecute, CanExecuteRemoveCaselessContactsCommand); } }
        protected void RemoveCaselessContactsCommandExecute()
        {
            DeleteCaselessContacts(true);
        }

        public ICommand HideDataExportCommand { get { return new RelayCommand(HideDataExportCommandExecute); } }
        protected void HideDataExportCommandExecute()
        {
            SyncStatus = String.Empty;
            TaskbarProgressValue = 0;
            IsShowingDataExporter = false;
        }

        public ICommand RepairMetaFieldsTableCommand { get { return new RelayCommand(RepairMetaFieldsTableCommandExecute, CanExecuteRemoveCaselessContactsCommand); } }
        protected void RepairMetaFieldsTableCommandExecute()
        {
            // only do this for SQL Server!
            if (IsMicrosoftSQLDatabase)
            {
                try
                {
                    SendMessageForAwaitAll();

                    // Fix bit columns
                    List<string> columnNames = new List<string>() { "HasTabStop", "ShouldRepeatLast", "IsRequired", "IsReadOnly", "ShouldRetainImageSize", "ShouldReturnToParent", "Sort", "IsExclusiveTable", "ShowTextOnRight" };
                    foreach (string columnName in columnNames)
                    {
                        Query bitColumnUpdater = Database.CreateQuery("ALTER TABLE [metaFields] ALTER COLUMN [" + columnName + "] bit null");
                        Database.ExecuteNonQuery(bitColumnUpdater);
                    }

                    // Get the IDs that will become the 'old' set of IDs...
                    Query selectQuery = Database.CreateQuery("SELECT FieldId, UniqueId, Name, SourceFieldId, RelateCondition FROM metaFields");
                    DataTable previousMetaFieldsTable = Database.Select(selectQuery);

                    Dictionary<string, int> prevFieldIdLookup = new Dictionary<string, int>();

                    foreach (DataRow row in previousMetaFieldsTable.Rows)
                    {
                        if (!String.IsNullOrEmpty(row["UniqueId"].ToString()))
                        {
                            prevFieldIdLookup.Add(row["UniqueId"].ToString(), Convert.ToInt32(row["FieldId"]));
                        }
                    }

                    // Drop and re-add the FieldId column
                    Query metaFieldsTableQuery = Database.CreateQuery("ALTER TABLE metaFields DROP COLUMN FieldId");
                    Database.ExecuteNonQuery(metaFieldsTableQuery);

                    metaFieldsTableQuery = Database.CreateQuery("ALTER TABLE metaFields ADD FieldId int not null identity(1,1)");
                    Database.ExecuteNonQuery(metaFieldsTableQuery);

                    // Get the IDs that will become the 'new' set of IDs...
                    selectQuery = Database.CreateQuery("SELECT FieldId, UniqueId, Name, SourceFieldId, RelateCondition FROM metaFields");
                    DataTable updatedMetaFieldsTable = Database.Select(selectQuery);

                    Dictionary<string, int> updatedFieldIdLookup = new Dictionary<string, int>();
                    foreach (DataRow row in updatedMetaFieldsTable.Rows)
                    {
                        if (!String.IsNullOrEmpty(row["UniqueId"].ToString()))
                        {
                            updatedFieldIdLookup.Add(row["UniqueId"].ToString(), Convert.ToInt32(row["FieldId"]));
                        }
                    }

                    foreach (DataRow updatedRow in updatedMetaFieldsTable.Rows)
                    {
                        int? updatedSourceFieldId = null;
                        if (updatedRow["SourceFieldId"] != DBNull.Value)
                        {
                            updatedSourceFieldId = Convert.ToInt32(updatedRow["SourceFieldId"]);
                        }
                        string updatedUniqueId = updatedRow["UniqueId"].ToString();
                        string updatedRelateCondition = updatedRow["RelateCondition"].ToString();

                        // update any relate conditions
                        if (!String.IsNullOrEmpty(updatedRelateCondition) && updatedRelateCondition.Contains(':'))
                        {
                            string[] condition = updatedRelateCondition.Split(':');

                            if (condition.Length == 2)
                            {
                                int id;
                                bool success = int.TryParse(condition[1], out id);

                                if (success)
                                {
                                    foreach (KeyValuePair<string, int> kvp in prevFieldIdLookup)
                                    {
                                        if (kvp.Value.Equals(id))
                                        {
                                            int updatedRelatedId = updatedFieldIdLookup[kvp.Key];
                                            updatedRelateCondition = condition[0] + ":" + updatedRelatedId.ToString();

                                            Query updateQuery = Database.CreateQuery("UPDATE metaFields SET RelateCondition = @RelateCondition WHERE UniqueId = @UniqueId");
                                            updateQuery.Parameters.Add(new QueryParameter("@RelateCondition", DbType.String, updatedRelateCondition));
                                            updateQuery.Parameters.Add(new QueryParameter("@UniqueId", DbType.String, updatedUniqueId));
                                            Database.ExecuteNonQuery(updateQuery);
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        // update any source field Ids
                        if (updatedSourceFieldId.HasValue)
                        {
                            foreach (KeyValuePair<string, int> kvp in prevFieldIdLookup)
                            {
                                if (kvp.Value.Equals(updatedSourceFieldId.Value))
                                {
                                    int newId = updatedFieldIdLookup[kvp.Key];

                                    Query updateQuery = Database.CreateQuery("UPDATE metaFields SET SourceFieldId = @SourceFieldId WHERE UniqueId = @UniqueId");
                                    updateQuery.Parameters.Add(new QueryParameter("@SourceFieldId", DbType.Int32, newId));
                                    updateQuery.Parameters.Add(new QueryParameter("@UniqueId", DbType.String, updatedUniqueId));
                                    Database.ExecuteNonQuery(updateQuery);
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {

                }
                finally
                {
                    SendMessageForUnAwaitAll();
                }
            }
        }

        public ICommand RefreshAgeGroupData { get { return new RelayCommand<bool>(RefreshAgeGroupDataExecute); } }
        protected void RefreshAgeGroupDataExecute(bool reverse)
        {
            #region Confirmed and Probable
            AgeGroupDataPointCollectionCP = new ObservableCollection<XYColumnChartData>();
            List<XYColumnChartData> ageGroupDataPointListCP = new List<XYColumnChartData>();

            if (CaseCollection == null)
            {
                return;
            }

            for (int i = 0; i < 100; i = i + 10)
            {
                XYColumnChartData xyDataCP = new XYColumnChartData();
                xyDataCP.X = i.ToString() + " - <" + (i + 10).ToString();
                xyDataCP.S = "Count"; // Properties.Resources.Probable;
                xyDataCP.Y = 0;

                ageGroupDataPointListCP.Add(xyDataCP);

                //XYColumnChartData xyDataCA = new XYColumnChartData();
                //xyDataCA.X = i.ToString() + " - <" + (i + 10).ToString();
                //xyDataCA.S = Properties.Resources.Confirmed;
                //xyDataCA.Y = 0;

                //AgeGroupDataPointCollectionCP.Add(xyDataCA);
            }

            var caseQuery = from caseVM in CaseCollection
                            where (caseVM.AgeUnit.HasValue == true || IsCountryUS) &&
                            caseVM.Age.HasValue == true &&
                            (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed ||
                            caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable)
                            orderby caseVM.AgeYears.Value ascending
                            select caseVM;

            foreach (var c in caseQuery)
            {
                for (int i = 0; i < 100; i = i + 10)
                {
                    if (c.AgeYears >= i && c.AgeYears < (i + 10))
                    {
                        string xLabel = i.ToString() + " - <" + (i + 10).ToString();

                        var pointQuery = from point in ageGroupDataPointListCP
                                         where point.X.ToString() == xLabel //&& point.S.ToString() == c.EpiCaseDef
                                         select point;

                        XYColumnChartData xyDataCA = pointQuery.First();

                        xyDataCA.Y = ((int)(xyDataCA.Y)) + 1;
                        //AgeGroupDataPointCollectionCP.Add(xyDataCA);
                    }
                }
            }

            if (reverse)
            {
                for (int i = ageGroupDataPointListCP.Count - 1; i >= 0; i--)
                {
                    AgeGroupDataPointCollectionCP.Add(ageGroupDataPointListCP[i]);
                }
            }
            else
            {
                for (int i = 0; i > ageGroupDataPointListCP.Count; i++)
                {
                    AgeGroupDataPointCollectionCP.Add(ageGroupDataPointListCP[i]);
                }
            }

            #endregion Confirmed and Probable
        }

        public ICommand PopulateLabRecordsForCase { get { return new RelayCommand<CaseViewModel>(PopulateLabRecordsForCaseExecute); } }
        protected virtual void PopulateLabRecordsForCaseExecute(CaseViewModel c)
        {
            if (CaseCollection == null || LabResultCollection == null)
            {
                return;
            }

            LabResultCollection.Clear();

            Query selectQuery = Database.CreateQuery("SELECT * " + LabForm.FromViewSQL + " WHERE [FKEY] = @FKEY AND RecStatus = 1");
            selectQuery.Parameters.Add(new QueryParameter("@FKEY", DbType.String, c.RecordId));
            DataTable labDt = Database.Select(selectQuery);

            SudanTestsDetected = false;
            EbolaTestsDetected = false;
            BundibugyoTestsDetected = false;
            MarburgTestsDetected = false;
            CCHFTestsDetected = false;
            RiftTestsDetected = false;

            LassaTestsDetected = false;
            foreach (DataRow labRow in labDt.Rows)
            {
                LabResultViewModel r = new LabResultViewModel(LabForm);
                LoadResultData(labRow, r);
                r.CaseRecordGuid = c.RecordId;
                r.CaseID = c.ID;
                r.Surname = c.Surname;
                r.OtherNames = c.OtherNames;
                switch (c.FinalLabClass)
                {
                    case Core.Enums.FinalLabClassification.ConfirmedAcute:
                        r.FinalLabClassification = Properties.Resources.AnalysisClassConfirmedAcute;
                        break;
                    case Core.Enums.FinalLabClassification.ConfirmedConvalescent:
                        r.FinalLabClassification = Properties.Resources.AnalysisClassConfirmedConvalescent;
                        break;
                    case Core.Enums.FinalLabClassification.Indeterminate:
                        r.FinalLabClassification = Properties.Resources.AnalysisClassIndeterminate;
                        break;
                    case Core.Enums.FinalLabClassification.NeedsFollowUpSample:
                        r.FinalLabClassification = Properties.Resources.AnalysisClassNeedsFollowUp;
                        break;
                    case Core.Enums.FinalLabClassification.NotCase:
                        r.FinalLabClassification = Properties.Resources.NotCase;
                        break;
                    default:
                        r.FinalLabClassification = String.Empty;
                        break;
                }
                //r.FinalLabClassification = c.FinalLabClass;
                LabResultCollection.Add(r);

                if (!String.IsNullOrEmpty(r.MARVPCR)) MarburgTestsDetected = true;
                if (!String.IsNullOrEmpty(r.EBOVPCR)) EbolaTestsDetected = true;
                if (!String.IsNullOrEmpty(r.BDBVPCR)) BundibugyoTestsDetected = true;
                if (!String.IsNullOrEmpty(r.SUDVPCR)) SudanTestsDetected = true;
                if (!String.IsNullOrEmpty(r.CCHFPCR)) CCHFTestsDetected = true;
                if (!String.IsNullOrEmpty(r.RVFPCR)) RiftTestsDetected = true;
                if (!String.IsNullOrEmpty(r.LHFPCR)) LassaTestsDetected = true;
            }
        }

        public ICommand UpdateDaily { get { return new RelayCommand<KeyValuePair<DateTime, DailyCheckViewModel>>(UpdateDailyExecute); } }
        protected virtual void UpdateDailyExecute(KeyValuePair<DateTime, DailyCheckViewModel> kvpDC)
        {
            if (DailyFollowUpCollection == null)
                return;

            DailyCheckViewModel updatedDailyCheckVM = kvpDC.Value;
            DateTime dt = kvpDC.Key;

            foreach (FollowUpVisitViewModel fuVM in updatedDailyCheckVM.ContactVM.FollowUpWindowViewModel.FollowUpVisits)
            {
                if (fuVM.FollowUpVisit.Day == updatedDailyCheckVM.GetDay(dt)) //updatedDailyCheckVM.Day)
                {
                    fuVM.Status = updatedDailyCheckVM.Status;
                    fuVM.Notes = updatedDailyCheckVM.Notes;
                    if (updatedDailyCheckVM.Temp1 != null)
                        fuVM.Temp1 = (double)updatedDailyCheckVM.Temp1;
                    else
                        fuVM.Temp1 = 0.0;
                    if (updatedDailyCheckVM.Temp2 != null)
                        fuVM.Temp2 = (double)updatedDailyCheckVM.Temp2;
                    else
                        fuVM.Temp2 = 0.0;
                    break;
                }
            }

            ContactDailyStatus? status = updatedDailyCheckVM.Status;

            #region Database Query
            Query updateQuery = Database.CreateQuery("UPDATE [metaLinks] SET " +
                "[Day" + updatedDailyCheckVM.GetDay(dt).ToString() + "] = @Status, " +
                "[Day" + updatedDailyCheckVM.GetDay(dt).ToString() + "Notes] = @Notes " +
                "WHERE [ToRecordGuid] = @ToRecordGuid AND [FromRecordGuid] = @FromRecordGuid AND [ToViewId] = @ToViewId AND " +
                "[FromViewId] = @FromViewId");

            Query insertIntoMetaHistory = Database.CreateQuery("INSERT INTO [metaHistory] (ContactGUID, FollowUpDate, StatusOnDate, [Note], Temp1, Temp2) VALUES (" +
                                    "@ToRecordGuid, @FollowUpDate, @Status, @Notes, @Temp1, @Temp2)");

            Query deleteFromMetaHistory = Database.CreateQuery("DELETE FROM [metaHistory] " +
                "WHERE [ContactGUID] = @ToRecordGuid AND [FollowUpDate] = @FollowUpDate");

            insertIntoMetaHistory.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.Guid, updatedDailyCheckVM.ContactVM.RecordId));
            deleteFromMetaHistory.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.Guid, updatedDailyCheckVM.ContactVM.RecordId));

            System.DateTime followUpDate = updatedDailyCheckVM.DateLastContact.Add(new TimeSpan(updatedDailyCheckVM.GetDay(dt), 0, 0, 0));

            insertIntoMetaHistory.Parameters.Add(new QueryParameter("@FollowUpDate", DbType.DateTime, followUpDate));
            deleteFromMetaHistory.Parameters.Add(new QueryParameter("@FollowUpDate", DbType.DateTime, followUpDate));

            if (!status.HasValue)
            {
                updateQuery.Parameters.Add(new QueryParameter("@Status", DbType.Int16, DBNull.Value));
                insertIntoMetaHistory.Parameters.Add(new QueryParameter("@Status", DbType.Int16, DBNull.Value));
            }
            else
            {
                updateQuery.Parameters.Add(new QueryParameter("@Status", DbType.Int16, Convert.ToInt16(status.Value)));
                insertIntoMetaHistory.Parameters.Add(new QueryParameter("@Status", DbType.Int16, Convert.ToInt16(status.Value)));
            }
            if (updatedDailyCheckVM.Notes == null)
            {
                updateQuery.Parameters.Add(new QueryParameter("@Notes", DbType.String, ""));
                insertIntoMetaHistory.Parameters.Add(new QueryParameter("@Notes", DbType.String, ""));
            }
            else
            {
                updateQuery.Parameters.Add(new QueryParameter("@Notes", DbType.String, updatedDailyCheckVM.Notes));
                insertIntoMetaHistory.Parameters.Add(new QueryParameter("@Notes", DbType.String, updatedDailyCheckVM.Notes));
            }
            if (updatedDailyCheckVM.Temp1 == null)
                insertIntoMetaHistory.Parameters.Add(new QueryParameter("@Temp1", DbType.Double, DBNull.Value));
            else
                insertIntoMetaHistory.Parameters.Add(new QueryParameter("@Temp1", DbType.Double, updatedDailyCheckVM.Temp1));
            if (updatedDailyCheckVM.Temp2 == null)
            {
                insertIntoMetaHistory.Parameters.Add(new QueryParameter("@Temp2", DbType.Double, DBNull.Value));
            }
            else
            {
                insertIntoMetaHistory.Parameters.Add(new QueryParameter("@Temp2", DbType.Double, updatedDailyCheckVM.Temp2));
            }

            updateQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, updatedDailyCheckVM.ContactVM.RecordId));
            updateQuery.Parameters.Add(new QueryParameter("@FromRecordGuid", DbType.String, updatedDailyCheckVM.CaseVM.RecordId));
            updateQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int16, ContactFormId));
            updateQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int16, CaseFormId));
            Database.ExecuteNonQuery(updateQuery);

            Database.ExecuteNonQuery(deleteFromMetaHistory);
            if (status.HasValue || !String.IsNullOrEmpty(updatedDailyCheckVM.Notes) || updatedDailyCheckVM.Temp1 > 0.0 || updatedDailyCheckVM.Temp2 > 0.0)
                Database.ExecuteNonQuery(insertIntoMetaHistory);

            #endregion // Database Query

            if (updatedDailyCheckVM.GetDay(dt) == Core.Common.DaysInWindow)
            {
                if (status.HasValue && (status.Value == ContactDailyStatus.NotSeen || status.Value == ContactDailyStatus.NotRecorded))
                {
                    updatedDailyCheckVM.ContactVM.IsActive = true;
                }
                else
                {
                    if (status.HasValue && (status.Value == ContactDailyStatus.SeenNotSick || status.Value == ContactDailyStatus.SeenSickAndNotIsolated))
                    {
                        updatedDailyCheckVM.ContactVM.IsActive = false;
                        updatedDailyCheckVM.ContactVM.FinalOutcome = "1";
                        SetContactFinalStatus(updatedDailyCheckVM.ContactVM);
                        RaisePropertyChanged("ContactCollection");
                    }
                    else if (status.HasValue && (status.Value == ContactDailyStatus.SeenSickAndIsolated || status.Value == ContactDailyStatus.Dead))
                    {
                        updatedDailyCheckVM.ContactVM.IsActive = false;
                    }
                    else
                    {
                        // On day 21, the contact has no status, so it therefore shouldn't have a
                        // final outcome according to the client. And therefore it should be re-activated.
                        // Make sure we re-active it and erase any prior final outcome.
                        updatedDailyCheckVM.ContactVM.IsActive = true;
                        updatedDailyCheckVM.ContactVM.FinalOutcome = String.Empty;
                        SetContactFinalStatus(updatedDailyCheckVM.ContactVM);
                        RaisePropertyChanged("ContactCollection");
                    }
                }
            }
            else if (updatedDailyCheckVM.GetDay(dt) < Core.Common.DaysInWindow)
            {

            }
        }

        public ICommand UpdateCaseContactLink { get { return new RelayCommand<CaseContactPairViewModel>(UpdateCaseContactLinkExecute); } }
        protected virtual void UpdateCaseContactLinkExecute(CaseContactPairViewModel ccpVM)
        {
            bool shiftedWindow = false;

            if (ContactLinkCollection == null)
                return;

            foreach (var iccpVM in ContactLinkCollection)
            {
                if (iccpVM.ContactVM == ccpVM.ContactVM && iccpVM.CaseVM == ccpVM.CaseVM)
                {
                    DateTime originalWindowStartDate = iccpVM.ContactVM.FollowUpWindowViewModel.WindowStartDate;
                    DateTime newWindowStartDate = ccpVM.DateFirstFollowUp;
                    TimeSpan windowDifference = newWindowStartDate - originalWindowStartDate;

                    iccpVM.Update.Execute(ccpVM);

                    DateTime highestDate = DateTime.MinValue;

                    var query = from jccpVM in ContactLinkCollection
                                where jccpVM.ContactVM == ccpVM.ContactVM && jccpVM.CaseVM != ccpVM.CaseVM
                                select jccpVM;

                    foreach (CaseContactPairViewModel jccpVM in query)
                    {
                        if (jccpVM.DateLastContact > highestDate)
                        {
                            highestDate = jccpVM.DateLastContact;
                        }
                    }

                    if (ccpVM.DateLastContact > highestDate)
                    {
                        iccpVM.ContactVM.FollowUpWindowViewModel.WindowStartDate = ccpVM.DateFirstFollowUp; //ccpVM.DateLastContact;
                    }

                    RaisePropertyChanged("CurrentContactLinkCollection");

                    Query updateQuery = iccpVM.GenerateUpdateQuery(this.Database, ContactFormId, CaseFormId);
                    int rowsAffected = Database.ExecuteNonQuery(updateQuery);

                    if (windowDifference.TotalDays != 0)
                    {
                        ShiftWindow(windowDifference, ccpVM.ContactVM, ccpVM.CaseVM);
                        shiftedWindow = true;
                    }

                    break;
                }
            }

            CheckCaseContactForDailyFollowUp(ccpVM.CaseVM, ccpVM.ContactVM, DateTime.Today);
            UpdateDateOfLastContact(ccpVM.ContactVM);

            if (shiftedWindow)
            {
                RepopulateCollections(false);
            }
            //UpdateDatesOfLastContact();
        }

        private void ShiftWindow(TimeSpan windowDifference, ContactViewModel contact, CaseViewModel sourceCase)
        {
            int shift = Convert.ToInt32(Math.Round(windowDifference.TotalDays, 0));

            int?[] dailyStatuses = new int?[Core.Common.DaysInWindow];
            int?[] newDailyStatuses = new int?[Core.Common.DaysInWindow];

            string[] dailyStatusesNotes = new string[Core.Common.DaysInWindow];
            string[] newDailyStatusesNotes = new string[Core.Common.DaysInWindow];

            int linkId = -1;

            for (int i = 0; i < Core.Common.DaysInWindow; i++)
            {
                Query selectQuery = Database.CreateQuery("SELECT Day" + (i + 1).ToString() + ", Day" + (i + 1).ToString() + "Notes, LinkId FROM [metaLinks] WHERE ToRecordGuid = @ToRecordGuid AND FromRecordGuid = @FromRecordGuid AND ToViewId = @ToViewId AND FromViewId = @FromViewId ORDER BY [LastContactDate] DESC");
                selectQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, contact.RecordId));
                selectQuery.Parameters.Add(new QueryParameter("@FromRecordGuid", DbType.String, sourceCase.RecordId));
                selectQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, ContactFormId));
                selectQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, CaseFormId));
                DataTable linkTable = Database.Select(selectQuery);
                if (linkTable.Rows[0][0] == DBNull.Value)
                {
                    dailyStatuses[i] = null;
                }
                else
                {
                    dailyStatuses[i] = Convert.ToInt32(linkTable.Rows[0][0]);
                }

                dailyStatusesNotes[i] = linkTable.Rows[0][1].ToString();

                linkId = Convert.ToInt32(linkTable.Rows[0]["LinkId"]);
            }

            for (int i = 0; i < Core.Common.DaysInWindow; i++)
            {
                int shiftedIndex = i + shift;

                if (shiftedIndex >= 0 && shiftedIndex < Core.Common.DaysInWindow)
                {
                    newDailyStatuses[i] = dailyStatuses[shiftedIndex];
                    newDailyStatusesNotes[i] = dailyStatusesNotes[shiftedIndex];

                    Query updateQuery = Database.CreateQuery("UPDATE [metaLinks] SET Day" + (i + 1).ToString() + " = @Day, Day" + (i + 1).ToString() + "Notes = @DayNotes WHERE LinkId = @LinkId");
                    if (newDailyStatuses[i].HasValue)
                    {
                        updateQuery.Parameters.Add(new QueryParameter("@Day", DbType.Int16, newDailyStatuses[i]));
                    }
                    else
                    {
                        updateQuery.Parameters.Add(new QueryParameter("@Day", DbType.Int16, DBNull.Value));
                    }
                    updateQuery.Parameters.Add(new QueryParameter("@DayNotes", DbType.String, newDailyStatusesNotes[i]));
                    updateQuery.Parameters.Add(new QueryParameter("@LinkId", DbType.Int32, linkId));
                    int rows = Database.ExecuteNonQuery(updateQuery);
                }
                else
                {
                    Query updateQuery = Database.CreateQuery("UPDATE [metaLinks] SET Day" + (i + 1).ToString() + " = @Day, Day" + (i + 1).ToString() + "Notes = @DayNotes WHERE LinkId = @LinkId");
                    updateQuery.Parameters.Add(new QueryParameter("@Day", DbType.Int16, DBNull.Value));

                    updateQuery.Parameters.Add(new QueryParameter("@DayNotes", DbType.String, String.Empty));
                    updateQuery.Parameters.Add(new QueryParameter("@LinkId", DbType.Int32, linkId));
                    int rows = Database.ExecuteNonQuery(updateQuery);
                }
            }

            if (linkId >= 0)
            {
                /* Query selectQuery = Database.CreateQuery("SELECT * FROM [metaLinks] WHERE ToRecordGuid = @ToRecordGuid AND ToViewId = @ToViewId AND FromViewId = @FromViewId ORDER BY [LastContactDate] DESC");
                 selectQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, contact.RecordId));
                 selectQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, ContactFormId));
                 selectQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, CaseFormId));
                 DataTable linkTable = Database.Select(selectQuery);
                 if (linkTable.Rows.Count >= 1)
                 {
                     LoadContactFollowUps(linkTable.Rows[0], contact, true);
                 }*/
                ///////fix for the error on updating existing contact .
                DataRow[] filteredrows = metaLinksDataTable.Select("ToRecordGuid ='" + contact.RecordId + "' AND ToViewId ='" + ContactFormId + "' AND FromViewId = '" + CaseFormId + "'");
                foreach (DataRow r in filteredrows)
                {
                    LoadContactFollowUps(r, contact, true);
                    break;
                }
            }
        }

        public ICommand UpdateCaseExposureLink { get { return new RelayCommand<CaseExposurePairViewModel>(UpdateCaseExposureLinkExecute); } }
        protected virtual void UpdateCaseExposureLinkExecute(CaseExposurePairViewModel cepVM)
        {
            if (CurrentSourceCaseCollection == null)
                return;

            foreach (var icepVM in CurrentSourceCaseCollection)
            {
                if (icepVM.SourceCaseVM.RecordId == cepVM.SourceCaseVM.RecordId && icepVM.ExposedCaseVM.RecordId == cepVM.ExposedCaseVM.RecordId)
                {
                    icepVM.Update.Execute(cepVM);
                    RaisePropertyChanged("CurrentSourceCaseCollection");

                    Query updateQuery = icepVM.GenerateUpdateQuery(this.Database, CaseFormId, CaseFormId);
                    Database.ExecuteNonQuery(updateQuery);
                    break;
                }
            }
        }

        public ICommand UpdateContact { get { return new RelayCommand<ContactViewModel>(UpdateContactExecute); } }
        protected void UpdateContactExecute(ContactViewModel updatedContact)
        {
            if (ContactCollection == null)
                return;

            foreach (var iContact in ContactCollection)
            {
                if (iContact.RecordId == updatedContact.RecordId)
                {
                    iContact.Update.Execute(updatedContact);
                    break;
                }
            }

            UpdateDatesOfLastContact();
        }

        public ICommand UpdateOrAddContact { get { return new RelayCommand<CaseContactPairViewModel>(UpdateOrAddContactExecute); } }
        protected void UpdateOrAddContactExecute(CaseContactPairViewModel caseContactPair)
        {
            if (ContactCollection == null)
                return;

            if (caseContactPair.ContactVM == null)
            {
                ContactViewModel c = CreateContactFromGuid(caseContactPair.ContactRecordId);
                c.DateOfLastContact = caseContactPair.DateLastContact;
                caseContactPair.ContactVM = new ContactViewModel(c);
            }

            bool found = false;
            ContactViewModel contactToUpdate = GetContactVM(caseContactPair.ContactVM.RecordId);
            if (contactToUpdate != null)
            {
                contactToUpdate.Update.Execute(caseContactPair.ContactVM);
                found = true;
                if (caseContactPair.CaseVM != null)
                {
                    ShowContactsForCase.Execute(caseContactPair.CaseVM); // hack to get an updated contact to show up right
                }
            }

            // Replaced this with the above code for performance reasons
            //foreach (var iContact in ContactCollection)
            //{
            //    if (iContact == caseContactPair.ContactVM)
            //    {
            //        iContact.Update.Execute(caseContactPair.ContactVM.Contact);
            //        found = true;
            //        if (caseContactPair.CaseVM != null)
            //        {
            //            ShowContactsForCase.Execute(caseContactPair.CaseVM); // hack to get an updated contact to show up right
            //        }
            //        break;
            //    }
            //}

            //UpdateDatesOfLastContact();
            if (caseContactPair.CaseVM != null)
            {
                UpdateDatesOfLastContactForSourceCase(caseContactPair.CaseVM);
            }
            else
            {
                // should never be null...
                UpdateDatesOfLastContact();
            }

            if (!found)
            {
                AddContact.Execute(caseContactPair);
                SendMessage("Added contact", caseContactPair.ContactVM.RecordId, ServerUpdateType.AddContact);
            }
            else
            {
                RaisePropertyChanged("DailyFollowUpCollection");
                SendMessage("Updated contact", caseContactPair.ContactVM.RecordId, ServerUpdateType.UpdateContact);
            }
        }

        public void AddCaseContactRelationship(CaseContactPairViewModel caseContactPair)
        {
            Database.ExecuteNonQuery(caseContactPair.GenerateInsertQuery(Database, ContactFormId, CaseFormId));
        }

        public ICommand AddContact { get { return new RelayCommand<CaseContactPairViewModel>(AddContactExecute); } }
        protected void AddContactExecute(CaseContactPairViewModel caseContactPair)
        {
            if (ContactCollection == null)
                return;

            bool found = false;
            ContactViewModel c = GetContactVM(caseContactPair.ContactVM.RecordId);
            if (c != null)
            {
                found = true;
            }

            if (!found)
            {
                lock (_contactCollectionLock)
                {
                    ContactCollection.Add(caseContactPair.ContactVM);

                    if (caseContactPair.CaseVM.EpiCaseDef == EpiCaseClassification.NotCase)
                    {
                        // this is the only source case for this contact and the case is not a case, so drop

                        caseContactPair.ContactVM.IsActive = false;
                        caseContactPair.ContactVM.FinalOutcome = "3"; // dropped from follow-up
                        SetContactFinalStatus(caseContactPair.ContactVM); // persist to DB
                    }
                }
            }

            CaseViewModel caseVM = GetCaseVM(caseContactPair.CaseVM.RecordId);
            if (caseVM != null)
            {
                if (caseVM.EpiCaseDef != EpiCaseClassification.NotCase)
                {
                    caseContactPair.ContactVM.IsActive = true;
                    caseContactPair.ContactVM.FinalOutcome = string.Empty; //reactivate if the contact for existing noncase is linked to a case other than "Not a case"
                    SetContactFinalStatus(caseContactPair.ContactVM);
                }
                if (!caseVM.Contacts.Contains(caseContactPair.ContactVM))
                {
                    caseVM.Contacts.Add(caseContactPair.ContactVM);
                }
            }
            // Add the contact to the corresponding case
            //foreach (CaseViewModel iCaseVM in CaseCollection)
            //{
            //    if (iCaseVM == caseContactPair.CaseVM)
            //    {
            //        caseVM = iCaseVM;
            //        caseVM.Contacts.Add(caseContactPair.ContactVM);
            //        break;
            //    }
            //}

            //ContactLinkCollection.Add(caseContactPair);

            // Add corresponding row to metaLinks
            AddCaseContactRelationship(caseContactPair);

            Query selectQuery = Database.CreateQuery("SELECT * FROM [metaLinks] WHERE [ToViewId] = @ToViewId AND [FromViewId] = @FromViewId AND [ToRecordGuid] = @ToRecordGuid ORDER BY [LastContactDate] DESC");
            selectQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, ContactFormId));
            selectQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, CaseFormId));
            selectQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, caseContactPair.ContactVM.RecordId));
            DataTable linksTable = Database.Select(selectQuery);

            if (linksTable.Rows.Count > 0) // should never be 0
            {
                if (!found)
                {
                    DataRow row = linksTable.Rows[0];
                    LoadContactLinkData(row, caseContactPair.ContactVM, caseContactPair.CaseVM);
                }
                else
                {
                    foreach (DataRow row in linksTable.Rows)
                    {
                        // TODO: Look into non-case filtering
                        if (row["FromRecordGuid"].ToString().Equals(caseContactPair.CaseVM.RecordId))
                        {
                            LoadContactLinkData(row, caseContactPair.ContactVM, caseContactPair.CaseVM, WindowGenerationMode.Shift);
                            break;
                        }
                    }
                }
            }
            else if (linksTable.Rows.Count == 0)
            {
                throw new InvalidOperationException("While adding a contact to a case, zero records were returned for this case-contact relationship when one record was expected. The contact's relationship with this case cannot be established.");
            }

            // VHF-357
            if (caseContactPair.ContactVM.LastSourceCase == null && caseContactPair.CaseVM.Contacts.Contains(caseContactPair.ContactVM))
            {
                caseContactPair.ContactVM.LastSourceCase = caseContactPair.CaseVM;
            }

            ShowContactsForCase.Execute(caseVM);
            CheckCaseContactForDailyFollowUp(caseContactPair.CaseVM, caseContactPair.ContactVM, DateTime.Today);
            SortFollowUps(DailyFollowUpCollection);

            SendMessageForAddContact(caseContactPair.ContactVM.RecordId);
        }

        public ICommand AddExposure { get { return new RelayCommand<CaseExposurePairViewModel>(AddExposureExecute); } }
        protected void AddExposureExecute(CaseExposurePairViewModel caseExposurePair)
        {
            if (CaseCollection == null)
                return;

            CurrentSourceCaseCollection.Add(caseExposurePair);

            // Add corresponding row to metaLinks
            Database.ExecuteNonQuery(caseExposurePair.GenerateInsertQuery(Database, CaseFormId, CaseFormId));

            if (caseExposurePair.SourceCaseVM.IsContact)
            {
                Database.ExecuteNonQuery(caseExposurePair.GenerateInsertQuery(Database, ContactFormId, CaseFormId, true));
                RepopulateCollections();
            }
            else
            {
                ShowSourceCasesForCase.Execute(caseExposurePair.SourceCaseVM);
            }
        }

        public ICommand DeleteExposureLink { get { return new RelayCommand<CaseExposurePairViewModel>(DeleteExposureLinkExecute); } }
        protected void DeleteExposureLinkExecute(CaseExposurePairViewModel caseExposurePair)
        {
            if (CaseCollection == null)
                return;

            Database.ExecuteNonQuery(caseExposurePair.GenerateDeleteQuery(Database, CaseFormId, CaseFormId));

            CurrentSourceCaseCollection.Remove(caseExposurePair);
        }

        public ICommand UpdateOrAddCase { get { return new RelayCommand<string>(UpdateOrAddCaseExecute); } }
        protected void UpdateOrAddCaseExecute(string caseGuid)
        {
            if (CaseCollection == null)
                return;

            bool originallyNotACase = false;
            bool idUpdated = false;

            CaseViewModel previousCaseVM = GetCaseVM(caseGuid);

            if (previousCaseVM != null && previousCaseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.NotCase)
            {
                originallyNotACase = true;
            }

            LabTable = GetLabTable();

            CaseViewModel c = CreateCaseFromGuid(caseGuid);

            if (previousCaseVM != null && !previousCaseVM.ID.Equals(c.ID, StringComparison.OrdinalIgnoreCase))
            {
                idUpdated = true;
            }

            CaseViewModel newCaseVM = null;
            bool found = false;
            foreach (var iCase in CaseCollection)
            {
                if (iCase.RecordId == caseGuid)
                {
                    newCaseVM = iCase;
                    iCase.CopyCase(c);
                    found = true;

                    if (Database is Epi.Data.Office.OleDbDatabase)
                    {
                        // Cascade update all lab records with new ID
                        RenderableField idField = LabForm.Fields["ID"] as RenderableField;
                        if (idField != null)
                        {
                            Query updateQuery = Database.CreateQuery("UPDATE " + LabForm.TableName +
                                " lf INNER JOIN " + idField.Page.TableName + " lf1 ON lf.GlobalRecordId = lf1.GlobalRecordId " +
                                " SET [ID] = @ID " +
                                " WHERE lf.FKEY = @FKEY");
                            updateQuery.Parameters.Add(new QueryParameter("@ID", DbType.String, newCaseVM.ID));
                            updateQuery.Parameters.Add(new QueryParameter("@FKEY", DbType.String, newCaseVM.RecordId));
                            int rowsUpdated = Database.ExecuteNonQuery(updateQuery);
                        }
                        break;
                    }
                    else
                    {
                        // Cascade update all lab records with new ID
                        RenderableField idField = LabForm.Fields["ID"] as RenderableField;
                        if (idField != null)
                        {
                            Query updateQuery = Database.CreateQuery("UPDATE L " +
                                "SET L.[ID] = @ID " +
                                "FROM " + idField.Page.TableName + " AS L " +
                                "INNER JOIN " + LabForm.TableName + " AS LR " +
                                "ON L.GlobalRecordId = LR.GlobalRecordId " +
                                "WHERE LR.FKEY = @FKEY");
                            updateQuery.Parameters.Add(new QueryParameter("@ID", DbType.String, newCaseVM.ID));
                            updateQuery.Parameters.Add(new QueryParameter("@FKEY", DbType.String, newCaseVM.RecordId));
                            int rowsUpdated = Database.ExecuteNonQuery(updateQuery);
                        }

                        break;
                    }
                }
            }

            if (!found)
            {
                //AddCase.Execute(c);
                AddCase(c);
            }
            else if (found && c.EpiCaseDef == Core.Enums.EpiCaseClassification.NotCase)
            {
                // do a search for this same person in the contact database
                foreach (ContactViewModel contactVM in ContactCollection)
                {
                    // if a match is found...
                    if (contactVM.RecordId == caseGuid)
                    {
                        string prevContactFinalOutcome = contactVM.FinalOutcome;
                        bool prevContactIsActive = contactVM.IsActive;

                        if (contactVM.IsWithin21DayWindow == true)
                        {
                            contactVM.FinalOutcome = String.Empty;
                            contactVM.IsActive = true;

                            if (newCaseVM != null)
                            {
                                CheckCaseContactForDailyFollowUp(newCaseVM, contactVM, DateTime.Now);
                                SortFollowUps(DailyFollowUpCollection);
                            }
                        }
                        else
                        {
                            contactVM.FinalOutcome = "1";
                            contactVM.IsActive = false;

                            lock (_dailyCollectionLock)
                            {
                                DailyCheckViewModel dcToRemove = null;
                                foreach (DailyCheckViewModel dcVM in this.DailyFollowUpCollection)
                                {
                                    if (dcVM.ContactVM.Equals(contactVM))
                                    {
                                        dcToRemove = dcVM;
                                    }
                                }

                                if (dcToRemove != null)
                                {
                                    DailyFollowUpCollection.Remove(dcToRemove);
                                }
                            }
                        }

                        if (prevContactIsActive != contactVM.IsActive ||
                            !prevContactFinalOutcome.Equals(contactVM.FinalOutcome, StringComparison.OrdinalIgnoreCase))
                        {
                            SetContactFinalStatus(contactVM);
                        }
                    }
                }

                foreach (ContactViewModel contactVM in newCaseVM.Contacts)
                {
                    string prevContactFinalOutcome = contactVM.FinalOutcome;
                    bool prevContactIsActive = contactVM.IsActive;

                    bool wasExposedByOtherCases = false;
                    // if the contact has no other cases that exposed them...
                    foreach (CaseViewModel caseVM in CaseCollection)
                    {
                        if (caseVM.RecordId != newCaseVM.RecordId && caseVM.Contacts.Contains(contactVM) && !(caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.NotCase))
                        {
                            wasExposedByOtherCases = true;
                            break;
                        }
                    }

                    if (!wasExposedByOtherCases)
                    {
                        // then DROP FROM FOLLOW UP
                        contactVM.IsActive = false;
                        contactVM.FinalOutcome = "3";

                        if (prevContactIsActive != contactVM.IsActive ||
                            !prevContactFinalOutcome.Equals(contactVM.FinalOutcome, StringComparison.OrdinalIgnoreCase))
                        {
                            SetContactFinalStatus(contactVM);
                            RaisePropertyChanged("ContactCollection");

                            lock (_dailyCollectionLock)
                            {
                                DailyCheckViewModel dcToRemove = null;
                                foreach (DailyCheckViewModel dcVM in this.DailyFollowUpCollection)
                                {
                                    if (dcVM.ContactVM.Equals(contactVM))
                                    {
                                        dcToRemove = dcVM;
                                    }
                                }

                                if (dcToRemove != null)
                                {
                                    DailyFollowUpCollection.Remove(dcToRemove);
                                }
                            }
                        }
                    }
                    //else
                    //{
                    //    contactVM.IsActive = true;
                    //}
                }
            }
            // the case already exists, it was 'not a case' before we saved it, and now it's confirmed/probable/suspect
            else if (found && originallyNotACase &&
                (c.EpiCaseClassification.Equals("1") || c.EpiCaseClassification.Equals("2") || c.EpiCaseClassification.Equals("3") || c.EpiCaseClassification.Equals("4") || c.EpiCaseClassification.Equals("5")))
            {
                if (c.IsContact)
                {
                    ContactViewModel contactVM = ContactCollection[c.RecordId];

                    if (contactVM.IsActive == true)
                    {
                        //if (contactVM.IsWithin21DayWindow)
                        //{
                        contactVM.IsActive = false;
                        contactVM.FinalOutcome = "2";
                        SetContactFinalStatus(contactVM);
                        //}
                    }
                }

                foreach (ContactViewModel contactVM in newCaseVM.Contacts)
                {
                    string prevContactFinalOutcome = contactVM.FinalOutcome;
                    bool prevContactIsActive = contactVM.IsActive;

                    bool wasExposedByOtherCases = false;
                    // if the contact has no other cases that exposed them...
                    foreach (CaseViewModel caseVM in CaseCollection)
                    {
                        if (caseVM.RecordId != newCaseVM.RecordId && caseVM.Contacts.Contains(contactVM) && !(caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.NotCase))
                        {
                            wasExposedByOtherCases = true;
                            break;
                        }
                    }

                    if (!wasExposedByOtherCases && contactVM.FinalOutcome == "3" && contactVM.IsActive == false &&
                        (contactVM.FollowUpWindowViewModel.FollowUpVisits[Core.Common.DaysInWindow - 1].Status == null ||
                        contactVM.FollowUpWindowViewModel.FollowUpVisits[Core.Common.DaysInWindow - 1].Status.Value == ContactDailyStatus.NotRecorded ||
                        contactVM.FollowUpWindowViewModel.FollowUpVisits[Core.Common.DaysInWindow - 1].Status.Value == ContactDailyStatus.NotSeen))
                    {
                        contactVM.IsActive = true;
                        contactVM.FinalOutcome = String.Empty;

                        if (prevContactIsActive != contactVM.IsActive ||
                            !prevContactFinalOutcome.Equals(contactVM.FinalOutcome, StringComparison.OrdinalIgnoreCase))
                        {
                            SetContactFinalStatus(contactVM);
                            RaisePropertyChanged("ContactCollection");
                        }

                        if (newCaseVM != null)
                        {
                            CheckCaseContactForDailyFollowUp(newCaseVM, contactVM, DateTime.Now);
                            SortFollowUps(DailyFollowUpCollection);
                        }
                    }
                }
            }

            //List<CaseViewModel> duplicates = GetDuplicateCasesBasedOnID();

            //if (duplicates.Count > 0)
            //{
            //    if (DuplicateIdDetected != null)
            //    {
            //        DuplicateIdDetected(this, new DuplicateIdDetectedArgs(duplicates));
            //    }
            //}

            //UpdateDatesOfLastContact();
            if (newCaseVM != null)
            {
                UpdateDatesOfLastContactForSourceCase(newCaseVM);
            }
            else
            {
                // the case is new, and so therefore doesn't have any contacts... don't bother updating last dates of contact
                // UpdateDatesOfLastContact();
            }

            if (!found)
            {
                SortCases();
            }
            else if (idUpdated)
            {
                SortCases();
            }
        }

        public ICommand DeleteCase { get { return new RelayCommand<string>(DeleteCaseExecute); } }
        protected void DeleteCaseExecute(string caseGuid)
        {
            //Logger.Log(DateTime.Now + ":  " + 
            //    CurrentUser + ": " +
            //    "Case deletion requested for record with GUID " + caseGuid);

            if (CaseCollection == null)
                return;

            RemoveCase(caseGuid, true);
            SendMessage("Delete case " + caseGuid, caseGuid, ServerUpdateType.DeleteCase);
        }

        private void RemoveContactFromCollections(string contactGuid)
        {
            ContactViewModel contactVM = GetContactVM(contactGuid);

            for (int i = ContactCollection.Count - 1; i >= 0; i--)
            {
                var iContactVM = ContactCollection[i];
                if (iContactVM == contactVM)
                {
                    lock (_contactCollectionLock)
                    {
                        ContactCollection.Remove(iContactVM);
                    }
                    //CurrentContactCollection.Remove(contactVM);

                    for (int j = ContactLinkCollection.Count - 1; j >= 0; j--)
                    {
                        var ccp = ContactLinkCollection[j];
                        if (ccp.ContactVM == contactVM)
                        {
                            lock (_contactCollectionLock)
                            {
                                ContactLinkCollection.Remove(ccp);
                                CurrentContactLinkCollection.Remove(ccp);
                            }
                            break;
                        }
                    }

                    break;
                }
            }

            for (int i = DailyFollowUpCollection.Count - 1; i >= 0; i--)
            {
                var dailyCheck = DailyFollowUpCollection[i];
                var iContactVM = dailyCheck.ContactVM;
                if (iContactVM == contactVM)
                {
                    lock (_dailyCollectionLock)
                    {
                        DailyFollowUpCollection.Remove(dailyCheck);
                    }
                }
            }

            for (int i = PrevFollowUpCollection.Count - 1; i >= 0; i--)
            {
                var dailyCheck = PrevFollowUpCollection[i];
                var iContactVM = dailyCheck.ContactVM;
                if (iContactVM == contactVM)
                {
                    lock (_prevCollectionLock)
                    {
                        PrevFollowUpCollection.Remove(dailyCheck);
                    }
                }
            }

            foreach (CaseViewModel c in CaseCollection)
            {
                if (c.Contacts.Contains(contactVM))
                {
                    c.Contacts.Remove(contactVM);
                }
            }

            SetCaseAndContactFlags();
        }

        public ICommand DeleteContact { get { return new RelayCommand<string>(DeleteContactExecute); } }
        protected void DeleteContactExecute(string contactGuid)
        {
            if (CaseCollection == null || ContactCollection == null)
                return;

            //Logger.Log(DateTime.Now + ":  " +
            //    CurrentUser + ": " +
            //    "Contact delete routine initiated for GUID " + contactGuid);

            // Step 1: Delete the contact from memory
            RemoveContactFromCollections(contactGuid);

            // Step 2: Delete the contact from the database
            RemoveContactFromDatabase(contactGuid);

            //Logger.Log(DateTime.Now + ":  " +
            //    CurrentUser + ": " +
            //    "Case-contact link delete query for " + contactGuid + " executed successfully. " + rows.ToString() + " deleted.");

            //UpdateDuality();

            SendMessage("Deleted contact", contactGuid, ServerUpdateType.DeleteContact);

            //RepopulateCollections(false);
        }

        public ICommand DeleteContactLink { get { return new RelayCommand<CaseContactPairViewModel>(DeleteContactLinkExecute); } }
        protected void DeleteContactLinkExecute(CaseContactPairViewModel caseContactPair)
        {
            if (CaseCollection == null || ContactCollection == null)
                return;

            string contactGuid = caseContactPair.ContactVM.RecordId;
            string caseGuid = caseContactPair.CaseVM.RecordId;
            
            Query deleteQuery = Database.CreateQuery("DELETE FROM [metaLinks] WHERE [ToRecordGuid] = @ToRecordGuid AND [FromRecordGuid] = @FromRecordGuid AND [ToViewId] = @ToViewId");
            deleteQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, contactGuid));
            deleteQuery.Parameters.Add(new QueryParameter("@FromRecordGuid", DbType.String, caseGuid));
            deleteQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, ContactFormId));
            Database.ExecuteNonQuery(deleteQuery);

            for (int i = ContactCollection.Count - 1; i >= 0; i--)
            {
                var contactVM = ContactCollection[i];
                if (contactVM.RecordId == contactGuid)
                {
                    //CurrentContactCollection.Remove(contactVM);
                    for (int j = CurrentContactLinkCollection.Count - 1; j >= 0; j--)
                    {
                        var ccp = CurrentContactLinkCollection[j];
                        if (ccp.ContactVM.RecordId == contactGuid)
                        {
                            //ContactLinkCollection.Remove(ccp);
                            lock (_contactCollectionLock)
                            {
                                CurrentContactLinkCollection.Remove(ccp);
                            }
                            break;
                        }
                    }
                    break;
                }
            }

            
            Query selectQuery = Database.CreateQuery("SELECT * FROM [metaLinks] WHERE [ToViewId] = @ToViewId AND [FromViewId] = @FromViewId AND [ToRecordGuid] = @ToRecordGuid ORDER BY [LastContactDate] DESC");
            selectQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, ContactFormId));
            selectQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, CaseFormId));
            selectQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, contactGuid));
            DataTable linksTable = Database.Select(selectQuery);

            foreach (DataRow row in linksTable.Rows)
            {
                string recordId = row["FromRecordGuid"].ToString();

                CaseViewModel iCase = GetCaseVM(recordId);
                ContactViewModel contactVM = GetContactVM(contactGuid);

                if (iCase != null && iCase.EpiCaseDef != EpiCaseClassification.NotCase)
                {
                    LoadContactLinkData(row, contactVM, iCase, WindowGenerationMode.Shift);
                    break;
                }
            }
            

            if (caseContactPair.CaseVM.Contacts.Contains(caseContactPair.ContactVM))
            {
                caseContactPair.CaseVM.Contacts.Remove(caseContactPair.ContactVM);
            }

            var Contactvm = GetContactVM(contactGuid);
            var query = from ccpVM in ContactLinkCollection
                        where ccpVM.ContactVM == caseContactPair.ContactVM || ccpVM.ContactVM.RecordId == caseContactPair.ContactVM.RecordId
                        orderby ccpVM.DateLastContact descending
                        select ccpVM;
            bool wasExposedByOtherCases = false;
            foreach (CaseContactPairViewModel ccpVM in query)
            {
                if (ccpVM.CaseVM.EpiCaseDef != Core.Enums.EpiCaseClassification.NotCase && ccpVM.CaseVM.Contacts.Contains(Contactvm))
                {
                    wasExposedByOtherCases = true;//finding source cases other than "Not a Case"
                    break;
                }
            }
            if (!wasExposedByOtherCases)
            {
                Contactvm.IsActive = false;
                Contactvm.FinalOutcome = "3";//If only source case is the "Non Case" while unlinking then inactivate the contact
                SetContactFinalStatus(Contactvm);
            }
            else
            {
                Contactvm.IsActive = true;
                Contactvm.FinalOutcome = string.Empty;
                SetContactFinalStatus(Contactvm);
            }

            ContactLinkCollection.Remove(caseContactPair);

            UpdateDateOfLastContact(Contactvm);

            SendMessageForUpdateCaseToContactRelationship(caseContactPair.CaseVM.RecordId);
        }

        public ICommand ShowContactsForCase { get { return new RelayCommand<CaseViewModel>(ShowContactsForCaseExecute); } }
        protected void ShowContactsForCaseExecute(CaseViewModel caseVM)
        {
            if (CaseCollection == null)
                return;

            UpdateDatesOfLastContactForSourceCaseAsync(caseVM);
            lock (_contactCollectionLock)
            {
                CurrentContactLinkCollection.Clear();

                List<ContactViewModel> tempContactlist = null;
                if (IsCountryUS)
                {
                    tempContactlist = caseVM.Contacts.OrderBy(con => con.ContactStateID).ToList();
                }
                else
                {
                    tempContactlist = caseVM.Contacts.OrderBy(contact => contact.UniqueKey).ThenBy(contact => contact.ContactID).ToList();
                }


                foreach (ContactViewModel contactVM in tempContactlist)
                {
                    //CurrentContactCollection.Add(contactVM);
                    foreach (CaseContactPairViewModel ccpVM in ContactLinkCollection.Where(x => x.ContactVM == contactVM && x.CaseVM == caseVM))
                    {
                        if (!CurrentContactLinkCollection.Contains(ccpVM))
                        {
                            CurrentContactLinkCollection.Add(ccpVM);
                        }
                    }
                }
            }
        }

        public ICommand ShowCasesForContact { get { return new RelayCommand<ContactViewModel>(ShowCasesForContactExecute); } }
        protected void ShowCasesForContactExecute(ContactViewModel contactVM)
        {
            if (ContactCollection == null || contactVM == null)
                return;

            List<CaseContactPairViewModel> tempCaseList = ContactLinkCollection.OrderBy(link => link.CaseVM.IDForSorting).ToList();

            lock (_caseCollectionLock) { CurrentCaseLinkCollection.Clear(); };

            foreach (CaseContactPairViewModel ccpVM in tempCaseList)
            {
                // WARNING: Sometimes this is null...??
                if (ccpVM.ContactVM.RecordId == contactVM.RecordId) // && ccpVM.CaseVM.Case.RecordId == caseVM.Case.RecordId)
                {
                    CaseViewModel caseVM = ccpVM.CaseVM;
                    lock (_caseCollectionLock) { CurrentCaseLinkCollection.Add(ccpVM); }
                }
            }
        }

        public ICommand ShowExposedCasesForCase { get { return new RelayCommand<CaseViewModel>(ShowExposedCasesForCaseExecute); } }
        protected void ShowExposedCasesForCaseExecute(CaseViewModel caseVM)
        {
            if (CaseCollection == null)
                return;

            CurrentExposureCollection.Clear();

            Query selectQuery = Database.CreateQuery("SELECT * FROM [metaLinks] WHERE [ToRecordGuid] = @ToRecordGuid AND [FromViewId] = @FromViewId AND [ToViewId] = @ToViewId");
            selectQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, caseVM.RecordId));
            selectQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, CaseFormId));
            selectQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, CaseFormId));
            DataTable dt = Database.Select(selectQuery);

            foreach (DataRow row in dt.Rows)
            {
                CaseExposurePairViewModel cepVM = new CaseExposurePairViewModel();
                cepVM.ExposedCaseVM = new CaseViewModel(CreateCaseFromGuid(row["FromRecordGuid"].ToString()));
                cepVM.SourceCaseVM = new CaseViewModel(CreateCaseFromGuid(row["ToRecordGuid"].ToString()));
                cepVM.Relationship = row["RelationshipType"].ToString();

                if (row["ContactType"] != DBNull.Value)
                {
                    cepVM.ContactType = Convert.ToInt32(row["ContactType"]);
                }
                if (row["LastContactDate"] != DBNull.Value)
                {
                    cepVM.DateLastContact = (DateTime)row["LastContactDate"];
                }

                switch (row["Tentative"].ToString())
                {
                    case "1":
                        cepVM.IsTentative = true;
                        break;
                    case "2":
                        cepVM.IsTentative = false;
                        break;
                }

                int count = (from caseExposurePair in CurrentExposureCollection
                             where caseExposurePair.SourceCaseVM.RecordId == cepVM.SourceCaseVM.RecordId &&
                             caseExposurePair.ExposedCaseVM.RecordId == cepVM.ExposedCaseVM.RecordId
                             select caseExposurePair).Count();

                if (count == 0)
                {
                    CurrentExposureCollection.Add(cepVM);
                }
            }
        }

        public ICommand ShowSourceCasesForCase { get { return new RelayCommand<CaseViewModel>(ShowSourceCasesForCaseExecute); } }
        protected void ShowSourceCasesForCaseExecute(CaseViewModel caseVM)
        {
            if (CaseCollection == null)
                return;

            lock (_caseCollectionLock)
            {
                CurrentSourceCaseCollection.Clear();
            }

            DataTable dt = new DataTable();

            Query selectQuery = Database.CreateQuery("SELECT * FROM [metaLinks] WHERE [FromRecordGuid] = @FromRecordGuid AND [FromViewId] = @FromViewId AND [ToViewId] = @ToViewId");
            selectQuery.Parameters.Add(new QueryParameter("@FromRecordGuid", DbType.String, caseVM.RecordId));
            selectQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, CaseFormId));
            selectQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, CaseFormId));
            dt = Database.Select(selectQuery);

            foreach (DataRow row in dt.Rows)
            {
                CaseExposurePairViewModel cepVM = new CaseExposurePairViewModel();
                cepVM.SourceCaseVM = new CaseViewModel(CreateCaseFromGuid(row["FromRecordGuid"].ToString()));

                if (!DoesCaseExist(row["ToRecordGuid"].ToString()))
                {
                    // this relationship doesn't exist, probably because the source case was deleted and old versions
                    // of the software didn't clean up the source case-to-case meta links records
                    continue;
                }
                else
                {
                    cepVM.ExposedCaseVM = new CaseViewModel(CreateCaseFromGuid(row["ToRecordGuid"].ToString()));
                    cepVM.ExposedCaseVM.UpdateIsNewRecord();
                    cepVM.ExposedCaseVM.SwitchToLegacyEnter += ExposedCaseVM_SwitchToLegacyEnter;

                    cepVM.Relationship = row["RelationshipType"].ToString();

                    if (row["ContactType"] != DBNull.Value)
                    {
                        cepVM.ContactType = Convert.ToInt32(row["ContactType"]);
                    }
                    if (row["LastContactDate"] != DBNull.Value)
                    {
                        cepVM.DateLastContact = (DateTime)row["LastContactDate"];
                    }

                    switch (row["Tentative"].ToString())
                    {
                        case "1":
                            cepVM.IsTentative = true;
                            break;
                        case "2":
                            cepVM.IsTentative = false;
                            break;
                    }

                    int count = (from caseExposurePair in CurrentSourceCaseCollection
                                 where caseExposurePair.SourceCaseVM.RecordId == cepVM.SourceCaseVM.RecordId &&
                                 caseExposurePair.ExposedCaseVM.RecordId == cepVM.ExposedCaseVM.RecordId
                                 select caseExposurePair).Count();

                    if (count == 0)
                    {
                        lock (_caseCollectionLock)
                        {
                            CurrentSourceCaseCollection.Add(cepVM);
                        }
                    }

                    SortCurrentSourceCaseCollection(CurrentSourceCaseCollection);
                }
            }
        }

        void ExposedCaseVM_SwitchToLegacyEnter(object sender, EventArgs e)
        {
            CaseViewModel c = sender as CaseViewModel;

            if (c != null && CaseSwitchToLegacyEnter != null)
            {
                CaseSwitchToLegacyEnter(c, new EventArgs());
            }
        }

        private void SortCurrentSourceCaseCollection(ObservableCollection<CaseExposurePairViewModel> CurrentSourceCaseCollection)
        {
            ObservableCollection<CaseExposurePairViewModel> tempCEPV = new ObservableCollection<CaseExposurePairViewModel>();

            foreach (CaseExposurePairViewModel dcVM in CurrentSourceCaseCollection.OrderBy(cas => cas.ExposedCaseVM.IDForSorting).ToList())
            {
                tempCEPV.Add(dcVM);
            }

            var query = from source in tempCEPV
                        //orderby dailyCheck.ContactVM.Village,/* dailyCheck.ContactVM.SubCounty, dailyCheck.ContactVM.District,*/ dailyCheck.ContactVM.Surname, dailyCheck.ContactVM.OtherNames
                        select source;

            CurrentSourceCaseCollection.Clear();

            foreach (var source in query)
            {
                CurrentSourceCaseCollection.Add(source);
            }
        }

        public ICommand ShowContactsForDate { get { return new RelayCommand<DateTime>(ShowContactsForDateExecute); } }
        protected void ShowContactsForDateExecute(DateTime dt)
        {
            if (CaseCollection == null || ContactCollection == null)
                return;

            Task.Factory.StartNew(
            () =>
            {
                IsLoadingProjectData = true;
                LoadStatus = "Initializing Contacts...";
                TaskbarProgressValue = 0;
                TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;

                PrevFollowUpCollection.Clear();

                double percentPerCase = 1.0 / CaseCollection.Count();

                foreach (CaseViewModel caseVM in CaseCollection)
                {
                    TaskbarProgressValue = TaskbarProgressValue + percentPerCase;
                    foreach (ContactViewModel contactVM in caseVM.Contacts)
                    {
                        CheckCaseContactForDailyFollowUp(caseVM, contactVM, dt);
                    }
                }

                SortFollowUps(PrevFollowUpCollection);
                SortFollowUps(DailyFollowUpCollection);
            },

            System.Threading.CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).ContinueWith(
            delegate
            {
                TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                IsLoadingProjectData = false;

            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public ICommand ShowContactsForDateforFollowup { get { return new RelayCommand<DateTime>(ShowContactsForDateExecuteforFollowup); } }
        protected void ShowContactsForDateExecuteforFollowup(DateTime dt)
        {
            if (CaseCollection == null || ContactCollection == null)
                return;

            PrevFollowUpCollection.Clear();

            TaskbarProgressValue = 0;
            SyncStatus = "Processing contacts...";
            double incrementConst = 1d / CaseCollection.Count;

            foreach (CaseViewModel caseVM in CaseCollection)
            {
                TaskbarProgressValue = TaskbarProgressValue + incrementConst;
                foreach (ContactViewModel contactVM in caseVM.Contacts)
                {
                    CheckCaseContactForDailyFollowUp(caseVM, contactVM, dt);
                }
            }

            SortFollowUps(PrevFollowUpCollection);
            SortFollowUps(DailyFollowUpCollection);
        }

        public ICommand ConvertCaseToContactWithNewSource { get { return new RelayCommand<CaseExposurePairViewModel>(ConvertCaseToContactWithNewSourceExecute); } }
        protected void ConvertCaseToContactWithNewSourceExecute(CaseExposurePairViewModel caseExposurePair)
        {
            if (CaseCollection == null)
                return;

            string newCaseGuid = caseExposurePair.ExposedCaseVM.RecordId;

            //Logger.Log(DateTime.Now + ":  " +
            //    CurrentUser + ": " +
            //    "Case-to-contact conversion (with new source case) requested for case with ID " + newCaseGuid);

            DateTime dtNow = DateTime.Now;
            dtNow = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, dtNow.Hour, dtNow.Minute, dtNow.Second, 0);
            //Check to see if the case is already converted to contact and deleted(Recstatus=0)
            Query selectQuery = null;

            selectQuery = Database.CreateQuery("SELECT * FROM [" + ContactForm.TableName + "]  WHERE GlobalRecordId = @ToRecordGuid and RECSTATUS >= 0 ");
            selectQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, newCaseGuid));
            DataTable contacttable = Database.Select(selectQuery);
            // Add case to the contact database
            if (contacttable.Rows.Count == 0)
            {
                Query insertQuery = Database.CreateQuery("INSERT INTO [" + ContactForm.TableName + "] (GlobalRecordId, RECSTATUS, FirstSaveTime, FirstSaveLogonName, LastSaveTime, LastSaveLogonName) VALUES (" +
                        "@GlobalRecordId, @RECSTATUS, @FirstSaveTime, @FirstSaveLogonName, @LastSaveTime, @LastSaveLogonName)");
                insertQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, newCaseGuid));
                insertQuery.Parameters.Add(new QueryParameter("@RECSTATUS", DbType.Byte, 1));
                insertQuery.Parameters.Add(new QueryParameter("@FirstSaveTime", DbType.DateTime, dtNow));
                insertQuery.Parameters.Add(new QueryParameter("@FirstSaveLogonName", DbType.String, CurrentUser));
                insertQuery.Parameters.Add(new QueryParameter("@LastSaveTime", DbType.DateTime, dtNow));
                insertQuery.Parameters.Add(new QueryParameter("@LastSaveLogonName", DbType.String, CurrentUser));
                Database.ExecuteNonQuery(insertQuery);

                foreach (Epi.Page page in ContactForm.Pages)
                {
                    insertQuery = Database.CreateQuery("INSERT INTO [" + page.TableName + "] (GlobalRecordId) VALUES (" +
                    "@GlobalRecordId)");
                    insertQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, newCaseGuid));
                    Database.ExecuteNonQuery(insertQuery);
                }
            }
            else
            {
                Query updatetQuery = Database.CreateQuery("Update [" + ContactForm.TableName + " set ] ( RECSTATUS=@RECSTATUS,  LastSaveTime=@LastSaveTime,  LastSaveLogonName = @LastSaveLogonName) Where GlobalRecordId= @GlobalRecordId");
                updatetQuery.Parameters.Add(new QueryParameter("@RECSTATUS", DbType.Byte, 1));
                updatetQuery.Parameters.Add(new QueryParameter("@LastSaveTime", DbType.DateTime, dtNow));
                updatetQuery.Parameters.Add(new QueryParameter("@LastSaveLogonName", DbType.String, CurrentUser));
                updatetQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, newCaseGuid));
                Database.ExecuteNonQuery(updatetQuery);
            }

            selectQuery = Database.CreateQuery("SELECT UniqueKey FROM [" + ContactForm.TableName + "] WHERE [GlobalRecordId] = @GlobalRecordId");
            selectQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, newCaseGuid));
            int uniqueKey = int.Parse(Database.ExecuteScalar(selectQuery).ToString());

            ContactViewModel newContact = new ContactViewModel(caseExposurePair.ExposedCaseVM, uniqueKey);
            newContact.FollowUpWindowViewModel = new FollowUpWindowViewModel(caseExposurePair.DateLastContact, newContact, caseExposurePair.SourceCaseVM);
            newContact.FirstSaveTime = DateTime.Now;

            Query updateQuery = null;
            if (IsCountryUS)
            {
                updateQuery = Database.CreateQuery("UPDATE [" + ContactForm.Pages[0].TableName + "] SET " +
            "[ContactSurname] = @Surname, " +
            "[ContactOtherNames] = @OtherNames, " +
            "[ContactAge] = @Age, " +
            "[ContactDOB] = @DOB, " +
            "[ContactAddress] = @Address, " +
            "[ContactZip] = @ContactZip, " +
            "[ContactEmail] = @ContactEmail, " +
            "[ContactHCW] = @ContactHCW, " +
            "[ContactHCWPosition] = @ContactHCWPosition, " +
            "[ContactHCWFacility] = @ContactHCWFacility, " +
            "[ContactHCWDistrict] = @ContactHCWDistrict, " +
            "[ContactHCWSC] = @ContactHCWSC, " +
            "[ContactHCWVillage] = @ContactHCWVillage, " +
            "[ContactGender] = @Gender, " +
            "[ContactPhone] = @PhoneNumber, " +
            "[ContactVillage] = @VillageRes, " +
            "[ContactSC] = @SCRes, " +
            "[ContactDistrict] = @DistrictRes " +
            "WHERE [GlobalRecordId] = @GlobalRecordId");
            }
            else
            {
                updateQuery = Database.CreateQuery("UPDATE [" + ContactForm.Pages[0].TableName + "] SET " +
            "[ContactSurname] = @Surname, " +
            "[ContactOtherNames] = @OtherNames, " +
            "[ContactAge] = @Age, " +
            "[ContactAgeUnit] = @AgeUnit, " +
            "[ContactGender] = @Gender, " +
            "[ContactPhone] = @PhoneNumber, " +
            "[ContactHeadHouse] = @HeadHouse, " +
            "[ContactParish] = @ParishRes, " +
            "[ContactVillage] = @VillageRes, " +
            "[ContactSC] = @SCRes, " +
            "[ContactDistrict] = @DistrictRes " +
            "WHERE [GlobalRecordId] = @GlobalRecordId");
            }



            updateQuery.Parameters.Add(new QueryParameter("@Surname", DbType.String, newContact.Surname));
            updateQuery.Parameters.Add(new QueryParameter("@OtherNames", DbType.String, newContact.OtherNames));
            if (newContact.Age.HasValue)
            {
                updateQuery.Parameters.Add(new QueryParameter("@Age", DbType.Double, newContact.Age));
            }
            else
            {
                updateQuery.Parameters.Add(new QueryParameter("@Age", DbType.Double, DBNull.Value));
            }

            string ageUnit = String.Empty;
            if (newContact.AgeUnit.HasValue)
            {
                switch (newContact.AgeUnit)
                {
                    case AgeUnits.Months:
                        ageUnit = Properties.Resources.AgeUnitMonths;
                        break;
                    case AgeUnits.Years:
                        ageUnit = Properties.Resources.AgeUnitYears;
                        break;
                }
            }

            if (IsCountryUS)
            {
                if (newContact.ContactDOB == null)
                {
                    updateQuery.Parameters.Add(new QueryParameter("@DOB", DbType.DateTime, DBNull.Value));
                }
                else
                {
                    updateQuery.Parameters.Add(new QueryParameter("@DOB", DbType.DateTime, newContact.ContactDOB));
                }
                updateQuery.Parameters.Add(new QueryParameter("@Address", DbType.String, newContact.ContactAddress));
                updateQuery.Parameters.Add(new QueryParameter("@ContactZip", DbType.String, newContact.ContactZip));
                updateQuery.Parameters.Add(new QueryParameter("@ContactEmail", DbType.String, newContact.ContactEmail));

                switch (newContact.HCW.ToLower())
                {
                    case "true":
                        newContact.HCW = "1";
                        break;
                    case "false":
                        newContact.HCW = "2";
                        break;
                }

                updateQuery.Parameters.Add(new QueryParameter("@ContactHCW", DbType.String, newContact.HCW));
                updateQuery.Parameters.Add(new QueryParameter("@ContactHCWPosition", DbType.String, newContact.ContactHCWPosition));
                updateQuery.Parameters.Add(new QueryParameter("@ContactHCWFacility", DbType.String, newContact.HCWFacility));
                updateQuery.Parameters.Add(new QueryParameter("@ContactHCWDistrict", DbType.String, newContact.ContactHCWDistrict));
                updateQuery.Parameters.Add(new QueryParameter("@ContactHCWSC", DbType.String, newContact.ContactHCWSC));
                updateQuery.Parameters.Add(new QueryParameter("@ContactHCWVillage", DbType.String, newContact.ContactHCWVillage));
            }
            else
            {
                updateQuery.Parameters.Add(new QueryParameter("@AgeUnit", DbType.String, ageUnit));
            }


            string genderValue = String.Empty;
            if (newContact.Gender.Equals(Properties.Resources.Male))
            {
                genderValue = "1";
            }
            else if (newContact.Gender.Equals(Properties.Resources.Female))
            {
                genderValue = "2";
            }
            updateQuery.Parameters.Add(new QueryParameter("@Gender", DbType.String, genderValue));

            updateQuery.Parameters.Add(new QueryParameter("@PhoneNumber", DbType.String, newContact.Phone));

            if (!IsCountryUS)
            {
                updateQuery.Parameters.Add(new QueryParameter("@HeadHouse", DbType.String, newContact.HeadOfHousehold));
                updateQuery.Parameters.Add(new QueryParameter("@ParishRes", DbType.String, newContact.Parish));
            }

            updateQuery.Parameters.Add(new QueryParameter("@VillageRes", DbType.String, newContact.Village));
            updateQuery.Parameters.Add(new QueryParameter("@SCRes", DbType.String, newContact.SubCounty));
            updateQuery.Parameters.Add(new QueryParameter("@DistrictRes", DbType.String, newContact.District));
            updateQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, newCaseGuid));
            Database.ExecuteNonQuery(updateQuery);

            Database.ExecuteNonQuery(caseExposurePair.GenerateInsertQuery(Database, ContactFormId, CaseFormId));

            selectQuery = Database.CreateQuery("SELECT * FROM [metaLinks] WHERE [ToViewId] = @ToViewId AND [FromViewId] = @FromViewId AND [ToRecordGuid] = @ToRecordGuid ORDER BY [LastContactDate] ASC");
            selectQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, ContactFormId));
            selectQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, CaseFormId));
            selectQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, newContact.RecordId));
            DataTable linksTable = Database.Select(selectQuery);

            foreach (CaseViewModel caseVM in CaseCollection)
            {
                // Load linked contacts
                foreach (DataRow row in linksTable.Rows)
                {
                    if (row["FromRecordGuid"].ToString() == caseVM.RecordId)
                    {
                        LoadContactFollowUps(row, newContact);
                    }
                }
            }

            lock (_contactCollectionLock)
            {
                ContactCollection.Add(newContact);
            }
            if (!caseExposurePair.SourceCaseVM.Contacts.Contains(newContact))
            {
                caseExposurePair.SourceCaseVM.Contacts.Add(newContact);
            }
            newContact.IsCase = true;
            caseExposurePair.ExposedCaseVM.IsContact = true;

            CaseContactPairViewModel ccpVM = new CaseContactPairViewModel();
            ccpVM.ContactVM = newContact;
            ccpVM.CaseVM = caseExposurePair.SourceCaseVM;

            //ContactLinkCollection.Add(ccpVM); // VHF-345 - link is already added later in LoadContactLinkData, so adding it here generates a duplicate of this contact with no date of last contact and other problems

            selectQuery = Database.CreateQuery("SELECT * FROM [metaLinks] WHERE [ToViewId] = @ToViewId AND [FromViewId] = @FromViewId AND [ToRecordGuid] = @ToRecordGuid ORDER BY [LastContactDate] DESC");
            selectQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, ContactFormId));
            selectQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, CaseFormId));
            selectQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, ccpVM.ContactVM.RecordId));
            linksTable = Database.Select(selectQuery);

            if (linksTable.Rows.Count > 0) // should never be 0
            {
                DataRow row = linksTable.Rows[0];
                LoadContactLinkData(row, ccpVM.ContactVM, ccpVM.CaseVM);
            }

            UpdateDateOfLastContact(newContact);
        }

        public ICommand ConvertCaseToContactWithExistingSources { get { return new RelayCommand<CaseViewModel>(ConvertCaseToContactWithExistingSourcesExecute); } }
        protected void ConvertCaseToContactWithExistingSourcesExecute(CaseViewModel caseVM)
        {
            if (CaseCollection == null)
                return;

            string newCaseGuid = caseVM.RecordId;

            DateTime dtNow = DateTime.Now;
            dtNow = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, dtNow.Hour, dtNow.Minute, dtNow.Second, 0);

            //Logger.Log(DateTime.Now + ":  " +
            //    CurrentUser + ": " +
            //    "Case-to-contact conversion (with existing source cases) requested for case with ID " + newCaseGuid);

            // Add case to the contact database
            Query selectQuery = null;
            Query insertQuery = null;
            selectQuery = Database.CreateQuery("SELECT * FROM [" + ContactForm.TableName + "]  WHERE GlobalRecordId = @ToRecordGuid  and RECSTATUS=0 ");
            selectQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, newCaseGuid));
            DataTable contacttable = Database.Select(selectQuery);
            if (contacttable.Rows.Count == 0)
            {
                insertQuery = Database.CreateQuery("INSERT INTO [" + ContactForm.TableName + "] (GlobalRecordId, RECSTATUS, FirstSaveTime, FirstSaveLogonName, LastSaveTime, LastSaveLogonName) VALUES (" +
                    "@GlobalRecordId, @RECSTATUS, @FirstSaveTime, @FirstSaveLogonName, @LastSaveTime, @LastSaveLogonName)");
                insertQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, newCaseGuid));
                insertQuery.Parameters.Add(new QueryParameter("@RECSTATUS", DbType.Byte, 1));
                insertQuery.Parameters.Add(new QueryParameter("@FirstSaveTime", DbType.DateTime, dtNow));
                insertQuery.Parameters.Add(new QueryParameter("@FirstSaveLogonName", DbType.String, CurrentUser));
                insertQuery.Parameters.Add(new QueryParameter("@LastSaveTime", DbType.DateTime, dtNow));
                insertQuery.Parameters.Add(new QueryParameter("@LastSaveLogonName", DbType.String, CurrentUser));
                Database.ExecuteNonQuery(insertQuery);

                foreach (Epi.Page page in ContactForm.Pages)
                {
                    insertQuery = Database.CreateQuery("INSERT INTO [" + page.TableName + "] (GlobalRecordId) VALUES (" +
                    "@GlobalRecordId)");
                    insertQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, newCaseGuid));
                    Database.ExecuteNonQuery(insertQuery);
                }
            }
            else
            {
                Query updatetQuery = Database.CreateQuery("Update [" + ContactForm.TableName + " set ] ( RECSTATUS=@RECSTATUS,  LastSaveTime=@LastSaveTime,  LastSaveLogonName = @LastSaveLogonName) Where GlobalRecordId= @GlobalRecordId");
                updatetQuery.Parameters.Add(new QueryParameter("@RECSTATUS", DbType.Byte, 1));
                updatetQuery.Parameters.Add(new QueryParameter("@LastSaveTime", DbType.DateTime, dtNow));
                updatetQuery.Parameters.Add(new QueryParameter("@LastSaveLogonName", DbType.String, CurrentUser));
                updatetQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, newCaseGuid));
                Database.ExecuteNonQuery(updatetQuery);
            }
            selectQuery = Database.CreateQuery("SELECT UniqueKey FROM [" + ContactForm.TableName + "] WHERE [GlobalRecordId] = @GlobalRecordId");
            selectQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, newCaseGuid));
            int uniqueKey = int.Parse(Database.ExecuteScalar(selectQuery).ToString());

            ContactViewModel newContact = new ContactViewModel(caseVM, uniqueKey);
            newContact.FirstSaveTime = DateTime.Now;

            Query updateQuery = null;

            if (IsCountryUS)
            {
                updateQuery = Database.CreateQuery("UPDATE [" + ContactForm.Pages[0].TableName + "] SET " +
            "[ContactSurname] = @Surname, " +
            "[ContactOtherNames] = @OtherNames, " +
            "[ContactAge] = @Age, " +
            "[ContactDOB] = @DOB, " +
            "[ContactAddress] = @Address, " +
            "[ContactZip] = @ContactZip, " +
            "[ContactEmail] = @ContactEmail, " +
            "[ContactHCW] = @ContactHCW, " +
            "[ContactHCWPosition] = @ContactHCWPosition, " +
            "[ContactHCWFacility] = @ContactHCWFacility, " +
            "[ContactHCWDistrict] = @ContactHCWDistrict, " +
            "[ContactHCWSC] = @ContactHCWSC, " +
            "[ContactHCWVillage] = @ContactHCWVillage, " +
            "[ContactGender] = @Gender, " +
            "[ContactPhone] = @PhoneNumber, " +
            "[ContactVillage] = @VillageRes, " +
            "[ContactSC] = @SCRes, " +
            "[ContactDistrict] = @DistrictRes " +
            "WHERE [GlobalRecordId] = @GlobalRecordId");
            }
            else
            {
                updateQuery = Database.CreateQuery("UPDATE [" + ContactForm.Pages[0].TableName + "] SET " +
                            "[ContactSurname] = @Surname, " +
                            "[ContactOtherNames] = @OtherNames, " +
                            "[ContactAge] = @Age, " +
                            "[ContactAgeUnit] = @AgeUnit, " +
                            "[ContactGender] = @Gender, " +
                            "[ContactPhone] = @PhoneNumber, " +
                            "[ContactHeadHouse] = @HeadHouse, " +
                            "[ContactParish] = @ParishRes, " +
                            "[ContactVillage] = @VillageRes, " +
                            "[ContactSC] = @SCRes, " +
                            "[ContactDistrict] = @DistrictRes " +
                            "WHERE [GlobalRecordId] = @GlobalRecordId");
            }

            updateQuery.Parameters.Add(new QueryParameter("@Surname", DbType.String, newContact.Surname));
            updateQuery.Parameters.Add(new QueryParameter("@OtherNames", DbType.String, newContact.OtherNames));
            if (newContact.Age.HasValue)
            {
                updateQuery.Parameters.Add(new QueryParameter("@Age", DbType.Double, newContact.Age));
            }
            else
            {
                updateQuery.Parameters.Add(new QueryParameter("@Age", DbType.Double, DBNull.Value));
            }

            string ageUnit = String.Empty;
            if (newContact.AgeUnit.HasValue)
            {
                switch (newContact.AgeUnit)
                {
                    case AgeUnits.Months:
                        ageUnit = Properties.Resources.AgeUnitMonths;
                        break;
                    case AgeUnits.Years:
                        ageUnit = Properties.Resources.AgeUnitYears;
                        break;
                }
            }

            if (IsCountryUS)
            {
                if (newContact.ContactDOB == null)
                {
                    updateQuery.Parameters.Add(new QueryParameter("@DOB", DbType.DateTime, DBNull.Value));
                }
                else
                {
                    updateQuery.Parameters.Add(new QueryParameter("@DOB", DbType.DateTime, newContact.ContactDOB));
                }
                updateQuery.Parameters.Add(new QueryParameter("@Address", DbType.String, newContact.ContactAddress));
                updateQuery.Parameters.Add(new QueryParameter("@ContactZip", DbType.String, newContact.ContactZip));
                updateQuery.Parameters.Add(new QueryParameter("@ContactEmail", DbType.String, newContact.ContactEmail));

                switch (newContact.HCW.ToLower())
                {
                    case "true":
                        newContact.HCW = "1";
                        break;
                    case "false":
                        newContact.HCW = "2";
                        break;
                }

                updateQuery.Parameters.Add(new QueryParameter("@ContactHCW", DbType.String, newContact.HCW));
                updateQuery.Parameters.Add(new QueryParameter("@ContactHCWPosition", DbType.String, newContact.ContactHCWPosition));
                updateQuery.Parameters.Add(new QueryParameter("@ContactHCWFacility", DbType.String, newContact.HCWFacility));
                updateQuery.Parameters.Add(new QueryParameter("@ContactHCWDistrict", DbType.String, newContact.ContactHCWDistrict));
                updateQuery.Parameters.Add(new QueryParameter("@ContactHCWSC", DbType.String, newContact.ContactHCWSC));
                updateQuery.Parameters.Add(new QueryParameter("@ContactHCWVillage", DbType.String, newContact.ContactHCWVillage));
            }
            else
            {
                updateQuery.Parameters.Add(new QueryParameter("@AgeUnit", DbType.String, ageUnit));
            }

            string genderValue = String.Empty;
            if (newContact.Gender.Equals(Properties.Resources.Male))
            {
                genderValue = "1";
            }
            else if (newContact.Gender.Equals(Properties.Resources.Female))
            {
                genderValue = "2";
            }
            updateQuery.Parameters.Add(new QueryParameter("@Gender", DbType.String, genderValue));
            updateQuery.Parameters.Add(new QueryParameter("@PhoneNumber", DbType.String, newContact.Phone));

            if (!IsCountryUS)
            {
                updateQuery.Parameters.Add(new QueryParameter("@HeadHouse", DbType.String, newContact.HeadOfHousehold));
                updateQuery.Parameters.Add(new QueryParameter("@ParishRes", DbType.String, newContact.Parish));
            }

            updateQuery.Parameters.Add(new QueryParameter("@VillageRes", DbType.String, newContact.Village));
            updateQuery.Parameters.Add(new QueryParameter("@SCRes", DbType.String, newContact.SubCounty));
            updateQuery.Parameters.Add(new QueryParameter("@DistrictRes", DbType.String, newContact.District));
            updateQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, newCaseGuid));
            Database.ExecuteNonQuery(updateQuery);

            selectQuery = Database.CreateQuery("SELECT * FROM [metaLinks] WHERE [FromRecordGuid] = @FromRecordGuid AND [FromViewId] = @FromViewId AND [ToViewId] = @ToViewId");
            selectQuery.Parameters.Add(new QueryParameter("@FromRecordGuid", DbType.String, caseVM.RecordId));
            selectQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, CaseFormId));
            selectQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, CaseFormId));
            DataTable dt = Database.Select(selectQuery);

            int existingSourcesConverted = 0;
            foreach (DataRow row in dt.Rows)
            {
                CaseExposurePairViewModel cepVM = new CaseExposurePairViewModel();
                cepVM.SourceCaseVM = new CaseViewModel(CreateCaseFromGuid(row["FromRecordGuid"].ToString()));
                cepVM.ExposedCaseVM = new CaseViewModel(CreateCaseFromGuid(row["ToRecordGuid"].ToString()));
                cepVM.Relationship = row["RelationshipType"].ToString();

                if (row["ContactType"] != DBNull.Value)
                {
                    cepVM.ContactType = Convert.ToInt32(row["ContactType"]);
                }
                if (row["LastContactDate"] != DBNull.Value)
                {
                    cepVM.DateLastContact = (DateTime)row["LastContactDate"];
                }

                switch (row["Tentative"].ToString())
                {
                    case "1":
                        cepVM.IsTentative = true;
                        break;
                    case "2":
                        cepVM.IsTentative = false;
                        break;
                }

                int rows = Database.ExecuteNonQuery(cepVM.GenerateInsertQueryForConversion(Database, ContactFormId, CaseFormId));
                existingSourcesConverted += rows;
                newContact.IsCase = true;
                caseVM.IsContact = true;
            }

            //Logger.Log(DateTime.Now + ":  " +
            //    CurrentUser + ": " +
            //    "Case-to-contact conversion (with new existing source cases) completed; " + existingSourcesConverted.ToString() + 
            //    " source cases brought over.");

            lock (_contactCollectionLock)
            {
                ContactCollection.Add(newContact);
            }

            RepopulateCollections();
        }

        public ICommand SearchCases { get { return new RelayCommand(SearchCasesExecute); } }
        private void SearchCasesExecute()
        {
            try
            {
                string searchString = SearchCasesText.Trim().ToLower();

                if (!String.IsNullOrEmpty(searchString))
                {

                    Converters.HCWConverter HCWConverter = new Converters.HCWConverter();
                    Converters.GenderConverter GenderConverter = new Converters.GenderConverter();
                    Converters.DateConverter DateConverter = new Converters.DateConverter();
                    Converters.EpiCaseClassificationConverter EpiCaseClassificationConverter = new Converters.EpiCaseClassificationConverter();
                    Converters.FinalLabClassificationConverter FinalLabClassificationConverter = new Converters.FinalLabClassificationConverter();
                    string dateFormat = string.Empty;
                    dateFormat = "{0:" + DateConverter.GetDateFormat(this.ApplicationCulture) + "}";

                    if (IsCountryUS)
                    {
                        CaseCollectionView.Filter = new Predicate<object>
                        (
                            // todo: Add extension method to string to better handle case-insensitive Contains() 
                            caseVM =>
                                EpiCaseClassificationConverter.Convert(((CaseViewModel)caseVM).EpiCaseDef, null, null, null).ToString().ToLower().Contains(searchString) ||
                                GenderConverter.Convert(((CaseViewModel)caseVM).Gender, null, null, null).ToString().ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).IsHCW != null && HCWConverter.Convert(((CaseViewModel)caseVM).IsHCW, null, null, null).ToString().ToLower().Contains(searchString) ||
                               FinalLabClassificationConverter.Convert(((CaseViewModel)caseVM).FinalLabClass, null, null, null).ToString().ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).OriginalID.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).Age.ToString().ToLower().Contains(searchString) ||
                                //((CaseViewModel)caseVM).DateDeath.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).Citizenship.ToString().ToLower().Contains(searchString) ||

                                ((CaseViewModel)caseVM).ID.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).Surname.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).OtherNames.ToLower().Contains(searchString) ||
                                //((CaseViewModel)caseVM).FinalLabClass.ToLower().Contains(searchString) ||
                                //((CaseViewModel)caseVM).Gender.ToLower().Contains(searchString) ||
                                (((CaseViewModel)caseVM).DateOnset.HasValue && String.Format(dateFormat, ((CaseViewModel)caseVM).DateOnset).Contains(searchString)) ||
                                (((CaseViewModel)caseVM).DateDeath.HasValue && String.Format(dateFormat, ((CaseViewModel)caseVM).DateDeath).Contains(searchString)) ||
                                (((CaseViewModel)caseVM).DateDeath2.HasValue && String.Format(dateFormat, ((CaseViewModel)caseVM).DateDeath2).Contains(searchString)) ||
                                (((CaseViewModel)caseVM).DateIsolationCurrent.HasValue && String.Format(dateFormat, ((CaseViewModel)caseVM).DateIsolationCurrent).Contains(searchString)) ||
                                (((CaseViewModel)caseVM).DateDischargeIso.HasValue && String.Format(dateFormat, ((CaseViewModel)caseVM).DateDischargeIso).Contains(searchString)) ||
                                ((CaseViewModel)caseVM).CurrentStatus.ToLower().Contains(searchString) ||
                                //((CaseViewModel)caseVM).EpiCaseClassification.Contains((new Converters.EpiCaseClassificationConverter()).Convert(searchString, null, true, null).ToString().ToLower()) ||
                                ((CaseViewModel)caseVM).Village.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).District.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).SubCounty.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).DistrictOnset.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).Country.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).CountryOnset.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).RecordStatus.ToLower().Contains(searchString)
                        );
                    }
                    else
                    {
                        CaseCollectionView.Filter = new Predicate<object>
                        (
                            // todo: Add extension method to string to better handle case-insensitive Contains() 
                            caseVM =>
                                EpiCaseClassificationConverter.Convert(((CaseViewModel)caseVM).EpiCaseDef, null, null, null).ToString().ToLower().Contains(searchString) ||
                                GenderConverter.Convert(((CaseViewModel)caseVM).Gender, null, null, null).ToString().ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).IsHCW != null && HCWConverter.Convert(((CaseViewModel)caseVM).IsHCW, null, null, null).ToString().ToLower().Contains(searchString) ||
                               FinalLabClassificationConverter.Convert(((CaseViewModel)caseVM).FinalLabClass, null, null, null).ToString().ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).Age.ToString().ToLower().Contains(searchString) ||
                                //((CaseViewModel)caseVM).DateDeath.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).ID.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).Surname.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).OtherNames.ToLower().Contains(searchString) ||
                                //((CaseViewModel)caseVM).FinalLabClass.ToLower().Contains(searchString) ||
                                //((CaseViewModel)caseVM).Gender.ToLower().Contains(searchString) ||
                                (((CaseViewModel)caseVM).DateOnset.HasValue && String.Format(dateFormat, ((CaseViewModel)caseVM).DateOnset).Contains(searchString)) ||
                                (((CaseViewModel)caseVM).DateDeath.HasValue && String.Format(dateFormat, ((CaseViewModel)caseVM).DateDeath).Contains(searchString)) ||
                                (((CaseViewModel)caseVM).DateDeath2.HasValue && String.Format(dateFormat, ((CaseViewModel)caseVM).DateDeath2).Contains(searchString)) ||
                                (((CaseViewModel)caseVM).DateIsolationCurrent.HasValue && String.Format(dateFormat, ((CaseViewModel)caseVM).DateIsolationCurrent).Contains(searchString)) ||
                                (((CaseViewModel)caseVM).DateDischargeIso.HasValue && String.Format(dateFormat, ((CaseViewModel)caseVM).DateDischargeIso).Contains(searchString)) ||
                                ((CaseViewModel)caseVM).CurrentStatus.ToLower().Contains(searchString) ||
                                //((CaseViewModel)caseVM).EpiCaseClassification.Contains((new Converters.EpiCaseClassificationConverter()).Convert(searchString, null, true, null).ToString().ToLower()) ||
                                ((CaseViewModel)caseVM).Village.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).Parish.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).District.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).SubCounty.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).DistrictOnset.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).Country.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).CountryOnset.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).RecordStatus.ToLower().Contains(searchString)
                        );
                    }


                }
                else
                {
                    CaseCollectionView.Filter = null;
                }
            }
            catch (Exception)
            {
                //Epi.Logger.Log(DateTime.Now + ":  " +
                //CurrentUser + ": " +
                //"Searching for " + SearchCasesText + " in cases list generated exception with message: " + ex.Message);
                CaseCollectionView.Filter = null;
                SearchCasesText = String.Empty;
            }
        }

        public ICommand SearchExistingCases { get { return new RelayCommand(SearchExistingCasesExecute); } }
        private void SearchExistingCasesExecute()
        {

            string searchString = SearchExistingCasesText.Trim().ToLower();
            Converters.GenderConverter GenderConverter = new Converters.GenderConverter();
            Converters.DateConverter DateConverter = new Converters.DateConverter();
            Converters.EpiCaseClassificationConverter EpiCaseClassificationConverter = new Converters.EpiCaseClassificationConverter();
            Converters.FinalLabClassificationConverter FinalLabClassificationConverter = new Converters.FinalLabClassificationConverter();
            string dateFormat = string.Empty;
            dateFormat = "{0:" + DateConverter.GetDateFormat(this.ApplicationCulture) + "}";

            if (!String.IsNullOrEmpty(searchString))
            {
                Converters.HCWConverter HCWConverter = new Converters.HCWConverter();
                if (IsCountryUS)
                {
                    ExistingCaseCollectionView.Filter = new Predicate<object>
                   (
                       caseVM =>
                            EpiCaseClassificationConverter.Convert(((CaseViewModel)caseVM).EpiCaseDef, null, null, null).ToString().ToLower().Contains(searchString) ||
                                GenderConverter.Convert(((CaseViewModel)caseVM).Gender, null, null, null).ToString().ToLower().Contains(searchString) ||
                               ((CaseViewModel)caseVM).IsHCW != null && HCWConverter.Convert(((CaseViewModel)caseVM).IsHCW, null, null, null).ToString().ToLower().Contains(searchString) ||
                               FinalLabClassificationConverter.Convert(((CaseViewModel)caseVM).FinalLabClass, null, null, null).ToString().ToLower().Contains(searchString) ||
                              ((CaseViewModel)caseVM).OriginalID.ToLower().Contains(searchString) ||
                               ((CaseViewModel)caseVM).Age.ToString().ToLower().Contains(searchString) ||
                           //((CaseViewModel)caseVM).DateDeath.ToLower().Contains(searchString) ||
                               ((CaseViewModel)caseVM).Citizenship.ToString().ToLower().Contains(searchString) ||
                               (((CaseViewModel)caseVM).DateOnset.HasValue && String.Format(dateFormat, ((CaseViewModel)caseVM).DateOnset).Contains(searchString)) ||
                                (((CaseViewModel)caseVM).DateDeath.HasValue && String.Format(dateFormat, ((CaseViewModel)caseVM).DateDeath).Contains(searchString)) ||
                                (((CaseViewModel)caseVM).DateDeath2.HasValue && String.Format(dateFormat, ((CaseViewModel)caseVM).DateDeath2).Contains(searchString)) ||
                                (((CaseViewModel)caseVM).DateIsolationCurrent.HasValue && String.Format(dateFormat, ((CaseViewModel)caseVM).DateIsolationCurrent).Contains(searchString)) ||
                                (((CaseViewModel)caseVM).DateDischargeIso.HasValue && String.Format(dateFormat, ((CaseViewModel)caseVM).DateDischargeIso).Contains(searchString)) ||

                           ((CaseViewModel)caseVM).ID.ToLower().Contains(searchString) ||
                           ((CaseViewModel)caseVM).Surname.ToLower().Contains(searchString) ||
                           ((CaseViewModel)caseVM).OtherNames.ToLower().Contains(searchString) ||
                           //((CaseViewModel)caseVM).FinalLabClass.ToLower().Contains(searchString) ||
                           //((CaseViewModel)caseVM).Gender.ToLower().Contains(searchString) ||
                           ((CaseViewModel)caseVM).CurrentStatus.ToLower().Contains(searchString) ||
                           //((CaseViewModel)caseVM).EpiCaseDef.ToLower().Contains((new Converters.EpiCaseClassificationConverter()).Convert(searchString, null, true, null).ToString().ToLower()) ||
                           ((CaseViewModel)caseVM).Village.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).SubCounty.ToLower().Contains(searchString) ||
                           ((CaseViewModel)caseVM).District.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).RecordStatus.ToLower().Contains(searchString)
                   );
                }
                else
                {
                    ExistingCaseCollectionView.Filter = new Predicate<object>
                   (
                       caseVM =>
                            EpiCaseClassificationConverter.Convert(((CaseViewModel)caseVM).EpiCaseDef, null, null, null).ToString().ToLower().Contains(searchString) ||
                                GenderConverter.Convert(((CaseViewModel)caseVM).Gender, null, null, null).ToString().ToLower().Contains(searchString) ||
                               ((CaseViewModel)caseVM).IsHCW != null && HCWConverter.Convert(((CaseViewModel)caseVM).IsHCW, null, null, null).ToString().ToLower().Contains(searchString) ||
                               FinalLabClassificationConverter.Convert(((CaseViewModel)caseVM).FinalLabClass, null, null, null).ToString().ToLower().Contains(searchString) ||
                              ((CaseViewModel)caseVM).Age.ToString().ToLower().Contains(searchString) ||
                           //((CaseViewModel)caseVM).DateDeath.ToLower().Contains(searchString) ||
                               (((CaseViewModel)caseVM).DateOnset.HasValue && String.Format(dateFormat, ((CaseViewModel)caseVM).DateOnset).Contains(searchString)) ||
                                (((CaseViewModel)caseVM).DateDeath.HasValue && String.Format(dateFormat, ((CaseViewModel)caseVM).DateDeath).Contains(searchString)) ||
                                (((CaseViewModel)caseVM).DateDeath2.HasValue && String.Format(dateFormat, ((CaseViewModel)caseVM).DateDeath2).Contains(searchString)) ||
                                (((CaseViewModel)caseVM).DateIsolationCurrent.HasValue && String.Format(dateFormat, ((CaseViewModel)caseVM).DateIsolationCurrent).Contains(searchString)) ||
                                (((CaseViewModel)caseVM).DateDischargeIso.HasValue && String.Format(dateFormat, ((CaseViewModel)caseVM).DateDischargeIso).Contains(searchString)) ||

                           ((CaseViewModel)caseVM).ID.ToLower().Contains(searchString) ||
                           ((CaseViewModel)caseVM).Surname.ToLower().Contains(searchString) ||
                           ((CaseViewModel)caseVM).OtherNames.ToLower().Contains(searchString) ||
                           //((CaseViewModel)caseVM).FinalLabClass.ToLower().Contains(searchString) ||
                           //((CaseViewModel)caseVM).Gender.ToLower().Contains(searchString) ||
                           //(((CaseViewModel)caseVM).AgeUnit.HasValue && ((CaseViewModel)caseVM).AgeYears.HasValue && ((CaseViewModel)caseVM).AgeYears.Value.ToString().Equals(searchString)) ||
                           ((CaseViewModel)caseVM).CurrentStatus.ToLower().Contains(searchString) ||
                           //((CaseViewModel)caseVM).EpiCaseDef.ToLower().Contains((new Converters.EpiCaseClassificationConverter()).Convert(searchString, null, true, null).ToString().ToLower()) ||
                           ((CaseViewModel)caseVM).Parish.ToLower().Contains(searchString) ||
                           ((CaseViewModel)caseVM).Village.ToLower().Contains(searchString) ||
                           ((CaseViewModel)caseVM).District.ToLower().Contains(searchString) ||
                           ((CaseViewModel)caseVM).SubCounty.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).CountryOnset.ToLower().Contains(searchString)
                   );
                }
            }
            else
            {
                ExistingCaseCollectionView.Filter = null;
            }
        }

        public ICommand SearchIsoCases { get { return new RelayCommand(SearchIsoCasesExecute); } }
        private void SearchIsoCasesExecute()
        {
            string searchString = SearchIsoCasesText.Trim().ToLower();

            Converters.HCWConverter HCWConverter = new Converters.HCWConverter();
            Converters.GenderConverter GenderConverter = new Converters.GenderConverter();
            Converters.DateConverter DateConverter = new Converters.DateConverter();
            Converters.EpiCaseClassificationConverter EpiCaseClassificationConverter = new Converters.EpiCaseClassificationConverter();
            Converters.FinalLabClassificationConverter FinalLabClassificationConverter = new Converters.FinalLabClassificationConverter();

            string dateFormat = string.Empty;
            dateFormat = "{0:" + DateConverter.GetDateFormat(this.ApplicationCulture) + "}";
            if (!String.IsNullOrEmpty(searchString))
            {
                if (IsCountryUS)
                {
                    IsolatedCollectionView.Filter = new Predicate<object>
                     (
                         caseVM =>
                             (
                                 (
                     (
                     (((CaseViewModel)caseVM).IsolationCurrent != null &&
                     ((CaseViewModel)caseVM).IsolationCurrent.Equals("1")) ||
                     (((CaseViewModel)caseVM).DateIsolationCurrent.HasValue)
                     ) &&
                     !((CaseViewModel)caseVM).DateDeath.HasValue &&
                     !((CaseViewModel)caseVM).DateDeath2.HasValue &&
                     !((CaseViewModel)caseVM).DateDischargeIso.HasValue &&
                     !((CaseViewModel)caseVM).DateDischargeHospital.HasValue &&
                     !((CaseViewModel)caseVM).CurrentStatus.Equals(Properties.Resources.Dead) &&
                     !((CaseViewModel)caseVM).FinalStatus.Equals(Core.Enums.AliveDead.Dead)
                     )
                             ) &&
                             (
                             EpiCaseClassificationConverter.Convert(((CaseViewModel)caseVM).EpiCaseDef, null, null, null).ToString().ToLower().Contains(searchString) ||
                                GenderConverter.Convert(((CaseViewModel)caseVM).Gender, null, null, null).ToString().ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).IsHCW != null && HCWConverter.Convert(((CaseViewModel)caseVM).IsHCW, null, null, null).ToString().ToLower().Contains(searchString) ||
                               FinalLabClassificationConverter.Convert(((CaseViewModel)caseVM).FinalLabClass, null, null, null).ToString().ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).OriginalID.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).Age.ToString().ToLower().Contains(searchString) ||
                             //((CaseViewModel)caseVM).DateDeath.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).Citizenship.ToString().ToLower().Contains(searchString) ||

                                ((CaseViewModel)caseVM).ID.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).Surname.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).OtherNames.ToLower().Contains(searchString) ||
                             //((CaseViewModel)caseVM).FinalLabClass.ToLower().Contains(searchString) ||
                             //((CaseViewModel)caseVM).Gender.ToLower().Contains(searchString) ||
                                (((CaseViewModel)caseVM).DateOnset.HasValue && String.Format(dateFormat, ((CaseViewModel)caseVM).DateOnset).Contains(searchString)) ||
                                (((CaseViewModel)caseVM).DateDeath.HasValue && String.Format(dateFormat, ((CaseViewModel)caseVM).DateDeath).Contains(searchString)) ||
                                (((CaseViewModel)caseVM).DateDeath2.HasValue && String.Format(dateFormat, ((CaseViewModel)caseVM).DateDeath2).Contains(searchString)) ||
                                (((CaseViewModel)caseVM).DateIsolationCurrent.HasValue && String.Format(dateFormat, ((CaseViewModel)caseVM).DateIsolationCurrent).Contains(searchString)) ||
                                (((CaseViewModel)caseVM).DateDischargeIso.HasValue && String.Format(dateFormat, ((CaseViewModel)caseVM).DateDischargeIso).Contains(searchString)) ||
                                ((CaseViewModel)caseVM).CurrentStatus.ToLower().Contains(searchString) ||
                             //((CaseViewModel)caseVM).EpiCaseClassification.Contains((new Converters.EpiCaseClassificationConverter()).Convert(searchString, null, true, null).ToString().ToLower()) ||
                                ((CaseViewModel)caseVM).Village.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).District.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).SubCounty.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).DistrictOnset.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).Country.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).CountryOnset.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).RecordStatus.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).LastSamplePCRResult.ToLower().Contains(searchString) ||
                                (((CaseViewModel)caseVM).DateLastLabSampleCollected.HasValue && String.Format(dateFormat, ((CaseViewModel)caseVM).DateLastLabSampleCollected).Contains(searchString)) ||
                                ((CaseViewModel)caseVM).CurrentHospital.ToLower().Contains(searchString)
                             )
                     );
                }
                else
                {
                    IsolatedCollectionView.Filter = new Predicate<object>
                    (
                        caseVM =>
                            (
                                (
                    (
                    (((CaseViewModel)caseVM).IsolationCurrent != null &&
                    ((CaseViewModel)caseVM).IsolationCurrent.Equals("1")) ||
                    (((CaseViewModel)caseVM).DateIsolationCurrent.HasValue)
                    ) &&
                    !((CaseViewModel)caseVM).DateDeath.HasValue &&
                    !((CaseViewModel)caseVM).DateDeath2.HasValue &&
                    !((CaseViewModel)caseVM).DateDischargeIso.HasValue &&
                    !((CaseViewModel)caseVM).DateDischargeHospital.HasValue &&
                    !((CaseViewModel)caseVM).CurrentStatus.Equals(Properties.Resources.Dead) &&
                    !((CaseViewModel)caseVM).FinalStatus.Equals(Core.Enums.AliveDead.Dead)
                    )
                            ) &&
                            (
                                EpiCaseClassificationConverter.Convert(((CaseViewModel)caseVM).EpiCaseDef, null, null, null).ToString().ToLower().Contains(searchString) ||
                                GenderConverter.Convert(((CaseViewModel)caseVM).Gender, null, null, null).ToString().ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).IsHCW != null && HCWConverter.Convert(((CaseViewModel)caseVM).IsHCW, null, null, null).ToString().ToLower().Contains(searchString) ||
                               FinalLabClassificationConverter.Convert(((CaseViewModel)caseVM).FinalLabClass, null, null, null).ToString().ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).Age.ToString().ToLower().Contains(searchString) ||
                            //((CaseViewModel)caseVM).DateDeath.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).ID.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).OriginalID.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).Surname.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).OtherNames.ToLower().Contains(searchString) ||
                            //((CaseViewModel)caseVM).FinalLabClass.ToLower().Contains(searchString) ||
                            //((CaseViewModel)caseVM).Gender.ToLower().Contains(searchString) ||
                                (((CaseViewModel)caseVM).DateOnset.HasValue && String.Format(dateFormat, ((CaseViewModel)caseVM).DateOnset).Contains(searchString)) ||
                                (((CaseViewModel)caseVM).DateDeath.HasValue && String.Format(dateFormat, ((CaseViewModel)caseVM).DateDeath).Contains(searchString)) ||
                                (((CaseViewModel)caseVM).DateDeath2.HasValue && String.Format(dateFormat, ((CaseViewModel)caseVM).DateDeath2).Contains(searchString)) ||
                                (((CaseViewModel)caseVM).DateIsolationCurrent.HasValue && String.Format(dateFormat, ((CaseViewModel)caseVM).DateIsolationCurrent).Contains(searchString)) ||
                                (((CaseViewModel)caseVM).DateDischargeIso.HasValue && String.Format(dateFormat, ((CaseViewModel)caseVM).DateDischargeIso).Contains(searchString)) ||
                                (((CaseViewModel)caseVM).DateLastLabSampleCollected.HasValue && String.Format(dateFormat, ((CaseViewModel)caseVM).DateLastLabSampleCollected).Contains(searchString)) ||
                                ((CaseViewModel)caseVM).CurrentStatus.ToLower().Contains(searchString) ||
                            //((CaseViewModel)caseVM).EpiCaseClassification.Contains((new Converters.EpiCaseClassificationConverter()).Convert(searchString, null, true, null).ToString().ToLower()) ||
                                ((CaseViewModel)caseVM).Village.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).District.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).SubCounty.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).DistrictOnset.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).Country.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).CountryOnset.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).RecordStatus.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).CurrentHospital.ToLower().Contains(searchString) ||
                                ((CaseViewModel)caseVM).LastSamplePCRResult.ToLower().Contains(searchString)
                            )
                    );
                }

            }
            else
            {
                IsolatedCollectionView.Filter = new Predicate<object>(x =>
                    (
                    (
                    (((CaseViewModel)x).IsolationCurrent != null &&
                    ((CaseViewModel)x).IsolationCurrent.Equals("1")) ||
                    (((CaseViewModel)x).DateIsolationCurrent.HasValue)
                    ) &&
                    !((CaseViewModel)x).DateDeath2.HasValue &&
                    !((CaseViewModel)x).DateDischargeIso.HasValue &&
                    !((CaseViewModel)x).DateDischargeHospital.HasValue &&
                    !((CaseViewModel)x).CurrentStatus.Equals(Properties.Resources.Dead) &&
                    !((CaseViewModel)x).FinalStatus.Equals(Core.Enums.AliveDead.Dead)
                    )
                    );
            }
        }

        public ICommand SearchContacts { get { return new RelayCommand(SearchContactsExecute); } }
        private void SearchContactsExecute()
        {
            string searchString = SearchContactsText.Trim().ToLower();
            Converters.DateConverter DateConverter = new Converters.DateConverter();
            Converters.FinalOutcomeConverter FinalOutcomeConverter = new Converters.FinalOutcomeConverter();
            string dateFormat = string.Empty;
            dateFormat = "{0:" + DateConverter.GetDateFormat(this.ApplicationCulture) + "}";
            if (!String.IsNullOrEmpty(searchString))
            {
                if (IsCountryUS)
                {
                    ContactCollectionView.Filter = new Predicate<object>
                    (
                        contactVM =>
                            (
                            ((ContactViewModel)contactVM).ContactCDCID.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).ContactStateID.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).Team.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).HCW.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).PublicHealthAction.ToLower().Contains(searchString) ||
                            FinalOutcomeConverter.Convert(((ContactViewModel)contactVM).FinalOutcome, null, null, null).ToString().ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).Surname.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).OtherNames.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).Gender.ToLower().Contains(searchString) ||
                            (((ContactViewModel)contactVM).Age.HasValue && ((ContactViewModel)contactVM).Age.Value.ToString().Equals(searchString)) ||
                            (((ContactViewModel)contactVM).DateOfLastContact.HasValue && String.Format(dateFormat, ((ContactViewModel)contactVM).DateOfLastContact).Contains(searchString)) ||
                            (((ContactViewModel)contactVM).DateOfLastFollowUp.HasValue && String.Format(dateFormat, ((ContactViewModel)contactVM).DateOfLastFollowUp).Contains(searchString)) ||
                            ((ContactViewModel)contactVM).RiskLevel.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).Village.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).District.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).HCWFacility.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).Phone.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).SubCounty.ToLower().Contains(searchString)
                            )
                    );
                }
                else
                {
                    ContactCollectionView.Filter = new Predicate<object>
                    (
                        contactVM =>
                            (
                            ((ContactViewModel)contactVM).Team.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).HCW.ToLower().Contains(searchString) ||
                            FinalOutcomeConverter.Convert(((ContactViewModel)contactVM).FinalOutcome, null, null, null).ToString().ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).ContactID.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).Surname.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).OtherNames.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).Gender.ToLower().Contains(searchString) ||
                            (((ContactViewModel)contactVM).Age.HasValue && ((ContactViewModel)contactVM).Age.Value.ToString().Equals(searchString)) ||
                            (((ContactViewModel)contactVM).DateOfLastContact.HasValue && String.Format(dateFormat, ((ContactViewModel)contactVM).DateOfLastContact).Contains(searchString)) ||
                            (((ContactViewModel)contactVM).DateOfLastFollowUp.HasValue && String.Format(dateFormat, ((ContactViewModel)contactVM).DateOfLastFollowUp).Contains(searchString)) ||
                            ((ContactViewModel)contactVM).RiskLevel.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).HeadOfHousehold.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).LC1Chairman.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).Village.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).Parish.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).District.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).HCWFacility.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).Phone.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).SubCounty.ToLower().Contains(searchString)
                            )
                    );
                }
            }
            else
            {
                ContactCollectionView.Filter = null;
            }
        }

        public ICommand SearchExistingContacts { get { return new RelayCommand(SearchExistingContactsExecute); } }
        private void SearchExistingContactsExecute()
        {
            string searchString = SearchExistingContactsText.Trim().ToLower();

            Converters.FinalOutcomeConverter FinalOutcomeConverter = new Converters.FinalOutcomeConverter();
            Converters.DateConverter DateConverter = new Converters.DateConverter();

            string dateFormat = string.Empty;
            dateFormat = "{0:" + DateConverter.GetDateFormat(this.ApplicationCulture) + "}";

            if (!String.IsNullOrEmpty(searchString))
            {
                if (IsCountryUS)
                {
                    ExistingContactCollectionView.Filter = new Predicate<object>
                    (
                        contactVM =>
                            (
                            ((ContactViewModel)contactVM).ContactCDCID.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).ContactStateID.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).Team.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).HCW.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).PublicHealthAction.ToLower().Contains(searchString) ||
                            FinalOutcomeConverter.Convert(((ContactViewModel)contactVM).FinalOutcome, null, null, null).ToString().ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).Surname.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).OtherNames.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).Gender.ToLower().Contains(searchString) ||
                            (((ContactViewModel)contactVM).Age.HasValue && ((ContactViewModel)contactVM).Age.Value.ToString().Equals(searchString)) ||
                            (((ContactViewModel)contactVM).DateOfLastContact.HasValue && String.Format(dateFormat, ((ContactViewModel)contactVM).DateOfLastContact).Contains(searchString)) ||
                            (((ContactViewModel)contactVM).DateOfLastFollowUp.HasValue && String.Format(dateFormat, ((ContactViewModel)contactVM).DateOfLastFollowUp).Contains(searchString)) ||
                            ((ContactViewModel)contactVM).RiskLevel.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).Village.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).District.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).HCWFacility.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).Phone.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).SubCounty.ToLower().Contains(searchString)
                            )
                    );
                }
                else
                {
                    ExistingContactCollectionView.Filter = new Predicate<object>
                    (
                        contactVM =>
                            (
                            ((ContactViewModel)contactVM).Team.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).HCW.ToLower().Contains(searchString) ||
                            FinalOutcomeConverter.Convert(((ContactViewModel)contactVM).FinalOutcome, null, null, null).ToString().ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).ContactID.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).Surname.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).OtherNames.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).Gender.ToLower().Contains(searchString) ||
                            (((ContactViewModel)contactVM).Age.HasValue && ((ContactViewModel)contactVM).Age.Value.ToString().Equals(searchString)) ||
                            (((ContactViewModel)contactVM).DateOfLastContact.HasValue && String.Format(dateFormat, ((ContactViewModel)contactVM).DateOfLastContact).Contains(searchString)) ||
                            (((ContactViewModel)contactVM).DateOfLastFollowUp.HasValue && String.Format(dateFormat, ((ContactViewModel)contactVM).DateOfLastFollowUp).Contains(searchString)) ||
                            ((ContactViewModel)contactVM).RiskLevel.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).HeadOfHousehold.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).LC1Chairman.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).Village.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).Parish.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).District.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).HCWFacility.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).Phone.ToLower().Contains(searchString) ||
                            ((ContactViewModel)contactVM).SubCounty.ToLower().Contains(searchString)
                            )
                    );
                }
            }
            else
            {
                ExistingContactCollectionView.Filter = null;
            }
        }

        public ICommand CancelIssueListGeneration { get { return new RelayCommand(CancelIssueListGenerationExecute); } }
        void CancelIssueListGenerationExecute()
        {
            IssueCollection.Clear();
            IssueList.Clear();

            // TODO: Work on this
        }

        public ICommand RequestIssueListGeneration { get { return new RelayCommand(RequestIssueListGenerationExecute); } }
        void RequestIssueListGenerationExecute()
        {
            IssueCollection.Clear();
            IssueList.Clear();

            TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
            Task.Factory.StartNew(() =>
            {
                GenerateIssuesList();
            }).ContinueWith(t =>
            {
                foreach (Issue issue in IssueList)
                {
                    if (!IssueCollection.Contains(issue))
                    {
                        IssueCollection.Add(issue);
                    }
                }
            },
            TaskScheduler.FromCurrentSynchronizationContext());
        }

        public ICommand ConvertContactToCase { get { return new RelayCommand<ContactConversionInfo>(ConvertContactToCaseExecute); } }
        void ConvertContactToCaseExecute(ContactConversionInfo conversionInfo)
        {
            if (CaseCollection == null)
                return;

            RenderableField surnameField = (CaseForm.Fields["Surname"]) as RenderableField;
            RenderableField isoField = (CaseForm.Fields["IsolationCurrent"]) as RenderableField;
            RenderableField fsField = (CaseForm.Fields["FinalStatus"]) as RenderableField;

            if (surnameField != null && isoField != null && fsField != null)
            {
                ContactViewModel contactVM = conversionInfo.ContactVM;

                string currentContactGuid = contactVM.RecordId;

                //Logger.Log(DateTime.Now + ":  " +
                //    CurrentUser + ": " +
                //    "Contact-to-case conversion requested for contact " + contactVM.ContactID + " with GUID " + currentContactGuid + ". Final status: " + contactVM.FinalOutcome + "  Last date of contact: " + contactVM.DateOfLastContact.Value.ToShortDateString());

                DateTime dtNow = DateTime.Now;
                dtNow = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, dtNow.Hour, dtNow.Minute, dtNow.Second, 0);

                if (DoesCaseExist(conversionInfo.ContactVM.RecordId))
                {
                    DbLogger.Log("Exception: A contact is being converted to a case, but the contact already exists in the case database");
                    throw new InvalidOperationException("A contact is being converted to a case, but the contact already exists in the case database.");
                }
                Query selectQuery = null;
                Query insertQuery = null;
                selectQuery = Database.CreateQuery("SELECT * FROM [" + CaseForm.TableName + "]  WHERE GlobalRecordId = @ToRecordGuid and RECSTATUS=0 ");
                selectQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, currentContactGuid));
                DataTable casetable = Database.Select(selectQuery);
                if (casetable.Rows.Count == 0)
                {

                    // Add contact to the case database
                    insertQuery = Database.CreateQuery("INSERT INTO [" + CaseForm.TableName + "] (GlobalRecordId, RECSTATUS, FirstSaveTime, LastSaveTime, FirstSaveLogonName, LastSaveLogonName) VALUES (" +
                       "@GlobalRecordId, @RECSTATUS, @FirstSaveTime, @LastSaveTime, @FirstSaveLogonName, @LastSaveLogonName)");
                    insertQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, currentContactGuid));
                    insertQuery.Parameters.Add(new QueryParameter("@RECSTATUS", DbType.Byte, 1));
                    insertQuery.Parameters.Add(new QueryParameter("@FirstSaveTime", DbType.DateTime, dtNow));
                    insertQuery.Parameters.Add(new QueryParameter("@LastSaveTime", DbType.DateTime, dtNow));
                    insertQuery.Parameters.Add(new QueryParameter("@FirstSaveLogonName", DbType.String, CurrentUser));
                    insertQuery.Parameters.Add(new QueryParameter("@LastSaveLogonName", DbType.String, CurrentUser));
                    Database.ExecuteNonQuery(insertQuery);

                    foreach (Epi.Page page in CaseForm.Pages)
                    {
                        insertQuery = Database.CreateQuery("INSERT INTO [" + page.TableName + "] (GlobalRecordId) VALUES (" +
                        "@GlobalRecordId)");
                        insertQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, currentContactGuid));
                        Database.ExecuteNonQuery(insertQuery);
                    }
                }
                else
                {
                    Query updatetQuery = Database.CreateQuery("Update [" + CaseForm.TableName + "] set  RECSTATUS=@RECSTATUS,  LastSaveTime=@LastSaveTime,  LastSaveLogonName = @LastSaveLogonName Where GlobalRecordId= @GlobalRecordId ");
                    updatetQuery.Parameters.Add(new QueryParameter("@RECSTATUS", DbType.Byte, 1));
                    updatetQuery.Parameters.Add(new QueryParameter("@LastSaveTime", DbType.DateTime, dtNow));
                    updatetQuery.Parameters.Add(new QueryParameter("@LastSaveLogonName", DbType.String, CurrentUser));
                    updatetQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, currentContactGuid));
                    Database.ExecuteNonQuery(updatetQuery);
                }

                selectQuery = Database.CreateQuery("SELECT UniqueKey FROM [" + CaseForm.TableName + "] WHERE [GlobalRecordId] = @GlobalRecordId");
                selectQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, currentContactGuid));
                int uniqueKey = int.Parse(Database.ExecuteScalar(selectQuery).ToString());

                CaseViewModel newCase = new CaseViewModel(CaseForm, LabForm, contactVM, uniqueKey);
                newCase.ID = String.Empty;

                Query updateQuery = null;
                if (IsCountryUS)
                {
                    updateQuery = Database.CreateQuery("UPDATE [" + surnameField.Page.TableName + "] SET " +
                       "[Surname] = @Surname, " +
                       "[OtherNames] = @OtherNames, " +
                       "[Age] = @Age, " +
                       "[Gender] = @Gender, " +
                        "[DOB] = @DOB, " +
                        "[AddressRes] = @Address, " +
                        "[ZipRes] = @Zip, " +
                        "[PhoneNumber] = @PhoneNumber, " +
                        "[Email] = @Email, " +
                        "[HCW] = @HCW, " +
                        "[HCWPosition] = @HCWPosition, " +
                        "[HCWFacility] = @HCWFacility, " +
                        "[HCWDistrict] = @HCWDistrict, " +
                        "[HCWSC] = @HCWSC, " +
                        "[HCWVillage] = @HCWVillage, " +
                        //   "[HeadHouse] = @HeadHouse, " +
                       "[VillageRes] = @VillageRes, " +
                       "[SCRes] = @SCRes, " +
                       "[DistrictRes] = @DistrictRes " +
                       "WHERE [GlobalRecordId] = @GlobalRecordId");
                }
                else
                {
                    updateQuery = Database.CreateQuery("UPDATE [" + surnameField.Page.TableName + "] SET " +
                   "[Surname] = @Surname, " +
                   "[OtherNames] = @OtherNames, " +
                   "[Age] = @Age, " +
                   "[AgeUnit] = @AgeUnit, " +
                   "[Gender] = @Gender, " +
                   "[HeadHouse] = @HeadHouse, " +
                   "[VillageRes] = @VillageRes, " +
                   "[SCRes] = @SCRes, " +
                   "[DistrictRes] = @DistrictRes " +
                   "WHERE [GlobalRecordId] = @GlobalRecordId");
                }


                updateQuery.Parameters.Add(new QueryParameter("@Surname", DbType.String, newCase.Surname));
                updateQuery.Parameters.Add(new QueryParameter("@OtherNames", DbType.String, newCase.OtherNames));
                if (newCase.Age.HasValue)
                {
                    updateQuery.Parameters.Add(new QueryParameter("@Age", DbType.Double, newCase.Age));
                }
                else
                {
                    updateQuery.Parameters.Add(new QueryParameter("@Age", DbType.Double, DBNull.Value));
                }

                string ageUnit = String.Empty;
                if (newCase.AgeUnit.HasValue)
                {
                    switch (newCase.AgeUnit)
                    {
                        case AgeUnits.Months:
                            ageUnit = Properties.Resources.AgeUnitMonths;
                            break;
                        case AgeUnits.Years:
                            ageUnit = Properties.Resources.AgeUnitYears;
                            break;
                    }
                }

                if (!IsCountryUS)
                {
                    updateQuery.Parameters.Add(new QueryParameter("@AgeUnit", DbType.String, ageUnit));
                }


                string genderValue = String.Empty;
                if (newCase.Gender.Equals(Core.Enums.Gender.Male))
                {
                    genderValue = "1";
                }
                else if (newCase.Gender.Equals(Core.Enums.Gender.Female))
                {
                    genderValue = "2";
                }
                updateQuery.Parameters.Add(new QueryParameter("@Gender", DbType.String, genderValue));

                if (IsCountryUS)
                {
                    if (newCase.DOB == null)
                    {
                        updateQuery.Parameters.Add(new QueryParameter("@DOB", DbType.DateTime, DBNull.Value));
                    }
                    else
                    {
                        updateQuery.Parameters.Add(new QueryParameter("@DOB", DbType.DateTime, newCase.DOB));
                    }

                    updateQuery.Parameters.Add(new QueryParameter("@Address", DbType.String, newCase.AddressRes));
                    updateQuery.Parameters.Add(new QueryParameter("@Zip", DbType.String, newCase.ZipRes));
                    updateQuery.Parameters.Add(new QueryParameter("@PhoneNumber", DbType.String, newCase.PhoneNumber));
                    updateQuery.Parameters.Add(new QueryParameter("@Email", DbType.String, newCase.Email));

                    if (contactVM.HCW == ContactTracing.CaseView.Properties.Resources.Yes)
                    {
                        newCase.IsHCW = true;
                    }
                    else
                    {
                        newCase.IsHCW = false;
                    }

                    updateQuery.Parameters.Add(new QueryParameter("@HCW", DbType.Boolean, newCase.IsHCW));
                    updateQuery.Parameters.Add(new QueryParameter("@HCWPosition", DbType.String, newCase.OccupationHCWPosition));
                    updateQuery.Parameters.Add(new QueryParameter("@HCWFacility", DbType.String, newCase.OccupationHCWFacility));
                    updateQuery.Parameters.Add(new QueryParameter("@HCWDistrict", DbType.String, newCase.OccupationHCWDistrict));
                    updateQuery.Parameters.Add(new QueryParameter("@HCWSC", DbType.String, newCase.OccupationHCWSC));
                    updateQuery.Parameters.Add(new QueryParameter("@HCWVillage", DbType.String, newCase.OccupationHCWVillage));
                }
                else
                {
                    updateQuery.Parameters.Add(new QueryParameter("@HeadHouse", DbType.String, newCase.HeadOfHousehold));
                }

                updateQuery.Parameters.Add(new QueryParameter("@VillageRes", DbType.String, newCase.Village));
                updateQuery.Parameters.Add(new QueryParameter("@SCRes", DbType.String, newCase.SubCounty));
                updateQuery.Parameters.Add(new QueryParameter("@DistrictRes", DbType.String, newCase.District));
                updateQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, currentContactGuid));
                Database.ExecuteNonQuery(updateQuery);

                //Logger.Log(DateTime.Now + ":  " +
                //    CurrentUser + ": " +
                //    "Contact-to-case conversion updated case record " + newCase.RecordId + " with contact data.");


                if (conversionInfo.FinalOutcome == ContactFinalOutcome.Isolated)
                {
                    if (conversionInfo.IsDead)
                    {
                        #region Died

                        updateQuery = Database.CreateQuery("UPDATE [" + fsField.Page.TableName + "] SET " +
                            "[FinalStatus] = @FinalStatus, " +
                            "[DateDeath2] = @DateDeath2 " +
                            "WHERE [GlobalRecordId] = @GlobalRecordId");

                        updateQuery.Parameters.Add(new QueryParameter("@FinalStatus", DbType.String, "1"));
                        updateQuery.Parameters.Add(new QueryParameter("@DateDeath2", DbType.DateTime, conversionInfo.DateIsolated));
                        updateQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, currentContactGuid));
                        Database.ExecuteNonQuery(updateQuery);

                        //newCase.FinalStatus = Properties.Resources.Dead;
                        newCase.FinalCaseStatus = "1"; // 1 = Dead
                        newCase.DateDeath2 = conversionInfo.DateIsolated;

                        //Logger.Log(DateTime.Now + ":  " +
                        //    CurrentUser + ": " +
                        //    "Contact-to-case conversion routine set case dead status to " + newCase.FinalStatus + " with a date " +
                        //    newCase.DateDeath2.Value.ToShortDateString() + ".");

                        RenderableField sField = (CaseForm.Fields["StatusAsOfCurrentDate"]) as RenderableField;
                        updateQuery = Database.CreateQuery("UPDATE [" + sField.Page.TableName + "] SET " +
                            "[StatusAsOfCurrentDate] = @StatusAsOfCurrentDate " +
                            "WHERE [GlobalRecordId] = @GlobalRecordId");

                        updateQuery.Parameters.Add(new QueryParameter("@StatusAsOfCurrentDate", DbType.String, Properties.Resources.Dead));
                        updateQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, currentContactGuid));
                        Database.ExecuteNonQuery(updateQuery);

                        newCase.CurrentStatus = Properties.Resources.Dead;
                        #endregion // Died
                    }
                    else
                    {
                        updateQuery = Database.CreateQuery("UPDATE [" + isoField.Page.TableName + "] SET " +
                            "[IsolationCurrent] = @IsolationCurrent, " +
                            "[HospitalizedCurrent] = @HospitalizedCurrent, " +
                            "[DateIsolationCurrent] = @DateIsolationCurrent " +
                            "WHERE [GlobalRecordId] = @GlobalRecordId");

                        updateQuery.Parameters.Add(new QueryParameter("@IsolationCurrent", DbType.String, "1"));
                        updateQuery.Parameters.Add(new QueryParameter("@HospitalizedCurrent", DbType.String, "1"));
                        updateQuery.Parameters.Add(new QueryParameter("@DateIsolationCurrent", DbType.DateTime, conversionInfo.DateIsolated));
                        updateQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, currentContactGuid));
                        Database.ExecuteNonQuery(updateQuery);

                        newCase.IsolationCurrent = "1";
                        newCase.DateIsolationCurrent = conversionInfo.DateIsolated;

                        //Logger.Log(DateTime.Now + ":  " +
                        //    CurrentUser + ": " +
                        //    "Contact-to-case conversion routine set case isolation status to " + newCase.IsolationCurrent + " with a date " +
                        //    newCase.DateIsolationCurrent.Value.ToShortDateString() + ".");
                    }
                }

                lock (_caseCollectionLock)
                {
                    CaseCollection.Add(newCase);
                }
                contactVM.IsActive = false;

                contactVM.IsCase = true;
                newCase.IsContact = true;

                for (int i = DailyFollowUpCollection.Count - 1; i >= 0; i--)
                {
                    DailyCheckViewModel dcVM = DailyFollowUpCollection[i];
                    if (dcVM.ContactVM == contactVM)
                    {
                        DailyFollowUpCollection.Remove(dcVM);
                        break;
                    }
                }

                for (int i = PrevFollowUpCollection.Count - 1; i >= 0; i--)
                {
                    DailyCheckViewModel dcVM = PrevFollowUpCollection[i];
                    if (dcVM.ContactVM == contactVM)
                    {
                        PrevFollowUpCollection.Remove(dcVM);
                        break;
                    }
                }

                // Add the contact's source cases to the new case's source cases
                var query = from ccpVM in ContactLinkCollection
                            where ccpVM.ContactVM.RecordId == contactVM.RecordId
                            select ccpVM;

                foreach (CaseContactPairViewModel ccpVM in query)
                {
                    insertQuery = ccpVM.GenerateInsertQueryForConversion(this.Database, this.CaseFormId);
                    Database.ExecuteNonQuery(insertQuery);
                }

                switch (conversionInfo.FinalOutcome)
                {
                    case ContactFinalOutcome.Discharged:
                        contactVM.FinalOutcome = "1";
                        break;
                    case ContactFinalOutcome.Isolated:
                        contactVM.FinalOutcome = "2";
                        break;
                    case ContactFinalOutcome.Dropped:
                        contactVM.FinalOutcome = "3";
                        break;
                }

                if (contactVM.FinalOutcome == "2")
                {
                    foreach (CaseViewModel caseVM in CaseCollection)
                    {
                        bool found = false; // We only need to do this once; TODO: Replace with LINQ?
                        if (caseVM.Contacts.Contains(contactVM))
                        {
                            #region Routine
                            for (int i = 0; i < Core.Common.DaysInWindow; i++)
                            {
                                FollowUpVisitViewModel fuvvm = contactVM.FollowUpWindowViewModel.FollowUpVisits[i];

                                if (fuvvm.Date.Day == conversionInfo.DateIsolated.Day &&
                                    fuvvm.Date.Month == conversionInfo.DateIsolated.Month &&
                                    fuvvm.Date.Year == conversionInfo.DateIsolated.Year)
                                {
                                    found = true;
                                    //fuvvm.Seen = SeenType.Seen;
                                    //fuvvm.Sick = SicknessType.SickIsolated;
                                    if (conversionInfo.IsDead)
                                    {
                                        fuvvm.Status = Core.ContactDailyStatus.Dead;
                                        //                    Logger.Log(DateTime.Now + ":  " +
                                        //CurrentUser + ": " +
                                        //"Contact-to-case conversion routine set contact's final status to dead on " +
                                        //conversionInfo.DateIsolated.ToShortDateString() + ".");
                                    }
                                    else
                                    {
                                        fuvvm.Status = Core.ContactDailyStatus.SeenSickAndIsolated;
                                        //                    Logger.Log(DateTime.Now + ":  " +
                                        //CurrentUser + ": " +
                                        //"Contact-to-case conversion routine set contact's final status to seen/sick/isolated on " +
                                        //conversionInfo.DateIsolated.ToShortDateString() + ".");
                                    }


                                    using (IDbTransaction transaction = Database.OpenTransaction())
                                    {
                                        updateQuery = Database.CreateQuery("UPDATE [metaLinks] SET " +
                                            "[Day" + (i + 1) + "] = @Day WHERE " +
                                            "[FromViewId] = @FromViewId AND" +
                                            "[ToViewId] = @ToViewId AND" +
                                            "[FromRecordGuid] = @FromRecordGuid AND" +
                                            "[ToRecordGuid] = @ToRecordGuid");

                                        updateQuery.Parameters.Add(new QueryParameter("@Day", DbType.Int16, Convert.ToInt16(fuvvm.Status.Value)));
                                        updateQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int16, CaseFormId));
                                        updateQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int16, ContactFormId));
                                        updateQuery.Parameters.Add(new QueryParameter("@FromRecordGuid", DbType.String, caseVM.RecordId));
                                        updateQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, contactVM.RecordId));

                                        Database.ExecuteNonQuery(updateQuery, transaction);

                                        updateQuery = Database.CreateQuery("UPDATE [metaHistory] SET " +
                                            "[StatusOnDate] = @Status WHERE " +
                                            "[FollowUpDate] = @FollowUpDate AND" +
                                            "[ContactGUID] = @ContactGUID");

                                        updateQuery.Parameters.Add(new QueryParameter("@Status", DbType.Int16, Convert.ToInt16(fuvvm.Status.Value)));
                                        updateQuery.Parameters.Add(new QueryParameter("@FollowUpDate", DbType.DateTime, fuvvm.Date));
                                        updateQuery.Parameters.Add(new QueryParameter("@ContactGUID", DbType.Guid, contactVM.RecordId));

                                        Database.ExecuteNonQuery(updateQuery, transaction);

                                        try
                                        {
                                            transaction.Commit();
                                        }
                                        catch (Exception ex0)
                                        {
                                            DbLogger.Log("Contact metaLinks and metaHistory updates failed on commit. Transaction rolled back. Exception: " + ex0.Message);
                                            try
                                            {
                                                transaction.Rollback();
                                            }
                                            catch (Exception ex1)
                                            {
                                                DbLogger.Log("Contact metaLinks and metaHistory rollback failed. Exception: " + ex1.Message);
                                            }
                                        }
                                    }
                                }
                            }
                            #endregion Routine
                        }
                        if (found)
                        {
                            break;
                        }
                    }
                }

                contactVM.IsCase = true;
                SetContactFinalStatus(contactVM);
                //SortCases();
                SetCaseAndContactFlags();

                SendMessageForAddCase(contactVM.RecordId);
            }
            else
            {
                DbLogger.Log("Exception: Missing fields detected on case form; cannot proceed with contact-to-case conversion");
                throw new InvalidOperationException("The conversion cannot be completed because the case form is missing one or more fields that are required for the contact-to-case conversion. Please check " +
                    "that the Surname, IsolationCurrent, and FinalStatus fields are present and try again.");
            }
        }

        #endregion // Commands

        #region Events
        public delegate void CaseDataPopulatedHandler(object sender, CaseDataPopulatedArgs e);
        public delegate void InitialSetupRunHandler(object sender, InitialSetupArgs e);
        public delegate void CaseDeletedHandler(object sender, CaseDeletedArgs e);
        public delegate void DuplicateIdDetectedHandler(object sender, DuplicateIdDetectedArgs e);

        public event EventHandler CaseSwitchToLegacyEnter;
        public event InitialSetupRunHandler InitialSetupRun;
        public event CaseDataPopulatedHandler CaseDataPopulated;
        public event EventHandler IssueDataPopulated;
        public event EventHandler RefreshRequired;
        public event CaseDeletedHandler CaseDeleted;
        public event DuplicateIdDetectedHandler DuplicateIdDetected;
        public event EventHandler FollowUpVisitUpdated;
        public event EventHandler SyncProblemsDetected;
        #endregion // Events

        public async void UpdateAdministrativeBoundariesAsync()
        {
            if (Database != null)
            {
                await Task.Factory.StartNew(delegate
                {
                    UpdateAdministrativeBoundaries();
                });
            }
        }

        public void UpdateAdministrativeBoundaries()
        {
            if (Database != null && !IsCountryUS) // Bypass this logic for US
            {
                List<string> adm1FieldNames = new List<string>() { "DistrictDeath", "DistrictFuneral", "DistrictHospitalCurrent", "DistrictHospitalPast1", "DistrictHospitalPast2", "DistrictOnset", "DistrictRes", "ContactDistrict", "HospitalDischargeDistrict", "InterviewerDistrict", "TradHealerDistrict", "HospitalBeforeIllDistrict", "TravelDistrict", "FuneralDistrict1", "FuneralDistrict2", "ContactDistrict1", "ContactDistrict2", "ContactDistrict3" };
                List<string> adm2FieldNames = new List<string>() { "SCOnset", "SCRes", "SCDeath", "SCFuneral", "SCHospitalCurrent", "ContactSC" };
                List<string> adm3FieldNames = new List<string>() { "ParishRes" };
                List<string> adm4FieldNames = new List<string>() { "VillageRes", "VillageDeath", "VillageFuneral", "VillageHospitalCurrent", "VillageHospitalPast1", "VillageHospitalPast2", "VillageOnset", "TravelVillage", "TradHealerVillage", "HospitalBeforeIllVillage", "FuneralVillage2", "FuneralVillage1", "ContactVillage", "ContactVillage1", "ContactVillage2", "ContactVillage3" };

                string queryText = "UPDATE metaFields SET PromptText = @PromptText WHERE Name = @Name";

                foreach (string districtFieldName in adm1FieldNames)
                {
                    Query updateQuery = Database.CreateQuery(queryText);
                    updateQuery.Parameters.Add(new QueryParameter("@PromptText", DbType.String, Adm1 + ":"));
                    updateQuery.Parameters.Add(new QueryParameter("@Name", DbType.String, districtFieldName));
                    Database.ExecuteNonQuery(updateQuery);
                }

                foreach (string districtFieldName in adm2FieldNames)
                {
                    Query updateQuery = Database.CreateQuery(queryText);
                    updateQuery.Parameters.Add(new QueryParameter("@PromptText", DbType.String, Adm2 + ":"));
                    updateQuery.Parameters.Add(new QueryParameter("@Name", DbType.String, districtFieldName));
                    Database.ExecuteNonQuery(updateQuery);
                }

                foreach (string districtFieldName in adm3FieldNames)
                {
                    Query updateQuery = Database.CreateQuery(queryText);
                    updateQuery.Parameters.Add(new QueryParameter("@PromptText", DbType.String, Adm3 + ":"));
                    updateQuery.Parameters.Add(new QueryParameter("@Name", DbType.String, districtFieldName));
                    Database.ExecuteNonQuery(updateQuery);
                }

                foreach (string villageFieldName in adm4FieldNames)
                {
                    Query updateQuery = Database.CreateQuery(queryText);
                    updateQuery.Parameters.Add(new QueryParameter("@PromptText", DbType.String, Adm4 + ":"));
                    updateQuery.Parameters.Add(new QueryParameter("@Name", DbType.String, villageFieldName));
                    Database.ExecuteNonQuery(updateQuery);
                }
            }
        }

        private CaseViewModel AddCase(CaseViewModel newCase)
        {
            if (CaseCollection == null)
                return null;

            CaseViewModel newCaseVM = new CaseViewModel(newCase);
            lock (_caseCollectionLock)
            {
                newCaseVM.UpdateIsNewRecord(); //17109
                CaseCollection.Add(newCaseVM);
            }

            return newCaseVM;
        }

        private void RemoveCase(string caseGuid, bool initiatedOnClient = true)
        {
            CaseViewModel cvm = null;
            for (int i = CaseCollection.Count - 1; i >= 0; i--)
            {
                cvm = CaseCollection[i];
                if (cvm.RecordId == caseGuid)
                {
                    lock (_caseCollectionLock)
                    {
                        CaseCollection.Remove(cvm);
                    }
                    //CurrentHospitalizedCollection.Remove(cvm);

                    for (int j = CurrentSourceCaseCollection.Count - 1; j >= 0; j--)
                    {
                        var ceVM = CurrentSourceCaseCollection[j];
                        if (cvm.RecordId == ceVM.ExposedCaseVM.RecordId)
                        {
                            CurrentSourceCaseCollection.Remove(ceVM);
                        }
                    }

                    for (int j = CurrentCaseLinkCollection.Count - 1; j >= 0; j--)
                    {
                        var ceVM = CurrentCaseLinkCollection[j];
                        if (cvm.RecordId == ceVM.CaseVM.RecordId)
                        {
                            lock (_caseCollectionLock) { CurrentCaseLinkCollection.Remove(ceVM); }
                        }
                    }

                    break;
                }
            }

            if (initiatedOnClient)
            {
                // Case form base table removal
                int rows = 0;
                Query deleteQuery = Database.CreateQuery("DELETE FROM [" + CaseForm.TableName + "] WHERE [GlobalRecordId] = @GlobalRecordId");
                deleteQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, caseGuid));
                rows = Database.ExecuteNonQuery(deleteQuery);
                 
                //commented out Hard Delete part and implementing Soft Delete

                if (cvm != null)
                {
                    DbLogger.Log(String.Format(
                        "Deleted case : ID = {0}, EpiCaseDef = {1}, GUID = {2}",
                            cvm.ID, cvm.EpiCaseDef, caseGuid));
                }
                else
                {
                    DbLogger.Log(String.Format(
                        "Deleted case : GUID = {0}",
                            caseGuid));
                }

                // Case form page table removal
                foreach (Epi.Page page in CaseForm.Pages)
                {
                    deleteQuery = Database.CreateQuery("DELETE FROM [" + page.TableName + "] WHERE [GlobalRecordId] = @GlobalRecordId");
                    deleteQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, caseGuid));
                    Database.ExecuteNonQuery(deleteQuery);
                }

                // Lab removal
                if (cvm != null)
                {
                    try
                    {
                        DeleteLabs(cvm);
                    }
                    catch (Exception ex)
                    {
                        DbLogger.Log(String.Format(
                            "Exception generated while trying to soft-delete labs for case GUID = {0}, ID = {1}. Message: {2}",
                            caseGuid, cvm.ID, ex.Message));
                    }
                }

                //string querySyntax = "Update  [" + CaseForm.TableName + "] Set RecStatus = 0 WHERE [GlobalRecordId] = @GlobalRecordId";
                //Query deleteQuery = Database.CreateQuery(querySyntax);
                //deleteQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, caseGuid));
                //rows = Database.ExecuteNonQuery(deleteQuery);
                //if (rows != 1)
                //{
                //    throw new InvalidOperationException("A deletion was requested for case with GUID " + caseGuid + " but the case no longer exists.");
                //}

                // MetaLinks removal for case-source case relationships
                
                deleteQuery = Database.CreateQuery("DELETE FROM [metaLinks] WHERE [ToRecordGuid] = @ToRecordGuid AND [ToViewId] = @ToViewId AND [FromViewId] = @FromViewId");
                deleteQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, caseGuid));
                deleteQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, CaseFormId));
                deleteQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, CaseFormId));
                Database.ExecuteNonQuery(deleteQuery);

                foreach (CaseContactPairViewModel ccpVM in ContactLinkCollection)
                {
                    if (ccpVM.ContactVM.RecordId == cvm.RecordId)
                    {
                        CaseViewModel linkedCaseVM = ccpVM.CaseVM;
                        DailyCheckViewModel dcVM = new DailyCheckViewModel(ccpVM.ContactVM, linkedCaseVM);
                        foreach (FollowUpVisitViewModel fuVM in ccpVM.ContactVM.FollowUpWindowViewModel.FollowUpVisits)
                        {
                            //if (fuVM.FollowUpVisit.Day == dcVM.GetDay(DateTime.Today)) //dcVM.Day)
                            //{
                            dcVM.Status = fuVM.Status;
                            dcVM.Notes = fuVM.Notes;

                            bool foundChangeableStatus = false;
                            if (dcVM.Status.HasValue && dcVM.Status == Core.ContactDailyStatus.SeenSickAndIsolated)
                            {
                                dcVM.Status = Core.ContactDailyStatus.SeenSickAndIsoNotFilledOut;
                                foundChangeableStatus = true;
                                dcVM.ContactVM.FinalOutcome = String.Empty;
                                dcVM.ContactVM.IsActive = true;
                                dcVM.Status = null;
                                SetContactFinalStatus(dcVM.ContactVM);
                                RaisePropertyChanged("ContactCollection");
                                RaisePropertyChanged("DailyFollowUpCollection");
                            }
                            else if (dcVM.Status.HasValue && dcVM.Status == Core.ContactDailyStatus.Dead)
                            {
                                dcVM.Status = Core.ContactDailyStatus.Unknown; // What to do here?
                                foundChangeableStatus = true;
                                dcVM.ContactVM.FinalOutcome = "3";
                                dcVM.ContactVM.IsActive = false;
                                SetContactFinalStatus(dcVM.ContactVM);
                                RaisePropertyChanged("ContactCollection");
                                RaisePropertyChanged("DailyFollowUpCollection");
                            }

                            KeyValuePair<DateTime, DailyCheckViewModel> kvp = new KeyValuePair<DateTime, DailyCheckViewModel>(fuVM.Date, dcVM);
                            UpdateDaily.Execute(kvp);
                            //Logger.Log(DateTime.Now + ":  " +
                            //    CurrentUser + ": " +
                            //    "Case delete routine found a matching contact; contact's status is now " + dcVM.Status);

                            bool found = false;
                            foreach (DailyCheckViewModel iDcVM in DailyFollowUpCollection)
                            {
                                if (iDcVM.ContactVM == ccpVM.ContactVM)
                                {
                                    found = true;
                                    break;
                                }
                            }

                            if (!found)
                            {
                                lock (_dailyCollectionLock)
                                {
                                    DailyFollowUpCollection.Add(dcVM);
                                }
                            }

                            if (foundChangeableStatus)
                            {
                                break;
                            }
                            //}
                        }

                        break;
                    }
                }

                // See if the case also exists in contacts so we can possibly re-activate it
                ContactViewModel contactVM = GetContactVM(cvm.RecordId);
                if (contactVM != null)
                {
                    // it does exist, so now check to see if it's within the 21-day window
                    DateTime now = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 23, 59, 59);
                    DateTime monitorTo = contactVM.FollowUpWindowViewModel.WindowEndDate; //contactVM.FollowUpWindowViewModel.WindowStartDate.AddDays(20);
                    bool found = false;
                    if (now <= monitorTo)
                    {
                        // it's within the window, so check to see if it's been linked by any other cases
                        foreach (CaseViewModel iCaseVM in CaseCollection)
                        {
                            if (iCaseVM != cvm)
                            {
                                foreach (ContactViewModel iContactVM in iCaseVM.Contacts)
                                {
                                    if (iContactVM == contactVM)
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                            }
                        }

                        if (!found)
                        {
                            contactVM.IsActive = true;
                            RaisePropertyChanged("ContactCollection");
                        }
                    }
                }

                if (CaseDeleted != null)
                {
                    CaseDeletedArgs args = new CaseDeletedArgs(caseGuid);
                    if (contactVM != null)
                    {
                        args.ContactVM = contactVM;
                    }
                    CaseDeleted(this, args);
                }

                foreach (ContactViewModel iContactVM in cvm.Contacts)
                {
                    bool found = false;
                    foreach (CaseViewModel caseVM in CaseCollection)
                    {
                        if (cvm != caseVM && caseVM.Contacts.Contains(iContactVM))
                        {
                            found = true;
                        }
                    }

                    if (!found)
                    {
                        RemoveContactFromDatabase(iContactVM.RecordId);
                    }
                }
                RepopulateCollections(false);
            }
        }

        private void DeleteLabs(CaseViewModel caseVM)
        {
            Query selectQuery = Database.CreateQuery("SELECT GlobalRecordId FROM " + LabForm.TableName + " WHERE FKEY = @FKEY");
            selectQuery.Parameters.Add(new QueryParameter("@FKEY", DbType.String, caseVM.RecordId));

            using (IDataReader reader = Database.ExecuteReader(selectQuery))
            {
                while (reader.Read())
                {
                    string guid = reader[0].ToString();

                    int rows = 0;
                    Query deleteQuery = Database.CreateQuery("DELETE FROM [" + LabForm.TableName + "] WHERE [GlobalRecordId] = @GlobalRecordId");
                    deleteQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, guid));
                    rows = Database.ExecuteNonQuery(deleteQuery);

                    DbLogger.Log(String.Format(
                        "Deleted lab : GUID = {0}",
                            guid));

                    // Case form page table removal
                    foreach (Epi.Page page in LabForm.Pages)
                    {
                        deleteQuery = Database.CreateQuery("DELETE FROM [" + page.TableName + "] WHERE [GlobalRecordId] = @GlobalRecordId");
                        deleteQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, guid));
                        Database.ExecuteNonQuery(deleteQuery);
                    }
                    //string guid = reader[0].ToString();

                    //int rows = 0;
                    //string querySyntax = "UPDATE [" + LabForm.TableName + "] SET RecStatus = 0 WHERE [GlobalRecordId] = @GlobalRecordId";
                    //Query labUpdateQuery = Database.CreateQuery(querySyntax);
                    //labUpdateQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, guid));
                    //rows = Database.ExecuteNonQuery(labUpdateQuery);

                    //if (rows == 1)
                    //{
                    //    DbLogger.Log(String.Format(
                    //        "Soft-deleted lab : GUID = {0}",
                    //            guid));
                    //}
                    //else
                    //{
                    //    DbLogger.Log(String.Format(
                    //        "Lab soft-deletion attempted but no lab record found in database : GUID = {0}",
                    //            guid));

                    //    throw new InvalidOperationException("A soft-deletion was requested for lab record with GUID " + guid + " but the lab no longer exists.");
                    //}
                }
            }
        }

        void unpackager_UpdateProgress(double progress)
        {
            TaskbarProgressValue = (progress / 100);
        }

        void unpackager_StatusChanged(string message)
        {
            SyncStatus = message;
        }

        private void RemoveDuplicateRecords(View form)
        {
            if (IsMicrosoftSQLDatabase)
            {
                Query deleteQuery = Database.CreateQuery("WITH cte AS (SELECT *, ROW_NUMBER() OVER(PARTITION BY GlobalRecordId ORDER BY GlobalRecordId) 'RowRank' FROM " + form.TableName + ") " +
                    "DELETE FROM cte " +
                    "WHERE RowRank > 1");
                Database.ExecuteNonQuery(deleteQuery);

                foreach (Page page in form.Pages)
                {
                    deleteQuery = Database.CreateQuery("WITH cte AS (SELECT *, ROW_NUMBER() OVER(PARTITION BY GlobalRecordId ORDER BY GlobalRecordId) 'RowRank' FROM " + page.TableName + ") " +
                    "DELETE FROM cte " +
                    "WHERE RowRank > 1");
                    Database.ExecuteNonQuery(deleteQuery);
                }
            }
        }

        private void RemoveExtraneousPageTableRecords(View form)
        {
            // check page table counts
            Query baseCountQuery = Database.CreateQuery("SELECT COUNT(*) FROM " + form.TableName);
            int baseCount = (int)Database.ExecuteScalar(baseCountQuery);

            var pageTableCounts = new Dictionary<Page, int>();
            foreach (Page page in form.Pages)
            {
                Query countQuery = Database.CreateQuery("SELECT COUNT(*) FROM " + page.TableName);
                var count = (int)Database.ExecuteScalar(countQuery);
                pageTableCounts.Add(page, count);
            }

            bool needsRepair = false;

            foreach (KeyValuePair<Page, int> kvp in pageTableCounts)
            {
                if (kvp.Value != baseCount)
                {
                    needsRepair = true;
                }
            }

            if (needsRepair)
            {
                foreach (Page page in form.Pages)
                {
                    Query guidQuery = Database.CreateQuery("SELECT GlobalRecordId FROM " + form.TableName);

                    DataTable originalGuidTable = Database.Select(guidQuery);
                    DataColumn[] keyColumns = new DataColumn[1];
                    keyColumns[0] = originalGuidTable.Columns[0];
                    originalGuidTable.PrimaryKey = keyColumns;

                    List<string> guidsToRemove = new List<string>();

                    foreach (DataRow row in originalGuidTable.Rows)
                    {
                        guidsToRemove.Add(row["GlobalRecordId"].ToString());
                    }

                    //List<string> guidsToKeep = new List<string>();
                    Query query = Database.CreateQuery("SELECT GlobalRecordId FROM " + page.TableName);
                    DataTable dt = Database.Select(query);

                    foreach (DataRow row in dt.Rows)
                    {
                        DataRow originalRow = originalGuidTable.Rows.Find(row["GlobalRecordId"].ToString());
                        //guidsToKeep.Add(originalRow["GlobalRecordId"].ToString());
                        if (originalRow != null)
                        {
                            if (guidsToRemove.Contains(originalRow["GlobalRecordId"].ToString()))
                            {
                                guidsToRemove.Remove(originalRow["GlobalRecordId"].ToString());
                            }
                        }
                    }

                    foreach (string guidToRemove in guidsToRemove)
                    {
                        using (IDbTransaction transaction = Database.OpenTransaction())
                        {
                            string querySyntax = "DELETE FROM [" + form.TableName + "] WHERE [GlobalRecordId] = @GlobalRecordId";
                            Query deleteQuery = Database.CreateQuery(querySyntax);
                            deleteQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, guidToRemove));
                            Database.ExecuteNonQuery(deleteQuery, transaction);

                            foreach (Page iPage in form.Pages)
                            {
                                if (iPage != page)
                                {
                                    querySyntax = "DELETE FROM [" + iPage.TableName + "] WHERE [GlobalRecordId] = @GlobalRecordId";
                                    deleteQuery = Database.CreateQuery(querySyntax);
                                    deleteQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, guidToRemove));
                                    Database.ExecuteNonQuery(deleteQuery, transaction);
                                }
                            }

                            try
                            {
                                transaction.Commit();
                            }
                            catch (Exception ex0)
                            {
                                DbLogger.Log("Extraneous page table delete failed on commit. Exception: " + ex0.Message);
                                Epi.Logger.Log(String.Format(DateTime.Now + ":  " + "Extraneous page table delete commit exception type: {0}", ex0.GetType()));
                                Epi.Logger.Log(String.Format(DateTime.Now + ":  " + "Extraneous page table delete commit exception message: {0}", ex0.Message));
                                Epi.Logger.Log(String.Format(DateTime.Now + ":  " + "Extraneous page table delete commit rollback started..."));
                                try
                                {
                                    transaction.Rollback();
                                    Epi.Logger.Log(String.Format(DateTime.Now + ":  " + "Extraneous page table delete commit rollback was successful."));
                                }
                                catch (Exception ex1)
                                {
                                    DbLogger.Log("Extraneous page table delete commit rollback failed. Exception: " + ex1.Message);
                                }
                            }

                            Database.CloseTransaction(transaction);
                        }
                    }
                }
            }
        }

        public ICommand ToggleAdminBoundaryFieldEditorCommand { get { return new RelayCommand(ToggleAdminBoundaryFieldEditorCommandExecute); } }
        private void ToggleAdminBoundaryFieldEditorCommandExecute()
        {
            IsEditingAdminBoundaryFieldTypes = !IsEditingAdminBoundaryFieldTypes;
        }

        public ICommand RemoveAllRecordLocksCommand { get { return new RelayCommand(RemoveAllRecordLocksCommandExecute); } }
        private void RemoveAllRecordLocksCommandExecute()
        {
            string star = " * ";
            if (IsMicrosoftSQLDatabase)
            {
                star = String.Empty;
            }

            string querySyntax = "DELETE " + star + " FROM Changesets WHERE ((UpdateType >= 0 AND UpdateType <= 2) OR UpdateType = 6)";

            Query deleteQuery = Database.CreateQuery(querySyntax);
            deleteQuery.Parameters.Add(new QueryParameter("@MACADDR", DbType.String, MacAddress));
            int rows = Database.ExecuteNonQuery(deleteQuery);

            RepopulateCollections(false);
        }

        public ICommand CleanUpDataTablesCommand { get { return new RelayCommand(CleanUpDataTablesCommandExecute); } }
        private void CleanUpDataTablesCommandExecute()
        {
            DbLogger.Log(String.Format("Data table cleaner executed"));

            Parallel.Invoke(
                () =>
                {
                    RemoveExtraneousPageTableRecords(CaseForm);
                    RemoveDuplicateRecords(CaseForm); // VHF-328
                },
                () =>
                {
                    RemoveExtraneousPageTableRecords(ContactForm);
                    RemoveDuplicateRecords(ContactForm); // VHF-328
                },
                () =>
                {
                    RemoveExtraneousPageTableRecords(LabForm);
                    RemoveDuplicateRecords(LabForm); // VHF-328
                });
        }

        private void RemoveContactFromDatabase(string contactGuid)
        {
             Query deleteQuery = Database.CreateQuery("DELETE FROM [" + ContactForm.TableName + "] WHERE [GlobalRecordId] = @GlobalRecordId");
             deleteQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, contactGuid));
             int rows = Database.ExecuteNonQuery(deleteQuery);

             foreach (Epi.Page page in ContactForm.Pages)
             {
                 deleteQuery = Database.CreateQuery("DELETE FROM [" + page.TableName + "] WHERE [GlobalRecordId] = @GlobalRecordId");
                 deleteQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, contactGuid));
                 Database.ExecuteNonQuery(deleteQuery);
             }
            ///commented out hard Delete part and Implementing Soft Delete.
            ///
            
            DbLogger.Log(String.Format(
                    "Deleted contact : GUID = {0}",
                        contactGuid));

            //string querySyntax = "Update [" + ContactForm.TableName + "] Set RecStatus = 0  WHERE [GlobalRecordId] = @GlobalRecordId";

            //Query deleteQuery = Database.CreateQuery(querySyntax);
            //deleteQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, contactGuid));
            //int rows = Database.ExecuteNonQuery(deleteQuery);

            deleteQuery = Database.CreateQuery("DELETE FROM [metaLinks] WHERE [ToRecordGuid] = @ToRecordGuid AND [ToViewId] = @ToViewId");//"DELETE * FROM [metaLinks] WHERE [ToRecordGuid] = @ToRecordGuid AND [ToViewId] = @ToViewId");
            deleteQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, contactGuid));
            deleteQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, ContactFormId));
            rows = Database.ExecuteNonQuery(deleteQuery);

            deleteQuery = Database.CreateQuery("DELETE FROM [metaHistory] WHERE [ContactGUID] = @ContactGUID");
            deleteQuery.Parameters.Add(new QueryParameter("@ContactGUID", DbType.Guid, contactGuid));
            rows = Database.ExecuteNonQuery(deleteQuery);
        }

        private void GenerateIssuesList()
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            DataTable contactTable = GetContactsTable();
            IDbDriver db = this.Project.CollectedData.GetDatabase();
            double denominator = CaseCollection.Count;

            try
            {
                Query selectQuery = db.CreateQuery("SELECT * FROM [metaLinks] WHERE [ToViewId] = @ToViewId AND [FromViewId] = @FromViewId ORDER BY [LastContactDate] ASC");
                selectQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.String, CaseFormId));
                selectQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.String, CaseFormId));

                selectQuery = db.CreateQuery("SELECT * FROM [metaLinks] WHERE [ToViewId] = @ToViewId AND [FromViewId] = @FromViewId ORDER BY [LastContactDate] ASC");
                selectQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.String, CaseFormId));
                selectQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.String, ContactFormId));

                DataTable linkTableContacts = db.Select(selectQuery);
                DataTable linkTable = db.Select(selectQuery);

                //foreach (CaseViewModel c in IsolatedCollectionView)
                //{
                //    if (c.DateIsolationCurrent.HasValue)
                //    {
                //        TimeSpan ts = DateTime.Today - c.DateIsolationCurrent.Value;
                //        if (ts.TotalDays > 45)
                //        {
                //            IssueList.Add(new Issue(c.ID, "1000", String.Format("Case {0} was admitted to isolation {1} days ago on {2}.",
                //                c.ID, ts.TotalDays.ToString(), c.DateIsolationCurrent.Value.ToShortDateString())));
                //        }
                //    }

                //    if ((c.DateIsolationCurrent.HasValue || c.IsolationCurrent == "1") && (c.EpiCaseDef == Core.Enums.EpiCaseClassification.NotCase || c.EpiCaseDef == Core.Enums.EpiCaseClassification.Excluded))
                //    {
                //        IssueList.Add(new Issue(c.ID, "1046", String.Format("Case {0} is currently listed as being in isolation, but has an epi case classification of {1}.",
                //            c.ID, c.EpiCaseDef)));
                //    }

                //    if ((c.DateIsolationCurrent.HasValue || c.IsolationCurrent == "1") && (c.CurrentStatus == Properties.Resources.Dead))
                //    {
                //        IssueList.Add(new Issue(c.ID, "1047", String.Format("Case {0} is currently listed as being in isolation, but has a final status of dead.",
                //            c.ID, c.EpiCaseDef)));
                //    }
                //}

                Parallel.ForEach(ContactCollection, c =>
                {
                    for (int i = 0; i < Core.Common.DaysInWindow; i++)
                    {
                        // check to see if a contact was seen in the future
                        if (c.FollowUpWindowViewModel.FollowUpVisits[i].Date > DateTime.Today &&
                            c.FollowUpWindowViewModel.FollowUpVisits[i].IsSeen == true)
                        {
                            IssueList.Add(new Issue(c.LastSourceCase.ID, "1001", String.Format("Case {0} has a contact ({1}) who was marked as being 'seen' on a date that occurs in the future ({2}).",
                                c.LastSourceCase.ID, c.ContactID, c.FollowUpWindowViewModel.FollowUpVisits[i].Date.ToShortDateString())));
                        }
                    }

                    // check to see if a contact was discharged from follow-up without having been seen on their 21st day
                    if (c.FinalOutcome == "1" &&
                        c.FollowUpWindowViewModel.FollowUpVisits[Core.Common.DaysInWindow - 1].IsSeen == false)
                    {
                        IssueList.Add(new Issue(c.LastSourceCase.ID, "1002", String.Format("Case {0} has a contact ({1}) who was marked as 'discharged from follow-up', but was not marked as 'seen' on their final day of follow-up on {2}.",
                                                        c.LastSourceCase.ID, c.ContactID, c.FollowUpWindowViewModel.FollowUpVisits[Core.Common.DaysInWindow - 1].Date.ToShortDateString())));
                    }
                });

                #region Tests
                Parallel.ForEach(CaseCollection, c =>
                {
                    //LoadExtendedCaseData(c);
                    CaseViewModel.IsCountryUS = this.IsCountryUS; //17178
                    c.Load();

                    if (c.DateReport.HasValue && c.DateOnset.HasValue && c.DateReport < c.DateOnset)
                        IssueList.Add(new Issue(c.ID, "1003", String.Format("Date of report ({0}) is less than the onset date ({1}).",
                            c.DateReport.Value.ToShortDateString(), c.DateOnset.Value.ToShortDateString())));

                    if (c.DateReport.HasValue && c.DateReport > DateTime.Now)
                        IssueList.Add(new Issue(c.ID, "1004", String.Format("Date of report ({0}) occurs in the future.",
                            c.DateReport.Value.ToShortDateString())));

                    if (c.DateOnset.HasValue && c.DateOnset > DateTime.Now)
                        IssueList.Add(new Issue(c.ID, "1005", String.Format("Date of onset ({0}) occurs in the future.",
                            c.DateOnset.Value.ToShortDateString())));

                    if (c.DateDeathCurrentOrFinal.HasValue && c.DateDeathCurrentOrFinal > DateTime.Now)
                        IssueList.Add(new Issue(c.ID, "1006", String.Format("Date of death ({0}) occurs in the future.",
                            c.DateDeathCurrentOrFinal.Value.ToShortDateString())));

                    if (c.DateIsolationCurrent.HasValue && c.DateIsolationCurrent > DateTime.Now)
                        IssueList.Add(new Issue(c.ID, "1007", String.Format("Date of isolation ({0}) occurs in the future.",
                            c.DateIsolationCurrent.Value.ToShortDateString())));

                    if (c.DateDeath2.HasValue && (c.FinalStatus == Core.Enums.AliveDead.None || c.FinalStatus == Core.Enums.AliveDead.Alive))
                        IssueList.Add(new Issue(c.ID, "1008", String.Format("Final date of death ({0}) is specified, but the final status is not dead.",
                            c.DateDeath2.Value.ToShortDateString())));

                    if (c.TraditionalHealerDate.HasValue && c.TraditionalHealerDate > DateTime.Now)
                        IssueList.Add(new Issue(c.ID, "1009", String.Format("Date of visiting a traditional healer ({0}) occurs in the future.",
                            c.TraditionalHealerDate.Value.ToShortDateString())));

                    if (c.DateOnset.HasValue && c.DateDeathCurrentOrFinal.HasValue && c.DateOnset > c.DateDeathCurrentOrFinal)
                        IssueList.Add(new Issue(c.ID, "1010", String.Format("Date of onset ({0}) ocurrs after either the current or final date of death.",
                            c.DateOnset.Value.ToShortDateString())));

                    if (c.DateDeath.HasValue && c.DateDeath2.HasValue && c.DateDeath != c.DateDeath2)
                        IssueList.Add(new Issue(c.ID, "1011", String.Format("A current ({0}) and final ({1}) date of death exist, but they are inconsistent.",
                            c.DateDeath.Value.ToShortDateString(), c.DateDeath2.Value.ToShortDateString())));

                    if (c.DateDeath.HasValue && !c.DateDeath2.HasValue)
                        IssueList.Add(new Issue(c.ID, "1012", String.Format("A current ({0}) date of death exists, but no final date of death exists.",
                            c.DateDeath.Value.ToShortDateString())));

                    if (c.DateDeathCurrentOrFinal.HasValue && c.DateIsolationCurrent.HasValue && c.DateIsolationCurrent.Value > c.DateDeathCurrentOrFinal.Value)
                        IssueList.Add(new Issue(c.ID, "1013", String.Format("The date of current isolation ({0}) occurs after the date of death.",
                            c.DateIsolationCurrent.Value.ToShortDateString(), c.DateDeathCurrentOrFinal.Value.ToShortDateString())));

                    if (c.DateIsolationCurrent.HasValue && !c.DateHospitalCurrentAdmit.HasValue)
                        IssueList.Add(new Issue(c.ID, "1014", String.Format("The date of current isolation ({0}) exists but the case does not have a hospital admission date.",
                            c.DateIsolationCurrent.Value.ToShortDateString())));

                    if (c.DateIsolationCurrent.HasValue && (String.IsNullOrEmpty(c.IsolationCurrent) || c.IsolationCurrent == "2"))
                        IssueList.Add(new Issue(c.ID, "1015", String.Format("A date of current isolation ({0}) exists but the case's isolation status is not marked as 'Yes'.",
                            c.DateIsolationCurrent.Value.ToShortDateString())));

                    if (c.DateDeathCurrentOrFinal.HasValue && (String.IsNullOrEmpty(c.CurrentStatus) || c.CurrentStatus == Properties.Resources.Alive))
                        IssueList.Add(new Issue(c.ID, "1016", String.Format("A date of death was recorded ({0}) but the case status is not reported as dead.",
                            c.DateDeathCurrentOrFinal.Value.ToShortDateString())));

                    if (c.Contacts.Count == 0 && (c.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed || c.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable))
                        IssueList.Add(new Issue(c.ID, "1017", String.Format("Case is classified '{0}' in district '{1}' but has no contacts.",
                            c.EpiCaseClassification, c.District)));

                    if (String.IsNullOrEmpty(c.ID))
                    {
                        IssueList.Add(new Issue(c.ID, "1018", "A case ID does not exist."));
                    }
                    else
                    {
                        string ID = c.ID;
                        if (!ID.Contains(CaseViewModel.IDSeparator))
                        {
                            string errorMessage = "The case ID field is incorrectly formatted. The separator character '" + CaseViewModel.IDSeparator + "' is required.";
                            IssueList.Add(new Issue(c.ID, "1019", errorMessage));
                        }
                        else
                        {
                            int position = ID.IndexOf(CaseViewModel.IDSeparator);
                            string prefix = ID.Substring(0, position);
                            int prefixLength = prefix.Length;

                            string pattern = ID.Substring(position + 1);

                            double caseNumber = -1;
                            bool success = double.TryParse(pattern, out caseNumber);

                            //if (prefixLength != CaseViewModel.IDPrefix.Length)
                            //{
                            //    string errorMessage = "The ID prefix has an incorrect length. Please ensure you are using the ID format " + CaseViewModel.IDPrefix + CaseViewModel.IDSeparator + CaseViewModel.IDPattern;
                            //    IssueList.Add(new Issue(c.ID, "1020", errorMessage));
                            //}
                            //else if (prefix != CaseViewModel.IDPrefix)
                            //{
                            //    string errorMessage = "The ID prefix is incorrect. Please ensure you are using the ID format " + CaseViewModel.IDPrefix + CaseViewModel.IDSeparator + CaseViewModel.IDPattern;
                            //    IssueList.Add(new Issue(c.ID, "1021", errorMessage));
                            //}
                            //else if (!success)
                            //{
                            //    string errorMessage = "The ID suffix must be a valid number. Please ensure you are using the ID format " + CaseViewModel.IDPrefix + CaseViewModel.IDSeparator + CaseViewModel.IDPattern;
                            //    IssueList.Add(new Issue(c.ID, "1022", errorMessage));
                            //}
                            //else if (pattern.Length != CaseViewModel.IDPattern.Length)
                            //{
                            //    string errorMessage = "The ID suffix does not have a valid number of characters. Please ensure you are using the ID format " + CaseViewModel.IDPrefix + CaseViewModel.IDSeparator + CaseViewModel.IDPattern;
                            //    IssueList.Add(new Issue(c.ID, "1023", errorMessage));
                            //}
                        }
                    }

                    if (c.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed && c.FinalLabClass != Core.Enums.FinalLabClassification.ConfirmedConvalescent &&
                        c.FinalLabClass != Core.Enums.FinalLabClassification.ConfirmedAcute)
                        IssueList.Add(new Issue(c.ID, "1024", String.Format("Epi case classification is {0} and final lab classification is {1}.",
                            c.EpiCaseClassification, c.FinalLabClass)));

                    DataTable labTable = LabTable;
                    DataRow[] rows = labTable.Select("[FKEY] = '" + c.RecordId + "'");

                    foreach (DataRow row in rows)
                    {
                        string sampleInterpret = row["SampleInterpret"].ToString();
                        if ((sampleInterpret == "1" || sampleInterpret.ToString() == "2") && c.EpiCaseDef != Core.Enums.EpiCaseClassification.Confirmed)
                        {
                            IssueList.Add(new Issue(c.ID, "1025", String.Format("Epi case classification is {0} and at least one lab record for this case is {1}.",
                                c.EpiCaseClassification, sampleInterpret)));
                            break;
                        }
                        else if ((sampleInterpret == "1" || sampleInterpret.ToString() == "2") && (c.FinalLabClass != Core.Enums.FinalLabClassification.ConfirmedAcute && c.FinalLabClass != Core.Enums.FinalLabClassification.ConfirmedConvalescent))
                        {
                            IssueList.Add(new Issue(c.ID, "1026", String.Format("Final lab classification is {0} and at least one lab record for this case is {1}.",
                                c.FinalLabClass, sampleInterpret)));
                            break;
                        }
                    }

                    if (rows.Length == 0 && c.FinalLabClass != Core.Enums.FinalLabClassification.None)
                    {
                        // this means case has a final lab class, but no lab records
                        IssueList.Add(new Issue(c.ID, "1027", String.Format("Final lab classification is {0}, but the patient has no lab records in the database.",
                                c.FinalLabClass)));
                    }

                    if (rows.Length > 5)
                    {
                        // this means case has more than 5 lab records, which may be unusual
                        IssueList.Add(new Issue(c.ID, "1028", String.Format("This case has {0} lab records on file.",
                                rows.Length.ToString())));
                    }

                    if (OutbreakDate.HasValue)
                    {
                        DateTime past180 = (OutbreakDate.Value).AddDays(-180);
                        DateTime past365 = (OutbreakDate.Value).AddDays(-365);

                        if (c.DateReport.HasValue && c.DateReport.Value < past180)
                            IssueList.Add(new Issue(c.ID, "1029", String.Format("Date of report ({0}) occurs more than 180 days prior to the date of outbreak detection ({1}).",
                                c.DateReport.Value.ToShortDateString(), OutbreakDate.Value.ToShortDateString())));

                        if (c.DateOnset.HasValue && c.DateOnset.Value < past365)
                            IssueList.Add(new Issue(c.ID, "1030", String.Format("Date of onset ({0}) occurs more than 365 days prior to the date of outbreak detection ({1}).",
                                c.DateOnset.Value.ToShortDateString(), OutbreakDate.Value.ToShortDateString())));
                    }

                    if (c.DateOnset.HasValue)
                    {
                        DateTime onsetDt = c.DateOnset.Value;
                        //DateTime onsetDt = c.DateOnset.Value;
                        //Query selectQuery = db.CreateQuery("SELECT * FROM [metaLinks] WHERE [ToViewId] = @ToViewId AND [FromViewId] = @FromViewId AND [ToRecordGuid] = @ToRecordGuid ORDER BY [LastContactDate] ASC");
                        //selectQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.String, CaseFormId));
                        //selectQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.String, CaseFormId));
                        //selectQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, c.RecordId));

                        //DataTable linkTable = db.Select(selectQuery);

                        DataRow[] filteredRows = linkTable.Select("[ToRecordGuid] = '" + c.RecordId + "'");

                        //foreach (DataRow row in linkTable.Rows)
                        foreach (DataRow row in filteredRows)
                        {
                            DateTime lastContactDate = Convert.ToDateTime(row["LastContactDate"]);
                            string fromCaseId = row["FromRecordGuid"].ToString();
                            if (lastContactDate < onsetDt)
                            {
                                CaseViewModel fromCase = GetCaseVM(fromCaseId);
                                IssueList.Add(new Issue(c.ID, "1031", String.Format("This case was exposed by case {0}, but the date of exposure occurs prior to the date of onset of symptoms ({1}).",
                                    fromCase.ID, c.DateOnset.Value.ToShortDateString())));
                            }
                        }

                        filteredRows = linkTableContacts.Select("[FromRecordGuid] = '" + c.RecordId + "'");

                        foreach (DataRow row in filteredRows)
                        {
                            if (row["LastContactDate"] == DBNull.Value)
                            {
                                IssueList.Add(new Issue(c.ID, "1032", "This case has a case-contact relationship that lacks a last date of contact."));
                            }
                            else
                            {
                                DateTime lastContactDate = Convert.ToDateTime(row["LastContactDate"]);
                                string toCaseGuid = row["ToRecordGuid"].ToString();
                                if (lastContactDate < onsetDt)
                                {
                                    ContactViewModel toContact = GetContactVM(toCaseGuid);

                                    IssueList.Add(new Issue(c.ID, "1033", String.Format("This case has a contact ({0}), but the last date of contact for this relationship occurs prior to the date of onset of symptoms.",
                                        toContact.ContactID)));
                                }
                            }
                        }
                    }

                    if (c.FuneralStartDate1.HasValue && c.FuneralEndDate1.HasValue && c.FuneralStartDate1.Value > c.FuneralEndDate1.Value)
                        IssueList.Add(new Issue(c.ID, "1034", String.Format("The start date of first funeral attendance ({0}) occurs after the end date.",
                            c.FuneralStartDate1.Value.ToShortDateString())));

                    if (c.FuneralStartDate2.HasValue && c.FuneralEndDate2.HasValue && c.FuneralStartDate2.Value > c.FuneralEndDate2.Value)
                        IssueList.Add(new Issue(c.ID, "1035", String.Format("The start date of first funeral attendance ({0}) occurs after the end date.",
                            c.FuneralStartDate2.Value.ToShortDateString())));

                    if (c.SymptomFever == "1" && c.SymptomFeverTemp.HasValue)
                    {
                        IssueList.Add(new Issue(c.ID, "1036", String.Format("This case is listed as having a fever temperature but 'fever' is not selected as a symptom.")));
                    }

                    if (c.DateIsolationCurrent.HasValue && c.DateDischargeIso.HasValue && c.DateIsolationCurrent.Value > c.DateDischargeIso.Value)
                        IssueList.Add(new Issue(c.ID, "1037", String.Format("The date of admission to isolation occurs after the date of discharge from isolation.")));

                    if (c.DateOnset.HasValue && c.DateDischargeIso.HasValue && c.DateOnset.Value > c.DateDischargeIso.Value)
                        IssueList.Add(new Issue(c.ID, "1038", String.Format("The date of onset occurs after the date of discharge from isolation.")));

                    if (c.AgeYears.HasValue && c.AgeYears.Value > 20 && c.OccupationChild == true)
                        IssueList.Add(new Issue(c.ID, "1039", String.Format("The case's age in years is {0} but the case's occupation is 'Child'.", c.AgeYears.Value)));

                    if (c.Gender == Core.Enums.Gender.Male && c.OccupationHousewife == true)
                        IssueList.Add(new Issue(c.ID, "1040", String.Format("The case's gender is male, but the case's occupation is 'Housewife'.")));

                    if (c.SymptomOtherNonHemorrhagic == "1" && String.IsNullOrEmpty(c.SymptomOtherNonHemorrhagicSpecify))
                        IssueList.Add(new Issue(c.ID, "1041", String.Format("The clinical signs and symptoms in section 2 of the case report form indicates other non-hemorrhagic clinical symptoms. However, no such symptoms are specified.")));

                    if (c.SymptomOtherHemo == "1" && String.IsNullOrEmpty(c.SymptomOtherHemoSpecify))
                        IssueList.Add(new Issue(c.ID, "1042", String.Format("The clinical signs and symptoms in section 2 of the case report form indicates other hemorrhagic symptoms. However, no such symptoms are specified.")));

                    if (c.SymptomUnexplainedBleeding == "1" &&
                        String.IsNullOrEmpty(c.SymptomBleedGums) &&
                        String.IsNullOrEmpty(c.SymptomBleedInjectionSite) &&
                        String.IsNullOrEmpty(c.SymptomNoseBleed) &&
                        String.IsNullOrEmpty(c.SymptomBloodyStool) &&
                        String.IsNullOrEmpty(c.SymptomHematemesis) &&
                        String.IsNullOrEmpty(c.SymptomBloodVomit) &&
                        String.IsNullOrEmpty(c.SymptomCoughBlood) &&
                        String.IsNullOrEmpty(c.SymptomBleedVagina) &&
                        String.IsNullOrEmpty(c.SymptomBleedSkin) &&
                        String.IsNullOrEmpty(c.SymptomBleedUrine) &&
                        String.IsNullOrEmpty(c.SymptomOtherHemoSpecify) &&
                        String.IsNullOrEmpty(c.SymptomOtherNonHemorrhagicSpecify)
                        )
                        IssueList.Add(new Issue(c.ID, "1043", String.Format("The clinical signs and symptoms in section 2 of the case report form indicates unexplained bleeding. However, no such symptoms are specified.")));

                    if (c.Animals == "1" &&
                        c.AnimalBats == false &&
                        c.AnimalPrimates == false &&
                        c.AnimalRodents == false &&
                        c.AnimalPigs == false &&
                        c.AnimalBirds == false &&
                        c.AnimalCows == false &&
                        c.AnimalOther == false
                        )
                        IssueList.Add(new Issue(c.ID, "1044", String.Format("The patient indicated direct contact with animals, but no animal types are specified.")));

                    if (c.District == String.Empty && (c.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed || c.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable || c.EpiCaseDef == Core.Enums.EpiCaseClassification.Suspect))
                        IssueList.Add(new Issue(c.ID, "1045", String.Format("This case has an epi case definition of {0} but does not have a district of residence.", c.EpiCaseClassification)));

                    TaskbarProgressValue = TaskbarProgressValue + (1 / denominator);

                    //RaisePropertyChanged("IssueCollection");
                });
                #endregion // Tests

                sw.Stop();
                string elapsed = sw.Elapsed.TotalSeconds.ToString();
            }
            catch (Exception)
            {
                // TODO: ??
            }
            finally
            {
                TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                TaskbarProgressValue = 0;
                if (IssueDataPopulated != null)
                {
                    IssueDataPopulated(this, new EventArgs());
                }

                sw.Stop();
                string elapsed = sw.Elapsed.TotalSeconds.ToString();
            }
        }

        protected override void RunInitialSetup(bool showSetupScreen)
        {
            if (InitialSetupRun != null)
            {
                InitialSetupArgs args = new InitialSetupArgs(showSetupScreen);
                InitialSetupRun(this, args);
            }
        }

        private void UpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!IsShowingDataExporter && !IsExportingData)
            {
                if (RefreshOnTick && RefreshRequired != null && !IsShowingCaseReportForm)
                {
                    RefreshOnTick = false;
                    RefreshRequired(this, new EventArgs());
                }

                if (!IsCheckingServerForUpdates && !IsSendingServerUpdates && !IsDataSyncing)
                {
                    using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Database.ConnectionString + ";Connection Timeout=1"))
                    {
                        try
                        {
                            conn.Open();

                            using (System.Data.SqlClient.SqlCommand comm = new System.Data.SqlClient.SqlCommand("SELECT COUNT(*) FROM [metaDbInfo]", conn))
                            {
                                object count = comm.ExecuteScalar();
                            }
                            IsConnected = true;
                        }
                        catch (System.Data.SqlClient.SqlException)
                        {
                            IsConnected = false;
                            return;
                        }
                        finally
                        {
                            conn.Close();
                        }
                    }

                    PollServerForUpdatesAsync();
                }
            }
        }

        private void ExecuteServerChangesetMessage(ServerUpdateMessage message)
        {
            switch (message.UpdateType)
            {
                case ServerUpdateType.LockAllClientIsRefreshing:
                    IsWaitingOnOtherClients = true;
                    break;
                case ServerUpdateType.UnlockAllClientRefreshComplete:
                    IsWaitingOnOtherClients = false;
                    break;
                case ServerUpdateType.LockCase:
                    CaseViewModel caseToLock = GetCaseVM(message.RecordID);
                    if (caseToLock != null)
                    {
                        caseToLock.IsLocked = true;

                        foreach (ContactViewModel contactVM in caseToLock.Contacts)
                        {
                            contactVM.IsLocked = true;
                        }
                    }
                    break;
                case ServerUpdateType.UnlockCase:
                    CaseViewModel caseToUnlock = GetCaseVM(message.RecordID);
                    if (caseToUnlock != null)
                    {
                        caseToUnlock.IsLocked = false;

                        foreach (ContactViewModel contactVM in caseToUnlock.Contacts)
                        {
                            contactVM.IsLocked = false;
                        }
                    }
                    break;
                case ServerUpdateType.LockContact:
                    ContactViewModel contactToLock = GetContactVM(message.RecordID);
                    if (contactToLock != null)
                    {
                        contactToLock.IsLocked = true;

                        foreach (CaseViewModel caseVM in CaseCollection)
                        {
                            if (caseVM.Contacts.Contains(contactToLock))
                            {
                                caseVM.IsLocked = true;
                            }
                        }
                    }
                    break;
                case ServerUpdateType.UnlockContact:
                    ContactViewModel contactToUnlock = GetContactVM(message.RecordID);
                    if (contactToUnlock != null)
                    {
                        contactToUnlock.IsLocked = false;

                        foreach (CaseViewModel caseVM in CaseCollection)
                        {
                            if (caseVM.Contacts.Contains(contactToUnlock))
                            {
                                caseVM.IsLocked = false;
                            }
                        }
                    }
                    break;
                case ServerUpdateType.UpdateCase:
                    string updatedCaseGuid = message.RecordID;
                    UpdateOrAddCaseExecute(updatedCaseGuid);
                    break;
                case ServerUpdateType.AddCase:
                    string newCaseGuid = message.RecordID;
                    CaseViewModel c = CreateCaseFromGuid(newCaseGuid);
                    lock (_caseCollectionLock)
                    {
                        if (!CaseCollection.Contains(c.RecordId))
                        {
                            CaseCollection.Add(c);
                        }
                    }
                    break;
                case ServerUpdateType.DeleteCase:
                    RefreshOnTick = true;
                    break;
                case ServerUpdateType.UpdateContact:
                    RefreshContact(message.RecordID);
                    break;
                case ServerUpdateType.AddContact:
                    AddContactFromServerMessage(message.RecordID);
                    break;
                case ServerUpdateType.DeleteContact:

                    // unlock first!
                    ContactViewModel contactToDeleteAndUnlock = GetContactVM(message.RecordID);
                    if (contactToDeleteAndUnlock != null)
                    {
                        contactToDeleteAndUnlock.IsLocked = false;

                        foreach (CaseViewModel caseVM in CaseCollection)
                        {
                            if (caseVM.Contacts.Contains(contactToDeleteAndUnlock))
                            {
                                caseVM.IsLocked = false;
                            }
                        }
                    }

                    // then remove!
                    RemoveContactFromCollections(message.RecordID);

                    break;
                case ServerUpdateType.UpdateCaseContactRelationship:
                    RefreshOnTick = true;
                    break;
                case ServerUpdateType.UpdateFollowUpAndForceRefresh:
                    RefreshOnTick = true;
                    break;
                case ServerUpdateType.UpdateFollowUp:
                    RefreshFollowUp(message.RecordID);
                    break;
                case ServerUpdateType.DataImported:
                    RefreshOnTick = true;
                    break;
            }
        }

        private void AddContactFromServerMessage(string contactGuid)
        {
            if (ContactCollection == null)
                return;

            // Check to see if the contact exists (it shouldn't)
            bool found = false;
            ContactViewModel c = GetContactVM(contactGuid);
            if (c != null)
            {
                // the contact already exists, don't add it; ideally we'd never be here, of course
                // TODO: throw exception??
                found = true;
                return;
            }

            // When the contact isn't found, create a new contact view model and add it to the main collection of contacts
            if (!found)
            {
                ContactViewModel contact = CreateContactFromGuid(contactGuid);
                ContactViewModel contactVM = new ContactViewModel(contact);

                // Link the contact to its source case
                Query selectQuery = Database.CreateQuery("SELECT * FROM [metaLinks] WHERE ToRecordGuid = @ToRecordGuid AND ToViewId = @ToViewId AND FromViewId = @FromViewId ORDER BY [LastContactDate] DESC");
                selectQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, contactGuid));
                selectQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, ContactFormId));
                selectQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, CaseFormId));
                DataTable linkTable = Database.Select(selectQuery);

                if (linkTable.Rows.Count >= 1) // should never be zero
                {
                    DataRow row = linkTable.Rows[0];

                    contactVM.DateOfLastContact = (DateTime)row["LastContactDate"];
                    string caseGuid = row["FromRecordGuid"].ToString();
                    CaseViewModel caseVM = GetCaseVM(caseGuid);

                    if (caseVM != null) // should never be null
                    {
                        lock (_contactCollectionLock)
                        {
                            ContactCollection.Add(contactVM);
                        }

                        if (!caseVM.Contacts.Contains(contactVM))
                        {
                            caseVM.Contacts.Add(contactVM);
                        }
                        LoadContactLinkData(row, contactVM, caseVM);
                        CheckCaseContactForDailyFollowUp(caseVM, contactVM, DateTime.Now);
                        SortFollowUps(DailyFollowUpCollection);
                    }
                }
            }
        }

        private async void RefreshFollowUp(string contactGuid)
        {
            await Task.Factory.StartNew(delegate
            {
                ContactViewModel contactVM = GetContactVM(contactGuid);
                CaseViewModel lastSourceCase = contactVM.LastSourceCase;

                if (lastSourceCase == null)
                {
                    return;
                }

                Query selectQuery = Database.CreateQuery("SELECT * FROM [metaLinks] WHERE [ToViewId] = @ToViewId AND [FromViewId] = @FromViewId AND [ToRecordGuid] = @ToRecordGuid AND [FromRecordGuid] = @FromRecordGuid ORDER BY [LastContactDate] DESC");
                selectQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, ContactFormId));
                selectQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, CaseFormId));
                selectQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, contactGuid));
                selectQuery.Parameters.Add(new QueryParameter("@FromRecordGuid", DbType.String, lastSourceCase.RecordId));
                DataTable linksTable = Database.Select(selectQuery);

                if (linksTable.Rows.Count > 0)
                {
                    DataRow row = linksTable.Rows[0];

                    FollowUpWindowViewModel followUpWindow = contactVM.FollowUpWindowViewModel;

                    int day = 1;
                    foreach (FollowUpVisitViewModel fuVM in followUpWindow.FollowUpVisits)
                    {
                        string notes = row["Day" + day.ToString() + "Notes"].ToString();
                        fuVM.Notes = notes;
                        if (!string.IsNullOrEmpty(row["Temp1_" + day.ToString()].ToString()))
                        {
                            double temp = 0.0;
                            if (Double.TryParse(row["Temp1_" + day.ToString()].ToString(), out temp))
                                if (temp != 0.0)
                                    fuVM.Temp1 = temp;
                        }
                        if (!string.IsNullOrEmpty(row["Temp2_" + day.ToString()].ToString()))
                        {
                            double temp = 0.0;
                            if (Double.TryParse(row["Temp2_" + day.ToString()].ToString(), out temp))
                                if (temp != 0.0)
                                    fuVM.Temp2 = temp;
                        }
                        if (row["Day" + day.ToString()] != DBNull.Value)
                        {
                            fuVM.Status = (ContactDailyStatus)(Enum.Parse(typeof(ContactDailyStatus), row["Day" + day.ToString()].ToString()));
                        }
                        else
                        {
                            fuVM.Status = null;
                        }

                        day++;
                    }

                    CheckCaseContactForDailyFollowUps(contactVM, DateTime.Today, true);

                    if (FollowUpVisitUpdated != null)
                    {
                        FollowUpVisitUpdated(this, new EventArgs());
                    }
                }
            });
        }

        private async void RefreshContact(string contactGuid)
        {
            await Task.Factory.StartNew(delegate
            {
                ContactViewModel contactFromMemory = GetContactVM(contactGuid);
                ContactViewModel contactFromDb = CreateContactFromGuid(contactGuid);

                if (contactFromDb.FinalOutcome.Equals(contactFromMemory.FinalOutcome, StringComparison.OrdinalIgnoreCase))
                {
                    contactFromMemory.UpdateContactFormDataOnly(contactFromDb);
                }
                else
                {
                    RefreshOnTick = true;
                }
            });
        }

        private async void PollServerForUpdatesAsync()
        {
            await Task.Factory.StartNew(delegate
            {
                if (Database == null)
                {
                    return;
                }

#if DEBUG
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();
#endif
                // conduct poll
                DataTable dt = new DataTable();

                if (IsMicrosoftSQLDatabase)
                {
                    if (IsConnected == false)
                    {
                        return;
                    }

                    Query selectQuery = Database.CreateQuery("SELECT * FROM Changesets WHERE Changeset > @Changeset AND MACADDR <> @MACADDR");
                    selectQuery.Parameters.Add(new QueryParameter("@Changeset", DbType.Int32, Changeset));
                    selectQuery.Parameters.Add(new QueryParameter("@MACADDR", DbType.String, MacAddress));
                    try
                    {
                        dt = Database.Select(selectQuery);
                        //IsConnected = true;
                    }
                    catch (System.Data.SqlClient.SqlException)
                    {
                        IsConnected = false;
                        dt = new DataTable();
                    }
                    catch (ApplicationException ex)
                    {
                        if (ex.InnerException != null && ex.InnerException is System.Data.SqlClient.SqlException)
                        {
                            string message = ex.InnerException.Message;
                            if (message.Equals("A transport-level error has occurred when receiving results", StringComparison.OrdinalIgnoreCase))
                            {
                                dt = new DataTable();
                                IsConnected = false;
                            }
                        }
                    }
                }

                if (dt.Rows.Count > 0)
                {
                    IsConnected = true;
                    IsCheckingServerForUpdates = true;

                    Queue<ServerUpdateMessage> updateQueue = new Queue<ServerUpdateMessage>();

                    foreach (DataRow row in dt.Rows)
                    {
                        updateQueue.Enqueue(CreateServerUpdateMessage(row));
                    }

                    do
                    {
                        ServerUpdateMessage message = (ServerUpdateMessage)updateQueue.Dequeue();

                        // advance the client's current changeset # to the one in the message
                        if (message.Changeset > Changeset)
                        {
                            Changeset = message.Changeset;
                        }

                        ExecuteServerChangesetMessage(message);

                    } while (updateQueue.Count != 0);
                }
#if DEBUG
                sw.Stop();
                double elapsed = sw.Elapsed.TotalMilliseconds;
#endif
            }).ContinueWith(delegate
            {
                IsCheckingServerForUpdates = false;
            });
        }

        protected virtual void UpdateDatesOfLastContact()
        {
            foreach (ContactViewModel contactVM in ContactCollection)
            {
                UpdateDateOfLastContact(contactVM);
            }
        }

        protected virtual void UpdateDatesOfLastContact(DateTime maxDate, bool initialLoad = false)
        {
            Parallel.ForEach(ContactCollection, contactVM =>
            {
                UpdateDateOfLastContact(contactVM, maxDate, initialLoad);
            });
        }

        private void UpdateDatesOfLastContactForSourceCase(CaseViewModel caseVM)
        {
            foreach (ContactViewModel contactVM in caseVM.Contacts)
            {
                UpdateDateOfLastContact(contactVM);
            }
        }

        private async void UpdateDatesOfLastContactForSourceCaseAsync(CaseViewModel caseVM)
        {
            await Task.Factory.StartNew(delegate
            {
                foreach (ContactViewModel contactVM in caseVM.Contacts)
                {
                    UpdateDateOfLastContact(contactVM);
                }
            });
        }

        private void UpdateDateOfLastContact(ContactViewModel contactVM)
        {
            UpdateDateOfLastContact(contactVM, DateTime.Now.AddDays(10));
        }

        private void UpdateDateOfLastContact(ContactViewModel contactVM, DateTime maxDate, bool isInitialLoad = false)
        {
            if (isInitialLoad)
            {
                // This method of look-up is ONLY safe on initial load of the data
                if (ContactLinkCollection.ContainsContact(contactVM))
                {
                    CaseContactPairViewModel ccpVM = ContactLinkCollection[contactVM];
                    contactVM.DateOfLastContact = ccpVM.DateLastContact;
                    contactVM.LastSourceCase = ccpVM.CaseVM;
                    contactVM.LastSourceCaseContactTypes = ccpVM.ContactTypeString;
                    contactVM.RelationshipToLastSourceCase = ccpVM.Relationship;
                }
            }
            else
            {
                DateTime lastDate = DateTime.MinValue;
                CaseViewModel caseVM = null;
                string relationship = String.Empty;
                string contactTypes = String.Empty;

                var query = from ccp in ContactLinkCollection
                            where (ccp.CaseVM.EpiCaseDef != EpiCaseClassification.NotCase)
                            select ccp;

                int validCases = query.Count();

                if (validCases == 0)
                {
                    query = from ccp in ContactLinkCollection
                            select ccp;
                }

                foreach (CaseContactPairViewModel ccp in query)
                {
                    if (ccp.ContactVM == contactVM && ccp.DateLastContact > lastDate)
                    {
                        lastDate = ccp.DateLastContact;
                        caseVM = ccp.CaseVM;
                        relationship = ccp.Relationship;
                        contactTypes = ccp.ContactTypeString;

                        if (lastDate >= maxDate)
                        {
                            //we found the highest, there is no need to keep looking
                            break;
                        }
                    }
                }

                contactVM.RelationshipToLastSourceCase = relationship;
                contactVM.LastSourceCaseContactTypes = contactTypes;
                contactVM.DateOfLastContact = lastDate;
                if (caseVM != null) // should never be null!
                {
                    contactVM.LastSourceCase = caseVM;
                }
            }
        }

        /// <summary>
        /// Used to determine if a contact exists based on the contact's global record ID value
        /// </summary>
        /// <param name="recordId">The global record ID value of the contact</param>
        /// <returns>bool; represents whether the contact was found</returns>
        public bool DoesContactExist(string recordId)
        {
            ContactViewModel contactVM = GetContactVM(recordId);
            if (contactVM != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Used to determine if a contact exists based on the contact's global record ID value
        /// </summary>
        /// <param name="recordId">The global record ID value of the contact</param>
        /// <returns>bool; represents whether the contact was found</returns>
        public bool DoesCaseExist(string recordId)
        {
            CaseViewModel caseVM = GetCaseVM(recordId);
            if (caseVM != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Used to get a CaseViewModel object from a given global record ID value
        /// </summary>
        /// <param name="recordId">The global record ID value of the case</param>
        /// <returns>CaseViewModel</returns>
        public CaseViewModel GetCaseVM(string recordId)
        {
            if (CaseCollection.Contains(recordId))
            {
                return CaseCollection[recordId];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Used to get a list of all duplicate cases, using the ID field as the match value.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<CaseViewModel> GetDuplicateCasesBasedOnID()
        {
            List<CaseViewModel> duplicates = new List<CaseViewModel>();
            foreach (CaseViewModel iCaseVM in CaseCollection)
            {
                string iID = iCaseVM.ID;

                foreach (CaseViewModel jCaseVM in CaseCollection)
                {
                    if (jCaseVM != iCaseVM)
                    {
                        string jID = jCaseVM.ID;

                        if (jID.Equals(iID, StringComparison.OrdinalIgnoreCase))
                        {
                            if (!duplicates.Contains(iCaseVM) && !duplicates.Contains(jCaseVM) && !String.IsNullOrEmpty(jID))
                            {
                                duplicates.Add(iCaseVM);
                            }
                        }
                    }
                }
            }

            return duplicates;
        }

        private async void FlagDuplicateCasesBasedOnIDAsync()
        {
            List<CaseViewModel> duplicates = new List<CaseViewModel>();

            await Task.Factory.StartNew(delegate
            {
                //foreach (CaseViewModel iCaseVM in CaseCollection)
                //{
                //    string iID = iCaseVM.ID;

                //    foreach (CaseViewModel jCaseVM in CaseCollection)
                //    {
                //        if (jCaseVM != iCaseVM)
                //        {
                //            string jID = jCaseVM.ID;

                //            if (jID == iID)
                //            {
                //                if (!duplicates.Contains(iCaseVM) && !duplicates.Contains(jCaseVM) && !String.IsNullOrEmpty(jID))
                //                {
                //                    duplicates.Add(iCaseVM);
                //                }
                //            }
                //        }
                //    }
                //}
            });

            if (duplicates.Count > 0)
            {
                if (DuplicateIdDetected != null)
                {
                    DuplicateIdDetected(this, new DuplicateIdDetectedArgs(duplicates));
                }
            }
        }

        /// <summary>
        /// Checks to see if adding a source case-to-case relationship would cause a circular relationship. This is designed
        /// to help prevent scenarios where two cases are each other's source cases, which should never happen.
        /// </summary>
        /// <param name="proposedExposure">The proposed exposure</param>
        /// <param name="proposedSource">The proposed source</param>
        /// <returns>bool; whether the proposed relationship would be circular</returns>
        public bool CheckForCircularRelationship(CaseViewModel proposedExposure, CaseViewModel proposedSource)
        {
            Query selectQuery = Database.CreateQuery("SELECT * FROM [metaLinks] WHERE [ToViewId] = @ToViewId AND [FromViewId] = @FromViewId " +
                " AND [ToRecordGuid] = @ToRecordGuid AND [FromRecordGuid] = @FromRecordGuid");
            selectQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int16, CaseFormId));
            selectQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int16, CaseFormId));
            selectQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, proposedSource.RecordId));
            selectQuery.Parameters.Add(new QueryParameter("@FromRecordGuid", DbType.String, proposedExposure.RecordId));

            DataTable dt = Database.Select(selectQuery);

            if (dt.Rows.Count > 0)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks to see if adding a source case-to-case relationship would cause a duplicate relationship. This is designed
        /// to help prevent scenarios where the same case is added as a source case twice.
        /// </summary>
        /// <param name="proposedExposure">The proposed exposure</param>
        /// <param name="proposedSource">The proposed source</param>
        /// <returns>bool; whether the proposed relationship would be a duplicate</returns>
        public bool CheckForDuplicateCaseToCaseRelationship(CaseViewModel proposedExposure, CaseViewModel proposedSource)
        {
            Query selectQuery = Database.CreateQuery("SELECT * FROM [metaLinks] WHERE [ToViewId] = @ToViewId AND [FromViewId] = @FromViewId " +
                " AND [ToRecordGuid] = @ToRecordGuid AND [FromRecordGuid] = @FromRecordGuid");
            selectQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int16, CaseFormId));
            selectQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int16, CaseFormId));
            selectQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, proposedExposure.RecordId));
            selectQuery.Parameters.Add(new QueryParameter("@FromRecordGuid", DbType.String, proposedSource.RecordId));

            DataTable dt = Database.Select(selectQuery);

            if (dt.Rows.Count > 0)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the global record ID value for a case, based on a global record ID value for a lab record.
        /// </summary>
        /// <param name="labGuid"></param>
        /// <returns>string; represents the case global record id of the lab record</returns>
        public string GetCaseGuidForLabRecord(string labGuid)
        {
            Query selectQuery = Database.CreateQuery("SELECT [FKEY] FROM " + LabForm.TableName + " WHERE [GlobalRecordId] = @GlobalRecordId");
            selectQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, labGuid));
            DataTable dt = Database.Select(selectQuery);

            if (dt.Rows.Count == 1)
            {
                return dt.Rows[0]["FKEY"].ToString();
            }

            return String.Empty;
        }

        /// <summary>
        /// Used to get a ContactViewModel object from a given global record ID value
        /// </summary>
        /// <param name="recordId">The global record ID value of the contact</param>
        /// <returns>ContactViewModel</returns>
        public ContactViewModel GetContactVM(string recordId)
        {
            if (ContactCollection.Contains(recordId))
            {
                return ContactCollection[recordId];
            }
            else
            {
                return null;
            }
        }

        protected void CheckAndSetContactFinalStatusesOnInitialLoad()
        {
            double totalWork = 0.2;

            if (ContactCollection.Count == 0)
            {
                TaskbarProgressValue = TaskbarProgressValue + totalWork;
                return;
            }
            
            double count = CaseCollection.NotCase.Count;
            double completed = 0;

            foreach (CaseViewModel caseVM in CaseCollection.NotCase)
            {
                string caseGuid = caseVM.RecordId;
                ContactViewModel c = GetContactVM(caseGuid);

                if (c != null)
                {
                    // VHF-363 : does contact have any non-case source cases? we do not want to
                    // re-activate it if it has no confirmed/probable/suspect source cases.
                    bool hasRealSourceCases = false;
                    foreach (CaseViewModel iCaseVM in CaseCollection.Where(x => (x != caseVM && x.Contacts.Contains(c) && x.EpiCaseDef != EpiCaseClassification.NotCase)))
                    {
                        hasRealSourceCases = true;
                    }

                    if (c.DateOfLastContact.HasValue && /* VHF-363 */ hasRealSourceCases)
                    {
                        string prevContactFinalOutcome = c.FinalOutcome;
                        bool prevContactIsActive = c.IsActive;

                        if (c.IsWithin21DayWindow == true)
                        {
                            c.FinalOutcome = String.Empty;
                            c.IsActive = true;
                        }
                        else
                        {
                            c.FinalOutcome = "1";
                            c.IsActive = false;
                        }

                        if (prevContactIsActive != c.IsActive ||
                            !prevContactFinalOutcome.Equals(c.FinalOutcome, StringComparison.OrdinalIgnoreCase))
                        {
                            SetContactFinalStatusAsync(c);
                        }
                    }
                }

                foreach (ContactViewModel contactVM in caseVM.Contacts)
                {
                    bool wasExposedByOtherCases = false;
                    // if the contact has no other cases that exposed them...
                    foreach (CaseViewModel iCaseVM in CaseCollection)
                    {
                        if (iCaseVM != caseVM && iCaseVM.Contacts.Contains(contactVM) && !(iCaseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.NotCase))
                        {
                            wasExposedByOtherCases = true;
                            break;
                        }
                    }

                    if (!wasExposedByOtherCases)
                    {
                        string prevContactFinalOutcome = contactVM.FinalOutcome;
                        bool prevContactIsActive = contactVM.IsActive;

                        if (prevContactIsActive != contactVM.IsActive ||
                            !prevContactFinalOutcome.Equals(contactVM.FinalOutcome, StringComparison.OrdinalIgnoreCase))
                        {
                            // then DROP FROM FOLLOW UP
                            contactVM.IsActive = false;
                            contactVM.FinalOutcome = "3";
                            SetContactFinalStatusAsync(contactVM);
                            RaisePropertyChanged("ContactCollection");
                        }
                    }
                    //else
                    //{
                    //    contactVM.IsActive = true;
                    //}
                }
                //}

                completed++;
                TaskbarProgressValue = TaskbarProgressValue + ((completed / count) * totalWork) - ((--completed / count) * totalWork);
            }

            DateTime endOfToday = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 23, 59, 59);

            foreach (ContactViewModel contactVM in ContactCollection)
            {
                if (contactVM.IsActive == false
                        && contactVM.FinalOutcome.Equals("1")
                        //&& contactVM.FollowUpWindowViewModel.WindowEndDate > endOfToday
                        && contactVM.FollowUpWindowViewModel != null
                        && contactVM.FollowUpWindowViewModel.WindowStartDate <= DateTime.Now
                        && ((contactVM.FollowUpWindowViewModel.FollowUpVisits[ContactTracing.Core.Common.DaysInWindow - 1].Status.HasValue && contactVM.FollowUpWindowViewModel.FollowUpVisits[ContactTracing.Core.Common.DaysInWindow - 1].Status.Value != ContactDailyStatus.SeenNotSick) ||
                        contactVM.FollowUpWindowViewModel.FollowUpVisits[ContactTracing.Core.Common.DaysInWindow - 1].Status.HasValue == false))
                {
                    if (ContactHasConfirmedProbableSuspectPUISourceCases(contactVM))
                    {
                        contactVM.IsActive = true;
                        contactVM.FinalOutcome = String.Empty;
                        SetContactFinalStatus(contactVM);
                    }
                }
                else if (contactVM.IsActive == true
                    && String.IsNullOrEmpty(contactVM.FinalOutcome)
                    && contactVM.FollowUpWindowViewModel != null
                    && contactVM.FollowUpWindowViewModel.FollowUpVisits[ContactTracing.Core.Common.DaysInWindow - 1].Status.HasValue
                    && contactVM.FollowUpWindowViewModel.FollowUpVisits[ContactTracing.Core.Common.DaysInWindow - 1].Status.Value == ContactDailyStatus.SeenNotSick
                    && contactVM.FollowUpWindowViewModel.WindowEndDate >= endOfToday)
                {
                    contactVM.IsActive = false;
                    contactVM.FinalOutcome = "1";
                    SetContactFinalStatus(contactVM);
                }
                //else if (contactVM.IsActive == false
                //        && contactVM.FinalOutcome.Equals("1")
                //        && contactVM.FollowUpWindowViewModel.WindowStartDate <= DateTime.Now
                //        && ((contactVM.FollowUpWindowViewModel.FollowUpVisits[ContactTracing.Core.Common.DaysInWindow - 1].Status.HasValue && contactVM.FollowUpWindowViewModel.FollowUpVisits[ContactTracing.Core.Common.DaysInWindow - 1].Status.Value != ContactDailyStatus.SeenNotSick) ||
                //        contactVM.FollowUpWindowViewModel.FollowUpVisits[ContactTracing.Core.Common.DaysInWindow - 1].Status.HasValue == false))
                //{
                //    if (ContactHasConfirmedProbableSuspectPUISourceCases(contactVM))
                //    {
                //        contactVM.IsActive = true;
                //        contactVM.FinalOutcome = String.Empty;
                //        SetContactFinalStatus(contactVM);
                //    }
                //}
            }
        }

        protected bool ContactHasConfirmedProbableSuspectPUISourceCases(ContactViewModel contactVM)
        {
            foreach (CaseViewModel caseVM in CaseCollection
                            .Where(c => c.Contacts.Contains(contactVM))
                            .Where(c => c.EpiCaseDef == EpiCaseClassification.Confirmed || c.EpiCaseDef == EpiCaseClassification.Probable || c.EpiCaseDef == EpiCaseClassification.Suspect || c.EpiCaseDef == EpiCaseClassification.PUI))
            {
                return true;
            }
            return false;
        }

        protected void NotifyCaseDataPopulated()
        {
            if (CaseDataPopulated != null)
            {
                CaseDataPopulated(this, new CaseDataPopulatedArgs(Core.Enums.VirusTestTypes.Sudan, true));
            }
        }

        protected virtual async void SetContactFinalStatusAsync(ContactViewModel contactVM)
        {
            await Task.Factory.StartNew(delegate
            {
                Page page = ContactForm.Pages[0];
                Query updateQuery = Database.CreateQuery("UPDATE [" + page.TableName + "] SET [FinalOutcome] = @FinalOutcome WHERE [GlobalRecordId] = @GlobalRecordId");
                updateQuery.Parameters.Add(new QueryParameter("@FinalOutcome", DbType.String, contactVM.FinalOutcome));
                updateQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, contactVM.RecordId));
                int rows = Database.ExecuteNonQuery(updateQuery);
                if (rows == 0)
                {
                    DbLogger.Log("Exception: There was a problem setting the final status for contact " + contactVM.ContactID + ". No record for this contact was found to update.");
                    throw new InvalidOperationException("There was a problem setting the final status for contact " + contactVM.ContactID + ". No record for this contact was found to update.");
                }

                // make sure to update the last saved time - added 0.9.5.9
                DateTime currentTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, 0);

                updateQuery = Database.CreateQuery("UPDATE [" + ContactForm.TableName + "] SET [LastSaveTime] = @LastSaveTime WHERE [GlobalRecordId] = @GlobalRecordId");
                updateQuery.Parameters.Add(new QueryParameter("@LastSaveTime", DbType.DateTime, currentTime));
                updateQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, contactVM.RecordId));
                rows = Database.ExecuteNonQuery(updateQuery);
                if (rows == 0)
                {
                    DbLogger.Log("Exception: There was a problem setting the final status for contact " + contactVM.ContactID + ". The final status was set successfully, but the last save time for this contact has not been updated.");
                    throw new InvalidOperationException("There was a problem setting the final status for contact " + contactVM.ContactID + ". The final status was set successfully, but the last save time for this contact has not been updated.");
                }
            });
        }

        private void SetContactFinalStatus(ContactViewModel contactVM)
        {
            Page page = ContactForm.Pages[0];
            Query updateQuery = Database.CreateQuery("UPDATE [" + page.TableName + "] SET [FinalOutcome] = @FinalOutcome WHERE [GlobalRecordId] = @GlobalRecordId");
            updateQuery.Parameters.Add(new QueryParameter("@FinalOutcome", DbType.String, contactVM.FinalOutcome));
            updateQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, contactVM.RecordId));
            int rows = Database.ExecuteNonQuery(updateQuery);
            if (rows == 0)
            {
                DbLogger.Log("Exception: There was a problem setting the final status for contact " + contactVM.ContactID + ". No record for this contact was found to update.");
                throw new InvalidOperationException("There was a problem setting the final status for contact " + contactVM.ContactID + ". No record for this contact was found to update.");
            }

            // make sure to update the last saved time - added 0.9.5.9
            DateTime currentTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, 0);

            updateQuery = Database.CreateQuery("UPDATE [" + ContactForm.TableName + "] SET [LastSaveTime] = @LastSaveTime WHERE [GlobalRecordId] = @GlobalRecordId");
            updateQuery.Parameters.Add(new QueryParameter("@LastSaveTime", DbType.DateTime, currentTime));
            updateQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, contactVM.RecordId));
            rows = Database.ExecuteNonQuery(updateQuery);
            if (rows == 0)
            {
                DbLogger.Log("Exception: There was a problem setting the final status for contact " + contactVM.ContactID + ". The final status was set successfully, but the last save time for this contact has not been updated.");
                throw new InvalidOperationException("There was a problem setting the final status for contact " + contactVM.ContactID + ". The final status was set successfully, but the last save time for this contact has not been updated.");
            }
        }

        protected override string ConvertFinalLabClassificationCode(string code)
        {
            switch (code)
            {
                case "0":
                    return Properties.Resources.NotCase;// "Not a Case";
                case "1":
                    return Properties.Resources.SampleInterpretationConfirmedAcute;// "Confirmed Acute";
                case "2":
                    return Properties.Resources.SampleInterpretationConfirmedConvalescent;// "Confirmed Convalescent";
                case "3":
                    return Properties.Resources.SampleInterpretationIndeterminate;
                case "4":
                    return Properties.Resources.AnalysisClassNeedsFollowUp;
            }

            return String.Empty;
        }

        protected virtual void LoadLabDataForCases()
        {
            lock (_caseCollectionLock)
            {
                foreach (CaseViewModel c in CaseCollection)
                {
                    DataRow[] rows = LabTable.Select("FKEY = '" + c.RecordId + "'", "DateSampleCollected DESC", DataViewRowState.CurrentRows);

                    //DataView dv = new DataView(LabTable, "FKEY = '" + c.RecordId + "'", "DateSampleCollected DESC", DataViewRowState.CurrentRows);
                    //if (dv.Count >= 1)
                    if (rows.Length >= 1)
                    {
                        //DataRowView rowView = dv[0];
                        DataRow rowView = rows[0];
                        if (!String.IsNullOrEmpty(rowView["DateSampleCollected"].ToString()))
                        {
                            c.DateLastLabSampleCollected = DateTime.Parse(rowView["DateSampleCollected"].ToString());
                        }
                        if (!String.IsNullOrEmpty(rowView["DateSampleTested"].ToString()))
                        {
                            c.DateLastLabSampleTested = DateTime.Parse(rowView["DateSampleTested"].ToString());
                        }

                        //switch (rowView["SampleInterpret"].ToString())
                        //{
                        //    case "1":
                        //        c.LastSampleInterpretation = Core.Enums.SampleInterpretation.ConfirmedAcute; // ContactTracing.CaseView.Properties.Resources.SampleInterpretationConfirmedAcute; //"Confirmed Acute";
                        //        break;
                        //    case "2":
                        //        c.LastSampleInterpretation = Core.Enums.SampleInterpretation.ConfirmedConvalescent; //ContactTracing.CaseView.Properties.Resources.SampleInterpretationConfirmedConvalescent; //"Confirmed Convalescent";
                        //        break;
                        //    case "3":
                        //        c.LastSampleInterpretation = Core.Enums.SampleInterpretation.Negative; //ContactTracing.CaseView.Properties.Resources.SampleInterpretationNotCase; //"Not a case";
                        //        break;
                        //    case "4":
                        //        c.LastSampleInterpretation = Core.Enums.SampleInterpretation.Indeterminate; //ContactTracing.CaseView.Properties.Resources.SampleInterpretationIndeterminate; //"Indeterminate";
                        //        break;
                        //    case "5":
                        //        c.LastSampleInterpretation = Core.Enums.SampleInterpretation.NegativeNeedFollowUp; //ContactTracing.CaseView.Properties.Resources.SampleInterpretationNegativeNeedFollowUp; //"Negative, Need Follow-up Sample";
                        //        break;
                        //    default:
                        //        c.LastSampleInterpretation = Core.Enums.SampleInterpretation.None;
                        //        break;
                        //}

                        c.LastSamplePCRResult = String.Empty;

                        if (rowView["SUDVNPPCR"].ToString() == "1" ||
                            rowView["SUDVPCR2"].ToString() == "1" ||
                            rowView["BDBVNPPCR"].ToString() == "1" ||
                            rowView["BDBVVP40PCR"].ToString() == "1" ||
                            rowView["EBOVPCR1"].ToString() == "1" ||
                            rowView["EBOVPCR2"].ToString() == "1" ||
                            rowView["MARVPolPCR"].ToString() == "1" ||
                            rowView["MARVVP40PCR"].ToString() == "1"
                            )
                        {
                            c.LastSamplePCRResult = ContactTracing.CaseView.Properties.Resources.Positive;
                        }
                        else
                        {
                            c.LastSamplePCRResult = ContactTracing.CaseView.Properties.Resources.Negative;
                        }
                    }
                }
            }
        }

        protected virtual async void LoadLabDataForCasesAsync()
        {
            #region Code
            await Task.Factory.StartNew(delegate
            {
                try
                {
                    LoadLabDataForCases();
                }
                catch (InvalidOperationException ex)
                {
                    if (!IsShowingError)
                    {
                        IsShowingError = true;
                        ErrorMessage = "An exception occurred while loading laboratory results data. Please close and re-open the database.";
                        ErrorMessageDetail = ex.Message;
                    }
                }
            });
            #endregion // Code
        }

        protected virtual void LoadCaseData(DataRow row, CaseViewModel c, bool shouldCheckContactFlag = true, bool loadLabData = true)
        {
            // TODO: Find a way to move all of this off the EpiDataHelper
            if (loadLabData)
            {
                DataView dv = new DataView(LabTable, "FKEY = '" + c.RecordId + "'", "DateSampleCollected DESC", DataViewRowState.CurrentRows);
                if (dv.Count >= 1)
                {
                    DataRowView rowView = dv[0];
                    if (!String.IsNullOrEmpty(rowView["DateSampleCollected"].ToString()))
                    {
                        c.DateLastLabSampleCollected = DateTime.Parse(rowView["DateSampleCollected"].ToString());
                    }
                    if (!String.IsNullOrEmpty(rowView["DateSampleTested"].ToString()))
                    {
                        c.DateLastLabSampleTested = DateTime.Parse(rowView["DateSampleTested"].ToString());
                    }

                    c.LastSamplePCRResult = String.Empty;

                    if (rowView["SUDVNPPCR"].ToString() == "1" ||
                        rowView["SUDVPCR2"].ToString() == "1" ||
                        rowView["BDBVNPPCR"].ToString() == "1" ||
                        rowView["BDBVVP40PCR"].ToString() == "1" ||
                        rowView["EBOVPCR1"].ToString() == "1" ||
                        rowView["EBOVPCR2"].ToString() == "1" ||
                        rowView["MARVPolPCR"].ToString() == "1" ||
                        rowView["MARVVP40PCR"].ToString() == "1"
                        )
                    {
                        c.LastSamplePCRResult = ContactTracing.CaseView.Properties.Resources.Positive;
                    }
                    else
                    {
                        c.LastSamplePCRResult = ContactTracing.CaseView.Properties.Resources.Negative;
                    }
                }
            }

            if (shouldCheckContactFlag)
            {
                if (DoesContactExist(c.RecordId))
                {
                    c.IsContact = true;
                }
                //if (ContactCollection != null && ContactCollection.Count > 0)
                //{
                //    foreach (ContactViewModel contact in ContactCollection)
                //    {
                //        if (contact.RecordId.Equals(c.RecordId))
                //        {
                //            c.IsContact = true;
                //            break;
                //        }
                //    }
                //}
            }
        }

        protected virtual void LoadContactData(DataRow row, ContactViewModel contactVM, bool checkForDuality = true)
        {
            if (row.Table.Columns.Contains("GlobalRecordId"))
            {
                contactVM.RecordId = row["GlobalRecordId"].ToString();
            }
            else if (row.Table.Columns.Contains("t.GlobalRecordId"))
            {
                contactVM.RecordId = row["t.GlobalRecordId"].ToString();
            }

            contactVM.Surname = row["ContactSurname"].ToString();
            //contactVM.RecordId = row["GlobalRecordId"].ToString();

            contactVM.OtherNames = row["ContactOtherNames"].ToString();
            switch (row["ContactGender"].ToString())
            {
                case "1":
                    contactVM.Gender = ContactTracing.CaseView.Properties.Resources.Male;
                    break;
                case "2":
                    contactVM.Gender = ContactTracing.CaseView.Properties.Resources.Female;
                    break;
            }

            if (!IsCountryUS)
            {
                switch (row["ContactAgeUnit"].ToString().ToLower())
                {
                    case "ans":
                    case "years":
                        contactVM.AgeUnit = AgeUnits.Years;
                        break;
                    case "months":
                    case "mois":
                        contactVM.AgeUnit = AgeUnits.Months;
                        break;
                    default:
                        contactVM.AgeUnit = null;
                        break;
                }
                contactVM.LC1Chairman = row["LC1"].ToString().Trim();
                contactVM.HeadOfHousehold = row["ContactHeadHouse"].ToString();
            }
            else
            {
                contactVM.ContactStateID = row["ContactStateID"].ToString();
                contactVM.ContactCDCID = row["ContactCDCID"].ToString();

                IField phaField = ContactForm.Fields["PublicHealthAction"];
                if (phaField is DDLFieldOfCommentLegal)
                {
                    DDLFieldOfCommentLegal cmField = phaField as DDLFieldOfCommentLegal;
                    if (cmField != null)
                    {
                        contactVM.PublicHealthAction = Core.Common.GetCommentLegalLabel(cmField, row, "PublicHealthAction", Database);
                    }
                }
                else
                {
                    contactVM.PublicHealthAction = row["PublicHealthAction"].ToString();
                }

                if (row["ContactDOB"] != DBNull.Value) contactVM.ContactDOB = DateTime.Parse(row["ContactDOB"].ToString());
                contactVM.ContactAddress = row["ContactAddress"].ToString();
                contactVM.ContactZip = row["ContactZip"].ToString();
                contactVM.Phone = row["ContactPhone"].ToString();
                contactVM.ContactEmail = row["ContactEmail"].ToString();
                switch (row["ContactHCW"].ToString().Trim())
                {
                    case "1":
                        contactVM.HCW = ContactTracing.CaseView.Properties.Resources.Yes;
                        break;
                    case "2":
                        contactVM.HCW = ContactTracing.CaseView.Properties.Resources.No;
                        break;
                }
                contactVM.ContactHCWPosition = row["ContactHCWPosition"].ToString();
                contactVM.HCWFacility = row["ContactHCWFacility"].ToString();
                contactVM.ContactHCWDistrict = row["ContactHCWDistrict"].ToString();
                contactVM.ContactHCWSC = row["ContactHCWSC"].ToString();
                contactVM.ContactHCWVillage = row["ContactHCWVillage"].ToString();
            }


            if (!String.IsNullOrEmpty(row["ContactAge"].ToString()))
            {
                contactVM.Age = double.Parse(row["ContactAge"].ToString());
            }

            contactVM.District = row["ContactDistrict"].ToString().Trim();
            contactVM.Village = row["ContactVillage"].ToString().Trim();
            if (row.Table.Columns.Contains("ContactParish"))
            {
                contactVM.Parish = row["ContactParish"].ToString().Trim();
            }
            contactVM.SubCounty = row["ContactSC"].ToString().Trim();
            contactVM.Phone = row["ContactPhone"].ToString().Trim();
            contactVM.FinalOutcome = row["FinalOutcome"].ToString().Trim();
            contactVM.UniqueKey = Convert.ToInt32(row["UniqueKey"]);

            if (row.Table.Columns.Contains("Team"))
            {
                contactVM.Team = row["Team"].ToString().Trim();
            }

            string riskLevel = row["Risklevel"].ToString().Trim();
            if (IsCountryUS)
            {
                //contactVM.RiskLevel = riskLevel;

                IField riskLevelField = ContactForm.Fields["Risklevel"];
                if (riskLevelField is DDLFieldOfCommentLegal)
                {
                    DDLFieldOfCommentLegal cmField = riskLevelField as DDLFieldOfCommentLegal;
                    if (cmField != null)
                    {
                        contactVM.RiskLevel = Core.Common.GetCommentLegalLabel(cmField, row, "Risklevel", Database);
                    }
                }
                else
                {
                    contactVM.RiskLevel = row["Risklevel"].ToString();
                }
            }
            else
            {
                switch (riskLevel)
                {
                    case "1":
                        contactVM.RiskLevel = ContactTracing.CaseView.Properties.Resources.RiskLevelHigh;
                        break;
                    case "2":
                        contactVM.RiskLevel = ContactTracing.CaseView.Properties.Resources.RiskLevelMedium;
                        break;
                    case "3":
                        contactVM.RiskLevel = ContactTracing.CaseView.Properties.Resources.RiskLevelLow;
                        break;
                }
            }


            switch (row["ContactHCW"].ToString().Trim())
            {
                case "1":
                    contactVM.HCW = ContactTracing.CaseView.Properties.Resources.Yes;
                    break;
                case "2":
                    contactVM.HCW = ContactTracing.CaseView.Properties.Resources.No;
                    break;
            }

            contactVM.HCWFacility = row["ContactHCWFacility"].ToString();

            if (checkForDuality)
            {
                CaseViewModel caseVM = GetCaseVM(contactVM.RecordId);

                if (caseVM != null)
                {
                    contactVM.IsCase = true;
                }
            }

            if (row["FirstSaveTime"] != DBNull.Value && row["FirstSaveTime"] is DateTime)
            {
                DateTime dt = (DateTime)(row["FirstSaveTime"]);
                contactVM.FirstSaveTime = dt;
            }
            else
            {
                contactVM.FirstSaveTime = null;
            }
        }

        public enum WindowGenerationMode
        {
            Rebuild,
            Shift
        }

        private void LoadContactLinkData(DataRow row, ContactViewModel contactVM, CaseViewModel caseVM, WindowGenerationMode windowGenerationMode = WindowGenerationMode.Rebuild)
        {
            DateTime lastContactDate = Convert.ToDateTime(row[ContactTracing.Core.Constants.LAST_CONTACT_DATE_COLUMN_NAME]);
            
            if (contactVM.FollowUpWindowViewModel == null)
            {
                contactVM.FollowUpWindowViewModel = new FollowUpWindowViewModel(lastContactDate, contactVM, caseVM);
                LoadContactFollowUps(row, contactVM);
            }
            else if (lastContactDate > contactVM.FollowUpWindowViewModel.WindowStartDate && caseVM.EpiCaseDef != EpiCaseClassification.NotCase)
            {
                if (windowGenerationMode == WindowGenerationMode.Rebuild)
                {
                    // so the contact already has a window in place, but we need to update it
                    LoadContactFollowUps(row, contactVM);
                }
                else
                {
                    TimeSpan ts = contactVM.FollowUpWindowViewModel.WindowStartDate - lastContactDate;

                    DataTable linksTable = new DataTable();
                    DataTable historyTable = new DataTable();

                    if (IsMicrosoftSQLDatabase)
                    {
                        CreateLinksTableForSQLServer(ref linksTable, ref historyTable);
                    }
                    else
                    {
                        CreateLinksTableForAccess(ref linksTable, ref historyTable);
                    }

                    ShiftWindow(ts, contactVM, caseVM);

                }
                contactVM.FollowUpWindowViewModel.WindowStartDate = lastContactDate.AddDays(1); // window start date should always be +1 from last contact date
            }
                // VHF-383
            else if (windowGenerationMode == WindowGenerationMode.Shift && caseVM.EpiCaseDef != EpiCaseClassification.NotCase)
            {
                TimeSpan ts = contactVM.FollowUpWindowViewModel.WindowStartDate - lastContactDate;

                DataTable linksTable = new DataTable();
                DataTable historyTable = new DataTable();

                if (IsMicrosoftSQLDatabase)
                {
                    CreateLinksTableForSQLServer(ref linksTable, ref historyTable);
                }
                else
                {
                    CreateLinksTableForAccess(ref linksTable, ref historyTable);
                }

                ShiftWindow(ts, contactVM, caseVM);
                contactVM.FollowUpWindowViewModel.WindowStartDate = lastContactDate.AddDays(1); // window start date should always be +1 from last contact date
            }

            CaseContactPairViewModel ccpVM = new CaseContactPairViewModel();
            ccpVM.ContactVM = contactVM;
            ccpVM.ContactRecordId = contactVM.RecordId;
            ccpVM.CaseVM = caseVM;
            ccpVM.Relationship = row["RelationshipType"].ToString();
            ccpVM.ContactType = null;
            ccpVM.Team = contactVM.Team;
            if (row["ContactType"] != DBNull.Value)
            {
                ccpVM.ContactType = Convert.ToInt32(row["ContactType"]);
            }
            if (row["LastContactDate"] != DBNull.Value)
            {
                ccpVM.DateLastContact = lastContactDate;
                //contactVM.DateOfLastContact = lastContactDate;
            }

            lock (_contactCollectionLock)
            {
                bool found = false;
                foreach (CaseContactPairViewModel iccpvm in ContactLinkCollection)
                {
                    if (iccpvm.CaseVM == caseVM && iccpvm.ContactVM == contactVM)
                    {
                        found = true;
                    }
                }

                if (!found && !ContactLinkCollection.Contains(ccpVM))
                {
                    ContactLinkCollection.Add(ccpVM);
                }
            }
        }

        private void LoadContactFollowUps(DataRow row, ContactViewModel c, bool processFuture = false)
        {
            for (int i = 1; i <= Core.Common.DaysInWindow; i++)
            {
                if (processFuture == false)
                {
                    // don't bother processing stuff from the day after tomorrow
                    // (how would a contact have a status in the future?)
                    // this saves a small amount of processing time
                    TimeSpan ts = DateTime.Now.AddDays(1) - c.FollowUpWindowViewModel.FollowUpVisits[i - 1].Date;
                    if (ts.TotalDays < 0)
                    {
                        break;
                    }
                }

                if (row["Day" + i.ToString()] != DBNull.Value)
                {
                    c.FollowUpWindowViewModel.FollowUpVisits[i - 1].Status = (ContactDailyStatus)(Enum.Parse(typeof(ContactDailyStatus), row["Day" + i.ToString()].ToString()));
                }
                else
                {
                    c.FollowUpWindowViewModel.FollowUpVisits[i - 1].Status = null;
                }

                if (row["Day" + i.ToString() + "Notes"] != DBNull.Value)
                {
                    c.FollowUpWindowViewModel.FollowUpVisits[i - 1].Notes = row["Day" + i.ToString() + "Notes"].ToString();
                    if (row["Temp1_" + i.ToString()] != DBNull.Value)
                    {
                        try
                        {
                            c.FollowUpWindowViewModel.FollowUpVisits[i - 1].Temp1 = Convert.ToDouble(row["Temp1_" + i.ToString()].ToString());
                        }
                        catch
                        {
                        }
                    }
                    if (row["Temp2_" + i.ToString()] != DBNull.Value)
                    {
                        try
                        {
                            c.FollowUpWindowViewModel.FollowUpVisits[i - 1].Temp2 = Convert.ToDouble(row["Temp2_" + i.ToString()].ToString());
                        }
                        catch
                        {
                        }
                    }
                }
            } // end for
        }

        private ServerUpdateMessage CreateServerUpdateMessage(DataRow row)
        {
            int changeset = (int)(row["Changeset"]);
            string desc = row["Description"].ToString();
            string guid = row["ChangesetID"].ToString();
            string userID = row["UserID"].ToString();
            string recordID = row["DestinationRecordId"].ToString();
            int type = (int)(row["UpdateType"]);
            DateTime checkinDate = (DateTime)(row["CheckinDate"]);
            ServerUpdateType updateType = (ServerUpdateType)type;

            ServerUpdateMessage message = new ServerUpdateMessage(changeset, guid, userID, desc, recordID, checkinDate, updateType);
            return message;
        }

        private CaseViewModel CreateCaseFromGuid(string guid)
        {
            CaseViewModel c = new CaseViewModel(CaseForm, LabForm);
            //17178
            Query selectQuery = null;
            if (IsCountryUS)
            {
                selectQuery = Database.CreateQuery("SELECT t.GlobalRecordId, FirstSaveTime, LastSaveTime, ID, OrigID, Surname, OtherNames, Age, Gender, StatusReport, " +
                "RecComplete, RecNoCRF, RecMissingCRFInfo, DateReport, RecPendLab, RecPendOutcome, DateDeath, DateDeath2, DateOnset, DistrictOnset, SCOnset, CountryOnset, VillageOnset, DateDischargeIso, DateHospitalCurrentAdmit, DateIsolationCurrent, " +
                "DateIsolationCurrent, PlaceDeath, HCW, StatusAsOfCurrentDate, FinalLabClass, FinalStatus, DistrictRes, AddressRes, ZipRes, VillageRes, Citizenship, CountryRes, VillageRes, SCRes, t.UniqueKey, EpiCaseDef, IsolationCurrent, HospitalCurrent, " +
                "DateDischargeHosp, DOB, Email, HCWposition, HCWFacility, HCWDistrict, HCWSC, HCWVillage, PhoneNumber " + //17227 Added DOB, PhoneNumber, Email, HCWposition, HCWFacility, HCWDistrict, HCWSC, HCWVillage
                CaseForm.FromViewSQL + " " +
                "WHERE t.GlobalRecordId = @GlobalRecordId");
            }
            else
            {
                selectQuery = Database.CreateQuery("SELECT t.GlobalRecordId, FirstSaveTime, LastSaveTime, ID, OrigID, Surname, OtherNames, Age, AgeUnit, Gender, HeadHouse, StatusReport, " +
                "RecComplete, RecNoCRF, RecMissingCRFInfo, DateReport, RecPendLab, RecPendOutcome, DateDeath, DateDeath2, DateOnset, DistrictOnset, SCOnset, CountryOnset, VillageOnset, DateDischargeIso, DateHospitalCurrentAdmit, DateIsolationCurrent, " +
                "DateIsolationCurrent, PlaceDeath, HCW, StatusAsOfCurrentDate, FinalLabClass, FinalStatus, DistrictRes, CountryRes, VillageRes, ParishRes, SCRes, t.UniqueKey, EpiCaseDef, IsolationCurrent, HospitalCurrent, " +
                "DateDischargeHosp " +
                CaseForm.FromViewSQL + " " +
                "WHERE t.GlobalRecordId = @GlobalRecordId");
            }
            selectQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, guid));
            DataTable dt = Database.Select(selectQuery);

            if (dt.Rows.Count == 1)
            {
                DataRow row = dt.Rows[0];
                CaseViewModel.IsCountryUS = this.IsCountryUS; //17178
                c.LoadPartial(row);
                LoadCaseData(row, c);
            }
            return c;
        }

        public void ReloadAllCaseData(CaseViewModel caseVM)
        {
            throw new NotImplementedException();
        }

        private ContactViewModel CreateContactFromGuid(string guid)
        {
            ContactViewModel c = new ContactViewModel();

            string queryText = "";

            if (IsCountryUS)
            {
                queryText = "SELECT t.GlobalRecordId, FirstSaveTime, LastSaveTime, ContactCDCID, ContactStateID, Team, PublicHealthAction, ContactSurname, ContactOtherNames, ContactAge, ContactGender, " +
                " ContactVillage, ContactSC, ContactDistrict, ContactPhone, UniqueKey, Risklevel, ContactHCW, ContactHCWFacility,  FinalOutcome,  ContactDOB, ContactAddress, ContactZip, ContactPhone," +
                "ContactEmail, ContactHCW, ContactHCWPosition, ContactHCWFacility,ContactHCWDistrict,ContactHCWSC,ContactHCWVillage   " +
                ContactForm.FromViewSQL + " " +
                "WHERE [t.GlobalRecordId] = @GlobalRecordId";
                if (IsMicrosoftSQLDatabase)
                {
                    queryText = "SELECT t.GlobalRecordId, FirstSaveTime, LastSaveTime, ContactCDCID, ContactStateID, Team, PublicHealthAction, ContactSurname, ContactOtherNames, ContactAge, ContactGender, " +
                " ContactVillage, ContactSC, ContactDistrict, ContactPhone, UniqueKey, Risklevel, ContactHCW, ContactHCWFacility,  FinalOutcome,  ContactDOB, ContactAddress, ContactZip, ContactPhone," +
                "ContactEmail, ContactHCW, ContactHCWPosition, ContactHCWFacility,ContactHCWDistrict,ContactHCWSC,ContactHCWVillage  " +
                ContactForm.FromViewSQL + " " +
                "WHERE t.GlobalRecordId = @GlobalRecordId";
                }
            }
            else
            {
                queryText = "SELECT t.GlobalRecordId, FirstSaveTime, LastSaveTime, ContactSurname, ContactOtherNames, ContactAge, ContactAgeUnit, ContactGender, " +
                "ContactHeadHouse, ContactVillage, ContactSC, ContactDistrict, ContactPhone, UniqueKey, Risklevel, ContactHCW, ContactHCWFacility, LC1, FinalOutcome " +
                ContactForm.FromViewSQL + " " +
                "WHERE [t.GlobalRecordId] = @GlobalRecordId";
                if (IsMicrosoftSQLDatabase)
                {
                    queryText = "SELECT t.GlobalRecordId, FirstSaveTime, LastSaveTime, ContactSurname, ContactOtherNames, ContactAge, ContactAgeUnit, ContactGender, " +
                    "ContactHeadHouse, ContactVillage, ContactSC, ContactDistrict, ContactPhone, UniqueKey, Risklevel, ContactHCW, ContactHCWFacility, LC1, FinalOutcome " +
                    ContactForm.FromViewSQL + " " +
                    "WHERE t.GlobalRecordId = @GlobalRecordId";
                }
            }

            Query selectQuery = Database.CreateQuery(queryText);
            selectQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, guid));
            DataTable dt = Database.Select(selectQuery);

            if (dt.Rows.Count == 1)
            {
                DataRow row = dt.Rows[0];
                LoadContactData(row, c);
            }

            return c;
        }

        public void RefreshEpiCurveData()
        {
            #region Confirmed and Probable
            lock (_epiCurveCollectionLock)
            {
                EpiCurveDataPointCollectionCP.Clear();
            }

            var minQuery = from caseVM in CaseCollection
                           where caseVM.DateOnset.HasValue == true &&
                           (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed ||
                           caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable)
                           orderby caseVM.DateOnset ascending
                           select caseVM;

            if (minQuery.Count() >= 1)
            {
                CaseViewModel first = minQuery.First();
                CaseViewModel last = minQuery.Last();

                DateTime lowDate = first.DateOnset.Value;
                DateTime highDate = last.DateOnset.Value;
                DateTime incDate = new DateTime(lowDate.Ticks);

                // add padding before and after
                incDate = incDate.AddDays(-7);
                highDate = highDate.AddDays(7);

                while (incDate <= highDate)
                {
                    DateTime MMWR__Start;
                    MMWR__Start = Core.Common.GetMMWRStart(incDate, 1);

                    TimeSpan MMWR__DayCount = incDate.Subtract(MMWR__Start);
                    int MMWR__Week = ((int)(MMWR__DayCount.Days / 7)) + 1;
                    DateTime curDate = MMWR__Start.AddDays((MMWR__Week * 7) - 1);

                    incDate = incDate.AddDays(7);

                    XYColumnChartData xyDataCA = new XYColumnChartData();
                    xyDataCA.X = curDate.ToString("dd/MM");
                    xyDataCA.S = Properties.Resources.Confirmed;

                    XYColumnChartData xyDataP = new XYColumnChartData();
                    xyDataP.X = curDate.ToString("dd/MM");
                    xyDataP.S = Properties.Resources.Probable;

                    lock (_epiCurveCollectionLock)
                    {
                        EpiCurveDataPointCollectionCP.Add(xyDataCA);
                        EpiCurveDataPointCollectionCP.Add(xyDataP);
                    }
                }
            }

            var selectQuery = from caseVM in CaseCollection
                              where caseVM.EpiCurveDisplayDate.HasValue == true &&
                              caseVM.EpiCurveCaseCategory != Core.Enums.EpiCaseClassification.None
                              group caseVM by caseVM.EpiCurveCaseCategory;

            Converters.EpiCaseClassificationConverter caseClassConverter = new Converters.EpiCaseClassificationConverter();

            foreach (var entry in selectQuery)
            {
                var query = from caseVM in entry
                            group caseVM by caseVM.EpiCurveDisplayDate;

                foreach (var subEntry in query)
                {
                    var pointQuery = from point in EpiCurveDataPointCollectionCP
                                     where point.X.ToString() == subEntry.Key.Value.ToString("dd/MM") &&
                                     point.S.ToString() == caseClassConverter.Convert(entry.Key, null, null, null).ToString()
                                     select point;

                    if (pointQuery.Count() > 0)
                    {
                        XYColumnChartData xyData = pointQuery.First();
                        xyData.Y = subEntry.Count();
                    }

                    //XYColumnChartData xyData = new XYColumnChartData();
                    //xyData.Y = subEntry.Count();
                    //xyData.X = subEntry.Key;
                    //xyData.S = entry.Key;

                    //EpiCurveDataPointCollection.Add(xyData);
                }
            }
            #endregion Confirmed and Probable

            #region Confirmed, Probable, Suspect
            lock (_epiCurveCollectionLock)
            {
                EpiCurveDataPointCollectionCPS.Clear();
            }

            if (ApplicationViewModel.Instance.CurrentRegion == Core.Enums.RegionEnum.USA)
            {
                minQuery = from caseVM in CaseCollection
                           where caseVM.DateOnset.HasValue == true &&
                           (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed ||
                           caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable ||
                           caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Suspect ||
                           caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.PUI)
                           orderby caseVM.DateOnset ascending
                           select caseVM;
            }
            else
            {
                minQuery = from caseVM in CaseCollection
                           where caseVM.DateOnset.HasValue == true &&
                           (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed ||
                           caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable ||
                           caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Suspect ||
                           caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.PUI)
                           orderby caseVM.DateOnset ascending
                           select caseVM;

            }

            if (minQuery.Count() >= 1)
            {
                CaseViewModel first = minQuery.First();
                CaseViewModel last = minQuery.Last();

                DateTime lowDate = first.DateOnset.Value;
                DateTime highDate = last.DateOnset.Value;
                DateTime incDate = new DateTime(lowDate.Ticks);

                // add padding before and after
                incDate = incDate.AddDays(-7);
                highDate = highDate.AddDays(7);

                while (incDate <= highDate)
                {
                    DateTime MMWR__Start;
                    MMWR__Start = Core.Common.GetMMWRStart(incDate, 1);

                    TimeSpan MMWR__DayCount = incDate.Subtract(MMWR__Start);
                    int MMWR__Week = ((int)(MMWR__DayCount.Days / 7)) + 1;
                    DateTime curDate = MMWR__Start.AddDays((MMWR__Week * 7) - 1);

                    incDate = incDate.AddDays(7);

                    XYColumnChartData xyDataCA = new XYColumnChartData();
                    xyDataCA.X = curDate.ToString("dd/MM");
                    xyDataCA.S = Properties.Resources.Confirmed;

                    XYColumnChartData xyDataP = new XYColumnChartData();
                    xyDataP.X = curDate.ToString("dd/MM");
                    xyDataP.S = Properties.Resources.Probable;

                    XYColumnChartData xyDataS = new XYColumnChartData();
                    xyDataS.X = curDate.ToString("dd/MM");
                    xyDataS.S = Properties.Resources.Suspect;


                    XYColumnChartData xyDataPUI = null;

                    if (ApplicationViewModel.Instance.CurrentRegion == Core.Enums.RegionEnum.USA)
                    {
                        xyDataPUI = new XYColumnChartData();
                        xyDataPUI.X = curDate.ToString("dd/MM");
                        xyDataPUI.S = Properties.Resources.PUI;
                    }


                    lock (_epiCurveCollectionLock)
                    {
                        EpiCurveDataPointCollectionCPS.Add(xyDataCA);
                        EpiCurveDataPointCollectionCPS.Add(xyDataP);
                        EpiCurveDataPointCollectionCPS.Add(xyDataS);
                        if (ApplicationViewModel.Instance.CurrentRegion == Core.Enums.RegionEnum.USA)
                        {
                            EpiCurveDataPointCollectionCPS.Add(xyDataPUI);
                        }
                    }
                }
            }

            selectQuery = from caseVM in CaseCollection
                          where caseVM.EpiCurveDisplayDate.HasValue == true &&
                          caseVM.EpiCurveCaseCategory != Core.Enums.EpiCaseClassification.None
                          group caseVM by caseVM.EpiCurveCaseCategory;

            foreach (var entry in selectQuery)
            {
                var query = from caseVM in entry
                            group caseVM by caseVM.EpiCurveDisplayDate;

                foreach (var subEntry in query)
                {
                    var pointQuery = from point in EpiCurveDataPointCollectionCPS
                                     where point.X.ToString() == subEntry.Key.Value.ToString("dd/MM") &&
                                     point.S.ToString() == caseClassConverter.Convert(entry.Key, null, null, null).ToString()
                                     select point;

                    if (pointQuery.Count() > 0)
                    {
                        XYColumnChartData xyData = pointQuery.First();
                        xyData.Y = subEntry.Count();
                    }

                    //XYColumnChartData xyData = new XYColumnChartData();
                    //xyData.Y = subEntry.Count();
                    //xyData.X = subEntry.Key;
                    //xyData.S = entry.Key;

                    //EpiCurveDataPointCollection.Add(xyData);
                }
            }
            #endregion Confirmed, Probable, Suspect

            #region Confirmed, Probable, Suspect DEATHS
            lock (_epiCurveCollectionLock)
            {
                EpiCurveDataPointCollectionDeathsCPS.Clear();
            }

            minQuery = from caseVM in CaseCollection
                       where
                       (caseVM.DateDeathCurrentOrFinal.HasValue == true) &&
                       (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed ||
                       caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable ||
                       caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Suspect)
                       orderby caseVM.DateDeathCurrentOrFinal ascending
                       select caseVM;

            if (minQuery.Count() >= 1)
            {

                CaseViewModel first = minQuery.First();
                CaseViewModel last = minQuery.Last();

                DateTime lowDate = first.DateDeathCurrentOrFinal.Value;
                DateTime highDate = last.DateDeathCurrentOrFinal.Value;
                DateTime incDate = new DateTime(lowDate.Ticks);

                // add padding before and after
                incDate = incDate.AddDays(-7);
                highDate = highDate.AddDays(7);

                while (incDate <= highDate)
                {
                    DateTime MMWR__Start;
                    MMWR__Start = Core.Common.GetMMWRStart(incDate, 1);

                    TimeSpan MMWR__DayCount = incDate.Subtract(MMWR__Start);
                    int MMWR__Week = ((int)(MMWR__DayCount.Days / 7)) + 1;
                    DateTime curDate = MMWR__Start.AddDays((MMWR__Week * 7) - 1);

                    incDate = incDate.AddDays(7);

                    XYColumnChartData xyDataCA = new XYColumnChartData();
                    xyDataCA.X = curDate.ToString("dd/MM");
                    xyDataCA.S = Properties.Resources.Confirmed;

                    XYColumnChartData xyDataP = new XYColumnChartData();
                    xyDataP.X = curDate.ToString("dd/MM");
                    xyDataP.S = Properties.Resources.Probable;

                    XYColumnChartData xyDataS = new XYColumnChartData();
                    xyDataS.X = curDate.ToString("dd/MM");
                    xyDataS.S = Properties.Resources.Suspect;

                    lock (_epiCurveCollectionLock)
                    {
                        EpiCurveDataPointCollectionDeathsCPS.Add(xyDataCA);
                        EpiCurveDataPointCollectionDeathsCPS.Add(xyDataP);
                        EpiCurveDataPointCollectionDeathsCPS.Add(xyDataS);
                    }
                }
            }

            selectQuery = from caseVM in CaseCollection
                          where caseVM.EpiCurveDeathDisplayDate.HasValue == true &&
                          caseVM.EpiCurveCaseCategory != Core.Enums.EpiCaseClassification.None
                          group caseVM by caseVM.EpiCurveCaseCategory;

            foreach (var entry in selectQuery)
            {
                var query = from caseVM in entry
                            group caseVM by caseVM.EpiCurveDeathDisplayDate;

                foreach (var subEntry in query)
                {
                    var pointQuery = from point in EpiCurveDataPointCollectionDeathsCPS
                                     where point.X.ToString() == subEntry.Key.Value.ToString("dd/MM") &&
                                     point.S.ToString() == caseClassConverter.Convert(entry.Key, null, null, null).ToString()
                                     select point;

                    if (pointQuery.Count() > 0)
                    {
                        XYColumnChartData xyData = pointQuery.First();
                        xyData.Y = subEntry.Count();
                    }

                    //XYColumnChartData xyData = new XYColumnChartData();
                    //xyData.Y = subEntry.Count();
                    //xyData.X = subEntry.Key;
                    //xyData.S = entry.Key;

                    //EpiCurveDataPointCollection.Add(xyData);
                }
            }
            #endregion Confirmed, Probable, Suspect DEATHS
        }

        //private void LoadContactStatuses(DataTable historyTable, ContactViewModel c)
        //{
        //    throw new NotImplementedException();

        //    // method stub

        //    DataRow[] rows = historyTable.Select("ContactGUID = '" + c.RecordId + "'");
        //    DateTime lastContactDate = c.DateOfLastContact.Value;
        //    DateTime lastWindowDate = lastContactDate.AddDays(21);
        //    DateTime curPos = lastContactDate.AddDays(1);

        //    while (curPos <= lastWindowDate)
        //    {
        //        DataRow[] singleRow = historyTable.Select("FollowUpDate = #" + curPos.ToShortDateString() + "#");
        //        if (singleRow.Length == 1)
        //        {
        //            DateTime date = Convert.ToDateTime(singleRow[0]["FollowUpDate"]);
        //        }

        //        curPos = curPos.AddDays(1);
        //    }

        //    foreach (DataRow row in rows)
        //    {




        //    }

        //    for (int i = 1; i <= Core.Common.DaysInWindow; i++)
        //    {

        //    }
        //}

        protected virtual void LoadContactsLinkData(DataTable linksTable)
        {
            DateTime today = DateTime.Today;

            //Parallel.ForEach(linksTable.AsEnumerable(), row =>
            //{

            double count = linksTable.Rows.Count;
            double completed = 0;

            foreach (DataRow row in linksTable.Rows)
            {
                string caseGuid = row["FromRecordGuid"].ToString();
                CaseViewModel caseVM = GetCaseVM(caseGuid);
                ContactViewModel contactVM = GetContactVM(row["ToRecordGuid"].ToString());
                if (caseVM != null && contactVM != null)
                {
                    if (contactVM.LastSourceCase == null)
                    {
                        if (row["ContactType"] != DBNull.Value)
                        {
                            contactVM.LastSourceCaseContactTypes = Core.Common.ContactTypeConverter(Convert.ToInt32(row["ContactType"]));
                        }
                        contactVM.RelationshipToLastSourceCase = row["RelationshipType"].ToString();
                        contactVM.LastSourceCase = caseVM;
                        contactVM.DateOfLastContact = Convert.ToDateTime(row[ContactTracing.Core.Constants.LAST_CONTACT_DATE_COLUMN_NAME]);
                        CheckCaseContactForDailyFollowUps(contactVM, today, false);

                        //foreach (ContactViewModel contactVM in this.ContactCollection)
                        //{
                        //    CheckCaseContactForDailyFollowUps(contactVM, today, false);
                        //
                        //}
                    }

                    LoadContactLinkData(row, contactVM, caseVM);
                    if (!caseVM.Contacts.Contains(contactVM))
                    {
                        caseVM.Contacts.Add(contactVM);
                    }

                    if (ContactCollection.Contains(caseGuid))
                    {
                        ContactCollection[caseGuid].IsActive = false;
                    }
                }

                completed++;
                TaskbarProgressValue = TaskbarProgressValue + ((completed / count) * 0.3) - ((--completed / count) * 0.3);
            }
        }

        /// <summary>
        /// Used to clear all case, contact, and link objects from memory.
        /// </summary>
        public override void ClearCollections()
        {
            lock (_caseCollectionLock)
            {
                CaseCollection.Dispose();
                CaseCollection.Clear();
            }

            lock (_contactCollectionLock) { ContactCollection.Clear(); }
            lock (_contactCollectionLock) { CurrentContactLinkCollection.Clear(); }
            lock (_caseCollectionLock) { CurrentCaseLinkCollection.Clear(); }
            lock (_contactCollectionLock) { ContactLinkCollection.Clear(); }
            CurrentSourceCaseCollection.Clear();
            lock (_dailyCollectionLock) { DailyFollowUpCollection.Clear(); }
            lock (_prevCollectionLock) { PrevFollowUpCollection.Clear(); }
            lock (_epiCurveCollectionLock)
            {
                try
                {
                    EpiCurveDataPointCollectionCP.Clear();
                    EpiCurveDataPointCollectionCPS.Clear();
                    EpiCurveDataPointCollectionDeathsCPS.Clear();
                }
                catch (InvalidOperationException)
                {
                    // do nothing
                }
            }

            Countries.Clear();
            Districts.Clear();
            DistrictsSubCounties.Clear();
            SubCounties.Clear();
            HcwPositions.Clear();
            TransportTypes.Clear();
            HospitalsPast.Clear();
            ContactRelations.Clear();
            FuneralRelations.Clear();
            OtherOccupDetails.Clear();
            BusinessTypes.Clear();
            BleedOtherComments.Clear();
            SymptOtherComments.Clear();
            HcwFacilities.Clear();
            CurrentHospitals.Clear();
            InterviewerPositions.Clear();
            InterviewerHealthFacilities.Clear();
            ProxyRelations.Clear();
            HospitalDischarges.Clear();
            OtherHemoFinalSpecifyCollection.Clear();
            LabResultCollection.Clear();
            ContactHCWFacilities.Clear();
            ContactTeamCollection.Clear();
        }

        /// <summary>
        /// Close the project
        /// </summary>
        public virtual void CloseProject()
        {
            DbLogger.Log(String.Format("Closed project"));

            ClearCollections();

            _prefix = "";
            _separator = "-";
            _outbreakName = "New Outbreak";
            _projectName = "NewProject";

            //_adm1 = "District";
            //_adm2 = "Sub-County";
            //_adm3 = "Parish";
            //_adm4 = "Village/Town";

            _adm1 = Properties.Resources.Adm1Default;
            _adm2 = Properties.Resources.Adm2Default;
            _adm3 = Properties.Resources.Adm3Default;
            _adm4 = Properties.Resources.Adm4Default;

            RaisePropertyChanged("Prefix");
            RaisePropertyChanged("Separator");
            RaisePropertyChanged("ProjectName");
            RaisePropertyChanged("OutbreakName");
            RaisePropertyChanged("Adm1");
            RaisePropertyChanged("Adm2");
            RaisePropertyChanged("Adm3");
            RaisePropertyChanged("Adm4");


            Countries.Clear();
            HcwPositions.Clear();
            TransportTypes.Clear();
            HospitalsPast.Clear();
            ContactRelations.Clear();
            FuneralRelations.Clear();
            OtherOccupDetails.Clear();
            BusinessTypes.Clear();
            BleedOtherComments.Clear();
            SymptOtherComments.Clear();
            HcwFacilities.Clear();
            CurrentHospitals.Clear();
            InterviewerPositions.Clear();
            InterviewerHealthFacilities.Clear();
            ProxyRelations.Clear();
            HospitalDischarges.Clear();
            OtherHemoFinalSpecifyCollection.Clear();
            Districts.Clear();
            SubCounties.Clear();
            ContactTeams.Clear();
            Facilities.Clear();
            DistrictsSubCounties.Clear();

            ContactHCWFacilities.Clear();
            ContactTeamCollection.Clear();

            if (Database != null && (IsMicrosoftSQLDatabase || PollRate > 0))
            {
                PollRate = 0;

                // clean up all of this client's prior record locks

                string querySyntax = "DELETE * FROM Changesets WHERE MACADDR = @MACADDR AND ((UpdateType >= 0 AND UpdateType <= 2) OR UpdateType = 6)";
                if (IsMicrosoftSQLDatabase)
                {
                    querySyntax = "DELETE FROM Changesets WHERE MACADDR = @MACADDR AND ((UpdateType >= 0 AND UpdateType <= 2) OR UpdateType = 6)";
                }

                Query deleteQuery = Database.CreateQuery(querySyntax);//"DELETE * FROM Changesets WHERE MACADDR = @MACADDR AND UpdateType >= 0 AND UpdateType <= 2");
                deleteQuery.Parameters.Add(new QueryParameter("@MACADDR", DbType.String, MacAddress));
                int rows = Database.ExecuteNonQuery(deleteQuery);
            }

            DbLogger.Database = null;

            if (Database != null && Database is Epi.Data.Office.OleDbDatabase)
            {
                _oleDbConnection.Close();
                _oleDbConnection.Dispose();
                _oleDbConnection = null;
            }

            if (Database != null)
            {
                Database.Dispose();
                Database = null;
            }

            if (Project != null)
            {
                Project.Dispose();
                Project = null;
            }
        }

        public void SendMessageForUpdateDaily(string guid, bool forceRefresh = false)
        {
            if (forceRefresh)
            {
                SendMessage("Updating follow up (with forced refresh) for contact " + guid, guid, ServerUpdateType.UpdateFollowUpAndForceRefresh);
            }
            else
            {
                SendMessage("Updating follow-up for contact " + guid, guid, ServerUpdateType.UpdateFollowUp);
            }
        }

        public void SendMessageForDataImported()
        {
            SendMessage("Data was imported", String.Empty, ServerUpdateType.DataImported);
        }

        public void SendMessageForAwaitAll()
        {
            SendMessage("Locked database", string.Empty, ServerUpdateType.LockAllClientIsRefreshing);
        }

        public void SendMessageForUnAwaitAll()
        {
            SendMessage("Unlocked database", string.Empty, ServerUpdateType.UnlockAllClientRefreshComplete);
        }

        public void SendMessageForAddCase(string guid)
        {
            SendMessage("Adding new case " + guid, guid, ServerUpdateType.AddCase);
        }

        public void SendMessageForUpdateCaseToContactRelationship(string guid) // guid = for case
        {
            SendMessage("Updating case-contact relationship " + guid, guid, ServerUpdateType.UpdateCaseContactRelationship);
        }

        public void SendMessageForAddContact(string guid)
        {
            SendMessage("Adding new contact " + guid, guid, ServerUpdateType.AddContact);
        }

        public void SendMessageForUpdateContact(string guid)
        {
            SendMessage("Updating contact " + guid, guid, ServerUpdateType.UpdateContact);
        }

        public void SendMessageForUpdateCase(string guid)
        {
            SendMessage("Updating case " + guid, guid, ServerUpdateType.UpdateCase);
        }

        public void SendMessageForLockCase(CaseViewModel caseVM)
        {
            SendMessage("Locking case " + caseVM.ID, caseVM.RecordId, ServerUpdateType.LockCase);
        }

        public void SendMessageForUnlockCase(CaseViewModel caseVM)
        {
            if (caseVM != null)
                SendMessage("Unlocking case " + caseVM.ID, caseVM.RecordId, ServerUpdateType.UnlockCase);
        }

        public void SendMessageForLockContact(ContactViewModel contactVM)
        {
            SendMessage("Locking contact " + contactVM.RecordId, contactVM.RecordId, ServerUpdateType.LockContact);
        }

        public void SendMessageForUnlockContact(ContactViewModel contactVM)
        {
            if (contactVM != null)
                SendMessage("Unlocking contact " + contactVM.RecordId, contactVM.RecordId, ServerUpdateType.UnlockContact);
        }

        protected virtual bool SendMessage(string description, string recordId, ServerUpdateType updateType)
        {
            bool success = false;

            if (IsMicrosoftSQLDatabase || PollRate > 0)
            {
                System.Guid guid = System.Guid.NewGuid();
                string guidString = guid.ToString();
                DateTime now = DateTime.Now;

                lock (_sendMessageLock)
                {
                    IsSendingServerUpdates = true;

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
                            insertCommand.Parameters.Add("@UserID", SqlDbType.NVarChar).Value = CurrentUser;
                            insertCommand.Parameters.Add("@MACADDR", SqlDbType.NVarChar).Value = MacAddress;
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

                        Query selectQuery = Database.CreateQuery("SELECT Changeset FROM Changesets WHERE ChangesetID = @ChangesetID");
                        selectQuery.Parameters.Add(new QueryParameter("@ChangesetID", DbType.String, guidString));
                        DataTable dt = Database.Select(selectQuery);

                        int dbChangeset = (int)(dt.Rows[0][0]);

                        if (dbChangeset < Changeset)
                        {
                            throw new InvalidOperationException("The changeset detected in the database is less than the client's current changeset.");
                        }

                        Changeset = dbChangeset;

                        IsSendingServerUpdates = false;

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

//        private void CheckVersioning()
//        {
//            System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
//            Version thisVersion = a.GetName().Version;
//            string thisVersionString = thisVersion.ToString();

//            IsUsingOutdatedVersion = false;
//            IsUsingDeprecatedVersion = false;

//            string[] dbVersion = VhfDbVersion.Split('.');
//            if (dbVersion.Length == 4)
//            {
//                int dbMajor = int.Parse(dbVersion[0]);
//                int dbMinor = int.Parse(dbVersion[1]);
//                int dbBuild = int.Parse(dbVersion[2]);
//                int dbRevision = int.Parse(dbVersion[3]);

//                bool shouldUpdateDbVersion = false;

//                if (dbMajor > thisVersion.Major)
//                {
//                    IsUsingOutdatedVersion = true;
//                    IsUsingDeprecatedVersion = true;
//                }
//                else if (dbMajor == thisVersion.Major)
//                {
//                    if (dbMinor > thisVersion.Minor)
//                    {
//                        IsUsingOutdatedVersion = true;
//                        IsUsingDeprecatedVersion = true;
//                    }
//                    else if (dbMinor == thisVersion.Minor)
//                    {
//                        if (dbBuild > thisVersion.Build)
//                        {
//                            IsUsingOutdatedVersion = true;
//                            IsUsingDeprecatedVersion = true;
//                        }
//                        else if (dbBuild == thisVersion.Build)
//                        {
//                            if (dbRevision > thisVersion.Revision + 12)
//                            {
//                                IsUsingOutdatedVersion = true;
//                                IsUsingDeprecatedVersion = true;
//                            }
//                            else if (dbRevision > thisVersion.Revision)
//                            {
//                                IsUsingOutdatedVersion = true;
//                            }


//                            if (dbRevision < thisVersion.Revision)
//                            {
//                                shouldUpdateDbVersion = true;
//                            }
//                        }
//                        else
//                        {
//                            shouldUpdateDbVersion = true;
//                        }
//                    }
//                    else
//                    {
//                        shouldUpdateDbVersion = true;
//                    }
//                }
//                else
//                {
//                    shouldUpdateDbVersion = true;
//                }

//                if (shouldUpdateDbVersion)
//                {
//#if DEBUG
//                    // TODO: This is for RC builds only, remove later!!!
//                    thisVersionString = "0.8.0.0";
//#endif

//                    Query updateQuery = Database.CreateQuery("UPDATE metaDbInfo SET VhfVersion = @Version");
//                    updateQuery.Parameters.Add(new QueryParameter("@Version", DbType.String, thisVersionString));
//                    Database.ExecuteNonQuery(updateQuery);
//                }
//            }
//        }

        /// <summary>
        /// Deletes all contacts that lack source cases from the database
        /// </summary>
        /// <param name="deleteFromDatabase">Whether to delete the contacts from the database. If false, the deletions will only occur on the in-memory dataset and will not affect the database.</param>
        protected internal void DeleteCaselessContacts(bool deleteFromDatabase = true)
        {
            if (deleteFromDatabase)
            {
                SendMessageForAwaitAll();
            }

            try
            {
                lock (_contactCollectionLock)
                {
                    List<ContactViewModel> contactsToDelete = new List<ContactViewModel>();

                    foreach (ContactViewModel c in this.ContactCollection.Where(x => x.LastSourceCase == null && x.DateOfLastContact.HasValue == false))
                    {
                        contactsToDelete.Add(c);
                    }

                    foreach (ContactViewModel c in contactsToDelete.Where(x => x.LastSourceCase == null && x.DateOfLastContact.HasValue == false))
                    {
                        if (deleteFromDatabase)
                        {
                            // look for case-to-contact links in the DB itself
                            Query selectQuery = Database.CreateQuery("SELECT FromRecordGuid FROM metaLinks WHERE ToRecordGuid = @ToRecordGuid AND FromViewId = @FromViewId AND ToViewId = @ToViewId");
                            selectQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, c.RecordId));
                            selectQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, CaseFormId));
                            selectQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, ContactFormId));
                            DataTable dt = Database.Select(selectQuery);

                            bool foundSourceCase = false;

                            // if we find any, see if the case-contact link leads back to a real case record
                            foreach (DataRow row in dt.Rows)
                            {
                                string guid = row["FromRecordGuid"].ToString();
                                selectQuery = Database.CreateQuery("SELECT GlobalRecordId FROM [" + CaseForm.TableName + "] WHERE GlobalRecordId = @GlobalRecordId");
                                selectQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, guid));
                                DataTable caseTable = Database.Select(selectQuery);

                                if (caseTable.Rows.Count >= 1)
                                {
                                    foundSourceCase = true;
                                    break;
                                }
                            }

                            // if we didn't find a real case record, remove
                            if (!foundSourceCase)
                            {
                                RemoveContactFromDatabase(c.RecordId);
                                ContactCollection.Remove(c);
                            }
                        }
                        else
                        {
                            ContactCollection.Remove(c);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // TODO: Do nothing for the moment, but we should display a message box of some kind probably indicating how many contacts were deleted, how many we failed to delete, etc
            }
            finally
            {
                if (deleteFromDatabase)
                {
                    SendMessageForUnAwaitAll();
                }
            }
        }

        //protected internal async void DeleteCaselessContactsAsync(bool deleteFromDatabase = true)
        //{
        //    await Task.Factory.StartNew(delegate
        //    {
        //        DeleteCaselessContacts(deleteFromDatabase);
        //    });
        //}

        protected internal bool CanLoadData()
        {
            bool canLoad = true;

            if (IsMicrosoftSQLDatabase || PollRate > 0)
            {
                DataTable dt = new DataTable();

                Query selectQuery = Database.CreateQuery("SELECT * FROM Changesets WHERE MACADDR <> @MACADDR");
                selectQuery.Parameters.Add(new QueryParameter("@MACADDR", DbType.String, MacAddress));
                dt = Database.Select(selectQuery);

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        ServerUpdateMessage message = CreateServerUpdateMessage(row);

                        DateTime now = DateTime.Now;
                        DateTime changeset = message.CheckinDate;

                        TimeSpan ts = now - changeset;

                        if (message.UpdateType == ServerUpdateType.LockAllClientIsRefreshing)
                        {
                            // if someone is loading > 60 seconds then they probably crashed; ignore the lock
                            if (ts.TotalSeconds < 60)
                            {
                                canLoad = false;
                            }
                        }
                        else if (message.UpdateType == ServerUpdateType.UnlockAllClientRefreshComplete)
                        {
                            canLoad = true;
                        }
                    }
                }
            }

            return canLoad;
        }

        /// <summary>
        /// Sets the client's internal changeset to the current changeset in the database
        /// </summary>
        private void SyncChangesets(bool initialLoad = false)
        {
            // if the poll rate is zero, we're dealing with single-user input and don't need to deal with changesets
            if (IsMicrosoftSQLDatabase || PollRate > 0)
            {
                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(Database.ConnectionString + ";Connection Timeout=5"))
                {
                    conn.Open();

                    DataTable dt = new DataTable();

                    if (initialLoad)
                    {
                        DateTime yesterday = DateTime.Today.AddDays(-3);
                        using (System.Data.SqlClient.SqlCommand deleteCommand = new System.Data.SqlClient.SqlCommand("DELETE FROM [Changesets] WHERE [CheckinDate] < @CheckinDate", conn))
                        {
                            deleteCommand.Parameters.Add("@CheckinDate", SqlDbType.DateTime2).Value = yesterday;
                            deleteCommand.ExecuteNonQuery();
                        }
                    }

                    // pull all new changesets
                    Query selectQuery = Database.CreateQuery("SELECT * FROM Changesets WHERE MACADDR <> @MACADDR ORDER BY [Changeset] ASC");
                    selectQuery.Parameters.Add(new QueryParameter("@MACADDR", DbType.String, MacAddress));
                    dt = Database.Select(selectQuery);

                    if (dt.Rows.Count > 0)
                    {
                        // Set the changeset to the last one. Changeset column is autonumber, so the final row should always have the highest value (without otherwise specifying order by)
                        try
                        {
                            int currentDbChangeset = (int)dt.Rows[dt.Rows.Count - 1]["Changeset"];
                            if (currentDbChangeset > Changeset)
                            {
                                Changeset = currentDbChangeset;
                            }
                        }
                        catch (Core.Exceptions.InvalidChangesetException)
                        {
                            // Should never arrive here, but I've seen where data are not instantly propagated in MS Access after an INSERT, so
                            // this then doesn't pick up the new rows in the subsequent SELECT. Hopefully not an issue in MSSQLServer. Ideally,
                            // nobody should ever be using multi-user with MS Access anyway.

                            // do nothing and assume changeset is current (which is highly likely)
                        }
                        SyncLocks(dt);

                        if (initialLoad)
                        {
                            // send release notifications for any prior locks

                            using (System.Data.SqlClient.SqlCommand selectLocksCommand = new System.Data.SqlClient.SqlCommand("SELECT * FROM Changesets WHERE MACADDR = @MACADDR AND ((UpdateType >= 0 AND UpdateType <= 2) OR UpdateType = 6)", conn))
                            {
                                selectLocksCommand.Parameters.Add("@MACADDR", SqlDbType.NVarChar).Value = MacAddress;
                                using (System.Data.SqlClient.SqlDataReader r = selectLocksCommand.ExecuteReader())
                                {
                                    while (r.Read())
                                    {
                                        string destinationRecordId = (string)r["DestinationRecordID"];
                                        int updateType = (int)r["UpdateType"];

                                        if (updateType == 0)
                                        {
                                            SendMessage("Unlock case", destinationRecordId, ServerUpdateType.UnlockCase);
                                        }
                                        else if (updateType == 1)
                                        {
                                            SendMessage("Unlock contact", destinationRecordId, ServerUpdateType.UnlockContact);
                                        }
                                        else if (updateType == 2)
                                        {
                                            SendMessage("Unlock relationship", destinationRecordId, ServerUpdateType.UnlockRelationship);
                                        }
#if DEBUG
                                        System.Diagnostics.Debug.WriteLine(destinationRecordId + "(" + updateType.ToString() + ")");
#endif
                                    }
                                }
                            }

                            // clean up all of this client's prior locks - since we're just now connecting, those locks ought to be released
                            // obviously, only do this on initial connection otherwise we may inadvertendly release locks on a simple refresh

                            using (System.Data.SqlClient.SqlCommand deleteCommand = new System.Data.SqlClient.SqlCommand("DELETE FROM Changesets WHERE MACADDR = @MACADDR AND ((UpdateType >= 0 AND UpdateType <= 2) OR UpdateType = 6)", conn))
                            {
                                deleteCommand.Parameters.Add("@MACADDR", SqlDbType.NVarChar).Value = MacAddress;
                                deleteCommand.ExecuteNonQuery();
                            }
                        }
                    }
                    else
                    {
                        // no rows were returned, so no changes have been made yet - set changeset to zero
                        //Changeset = 0;
                    }

                    conn.Close();
                }
            }
        }

        /// <summary>
        /// Synchronizes the locks currently implemented in the database with the client. For example, when the client first connects and downloads the data, the locked records aren't otherwise known
        /// to the client, because the lock data doesn't exist in the case and contact records. 
        /// </summary>
        /// <param name="dt">The data table that contains changeset information</param>
        private void SyncLocks(DataTable dt)
        {
            foreach (DataRow row in dt.Rows)
            {
                ServerUpdateMessage message = CreateServerUpdateMessage(row);

                if (message.UpdateType == ServerUpdateType.UnlockCase ||
                    message.UpdateType == ServerUpdateType.UnlockContact ||
                    message.UpdateType == ServerUpdateType.UnlockRelationship)
                {
                    ExecuteServerChangesetMessage(message);
                }
                else if (message.UpdateType == ServerUpdateType.LockCase ||
                    message.UpdateType == ServerUpdateType.LockContact ||
                    message.UpdateType == ServerUpdateType.LockRelationship)
                {
                    DateTime now = DateTime.Now;
                    DateTime changeset = message.CheckinDate;

                    TimeSpan ts = now - changeset;

                    // Ignore any locks in place for > 24 hours, in case clients have crashed out after a lock was
                    // issued but before the unlock was issued. Note: Other mechanisms are supposed to better handle
                    // this, so look at removing...
                    if (ts.TotalHours <= 24)
                    {
                        ExecuteServerChangesetMessage(message);
                    }
                }
            }
        }

        public void SetDefaultIsolationViewFilter() // sets the 'isolated patients' filter, don't change without asking
        {
            SearchIsoCasesText = String.Empty;
            IsolatedCollectionView.Filter = new Predicate<object>(x =>
                    (
                    (
                    (((CaseViewModel)x).IsolationCurrent != null &&
                    ((CaseViewModel)x).IsolationCurrent.Equals("1")) ||
                    (((CaseViewModel)x).DateIsolationCurrent.HasValue)
                    ) &&
                    !((CaseViewModel)x).DateDeath2.HasValue &&
                    !((CaseViewModel)x).DateDischargeIso.HasValue &&
                    !((CaseViewModel)x).DateDischargeHospital.HasValue &&
                    !((CaseViewModel)x).CurrentStatus.Equals(Properties.Resources.Dead) &&
                    !((CaseViewModel)x).FinalStatus.Equals(Core.Enums.AliveDead.Dead)
                    )
                    );
            IsolatedCollectionView.Refresh();
            RaisePropertyChanged("IsolatedCollectionView");
        }

        private void SetCaseAndContactFlags()
        {
            foreach (CaseViewModel caseVM in CaseCollection)
            {
                caseVM.IsContact = false;
            }

            foreach (ContactViewModel contactVM in ContactCollection)
            {
                contactVM.IsCase = false;
            }

            // LINQ join query with anonymous type is slower than the existing method, so it is commented out
            //var query = from caseVM in CaseCollection
            //            join contactVM in ContactCollection on caseVM.RecordId equals contactVM.RecordId
            //            select new { CaseVM = caseVM, ContactVM = contactVM };

            //foreach (var item in query)
            //{
            //    item.CaseVM.IsContact = true;
            //    item.ContactVM.IsCase = true;
            //}

            foreach (CaseViewModel caseVM in CaseCollection)
            {
                ContactViewModel contactVM = GetContactVM(caseVM.RecordId);

                if (contactVM != null)
                {
                    caseVM.IsContact = true;
                    contactVM.IsCase = true;
                }
            }
        }

        protected virtual void SortFollowUps(ObservableCollection<DailyCheckViewModel> followUpCollection)
        {
            if (IsCountryUS)
            {
                ObservableCollection<DailyCheckViewModel> tempDFUC = new ObservableCollection<DailyCheckViewModel>();

                foreach (DailyCheckViewModel dcVM in followUpCollection.OrderBy(con => con.ContactVM.ContactStateID))
                {
                    tempDFUC.Add(dcVM);
                }

                var query = from dailyCheck in tempDFUC
                            //orderby dailyCheck.ContactVM.Village,/* dailyCheck.ContactVM.SubCounty, dailyCheck.ContactVM.District,*/ dailyCheck.ContactVM.Surname, dailyCheck.ContactVM.OtherNames
                            select dailyCheck;

                followUpCollection.Clear();

                foreach (var dailyCheck in query)
                {
                    followUpCollection.Add(dailyCheck);
                }
            }
            else
            {
                ObservableCollection<DailyCheckViewModel> tempDFUC = new ObservableCollection<DailyCheckViewModel>();

                foreach (DailyCheckViewModel dcVM in followUpCollection)
                {
                    tempDFUC.Add(dcVM);
                }

                var query = from dailyCheck in tempDFUC
                            //orderby dailyCheck.ContactVM.Village,/* dailyCheck.ContactVM.SubCounty, dailyCheck.ContactVM.District,*/ dailyCheck.ContactVM.Surname, dailyCheck.ContactVM.OtherNames
                            select dailyCheck;

                followUpCollection.Clear();

                foreach (var dailyCheck in query)
                {
                    followUpCollection.Add(dailyCheck);
                }
            }
        }

        protected override void SortCases()
        {
            lock (_caseCollectionLock)
            {
                CaseCollection.SortByID();
            }

            //ObservableCollection<CaseViewModel> tempCases = new ObservableCollection<CaseViewModel>();

            //foreach (CaseViewModel caseVM in CaseCollection)
            //{
            //    tempCases.Add(caseVM);
            //}

            //if (IsCountryUS)
            //{
            //    var query = from caseVM in tempCases
            //                orderby caseVM.OriginalID
            //                select caseVM;

            //    CaseCollection.Clear();

            //    foreach (var caseVM in query)
            //    {
            //        lock (_caseCollectionLock)
            //        {
            //            if (!CaseCollection.Contains(caseVM.RecordId))
            //            {
            //                CaseCollection.Add(caseVM);
            //            }

            //        }
            //    }
            //}
            //else
            //{
            //    var query = from caseVM in tempCases
            //                orderby caseVM.ID
            //                select caseVM;

            //    CaseCollection.Clear();

            //    foreach (var caseVM in query)
            //    {
            //        lock (_caseCollectionLock)
            //        {
            //            if (!CaseCollection.Contains(caseVM.RecordId))
            //            {
            //                CaseCollection.Add(caseVM);
            //            }
            //        }
            //    }
            //}

        }

        public void CheckCaseContactForDailyFollowUps(ContactViewModel contactVM, DateTime today, bool bypassCaseCheck = false)
        {
            if (contactVM.IsActive && ContactLinkCollection.ContainsContact(contactVM) && contactVM.DateOfLastContact.HasValue)
            {
                CaseContactPairViewModel ccpVM = ContactLinkCollection[contactVM];
                CaseViewModel caseVM = ccpVM.CaseVM;

                DateTime startDate = contactVM.FollowUpWindowViewModel.WindowStartDate;
                DateTime endDate = contactVM.FollowUpWindowViewModel.WindowEndDate;

                if (today >= startDate && today <= endDate)
                {
                    DailyCheckViewModel dcVM = new DailyCheckViewModel(contactVM, caseVM);

                    foreach (FollowUpVisitViewModel fuVM in contactVM.FollowUpWindowViewModel.FollowUpVisits)
                    {
                        if (fuVM.FollowUpVisit.Day == dcVM.GetDay(today)) //dcVM.Day)
                        {
                            dcVM.Status = fuVM.Status;
                            dcVM.Notes = fuVM.Notes;
                            dcVM.Temp1 = fuVM.Temp1;
                            dcVM.Temp2 = fuVM.Temp2;

                            break;
                        }
                    }

                    if (today.Day == DateTime.Now.Day && today.Month == DateTime.Now.Month && today.Year == DateTime.Now.Year)
                    {
                        bool found = false;
                        DailyCheckViewModel jDcVM = null;
                        lock (_dailyCollectionLock)
                        {
                            if (DailyFollowUpCollection.ContainsContact(contactVM))
                            {
                                jDcVM = DailyFollowUpCollection[contactVM];
                                found = true;
                            }
                        }

                        if (found)
                        {
                            if (!bypassCaseCheck)
                            {
                                if (ccpVM.CaseVM != jDcVM.CaseVM)
                                {
                                    lock (_dailyCollectionLock)
                                    {
                                        DailyFollowUpCollection.Remove(jDcVM);
                                        DailyFollowUpCollection.Add(dcVM);
                                    }
                                }
                            }
                            else
                            {
                                lock (_dailyCollectionLock)
                                {
                                    DailyFollowUpCollection.Remove(jDcVM);
                                    DailyFollowUpCollection.Add(dcVM);
                                }
                            }
                        }
                        else
                        {
                            lock (_dailyCollectionLock)
                            {
                                DailyFollowUpCollection.Add(dcVM);
                            }
                        }
                    }
                }
            }
        }

        protected virtual void CheckCasesContactsForDailyFollowUps()
        {
            DateTime today = DateTime.Today;

            foreach (ContactViewModel contactVM in this.ContactCollection)
            {
                CheckCaseContactForDailyFollowUps(contactVM, today, false);
            }
        }

        private void CheckCaseContactForDailyFollowUp(CaseViewModel caseVM, ContactViewModel contactVM, DateTime today, bool checkOutsideOfWindow = true)
        {
            if (contactVM.IsActive)
            {
                DateTime startDate = contactVM.FollowUpWindowViewModel.WindowStartDate;
                DateTime endDate = contactVM.FollowUpWindowViewModel.WindowEndDate;

                if (today >= startDate && today <= endDate)
                {
                    DailyCheckViewModel dcVM = new DailyCheckViewModel(contactVM, caseVM);

                    foreach (FollowUpVisitViewModel fuVM in contactVM.FollowUpWindowViewModel.FollowUpVisits)
                    {
                        if (fuVM.FollowUpVisit.Day == dcVM.GetDay(today)) //dcVM.Day)
                        {
                            dcVM.Status = fuVM.Status;
                            dcVM.Notes = fuVM.Notes;
                            dcVM.Temp1 = fuVM.Temp1;
                            dcVM.Temp2 = fuVM.Temp2;
                            break;
                        }
                    }

                    if (today.Day == DateTime.Now.Day && today.Month == DateTime.Now.Month && today.Year == DateTime.Now.Year)
                    {
                        bool found = false;
                        DailyCheckViewModel jDcVM = null;
                        lock (_dailyCollectionLock)
                        {
                            foreach (DailyCheckViewModel iDcVM in DailyFollowUpCollection)
                            {
                                if (iDcVM.ContactVM == contactVM)
                                {
                                    found = true;
                                    jDcVM = iDcVM;
                                    break;
                                }
                            }
                        }

                        if (found)
                        {
                            CaseContactPairViewModel ccpVM = (from ccp in ContactLinkCollection
                                                              where ccp.ContactVM == contactVM
                                                              orderby ccp.DateLastContact descending
                                                              select ccp).First();

                            if (ccpVM != null)
                            {
                                if (ccpVM.CaseVM != jDcVM.CaseVM)
                                {
                                    lock (_dailyCollectionLock)
                                    {
                                        DailyFollowUpCollection.Remove(jDcVM);
                                        DailyFollowUpCollection.Add(dcVM);
                                    }
                                }
                            }
                        }
                        else
                        {
                            lock (_dailyCollectionLock)
                            {
                                DailyFollowUpCollection.Add(dcVM);
                            }
                        }
                    }
                    else
                    {
                        #region Previous Follow-ups
                        //PrevFollowUpCollection.Add(dcVM);
                        bool found = false;
                        DailyCheckViewModel jDcVM = null;
                        foreach (DailyCheckViewModel iDcVM in PrevFollowUpCollection)
                        {
                            if (iDcVM.ContactVM == contactVM || iDcVM.ContactVM.RecordId == contactVM.RecordId)
                            {
                                found = true;
                                jDcVM = iDcVM;
                                break;
                            }
                        }

                        if (found)
                        {
                            var query = from ccpVM in ContactLinkCollection
                                        where ccpVM.ContactVM == contactVM || ccpVM.ContactVM.RecordId == contactVM.RecordId
                                        orderby ccpVM.DateLastContact descending
                                        select ccpVM;

                            foreach (CaseContactPairViewModel ccpVM in query)
                            {
                                if (ccpVM.CaseVM != jDcVM.CaseVM || ccpVM.CaseVM.RecordId != jDcVM.CaseVM.RecordId)
                                {
                                    lock (_prevCollectionLock)
                                    {
                                        PrevFollowUpCollection.Remove(jDcVM);
                                        PrevFollowUpCollection.Add(dcVM);
                                    }
                                }
                                break;
                            }
                        }
                        else
                        {
                            lock (_prevCollectionLock)
                            {
                                PrevFollowUpCollection.Add(dcVM);
                            }
                        }
                        #endregion // Previous Follow-ups
                    }
                }
                else
                {
                    if (checkOutsideOfWindow)
                    {
                        for (int i = DailyFollowUpCollection.Count - 1; i >= 0; i--)
                        {
                            DailyCheckViewModel dcVM = DailyFollowUpCollection[i];
                            if (dcVM.ContactVM == contactVM && dcVM.CaseVM == caseVM)
                            {
                                DailyFollowUpCollection.Remove(dcVM);
                            }
                        }
                    }
                }
            }
        }

        protected virtual DataTable GetContactsTable()
        {
            if (Database is Epi.Data.Office.OleDbDatabase && _oleDbConnection != null && _oleDbConnection.State == ConnectionState.Open)
            {
                OleDbCommand comm = new OleDbCommand();
                OleDbDataAdapter dAdpter = new OleDbDataAdapter(comm);

                comm.Connection = _oleDbConnection;
                comm.CommandText = ContactDataSelectCommand;

                DataTable dt = new DataTable();
                dAdpter.Fill(dt);
                return dt;
            }
            else
            {
                Query selectQuery = Database.CreateQuery(ContactDataSelectCommand);
                return Database.Select(selectQuery);
            }
        }

        public virtual void InitializeProject(VhfProject project)
        {
            Common.DaysInWindow = 21; // default

            if (LoadConfig())
            {
                IsLoadingProjectData = true;
                LoadStatus = "Initializing...";
                TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
                //IsCountryUS = (Properties.Settings.Default["Region"].ToString() == "USA");
                IsCountryUS = (ApplicationViewModel.Instance.CurrentRegion == RegionEnum.USA);
                CaseForm = project.Views[ContactTracing.Core.Constants.CASE_FORM_NAME];
                LabForm = project.Views[ContactTracing.Core.Constants.LAB_FORM_NAME];
                ContactForm = project.Views[ContactTracing.Core.Constants.CONTACT_FORM_NAME];
                Project = project;

                Database = Project.CollectedData.GetDatabase();
                DbLogger.Database = Database;

                if (Database is Epi.Data.Office.OleDbDatabase)
                {
                    _oleDbConnection = new System.Data.OleDb.OleDbConnection(Database.ConnectionString);
                    _oleDbConnection.Open();
                    IsMultiUser = false;
                }
                else
                {
                    IsMultiUser = true;
                }

                Query selectQuery = Database.CreateQuery("SELECT * FROM [metaViews]");
                DataTable dt = Database.Select(selectQuery);

                foreach (DataRow row in dt.Rows)
                {
                    if (row["Name"].ToString().Equals(Core.Constants.CASE_FORM_NAME))
                    {
                        CaseFormId = int.Parse(row["ViewId"].ToString());
                    }
                    else if (row["Name"].ToString().Equals(Core.Constants.CONTACT_FORM_NAME))
                    {
                        ContactFormId = int.Parse(row["ViewId"].ToString());
                    }
                    else if (row["Name"].ToString().Equals(Core.Constants.LAB_RESULTS_FORM_NAME))
                    {
                        LabFormId = int.Parse(row["ViewId"].ToString());
                    }
                }

                if (CaseFormId == -1 || ContactFormId == -1)
                {
                    throw new InvalidOperationException("The database is corrupt. The application cannot run. Check to make sure there is a contact form and a case form with the expected database structures.");
                }

                #region Create changeset table

                // only do this if we're MSSQL server
                if (!(Database is Epi.Data.Office.OleDbDatabase))
                {
                    // don't create the table if it's already there; this should only be done once
                    if (!Database.TableExists("UpdateLog") && IsMicrosoftSQLDatabase)
                    {
                        Query createTableQuery = Database.CreateQuery("CREATE TABLE dbo.UpdateLog (" +
                            "ID int not null identity(1,1), " +
                            "UDate datetime2 not null, " +
                            "VhfVer nvarchar(32), " +
                            "UserName nvarchar(32), " +
                            "MACADDR nvarchar(128), " +
                            "Description nvarchar(512) " +
                                ");");

                        Database.ExecuteNonQuery(createTableQuery);
                    }

                    // don't create the table if it's already there; this should only be done once
                    if (!Database.TableExists("Changesets"))
                    {
                        if (IsMicrosoftSQLDatabase)
                        {
                            Query createTableQuery = Database.CreateQuery("CREATE TABLE dbo.Changesets (" +
                                "ChangesetID nvarchar(64), " +
                                "UpdateType int not null, " +
                                "UserId nvarchar(32), " +
                                "MACADDR nvarchar(128), " +
                                "Description nvarchar(256), " +
                                "DestinationRecordID nvarchar(64), " +
                                "CheckinDate datetime2 not null, " +
                                "Changeset int not null identity(1,1), " +
                                "VhfVersion nvarchar(32) " +
                                ");");

                            Database.ExecuteNonQuery(createTableQuery);


                            // TODO: Add a way to create MSSQL server databases using Epi Info templates so we can avoid this disaster of a subroutine... keep in mind, this is all needed when manually migrating from MDB files to MSSQL using the SQL Server migration tool

                            // fix unique key columns that likely didn't come over right during a SQL migration
                            // unique key must be set to 'identity' for the application to work properly
                            Query alterContactTableQuery = Database.CreateQuery("ALTER TABLE " + ContactForm.TableName + " " +
                                "DROP COLUMN UniqueKey");
                            Database.ExecuteNonQuery(alterContactTableQuery);

                            alterContactTableQuery = Database.CreateQuery("ALTER TABLE " + ContactForm.TableName + " " +
                                "ADD UniqueKey int not null identity(1,1)");
                            Database.ExecuteNonQuery(alterContactTableQuery);

                            Query alterCaseTableQuery = Database.CreateQuery("ALTER TABLE " + CaseForm.TableName + " " +
                                "DROP COLUMN UniqueKey");
                            Database.ExecuteNonQuery(alterCaseTableQuery);

                            alterCaseTableQuery = Database.CreateQuery("ALTER TABLE " + CaseForm.TableName + " " +
                                "ADD UniqueKey int not null identity(1,1)");
                            Database.ExecuteNonQuery(alterCaseTableQuery);

                            Query metaLinksTableQuery = Database.CreateQuery("ALTER TABLE metaLinks " +
                                "DROP COLUMN LinkId");
                            Database.ExecuteNonQuery(metaLinksTableQuery);

                            metaLinksTableQuery = Database.CreateQuery("ALTER TABLE metaLinks " +
                                "ADD LinkId int not null identity(1,1)");
                            Database.ExecuteNonQuery(metaLinksTableQuery);

                            // rec status column - needs default value
                            try
                            {
                                alterContactTableQuery = Database.CreateQuery("ALTER TABLE " + ContactForm.TableName + " " +
                                    "DROP COLUMN RECSTATUS");
                                Database.ExecuteNonQuery(alterContactTableQuery);

                                alterContactTableQuery = Database.CreateQuery("ALTER TABLE " + ContactForm.TableName + " " +
                                    "ADD RECSTATUS int not null DEFAULT 1");
                                Database.ExecuteNonQuery(alterContactTableQuery);

                                alterCaseTableQuery = Database.CreateQuery("ALTER TABLE " + CaseForm.TableName + " " +
                                    "DROP COLUMN RECSTATUS");
                                Database.ExecuteNonQuery(alterCaseTableQuery);

                                alterCaseTableQuery = Database.CreateQuery("ALTER TABLE " + CaseForm.TableName + " " +
                                    "ADD RECSTATUS int not null DEFAULT 1");
                                Database.ExecuteNonQuery(alterCaseTableQuery);
                            }
                            catch (System.Data.SqlClient.SqlException)
                            {
                                // do nothing, because we're assuming this operation was done once already... we'd
                                // only expect to arrive in this block if we already migrated over once, something
                                // went wrong, and we had to drop Changesets to re-do the table maniupation (in
                                // which case we can't drop columns with default values easily... this try/catch
                                // is just simpler than quertying sys tables (etc) for now)
                            }

                            #region Lab Form SQL Migration fixes
                            // rec status column - needs default value
                            try
                            {
                                Query alterLabTableQuery = Database.CreateQuery("ALTER TABLE " + LabForm.TableName + " " +
                                    "DROP COLUMN UniqueKey");
                                Database.ExecuteNonQuery(alterLabTableQuery);

                                alterLabTableQuery = Database.CreateQuery("ALTER TABLE " + LabForm.TableName + " " +
                                    "ADD UniqueKey int not null identity(1,1)");
                                Database.ExecuteNonQuery(alterLabTableQuery);

                                alterLabTableQuery = Database.CreateQuery("ALTER TABLE " + LabForm.TableName + " " +
                                    "DROP COLUMN RECSTATUS");
                                Database.ExecuteNonQuery(alterLabTableQuery);

                                alterLabTableQuery = Database.CreateQuery("ALTER TABLE " + LabForm.TableName + " " +
                                    "ADD RECSTATUS int not null DEFAULT 1");
                                Database.ExecuteNonQuery(alterLabTableQuery);
                            }
                            catch (System.Data.SqlClient.SqlException)
                            {
                                // do nothing, because we're assuming this operation was done once already... we'd
                                // only expect to arrive in this block if we already migrated over once, something
                                // went wrong, and we had to drop Changesets to re-do the table maniupation (in
                                // which case we can't drop columns with default values easily... this try/catch
                                // is just simpler than quertying sys tables (etc) for now)
                            }

                            // fix all bit columns that were migrated
                            foreach (Field field in LabForm.Fields.DataFields)
                            {
                                if (field is CheckBoxField)
                                {
                                    CheckBoxField cboxField = (field as CheckBoxField);
                                    if (cboxField != null)
                                    {
                                        Query bitColumnUpdater = Database.CreateQuery("ALTER TABLE " + cboxField.Page.TableName + " " +
                                            "ALTER COLUMN " + cboxField.Name + " bit null");
                                        Database.ExecuteNonQuery(bitColumnUpdater);
                                    }
                                }
                            }

                            #endregion Lab Form SQL Migration fixes

                            // fix all bit columns that were migrated
                            foreach (Field field in CaseForm.Fields.DataFields)
                            {
                                if (field is CheckBoxField)
                                {
                                    CheckBoxField cboxField = (field as CheckBoxField);
                                    if (cboxField != null)
                                    {
                                        Query bitColumnUpdater = Database.CreateQuery("ALTER TABLE " + cboxField.Page.TableName + " " +
                                            "ALTER COLUMN " + cboxField.Name + " bit null");
                                        Database.ExecuteNonQuery(bitColumnUpdater);
                                    }
                                }
                            }
                        }
                    }
                }

                CaseDataSelectCommand = "";

                if (IsCountryUS)
                {
                    CaseDataSelectCommand = "SELECT t.GlobalRecordId, ID, OrigID, Surname, OtherNames, Age,  Gender,  StatusReport, " + //17178 removed ageunit, headhouse
                           "RecComplete, RecNoCRF, RecMissingCRFInfo, DateReport, RecPendLab, RecPendOutcome, DateDeath, DateDeath2, DateOnset, DistrictOnset, SCOnset, CountryOnset, VillageOnset, DateDischargeIso, DateHospitalCurrentAdmit, " +
                           "DateIsolationCurrent, PlaceDeath, HCW, StatusAsOfCurrentDate, FinalLabClass, FinalStatus, DistrictRes,  AddressRes, ZipRes, VillageRes, Citizenship, CountryRes, SCRes, t.UniqueKey, EpiCaseDef, IsolationCurrent, HospitalCurrent, " +
                           "DateDischargeHosp, DOB, Email, HCWposition, HCWFacility, HCWDistrict, HCWSC, HCWVillage, PhoneNumber " + //17227 Added DOB, PhoneNumber, Email, HCWposition, HCWFacility, HCWDistrict, HCWSC, HCWVillage
                           CaseForm.FromViewSQL + " WHERE [RECSTATUS] > 0 ORDER BY OrigID";

                    ContactDataSelectCommand = "SELECT * " +
                    ContactForm.FromViewSQL + " WHERE [RECSTATUS] > 0 ORDER BY ContactStateID";
                }
                else
                {
                    CaseDataSelectCommand = "SELECT t.GlobalRecordId, ID, OrigID, Surname, OtherNames, Age, AgeUnit, Gender, HeadHouse, StatusReport, " +
                           "RecComplete, RecNoCRF, RecMissingCRFInfo, DateReport, RecPendLab, RecPendOutcome, DateDeath, DateDeath2, DateOnset, DistrictOnset, SCOnset, CountryOnset, VillageOnset, DateDischargeIso, DateHospitalCurrentAdmit, " +
                           "DateIsolationCurrent, PlaceDeath, HCW, StatusAsOfCurrentDate, FinalLabClass, FinalStatus, DistrictRes, VillageRes, CountryRes, SCRes, ParishRes, t.UniqueKey, EpiCaseDef, IsolationCurrent, HospitalCurrent, " +
                           "DateDischargeHosp " +
                           CaseForm.FromViewSQL + " WHERE [RECSTATUS] > 0 ORDER BY ID";
                    ContactDataSelectCommand = "SELECT * " +
                    ContactForm.FromViewSQL + " WHERE [RECSTATUS] > 0 ORDER BY UniqueKey";
                }





                #endregion
            }
        }

        protected override DataTable GetLabTable()
        {
            if (Database is Epi.Data.Office.OleDbDatabase && _oleDbConnection != null && _oleDbConnection.State == ConnectionState.Open)
            {
                OleDbCommand comm = new OleDbCommand();
                OleDbDataAdapter dAdpter = new OleDbDataAdapter(comm);

                comm.Connection = _oleDbConnection;
                comm.CommandText = "SELECT * " + LabForm.FromViewSQL;

                DataTable dt = new DataTable();
                dAdpter.Fill(dt);
                return dt;
            }
            else
            {
                Query selectQuery = Database.CreateQuery("SELECT * " +
                    LabForm.FromViewSQL);
                return Database.Select(selectQuery);
            }
        }

        protected DataTable GetLabTableforCase(string fKey)
        {
            if (Database is Epi.Data.Office.OleDbDatabase && _oleDbConnection != null && _oleDbConnection.State == ConnectionState.Open)
            {
                OleDbCommand comm = new OleDbCommand();
                OleDbDataAdapter dAdpter = new OleDbDataAdapter(comm);

                comm.Connection = _oleDbConnection;
                comm.CommandText = "SELECT * " + LabForm.FromViewSQL + " Where FKEY = '" + fKey + "'";

                DataTable dt = new DataTable();
                dAdpter.Fill(dt);
                return dt;
            }
            else
            {
                Query selectQuery = Database.CreateQuery("SELECT * " +
                    LabForm.FromViewSQL + " Where FKEY = '" + fKey + "'");
                return Database.Select(selectQuery);
            }
        }

        protected override DataTable GetCasesTable()
        {
            if (Database is Epi.Data.Office.OleDbDatabase && _oleDbConnection != null && _oleDbConnection.State == ConnectionState.Open)
            {
                OleDbCommand comm = new OleDbCommand();
                OleDbDataAdapter dAdpter = new OleDbDataAdapter(comm);

                comm.Connection = _oleDbConnection;
                comm.CommandText = CaseDataSelectCommand;

                DataTable dt = new DataTable();
                dAdpter.Fill(dt);
                return dt;
            }
            else
            {
                Query selectQuery = Database.CreateQuery(CaseDataSelectCommand);
                return Database.Select(selectQuery);
            }
        }

        public void ReActivateContact(ContactViewModel contactVM)
        {
            contactVM.FinalOutcome = String.Empty;
            SetContactFinalStatus(contactVM);
        }

        public bool CaseHasExposures(CaseViewModel caseVM)
        {
            Query selectQuery = Database.CreateQuery("SELECT * FROM [metaLinks] WHERE [FromRecordGuid] = @FromRecordGuid AND [FromViewId] = @FromViewId AND [ToViewId] = @ToViewId");
            selectQuery.Parameters.Add(new QueryParameter("@FromRecordGuid", DbType.String, caseVM.RecordId));
            selectQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, CaseFormId));
            selectQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, CaseFormId));
            DataTable dt = Database.Select(selectQuery);

            if (dt.Rows.Count >= 1) return true;

            return false;
        }

        private string ParsePhoneNumber(string phoneNumber)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in phoneNumber)
            {
                int digit;
                bool result = int.TryParse(c.ToString(), out digit);
                if (result)
                {
                    sb.Append(c.ToString());
                }
            }

            return sb.ToString();
        }

        private IEnumerable<IGrouping<string, DailyCheckViewModel>> GetPrevCheck(int indexDate, DateTime minDate, Core.Enums.LocationType locationType = Core.Enums.LocationType.Village)
        {
            if (locationType == Core.Enums.LocationType.Village)
            {
                var query = from prevCheck in PrevFollowUpCollection
                            where
                            (!prevCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate].Status.HasValue || prevCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate].Status == ContactDailyStatus.NotSeen ||
                            prevCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate].Status == ContactDailyStatus.NotRecorded)
                            &&
                            !prevCheck.ContactVM.HasFinalOutcome &&
                            prevCheck.ContactVM.FollowUpWindowViewModel.WindowStartDate < minDate
                            group prevCheck by string.Concat("<span style=\"font-weight: bold;\">" + Adm4 + ":</span> ", prevCheck.ContactVM.Village);
                return query as IEnumerable<IGrouping<string, DailyCheckViewModel>>;
            }
            else if (locationType == Core.Enums.LocationType.District)
            {
                var query = from prevCheck in PrevFollowUpCollection
                            where
                            (!prevCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate].Status.HasValue || prevCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate].Status == ContactDailyStatus.NotSeen ||
                            prevCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate].Status == ContactDailyStatus.NotRecorded)
                            &&
                            !prevCheck.ContactVM.HasFinalOutcome &&
                            prevCheck.ContactVM.FollowUpWindowViewModel.WindowStartDate < minDate
                            group prevCheck by string.Concat("<span style=\"font-weight: bold;\">" + Adm1 + ":</span> ", prevCheck.ContactVM.District);
                return query as IEnumerable<IGrouping<string, DailyCheckViewModel>>;
            }
            else if (locationType == Core.Enums.LocationType.SubCounty)
            {
                var query = from prevCheck in PrevFollowUpCollection
                            where
                            (!prevCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate].Status.HasValue || prevCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate].Status == ContactDailyStatus.NotSeen ||
                            prevCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate].Status == ContactDailyStatus.NotRecorded)
                            &&
                            !prevCheck.ContactVM.HasFinalOutcome &&
                            prevCheck.ContactVM.FollowUpWindowViewModel.WindowStartDate < minDate
                            group prevCheck by string.Concat("<span style=\"font-weight: bold;\">" + Adm2 + ":</span> ", prevCheck.ContactVM.SubCounty);
                return query as IEnumerable<IGrouping<string, DailyCheckViewModel>>;
            }
            else
            {
                throw new NotImplementedException("The specified location field is unavailable for contact tracing.");
            }
        }

        private IEnumerable<IGrouping<string, DailyCheckViewModel>> GetDailyCheck(ObservableCollection<DailyCheckViewModel> collection, DateTime minDate, ContactTracing.Core.Enums.LocationType locationType = Core.Enums.LocationType.Village)
        {
            if (locationType == Core.Enums.LocationType.Village)
            {
                var query = from dailyCheck in collection
                            where dailyCheck.ContactVM.FollowUpWindowViewModel.WindowStartDate >= minDate && !dailyCheck.ContactVM.HasFinalOutcome
                            group dailyCheck by String.Concat("<span style=\"font-weight: bold;\">" + Adm4 + ":</span> ", dailyCheck.ContactVM.Village);
                return query as IEnumerable<IGrouping<string, DailyCheckViewModel>>;
            }
            else if (locationType == Core.Enums.LocationType.District)
            {
                var query = from dailyCheck in collection
                            where dailyCheck.ContactVM.FollowUpWindowViewModel.WindowStartDate >= minDate && !dailyCheck.ContactVM.HasFinalOutcome
                            group dailyCheck by String.Concat("<span style=\"font-weight: bold;\">" + Adm1 + ":</span> ", dailyCheck.ContactVM.District);
                return query as IEnumerable<IGrouping<string, DailyCheckViewModel>>;
            }
            else if (locationType == Core.Enums.LocationType.SubCounty)
            {
                var query = from dailyCheck in collection
                            where dailyCheck.ContactVM.FollowUpWindowViewModel.WindowStartDate >= minDate && !dailyCheck.ContactVM.HasFinalOutcome
                            group dailyCheck by String.Concat("<span style=\"font-weight: bold;\">" + Adm2 + ":</span> ", dailyCheck.ContactVM.SubCounty);
                return query as IEnumerable<IGrouping<string, DailyCheckViewModel>>;
            }
            else
            {
                throw new NotImplementedException("The specified location field is unavailable for contact tracing.");
            }
        }


    }
}
