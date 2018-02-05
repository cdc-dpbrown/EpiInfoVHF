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

namespace ContactTracing.Controls
{
    /// <summary>
    /// Interaction logic for ContactTracingLegend.xaml
    /// </summary>
    public partial class ContactTracingLegend : UserControl
    {
        public ContactTracingLegend()
        {
            ContactTracing.WPF.Properties.Resources.Culture = System.Threading.Thread.CurrentThread.CurrentCulture;
            InitializeComponent();
        }
    }
}
