using System;
using System.Windows.Data;

namespace ContactTracing.CaseView.Converters
{
    public sealed class TempStringFormatter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || !(value is Double)) return String.Empty;

            double temperature = (double)value;

            if (temperature == 0.0) return "n/a";

            return temperature.ToString("F1");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
