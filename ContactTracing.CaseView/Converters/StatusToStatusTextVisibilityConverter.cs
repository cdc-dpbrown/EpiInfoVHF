using System;
using System.Windows;
using System.Windows.Data;
using ContactTracing.Core;

namespace ContactTracing.CaseView.Converters
{
    public sealed class StatusToStatusTextVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return Visibility.Collapsed;

            ContactDailyStatus status = (ContactDailyStatus)value;

            if (status == ContactDailyStatus.SeenSickAndNotIsolated || 
                status == ContactDailyStatus.Dead ||
                status == ContactDailyStatus.SeenNotSick ||
                status == ContactDailyStatus.SeenSickAndIsoNotFilledOut ||
                status == ContactDailyStatus.SeenSickAndIsolated)
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
