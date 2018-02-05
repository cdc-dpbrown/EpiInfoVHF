using System;
using System.Windows.Data;

namespace ContactTracing.Core.Converters
{
    public class SelectedItemToEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int pos = (int)value;

            if (pos >= 0) return true;
            else return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
