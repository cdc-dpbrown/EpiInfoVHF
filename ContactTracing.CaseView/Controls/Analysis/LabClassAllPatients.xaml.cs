using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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
using Epi;
using Epi.Data;

namespace ContactTracing.CaseView.Controls.Analysis
{
    /// <summary>
    /// Interaction logic for LabClassAllPatients.xaml
    /// </summary>
    public partial class LabClassAllPatients : AnalysisOutputBase
    {
        public class Result
        {
            public string ConfirmedAcuteCount;
            public string ConfirmedAcutePercent;

            public string ConfirmedConvalescentCount;
            public string ConfirmedConvalescentPercent;

            public string NegativeCount;
            public string NegativePercent;

            public string IndeterminateCount;
            public string IndeterminatePercent;

            public string NeedsFollowUpCount;
            public string NeedsFollowUpPercent;

            public string PendingCount;
            public string PendingPercent;

            public string NotSampledCount;
            public string NotSampledPercent;

            public string PendingIDs;
        }

        private delegate void SetGridTextHandler(Result result);

        private EpiDataHelper DataHelper
        {
            get
            {
                return (this.DataContext as EpiDataHelper);
            }
        }

        public LabClassAllPatients()
        {
            InitializeComponent();
        }

        public void Compute()
        {
            tblockConfirmedAcuteCount.Text = "...";
            tblockConfirmedAcutePercent.Text = "...";

            tblockConfirmedConvalescentCount.Text = "...";
            tblockConfirmedConvalescentPercent.Text = "...";

            tblockNegativeCount.Text = "...";
            tblockNegativePercent.Text = "...";

            tblockIndeterminateCount.Text = "...";
            tblockIndeterminateCount.Text = "...";

            tblockNeedsFollowUpCount.Text = "...";
            tblockNeedsFollowUpCount.Text = "...";

            tblockPendingCount.Text = "...";
            tblockPendingPercent.Text = "...";

            tblockNotSampledCount.Text = "...";
            tblockNotSampledPercent.Text = "...";

            BackgroundWorker computeWorker = new BackgroundWorker();
            computeWorker.DoWork += new DoWorkEventHandler(computeWorker_DoWork);
            computeWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(computeWorker_RunWorkerCompleted);
            computeWorker.RunWorkerAsync(this.DataHelper);
        }

