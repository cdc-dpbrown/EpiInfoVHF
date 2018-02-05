using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContactTracing.ViewModel.Events
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
