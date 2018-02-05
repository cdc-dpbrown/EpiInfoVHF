using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Collections.Specialized;
using System.Text;
using System.Windows.Input;
using Epi;
using Epi.Data;
using Epi.Fields;
using ContactTracing.Core;

namespace ContactTracing.ViewModel
{
    public interface IDataHelper
    {
        View CaseForm { get; set; }
        VhfProject Project { get; set; }

        void RepopulateCollections(bool initialLoad = false);
        bool ExportCases(string fileName);
    }
}
