using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ContactTracing.Core;
using ContactTracing.ViewModel;

namespace ContactTracing.ViewModel.Collections
{
    public class ContactCollectionMaster : ObservableCollection<ContactViewModel>
    {
        #region Members
        private Dictionary<string, ContactViewModel> _master = new Dictionary<string, ContactViewModel>(StringComparer.OrdinalIgnoreCase);
        #endregion // Members

        #region Properties
        /// <summary>
        /// Type Name
        /// </summary>
        /// <param name="name">Name</param>
        /// <returns>NamedObjectCollection</returns>
        public ContactViewModel this[string id]
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
        public ContactViewModel this[Guid id]
        {
            get
            {
                return _master[id.ToString()];
            }
        }
        #endregion // Properties

        #region Methods
        public virtual bool Contains(string id)
        {
            return _master.ContainsKey(id);
        }

        public virtual bool Contains(Guid id)
        {
            return _master.ContainsKey(id.ToString());
        }

        protected override void InsertItem(int index, ContactViewModel item)
        {

            #region Input Validation
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (_master.ContainsKey(item.RecordId))//Error when trying to add existing item. VHF-259
            {
                return;
            }
            #endregion // Input Validation

            base.InsertItem(index, item);
            _master.Add(item.RecordId, item);
        }

        protected override void ClearItems()
        {
            _master.Clear();

            base.ClearItems();
        }

        protected override void RemoveItem(int index)
        {
            ContactViewModel itemToRemove = this[index];
            _master.Remove(itemToRemove.RecordId);
            base.RemoveItem(index);
        }

        protected override void SetItem(int index, ContactViewModel item)
        {
            #region Input Validation
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            #endregion // Input Validation

            ContactViewModel itemToChange = this[index];
            _master.Remove(itemToChange.RecordId);

            _master.Add(itemToChange.RecordId, item);

            base.SetItem(index, item);
        }
        #endregion // Methods
    }
}
