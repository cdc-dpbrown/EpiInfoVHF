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
    public sealed class LabResultCollectionMaster : ObservableCollection<LabResultViewModel>, IDisposable
    {
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (LabResultViewModel labVM in this)
                {
                    labVM.MarkedForRemoval -= item_MarkedForRemoval;
                }
            }
            // free native resources if there are any.
        }

        protected override void InsertItem(int index, LabResultViewModel item)
        {
            #region Input Validation
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            #endregion // Input Validation

            base.InsertItem(index, item);

            item.MarkedForRemoval += item_MarkedForRemoval;
        }

        private void item_MarkedForRemoval(object sender, EventArgs e)
        {
            //await Task.Factory.StartNew(delegate
            //{
                LabResultViewModel labVM = (sender as LabResultViewModel);
                if (labVM != null)
                {
                    Remove(labVM);
                }
            //});
        }
    }
}
