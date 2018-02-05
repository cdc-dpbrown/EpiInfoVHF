using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ContactTracing.Core;
using ContactTracing.ViewModel;
using ContactTracing.ViewModel.Events;

namespace ContactTracing.ViewModel.Collections
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
        public event EventHandler CaseSwitchToLegacyEnter;
        public event EventHandler CaseViewerClosed;
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
                    caseVM.CaseSecondaryIDChanging -= item_CaseIDChanging;
                    caseVM.MarkedForRemoval -= item_MarkedForRemoval;
                    caseVM.Inserted -= item_Inserted;
                    caseVM.Updated -= item_Updated;
                    caseVM.ViewerClosed -= item_ViewerClosed;
                    caseVM.SwitchToLegacyEnter -= item_SwitchToLegacyEnter;
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

        public void SortSingleItemByID(CaseViewModel item)
        {
            this.Remove(item);
            this.Add(item);
        }

        public void SortByID()
        {
            IEnumerable<CaseViewModel> orderedCases = this.OrderBy(x => x.IDForSorting).ToList();

            this.Clear();

            foreach (var c in orderedCases)
            {
                this.Add(c);
            }

            System.Collections.Specialized.NotifyCollectionChangedEventArgs args = new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Add, orderedCases);
            OnCollectionChanged(args);
        }

        protected override void InsertItem(int index, CaseViewModel item)
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

            //base.InsertItem(index, item);

            if (Count == 0)
            {
                base.InsertItem(0, item);
            }
            else
            {
                var lastItem = this[Count - 1];

                if (item.IDForSorting.CompareTo(lastItem.IDForSorting) == 1)
                {
                    base.InsertItem(Count, item);
                }
                else
                {
                    bool wasInserted = false;
                    for (int i = Count - 1; i >= 0; i--)
                    {
                        var c = this[i];

                        if (item.IDForSorting.CompareTo(c.IDForSorting) == 1)
                        {
                            base.InsertItem(i + 1, item);
                            wasInserted = true;
                            break;
                        }
                    }

                    if (!wasInserted)
                    {
                        base.InsertItem(0, item);
                    }
                }
            }

            _master.Add(item.RecordId, item);

            if (item.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed)
            {
                Confirmed.Add(item);
            }
            if (item.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable)
            {
                Probable.Add(item);
            }
            if (item.EpiCaseDef == Core.Enums.EpiCaseClassification.Suspect)
            {
                Suspect.Add(item);
            }
            if (item.EpiCaseDef == Core.Enums.EpiCaseClassification.NotCase)
            {
                NotCase.Add(item);
            }

            CheckCaseForInvalidId(item, item.ID);
            CheckCaseForDuplicateId(item, item.IDForSorting);

            item.EpiCaseDefinitionChanging += item_EpiCaseDefinitionChanging;
            item.CaseIDChanging += item_CaseIDChanging;
            item.CaseSecondaryIDChanging += item_CaseIDChanging;
            item.MarkedForRemoval += item_MarkedForRemoval;
            item.Inserted += item_Inserted;
            item.Updated += item_Updated;
            item.ViewerClosed += item_ViewerClosed;
            item.SwitchToLegacyEnter += item_SwitchToLegacyEnter;
        }

        void item_SwitchToLegacyEnter(object sender, EventArgs e)
        {
            if (CaseSwitchToLegacyEnter != null && sender != null)
            {
                CaseViewModel c = sender as CaseViewModel;
                if (c != null)
                {
                    CaseSwitchToLegacyEnter(c, e);
                }
            }
        }

        void item_ViewerClosed(object sender, EventArgs e)
        {
            if (CaseViewerClosed != null && sender != null)
            {
                CaseViewModel c = sender as CaseViewModel;
                if (c != null)
                {
                    CaseViewerClosed(c, e);
                }
            }
        }

        private void item_Inserted(object sender, CaseAddedArgs e)
        {
            // run logic for epi case def, etc
            if (CaseAdded != null)
            {
                CaseAdded(this, e);
            }

            CheckExistingDuplicateCases();
        }

        private void item_Updated(object sender, CaseChangedArgs e)
        {
            // run logic for epi case def, etc
            if (CaseUpdated != null)
            {
                CaseUpdated(this, e);
            }

            CheckExistingDuplicateCases();
        }

        private void CheckCaseForDuplicateId(CaseViewModel givenCase, string candidateCaseId)
        {
            if (String.IsNullOrEmpty(candidateCaseId.Trim()))
            {
                return;
            }

            foreach (CaseViewModel eachCase in this)
            {
                if (eachCase.IDForSorting.Equals(candidateCaseId, StringComparison.OrdinalIgnoreCase))
                {
                    if (eachCase != givenCase)
                    {
                        givenCase.IsDuplicateId = true;
                        eachCase.IsDuplicateId = true;
                        return;
                    }
                }
            }

            givenCase.IsDuplicateId = false;
        }

        private void CheckExistingDuplicateCases()
        {
            foreach (CaseViewModel c in this.Where(x => x.IsDuplicateId == true))
            {
                CheckCaseForDuplicateId(c, c.ID);
            }
        }

        private void CheckCaseForInvalidId(CaseViewModel caseVM, string newValue)
        {
            //// blank, missing, or null ID prefix means we don't validate the ID field
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

                if (!CaseViewModel.IsCountryUS)
                {
                    CheckCaseForDuplicateId(caseVM, e.NewValue);
                }
            }
        }

        private void item_EpiCaseDefinitionChanging(object sender, Events.EpiCaseDefinitionChangingEventArgs e)
        {
            CaseViewModel caseVM = sender as CaseViewModel;
            if (caseVM != null)
            {
                if (e.PreviousDefinition == Core.Enums.EpiCaseClassification.Confirmed)
                {
                    Confirmed.Remove(caseVM);
                }
                else if (e.PreviousDefinition == Core.Enums.EpiCaseClassification.Probable)
                {
                    Probable.Remove(caseVM);
                }
                else if (e.PreviousDefinition == Core.Enums.EpiCaseClassification.Suspect)
                {
                    Suspect.Remove(caseVM);
                }
                else if (e.PreviousDefinition == Core.Enums.EpiCaseClassification.NotCase)
                {
                    NotCase.Remove(caseVM);
                }

                if (e.NewDefinition == Core.Enums.EpiCaseClassification.Confirmed)
                {
                    Confirmed.Add(caseVM);
                }
                else if (e.NewDefinition == Core.Enums.EpiCaseClassification.Probable)
                {
                    Probable.Add(caseVM);
                }
                else if (e.NewDefinition == Core.Enums.EpiCaseClassification.Suspect)
                {
                    Suspect.Add(caseVM);
                }
                else if (e.NewDefinition == Core.Enums.EpiCaseClassification.NotCase)
                {
                    NotCase.Add(caseVM);
                }
            }
        }

        protected override void ClearItems()
        {
            foreach (CaseViewModel c in this)
            {
                c.EpiCaseDefinitionChanging -= item_EpiCaseDefinitionChanging;
                c.CaseIDChanging -= item_CaseIDChanging;
                c.CaseSecondaryIDChanging -= item_CaseIDChanging;
                c.MarkedForRemoval -= item_MarkedForRemoval;
                c.Inserted -= item_Inserted;
                c.Updated -= item_Updated;
                c.ViewerClosed -= item_ViewerClosed;
                c.SwitchToLegacyEnter -= item_SwitchToLegacyEnter;
            }
            _master.Clear();

            Confirmed.Clear();
            Probable.Clear();
            Suspect.Clear();
            NotCase.Clear();

            base.ClearItems();
        }

        protected override void RemoveItem(int index)
        {
            CaseViewModel caseToRemove = this[index];
            _master.Remove(caseToRemove.RecordId);
            base.RemoveItem(index);

            if (caseToRemove.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed)
            {
                Confirmed.Remove(caseToRemove);
            }
            if (caseToRemove.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable)
            {
                Probable.Remove(caseToRemove);
            }
            if (caseToRemove.EpiCaseDef == Core.Enums.EpiCaseClassification.Suspect)
            {
                Suspect.Remove(caseToRemove);
            }
            if (caseToRemove.EpiCaseDef == Core.Enums.EpiCaseClassification.NotCase)
            {
                NotCase.Remove(caseToRemove);
            }

            caseToRemove.EpiCaseDefinitionChanging -= item_EpiCaseDefinitionChanging;
            caseToRemove.CaseIDChanging -= item_CaseIDChanging;
            caseToRemove.CaseSecondaryIDChanging -= item_CaseIDChanging;
            caseToRemove.MarkedForRemoval -= item_MarkedForRemoval;
            caseToRemove.Inserted -= item_Inserted;
            caseToRemove.Updated -= item_Updated;
            caseToRemove.ViewerClosed -= item_ViewerClosed;
            caseToRemove.SwitchToLegacyEnter -= item_SwitchToLegacyEnter;
        }

        protected override void SetItem(int index, CaseViewModel item)
        {
            #region Input Validation
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            #endregion // Input Validation

            CaseViewModel caseToChange = this[index];
            _master.Remove(caseToChange.RecordId);
            _master.Add(caseToChange.RecordId, item);
            base.SetItem(index, item);
        }
        #endregion // Methods
    }
}