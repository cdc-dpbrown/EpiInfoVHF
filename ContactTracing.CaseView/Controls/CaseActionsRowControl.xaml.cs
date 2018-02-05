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
    public partial class CaseActionsRowControl : UserControl
    {
        public static readonly DependencyProperty ShowUnlockButtonProperty = DependencyProperty.Register("ShowUnlockButton", typeof(bool), typeof(CaseActionsRowControl), new PropertyMetadata(false));
        public bool ShowUnlockButton
        {
            get
            {
                return (bool)(this.GetValue(ShowUnlockButtonProperty));
            }
            set
            {
                this.SetValue(ShowUnlockButtonProperty, value);
            }
        }

        public static readonly DependencyProperty ShowRelationshipButtonProperty = DependencyProperty.Register("ShowRelationshipButton", typeof(bool), typeof(CaseActionsRowControl), new PropertyMetadata(false));
        public bool ShowRelationshipButton
        {
            get
            {
                return (bool)(this.GetValue(ShowRelationshipButtonProperty));
            }
            set
            {
                this.SetValue(ShowRelationshipButtonProperty, value);
            }
        }

        public static readonly DependencyProperty ShowConvertButtonProperty = DependencyProperty.Register("ShowConvertButton", typeof(bool), typeof(CaseActionsRowControl), new PropertyMetadata(true));
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

        public static readonly DependencyProperty ShowLabButtonProperty = DependencyProperty.Register("ShowLabButton", typeof(bool), typeof(CaseActionsRowControl), new PropertyMetadata(true));
        public bool ShowLabButton
        {
            get
            {
                return (bool)(this.GetValue(ShowLabButtonProperty));
            }
            set
            {
                this.SetValue(ShowLabButtonProperty, value);
            }
        }

        public static readonly DependencyProperty ShowPrintButtonProperty = DependencyProperty.Register("ShowPrintButton", typeof(bool), typeof(CaseActionsRowControl), new PropertyMetadata(true));
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

        public static readonly DependencyProperty ShowDeleteButtonProperty = DependencyProperty.Register("ShowDeleteButton", typeof(bool), typeof(CaseActionsRowControl), new PropertyMetadata(true));
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

        public delegate void DeleteRequestedEventHandler(object sender, RoutedEventArgs e);

        public event DeleteRequestedEventHandler DeleteRequested;
        public event EventHandler PrintOutcomeFormRequested;
        public event EventHandler PrintFullFormRequested;
        public event EventHandler ConversionToContactRequested;
        public event EventHandler ListLabSamplesRequested;
        public event EventHandler ListExposureRelationshipsRequested;
        public event EventHandler ForceUnlockRequested;

        public CaseActionsRowControl()
        {
            InitializeComponent();
            //IsSelected = false;
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (DeleteRequested != null)
            {
                DeleteRequested(this, new RoutedEventArgs());
            }
            else
            {
                MessageBox.Show("That action is not allowed from here.");
            }
        }

        //private void btnPrint_Click(object sender, RoutedEventArgs e)
        //{
        //    if (btnPrint.ContextMenu != null)
        //    {
        //        btnPrint.ContextMenu.PlacementTarget = btnPrint;
        //        btnPrint.ContextMenu.IsOpen = true;
        //    }

        //    e.Handled = true;
        //    return;
        //}

        private void btnConvert_Click(object sender, RoutedEventArgs e)
        {
            if (ConversionToContactRequested != null)
            {
                ConversionToContactRequested(this, new EventArgs());
            }
            else
            {
                MessageBox.Show("That action is not allowed from here.");
            }
        }

        private void mnuPrintOutcomeForm_Click(object sender, RoutedEventArgs e)
        {
            if (PrintOutcomeFormRequested != null)
            {
                PrintOutcomeFormRequested(this, new EventArgs());
            }
        }

        private void mnuPrintCaseReportForm_Click(object sender, RoutedEventArgs e)
        {
            if (PrintFullFormRequested != null)
            {
                PrintFullFormRequested(this, new EventArgs());
            }
        }

        private void btnLab_Click(object sender, RoutedEventArgs e)
        {
            if (ListLabSamplesRequested != null)
            {
                ListLabSamplesRequested(this, new EventArgs());
            }
        }

        private void btnActions_Click(object sender, RoutedEventArgs e)
        {
            DependencyObject dep = (DependencyObject)e.OriginalSource;

            while ((dep != null) && !(dep is DataGridCell) && !(dep is System.Windows.Controls.Primitives.DataGridColumnHeader))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep == null) return;
            
            if (dep is DataGridCell)
            {
                DataGridCell cell = dep as DataGridCell;

                while ((dep != null) && !(dep is DataGridRow))
                {
                    dep = VisualTreeHelper.GetParent(dep);
                }

                DataGridRow row = dep as DataGridRow;
                row.IsSelected = true;
            }
            
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

        private void btnRelationships_Click(object sender, RoutedEventArgs e)
        {
            if (ListExposureRelationshipsRequested != null)
            {
                ListExposureRelationshipsRequested(this, new EventArgs());
            }
        }

        private void btnForceUnlock_Click(object sender, RoutedEventArgs e)
        {
            if (ForceUnlockRequested != null)
            {
                ForceUnlockRequested(this, new EventArgs());
            }
        }
    }
}
