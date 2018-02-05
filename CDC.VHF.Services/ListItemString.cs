using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactTracing.Core
{
    /// <summary>
    /// This class provides an object to fill list-based fields (e.g. WPF ComboBoxes)
    /// that can be bound to string fields in the binding object.
    /// </summary>
    public class ListItemString
    {
        public string ValueString { get; set; }
        public string DisplayString { get; set; }
    }
}
