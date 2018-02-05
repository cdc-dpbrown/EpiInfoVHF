using System;
using System.Windows.Data;

namespace ContactTracing.Core.Converters
{
    public class PlusOneConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int newValue = (int)value;
            return (newValue + 1);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int newValue = (int)value;
            return (newValue - 1);
        }
    }
}
