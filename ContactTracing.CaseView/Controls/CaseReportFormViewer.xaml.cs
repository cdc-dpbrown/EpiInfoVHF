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
    /// Interaction logic for CaseReportFormViewer.xaml
    /// </summary>
    public partial class CaseReportFormViewer : UserControl
    {
        public event EventHandler Closed;

        public CaseReportFormViewer()
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
    }
}