        void SetGridText(Result result)
        {
            tblockConfirmedAcuteCount.Text = result.ConfirmedAcuteCount;
            tblockConfirmedAcutePercent.Text = result.ConfirmedAcutePercent;

            tblockConfirmedConvalescentCount.Text = result.ConfirmedConvalescentCount;
            tblockConfirmedConvalescentPercent.Text = result.ConfirmedConvalescentPercent;

            tblockNegativeCount.Text = result.NegativeCount;
            tblockNegativePercent.Text = result.NegativePercent;

            tblockIndeterminateCount.Text = result.IndeterminateCount;
            tblockIndeterminatePercent.Text = result.IndeterminatePercent;

            tblockNeedsFollowUpCount.Text = result.NeedsFollowUpCount;
            tblockNeedsFollowUpPercent.Text = result.NeedsFollowUpPercent;

            tblockPendingCount.Text = result.PendingCount;
            tblockPendingPercent.Text = result.PendingPercent;

            tblockNotSampledCount.Text = result.NotSampledCount;
            tblockNotSampledPercent.Text = result.NotSampledPercent;

            tblockResultsPendingToolTip.Text = result.PendingIDs;
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

            if (DataHelper != null && DataHelper.Project != null && DataHelper.Project.CollectedData != null)
            {
                IDbDriver db = DataHelper.Project.CollectedData.GetDatabase();
                int total = (from caseVM in DataHelper.CaseCollection
                             where caseVM.EpiCaseDef != Core.Enums.EpiCaseClassification.Excluded
                             select caseVM).Count();
                string format = "P1";

                int count = (from caseVM in DataHelper.CaseCollection
                             where caseVM.FinalLabClass == Core.Enums.FinalLabClassification.ConfirmedAcute && caseVM.EpiCaseDef != Core.Enums.EpiCaseClassification.Excluded
                             select caseVM).Count();

                result.ConfirmedAcuteCount = count.ToString();
                result.ConfirmedAcutePercent = ((double)count / (double)total).ToString(format);

                count = (from caseVM in DataHelper.CaseCollection
                         where caseVM.FinalLabClass == Core.Enums.FinalLabClassification.ConfirmedConvalescent && caseVM.EpiCaseDef != Core.Enums.EpiCaseClassification.Excluded
                         select caseVM).Count();

                result.ConfirmedConvalescentCount = count.ToString();
                result.ConfirmedConvalescentPercent = ((double)count / (double)total).ToString(format);

                count = (from caseVM in DataHelper.CaseCollection
                         where caseVM.FinalLabClass == Core.Enums.FinalLabClassification.NotCase && caseVM.EpiCaseDef != Core.Enums.EpiCaseClassification.Excluded
                         select caseVM).Count();

                result.NegativeCount = count.ToString();
                result.NegativePercent = ((double)count / (double)total).ToString(format);

                count = (from caseVM in DataHelper.CaseCollection
                         where caseVM.FinalLabClass == Core.Enums.FinalLabClassification.Indeterminate && caseVM.EpiCaseDef != Core.Enums.EpiCaseClassification.Excluded
                         select caseVM).Count();

                result.IndeterminateCount = count.ToString();
                result.IndeterminatePercent = ((double)count / (double)total).ToString(format);

                count = (from caseVM in DataHelper.CaseCollection
                         where caseVM.FinalLabClass == Core.Enums.FinalLabClassification.NeedsFollowUpSample && caseVM.EpiCaseDef != Core.Enums.EpiCaseClassification.Excluded
                         select caseVM).Count();

                result.NeedsFollowUpCount = count.ToString();
                result.NeedsFollowUpPercent = ((double)count / (double)total).ToString(format);

                Epi.Fields.RenderableField finalLabClassField = DataHelper.CaseForm.Fields["FinalLabClass"] as Epi.Fields.RenderableField;
                Epi.Fields.RenderableField epiCaseDefField = DataHelper.CaseForm.Fields["EpiCaseDef"] as Epi.Fields.RenderableField;

                if (finalLabClassField != null && epiCaseDefField != null && finalLabClassField.Page != null && epiCaseDefField.Page != null)
                {
                    string finalLabClassTableName = finalLabClassField.Page.TableName;
                    string epiCaseClassTableName = epiCaseDefField.Page.TableName;

                    string queryText = "";
                    if (db.ToString().ToLower().Contains("sql"))
                    {
                        queryText = "select count(*) from " + finalLabClassTableName + " AS crf INNER JOIN " + epiCaseClassTableName + " AS crfEpiCaseClass on crf.GlobalRecordId = crfEpiCaseClass.GlobalRecordId INNER JOIN LaboratoryResultsForm lrf on crf.GlobalRecordId = lrf.FKEY where ((crf.FinalLabClass = '' OR crf.FinalLabClass is null) AND (crfEpiCaseClass.EpiCaseDef <> '4'))";
                    }
                    else
                    {
                        queryText = "select count(*) from ((" + finalLabClassTableName + " AS crf) INNER JOIN " + epiCaseClassTableName + " AS crfEpiCaseClass on crf.GlobalRecordId = crfEpiCaseClass.GlobalRecordId) INNER JOIN LaboratoryResultsForm lrf on crf.GlobalRecordId = lrf.FKEY where ((crf.FinalLabClass = '' OR crf.FinalLabClass is null) AND (crfEpiCaseClass.EpiCaseDef <> '4'))";
                    }
                    Query selectQuery = db.CreateQuery(queryText);
                    count = (int)db.ExecuteScalar(selectQuery);

                    if (db.ToString().ToLower().Contains("sql"))
                    {
                        queryText = "select crfEpiCaseClass.ID from " + finalLabClassTableName + " AS crf INNER JOIN " + epiCaseClassTableName + " AS crfEpiCaseClass on crf.GlobalRecordId = crfEpiCaseClass.GlobalRecordId INNER JOIN LaboratoryResultsForm lrf on crf.GlobalRecordId = lrf.FKEY where ((crf.FinalLabClass = '' OR crf.FinalLabClass is null) AND (crfEpiCaseClass.EpiCaseDef <> '4'))";
                    }
                    else
                    {
                        queryText = "select crfEpiCaseClass.ID from ((" + finalLabClassTableName + " AS crf) INNER JOIN " + epiCaseClassTableName + " AS crfEpiCaseClass on crf.GlobalRecordId = crfEpiCaseClass.GlobalRecordId) INNER JOIN LaboratoryResultsForm lrf on crf.GlobalRecordId = lrf.FKEY where ((crf.FinalLabClass = '' OR crf.FinalLabClass is null) AND (crfEpiCaseClass.EpiCaseDef <> '4'))";
                    }
                    selectQuery = db.CreateQuery(queryText);
                    DataTable dt = db.Select(selectQuery);
                    WordBuilder wb = new WordBuilder(",");

                    foreach (DataRow row in dt.Rows)
                    {
                        wb.Add(row["ID"].ToString());
                    }

                    result.PendingIDs = wb.ToString();

                    result.PendingCount = count.ToString();
                    result.PendingPercent = ((double)count / (double)total).ToString(format);

                    if (db.ToString().ToLower().Contains("sql"))
                    {
                        queryText = "select count(*) from CaseInformationForm AS crf LEFT JOIN " + epiCaseClassTableName + " AS crfEpiCaseClass on crf.GlobalRecordId = crfEpiCaseClass.GlobalRecordId LEFT JOIN LaboratoryResultsForm lrf on crf.GlobalRecordId = lrf.FKEY where ((lrf.GlobalRecordId = '' OR lrf.GlobalRecordId is null) AND (crfEpiCaseClass.EpiCaseDef <> '4') AND crf.RecStatus = 1)";
                    }
                    else
                    {
                        queryText = "select count(*) from ((CaseInformationForm AS crf) LEFT JOIN " + epiCaseClassTableName + " AS crfEpiCaseClass on crf.GlobalRecordId = crfEpiCaseClass.GlobalRecordId) LEFT JOIN LaboratoryResultsForm lrf on crf.GlobalRecordId = lrf.FKEY where ((lrf.GlobalRecordId = '' OR lrf.GlobalRecordId is null) AND (crfEpiCaseClass.EpiCaseDef <> '4') AND crf.RecStatus = 1)";
                    }
                    selectQuery = db.CreateQuery(queryText);
                    count = (int)db.ExecuteScalar(selectQuery);

                    result.NotSampledCount = count.ToString();
                    result.NotSampledPercent = ((double)count / (double)total).ToString(format);

                    e.Result = result;
                }
                else
                {
                    throw new InvalidOperationException("FinalLabClass and EpiCaseDef must both be non-null fields in computeWorker_doWork in LabClassAllPatients.xaml.cs");
                }
            }
        }
    }
}
