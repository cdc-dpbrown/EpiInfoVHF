using System;
using System.Collections.Generic;
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
using ContactTracing.ViewModel;

namespace ContactTracing.CaseView.Controls
{
    /// <summary>
    /// Interaction logic for ConvertToContact.xaml
    /// </summary>
    public partial class ConvertToContact : UserControl
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
            }
        }
        #endregion // Properties

        public ConvertToContact()
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

            CaseViewModel caseVM = dgCases.SelectedItem as CaseViewModel;

            if (caseVM != null)
            {
                if (caseVM.IsLocked)
                {
                    MessageBox.Show("Either this case or contacts of this case are locked for editing. Please wait until other users have released this lock before proceeding.", "Record locked", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (dgCases.Visibility == System.Windows.Visibility.Visible)
                {
                    dateContact.SelectedDate = null;

                    dgCases.Visibility = System.Windows.Visibility.Collapsed;
                    panelInfo.Visibility = System.Windows.Visibility.Visible;
                    txtBorderHeading.Text = Properties.Resources.RelationshipInformation;
                    casesSearchBox.Visibility = System.Windows.Visibility.Collapsed;

                    txtName.Text = CaseVM.Surname + ", " + CaseVM.OtherNames;
                    if (CaseVM.Age.HasValue)
                    {
                        txtAge.Text = CaseVM.Age.Value.ToString();
                    }
                    //txtSex.Text = CaseVM.Gender;

                    Converters.GenderConverter converter = new Converters.GenderConverter();
                    txtSex.Text = converter.Convert(CaseVM.Gender, null, false, null).ToString(); // CaseVM.Gender;

                    txtRelationship.Text = String.Empty;
                    dateContact.DisplayDate = DateTime.Now;

                    cbxEstimated.IsChecked = false;

                    cbxCon1.IsChecked = false;
                    cbxCon2.IsChecked = false;
                    cbxCon3.IsChecked = false;
                    cbxCon4.IsChecked = false;
                }
                else if (Click != null)
                {
                    dgCases.Visibility = System.Windows.Visibility.Visible;
                    casesSearchBox.Visibility = System.Windows.Visibility.Visible;
                    panelInfo.Visibility = System.Windows.Visibility.Collapsed;
                    txtBorderHeading.Text = "All cases";

                    RoutedEventArgs args = new RoutedEventArgs(ConvertToContact.OkClickEvent);
                    Click(this, args);
                }
            }
            else
            {
                MessageBox.Show("No case selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
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

                txtRelationship.Text = String.Empty;
                dateContact.DisplayDate = DateTime.Today;

                RoutedEventArgs args = new RoutedEventArgs(ConvertToContact.CancelClickEvent);
                Click(this, args);
            }
        }

        public static readonly RoutedEvent OkClickEvent = EventManager.RegisterRoutedEvent(
        "OK", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ConvertToContact));

        public static readonly RoutedEvent CancelClickEvent = EventManager.RegisterRoutedEvent(
        "Cancel", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ConvertToContact));

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
