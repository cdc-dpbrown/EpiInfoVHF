using System;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Linq;
using System.Xml.Serialization;
using Epi;
using Epi.Data;
using ContactTracing.Core.Data;

namespace ContactTracing.Core
{
    [Serializable()]
    public sealed class VhfProject : Project, INotifyPropertyChanged
    {
        #region Members
        private string _outbreakName = String.Empty;
        #endregion // Members

        #region Properties

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
        public VhfProject()
            : base()
        {
            MacAddress = Core.Common.GetMacAddress();
        }

        /// <summary>
        /// Constructor with file path
        /// </summary>
        /// <param name="filePath">The file path to the location of the PRJ file</param>
        public VhfProject(string filePath)
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

            MacAddress = Core.Common.GetMacAddress();
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

            bool connected = db.TestConnection();

            if (connected)
            {
                Query selectQuery = db.CreateQuery("SELECT * FROM [metaViews]");
                DataTable dt = db.Select(selectQuery);

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
                }
            }
            else
            {
                throw new InvalidOperationException("No connection to database detected.");
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
