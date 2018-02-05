using System;
using System.Windows.Data;

namespace ContactTracing.Core.Converters
{
    public class ProgressBarStartButtonEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return false;
            double progressValue = System.Convert.ToDouble(value);

            if (progressValue == 0) return true;
            else return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
