using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactTracing.Core
{
    //public delegate void SetProgressBarDelegate(double progress);
    //public delegate void SimpleEventHandler();
    //public delegate void UpdateStatusEventHandler(string message);
    public delegate void SmsReceivedHandler(object sender, Core.Events.SmsReceivedArgs e);
}
