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

namespace ContactTracing.CaseView.Controls.Printing
{
    /// <summary>
    /// Interaction logic for DateDisplay.xaml
    /// </summary>
    public partial class DateDisplay : UserControl
    {
        public DateDisplay()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty DayMonthYearVisibilityProperty = DependencyProperty.Register("DayMonthYearVisibility", typeof(Visibility?), typeof(DateDisplay));
        public Visibility? DayMonthYearVisibility
        {
            get
            {
                return this.GetValue(DayMonthYearVisibilityProperty) as Nullable<Visibility>;
            }
            set
            {
                this.SetValue(DayMonthYearVisibilityProperty, value);
            }
        }
    }
}
