using System;
using CDC.VHF.Services;

namespace CDC.VHF.Domain
{
    /// <summary>
    /// Representation of a person who has, is suspected of having, had in the past, or was suspected of having in the past, viral hemorrgahic fever. 
    /// </summary>
    public class CasePatient : Record
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public CasePatient()
            : base(System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString())
        {

        }
    }
}
