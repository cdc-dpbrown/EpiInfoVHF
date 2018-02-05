using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using Epi.Data;
using Epi.Fields;
using CDC.VHF.Foundation;
using CDC.VHF.Foundation.Enums;
using CDC.VHF.Services;
using CDC.VHF.Services.Collections;
using CDC.VHF.Domain.Sms;

namespace CDC.VHF.Domain
{
    [Serializable()]
    public class Project : Epi.Project, INotifyPropertyChanged
    {
        #region Members
        private string _outbreakName = String.Empty;
        private string _vhfDbVersion = String.Empty;
        private double _taskbarProgressValue = 0.0;
        private string _adm1 = String.Empty;
        private string _adm2 = String.Empty;
        private string _adm3 = String.Empty;
        private string _adm4 = String.Empty;
        #endregion // Members

        #region Properties

        [XmlIgnore]
        public RecordCollectionMaster CaseCollection { get; set; }

        [XmlIgnore]
        public RecordCollectionMaster ContactCollection { get; set; }

        [XmlIgnore]
        public Epi.View CaseForm { get; private set; }

        [XmlIgnore]
        public Epi.View LabForm { get; private set; }

        [XmlIgnore]
        public Epi.View ContactForm { get; private set; }

        [XmlIgnore]
        public bool IsShowingError { get; private set; }

        [XmlIgnore]
        public string LoadStatus { get; private set; }

