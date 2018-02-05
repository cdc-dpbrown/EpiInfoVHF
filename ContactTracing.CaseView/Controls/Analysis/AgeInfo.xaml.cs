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
    /// Interaction logic for AgeInfo.xaml
    /// </summary>
    public partial class AgeInfo : UserControl
    {
        public class Result
        {
            public string Median;
            public string Mean;
            public string Min;
            public string Max;
        }

        private delegate void SetGridTextHandler(Result result);

        public AgeInfo()
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
            tblockMedianAge.Text = "...";
            tblockMeanAge.Text = "...";
            tblockMinAge.Text = "...";
            tblockMaxAge.Text = "...";

            if (this.DataHelper.CaseCollection == null || this.DataHelper.CaseCollection.Count == 0)
            {
                return;
            }

            BackgroundWorker computeWorker = new BackgroundWorker();
            computeWorker.DoWork += new DoWorkEventHandler(computeWorker_DoWork);
            computeWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(computeWorker_RunWorkerCompleted);
            computeWorker.RunWorkerAsync(this.DataHelper);
        }

        void SetGridText(Result result)
        {
            tblockMedianAge.Text = result.Median;
            tblockMeanAge.Text = result.Mean;
            tblockMinAge.Text = result.Min;
            tblockMaxAge.Text = result.Max;
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
                List<double> numbers = new List<double>();

                foreach (CaseViewModel caseVM in DataHelper.CaseCollection)
                {
                    if (caseVM.AgeYears.HasValue && (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed || caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable))
                    {
                        numbers.Add(caseVM.AgeYears.Value);
                    }
                }

                if (numbers.Count == 0) return;

                result.Mean = numbers.Average().ToString("G2");

                int numberCount = numbers.Count();
                int halfIndex = numbers.Count() / 2;
                var sortedNumbers = numbers.OrderBy(n => n);
                double median;
                if ((numberCount % 2) == 0)
                {
                    median = ((sortedNumbers.ElementAt(halfIndex) +
                        sortedNumbers.ElementAt((halfIndex - 1))) / 2);
                }
                else
                {
                    median = sortedNumbers.ElementAt(halfIndex);
                }

                result.Median = median.ToString("G2");

                result.Min = numbers.Min().ToString("G2");
                result.Max = numbers.Max().ToString("G2");

                e.Result = result;
            }
        }
    }
}
