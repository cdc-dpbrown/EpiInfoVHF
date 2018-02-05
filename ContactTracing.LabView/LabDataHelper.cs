using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Collections.Specialized;
using System.Text;
using System.Windows.Input;
using Epi;
using Epi.Data;
using Epi.Fields;
using ContactTracing.Core;
using ContactTracing.ViewModel;
using ContactTracing.ViewModel.Events;

namespace ContactTracing.LabView
{
    public class LabDataHelper : DataHelperBase
    {
        #region Members
        
        //private readonly FileInfo PROJECT_FILE_INFO = new FileInfo(@"Projects\UgandaVHFOutbreakLabDatabase\UgandaVHFOutbreakLabDatabase.prj");
        private int CaseFormId = -1;
        private int ContactFormId = -1;

        

        private string _searchSamplesText = String.Empty;
        #endregion // Members

        #region Properties
        public ICollectionView LabResultCollectionView { get; set; }

        public string SearchSamplesText
        {
            get
            {
                return this._searchSamplesText;
            }
            set
            {
                if (this._searchSamplesText != value)
                {
                    this._searchSamplesText = value;
                    RaisePropertyChanged("SearchSamplesText");
                }
            }
        }

        

        
        #endregion // Properties

        #region Constructors
        public LabDataHelper()
        {
            LoadConfig();
            TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
        }
        #endregion // Constructors

        #region Commands
        //public ICommand UpdateCase { get { return new RelayCommand<Case>(UpdateCaseExecute); } }
        //void UpdateCaseExecute(Case updatedCase)
        //{
        //    if (CaseCollection == null)
        //        return;

        //    foreach (var iCase in CaseCollection)
        //    {
        //        if (iCase.ID == updatedCase.ID)
        //        {
        //            iCase.Update.Execute(updatedCase);
        //            break;
        //        }
        //    }
        //}

        //public ICommand UpdateOrAddCase { get { return new RelayCommand<string>(UpdateOrAddCaseExecute); } }
        //void UpdateOrAddCaseExecute(string caseGuid)
        //{
        //    if (CaseCollection == null)
        //        return;

        //    Case c = CreateCaseFromGuid(caseGuid);
        //    CaseViewModel newCaseVM = null;
        //    bool found = false;
        //    foreach (var iCase in CaseCollection)
        //    {
        //        if (iCase.RecordId == caseGuid)
        //        {
        //            newCaseVM = iCase;
        //            iCase.Update.Execute(c);
        //            found = true;
        //            break;
        //        }
        //    }

        //    if (!found)
        //    {
        //        AddCase.Execute(c);
        //    }
        //}

        //public ICommand AddCase { get { return new RelayCommand<Case>(AddCaseExecute); } }
        //void AddCaseExecute(Case newCase)
        //{
        //    if (CaseCollection == null)
        //        return;

        //    CaseViewModel newCaseVM = new CaseViewModel { Case = newCase };
        //    CaseCollection.Add(newCaseVM);
        //}

        //public ICommand DeleteCase { get { return new RelayCommand<string>(DeleteCaseExecute); } }
        //void DeleteCaseExecute(string caseGuid)
        //{
        //    if (CaseCollection == null)
        //        return;

        //    CaseViewModel cvm = null;
        //    for (int i = CaseCollection.Count - 1; i >= 0; i--)
        //    {
        //        cvm = CaseCollection[i];
        //        if (cvm.RecordId == caseGuid)
        //        {
        //            CaseCollection.Remove(cvm);
        //            break;
        //        }
        //    }

        //    Query deleteQuery = Database.CreateQuery("DELETE * FROM [" + CaseForm.TableName + "] WHERE [GlobalRecordId] = '" + caseGuid + "'");
        //    Database.ExecuteNonQuery(deleteQuery);

        //    foreach (Epi.Page page in CaseForm.Pages)
        //    {
        //        deleteQuery = Database.CreateQuery("DELETE * FROM [" + page.TableName + "] WHERE [GlobalRecordId] = '" + caseGuid + "'");
        //        Database.ExecuteNonQuery(deleteQuery);
        //    }
        //}
        #endregion // Commands

        public delegate void InitialSetupRunHandler(object sender, EventArgs e);
        public delegate void CaseDataPopulatedHandler(object sender, CaseDataPopulatedArgs e);
        public delegate void LabRecordAddedHandler(object sender, CaseDataPopulatedArgs e);

