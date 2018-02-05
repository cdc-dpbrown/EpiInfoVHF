using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactTracing.SyncFileViewer
{
    public class Record : DynamicObject
    {
        private Dictionary<string, object> _properties = new Dictionary<string, object>();

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            string name = binder.Name.ToLower();

            return _properties.TryGetValue(name, out result);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            _properties[binder.Name.ToLower()] = value;

            return true;
        }

        public void AddField(string fieldName, object value)
        {
            if (_properties.ContainsKey(fieldName))
            {
                _properties[fieldName] = value;
            }
            else
            {
                _properties.Add(fieldName, value);
            }
        }
    }
}
