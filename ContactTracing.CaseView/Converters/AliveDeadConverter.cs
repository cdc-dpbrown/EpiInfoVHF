using System;
using System.Windows.Data;

namespace ContactTracing.CaseView.Converters
{
    public class AliveDeadConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool passThroughOnFail = false;
            if (parameter != null && parameter is bool)
            {
                passThroughOnFail = (bool)parameter;
            }

            Core.Enums.AliveDead aliveDead = (Core.Enums.AliveDead)(value);
            string textRepresentation = String.Empty;
            switch (aliveDead)
            {
                case Core.Enums.AliveDead.Alive:
                    textRepresentation = Properties.Resources.Alive; // "Alive";
                    break;
                case Core.Enums.AliveDead.Dead:
                    textRepresentation = Properties.Resources.Dead; // "Dead";
                    break;
                case Core.Enums.AliveDead.None:
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
            Core.Enums.AliveDead gender = Core.Enums.AliveDead.None;
            if (text.Equals(Properties.Resources.Alive))
            {
                gender = Core.Enums.AliveDead.Alive;
            }
            else if (text.Equals(Properties.Resources.Dead))
            {
                gender = Core.Enums.AliveDead.Dead;
            }
            else
            {
                gender = Core.Enums.AliveDead.None;
            }

            return gender;
        }
    }
}
