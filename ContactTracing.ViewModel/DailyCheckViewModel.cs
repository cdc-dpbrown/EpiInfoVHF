using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContactTracing.Core;

namespace ContactTracing.ViewModel
{
    public class DailyCheckViewModel : ObservableObject
    {
        public DailyCheck DailyCheck;

        public DailyCheckViewModel(ContactViewModel contactVM, CaseViewModel caseVM)
        {
            DailyCheck = new DailyCheck();
            DailyCheck.CaseVM = caseVM;
            DailyCheck.ContactVM = contactVM;
        }

        public CaseViewModel CaseVM
        {
            get { return DailyCheck.CaseVM; }
        }

        public ContactViewModel ContactVM
        {
            get { return DailyCheck.ContactVM; }
            set
            {
                if (DailyCheck.ContactVM != value)
                {
                    DailyCheck.ContactVM = value;
                    RaisePropertyChanged("ContactVM");
                }
            }
        }

        public string CaseID
        {
            get
            {
                if (CaseViewModel.IsCountryUS)//17226
                {
                    return CaseVM.OriginalID;
                }
                return CaseVM.ID;
            }
        }

        public string CaseSurname
        {
            get { return CaseVM.Surname; }
        }

        public string CaseOtherNames
        {
            get { return CaseVM.OtherNames; }
        }

        public string Surname
        {
            get { return ContactVM.Surname; }
        }

        public string OtherNames
        {
            get { return ContactVM.OtherNames; }
        }

        public double? Age
        {
            get { return ContactVM.Age; }
        }
        public double? AgeYears
        {
            get { return ContactVM.AgeYears; }
        }

        public string Gender
        {
            get { return ContactVM.Gender; }
        }

        public string ContactID
        {
            get { return ContactVM.ContactID; }
        }

        //public string Address
        //{
        //    get { return ContactVM.Address; }
        //}

        public string Village
        {
            get { return ContactVM.Village; }
        }

        public int Day
        {
            get
            {
                DateTime today = DateTime.Today;
                foreach (FollowUpVisitViewModel fuVM in ContactVM.FollowUpWindowViewModel.FollowUpVisits)
                {
                    DateTime fuDate = fuVM.FollowUpVisit.Date;
                    if (fuDate.Day == today.Day && fuDate.Month == today.Month && fuDate.Year == today.Year)
                    {
                        return fuVM.FollowUpVisit.Day;
                    }
                }

                return -1; // maybe throw exception instead?
            }
        }

        public DateTime DateLastContact
        {
            get
            {
                return ContactVM.FollowUpWindowViewModel.WindowStartDate.AddDays(-1);
            }
        }

        public DateTime MonitorFrom
        {
            get
            {
                return ContactVM.FollowUpWindowViewModel.WindowStartDate;
            }
        }

        public DateTime MonitorTo
        {
            get
            {
                return ContactVM.FollowUpWindowViewModel.WindowEndDate; //WindowStartDate.AddDays(20);
            }
        }

        public ContactDailyStatus? Status
        {
            get { return DailyCheck.Status; }
            set
            {
                if (DailyCheck.Status != value)
                {
                    this.DailyCheck.Status = value;
                    RaisePropertyChanged("Status");
                    RaiseStatusPropertiesChanged();
                }
            }
        }

