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
    /// Interaction logic for YesNoUnknownBoxDisplay.xaml
    /// </summary>
    public partial class YesNoUnknownBoxDisplay : UserControl
    {
        public YesNoUnknownBoxDisplay()
        {
            InitializeComponent();
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.DataContext is string)
            {
                string value = (string)(this.DataContext);

                switch (value)
                {
                    case "1":
                    case "Yes":
                        checkboxYes.IsChecked = true;
                        checkboxNo.IsChecked = false;
                        checkboxUnk.IsChecked = false;
                        break;
                    case "2":
                    case "No":
                        checkboxYes.IsChecked = false;
                        checkboxNo.IsChecked = true;
                        checkboxUnk.IsChecked = false;
                        break;
                    case "3":
                    case "Unk":
                    case "Unknown":
                        checkboxYes.IsChecked = false;
                        checkboxNo.IsChecked = false;
                        checkboxUnk.IsChecked = true;
                        break;
                }
            }
        }

        private void checkboxYes_Checked(object sender, RoutedEventArgs e)
        {
            //this.DataContext = "1";
            checkboxNo.IsChecked = false;
            checkboxUnk.IsChecked = false;
        }

        private void checkboxNo_Checked(object sender, RoutedEventArgs e)
        {
            //this.DataContext = "2";
            checkboxUnk.IsChecked = false;
            checkboxYes.IsChecked = false;
        }

        private void checkboxUnk_Checked(object sender, RoutedEventArgs e)
        {
            //this.DataContext = "3";
            checkboxNo.IsChecked = false;
            checkboxYes.IsChecked = false;
        }
    }
}
