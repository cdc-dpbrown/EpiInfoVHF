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
    /// Interaction logic for ShortLabResultForm.xaml
    /// </summary>
    public partial class ShortLabResultForm : UserControl
    {
        public ShortLabResultForm()
        {
            InitializeComponent();
        }

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null && e.OldValue != null && (bool)(e.NewValue) == false && (bool)(e.OldValue) == true)
            {
                ViewModel.LabResultViewModel result = this.DataContext as ViewModel.LabResultViewModel;
                if (result != null)
                {
                    result.CancelEditModeCommand.Execute(null);
                }
            }
        }
    }
}