        [XmlIgnore]
        public string Adm1 { get { return this._adm1; }
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
                        updateQuery.Parameters.Add(new QueryParameter("@Adm1", DbType.String, Adm1));
                        Database.ExecuteNonQuery(updateQuery);

                        UpdateAdministrativeBoundariesAsync();
                    }
                }
            }
        }

        [XmlIgnore]
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

        [XmlIgnore]
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

        [XmlIgnore]
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

        [XmlIgnore]
        public bool IsUsingOutdatedVersion { get; private set; }

        [XmlIgnore]
        public bool IsUsingDeprecatedVersion { get; private set; }

        /// <summary>
        /// Gets/sets the current progress value for the progress bar that shows up in the app's taskbar icon.
        /// </summary>
        [XmlIgnore]
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

        [XmlIgnore]
        public short DaysInWindow { get; private set; }

        [XmlIgnore]
        public VirusTestTypes VirusTestType { get; private set; }

        [XmlIgnore]
        private string IDPrefix { get; set; }

        [XmlIgnore]
        private string IDSeparator { get; set; }

        [XmlIgnore]
        private string IDPattern { get; set; }

        [XmlIgnore]
        private string Country { get; set; }

        [XmlIgnore]
        private IDbDriver Database { get; set; }

        [XmlIgnore]
        public string ErrorMessage { get; private set; }

        [XmlIgnore]
        public string ErrorMessageDetail { get; private set; }

        [XmlIgnore]
        public string MacAddress { get; private set; }

        [XmlElement]
        public SmsModule SmsModule { get; set; }

        [XmlAttribute("id")]
        public string VhfProjectId { get; set; }

        [XmlAttribute("name")]
        public string VhfProjectName { get; set; }

        [XmlAttribute("createDate", typeof(DateTime))]
        public DateTime VhfProjectCreateDate { get; set; }

        [XmlElement]
        public bool IsVHF { get; set; }

        [XmlElement]
        public bool IsLabProject { get; set; }

        [XmlElement(typeof(long))]
        public long OutbreakDateLong { get; set; }

        [XmlIgnore]
        public DateTime OutbreakDate { get; set; }

        [XmlElement]
        public string OutbreakName
        {
            get
            {
                return this._outbreakName;
            }
            set
            {
                this._outbreakName = value;
                RaisePropertyChanged("OutbreakName");
            }
        }

        /// <summary>
        /// The latest version of the Vhf app that has connected to the server
        /// </summary>
        [XmlIgnore]
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
        /// <summary>
        /// Gets/sets the internal Epi Info ID value for the case form
        /// </summary>
        [XmlIgnore]
        private int CaseFormId { get; set; }

        /// <summary>
        /// Gets/sets the internal Epi Info ID value for the contact form
        /// </summary>
        [XmlIgnore]
        private int ContactFormId { get; set; }

        [XmlElement]
        public string Culture { get; set; }
        #endregion // Properties

        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public Project()
            : base()
        {
            MacAddress = GetMacAddress();
        }

        /// <summary>
        /// Constructor with file path
        /// </summary>
        /// <param name="filePath">The file path to the location of the PRJ file</param>
        public Project(string filePath)
            : base(filePath)
        {
            XDocument doc = XDocument.Load(filePath);
            XElement root = doc.Root;

            XElement isVhf = root.Element("IsVHF");
            XElement isLab = root.Element("IsLabProject");
            XElement outbreakDate = root.Element("OutbreakDate");
            XElement outbreakName = root.Element("OutbreakName");
            XElement culture = root.Element("Culture");

            XElement smsModule = root.Element("SmsModule");

            MacAddress = GetMacAddress();
            IsVHF = bool.Parse(isVhf.Value);
            IsLabProject = bool.Parse(isLab.Value);
            OutbreakDate = new DateTime(long.Parse(outbreakDate.Value));
            OutbreakName = outbreakName.Value;
            if (culture == null)
            {
                Culture = String.Empty;
                //throw new InvalidOperationException("Project's culture settings cannot be null.");
                // probably don't need an exception here since we're moving to database-based culture checking
            }
            else
            {
                Culture = culture.Value;
            }

            SmsModule = new SmsModule();

            // setup SMS module
            if (smsModule != null)
            {
                XElement readReceipts = smsModule.Element("SendsReadReceipts");
                if (readReceipts != null)
                {
                    SmsModule.SendsReadReceipts = bool.Parse(readReceipts.Value);
                }

                XElement startupCommands = smsModule.Element("StartupCommands");
                if (startupCommands != null)
                {
                    SmsModule.StartupCommands = startupCommands.Value;
                }

                XElement authCode = smsModule.Element("SelfRegistrationCode");
                if (authCode != null)
                {
                    SmsModule.SelfRegistrationCode = authCode.Value;
                }

                XElement allowSelfReg = smsModule.Element("AllowsSelfRegistration");
                if (allowSelfReg != null)
                {
                    SmsModule.AllowsSelfRegistration = bool.Parse(allowSelfReg.Value);
                }

                XElement pollRate = smsModule.Element("PollRate");
                if (pollRate != null)
                {
                    SmsModule.PollRate = int.Parse(pollRate.Value);
                }

                XElement authorizedSenders = smsModule.Element("AuthorizedSmsSenders");
                if (authorizedSenders != null)
                {
                    SmsModule.AuthorizedSmsSenders.Clear();

                    var query = (from xml in authorizedSenders.Descendants() 
                                select xml);

                    foreach (XElement element in query)
                    {
                        if (element.Name.ToString().Equals("AuthorizedSmsSender", StringComparison.OrdinalIgnoreCase))
                        {
                            string phoneNumber = element.Element("PhoneNumber").Value;

                            SmsSenderInfo sender = new SmsSenderInfo(phoneNumber);

                            sender.Name = element.Element("Name").Value;
                            sender.CanAddUpdateCases = bool.Parse(element.Element("CanAddUpdateCases").Value);
                            sender.CanAddUpdateContacts = bool.Parse(element.Element("CanAddUpdateContacts").Value);
                            sender.CanAddUpdateLabResults = bool.Parse(element.Element("CanAddUpdateLabResults").Value);

                            SmsModule.AuthorizedSmsSenders.Add(sender);
                        }
                    }
                }
            }

            IDbDriver db = this.CollectedData.GetDatabase();
            Query selectQuery = db.CreateQuery("SELECT * FROM [metaViews]");
            DataTable dt = db.Select(selectQuery);

            foreach (DataRow row in dt.Rows)
            {
                if (row["Name"].ToString().Equals(Services.Constants.CASE_FORM_NAME))
                {
                    CaseFormId = int.Parse(row["ViewId"].ToString());
                }
                else if (row["Name"].ToString().Equals(Services.Constants.CONTACT_FORM_NAME))
                {
                    ContactFormId = int.Parse(row["ViewId"].ToString());
                }
            }
        }
        #endregion // Constructors

        #region INotifyPropertyChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            var handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void RaisePropertyChanged<T>(Expression<Func<T>> propertyExpresssion)
        {
            var propertyName = PropertySupport.ExtractPropertyName(propertyExpresssion);
            this.RaisePropertyChanged(propertyName);
        }

        private void RaisePropertyChanged(String propertyName)
        {
            VerifyPropertyName(propertyName);
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Warns the developer if this Object does not have a public property with
        /// the specified name. This method does not exist in a Release build.
        /// </summary>
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public void VerifyPropertyName(String propertyName)
        {
            // verify that the property name matches a real,  
            // public, instance property on this Object.
            if (TypeDescriptor.GetProperties(this)[propertyName] == null)
            {
                Debug.Fail("Invalid property name: " + propertyName);
            }
        }
        #endregion // INotifyPropertyChanged

        #region Methods

        /// <summary>
        /// Used to populate the entire set of collection objects based on what is currently residing in the database. This is
        /// not recommended to be called often; once on startup is probably enough, otherwise performance issues may abound.
        /// </summary>
        protected virtual bool PopulateCollections(bool initialLoad = false)
        {
            CaseForm = this.Views[Constants.CASE_FORM_NAME];
            LabForm = this.Views[Constants.LAB_FORM_NAME];
            ContactForm = this.Views[Constants.CONTACT_FORM_NAME];

            ErrorMessage = String.Empty;
            ErrorMessageDetail = String.Empty;
            
            Database = this.CollectedData.GetDatabase();

            DataTable caseTable = new DataTable();
            DataTable labTable = new DataTable();
            DataTable contactTable = new DataTable();

            try
            {
                Query selectmetaDbQuery = Database.CreateQuery("SELECT * FROM metaDbInfo");
                DataTable dt = Database.Select(selectmetaDbQuery);

                OutbreakName = dt.Rows[0]["OutbreakName"].ToString();
                if (!String.IsNullOrEmpty(dt.Rows[0]["OutbreakDate"].ToString()))
                {
                    OutbreakDate = (DateTime)dt.Rows[0]["OutbreakDate"];
                }
                IDPrefix = dt.Rows[0]["IDPrefix"].ToString();
                IDSeparator = dt.Rows[0]["IDSeparator"].ToString();
                IDPattern = dt.Rows[0]["IDPattern"].ToString();
                Country = dt.Rows[0]["PrimaryCountry"].ToString();

                switch (dt.Rows[0]["Virus"].ToString())
                {
                    case "Sudan":
                        VirusTestType = VirusTestTypes.Sudan;
                        break;
                    case "Ebola":
                        VirusTestType = VirusTestTypes.Ebola;
                        break;
                    case "Marburg":
                        VirusTestType = VirusTestTypes.Marburg;
                        break;
                    case "Bundibugyo":
                        VirusTestType = VirusTestTypes.Bundibugyo;
                        break;
                    case "CCHF":
                        VirusTestType = VirusTestTypes.CCHF;
                        DaysInWindow = 14;
                        break;
                    case "Rift":
                        VirusTestType = VirusTestTypes.Rift;
                        break;
                    case "Lassa":
                        VirusTestType = VirusTestTypes.Lassa;
                        break;
                }

                #region Parallel Load
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
                    Parallel.Invoke(
                        () =>
                        {
                            caseTable = null; //GetCasesTable(); // time-consuming
                        },
                        () =>
                        {
                            labTable = null; // GetLabTable(); // time-consuming
                        }
                    );
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

                    //List<Record> tempCaseList = new List<Record>();

                    //Parallel.ForEach(caseTable.AsEnumerable(), drow =>
                    //{
                    //    CaseViewModel c = new CaseViewModel(CaseForm, LabForm, drow);
                    //    //LoadCaseData(drow, c, false, false);
                    //    lock (syncCaseCollectionAdd)
                    //    {
                    //        tempCaseList.Add(c);
                    //    }
                    //});

                    ///* TODO: Find a better way of handling sorting. Tried CollectionView but ran into threading issues, 
                    // * which I didn't have time to resolve. The below code seems a little wasteful.
                    // */
                    //var caseSortQuery = from caseVM in tempCaseList
                    //                    orderby caseVM.ID
                    //                    select caseVM;

                    //foreach (Record c in caseSortQuery)
                    //{
                    //    lock (_caseCollectionLock)
                    //    {
                    //        RecordCollection.Add(c);
                    //    }
                    //}

                    //LoadLabDataForCasesAsync();

                    TaskbarProgressValue = TaskbarProgressValue + 0.1;
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
                    contactTable = null; // GetContactsTable(); // time-consuming
#if DEBUG
                    sw5a.Stop();
                    System.Diagnostics.Debug.Print("Contact SELECT query: " + sw5a.Elapsed.TotalMilliseconds.ToString());
#endif

#if DEBUG
                    System.Diagnostics.Stopwatch sw5 = new System.Diagnostics.Stopwatch();
                    sw5.Start();
#endif
                    //Parallel.ForEach(contactTable.AsEnumerable(), row =>
                    //{
                    //    ContactViewModel c = new ContactViewModel();
                    //    LoadContactData(row, c, false);
                    //    // TODO: Check for final status and inactivate based on conditions

                    //    if (!String.IsNullOrEmpty(c.FinalOutcome))
                    //    {
                    //        // anything present in this field is grounds for inactivation
                    //        c.IsActive = false;
                    //    }

                    //    lock (_contactCollectionLock)
                    //    {
                    //        ContactCollection.Add(c);
                    //    }
                    //});
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

                            if (initialLoad)
                            {
                                System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
                                Version thisVersion = a.GetName().Version;
                                string thisVersionString = thisVersion.ToString();

                                if (String.IsNullOrEmpty(VhfDbVersion))
                                {
                                    // update database with this version
                                    Query updateQuery = Database.CreateQuery("UPDATE metaDbInfo SET VhfVersion = @Version");
                                    updateQuery.Parameters.Add(new QueryParameter("@Version", DbType.String, thisVersionString));
                                    Database.ExecuteNonQuery(updateQuery);
                                }
                                else
                                {
                                    CheckVersioning();
                                }
                            }

                            if (initialLoad)
                            {
                                string culture = dt.Rows[0]["Culture"].ToString();
                                string currentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture.ToString();

                                if (String.IsNullOrEmpty(culture))
                                {
                                    Query updateQuery = Database.CreateQuery("UPDATE metaDbInfo SET Culture = @Culture");
                                    updateQuery.Parameters.Add(new QueryParameter("@Culture", DbType.String, currentCulture));
                                    Database.ExecuteNonQuery(updateQuery);
                                }
                                else if (!culture.Equals(currentCulture, StringComparison.OrdinalIgnoreCase))
                                {
                                    throw new InvalidOperationException("The language of the executing assembly does not match the language value specified in the database. The database setting is " + culture + ", but the application's setting is " + currentCulture + ".");
                                }
                            }

                            if (initialLoad)
                            {
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
                            }
                    },
                    #endregion // VHF Version and Admin Labels
                () =>
                {
//                    #region Links
//#if DEBUG
//                    System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
//                    sw.Start();
//#endif
//                    Query selectQuery = Database.CreateQuery("SELECT * FROM [metaLinks] WHERE [ToViewId] = @ToViewId ORDER BY [ToRecordGuid] ASC, [LastContactDate] DESC");
//                    selectQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, ContactFormId));
//                    linksTable = Database.Select(selectQuery);
//#if DEBUG
//                    sw.Stop();
//                    System.Diagnostics.Debug.Print("Meta links SELECT query: " + sw.Elapsed.TotalMilliseconds.ToString());
//#endif
//                    #endregion // Links
                },
                () =>
                {
                    #region District and Sub-county clearer
                    if (initialLoad)
                    {
//#if DEBUG
//                        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
//                        sw.Start();
//#endif
                        // TODO: See if this can finally be removed; note that this may require updating the base templates... which for EN-US assumes Uganda, therefore you wind up with 900+ items in your district table.
                        //try
                        //{
                        //    // get rid of district / subcounty lists that came in from base templates; if the district and SC fields are text fields then this is obviously the case
                        //    IField districtField = CaseForm.Fields["DistrictRes"];
                        //    IField subCountyField = CaseForm.Fields["SCRes"];
                        //    if (districtField != null && (districtField is DDLFieldOfLegalValues || districtField is DDLFieldOfCodes))
                        //    {
                        //        // set up district list
                        //        Query selectQuery = Database.CreateQuery("SELECT * FROM [codeDistrictSubCountyList] ORDER BY DISTRICT, SUBCOUNTIES");
                        //        DataTable districtsTable = Database.Select(selectQuery);

                        //        foreach (DataRow row in districtsTable.Rows)
                        //        {
                        //            lock (_locationCollectionLock)
                        //            {
                        //                string district = row["DISTRICT"].ToString().Trim();

                        //                if (!Districts.Contains(district))
                        //                {
                        //                    Districts.Add(district);
                        //                }

                        //                if (subCountyField != null && (subCountyField is DDLFieldOfLegalValues || subCountyField is DDLFieldOfCodes))
                        //                {
                        //                    string subCounty = row["SUBCOUNTIES"].ToString().Trim();
                        //                    if (!SubCounties.Contains(subCounty))
                        //                    {
                        //                        SubCounties.Add(subCounty);

                        //                        if (!DistrictsSubCounties.ContainsKey(district))
                        //                        {
                        //                            DistrictsSubCounties.Add(district, new List<string>());
                        //                        }
                        //                        DistrictsSubCounties[district].Add(subCounty);
                        //                    }
                        //                }
                        //            }
                        //        }
                        //    }
                        //}
                        //catch (Exception)
                        //{
                        //    // do nothing
                        //}

                        // Countries

                        //IField countryField = CaseForm.Fields["CountryRes"];
                        //if (countryField != null && (countryField is DDLFieldOfLegalValues || countryField is DDLFieldOfCodes))
                        //{
                        //    // set up district list
                        //    Query selectQuery = Database.CreateQuery("SELECT * FROM [codeCountryList] ORDER BY COUNTRY");
                        //    DataTable countryTable = Database.Select(selectQuery);

                        //    foreach (DataRow row in countryTable.Rows)
                        //    {
                        //        lock (_locationCollectionLock)
                        //        {
                        //            string country = row["COUNTRY"].ToString().Trim();

                        //            if (!Countries.Contains(country))
                        //            {
                        //                Countries.Add(country);
                        //            }
                        //        }
                        //    }
                        //}

//#if DEBUG
//                        sw.Stop();
//                        System.Diagnostics.Debug.Print("District filler query: " + sw.Elapsed.TotalMilliseconds.ToString());
//#endif

                    }
                    #endregion // District and Sub-county clearer
                }
                );

                #endregion // Parallel Load

                TaskbarProgressValue = TaskbarProgressValue + 0.4;
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                ErrorMessage = "";//String.Format(Properties.Resources.ErrorInitialLoadSqlException, initialLoad.ToString(), System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(), Database.FullName);
                ErrorMessageDetail = ex.Message + "\n\n" + ex.StackTrace;
                IsShowingError = true;
                return false;
            }
            catch (AggregateException ex)
            {
                ErrorMessage = "";// String.Format(Properties.Resources.ErrorInitialLoadExceptions, initialLoad.ToString(), System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
                int errorIndex = 1;
                foreach (Exception innerEx in ex.InnerExceptions)
                {
                    if (ex.InnerExceptions.Count > 1)
                    {
                        ErrorMessageDetail = ErrorMessageDetail + errorIndex + ": " + innerEx.Message + "\n\n";
                    }
                    else
                    {
                        ErrorMessageDetail = ErrorMessageDetail + innerEx.Message + "\n\n" + ex.StackTrace;
                    }
                }

                ErrorMessageDetail = ErrorMessageDetail.TrimEnd('\n');
                IsShowingError = true;
                return false;
            }
            catch (Exception ex)
            {
                ErrorMessage = "";//String.Format(Properties.Resources.ErrorInitialLoadException, initialLoad.ToString(), System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
                ErrorMessageDetail = ex.Message + "\n\n" + ex.StackTrace;
                IsShowingError = true;
                return false;
            }
            finally
            {
            }

            LoadStatus = "Finished.";

            TaskbarProgressValue = TaskbarProgressValue + 0.1;

            return true;
        }

        private void CheckVersioning()
        {
            System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
            Version thisVersion = a.GetName().Version;
            string thisVersionString = thisVersion.ToString();

            IsUsingOutdatedVersion = false;
            IsUsingDeprecatedVersion = false;

            string[] dbVersion = VhfDbVersion.Split('.');
            if (dbVersion.Length == 4)
            {
                int dbMajor = int.Parse(dbVersion[0]);
                int dbMinor = int.Parse(dbVersion[1]);
                int dbBuild = int.Parse(dbVersion[2]);
                int dbRevision = int.Parse(dbVersion[3]);

                bool shouldUpdateDbVersion = false;

                if (dbMajor > thisVersion.Major)
                {
                    IsUsingOutdatedVersion = true;
                    IsUsingDeprecatedVersion = true;
                }
                else if (dbMajor == thisVersion.Major)
                {
                    if (dbMinor > thisVersion.Minor)
                    {
                        IsUsingOutdatedVersion = true;
                        IsUsingDeprecatedVersion = true;
                    }
                    else if (dbMinor == thisVersion.Minor)
                    {
                        if (dbBuild > thisVersion.Build)
                        {
                            IsUsingOutdatedVersion = true;
                            IsUsingDeprecatedVersion = true;
                        }
                        else if (dbBuild == thisVersion.Build)
                        {
                            if (dbRevision > thisVersion.Revision + 12)
                            {
                                IsUsingOutdatedVersion = true;
                                IsUsingDeprecatedVersion = true;
                            }
                            else if (dbRevision > thisVersion.Revision)
                            {
                                IsUsingOutdatedVersion = true;
                            }
                            

                            if (dbRevision < thisVersion.Revision)
                            {
                                shouldUpdateDbVersion = true;
                            }
                        }
                        else
                        {
                            shouldUpdateDbVersion = true;
                        }
                    }
                    else
                    {
                        shouldUpdateDbVersion = true;
                    }
                }
                else
                {
                    shouldUpdateDbVersion = true;
                }

                if (shouldUpdateDbVersion)
                {
                    Query updateQuery = Database.CreateQuery("UPDATE metaDbInfo SET VhfVersion = @Version");
                    updateQuery.Parameters.Add(new QueryParameter("@Version", DbType.String, thisVersionString));
                    Database.ExecuteNonQuery(updateQuery);
                }
            }
        }

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
            if (Database != null)
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

        /// <summary>
        /// Finds the MAC address of the NIC with maximum speed.
        /// </summary>
        /// <returns>The MAC address</returns>
        private string GetMacAddress()
        {
            const int MIN_MAC_ADDR_LENGTH = 12;
            string macAddress = string.Empty;
            long maxSpeed = -1;

            foreach (System.Net.NetworkInformation.NetworkInterface nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                string tempMac = nic.GetPhysicalAddress().ToString();
                if (nic.Speed > maxSpeed &&
                    !string.IsNullOrEmpty(tempMac) &&
                    tempMac.Length >= MIN_MAC_ADDR_LENGTH)
                {
                    maxSpeed = nic.Speed;
                    macAddress = tempMac;
                }
            }

            return macAddress;
        }

        /// <summary>
        /// Executes a self-registration event. This occurs when an end-user sends a special 
        /// self-register code to the server
        /// </summary>
        /// <remarks>
        /// Self-registration eliminates the need for one person to sit in front of a server all
        /// day long entering SMS phone numbers for the possibly hundreds of contact tracing staff
        /// that are being hired
        /// </remarks>
        /// <param name="phoneNumber">The phone number of the person attempting to register their phone</param>
        /// <returns>bool; whether the self-register was successful</returns>
        public bool ExecuteSmsSelfRegister(string phoneNumber)
        {
            bool registered = SmsModule.ExecuteSmsSelfRegister(phoneNumber);

            try
            {
                if (registered)
                {
                    this.SaveSettings();
                }
            }
            catch (Exception)
            {
                return false;
            }

            return registered;
        }

        /// <summary>
        /// Executes a SQL query based on a valid SMS message
        /// </summary>
        /// <param name="updateType">The type of update</param>
        /// <param name="message">The SMS message</param>
        /// <returns>bool; whether the execution was successful</returns>
        public bool ExecuteSmsUpdate(ShortMessage message)
        {
            #region Input Validation

            if (message == null)
            {
                throw new ArgumentNullException("Message object cannot be null");
            }

            if (String.IsNullOrEmpty(message.Message))
            {
                throw new InvalidOperationException("SMS message cannot be empty");
            }

            string[] parts = message.Message.Split(' ');

            if (parts.Length != 3)
            {
                throw new InvalidOperationException("SMS message must contain three parts.");
            }

            int updateType;
            bool success = int.TryParse(parts[0], out updateType);

            if (updateType < 1 || updateType > 3)
            {
                throw new InvalidOperationException("SMS message update type is invalid. Valid values are 1, 2, and 3");
            }

            #endregion // Input Validation

            bool operationWasSuccessful = true;

            int id;
            success = int.TryParse(parts[1], out id);

            if (success)
            {
                if (updateType == 1)
                {
                    #region Case Data Update
                    // do nothing right now
                    operationWasSuccessful = false;
                    #endregion Case Data Update
                }
                else if (updateType == 2)
                {
                    #region Daily Follow-up Update
                    int status;
                    success = int.TryParse(parts[2], out status);

                    if (status >= 0 && status <= 7)
                    {
                        // get the database
                        IDbDriver db = this.CollectedData.GetDatabase();

                        // get the global record ID for this contact
                        Query selectQuery = db.CreateQuery("SELECT GlobalRecordId FROM ContactEntryForm WHERE UniqueKey = @Id");
                        selectQuery.Parameters.Add(new QueryParameter("@Id", System.Data.DbType.Int32, id));
                        DataTable dt = db.Select(selectQuery);
                        string guid = dt.Rows[0][0].ToString();

                        // calculate the window so we know which day column to update
                        selectQuery = db.CreateQuery("SELECT LastContactDate FROM metaLinks WHERE " +
                            "ToRecordGuid = @ToRecordGuid AND " +
                            "FromViewId = @FromViewId AND " +
                            "ToViewId = @ToViewId");
                        selectQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", System.Data.DbType.String, guid));
                        selectQuery.Parameters.Add(new QueryParameter("@FromViewId", System.Data.DbType.Int32, CaseFormId));
                        selectQuery.Parameters.Add(new QueryParameter("@ToViewId", System.Data.DbType.Int32, ContactFormId));
                        dt = db.Select(selectQuery);
                        DateTime lastContactDate = (DateTime)(dt.Rows[0][0]);
                        DateTime today = DateTime.Today;
                        TimeSpan ts = today - lastContactDate;

                        int day = (int)ts.TotalDays; // this should never be a decimal since all dates stored should not have time componenets

                        // update the right row in metaLinks
                        Query updateQuery = db.CreateQuery("UPDATE metaLinks SET Day" + day.ToString() + " = @Status WHERE " +
                            "ToRecordGuid = @ToRecordGuid AND " +
                            "FromViewId = @FromViewId AND " +
                            "ToViewId = @ToViewId");
                        updateQuery.Parameters.Add(new QueryParameter("@Status", DbType.Byte, status));
                        updateQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", System.Data.DbType.String, guid));
                        updateQuery.Parameters.Add(new QueryParameter("@FromViewId", System.Data.DbType.Int32, CaseFormId));
                        updateQuery.Parameters.Add(new QueryParameter("@ToViewId", System.Data.DbType.Int32, ContactFormId));
                        int records = db.ExecuteNonQuery(updateQuery);

                        if (records == 1)
                        {
                            operationWasSuccessful = true;
                        }
                        else
                        {
                            operationWasSuccessful = false;
                        }

                        // add a changeset message
                        if (db.ToString().ToLower().Contains("sql"))
                        {
                            System.Guid changesetGuid = System.Guid.NewGuid();
                            string changesetGuidString = changesetGuid.ToString();
                            DateTime now = DateTime.Now;

                            using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(db.ConnectionString + ";Connection Timeout=10"))
                            {
                                conn.Open();

                                System.Data.SqlClient.SqlCommand insertCommand = new System.Data.SqlClient.SqlCommand("INSERT INTO Changesets (ChangesetID, UpdateType, UserID, MACADDR, Description, DestinationRecordID, CheckinDate) VALUES (" +
                                    "@ChangesetID, @UpdateType, @UserID, @MACADDR, @Description, @DestinationRecordID, @CheckinDate)", conn);

                                insertCommand.Parameters.Add("@ChangesetID", SqlDbType.NVarChar).Value = changesetGuidString;
                                insertCommand.Parameters.Add("@UpdateType", SqlDbType.Int).Value = 15;
                                insertCommand.Parameters.Add("@UserID", SqlDbType.NVarChar).Value = "SMS server";
                                insertCommand.Parameters.Add("@MACADDR", SqlDbType.NVarChar).Value = "SMS";
                                insertCommand.Parameters.Add("@Description", SqlDbType.NVarChar).Value = "SMS contact status update";
                                insertCommand.Parameters.Add("@DestinationRecordID", SqlDbType.NVarChar).Value = guid;
                                insertCommand.Parameters.Add("@CheckinDate", SqlDbType.DateTime2).Value = now;

                                records = insertCommand.ExecuteNonQuery();

                                if (records == 1)
                                {
                                    success = true;
                                }
                            }
                        }
                    }
                    #endregion // Daily Follow-up Update
                }
                else if (updateType == 3)
                {
                    #region Lab Data Update
                    // do nothing right now
                    operationWasSuccessful = false;
                    #endregion Lab Data Update
                }
            }

            return operationWasSuccessful;
        }

        /// <summary>
        /// Saves project settings stored in the PRJ file (Xml)
        /// </summary>
        public void SaveSettings()
        {
            // todo: Add other settings here; so far not necessary to update anything other than the Sms module, though

            XDocument doc = XDocument.Load(this.FullName);

            XElement smsModule = doc.Descendants("SmsModule").FirstOrDefault();
            if (smsModule != null)
            {
                XElement sendsReadReceipts = smsModule.Element("SendsReadReceipts");
                sendsReadReceipts.Value = this.SmsModule.SendsReadReceipts.ToString();

                XElement allowsSelfRegistration = smsModule.Element("AllowsSelfRegistration");
                allowsSelfRegistration.Value = this.SmsModule.AllowsSelfRegistration.ToString();

                XElement selfRegistrationCode = smsModule.Element("SelfRegistrationCode");
                selfRegistrationCode.Value = this.SmsModule.SelfRegistrationCode;

                XElement commands = smsModule.Element("StartupCommands");
                commands.Value = this.SmsModule.StartupCommands;

                XElement pollRate = smsModule.Element("PollRate");
                if (pollRate != null)
                {
                    pollRate.Value = this.SmsModule.PollRate.ToString();
                }

                XElement senders = smsModule.Element("AuthorizedSmsSenders");
                senders.Value = String.Empty;

                // todo: move this into SmsModule
                foreach (SmsSenderInfo senderInfo in SmsModule.AuthorizedSmsSenders)
                {
                    senders.Add(
                        new XElement("AuthorizedSmsSender", 
                            new XElement("Name", senderInfo.Name),
                            new XElement("PhoneNumber", senderInfo.PhoneNumber),
                            new XElement("CanAddUpdateCases", senderInfo.CanAddUpdateCases.ToString()),
                            new XElement("CanAddUpdateContacts", senderInfo.CanAddUpdateContacts.ToString()),
                            new XElement("CanAddUpdateLabResults", senderInfo.CanAddUpdateLabResults.ToString())
                            ));
                }
            }
            else
            {
                doc.Element("Project").Add(
                    new XElement("SmsModule",
                        new XElement("SendsReadReceipts", SmsModule.SendsReadReceipts),
                        new XElement("AllowsSelfRegistration", SmsModule.AllowsSelfRegistration),
                        new XElement("SelfRegistrationCode", SmsModule.SelfRegistrationCode),
                        new XElement("StartupCommands", SmsModule.StartupCommands),
                        new XElement("AuthorizedSmsSenders")));
            }

            doc.Save(this.FullName);
        }

        /// <summary>
        /// Checks to see if a given phone number is an authorized sender
        /// </summary>
        /// <param name="phoneNumber">The phone number to validate</param>
        /// <param name="updateType">The kind of update the sender is attempting</param>
        /// <returns>bool; whether this phone number is an authorized sender</returns>
        public bool IsUserAuthorized(string phoneNumber, int updateType)
        {
            return SmsModule.IsUserAuthorized(phoneNumber, updateType);
        }

        /// <summary>
        /// Checks to see if a given phone number is in the list of authorized numbers
        /// </summary>
        /// <param name="phoneNumber">The phone number to validate</param>
        /// <returns>bool; whether this phone number is in the list of senders</returns>
        /// <remarks>Be careful using this method because it will return true if a number is in the list, and
        /// does not check for the type of update being requested. This method is intended to identify people, e.g.
        /// to see if a sender is recognized or not; not necessarily to tell if the sender is authorized.</remarks>
        public bool IsUserInList(string phoneNumber)
        {
            return SmsModule.IsUserInList(phoneNumber);
        }
        #endregion // Methods
    }
}
