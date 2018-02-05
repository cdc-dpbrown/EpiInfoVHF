using System;
using System.Windows.Data;

namespace ContactTracing.CaseView.Converters
{
    /// <summary>
    /// DateConverter is used for Formatting the date in correct format based on Application Culture.
    /// </summary>
    public class DateConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string textRepresentation = String.Empty;

            if (value[0] == null || value[0].ToString().Contains("UnsetValue") || value[1].ToString().Contains("UnsetValue"))
            {
                return textRepresentation;
            }

            Core.Enums.Cultures AppCulture = (Core.Enums.Cultures)Enum.Parse(typeof(Core.Enums.Cultures), value[1].ToString().Replace("-", ""));

            //DateTime dateValue = (DateTime)(value[0].ToString());

            DateTime dateValue = DateTime.Parse(value[0].ToString());

            switch (AppCulture.ToString().ToLower())
            {
                case "en":
                    textRepresentation = dateValue.ToString("MM/dd/yyyy");
                    break;
                default:
                    textRepresentation = dateValue.ToString("dd/MM/yyyy");
                    break;
            }


            return textRepresentation;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string text = value as string;
            return null;
        }

        public string GetDateFormat(string culture)
        {
            Core.Enums.Cultures AppCulture = (Core.Enums.Cultures)Enum.Parse(typeof(Core.Enums.Cultures), culture.ToString().Replace("-", ""));

            switch (AppCulture)
            {
                case ContactTracing.Core.Enums.Cultures.en:
                    return "MM/dd/yyyy";
                case ContactTracing.Core.Enums.Cultures.fr:
                case ContactTracing.Core.Enums.Cultures.enUS:
                case ContactTracing.Core.Enums.Cultures.frFR:
                default:
                    return "dd/MM/yyyy";
            }
        }
    }
}
