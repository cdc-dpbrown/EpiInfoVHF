using System;
using System.IO;
using ContactTracing.Core.Data;

namespace ContactTracing.Core.Events
{
    public sealed class SmsReceivedArgs : EventArgs
    {
        public SmsReceivedArgs(ShortMessage message)
        {
            this.Message = message;
        }

        public ShortMessage Message { get; set; }
    }
}
