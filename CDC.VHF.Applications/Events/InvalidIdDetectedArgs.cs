using System;
using System.Collections.Generic;

namespace CDC.VHF.Applications.Events
{
    public class InvalidIdDetectedArgs : EventArgs
    {
        public InvalidIdDetectedArgs(List<string> invalidIds)
        {
            this.InvalidIds = invalidIds;
        }

        public List<string> InvalidIds { get; private set; }
    }
}
