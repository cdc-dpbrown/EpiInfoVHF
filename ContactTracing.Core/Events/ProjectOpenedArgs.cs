using System;
using System.IO;
using ContactTracing.Core.Data;

namespace ContactTracing.Core.Events
{
    public class ProjectOpenedArgs : EventArgs
    {
        public ProjectOpenedArgs(ProjectInfo projectInfo, bool superUser = false)
        {
            this.ProjectInfo = projectInfo;
            this.SuperUser = superUser;
        }

        public ProjectInfo ProjectInfo { get; private set; }
        public bool SuperUser { get; private set; }
    }
}
