using System;
using System.Windows.Data;

namespace ContactTracing.CaseView.Converters
{
    public class FinalLabClassificationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool passThroughOnFail = false;
            if (parameter != null && parameter is bool)
            {
                passThroughOnFail = (bool)parameter;
            }

            Core.Enums.FinalLabClassification labClassification = (Core.Enums.FinalLabClassification)(value);
            string textRepresentation = String.Empty;
            switch (labClassification)
            {
                case Core.Enums.FinalLabClassification.ConfirmedAcute:
                    textRepresentation = Properties.Resources.SampleInterpretationConfirmedAcute; // "Confirmed";
                    break;
                case Core.Enums.FinalLabClassification.ConfirmedConvalescent:
                    textRepresentation = Properties.Resources.SampleInterpretationConfirmedConvalescent; // "Probable";
                    break;
                case Core.Enums.FinalLabClassification.Indeterminate:
                    textRepresentation = Properties.Resources.SampleInterpretationIndeterminate; // "Suspect";
                    break;
                case Core.Enums.FinalLabClassification.NeedsFollowUpSample:
                    textRepresentation = Properties.Resources.AnalysisClassNeedsFollowUp; // "Excluded";
                    break;
                case Core.Enums.FinalLabClassification.NotCase:
                    textRepresentation = Properties.Resources.NotCase; // "Not a case";
                    break;
                case Core.Enums.FinalLabClassification.None:
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
            Core.Enums.FinalLabClassification labClassification = Core.Enums.FinalLabClassification.None;
            if (text.Equals(Properties.Resources.SampleInterpretationConfirmedAcute))
            {
                labClassification = Core.Enums.FinalLabClassification.ConfirmedAcute;
            }
            else if (text.Equals(Properties.Resources.SampleInterpretationConfirmedConvalescent))
            {
                labClassification = Core.Enums.FinalLabClassification.ConfirmedConvalescent;
            }
            else if (text.Equals(Properties.Resources.SampleInterpretationIndeterminate))
            {
                labClassification = Core.Enums.FinalLabClassification.Indeterminate;
            }
            else if (text.Equals(Properties.Resources.AnalysisClassNeedsFollowUp))
            {
                labClassification = Core.Enums.FinalLabClassification.NeedsFollowUpSample;
            }
            else if (text.Equals(Properties.Resources.NotCase))
            {
                labClassification = Core.Enums.FinalLabClassification.NotCase;
            }
            else
            {
                labClassification = Core.Enums.FinalLabClassification.None;
            }

            return labClassification;
        }
    }
}
