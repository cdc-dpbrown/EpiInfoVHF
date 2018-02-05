using System;
using System.Collections.Generic;
using System.Text;

namespace ContactTracing.ViewModel.Events
{
    public class CaseDataPopulatedArgs : EventArgs
    {
        public CaseDataPopulatedArgs(Core.Enums.VirusTestTypes virusTestType, bool showAllTests)
        {
            this.VirusTestType = virusTestType;
            this.ShowAllTests = showAllTests;
        }

        public Core.Enums.VirusTestTypes VirusTestType { get; private set; }
        public bool ShowAllTests { get; private set; }
    }
}