        public event InitialSetupRunHandler InitialSetupRun;
        public event CaseDataPopulatedHandler CaseDataPopulated;
        public event LabRecordAddedHandler LabRecordAdded;
                
        public CaseViewModel GetCaseVM(string recordId)
        {
            foreach (LabResultViewModel labVM in this.LabResultCollection)
            {
                if (labVM.CaseID == recordId)
                {
                    return labVM.CaseVM;
                }
            }

            return null;
        }

        public void InitializeProject(VhfProject project)
        {
            if (LoadConfig())
            {
                this.CaseForm = project.Views[ContactTracing.Core.Constants.LAB_CASE_FORM_NAME];
                LabForm = project.Views[ContactTracing.Core.Constants.LAB_RESULTS_FORM_NAME];
                this.Project = project;

                Database = Project.CollectedData.GetDatabase();

                Query selectQuery = Database.CreateQuery("SELECT * FROM [metaViews]");
                DataTable dt = Database.Select(selectQuery);

                foreach (DataRow row in dt.Rows)
                {
                    if (row["Name"].ToString().Equals(ContactTracing.Core.Constants.LAB_CASE_FORM_NAME))
                    {
                        CaseFormId = int.Parse(row["ViewId"].ToString());
                    }
                    else if (row["Name"].ToString().Equals(ContactTracing.Core.Constants.LAB_RESULTS_FORM_NAME))
                    {
                        ContactFormId = int.Parse(row["ViewId"].ToString());
                    }
                }

                if (CaseFormId == -1 || ContactFormId == -1)
                {
                    throw new ApplicationException("The database is corrupt. The application cannot run.");
                }
            }
        }

        public ICommand RepopulateCollectionsCommand { get { return new RelayCommand<bool>(RepopulateCollections); } }

        protected override void SortCases()
        {
            ObservableCollection<LabResultViewModel> tempResults = new ObservableCollection<LabResultViewModel>();

            foreach (LabResultViewModel labVM in LabResultCollection)
            {
                tempResults.Add(labVM);
            }

            var query = from labVM in tempResults
                        orderby labVM.CaseID
                        select labVM;

            LabResultCollection.Clear();

            foreach (var labVM in query)
            {
                LabResultCollection.Add(labVM);
            }
        }

        public ICommand SearchSamples { get { return new RelayCommand(SearchSamplesExecute); } }
        private void SearchSamplesExecute()
        {
            string searchString = SearchSamplesText.Trim().ToLower();

            if (!String.IsNullOrEmpty(searchString))
            {
                LabResultCollectionView.Filter = new Predicate<object>
                    (
                        resultVM =>
                            ((LabResultViewModel)resultVM).Surname.ToLower().Contains(searchString) ||
                            ((LabResultViewModel)resultVM).OtherNames.ToLower().Contains(searchString) ||
                            ((LabResultViewModel)resultVM).CaseID.ToLower().Contains(searchString) ||
                            ((LabResultViewModel)resultVM).ResultNumber.ToString().Equals(searchString) ||
                            ((LabResultViewModel)resultVM).FieldLabSpecimenID.ToLower().Contains(searchString) ||
                            ((LabResultViewModel)resultVM).UVRIVSPBLogNumber.ToLower().Contains(searchString) ||
                            ((LabResultViewModel)resultVM).Village.ToLower().Contains(searchString) ||
                            ((LabResultViewModel)resultVM).District.ToLower().Contains(searchString) ||
                            ((LabResultViewModel)resultVM).SampleType.ToLower().Contains(searchString) ||
                            ((LabResultViewModel)resultVM).SampleInterpretation.ToLower().Contains(searchString) ||
                            ((LabResultViewModel)resultVM).FinalLabClassification.ToLower().Contains(searchString)
                    );
            }
            else
            {
                LabResultCollectionView.Filter = delegate(object item)
                {
                    return true;
                };
            }
        }