        public bool IsStatusSeenNotSick
        {
            get { return DailyCheck.IsStatusSeenNotSick; }
            set
            {
                if (DailyCheck.IsStatusSeenNotSick != value)
                {
                    DailyCheck.IsStatusSeenNotSick = value;
                    RaiseStatusPropertiesChanged();
                }
            }
        }
        public bool IsStatusSeenSick
        {
            get
            {
                if (DailyCheck.Status.HasValue &&
                    (DailyCheck.Status.Value == ContactDailyStatus.SeenSickAndIsolated || DailyCheck.Status.Value == ContactDailyStatus.SeenSickAndIsoNotFilledOut || DailyCheck.Status.Value == ContactDailyStatus.SeenSickAndNotIsolated))
                {
                    return true;
                }
                else
                {
                    return false;
                }
                //return DailyCheck.IsStatusSeenNotSick; 
            }
            //set
            //{
            //    if (DailyCheck.IsStatusSeenNotSick != value)
            //    {
            //        DailyCheck.IsStatusSeenNotSick = value;
            //        RaiseStatusPropertiesChanged();
            //    }
            //}
        }
        public bool IsStatusSeenSickAndIsolated
        {
            get { return DailyCheck.IsStatusSeenSickAndIsolated; }
            set
            {
                if (DailyCheck.IsStatusSeenSickAndIsolated != value)
                {
                    DailyCheck.IsStatusSeenSickAndIsolated = value;
                    RaiseStatusPropertiesChanged();
                }
            }
        }
        public bool IsStatusSeenSickAndNotIsolated
        {
            get { return DailyCheck.IsStatusSeenSickAndNotIsolated; }
            set
            {
                if (DailyCheck.IsStatusSeenSickAndNotIsolated != value)
                {
                    DailyCheck.IsStatusSeenSickAndNotIsolated = value;
                    RaiseStatusPropertiesChanged();
                }
            }
        }
        public bool IsStatusSeenSickAndIsoNotFilledOut
        {
            get { return DailyCheck.IsStatusSeenSickAndIsoNotFilledOut; }
            set
            {
                if (DailyCheck.IsStatusSeenSickAndIsoNotFilledOut != value)
                {
                    DailyCheck.IsStatusSeenSickAndIsoNotFilledOut = value;
                    RaiseStatusPropertiesChanged();
                }
            }
        }
        public bool IsStatusNotSeen
        {
            get { return DailyCheck.IsStatusNotSeen; }
            set
            {
                if (DailyCheck.IsStatusNotSeen != value)
                {
                    DailyCheck.IsStatusNotSeen = value;
                    RaiseStatusPropertiesChanged();
                }
            }
        }
        public bool IsStatusNotRecorded
        {
            get { return DailyCheck.IsStatusNotRecorded; }
            set
            {
                if (DailyCheck.IsStatusNotRecorded != value)
                {
                    DailyCheck.IsStatusNotRecorded = value;
                    RaiseStatusPropertiesChanged();
                }
            }
        }
        public bool IsStatusUnknown
        {
            get { return DailyCheck.IsStatusUnknown; }
            set
            {
                if (DailyCheck.IsStatusUnknown != value)
                {
                    DailyCheck.IsStatusUnknown = value;
                    RaiseStatusPropertiesChanged();
                }
            }
        }

        private void RaiseStatusPropertiesChanged()
        {
            RaisePropertyChanged("IsStatusSeenNotSick");
            RaisePropertyChanged("IsStatusSeenSickAndIsolated");
            RaisePropertyChanged("IsStatusSeenSickAndNotIsolated");
            RaisePropertyChanged("IsStatusSeenSickAndIsoNotFilledOut");
            RaisePropertyChanged("IsStatusNotSeen");
            RaisePropertyChanged("IsStatusNotRecorded");
            RaisePropertyChanged("IsStatusUnknown");
        }

        //public bool IsSeenAndNotSick
        //{
        //    get { return DailyCheck.IsSeenAndNotSick; }
        //    set
        //    {
        //        if (DailyCheck.IsSeenAndNotSick != value)
        //        {
        //            DailyCheck.IsSeenAndNotSick = value;
        //            RaisePropertyChanged("IsSeenAndNotSick");
        //        }
        //    }
        //}

        //public bool IsSeenAndSick
        //{
        //    get { return DailyCheck.IsSeenAndSick; }
        //    set
        //    {
        //        if (DailyCheck.IsSeenAndSick != value)
        //        {
        //            DailyCheck.IsSeenAndSick = value;
        //            RaisePropertyChanged("IsSeenAndSick");
        //        }
        //    }
        //}

        //public bool IsSeenAndSickNotIsolated
        //{
        //    get { return DailyCheck.IsSeenAndSickNotIsolated; }
        //    set
        //    {
        //        if (DailyCheck.IsSeenAndSickNotIsolated != value)
        //        {
        //            DailyCheck.IsSeenAndSickNotIsolated = value;
        //            RaisePropertyChanged("IsSeenAndSickNotIsolated");
        //            RaisePropertyChanged("IsSeenAndSickIsolated");
        //        }
        //    }
        //}

