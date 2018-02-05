using System;
using ContactTracing.ViewModel;

namespace ContactTracing.Sms
{
    public sealed class ProjectListViewModel : FileScreenViewModel
    {
        public ProjectListViewModel()
            : base()
        {
            PopulateCollections();
        }
    }
}
