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
    /// Interaction logic for ContactLinkActionsRowControl.xaml
    /// </summary>
    public partial class ContactLinkActionsRowControl : UserControl
    {
        public static readonly DependencyProperty ShowUnlinkOptionProperty = DependencyProperty.Register("ShowUnlinkOption", typeof(bool), typeof(ContactLinkActionsRowControl), new PropertyMetadata(false));
        public bool ShowUnlinkOption
        {
            get
            {
                return (bool)(this.GetValue(ShowUnlinkOptionProperty));
            }
            set
            {
                this.SetValue(ShowUnlinkOptionProperty, value);
            }
        }

        public event EventHandler EditLinkRequested;
        public event EventHandler UnlinkContactRequested;

        public ContactLinkActionsRowControl()
        {
            InitializeComponent();
        }

        private void btnLink_Click(object sender, RoutedEventArgs e)
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

            if (!ShowUnlinkOption)
            {
                if (EditLinkRequested != null)
                {
                    EditLinkRequested(this, new EventArgs());
                }
            }
            else
            {
                if (btnLink.ContextMenu != null)
                {
                    btnLink.ContextMenu.PlacementTarget = btnLink;
                    btnLink.ContextMenu.IsOpen = true;
                }

                e.Handled = true;
                return;
            }
        }

        private void mnuEditLink_Click(object sender, RoutedEventArgs e)
        {
            if (EditLinkRequested != null)
            {
                EditLinkRequested(this, new EventArgs());
            }
        }

        private void mnuUnlink_Click(object sender, RoutedEventArgs e)
        {
            if (UnlinkContactRequested != null)
            {
                UnlinkContactRequested(this, new EventArgs());
            }
        }

    }
}
