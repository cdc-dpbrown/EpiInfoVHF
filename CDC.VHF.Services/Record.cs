using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Epi.Fields;
using CDC.VHF.Foundation;

namespace CDC.VHF.Services
{
    public class Record : ValidatableDynamicObservableObject, IEntity
    {
        #region Members
        /// <summary>
        /// The foreign key for this record
        /// </summary>
        private Guid _foreignKey = new Guid("00000000000000000000000000000000");

        /// <summary>
        /// Whether or not this record has unsaved changes
        /// </summary>
        private bool _hasUnsavedChanges = false;

        /// <summary>
        /// Whether or not this record exists in the database
        /// </summary>
        private bool _isInDatabase = false;
        #endregion // Members

        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        public Record(string userName)
        {
            // Set the GUID value and other properties that are always going to be common across data sets
            this.Id = System.Guid.NewGuid();
            this.IdString = Id.ToString();
            this.RecStatus = 1;
            this.FirstSaveTime = DateTime.Now;
            this.FirstSaveLogOn = userName;
            this.LastSaveTime = DateTime.Now;
            this.LastSaveLogOn = userName;
            //this.SuppressErrorNotification = true;
            //this.EnableValidation();
            // Note we can't set FKEY here because we don't have a parent form reference, so this has to be attached
            // later when we're dealing with child records.
        }
        #endregion // Constructors

        /// <summary>
        /// Gets whether or not this record has unsaved changes
        /// </summary>
        public bool HasUnsavedChanges
        {
            get
            {
                return this._hasUnsavedChanges;
            }
            internal set
            {
                if (value != HasUnsavedChanges)
                {
                    this._hasUnsavedChanges = value;
                    RaiseNonValidatingPropertyChanged("HasUnsavedChanges");
                    if (HasUnsavedChanges == false)
                    {
                        DataChanges.Clear();
                    }
                }
            }
        }

        /// <summary>
        /// Gets whether or not this record exists in the database
        /// </summary>
        /// <remarks>
        /// This property doesn't specify whether the record in memory matches the record on disk - only 
        /// that the GUIDs match. The purpose of this is to figure out which records in memory should be
        /// used in update queries and which ones should be used in insertion queries when a save (or
        /// similar operation) is initiated.
        /// </remarks>
        public bool IsInDatabase
        {
            get
            {
                return this._isInDatabase;
            }
            internal set
            {
                if (value != IsInDatabase)
                {
                    this._isInDatabase = value;
                    RaisePropertyChanged("IsInDatabase");
                }
            }
        }

        /// <summary>
        /// The ID for this record
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The time the record was first saved
        /// </summary>
        public DateTime FirstSaveTime { get; set; }

        /// <summary>
        /// The time the record was last saved
        /// </summary>
        public DateTime LastSaveTime { get; set; }

        /// <summary>
        /// The user ID of the person who created the record
        /// </summary>
        public string FirstSaveLogOn { get; set; }

        /// <summary>
        /// The user ID of the person who last saved the record
        /// </summary>
        public string LastSaveLogOn { get; set; }

        /// <summary>
        /// The record status (currently 1= active, 0= soft-deleted, to mimic Epi Info 7)
        /// </summary>
        public short RecStatus { get; set; }

        /// <summary>
        /// String representation of the foreign key
        /// </summary>
        public string ForeignKeyString { get; private set; }

        /// <summary>
        /// String representation of the record ID
        /// </summary>
        public string IdString { get; private set; }

        /// <summary>
        /// The foreign key value; this should match an ID value of a record in a parent form
        /// </summary>
        public Guid ForeignKey
        {
            get
            {
                return this._foreignKey;
            }
            set
            {
                this._foreignKey = value;
                ForeignKeyString = ForeignKey.ToString();
                RaisePropertyChanged("ForeignKey");
            }
        }

        public string this[string propertyName]
        {
            get
            {
                string result = string.Empty;

                // TODO: Add validation, etc?

                return result;
            }
        }

