using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContactTracing.ViewModel.Events
{
    public class InitialSetupArgs
    {
        public InitialSetupArgs(bool showSetupScreen)
        {
            this.ShowSetupScreen = showSetupScreen;
        }

        public bool ShowSetupScreen { get; private set; }
    }
}
