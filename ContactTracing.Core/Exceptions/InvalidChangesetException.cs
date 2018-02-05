using System;

namespace ContactTracing.Core.Exceptions
{
    [Serializable()]
    public class InvalidChangesetException : Exception
    {
        private string _extraInfo;
        
        public string ExtraErrorInfo
        {
            get
            {
                return _extraInfo;
            }

            set
            {
                _extraInfo = value;
            }
        }

        public InvalidChangesetException()
            : base()
        {

        }

        public InvalidChangesetException(string message)
            : base(message)
        {

        }

        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}