        private void LoadCaseData(DataRow row, CaseViewModel c)
        {
            c.Surname = row["Surname"].ToString();
            c.ID = row["ID"].ToString();
            c.RecordId = row["GlobalRecordId"].ToString();
            c.OtherNames = row["OtherNames"].ToString();

            if (!string.IsNullOrEmpty(row["Age"].ToString()))
            {
                c.Age = double.Parse(row["Age"].ToString());
            }
            if (!string.IsNullOrEmpty(row["DateOnset"].ToString()))
            {
                c.DateOnset = DateTime.Parse(row["DateOnset"].ToString());
            }

            c.Sex = row["Gender"].ToString();
            //string genderCode = row["Gender"].ToString();
            //switch (genderCode)
            //{
            //    case "1":
            //        c.Gender = "Male";
            //        break;
            //    case "2":
            //        c.Gender = "Female";
            //        break;
            //    default:
            //        c.Gender = String.Empty;
            //        break;
            //}

            //c.FinalLabClass = ConvertFinalLabClassificationCode(row["FinalLabClass"].ToString());
            c.FinalLabClassification = row["FinalLabClass"].ToString();
            
            c.District = row["DistrictRes"].ToString();
            c.Village = row["VillageRes"].ToString();
            c.UniqueKey = Convert.ToInt32(row["UniqueKey"]);

            c.IsContact = false;
        }

        private CaseViewModel CreateCaseFromGuid(string guid)
        {
            CaseViewModel c = new CaseViewModel(CaseForm, LabForm);
            string queryText = "SELECT t.GlobalRecordId, ID, Surname, OtherNames, Age, Gender, " +
                "DateOnset, DateDeath, " +
                "FinalLabClass, DistrictRes, VillageRes, UniqueKey " +
                CaseForm.FromViewSQL + " " +
                "WHERE [t.GlobalRecordId] = @GlobalRecordId";

            if (Database.ToString().ToLower().Contains("sql"))
            {
                queryText = "SELECT t.GlobalRecordId, ID, Surname, OtherNames, Age, Gender, " +
                "DateOnset, DateDeath, " +
                "FinalLabClass, DistrictRes, VillageRes, UniqueKey " +
                CaseForm.FromViewSQL + " " +
                "WHERE t.GlobalRecordId = @GlobalRecordId";
            }

            Query selectQuery = Database.CreateQuery(queryText);
            selectQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, guid));
            DataTable dt = Database.Select(selectQuery);

