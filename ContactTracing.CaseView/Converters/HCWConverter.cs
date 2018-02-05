using System;
using System.Windows.Data;

namespace ContactTracing.CaseView.Converters
{
    public class HCWConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool passThroughOnFail = false;
            if (parameter != null && parameter is bool)
            {
                passThroughOnFail = (bool)parameter;
            }

            bool HCW = (bool)(value);
            string textRepresentation = String.Empty;
            switch (HCW)
            {
                case true:
                    textRepresentation = Properties.Resources.Yes;
                    break;
                case false:
                    textRepresentation = Properties.Resources.No; 
                    break;
                default:
                    if (passThroughOnFail)
                    {
                        textRepresentation = value.ToString();
                    }
                    else
                    {
                        textRepresentation = String.Empty;
                    }
                    break;
            }

            return textRepresentation;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string text = value as string;
            bool HCW  = false;
            if (text.Equals(Properties.Resources.Yes))
            {
                HCW = true;
            }
            else if (text.Equals(Properties.Resources.Female))
            {
                HCW = false;
            }

            return HCW;
        }
    }
}
