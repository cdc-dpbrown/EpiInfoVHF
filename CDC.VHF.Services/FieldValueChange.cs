using System;

namespace CDC.VHF.Services
{
    public class FieldValueChange
    {
        public string FieldName { get; set; }
        public string OriginalValue { get; set; }
        public string NewValue { get; set; }
    }
}
