using System;
using System.Windows.Data;

namespace ContactTracing.CaseView.Converters
{
    public sealed class TempStringPrefix : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || !(value is Double)) return String.Empty;

            double temperature = (double)value;

            if (temperature == 0.0) return String.Empty;

            return String.Format("T{0}: {1}", parameter.ToString(), temperature.ToString("F1"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
