using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContactTracing.ViewModel.Events
{
    public class DailyCheckStatusChangedEventArgs : EventArgs
    {
        public DailyCheckStatusChangedEventArgs(object source)
        {
            this.Source = source;
        }

        public object Source { get; private set; }
    }
}
