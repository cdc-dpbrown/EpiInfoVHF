using System;
using System.Windows.Data;

namespace ContactTracing.Core.Converters
{
    public class LengthToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int length = (int)value;

            if (length > 0) return System.Windows.Visibility.Hidden;
            else return System.Windows.Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
