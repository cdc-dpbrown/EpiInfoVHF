using System;
using System.Windows.Data;

namespace ContactTracing.CaseView.Converters
{
    public class EpiCaseClassificationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool passThroughOnFail = false;
            if (parameter != null && parameter is bool)
            {
                passThroughOnFail = (bool)parameter;
            }

            Core.Enums.EpiCaseClassification caseClassification = (Core.Enums.EpiCaseClassification)(value);
            string textRepresentation = String.Empty;
            switch (caseClassification)
            {
                case Core.Enums.EpiCaseClassification.Confirmed:
                    textRepresentation = Properties.Resources.Confirmed; // "Confirmed";
                    break;
                case Core.Enums.EpiCaseClassification.Probable:
                    textRepresentation = Properties.Resources.Probable; // "Probable";
                    break;
                case Core.Enums.EpiCaseClassification.Suspect:
                    textRepresentation = Properties.Resources.Suspect; // "Suspect";
                    break;
                case Core.Enums.EpiCaseClassification.Excluded:
                    textRepresentation = Properties.Resources.Excluded; // "Excluded";
                    break;
                case Core.Enums.EpiCaseClassification.NotCase:
                    textRepresentation = Properties.Resources.NotCase; // "Not a case";
                    break;
                case Core.Enums.EpiCaseClassification.PUI:
                    textRepresentation = Properties.Resources.PUI; // "Person Under Investigation";
                    break;
                case Core.Enums.EpiCaseClassification.None:
                    textRepresentation = String.Empty; // "missing case def";
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
            Core.Enums.EpiCaseClassification caseClassification = Core.Enums.EpiCaseClassification.None;
            if (text.Equals(Properties.Resources.Confirmed))
            {
                caseClassification = Core.Enums.EpiCaseClassification.Confirmed;
            }
            else if (text.Equals(Properties.Resources.Probable))
            {
                caseClassification = Core.Enums.EpiCaseClassification.Probable;
            }
            else if (text.Equals(Properties.Resources.Suspect))
            {
                caseClassification = Core.Enums.EpiCaseClassification.Suspect;
            }
            else if (text.Equals(Properties.Resources.Excluded))
            {
                caseClassification = Core.Enums.EpiCaseClassification.Excluded;
            }
            else if (text.Equals(Properties.Resources.NotCase))
            {
                caseClassification = Core.Enums.EpiCaseClassification.NotCase;
            }
            else
            {
                caseClassification = Core.Enums.EpiCaseClassification.None;
            }

            return caseClassification;
        }
    }
}
