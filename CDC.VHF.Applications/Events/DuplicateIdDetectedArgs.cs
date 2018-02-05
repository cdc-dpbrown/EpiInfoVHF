using System;
using System.Collections.Generic;

namespace CDC.VHF.Applications.Events
{
    public class DuplicateIdDetectedArgs : EventArgs
    {
        public DuplicateIdDetectedArgs(List<CaseViewModel> duplicateCases)
        {
            this.DuplicateCases = duplicateCases;
        }

        public List<CaseViewModel> DuplicateCases { get; private set; }
    }
}
