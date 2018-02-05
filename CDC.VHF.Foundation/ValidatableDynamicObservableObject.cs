using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using CDC.VHF.Foundation.Collections;

namespace CDC.VHF.Foundation
{
    public class ValidatableDynamicObservableObject : DynamicObject, INotifyPropertyChanged, INotifyDataErrorInfo
    {
        #region Members
        private RecordErrorDictionary _errors = new RecordErrorDictionary();
        private object _lock = new object();
        /// <summary>
        /// The dictionary that is the under-the-hood mechanism of storing dynamic properties and their values
        /// </summary>
        private Dictionary<string, object> _data = new Dictionary<string, object>();

        /// <summary>
        /// The dictionary that stores what fields have had their values updated since the last save (or load)
        /// </summary>
        private Dictionary<string, object> _dataChanges = new Dictionary<string, object>();
        #endregion // Members

        #region Events
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion // Events

        #region Properties
        /// <summary>
        /// Whether or not this object runs validation
        /// </summary>
        protected bool DoesValidation { get; set; }

        /// <summary>
        /// The under-the-hood mechanism of storing dynamic properties and their values
        /// </summary>
        protected Dictionary<string, object> Data { get { return this._data; } }

        /// <summary>
        /// Stores what fields have had their values updated since the last save (or load)
        /// </summary>
        protected Dictionary<string, object> DataChanges { get { return this._dataChanges; } }

        /// <summary>
        /// Stores errors for each field
        /// </summary>
        //public ConcurrentDictionary<string, List<string>> Errors { get { return this._errors; } }
        public RecordErrorDictionary Errors { get { return this._errors; } }

        /// <summary>
        /// Record locking object for async operations
        /// </summary>
        protected object Lock { get { return this._lock; } }

        /// <summary>
        /// Gets/sets whether error notification should be suppressed. Only set to TRUE in special circumstances.
        /// </summary>
        internal bool SuppressErrorNotification { get; set; }

        /// <summary>
        /// Gets whether the record currently has any errors
        /// </summary>
        public virtual bool HasErrors
        {
            get
            {
                return Errors.Any(kv => kv.Value != null && kv.Value.Count > 0);
            }
        }
        #endregion // Properties

        #region Methods

        internal virtual void EnableValidation()
        {
            DoesValidation = true;
        }

        internal virtual void DisableValidation()
        {
            DoesValidation = false;
        }

        public virtual Task ValidateAsync()
        {
            return Task.Run(() => Validate());
        }

        public virtual void Validate()
        {
        }

        public virtual void OnErrorsChanged(string propertyName)
        {
            var handler = ErrorsChanged;
            if (handler != null)
                handler(this, new DataErrorsChangedEventArgs(propertyName));
        }


        /// <summary>
        /// Gets the errors for a given property
        /// </summary>
        /// <param name="propertyName">The property to check</param>
        /// <returns>list of errors</returns>
        public virtual IEnumerable GetErrors(string propertyName)
        {
            List<string> errorsForName = new List<string>();
            Errors.TryGetValue(propertyName, out errorsForName);
            return errorsForName;
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            var handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, e);
            }

            if (!SuppressErrorNotification)
            {
                Validate();
            }
        }

        protected virtual void OnNonValidatingPropertyChanged(PropertyChangedEventArgs e)
        {
            var handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void RaisePropertyChanged<T>(Expression<Func<T>> propertyExpresssion)
        {
            var propertyName = PropertySupport.ExtractPropertyName(propertyExpresssion);
            this.RaisePropertyChanged(propertyName);
        }

        protected virtual void RaisePropertyChanged(String propertyName)
        {
            VerifyPropertyName(propertyName);
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void RaiseNonValidatingPropertyChanged(String propertyName)
        {
            VerifyPropertyName(propertyName);
            OnNonValidatingPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Warns the developer if this Object does not have a public property with
        /// the specified name. This method does not exist in a Release build.
        /// </summary>
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public virtual void VerifyPropertyName(String propertyName)
        {
            // TODO: Re-implement in Windows RT / WP 8.1 API

            //bool foundProperty = true;
            // verify that the property name matches a real,  
            // public, instance property on this Object.
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
        #endregion // Methods
    }
}
