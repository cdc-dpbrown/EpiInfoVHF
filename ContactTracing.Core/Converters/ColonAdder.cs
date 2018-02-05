using System;
using System.Windows.Data;

namespace ContactTracing.Core.Converters
{
    public class ColonAdder : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string text = value as string;
            if (text != null)
            {
                return text + ":";
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
}
