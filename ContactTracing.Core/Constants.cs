using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ContactTracing.Core
{
    public static class Constants
    {
        public const string CASE_FORM_NAME = "CaseInformationForm";
        public const string LAB_FORM_NAME = "LaboratoryResultsForm";
        public const string CONTACT_FORM_NAME = "ContactEntryForm";

        public const string LAB_CASE_FORM_NAME = "CaseInformationForm";
        public const string LAB_RESULTS_FORM_NAME = "LaboratoryResultsForm";

        public const string LAST_CONTACT_DATE_COLUMN_NAME = "LastContactDate";
        public const string FIRST_EXPOSURE_DATE_COLUMN_NAME = "FirstExposureDate";
        public const string LAST_EXPOSURE_DATE_COLUMN_NAME = "LastExposureDate";

        public const string FIELD_LAB_SPEC_COLUMN_NAME = "FieldLabSpecID";

        public const string AUTH_CODE = "2468";
        public const string SUPER_USER_CODE = "85017";

        public const double TRANS_CHAIN_SCALE = 10;

        public const double A4_PAGE_LENGTH = 1122;
        public const double A4_PAGE_WIDTH = 793;

        public const short DEFAULT_POLL_RATE = 333;
        public const short DISCONNECT_POLL_RATE = 700;

        public const int EXPORT_FIELD_LIMIT = 180;
    }
}
