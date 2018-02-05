using System;

namespace ContactTracing.Core.Enums
{
    public enum EpiCaseClassification
    {
        NotCase = 0,
        Confirmed = 1,
        Probable = 2,
        Suspect = 3,
        Excluded = 4,
        PUI = 5,
        None = 99
    }
}
