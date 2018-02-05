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
    /// Interaction logic for CaseActionsRowControl.xaml
    /// </summary>
    public partial class ContactActionsRowControl : UserControl
    {
        public static readonly DependencyProperty ShowConvertButtonProperty = DependencyProperty.Register("ShowConvertButton", typeof(bool), typeof(ContactActionsRowControl));
        public bool ShowConvertButton
        {
            get
            {
                return (bool)(this.GetValue(ShowConvertButtonProperty));
            }
            set
            {
                this.SetValue(ShowConvertButtonProperty, value);
            }
        }

        public static readonly DependencyProperty ShowPrintButtonProperty = DependencyProperty.Register("ShowPrintButton", typeof(bool), typeof(ContactActionsRowControl));
        public bool ShowPrintButton
        {
            get
            {
                return (bool)(this.GetValue(ShowPrintButtonProperty));
            }
            set
            {
                this.SetValue(ShowPrintButtonProperty, value);
            }
        }

        public static readonly DependencyProperty ShowDeleteButtonProperty = DependencyProperty.Register("ShowDeleteButton", typeof(bool), typeof(ContactActionsRowControl));
        public bool ShowDeleteButton
        {
            get
            {
                return (bool)(this.GetValue(ShowDeleteButtonProperty));
            }
            set
            {
                this.SetValue(ShowDeleteButtonProperty, value);
            }
        }

        public event EventHandler DeleteRequested;
        public event EventHandler PrintFormRequested;
        public event EventHandler ConversionToCaseIsoRequested;
        public event EventHandler ConversionToCasePrevSickRequested;
        public event EventHandler ConversionToCaseDiedRequested;

        public ContactActionsRowControl()
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

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (PrintFormRequested != null)
            {
                PrintFormRequested(this, new EventArgs());
            }
        }

        //private void btnConvert_Click(object sender, RoutedEventArgs e)
        //{
        //    if (btnConvert.ContextMenu != null)
        //    {
        //        btnConvert.ContextMenu.PlacementTarget = btnConvert;
        //        btnConvert.ContextMenu.IsOpen = true;
        //    }

        //    e.Handled = true;
        //    return;
        //}

        private void mnuConvertToCaseIso_Click(object sender, RoutedEventArgs e)
        {
            if (ConversionToCaseIsoRequested != null)
            {
                ConversionToCaseIsoRequested(this, new EventArgs());
            }
        }

        private void mnuConvertToCasePrevSick_Click(object sender, RoutedEventArgs e)
        {
            if (ConversionToCasePrevSickRequested != null)
            {
                ConversionToCasePrevSickRequested(this, new EventArgs());
            }
        }

        private void mnuConvertToCaseDied_Click(object sender, RoutedEventArgs e)
        {
            if (ConversionToCaseDiedRequested != null)
            {
                ConversionToCaseDiedRequested(this, new EventArgs());
            }
        }

        private void btnActions_Click(object sender, RoutedEventArgs e)
        {
            if (btnActions.ContextMenu != null)
            {
                btnActions.ContextMenu.PlacementTarget = btnActions;
                btnActions.ContextMenu.IsOpen = true;

                if (ShowConvertButton != true)
                {
                    mnuConvert.Visibility = System.Windows.Visibility.Collapsed;
                }
                if (ShowDeleteButton != true)
                {
                    mnuDelete.Visibility = System.Windows.Visibility.Collapsed;
                }
                if (ShowPrintButton != true)
                {
                    mnuPrint.Visibility = System.Windows.Visibility.Collapsed;
                }
            }

            e.Handled = true;
            return;
        }
    }
}
