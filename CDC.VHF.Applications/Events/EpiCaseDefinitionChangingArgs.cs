using System;
using CDC.VHF.Foundation.Enums;

namespace CDC.VHF.Applications.Events
{
    public sealed class EpiCaseDefinitionChangingEventArgs : EventArgs
    {
        public EpiCaseDefinitionChangingEventArgs(EpiCaseClassification previousDefinition, EpiCaseClassification newDefinition)
        {
            this.PreviousDefinition = previousDefinition;
            this.NewDefinition = newDefinition;
        }

        public EpiCaseClassification PreviousDefinition { get; private set; }
        public EpiCaseClassification NewDefinition { get; private set; }
    }
}
