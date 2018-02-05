using System;
using System.Windows.Data;

namespace ContactTracing.CaseView.Converters
{
    public sealed class MultiBooleanCaseWindowToEnabledConverter : IMultiValueConverter
    {
        public object Convert(object[] values,
                            Type targetType,
                            object parameter,
                            System.Globalization.CultureInfo culture)
        {
            bool enabled = true;

            if (values.Length == 2)
            {
                if ((bool)values[0] == true && (bool)values[1] == false)
                {
                    enabled = false;
                }
            }

            return enabled;
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
