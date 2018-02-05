using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactTracing.ViewModel.Events
{
    public sealed class FieldValueChangingEventArgs : EventArgs
    {
        public FieldValueChangingEventArgs(string previousValue, string newValue)
        {
            this.PreviousValue = previousValue;
            this.NewValue = newValue;
        }

        public string PreviousValue { get; private set; }
        public string NewValue { get; private set; }
    }
}
