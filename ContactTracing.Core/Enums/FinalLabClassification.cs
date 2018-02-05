using System;

namespace ContactTracing.Core.Enums
{
    public enum FinalLabClassification
    {
        NotCase = 0,
        ConfirmedAcute = 1,
        ConfirmedConvalescent = 2,
        Indeterminate = 3,
        NeedsFollowUpSample = 4,
        None = 5
    }
}
