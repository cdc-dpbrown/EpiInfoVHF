using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContactTracing.ViewModel.Events
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
