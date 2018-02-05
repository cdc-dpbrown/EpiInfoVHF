using System;
using System.Windows.Data;

namespace ContactTracing.CaseView.Converters
{
    class TaskbarProgressStateToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return true;

            System.Windows.Shell.TaskbarItemProgressState state = (System.Windows.Shell.TaskbarItemProgressState)value;

            if (state == System.Windows.Shell.TaskbarItemProgressState.Indeterminate)
                return true;

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
