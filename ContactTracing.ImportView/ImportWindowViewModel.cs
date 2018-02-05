using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml;
using Epi;
using Epi.Data;
using Epi.ImportExport;
using Epi.ImportExport.ProjectPackagers;

namespace ContactTracing.ImportView
{
    public sealed class ImportWindowViewModel : ObservableObject
    {
        #region Members
        private System.Windows.Shell.TaskbarItemProgressState _taskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
        private double _taskbarProgressValue = 0.0;
        private string _syncStatus = String.Empty;
        private Project _project;
        private bool _isDataSyncing = false;
        private bool _isShowingImportProgress = false;
        private string _syncFilePath = String.Empty;
        private bool _includeCases = true;
        private bool _includeCasesAndContacts = true;
        private bool _deIdentifyData = false;
        private string _projectFilePath = String.Empty;
        #endregion // Members

        #region Events
        public event EventHandler SyncProblemsDetected;
        #endregion // Events

        #region Properties
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

        public bool IsShowingImportProgress
        {
            get
            {
                return _isShowingImportProgress;
            }
            protected internal set
            {
                if (_isShowingImportProgress != value)
                {
                    _isShowingImportProgress = value;
                    RaisePropertyChanged("IsShowingImportProgress");
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

        public ImportWindowViewModel(string projectPath)
            : base()
        {
            LoadConfig();

            SetProject(projectPath);
        }

        public ImportWindowViewModel()
            : base()
        {
            LoadConfig();
        }
        #endregion // Constructors

        #region Methods

        public void SetProject(string projectFilePath)
        {
            ProjectFilePath = projectFilePath;
        }

        void unpackager_UpdateProgress(double progress)
        {
            TaskbarProgressValue = (progress / 100);
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

        private void SendMessageForAwaitAll()
        {
            // do nothing for now
        }

        private void SendMessageForUnAwaitAll()
        {
            // do nothing for now
        }

        private void SendMessageForDataImported()
        {
            // do nothing for now
        }


        private void SyncCaseData(XmlDocument doc)
        {
            TaskbarProgressValue = 0;

            IDbDriver Database = _project.CollectedData.GetDatabase();

            bool includesContacts = false;
            bool includesLinks = false;

            XmlDocument caseDoc = doc.Clone() as XmlDocument;
            if (caseDoc != null)
            {
                caseDoc.XmlResolver = null;
                XmlNode linkNode = null;
                XmlNode followUpNode = null;

                foreach (XmlNode node in caseDoc.ChildNodes[0].ChildNodes)
                {
                    if (node.Name.Equals("Links", StringComparison.OrdinalIgnoreCase))
                    {
                        linkNode = node;
                        includesLinks = true;
                    }
                    else if (node.Name.Equals("ContactFollowUps", StringComparison.OrdinalIgnoreCase))
                    {
                        followUpNode = node;
                    }
                    else if (node.Attributes["Name"].Value.ToString().StartsWith("Contact", StringComparison.OrdinalIgnoreCase))
                    {
                        includesContacts = true;
                    }
                }

                //foreach (XmlNode node in nodesToRemove)
                //{
                //    caseDoc.ChildNodes[0].RemoveChild(node);
                //}

                Epi.ImportExport.ProjectPackagers.XmlDataUnpackager unpackager = new Epi.ImportExport.ProjectPackagers.XmlDataUnpackager(CaseForm, caseDoc);
                unpackager.StatusChanged += unpackager_StatusChanged;
                unpackager.UpdateProgress += unpackager_UpdateProgress;

                SendMessageForAwaitAll();

                try
                {
                    unpackager.Unpackage();
                }
                catch (Exception ex)
                {
                    if (SyncProblemsDetected != null)
                    {
                        SyncProblemsDetected(ex, new EventArgs());
                    }
                }
                finally
                {
                    SendMessageForUnAwaitAll();
                    unpackager.StatusChanged -= unpackager_StatusChanged;
                    unpackager.UpdateProgress -= unpackager_UpdateProgress;
                    unpackager = null;
                }

                #region Import Link Data

                if (includesLinks && linkNode != null)
                {
                    SendMessageForAwaitAll();

                    TaskbarProgressValue = 0;
                    SyncStatus = "Synchronizing relationship data...";

                    double inc = 1.0 / linkNode.ChildNodes.Count;

                    try
                    {
                        foreach (XmlNode node in linkNode.ChildNodes)
                        {
                            if (!String.IsNullOrEmpty(node.InnerText))
                            {
                                string fromRecordGuid = node.SelectSingleNode("FromRecordGuid").InnerText;
                                string toRecordGuid = node.SelectSingleNode("ToRecordGuid").InnerText;

                                int fromViewId = Int32.Parse(node.SelectSingleNode("FromViewId").InnerText);
                                int toViewId = Int32.Parse(node.SelectSingleNode("ToViewId").InnerText);

                                Query selectQuery = Database.CreateQuery("SELECT * FROM [metaLinks] WHERE [FromRecordGuid] = @FromRecordGuid AND " +
                                    "[ToRecordGuid] = @ToRecordGuid AND [FromViewId] = @FromViewId AND [ToViewId] = @ToViewId");

                                selectQuery.Parameters.Add(new QueryParameter("@FromRecordGuid", DbType.String, fromRecordGuid));
                                selectQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, toRecordGuid));
                                selectQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, fromViewId));
                                selectQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, toViewId));

                                DataTable destinationLinkTable = Database.Select(selectQuery);

                                bool linkExists = false;
                                if (destinationLinkTable.Rows.Count >= 1)
                                {
                                    linkExists = true;
                                }

                                DateTime lastContactDate = DateTime.MinValue;
                                string lastContactDateString = node.SelectSingleNode("LastContactDate").InnerText;
                                long lastContactDateTicks = long.Parse(lastContactDateString);

                                lastContactDate = new DateTime(lastContactDateTicks);
                                lastContactDate = new DateTime(lastContactDate.Year,
                                    lastContactDate.Month,
                                    lastContactDate.Day,
                                    lastContactDate.Hour,
                                    lastContactDate.Minute,
                                    lastContactDate.Second);

                                int contactType = int.Parse(node.SelectSingleNode("ContactType").InnerText);
                                string relationshipType = node.SelectSingleNode("RelationshipType").InnerText;

                                if (String.IsNullOrEmpty(relationshipType.Trim()) && linkExists)
                                {
                                    relationshipType = destinationLinkTable.Rows[0]["RelationshipType"].ToString();
                                }

                                object tentative = DBNull.Value;

                                string tentativeString = node.SelectSingleNode("Tentative").InnerText;
                                if (!String.IsNullOrEmpty(tentativeString))
                                {
                                    tentative = int.Parse(tentativeString);
                                }
                                else if (linkExists && destinationLinkTable.Rows[0]["Tentative"] != DBNull.Value)
                                {
                                    tentative = int.Parse(destinationLinkTable.Rows[0]["Tentative"].ToString());
                                }

                                bool isEstimated = bool.Parse(node.SelectSingleNode("IsEstimatedContactDate").InnerText);

                                object day1 = DBNull.Value;
                                object day2 = DBNull.Value;
                                object day3 = DBNull.Value;
                                object day4 = DBNull.Value;
                                object day5 = DBNull.Value;
                                object day6 = DBNull.Value;
                                object day7 = DBNull.Value;
                                object day8 = DBNull.Value;
                                object day9 = DBNull.Value;
                                object day10 = DBNull.Value;
                                object day11 = DBNull.Value;
                                object day12 = DBNull.Value;
                                object day13 = DBNull.Value;
                                object day14 = DBNull.Value;
                                object day15 = DBNull.Value;
                                object day16 = DBNull.Value;
                                object day17 = DBNull.Value;
                                object day18 = DBNull.Value;
                                object day19 = DBNull.Value;
                                object day20 = DBNull.Value;
                                object day21 = DBNull.Value;

                                if (node.SelectSingleNode("Day1") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day1").InnerText))
                                    {
                                        day1 = short.Parse(node.SelectSingleNode("Day1").InnerText);
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day1"] != DBNull.Value)
                                    {
                                        day1 = short.Parse(destinationLinkTable.Rows[0]["Day1"].ToString());
                                    }
                                }

                                if (node.SelectSingleNode("Day2") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day2").InnerText))
                                    {
                                        day2 = short.Parse(node.SelectSingleNode("Day2").InnerText);
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day2"] != DBNull.Value)
                                    {
                                        day2 = short.Parse(destinationLinkTable.Rows[0]["Day2"].ToString());
                                    }
                                }

                                if (node.SelectSingleNode("Day3") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day3").InnerText))
                                    {
                                        day3 = short.Parse(node.SelectSingleNode("Day3").InnerText);
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day3"] != DBNull.Value)
                                    {
                                        day3 = short.Parse(destinationLinkTable.Rows[0]["Day3"].ToString());
                                    }
                                }

                                if (node.SelectSingleNode("Day4") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day4").InnerText))
                                    {
                                        day4 = short.Parse(node.SelectSingleNode("Day4").InnerText);
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day4"] != DBNull.Value)
                                    {
                                        day4 = short.Parse(destinationLinkTable.Rows[0]["Day4"].ToString());
                                    }
                                }

                                if (node.SelectSingleNode("Day5") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day5").InnerText))
                                    {
                                        day5 = short.Parse(node.SelectSingleNode("Day5").InnerText);
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day5"] != DBNull.Value)
                                    {
                                        day5 = short.Parse(destinationLinkTable.Rows[0]["Day5"].ToString());
                                    }
                                }

                                if (node.SelectSingleNode("Day6") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day6").InnerText))
                                    {
                                        day6 = short.Parse(node.SelectSingleNode("Day6").InnerText);
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day6"] != DBNull.Value)
                                    {
                                        day6 = short.Parse(destinationLinkTable.Rows[0]["Day6"].ToString());
                                    }
                                }

                                if (node.SelectSingleNode("Day7") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day7").InnerText))
                                    {
                                        day7 = short.Parse(node.SelectSingleNode("Day7").InnerText);
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day7"] != DBNull.Value)
                                    {
                                        day7 = short.Parse(destinationLinkTable.Rows[0]["Day7"].ToString());
                                    }
                                }

                                if (node.SelectSingleNode("Day8") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day8").InnerText))
                                    {
                                        day8 = short.Parse(node.SelectSingleNode("Day8").InnerText);
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day8"] != DBNull.Value)
                                    {
                                        day8 = short.Parse(destinationLinkTable.Rows[0]["Day8"].ToString());
                                    }
                                }

                                if (node.SelectSingleNode("Day9") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day9").InnerText))
                                    {
                                        day9 = short.Parse(node.SelectSingleNode("Day9").InnerText);
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day9"] != DBNull.Value)
                                    {
                                        day9 = short.Parse(destinationLinkTable.Rows[0]["Day9"].ToString());
                                    }
                                }

                                if (node.SelectSingleNode("Day10") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day10").InnerText))
                                    {
                                        day10 = short.Parse(node.SelectSingleNode("Day10").InnerText);
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day10"] != DBNull.Value)
                                    {
                                        day10 = short.Parse(destinationLinkTable.Rows[0]["Day10"].ToString());
                                    }
                                }

                                if (node.SelectSingleNode("Day11") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day11").InnerText))
                                    {
                                        day11 = short.Parse(node.SelectSingleNode("Day11").InnerText);
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day11"] != DBNull.Value)
                                    {
                                        day11 = short.Parse(destinationLinkTable.Rows[0]["Day11"].ToString());
                                    }
                                }

                                if (node.SelectSingleNode("Day12") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day12").InnerText))
                                    {
                                        day12 = short.Parse(node.SelectSingleNode("Day12").InnerText);
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day12"] != DBNull.Value)
                                    {
                                        day12 = short.Parse(destinationLinkTable.Rows[0]["Day12"].ToString());
                                    }
                                }

                                if (node.SelectSingleNode("Day13") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day13").InnerText))
                                    {
                                        day13 = short.Parse(node.SelectSingleNode("Day13").InnerText);
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day13"] != DBNull.Value)
                                    {
                                        day13 = short.Parse(destinationLinkTable.Rows[0]["Day13"].ToString());
                                    }
                                }

                                if (node.SelectSingleNode("Day14") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day14").InnerText))
                                    {
                                        day14 = short.Parse(node.SelectSingleNode("Day14").InnerText);
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day14"] != DBNull.Value)
                                    {
                                        day14 = short.Parse(destinationLinkTable.Rows[0]["Day14"].ToString());
                                    }
                                }

                                if (node.SelectSingleNode("Day15") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day15").InnerText))
                                    {
                                        day15 = short.Parse(node.SelectSingleNode("Day15").InnerText);
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day15"] != DBNull.Value)
                                    {
                                        day15 = short.Parse(destinationLinkTable.Rows[0]["Day15"].ToString());
                                    }
                                }

                                if (node.SelectSingleNode("Day16") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day16").InnerText))
                                    {
                                        day16 = short.Parse(node.SelectSingleNode("Day16").InnerText);
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day16"] != DBNull.Value)
                                    {
                                        day16 = short.Parse(destinationLinkTable.Rows[0]["Day16"].ToString());
                                    }
                                }

                                if (node.SelectSingleNode("Day17") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day17").InnerText))
                                    {
                                        day17 = short.Parse(node.SelectSingleNode("Day17").InnerText);
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day17"] != DBNull.Value)
                                    {
                                        day17 = short.Parse(destinationLinkTable.Rows[0]["Day17"].ToString());
                                    }
                                }

                                if (node.SelectSingleNode("Day18") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day18").InnerText))
                                    {
                                        day18 = short.Parse(node.SelectSingleNode("Day18").InnerText);
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day18"] != DBNull.Value)
                                    {
                                        day18 = short.Parse(destinationLinkTable.Rows[0]["Day18"].ToString());
                                    }
                                }

                                if (node.SelectSingleNode("Day19") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day19").InnerText))
                                    {
                                        day19 = short.Parse(node.SelectSingleNode("Day19").InnerText);
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day19"] != DBNull.Value)
                                    {
                                        day19 = short.Parse(destinationLinkTable.Rows[0]["Day19"].ToString());
                                    }
                                }

                                if (node.SelectSingleNode("Day20") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day20").InnerText))
                                    {
                                        day20 = short.Parse(node.SelectSingleNode("Day20").InnerText);
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day20"] != DBNull.Value)
                                    {
                                        day20 = short.Parse(destinationLinkTable.Rows[0]["Day20"].ToString());
                                    }
                                }

                                if (node.SelectSingleNode("Day21") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day21").InnerText))
                                    {
                                        day21 = short.Parse(node.SelectSingleNode("Day21").InnerText);
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day21"] != DBNull.Value)
                                    {
                                        day21 = short.Parse(destinationLinkTable.Rows[0]["Day21"].ToString());
                                    }
                                }

                                string day1Notes = String.Empty;
                                string day2Notes = String.Empty;
                                string day3Notes = String.Empty;
                                string day4Notes = String.Empty;
                                string day5Notes = String.Empty;
                                string day6Notes = String.Empty;
                                string day7Notes = String.Empty;
                                string day8Notes = String.Empty;
                                string day9Notes = String.Empty;
                                string day10Notes = String.Empty;
                                string day11Notes = String.Empty;
                                string day12Notes = String.Empty;
                                string day13Notes = String.Empty;
                                string day14Notes = String.Empty;
                                string day15Notes = String.Empty;
                                string day16Notes = String.Empty;
                                string day17Notes = String.Empty;
                                string day18Notes = String.Empty;
                                string day19Notes = String.Empty;
                                string day20Notes = String.Empty;
                                string day21Notes = String.Empty;

                                if (node.SelectSingleNode("Day1Notes") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day1Notes").InnerText))
                                    {
                                        day1Notes = node.SelectSingleNode("Day1Notes").InnerText;
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day1Notes"] != DBNull.Value)
                                    {
                                        day1Notes = destinationLinkTable.Rows[0]["Day1Notes"].ToString();
                                    }
                                }

                                if (node.SelectSingleNode("Day2Notes") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day2Notes").InnerText))
                                    {
                                        day2Notes = node.SelectSingleNode("Day2Notes").InnerText;
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day2Notes"] != DBNull.Value)
                                    {
                                        day2Notes = destinationLinkTable.Rows[0]["Day2Notes"].ToString();
                                    }
                                }

                                if (node.SelectSingleNode("Day3Notes") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day3Notes").InnerText))
                                    {
                                        day3Notes = node.SelectSingleNode("Day3Notes").InnerText;
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day3Notes"] != DBNull.Value)
                                    {
                                        day3Notes = destinationLinkTable.Rows[0]["Day3Notes"].ToString();
                                    }
                                }

                                if (node.SelectSingleNode("Day4Notes") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day4Notes").InnerText))
                                    {
                                        day4Notes = node.SelectSingleNode("Day4Notes").InnerText;
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day4Notes"] != DBNull.Value)
                                    {
                                        day4Notes = destinationLinkTable.Rows[0]["Day4Notes"].ToString();
                                    }
                                }

                                if (node.SelectSingleNode("Day5Notes") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day5Notes").InnerText))
                                    {
                                        day5Notes = node.SelectSingleNode("Day5Notes").InnerText;
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day5Notes"] != DBNull.Value)
                                    {
                                        day5Notes = destinationLinkTable.Rows[0]["Day5Notes"].ToString();
                                    }
                                }

                                if (node.SelectSingleNode("Day6Notes") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day6Notes").InnerText))
                                    {
                                        day6Notes = node.SelectSingleNode("Day6Notes").InnerText;
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day6Notes"] != DBNull.Value)
                                    {
                                        day6Notes = destinationLinkTable.Rows[0]["Day6Notes"].ToString();
                                    }
                                }

                                if (node.SelectSingleNode("Day7Notes") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day7Notes").InnerText))
                                    {
                                        day7Notes = node.SelectSingleNode("Day7Notes").InnerText;
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day7Notes"] != DBNull.Value)
                                    {
                                        day7Notes = destinationLinkTable.Rows[0]["Day7Notes"].ToString();
                                    }
                                }

                                if (node.SelectSingleNode("Day8Notes") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day8Notes").InnerText))
                                    {
                                        day8Notes = node.SelectSingleNode("Day8Notes").InnerText;
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day8Notes"] != DBNull.Value)
                                    {
                                        day8Notes = destinationLinkTable.Rows[0]["Day8Notes"].ToString();
                                    }
                                }

                                if (node.SelectSingleNode("Day9Notes") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day9Notes").InnerText))
                                    {
                                        day9Notes = node.SelectSingleNode("Day9Notes").InnerText;
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day9Notes"] != DBNull.Value)
                                    {
                                        day9Notes = destinationLinkTable.Rows[0]["Day9Notes"].ToString();
                                    }
                                }

                                if (node.SelectSingleNode("Day10Notes") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day10Notes").InnerText))
                                    {
                                        day10Notes = node.SelectSingleNode("Day10Notes").InnerText;
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day10Notes"] != DBNull.Value)
                                    {
                                        day10Notes = destinationLinkTable.Rows[0]["Day10Notes"].ToString();
                                    }
                                }

                                if (node.SelectSingleNode("Day11Notes") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day11Notes").InnerText))
                                    {
                                        day11Notes = node.SelectSingleNode("Day11Notes").InnerText;
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day11Notes"] != DBNull.Value)
                                    {
                                        day11Notes = destinationLinkTable.Rows[0]["Day11Notes"].ToString();
                                    }
                                }

                                if (node.SelectSingleNode("Day12Notes") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day12Notes").InnerText))
                                    {
                                        day12Notes = node.SelectSingleNode("Day12Notes").InnerText;
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day12Notes"] != DBNull.Value)
                                    {
                                        day12Notes = destinationLinkTable.Rows[0]["Day12Notes"].ToString();
                                    }
                                }

                                if (node.SelectSingleNode("Day13Notes") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day13Notes").InnerText))
                                    {
                                        day13Notes = node.SelectSingleNode("Day13Notes").InnerText;
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day13Notes"] != DBNull.Value)
                                    {
                                        day13Notes = destinationLinkTable.Rows[0]["Day13Notes"].ToString();
                                    }
                                }

                                if (node.SelectSingleNode("Day14Notes") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day14Notes").InnerText))
                                    {
                                        day14Notes = node.SelectSingleNode("Day14Notes").InnerText;
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day14Notes"] != DBNull.Value)
                                    {
                                        day14Notes = destinationLinkTable.Rows[0]["Day14Notes"].ToString();
                                    }
                                }

                                if (node.SelectSingleNode("Day15Notes") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day15Notes").InnerText))
                                    {
                                        day15Notes = node.SelectSingleNode("Day15Notes").InnerText;
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day15Notes"] != DBNull.Value)
                                    {
                                        day15Notes = destinationLinkTable.Rows[0]["Day15Notes"].ToString();
                                    }
                                }

                                if (node.SelectSingleNode("Day16Notes") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day16Notes").InnerText))
                                    {
                                        day16Notes = node.SelectSingleNode("Day16Notes").InnerText;
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day16Notes"] != DBNull.Value)
                                    {
                                        day16Notes = destinationLinkTable.Rows[0]["Day16Notes"].ToString();
                                    }
                                }

                                if (node.SelectSingleNode("Day17Notes") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day17Notes").InnerText))
                                    {
                                        day17Notes = node.SelectSingleNode("Day17Notes").InnerText;
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day17Notes"] != DBNull.Value)
                                    {
                                        day17Notes = destinationLinkTable.Rows[0]["Day17Notes"].ToString();
                                    }
                                }

                                if (node.SelectSingleNode("Day18Notes") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day18Notes").InnerText))
                                    {
                                        day18Notes = node.SelectSingleNode("Day18Notes").InnerText;
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day18Notes"] != DBNull.Value)
                                    {
                                        day18Notes = destinationLinkTable.Rows[0]["Day18Notes"].ToString();
                                    }
                                }

                                if (node.SelectSingleNode("Day19Notes") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day19Notes").InnerText))
                                    {
                                        day19Notes = node.SelectSingleNode("Day19Notes").InnerText;
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day19Notes"] != DBNull.Value)
                                    {
                                        day19Notes = destinationLinkTable.Rows[0]["Day19Notes"].ToString();
                                    }
                                }

                                if (node.SelectSingleNode("Day20Notes") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day20Notes").InnerText))
                                    {
                                        day20Notes = node.SelectSingleNode("Day20Notes").InnerText;
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day20Notes"] != DBNull.Value)
                                    {
                                        day20Notes = destinationLinkTable.Rows[0]["Day20Notes"].ToString();
                                    }
                                }

                                if (node.SelectSingleNode("Day21Notes") != null)
                                {
                                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Day21Notes").InnerText))
                                    {
                                        day21Notes = node.SelectSingleNode("Day21Notes").InnerText;
                                    }
                                    else if (linkExists && destinationLinkTable.Rows[0]["Day21Notes"] != DBNull.Value)
                                    {
                                        day21Notes = destinationLinkTable.Rows[0]["Day21Notes"].ToString();
                                    }
                                }

                                if (linkExists)
                                {
                                    Query updateQuery = Database.CreateQuery("UPDATE [metaLinks] SET " +
                                        "[LastContactDate] = @LastContactDate, " +
                                        "[ContactType] = @ContactType, " +
                                        "[RelationshipType] = @RelationshipType, " +
                                        "[Tentative] = @Tentative, " +
                                        "[IsEstimatedContactDate] = @IsEstimatedContactDate, " +
                                        "[Day1] = @Day1, " +
                                        "[Day2] = @Day2, " +
                                        "[Day3] = @Day3, " +
                                        "[Day4] = @Day4, " +
                                        "[Day5] = @Day5, " +
                                        "[Day6] = @Day6, " +
                                        "[Day7] = @Day7, " +
                                        "[Day8] = @Day8, " +
                                        "[Day9] = @Day9, " +
                                        "[Day10] = @Day10, " +
                                        "[Day11] = @Day11, " +
                                        "[Day12] = @Day12, " +
                                        "[Day13] = @Day13, " +
                                        "[Day14] = @Day14, " +
                                        "[Day15] = @Day15, " +
                                        "[Day16] = @Day16, " +
                                        "[Day17] = @Day17, " +
                                        "[Day18] = @Day18, " +
                                        "[Day19] = @Day19, " +
                                        "[Day20] = @Day20, " +
                                        "[Day21] = @Day21, " +
                                        "[Day1Notes] = @Day1Notes, " +
                                        "[Day2Notes] = @Day2Notes, " +
                                        "[Day3Notes] = @Day3Notes, " +
                                        "[Day4Notes] = @Day4Notes, " +
                                        "[Day5Notes] = @Day5Notes, " +
                                        "[Day6Notes] = @Day6Notes, " +
                                        "[Day7Notes] = @Day7Notes, " +
                                        "[Day8Notes] = @Day8Notes, " +
                                        "[Day9Notes] = @Day9Notes, " +
                                        "[Day10Notes] = @Day10Notes, " +
                                        "[Day11Notes] = @Day11Notes, " +
                                        "[Day12Notes] = @Day12Notes, " +
                                        "[Day13Notes] = @Day13Notes, " +
                                        "[Day14Notes] = @Day14Notes, " +
                                        "[Day15Notes] = @Day15Notes, " +
                                        "[Day16Notes] = @Day16Notes, " +
                                        "[Day17Notes] = @Day17Notes, " +
                                        "[Day18Notes] = @Day18Notes, " +
                                        "[Day19Notes] = @Day19Notes, " +
                                        "[Day20Notes] = @Day20Notes, " +
                                        "[Day21Notes] = @Day21Notes " +
                                "WHERE [ToRecordGuid] = @ToRecordGuid AND [FromRecordGuid] = @FromRecordGuid AND [ToViewId] = @ToViewId AND " +
                                "[FromViewId] = @FromViewId");

                                    updateQuery.Parameters.Add(new QueryParameter("@LastContactDate", DbType.DateTime, lastContactDate));
                                    updateQuery.Parameters.Add(new QueryParameter("@ContactType", DbType.Int32, contactType));
                                    updateQuery.Parameters.Add(new QueryParameter("@RelationshipType", DbType.String, relationshipType));
                                    updateQuery.Parameters.Add(new QueryParameter("@Tentative", DbType.Byte, tentative));
                                    updateQuery.Parameters.Add(new QueryParameter("@IsEstimatedContactDate", DbType.Boolean, isEstimated));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day1", DbType.Byte, day1));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day2", DbType.Byte, day2));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day3", DbType.Byte, day3));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day4", DbType.Byte, day4));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day5", DbType.Byte, day5));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day6", DbType.Byte, day6));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day7", DbType.Byte, day7));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day8", DbType.Byte, day8));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day9", DbType.Byte, day9));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day10", DbType.Byte, day10));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day11", DbType.Byte, day11));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day12", DbType.Byte, day12));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day13", DbType.Byte, day13));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day14", DbType.Byte, day14));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day15", DbType.Byte, day15));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day16", DbType.Byte, day16));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day17", DbType.Byte, day17));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day18", DbType.Byte, day18));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day19", DbType.Byte, day19));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day20", DbType.Byte, day20));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day21", DbType.Byte, day21));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day1Notes", DbType.String, day1Notes));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day2Notes", DbType.String, day2Notes));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day3Notes", DbType.String, day3Notes));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day4Notes", DbType.String, day4Notes));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day5Notes", DbType.String, day5Notes));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day6Notes", DbType.String, day6Notes));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day7Notes", DbType.String, day7Notes));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day8Notes", DbType.String, day8Notes));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day9Notes", DbType.String, day9Notes));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day10Notes", DbType.String, day10Notes));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day11Notes", DbType.String, day11Notes));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day12Notes", DbType.String, day12Notes));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day13Notes", DbType.String, day13Notes));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day14Notes", DbType.String, day14Notes));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day15Notes", DbType.String, day15Notes));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day16Notes", DbType.String, day16Notes));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day17Notes", DbType.String, day17Notes));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day18Notes", DbType.String, day18Notes));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day19Notes", DbType.String, day19Notes));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day20Notes", DbType.String, day20Notes));
                                    updateQuery.Parameters.Add(new QueryParameter("@Day21Notes", DbType.String, day21Notes));

                                    updateQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, toRecordGuid));
                                    updateQuery.Parameters.Add(new QueryParameter("@FromRecordGuid", DbType.String, fromRecordGuid));
                                    updateQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, toViewId));
                                    updateQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, fromViewId));

