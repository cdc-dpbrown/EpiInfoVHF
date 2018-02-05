using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows.Input;
using System.Xml.Linq;
using Epi;
using Epi.Data;
using ContactTracing.Core;
using ContactTracing.Core.Data;
using ContactTracing.ViewModel;

namespace ContactTracing.Sms
{
    /// <summary>
    /// The viewmodel for the Sms server
    /// </summary>
    public sealed class SmsViewModel : ObservableObject
    {
        #region Members
        private string _outbreakName = "SMS Server";
        private bool _isConnectedToDatabase = false;
        private bool _isConnectedToModem = false;
        private bool _isShowingSettings = false;
        private bool _isShowingErrorPanel = false;
        private bool _isShowingCommandConsole = false;
        private string _errorMessage = String.Empty;
        private VhfProject _project = null;
        private ObservableCollection<SmsStatusMessage> _statusMessages = new ObservableCollection<SmsStatusMessage>();
        private object _statusMessagesLock = new object();
        #endregion // Members

        #region Properties

        /// <summary>
        /// Gets/sets the SmsController associated with the viewmodel
        /// </summary>
        private SmsController SmsController { get; set; }

        /// <summary>
        /// Gets/sets whether or not the settings should be shown to the user
        /// </summary>
        public bool IsShowingSettings
        {
            get { return this._isShowingSettings; }
            set
            {
                if (IsShowingSettings != value)
                {
                    _isShowingSettings = value;
                    RaisePropertyChanged("IsShowingSettings");
                }
            }
        }

        /// <summary>
        /// Gets/sets whether or not the settings should be shown to the user
        /// </summary>
        public bool IsShowingErrorPanel
        {
            get { return this._isShowingErrorPanel; }
            private set
            {
                if (IsShowingErrorPanel != value)
                {
                    _isShowingErrorPanel = value;
                    RaisePropertyChanged("IsShowingErrorPanel");

                    if (IsShowingErrorPanel == false)
                    {
                        ErrorMessage = String.Empty;
                    }
                }
            }
        }

        /// <summary>
        /// Gets/sets whether or not the command console should be shown to the user
        /// </summary>
        public bool IsShowingCommandConsole
        {
            get { return this._isShowingCommandConsole; }
            private set
            {
                if (IsShowingCommandConsole != value)
                {
                    _isShowingCommandConsole = value;
                    RaisePropertyChanged("IsShowingCommandConsole");
                }
            }
        }

        /// <summary>
        /// Gets/sets an error message for a generic UI-based error message panel
        /// </summary>
        public string ErrorMessage
        {
            get { return this._errorMessage; }
            set
            {
                if (ErrorMessage != value)
                {
                    this._errorMessage = value;
                    RaisePropertyChanged("ErrorMessage");
                }
            }
        }

        /// <summary>
        /// Gets/sets the name of the outbreak database
        /// </summary>
        public string OutbreakName
        {
            get { return this._outbreakName; }
            set
            {
                if (OutbreakName != value)
                {
                    this._outbreakName = value;
                    RaisePropertyChanged("OutbreakName");
                }
            }
        }

        /// <summary>
        /// Gets/sets whether the app is connected to a database
        /// </summary>
        public bool IsConnectedToDatabase
        {
            get { return this._isConnectedToDatabase; }
            set
            {
                if (IsConnectedToDatabase != value)
                {
                    this._isConnectedToDatabase = value;
                    RaisePropertyChanged("IsConnectedToDatabase");
                }
            }
        }

        /// <summary>
        /// Gets/sets whether the app is connected to an SMS modem
        /// </summary>
        public bool IsConnectedToModem
        {
            get { return this._isConnectedToModem; }
            set
            {
                if (IsConnectedToModem != value)
                {
                    this._isConnectedToModem = value;
                    RaisePropertyChanged("IsConnectedToModem");
                }
            }
        }

        /// <summary>
        /// Gets the collection of status messages
        /// </summary>
        /// <remarks>
        /// This collection is intended to contain only status messages about what 
        /// actions the server has taken for incoming/outgoing SMS, e.g. to display 
        /// on a UI or to write a log file
        /// </remarks>
        public ObservableCollection<SmsStatusMessage> StatusMessages { get { return this._statusMessages; } }

        /// <summary>
        /// A list of results from issuing diagnostic AT commands directly to the modem
        /// </summary>
        public ObservableCollection<string> DiagnosticCommandResults { get; private set; }

        /// <summary>
        /// Gets/sets the Epi Info 7 project file that points to the outbreak database
        /// </summary>
        public VhfProject Project
        {
            get { return this._project; }
            set
            {
                if (Project != value)
                {
                    this._project = value;
                    RaisePropertyChanged("Project");

                    if (Project != null)
                    {
                        IsConnectedToDatabase = true;
                    }
                    else
                    {
                        IsConnectedToDatabase = false;
                    }
                }
            }
        }
        #endregion // Properties

