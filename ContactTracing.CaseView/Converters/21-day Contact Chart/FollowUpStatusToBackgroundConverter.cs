using System;
using System.Windows.Data;
using System.Windows.Media;

namespace ContactTracing.CaseView.Converters
{
    public sealed class FollowUpStatusToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return Brushes.White;

            Core.ContactDailyStatus status = (Core.ContactDailyStatus)value;

            if (status == Core.ContactDailyStatus.NotSeen)
            {
                return System.Windows.Application.Current.FindResource("HatchBrush") as VisualBrush;
            }
            else if (status == Core.ContactDailyStatus.SeenSickAndNotIsolated || status == Core.ContactDailyStatus.SeenSickAndIsoNotFilledOut)
            {
                return Brushes.Gold;
            }
            else if (status == Core.ContactDailyStatus.NotRecorded)
            {
                return Brushes.White;
            }
            else if ((status == Core.ContactDailyStatus.SeenNotSick || status == Core.ContactDailyStatus.SeenSickAndNotIsolated || status == Core.ContactDailyStatus.SeenSickAndIsoNotFilledOut))
            {
                return new SolidColorBrush(Color.FromRgb(45, 166, 81)); // Colors.ForestGreen);
            }
            else
            {
                return Brushes.Tomato;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
