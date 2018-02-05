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
    public sealed class DailyFollowUpCollectionMaster : ObservableCollection<DailyCheckViewModel>, IDisposable
    {
        private Dictionary<ContactViewModel, DailyCheckViewModel> _master = new Dictionary<ContactViewModel, DailyCheckViewModel>();

        public bool ContainsContact(ContactViewModel contact)
        {
            return _master.ContainsKey(contact);
        }

        public DailyCheckViewModel this[ContactViewModel contact]
        {
            get
            {
                return _master[contact];
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                //foreach (DailyCheckViewModel caseVM in this)
                //{
                //    //caseVM.EpiCaseDefinitionChanging -= item_EpiCaseDefinitionChanging;
                //}
            }
            // free native resources if there are any.
        }

        protected override void InsertItem(int index, DailyCheckViewModel item)
        {
            //return;

            base.InsertItem(index, item);

            #region Input Validation
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            #endregion // Input Validation

            if (_master.ContainsKey(item.ContactVM))
            {
                DailyCheckViewModel currentDcVM = _master[item.ContactVM];
                throw new InvalidOperationException("Cannot add another daily check for an existing contact");
            }
            else
            {
                _master.Add(item.ContactVM, item);
            }
        }

        protected override void ClearItems()
        {
            base.ClearItems();
            _master.Clear();
        }

        protected override void RemoveItem(int index)
        {
            DailyCheckViewModel dailyCheckToRemove = this[index];
            _master.Remove(dailyCheckToRemove.ContactVM);
            base.RemoveItem(index);
        }

        protected override void SetItem(int index, DailyCheckViewModel item)
        {
            #region Input Validation
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            #endregion // Input Validation

            DailyCheckViewModel caseToChange = this[index];
            _master.Remove(caseToChange.ContactVM);

            _master.Add(caseToChange.ContactVM, item);

            base.SetItem(index, item);
        }
    }
}
