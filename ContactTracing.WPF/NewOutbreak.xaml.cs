using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ContactTracing.Core.Data;


namespace ContactTracing.Controls
{
    /// <summary>
    /// Interaction logic for NewOutbreak.xaml
    /// </summary>
    public partial class NewOutbreak : UserControl
    {
        public event RoutedEventHandler Closed;


        public NewOutbreak()
        {
            InitializeComponent();
            this.dpOutbreakDate.SelectedDate = DateTime.Today;
            this.dpOutbreakDate.DisplayDate = DateTime.Today;


            //if (System.Threading.Thread.CurrentThread.CurrentUICulture.ToString().Equals("en-US", StringComparison.OrdinalIgnoreCase))
            //{
            //    cmbCountry.SelectedIndex = 10;
            //}

            this.IsVisibleChanged += NewOutbreak_IsVisibleChanged;

            //if (textApplicationRegion.Text == "USA")
            //{
            //    spCountryAggregates.Visibility = System.Windows.Visibility.Collapsed;
            //}
            //else
            //{
            //    spCountryAggregates.Visibility = System.Windows.Visibility.Visible;
            //}

        }

        void NewOutbreak_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.Visibility == System.Windows.Visibility.Visible)
            {
                if (Country == "USA")
                {
                    spCountryAggregates.Visibility = System.Windows.Visibility.Hidden;
                    chkIsShortForm.Visibility = System.Windows.Visibility.Hidden;
                    chkIsShortForm.IsChecked = false;
                }
                else
                {
                    spCountryAggregates.Visibility = System.Windows.Visibility.Visible;
                    chkIsShortForm.Visibility = System.Windows.Visibility.Visible;
                    chkIsShortForm.IsChecked = true;

                    string currentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture.ToString();

                    if (currentCulture.Equals("fr-FR", StringComparison.OrdinalIgnoreCase) || currentCulture.Equals("fr", StringComparison.OrdinalIgnoreCase))
                    {
                        cmbCountry.SelectedIndex = 10;
                    }
                    else
                    {
                        cmbCountry.SelectedIndex = 23;
                    }
                }
                dpOutbreakDate.SelectedDate = DateTime.Today;
                cmbVirus.SelectedIndex = 0;
                cmbPattern.SelectedIndex = 1;
            }
        }


        public string FileName
        {
            get { return this.txtFileName.Text; }
            set
            {
                this.txtFileName.Text = value;
            }
        }

        public string OutbreakName
        {
            get { return this.txtOutbreakName.Text; }
            set
            {
                this.txtOutbreakName.Text = value;
            }
        }

        public string IDPrefix
        {
            get { return this.txtPrefix.Text; }
            set
            {
                this.txtPrefix.Text = value;
            }
        }

        public string IDSeparator
        {
            get { return this.txtSep.Text; }
            set
            {
                this.txtSep.Text = value;
            }
        }

        public DateTime? OutbreakDate
        {
            get { return this.dpOutbreakDate.SelectedDate; }
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
            get
            { //return cmbCountry.Text;
                if (textApplicationRegion.Text == "USA")
                {

                    return textApplicationRegion.Text;
                }
                else
                {
                    return cmbCountry.Text;
                }
            }
            set
            {
                cmbCountry.Text = value;
            }
        }

        public bool IsShortForm //17040
        {
            get
            {
                return (chkIsShortForm.IsChecked != null ? (bool)chkIsShortForm.IsChecked : false);
            }
            set { chkIsShortForm.IsChecked = value; }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            string currentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture.ToString();

            //17157
            //No error message for Cameroon coz it is possible to have the french and english as official language.
            if (currentCulture.Equals("en-US", StringComparison.OrdinalIgnoreCase))
            {
                if (cmbCountry.Text.Equals("Senegal", StringComparison.OrdinalIgnoreCase) ||
                    cmbCountry.Text.Equals("Democratic Republic of the Congo", StringComparison.OrdinalIgnoreCase) ||
                    cmbCountry.Text.Equals("Mali", StringComparison.OrdinalIgnoreCase) ||
                    cmbCountry.Text.Equals("Côte d'Ivoire", StringComparison.OrdinalIgnoreCase) ||
                    cmbCountry.Text.Equals("Burkina Faso", StringComparison.OrdinalIgnoreCase) ||
                    cmbCountry.Text.Equals("Benin", StringComparison.OrdinalIgnoreCase) ||
                    cmbCountry.Text.Equals("Togo", StringComparison.OrdinalIgnoreCase) ||
                    cmbCountry.Text.Equals("Niger", StringComparison.OrdinalIgnoreCase) ||
                    cmbCountry.Text.Equals("Central African Republic", StringComparison.OrdinalIgnoreCase) ||
                    cmbCountry.Text.Equals("Mauritania", StringComparison.OrdinalIgnoreCase) ||
                    cmbCountry.Text.Equals("Gabon", StringComparison.OrdinalIgnoreCase) ||
                    cmbCountry.Text.Equals("Guinea", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("This computer is set to English. A new outbreak cannot be created for " + cmbCountry.Text + " because the form for this country is in French. Please change your application settings to French and try again", "Language Settings", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }

            // FOR USA ANTIPATERN  


            if (currentCulture.Equals("fr-FR", StringComparison.OrdinalIgnoreCase) || currentCulture.Equals("fr", StringComparison.OrdinalIgnoreCase))
            {
                if (cmbCountry.Text.Equals("Uganda", StringComparison.OrdinalIgnoreCase) ||
                    cmbCountry.Text.Equals("Liberia", StringComparison.OrdinalIgnoreCase) ||
                    cmbCountry.Text.Equals("Ghana", StringComparison.OrdinalIgnoreCase) ||
                    cmbCountry.Text.Equals("Tanzania", StringComparison.OrdinalIgnoreCase) ||
                    cmbCountry.Text.Equals("Sierra Leone", StringComparison.OrdinalIgnoreCase) ||
                    cmbCountry.Text.Equals("South Africa", StringComparison.OrdinalIgnoreCase) ||
                    cmbCountry.Text.Equals("The Gambia", StringComparison.OrdinalIgnoreCase) ||
                    cmbCountry.Text.Equals("South Sudan", StringComparison.OrdinalIgnoreCase) ||
                    cmbCountry.Text.Equals("Kenya", StringComparison.OrdinalIgnoreCase) ||
                    cmbCountry.Text.Equals("Ethiopia", StringComparison.OrdinalIgnoreCase) || 
                    cmbCountry.Text.Equals("Nigeria", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("This computer is set to French. A new outbreak cannot be created for " + cmbCountry.Text + " because the form for this country is in English. Please change your application settings to English and try again", "Language Settings", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }

            if (String.IsNullOrEmpty(txtFileName.Text))
            {
                MessageBox.Show("File name cannot be blank.");
                return;
            }

            Match m = Regex.Match(txtFileName.Text, "^[a-zA-Z0-9_]*$");
            if (!m.Success)
            {
                MessageBox.Show("Invalid file name detected for this outbreak. File names may only contain letters, numbers, and underscores. The operation has been halted.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

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
                //MessageBox.Show("The ID prefix cannot be blank.");
                //return;
            }

            if (Closed != null)
            {
                RoutedEventArgs args = new RoutedEventArgs(NewOutbreak.OkClickEvent);
                Closed(this, args);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (Closed != null)
            {
                RoutedEventArgs args = new RoutedEventArgs(NewOutbreak.CloseClickEvent);
                Closed(this, args);
            }
        }

        //public static readonly RoutedEvent OkClickEvent = EventManager.RegisterRoutedEvent(
        //"OK", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ExistingCase));

        public static readonly RoutedEvent CloseClickEvent = EventManager.RegisterRoutedEvent(
        "Close", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(NewOutbreak));

        public static readonly RoutedEvent OkClickEvent = EventManager.RegisterRoutedEvent(
        "OK", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(NewOutbreak));

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

        private void txtFileName_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Match m = Regex.Match(e.Text, "[A-Za-z0-9_]");
            if (!m.Success)
            {
                e.Handled = true;
            }
        }

        public string Adm1
        {
            get
            {
                return txtADM1.Text;
            }
        }

        public string Adm2
        {
            get
            {
                return txtADM2.Text;
            }
        }

        public string Adm3
        {
            get
            {
                return txtADM3.Text;
            }
        }

        public string Adm4
        {
            get
            {
                return txtADM4.Text;
            }
        }


    }
}
