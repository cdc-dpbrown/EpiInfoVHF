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
    /// Interaction logic for LastIsoInfo.xaml
    /// </summary>
    public partial class LastIsoInfo : UserControl
    {
        public class Result
        {
            public string Admitted;
            public string Discharged;
        }

        private delegate void SetGridTextHandler(Result result);

        public LastIsoInfo()
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
            tblockLastAdmittedIso.Text = "...";
            tblockLastDischargedIso.Text = "...";

            SetGridText(RunCompute());
            //BackgroundWorker computeWorker = new BackgroundWorker();
            //computeWorker.DoWork += new DoWorkEventHandler(computeWorker_DoWork);
            //computeWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(computeWorker_RunWorkerCompleted);
            //computeWorker.RunWorkerAsync(this.DataHelper);
        }

        void SetGridText(Result result)
        {
            tblockLastAdmittedIso.Text = result.Admitted;
            tblockLastDischargedIso.Text = result.Discharged;
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

        private Result RunCompute()
        {
            Result result = new Result();
            
            DateTime today = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

            ListCollectionView isolatedCollectionView = DataHelper.IsolatedCollectionView as ListCollectionView;

            if (isolatedCollectionView != null)
            {
                var query = from caseVM in isolatedCollectionView.Cast<CaseViewModel>()
                            where caseVM.DateIsolationCurrent.HasValue && !caseVM.DateDischargeIso.HasValue
                            orderby caseVM.DateIsolationCurrent descending
                            select caseVM;

                foreach (CaseViewModel caseVM in query)
                {
                    DateTime? dt = caseVM.DateIsolationCurrent;
                    result.Admitted = dt.Value.ToLongDateString() + " (" + caseVM.ID + ")";
                    break;
                }

                query = from caseVM in DataHelper.CaseCollection
                        where caseVM.DateDischargeIso.HasValue
                        orderby caseVM.DateDischargeIso descending
                        select caseVM;

                DateTime? dtIsoDischarged = null;
                foreach (CaseViewModel caseVM in query)
                {
                    dtIsoDischarged = caseVM.DateDischargeIso;
                    result.Discharged = dtIsoDischarged.Value.ToLongDateString() + " (" + caseVM.ID + ")";
                    break;
                }

                query = from caseVM in DataHelper.CaseCollection
                        where caseVM.DateDeathCurrentOrFinal.HasValue
                        orderby caseVM.DateDeathCurrentOrFinal descending
                        select caseVM;

                foreach (CaseViewModel caseVM in query)
                {
                    DateTime? dt = caseVM.DateDeathCurrentOrFinal;
                    if (dtIsoDischarged.HasValue)
                    {
                        if (dtIsoDischarged.Value < dt.Value)
                        {
                            result.Discharged = dt.Value.ToLongDateString() + " (" + caseVM.ID + ")";
                        }
                    }
                    else
                    {
                        result.Discharged = dt.Value.ToLongDateString() + " (" + caseVM.ID + ")";
                    }
                    break;
                }

                // TODO: OR latest date of death of confirmed case
            }
            return result;
        }

        void computeWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Result result = new Result();
            EpiDataHelper DataHelper = e.Argument as EpiDataHelper;

            if (DataHelper != null && DataHelper.Project != null && DataHelper.IsolatedCollectionView != null)
            {
                DateTime today = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

                ListCollectionView isolatedCollectionView = DataHelper.IsolatedCollectionView as ListCollectionView;

                if (isolatedCollectionView != null)
                {
                    var query = from caseVM in isolatedCollectionView.Cast<CaseViewModel>()
                                where caseVM.DateIsolationCurrent.HasValue && !caseVM.DateDischargeIso.HasValue
                                orderby caseVM.DateIsolationCurrent descending
                                select caseVM;

                    foreach (CaseViewModel caseVM in query)
                    {
                        DateTime? dt = caseVM.DateIsolationCurrent;
                        result.Admitted = dt.Value.ToLongDateString() + " (" + caseVM.ID + ")";
                        break;
                    }

                    query = from caseVM in DataHelper.CaseCollection
                            where caseVM.DateDischargeIso.HasValue
                            orderby caseVM.DateDischargeIso descending
                            select caseVM;

                    DateTime? dtIsoDischarged = null;
                    foreach (CaseViewModel caseVM in query)
                    {
                        dtIsoDischarged = caseVM.DateDischargeIso;
                        result.Discharged = dtIsoDischarged.Value.ToLongDateString() + " (" + caseVM.ID + ")";
                        break;
                    }

                    query = from caseVM in DataHelper.CaseCollection
                            where caseVM.DateDeathCurrentOrFinal.HasValue
                            orderby caseVM.DateDeathCurrentOrFinal descending
                            select caseVM;

                    foreach (CaseViewModel caseVM in query)
                    {
                        DateTime? dt = caseVM.DateDeathCurrentOrFinal;
                        if (dtIsoDischarged.HasValue)
                        {
                            if (dtIsoDischarged.Value < dt.Value)
                            {
                                result.Discharged = dt.Value.ToLongDateString() + " (" + caseVM.ID + ")";
                            }
                        }
                        else
                        {
                            result.Discharged = dt.Value.ToLongDateString() + " (" + caseVM.ID + ")";
                        }
                        break;
                    }

                    // TODO: OR latest date of death of confirmed case


                    e.Result = result;
                }
            }
        }
    }
}