        public override void Validate()
        {
            if (!DoesValidation)
            {
                return;
            }

            //base.Validate();
            Errors.Clear();

            List<string> fieldNames = Data.Keys.ToList();
            //foreach (KeyValuePair<string, object> kvp in _data)
            foreach (string fieldName in fieldNames)
            {
                string message = this[fieldName].Trim();
                if (!String.IsNullOrEmpty(message))
                {
                    if (Errors.ContainsKey(fieldName))
                    {
                        List<string> messages = new List<string>();
                        bool success = Errors.TryGetValue(fieldName, out messages);
                        if (success)
                        {
                            messages.Add(message);
                            OnErrorsChanged(fieldName);
                        }
                    }
                    else
                    {
                        List<string> messages = new List<string>() { message };
                        Errors.TryAdd(fieldName, messages);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the data for a given field
        /// </summary>
        /// <param name="field">The field</param>
        /// <returns>data</returns>
        public object GetFieldData(IField field)
        {
            foreach (KeyValuePair<string, object> kvp in this.Data)
            {
                if (kvp.Key.Equals(field.Name))
                {
                    return kvp.Value;
                }
            }
            return null;
        }

        /// <summary>
        /// Adds or updates data for a given field on this record
        /// </summary>
        /// <param name="field">The field in the record to update</param>
        /// <param name="value">The value of the field</param>
        /// <returns>bool; represents whether the operation was successful</returns>
        public bool AddOrUpdateFieldData(IField field, object value)
        {
            string fieldName = field.Name;
            if (Data.ContainsKey(fieldName)) // we already have this property, so update it with the data
            {
                return UpdateFieldData(field, value);
            }
            else // we don't have already this property, so add it to the internal representation of properties
            {
                return AddFieldData(field, value);
            }
        }

        private bool AddFieldData(IField field, object value)
        {
            string fieldName = field.Name;
            if (Data.ContainsKey(fieldName))
            {
                throw new InvalidOperationException("_data already contains this object");
            }
            else
            {
                FlagFieldAsChanged(fieldName, null);
                //HasUnsavedChanges = true;
                Data.Add(fieldName, value);

                return true;
            }
        }

        private void FlagFieldAsChanged(string fieldName, object previousValue)
        {
            if (fieldName.Equals("SuppressErrorNotification", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!DataChanges.ContainsKey(fieldName))
            {
                DataChanges.Add(fieldName, previousValue);
                HasUnsavedChanges = true;
            }
        }

        private bool UpdateFieldData(IField field, object value)
        {
            string fieldName = field.Name;
            if (Data.ContainsKey(fieldName))
            {
                if (Data[fieldName] != null && !Data[fieldName].Equals(value))
                {
                    FlagFieldAsChanged(fieldName, Data[fieldName]);
                    //HasUnsavedChanges = true;
                }
                Data[fieldName] = value;
                RaisePropertyChanged(fieldName);
                return true;
            }
            else
            {
                throw new InvalidOperationException("_data does not contain this object");
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            string fieldName = binder.Name;
            if (Data.Keys.Contains(fieldName))
            {
                object value = Data[fieldName];

                result = value;
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        public override bool TrySetMember(SetMemberBinder binder, object result)
        {
            string fieldName = binder.Name;
            if (fieldName.Equals("SuppressErrorNotification"))
            {
                //SuppressErrorNotification = (bool)result;
            }
            if (Data.Keys.Contains(fieldName))
            {
                if ((Data[fieldName] != null && !Data[fieldName].Equals(result)) ||
                    (Data[fieldName] == null && result != null))
                {
                    FlagFieldAsChanged(fieldName, Data[fieldName]);
                    //HasUnsavedChanges = true;
                    Data[fieldName] = result;
                    RaisePropertyChanged(binder.Name);
                }
            }
            else
            {
                FlagFieldAsChanged(fieldName, null);
                //HasUnsavedChanges = true;
                Data.Add(fieldName, result);
            }

            return true;
        }
    }
}
