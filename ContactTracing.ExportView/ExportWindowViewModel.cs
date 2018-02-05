using System;
using System.Collections.Generic;
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
using Epi.ImportExport.ProjectPackagers;

namespace ContactTracing.ExportView
{
    public sealed class ExportWindowViewModel : ObservableObject
    {
        #region Members
        private System.Windows.Shell.TaskbarItemProgressState _taskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
        private double _taskbarProgressValue = 0.0;
        private double _progressValue = 0.0;
        private string _syncStatus = String.Empty;
        private string _overallSyncStatus = String.Empty;
        private Project _project;
        private bool _isDataSyncing = false;
        private bool _isShowingExportProgress = false;
        private string _syncFilePath = String.Empty;
        private bool _includeCases = true;
        private bool _includeCasesAndContacts = true;
        private bool _deIdentifyData = false;
        private string _projectFilePath = String.Empty;
        private string _recordScope = "Active";
        private string _recordsExported = String.Empty;
        private double _increment = 0.0;
        private string _days = "18250";
        #endregion // Members

        #region Events
        public event EventHandler SyncProblemsDetected;
        #endregion // Events

        #region Properties


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

        public string ProjectFilePath
        {
            get
            {
                return _projectFilePath;
            }
            set
            {
                _projectFilePath = value;
                RaisePropertyChanged("ProjectFilePath");

                _project = new Project(_projectFilePath);

                CaseForm = _project.Views["CaseInformationForm"];
                LabForm = _project.Views["LaboratoryResultsForm"];
                ContactForm = _project.Views["ContactEntryForm"];

                CaseFormId = CaseForm.Id;
                LabFormId = LabForm.Id;
                ContactFormId = ContactForm.Id;
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
            protected internal set
            {
                if (_isDataSyncing != value)
                {
                    _isDataSyncing = value;
                    RaisePropertyChanged("IsDataSyncing");
                }
            }
        }

        public string OverallSyncStatus
        {
            get
            {
                return this._overallSyncStatus;
            }
            set
            {
                this._overallSyncStatus = value;
                RaisePropertyChanged("OverallSyncStatus");
            }

        }
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

        /// <summary>
        /// Gets/sets the current progress value for the progress bar that shows up in the export panel
        /// </summary>
        public double ProgressValue
        {
            get
            {
                return this._progressValue;
            }
            set
            {
                if (this.ProgressValue != value)
                {
                    this._progressValue = value;
                    RaisePropertyChanged("ProgressValue");
                }
            }
        }

        /// <summary>
        /// Gets/sets the current progress value for the progress bar that shows up in the app's taskbar icon.
        /// </summary>
        public double TaskbarProgressValue
        {
            get
            {
                return this._taskbarProgressValue;
            }
            set
            {
                if (this.TaskbarProgressValue != value)
                {
                    this._taskbarProgressValue = value;
                    RaisePropertyChanged("TaskbarProgressValue");
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
        #endregion // Properties

        #region Constructors

        public ExportWindowViewModel(string projectPath)
            : base()
        {
            LoadConfig();
            IncludeCasesAndContacts = true;
            IncludeCasesOnly = false;

            SetProject(projectPath);
        }

        public ExportWindowViewModel()
            : base()
        {
            LoadConfig();
            IncludeCasesAndContacts = true;
            IncludeCasesOnly = false;
        }
        #endregion // Constructors

        #region Methods

        public void SetProject(string projectFilePath)
        {
            ProjectFilePath = projectFilePath;
        }

        void unpackager_UpdateProgress(double progress)
        {
            //TaskbarProgressValue = (progress / 100);
            ProgressValue = progress / 100;
            TaskbarProgressValue = progress / 100 * _increment;
        }

        void unpackager_StatusChanged(string message)
        {
            SyncStatus = message;
        }

        private bool LoadConfig()
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

                if (!File.Exists(configFilePath))
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

        private XmlDocument CreateCaseSyncFile(bool includeCases, bool includeCaseExposures, bool includeContacts, Epi.ImportExport.Filters.RowFilters filters, bool deIdentifyData, Epi.RecordProcessingScope recordProcessingScope)
        {
            TaskbarProgressValue = 0;
            TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
            ProgressValue = 0;

            _increment = 0.25;

            if (includeCaseExposures && includeContacts)
            {
                _increment = 0.25;
            }
            else if (includeCaseExposures && !includeContacts)
            {
                _increment = 0.34;
            }
            else if (!includeCaseExposures && !includeContacts)
            {
                _increment = 0.5;
            }

            IDbDriver database = _project.CollectedData.GetDatabase();
            //#region Repair page tables
            //RemoveExtraneousPageTableRecordsCommand.Execute(null);
            //#endregion // Repair page tables

            #region Case and Lab Data
            //var packager = new ContactTracing.ExportView.XmlSqlDataPackager(CaseForm, "sync") //new Epi.ImportExport.ProjectPackagers.XmlDataPackager(CaseForm, "sync")
            var packager = new Epi.ImportExport.ProjectPackagers.XmlDataPackager(CaseForm, "sync")
            {
                RecordProcessingScope = recordProcessingScope
            };

            packager.StatusChanged += unpackager_StatusChanged;
            packager.UpdateProgress += unpackager_UpdateProgress;

            if (filters == null)
            {
                filters = new Epi.ImportExport.Filters.RowFilters(database, Epi.ImportExport.Filters.ConditionJoinTypes.And);
            }

            if (includeCases == false)
            {
                // filter out all cases
                var tfc = new Epi.ImportExport.TextRowFilterCondition("[EpiCaseDef] = @EpiCaseDef", "EpiCaseDef", "@EpiCaseDef", "1000")
                {
                    Description = "EpiCaseDef is equal to 1000"
                };
                filters.Add(tfc);
            }

            DateTime dateValue = DateTime.MinValue;

            DateTime today = DateTime.Now;
            TimeSpan ts = new TimeSpan(int.Parse(Days), 0, 0, 0);
            DateTime nDaysAgo = today - ts;
            dateValue = nDaysAgo;

            var daysAgoFilter = new Epi.ImportExport.DateRowFilterCondition("LastSaveTime >= @LastSaveTime", "LastSaveTime", "@LastSaveTime", dateValue);
            filters.Add(daysAgoFilter);

            packager.Filters = new Dictionary<string, Epi.ImportExport.Filters.RowFilters>
            {
                {"CaseInformationForm", filters}
            };

            if (deIdentifyData)
            {
                if (!IsCountryUS)
                {
                    packager.FieldsToNull.Add(CaseForm.Name, new List<string> { "Surname", "OtherNames", "PhoneNumber", "PhoneOwner", "HeadHouse", "ContactName1", "ContactName2", "ContactName3", "FuneralName1", "FuneralName2", "HospitalBeforeIllPatient", "TradHealerName", "InterviewerName", "InterviewerPhone", "InterviwerEmail", "ProxyName" });
                    packager.FieldsToNull.Add(LabForm.Name, new List<string> { "SurnameLab", "OtherNameLab" });
                }
                else
                {
                    packager.FieldsToNull.Add(CaseForm.Name, new List<string> { "Surname", "OtherNames", "PhoneNumber", "PhoneOwner", "HeadHouse", "ContactName1", "ContactName2", "ContactName3", "FuneralName1", "FuneralName2", "HospitalBeforeIllPatient", "TradHealerName", "InterviewerName", "InterviewerPhone", "InterviwerEmail", "ProxyName", "DOB", "Email", "AddressRes", "AddressOnset", "ProxyPhone", "ProxyEmail" });
                    packager.FieldsToNull.Add(LabForm.Name, new List<string> { "SurnameLab", "OtherNameLab", "PersonLabSubmit", "PhoneLabSubmit", "EmailLabSubmit" });
                }
            }
            packager.IncludeNullFieldData = false;

            var doc = new XmlDocument { XmlResolver = null };

            bool failed = false;

            try
            {
                OverallSyncStatus = "Packaging case records...";
                doc = packager.PackageForm();
                TaskbarProgressValue = TaskbarProgressValue + _increment;
                OverallSyncStatus = "Finished packaging case records";

                if (packager.ExportInfo.RecordsPackaged.ContainsKey(LabForm))
                {
                    RecordsExported = "Exported: " + RecordsExported + packager.ExportInfo.RecordsPackaged[CaseForm].ToString() + " cases, " + packager.ExportInfo.RecordsPackaged[LabForm].ToString() + " lab results";
                }
                else
                {
                    RecordsExported = "Exported: " + RecordsExported + packager.ExportInfo.TotalRecordsPackaged.ToString() + " cases";
                }
            }
            catch (Exception ex)
            {
                if (SyncProblemsDetected != null)
                {
                    SyncProblemsDetected(ex, new EventArgs());
                }
                failed = true;
            }
            finally
            {
                packager.StatusChanged -= unpackager_StatusChanged;
                packager.UpdateProgress -= unpackager_UpdateProgress;
            }

            if (failed)
            {
                return doc;
            }
            #endregion // Case and Lab Data

            #region Contact Data
            if (includeContacts)
            {
                OverallSyncStatus = "Packaging contact records...";
                //packager = new ContactTracing.ExportView.XmlSqlDataPackager(ContactForm, "sync") //new Epi.ImportExport.ProjectPackagers.XmlSqlDataPackager(ContactForm, "sync");
                packager = new Epi.ImportExport.ProjectPackagers.XmlDataPackager(ContactForm, "sync")
                {
                    RecordProcessingScope = recordProcessingScope
                };

                packager.StatusChanged += unpackager_StatusChanged;
                packager.UpdateProgress += unpackager_UpdateProgress;

                packager.RecordProcessingScope = recordProcessingScope;

                filters = new Epi.ImportExport.Filters.RowFilters(database, Epi.ImportExport.Filters.ConditionJoinTypes.And);
                daysAgoFilter = new Epi.ImportExport.DateRowFilterCondition("LastSaveTime >= @LastSaveTime", "LastSaveTime", "@LastSaveTime", dateValue);
                filters.Add(daysAgoFilter);

                packager.Filters = new Dictionary<string, Epi.ImportExport.Filters.RowFilters>
                {
                    {ContactForm.Name, filters}
                };

                if (deIdentifyData)
                {
                    if (!IsCountryUS)
                        packager.FieldsToNull.Add(ContactForm.Name, new List<string> { "ContactSurname", "ContactOtherNames", "ContactHeadHouse", "ContactPhone", "LC1" });
                    else
                        packager.FieldsToNull.Add(ContactForm.Name, new List<string> { "ContactSurname", "ContactOtherNames", "ContactHeadHouse", "ContactPhone", "LC1", "ContactDOB", "ContactAddress", "ContactEmail" });
                }

                try
                {
                    XmlDocument contactDoc = packager.PackageForm();
                    RecordsExported = RecordsExported + ", " + packager.ExportInfo.TotalRecordsPackaged.ToString() + " contacts";
                    XmlNodeList xnList = contactDoc.SelectNodes("/DataPackage/Form");
                    if (IsCountryUS)
                    {
                        foreach (XmlNode node in contactDoc.GetElementsByTagName("FieldInfo"))
                        {
                            if (node.Attributes[0].Value == "AdminOverride")
                            {
                                node.ParentNode.RemoveChild(node);
                                break;
                            }
                        }
                    }

                    if (xnList.Count == 1)
                    {
                        XmlNode nodeToCopy = doc.ImportNode(contactDoc.SelectSingleNode("/DataPackage/Form"), true); // note: target
                        XmlNode parentNode = doc.SelectSingleNode("/DataPackage");
                        parentNode.AppendChild(nodeToCopy);

                        //doc.Save(@"C:\Temp\ContactTest.xml");
                    }
                }
                catch (Exception ex)
                {
                    //if (SyncProblemsDetected != null)
                    //{
                    //    SyncProblemsDetected(ex, new EventArgs());
                    //}
                    // TODO: Re-work this
                }
                finally
                {
                    packager.StatusChanged -= unpackager_StatusChanged;
                    packager.UpdateProgress -= unpackager_UpdateProgress;
                }
            }
            TaskbarProgressValue = TaskbarProgressValue + _increment;
            OverallSyncStatus = "Finished packaging contact records";
            #endregion // Contact Data

            #region Link Data

            if (includeCaseExposures || includeContacts)
            {
                OverallSyncStatus = "Packaging relationship records...";
                #region metaLinks table
                XmlElement links = doc.CreateElement("Links");

                Query selectQuery = database.CreateQuery("SELECT * FROM [metaLinks] ORDER BY [LastContactDate] DESC");
                DataTable linksTable = database.Select(selectQuery);

                foreach (DataRow row in linksTable.Rows)
                {
                    XmlElement link = doc.CreateElement("Link");

                    var toViewId = (int)row["ToViewId"];
                    var fromViewId = (int)row["FromViewId"];

                    if (includeCaseExposures && toViewId == CaseFormId && fromViewId == CaseFormId)
                    {
                        // we have a case-to-case link, add it

                        foreach (DataColumn dc in linksTable.Columns)
                        {
                            XmlElement element = doc.CreateElement(dc.ColumnName);
                            if (row[dc] != DBNull.Value)
                            {
                                if (row[dc] is DateTime || dc.ColumnName.Equals("LastContactDate", StringComparison.OrdinalIgnoreCase))
                                {
                                    var dt = (DateTime)row[dc];
                                    element.InnerText = dt.Ticks.ToString();
                                }
                                else
                                {
                                    element.InnerText = row[dc].ToString();
                                }
                            }
                            else
                            {
                                element.InnerText = String.Empty;
                            }

                            //if (!String.IsNullOrEmpty(element.InnerText) || !element.Name.StartsWith("Day", StringComparison.OrdinalIgnoreCase))
                            //{
                            link.AppendChild(element);
                            //}
                        }
                    }

                    if (includeContacts && toViewId == ContactFormId && fromViewId == CaseFormId)
                    {
                        // we have a case-to-contact link, add it
                        foreach (DataColumn dc in linksTable.Columns)
                        {
                            XmlElement element = doc.CreateElement(dc.ColumnName);
                            if (row[dc] != DBNull.Value)
                            {
                                if (row[dc] is DateTime || dc.ColumnName.Equals("LastContactDate", StringComparison.OrdinalIgnoreCase))
                                {
                                    var dt = (DateTime)row[dc];
                                    element.InnerText = dt.Ticks.ToString();
                                }
                                else
                                {
                                    element.InnerText = row[dc].ToString();
                                }
                            }
                            else
                            {
                                element.InnerText = String.Empty;
                            }
                            //if (!String.IsNullOrEmpty(element.InnerText) || !element.Name.StartsWith("Day", StringComparison.OrdinalIgnoreCase))
                            //{
                            link.AppendChild(element);
                            //}
                        }
                    }

                    links.AppendChild(link);
                }

                doc.ChildNodes[0].AppendChild(links);
                #endregion // metaLinks table
                TaskbarProgressValue = TaskbarProgressValue + _increment;
                RecordsExported = RecordsExported + ", " + linksTable.Rows.Count.ToString() + " relationships";

                if (includeContacts)
                {
                    if (database.TableExists("metaHistory"))
                    {
                        OverallSyncStatus = "Packaging daily follow-up records...";
                        #region metaHistory table
                        XmlElement followUps = doc.CreateElement("ContactFollowUps");

                        selectQuery = database.CreateQuery("SELECT * FROM [metaHistory] ORDER BY [ContactGUID] DESC, [FollowUpDate] DESC");
                        DataTable followUpsTable = database.Select(selectQuery);

                        foreach (DataRow row in followUpsTable.Rows)
                        {
                            XmlElement followUp = doc.CreateElement("ContactFollowUp");

                            XmlElement guid = doc.CreateElement("ContactGUID");
                            guid.InnerText = row["ContactGUID"].ToString();
                            followUp.AppendChild(guid);

                            CultureInfo format = CultureInfo.InvariantCulture;

                            XmlElement fuDate = doc.CreateElement("FollowUpDate");
                            fuDate.InnerText = Convert.ToDateTime(row["FollowUpDate"]).ToString(format.DateTimeFormat.ShortDatePattern);
                            followUp.AppendChild(fuDate);

                            XmlElement statusOnDate = doc.CreateElement("StatusOnDate");
                            statusOnDate.InnerText = row["StatusOnDate"].ToString();
                            followUp.AppendChild(statusOnDate);

                            XmlElement note = doc.CreateElement("Note");
                            note.InnerText = row["Note"].ToString();
                            followUp.AppendChild(note);

                            if (row.Table.Columns.Contains("Temp1"))
                            {
                                XmlElement temp1 = doc.CreateElement("Temp1");
                                if (row["Temp1"] != DBNull.Value)
                                {
                                    temp1.InnerText = Convert.ToDouble(row["Temp1"]).ToString(System.Globalization.CultureInfo.InvariantCulture);
                                }
                                followUp.AppendChild(temp1);

                                XmlElement temp2 = doc.CreateElement("Temp2");
                                if (row["Temp2"] != DBNull.Value)
                                {
                                    temp2.InnerText = Convert.ToDouble(row["Temp2"]).ToString(System.Globalization.CultureInfo.InvariantCulture);
                                }
                                followUp.AppendChild(temp2);
                            }

                            followUps.AppendChild(followUp);
                        }
                        #endregion // metaHistory table

                        doc.ChildNodes[0].AppendChild(followUps);
                        TaskbarProgressValue = TaskbarProgressValue + _increment;
                        RecordsExported = RecordsExported + ", " + followUpsTable.Rows.Count.ToString() + " follow-ups";
                    }
                }
            }
            #endregion // Link Data

            return doc;
        }

        private void SendMessageForAwaitAll()
        {
            // do nothing for now
        }

        private void SendMessageForUnAwaitAll()
        {
            // do nothing for now
        }

        public void CreateCaseSyncFileStart(Epi.ImportExport.Filters.RowFilters filters, Epi.RecordProcessingScope recordProcessingScope)
        {
            if (IsWaitingOnOtherClients)
            {
                return;
            }

            if (String.IsNullOrEmpty(SyncFilePath.Trim()))
            {
                throw new InvalidOperationException();
            }

            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            bool includeCases = IncludeCasesAndContacts || IncludeCasesOnly;
            bool includeCaseExposures = true;
            bool includeContacts = IncludeCasesAndContacts;
            bool deIdentifyData = DeIdentifyData;

            #region Remove extraneous data

            int rows = 0;

            OverallSyncStatus = "Deleting extraneous page table rows...";
            IDbDriver db = _project.CollectedData.GetDatabase();

            foreach (View form in _project.Views)
            {
                foreach (Page page in form.Pages)
                {
                    Query deleteQuery = db.CreateQuery("DELETE FROM " + form.Name + " WHERE GlobalRecordId NOT IN (SELECT GlobalRecordId FROM " + page.TableName + ")");
                    rows = db.ExecuteNonQuery(deleteQuery);
                    if (rows > 0)
                    {
                        // report ??
                    }

                    Query pageDeleteQuery = db.CreateQuery("DELETE FROM " + page.TableName + " WHERE GlobalRecordId NOT IN (SELECT GlobalRecordId FROM " + form.Name + ")");
                    rows = db.ExecuteNonQuery(deleteQuery);
                    if (rows > 0)
                    {
                        // report ??
                    }
                }
            }

            Query linksDeleteQuery = db.CreateQuery("DELETE FROM metaLinks WHERE ToViewId = @ToViewId AND ToRecordGuid NOT IN (SELECT GlobalRecordId FROM " + ContactForm.TableName + ")");
            linksDeleteQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, ContactFormId));
            rows = db.ExecuteNonQuery(linksDeleteQuery);

            if (db.TableExists("metaHistory"))
            {
                Query historyDeleteQuery = db.CreateQuery("DELETE FROM metaHistory WHERE ContactGUID NOT IN (SELECT GlobalRecordId FROM " + ContactForm.TableName + ")");
                rows = db.ExecuteNonQuery(historyDeleteQuery);
            }

            #endregion // Remove extraneous data

            RecordsExported = String.Empty;
            IsDataSyncing = true;
            IsShowingExportProgress = true;

            var doc = new XmlDocument { XmlResolver = null };

            SendMessageForAwaitAll();

            Task.Factory.StartNew(
                () =>
                {
                    doc = CreateCaseSyncFile(includeCases, includeCaseExposures, includeContacts, filters, deIdentifyData, recordProcessingScope);
                },
                 System.Threading.CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).ContinueWith(
                 delegate
                 {
                     try
                     {
                         if (!String.IsNullOrEmpty(doc.InnerText))
                         {
                             string compressedText = Epi.ImportExport.ImportExportHelper.Zip(doc.OuterXml); 
                             compressedText = "[[EPIINFO7_VHF_CASE_SYNC_FILE__0937]]" + compressedText;
                             Epi.Configuration.EncryptStringToFile(compressedText, SyncFilePath, "vQ@6L'<J3?)~5=vQnwh(2ic;>.<=dknF&/TZ4Uu!$78", "", "", 1000);
                         }
                     }
                     catch (Exception)
                     {
                         // do nothing... if the XML is invalid, we should have already alerted the user in a different method
                     }
                     finally
                     {
                         SendMessageForUnAwaitAll();
                     }

                     TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                     TaskbarProgressValue = 0;
                     ProgressValue = 0;

                     IsDataSyncing = false;

                     stopwatch.Stop();
                     SyncStatus = String.Empty;
                     OverallSyncStatus = "Finished exporting data to sync file. Elapsed time: " + stopwatch.Elapsed.TotalMinutes.ToString("F1") + " minutes.";

                 }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        #endregion // Methods

        #region Commands

        public ICommand StartExportCommand { get { return new RelayCommand(StartExportCommandExecute, CanExecuteStartExportCommand); } }
        private void StartExportCommandExecute()
        {
            if (!String.IsNullOrEmpty(ProjectFilePath))
            {
                XDocument doc = XDocument.Load(ProjectFilePath);
                string dataDriver = doc.Element("Project").Element("CollectedData").Element("Database").Attribute("dataDriver").Value;

                if (!dataDriver.Equals("Epi.Data.SqlServer.SqlDBFactory, Epi.Data.SqlServer", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Only projects using Microsoft SQL Server are supported.");
                }
            }

            RecordProcessingScope scope = RecordProcessingScope.Undeleted;

            switch (RecordScope)
            {
                case "Deleted":
                    scope = RecordProcessingScope.Deleted;
                    break;
                case "Both":
                    scope = RecordProcessingScope.Both;
                    break;
                case "Active":
                default:
                    scope = RecordProcessingScope.Undeleted;
                    break;
            }

            CreateCaseSyncFileStart(null, scope);
        }

        private bool CanExecuteStartExportCommand()
        {
            return (!String.IsNullOrEmpty(SyncFilePath) && !String.IsNullOrEmpty(ProjectFilePath));
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
