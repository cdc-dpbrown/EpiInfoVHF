using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using System.Xml.Linq;
using ContactTracing.Core;

namespace ContactTracing.Core.Data
{
    public class ProjectInfo : ObservableObject, IDisposable
    {

        #region Members
        private string _connectionString = String.Empty;
        private string _inputConnectionString = String.Empty;
        private string _outbreakName = String.Empty;
        private DateTime? _outbreakDetectionDate = null;
        private bool _isVHF = false;
        public string culture = String.Empty;
        private bool _isConnected = true;
        private bool _isCheckingConnection = false;
        private bool _isShowingConnectionEditor = false;
        private bool _isServerBasedStorage = false;
        private bool _isLocalStorage = false;
        //public Timer _updateTimer = new Timer(4000);
        private string _region;
        private string _reg_culture;


        public string Reg_culture
        {
            get { return _reg_culture; }
            set { _reg_culture = value; }
        }


        public string Region
        {
            get { return _region; }
            set { _region = value; }
        }
        private string _culture;

        public string Culture1
        {
            get { return _culture; }
            set { _culture = value; }
        }

        #endregion // Members

        #region Constructor
        public ProjectInfo(string fileName, string outbreakName, string connectionString, bool isVHF, DateTime? outbreakDate, string culture)
        {
            this.FileInfo = new FileInfo(fileName);
            this.OutbreakName = outbreakName;
            this.ConnectionString = connectionString;
            this.IsVHF = isVHF;
            this.OutbreakDetectionDate = outbreakDate;
            this.Culture = culture;
            this.IsShowingConnectionEditor = false;
            this.IsCheckingConnection = false;


            CheckForDbConnectivityAsync();
        }
        #endregion // Constructor

        public override string ToString()
        {
            return OutbreakName;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                //this.UpdateTimer.Stop();
                //this.UpdateTimer.Elapsed -= UpdateTimer_Elapsed;
                //this.UpdateTimer.Dispose();
            }
            // free native resources if there are any.
        }

