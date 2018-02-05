using System;
using System.Windows.Data;

namespace ContactTracing.CaseView.Converters
{
    public sealed class DateToBorderThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || !(value is DateTime)) return 1;

            DateTime dt = (DateTime)value;

            DateTime today = DateTime.Today;

            if (dt.Year == today.Year && dt.Month == today.Month && dt.Day == today.Day)
            {
                return 3;
            }

            return 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
