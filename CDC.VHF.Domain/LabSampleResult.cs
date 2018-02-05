using System;
using CDC.VHF.Services;

namespace CDC.VHF.Domain
{
    /// <summary>
    /// Representation of a laboratory sample result that was tested (or will be tested) for viral hemorrhagic fever
    /// </summary>
    public class LabSampleResult : Record
    {
        public string FieldLabSpecimenID { get; set; }

        public LabSampleResult()
            : base(System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString())
        {

        }
    }
}
