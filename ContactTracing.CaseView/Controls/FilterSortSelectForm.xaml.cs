using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ContactTracing.Core;
using ContactTracing.ViewModel;
using Epi;
using Epi.Data;

namespace ContactTracing.CaseView.Controls
{
    /// <summary>
    /// Interaction logic for FilterSortSelectForm.xaml
    /// </summary>
    public partial class FilterSortSelectForm : UserControl
    {
        public event EventHandler Closed;
        public event EventHandler Print;
        private bool IsViewingRelationshipInfo { get; set; }
        private IDbDriver Database { get; set; }
        private string ContactFormTableName { get; set; }

        public CaseContactPairViewModel CaseContactPair { get; private set; }

        private bool _isNewContact = false;

        public FilterSortSelectForm(EpiDataHelper dataHelper, bool isSuperUser = false)
        {
            InitializeComponent();
            this.DataContext = dataHelper;
            Construct();
        }

        private void Construct()
        {
            IsViewingRelationshipInfo = false;
        }

        public EpiDataHelper DataHelper
        {
            get { return this.DataContext as EpiDataHelper; }
        }

        #region Event Handlers

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void cmbBoundyAggregateLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((ComboBox)sender).SelectedValue == null)
            {
                DataHelper.FilterSelectedBoundry = -1;
            }
            else
            {
                int parsedInt = -1;
                if (((ComboBox)sender).SelectedValue is string && int.TryParse((string)((ComboBox)sender).SelectedValue, out parsedInt))
                {
                    DataHelper.FilterSelectedBoundry = parsedInt;
                }
                else if (((ComboBox)sender).SelectedValue is int)
                {
                    DataHelper.FilterSelectedBoundry = (int)((ComboBox)sender).SelectedValue;
                }
                else
                {
                    DataHelper.FilterSelectedBoundry = -1;
                }
            }
            
            cmbBoundry.ItemsSource = DataHelper.BoundriesAtLevel;
        }

        private void checkboxFO_Discharged_Checked(object sender, RoutedEventArgs e)
        {
        }

        private void checkboxFO_Isolated_Checked(object sender, RoutedEventArgs e)
        {
        }

        private void checkboxFO_Dropped_Checked(object sender, RoutedEventArgs e)
        {
        }

        #endregion // Event Handlers

        public void ClearCaseData()
        {
        }

        public delegate void ContactIdUpdatedEventHandler(KeyValuePair<int, CaseContactPairViewModel> kvp);

        public void ForceRepopulation()
        {
            MessageBox.Show("There was a problem obtaining the contact ID number. Please contact the application developer. Click OK to refresh the database.");
            DataHelper.RepopulateCollections();
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result is KeyValuePair<int, CaseContactPairViewModel>)
            {
                KeyValuePair<int, CaseContactPairViewModel> kvp = (KeyValuePair<int, CaseContactPairViewModel>)e.Result;
            }
            else if (e.Result is Exception)
            {
                this.Dispatcher.BeginInvoke(new SimpleEventHandler(ForceRepopulation));
            }
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                CaseContactPairViewModel ccp = e.Argument as CaseContactPairViewModel;

                if (ccp != null)
                {
                    Query selectQuery = Database.CreateQuery("SELECT UniqueKey FROM " + ContactFormTableName + " WHERE [GlobalRecordId] = @GlobalRecordId");
                    selectQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, ccp.ContactVM.RecordId));

                    KeyValuePair<int, CaseContactPairViewModel> kvp = new KeyValuePair<int, CaseContactPairViewModel>(Convert.ToInt32(Database.Select(selectQuery).Rows[0]["UniqueKey"]),
                        ccp);
                    e.Result = kvp;
                }
            }
            catch (Exception ex)
            {
                e.Result = ex;
            }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (this.Print != null)
            {
                Print(this, new EventArgs());
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            if (this.Closed != null)
            {
                Closed(this, new EventArgs());
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (this.Closed != null)
            {
                Closed(this, new EventArgs());
            }
        }
    }
}
