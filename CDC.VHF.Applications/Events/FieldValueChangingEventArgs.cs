using System;

namespace CDC.VHF.Applications.Events
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
