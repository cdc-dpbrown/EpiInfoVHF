using ContactTracing.CaseView.Converters;
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


namespace ContactTracing.CaseView.Controls.Analysis
{
    public class AnalysisOutputBase : UserControl
    {
        private DateTime _displayDate = DateTime.Now;
        IMultiValueConverter dateConverter = new DateConverter();
       
        public DateTime DisplayDate
        {
            get
            {
                return _displayDate; // (DateTime)(this.GetValue(DisplayDateProperty));
            }
            set
            {
                this._displayDate = value; //this.SetValue(DisplayDateProperty, value);
                object element = this.FindName("tblockCurrentDate");
                if (element != null)
                {
                    TextBlock t = element as TextBlock;
                    if (t != null)
                    {
                        string[] parms = { value.ToString(), ((EpiDataHelper)this.DataContext).ApplicationCulture };
                        t.Text = dateConverter.Convert(parms, null, null, null).ToString();
                        //t.Text = value.ToString("dd/MM/yyyy HH:mm");
                    }
                }
            }
        }
    }
}
