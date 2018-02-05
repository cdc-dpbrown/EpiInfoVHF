using System;
using System.Windows.Data;

namespace ContactTracing.Core.Converters
{
    public class NumberToUnknownConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return false;
            string strValue = value.ToString();

            if (strValue.StartsWith("3")) return true;
            else return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)(value) == true)
            {
                return "3";
            }
            else return String.Empty;
        }
    }
}
