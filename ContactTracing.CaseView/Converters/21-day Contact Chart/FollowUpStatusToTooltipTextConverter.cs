using System;
using System.Windows.Data;
using ContactTracing.Core;

namespace ContactTracing.CaseView.Converters
{
    public sealed class FollowUpStatusToTooltipTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return "Unknown";

            ContactDailyStatus status = (ContactDailyStatus)value;

            switch (status)
            {
                case ContactDailyStatus.Dead:
                    return "Dead";
                case ContactDailyStatus.NotRecorded:
                    return "Status not recorded";
                case ContactDailyStatus.NotSeen:
                    return "Not seen";
                case ContactDailyStatus.SeenNotSick:
                    return "Seen and not sick";
                case ContactDailyStatus.SeenSickAndIsolated:
                    return "Seen and sick, isolated";
                case ContactDailyStatus.SeenSickAndIsoNotFilledOut:
                    return "Seen and sick, isolation unknown";
                case ContactDailyStatus.SeenSickAndNotIsolated:
                    return "Seen and sick, not isolated";
                case ContactDailyStatus.Unknown:
                    return "Unknown";
            }

            return String.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
