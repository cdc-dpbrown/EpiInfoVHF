using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactTracing.Core.Data
{
    public class SmsStatusMessage
    {
        public string StatusMessage { get; set; }

        public override string ToString()
        {
            return StatusMessage;
        }
    }
}
