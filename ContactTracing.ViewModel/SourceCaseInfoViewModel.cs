using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using ContactTracing.Core;
using ContactTracing.Core.Collections;
using ContactTracing.ViewModel.Collections;
using ContactTracing.ViewModel.Events;
using Epi;
using Epi.Data;
using Epi.Fields;

namespace ContactTracing.ViewModel
{
    public class SourceCaseInfoViewModel : ObservableObject
    {
        private string _relationship = String.Empty;
        private string _lastName = String.Empty;
        private string _firstName = String.Empty;
        private string _id = String.Empty;
        private string _adm1 = String.Empty;
        private string _adm4 = String.Empty;
        private DateTime? _dateLastContact;
        private bool _estimated = false;
        private bool _tentative = false;
        private DateTime? _dateDeath;
        private string _status = String.Empty;
        private string _recordId = String.Empty;

        public string Relationship
        {
            get
            {
                return _relationship;
            }
            set
            {
                if (_relationship != value)
                {
                    _relationship = value;
                    RaisePropertyChanged("Relationship");
                }
            }
        }
        public string LastName
        {
            get
            {
                return _lastName;
            }
            set
            {
                if (_lastName != value)
                {
                    _lastName = value;
                    RaisePropertyChanged("LastName");
                }
            }
        }
        public string FirstName
        {
            get
            {
                return _firstName;
            }
            set
            {
                if (_firstName != value)
                {
                    _firstName = value;
                    RaisePropertyChanged("FirstName");
                }
            }
        }
        public string ID
        {
            get
            {
                return _id;
            }
            set
            {
                if (_id != value)
                {
                    _id = value;
                    RaisePropertyChanged("ID");
                }
            }
        }
        public string Adm1
        {
            get
            {
                return _adm1;
            }
            set
            {
                if (_adm1 != value)
                {
                    _adm1 = value;
                    RaisePropertyChanged("Adm1");
                }
            }
        }
        public string Adm4
        {
            get
            {
                return _adm4;
            }
            set
            {
                if (_adm4 != value)
                {
                    _adm4 = value;
                    RaisePropertyChanged("Adm4");
                }
            }
        }
        public DateTime? DateLastContact
        {
            get
            {
                return _dateLastContact;
            }
            set
            {
                if (_dateLastContact != value)
                {
                    _dateLastContact = value;
                    RaisePropertyChanged("DateLastContact");
                }
            }
        }
        public bool Estimated
        {
            get
            {
                return _estimated;
            }
            set
            {
                if (_estimated != value)
                {
                    _estimated = value;
                    RaisePropertyChanged("Estimated");
                }
            }
        }
        public bool Tentative
        {
            get
            {
                return _tentative;
            }
            set
            {
                if (_tentative != value)
                {
                    _tentative = value;
                    RaisePropertyChanged("Tentative");
                }
            }
        }
        public DateTime? DateDeath
        {
            get
            {
                return _dateDeath;
            }
            set
            {
                if (_dateDeath != value)
                {
                    _dateDeath = value;
                    RaisePropertyChanged("DateDeath");
                }
            }
        }
        public string Status
        {
            get
            {
                return _status;
            }
            set
            {
                if (_status != value)
                {
                    _status = value;
                    RaisePropertyChanged("Status");
                }
            }
        }
        public string RecordId
        {
            get
            {
                return _recordId;
            }
            set
            {
                if (_recordId != value)
                {
                    _recordId = value;
                    RaisePropertyChanged("RecordId");
                }
            }
        }

        public SourceCaseInfoViewModel()
            : base()
        {
            RecordId = System.Guid.NewGuid().ToString();
        }
    }
}
