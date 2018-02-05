using System;
using System.Windows.Data;

namespace ContactTracing.Core.Converters
{
    public class BooleanToCollapsedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool newValue = (bool)value;
            if (newValue == true)
            {
                return System.Windows.Visibility.Collapsed;
            }
            else
            {
                return System.Windows.Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
