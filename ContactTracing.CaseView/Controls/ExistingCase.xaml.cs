using System;
using System.Collections.Generic;
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
using Epi.Data;
using ContactTracing.ViewModel;

namespace ContactTracing.CaseView.Controls
{
    /// <summary>
    /// Interaction logic for ExistingCase.xaml
    /// </summary>
    public partial class ExistingCase : UserControl
    {
        public event RoutedEventHandler Click;

        #region Properties
        public CaseViewModel CaseVM
        {
            get
            {
                return (dgCases.SelectedItem as CaseViewModel);
            }
        }

        public CaseViewModel ExposedCaseVM { get; set; }

        public EpiDataHelper DataHelper
        {
            get
            {
                return (this.DataContext as EpiDataHelper);
            }
        }

        public DateTime? DateLastContact
        {
            get
            {
                return dateContact.SelectedDate;
            }
            set
            {
                dateContact.SelectedDate = value;
            }
        }

        public string Relationship
        {
            get
            {
                return this.txtRelationship.Text;
            }
            set
            {
                this.txtRelationship.Text = value;
            }
        }

        public bool IsTentative
        {
            get
            {
                return this.cbxTentative.IsChecked.Value;
            }
            set
            {
                this.cbxTentative.IsChecked = value;
            }
        }

        public bool IsEstimated
        {
            get
            {
                return this.cbxEstimated.IsChecked.Value;
            }
            set
            {
                this.cbxEstimated.IsChecked = value;
            }
        }

        public int? ContactType
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                if (cbxCon1.IsChecked == true) sb.Append("1");
                else sb.Append("0");
                if (cbxCon2.IsChecked == true) sb.Append("1");
                else sb.Append("0");
                if (cbxCon3.IsChecked == true) sb.Append("1");
                else sb.Append("0");
                if (cbxCon4.IsChecked == true) sb.Append("1");
                else sb.Append("0");

                return Convert.ToInt32(sb.ToString(), 2);

                //if (cmbContact.SelectedIndex == -1 || cmbContact.SelectedIndex == 0)
                //{
                //    return null;
                //}
                //return cmbContact.SelectedIndex;
            }
        }
        #endregion // Properties

        public ExistingCase()
        {
            InitializeComponent();
        }

        private void dg_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            e.Cancel = true;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (dgCases.SelectedItems.Count != 1) return;

            if (CaseVM.IsLocked)
            {
                MessageBox.Show("Either this case or contacts of this case are locked for editing. Please wait until other users have released this lock before proceeding.", "Record locked", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (dgCases.Visibility == System.Windows.Visibility.Visible)
            {
                if (ExposedCaseVM != null && (ExposedCaseVM == CaseVM || ExposedCaseVM.RecordId == CaseVM.RecordId))
                {
                    MessageBox.Show("A case cannot be its own source case.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                if (DataHelper.CheckForCircularRelationship(CaseVM, ExposedCaseVM) == true)
                {
                    MessageBox.Show("The selected case (" + CaseVM.ID + ") was exposed by " + ExposedCaseVM.ID + ". Two cases cannot be each other's source case.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                if (DataHelper.CheckForDuplicateCaseToCaseRelationship(CaseVM, ExposedCaseVM))
                {
                    MessageBox.Show("The selected case (" + CaseVM.ID + ") is already a source case of " + ExposedCaseVM.ID + ". ", "Cannot add case", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                dateContact.SelectedDate = null;

                //if (ExposedCaseVM != null)
                //{
                //    IDbDriver db = DataHelper.Project.CollectedData.GetDatabase();
                //    Query selectQuery = db.CreateQuery("SELECT * FROM [metaLinks] WHERE [FromRecordGuid] = @FromRecordGuid AND [FromViewId] = @FromViewId AND [ToViewId] = @ToViewId");
                //    selectQuery.Parameters.Add(new QueryParameter("@FromRecordGuid", DbType.String, caseVM.RecordId));
                //    selectQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, DataHelper.CaseFormId));
                //    selectQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, CaseFormId));
                //    DataTable dt = db.Select(selectQuery);

                //    var query = from cepVM in DataHelper.CurrentExposureCollection
                //                where cepVM.ExposedCaseVM == CaseVM && cepVM.SourceCaseVM == ExposedCaseVM
                //                          select cepVM;

                //    int foundCount = query.Count();

                //    if (foundCount > 0)
                //    {
                //        CaseExposurePairViewModel cepVM = query.First();
                //        MessageBox.Show("Case " + cepVM.SourceCaseVM + " is already a source case for " + cepVM.ExposedCaseVM + ".", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                //        return;
                //    }
                //}

                dgCases.Visibility = System.Windows.Visibility.Collapsed;
                panelInfo.Visibility = System.Windows.Visibility.Visible;
                txtBorderHeading.Text = Properties.Resources.RelationshipInformation;
                casesSearchBox.Visibility = System.Windows.Visibility.Collapsed;

                txtName.Text = CaseVM.Surname + ", " + CaseVM.OtherNames;
                if (CaseVM.Age.HasValue)
                {
                    txtAge.Text = CaseVM.Age.Value.ToString();
                }

                Converters.GenderConverter converter = new Converters.GenderConverter();
                txtSex.Text = converter.Convert(CaseVM.Gender, null, false, null).ToString(); // CaseVM.Gender;

                txtRelationship.Text = String.Empty;
                dateContact.DisplayDate = DateTime.Now;
                cbxTentative.IsChecked = false;
                cbxEstimated.IsChecked = false;

                cbxCon1.IsChecked = false;
                cbxCon2.IsChecked = false;
                cbxCon3.IsChecked = false;
                cbxCon4.IsChecked = false;
            }
            else if (Click != null)
            {
                if (dateContact.SelectedDate.HasValue)
                {
                    dgCases.Visibility = System.Windows.Visibility.Visible;
                    casesSearchBox.Visibility = System.Windows.Visibility.Visible;
                    panelInfo.Visibility = System.Windows.Visibility.Collapsed;
                    txtBorderHeading.Text = "All cases";
                    ExposedCaseVM = null;
                    RoutedEventArgs args = new RoutedEventArgs(ExistingCase.OkClickEvent);
                    Click(this, args);
                }
                else
                {
                    MessageBox.Show("Must have a date of last contact to proceed.");
                    return;
                }
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (Click != null)
            {
                dgCases.Visibility = System.Windows.Visibility.Visible;
                casesSearchBox.Visibility = System.Windows.Visibility.Visible;
                panelInfo.Visibility = System.Windows.Visibility.Collapsed;
                txtBorderHeading.Text = "All contacts";

                cbxTentative.IsChecked = false;
                cbxEstimated.IsChecked = false;
                txtRelationship.Text = String.Empty;
                dateContact.DisplayDate = DateTime.Today;

                ExposedCaseVM = null;

                RoutedEventArgs args = new RoutedEventArgs(ExistingCase.CancelClickEvent);
                Click(this, args);
            }
        }

        public static readonly RoutedEvent OkClickEvent = EventManager.RegisterRoutedEvent(
        "OK", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ExistingCase));

        public static readonly RoutedEvent CancelClickEvent = EventManager.RegisterRoutedEvent(
        "Cancel", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ExistingCase));

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
    }
}