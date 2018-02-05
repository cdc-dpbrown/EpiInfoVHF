using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactTracing.ImportView
{
    public sealed class FollowUpRow
    {
        public Guid ContactGUID;
        public DateTime FollowUpDate;
        public int? StatusOnDate;
        public string Note;

        public double? Temp1;
        public double? Temp2;

        public FollowUpRow() 
        {
            Note = String.Empty;
        }
    }
}
