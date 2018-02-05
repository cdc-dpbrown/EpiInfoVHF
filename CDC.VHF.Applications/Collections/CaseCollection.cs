using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CDC.VHF.Foundation.Enums;
using CDC.VHF.Applications;
using CDC.VHF.Applications.Events;

namespace CDC.VHF.Applications.Collections
{
    public sealed class CaseCollectionMaster : ObservableCollection<CaseViewModel>, IDisposable
    {
        #region Members
        private ObservableCollection<CaseViewModel> _confirmed = new ObservableCollection<CaseViewModel>();
        private ObservableCollection<CaseViewModel> _probable = new ObservableCollection<CaseViewModel>();
        private ObservableCollection<CaseViewModel> _suspect = new ObservableCollection<CaseViewModel>();
        private ObservableCollection<CaseViewModel> _notCase = new ObservableCollection<CaseViewModel>();
        private Dictionary<string, CaseViewModel> _master = new Dictionary<string, CaseViewModel>(StringComparer.OrdinalIgnoreCase);
        #endregion // Members

        #region Properties
        /// <summary>
        /// Type Name
        /// </summary>
        /// <param name="name">Name</param>
        /// <returns>NamedObjectCollection</returns>
        public CaseViewModel this[string id]
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
        public CaseViewModel this[Guid id]
        {
            get
            {
                return _master[id.ToString()];
            }
        }

        /// <summary>
        /// Returns the collection of Text Fields.
        /// </summary>
        public ObservableCollection<CaseViewModel> Confirmed
        {
            get
            {
                return this._confirmed;
            }
        }

        /// <summary>
        /// Returns the collection of Mirror Fields.
        /// </summary>
        public ObservableCollection<CaseViewModel> Probable
        {
            get
            {
                return this._probable;
            }
        }

        /// <summary>
        /// Returns the collection of Number Fields.
        /// </summary>
        public ObservableCollection<CaseViewModel> Suspect
        {
            get
            {
                return this._suspect;
            }
        }

        /// <summary>
        /// Returns the collection of Number Fields.
        /// </summary>
        public ObservableCollection<CaseViewModel> NotCase
        {
            get
            {
                return this._notCase;
            }
        }
        #endregion // Properties

