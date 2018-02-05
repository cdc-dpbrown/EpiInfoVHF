using System;
using System.Windows.Data;

namespace ContactTracing.Core.Converters
{
    public class YesToCheckedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value.ToString().Equals("1"))
            {
                return true;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)value == true)
            {
                return "1";
            }
            else
            {
                return String.Empty;
            }
        }
    }
}
