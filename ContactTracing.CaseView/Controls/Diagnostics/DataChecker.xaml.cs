using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using ContactTracing.ViewModel;
using Epi.Data;

namespace ContactTracing.CaseView.Controls.Diagnostics
{
    /// <summary>
    /// Interaction logic for DataChecker.xaml
    /// </summary>
    public partial class DataChecker : UserControl
    {
        public DataChecker()
        {
            InitializeComponent();
        }

        public event EventHandler Closed;

        public EpiDataHelper DataHelper { get { return this.DataContext as EpiDataHelper; } }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            if (this.Closed != null)
            {
                Closed(this, new EventArgs());
            }

            DataHelper.IssueCollection.Clear();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            dg.MaxHeight = this.ActualHeight - 160;
        }
    }
}
