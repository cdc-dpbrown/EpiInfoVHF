using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Epi;
using Epi.Data;
using Epi.ImportExport;
using Epi.ImportExport.Filters;
using Epi.ImportExport.ProjectPackagers;
using ContactTracing.Core;

namespace ContactTracing.CaseView
{
    public sealed class SqlToMdbCopierViewModel : ObservableObject
    {
        #region Members
        private readonly VhfProject _project;
        private readonly string _country;
        private readonly string _appCulture;
        private readonly string _outbreakName;
        private readonly DateTime _outbreakDate;
        private readonly View _caseForm;
        private bool _isDisplaying = false;
        private bool _isCopying = false;
        private bool _isShowingCopyProgress = false;
        private double _progressValue = 0.0;
        private double _maxProgressBarValue = 100.0;
        private string _copyStatus = String.Empty;
        #endregion // Members

        #region Properties
        public string Country { get { return _country; } }
        public string AppCulture { get { return _appCulture; } }
        public bool IsDisplaying { get { return _isDisplaying; } set { _isDisplaying = value; RaisePropertyChanged("IsDisplaying"); } }
        public bool IsCopying { get { return _isCopying; } set { _isCopying = value; RaisePropertyChanged("IsCopying"); } }
        public bool IsShowingCopyProgress { get { return _isShowingCopyProgress; } set { _isShowingCopyProgress = value; RaisePropertyChanged("IsShowingCopyProgress"); } }

        public string CopyStatus
        {
            get
            {
                return this._copyStatus;
            }
            set
            {
                this._copyStatus = value;
                RaisePropertyChanged("CopyStatus");
            }
        }

        public double MaxProgressValue
        {
            get
            {
                return this._maxProgressBarValue;
            }
            set
            {
                if (this.MaxProgressValue != value)
                {
                    this._maxProgressBarValue = value;
                    RaisePropertyChanged("MaxProgressValue");
                }
            }
        }

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
        #endregion // Properties

        #region Constructors

        public SqlToMdbCopierViewModel(VhfProject project, string country, string applicationCulture, string outbreakName, DateTime outbreakDate)
        {
            _project = project;
            _country = country;
            RaisePropertyChanged("Country");

            _caseForm = _project.Views[Core.Constants.CASE_FORM_NAME];

            _appCulture = applicationCulture;
            RaisePropertyChanged("AppCulture");

            _outbreakName = outbreakName;
            _outbreakDate = outbreakDate;

            IsShowingCopyProgress = false;
            IsDisplaying = false;
            IsCopying = false;
        }

        #endregion // Constructors

        #region Methods
        #endregion // Methods

        #region Commands

        public ICommand CreateMDBCommand { get { return new RelayCommand(CreateMDBCommandExecute, CanExecuteCreateMDBCommand); } }
        private void CreateMDBCommandExecute()
        {
            if (IsCopying) return;

            IsCopying = true;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            bool hasError = false;
            IsShowingCopyProgress = true;
            string guid = Guid.NewGuid().ToString();
            string fileName = _project.FileName.Replace(".prj", String.Empty);

            Task.Factory.StartNew(
                () =>
                {
                    Project newProject = ContactTracing.ImportExport.ImportExportHelper.CreateNewOutbreak(Country,
                        _appCulture, @"Projects\VHF\" + fileName + "_" + guid + ".prj",
                        @"Projects\VHF\" + fileName + "_" + guid + ".mdb",
                        _outbreakDate.Ticks.ToString(),
                        _outbreakName);

                    ContactTracing.ImportExport.FormCopier formCopier = new ImportExport.FormCopier(_project, newProject, _caseForm);

                    formCopier.SetProgressBar += formCopier_SetProgressBar;
                    formCopier.SetMaxProgressBarValue += formCopier_SetMaxProgressBarValue;

                    try
                    {
                        formCopier.Copy();
                    }
                    catch (Exception ex)
                    {
                        hasError = true;
                        System.Windows.Forms.MessageBox.Show(ex.Message, "Exception", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                    }
                    finally
                    {
                        formCopier.SetProgressBar -= formCopier_SetProgressBar;
                        formCopier.SetMaxProgressBarValue -= formCopier_SetMaxProgressBarValue;
                    }
                },
                System.Threading.CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).ContinueWith(
                    delegate
                    {
                        ProgressValue = 0;

                        IsCopying = false;

                        sw.Stop();

                        if (hasError)
                        {
                            CopyStatus = "Copying halted due to error.";

                            System.IO.File.Delete(@"Projects\VHF\" + fileName + "_" + guid + ".prj");
                            System.IO.File.Delete(@"Projects\VHF\" + fileName + "_" + guid + ".mdb");
                        }
                        else
                        {
                            CopyStatus = "Finished copying data to mdb file. Elapsed time: " + sw.Elapsed.TotalMinutes.ToString("F1") + " minutes.";
                        }

                    }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        void formCopier_SetStatus(string message)
        {
            CopyStatus = message;
        }

        void formCopier_SetMaxProgressBarValue(double maxProgress)
        {
            MaxProgressValue = maxProgress;
        }

        void formCopier_SetProgressBar(double progress)
        {
            ProgressValue = progress;
            CopyStatus = String.Format("{0} of {1} entities copied...", ProgressValue.ToString(), MaxProgressValue);
        }

        private bool CanExecuteCreateMDBCommand()
        {
            return true;
        }

        public ICommand CancelCommand { get { return new RelayCommand(CancelCommandExecute, CanExecuteCancelCommand); } }
        private void CancelCommandExecute()
        {
            IsDisplaying = false;
        }

        private bool CanExecuteCancelCommand()
        {
            if (IsCopying) return false;
            return true;
        }

        public ICommand StopCopyCommand { get { return new RelayCommand(StopCopyCommandExecute, CanExecuteStopCopyCommand); } }
        private void StopCopyCommandExecute()
        {
            IsShowingCopyProgress = false;
        }

        private bool CanExecuteStopCopyCommand()
        {
            if (IsCopying) return false;
            return true;
        }
        #endregion // Commands
    }
}
