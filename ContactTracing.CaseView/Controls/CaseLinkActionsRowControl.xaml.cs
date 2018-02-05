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
    /// Interaction logic for CaseLinkActionsRowControl.xaml
    /// </summary>
    public partial class CaseLinkActionsRowControl : UserControl
    {
        public event EventHandler EditLinkRequested;
        public event EventHandler UnlinkContactRequested;

        public CaseLinkActionsRowControl()
        {
            InitializeComponent();
        }

        private void btnLink_Click(object sender, RoutedEventArgs e)
        {
            if (btnLink.ContextMenu != null)
            {
                btnLink.ContextMenu.PlacementTarget = btnLink;
                btnLink.ContextMenu.IsOpen = true;
            }

            e.Handled = true;
            return;
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
