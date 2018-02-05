using System;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using ContactTracing.Core.Data;

namespace ContactTracing.Core
{
    [Serializable()]
    public sealed class SmsModule : ObservableObject
    {
        #region Members
        private SmsSenderInfoCollection _senders = new SmsSenderInfoCollection();
        private string _startupCommands = String.Empty;
        private bool _sendsReadReceipts = true;
        private bool _allowsSelfRegistration = true;
        private string _selfRegistrationCode = "8674009";
        private int _pollRate = 6000; // milliseconds
        private object _authorizedSendersLock = new object();
        #endregion // Members

        #region Properties
        /// <summary>
        /// Gets a collection of whitelisted phone numbers from which sent messages will be received. Any 
        /// numbers received from a number other than one listed in this collection will be rejected.
        /// </summary>
        [XmlArray("AuthorizedSmsSenders")]
        [XmlArrayItem("AuthorizedSmsSender", typeof(SmsSenderInfo))]
        public SmsSenderInfoCollection AuthorizedSmsSenders
        { 
            get { return this._senders; } 
            private set 
            { 
                this._senders = value;
                RaisePropertyChanged("AuthorizedSmsSenders");
            } 
        }

        [XmlElement]
        public string StartupCommands
        {
            get
            {
                return _startupCommands;
            }
            set
            {
                _startupCommands = value;
            }
        }

        [XmlElement]
        public bool SendsReadReceipts
        {
            get
            {
                return this._sendsReadReceipts;
            }
            set
            {
                if (SendsReadReceipts != value)
                {
                    _sendsReadReceipts = value;
                    RaisePropertyChanged("SendsReadReceipts");
                }
            }
        }

        [XmlElement]
        public bool AllowsSelfRegistration
        {
            get
            {
                return this._allowsSelfRegistration;
            }
            set
            {
                if (AllowsSelfRegistration != value)
                {
                    _allowsSelfRegistration = value;
                    RaisePropertyChanged("AllowsSelfRegistration");
                }
            }
        }

        [XmlElement]
        public string SelfRegistrationCode
        {
            get
            {
                return this._selfRegistrationCode;
            }
            set
            {
                if (SelfRegistrationCode != value)
                {
                    _selfRegistrationCode = value;
                    RaisePropertyChanged("SelfRegistrationCode");
                }
            }
        }

        /// <summary>
        /// Gets/sets the polling rate of the SMS controller (e.g. how long it takes to check for incoming messages) in milliseconds
        /// </summary>
        [XmlElement]
        public int PollRate
        {
            get
            {
                return this._pollRate;
            }
            set
            {
                if (PollRate != value)
                {
                    _pollRate = value;
                    RaisePropertyChanged("PollRate");
                }
            }
        }
        #endregion // Properties

        #region Constructors
        public SmsModule()
        {
            System.Windows.Data.BindingOperations.EnableCollectionSynchronization(AuthorizedSmsSenders, _authorizedSendersLock);
        }
        #endregion // Constructors

        #region Methods
        /// <summary>
        /// Executes a self-registration event. This occurs when an end-user sends a special 
        /// self-register code to the server
        /// </summary>
        /// <remarks>
        /// Self-registration eliminates the need for one person to sit in front of a server all
        /// day long entering SMS phone numbers for the possibly hundreds of contact tracing staff
        /// that are being hired
        /// </remarks>
        /// <param name="phoneNumber">The phone number of the person attempting to register their phone</param>
        /// <returns>bool; whether the self-register was successful</returns>
        internal bool ExecuteSmsSelfRegister(string phoneNumber)
        {
            if (String.IsNullOrEmpty(phoneNumber))
            {
                // invalid phone number
                return false;
            }

            if (AllowsSelfRegistration == false)
            {
                // we don't allow self-registration
                return false;
            }

            SmsSenderInfo senderInfo = new SmsSenderInfo(phoneNumber);

            foreach (SmsSenderInfo info in AuthorizedSmsSenders)
            {
                if (info.PhoneNumber.Equals(phoneNumber))
                {
                    // already exists, return false
                    return false;
                }
            }

            lock (_authorizedSendersLock)
            {
                AuthorizedSmsSenders.Add(senderInfo);
            }

            return true;
        }

        /// <summary>
        /// Checks to see if a given phone number is an authorized sender
        /// </summary>
        /// <param name="phoneNumber">The phone number to validate</param>
        /// <param name="updateType">The kind of update the sender is attempting (1 = case, 2 = contact, 3 = lab)</param>
        /// <returns>bool; whether this phone number is an authorized sender</returns>
        internal bool IsUserAuthorized(string phoneNumber, int updateType)
        {
            foreach (SmsSenderInfo info in AuthorizedSmsSenders)
            {
                if (info.PhoneNumber.Equals(phoneNumber, StringComparison.OrdinalIgnoreCase))
                {
                    // phone numbers match, now look for update permissions

                    switch (updateType)
                    {
                        case 1:
                            if (info.CanAddUpdateCases) return true;
                            break;
                        case 2:
                            if (info.CanAddUpdateContacts) return true;
                            break;
                        case 3:
                            if (info.CanAddUpdateLabResults) return true;
                            break;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks to see if a given phone number is in the list of authorized numbers
        /// </summary>
        /// <param name="phoneNumber">The phone number to validate</param>
        /// <returns>bool; whether this phone number is in the list of senders</returns>
        /// <remarks>Be careful using this method because it will return true if a number is in the list, and
        /// does not check for the type of update being requested. This method is intended to identify people, e.g.
        /// to see if a sender is recognized or not; not necessarily to tell if the sender is authorized.</remarks>
        internal bool IsUserInList(string phoneNumber)
        {
            foreach (SmsSenderInfo info in AuthorizedSmsSenders)
            {
                if (info.PhoneNumber.Equals(phoneNumber, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        #endregion // Methods
    }
}