            if (dt.Rows.Count == 1)
            {
                DataRow row = dt.Rows[0];
                LoadCaseData(row, c);
            }
            return c;
        }

        public override void ClearCollections()
        {
            //CaseCollection = new ObservableCollection<CaseViewModel>();
            LabResultCollection.Clear();// = new ObservableCollection<LabResultViewModel>();
        }

        /// <summary>
        /// Used to repopulate all collections. This is an expensive process and should only be called when absolutely necessary.
        /// </summary>
        public override void RepopulateCollections(bool initialLoad = false)
        {
            SetupDatabase();
            ClearCollections();
            PopulateCollections();
        }

        protected override void RunInitialSetup(bool showSetupScreen)
        {
            if (InitialSetupRun != null)
            {
                InitialSetupRun(this, new EventArgs());
            }
        }

        protected override bool PopulateCollections(bool initialLoad = false)
        {
            LabDataHelper.SampleInterpretConfirmedAcute = Properties.Resources.AnalysisClassConfirmedAcute;
            LabDataHelper.SampleInterpretConfirmedConvalescent = Properties.Resources.AnalysisClassConfirmedConvalescent;
            LabDataHelper.SampleInterpretNotCase = Properties.Resources.AnalysisClassNotCase;
            LabDataHelper.SampleInterpretIndeterminate = Properties.Resources.AnalysisClassIndeterminate;
            LabDataHelper.SampleInterpretNegativeNeedsFollowUp = Properties.Resources.AnalysisClassNegativeNeedsFollowUp;

            LabDataHelper.PCRPositive = Properties.Resources.Positive;
            LabDataHelper.PCRNegative = Properties.Resources.Negative;
            LabDataHelper.PCRIndeterminate = Properties.Resources.AnalysisClassIndeterminate;
            LabDataHelper.PCRNotAvailable = "n/a";

            LabDataHelper.SampleTypeWholeBlood = Properties.Resources.SampleTypeWholeBlood;
            LabDataHelper.SampleTypeSerum = Properties.Resources.SampleTypeSerum;
            LabDataHelper.SampleTypeHeartBlood = Properties.Resources.SampleTypeHeartBlood;
            LabDataHelper.SampleTypeSkin = Properties.Resources.SampleTypeSkin;
            LabDataHelper.SampleTypeOther = Properties.Resources.SampleTypeOther;

            // *********************************************************************************
            // Following code shows virus tests depending on what's in the database.
            // *********************************************************************************
            DataTable CaseTable = GetCasesTable();
            DataView CaseView = new DataView(CaseTable, String.Empty, String.Empty, DataViewRowState.CurrentRows);

            DataTable LabTable = GetLabTable();
            DataView LabView = new DataView(LabTable, String.Empty, String.Empty, DataViewRowState.CurrentRows);

            SudanTestsDetected = false;
            EbolaTestsDetected = false;
            BundibugyoTestsDetected = false;
            MarburgTestsDetected = false;
            CCHFTestsDetected = false;
            RiftTestsDetected = false;
            LassaTestsDetected = false;

            foreach (DataRowView rowView in LabView)
            {
                LabResultViewModel labResultVM = new LabResultViewModel(LabForm);
                LoadResultData(rowView.Row, labResultVM, CaseView);
                LabResultCollection.Add(labResultVM);

                if (!String.IsNullOrEmpty(labResultVM.MARVPCR)) MarburgTestsDetected = true;
                if (!String.IsNullOrEmpty(labResultVM.EBOVPCR)) EbolaTestsDetected = true;
                if (!String.IsNullOrEmpty(labResultVM.BDBVPCR)) BundibugyoTestsDetected = true;
                if (!String.IsNullOrEmpty(labResultVM.SUDVPCR)) SudanTestsDetected = true;
                if (!String.IsNullOrEmpty(labResultVM.CCHFPCR)) CCHFTestsDetected = true;
                if (!String.IsNullOrEmpty(labResultVM.RVFPCR)) RiftTestsDetected = true;
                if (!String.IsNullOrEmpty(labResultVM.LHFPCR)) LassaTestsDetected = true;
            }

            // *********************************************************************************
            // Following (uncommented) code shows virus tests depending on what the user entered on the initial app setup
            // *********************************************************************************
            Query selectQuery = Database.CreateQuery("SELECT * FROM metaDbInfo");
            DataTable dt = Database.Select(selectQuery);

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
                    VirusTestType = Core.Enums.VirusTestTypes.Sudan;
                    SudanTestsDetected = true;
                    break;
                case "Ebola":
                    VirusTestType = Core.Enums.VirusTestTypes.Ebola;
                    EbolaTestsDetected = true;
                    break;
                case "Marburg":
                    VirusTestType = Core.Enums.VirusTestTypes.Marburg;
                    MarburgTestsDetected = true;
                    break;
                case "Bundibugyo":
                    VirusTestType = Core.Enums.VirusTestTypes.Bundibugyo;
                    BundibugyoTestsDetected = true;
                    break;
                case "CCHF":
                    VirusTestType = Core.Enums.VirusTestTypes.CCHF;
                    CCHFTestsDetected = true;
                    break;
                case "Rift":
                    VirusTestType = Core.Enums.VirusTestTypes.Rift;
                    RiftTestsDetected = true;
                    break;
                case "Lassa":
                    VirusTestType = Core.Enums.VirusTestTypes.Lassa;
                    LassaTestsDetected = true;
                    break;
            }

            SortCases();
            UpdateResultNumbers();

            LabResultCollectionView = System.Windows.Data.CollectionViewSource.GetDefaultView(LabResultCollection);
            RaisePropertyChanged("LabResultCollectionView");

            if (CaseDataPopulated != null)
            {
                CaseDataPopulated(this, new CaseDataPopulatedArgs(VirusTestType, false));
            }

            TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None;

            return true;
        }

        

        public bool ExportCasesWithLabData(string fileName, bool exportFull = false)
        {
            DataTable casesTable = new DataTable("casesTable");
            casesTable.CaseSensitive = true;
            casesTable = ContactTracing.Core.Common.JoinPageTables(Database, CaseForm);

            DataTable labTable = new DataTable("labTable");
            labTable.CaseSensitive = true;
            labTable = ContactTracing.Core.Common.JoinPageTables(Database, Project.Views["LaboratoryResultsForm"]);

            DataView dv = new DataView(labTable);
            foreach (DataRow row in casesTable.Rows)
            {
                string guid = row["GlobalRecordId"].ToString();
                dv.RowFilter = "FKEY = '" + guid + "'";
                int rowCount = 1;

                foreach (DataRowView rowView in dv)
                {
                    DataRow labRow = rowView.Row;
                    foreach (DataColumn dc in labTable.Columns)
                    {
                        if (dc.ColumnName.Equals("GlobalRecordId") ||
                            dc.ColumnName.Equals("FKEY") ||
                            dc.ColumnName.Equals("UniqueKey") ||
                            dc.ColumnName.ToLower().Equals("recstatus"))
                        {
                            continue;
                        }

                        if (!exportFull)
                        {
                            if (dc.ColumnName.Equals("SUDVNPCT") ||
                            dc.ColumnName.Equals("SUDVCT2") ||
                            dc.ColumnName.Equals("SUDVAgTiter") ||
                            dc.ColumnName.Equals("SUDVIgMTiter") ||
                            dc.ColumnName.Equals("SUDVIgGTiter") ||
                            dc.ColumnName.Equals("SUDVAgSumOD") ||
                            dc.ColumnName.Equals("SUDVIgMSumOD") ||
                            dc.ColumnName.Equals("SUDVIgGSumOD") ||

                            dc.ColumnName.Equals("EBOVCT1") ||
                            dc.ColumnName.Equals("EBOVCT2") ||
                            dc.ColumnName.Equals("EBOVAgTiter") ||
                            dc.ColumnName.Equals("EBOVIgMTiter") ||
                            dc.ColumnName.Equals("EBOVIgGTiter") ||
                            dc.ColumnName.Equals("EBOVAgSumOD") ||
                            dc.ColumnName.Equals("EBOVIgMSumOD") ||
                            dc.ColumnName.Equals("EBOVIgGSumOD") ||

                            dc.ColumnName.Equals("EBOVCT1") ||
                            dc.ColumnName.Equals("EBOVCT2") ||
                            dc.ColumnName.Equals("EBOVAgTiter") ||
                            dc.ColumnName.Equals("EBOVIgMTiter") ||
                            dc.ColumnName.Equals("EBOVIgGTiter") ||
                            dc.ColumnName.Equals("EBOVAgSumOD") ||
                            dc.ColumnName.Equals("EBOVIgMSumOD") ||
                            dc.ColumnName.Equals("EBOVIgGSumOD") ||

                            dc.ColumnName.Equals("CCHFCT1") ||
                            dc.ColumnName.Equals("CCHFCT2") ||
                            dc.ColumnName.Equals("CCHFAgTiter") ||
                            dc.ColumnName.Equals("CCHFIgMTiter") ||
                            dc.ColumnName.Equals("CCHFIgGTiter") ||
                            dc.ColumnName.Equals("CCHFAgSumOD") ||
                            dc.ColumnName.Equals("CCHFIgMSumOD") ||
                            dc.ColumnName.Equals("CCHFIgGSumOD") ||

                            dc.ColumnName.Equals("RVFCT1") ||
                            dc.ColumnName.Equals("RVFCT2") ||
                            dc.ColumnName.Equals("RVFAgTiter") ||
                            dc.ColumnName.Equals("RVFIgMTiter") ||
                            dc.ColumnName.Equals("RVFIgGTiter") ||
                            dc.ColumnName.Equals("RVFAgSumOD") ||
                            dc.ColumnName.Equals("RVFIgMSumOD") ||
                            dc.ColumnName.Equals("RVFIgGSumOD") ||

                            dc.ColumnName.Equals("LASCT1") ||
                            dc.ColumnName.Equals("LASCT2") ||
                            dc.ColumnName.Equals("LASAgTiter") ||
                            dc.ColumnName.Equals("LASIgMTiter") ||
                            dc.ColumnName.Equals("LASIgGTiter") ||
                            dc.ColumnName.Equals("LASAgSumOD") ||
                            dc.ColumnName.Equals("LASIgMSumOD") ||
                            dc.ColumnName.Equals("LASIgGSumOD") ||

                            dc.ColumnName.Equals("BDBVNPCT") ||
                            dc.ColumnName.Equals("BDBVVP40CT") ||
                            dc.ColumnName.Equals("BDBVAgTiter") ||
                            dc.ColumnName.Equals("BDBVIgMTiter") ||
                            dc.ColumnName.Equals("BDBVIgGTiter") ||
                            dc.ColumnName.Equals("BDBVAgSumOD") ||
                            dc.ColumnName.Equals("BDBVIgMSumOD") ||
                            dc.ColumnName.Equals("BDBVIgGSumOD") ||

                            dc.ColumnName.Equals("MARVPolCT") ||
                            dc.ColumnName.Equals("MARVVP40CT") ||
                            dc.ColumnName.Equals("MARVAgTiter") ||
                            dc.ColumnName.Equals("MARVIgMTiter") ||
                            dc.ColumnName.Equals("MARVIgGTiter") ||
                            dc.ColumnName.Equals("MARVAgSumOD") ||
                            dc.ColumnName.Equals("MARVIgMSumOD") ||
                            dc.ColumnName.Equals("MARVIgGSumOD"))
                            {
                                continue;
                            }
                        }

                        string newColumnName = dc.ColumnName + rowCount;
                        if (!casesTable.Columns.Contains(newColumnName))
                        {
                            casesTable.Columns.Add(new DataColumn(newColumnName, dc.DataType));
                        }
                        row[newColumnName] = labRow[dc.ColumnName];
                    }

                    rowCount++;
                }
            }

            bool exportResult = ExportView(casesTable.DefaultView, fileName);

            return exportResult;
        }

        private void AddResult(LabResultViewModel newResult)
        {
            if (LabResultCollection == null)
                return;

            LabResultCollection.Add(newResult);

            //int SUDV = 0;
            //int BDBV = 0;
            //int EBOV = 0;
            //int MARV = 0;

            //foreach (LabResultViewModel labResultVM in LabResultCollection)
            //{
            //    if (!String.IsNullOrEmpty(labResultVM.MARVPCR)) MARV++;
            //    if (!String.IsNullOrEmpty(labResultVM.EBOVPCR)) EBOV++;
            //    if (!String.IsNullOrEmpty(labResultVM.BDBVPCR)) BDBV++;
            //    if (!String.IsNullOrEmpty(labResultVM.SUDVPCR)) SUDV++;
            //}

            //VirusTestTypes virusTestType = Core.VirusTestTypes.Sudan;

            //if (SUDV > BDBV && SUDV > EBOV && SUDV > MARV)
            //{
            //    virusTestType = Core.VirusTestTypes.Sudan;
            //}

            //if (MARV > SUDV && MARV > EBOV && MARV > BDBV)
            //{
            //    virusTestType = Core.VirusTestTypes.Marburg;
            //}

            //if (EBOV > MARV && EBOV > SUDV && EBOV > BDBV)
            //{
            //    virusTestType = Core.VirusTestTypes.Ebola;
            //}

            //if (BDBV > MARV && BDBV > EBOV && BDBV > SUDV)
            //{
            //    virusTestType = Core.VirusTestTypes.Bundibugyo;
            //}

            if (LabRecordAdded != null)
            {
                LabRecordAdded(this, new CaseDataPopulatedArgs(VirusTestType, false));
            }
        }

        public List<string> GetLabGuidsForCaseGuid(string caseGuid)
        {
            List<string> guids = new List<string>();
            Query selectQuery = Database.CreateQuery("SELECT GlobalRecordId FROM " + LabForm.TableName + " WHERE FKEY = @FKEY");
            selectQuery.Parameters.Add(new QueryParameter("@FKEY", DbType.String, caseGuid));
            DataTable dt = Database.Select(selectQuery);

            foreach (DataRow row in dt.Rows)
            {
                guids.Add(row[0].ToString());
            }

            return guids;
        }

        public ICommand UpdateOrAddLabResult { get { return new RelayCommand<string>(UpdateOrAddLabResultExecute); } }
        void UpdateOrAddLabResultExecute(string labGuid)
        {
            if (LabResultCollection == null)
                return;

            LabResultViewModel newResultVM = CreateLabResultFromGuid(labGuid);
            if (newResultVM != null)
            {
                bool found = false;
                foreach (var iResult in LabResultCollection)
                {
                    if (iResult.RecordId == labGuid)
                    {
                        iResult.Copy.Execute(newResultVM);
                        RaisePropertyChanged("LabResultCollection");
                        found = true;

                        if (newResultVM.CaseID != newResultVM.LabCaseID)
                        {
                            // Cascade update all lab records with new ID
                            RenderableField idField = LabForm.Fields["ID"] as RenderableField;
                            if (idField != null)
                            {
                                Query updateQuery = Database.CreateQuery("UPDATE " + LabForm.TableName +
                                    " lf INNER JOIN " + idField.Page.TableName + " lf1 ON lf.GlobalRecordId = lf1.GlobalRecordId " +
                                    " SET [ID] = @ID " +
                                    " WHERE lf.FKEY = @FKEY");
                                updateQuery.Parameters.Add(new QueryParameter("@ID", DbType.String, newResultVM.CaseID));
                                updateQuery.Parameters.Add(new QueryParameter("@FKEY", DbType.String, newResultVM.CaseRecordGuid));
                                int rowsUpdated = Database.ExecuteNonQuery(updateQuery);
                                newResultVM.LabCaseID = newResultVM.CaseID;
                            }
                            else
                            {
                                throw new InvalidOperationException("Field ID is missing on case report form.");
                            }
                        }

                        break;
                    }
                }

                if (!found)
                {
                    AddResult(newResultVM);

                    RaisePropertyChanged("LabResultCollection");
                }

                SortCases();
                UpdateResultNumbers();
            }
        }

        private void UpdateResultNumbers()
        {
            var countQuery = from result in LabResultCollection
                             orderby result.DateSampleTested ascending
                             group result by result.CaseRecordGuid;

            foreach (var entry in countQuery)
            {
                int count = 1;
                foreach (LabResultViewModel resultVM in entry)
                {
                    resultVM.ResultNumber = count;
                    count++;
                }
            }
        }

        public ICommand DeleteLabResult { get { return new RelayCommand<LabResultViewModel>(DeleteLabResultExecute); } }
        void DeleteLabResultExecute(LabResultViewModel resultVM)
        {
            if (LabResultCollection == null)
                return;

            string caseGuid = resultVM.CaseRecordGuid;
            string labGuid = resultVM.RecordId;

            LabResultCollection.Remove(resultVM);

            Query deleteQuery = Database.CreateQuery("DELETE * FROM " + LabForm.TableName + " WHERE GlobalRecordId = @GUID");
            if (Database.ToString().ToLower().Contains("sql"))
            {
                deleteQuery = Database.CreateQuery("DELETE FROM " + LabForm.TableName + " WHERE GlobalRecordId = @GUID");
            }
            deleteQuery.Parameters.Add(new QueryParameter("@GUID", DbType.String, labGuid));
            Database.ExecuteNonQuery(deleteQuery);

            foreach (Epi.Page page in LabForm.Pages)
            {
                deleteQuery = Database.CreateQuery("DELETE * FROM " + page.TableName + " WHERE GlobalRecordId = @GUID");
                if (Database.ToString().ToLower().Contains("sql"))
                {
                    deleteQuery = Database.CreateQuery("DELETE  FROM " + page.TableName + " WHERE GlobalRecordId = @GUID");
                }
                deleteQuery.Parameters.Add(new QueryParameter("@GUID", DbType.String, labGuid));
                Database.ExecuteNonQuery(deleteQuery);
            }

            Query selectQuery = Database.CreateQuery("SELECT FKEY FROM " + LabForm.TableName + " WHERE FKEY = @GUID");
            selectQuery.Parameters.Add(new QueryParameter("@GUID", DbType.String, caseGuid));

            DataTable dt = Database.Select(selectQuery);

            if (dt.Rows.Count == 0)
            {
                deleteQuery = Database.CreateQuery("DELETE * FROM " + CaseForm.TableName + " WHERE GlobalRecordId = @GUID");
                if (Database.ToString().ToLower().Contains("sql"))
                {
                    deleteQuery = Database.CreateQuery("DELETE  FROM " + CaseForm.TableName + " WHERE GlobalRecordId = @GUID");
                }
                deleteQuery.Parameters.Add(new QueryParameter("@GUID", DbType.String, caseGuid));
                Database.ExecuteNonQuery(deleteQuery);

                foreach (Epi.Page page in CaseForm.Pages)
                {
                    deleteQuery = Database.CreateQuery("DELETE * FROM " + page.TableName + " WHERE GlobalRecordId = @GUID");
                    if (Database.ToString().ToLower().Contains("sql"))
                    {
                        deleteQuery = Database.CreateQuery("DELETE  FROM " + page.TableName + " WHERE GlobalRecordId = @GUID");
                    }
                    deleteQuery.Parameters.Add(new QueryParameter("@GUID", DbType.String, caseGuid));
                    Database.ExecuteNonQuery(deleteQuery);
                }
            }
        }
    }
}
