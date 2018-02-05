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
using ContactTracing.CaseView.Converters;

namespace ContactTracing.CaseView
{
    public partial class EpiDataHelper
    {
        #region Commands
        public ICommand RepopulateCollectionsCommand { get { return new RelayCommand<bool>(RepopulateCollections, new Predicate<bool>(CanExecuteRepopulateCollectionsCommand)); } }
        #endregion // Commands

        /// <summary>
        /// Used to repopulate all collections. This is an expensive process and should only be called when absolutely necessary.
        /// </summary>
        public override void RepopulateCollections(bool initialLoad = false)
        {
            SearchCasesText = String.Empty;
            SearchContactsText = String.Empty;
            SearchExistingCasesText = String.Empty;
            SearchExistingContactsText = String.Empty;
            SearchIsoCasesText = String.Empty;

            bool success = false;

            ClearCollections();

            DbLogger.Log(String.Format("Initiated 'Repopulate collections', initial load = {0}", initialLoad.ToString()));

            Task.Factory.StartNew(
                () =>
                {
                    success = PopulateCollections(initialLoad);
                },
                 System.Threading.CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).ContinueWith(
                 delegate
                 {
                     if (success)
                     {
                         DbLogger.Log(String.Format("Completed 'Repopulate collections' - success"));

                         CaseCollectionView = new CollectionViewSource { Source = CaseCollection }.View;//System.Windows.Data.CollectionViewSource.GetDefaultView(CaseCollection);

                         CaseCollectionView.SortDescriptions.Add(new SortDescription("ID", ListSortDirection.Ascending));

                         RaisePropertyChanged("CaseCollectionView");
                         ExistingCaseCollectionView = new CollectionViewSource { Source = CaseCollection }.View;
                         RaisePropertyChanged("ExistingCaseCollectionView");

                         IsolatedCollectionView = new CollectionViewSource { Source = CaseCollection }.View;
                         SetDefaultIsolationViewFilter();

                         CasesWithoutContactsCollectionView = new CollectionViewSource { Source = CaseCollection }.View;
                         CasesWithoutContactsCollectionView.Filter = new Predicate<object>
                                 (
                                     caseVM =>
                                         ((CaseViewModel)caseVM).Contacts.Count == 0 && (
                                         ((CaseViewModel)caseVM).EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed ||
                                         ((CaseViewModel)caseVM).EpiCaseDef == Core.Enums.EpiCaseClassification.Probable)
                                         );

                         CasesWithoutContactsCollectionView.SortDescriptions.Add(new SortDescription("District", ListSortDirection.Ascending));
                         CasesWithoutContactsCollectionView.SortDescriptions.Add(new SortDescription("Surname", ListSortDirection.Ascending));
                         CasesWithoutContactsCollectionView.SortDescriptions.Add(new SortDescription("OtherNames", ListSortDirection.Ascending));

                         ContactCollectionView = new CollectionViewSource { Source = ContactCollection }.View;
                         ContactCollectionView.SortDescriptions.Add(new SortDescription("UniqueKey", ListSortDirection.Ascending));
                         ContactCollectionView.SortDescriptions.Add(new SortDescription("ContactID", ListSortDirection.Ascending));

                         RaisePropertyChanged("ContactCollectionView");

                         ExistingContactCollectionView = new CollectionViewSource { Source = ContactCollection }.View;
                         RaisePropertyChanged("ExistingContactCollectionView");


                         SyncChangesets(initialLoad);

                         if (!(this.Database is Epi.Data.Office.OleDbDatabase)) // if SQL database, do polling and assume we're multi-user
                         {
                             PollRate = ContactTracing.Core.Constants.DEFAULT_POLL_RATE;
                             this.UpdateTimer.Interval = PollRate;
                             this.UpdateTimer.Start();
                         }

                         LoadStatus = String.Empty;
                         IsLoadingProjectData = false;

                         TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                         TaskbarProgressValue = 0;
                     }
                     else
                     {
                         DbLogger.Log(String.Format("Completed 'Repopulate collections' - failure, closing project"));

                         LoadStatus = String.Empty;
                         IsLoadingProjectData = false;

                         TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                         TaskbarProgressValue = 0;

                         CloseProject();
                     }

                     if (CaseDataPopulated != null)
                     {
                         CaseDataPopulated(this, new CaseDataPopulatedArgs(Core.Enums.VirusTestTypes.Sudan, true));
                     }

                     CommandManager.InvalidateRequerySuggested();

                 }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        /// <summary>
        /// Used to populate the entire set of collection objects based on what is currently residing in the database. This is
        /// not recommended to be called often; once on startup is probably enough, otherwise performance issues may abound.
        /// </summary>
        protected override bool PopulateCollections(bool initialLoad = false)
        {
            if (Project == null) return false;

            IsShowingError = false;
            ErrorMessage = String.Empty;
            ErrorMessageDetail = String.Empty;

            IsLoadingProjectData = true;
#if DEBUG
            System.Diagnostics.Stopwatch swMain = new System.Diagnostics.Stopwatch();
            swMain.Start();
#endif

            MacAddress = Project.MacAddress;

            if (initialLoad)
            {
                #region Set Static Data
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
                LabResultViewModel.IsCountryUS = this.IsCountryUS;
                #endregion // Set Static Data
            }

            LoadStatus = String.Empty;
            TaskbarProgressValue = 0;
            TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;

            try
            {
                System.Diagnostics.Stopwatch canLoadWaitStopwatch = new System.Diagnostics.Stopwatch();
                canLoadWaitStopwatch.Start();
                while (!CanLoadData() && canLoadWaitStopwatch.Elapsed.TotalSeconds < 60)
                {
                    // just wait
                }
                canLoadWaitStopwatch.Stop();
#if DEBUG
                System.Diagnostics.Debug.Print("Waited on other clients to load: " + canLoadWaitStopwatch.Elapsed.TotalMilliseconds.ToString());
#endif
                canLoadWaitStopwatch = null;

                #region Parallel Load

                DataTable caseTable = new DataTable();
                DataTable contactTable = new DataTable();
                DataTable linksTable = new DataTable();
                DataTable historyTable = new DataTable();

                LoadStatus = "Downloading data from server...";

                #region Read metaDbInfo

#if DEBUG
                System.Diagnostics.Stopwatch swmdb = new System.Diagnostics.Stopwatch();
                swmdb.Start();
#endif

                if (initialLoad)
                {
                    #region 094x to 095x Upgrade
                    UpgradeDatabase();

                    if (!Database.ColumnExists("metaHistory", "Temp1"))
                    {
                        Database.AddColumn("metaHistory", new TableColumn("Temp1", GenericDbColumnType.Double, true));
                        Database.AddColumn("metaHistory", new TableColumn("Temp2", GenericDbColumnType.Double, true));
                    }
                    #endregion // 094x to 095x Upgrade

                    // TODO: This can probably be deprecated or moved several versions from now
                    #region Add Columns to old DB's

                    if (ApplicationViewModel.Instance.CurrentRegion == RegionEnum.International)
                    {
                        string contactEntryFormName = ContactTracing.CaseView.Properties.Settings.Default.ContactEntryFormName_International;

                        // Add Team to ContactEntryForm(x )    
                        if (!Database.ColumnExists(contactEntryFormName, "Team"))
                        {
                            TableColumn TeamColumn = new TableColumn("Team", GenericDbColumnType.String, true);

                            try
                            {
                                Database.AddColumn(contactEntryFormName, TeamColumn);
                            }
                            catch (Exception)
                            {
                                // do nothing
                            }
                        }
                        // Add  ContactParish  to ContactEntryForm(x)  
                        if (!Database.ColumnExists(contactEntryFormName, "ContactParish"))
                        {
                            TableColumn ContactParishColumn = new TableColumn("ContactParish", GenericDbColumnType.String, true);

                            try
                            {
                                Database.AddColumn(contactEntryFormName, ContactParishColumn);
                            }
                            catch (Exception)
                            {
                                // do nothing
                            }
                        }
                    }


                    if (!Database.ColumnExists("metaDbInfo", "IsShortForm")) //17040
                    {
                        TableColumn isShortFormColumn = new TableColumn("IsShortForm", GenericDbColumnType.Boolean, true); // 1 = ShortForm, 0 = LongForm
                        int isShortForm = 1;
                        if (IsCountryUS)
                        {
                            isShortForm = 0;
                        }
                        else
                        {
                            isShortForm = 1;
                        }

                        try
                        {
                            Database.AddColumn("metaDbInfo", isShortFormColumn);
                            Query updateQuery = Database.CreateQuery("UPDATE metaDbInfo SET IsShortForm = " + isShortForm); // 1 = VHF, 0 = Epi Info
                            Database.ExecuteNonQuery(updateQuery);
                        }
                        catch (Exception)
                        {
                            // do nothing
                        }
                    }
                    if (!Database.ColumnExists("metaDbInfo", "ContactFormType"))
                    {
                        TableColumn contactFormTypeColumn = new TableColumn("ContactFormType", GenericDbColumnType.Int32, true);
                        int contactType = 0;
                        if (IsCountryUS)
                        {
                            contactType = 0;
                        }
                        else
                        {
                            contactType = 1;
                        }
                        try
                        {
                            Database.AddColumn("metaDbInfo", contactFormTypeColumn);
                            Query updateQuery = Database.CreateQuery("UPDATE metaDbInfo SET ContactFormType = " + contactType); // 1 = VHF, 0 = Epi Info
                            Database.ExecuteNonQuery(updateQuery);
                            ContactFormType = contactType;
                        }
                        catch (Exception)
                        {
                            // do nothing
                        }
                    }

                    if (!Database.ColumnExists("metaDbInfo", "VhfVersion"))
                    {
                        TableColumn vhfVersionColumn = new TableColumn("VhfVersion", GenericDbColumnType.String, 32, false);
                        try
                        {
                            Database.AddColumn("metaDbInfo", vhfVersionColumn);
                        }
                        catch (Exception)
                        {
                            // do nothing
                        }
                    }

                    if (!Database.ColumnExists("metaDbInfo", "Culture"))
                    {
                        TableColumn cultureColumn = new TableColumn("Culture", GenericDbColumnType.String, 5, false);
                        try
                        {
                            Database.AddColumn("metaDbInfo", cultureColumn);
                        }
                        catch (Exception)
                        {
                            // do nothing
                        }
                    }

                    if (!Database.ColumnExists("metaDbInfo", "Adm1"))
                    {
                        TableColumn adm1VersionColumn = new TableColumn("Adm1", GenericDbColumnType.String, 48, false);
                        try
                        {
                            Database.AddColumn("metaDbInfo", adm1VersionColumn);
                        }
                        catch (Exception)
                        {
                            // do nothing
                        }
                    }

                    if (!Database.ColumnExists("metaDbInfo", "Adm2"))
                    {
                        TableColumn adm2VersionColumn = new TableColumn("Adm2", GenericDbColumnType.String, 48, false);
                        try
                        {
                            Database.AddColumn("metaDbInfo", adm2VersionColumn);
                        }
                        catch (Exception)
                        {
                            // do nothing
                        }
                    }

                    if (!Database.ColumnExists("metaDbInfo", "Adm3"))
                    {
                        TableColumn adm3VersionColumn = new TableColumn("Adm3", GenericDbColumnType.String, 48, false);
                        try
                        {
                            Database.AddColumn("metaDbInfo", adm3VersionColumn);
                        }
                        catch (Exception)
                        {
                            // do nothing
                        }
                    }

                    if (!Database.ColumnExists("metaDbInfo", "Adm4"))
                    {
                        TableColumn adm4VersionColumn = new TableColumn("Adm4", GenericDbColumnType.String, 48, false);
                        try
                        {
                            Database.AddColumn("metaDbInfo", adm4VersionColumn);
                        }
                        catch (Exception)
                        {
                            // do nothing
                        }
                    }
                    #endregion Add Columns to old DB's
                }

                Query selectmetaDbQuery = Database.CreateQuery("SELECT * FROM metaDbInfo");
                DataTable dt = Database.Select(selectmetaDbQuery);

                OutbreakName = dt.Rows[0]["OutbreakName"].ToString();
                RaisePropertyChanged("OutbreakName");
                if (!String.IsNullOrEmpty(dt.Rows[0]["OutbreakDate"].ToString()))
                {
                    OutbreakDate = (DateTime)dt.Rows[0]["OutbreakDate"];
                }
                IDPrefix = dt.Rows[0]["IDPrefix"].ToString();
                IDSeparator = dt.Rows[0]["IDSeparator"].ToString();
                IDPattern = dt.Rows[0]["IDPattern"].ToString();
                Country = dt.Rows[0]["PrimaryCountry"].ToString();

                if (Country.Equals("USA", StringComparison.OrdinalIgnoreCase))
                {
                    IsCountryUS = true;
                }
                else
                {
                    IsCountryUS = false;
                }

                var IsShortVal = dt.Rows[0]["IsShortForm"].ToString(); //17040

                if (IsShortVal != null && !string.IsNullOrEmpty(IsShortVal))
                {
                    IsShortForm = Convert.ToBoolean(IsShortVal);
                }
                else
                {
                    IsShortForm = false;
                }

                if (dt.Columns.Contains("ContactFormType") && dt.Rows[0]["ContactFormType"] != DBNull.Value)
                {
                    ContactFormType = (int)dt.Rows[0]["ContactFormType"];
                }
                else
                {
                    ContactFormType = 1; // 0 =vhf, 1 =EI7
                }

                CaseViewModel.IDPattern = this.IDPattern;
                CaseViewModel.IDPrefixes = new List<string>();
                CaseViewModel.IsCountryUS = this.IsCountryUS; //17178
                string[] prefixes = this.IDPrefix.Split(',');
                foreach (string prefix in prefixes)
                {
                    CaseViewModel.IDPrefixes.Add(prefix.Trim());
                }

                CaseViewModel.IDSeparator = this.IDSeparator;

                switch (dt.Rows[0]["Virus"].ToString())
                {
                    case "Ebola":
                        VirusTestType = Core.Enums.VirusTestTypes.Ebola;
                        break;
                    case "Sudan":
                        VirusTestType = Core.Enums.VirusTestTypes.Sudan;
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

#if DEBUG
                swmdb.Stop();
                System.Diagnostics.Debug.Print("Meta DB Info select query and processing: " + swmdb.Elapsed.TotalMilliseconds.ToString());
#endif
                #endregion // Read metaDbInfo

                Parallel.Invoke(
                () =>
                {
                    #region Cases
#if DEBUG
                    System.Diagnostics.Stopwatch swOverallCase = new System.Diagnostics.Stopwatch();
                    swOverallCase.Start();

                    System.Diagnostics.Stopwatch swSelect = new System.Diagnostics.Stopwatch();
                    swSelect.Start();
#endif

                    if (IsMicrosoftSQLDatabase)
                    {
                        Parallel.Invoke(
                            () =>
                            {
                                caseTable = GetCasesTable(); // time-consuming
                            },
                            () =>
                            {
                                LabTable = GetLabTable(); // time-consuming
                            }
                        );
                    }
                    else
                    {
                        caseTable = GetCasesTable();
                        LabTable = GetLabTable();
                    }
#if DEBUG
                    swSelect.Stop();
                    System.Diagnostics.Debug.Print("Case SELECT query: " + swSelect.Elapsed.TotalMilliseconds.ToString());
#endif

                    #region Load Case Data (parallel)
#if DEBUG
                    System.Diagnostics.Stopwatch swLoadCase = new System.Diagnostics.Stopwatch();
                    swLoadCase.Start();
#endif
                    object syncCaseCollectionAdd = new object();

                    List<CaseViewModel> tempCaseList = new List<CaseViewModel>();

                    Parallel.ForEach(caseTable.AsEnumerable(), drow =>
                    {
                        CaseViewModel c = new CaseViewModel(CaseForm, LabForm, drow);
                        //LoadCaseData(drow, c, false, false);
                        lock (syncCaseCollectionAdd)
                        {
                            tempCaseList.Add(c);
                        }
                    });

                    /* TODO: Find a better way of handling sorting. Tried CollectionView but ran into threading issues, 
                     * which I didn't have time to resolve. The below code seems a little wasteful.
                     */

                    TaskbarProgressValue = TaskbarProgressValue + 0.05;

                    foreach (CaseViewModel caseVM in tempCaseList
                        .OrderBy(c => c.IDForSorting))
                    {
                        lock (_caseCollectionLock)
                        {
                            if (!CaseCollection.Contains(caseVM.RecordId))
                            {
                                CaseCollection.Add(caseVM);
                            }
                        }
                    }

                    LoadLabDataForCasesAsync();

                    TaskbarProgressValue = TaskbarProgressValue + 0.05;
#if DEBUG
                    swLoadCase.Stop();
                    System.Diagnostics.Debug.Print("Case processing (parallel): " + swLoadCase.Elapsed.TotalMilliseconds.ToString());

                    swOverallCase.Stop();
                    System.Diagnostics.Debug.Print("Overall case select and processing: " + swOverallCase.Elapsed.TotalMilliseconds.ToString());
#endif
                    #endregion // Load Case Data (parallel)
                    #endregion // Cases
                },
                () =>
                {
                    #region Contacts
#if DEBUG
                    System.Diagnostics.Stopwatch sw5a = new System.Diagnostics.Stopwatch();
                    sw5a.Start();
#endif
                    contactTable = GetContactsTable(); // time-consuming
#if DEBUG
                    sw5a.Stop();
                    System.Diagnostics.Debug.Print("Contact SELECT query: " + sw5a.Elapsed.TotalMilliseconds.ToString());
#endif

#if DEBUG
                    System.Diagnostics.Stopwatch sw5 = new System.Diagnostics.Stopwatch();
                    sw5.Start();
#endif
                    Parallel.ForEach(contactTable.AsEnumerable(), row =>
                    {
                        ContactViewModel c = new ContactViewModel();
                        LoadContactData(row, c, false);
                        // TODO: Check for final status and inactivate based on conditions

                        if (!String.IsNullOrEmpty(c.FinalOutcome))
                        {
                            // anything present in this field is grounds for inactivation
                            c.IsActive = false;
                        }

                        lock (_contactCollectionLock)
                        {
                            if (!ContactCollection.Contains(c.RecordId))
                            {
                                ContactCollection.Add(c);
                            }
                        }
                    });

                    TaskbarProgressValue = TaskbarProgressValue + 0.10;
#if DEBUG
                    sw5.Stop();
                    System.Diagnostics.Debug.Print("Contact processing (parallel): " + sw5.Elapsed.TotalMilliseconds.ToString());
#endif
                    #endregion // Contacts
                },
                () =>
                #region VHF Version and Admin Labels
                {
                    //try
                    //{
                    VhfDbVersion = dt.Rows[0]["VhfVersion"].ToString();

                    if (initialLoad && String.IsNullOrEmpty(VhfDbVersion))
                    {
                        System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
                        Version thisVersion = a.GetName().Version;
                        string thisVersionString = thisVersion.ToString();

                        // update database with this version
                        Query updateQuery = Database.CreateQuery("UPDATE metaDbInfo SET VhfVersion = @Version");
                        updateQuery.Parameters.Add(new QueryParameter("@Version", DbType.String, thisVersionString));
                        Database.ExecuteNonQuery(updateQuery);

                        //else
                        //{
                        //    //CheckVersioning();
                        //}
                    }

                    if (initialLoad)
                    {
                        string culture = dt.Rows[0]["Culture"].ToString();
                        string currentCulture = CaseView.Properties.Resources.Culture.ToString();

                        if (String.IsNullOrEmpty(culture))
                        {
                            Query updateQuery = Database.CreateQuery("UPDATE metaDbInfo SET Culture = @Culture");
                            updateQuery.Parameters.Add(new QueryParameter("@Culture", DbType.String, currentCulture));
                            Database.ExecuteNonQuery(updateQuery);
                        }
                        else if (!culture.Equals(currentCulture, StringComparison.OrdinalIgnoreCase))
                        {
                            Query updateQuery = Database.CreateQuery("UPDATE metaDbInfo SET Culture = @Culture");
                            updateQuery.Parameters.Add(new QueryParameter("@Culture", DbType.String, currentCulture));
                            Database.ExecuteNonQuery(updateQuery);
                            //throw new InvalidOperationException("The language of the executing assembly does not match the language value specified in the database. The database setting is " + culture + ", but the application's setting is " + currentCulture + ".");
                        }
                    }

                    string adm1Label = dt.Rows[0]["Adm1"].ToString();
                    string adm2Label = dt.Rows[0]["Adm2"].ToString();
                    string adm3Label = dt.Rows[0]["Adm3"].ToString();
                    string adm4Label = dt.Rows[0]["Adm4"].ToString();

                    if (!String.IsNullOrEmpty(adm1Label))
                    {
                        _adm1 = adm1Label;
                    }
                    else
                    {
                        RenderableField districtField = CaseForm.Fields["DistrictRes"] as RenderableField;
                        if (districtField != null)
                        {
                            _adm1 = districtField.PromptText.TrimEnd(':');
                        }
                    }

                    if (!String.IsNullOrEmpty(adm2Label))
                    {
                        _adm2 = adm2Label;
                    }
                    else
                    {
                        RenderableField scField = CaseForm.Fields["SCRes"] as RenderableField;
                        if (scField != null)
                        {
                            _adm2 = scField.PromptText.TrimEnd(':');
                        }
                    }

                    if (CaseForm.Fields.Contains("ParishRes")) //TBD
                    {
                        if (!String.IsNullOrEmpty(adm3Label))
                        {
                            _adm3 = adm3Label;
                        }
                        else
                        {
                            RenderableField parishField = CaseForm.Fields["ParishRes"] as RenderableField;
                            if (parishField != null)
                            {
                                _adm3 = parishField.PromptText.TrimEnd(':');
                            }
                        }
                    }


                    if (!String.IsNullOrEmpty(adm4Label))
                    {
                        _adm4 = adm4Label;
                    }
                    else
                    {
                        RenderableField villageField = CaseForm.Fields["VillageRes"] as RenderableField;
                        if (villageField != null)
                        {
                            _adm4 = villageField.PromptText.TrimEnd(':');
                        }
                    }

                    RaisePropertyChanged("Adm1");
                    RaisePropertyChanged("Adm1Onset");
                    RaisePropertyChanged("Adm2");
                    RaisePropertyChanged("Adm3");
                    RaisePropertyChanged("Adm4");
                },
                #endregion // VHF Version and Admin Labels
 () =>
                {
                    #region Links
#if DEBUG
                     System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                     sw.Start();
#endif
                    if (IsMicrosoftSQLDatabase)
                    {
                        CreateLinksTableForSQLServer(ref linksTable, ref historyTable);
                    }
                    else
                    {
                        CreateLinksTableForAccess(ref linksTable, ref historyTable);
                    }
#if DEBUG
                     sw.Stop();
                     System.Diagnostics.Debug.Print("Meta links SELECT query: " + sw.Elapsed.TotalMilliseconds.ToString());
#endif
                    #endregion // Links
                },
                () =>
                {
                    #region District and Sub-county & form fields clearer
                    //if (initialLoad)
                    //{
#if DEBUG
                    System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                    sw.Start();
#endif
                    // TODO: See if this can finally be removed; note that this may require updating the base templates... which for EN-US assumes Uganda, therefore you wind up with 900+ items in your district table.
                    try
                    {
                        // get rid of district / subcounty lists that came in from base templates; if the district and SC fields are text fields then this is obviously the case
                        IField districtField = CaseForm.Fields["DistrictRes"];
                        IField subCountyField = CaseForm.Fields["SCRes"];
                        if (districtField != null && districtField is SingleLineTextField && subCountyField is SingleLineTextField)
                        {
                            string star = "*";
                            if (IsMicrosoftSQLDatabase)
                            {
                                star = String.Empty;
                            }
                            string querySyntax = "DELETE " + star + " FROM [codeDistrictSubCountyList]";

                            Query deleteQuery = Database.CreateQuery(querySyntax);
                            Database.ExecuteNonQuery(deleteQuery);
                        }
                        // however, if district (and/or SC) are drop-down fields, then we can make sure our behind-the-scenes collections contain the right sets of values
                        else if (districtField != null && (districtField is DDLFieldOfLegalValues || districtField is DDLFieldOfCodes))
                        {
                            // set up district list
                            Query selectQuery = Database.CreateQuery("SELECT * FROM [codeDistrictSubCountyList] ORDER BY DISTRICT, SUBCOUNTIES");
                            DataTable districtsTable = Database.Select(selectQuery);

                            foreach (DataRow row in districtsTable.Rows)
                            {
                                lock (_locationCollectionLock)
                                {
                                    string district = row["DISTRICT"].ToString().Trim();

                                    if (!Districts.Contains(district))
                                    {
                                        Districts.Add(district);
                                    }

                                    if (subCountyField != null && (subCountyField is DDLFieldOfLegalValues || subCountyField is DDLFieldOfCodes))
                                    {
                                        string subCounty = row["SUBCOUNTIES"].ToString().Trim();
                                        //17148 - Following condition was preventing from adding the same name county in DistrictSubCounties.
                                        if (!SubCounties.Contains(subCounty))
                                        {
                                            SubCounties.Add(subCounty);
                                        }

                                        if (!DistrictsSubCounties.ContainsKey(district))
                                        {
                                            DistrictsSubCounties.Add(district, new List<string>());
                                            DistrictsSubCounties[district].Add(subCounty);
                                        }
                                        else
                                        {
                                            if (!DistrictsSubCounties[district].Contains(subCounty))
                                            {
                                                DistrictsSubCounties[district].Add(subCounty);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // do nothing
                    }

                    // Countries
                    if (!IsCountryUS && CaseForm.Fields.Contains("CountryRes")) //17178
                    {
                        IField countryField = CaseForm.Fields["CountryRes"];
                        if (countryField != null && (countryField is DDLFieldOfLegalValues || countryField is DDLFieldOfCodes || countryField is DDLFieldOfCommentLegal))
                        {
                            Query selectQuery = Database.CreateQuery("SELECT * FROM [codeCountryList] ORDER BY COUNTRY");
                            DataTable countryTable = Database.Select(selectQuery);

                            foreach (DataRow row in countryTable.Rows)
                            {
                                lock (_locationCollectionLock)
                                {
                                    string country = row["COUNTRY"].ToString().Trim();

                                    if (!Countries.Contains(country))
                                    {
                                        Countries.Add(country);
                                    }
                                }
                            }
                        }
                    }


                    // SymptOtherComments
                    if (CaseForm.Fields.Contains("SymptOtherComment"))
                    {
                        IField SymptOtherComment = CaseForm.Fields["SymptOtherComment"];
                        if (SymptOtherComment != null && (SymptOtherComment is DDLFieldOfLegalValues || SymptOtherComment is DDLFieldOfCodes ||
                            SymptOtherComment is DDLFieldOfCommentLegal))
                        {
                            Query selectQuery = Database.CreateQuery("SELECT * FROM [codesymptothercomment1] ORDER BY SymptOtherComment");
                            DataTable SymptOtherCommentTable = Database.Select(selectQuery);

                            foreach (DataRow row in SymptOtherCommentTable.Rows)
                            {
                                lock (_caseCollectionLock)
                                {
                                    string SymptOtherCom = row["SymptOtherComment"].ToString().Trim();

                                    if (!SymptOtherComments.Contains(SymptOtherCom))
                                    {
                                        SymptOtherComments.Add(SymptOtherCom);
                                    }
                                }
                            }
                        }
                    }

                    // BleedOtherComments
                    if (CaseForm.Fields.Contains("BleedOtherComment"))
                    {
                        IField BleedOtherComment = CaseForm.Fields["BleedOtherComment"];
                        if (BleedOtherComment != null && (BleedOtherComment is DDLFieldOfLegalValues || BleedOtherComment is DDLFieldOfCodes ||
                            BleedOtherComment is DDLFieldOfCommentLegal))
                        {
                            Query selectQuery = Database.CreateQuery("SELECT * FROM [codebleedothercomment1] ORDER BY BleedOtherComment");
                            DataTable BleedOtherCommentTable = Database.Select(selectQuery);

                            foreach (DataRow row in BleedOtherCommentTable.Rows)
                            {
                                lock (_caseCollectionLock)
                                {
                                    string BleedOtherCom = row["BleedOtherComment"].ToString().Trim();

                                    if (!BleedOtherComments.Contains(BleedOtherCom))
                                    {
                                        BleedOtherComments.Add(BleedOtherCom);
                                    }
                                }
                            }
                        }
                    }

                    // OtherOccupDetail
                    if (CaseForm.Fields.Contains("OtherOccupDetail"))
                    {
                        IField OtherOccupDetail = CaseForm.Fields["OtherOccupDetail"];
                        if (OtherOccupDetail != null && (OtherOccupDetail is DDLFieldOfLegalValues || OtherOccupDetail is DDLFieldOfCodes ||
                            OtherOccupDetail is DDLFieldOfCommentLegal))
                        {
                            Query selectQuery = Database.CreateQuery("SELECT * FROM [codeotheroccupdetail1] ORDER BY OtherOccupDetail");
                            DataTable otheroccupdetailTable = Database.Select(selectQuery);

                            foreach (DataRow row in otheroccupdetailTable.Rows)
                            {
                                lock (_caseCollectionLock)
                                {
                                    string OtherOccup = row["OtherOccupDetail"].ToString().Trim();

                                    if (!OtherOccupDetails.Contains(OtherOccup))
                                    {
                                        OtherOccupDetails.Add(OtherOccup);
                                    }
                                }
                            }
                        }
                    }

                    // BusinessType
                    if (CaseForm.Fields.Contains("BusinessType"))
                    {
                        IField BusinessType = CaseForm.Fields["BusinessType"];
                        if (BusinessType != null && (BusinessType is DDLFieldOfLegalValues || BusinessType is DDLFieldOfCodes ||
                            BusinessType is DDLFieldOfCommentLegal))
                        {
                            Query selectQuery = Database.CreateQuery("SELECT * FROM [codebusinesstype1] ORDER BY BusinessType");
                            DataTable BusinessTypeTable = Database.Select(selectQuery);

                            foreach (DataRow row in BusinessTypeTable.Rows)
                            {
                                lock (_caseCollectionLock)
                                {
                                    string BusinessTyp = row["BusinessType"].ToString().Trim();

                                    if (!BusinessTypes.Contains(BusinessTyp))
                                    {
                                        BusinessTypes.Add(BusinessTyp);
                                    }
                                }
                            }
                        }
                    }

                    // Hospitals Past
                    if (CaseForm.Fields.Contains("HospitalPast1"))
                    {
                        IField hospitalPast1 = CaseForm.Fields["HospitalPast1"]; // checking for one field only. As if hospitalpast1 is dropdown so will be the other. 
                        if (hospitalPast1 != null && (hospitalPast1 is DDLFieldOfLegalValues || hospitalPast1 is DDLFieldOfCodes ||
                            hospitalPast1 is DDLFieldOfCommentLegal))
                        {
                            Query selectQuery = Database.CreateQuery("SELECT * FROM [codehospitalpast1] ORDER BY HospitalPast");
                            DataTable pastHospitalTable = Database.Select(selectQuery);

                            foreach (DataRow row in pastHospitalTable.Rows)
                            {
                                lock (_caseCollectionLock)
                                {
                                    string HospitalPast1 = row["HospitalPast"].ToString().Trim();

                                    if (!HospitalsPast.Contains(HospitalPast1))
                                    {
                                        HospitalsPast.Add(HospitalPast1);
                                    }
                                }
                            }
                        }
                    }

                    // Contact Relation
                    if (CaseForm.Fields.Contains("ContactRelation1"))
                    {
                        IField ContactRelation1 = CaseForm.Fields["ContactRelation1"]; // checking for one field only. As if ContactRelation1 is dropdown so will be the others. 
                        if (ContactRelation1 != null && (ContactRelation1 is DDLFieldOfLegalValues || ContactRelation1 is DDLFieldOfCodes ||
                            ContactRelation1 is DDLFieldOfCommentLegal))
                        {
                            Query selectQuery = Database.CreateQuery("SELECT * FROM [codecontactrelation1] ORDER BY ContactRelation");
                            DataTable contactRelationTable = Database.Select(selectQuery);

                            foreach (DataRow row in contactRelationTable.Rows)
                            {
                                lock (_caseCollectionLock)
                                {
                                    string ContactRelationStr = row["ContactRelation"].ToString().Trim();

                                    if (!ContactRelations.Contains(ContactRelationStr))
                                    {
                                        ContactRelations.Add(ContactRelationStr);
                                    }
                                }
                            }
                        }
                    }

                    // Funeral Relation
                    if (CaseForm.Fields.Contains("FuneralRelation1"))
                    {
                        IField FuneralRelation1 = CaseForm.Fields["FuneralRelation1"]; // checking for one field only. As if FuneralRelation1 is dropdown so will be the others. 
                        if (FuneralRelation1 != null && (FuneralRelation1 is DDLFieldOfLegalValues || FuneralRelation1 is DDLFieldOfCodes ||
                            FuneralRelation1 is DDLFieldOfCommentLegal))
                        {
                            Query selectQuery = Database.CreateQuery("SELECT * FROM [codefuneralrelation1] ORDER BY FuneralRelation");
                            DataTable funeralRelationTable = Database.Select(selectQuery);

                            foreach (DataRow row in funeralRelationTable.Rows)
                            {
                                lock (_caseCollectionLock)
                                {
                                    string FuneralRelationStr = row["FuneralRelation"].ToString().Trim();

                                    if (!FuneralRelations.Contains(FuneralRelationStr))
                                    {
                                        FuneralRelations.Add(FuneralRelationStr);
                                    }
                                }
                            }
                        }
                    }

                    // Transport Type
                    if (CaseForm.Fields.Contains("TransporterType"))
                    {
                        IField transportType = CaseForm.Fields["TransporterType"];
                        if (transportType != null && (transportType is DDLFieldOfLegalValues || transportType is DDLFieldOfCodes || transportType is DDLFieldOfCommentLegal))
                        {
                            Query selectQuery = Database.CreateQuery("SELECT * FROM [codetransportertype1] ORDER BY TransporterType");
                            DataTable transportTable = Database.Select(selectQuery);

                            foreach (DataRow row in transportTable.Rows)
                            {
                                lock (_caseCollectionLock)
                                {
                                    string OccupationTransporterSpecify = row["TransporterType"].ToString().Trim();

                                    if (!TransportTypes.Contains(OccupationTransporterSpecify))
                                    {
                                        TransportTypes.Add(OccupationTransporterSpecify);
                                    }
                                }
                            }
                        }
                    }
                    // HcwPosition

                    if (CaseForm.Fields.Contains("hcwposition"))
                    {
                        IField hcwPositionField = CaseForm.Fields["hcwposition"];
                        if (hcwPositionField != null && (hcwPositionField is DDLFieldOfLegalValues || hcwPositionField is DDLFieldOfCodes ||
                            hcwPositionField is DDLFieldOfCommentLegal))
                        {
                            Query selectQuery = Database.CreateQuery("SELECT * FROM [codehcwPosition1] ORDER BY hcwPosition");
                            DataTable hcwPositionTable = Database.Select(selectQuery);

                            foreach (DataRow row in hcwPositionTable.Rows)
                            {
                                lock (_caseCollectionLock)
                                {
                                    string hcwposition = row["hcwposition"].ToString().Trim();

                                    if (!HcwPositions.Contains(hcwposition))
                                    {
                                        HcwPositions.Add(hcwposition);
                                    }
                                }
                            }
                        }
                    }
                    // HcwFacility
                    if (CaseForm.Fields.Contains("hcwfacility"))
                    {
                        IField hcwFacilityField = CaseForm.Fields["hcwfacility"];
                        if (hcwFacilityField != null && (hcwFacilityField is DDLFieldOfLegalValues || hcwFacilityField is DDLFieldOfCodes ||
                            hcwFacilityField is DDLFieldOfCommentLegal))
                        {
                            Query selectQuery = Database.CreateQuery("SELECT * FROM [codehcwfacility1] ORDER BY hcwfacility");
                            DataTable hcwFacilityTable = Database.Select(selectQuery);

                            foreach (DataRow row in hcwFacilityTable.Rows)
                            {
                                lock (_caseCollectionLock)
                                {
                                    string hcwfacility = row["hcwfacility"].ToString().Trim();

                                    if (!HcwFacilities.Contains(hcwfacility))
                                    {
                                        HcwFacilities.Add(hcwfacility);
                                    }
                                }
                            }
                        }
                    }

                    // CurrentHospital
                    if (CaseForm.Fields.Contains("HospitalCurrent"))
                    {
                        IField currentHospitalField = CaseForm.Fields["HospitalCurrent"];
                        if (currentHospitalField != null && (currentHospitalField is DDLFieldOfLegalValues || currentHospitalField is DDLFieldOfCodes ||
                            currentHospitalField is DDLFieldOfCommentLegal))
                        {
                            Query selectQuery = Database.CreateQuery("SELECT * FROM [codehospitalcurrent1] ORDER BY hospitalcurrent");
                            DataTable currenthospitalTable = Database.Select(selectQuery);

                            foreach (DataRow row in currenthospitalTable.Rows)
                            {
                                lock (_caseCollectionLock)
                                {
                                    string currenthospital = row["hospitalcurrent"].ToString().Trim();

                                    if (!CurrentHospitals.Contains(currenthospital))
                                    {
                                        CurrentHospitals.Add(currenthospital);
                                    }
                                }
                            }
                        }
                    }

                    // Interviewer Position
                    if (CaseForm.Fields.Contains("InterviewerPosition"))
                    {
                        IField interviewerPositionField = CaseForm.Fields["InterviewerPosition"];
                        if (interviewerPositionField != null && (interviewerPositionField is DDLFieldOfLegalValues || interviewerPositionField is DDLFieldOfCodes ||
                            interviewerPositionField is DDLFieldOfCommentLegal))
                        {
                            Query selectQuery = Database.CreateQuery("SELECT * FROM [codeinterviewerposition1] ORDER BY InterviewerPosition");
                            DataTable InterviewerPositionTable = Database.Select(selectQuery);

                            foreach (DataRow row in InterviewerPositionTable.Rows)
                            {
                                lock (_caseCollectionLock)
                                {
                                    string interviewPosition = row["InterviewerPosition"].ToString().Trim();

                                    if (!InterviewerPositions.Contains(interviewPosition))
                                    {
                                        InterviewerPositions.Add(interviewPosition);
                                    }
                                }
                            }
                        }
                    }

                    // Interviewer Health Facility
                    if (CaseForm.Fields.Contains("InterviewerHealthFacility"))
                    {
                        IField interviewerHealthFacilityField = CaseForm.Fields["InterviewerHealthFacility"];
                        if (interviewerHealthFacilityField != null && (interviewerHealthFacilityField is DDLFieldOfLegalValues ||
                            interviewerHealthFacilityField is DDLFieldOfCodes || interviewerHealthFacilityField is DDLFieldOfCommentLegal))
                        {
                            Query selectQuery = Database.CreateQuery("SELECT * FROM [codeinterviewerhealthfacility1] ORDER BY InterviewerHealthFacility");
                            DataTable InterviewerHealthFacilityTable = Database.Select(selectQuery);

                            foreach (DataRow row in InterviewerHealthFacilityTable.Rows)
                            {
                                lock (_caseCollectionLock)
                                {
                                    string interviewerHealthFacility = row["InterviewerHealthFacility"].ToString().Trim();

                                    if (!InterviewerHealthFacilities.Contains(interviewerHealthFacility))
                                    {
                                        InterviewerHealthFacilities.Add(interviewerHealthFacility);
                                    }
                                }
                            }
                        }
                    }

                    // Proxy Relation
                    if (CaseForm.Fields.Contains("ProxyRelation"))
                    {
                        IField proxyRelationField = CaseForm.Fields["ProxyRelation"];
                        if (proxyRelationField != null && (proxyRelationField is DDLFieldOfLegalValues || proxyRelationField is DDLFieldOfCodes ||
                            proxyRelationField is DDLFieldOfCommentLegal))
                        {
                            Query selectQuery = Database.CreateQuery("SELECT * FROM [codeproxyrelation1] ORDER BY ProxyRelation");
                            DataTable ProxyRelationTable = Database.Select(selectQuery);

                            foreach (DataRow row in ProxyRelationTable.Rows)
                            {
                                lock (_caseCollectionLock)
                                {
                                    string proxyRelation = row["ProxyRelation"].ToString().Trim();

                                    if (!ProxyRelations.Contains(proxyRelation))
                                    {
                                        ProxyRelations.Add(proxyRelation);
                                    }
                                }
                            }
                        }
                    }

                    // Hospital Discharge
                    if (CaseForm.Fields.Contains("HospitalDischarge"))
                    {
                        IField hospitalDischargeField = CaseForm.Fields["HospitalDischarge"];
                        if (hospitalDischargeField != null && (hospitalDischargeField is DDLFieldOfLegalValues || hospitalDischargeField is DDLFieldOfCodes ||
                            hospitalDischargeField is DDLFieldOfCommentLegal))
                        {
                            Query selectQuery = Database.CreateQuery("SELECT * FROM [codehospitaldischarge1] ORDER BY HospitalDischarge");
                            DataTable hospitalDischargeTable = Database.Select(selectQuery);

                            foreach (DataRow row in hospitalDischargeTable.Rows)
                            {
                                lock (_caseCollectionLock)
                                {
                                    string hospitalDischarge = row["HospitalDischarge"].ToString().Trim();

                                    if (!HospitalDischarges.Contains(hospitalDischarge))
                                    {
                                        HospitalDischarges.Add(hospitalDischarge);
                                    }
                                }
                            }
                        }
                    }

                    // OtherHemoFinalSpecify
                    //if (CaseForm.Fields.Contains("OtherHemoFinalSpecify"))
                    //{
                    //    IField OtherHemoFinalSpecify = CaseForm.Fields["OtherHemoFinalSpecify"];
                    //    if (OtherHemoFinalSpecify != null && (OtherHemoFinalSpecify is DDLFieldOfLegalValues || OtherHemoFinalSpecify is DDLFieldOfCodes || OtherHemoFinalSpecify is DDLFieldOfCommentLegal))
                    //    {
                    //        Query selectQuery = Database.CreateQuery("SELECT * FROM [codeotherhemofinalspecify1] ORDER BY OtherHemoFinalSpecify");
                    //        DataTable OtherHemoFinalSpecifyTable = Database.Select(selectQuery);

                    //        foreach (DataRow row in OtherHemoFinalSpecifyTable.Rows)
                    //        {
                    //            lock (_caseCollectionLock)
                    //            {
                    //                string otherHemoFinalSpecify = row["OtherHemoFinalSpecify"].ToString().Trim();

                    //                if (!OtherHemoFinalSpecifyCollection.Contains(otherHemoFinalSpecify))
                    //                {
                    //                    OtherHemoFinalSpecifyCollection.Add(otherHemoFinalSpecify);
                    //                }
                    //            }
                    //        }
                    //    }
                    //}

                    // ContactHCWFacilities 
                    if (ContactForm.Fields.Contains("ContactHCWFacility"))
                    {
                        IField ContactHCWFacility = ContactForm.Fields["ContactHCWFacility"];
                        if (ContactHCWFacility != null && (ContactHCWFacility is DDLFieldOfLegalValues || ContactHCWFacility is DDLFieldOfCodes || ContactHCWFacility is DDLFieldOfCommentLegal))
                        {
                            Query selectQuery = Database.CreateQuery("SELECT * FROM [codecontacthcwfacility1] ORDER BY ContactHCWFacility");
                            DataTable ContactHCWFacilityTable = Database.Select(selectQuery);

                            foreach (DataRow row in ContactHCWFacilityTable.Rows)
                            {
                                lock (_contactCollectionLock)
                                {
                                    string contactHCWFacility = row["ContactHCWFacility"].ToString().Trim();

                                    if (!ContactHCWFacilities.Contains(contactHCWFacility))
                                    {
                                        ContactHCWFacilities.Add(contactHCWFacility);
                                    }
                                }
                            }
                        }
                    }

                    // Team
                    if (ContactForm.Fields.Contains("Team"))
                    {
                        IField Team = ContactForm.Fields["Team"];
                        if (Team != null && (Team is DDLFieldOfLegalValues || Team is DDLFieldOfCodes || Team is DDLFieldOfCommentLegal))
                        {
                            Query selectQuery = Database.CreateQuery("SELECT * FROM [codeteam1] ORDER BY Team");
                            DataTable TeamTable = Database.Select(selectQuery);

                            foreach (DataRow row in TeamTable.Rows)
                            {
                                lock (_contactCollectionLock)
                                {
                                    string team = row["Team"].ToString().Trim();

                                    if (!ContactTeamCollection.Contains(team))
                                    {
                                        ContactTeamCollection.Add(team);
                                    }
                                }
                            }
                        }
                    }
#if DEBUG
                    sw.Stop();
                    System.Diagnostics.Debug.Print("District filler query: " + sw.Elapsed.TotalMilliseconds.ToString());
#endif

                    //Query teamQuery = Database.CreateQuery("SELECT * FROM [codeCountryList] ORDER BY COUNTRY");
                    //DataTable teamTable = Database.Select(teamQuery);

                    //foreach (DataRow row in teamTable.Rows)
                    //{
                    //    lock (_contactCollectionLock)
                    //    {
                    //        string team = row["COUNTRY"].ToString().Trim();

                    //        if (!ContactTeams.Contains(team))
                    //        {
                    //            ContactTeams.Add(team);
                    //        }
                    //    }
                    //}




                    //}
                    #endregion // District and Sub-county clearer
                }
                );

                #endregion // Parallel Load

                TaskbarProgressValue = TaskbarProgressValue + 0.2;
#if DEBUG
                System.Diagnostics.Stopwatch sw8 = new System.Diagnostics.Stopwatch();
                sw8.Start();
#endif
                SetCaseAndContactFlags();
#if DEBUG
                sw8.Stop();
                System.Diagnostics.Debug.Print("Set IsCase and IsContact flags: " + sw8.Elapsed.TotalMilliseconds.ToString());
#endif

                #region Sequential Contact Link Load / DailyFollowUp Check
                LoadStatus = "Processing daily follow-up data...";
#if DEBUG
                System.Diagnostics.Stopwatch sw4a = new System.Diagnostics.Stopwatch();
                sw4a.Start();
#endif
                LoadContactsLinkData(linksTable);
                //TaskbarProgressValue = TaskbarProgressValue + 0.2;
#if DEBUG
                sw4a.Stop();
                System.Diagnostics.Debug.Print("Contact Link Load: " + sw4a.Elapsed.TotalMilliseconds.ToString());
#endif

                #endregion // Sequential Contact Link Load / DailyFollowUp Check

#if DEBUG
                System.Diagnostics.Stopwatch sw6 = new System.Diagnostics.Stopwatch();
                sw6.Start();
#endif
                SortFollowUps(DailyFollowUpCollection);
                TaskbarProgressValue = TaskbarProgressValue + 0.1;
#if DEBUG
                sw6.Stop();
                System.Diagnostics.Debug.Print("Sort daily follow ups: " + sw6.Elapsed.TotalMilliseconds.ToString());
#endif

#if DEBUG
                System.Diagnostics.Stopwatch sw9 = new System.Diagnostics.Stopwatch();
                sw9.Start();
#endif
                LoadStatus = "Checking contact final statuses...";
                CheckAndSetContactFinalStatusesOnInitialLoad();
                //TaskbarProgressValue = TaskbarProgressValue + 0.1;
#if DEBUG
                sw9.Stop();
                System.Diagnostics.Debug.Print("SetContactFinalStatus: " + sw9.Elapsed.TotalMilliseconds.ToString());

                if (initialLoad)
                {
                    DeleteCaselessContacts(false);
                }
#endif
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                ErrorMessage = String.Format(Properties.Resources.ErrorInitialLoadSqlException, initialLoad.ToString(), System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(), Database.FullName);
                ErrorMessageDetail = ex.Message + "\n\n" + ex.StackTrace;
                IsShowingError = true;
                DbLogger.Log("SqlException in PopulateCollections. Message: " + ex.Message);
                return false;
            }
            catch (AggregateException ex)
            {
                ErrorMessage = String.Format(Properties.Resources.ErrorInitialLoadExceptions, initialLoad.ToString(), System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
                int errorIndex = 1;
                foreach (Exception innerEx in ex.InnerExceptions)
                {
                    if (ex.InnerExceptions.Count > 1)
                    {
                        ErrorMessageDetail = ErrorMessageDetail + errorIndex + ": " + innerEx.Message + "\n\n";
                        DbLogger.Log("Exception in PopulateCollections (AggregateException). Message: " + innerEx.Message);
                    }
                    else
                    {
                        ErrorMessageDetail = ErrorMessageDetail + innerEx.Message + "\n\n" + ex.StackTrace;
                        DbLogger.Log("Exception in PopulateCollections (AggregateException). Message: " + innerEx.Message);
                        DbLogger.Log("Exception in PopulateCollections (AggregateException). StackTrace: " + innerEx.StackTrace);
                    }
                }

                ErrorMessageDetail = ErrorMessageDetail.TrimEnd('\n');
                IsShowingError = true;
                
                return false;
            }
            catch (Exception ex)
            {
                ErrorMessage = String.Format(Properties.Resources.ErrorInitialLoadException, initialLoad.ToString(), System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
                ErrorMessageDetail = ex.Message + "\n\n" + ex.StackTrace;
                IsShowingError = true;
                DbLogger.Log("Exception in PopulateCollections. Message: " + ex.Message);
                return false;
            }
            finally
            {
#if DEBUG
                swMain.Stop();
                System.Diagnostics.Debug.Print("PopulateCollections END: " + swMain.Elapsed.TotalMilliseconds.ToString());
#endif
            }

            LoadStatus = "Finished.";

            //TaskbarProgressValue = TaskbarProgressValue + 0.1;

            return true;
        }

        private void CreateLinksTable(ref DataTable linksTable, ref DataTable historyTable)
        {
            if (linksTable.Columns.Contains("NotACase"))
            {
                linksTable.Columns.Remove("NotACase");
            }

            linksTable.Columns.Add(new DataColumn("Temp1_1", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp1_2", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp1_3", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp1_4", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp1_5", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp1_6", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp1_7", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp1_8", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp1_9", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp1_10", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp1_11", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp1_12", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp1_13", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp1_14", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp1_15", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp1_16", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp1_17", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp1_18", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp1_19", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp1_20", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp1_21", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp2_1", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp2_2", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp2_3", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp2_4", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp2_5", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp2_6", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp2_7", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp2_8", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp2_9", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp2_10", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp2_11", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp2_12", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp2_13", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp2_14", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp2_15", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp2_16", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp2_17", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp2_18", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp2_19", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp2_20", typeof(double)));
            linksTable.Columns.Add(new DataColumn("Temp2_21", typeof(double)));

            // Test whether LinkId is the first column and adjust if not
            int minuser = 0;
            if (!linksTable.Columns[0].ColumnName.ToLower().Equals("linkid"))
            {
                minuser = 1;
            }
            //                    linksTable.Rows[0][14] = 2;

            Query selectMetaHistory = Database.CreateQuery("SELECT * FROM [metaHistory] ORDER BY [ContactGUID] ASC, [FollowUpDate] ASC");
            historyTable = Database.Select(selectMetaHistory);

            double count = linksTable.Rows.Count;
            double completed = 0;

            for (int i = 0; i < linksTable.Rows.Count; i++)
            {
                DataRow linksRow = linksTable.Rows[i];
                string id = linksRow[2 - minuser].ToString();
                DateTime contactDate = (DateTime)linksRow[5 - minuser];
                DataRow[] historyRows = historyTable.Select("ContactGUID = '" + id + "'");
                for (int j = 0; j < historyRows.Length; j++)
                {
                    DateTime historyDate = (DateTime)historyRows[j][1];
                    int diff = historyDate.Subtract(contactDate).Days;
                    if (diff > 21)
                        break;
                    if (diff > 0)
                    {
                        string notes = historyRows[j][3].ToString();
                        if (historyRows[j][2] != DBNull.Value)
                        {
                            try
                            {
                                linksRow[9 - minuser + diff] = Convert.ToInt32(historyRows[j][2]);
                            }
                            catch
                            {
                            }
                        }
                        linksRow[30 - minuser + diff] = notes;
                        if (historyRows[j][4] != DBNull.Value)
                        {
                            try
                            {
                                linksRow[51 + diff] = Convert.ToDouble(historyRows[j][4]);
                            }
                            catch
                            {
                            }
                        }
                        if (historyRows[j][5] != DBNull.Value)
                        {
                            try
                            {
                                linksRow[72 + diff] = Convert.ToDouble(historyRows[j][5]);
                            }
                            catch
                            {
                            }
                        }
                    }
                }

                completed++;
                TaskbarProgressValue = TaskbarProgressValue + ((completed / count) * 0.1) - ((--completed / count) * 0.1);
            }
            this.metaLinksDataTable = linksTable;
        }

        private void CreateLinksTableForAccess(ref DataTable linksTable, ref DataTable historyTable)
        {
            // This was the original query, which runs fine on small databases but fails badly on anything of significant size.
            //Query selectQuery = Database.CreateQuery("SELECT * FROM (SELECT *, (FromRecordGuid in (SELECT GlobalRecordId from CaseInformationForm1 WHERE EpiCaseDef = '0')) as NotACase FROM [metaLinks]) ORDER BY [ToRecordGuid] ASC, NotACase DESC,  [LastContactDate] DESC");
            //linksTable = Database.Select(selectQuery);

            // This is the new operation that adds the non-cases and re-sorts by the non-case column to achieve 
            // the same result, but doing so partially in-memory to eliminate the massive perf hit from using the
            // original MS Access query
            Query selectQuery = Database.CreateQuery("SELECT * FROM [metaLinks] WHERE ToViewId = @ToViewId ORDER BY [ToRecordGuid] ASC, [LastContactDate] DESC");
            selectQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, ContactFormId));
            linksTable = Database.Select(selectQuery);

            linksTable.Columns.Add("NotACase", typeof(int));

            Query selectNonCaseQuery = Database.CreateQuery("SELECT GlobalRecordId FROM CaseInformationForm1 WHERE EpiCaseDef = '0'");
            DataTable nonCasesTable = Database.Select(selectNonCaseQuery); // can't use CaseCollection since it may not have finished populating at this point in the order of execution

            foreach (DataRow iRow in nonCasesTable.Rows)
            {
                string guid = iRow["GlobalRecordId"].ToString();

                foreach (DataRow jRow in linksTable.Rows)
                {
                    string jCaseGuid = jRow["FromRecordGuid"].ToString();

                    if (jCaseGuid.Equals(guid, StringComparison.OrdinalIgnoreCase))
                    {
                        jRow["NotACase"] = 1;
                    }
                }
            }

            if (linksTable.Rows.Count > 0)
            {
                linksTable = linksTable.Select(String.Empty, "[ToRecordGuid] ASC, NotACase ASC, [LastContactDate] DESC").CopyToDataTable(); // TODO: Must be way to improve this
            }

            CreateLinksTable(ref linksTable, ref historyTable);
        }

        private void CreateLinksTableForSQLServer(ref DataTable linksTable, ref DataTable historyTable)
        {
            Query selectQuerySQL = Database.CreateQuery("SELECT *, CASE WHEN FromRecordGuid in (SELECT GlobalRecordId from CaseInformationForm1 WHERE EpiCaseDef = '0') THEN 1 ELSE 0 END as NotACase FROM [metaLinks] ORDER BY [ToRecordGuid] ASC, NotACase ASC, [LastContactDate] DESC");
            linksTable = Database.Select(selectQuerySQL);
            CreateLinksTable(ref linksTable, ref historyTable);
        }
    }
}
