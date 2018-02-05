using System;

namespace CDC.VHF.Foundation.Enums
{
    public enum ContactDailyStatus
    {
        SeenNotSick = 0,
        SeenSickAndIsolated = 1,
        SeenSickAndNotIsolated = 2,
        SeenSickAndIsoNotFilledOut = 3,
        NotSeen = 4,
        NotRecorded = 5,
        Unknown = 6,
        Dead = 7
    }
}
