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
    /// Interaction logic for FilterSortDropdown.xaml
    /// </summary>
    public partial class FilterSortDropdown : UserControl
    {
        public event EventHandler Closed;
        public event EventHandler Print;
        private bool IsViewingRelationshipInfo { get; set; }
        private IDbDriver Database { get; set; }
        private string ContactFormTableName { get; set; }
        private Dictionary<string, string> _sortOptions = new Dictionary<string, string>();
        private Dictionary<string, string> _filterOptions = new Dictionary<string, string>();
        private Dictionary<string, string> _filterOperators = new Dictionary<string, string>();
        private Dictionary<string, string> _filterAndOr = new Dictionary<string, string>();
        private List<string> _selectedSort { get; set; }
        private List<string> _selectedSortKeys { get; set; }
        private List<string> _selectedFilter { get; set; }
        private bool _isNewContact = false;

        public CaseContactPairViewModel CaseContactPair { get; private set; }
        public List<string> SelectedSort { get { return _selectedSort; } set { _selectedSort = value; } }
        public List<string> SelectedSortKeys { get { return _selectedSortKeys; } set { _selectedSortKeys = value; } }
        public List<string> SelectedFilter { get { return _selectedFilter; } set { _selectedFilter = value; } }
        public String FilterClause = "";
        public String SortClause = "";
        public object Collection { get; set; }

        public bool IsBoundryAggregate
        {
            get
            {
                if (cmdIsAggregateSort.SelectedValue == null || cmdIsAggregateSort.SelectedValue == "boundryAggregate")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool IsInclusive
        {
            get
            {
                if (cmdIsInclusive.SelectedValue == null || cmdIsInclusive.SelectedValue == "inclusive")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public Dictionary<string, string> SortOptions
        {
            get
            {
                if (_sortOptions == null)
                {
                    _sortOptions = new Dictionary<string, string>();
                }

                if (_sortOptions.Count == 0)
                {
                    _sortOptions.Add("", "");
                    _sortOptions.Add("ContactVM.Team", "Team");
                    
                    foreach(KeyValuePair<int, Boundry> option in DataHelper.BoundaryAggregation)
                    {
                        _sortOptions.Add(option.Key.ToString(), option.Value.Name);
                    }
                }

                return _sortOptions;
            }
        }

        public Dictionary<string, string> FilterOptions
        {
            get
            {
                if (_filterOptions == null)
                {
                    _filterOptions = new Dictionary<string, string>();
                }

                if (_filterOptions.Count == 0)
                {
                    _filterOptions.Add("", "");
                    _filterOptions.Add("ContactVM.Team", "Team");
                    _filterOptions.Add("ContactVM.HCWFacility", "Healthcare Facility");

                    foreach (KeyValuePair<int, Boundry> option in DataHelper.BoundaryAggregation)
                    {
                        _filterOptions.Add(option.Key.ToString(), option.Value.Name);
                    }
                }

                return _filterOptions;
            }
        }

        public Dictionary<string, string> FilterOperators
        {
            get
            {
                if (_filterOperators == null)
                {
                    _filterOperators = new Dictionary<string,string>();
                }

                if (_filterOperators.Count == 0)
                {
                    _filterOperators.Add("", "");
                    _filterOperators.Add("==", "equals"); // dpb
                    _filterOperators.Add("!=", "not equals");// dpb
                }

                return _filterOperators;
            }
        }

        public Dictionary<string, string> FilterAndOr
        {
            get
            {
                if (_filterAndOr == null)
                {
                    _filterAndOr = new Dictionary<string, string>();
                }

                if (_filterAndOr.Count == 0)
                {
                    _filterAndOr.Add("", "");
                    _filterAndOr.Add("and", "and");// dpb
                    _filterAndOr.Add("or", "or");// dpb
                }

                return _filterAndOr;
            }
        }

        public FilterSortDropdown(EpiDataHelper dataHelper, bool isSuperUser = false)
        {
            InitializeComponent();
            this.DataContext = dataHelper;
            Construct();
        }

        private void Construct()
        {
            IsViewingRelationshipInfo = false;
            cmdSort_1.ItemsSource = SortOptions;
            cmdSort_2.ItemsSource = SortOptions;
            cmdSort_3.ItemsSource = SortOptions;
                        
            Dictionary<string, string> isAg = new Dictionary<string, string>();
            isAg.Add("boundryAggregate", "Sort by all higher-level locations first (default)");
            isAg.Add("simpleAggregate", "Sort by chosen locations only");
            cmdIsAggregateSort.ItemsSource = isAg;

            Dictionary<string, string> isInc = new Dictionary<string, string>();
            isInc.Add("inclusive", "Include all active contacts (default)");
            isInc.Add("explicit", "Exclude contacts at >21 days of follow-up");
            cmdIsInclusive.ItemsSource = isInc;

            filter_combine_2.ItemsSource = FilterAndOr;
            filter_combine_3.ItemsSource = FilterAndOr;
            filter_combine_4.ItemsSource = FilterAndOr;
            filter_combine_5.ItemsSource = FilterAndOr;
            filter_combine_6.ItemsSource = FilterAndOr;
            filter_combine_7.ItemsSource = FilterAndOr;

            filter_var_1.ItemsSource = FilterOptions;
            filter_var_2.ItemsSource = FilterOptions;
            filter_var_3.ItemsSource = FilterOptions;
            filter_var_4.ItemsSource = FilterOptions;
            filter_var_5.ItemsSource = FilterOptions;

            filter_op_1.ItemsSource = FilterOperators;
            filter_op_2.ItemsSource = FilterOperators;
            filter_op_3.ItemsSource = FilterOperators;
            filter_op_4.ItemsSource = FilterOperators;
            filter_op_5.ItemsSource = FilterOperators;

            filter_var_1.Tag = new Dictionary<string, ComboBox>() {                                  { "op", filter_op_1 }, { "val", filter_val_1 } };
            filter_var_2.Tag = new Dictionary<string, ComboBox>() { { "combine", filter_combine_2 }, { "op", filter_op_2 }, { "val", filter_val_2 } };
            filter_var_3.Tag = new Dictionary<string, ComboBox>() { { "combine", filter_combine_3 }, { "op", filter_op_3 }, { "val", filter_val_3 } };
            filter_var_4.Tag = new Dictionary<string, ComboBox>() { { "combine", filter_combine_4 }, { "op", filter_op_4 }, { "val", filter_val_4 } };
            filter_var_5.Tag = new Dictionary<string, ComboBox>() { { "combine", filter_combine_5 }, { "op", filter_op_5 }, { "val", filter_val_5 } };

            cmbFilterOptionAddedAfter_Date.Tag = filter_combine_6;
            cmbFilterOptionSeenAfter_Date.Tag = filter_combine_7;
        }

        public EpiDataHelper DataHelper
        {
            get { return this.DataContext as EpiDataHelper; }
        }

        #region Event Handlers

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (Collection is ContactTracing.ViewModel.Collections.ContactCollectionMaster)
            {
                cmdIsInclusive.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                cmdIsInclusive.Visibility = System.Windows.Visibility.Visible;
            }
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

        private void filterVar_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((DatePicker)sender).SelectedDate == null)
            {
                
            }

            if (((DatePicker)sender).Tag is ComboBox)
            {
                ComboBox conjunction = (ComboBox)((DatePicker)sender).Tag;
                if(conjunction.SelectedValue == null)
                {
                    conjunction.SelectedValue = "and";
                }
            }
        }


        private void filterVar_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((ComboBox)sender).SelectedValue == null)
            {
                DataHelper.FilterSelectedBoundry = -1;
            }

            object tag = ((ComboBox)sender).Tag;
            List<string> values = new List<string>();

            string variableColumnName = "";

            if(((ComboBox)sender).SelectedValue is string)
            {
                if (((string)((ComboBox)sender).SelectedValue) == "")
                {
                    if (((Dictionary<string, ComboBox>)((ComboBox)sender).Tag).ContainsKey("combine"))
                    {
                        ((ComboBox)((Dictionary<string, ComboBox>)((ComboBox)sender).Tag)["combine"]).SelectedItem = null;
                    }
                    ((ComboBox)((Dictionary<string, ComboBox>)((ComboBox)sender).Tag)["op"]).SelectedItem = null;
                    ((ComboBox)((Dictionary<string, ComboBox>)((ComboBox)sender).Tag)["val"]).SelectedItem = null;

                    return;
                }
                
                variableColumnName = (string)((ComboBox)sender).SelectedValue;
                int boundryAggregate;
                if (int.TryParse(variableColumnName, out boundryAggregate))
                {
                    Boundry boundry = null;
                    if (DataHelper.BoundaryAggregation.TryGetValue(boundryAggregate, out boundry))
                    {
                        variableColumnName = boundry.ColumnName;
                    }
                }
                else
                {
                    Dictionary<string, string> objectColumnName = new Dictionary<string,string>();
                    objectColumnName.Add("ContactVM.Team","Team");
                    objectColumnName.Add("ContactVM.HCWFacility","ContactHCWFacility");
                    variableColumnName = objectColumnName[variableColumnName];
                }
            }

            values = DataHelper.ColumnValues(variableColumnName);

            if(tag is Dictionary<string, ComboBox>)
            {
                ((ComboBox)((Dictionary<string, ComboBox>)((ComboBox)sender).Tag)["val"]).ItemsSource = null;
                ((ComboBox)((Dictionary<string, ComboBox>)((ComboBox)sender).Tag)["val"]).ItemsSource = values;

                if (((Dictionary<string, ComboBox>)((ComboBox)sender).Tag).ContainsKey("op"))
                {
                    if (((ComboBox)((Dictionary<string, ComboBox>)((ComboBox)sender).Tag)["op"]).SelectedItem == null)
                    {
                        ComboBox op = ((ComboBox)((Dictionary<string, ComboBox>)((ComboBox)sender).Tag)["op"]);
                        op.SelectedIndex = 1;     
                    }
                }

                if (((Dictionary<string, ComboBox>)((ComboBox)sender).Tag).ContainsKey("combine"))
                {
                    if (((ComboBox)((Dictionary<string, ComboBox>)((ComboBox)sender).Tag)["combine"]).SelectedItem == null)
                    {
                        ComboBox combine = ((ComboBox)((Dictionary<string, ComboBox>)((ComboBox)sender).Tag)["combine"]);
                        combine.SelectedIndex = 1;
                    }
                }
            }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (this.Print != null)
            {
                _selectedSort = new List<string>();
                _selectedSortKeys = new List<string>();
                AddPredicate(cmdSort_1);
                AddPredicate(cmdSort_2);
                AddPredicate(cmdSort_3);

                if (_selectedSort.Count() == 0)
                {
                    SortClause = "";
                }
                else
                {
                    SortClause = _selectedSort.Aggregate((i, j) => i + "," + j);
                }

                _selectedFilter = new List<string>();
                AddPredicate(_selectedFilter,             null, filter_var_1, filter_op_1, filter_val_1); 
                AddPredicate(_selectedFilter, filter_combine_2, filter_var_2, filter_op_2, filter_val_2);
                AddPredicate(_selectedFilter, filter_combine_3, filter_var_3, filter_op_3, filter_val_3);
                AddPredicate(_selectedFilter, filter_combine_4, filter_var_4, filter_op_4, filter_val_4);
                AddPredicate(_selectedFilter, filter_combine_5, filter_var_5, filter_op_5, filter_val_5);

                AddPredicate(_selectedFilter, "ContactVM.FirstSaveTime", filter_combine_6, cmbFilterOptionAddedAfter_Date);
                AddPredicate(_selectedFilter, "ContactVM.DateOfLastFollowUp", filter_combine_7, cmbFilterOptionSeenAfter_Date);

                if (_selectedFilter.Count() == 0)
                {
                    FilterClause = "";
                }
                else
                {
                    FilterClause = _selectedFilter.Aggregate((i, j) => i + j);
                    FilterClause = FilterClause.Trim();
                    FilterClause = FilterClause.StartsWith("and ") ? FilterClause.Substring(3) : FilterClause;
                }
                
                Print(this, new EventArgs());
            }
        }

        private void AddPredicate(List<string> SelectedFilter, string objectResolution, ComboBox filter_combine, DatePicker filter_val)
        {
            string predicate = string.Empty;
            if (filter_val.SelectedDate != null)
            {
                predicate = string.Empty;

                if (filter_combine.SelectedValue != null)
                {
                    predicate = string.IsNullOrEmpty(filter_combine.SelectedValue as string) ? " && " : " " + filter_combine.SelectedValue.ToString() + " ";
                }

                string year, month, day;
                year = filter_val.SelectedDate.Value.Year.ToString();
                month = filter_val.SelectedDate.Value.Month.ToString();
                day = filter_val.SelectedDate.Value.Day.ToString();

                predicate = predicate + " " + objectResolution + " >= DATETIME(" + year + "," + month + "," + day + ")";
                _selectedFilter.Add(predicate);
            }
        }

        void AddPredicate(ComboBox sort_var)
        {
            string predicate = string.Empty;
            if (sort_var.SelectedValue != null && sort_var.SelectedValue is string && ((string)sort_var.SelectedValue) != "")
            {
                string value = sort_var.SelectedValue.ToString();
                int aggregateKey;
                if (int.TryParse(value, out aggregateKey))
                {
                    predicate = DataHelper.BoundaryAggregation[aggregateKey].ObjectResolution;
                }
                else
                {
                    predicate = value;
                }

                SelectedSort.Add(predicate);
                SelectedSortKeys.Add(value);
            }
        }

        void AddPredicate(List<string> SelectedFilter, ComboBox filter_combine, ComboBox filter_var, ComboBox filter_op, ComboBox filter_val)
        {
            string predicate = string.Empty;
            if (filter_var.SelectedValue != null && filter_val.SelectedValue != null)
            {
                if(filter_val.SelectedValue == null)
                {
                    filter_val.SelectedValue = "";
                }
                
                if(filter_combine != null)
                { 
                    predicate = string.IsNullOrEmpty(filter_combine.SelectedValue as string) ? " && " : " " + filter_combine.SelectedValue.ToString() + " ";
                }

                string value = filter_var.SelectedValue.ToString();
                int aggregateKey;
                if (int.TryParse(value, out aggregateKey))
                {
                    predicate = predicate + DataHelper.BoundaryAggregation[aggregateKey].ObjectResolution;
                }
                else
                {
                    predicate = predicate + value;
                }

                predicate = predicate + (string.IsNullOrEmpty(filter_op.SelectedValue as string) ? " == " : " " + filter_op.SelectedValue.ToString()) + " ";
                predicate = predicate + (string.IsNullOrEmpty(filter_val.SelectedValue as string) ? "" : "\"" + filter_val.SelectedValue.ToString()) + "\"";
                SelectedFilter.Add(predicate);
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
