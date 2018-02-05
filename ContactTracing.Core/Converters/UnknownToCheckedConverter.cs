using System;
using System.Windows.Data;

namespace ContactTracing.Core.Converters
{
    public class UnknownToCheckedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value.ToString().Equals("3"))
            {
                return true;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)value == true)
            {
                return "3";
            }
            else
            {
                return String.Empty;
            }
        }
    }
}
