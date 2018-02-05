using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using ContactTracing.Controls;
using ContactTracing.ViewModel;

namespace ContactTracing.CaseView.Controls
{
    /// <summary>
    /// Interaction logic for AnalysisViewer.xaml
    /// </summary>
    public partial class AnalysisViewer : UserControl
    {
        public event EventHandler Closed;
        private Popup Popup { get; set; }
        public static readonly DependencyProperty CurrentAsOfDateProperty = DependencyProperty.Register("CurrentAsOfDateProperty", typeof(DateTime), typeof(AnalysisViewer), new FrameworkPropertyMetadata(DateTime.Now));
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

        public AnalysisViewer()
        {
            InitializeComponent();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            if (this.Closed != null)
            {
                Closed(this, new EventArgs());
            }
        }

        public void Compute()
        {
            analysisEpiClassAllPatients.Compute();
            analysisEpiClassAllPatients.DisplayDate = this.CurrentAsOfDate;

            labClassAllPatients.Compute();
            labClassAllPatients.DisplayDate = this.CurrentAsOfDate;

            //dailyStats.Compute();
            //dailyStats.DisplayDate = this.CurrentAsOfDate;

            patientTestedInfo.Compute();

            lastIsoInfo.Compute();

            confirmedProbableTable.Compute();
            confirmedProbableTable.DisplayDate = this.CurrentAsOfDate;

            finalOutcomeTable.Compute();
            finalOutcomeTable.DisplayDate = this.CurrentAsOfDate;

            symptomsTable.Compute();
            symptomsTable.DisplayDate = this.CurrentAsOfDate;

            ageGroupChart.Compute();
            ageTable.Compute();

            residenceTable.Compute();
            residenceTable.DisplayDate = this.CurrentAsOfDate;
            
            onsetLocationTable.Compute();
            onsetLocationTable.DisplayDate = this.CurrentAsOfDate;

            finalOutcomeContactsTable.Compute();
            finalOutcomeContactsTable.DisplayDate = this.CurrentAsOfDate;

            epiCurve.Compute();
            epiCurve.DisplayDate = this.CurrentAsOfDate;

            epiCurveSuspect.Compute();
            epiCurveSuspect.DisplayDate = this.CurrentAsOfDate;
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

            tblockDate.Text = DateTime.Now.ToString("dd-MMM-yyyy HH:MM");
            tblockDateCurrent.Text = this.CurrentAsOfDate.ToString("dd-MMM-yyyy HH:MM");

            Compute();
        }
    }
}
