using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContactTracing.Core;

namespace ContactTracing.ViewModel
{
    public class DailyCheck : ObservableObject
    {
        public CaseViewModel CaseVM { get; set; }
        public ContactViewModel ContactVM { get; set; }
        public ContactDailyStatus? Status { get; set; }
        public string Notes { get; set; }
        public double Temp1 { get; set; }
        public double Temp2 { get; set; }

        public DailyCheck()
        {
            //Sickness = SicknessType.NotRecorded;
            //Seen = SeenType.Unknown;
            Notes = String.Empty;
            Status = null;
        }

        public bool IsStatusSeenNotSick
        {
            get
            {
                if (Status.HasValue && Status.Value == ContactDailyStatus.SeenNotSick) return true;
                else return false;
            }
            set
            {
                if (value) Status = ContactDailyStatus.SeenNotSick;
            }
        }
        public bool IsStatusSeenSickAndIsolated
        {
            get
            {
                if (Status.HasValue && Status.Value == ContactDailyStatus.SeenSickAndIsolated) return true;
                else return false;
            }
            set
            {
                if (value) Status = ContactDailyStatus.SeenSickAndIsolated;
            }
        }
        public bool IsStatusSeenSickAndNotIsolated
        {
            get
            {
                if (Status.HasValue && Status.Value == ContactDailyStatus.SeenSickAndNotIsolated) return true;
                else return false;
            }
            set
            {
                if (value) Status = ContactDailyStatus.SeenSickAndNotIsolated;
            }
        }
        public bool IsStatusSeenSickAndIsoNotFilledOut
        {
            get
            {
                if (Status.HasValue && Status.Value == ContactDailyStatus.SeenSickAndIsoNotFilledOut) return true;
                else return false;
            }
            set
            {
                if (value) Status = ContactDailyStatus.SeenSickAndIsoNotFilledOut;
            }
        }
        public bool IsStatusNotSeen
        {
            get
            {
                if (Status.HasValue && Status.Value == ContactDailyStatus.NotSeen) return true;
                else return false;
            }
            set
            {
                if (value) Status = ContactDailyStatus.NotSeen;
            }
        }
        public bool IsStatusNotRecorded
        {
            get
            {
                if (Status.HasValue && Status.Value == ContactDailyStatus.NotRecorded) return true;
                else return false;
            }
            set
            {
                if (value) Status = ContactDailyStatus.NotRecorded;
            }
        }
        public bool IsStatusUnknown
        {
            get
            {
                if (!Status.HasValue) return true;
                else return false;
            }
            set
            {
                if (value) Status = null;
            }
        }
    }
}
