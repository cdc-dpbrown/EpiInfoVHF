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
using Epi.Data;

namespace ContactTracing.CaseView.Controls.Analysis
{
    /// <summary>
    /// Interaction logic for PatientTestedInfo.xaml
    /// </summary>
    public partial class PatientTestedInfo : UserControl
    {
        public class Result
        {
            public string PatientsTested;
            public string SamplesCollected;
        }

        private delegate void SetGridTextHandler(Result result);

        public PatientTestedInfo()
        {
            InitializeComponent();
        }

        private EpiDataHelper DataHelper
        {
            get
            {
                return (this.DataContext as EpiDataHelper);
            }
        }

        public void Compute()
        {
            tblockPatientsTested.Text = "...";
            tblockSamplesCollected.Text = "...";

            BackgroundWorker computeWorker = new BackgroundWorker();
            computeWorker.DoWork += new DoWorkEventHandler(computeWorker_DoWork);
            computeWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(computeWorker_RunWorkerCompleted);
            computeWorker.RunWorkerAsync(this.DataHelper);
        }

        void SetGridText(Result result)
        {
            tblockPatientsTested.Text = result.PatientsTested;
            tblockSamplesCollected.Text = result.SamplesCollected;
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
                DateTime today = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

                DataTable labTable = DataHelper.LabTable;

                double patientsTested = 0;
                double samplesCollected = labTable.Rows.Count;//0;

                foreach (CaseViewModel caseVM in DataHelper.CaseCollection)
                {
                    if (caseVM.EpiCaseDef != Core.Enums.EpiCaseClassification.Excluded)
                    {
                        double samplesForCurrentCase = labTable.Select("[FKEY] = '" + caseVM.RecordId + "'").Count();
                        //samplesCollected = samplesCollected + samplesForCurrentCase;

                        if (samplesForCurrentCase > 0)
                        {
                            patientsTested++;
                        }
                    }
                }

                // Commented below is the old method, which simply counted everyone with lab sample records. Customer noted this needs to change. See new code.
                //result.PatientsTested = patientsTested.ToString(); 

                // New code is below.
                Epi.Fields.RenderableField finalLabClassField = DataHelper.CaseForm.Fields["FinalLabClass"] as Epi.Fields.RenderableField;
                if (finalLabClassField != null && finalLabClassField.Page != null)
                {
                    string finalLabClassTableName = finalLabClassField.Page.TableName;
                    string queryText = "select distinct lrf.FKEY from " + finalLabClassTableName + " crf INNER JOIN LaboratoryResultsForm lrf on crf.GlobalRecordId = lrf.FKEY where (crf.FinalLabClass <> '' AND crf.FinalLabClass is not null)";
                    Query selectQuery = db.CreateQuery(queryText);
                    int count = db.Select(selectQuery).Rows.Count;

                    // TODO: Remove excluded cases

                    result.PatientsTested = count.ToString();
                    result.SamplesCollected = samplesCollected.ToString();
                }
                e.Result = result;
            }
        }
    }
}