        #region Events
        public event EventHandler<CaseAddedArgs> CaseAdded;
        public event EventHandler<CaseChangedArgs> CaseUpdated;
        #endregion // Events

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
                foreach (CaseViewModel caseVM in this)
                {
                    caseVM.EpiCaseDefinitionChanging -= item_EpiCaseDefinitionChanging;
                    caseVM.CaseIDChanging -= item_CaseIDChanging;
                    caseVM.MarkedForRemoval -= item_MarkedForRemoval;
                    caseVM.Inserted -= item_Inserted;
                    caseVM.Updated -= item_Updated;
                }
            }
            // free native resources if there are any.
        }

        public bool Contains(string id)
        {
            return _master.ContainsKey(id);
        }

        public bool Contains(Guid id)
        {
            return _master.ContainsKey(id.ToString());
        }

        protected override void InsertItem(int index, CaseViewModel item)
        {
            #region Input Validation
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            #endregion // Input Validation

            base.InsertItem(index, item);

            _master.Add(item.Id.ToString(), item);

            //if (item.EpiCaseDef == EpiCaseClassification.Confirmed)
            //{
            //    Confirmed.Add(item);
            //}
            //if (item.EpiCaseDef == EpiCaseClassification.Probable)
            //{
            //    Probable.Add(item);
            //}
            //if (item.EpiCaseDef == EpiCaseClassification.Suspect)
            //{
            //    Suspect.Add(item);
            //}
            //if (item.EpiCaseDef == EpiCaseClassification.NotCase)
            //{
            //    NotCase.Add(item);
            //}

            //CheckCaseForInvalidId(item, item.ID);

            //if (item.IsNewRecord)
            //{
            //    item.EpiCaseDefinitionChanging += item_EpiCaseDefinitionChanging;
            //    item.CaseIDChanging += item_CaseIDChanging;
            //    item.MarkedForRemoval += item_MarkedForRemoval;
            //    item.Inserted += item_Inserted;
            //    item.Updated += item_Updated;
            //}
        }

        private void item_Inserted(object sender, CaseAddedArgs e)
        {
            // run logic for epi case def, etc
            if (CaseAdded != null)
            {
                CaseAdded(this, e);
            }
        }

        private void item_Updated(object sender, CaseChangedArgs e)
        {
            // run logic for epi case def, etc
            if (CaseUpdated != null)
            {
                CaseUpdated(this, e);
            }
        }

        private void CheckCaseForDuplicateId(CaseViewModel caseVM, string newValue)
        {
            if (String.IsNullOrEmpty(newValue.Trim()))
            {
                return;
            }

            //await Task.Factory.StartNew(delegate
            //{
                //foreach (CaseViewModel c in this)
                //{
                //    if (c.ID.Equals(newValue, StringComparison.OrdinalIgnoreCase))
                //    {
                //        if (c != caseVM)
                //        {
                //            caseVM.IsDuplicateId = true;
                //            return;
                //        }
                //    }
                //}

                //caseVM.IsDuplicateId = false;
            //});
        }

        private void CheckCaseForInvalidId(CaseViewModel caseVM, string newValue)
        {
            // blank, missing, or null ID prefix means we don't validate the ID field
            //if (CaseViewModel.IDPrefixes == null || CaseViewModel.IDPrefixes.Count == 0 || String.IsNullOrEmpty(CaseViewModel.IDPrefixes[0].Trim()))
            //{
            //    return;
            //}

            //caseVM.IsInvalidId = false;

            //string actualID = newValue;

            //if (!String.IsNullOrEmpty(actualID))
            //{
            //    // Check #1 - does separator exist?
            //    if (!String.IsNullOrEmpty(CaseViewModel.IDSeparator))
            //    {
            //        int index = actualID.LastIndexOf(CaseViewModel.IDSeparator); 
            //        if (index < 0)
            //        {
            //            caseVM.IsInvalidId = true;
            //            return;
            //        }
            //        else
            //        {
            //            string actualPrefix = actualID.Substring(0, index);

            //            bool foundValidPrefix = false;

            //            foreach (string prefix in CaseViewModel.IDPrefixes)
            //            {
            //                if (prefix.Equals(actualPrefix, StringComparison.OrdinalIgnoreCase))
            //                {
            //                    foundValidPrefix = true;
            //                }
            //            }

            //            if (!foundValidPrefix)
            //            {
            //                caseVM.IsInvalidId = true;
            //                return;
            //            }

            //            string actualNumbers = actualID.Substring(index + 1);
            //            double numbers;
            //            bool success = double.TryParse(actualNumbers, out numbers);
            //            if (!success)
            //            {
            //                caseVM.IsInvalidId = true;
            //                return;
            //            }

            //            if (actualNumbers.Length != CaseViewModel.IDPattern.Length)
            //            {
            //                caseVM.IsInvalidId = true;
            //                return;
            //            }
            //        }
            //    }
            //}
        }

        private async void item_MarkedForRemoval(object sender, EventArgs e)
        {
            await Task.Factory.StartNew(delegate
            {
                CaseViewModel caseVM = (sender as CaseViewModel);
                if (caseVM != null)
                {
                    Remove(caseVM);
                }
            });
        }

        private void item_CaseIDChanging(object sender, Events.FieldValueChangingEventArgs e)
        {
            CaseViewModel caseVM = (sender as CaseViewModel);
            if (caseVM != null)
            {
                CheckCaseForInvalidId(caseVM, e.NewValue);
                CheckCaseForDuplicateId(caseVM, e.NewValue);
            }
        }

        private void item_EpiCaseDefinitionChanging(object sender, Events.EpiCaseDefinitionChangingEventArgs e)
        {
            CaseViewModel caseVM = sender as CaseViewModel;
            if (caseVM != null)
            {
                if (e.PreviousDefinition == EpiCaseClassification.Confirmed)
                {
                    Confirmed.Remove(caseVM);
                }
                else if (e.PreviousDefinition == EpiCaseClassification.Probable)
                {
                    Probable.Remove(caseVM);
                }
                else if (e.PreviousDefinition == EpiCaseClassification.Suspect)
                {
                    Suspect.Remove(caseVM);
                }
                else if (e.PreviousDefinition == EpiCaseClassification.NotCase)
                {
                    NotCase.Remove(caseVM);
                }

                if (e.NewDefinition == EpiCaseClassification.Confirmed)
                {
                    Confirmed.Add(caseVM);
                }
                else if (e.NewDefinition == EpiCaseClassification.Probable)
                {
                    Probable.Add(caseVM);
                }
                else if (e.NewDefinition == EpiCaseClassification.Suspect)
                {
                    Suspect.Add(caseVM);
                }
                else if (e.NewDefinition == EpiCaseClassification.NotCase)
                {
                    NotCase.Add(caseVM);
                }
            }
        }

        protected override void ClearItems()
        {
            _master.Clear();

            Confirmed.Clear();
            Probable.Clear();
            Suspect.Clear();
            NotCase.Clear();

            base.ClearItems();
        }

        protected override void RemoveItem(int index)
        {
            //CaseViewModel caseToRemove = this[index];
            //_master.Remove(caseToRemove.RecordId);
            //base.RemoveItem(index);

            //if (caseToRemove.EpiCaseDef == EpiCaseClassification.Confirmed)
            //{
            //    Confirmed.Remove(caseToRemove);
            //}
            //if (caseToRemove.EpiCaseDef == EpiCaseClassification.Probable)
            //{
            //    Probable.Remove(caseToRemove);
            //}
            //if (caseToRemove.EpiCaseDef == EpiCaseClassification.Suspect)
            //{
            //    Suspect.Remove(caseToRemove);
            //}
            //if (caseToRemove.EpiCaseDef == EpiCaseClassification.NotCase)
            //{
            //    NotCase.Remove(caseToRemove);
            //}
        }

        protected override void SetItem(int index, CaseViewModel item)
        {
            #region Input Validation
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            #endregion // Input Validation

            //CaseViewModel caseToChange = this[index];
            //_master.Remove(caseToChange.RecordId);

            //_master.Add(caseToChange.RecordId, item);

            base.SetItem(index, item);
        }
        #endregion // Methods
    }
}