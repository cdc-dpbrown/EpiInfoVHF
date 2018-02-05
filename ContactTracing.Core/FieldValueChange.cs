using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactTracing.Core
{
    public class FieldValueChange
    {
        public string FieldName { get; set; }
        public string OriginalValue { get; set; }
        public string NewValue { get; set; }
    }
}
