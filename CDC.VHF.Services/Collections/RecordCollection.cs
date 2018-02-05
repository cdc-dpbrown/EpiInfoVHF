using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CDC.VHF.Foundation;

namespace CDC.VHF.Services.Collections
{
    public class RecordCollectionMaster : ObservableCollection<Record>, IDisposable
    {
        #region Members
        private Dictionary<string, Record> _master = new Dictionary<string, Record>(StringComparer.OrdinalIgnoreCase);
        #endregion // Members

        #region Properties
        /// <summary>
        /// Type Name
        /// </summary>
        /// <param name="name">Name</param>
        /// <returns>NamedObjectCollection</returns>
        public Record this[string id]
        {
            get
            {
                return _master[id];
            }
        }

        /// <summary>
        /// Type Name
        /// </summary>
        /// <param name="name">Name</param>
        /// <returns>NamedObjectCollection</returns>
        public Record this[Guid id]
        {
            get
            {
                return _master[id.ToString()];
            }
        }
        #endregion // Properties

        #region Methods

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
            // free native resources if there are any.
        }

        public virtual bool Contains(string id)
        {
            return _master.ContainsKey(id);
        }

        public virtual bool Contains(Guid id)
        {
            return _master.ContainsKey(id.ToString());
        }

        protected override void InsertItem(int index, Record item)
        {
            #region Input Validation
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            #endregion // Input Validation

            base.InsertItem(index, item);

            _master.Add(item.IdString, item);
        }

        protected virtual void CheckCaseForDuplicateId(Record record, string newValue)
        {
            if (String.IsNullOrEmpty(newValue.Trim()))
            {
                return;
            }

            foreach (Record r in this)
            {
                if (r.IdString.Equals(newValue, StringComparison.OrdinalIgnoreCase))
                {
                    if (r != record)
                    {
                        //record.IsDuplicateId = true;
                        return;
                    }
                }
            }

            //record.IsDuplicateId = false;
        }

        protected override void ClearItems()
        {
            _master.Clear();
            base.ClearItems();
        }

        protected override void RemoveItem(int index)
        {
            Record record = this[index];
            _master.Remove(record.IdString);
            base.RemoveItem(index);
        }

        protected override void SetItem(int index, Record record)
        {
            #region Input Validation
            if (record == null)
            {
                throw new ArgumentNullException("record");
            }
            #endregion // Input Validation

            Record recordToChange = this[index];
            _master.Remove(recordToChange.IdString);

            _master.Add(recordToChange.IdString, record);

            base.SetItem(index, record);
        }
        #endregion // Methods
    }
}
