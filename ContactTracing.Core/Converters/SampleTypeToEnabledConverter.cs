using System;
using System.Windows.Data;

namespace ContactTracing.Core.Converters
{
    public class SampleTypeToEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (String.IsNullOrEmpty(value.ToString().Trim()))
            {
                return false;
            }
            else if (value.ToString().Equals("5"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
