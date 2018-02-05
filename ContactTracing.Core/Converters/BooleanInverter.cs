using System;
using System.Windows.Data;

namespace ContactTracing.Core.Converters
{
    public class BooleanInverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool newValue = (bool)value;
            return !newValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool newValue = (bool)value;
            return !newValue;
        }
    }
}
