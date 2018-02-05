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
    /// Interaction logic for LabResultActionsRowControl.xaml
    /// </summary>
    public partial class LabResultActionsRowControl : UserControl
    {
        public event EventHandler DeleteRequested;

        public LabResultActionsRowControl()
        {
            InitializeComponent();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (DeleteRequested != null)
            {
                DeleteRequested(this, new EventArgs());
            }
        }

    }
}
