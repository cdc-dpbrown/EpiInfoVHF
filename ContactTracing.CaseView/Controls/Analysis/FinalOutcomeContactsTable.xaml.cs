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
    /// Interaction logic for FinalOutcomeContactsTable.xaml
    /// </summary>
    public partial class FinalOutcomeContactsTable : AnalysisOutputBase
    {
        public class Result
        {
            public string Discharged;
            public string Isolated;
            public string Dropped;
            public string InFollowUp;
            public string FollowedToday;
            public string FollowedYesterday;
            public string FollowedDayBeforeYesterday;
            public string Total;
        }

        private delegate void SetGridTextHandler(Result result);

        private EpiDataHelper DataHelper
        {
            get
            {
                return (this.DataContext as EpiDataHelper);
            }
        }

        public FinalOutcomeContactsTable()
        {
            InitializeComponent();
        }

        public void Compute()
        {
            tblockDischarged.Text = "...";
            tblockIsolated.Text = "...";
            tblockDropped.Text = "...";
            tblockInFollowUp.Text = "...";
            tblockFollowedToday.Text = "...";
            tblockTotal.Text = "...";
            tblockFollowedDayBeforeYesterday.Text = "...";

            BackgroundWorker computeWorker = new BackgroundWorker();
            computeWorker.DoWork += new DoWorkEventHandler(computeWorker_DoWork);
            computeWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(computeWorker_RunWorkerCompleted);
            computeWorker.RunWorkerAsync(this.DataHelper);
        }

        void SetGridText(Result result)
        {
            tblockDischarged.Text = result.Discharged;
            tblockIsolated.Text = result.Isolated;
            tblockDropped.Text = result.Dropped;
            tblockInFollowUp.Text = result.InFollowUp;
            tblockFollowedToday.Text = result.FollowedToday;
            tblockFollowedYesterday.Text = result.FollowedYesterday;
            tblockTotal.Text = result.Total;
            tblockFollowedDayBeforeYesterday.Text = result.FollowedDayBeforeYesterday;
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
                DateTime today = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);

                int count = (from contactVM in DataHelper.ContactCollection
                             where contactVM.FinalOutcome == "1"
                             select contactVM).Count();

                result.Discharged = count.ToString();

                count = (from contactVM in DataHelper.ContactCollection
                         where contactVM.FinalOutcome == "2"
                         select contactVM).Count();

                result.Isolated = count.ToString();

                count = (from contactVM in DataHelper.ContactCollection
                         where contactVM.FinalOutcome == "3"
                         select contactVM).Count();

                result.Dropped = count.ToString();

                count = (from contactVM in DataHelper.ContactCollection
                         where contactVM.HasFinalOutcome == false
                         select contactVM).Count();

                result.InFollowUp = count.ToString();
                double inFollowUpToday = count;

                // FOLLOWED TODAY
                var query = from contactVM in DataHelper.ContactCollection
                            where contactVM.HasFinalOutcome == false
                            select contactVM;

                double followedToday = 0;
                foreach (ContactViewModel contactVM in query)
                {
                    if (contactVM.FollowUpWindowViewModel != null) // this should NEVER be null... but in case it is
                    {
                        foreach (FollowUpVisitViewModel fuVM in contactVM.FollowUpWindowViewModel.FollowUpVisits)
                        {
                            if (fuVM.IsSeen /*fuVM.Seen == SeenType.Seen*/)
                            {
                                DateTime fuDate = new DateTime(fuVM.Date.Year, fuVM.Date.Month, fuVM.Date.Day, 0, 0, 0);
                                if (fuDate.Ticks == today.Ticks)
                                {
                                    followedToday++;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                    }
                }

                result.FollowedToday = followedToday.ToString() + " (" + (followedToday / inFollowUpToday).ToString("P1") + ")";


                // FOLLOWED YESTERDAY
                query = from contactVM in DataHelper.ContactCollection
                        where contactVM.HasFinalOutcome == false
                        select contactVM;

                double followedYesterday = 0;
                DateTime yesterday = (new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 0, 0, 0)).AddDays(-1);


                double inFollowUpYesterday = (from contactVM in DataHelper.ContactCollection
                                              where contactVM.HasFinalOutcome == false &&
                                              contactVM.FirstSaveTime.HasValue &&
                                              contactVM.FirstSaveTime.Value < yesterday
                                              select contactVM).Count(); // will exclude any contacts added yesterday and today, since those contacts wouldn't have been followed yesterday

                foreach (ContactViewModel contactVM in query)
                {
                    if (contactVM.FollowUpWindowViewModel != null) // this should NEVER be null... but in case it is
                    {
                        foreach (FollowUpVisitViewModel fuVM in contactVM.FollowUpWindowViewModel.FollowUpVisits)
                        {
                            if (fuVM.IsSeen /*fuVM.Seen == SeenType.Seen*/)
                            {
                                DateTime fuDate = new DateTime(fuVM.Date.Year, fuVM.Date.Month, fuVM.Date.Day, 0, 0, 0);
                                if (fuDate.Ticks == yesterday.Ticks)
                                {
                                    followedYesterday++;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                    }
                }

                // NOTE: This count may be inaccurate. I advised the customer of this and they said it was okay and they would
                // be willing to live with the potential inaccuracy. - EK April 9th 2014.
                result.FollowedYesterday = followedYesterday.ToString();// +" (" + (followedYesterday / inFollowUpYesterday).ToString("P1") + ")";









                // FOLLOWED DAY BEFORE YESTERDAY
                query = from contactVM in DataHelper.ContactCollection
                        where contactVM.HasFinalOutcome == false
                        select contactVM;

                double followedDayBeforeYesterday = 0;
                DateTime dayBeforeYesterday = (new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 0, 0, 0)).AddDays(-2);

                foreach (ContactViewModel contactVM in query)
                {
                    if (contactVM.FollowUpWindowViewModel != null) // this should NEVER be null... but in case it is
                    {
                        foreach (FollowUpVisitViewModel fuVM in contactVM.FollowUpWindowViewModel.FollowUpVisits)
                        {
                            if (fuVM.IsSeen /*fuVM.Seen == SeenType.Seen*/)
                            {
                                DateTime fuDate = new DateTime(fuVM.Date.Year, fuVM.Date.Month, fuVM.Date.Day, 0, 0, 0);
                                if (fuDate.Ticks == dayBeforeYesterday.Ticks)
                                {
                                    followedDayBeforeYesterday++;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                    }
                }

                // NOTE: This count may be inaccurate. I advised the customer of this and they said it was okay and they would
                // be willing to live with the potential inaccuracy. - EK April 14th 2014.
                result.FollowedDayBeforeYesterday = followedDayBeforeYesterday.ToString();// +" (" + (followedDayBeforeYesterday / inFollowUp).ToString("P1") + ")";







                count = (from contactVM in DataHelper.ContactCollection
                         select contactVM).Count();

                result.Total = count.ToString();

                e.Result = result;
            }
        }
    }
}
