using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContactTracing.ViewModel
{
    public class ContactConversionInfo
    {
        public ContactViewModel ContactVM { get; private set; }
        public ContactFinalOutcome FinalOutcome { get; private set; }
        public DateTime DateIsolated { get; private set; }
        public bool IsDead { get; private set; }

        public ContactConversionInfo(ContactViewModel contactVM, ContactFinalOutcome finalOutcome)
        {
            this.ContactVM = contactVM;
            this.FinalOutcome = finalOutcome;
        }

        public ContactConversionInfo(ContactViewModel contactVM, ContactFinalOutcome finalOutcome, DateTime dateIsolated, bool died = false)
        {
            this.ContactVM = contactVM;
            this.FinalOutcome = finalOutcome;
            this.IsDead = died;

            // Can't set isolatation date if the outcome isn't isolation!
            if (FinalOutcome != ContactFinalOutcome.Isolated)
            {
                throw new ApplicationException();
            }

            DateIsolated = dateIsolated;
        }
    }
}
