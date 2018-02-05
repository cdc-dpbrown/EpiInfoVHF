using System;
using System.Windows.Data;

namespace ContactTracing.CaseView.Converters
{
    public sealed class DayStringPrefix : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return String.Empty;

            int day = (int)value;

            if (day == 0) return String.Empty;

            return String.Format("Day {0}", day.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
