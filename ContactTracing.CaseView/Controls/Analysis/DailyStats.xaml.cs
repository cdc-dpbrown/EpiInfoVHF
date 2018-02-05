using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ContactTracing.ViewModel;
using Epi.Data;

namespace ContactTracing.CaseView.Controls.Analysis
{
    /// <summary>
    /// Interaction logic for DailyStats.xaml
    /// </summary>
    public partial class DailyStats : AnalysisOutputBase
    {
        public class Result
        {
            public string NewCasesT;
            public string NewDeathsT;
            public string IsoCurrentT;
            public string PendingT;

            public string NewCasesY;
            public string NewDeathsY;
            //public string IsoCurrentY;
            public string PendingY;
        }

        private delegate void SetGridTextHandler(Result result);

        private EpiDataHelper DataHelper
        {
            get
            {
                return (this.DataContext as EpiDataHelper);
            }
        }

        private int IsoCount { get; set; }

        public DailyStats()
        {
            InitializeComponent();
        }

        public void Compute()
        {
            DataHelper.SetDefaultIsolationViewFilter();

            tblockIsoCurrentCountT.Text = "...";
            tblockNewCasesCountT.Text = "...";
            tblockNewDeathsCountT.Text = "...";
            tblockPendingCountT.Text = "...";

            ListCollectionView lcv = DataHelper.IsolatedCollectionView as ListCollectionView;
            if (lcv != null)
            {
                IsoCount = lcv.Count;
            }
            else
            {
                throw new InvalidOperationException("DataHelper.IsolatedCollectionView cannot be null");
            }

            //tblockIsoCurrentCountY.Text = "...";
            tblockNewCasesCountY.Text = "...";
            tblockNewDeathsCountY.Text = "...";
            tblockPendingCountY.Text = "...";

            BackgroundWorker computeWorker = new BackgroundWorker();
            computeWorker.DoWork += new DoWorkEventHandler(computeWorker_DoWork);
            computeWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(computeWorker_RunWorkerCompleted);
            computeWorker.RunWorkerAsync(this.DataHelper);
        }

        void SetGridText(Result result)
        {
            tblockNewCasesCountT.Text = result.NewCasesT;
            tblockNewDeathsCountT.Text = result.NewDeathsT;
            tblockIsoCurrentCountT.Text = result.IsoCurrentT;
            tblockPendingCountT.Text = result.PendingT;

            tblockNewCasesCountY.Text = result.NewCasesY;
            tblockNewDeathsCountY.Text = result.NewDeathsY;
            //tblockIsoCurrentCountY.Text = result.IsoCurrentY;
            tblockPendingCountY.Text = result.PendingY;
        }

        void computeWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result != null)
            {
                Result result = e.Result as Result;
                if (result != null)
                {
                    this.Dispatcher.BeginInvoke(new SetGridTextHandler(SetGridText), result);
                }
            }
        }

        void computeWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Result result = new Result();
            EpiDataHelper DataHelper = e.Argument as EpiDataHelper;
            Epi.Fields.DateTimeField dtField = DataHelper.LabForm.Fields["DateSampleCollected"] as Epi.Fields.DateTimeField;
            if (DataHelper != null && dtField != null)
            {
                DateTime today = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

                int count = IsoCount; // (DataHelper.IsolatedCollectionView as ListCollectionView).Count;//.View.Cast<CaseViewModel>().Count();

                result.IsoCurrentT = count.ToString();

                count = (from caseVM in DataHelper.CaseCollection
                         where caseVM.DateDeathCurrentOrFinal.HasValue &&
                         (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed || caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable) &&
                         caseVM.DateDeathCurrentOrFinal.HasValue &&
                            caseVM.DateDeathCurrentOrFinal.Value.Day == today.Day &&
                            caseVM.DateDeathCurrentOrFinal.Value.Month == today.Month &&
                            caseVM.DateDeathCurrentOrFinal.Value.Year == today.Year
                         select caseVM).Count();

                result.NewDeathsT = count.ToString();

                count = (from caseVM in DataHelper.CaseCollection
                         where (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed || caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable || caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Suspect) &&
                         caseVM.DateReport.HasValue &&
                            caseVM.DateReport.Value.Day == today.Day &&
                            caseVM.DateReport.Value.Month == today.Month &&
                            caseVM.DateReport.Value.Year == today.Year
                         select caseVM).Count();

                result.NewCasesT = count.ToString();



                IDbDriver db = DataHelper.Project.CollectedData.GetDatabase();
                string queryText = "select count(*) FROM " + dtField.Page.TableName + " " +
                    "WHERE [DateSampleCollected] >= @Today";
                Query selectQuery = db.CreateQuery(queryText);
                selectQuery.Parameters.Add(new QueryParameter("@Today", System.Data.DbType.Date, DateTime.Today));
                count = (int)db.ExecuteScalar(selectQuery);

                result.PendingT = count.ToString();



                #region Yesterday
                DateTime yesterday = (DateTime.Today).AddDays(-1);
                count = (from caseVM in DataHelper.CaseCollection
                         where caseVM.DateDeathCurrentOrFinal.HasValue &&
                         (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed || caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable) &&
                         caseVM.DateDeathCurrentOrFinal.HasValue &&
                            caseVM.DateDeathCurrentOrFinal.Value.Day == yesterday.Day &&
                            caseVM.DateDeathCurrentOrFinal.Value.Month == yesterday.Month &&
                            caseVM.DateDeathCurrentOrFinal.Value.Year == yesterday.Year
                         select caseVM).Count();

                result.NewDeathsY = count.ToString();

                count = (from caseVM in DataHelper.CaseCollection
                         where (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed || caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable || caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Suspect) &&
                         caseVM.DateReport.HasValue &&
                            caseVM.DateReport.Value.Day == yesterday.Day &&
                            caseVM.DateReport.Value.Month == yesterday.Month &&
                            caseVM.DateReport.Value.Year == yesterday.Year
                         select caseVM).Count();

                result.NewCasesY = count.ToString();

                queryText = "select count(*) FROM " + dtField.Page.TableName + " " +
                    "WHERE [DateSampleCollected] < @Today AND [DateSampleCollected] >= @Yesterday";
                selectQuery = db.CreateQuery(queryText);
                selectQuery.Parameters.Add(new QueryParameter("@Today", System.Data.DbType.Date, DateTime.Today));
                selectQuery.Parameters.Add(new QueryParameter("@Yesterday", System.Data.DbType.Date, yesterday));

                count = (int)db.ExecuteScalar(selectQuery);

                result.PendingY = count.ToString();
                #endregion // Yesterday
                e.Result = result;
            }
        }
    }
}
