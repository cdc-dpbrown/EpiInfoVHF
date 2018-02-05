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
using ContactTracing.ViewModel;

namespace ContactTracing.CaseView.Controls
{
    public class SearchBox : Control
    {
        static SearchBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SearchBox), new FrameworkPropertyMetadata(typeof(SearchBox)));
        }

        public static readonly DependencyProperty SearchTextProperty = DependencyProperty.Register("SearchText", typeof(string), typeof(SearchBox));
        public string SearchText
        {
            get
            {
                return this.GetValue(SearchTextProperty).ToString();
            }
            set
            {
                this.SetValue(SearchTextProperty, value);
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            //var txtSearch = this.GetTemplateChild("txtSearch") as TextBox;
            //if (txtSearch != null)
            //{
            //    txtSearch.KeyDown += txtSearch_KeyDown;
            //}
        }

        public static readonly DependencyProperty SearchProperty = DependencyProperty.Register("Search", typeof(ICommand), typeof(SearchBox));
        public ICommand Search
        {
            get
            {
                return (ICommand)this.GetValue(SearchProperty);
            }
            set
            {
                this.SetValue(SearchProperty, value);
            }
        }
    }
}
