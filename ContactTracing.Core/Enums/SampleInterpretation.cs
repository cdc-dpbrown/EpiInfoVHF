using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactTracing.Core.Enums
{
    public enum SampleInterpretation
    {
        ConfirmedAcute = 1,
        ConfirmedConvalescent = 2,
        Negative = 3,
        Indeterminate = 4,
        NegativeNeedFollowUp = 5,
        None = 6
    }
}
