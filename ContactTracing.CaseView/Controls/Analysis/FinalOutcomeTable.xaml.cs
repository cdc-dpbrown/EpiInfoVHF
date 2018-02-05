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

namespace ContactTracing.CaseView.Controls.Analysis
{
    /// <summary>
    /// Interaction logic for FinalOutcomeTable.xaml
    /// </summary>
    public partial class FinalOutcomeTable : AnalysisOutputBase
    {
        public class Result
        {
            public string TotalAlive;
            public string TotalDead;

            public string FemaleAlive;
            public string FemaleDead;

            public string MaleAlive;
            public string MaleDead;

            public string HCWAlive;
            public string HCWDead;

            public string DiedCommunity;
            public string DiedHospital;
        }

        private delegate void SetGridTextHandler(Result result);

        private EpiDataHelper DataHelper
        {
            get
            {
                return (this.DataContext as EpiDataHelper);
            }
        }

        public FinalOutcomeTable()
        {
            InitializeComponent();
        }

        public void Compute()
        {
            BackgroundWorker computeWorker = new BackgroundWorker();
            computeWorker.DoWork += new DoWorkEventHandler(computeWorker_DoWork);
            computeWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(computeWorker_RunWorkerCompleted);
            computeWorker.RunWorkerAsync(this.DataHelper);
        }

        void SetGridText(Result result)
        {
            tblockTotalAlive.Text = result.TotalAlive;
            tblockTotalDead.Text = result.TotalDead;

            tblockFemaleAlive.Text = result.FemaleAlive;
            tblockFemaleDead.Text = result.FemaleDead;

            tblockMaleAlive.Text = result.MaleAlive;
            tblockMaleDead.Text = result.MaleDead;

            tblockHCWAlive.Text = result.HCWAlive;
            tblockHCWDead.Text = result.HCWDead;

            tblockDiedHosp.Text = result.DiedHospital;
            tblockDiedComm.Text = result.DiedCommunity;
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

            string format = "P1";

            double totalAlive = 0;
            double totalDead = 0;

            // TOTAL
            double count = (from caseVM in DataHelper.CaseCollection
                            where (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed || caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable) && caseVM.CurrentStatus == Properties.Resources.Alive
                            select caseVM).Count();
            result.TotalAlive = count.ToString();
            totalAlive = count;

            count = (from caseVM in DataHelper.CaseCollection
                     where (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed || caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable) && caseVM.CurrentStatus == Properties.Resources.Dead
                            select caseVM).Count();
            result.TotalDead = count.ToString();
            totalDead = count;

            // FEMALE
            count = (from caseVM in DataHelper.CaseCollection
                     where (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed || caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable) && caseVM.CurrentStatus == Properties.Resources.Alive && caseVM.Gender == Core.Enums.Gender.Female
                            select caseVM).Count();
            result.FemaleAlive = count.ToString() + " (" + (count / totalAlive).ToString(format) + ")";

            count = (from caseVM in DataHelper.CaseCollection
                     where (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed || caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable) && caseVM.CurrentStatus == Properties.Resources.Dead && caseVM.Gender == Core.Enums.Gender.Female
                     select caseVM).Count();
            result.FemaleDead = count.ToString() + " (" + (count / totalDead).ToString(format) + ")";

            // MALE
            count = (from caseVM in DataHelper.CaseCollection
                     where (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed || caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable) && caseVM.CurrentStatus == Properties.Resources.Alive && caseVM.Gender == Core.Enums.Gender.Male
                     select caseVM).Count();
            result.MaleAlive = count.ToString() + " (" + (count / totalAlive).ToString(format) + ")";

            count = (from caseVM in DataHelper.CaseCollection
                     where (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed || caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable) && caseVM.CurrentStatus == Properties.Resources.Dead && caseVM.Gender == Core.Enums.Gender.Male
                     select caseVM).Count();
            result.MaleDead = count.ToString() + " (" + (count / totalDead).ToString(format) + ")";

            // HCW
            count = (from caseVM in DataHelper.CaseCollection
                     where (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed || caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable) && caseVM.CurrentStatus == Properties.Resources.Alive && caseVM.IsHCW.HasValue && caseVM.IsHCW.Value == true
                     select caseVM).Count();
            result.HCWAlive = count.ToString() + " (" + (count / totalAlive).ToString(format) + ")";

            count = (from caseVM in DataHelper.CaseCollection
                     where (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed || caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable) && caseVM.CurrentStatus == Properties.Resources.Dead && caseVM.IsHCW.HasValue && caseVM.IsHCW.Value == true
                     select caseVM).Count();
            result.HCWDead = count.ToString() + " (" + (count / totalDead).ToString(format) + ")";

            // Death Location
            count = (from caseVM in DataHelper.CaseCollection
                     where (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed || caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable) && caseVM.CurrentStatus == Properties.Resources.Dead && caseVM.PlaceOfDeath == "1"
                     select caseVM).Count();
            result.DiedCommunity = count.ToString();

            count = (from caseVM in DataHelper.CaseCollection
                     where (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed || caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable) && caseVM.CurrentStatus == Properties.Resources.Dead && caseVM.PlaceOfDeath == "2"
                     select caseVM).Count();
            result.DiedHospital = count.ToString();

            e.Result = result;
        }
    }
}
