using System;
using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace CDC.VHF.Domain.Sms
{
    [Serializable()]
    public class SmsSenderInfoCollection : ObservableCollection<SmsSenderInfo>
    {
        public SmsSenderInfoCollection()
            : base()
        {
        }
    }
}
