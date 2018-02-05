using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq.Expressions;
using CDC.VHF.Foundation;

namespace CDC.VHF.Foundation.Collections
{
    public sealed class RecordErrorDictionary : ObservableConcurrentDictionary<string, List<string>>, INotifyPropertyChanged
    {
        private bool _hasErrors = false;
        private ObservableCollection<ErrorMessage> _errorMessageCollection = new ObservableCollection<ErrorMessage>();

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

        public ObservableCollection<ErrorMessage> ErrorMessageCollection
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
                    ErrorMessage errorMessage = new ErrorMessage(kvp.Key, message);
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

        }
    }
}
