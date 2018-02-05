using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace ContactTracing.Core.Enums
{
    public enum ContactSicknessType
    {
        [Description("Not sick")]
        NotSick,
        [Description("Sick (not isolated)")]
        SickNotIsolated,
        [Description("Sick (isolated)")]
        SickIsolated,
        [Description("Sick (isolation unknown)")]
        Sick,
        [Description("Not recorded")]
        NotRecorded
    }
}
