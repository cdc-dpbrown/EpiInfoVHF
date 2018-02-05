using System;
using System.Windows.Data;

namespace ContactTracing.CaseView.Converters
{
    public class PCRToEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if(value == null)
            {
                return false;
            }

            string textValue = value.ToString();

            switch (textValue)
            {
                case "1":
                case "3":
                    return true;
                default:
                    return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EbolaToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return false;
            }

            Core.Enums.VirusTestTypes virus = (Core.Enums.VirusTestTypes)(value);
            switch (virus)
            {
                case Core.Enums.VirusTestTypes.Ebola:
                    return System.Windows.Visibility.Visible;
                default:
                    return System.Windows.Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MarburgToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return false;
            }

            Core.Enums.VirusTestTypes virus = (Core.Enums.VirusTestTypes)(value);
            switch (virus)
            {
                case Core.Enums.VirusTestTypes.Marburg:
                    return System.Windows.Visibility.Visible;
                default:
                    return System.Windows.Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
