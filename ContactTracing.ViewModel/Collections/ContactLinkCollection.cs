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
    public sealed class ContactLinkCollectionMaster : ObservableCollection<CaseContactPairViewModel>, IDisposable
    {
        // Warning: This master list is only useful for the initial load. Code has not been written that would make this useful outside of those initial loads.
        private Dictionary<ContactViewModel, CaseContactPairViewModel> _master = new Dictionary<ContactViewModel, CaseContactPairViewModel>();

        #region Methods

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                //foreach (CaseViewModel caseVM in this)
                //{
                //    caseVM.EpiCaseDefinitionChanging -= item_EpiCaseDefinitionChanging;
                //}
            }
            // free native resources if there are any.
        }

        public bool ContainsContact(ContactViewModel contact)
        {
            return _master.ContainsKey(contact);
        }

        public CaseContactPairViewModel this[ContactViewModel contact]
        {
            get
            {
                return _master[contact];
            }
        }

        protected override void InsertItem(int index, CaseContactPairViewModel item)
        {
            //return;


            #region Input Validation
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
           
            #endregion // Input Validation

            base.InsertItem(index, item);

            if (_master.ContainsKey(item.ContactVM))
            {
                CaseContactPairViewModel currentCcp = _master[item.ContactVM];
                if (item.DateLastContact > currentCcp.DateLastContact)
                {
                    _master[item.ContactVM] = item;
                }
            }
            else
            {
                _master.Add(item.ContactVM, item);
            }

            //_master.Add(item.RecordId, item);

            //item.EpiCaseDefinitionChanging += item_EpiCaseDefinitionChanging;
        }

        //void item_EpiCaseDefinitionChanging(object sender, Events.EpiCaseDefinitionChangingEventArgs e)
        //{
        //    CaseViewModel caseVM = sender as CaseViewModel;

        //    if (e.PreviousDefinition == CaseViewModel.Confirmed)
        //    {
        //        Confirmed.Remove(caseVM);
        //    }
        //    else if (e.PreviousDefinition == CaseViewModel.Probable)
        //    {
        //        Probable.Remove(caseVM);
        //    }
        //    else if (e.PreviousDefinition == CaseViewModel.Suspect)
        //    {
        //        Suspect.Remove(caseVM);
        //    }
        //    else if (e.PreviousDefinition == CaseViewModel.NotCase)
        //    {
        //        NotCase.Remove(caseVM);
        //    }

        //    if (e.NewDefinition == CaseViewModel.Confirmed)
        //    {
        //        Confirmed.Add(caseVM);
        //    }
        //    else if (e.NewDefinition == CaseViewModel.Probable)
        //    {
        //        Probable.Add(caseVM);
        //    }
        //    else if (e.NewDefinition == CaseViewModel.Suspect)
        //    {
        //        Suspect.Add(caseVM);
        //    }
        //    else if (e.NewDefinition == CaseViewModel.NotCase)
        //    {
        //        NotCase.Add(caseVM);
        //    }
        //}

        protected override void ClearItems()
        {
            base.ClearItems();
            _master.Clear();
        }

        protected override void RemoveItem(int index)
        {
            base.RemoveItem(index);
        }

        protected override void SetItem(int index, CaseContactPairViewModel item)
        {
            #region Input Validation
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            #endregion // Input Validation

            base.SetItem(index, item);
        }
        #endregion // Methods
    }
}
