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
    /// Interaction logic for ExistingContact.xaml
    /// </summary>
    public partial class ExistingContact : UserControl
    {
        #region Constructors
        public ExistingContact()
        {
            InitializeComponent();
            dateContact.DisplayDate = DateTime.Now;
        }
        #endregion Constructors

        #region Events
        public event RoutedEventHandler Click;
        #endregion // Events

        #region Properties
        public ContactViewModel ContactVM
        {
            get
            {
                return (dgAllContacts.SelectedItem as ContactViewModel);
            }
        }

        public CaseViewModel CaseVM { get; set; }

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

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (dgAllContacts.SelectedItems.Count != 1) return;

            ContactViewModel contactVM = dgAllContacts.SelectedItem as ContactViewModel;
            if (contactVM != null)
            {
                if (contactVM.IsLocked)
                {
                    MessageBox.Show("Either this contact or source cases of this contact are locked for editing. Please wait until other users have released this lock before proceeding.", "Record locked", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                else
                {
                    if (CaseVM != null)
                    {
                        if (CaseVM.Contacts.Contains(contactVM))
                        {
                            MessageBox.Show("This contact is already linked to the selected case.", "Cannot add contact", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                    }

                    if (dgAllContacts.Visibility == System.Windows.Visibility.Visible)
                    {
                        dateContact.SelectedDate = null;

                        dgAllContacts.Visibility = System.Windows.Visibility.Collapsed;
                        panelInfo.Visibility = System.Windows.Visibility.Visible;
                        txtBorderHeading.Text = Properties.Resources.RelationshipInformation;
                        casesSearchBox.Visibility = System.Windows.Visibility.Collapsed;

                        txtName.Text = ContactVM.Surname + ", " + ContactVM.OtherNames;
                        if (ContactVM.Age.HasValue)
                        {
                            txtAge.Text = ContactVM.Age.Value.ToString();
                        }
                        txtSex.Text = ContactVM.Gender;

                        cbxEstimated.IsChecked = false;
                        txtRelationship.Text = String.Empty;
                        dateContact.DisplayDate = DateTime.Now;

                        cbxCon1.IsChecked = false;
                        cbxCon2.IsChecked = false;
                        cbxCon3.IsChecked = false;
                        cbxCon4.IsChecked = false;

                        EpiDataHelper dataHelper = this.DataContext as EpiDataHelper;
                        if (dataHelper != null)
                        {
                            dataHelper.SendMessageForLockContact(ContactVM);
                        }
                    }
                    else if (Click != null)
                    {
                        if (dateContact.SelectedDate.HasValue)
                        {
                            dgAllContacts.Visibility = System.Windows.Visibility.Visible;
                            casesSearchBox.Visibility = System.Windows.Visibility.Visible;
                            panelInfo.Visibility = System.Windows.Visibility.Collapsed;
                            txtBorderHeading.Text = "All contacts";

                            EpiDataHelper dataHelper = this.DataContext as EpiDataHelper;
                            if (dataHelper != null)
                            {
                                dataHelper.SendMessageForUnlockContact(ContactVM);
                            }

                            RoutedEventArgs args = new RoutedEventArgs(ExistingContact.OkClickEvent);
                            Click(this, args);
                        }
                        else
                        {
                            MessageBox.Show("Must have a date of last contact to proceed.");
                            return;
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("No contact selected.", "No contact", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (Click != null)
            {
                dgAllContacts.Visibility = System.Windows.Visibility.Visible;
                casesSearchBox.Visibility = System.Windows.Visibility.Visible;
                panelInfo.Visibility = System.Windows.Visibility.Collapsed;
                txtBorderHeading.Text = "All contacts";
                
                cbxEstimated.IsChecked = false;
                txtRelationship.Text = String.Empty;
                dateContact.DisplayDate = DateTime.Today;

                EpiDataHelper dataHelper = this.DataContext as EpiDataHelper;
                if (dataHelper != null)
                {
                    dataHelper.SendMessageForUnlockContact(ContactVM);
                }

                RoutedEventArgs args = new RoutedEventArgs(ExistingContact.CancelClickEvent);
                Click(this, args);
            }
        }

        public static readonly RoutedEvent OkClickEvent = EventManager.RegisterRoutedEvent(
        "OK", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ExistingContact));

        public static readonly RoutedEvent CancelClickEvent = EventManager.RegisterRoutedEvent(
        "Cancel", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ExistingContact));

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
