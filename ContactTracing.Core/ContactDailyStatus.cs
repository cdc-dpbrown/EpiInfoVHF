using System;
using System.ComponentModel;

namespace ContactTracing.Core
{
    public enum ContactDailyStatus
    {
        [Description("Seen and not sick")]
        SeenNotSick = 0,
        [Description("Seen and sick, isolated")]
        SeenSickAndIsolated = 1,
        [Description("Seen and sick, not isolated")]
        SeenSickAndNotIsolated = 2,
        [Description("Seen and sick, isolated not filled out")]
        SeenSickAndIsoNotFilledOut = 3,
        [Description("Not seen")]
        NotSeen = 4,
        [Description("Not recorded")]
        NotRecorded = 5,
        [Description("Unknown")]
        Unknown = 6,
        [Description("Dead")]
        Dead = 7
    }
}
