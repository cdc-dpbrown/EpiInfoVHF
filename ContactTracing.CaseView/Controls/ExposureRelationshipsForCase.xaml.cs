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
using Epi.Data;

namespace ContactTracing.CaseView.Controls
{
    /// <summary>
    /// Interaction logic for ExposureRelationshipsForCase.xaml
    /// </summary>
    public partial class ExposureRelationshipsForCase : UserControl
    {
        public event EventHandler Closed;
        private CaseViewModel CaseVM { get; set; }

        public ExposureRelationshipsForCase(CaseViewModel c)
        {
            InitializeComponent();
            panelCaseHeader.DataContext = c;
            CaseVM = c;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            if (this.Closed != null)
            {
                Closed(this, new EventArgs());
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            //dg.MaxHeight = this.ActualHeight / 2;
            dgExposures.MaxHeight = this.ActualHeight / 3;
            dgSourceCases.MaxHeight = this.ActualHeight / 3;

            EpiDataHelper dataHelper = (this.DataContext as EpiDataHelper);
            this.Cursor = Cursors.Wait;
            if (dataHelper != null)
            {
                dataHelper.ShowExposedCasesForCase.Execute(CaseVM);
                dataHelper.ShowSourceCasesForCase.Execute(CaseVM);
            }
            this.Cursor = Cursors.Arrow;
        }
    }
}