        public ICommand CopyProjectCommand { get { return new RelayCommand(CopyProjectCommandExecute); } }
        protected void CopyProjectCommandExecute()
        {
            bool exists = false;
            int iterations = 1;
            do
            {
                string proposedFileName = this.FileInfo.FullName.Replace(".prj", ".Copy" + iterations.ToString() + ".prj");
                exists = System.IO.File.Exists(proposedFileName);
                if (!exists)
                {
                    System.IO.File.Copy(this.FileInfo.FullName, proposedFileName);

                    try
                    {
                        XDocument doc = XDocument.Load(proposedFileName);
                        XElement root = doc.Root;
                        if (root != null)
                        {
                            XElement element = root.Element("OutbreakName");
                            if (element != null)
                            {
                                element.Value = element.Value + " (Copy " + iterations.ToString() + ")";
                                doc.Save(proposedFileName);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // do nothing, this failure isn't a major problem
                    }
                }
                else
                {
                    iterations++;
                }
            }
            while (exists == true);
        }

        public ICommand ChangeConnectionInformationCommand { get { return new RelayCommand(ChangeConnectionInformationCommandExecute); } }
        protected void ChangeConnectionInformationCommandExecute()
        {
            IsShowingConnectionEditor = true;
        }

        public ICommand CloseConnectionInformationCommand { get { return new RelayCommand(CloseConnectionInformationCommandExecute); } }
        protected void CloseConnectionInformationCommandExecute()
        {
            IsShowingConnectionEditor = false;
        }

        public ICommand SaveConnectionInformationCommand { get { return new RelayCommand(SaveConnectionInformationCommandExecute); } }
        protected void SaveConnectionInformationCommandExecute()
        {
            bool exists = System.IO.File.Exists(this.FileInfo.FullName);
            if (exists)
            {
                XDocument doc = XDocument.Load(this.FileInfo.FullName);
                XElement root = doc.Root;
                if (root != null)
                {
                    XElement cdElement = root.Element("CollectedData");
                    if (cdElement != null)
                    {
                        XElement dbElement = cdElement.Element("Database");
                        if (dbElement != null && dbElement.FirstAttribute != null && dbElement.LastAttribute != null)
                        {
                            XAttribute connectionStringAttribute = dbElement.FirstAttribute;
                            XAttribute dataDriverAttribute = dbElement.LastAttribute;

                            if (IsServerBasedStorage)
                            {
                                dataDriverAttribute.Value = "Epi.Data.SqlServer.SqlDBFactory, Epi.Data.SqlServer";
                            }
                            else if (IsLocalStorage)
                            {
                                dataDriverAttribute.Value = "Epi.Data.Office.AccessDBFactory, Epi.Data.Office";
                            }

                            connectionStringAttribute.Value = Epi.Configuration.Encrypt(InputConnectionString);

                            doc.Save(this.FileInfo.FullName);

                            ConnectionString = InputConnectionString;
                        }
                    }

                    IsShowingConnectionEditor = false;
                    CheckForDbConnectivityAsync();
                }
            }
        }

        private async void CheckForDbConnectivityAsync()
        {
            if (IsShowingConnectionEditor == true || IsCheckingConnection == true) // don't check for connectivity while the editor is open or if we're already checking
            {
                return;
            }

            IsCheckingConnection = true;

            await Task.Factory.StartNew(delegate
            {
                Epi.Project project = null;

                try
                {
                    if (!System.IO.File.Exists(this.FileInfo.FullName))
                    {
                        IsConnected = false;
                        IsCheckingConnection = false;
                        return;
                    }

                    project = new Epi.Project(this.FileInfo.FullName);
                    Epi.Data.IDbDriver db = project.CollectedData.GetDatabase();
                    if (db != null)
                    {
                        ConnectionString = db.ConnectionString;
                        InputConnectionString = db.ConnectionString;

                        if (db is Epi.Data.Office.OleDbDatabase)
                        {
                            IsLocalStorage = true;
                            IsServerBasedStorage = false;
                        }
                        else
                        {
                            IsLocalStorage = false;
                            IsServerBasedStorage = true;
                        }
                    }
                    //if (project.CollectedData.GetDatabase() is Epi.Data.Office.OleDbDatabase)
                    //{
                    //    string dataSource = project.CollectedData.DataSource;
                    //    dataSource = dataSource.Replace("data source=", String.Empty);
                    //    bool fileExists = System.IO.File.Exists(dataSource);
                    //    if (!fileExists)
                    //    {
                    //        IsConnected = false;
                    //        return;
                    //    }
                    //}

                    Epi.View view = project.Views["CaseInformationForm"];
                    IsConnected = true;
                }
                catch (Epi.GeneralException)
                {
                    IsConnected = false;
                }
                catch (System.Security.Cryptography.CryptographicException)
                {
                    // keys don't match
                    IsConnected = false;
                }
                catch (Exception)
                {
                    IsConnected = false;
                }
                finally
                {
                    if (project != null)
                    {
                        project.Dispose();
                    }

                    IsCheckingConnection = false;
                }
            });
        }

        public bool IsExistingProject { get; set; }
        public bool IsNewProject
        {
            get
            {
                return !IsExistingProject;
            }
        }

        public Guid Id { get; set; }

        public string Name { get; set; }
        public string Name2 { get { return "name2"; } }
        public bool IsConnected
        {
            get
            {
                return this._isConnected;
            }
            set
            {
                if (IsConnected != value)
                {
                    this._isConnected = value;
                    RaisePropertyChanged("IsConnected");
                }
            }
        }

        public bool IsCheckingConnection
        {
            get
            {
                return this._isCheckingConnection;
            }
            set
            {
                if (IsCheckingConnection != value)
                {
                    this._isCheckingConnection = value;
                    RaisePropertyChanged("IsCheckingConnection");
                }
            }
        }

        public bool IsShowingConnectionEditor
        {
            get
            {
                return this._isShowingConnectionEditor;
            }
            set
            {
                if (IsShowingConnectionEditor != value)
                {
                    this._isShowingConnectionEditor = value;
                    RaisePropertyChanged("IsShowingConnectionEditor");
                }
            }
        }

        public bool IsLocalStorage
        {
            get
            {
                return this._isLocalStorage;
            }
            set
            {
                if (IsLocalStorage != value)
                {
                    this._isLocalStorage = value;
                    RaisePropertyChanged("IsLocalStorage");
                }
            }
        }

        public bool IsServerBasedStorage
        {
            get
            {
                return this._isServerBasedStorage;
            }
            set
            {
                if (IsServerBasedStorage != value)
                {
                    this._isServerBasedStorage = value;
                    RaisePropertyChanged("IsServerBasedStorage");
                }
            }
        }

        public string Culture { get; set; }

        public FileInfo FileInfo { get; set; }

        public string ConnectionString
        {
            get
            {
                return this._connectionString;
            }
            set
            {
                this._connectionString = value;
                RaisePropertyChanged("ConnectionString");
            }
        }

        public string InputConnectionString
        {
            get
            {
                return this._inputConnectionString;
            }
            set
            {
                if (InputConnectionString != value)
                {
                    this._inputConnectionString = value;
                    RaisePropertyChanged("InputConnectionString");
                }
            }
        }

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

        public DateTime? OutbreakDetectionDate
        {
            get
            {
                return this._outbreakDetectionDate;
            }
            set
            {
                this._outbreakDetectionDate = value;
                RaisePropertyChanged("OutbreakDetectionDate");
            }
        }

        public bool IsVHF
        {
            get
            {
                return this._isVHF;
            }
            set
            {
                this._isVHF = value;
                //RaisePropertyChanged("IsVHF");
            }
        }
    }
}
