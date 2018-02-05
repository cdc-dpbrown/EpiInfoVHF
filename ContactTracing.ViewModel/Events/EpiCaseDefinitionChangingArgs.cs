using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactTracing.ViewModel.Events
{
    public sealed class EpiCaseDefinitionChangingEventArgs : EventArgs
    {
        public EpiCaseDefinitionChangingEventArgs(Core.Enums.EpiCaseClassification previousDefinition, Core.Enums.EpiCaseClassification newDefinition)
        {
            this.PreviousDefinition = previousDefinition;
            this.NewDefinition = newDefinition;
        }

        public Core.Enums.EpiCaseClassification PreviousDefinition { get; private set; }
        public Core.Enums.EpiCaseClassification NewDefinition { get; private set; }
    }
}