                                    int rows = Database.ExecuteNonQuery(updateQuery);
                                }
                                else
                                {
                                    // row didn't exist before, so insert instead

                                    Query insertQuery = Database.CreateQuery("INSERT INTO [metaLinks] (" +
                                    "[ToRecordGuid], " +
                                    "[FromRecordGuid], " +
                                    "[ToViewId], " +
                                    "[FromViewId], " +
                                    "[LastContactDate], " +
                                    "[ContactType], " +
                                    "[RelationshipType], " +
                                    "[Tentative], " +
                                    "[IsEstimatedContactDate], " +
                                    "[Day1], " +
                                    "[Day2], " +
                                    "[Day3], " +
                                    "[Day4], " +
                                    "[Day5], " +
                                    "[Day6], " +
                                    "[Day7], " +
                                    "[Day8], " +
                                    "[Day9], " +
                                    "[Day10], " +
                                    "[Day11], " +
                                    "[Day12], " +
                                    "[Day13], " +
                                    "[Day14], " +
                                    "[Day15], " +
                                    "[Day16], " +
                                    "[Day17], " +
                                    "[Day18], " +
                                    "[Day19], " +
                                    "[Day20], " +
                                    "[Day21], " +
                                    "[Day1Notes], " +
                                    "[Day2Notes], " +
                                    "[Day3Notes], " +
                                    "[Day4Notes], " +
                                    "[Day5Notes], " +
                                    "[Day6Notes], " +
                                    "[Day7Notes], " +
                                    "[Day8Notes], " +
                                    "[Day9Notes], " +
                                    "[Day10Notes], " +
                                    "[Day11Notes], " +
                                    "[Day12Notes], " +
                                    "[Day13Notes], " +
                                    "[Day14Notes], " +
                                    "[Day15Notes], " +
                                    "[Day16Notes], " +
                                    "[Day17Notes], " +
                                    "[Day18Notes], " +
                                    "[Day19Notes], " +
                                    "[Day20Notes], " +
                                    "[Day21Notes]) VALUES (" +
                                    "@ToRecordGuid, " +
                                    "@FromRecordGuid, " +
                                    "@ToViewId, " +
                                    "@FromViewId, " +
                                    "@LastContactDate, " +
                                    "@ContactType, " +
                                    "@RelationshipType, " +
                                    "@Tentative, " +
                                    "@IsEstimatedContactDate, " +
                                    "@Day1, " +
                                    "@Day2, " +
                                    "@Day3, " +
                                    "@Day4, " +
                                    "@Day5, " +
                                    "@Day6, " +
                                    "@Day7, " +
                                    "@Day8, " +
                                    "@Day9, " +
                                    "@Day10, " +
                                    "@Day11, " +
                                    "@Day12, " +
                                    "@Day13, " +
                                    "@Day14, " +
                                    "@Day15, " +
                                    "@Day16, " +
                                    "@Day17, " +
                                    "@Day18, " +
                                    "@Day19, " +
                                    "@Day20, " +
                                    "@Day21, " +
                                    "@Day1Notes, " +
                                    "@Day2Notes, " +
                                    "@Day3Notes, " +
                                    "@Day4Notes, " +
                                    "@Day5Notes, " +
                                    "@Day6Notes, " +
                                    "@Day7Notes, " +
                                    "@Day8Notes, " +
                                    "@Day9Notes, " +
                                    "@Day10Notes, " +
                                    "@Day11Notes, " +
                                    "@Day12Notes, " +
                                    "@Day13Notes, " +
                                    "@Day14Notes, " +
                                    "@Day15Notes, " +
                                    "@Day16Notes, " +
                                    "@Day17Notes, " +
                                    "@Day18Notes, " +
                                    "@Day19Notes, " +
                                    "@Day20Notes, " +
                                    "@Day21Notes) ");

                                    insertQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, toRecordGuid));
                                    insertQuery.Parameters.Add(new QueryParameter("@FromRecordGuid", DbType.String, fromRecordGuid));
                                    insertQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, toViewId));
                                    insertQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, fromViewId));

                                    insertQuery.Parameters.Add(new QueryParameter("@LastContactDate", DbType.DateTime, lastContactDate));
                                    insertQuery.Parameters.Add(new QueryParameter("@ContactType", DbType.Int32, contactType));
                                    insertQuery.Parameters.Add(new QueryParameter("@RelationshipType", DbType.String, relationshipType));
                                    insertQuery.Parameters.Add(new QueryParameter("@Tentative", DbType.Byte, tentative));
                                    insertQuery.Parameters.Add(new QueryParameter("@IsEstimatedContactDate", DbType.Boolean, isEstimated));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day1", DbType.Byte, day1));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day2", DbType.Byte, day2));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day3", DbType.Byte, day3));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day4", DbType.Byte, day4));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day5", DbType.Byte, day5));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day6", DbType.Byte, day6));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day7", DbType.Byte, day7));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day8", DbType.Byte, day8));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day9", DbType.Byte, day9));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day10", DbType.Byte, day10));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day11", DbType.Byte, day11));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day12", DbType.Byte, day12));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day13", DbType.Byte, day13));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day14", DbType.Byte, day14));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day15", DbType.Byte, day15));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day16", DbType.Byte, day16));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day17", DbType.Byte, day17));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day18", DbType.Byte, day18));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day19", DbType.Byte, day19));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day20", DbType.Byte, day20));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day21", DbType.Byte, day21));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day1Notes", DbType.String, day1Notes));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day2Notes", DbType.String, day2Notes));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day3Notes", DbType.String, day3Notes));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day4Notes", DbType.String, day4Notes));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day5Notes", DbType.String, day5Notes));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day6Notes", DbType.String, day6Notes));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day7Notes", DbType.String, day7Notes));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day8Notes", DbType.String, day8Notes));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day9Notes", DbType.String, day9Notes));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day10Notes", DbType.String, day10Notes));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day11Notes", DbType.String, day11Notes));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day12Notes", DbType.String, day12Notes));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day13Notes", DbType.String, day13Notes));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day14Notes", DbType.String, day14Notes));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day15Notes", DbType.String, day15Notes));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day16Notes", DbType.String, day16Notes));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day17Notes", DbType.String, day17Notes));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day18Notes", DbType.String, day18Notes));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day19Notes", DbType.String, day19Notes));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day20Notes", DbType.String, day20Notes));
                                    insertQuery.Parameters.Add(new QueryParameter("@Day21Notes", DbType.String, day21Notes));
                                    int rows = Database.ExecuteNonQuery(insertQuery);
                                    Contract.Assert(rows == 1);
                                }

                                TaskbarProgressValue = TaskbarProgressValue + inc;
                            }
                            //foreach (XmlNode node in linkNode.ChildNodes)
                            //{

                            //}
                        }
                    }
                    catch (Exception ex)
                    {
                        if (SyncProblemsDetected != null)
                        {
                            SyncProblemsDetected(ex, new EventArgs());
                        }
                    }
                    finally
                    {
                        SendMessageForUnAwaitAll();
                    }
                }

                #endregion // Import Link Data

                #region Import Follow-up Data

                if (followUpNode != null && Database.TableExists("metaHistory"))
                {
                    SendMessageForAwaitAll();

                    TaskbarProgressValue = 0;
                    SyncStatus = "Synchronizing contact follow-up data...";

                    var followUpRows = new List<FollowUpRow>();
                    Query selectQuery = Database.CreateQuery("SELECT * FROM [metaHistory]");
                    DataTable followUpsTable = Database.Select(selectQuery);

                    var inc = 1.0 / (double)(followUpNode.ChildNodes.Count + followUpsTable.Rows.Count);

                    #region Populate destination data
                    foreach (DataRow row in followUpsTable.Rows)
                    {
                        string guid = row["ContactGUID"].ToString();
                        DateTime date = Convert.ToDateTime(row["FollowUpDate"]);
                        int? status = null;
                        string note = row["Note"].ToString();
                        double? temp1 = null;
                        double? temp2 = null;

                        if (row["StatusOnDate"] != DBNull.Value)
                        {
                            status = Convert.ToInt32(row["StatusOnDate"]);
                        }
                        if (row["Temp1"] != DBNull.Value)
                        {
                            temp1 = Convert.ToDouble(row["Temp1"], CultureInfo.InvariantCulture);
                        }
                        if (row["Temp2"] != DBNull.Value)
                        {
                            temp2 = Convert.ToDouble(row["Temp2"], CultureInfo.InvariantCulture);
                        }

                        var followUpRow = new FollowUpRow
                        {
                            ContactGUID = new Guid(guid),
                            FollowUpDate = date,
                            StatusOnDate = status,
                            Note = note,
                            Temp1 = temp1,
                            Temp2 = temp2
                        };

                        followUpRows.Add(followUpRow);

                        TaskbarProgressValue += inc;
                    }
                    #endregion // Populate destination data

                    foreach (XmlNode node in followUpNode.ChildNodes)
                    {
                        if (!String.IsNullOrEmpty(node.InnerText))
                        {
                            string guid = node.SelectSingleNode("ContactGUID").InnerText;
                            DateTime date = DateTime.Parse(node.SelectSingleNode("FollowUpDate").InnerText, CultureInfo.InvariantCulture);
                            int? status = null;
                            string note = node.SelectSingleNode("Note").InnerText;
                            double? temp1 = null;
                            double? temp2 = null;

                            if (!String.IsNullOrEmpty(node.SelectSingleNode("StatusOnDate").InnerText))
                            {
                                status = Int32.Parse(node.SelectSingleNode("StatusOnDate").InnerText);
                            }

                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Temp1").InnerText))
                            {
                                temp1 = Double.Parse(node.SelectSingleNode("Temp1").InnerText, CultureInfo.InvariantCulture);
                            }

                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Temp2").InnerText))
                            {
                                temp2 = Double.Parse(node.SelectSingleNode("Temp2").InnerText, CultureInfo.InvariantCulture);
                            }

                            FollowUpRow existingRow = null;

                            foreach (FollowUpRow row in followUpRows)
                            {
                                if (row.ContactGUID.ToString().Equals(guid, StringComparison.OrdinalIgnoreCase) &&
                                    row.FollowUpDate.Year == date.Year &&
                                    row.FollowUpDate.Month == date.Month &&
                                    row.FollowUpDate.Day == date.Day)
                                {
                                    // found a match
                                    existingRow = row;
                                    break;
                                }
                            }

                            TaskbarProgressValue = TaskbarProgressValue + inc;

                            if (existingRow != null)
                            {
                                // update
                                string updateQueryText = "UPDATE [metaHistory] SET ";
                                var parameters = new List<QueryParameter>();

                                var wb = new WordBuilder(", ");

                                if (status != existingRow.StatusOnDate && status.HasValue)
                                {
                                    wb.Add("StatusOnDate = @StatusOnDate");
                                    parameters.Add(new QueryParameter("@StatusOnDate", DbType.Int16, status));
                                }

                                if (note != existingRow.Note && !String.IsNullOrEmpty(note))
                                {
                                    wb.Add("[Note] = @Note");
                                    parameters.Add(new QueryParameter("@Note", DbType.String, note));
                                }

                                if (temp1 != existingRow.Temp1 && temp1.HasValue)
                                {
                                    wb.Add("Temp1 = @Temp1");
                                    parameters.Add(new QueryParameter("@Temp1", DbType.Double, temp1));
                                }

                                if (temp2 != existingRow.Temp2 && temp2.HasValue)
                                {
                                    wb.Add("Temp2 = @Temp2");
                                    parameters.Add(new QueryParameter("@Temp2", DbType.Double, temp2));
                                }

                                if (parameters.Count == 0)
                                {
                                    continue; // no values changed, so don't update anything
                                }

                                updateQueryText += wb.ToString();
                                updateQueryText += " WHERE ContactGUID = @ContactGUID AND FollowUpDate = @FollowUpDate";

                                parameters.Add(new QueryParameter("@ContactGUID", DbType.Guid, new Guid(guid)));
                                parameters.Add(new QueryParameter("@FollowUpDate", DbType.DateTime, date));

                                Query updateQuery = Database.CreateQuery(updateQueryText);
                                foreach (QueryParameter parameter in parameters)
                                {
                                    updateQuery.Parameters.Add(parameter);
                                }

                                int rows = Database.ExecuteNonQuery(updateQuery);

                                if (rows == 0)
                                {
                                    throw new InvalidOperationException();
                                    // if we got here, then there's a problem with the query, because we "found a match" earlier and now the UPDATE is telling us
                                    // that no rows were updated...
                                }

                                //Query updateQuery = Database.CreateQuery()
                            }
                            else
                            {
                                // append
                                Query insertQuery = Database.CreateQuery("INSERT INTO [metaHistory] (ContactGUID, FollowUpDate, StatusOnDate, [Note], Temp1, Temp2) VALUES (" +
                                    "@ContactGuid, @FollowUpDate, @StatusOnDate, @Note, @Temp1, @Temp2)");
                                insertQuery.Parameters.Add(new QueryParameter("@ContactGuid", DbType.Guid, new Guid(guid)));
                                insertQuery.Parameters.Add(new QueryParameter("@FollowUpDate", DbType.DateTime, date));
                                insertQuery.Parameters.Add(status.HasValue
                                    ? new QueryParameter("@StatusOnDate", DbType.Int16, status.Value)
                                    : new QueryParameter("@StatusOnDate", DbType.Int16, DBNull.Value));
                                insertQuery.Parameters.Add(new QueryParameter("@Note", DbType.String, note));

                                insertQuery.Parameters.Add(temp1.HasValue
                                    ? new QueryParameter("@Temp1", DbType.Double, temp1.Value)
                                    : new QueryParameter("@Temp1", DbType.Double, DBNull.Value));

                                insertQuery.Parameters.Add(temp2.HasValue
                                    ? new QueryParameter("@Temp2", DbType.Double, temp2.Value)
                                    : new QueryParameter("@Temp2", DbType.Double, DBNull.Value));

                                int rows = Database.ExecuteNonQuery(insertQuery);
                                Contract.Assert(rows == 1);
                            }
                        }
                    }
                }
                #endregion Import Follow-up Data
            }
        }

        public void SyncCaseDataStart(XmlDocument doc)
        {
            if (IsWaitingOnOtherClients)
            {
                return;
            }

            if (doc == null)
            {
                throw new ArgumentNullException("doc");
            }

            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            IsDataSyncing = true;
            IsShowingImportProgress = true;
            TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;

            Task.Factory.StartNew(
                () =>
                {
                    SyncCaseData(doc);
                },
                 System.Threading.CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).ContinueWith(
                 delegate
                 {
                     TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                     TaskbarProgressValue = 0;

                     stopwatch.Stop();

                     IsDataSyncing = false;
                     SyncStatus = "Finished importing data. Elapsed time: " + stopwatch.Elapsed.TotalMinutes.ToString("F1") + " minutes.";
                     //RepopulateCollections(false);
                     SendMessageForDataImported();
                 }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        #endregion // Methods

        #region Commands

        public ICommand StartImportCommand { get { return new RelayCommand(StartImportCommandExecute, CanExecuteStartImportCommand); } }
        private void StartImportCommandExecute()
        {
            XmlDocument doc = new XmlDocument();
            doc.XmlResolver = null;
            try
            {
                string compressedText = Epi.Configuration.DecryptFileToString(SyncFilePath, "vQ@6L'<J3?)~5=vQnwh(2ic;>.<=dknF&/TZ4Uu!$78", "", "", 1000);
                if (compressedText.StartsWith("[[EPIINFO7_VHF_CASE_SYNC_FILE__0937]]", StringComparison.OrdinalIgnoreCase))
                {
                    string uncompressedText = Epi.ImportExport.ImportExportHelper.UnZip(compressedText.Substring(37));
                    doc.LoadXml(uncompressedText);
                }
            }
            catch (Exception ex)
            {
                // TODO: Add something here
                return;
            }

            SyncCaseDataStart(doc); 
        }

        private bool CanExecuteStartImportCommand()
        {
            return (!String.IsNullOrEmpty(SyncFilePath) && !String.IsNullOrEmpty(ProjectFilePath));
        }

        public ICommand StopImportCommand { get { return new RelayCommand(StopImportCommandExecute, CanExecuteStopImportCommand); } }
        private void StopImportCommandExecute()
        {
            IsShowingImportProgress = false;
        }

        private bool CanExecuteStopImportCommand()
        {
            return !IsDataSyncing;
        }
        #endregion // Commands
    }
}
