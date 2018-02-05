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
    /// Interaction logic for MenuItemPanel.xaml
    /// </summary>
    public partial class MenuItemPanel : UserControl
    {
        private SolidColorBrush highlightColor = new SolidColorBrush(Color.FromRgb(94, 168, 222));

        public MenuItemPanel()
        {
            Selected = false;
            InitializeComponent();
            SetMainButtonMouseLeave();
        }

        public SolidColorBrush HighlightColor
        {
            get
            {
                return this.highlightColor;
            }
            set
            {
                this.highlightColor = value;
                triangle.Fill = value;
                triangle.Stroke = value;
            }
        }

        public bool Selected { get; private set; }

        public string Text
        {
            get
            {
                return this.textblockMain.Text;
            }
            set
            {
                this.textblockMain.Text = value;
            }
        }

        public UIElement AssociatedPanel { get; set; }

        public void SetMainButtonMouseEnter()
        {
            if (!Selected)
            {
                line.Stroke = HighlightColor;
                panelInner.Background = new SolidColorBrush(Color.FromRgb(247, 247, 247));
                this.Cursor = Cursors.Hand;
            }
        }

        public void SetMainButtonMouseLeave()
        {
            if (!Selected)
            {
                line.Stroke = new SolidColorBrush(Color.FromRgb(192, 192, 192));
                panelInner.Background = new SolidColorBrush(Color.FromRgb(240, 241, 244));
                this.Cursor = Cursors.Arrow;
            }
        }

        public void Select()
        {
            if (AssociatedPanel != null)
            {
                AssociatedPanel.Visibility = System.Windows.Visibility.Visible;
            }
            line.Stroke = HighlightColor;
            panelInner.Background = HighlightColor;
            textblockMain.Foreground = grdMain.Resources["TextLight"] as SolidColorBrush;
            triangle.Visibility = System.Windows.Visibility.Visible;
            Selected = true;
        }

        public void Deselect()
        {
            if (AssociatedPanel != null)
            {
                AssociatedPanel.Visibility = System.Windows.Visibility.Collapsed;
            }
            line.Stroke = new SolidColorBrush(Color.FromRgb(192, 192, 192));
            panelInner.Background = new SolidColorBrush(Color.FromRgb(240, 241, 244));
            textblockMain.Foreground = grdMain.Resources["TextDark"] as SolidColorBrush;
            triangle.Visibility = System.Windows.Visibility.Collapsed;
            Selected = false;
        }

        private void StackPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            SetMainButtonMouseEnter();
        }

        private void StackPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            SetMainButtonMouseLeave();
        }

        private void StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Select();
        }
    }
}
