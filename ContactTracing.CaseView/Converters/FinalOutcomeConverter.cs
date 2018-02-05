using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ContactTracing.CaseView.Converters
{
        public class FinalOutcomeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool passThroughOnFail = false;
            if (parameter != null && parameter is bool)
            {
                passThroughOnFail = (bool)parameter;
            }
            string returnValue = string.Empty;
                switch (value.ToString())
                {
                    case "1":
                        returnValue = Properties.Resources.AnalysisDischargedFollowUp; // "Discharged from follow-up";
                        break;
                    case "2":
                        returnValue = Properties.Resources.AnalysisDevelopedSymptomsIso; // "Developed symptoms & isolated";
                        break;
                    case "3":
                        returnValue = Properties.Resources.AnalysisDroppedFollowUp; // "Dropped from follow-up";
                        break;
                    default:
                        returnValue = "";
                        break;
                }
                return returnValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
