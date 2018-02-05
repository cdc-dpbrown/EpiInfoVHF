using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using ContactTracing.Core;
using ContactTracing.Core.Enums;

namespace ContactTracing.ViewModel
{
    public class FollowUpWindow
    {
        private ObservableCollection<FollowUpVisitViewModel> _followUpVisits;
        public ObservableCollection<FollowUpVisitViewModel> FollowUpVisits
        {
            get
            {
                return this._followUpVisits;
            }
            set
            {
                this._followUpVisits = value;
            }
        }

        public DateTime WindowStartDate { get; set; }
        public CaseViewModel CaseVM { get; set; }
        public ContactViewModel ContactVM { get; set; }

        public FollowUpWindow(DateTime lastContactDate, ContactViewModel contactVM, CaseViewModel caseVM)
        {
            FollowUpVisits = new ObservableCollection<FollowUpVisitViewModel>();

            this.WindowStartDate = lastContactDate.AddDays(1);
            this.ContactVM = contactVM;
            this.CaseVM = caseVM;

            DateTime currentDate = new DateTime(this.WindowStartDate.Ticks);
            for (int i = 1; i <= Core.Common.DaysInWindow; i++)
            {
                FollowUpVisits.Add(new FollowUpVisitViewModel(i, currentDate, contactVM));
                currentDate = currentDate.AddDays(1);
            }
        }
    }

    public class FollowUpVisit
    {
        public ContactDailyStatus? Status { get; set; }
        public ContactSeenType Seen { get; set; }
        public ContactSicknessType Sick { get; set; }
        public int Day { get; set; }
        public DateTime Date { get; set; }
        public string Notes { get; set; }
        public double Temp1 { get; set; }
        public double Temp2 { get; set; }

        public FollowUpVisit(int day, DateTime date)
        {
            this.Day = day;
            this.Date = date;
        }
    }

    public class FollowUpWindowViewModel : ObservableObject
    {
        private FollowUpWindow FollowUpWindow;

        public ObservableCollection<FollowUpVisitViewModel> FollowUpVisits
        {
            get { return this.FollowUpWindow.FollowUpVisits; }
            set
            {
                if (FollowUpWindow.FollowUpVisits != value)
                {
                    this.FollowUpWindow.FollowUpVisits = value;
                    RaisePropertyChanged("FollowUpVisits");
                }
            }
        }

        public DateTime WindowStartDate
        {
            get { return FollowUpWindow.WindowStartDate; }
            set
            {
                if (FollowUpWindow.WindowStartDate != value)
                {
                    DateTime startDate = value;
                    startDate = new DateTime(startDate.Year, startDate.Month, startDate.Day, 0, 0, 0);
                    this.FollowUpWindow.WindowStartDate = value;
                    RaisePropertyChanged("WindowStartDate");

                    int indexDate = Core.Common.DaysInWindow - 1;

                    DateTime currentDate = this.FollowUpWindow.WindowStartDate;
                    for (int i = 0; i <= indexDate; i++)
                    {
                        FollowUpVisits[i].Date = currentDate;
                        currentDate = currentDate.AddDays(1);
                    }
                }
            }
        }

        public DateTime WindowEndDate
        {
            get 
            {
                int indexDate = Core.Common.DaysInWindow - 1;
                DateTime endDate = FollowUpWindow.WindowStartDate.AddDays(indexDate); 
                endDate = new DateTime(endDate.Year, endDate.Month, endDate.Day, 23, 59, 59);
                return endDate;
            } 
        }

        public ContactViewModel ContactVM
        {
            get { return FollowUpWindow.ContactVM; }
            set
            {
                if (FollowUpWindow.ContactVM != value)
                {
                    this.FollowUpWindow.ContactVM = value;
                    RaisePropertyChanged("ContactVM");
                }
            }
        }

        public CaseViewModel CaseVM
        {
            get { return FollowUpWindow.CaseVM; }
            set
            {
                if (FollowUpWindow.CaseVM != value)
                {
                    this.FollowUpWindow.CaseVM = value;
                    RaisePropertyChanged("CaseVM");
                }
            }
        }

        public FollowUpWindowViewModel(DateTime lastContactDate, ContactViewModel contactVM, CaseViewModel caseVM)
        {
            this.FollowUpWindow = new FollowUpWindow(lastContactDate, contactVM, caseVM);
        }
    }

    public class FollowUpVisitViewModel : ObservableObject
    {
        public FollowUpVisit FollowUpVisit;

        private readonly ContactViewModel _contactVM = null;

        public bool IsSeen
        {
            get
            {
                if (!Status.HasValue) return false;
                switch (Status.Value)
                {
                    case ContactDailyStatus.SeenNotSick:
                    case ContactDailyStatus.SeenSickAndIsolated:
                    case ContactDailyStatus.SeenSickAndIsoNotFilledOut:
                    case ContactDailyStatus.SeenSickAndNotIsolated:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public ContactDailyStatus? Status
        {
            get { return FollowUpVisit.Status; }
            set
            {
                if (FollowUpVisit.Status != value)
                {
                    this.FollowUpVisit.Status = value;
                    RaisePropertyChanged("Status");
                }
            }
        }

        [Obsolete("Not used anymore", true)]
        public ContactSeenType Seen
        {
            get { return FollowUpVisit.Seen; }
            set
            {
                if (FollowUpVisit.Seen != value)
                {
                    this.FollowUpVisit.Seen = value;
                    RaisePropertyChanged("Seen");
                }
            }
        }

        [Obsolete("Not used anymore", true)]
        public ContactSicknessType Sick
        {
            get { return FollowUpVisit.Sick; }
            set
            {
                if (FollowUpVisit.Sick != value)
                {
                    this.FollowUpVisit.Sick = value;
                    RaisePropertyChanged("Sick");
                }
            }
        }

        public string Notes
        {
            get { return FollowUpVisit.Notes; }
            set
            {
                if (FollowUpVisit.Notes != value)
                {
                    this.FollowUpVisit.Notes = value;
                    RaisePropertyChanged("Notes");
                }
            }
        }

        public double Temp1
        {
            get { return FollowUpVisit.Temp1; }
            set
            {
                if (FollowUpVisit.Temp1 != value)
                {
                    FollowUpVisit.Temp1 = value;
                    RaisePropertyChanged("Temp1");
                }
            }
        }

        public double Temp2
        {
            get { return FollowUpVisit.Temp2; }
            set
            {
                if (FollowUpVisit.Temp2 != value)
                {
                    FollowUpVisit.Temp2 = value;
                    RaisePropertyChanged("Temp2");
                }
            }
        }

        public int Day
        {
            get { return FollowUpVisit.Day; }
            set
            {
                if (FollowUpVisit.Day != value)
                {
                    this.FollowUpVisit.Day = value;
                    RaisePropertyChanged("Day");
                }
            }
        }

        public DateTime Date
        {
            get { return FollowUpVisit.Date; }
            set
            {
                if (FollowUpVisit.Date != value)
                {
                    this.FollowUpVisit.Date = value;
                    RaisePropertyChanged("Date");
                }
            }
        }

        public ContactViewModel ContactVM
        {
            get
            {
                return _contactVM;
            }
        }

        public FollowUpVisitViewModel(int day, DateTime date, ContactViewModel contactVM)
        {
            this.FollowUpVisit = new FollowUpVisit(day, date);
            _contactVM = contactVM;
        }
    }
}
