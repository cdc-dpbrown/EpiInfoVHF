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
    /// Interaction logic for ConfirmedProbableTable.xaml
    /// </summary>
    public partial class ConfirmedProbableTable : AnalysisOutputBase
    {
        //public static readonly DependencyProperty DisplayDateProperty = DependencyProperty.Register("DisplayDateProperty", typeof(DateTime), typeof(ConfirmedProbableTable));

        //public DateTime _displayDate = DateTime.Now;

        //public DateTime DisplayDate
        //{
        //    get
        //    {
        //        return _displayDate; // (DateTime)(this.GetValue(DisplayDateProperty));
        //    }
        //    set
        //    {
        //        this._displayDate = value; //this.SetValue(DisplayDateProperty, value);
        //        tblockCurrentDate.Text = value.ToString("dd/MM/yyyy HH:MM");
        //    }
        //}

        public class Result
        {
            public string TotalConfirmed;
            public string TotalProbable;
            public string TotalCombined;

            public string AliveConfirmed = string.Empty;
            public string AliveProbable = string.Empty;
            public string AliveCombined = string.Empty;

            public string DeadConfirmed = string.Empty;
            public string DeadProbable = string.Empty;
            public string DeadCombined = string.Empty;

            public string FemaleConfirmed = string.Empty;
            public string FemaleProbable = string.Empty;
            public string FemaleCombined = string.Empty;

            public string MaleConfirmed = string.Empty;
            public string MaleProbable = string.Empty;
            public string MaleCombined = string.Empty;

            public string HCWConfirmed = string.Empty;
            public string HCWProbable = string.Empty;
            public string HCWCombined = string.Empty;
        }

        private delegate void SetGridTextHandler(Result result);

        private EpiDataHelper DataHelper
        {
            get
            {
                return (this.DataContext as EpiDataHelper);
            }
        }

        public ConfirmedProbableTable()
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
            tblockTotalConfirmed.Text = result.TotalConfirmed;
            tblockTotalProbable.Text = result.TotalProbable;
            tblockTotalCombined.Text = result.TotalCombined;

            tblockTotalAliveConfirmed.Text = result.AliveConfirmed;
            tblockTotalAliveProbable.Text = result.AliveProbable;
            tblockTotalAliveCombined.Text = result.AliveCombined;

            tblockTotalDeadConfirmed.Text = result.DeadConfirmed;
            tblockTotalDeadProbable.Text = result.DeadProbable;
            tblockTotalDeadCombined.Text = result.DeadCombined;

            tblockTotalFemaleConfirmed.Text = result.FemaleConfirmed;
            tblockTotalFemaleProbable.Text = result.FemaleProbable;
            tblockTotalFemaleCombined.Text = result.FemaleCombined;

            tblockTotalMaleConfirmed.Text = result.MaleConfirmed;
            tblockTotalMaleProbable.Text = result.MaleProbable;
            tblockTotalMaleCombined.Text = result.MaleCombined;

            tblockTotalHCWConfirmed.Text = result.HCWConfirmed;
            tblockTotalHCWProbable.Text = result.HCWProbable;
            tblockTotalHCWCombined.Text = result.HCWCombined;
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

            if (DataHelper != null)
            {
                string format = "P1";

                double totalConfirmed = 0;
                double totalProbable = 0;
                double totalCombined = 0;

                double runningCount = 0;

                // TOTAL
                double count = (from caseVM in DataHelper.CaseCollection
                                where caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed
                                select caseVM).Count();

                runningCount = runningCount + count;
                result.TotalConfirmed = count.ToString();
                totalConfirmed = count;

                count = (from caseVM in DataHelper.CaseCollection
                         where caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable
                         select caseVM).Count();

                runningCount = runningCount + count;
                result.TotalProbable = count.ToString();
                totalProbable = count;

                result.TotalCombined = runningCount.ToString();
                totalCombined = runningCount;

                runningCount = 0;

                // ALIVE
                count = (from caseVM in DataHelper.CaseCollection
                         where caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed && caseVM.CurrentStatus == Properties.Resources.Alive
                         select caseVM).Count();

                runningCount = runningCount + count;
                if (totalConfirmed > 0) result.AliveConfirmed = count.ToString() + " (" + (count / totalConfirmed).ToString(format) + ")";

                count = (from caseVM in DataHelper.CaseCollection
                         where caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable && caseVM.CurrentStatus == Properties.Resources.Alive
                         select caseVM).Count();

                runningCount = runningCount + count;
                if (totalProbable > 0) result.AliveProbable = count.ToString() + " (" + (count / totalProbable).ToString(format) + ")";

                if (totalCombined > 0) result.AliveCombined = runningCount.ToString() + " (" + (runningCount / totalCombined).ToString(format) + ")";

                runningCount = 0;

                // DEAD
                count = (from caseVM in DataHelper.CaseCollection
                         where caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed && caseVM.CurrentStatus == Properties.Resources.Dead
                         select caseVM).Count();

                runningCount = runningCount + count;
                if (totalConfirmed > 0) result.DeadConfirmed = count.ToString() + " (" + (count / totalConfirmed).ToString(format) + ")";

                count = (from caseVM in DataHelper.CaseCollection
                         where caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable && caseVM.CurrentStatus == Properties.Resources.Dead
                         select caseVM).Count();

                runningCount = runningCount + count;
                if (totalProbable > 0) result.DeadProbable = count.ToString() + " (" + (count / totalProbable).ToString(format) + ")";

                if (totalCombined > 0) result.DeadCombined = runningCount.ToString() + " (" + (runningCount / totalCombined).ToString(format) + ")";

                runningCount = 0;

                // MALE
                count = (from caseVM in DataHelper.CaseCollection
                         where caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed && caseVM.Gender == Core.Enums.Gender.Male
                         select caseVM).Count();

                runningCount = runningCount + count;
                if (totalConfirmed > 0) result.MaleConfirmed = count.ToString() + " (" + (count / totalConfirmed).ToString(format) + ")";

                count = (from caseVM in DataHelper.CaseCollection
                         where caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable && caseVM.Gender == Core.Enums.Gender.Male
                         select caseVM).Count();

                runningCount = runningCount + count;
                if (totalProbable > 0) result.MaleProbable = count.ToString() + " (" + (count / totalProbable).ToString(format) + ")";

                if (totalCombined > 0) result.MaleCombined = runningCount.ToString() + " (" + (runningCount / totalCombined).ToString(format) + ")";

                runningCount = 0;

                // FEMALE
                count = (from caseVM in DataHelper.CaseCollection
                         where caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed && caseVM.Gender == Core.Enums.Gender.Female
                         select caseVM).Count();

                runningCount = runningCount + count;
                if (totalConfirmed > 0) result.FemaleConfirmed = count.ToString() + " (" + (count / totalConfirmed).ToString(format) + ")";

                count = (from caseVM in DataHelper.CaseCollection
                         where caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable && caseVM.Gender == Core.Enums.Gender.Female
                         select caseVM).Count();

                runningCount = runningCount + count;
                if (totalProbable > 0) result.FemaleProbable = count.ToString() + " (" + (count / totalProbable).ToString(format) + ")";

                if (totalCombined > 0) result.FemaleCombined = runningCount.ToString() + " (" + (runningCount / totalCombined).ToString(format) + ")";

                runningCount = 0;

                // HCW
                count = (from caseVM in DataHelper.CaseCollection
                         where caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed && caseVM.IsHCW.HasValue && caseVM.IsHCW.Value == true
                         select caseVM).Count();

                runningCount = runningCount + count;
                if (totalConfirmed > 0) result.HCWConfirmed = count.ToString() + " (" + (count / totalConfirmed).ToString(format) + ")";

                count = (from caseVM in DataHelper.CaseCollection
                         where caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable && caseVM.IsHCW.HasValue && caseVM.IsHCW.Value == true
                         select caseVM).Count();

                runningCount = runningCount + count;
                if (totalProbable > 0) result.HCWProbable = count.ToString() + " (" + (count / totalProbable).ToString(format) + ")";

                if (totalCombined > 0) result.HCWCombined = runningCount.ToString() + " (" + (runningCount / totalCombined).ToString(format) + ")";

                e.Result = result;
            }
        }
    }
}
