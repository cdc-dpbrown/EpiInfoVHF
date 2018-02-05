using System;
using System.Windows.Data;

namespace ContactTracing.Core.Converters
{
    public sealed class MultiBooleanToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values,
                            Type targetType,
                            object parameter,
                            System.Globalization.CultureInfo culture)
        {
            bool visible = false;

            if (values.Length == 2)
            {
                if ((bool)values[0] == true && (bool)values[1] == false)
                {
                    visible = true;
                }
            }

            if (visible)
                return System.Windows.Visibility.Visible;
            else
                return System.Windows.Visibility.Hidden;
        }

        public object[] ConvertBack(object value,
                                    Type[] targetTypes,
                                    object parameter,
                                    System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
