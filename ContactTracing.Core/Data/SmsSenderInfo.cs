using System;
using System.Xml.Serialization;
using ContactTracing.Core;

namespace ContactTracing.Core.Data
{
    /// <summary>
    /// A class representation of an SMS sender, who will be sending messages to this SMS server
    /// </summary>
    [Serializable()]
    public class SmsSenderInfo : ObservableObject
    {
        #region Members
        public string _phoneNumber = String.Empty;
        public string _name = String.Empty;
        public bool _canAddUpdateCases = true;
        public bool _canAddUpdateContacts = true;
        public bool _canAddUpdateLabResults = true;
        #endregion // Members

        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="phoneNumber">The phone number of this sender</param>
        public SmsSenderInfo(string phoneNumber)
        {
            this.PhoneNumber = phoneNumber;
        }
        #endregion // Constructors

        #region Properties
        /// <summary>
        /// Gets/sets the phone number of this sender
        /// </summary>
        [XmlElement]
        public string PhoneNumber
        {
            get
            {
                return this._phoneNumber;
            }
            set
            {
                this._phoneNumber = value;
                RaisePropertyChanged("PhoneNumber");
            }
        }

        /// <summary>
        /// Gets/sets the name of the sender
        /// </summary>
        [XmlElement]
        public string Name
        {
            get
            {
                return this._name;
            }
            set
            {
                this._name = value;
                RaisePropertyChanged("Name");
            }
        }

        /// <summary>
        /// Gets/sets whether or not this sender can add and update case data
        /// </summary>
        [XmlElement]
        public bool CanAddUpdateCases
        {
            get
            {
                return this._canAddUpdateCases;
            }
            set
            {
                this._canAddUpdateCases = value;
                RaisePropertyChanged("CanAddUpdateCases");
            }
        }

        /// <summary>
        /// Gets/sets whether or not this sender can add and update contact data (including contact daily follow-up data)
        /// </summary>
        [XmlElement]
        public bool CanAddUpdateContacts
        {
            get
            {
                return this._canAddUpdateContacts;
            }
            set
            {
                this._canAddUpdateContacts = value;
                RaisePropertyChanged("CanAddUpdateContacts");
            }
        }

        /// <summary>
        /// Gets/sets whether or not this sender can add lab results
        /// </summary>
        [XmlElement]
        public bool CanAddUpdateLabResults
        {
            get
            {
                return this._canAddUpdateLabResults;
            }
            set
            {
                this._canAddUpdateLabResults = value;
                RaisePropertyChanged("CanAddUpdateLabResults");
            }
        }
        #endregion Properties
    }
}
