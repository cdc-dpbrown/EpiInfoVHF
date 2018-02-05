using System;
using CDC.VHF.Services;

namespace CDC.VHF.Domain
{
    /// <summary>
    /// Representation of a person who has had contact with a case patient
    /// </summary>
    public class ContactOfCase : Record
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public ContactOfCase()
            : base(System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString())
        {

        }
    }
}
