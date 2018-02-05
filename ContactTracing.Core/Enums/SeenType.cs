using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace ContactTracing.Core.Enums
{
    public enum ContactSeenType
    {
        [Description("Seen")]
        Seen,
        [Description("Not Seen")]
        NotSeen,
        [Description("Not Recorded")]
        Unknown
    }
}