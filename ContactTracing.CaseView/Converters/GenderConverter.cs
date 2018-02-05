using System;
using System.Windows.Data;

namespace ContactTracing.CaseView.Converters
{
    public class GenderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool passThroughOnFail = false;
            if (parameter != null && parameter is bool)
            {
                passThroughOnFail = (bool)parameter;
            }

            Core.Enums.Gender gender = (Core.Enums.Gender)(value);
            string textRepresentation = String.Empty;
            switch (gender)
            {
                case Core.Enums.Gender.Male:
                    textRepresentation = Properties.Resources.Male; // "Confirmed";
                    break;
                case Core.Enums.Gender.Female:
                    textRepresentation = Properties.Resources.Female; // "Probable";
                    break;
                case Core.Enums.Gender.None:
                    textRepresentation = String.Empty;
                    break;
                default:
                    if (passThroughOnFail)
                    {
                        textRepresentation = value.ToString();
                    }
                    else
                    {
                        textRepresentation = String.Empty;
                    }
                    break;
            }

            return textRepresentation;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string text = value as string;
            Core.Enums.Gender gender = Core.Enums.Gender.None;
            if (text.Equals(Properties.Resources.Male))
            {
                gender = Core.Enums.Gender.Male;
            }
            else if (text.Equals(Properties.Resources.Female))
            {
                gender = Core.Enums.Gender.Female;
            }
            else
            {
                gender = Core.Enums.Gender.None;
            }

            return gender;
        }
    }
}
