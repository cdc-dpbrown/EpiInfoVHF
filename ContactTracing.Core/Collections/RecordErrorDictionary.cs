using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq.Expressions;
using ContactTracing.Core.Collections;

namespace ContactTracing.Core.Collections
{
    public sealed class RecordErrorMessage : ObservableObject
    {
        private string _fieldName = String.Empty;
        private string _errorMessage = String.Empty;

        public string FieldName
        {
            get
            {
                return this._fieldName;
            }
            set
            {
                if (!FieldName.Equals(value))
                {
                    this._fieldName = value;
                    RaisePropertyChanged("FieldName");
                }
            }
        }

        public string ErrorMessage
        {
            get
            {
                return this._errorMessage;
            }
            set
            {
                if (!ErrorMessage.Equals(value))
                {
                    this._errorMessage = value;
                    RaisePropertyChanged("ErrorMessage");
                }
            }
        }

        public RecordErrorMessage(string fieldName, string errorMessage)
        {
            this.ErrorMessage = errorMessage;
            this.FieldName = fieldName;
        }
    }

    public sealed class RecordErrorDictionary : ObservableConcurrentDictionary<string, List<string>>, INotifyPropertyChanged
    {
        private bool _hasErrors = false;
        private ObservableCollection<RecordErrorMessage> _errorMessageCollection = new ObservableCollection<RecordErrorMessage>();

        public bool HasErrors
        {
            get
            {
                return this._hasErrors;
            }
            set
            {
                if (HasErrors != value)
                {
                    this._hasErrors = value;
                    RaisePropertyChanged("HasErrors");
                }
            }
        }

        public ObservableCollection<RecordErrorMessage> ErrorMessageCollection
        {
            get
            {
                return this._errorMessageCollection;
            }
            set
            {
                this._errorMessageCollection = value;
                RaisePropertyChanged("ErrorMessageCollection");
            }
        }

        private void RegenerateErrorMessageCollection()
        {
            ErrorMessageCollection.Clear();

            foreach (KeyValuePair<string, List<string>> kvp in this)
            {
                foreach (string message in kvp.Value)
                {
                    RecordErrorMessage errorMessage = new RecordErrorMessage(kvp.Key, message);
                    ErrorMessageCollection.Add(errorMessage);
                }
            }
        }

        public new event PropertyChangedEventHandler PropertyChanged;

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);

            if (this.Count > 0)
            {
                HasErrors = true;
            }
            else
            {
                HasErrors = false;
            }

            RegenerateErrorMessageCollection();
        }

        private void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            var handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void RaisePropertyChanged<T>(Expression<Func<T>> propertyExpresssion)
        {
            var propertyName = PropertySupport.ExtractPropertyName(propertyExpresssion);
            this.RaisePropertyChanged(propertyName);
        }

        private void RaisePropertyChanged(String propertyName)
        {
            VerifyPropertyName(propertyName);
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Warns the developer if this Object does not have a public property with
        /// the specified name. This method does not exist in a Release build.
        /// </summary>
        //[Conditional("DEBUG")]
        //[DebuggerStepThrough]
        public void VerifyPropertyName(String propertyName)
        {
            //bool foundProperty = true;
            //// verify that the property name matches a real,  
            //// public, instance property on this Object.
            //if (TypeDescriptor.GetProperties(this)[propertyName] == null)
            //{
            //    foundProperty = false;
            //}

            //foreach (KeyValuePair<string, object> kvp in _data)
            //{
            //    if (kvp.Key == propertyName)
            //    {
            //        foundProperty = true;
            //    }
            //}

            //if (!foundProperty)
            //{
            //    Debug.Fail("Invalid property name: " + propertyName);
            //}
        }
    }
}