        #region Constructors
        /// <summary>
        /// Primary constructor
        /// </summary>
        public SmsViewModel()
        {
            LoadConfig();
            System.Windows.Data.BindingOperations.EnableCollectionSynchronization(StatusMessages, _statusMessagesLock);
            
            OutbreakName = "Not connected to a database";
            IsConnectedToDatabase = false;
            IsConnectedToModem = false;
            IsShowingSettings = false;

            DiagnosticCommandResults = new ObservableCollection<string>();
        }
        #endregion // Constructors

        #region Methods
        /// <summary>
        /// Adds a status message
        /// </summary>
        /// <param name="statusMessageText">The text of the status message</param>
        /// <param name="phoneNumber">The phone number associated with the SMS that this status message describes</param>
        /// <remarks>
        /// For example, if the server receives a message from phone number 555-454-3455 updating contact C-0830 to 'seen
        /// and healthy' then we would assume the status message collection would eventually contain a message stating that,
        /// so that anyone reading those status messages could see that such an action had taken place.
        /// </remarks>
        private void AddStatusMessage(string statusMessageText, string phoneNumber)
        {
            SmsStatusMessage message = new SmsStatusMessage();
            message.StatusMessage = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + " " + phoneNumber + " : " + statusMessageText;
            lock (_statusMessagesLock)
            {
                StatusMessages.Add(message);
                Epi.Logger.Log(message.StatusMessage);
            }
        }

