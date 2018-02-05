using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDC.VHF.Domain.Sms
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
