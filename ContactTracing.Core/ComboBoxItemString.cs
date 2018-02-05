﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactTracing.Core
{
    /// This class provides us with an object to fill a ComboBox with
    /// that can be bound to string fields in the binding object.
    public class ComboBoxItemString
    {
        public string ValueString { get; set; }
        public string DisplayString { get; set; }
    }
}
