using System;
using System.Windows.Data;

namespace ContactTracing.Core.Converters
{
    public class PCRToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return System.Windows.Visibility.Collapsed;

            int length = value.ToString().Trim().Length;

            if (length > 0) return System.Windows.Visibility.Visible;
            else return System.Windows.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
