using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContactTracing.ViewModel
{
    public class LabResult
    {
        public string Surname { get; set; }
        public string OtherNames { get; set; }
        public string CaseID { get; set; }
        public string LabCaseID { get; set; }
        public string CaseRecordGuid { get; set; }
        public string FieldLabSpecimenID { get; set; }
        public string UVRIVSPBLogNumber { get; set; }
        public string Village { get; set; }
        public string District { get; set; }
        public string SampleType { get; set; }
        public string SampleTypeLocalized { get; set; }
        public DateTime? DateOnset { get; set; }
        public DateTime? DateSampleCollected { get; set; }
        public DateTime? DateSampleTested { get; set; }
        public DateTime? DateDeath { get; set; }
        public int? DaysAcute { get; set; }
        public string FinalLabClassification { get; set; }
        public string SampleInterpretation { get; set; }
        public string SampleInterpret { get; set; }
        public string SampleOtherType { get; set; }
        public string MalariaRapidTest { get; set; }
        public string MRT { get; set; }
        public int UniqueKey { get; set; }
        public int ResultNumber { get; set; }

        public string RecordId { get; set; }

        public string SUDVPCR { get; set; }
        public string SUDVPCR2 { get; set; }
        public string SUDVAg { get; set; }
        public string SUDVIgM { get; set; }
        public string SUDVIgG { get; set; }

        public string BDBVPCR { get; set; }
        public string BDBVPCR2 { get; set; }
        public string BDBVAg { get; set; }
        public string BDBVIgM { get; set; }
        public string BDBVIgG { get; set; }

        public string EBOVPCR { get; set; }
        public string EBOVPCR2 { get; set; }
        public string EBOVAg { get; set; }
        public string EBOVIgM { get; set; }
        public string EBOVIgG { get; set; }

        public double? EBOVCT1 { get; set; }
        public double? EBOVCT2 { get; set; }
        public string EBOVAgTiter { get; set; }
        public string EBOVIgMTiter { get; set; }
        public string EBOVIgGTiter { get; set; }

        public double? EBOVAgSumOD { get; set; }
        public double? EBOVIgMSumOD { get; set; }
        public double? EBOVIgGSumOD { get; set; }

        public string MARVPCR { get; set; }
        public string MARVPCR2 { get; set; }
        public string MARVAg { get; set; }
        public string MARVIgM { get; set; }
        public string MARVIgG { get; set; }

        public double? MARVCT1 { get; set; }
        public double? MARVCT2 { get; set; }
        public string MARVAgTiter { get; set; }
        public string MARVIgMTiter { get; set; }
        public string MARVIgGTiter { get; set; }

        public double? MARVAgSumOD { get; set; }
        public double? MARVIgMSumOD { get; set; }
        public double? MARVIgGSumOD { get; set; }

        public string CCHFPCR { get; set; }
        public string CCHFPCR2 { get; set; }
        public string CCHFAg { get; set; }
        public string CCHFIgM { get; set; }
        public string CCHFIgG { get; set; }

        public string RVFPCR { get; set; }
        public string RVFPCR2 { get; set; }
        public string RVFAg { get; set; }
        public string RVFIgM { get; set; }
        public string RVFIgG { get; set; }

        public string LHFPCR { get; set; }
        public string LHFPCR2 { get; set; }
        public string LHFAg { get; set; }
        public string LHFIgM { get; set; }
        public string LHFIgG { get; set; }

        public string LabSampleTest { get; set; }
        public string FacilityLabSubmit { get; set; }
        public string PersonLabSubmit { get; set; }
        public string PhoneLabSubmit { get; set; }
        public string EmailLabSubmit { get; set; }

        public LabResult()
        {
            Surname = String.Empty;
            OtherNames = String.Empty;
            CaseID = String.Empty;
            LabCaseID = String.Empty;
            CaseRecordGuid = String.Empty;
            FieldLabSpecimenID = String.Empty;
            UVRIVSPBLogNumber = String.Empty;
            Village = String.Empty;
            District = String.Empty;
            SampleType = String.Empty;
            SampleOtherType = String.Empty;
            FinalLabClassification = String.Empty;
            SampleInterpretation = String.Empty;
            SampleInterpret = String.Empty;
            MalariaRapidTest = String.Empty;

            SUDVPCR = String.Empty;
            SUDVPCR2 = String.Empty;
            SUDVAg = String.Empty;
            SUDVIgM = String.Empty;
            SUDVIgG = String.Empty;

            BDBVPCR = String.Empty;
            BDBVPCR2 = String.Empty;
            BDBVAg = String.Empty;
            BDBVIgM = String.Empty;
            BDBVIgG = String.Empty;

            EBOVPCR = String.Empty;
            EBOVPCR2 = String.Empty;
            EBOVAg = String.Empty;
            EBOVIgM = String.Empty;
            EBOVIgG = String.Empty;

            EBOVCT1 = null;
            EBOVCT2 = null;
            EBOVAgTiter = String.Empty;
            EBOVIgMTiter = String.Empty;
            EBOVIgGTiter = String.Empty;

            EBOVAgSumOD = null;
            EBOVIgMSumOD = null;
            EBOVIgGSumOD = null;

            MARVPCR = String.Empty;
            MARVPCR2 = String.Empty;
            MARVAg = String.Empty;
            MARVIgM = String.Empty;
            MARVIgG = String.Empty;

            MARVCT1 = null;
            MARVCT2 = null;
            MARVAgTiter = String.Empty;
            MARVIgMTiter = String.Empty;
            MARVIgGTiter = String.Empty;

            MARVAgSumOD = null;
            MARVIgMSumOD = null;
            MARVIgGSumOD = null;

            CCHFPCR = String.Empty;
            CCHFPCR2 = String.Empty;
            CCHFAg = String.Empty;
            CCHFIgM = String.Empty;
            CCHFIgG = String.Empty;

            RVFPCR = String.Empty;
            RVFPCR2 = String.Empty;
            RVFAg = String.Empty;
            RVFIgM = String.Empty;
            RVFIgG = String.Empty;

            LHFPCR = String.Empty;
            LHFPCR2 = String.Empty;
            LHFAg = String.Empty;
            LHFIgM = String.Empty;
            LHFIgG = String.Empty;
            LabSampleTest = String.Empty;
            FacilityLabSubmit = String.Empty;
            PersonLabSubmit = String.Empty;
            PhoneLabSubmit = String.Empty;
            EmailLabSubmit = String.Empty;
        }
    }
}
