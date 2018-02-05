using System;
using System.Windows.Data;

namespace ContactTracing.Core.Converters
{
    public class IsPositiveConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string strValue = value.ToString();

            if (strValue.StartsWith("1")) return true;
            else return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
