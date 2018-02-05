using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    /// Interaction logic for DailyFollowUpCard.xaml
    /// </summary>
    public partial class DailyFollowUpCard : UserControl
    {
        public static readonly DependencyProperty IsCountryUSProperty = DependencyProperty.Register("IsCountryUS", typeof(bool), typeof(DailyFollowUpCard), new PropertyMetadata(false));
        public bool IsCountryUS
        {
            get
            {
                return (bool)(this.GetValue(IsCountryUSProperty));
            }
            set
            {
                this.SetValue(IsCountryUSProperty, value);
            }
        }

        public DailyFollowUpCard()
        {
            InitializeComponent();
        }
    }
}
