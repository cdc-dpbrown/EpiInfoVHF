using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactTracing.Core.Data
{
    public class ServerUpdateMessage
    {
        public int Changeset { get; private set; }
        public string GUID { get; private set; }
        public string UserID { get; private set; }
        public string Description { get; private set; }
        public string RecordID { get; private set; }
        public DateTime CheckinDate { get; private set; }
        public Core.Enums.ServerUpdateType UpdateType { get; private set; }

        public ServerUpdateMessage(int changeset, string guid, string userID, string description, string recordID, DateTime checkinDate, Core.Enums.ServerUpdateType updateType)
        {
            Changeset = changeset;
            GUID = guid;
            UserID = userID;
            Description = description;
            RecordID = recordID;
            CheckinDate = checkinDate;
            UpdateType = updateType;
        }
    }
}
