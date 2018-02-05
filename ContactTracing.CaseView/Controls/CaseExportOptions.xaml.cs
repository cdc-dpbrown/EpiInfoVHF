using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Epi.Data;
using Epi.ImportExport;
using Epi.ImportExport.Filters;
using ContactTracing.ImportExport;

namespace ContactTracing.CaseView.Controls
{
    /// <summary>
    /// Interaction logic for CaseExportOptions.xaml
    /// </summary>
    public partial class CaseExportOptions : UserControl
    {
        public event RoutedEventHandler Click;

        public EpiDataHelper DataHelper
        {
            get
            {
                return (this.DataContext as EpiDataHelper);
            }
        }

        public bool IncludeCaseExposures { get; set; }
        public bool IncludeContacts { get; set; }
        public bool IncludeCases { get; set; }
        public bool DeIdentifyData { get; set; }
        public string DistrictFilter { get; set; }
        public string FileName { get; set; }
        public Epi.RecordProcessingScope RecordProcessingScope { get; set; }
        public RowFilters Filters { get; set; }

        [Obsolete("Do not use default constructor", true)]
        public CaseExportOptions()
        {
            InitializeComponent();
        }

        public CaseExportOptions(EpiDataHelper dataHelper)
        {
            InitializeComponent();
            DataContext = dataHelper;
            IncludeCaseExposures = true;
            IncludeContacts = false;
            IncludeCases = false;
            DeIdentifyData = true;
            DistrictFilter = String.Empty;
            RecordProcessingScope = Epi.RecordProcessingScope.Undeleted;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            Epi.ImportExport.Filters.ConditionJoinTypes op = Epi.ImportExport.Filters.ConditionJoinTypes.And;

            if (cmbLogicalOperator.SelectedIndex == 2)
            {
                op = Epi.ImportExport.Filters.ConditionJoinTypes.Or;
            }

            Filters = new RowFilters(this.DataHelper.Database, op);

            if (Click != null)
            {
                if (includeCasesOnly.IsChecked == true)
                {
                    IncludeCases = true;
                    IncludeCaseExposures = true;
                    IncludeContacts = false;
                }

                if (includeCasesContacts.IsChecked == true)
                {
                    IncludeCases = true;
                    IncludeCaseExposures = true;
                    IncludeContacts = true;
                }

                if (checkboxDeidentifyData.IsChecked == true)
                {
                    DeIdentifyData = true;
                }
                else
                {
                    DeIdentifyData = false;
                }

                switch (cmbRecordProcessingScope.SelectedIndex)
                {
                    case 0:
                        RecordProcessingScope = Epi.RecordProcessingScope.Undeleted;
                        break;
                    case 1:
                        RecordProcessingScope = Epi.RecordProcessingScope.Deleted;
                        break;
                    case 2:
                        RecordProcessingScope = Epi.RecordProcessingScope.Both;
                        break;
                }

                // NOTE: Contact-only exports were removed at request of project coordinator...
                //if (includeContactsOnly.IsChecked == true)
                //{
                //    IncludeCases = false;
                //    IncludeCaseExposures = false;
                //    IncludeContacts = true;
                //}

                IDbDriver db = this.DataHelper.Database;
                // data package filters
                if (checkboxFilterData.IsChecked == true)
                {
                    string varName1 = cmbVariableName1.Text.Trim();
                    string value1 = tboxValue1.Text.Trim();

                    string varName2 = cmbVariableName2.Text.Trim();
                    string value2 = tboxValue2.Text.Trim();

                    #region Check to see if user's filtering options make sense

                    if (String.IsNullOrEmpty(varName1) && String.IsNullOrEmpty(value1))
                    {
                        MessageBox.Show("Neither a variable nor a value have been selected for the first condition. Please ensure both a variable and a value are present before proceeding.", "Missing filter information", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (!String.IsNullOrEmpty(varName1) && String.IsNullOrEmpty(value1))
                    {
                        MessageBox.Show("A variable has been selected for the first condition, but no value has been specified. Please specify a value and try again.", "No value specified", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (!String.IsNullOrEmpty(value1) && String.IsNullOrEmpty(varName1))
                    {
                        MessageBox.Show("A value has been selected for the first condition, but no variable has been specified. Please specify a variable on which to filter and try again.", "No variable specified", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (cmbLogicalOperator.SelectedIndex == 1 && String.IsNullOrEmpty(varName2) && String.IsNullOrEmpty(value2))
                    {
                        MessageBox.Show("Neither a variable nor a value have been selected for the second condition. Please ensure both a variable and a value are present before proceeding.", "Missing filter information", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (cmbLogicalOperator.SelectedIndex == 1 && !String.IsNullOrEmpty(varName2) && String.IsNullOrEmpty(value2))
                    {
                        MessageBox.Show("A variable has been selected for the second condition, but no value has been specified. Please specify a value and try again.", "No value specified", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (cmbLogicalOperator.SelectedIndex == 1 && !String.IsNullOrEmpty(value2) && String.IsNullOrEmpty(varName2))
                    {
                        MessageBox.Show("A value has been selected for the second condition, but no variable has been specified. Please specify a variable on which to filter and try again.", "No variable specified", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    #endregion 

                    if (!String.IsNullOrEmpty(varName1) && !String.IsNullOrEmpty(value1))
                    {
                        if (cmbOperator1.SelectedIndex == 0)
                        {
                            TextRowFilterCondition tfc = new TextRowFilterCondition("[" + varName1 + "] = @" + varName1 + "", "" + varName1 + "", "@" + varName1 + "", value1);
                            tfc.Description = "" + varName1 + " equals " + value1;
                            Filters.Add(tfc);
                        }
                        else
                        {
                            value1 = "%" + value1 + "%";
                            TextRowFilterCondition tfc = new TextRowFilterCondition("[" + varName1 + "] LIKE @" + varName1 + "", "" + varName1 + "", "@" + varName1 + "", value1);
                            tfc.Description = "" + varName1 + " contains " + value1;
                            Filters.Add(tfc);
                        }
                    }

                    if (!String.IsNullOrEmpty(varName2) && !String.IsNullOrEmpty(value2))
                    {
                        if (cmbOperator2.SelectedIndex == 0)
                        {
                            TextRowFilterCondition tfc = new TextRowFilterCondition("[" + varName2 + "] = @" + varName2 + "", "" + varName2 + "", "@" + varName2 + "", value2);
                            tfc.Description = "" + varName2 + " equals " + value2;
                            Filters.Add(tfc);
                        }
                        else
                        {
                            value2 = "%" + value2 + "%";
                            TextRowFilterCondition tfc = new TextRowFilterCondition("[" + varName2 + "] LIKE @" + varName2 + "", "" + varName2 + "", "@" + varName2 + "", value2);
                            tfc.Description = "" + varName2 + " contains " + value2;
                            Filters.Add(tfc);
                        }
                    }
                }

                RoutedEventArgs args = new RoutedEventArgs(CaseExportOptions.OkClickEvent);
                Click(this, args);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (Click != null)
            {
                RoutedEventArgs args = new RoutedEventArgs(CaseExportOptions.CancelClickEvent);
                Click(this, args);
            }
        }

        public static readonly RoutedEvent OkClickEvent = EventManager.RegisterRoutedEvent(
        "OK", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(CaseExportOptions));

        public static readonly RoutedEvent CancelClickEvent = EventManager.RegisterRoutedEvent(
        "Cancel", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(CaseExportOptions));

        // Provide CLR accessors for the event 
        public event RoutedEventHandler Ok
        {
            add { AddHandler(OkClickEvent, value); }
            remove { RemoveHandler(OkClickEvent, value); }
        }

        public event RoutedEventHandler Cancel
        {
            add { AddHandler(CancelClickEvent, value); }
            remove { RemoveHandler(CancelClickEvent, value); }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            //List<string> districts = new List<string>();

            //IDbDriver db = DataHelper.Database;

            //Query selectQuery = db.CreateQuery("SELECT DISTINCT

            //DataHelper.Database.Select
        }

        private void checkboxFilterData_Unchecked(object sender, RoutedEventArgs e)
        {
            cmbLogicalOperator.SelectedIndex = -1;
            cmbVariableName1.SelectedIndex = -1;
            cmbVariableName2.SelectedIndex = -1;
            tboxValue1.Text = String.Empty;
            tboxValue2.Text = String.Empty;
        }
    }
}
