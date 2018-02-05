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
using System.Xml;
using ContactTracing.ViewModel;

namespace ContactTracing.Controls
{
    /// <summary>
    /// Interaction logic for ProjectSettings.xaml
    /// </summary>
    public partial class ProjectSettings : UserControl
    {
        private bool _includeNewProjectDetails = false;

        public event RoutedEventHandler Closed;

        public bool IncludeNewProjectDetails 
        {
            get
            {
                return this._includeNewProjectDetails;
            }
            set
            {
                this._includeNewProjectDetails = value;

                if (IncludeNewProjectDetails)
                {
                    tblockFileName.Visibility = System.Windows.Visibility.Visible;
                    txtFileName.Visibility = System.Windows.Visibility.Visible;
                    spacerFileName.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    tblockFileName.Visibility = System.Windows.Visibility.Collapsed;
                    txtFileName.Visibility = System.Windows.Visibility.Collapsed;
                    spacerFileName.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
        }

        public ProjectSettings()
        {
            InitializeComponent();
            this.dpOutbreakDate.SelectedDate = DateTime.Today;
            this.dpOutbreakDate.DisplayDate = DateTime.Today;
            IncludeNewProjectDetails = false;

            IsVisibleChanged += ProjectSettings_IsVisibleChanged;
        }

        void ProjectSettings_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // TODO: standardize as per ApplicationviewModel  
            if (Country == "USA")
            {
                spCountryAggregates.Visibility = System.Windows.Visibility.Hidden;
                chkIsShortForm.Visibility = System.Windows.Visibility.Hidden;
                //chkIsShortForm.IsChecked = false;
                //IsShortForm = false;
            }
            else
            {
                spCountryAggregates.Visibility = System.Windows.Visibility.Visible;
                chkIsShortForm.Visibility = System.Windows.Visibility.Visible;
                //chkIsShortForm.IsChecked = true;
                //IsShortForm = true;

                cmbCountry.SelectedIndex = 10;
            }

            //dpOutbreakDate.SelectedDate = DateTime.Today;
            //cmbVirus.SelectedIndex = 0;
            //cmbPattern.SelectedIndex = 1;


        }



    
        public string OutbreakName { get { return this.txtOutbreakName.Text; }
            set
            {
                this.txtOutbreakName.Text = value;
            }
        }

        public string IDPrefix { get { return this.txtPrefix.Text; }
            set
            {
                this.txtPrefix.Text = value;
            }
        }

        public string IDSeparator { get { return this.txtSep.Text; }
            set
            {
                this.txtSep.Text = value;
            }
        }

        public DateTime? OutbreakDate { get { return this.dpOutbreakDate.SelectedDate; }
            set 
            {
                this.dpOutbreakDate.SelectedDate = value;
            }
        }

        public string IDPattern
        {
            get
            {
                switch (cmbPattern.SelectedIndex)
                {
                    case 0:
                        return "##";
                    case 1:
                        return "###";
                    case 2:
                        return "####";
                    case 3:
                        return "#####";
                    case 4:
                        return "######";
                    case 5:
                        return "#######";
                    default:
                        return "###";
                }
            }
            set
            {
                cmbPattern.Text = value;
            }
        }

        public string Virus
        {
            get
            {
                switch (cmbVirus.SelectedIndex)
                {
                    case 0:
                        return "Ebola";
                    case 1:
                        return "Sudan";
                    case 2:
                        return "Marburg";
                    case 3:
                        return "Bundibugyo";
                    case 4:
                        return "Rift";
                    case 5:
                        return "Lassa";
                    case 6:
                        return "CCHF";
                    default:
                        return "Ebola";
                }
            }
            set
            {
                cmbVirus.Text = value;
            }
        }

        public string Country
        {
            get { return cmbCountry.Text; }
            set
            {
                cmbCountry.Text = value;
            }
        }

        //17040
        public bool IsShortForm { 
            get {
                return (chkIsShortForm.IsChecked != null ? (bool)chkIsShortForm.IsChecked : false) ;
            }
            set { chkIsShortForm.IsChecked = value;  }
        }

        public void SetDefaults(DataHelperBase dataHelper)
        {
            OutbreakDate = dataHelper.OutbreakDate;
            OutbreakName = dataHelper.OutbreakName;

            switch (dataHelper.VirusTestType)
            {
                case Core.Enums.VirusTestTypes.Bundibugyo:
                    cmbVirus.SelectedIndex = 3;
                    break;
                case Core.Enums.VirusTestTypes.CCHF:
                    cmbVirus.SelectedIndex = 6;
                    break;
                case Core.Enums.VirusTestTypes.Ebola:
                    cmbVirus.SelectedIndex = 0;
                    break;
                case Core.Enums.VirusTestTypes.Lassa:
                    cmbVirus.SelectedIndex = 5;
                    break;
                case Core.Enums.VirusTestTypes.Marburg:
                    cmbVirus.SelectedIndex = 2;
                    break;
                case Core.Enums.VirusTestTypes.Rift:
                    cmbVirus.SelectedIndex = 4;
                    break;
                case Core.Enums.VirusTestTypes.Sudan:
                    cmbVirus.SelectedIndex = 1;
                    break;
            }

            IDPattern = dataHelper.IDPattern;
            IDPrefix = dataHelper.IDPrefix;
            IDSeparator = dataHelper.IDSeparator;
            Country = dataHelper.Country;
            IsShortForm = dataHelper.IsShortForm; //17040
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(txtOutbreakName.Text))
            {
                MessageBox.Show("Outbreak name cannot be blank.");
                return;
            }

            if (String.IsNullOrEmpty(txtSep.Text))
            {
                MessageBox.Show("The ID separator cannot be blank.");
                return;
            }

            if (String.IsNullOrEmpty(txtPrefix.Text))
            {
                MessageBox.Show("Leaving the ID prefix field blank will turn off all case ID validation.", "ID Validation", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            if (Closed != null)
            {
                RoutedEventArgs args = new RoutedEventArgs(ProjectSettings.OkClickEvent);
                Closed(this, args);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (Closed != null)
            {
                RoutedEventArgs args = new RoutedEventArgs(ProjectSettings.CloseClickEvent);
                Closed(this, args);
            }
        }

        //public static readonly RoutedEvent OkClickEvent = EventManager.RegisterRoutedEvent(
        //"OK", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ExistingCase));

        public static readonly RoutedEvent CloseClickEvent = EventManager.RegisterRoutedEvent(
        "Close", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ProjectSettings));

        public static readonly RoutedEvent OkClickEvent = EventManager.RegisterRoutedEvent(
        "OK", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ProjectSettings));

        // Provide CLR accessors for the event 
        //public event RoutedEventHandler Ok
        //{
        //    add { AddHandler(OkClickEvent, value); }
        //    remove { RemoveHandler(OkClickEvent, value); }
        //}

        public event RoutedEventHandler Close
        {
            add { AddHandler(CloseClickEvent, value); }
            remove { RemoveHandler(CloseClickEvent, value); }
        }


    }
}
