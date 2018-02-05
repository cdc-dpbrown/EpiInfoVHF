using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Epi;

namespace ContactTracing.ImportExport
{
    public struct PackageCaseFieldData
    {
        public string RecordGUID;
        public string RecordCaseId;
        public string RecordLabId;
        public string FieldName;
        public object FieldValue { get; set; }
        public Page Page;
    }
}
