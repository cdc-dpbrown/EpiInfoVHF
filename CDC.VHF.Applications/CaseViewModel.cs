using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CDC.VHF.Domain;
using CDC.VHF.Applications.Events;

namespace CDC.VHF.Applications
{
    public class CaseViewModel : CasePatient
    {
        #region Events
        public delegate void EpiCaseDefinitionChangingHandler(object sender, EpiCaseDefinitionChangingEventArgs e);
        public delegate void FieldValueChangingHandler(object sender, FieldValueChangingEventArgs e);

        [field: NonSerialized]
        public event EpiCaseDefinitionChangingHandler EpiCaseDefinitionChanging;
        [field: NonSerialized]
        public event FieldValueChangingHandler CaseIDChanging;
        [field: NonSerialized]
        public event EventHandler MarkedForRemoval;
        [field: NonSerialized]
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        [field: NonSerialized]
        public event EventHandler<CaseAddedArgs> Inserted;
        [field: NonSerialized]
        public event EventHandler<CaseChangedArgs> Updated;
        #endregion Events
    }
}
