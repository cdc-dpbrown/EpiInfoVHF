using System;

namespace CDC.VHF.Applications.Events
{
    public class CaseDeletedArgs : EventArgs
    {
        public CaseDeletedArgs(string caseGuid)
        {
            this.CaseGuid = caseGuid;
        }

        public string CaseGuid { get; private set; }
        //public ContactViewModel ContactVM { get; set; }
    }
}
