using System;
using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace ContactTracing.Core.Data
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
