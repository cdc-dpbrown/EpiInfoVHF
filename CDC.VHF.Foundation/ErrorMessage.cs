using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDC.VHF.Foundation
{
    public sealed class ErrorMessage : ObservableObject
    {
        private string _key = String.Empty;
        private string _message = String.Empty;

        public string Key
        {
            get
            {
                return this._key;
            }
            set
            {
                if (!Key.Equals(value))
                {
                    this._key = value;
                    RaisePropertyChanged("Key");
                }
            }
        }

        public string Message
        {
            get
            {
                return this._message;
            }
            set
            {
                if (!Message.Equals(value))
                {
                    this._message = value;
                    RaisePropertyChanged("Message");
                }
            }
        }

        public ErrorMessage(string fieldName, string message)
        {
            this.Message = message;
            this.Key = fieldName;
        }
    }
}