        /// <summary>
        /// Loads the Epi Info 7 config file; this is required for any Epi Info 7 interop to work correctly (e.g. to
        /// instantiate the Epi Info 7 Project class)
        /// </summary>
        /// <returns>bool; whether the configuration load was successful</returns>
        private bool LoadConfig()
        {
            string configFilePath = Configuration.DefaultConfigurationPath;
            bool configurationOk = true;
            try
            {
                string directoryName = System.IO.Path.GetDirectoryName(configFilePath);
                if (!System.IO.Directory.Exists(directoryName))
                {
                    System.IO.Directory.CreateDirectory(directoryName);
                }

                if (!System.IO.File.Exists(configFilePath))
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
        /// Handles the reception of an SMS message
        /// </summary>
        /// <param name="sender">The object that fired the event</param>
        /// <param name="e">The event arguments</param>
        private void SmsController_SmsReceived(object sender, Core.Events.SmsReceivedArgs e)
        {
            string[] parts = e.Message.Message.Split(' ');
            if (parts.Length == 1 && parts[0].Equals(Project.SmsModule.SelfRegistrationCode) && Project.SmsModule.AllowsSelfRegistration)
            {
                // self-register message received
                bool registered = Project.ExecuteSmsSelfRegister(e.Message.Sender);
                if (registered)
                {
                    SmsController.SendSMS(e.Message.Sender, "Your phone has been registered to send messages.");
                    AddStatusMessage("Self-registration successful", e.Message.Sender);
                }
                else
                {
                    SmsController.SendSMS(e.Message.Sender, "Registration failed. Your phone may already be registered, or self-registration may be disabled.");
                    AddStatusMessage("Self-registration failed", e.Message.Sender);
                }
            }
            else if (parts.Length == 3)
            {
                int updateType;
                bool success = int.TryParse(parts[0], out updateType);

                if (success)
                {
                    if (Project.IsUserAuthorized(e.Message.Sender, updateType))
                    {
                        AddStatusMessage(e.Message.Message, e.Message.Sender);
                    }
                    else
                    {
                        #region Rejection
                        AddStatusMessage("Unauthorized message was received and discarded: " + e.Message.Message, e.Message.Sender);

                        if (Project.SmsModule.SendsReadReceipts == true)
                        {
                            try
                            {
                                SmsController.SendSMS(e.Message.Sender, "Message was rejected. Reason: Unauthorized");
                            }
                            catch (Exception ex)
                            {
                                AddStatusMessage("An exception was thrown while sending message rejection message to sender " + e.Message.Sender + ". Exception: " + ex.Message, "[SERVER]");

                                // just in case this is causing a problem when the port fails, close/dispose and re-open
                                SmsController.SmsReceived -= SmsController_SmsReceived;
                                SmsController.Dispose();

                                SmsController = new SmsController(SmsController.ModemConfiguration, Project.SmsModule.PollRate);
                                SmsController.SmsReceived += SmsController_SmsReceived;
                            }
                        }

                        return;
                        #endregion // Rejection
                    }

                    bool operationWasSuccessful = true;

                    try 
                    {
                        operationWasSuccessful = Project.ExecuteSmsUpdate(e.Message);
                    }
                    catch (Exception ex)
                    {
                        operationWasSuccessful = false;
                        AddStatusMessage("An exception was thrown while updating contact follow-up information. Exception: " + ex.Message, "[SERVER]");
                    }

                    if (operationWasSuccessful && Project.SmsModule.SendsReadReceipts == true)
                    {
                        // send confirmation message
                        try
                        {
                            SmsController.SendSMS(e.Message.Sender, "Message received");
                        }
                        catch (Exception ex)
                        {
                            AddStatusMessage("An exception was thrown while sending message received message to sender " + e.Message.Sender + ". Exception: " + ex.Message, "[SERVER]");
                            
                            // just in case this is causing a problem when the port fails, close/dispose and re-open
                            SmsController.SmsReceived -= SmsController_SmsReceived;
                            SmsController.Dispose();

                            SmsController = new SmsController(SmsController.ModemConfiguration, Project.SmsModule.PollRate);
                            SmsController.SmsReceived += SmsController_SmsReceived;
                        }
                    }
                    else if (!operationWasSuccessful && Project.SmsModule.SendsReadReceipts == true)
                    {
                        #region Rejection
                        AddStatusMessage("Malformed message was discarded: " + e.Message.Message, e.Message.Sender);
                        if (Project.SmsModule.SendsReadReceipts == true)
                        {
                            try
                            {
                                SmsController.SendSMS(e.Message.Sender, "Message was rejected. Reason: Incorrect format");
                            }
                            catch (Exception ex)
                            {
                                AddStatusMessage("An exception was thrown while sending message rejection message to sender " + e.Message.Sender + ". Exception: " + ex.Message, "[SERVER]");

                                // just in case this is causing a problem when the port fails, close/dispose and re-open
                                SmsController.SmsReceived -= SmsController_SmsReceived;
                                SmsController.Dispose();

                                SmsController = new SmsController(SmsController.ModemConfiguration, Project.SmsModule.PollRate);
                                SmsController.SmsReceived += SmsController_SmsReceived;
                            }
                        }
                        #endregion // Rejection
                    }
                }
            }
            else
            {
                AddStatusMessage("Malformed message was discarded: " + e.Message.Message, e.Message.Sender);
                if (Project.SmsModule.SendsReadReceipts == true) 
                {
                    try
                    {
                        SmsController.SendSMS(e.Message.Sender, "Message was rejected. Reason: Incorrect format");
                    }
                    catch (Exception ex)
                    {
                        AddStatusMessage("An exception was thrown while sending message rejection message to sender " + e.Message.Sender + ". Exception: " + ex.Message, "[SERVER]");

                        // just in case this is causing a problem when the port fails, close/dispose and re-open
                        SmsController.SmsReceived -= SmsController_SmsReceived;
                        SmsController.Dispose();

                        SmsController = new SmsController(SmsController.ModemConfiguration, Project.SmsModule.PollRate);
                        SmsController.SmsReceived += SmsController_SmsReceived;
                    }
                }
            }
        }
        #endregion // Methods

        #region Commands
        /// <summary>
        /// Issues an AT command directly to the modem; should be used for diagnostic purposes only
        /// </summary>
        public ICommand IssueATCommand { get { return new RelayCommand<string>(IssueATCommandExecute); } }
        private void IssueATCommandExecute(string command)
        {
            DiagnosticCommandResults.Add(command);

            try
            {
                string result = SmsController.ExecCommand(command, 700, "VHF ERROR MESSAGE");
                string displayableResult = result.TrimStart('\r').TrimStart('\n').TrimEnd('\n').TrimEnd('\r');
                displayableResult = displayableResult.Replace("\r\n\r\nOK", "\nOK");
                DiagnosticCommandResults.Add(displayableResult);
                AddStatusMessage(String.Format("Diagnostic command issued: {0}", command), "Server");
            }
            catch (Exception ex)
            {
                DiagnosticCommandResults.Add(String.Format("Server generated an exception: {0}", ex.Message));

                // re-start the controller
                SmsController.SmsReceived -= SmsController_SmsReceived;
                SmsController.Dispose();

                SmsController = new SmsController(SmsController.ModemConfiguration, Project.SmsModule.PollRate);
                SmsController.SmsReceived += SmsController_SmsReceived;
            }
        }

        /// <summary>
        /// Clears the collection of diagnostic command results
        /// </summary>
        public ICommand ClearConsoleCommand { get { return new RelayCommand(ClearConsoleCommandExecute); } }
        private void ClearConsoleCommandExecute()
        {
            DiagnosticCommandResults.Clear();
        }

        /// <summary>
        /// Connects to an Epi Info 7 project and the database the project points to
        /// </summary>
        public ICommand ConnectDatabaseCommand { get { return new RelayCommand<ProjectInfo>(ConnectDatabaseCommandExecute); } }
        private void ConnectDatabaseCommandExecute(ProjectInfo projectInfo)
        {
            LoadConfig(); // load the EI7 config, which is required before we can instantiate the Project class from Epi.Core
            Project = new VhfProject(projectInfo.FileInfo.FullName);
            OutbreakName = projectInfo.OutbreakName;

            AddStatusMessage(String.Format("Connected to database {0}", projectInfo.FileInfo.FullName), "Server");
        }

        /// <summary>
        /// Connects to a valid SMS modem on the specified COM port
        /// </summary>
        public ICommand ConnectModemCommand { get { return new RelayCommand<ModemConfigInfo>(ConnectModemCommandExecute); } }
        private void ConnectModemCommandExecute(ModemConfigInfo modemInfo)
        {
            if (Project.SmsModule != null && Project.SmsModule.StartupCommands != null && !String.IsNullOrEmpty(Project.SmsModule.StartupCommands))
            {
                string[] commands = Project.SmsModule.StartupCommands.Split('\n');
                foreach (string command in commands)
                {
                    modemInfo.StartupCommands.Add(command);
                }
            }

            // save the most recent settings to the server's config for faster re-use later
            Properties.Settings.Default.LastUsedBaudRate = int.Parse(modemInfo.BaudRate);
            Properties.Settings.Default.LastUsedComPort = modemInfo.ComPort;
            Properties.Settings.Default.Save();

            try
            {
                SmsController = new SmsController(modemInfo, Project.SmsModule.PollRate);
                IsConnectedToModem = true;

                AddStatusMessage(String.Format("Connected to modem on {0}", modemInfo.ComPort), "Server");

                SmsController.SmsReceived += SmsController_SmsReceived;
            }
            catch (Exception ex)
            {
                ShowErrorBoxCommand.Execute(String.Format("Could not connect to the modem. Exception: {0}", ex.Message));
                IsConnectedToModem = false;
            }
        }

        /// <summary>
        /// Shows the error display panel with a given message
        /// </summary>
        public ICommand ShowErrorBoxCommand { get { return new RelayCommand<string>(ShowErrorBoxCommandExecute); } }
        private void ShowErrorBoxCommandExecute(string message)
        {
            ErrorMessage = message;
            IsShowingErrorPanel = true;
        }

        /// <summary>
        /// Hides the error display panel
        /// </summary>
        public ICommand HideErrorBoxCommand { get { return new RelayCommand(HideErrorBoxCommandExecute); } }
        private void HideErrorBoxCommandExecute()
        {
            IsShowingErrorPanel = false;
        }

        /// <summary>
        /// Toggles the display of the settings menu
        /// </summary>
        public ICommand ToggleSettingsCommand { get { return new RelayCommand<bool>(ToggleSettingsCommandExecute); } }
        private void ToggleSettingsCommandExecute(bool turnOn)
        {
            IsShowingSettings = turnOn;
        }

        /// <summary>
        /// Toggles the display of the AT command console
        /// </summary>
        public ICommand ToggleConsoleCommand { get { return new RelayCommand(ToggleConsoleCommandExecute); } }
        private void ToggleConsoleCommandExecute()
        {
            IsShowingCommandConsole = !IsShowingCommandConsole;
        }

        /// <summary>
        /// Saves project settings
        /// </summary>
        public ICommand SaveSettingsCommand { get { return new RelayCommand(SaveSettingsCommandExecute); } }
        private void SaveSettingsCommandExecute()
        {
            IsShowingSettings = false;
            Project.SaveSettings();
        }

        /// <summary>
        /// Adds an SMS sender to the list of authorized senders
        /// </summary>
        public ICommand AddAuthorizedSenderCommand { get { return new RelayCommand<string>(AddAuthorizedSenderCommandExecute); } }
        private void AddAuthorizedSenderCommandExecute(string number)
        {
            Project.SmsModule.AuthorizedSmsSenders.Add(new SmsSenderInfo(number));
        }

        /// <summary>
        /// Disconnects from both the database and the modem
        /// </summary>
        public ICommand DisconnectCommand { get { return new RelayCommand(DisconnectCommandExecute); } }
        private void DisconnectCommandExecute()
        {
            OutbreakName = "Not connected to a database";

            Project.Dispose();
            Project = null;

            SmsController.Dispose();
            SmsController = null;

            StatusMessages.Clear();
            DiagnosticCommandResults.Clear();

            IsConnectedToDatabase = false;
            IsConnectedToModem = false;
        }
        #endregion // Commands
    }
}
