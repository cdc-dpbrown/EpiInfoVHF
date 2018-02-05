using System;
using System.Windows.Data;
using ContactTracing.Core;

namespace ContactTracing.CaseView.Converters
{
    public sealed class FollowUpStatusToRotatedPositionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return 0;

            ContactDailyStatus status = (ContactDailyStatus)value;

            if (status == ContactDailyStatus.SeenSickAndNotIsolated)
            {
                return -19;
            }

            if (status == ContactDailyStatus.Dead)
            {
                return 0;
            }

            if (status == ContactDailyStatus.SeenSickAndIsolated)
            {
                return -16;
            }

            if (status == ContactDailyStatus.NotSeen)
            {
                return 0;
            }

            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
