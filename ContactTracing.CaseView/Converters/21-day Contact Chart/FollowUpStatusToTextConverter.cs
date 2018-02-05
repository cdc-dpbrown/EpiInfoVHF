using System;
using System.Windows.Data;
using ContactTracing.Core;

namespace ContactTracing.CaseView.Converters
{
    public sealed class FollowUpStatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return String.Empty;

            ContactDailyStatus status = (ContactDailyStatus)value;

            if (status == ContactDailyStatus.SeenSickAndNotIsolated)
            {
                return Properties.Resources.SingleContactChartSickNotIsolated;
            }

            if (status == ContactDailyStatus.Dead)
            {
                return Properties.Resources.Dead;
            }

            if (status == ContactDailyStatus.SeenSickAndIsolated)
            {
                return Properties.Resources.SingleContactChartSickIsolated;
            }

            if (status == ContactDailyStatus.NotSeen)
            {
                return Properties.Resources.SingleContactChartNotSeen;
            }

            return String.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
