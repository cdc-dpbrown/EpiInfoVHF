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

namespace ContactTracing.CaseView.Controls
{
    /// <summary>
    /// Interaction logic for AnalysisCountryViewer.xaml
    /// </summary>
    public partial class AnalysisCountryViewer : UserControl
    {
        public static readonly DependencyProperty CurrentAsOfDateProperty = DependencyProperty.Register("CurrentAsOfDateProperty", typeof(DateTime), typeof(AnalysisCountryViewer), new FrameworkPropertyMetadata(DateTime.Now));
        public DateTime CurrentAsOfDate
        {
            get
            {
                return (DateTime)(this.GetValue(CurrentAsOfDateProperty));
            }
            set
            {
                this.SetValue(CurrentAsOfDateProperty, value);
            }
        }

        public event EventHandler Closed;

        public AnalysisCountryViewer(string countryName)
        {
            InitializeComponent();
            this.CountryName = countryName;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            if (this.Closed != null)
            {
                Closed(this, new EventArgs());
            }
        }

        public string CountryName { get; set; }

        private void Compute()
        {
            countryResTable.DisplayDate = this.CurrentAsOfDate;
            countryResTable.Compute(CountryName);

            countryResTable2.DisplayDate = this.CurrentAsOfDate;
            countryResTable2.Compute(CountryName);

            countryResTable3.DisplayDate = this.CurrentAsOfDate;
            countryResTable3.Compute(CountryName);

            countryResTable4.DisplayDate = this.CurrentAsOfDate;
            countryResTable4.Compute(CountryName);

            countryResTable5.DisplayDate = this.CurrentAsOfDate;
            countryResTable5.Compute(CountryName);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Dialogs.DateAnalysisDialog dateDialog = new Dialogs.DateAnalysisDialog(System.Threading.Thread.CurrentThread.CurrentCulture);
            System.Windows.Forms.DialogResult dateResult = dateDialog.ShowDialog();

            if (dateResult == System.Windows.Forms.DialogResult.OK)
            {
                DateTime analysisDate = dateDialog.SelectedDate; // new DateTime(dateDialog.SelectedDate.Year, dateDialog.SelectedDate.Month, dateDialog.SelectedDate.Day);
                CurrentAsOfDate = analysisDate;
            }
            else
            {
                Closed(this, new EventArgs());
            }

            tblockDate.Text = DateTime.Now.ToString("dd-MMM-yyyy HH:mm");
            tblockDateCurrent.Text = this.CurrentAsOfDate.ToString("dd-MMM-yyyy HH:mm");

            tblockDate2.Text = DateTime.Now.ToString("dd-MMM-yyyy HH:mm");
            tblockDateCurrent2.Text = this.CurrentAsOfDate.ToString("dd-MMM-yyyy HH:mm");

            tblockDate3.Text = DateTime.Now.ToString("dd-MMM-yyyy HH:mm");
            tblockDateCurrent3.Text = this.CurrentAsOfDate.ToString("dd-MMM-yyyy HH:mm");

            tblockDate4.Text = DateTime.Now.ToString("dd-MMM-yyyy HH:mm");
            tblockDateCurrent4.Text = this.CurrentAsOfDate.ToString("dd-MMM-yyyy HH:mm");

            tblockDate5.Text = DateTime.Now.ToString("dd-MMM-yyyy HH:mm");
            tblockDateCurrent5.Text = this.CurrentAsOfDate.ToString("dd-MMM-yyyy HH:mm");

            Compute();
        }
    }
}
