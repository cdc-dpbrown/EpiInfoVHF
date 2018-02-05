using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactTracing.ImportExport
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

    public sealed class LinkRow
    {
        public string FromRecordGuid = String.Empty;
        public string ToRecordGuid = String.Empty;

        public int FromViewId;
        public int ToViewId;

        public string Relationship = String.Empty;

        public DateTime? LastContactDate;
        public int? ContactType;
        public bool? Tentative = false;
        public bool IsEstimatedContactDate = false;

        public int?[] DailyStatuses = new int?[21];
        public string[] DailyNotes = new string[21];

        public LinkRow()
        {
            
        }
    }
}
