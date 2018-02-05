using System;
using System.Windows.Data;

namespace ContactTracing.Core.Converters
{
    public class NumberToNoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return false;
            string strValue = value.ToString();

            if (strValue.StartsWith("2")) return true;
            else return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)(value) == true)
            {
                return "2";
            }
            else return String.Empty;
        }
    }
}