        //public bool IsSeenAndSickIsolated
        //{
        //    get { return DailyCheck.IsSeenAndSickIsolated; }
        //    set
        //    {
        //        if (DailyCheck.IsSeenAndSickIsolated != value)
        //        {
        //            DailyCheck.IsSeenAndSickIsolated = value;
        //            RaisePropertyChanged("IsSeenAndSickIsolated");
        //            RaisePropertyChanged("IsSeenAndSickNotIsolated");
        //        }
        //    }
        //}

        //public bool IsSeenAndSickNotRecorded
        //{
        //    get { return DailyCheck.IsSeenAndSickNotRecorded; }
        //    set
        //    {
        //        if (DailyCheck.IsSeenAndSickNotRecorded != value)
        //        {
        //            DailyCheck.IsSeenAndSickNotRecorded = value;
        //            RaisePropertyChanged("IsSeenAndSickNotRecorded");
        //        }
        //    }
        //}

        //public bool IsNotSeen
        //{
        //    get { return DailyCheck.IsNotSeen; }
        //    set
        //    {
        //        if (DailyCheck.IsNotSeen != value)
        //        {
        //            DailyCheck.IsNotSeen = value;
        //            RaisePropertyChanged("IsNotSeen");
        //        }
        //    }
        //}

        //public bool IsSeenUnknown
        //{
        //    get { return DailyCheck.IsSeenUnknown; }
        //    set
        //    {
        //        if (DailyCheck.IsSeenUnknown != value)
        //        {
        //            DailyCheck.IsSeenUnknown = value;
        //            RaisePropertyChanged("IsSeenUnknown");
        //        }
        //    }
        //}

        //public IEnumerable<SeenType> SeenTypeValues
        //{
        //    get
        //    {
        //        return Enum.GetValues(typeof(SeenType)).Cast<SeenType>();
        //    }
        //}

        //public IEnumerable<SicknessType> SicknessTypeValues
        //{
        //    get
        //    {
        //        return Enum.GetValues(typeof(SicknessType)).Cast<SicknessType>();

        //    }
        //}

        public string Notes
        {
            get { return DailyCheck.Notes; }
            set
            {
                if (DailyCheck.Notes != value)
                {
                    DailyCheck.Notes = value;
                    RaisePropertyChanged("Notes");
                }
            }
        }

        public Double? Temp1
        {
            get
            {
                if (DailyCheck.Temp1 == 0.0) return null;
                return DailyCheck.Temp1;
            }
            set
            {
                if (DailyCheck.Temp1 != value)
                {
                    if (value == null)
                    {
                        DailyCheck.Temp1 = 0;
                    }
                    else
                    {
                        DailyCheck.Temp1 = (double)value;
                    }
                    RaisePropertyChanged("Temp1");
                }
            }
        }

        public Double? Temp2
        {
            get
            {
                if (DailyCheck.Temp2 == 0.0) return null;
                return DailyCheck.Temp2;
            }
            set
            {
                if (DailyCheck.Temp2 != value)
                {
                    if (value == null)
                    {
                        DailyCheck.Temp2 = 0;
                    }
                    else
                    {
                        DailyCheck.Temp2 = (double)value;
                    }
                    RaisePropertyChanged("Temp2");
                }
            }
        }

        public int GetDay(DateTime dt)
        {
            // time spans are faster
            TimeSpan ts = dt - ContactVM.DateOfLastContact.Value;
            return (int)Math.Round(ts.TotalDays, 0);

            //foreach (FollowUpVisitViewModel fuVM in ContactVM.Contact.FollowUpWindowViewModel.FollowUpVisits)
            //{
            //    DateTime fuDate = fuVM.FollowUpVisit.Date;
            //    if (fuDate.Day == dt.Day && fuDate.Month == dt.Month && fuDate.Year == dt.Year)
            //    {
            //        return fuVM.FollowUpVisit.Day;
            //    }
            //}

            //return -1; // maybe throw exception instead?
        }
    }
}