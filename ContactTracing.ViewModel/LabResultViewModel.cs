using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Input;
using ContactTracing.Core;
using ContactTracing.Core.Collections;
using ContactTracing.ViewModel.Collections;
using ContactTracing.ViewModel.Events;
using Epi.Data;
using Epi.Fields;

namespace ContactTracing.ViewModel
{
    public class LabResultViewModel : ObservableObject, INotifyDataErrorInfo
    {
        #region Members
        private RecordErrorDictionary _errors = new RecordErrorDictionary();

        private object _validationLock = new object();
        private bool _isEditing = false;
        private bool _isShowingErrorDetailPanel = false;
        private bool _isNewRecord = false;
        private object _errorsLock = new object();
        private string _sampleNumber = String.Empty;
        private LabResult _labResult;
        private CaseViewModel _caseViewModel;
        private string _gender = String.Empty;
        private double? _age = null;
        public static bool IsCountryUS = false;
        #endregion // Members

        #region Events
        [field: NonSerialized]
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        [field: NonSerialized]
        public event EventHandler MarkedForRemoval;
        #endregion // Events

        #region Properties

        private bool IsValidating { get; set; }

        public bool IsShowingErrorDetailPanel
        {
            get
            {
                return _isShowingErrorDetailPanel;
            }
            set
            {
                _isShowingErrorDetailPanel = value;
                RaisePropertyChanged("IsShowingErrorDetailPanel");
            }
        }

        public bool IsEditing
        {
            get
            {
                return _isEditing;
            }
            set
            {
                if (_isEditing != value)
                {
                    _isEditing = value;
                    RaisePropertyChanged("IsEditing");

                    if (_isEditing == true)
                    {
                        SuppressValidation = false;
                        Validate();
                    }
                    else
                    {
                        SuppressValidation = true;
                    }
                }
            }
        }

        public bool SuppressValidation { get; private set; }

        public RecordErrorDictionary Errors { get { return this._errors; } }

        public bool IsNewRecord
        {
            get
            {
                return _isNewRecord;
            }
            set
            {
                if (_isNewRecord != value)
                {
                    _isNewRecord = value;
                    RaisePropertyChanged("IsNewRecord");
                }
            }
        }

        /// <summary>
        /// Gets whether the record currently has any errors
        /// </summary>
        public virtual bool HasErrors
        {
            get
            {
                if (SuppressValidation)
                {
                    return false;
                }
                else
                {
                    return Errors.Any(kv => kv.Value != null && kv.Value.Count > 0);
                }
            }
        }

        public string SampleNumber
        {
            get
            {
                return _sampleNumber;
            }
            set
            {
                _sampleNumber = value;
                RaisePropertyChanged("SampleNumber");
            }
        }

        public string Gender
        {
            get
            {
                return this._gender;
            }
            set
            {
                if (!Gender.Equals(value))
                {
                    this._gender = value;
                    RaisePropertyChanged("Gender");
                }
            }
        }

        public double? Age
        {
            get
            {
                return this._age;
            }
            set
            {
                if (!Age.Equals(value))
                {
                    this._age = value;
                    RaisePropertyChanged("Age");
                }
            }
        }
        private LabResult LabResult
        {
            get
            {
                return this._labResult;
            }
        }
        public CaseViewModel CaseVM
        {
            get
            {
                return this._caseViewModel;
            }
            set
            {
                this._caseViewModel = value;
            }
        }
        public string Surname
        {
            get
            {
                return LabResult.Surname;
            }
            set
            {
                if (LabResult.Surname != value)
                {
                    LabResult.Surname = value; RaisePropertyChanged("Surname");
                }
            }
        }

        public string RecordId { get { return LabResult.RecordId; } set { if (LabResult.RecordId != value) { LabResult.RecordId = value; RaisePropertyChanged("RecordId"); } } }
        public string OtherNames { get { return LabResult.OtherNames; } set { if (LabResult.OtherNames != value) { LabResult.OtherNames = value; RaisePropertyChanged("OtherNames"); } } }
        public string CaseID { get { return LabResult.CaseID; } set { if (LabResult.CaseID != value) { LabResult.CaseID = value; RaisePropertyChanged("CaseID"); } } }
        public string LabCaseID { get { return LabResult.LabCaseID; } set { if (LabResult.LabCaseID != value) { LabResult.LabCaseID = value; RaisePropertyChanged("LabCaseID"); } } }
        public string CaseRecordGuid { get { return LabResult.CaseRecordGuid; } set { if (LabResult.CaseRecordGuid != value) { LabResult.CaseRecordGuid = value; RaisePropertyChanged("CaseRecordGuid"); } } }
        public int ResultNumber { get { return LabResult.ResultNumber; } set { if (LabResult.ResultNumber != value) { LabResult.ResultNumber = value; RaisePropertyChanged("ResultNumber"); } } }

        public string FieldLabSpecimenID { get { return LabResult.FieldLabSpecimenID; } set { if (LabResult.FieldLabSpecimenID != value) { LabResult.FieldLabSpecimenID = value; RaisePropertyChanged("FieldLabSpecimenID"); } } }
        public string UVRIVSPBLogNumber { get { return LabResult.UVRIVSPBLogNumber; } set { if (LabResult.UVRIVSPBLogNumber != value) { LabResult.UVRIVSPBLogNumber = value; RaisePropertyChanged("UVRIVSPBLogNumber"); } } }
        public string Village { get { return LabResult.Village; } set { if (LabResult.Village != value) { LabResult.Village = value; RaisePropertyChanged("Village"); } } }
        public string District { get { return LabResult.District; } set { if (LabResult.District != value) { LabResult.District = value; RaisePropertyChanged("District"); } } }
        public string SampleType { get { return LabResult.SampleType; } set { if (LabResult.SampleType != value) { LabResult.SampleType = value; RaisePropertyChanged("SampleType"); } } }
        public string SampleTypeLocalized { get { return LabResult.SampleTypeLocalized; } set { if (LabResult.SampleTypeLocalized != value) { LabResult.SampleTypeLocalized = value; RaisePropertyChanged("SampleTypeLocalized"); } } }
        public DateTime? DateOnset { get { return LabResult.DateOnset; } set { if (LabResult.DateOnset != value) { LabResult.DateOnset = value; RaisePropertyChanged("DateOnset"); } } }
        public DateTime? DateSampleCollected { get { return LabResult.DateSampleCollected; } set { if (LabResult.DateSampleCollected != value) { LabResult.DateSampleCollected = value; RaisePropertyChanged("DateSampleCollected"); } } }
        public DateTime? DateSampleTested { get { return LabResult.DateSampleTested; } set { if (LabResult.DateSampleTested != value) { LabResult.DateSampleTested = value; RaisePropertyChanged("DateSampleTested"); } } }
        public DateTime? DateDeath { get { return LabResult.DateDeath; } set { if (LabResult.DateDeath != value) { LabResult.DateDeath = value; RaisePropertyChanged("DateDeath"); } } }
        public int? DaysAcute { get { return LabResult.DaysAcute; } set { if (LabResult.DaysAcute != value) { LabResult.DaysAcute = value; RaisePropertyChanged("DaysAcute"); } } }
        public string FinalLabClassification { get { return LabResult.FinalLabClassification; } set { if (LabResult.FinalLabClassification != value) { LabResult.FinalLabClassification = value; RaisePropertyChanged("FinalLabClassification"); } } }
        public string SampleInterpretation { get { return LabResult.SampleInterpretation; } set { if (LabResult.SampleInterpretation != value) { LabResult.SampleInterpretation = value; RaisePropertyChanged("SampleInterpretation"); } } }
        public string SampleInterpret { get { return LabResult.SampleInterpret; } set { if (LabResult.SampleInterpret != value) { LabResult.SampleInterpret = value; RaisePropertyChanged("SampleInterpret"); } } }
        public string SampleOtherType { get { return LabResult.SampleOtherType; } set { if (LabResult.SampleOtherType != value) { LabResult.SampleOtherType = value; RaisePropertyChanged("SampleOtherType"); } } }
        public string MalariaRapidTest { get { return LabResult.MalariaRapidTest; } set { if (LabResult.MalariaRapidTest != value) { LabResult.MalariaRapidTest = value; RaisePropertyChanged("MalariaRapidTest"); } } }
        public string MRT { get { return LabResult.MRT; } set { if (LabResult.MRT != value) { LabResult.MRT = value; RaisePropertyChanged("MRT"); } } }
        public int UniqueKey { get { return LabResult.UniqueKey; } set { if (LabResult.UniqueKey != value) { LabResult.UniqueKey = value; RaisePropertyChanged("UniqueKey"); } } }

        public string SUDVPCR { get { return LabResult.SUDVPCR; } set { if (LabResult.SUDVPCR != value) { LabResult.SUDVPCR = value; RaisePropertyChanged("SUDVPCR"); } } }
        public string SUDVPCR2 { get { return LabResult.SUDVPCR2; } set { if (LabResult.SUDVPCR2 != value) { LabResult.SUDVPCR2 = value; RaisePropertyChanged("SUDVPCR2"); } } }
        public string SUDVAg { get { return LabResult.SUDVAg; } set { if (LabResult.SUDVAg != value) { LabResult.SUDVAg = value; RaisePropertyChanged("SUDVAg"); } } }
        public string SUDVIgM { get { return LabResult.SUDVIgM; } set { if (LabResult.SUDVIgM != value) { LabResult.SUDVIgM = value; RaisePropertyChanged("SUDVIgM"); } } }
        public string SUDVIgG { get { return LabResult.SUDVIgG; } set { if (LabResult.SUDVIgG != value) { LabResult.SUDVIgG = value; RaisePropertyChanged("SUDVIgG"); } } }

        public string BDBVPCR { get { return LabResult.BDBVPCR; } set { if (LabResult.BDBVPCR != value) { LabResult.BDBVPCR = value; RaisePropertyChanged("BDBVPCR"); } } }
        public string BDBVPCR2 { get { return LabResult.BDBVPCR2; } set { if (LabResult.BDBVPCR2 != value) { LabResult.BDBVPCR2 = value; RaisePropertyChanged("BDBVPCR2"); } } }
        public string BDBVAg { get { return LabResult.BDBVAg; } set { if (LabResult.BDBVAg != value) { LabResult.BDBVAg = value; RaisePropertyChanged("BDBVAg"); } } }
        public string BDBVIgM { get { return LabResult.BDBVIgM; } set { if (LabResult.BDBVIgM != value) { LabResult.BDBVIgM = value; RaisePropertyChanged("BDBVIgM"); } } }
        public string BDBVIgG { get { return LabResult.BDBVIgG; } set { if (LabResult.BDBVIgG != value) { LabResult.BDBVIgG = value; RaisePropertyChanged("BDBVIgG"); } } }

        public string EBOVPCR { get { return LabResult.EBOVPCR; } set { if (LabResult.EBOVPCR != value) { LabResult.EBOVPCR = value; RaisePropertyChanged("EBOVPCR"); } } }
        public string EBOVPCR2 { get { return LabResult.EBOVPCR2; } set { if (LabResult.EBOVPCR2 != value) { LabResult.EBOVPCR2 = value; RaisePropertyChanged("EBOVPCR2"); } } }
        public string EBOVAg { get { return LabResult.EBOVAg; } set { if (LabResult.EBOVAg != value) { LabResult.EBOVAg = value; RaisePropertyChanged("EBOVAg"); } } }
        public string EBOVIgM { get { return LabResult.EBOVIgM; } set { if (LabResult.EBOVIgM != value) { LabResult.EBOVIgM = value; RaisePropertyChanged("EBOVIgM"); } } }
        public string EBOVIgG { get { return LabResult.EBOVIgG; } set { if (LabResult.EBOVIgG != value) { LabResult.EBOVIgG = value; RaisePropertyChanged("EBOVIgG"); } } }

        public double? EBOVCT1 { get { return LabResult.EBOVCT1; } set { if (LabResult.EBOVCT1 != value) { LabResult.EBOVCT1 = value; RaisePropertyChanged("EBOVCT1"); } } }
        public double? EBOVCT2 { get { return LabResult.EBOVCT2; } set { if (LabResult.EBOVCT2 != value) { LabResult.EBOVCT2 = value; RaisePropertyChanged("EBOVCT2"); } } }
        public string EBOVAgTiter { get { return LabResult.EBOVAgTiter; } set { if (LabResult.EBOVAgTiter != value) { LabResult.EBOVAgTiter = value; RaisePropertyChanged("EBOVAgTiter"); } } }
        public string EBOVIgMTiter { get { return LabResult.EBOVIgMTiter; } set { if (LabResult.EBOVIgMTiter != value) { LabResult.EBOVIgMTiter = value; RaisePropertyChanged("EBOVIgMTiter"); } } }
        public string EBOVIgGTiter { get { return LabResult.EBOVIgGTiter; } set { if (LabResult.EBOVIgGTiter != value) { LabResult.EBOVIgGTiter = value; RaisePropertyChanged("EBOVIgGTiter"); } } }

        public double? EBOVAgSumOD { get { return LabResult.EBOVAgSumOD; } set { if (LabResult.EBOVAgSumOD != value) { LabResult.EBOVAgSumOD = value; RaisePropertyChanged("EBOVAgSumOD"); } } }
        public double? EBOVIgMSumOD { get { return LabResult.EBOVIgMSumOD; } set { if (LabResult.EBOVIgMSumOD != value) { LabResult.EBOVIgMSumOD = value; RaisePropertyChanged("EBOVIgMSumOD"); } } }
        public double? EBOVIgGSumOD { get { return LabResult.EBOVIgGSumOD; } set { if (LabResult.EBOVIgGSumOD != value) { LabResult.EBOVIgGSumOD = value; RaisePropertyChanged("EBOVIgGSumOD"); } } }

        public string MARVPCR { get { return LabResult.MARVPCR; } set { if (LabResult.MARVPCR != value) { LabResult.MARVPCR = value; RaisePropertyChanged("MARVPCR"); } } }
        public string MARVPCR2 { get { return LabResult.MARVPCR2; } set { if (LabResult.MARVPCR2 != value) { LabResult.MARVPCR2 = value; RaisePropertyChanged("MARVPCR2"); } } }
        public string MARVAg { get { return LabResult.MARVAg; } set { if (LabResult.MARVAg != value) { LabResult.MARVAg = value; RaisePropertyChanged("MARVAg"); } } }
        public string MARVIgM { get { return LabResult.MARVIgM; } set { if (LabResult.MARVIgM != value) { LabResult.MARVIgM = value; RaisePropertyChanged("MARVIgM"); } } }
        public string MARVIgG { get { return LabResult.MARVIgG; } set { if (LabResult.MARVIgG != value) { LabResult.MARVIgG = value; RaisePropertyChanged("MARVIgG"); } } }

        public double? MARVCT1 { get { return LabResult.MARVCT1; } set { if (LabResult.MARVCT1 != value) { LabResult.MARVCT1 = value; RaisePropertyChanged("MARVCT1"); } } }
        public double? MARVCT2 { get { return LabResult.MARVCT2; } set { if (LabResult.MARVCT2 != value) { LabResult.MARVCT2 = value; RaisePropertyChanged("MARVCT2"); } } }
        public string MARVAgTiter { get { return LabResult.MARVAgTiter; } set { if (LabResult.MARVAgTiter != value) { LabResult.MARVAgTiter = value; RaisePropertyChanged("MARVAgTiter"); } } }
        public string MARVIgMTiter { get { return LabResult.MARVIgMTiter; } set { if (LabResult.MARVIgMTiter != value) { LabResult.MARVIgMTiter = value; RaisePropertyChanged("MARVIgMTiter"); } } }
        public string MARVIgGTiter { get { return LabResult.MARVIgGTiter; } set { if (LabResult.MARVIgGTiter != value) { LabResult.MARVIgGTiter = value; RaisePropertyChanged("MARVIgGTiter"); } } }

        public double? MARVAgSumOD { get { return LabResult.MARVAgSumOD; } set { if (LabResult.MARVAgSumOD != value) { LabResult.MARVAgSumOD = value; RaisePropertyChanged("MARVAgSumOD"); } } }
        public double? MARVIgMSumOD { get { return LabResult.MARVIgMSumOD; } set { if (LabResult.MARVIgMSumOD != value) { LabResult.MARVIgMSumOD = value; RaisePropertyChanged("MARVIgMSumOD"); } } }
        public double? MARVIgGSumOD { get { return LabResult.MARVIgGSumOD; } set { if (LabResult.MARVIgGSumOD != value) { LabResult.MARVIgGSumOD = value; RaisePropertyChanged("MARVIgGSumOD"); } } }

        public string CCHFPCR { get { return LabResult.CCHFPCR; } set { if (LabResult.CCHFPCR != value) { LabResult.CCHFPCR = value; RaisePropertyChanged("CCHFPCR"); } } }
        public string CCHFPCR2 { get { return LabResult.CCHFPCR2; } set { if (LabResult.CCHFPCR2 != value) { LabResult.CCHFPCR2 = value; RaisePropertyChanged("CCHFPCR2"); } } }
        public string CCHFAg { get { return LabResult.CCHFAg; } set { if (LabResult.CCHFAg != value) { LabResult.CCHFAg = value; RaisePropertyChanged("CCHFAg"); } } }
        public string CCHFIgM { get { return LabResult.CCHFIgM; } set { if (LabResult.CCHFIgM != value) { LabResult.CCHFIgM = value; RaisePropertyChanged("CCHFIgM"); } } }
        public string CCHFIgG { get { return LabResult.CCHFIgG; } set { if (LabResult.CCHFIgG != value) { LabResult.CCHFIgG = value; RaisePropertyChanged("CCHFIgG"); } } }

        public string RVFPCR { get { return LabResult.RVFPCR; } set { if (LabResult.RVFPCR != value) { LabResult.RVFPCR = value; RaisePropertyChanged("RVFPCR"); } } }
        public string RVFPCR2 { get { return LabResult.RVFPCR2; } set { if (LabResult.RVFPCR2 != value) { LabResult.RVFPCR2 = value; RaisePropertyChanged("RVFPCR2"); } } }
        public string RVFAg { get { return LabResult.RVFAg; } set { if (LabResult.RVFAg != value) { LabResult.RVFAg = value; RaisePropertyChanged("RVFAg"); } } }
        public string RVFIgM { get { return LabResult.RVFIgM; } set { if (LabResult.RVFIgM != value) { LabResult.RVFIgM = value; RaisePropertyChanged("RVFIgM"); } } }
        public string RVFIgG { get { return LabResult.RVFIgG; } set { if (LabResult.RVFIgG != value) { LabResult.RVFIgG = value; RaisePropertyChanged("RVFIgG"); } } }

        public string LHFPCR { get { return LabResult.LHFPCR; } set { if (LabResult.LHFPCR != value) { LabResult.LHFPCR = value; RaisePropertyChanged("LHFPCR"); } } }
        public string LHFPCR2 { get { return LabResult.LHFPCR2; } set { if (LabResult.LHFPCR2 != value) { LabResult.LHFPCR2 = value; RaisePropertyChanged("LHFPCR2"); } } }
        public string LHFAg { get { return LabResult.LHFAg; } set { if (LabResult.LHFAg != value) { LabResult.LHFAg = value; RaisePropertyChanged("LHFAg"); } } }
        public string LHFIgM { get { return LabResult.LHFIgM; } set { if (LabResult.LHFIgM != value) { LabResult.LHFIgM = value; RaisePropertyChanged("LHFIgM"); } } }
        public string LHFIgG { get { return LabResult.LHFIgG; } set { if (LabResult.LHFIgG != value) { LabResult.LHFIgG = value; RaisePropertyChanged("LHFIgG"); } } }

        private Epi.View LabForm { get; set; }

        public string LabSampleTest
        {
            get
            {
                return LabResult.LabSampleTest;
            }
            set
            {
                if (LabResult.LabSampleTest != value)
                {
                    LabResult.LabSampleTest = value; RaisePropertyChanged("LabSampleTest");
                }
            }
        }
        public string FacilityLabSubmit
        {
            get
            {
                return LabResult.FacilityLabSubmit;
            }
            set
            {
                if (LabResult.FacilityLabSubmit != value)
                {
                    LabResult.FacilityLabSubmit = value; RaisePropertyChanged("FacilityLabSubmit");
                }
            }
        }
        public string PersonLabSubmit
        {
            get
            {
                return LabResult.PersonLabSubmit;
            }
            set
            {
                if (LabResult.PersonLabSubmit != value)
                {
                    LabResult.PersonLabSubmit = value; RaisePropertyChanged("PersonLabSubmit");
                }
            }
        }
        public string PhoneLabSubmit
        {
            get
            {
                return LabResult.PhoneLabSubmit;
            }
            set
            {
                if (LabResult.PhoneLabSubmit != value)
                {
                    LabResult.PhoneLabSubmit = value; RaisePropertyChanged("PhoneLabSubmit");
                }
            }
        }
        public string EmailLabSubmit
        {
            get
            {
                return LabResult.EmailLabSubmit;
            }
            set
            {
                if (LabResult.EmailLabSubmit != value)
                {
                    LabResult.EmailLabSubmit = value; RaisePropertyChanged("EmailLabSubmit");
                }
            }
        }
        #endregion // Properties

        #region Constructors
        public LabResultViewModel(Epi.View labForm)
        {
            _labResult = new LabResult();
            LabForm = labForm;
            IsEditing = false;
            SuppressValidation = true;
        }

        public LabResultViewModel(Epi.View labForm, LabResult labResult)
        {
            _labResult = labResult;
            LabForm = labForm;
            IsEditing = false;
            SuppressValidation = true;
        }
        #endregion // Constructors



        //public string Error
        //{
        //    get { return (this as IDataErrorInfo).Error; }
        //}
        public string this[string columnName]
        {
            get
            {
                String errorMessage = String.Empty;
                switch (columnName)
                {
                    case "DateSampleCollected":
                        if (DateSampleCollected.HasValue)
                        {
                            if (DateSampleCollected.Value > DateTime.Now)
                            {
                                errorMessage = "Date of sample collection cannot be greater than current date.";
                            }
                            else if (CaseVM.DateOnset.HasValue && DateSampleCollected.Value < CaseVM.DateOnset.Value)
                            {
                                errorMessage = String.Format("Date of sample collected must be equal or greater than date of onset ({0}). Please verify your date.", CaseVM.DateOnset.Value.ToShortDateString());
                            }
                            else if (DateSampleTested.HasValue && DateSampleTested.Value < DateSampleCollected.Value)
                            {
                                errorMessage = String.Format("Date sample collected must be equal or greater than date sample tested ({0}) .  Please verify your date.", DateSampleTested.Value.ToShortDateString());
                            }
                        }
                        break;
                    case "DateSampleTested":
                        if (DateSampleTested.HasValue && DateSampleCollected.HasValue)
                        {
                            if (DateSampleTested.Value < DateSampleCollected.Value)
                            {
                                errorMessage = String.Format("Date sample tested must be equal or greater than date sample collected ({0}).  Please verify your date.", DateSampleCollected.Value.ToShortDateString());
                            }
                        }
                        break;
                    case "SampleInterpret":
                        if (DaysAcute.HasValue && DaysAcute.Value < 3 && SampleInterpret == "3")
                        {
                            errorMessage = "The sample was collected less than 3 days after symptom onset, therefore it is necessary to select 'Negative, needs followup sample'";
                        }
                        break;
                    case "SampleType":
                        if (SampleType != "5")
                        {
                            SampleOtherType = String.Empty;
                        }
                        break;
                }

                if (CaseVM.DateOnset.HasValue && DateSampleCollected.HasValue)
                {
                    TimeSpan ts = DateSampleCollected.Value - CaseVM.DateOnset.Value;
                    DaysAcute = Convert.ToInt32(Math.Truncate(ts.TotalDays));
                }
                else
                {
                    DaysAcute = null;
                }

                if (SampleType != "5")
                {
                    SampleOtherType = String.Empty;
                }

                // Ebola logic
                if (EBOVPCR != "1" && EBOVPCR != "3")
                {
                    EBOVCT1 = null;
                }
                if (EBOVPCR2 != "1" && EBOVPCR2 != "3")
                {
                    EBOVCT2 = null;
                }
                if (EBOVAg != "1" && EBOVAg != "3")
                {
                    EBOVAgTiter = String.Empty;
                    EBOVAgSumOD = null;
                }
                if (EBOVIgM != "1" && EBOVIgM != "3")
                {
                    EBOVIgMTiter = String.Empty;
                    EBOVIgMSumOD = null;
                }
                if (EBOVIgG != "1" && EBOVIgG != "3")
                {
                    EBOVIgGTiter = String.Empty;
                    EBOVIgGSumOD = null;
                }

                // Marburg logic
                if (MARVPCR != "1" && MARVPCR != "3")
                {
                    MARVCT1 = null;
                }
                if (MARVPCR2 != "1" && MARVPCR2 != "3")
                {
                    MARVCT2 = null;
                }
                if (MARVAg != "1" && MARVAg != "3")
                {
                    MARVAgTiter = String.Empty;
                    MARVAgSumOD = null;
                }
                if (MARVIgM != "1" && MARVIgM != "3")
                {
                    MARVIgMTiter = String.Empty;
                    MARVIgMSumOD = null;
                }
                if (MARVIgG != "1" && MARVIgG != "3")
                {
                    MARVIgGTiter = String.Empty;
                    MARVIgGSumOD = null;
                }

                return errorMessage;
            }
        }

        public virtual void Validate()
        {
            if (SuppressValidation || IsValidating)
            {
                return;
            }

            IsValidating = true;

            lock (_validationLock)
            {
                Errors.Clear();

                List<string> fieldNames = new List<string>();

                var properties = typeof(LabResultViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var property in properties)
                {
                    if (!property.Name.Equals("HasErrors") &&
                        !property.Name.Equals("IsLocked") &&
                        !property.Name.Equals("IsActive") &&
                        !property.Name.Equals("IsContact") &&
                        !property.Name.Equals("IsEditing") &&
                        !property.Name.Equals("Errors") &&
                        !property.Name.Equals("IsShowingErrorDetailPanel") &&
                        !property.Name.Equals("SuppressValidation") &&
                        !property.Name.Equals("IsValidating") &&
                        !property.Name.Equals("IsSaving") &&
                        !property.Name.Equals("IsNewRecord") &&
                        !property.Name.EndsWith("Command") &&
                        !property.Name.Equals("IsLoading"))
                    {
                        fieldNames.Add(property.Name.ToString());
                    }
                }

                foreach (string fieldName in fieldNames)
                {
                    string message = this[fieldName].Trim();
                    if (!String.IsNullOrEmpty(message))
                    {
                        if (Errors.ContainsKey(fieldName))
                        {
                            List<string> messages = new List<string>();
                            bool success = Errors.TryGetValue(fieldName, out messages);
                            if (success)
                            {
                                messages.Add(message);
                                OnErrorsChanged(fieldName);
                            }
                        }
                        else
                        {
                            List<string> messages = new List<string>() { message };
                            Errors.TryAdd(fieldName, messages);
                        }
                    }
                }
            }

            IsValidating = false;
        }

        public void OnErrorsChanged(string propertyName)
        {
            var handler = ErrorsChanged;
            if (handler != null)
                handler(this, new DataErrorsChangedEventArgs(propertyName));
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            //if (e.PropertyName != "HasUnsavedChanges" &&
            //    e.PropertyName != "NeedsCollectionDeletion" &&
            //    e.PropertyName != "IsActive" &&
            //    e.PropertyName != "IsContact" &&
            //    e.PropertyName != "IsEditing" &&
            //    e.PropertyName != "IsSaving" &&
            //    e.PropertyName != "IsLoading" &&
            //    e.PropertyName != "IsNewRecord" &&
            //    e.PropertyName != "IsOtherOccupation" &&
            //    e.PropertyName != "IsShowingErrorDetailPanel" &&
            //    e.PropertyName != "IsShowingBleedingPanel" &&
            //    e.PropertyName != "IsShowingErrorDetailPanel" &&
            //    e.PropertyName != "IsShowingFieldValueChangesPanel" &&
            //    e.PropertyName != "IsShowingLabResultPanel" &&
            //    e.PropertyName != "IsActive" &&
            //    e.PropertyName != "LabResultsView" &&
            //    e.PropertyName != "SuppressValidation" &&
            //    e.PropertyName != "SuppressCriticalErrors" &&
            //    e.PropertyName != "IsInvalidId" &&
            //    e.PropertyName != "FieldValueChanges" &&
            //    e.PropertyName != "IsContact")


            Validate();
        }

        /// <summary>
        /// Gets the errors for a given property
        /// </summary>
        /// <param name="propertyName">The property to check</param>
        /// <returns>list of errors</returns>
        public IEnumerable GetErrors(string propertyName)
        {
            if (propertyName != null)
            {
                List<string> errorsForName;
                Errors.TryGetValue(propertyName, out errorsForName);
                return errorsForName;
            }
            else
            {
                return null; // ??
            }
        }

        /// <summary>
        /// Inserts this lab record into the database
        /// </summary>
        private void InsertIntoDatabase()
        {
            // TODO: Move this logic to some lower-level data model class

            string user = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString();
            DateTime now = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, 0);
            IDbDriver db = LabForm.Project.CollectedData.GetDatabase();

            Query insertQuery = db.CreateQuery("INSERT INTO [" + LabForm.TableName + "] (GlobalRecordId, RECSTATUS, FKEY, FirstSaveLogonName, LastSaveLogonName, FirstSaveTime, LastSaveTime) VALUES (" +
                    "@GlobalRecordId, @RECSTATUS, @FKEY, @FirstSaveLogonName, @LastSaveLogonName, @FirstSaveTime, @LastSaveTime)");
            insertQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, RecordId));
            insertQuery.Parameters.Add(new QueryParameter("@RECSTATUS", DbType.Byte, 1));
            insertQuery.Parameters.Add(new QueryParameter("@FKEY", DbType.String, CaseVM.RecordId));
            insertQuery.Parameters.Add(new QueryParameter("@FirstSaveLogonName", DbType.String, user));
            insertQuery.Parameters.Add(new QueryParameter("@LastSaveLogonName", DbType.String, user));
            insertQuery.Parameters.Add(new QueryParameter("@FirstSaveTime", DbType.DateTime, now));
            insertQuery.Parameters.Add(new QueryParameter("@LastSaveTime", DbType.DateTime, now));
            int rows = db.ExecuteNonQuery(insertQuery);

            if (rows != 1)
            {
                throw new InvalidOperationException("Row insert failed");
            }

            foreach (Epi.Page page in LabForm.Pages)
            {
                Query pageInsertQuery = db.CreateQuery("INSERT INTO [" + page.TableName + "] (GlobalRecordId) VALUES (@GlobalRecordId)");
                pageInsertQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, RecordId));
                rows = db.ExecuteNonQuery(pageInsertQuery);

                if (rows != 1)
                {
                    throw new InvalidOperationException("Row insert failed");
                }
            }

            // Now that empty rows exist for all page tables, do a normal update
            UpdateInDatabase();
        }


        /// <summary>
        /// Updates this case record's first page in the database with the in-memory representation
        /// </summary>
        private void UpdateInDatabasePage1()
        {
            RenderableField field1 = LabForm.Fields["FieldLabSpecID"] as RenderableField;
            IDbDriver db = LabForm.Project.CollectedData.GetDatabase();

            if (field1 != null && field1.Page != null)
            {
               
                Query updateQuery = db.CreateQuery("UPDATE [" + field1.Page.TableName + "] SET " +
                        "[SurnameLab] = @SurnameLab, " +
                        "[OtherNameLab] = @OtherNameLab, " +
                        "[ID] = @ID, " +
                        "[AgeLab] = @AgeLab, " +
                        "[SampleType] = @SampleType, " +
                        "[SampleOtherType] = @SampleOtherType, " +
                        "[DateSampleCollected] = @DateSampleCollected, " +
                        "[DateSampleTested] = @DateSampleTested, " +
                        "[FieldLabSpecID] = @FieldLabSpecID, " +
                        "[UGSPBLog] = @UGSPBLog, " +
                        "[DaysAcute] = @DaysAcute, " +
                        "[SampleInterpret] = @SampleInterpret, " +
                        "[EbolaTested] = @EbolaTested, " +
                        "[EBOVPCR1] = @EBOVPCR1, " +
                        "[EBOVPCR2] = @EBOVPCR2, " +
                        "[EBOVAg] = @EBOVAg, " +
                        "[EBOVIgM] = @EBOVIgM, " +
                        "[EBOVIgG] = @EBOVIgG, " +
                        "[EBOVCT1] = @EBOVCT1, " +
                        "[EBOVCT2] = @EBOVCT2, " +
                        "[EBOVAgTiter] = @EBOVAgTiter, " +
                        "[EBOVIgMTiter] = @EBOVIgMTiter, " +
                        "[EBOVIgGTiter] = @EBOVIgGTiter, " +
                        "[EBOVAgSumOD] = @EBOVAgSumOD, " +
                        "[EBOVIgMSumOD] = @EBOVIgMSumOD, " +
                        "[EBOVIgGSumOD] = @EBOVIgGSumOD, " +

                        "[MARVPolPCR] = @MARVPolPCR, " +
                        "[MARVVP40PCR] = @MARVVP40PCR, " +
                        "[MARVAg] = @MARVAg, " +
                        "[MARVIgM] = @MARVIgM, " +
                        "[MARVIgG] = @MARVIgG, " +
                        "[MARVPolCT] = @MARVPolCT, " +
                        "[MARVVP40CT] = @MARVVP40CT, " +
                        "[MARVAgTiter] = @MARVAgTiter, " +
                        "[MARVIgMTiter] = @MARVIgMTiter, " +
                        "[MARVIgGTiter] = @MARVIgGTiter, " +
                        "[MARVAgSumOD] = @MARVAgSumOD, " +
                        "[MARVIgMSumOD] = @MARVIgMSumOD, " +
                        "[MARVIgGSumOD] = @MARVIgGSumOD " +

                        "WHERE [GlobalRecordId] = @GlobalRecordId");            

                updateQuery.Parameters.Add(new QueryParameter("@SurnameLab", DbType.String, CaseVM.Surname));
                updateQuery.Parameters.Add(new QueryParameter("@OtherNameLab", DbType.String, CaseVM.OtherNames));
                updateQuery.Parameters.Add(new QueryParameter("@ID", DbType.String, CaseVM.ID));

                if (CaseVM.AgeYears.HasValue)
                {
                    updateQuery.Parameters.Add(new QueryParameter("@AgeLab", DbType.String, CaseVM.AgeYears.ToString()));
                }
                else
                {
                    updateQuery.Parameters.Add(new QueryParameter("@AgeLab", DbType.String, String.Empty));
                }

                updateQuery.Parameters.Add(new QueryParameter("@SampleType", DbType.String, SampleType));
                updateQuery.Parameters.Add(new QueryParameter("@SampleOtherType", DbType.String, SampleOtherType));
                //updateQuery.Parameters.Add(new QueryParameter("@DateSampleCollected", DbType.String, REPLACEME));
                //updateQuery.Parameters.Add(new QueryParameter("@DateSampleTested", DbType.String, REPLACEME));

                if (DateSampleCollected.HasValue)
                {
                    updateQuery.Parameters.Add(new QueryParameter("@DateSampleCollected", DbType.DateTime, DateSampleCollected.Value));
                }
                else
                {
                    updateQuery.Parameters.Add(new QueryParameter("@DateSampleCollected", DbType.DateTime, DBNull.Value));
                }

                if (DateSampleTested.HasValue)
                {
                    updateQuery.Parameters.Add(new QueryParameter("@DateSampleTested", DbType.DateTime, DateSampleTested.Value));
                }
                else
                {
                    updateQuery.Parameters.Add(new QueryParameter("@DateSampleTested", DbType.DateTime, DBNull.Value));
                }

                updateQuery.Parameters.Add(new QueryParameter("@FieldLabSpecID", DbType.String, FieldLabSpecimenID));
                updateQuery.Parameters.Add(new QueryParameter("@UGSPBLog", DbType.String, UVRIVSPBLogNumber));
                //updateQuery.Parameters.Add(new QueryParameter("@DaysAcute", DbType.String, REPLACEME));

                if (DaysAcute.HasValue)
                {
                    updateQuery.Parameters.Add(new QueryParameter("@DaysAcute", DbType.Double, DaysAcute.Value));
                }
                else
                {
                    updateQuery.Parameters.Add(new QueryParameter("@DaysAcute", DbType.Double, DBNull.Value));
                }

                updateQuery.Parameters.Add(new QueryParameter("@SampleInterpret", DbType.String, SampleInterpret));
                if (LabResult.EBOVPCR != String.Empty ||
                LabResult.EBOVPCR2 != String.Empty ||
                LabResult.EBOVAg != String.Empty ||
                LabResult.EBOVIgM != String.Empty ||
                LabResult.EBOVIgG != String.Empty)
                    updateQuery.Parameters.Add(new QueryParameter("@EbolaTested", DbType.Boolean, true));
                else
                    updateQuery.Parameters.Add(new QueryParameter("@EbolaTested", DbType.Boolean, false));

                updateQuery.Parameters.Add(new QueryParameter("@EBOVPCR1", DbType.String, EBOVPCR));
                updateQuery.Parameters.Add(new QueryParameter("@EBOVPCR2", DbType.String, EBOVPCR2));
                updateQuery.Parameters.Add(new QueryParameter("@EBOVAg", DbType.String, EBOVAg));
                updateQuery.Parameters.Add(new QueryParameter("@EBOVIgM", DbType.String, EBOVIgM));
                updateQuery.Parameters.Add(new QueryParameter("@EBOVIgG", DbType.String, EBOVIgG));
                if (EBOVCT1.HasValue) { updateQuery.Parameters.Add(new QueryParameter("@EBOVCT1", DbType.Double, EBOVCT1)); } else { updateQuery.Parameters.Add(new QueryParameter("@EBOVCT1", DbType.Double, DBNull.Value)); }
                if (EBOVCT2.HasValue) { updateQuery.Parameters.Add(new QueryParameter("@EBOVCT2", DbType.Double, EBOVCT2)); } else { updateQuery.Parameters.Add(new QueryParameter("@EBOVCT2", DbType.Double, DBNull.Value)); }
                updateQuery.Parameters.Add(new QueryParameter("@EBOVAgTiter", DbType.String, EBOVAgTiter));
                updateQuery.Parameters.Add(new QueryParameter("@EBOVIgMTiter", DbType.String, EBOVIgMTiter));
                updateQuery.Parameters.Add(new QueryParameter("@EBOVIgGTiter", DbType.String, EBOVIgGTiter));

                if (EBOVAgSumOD.HasValue) { updateQuery.Parameters.Add(new QueryParameter("@EBOVAgSumOD", DbType.Double, EBOVAgSumOD)); } else { updateQuery.Parameters.Add(new QueryParameter("@EBOVAgSumOD", DbType.Double, DBNull.Value)); }
                if (EBOVIgMSumOD.HasValue) { updateQuery.Parameters.Add(new QueryParameter("@EBOVIgMSumOD", DbType.Double, EBOVIgMSumOD)); } else { updateQuery.Parameters.Add(new QueryParameter("@EBOVIgMSumOD", DbType.Double, DBNull.Value)); }
                if (EBOVIgGSumOD.HasValue) { updateQuery.Parameters.Add(new QueryParameter("@EBOVIgGSumOD", DbType.Double, EBOVIgGSumOD)); } else { updateQuery.Parameters.Add(new QueryParameter("@EBOVIgGSumOD", DbType.Double, DBNull.Value)); }


                updateQuery.Parameters.Add(new QueryParameter("@MARVPolPCR", DbType.String, MARVPCR));
                updateQuery.Parameters.Add(new QueryParameter("@MARVVP40PCR", DbType.String, MARVPCR2));
                updateQuery.Parameters.Add(new QueryParameter("@MARVAg", DbType.String, MARVAg));
                updateQuery.Parameters.Add(new QueryParameter("@MARVIgM", DbType.String, MARVIgM));
                updateQuery.Parameters.Add(new QueryParameter("@MARVIgG", DbType.String, MARVIgG));
                if (MARVCT1.HasValue) { updateQuery.Parameters.Add(new QueryParameter("@MARVPolCT", DbType.Double, MARVCT1)); } else { updateQuery.Parameters.Add(new QueryParameter("@MARVPolCT", DbType.Double, DBNull.Value)); }
                if (MARVCT2.HasValue) { updateQuery.Parameters.Add(new QueryParameter("@MARVVP40CT", DbType.Double, MARVCT2)); } else { updateQuery.Parameters.Add(new QueryParameter("@MARVVP40CT", DbType.Double, DBNull.Value)); }
                updateQuery.Parameters.Add(new QueryParameter("@MARVAgTiter", DbType.String, MARVAgTiter));
                updateQuery.Parameters.Add(new QueryParameter("@MARVIgMTiter", DbType.String, MARVIgMTiter));
                updateQuery.Parameters.Add(new QueryParameter("@MARVIgGTiter", DbType.String, MARVIgGTiter));

                if (MARVAgSumOD.HasValue) { updateQuery.Parameters.Add(new QueryParameter("@MARVAgSumOD", DbType.Double, MARVAgSumOD)); } else { updateQuery.Parameters.Add(new QueryParameter("@MARVAgSumOD", DbType.Double, DBNull.Value)); }
                if (MARVIgMSumOD.HasValue) { updateQuery.Parameters.Add(new QueryParameter("@MARVIgMSumOD", DbType.Double, MARVIgMSumOD)); } else { updateQuery.Parameters.Add(new QueryParameter("@MARVIgMSumOD", DbType.Double, DBNull.Value)); }
                if (MARVIgGSumOD.HasValue) { updateQuery.Parameters.Add(new QueryParameter("@MARVIgGSumOD", DbType.Double, MARVIgGSumOD)); } else { updateQuery.Parameters.Add(new QueryParameter("@MARVIgGSumOD", DbType.Double, DBNull.Value)); }
               
                updateQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, RecordId));

#if DEBUG
                foreach (QueryParameter p in updateQuery.Parameters)
                {
                    System.Diagnostics.Debug.Print(p.ParameterName);
                }
#endif

                db.ExecuteNonQuery(updateQuery);
            }
        }


        /// <summary>
        /// Updates this case record's second page in the database with the in-memory representation
        /// </summary>
        private void UpdateInDatabasePage2()
        {
            RenderableField field = LabForm.Fields["Malariat"] as RenderableField;
            IDbDriver db = LabForm.Project.CollectedData.GetDatabase();

            if (field != null && field.Page != null)
            {
                Query updateQuery = db.CreateQuery("UPDATE [" + field.Page.TableName + "] SET " +
                        "[Malariat] = @Malariat " +

                        "WHERE [GlobalRecordId] = @GlobalRecordId");

                if (string.IsNullOrEmpty(MRT))
                {
                    updateQuery.Parameters.Add(new QueryParameter("@Malariat", DbType.String, ""));
                }
                else
                {
                    updateQuery.Parameters.Add(new QueryParameter("@Malariat", DbType.String, MRT));
                }

                updateQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, RecordId));

                //#if DEBUG
                //                foreach (QueryParameter p in updateQuery.Parameters)
                //                {
                //                    System.Diagnostics.Debug.Print(p.ParameterName);
                //                }
                //#endif

                db.ExecuteNonQuery(updateQuery);
            }
        }


        /// <summary>
        /// Updates this lab record in the database with the in-memory representation
        /// </summary>
        private void UpdateInDatabase()
        {
            // TODO: Move this logic to some lower-level data model class

            string user = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString();
            DateTime now = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, 0);
            IDbDriver db = LabForm.Project.CollectedData.GetDatabase();

            // update base table
            Query baseTableQuery = db.CreateQuery("UPDATE " + LabForm.TableName + " SET [LastSaveLogonName] = @LastSaveLogonName, [LastSaveTime] = @LastSaveTime WHERE [GlobalRecordId] = @GlobalRecordId");
            baseTableQuery.Parameters.Add(new QueryParameter("@LastSaveLogonName", DbType.String, user));
            baseTableQuery.Parameters.Add(new QueryParameter("@LastSaveTime", DbType.DateTime, now));
            baseTableQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, RecordId));
            db.ExecuteNonQuery(baseTableQuery);

            RenderableField field1 = LabForm.Fields["FieldLabSpecID"] as RenderableField;

            if (field1 != null)
            {
                UpdateInDatabasePage1();
                UpdateInDatabasePage2();
            }
        }

        /// <summary>
        /// Saves the in-memory representation of this result to disk
        /// </summary>
        public void Save()
        {
            //IsSaving = true;

            bool exists = false;

            IDbDriver db = LabForm.Project.CollectedData.GetDatabase();

            Query existsQuery = db.CreateQuery("SELECT GlobalRecordId FROM " + LabForm.TableName + " WHERE GlobalRecordId = @GlobalRecordId");
            existsQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, RecordId));
            DataTable dt = db.Select(existsQuery);

            if (dt.Rows.Count == 1)
            {
                exists = true;
            }
            else if (dt.Rows.Count > 1)
            {
                throw new InvalidOperationException("Something that should never fail has failed."); // TODO: Improve this verbiage
            }

            if (!exists)
            {
                // existing case record; do an UPDATE
                InsertIntoDatabase();

                DbLogger.Log(String.Format(
                    "Inserted lab : FLSID = {0}, SampleInterpret = {1}, EBOV PCR1 = {2}, GUID = {3}, ID of parent case = {4}, EpiCaseDef of parent case = {5}, GUID of parent case = {6}",
                        FieldLabSpecimenID, SampleInterpret, EBOVPCR, RecordId, _caseViewModel.ID, _caseViewModel.EpiCaseDef, _caseViewModel.RecordId));
            }
            else
            {
                // new case record; do an INSERT
                UpdateInDatabase();

                DbLogger.Log(String.Format(
                    "Updated lab : FLSID = {0}, SampleInterpret = {1}, EBOV PCR1 = {2}, GUID = {3}, ID of parent case = {4}, EpiCaseDef of parent case = {5}, GUID of parent case = {6}",
                        FieldLabSpecimenID, SampleInterpret, EBOVPCR, RecordId, _caseViewModel.ID, _caseViewModel.EpiCaseDef, _caseViewModel.RecordId));
            }

            //HasUnsavedChanges = false;

            IsNewRecord = false;

            //IsSaving = false;

            //_localCopy = new CaseViewModel(this);
            //FieldValueChanges.Clear();
        }

        public void Load()
        {
            IDbDriver db = LabForm.Project.CollectedData.GetDatabase();
            Query selectQuery = db.CreateQuery("SELECT * " + LabForm.FromViewSQL + " WHERE t.GlobalRecordId = @GlobalRecordId");
            selectQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, RecordId));
            DataTable labTable = db.Select(selectQuery);

            if (labTable.Rows.Count == 1)
            {
                Load(labTable.Rows[0]);
            }
        }

        public void Load(DataRow row)
        {
            #region Input Validation
            if (row == null)
            {
                throw new ArgumentNullException("row");
            }
            #endregion // Input Validation

            if (row.Table.Columns.Contains("t.GlobalRecordId"))
            {
                RecordId = row["t.GlobalRecordId"].ToString();
            }
            else if (row.Table.Columns.Contains("GlobalRecordId"))
            {
                RecordId = row["GlobalRecordId"].ToString();
            }

            LabCaseID = row["ID"].ToString();
            if (!String.IsNullOrEmpty(row["DateSampleCollected"].ToString()))
            {
                DateSampleCollected = DateTime.Parse(row["DateSampleCollected"].ToString());
            }
            else
            {
                DateSampleCollected = null;
            }

            if (!String.IsNullOrEmpty(row["DateSampleTested"].ToString()))
            {
                DateSampleTested = DateTime.Parse(row["DateSampleTested"].ToString());
            }
            else
            {
                DateSampleTested = null;
            }

            if (!String.IsNullOrEmpty(row["DaysAcute"].ToString()))
            {
                DaysAcute = Int32.Parse(row["DaysAcute"].ToString());
            }
            else
            {
                DaysAcute = null;
            }

            FieldLabSpecimenID = row["FieldLabSpecID"].ToString();
            SampleInterpret = row["SampleInterpret"].ToString();
            SampleOtherType = row["SampleOtherType"].ToString();

            switch (row["SampleInterpret"].ToString())
            {
                case "1":
                    SampleInterpretation = DataHelperBase.SampleInterpretConfirmedAcute;
                    break;
                case "2":
                    SampleInterpretation = DataHelperBase.SampleInterpretConfirmedConvalescent;
                    break;
                case "3":
                    SampleInterpretation = DataHelperBase.SampleInterpretNotCase;
                    break;
                case "4":
                    SampleInterpretation = DataHelperBase.SampleInterpretIndeterminate;
                    break;
                case "5":
                    SampleInterpretation = DataHelperBase.SampleInterpretNegativeNeedsFollowUp;
                    break;
                default:
                    SampleInterpretation = String.Empty;
                    break;
            }

            MRT = row["Malariat"].ToString();
            //switch (row["Malariat"].ToString())
            //{
            //    case "1":
            //        MalariaRapidTest = DataHelperBase.PCRPositive;
            //        break;
            //    case "2":
            //        MalariaRapidTest = DataHelperBase.PCRNegative;
            //        break;
            //    case "3":
            //        MalariaRapidTest = DataHelperBase.PCRNotAvailable;
            //        break;
            //}

            SampleType = row["SampleType"].ToString();
            switch (row["SampleType"].ToString())
            {
                case "1":
                    SampleTypeLocalized = DataHelperBase.SampleTypeWholeBlood;
                    break;
                case "2":
                    SampleTypeLocalized = DataHelperBase.SampleTypeSerum;//"Serum";
                    break;
                case "3":
                    SampleTypeLocalized = DataHelperBase.SampleTypeHeartBlood; //"Post-mortem heart blood";
                    break;
                case "4":
                    SampleTypeLocalized = DataHelperBase.SampleTypeSkin;//"Skin biopsy";
                    break;
                case "5":
                    SampleTypeLocalized = DataHelperBase.SampleTypeOther;//"Other";
                    break;
                case "6":
                    SampleTypeLocalized = DataHelperBase.SampleTypeSalivaSwab;//"Saliva";
                    break;
                default:
                    SampleTypeLocalized = String.Empty;
                    break;
            }

            UVRIVSPBLogNumber = row["UGSPBLog"].ToString();

            SUDVPCR = row["SUDVNPPCR"].ToString();
            SUDVPCR2 = row["SUDVPCR2"].ToString();
            SUDVAg = row["SUDVAg"].ToString();
            SUDVIgM = row["SUDVIgM"].ToString();
            SUDVIgG = row["SUDVIgG"].ToString();

            BDBVPCR = row["BDBVNPPCR"].ToString();
            BDBVPCR2 = row["BDBVVP40PCR"].ToString();
            BDBVAg = row["BDBVAg"].ToString();
            BDBVIgM = row["BDBVIgM"].ToString();
            BDBVIgG = row["BDBVIgG"].ToString();

            EBOVPCR = row["EBOVPCR1"].ToString();
            EBOVPCR2 = row["EBOVPCR2"].ToString();
            EBOVAg = row["EBOVAg"].ToString();
            EBOVIgM = row["EBOVIgM"].ToString();
            EBOVIgG = row["EBOVIgG"].ToString();

            if (row["EBOVCT1"] != DBNull.Value) { EBOVCT1 = Convert.ToDouble(row["EBOVCT1"]); } else { EBOVCT1 = null; }
            if (row["EBOVCT2"] != DBNull.Value) { EBOVCT2 = Convert.ToDouble(row["EBOVCT2"]); } else { EBOVCT2 = null; }
            EBOVAgTiter = row["EBOVAgTiter"].ToString();
            EBOVIgMTiter = row["EBOVIgMTiter"].ToString();
            EBOVIgGTiter = row["EBOVIgGTiter"].ToString();

            if (row["EBOVAgSumOD"] != DBNull.Value) { EBOVAgSumOD = Convert.ToDouble(row["EBOVAgSumOD"]); } else { EBOVAgSumOD = null; }
            if (row["EBOVIgMSumOD"] != DBNull.Value) { EBOVIgMSumOD = Convert.ToDouble(row["EBOVIgMSumOD"]); } else { EBOVIgMSumOD = null; }
            if (row["EBOVIgGSumOD"] != DBNull.Value) { EBOVIgGSumOD = Convert.ToDouble(row["EBOVIgGSumOD"]); } else { EBOVIgGSumOD = null; }

            MARVPCR = row["MARVPolPCR"].ToString();
            MARVPCR2 = row["MARVVP40PCR"].ToString();
            MARVAg = row["MARVAg"].ToString();
            MARVIgM = row["MARVIgM"].ToString();
            MARVIgG = row["MARVIgG"].ToString();

            if (row["MARVPolCT"] != DBNull.Value) { MARVCT1 = Convert.ToDouble(row["MARVPolCT"]); } else { MARVCT1 = null; }
            if (row["MARVVP40CT"] != DBNull.Value) { MARVCT2 = Convert.ToDouble(row["MARVVP40CT"]); } else { MARVCT2 = null; }
            MARVAgTiter = row["MARVAgTiter"].ToString();
            MARVIgMTiter = row["MARVIgMTiter"].ToString();
            MARVIgGTiter = row["MARVIgGTiter"].ToString();

            if (row["MARVAgSumOD"] != DBNull.Value) { MARVAgSumOD = Convert.ToDouble(row["MARVAgSumOD"]); } else { MARVAgSumOD = null; }
            if (row["MARVIgMSumOD"] != DBNull.Value) { MARVIgMSumOD = Convert.ToDouble(row["MARVIgMSumOD"]); } else { MARVIgMSumOD = null; }
            if (row["MARVIgGSumOD"] != DBNull.Value) { MARVIgGSumOD = Convert.ToDouble(row["MARVIgGSumOD"]); } else { MARVIgGSumOD = null; }

            CCHFPCR = row["CCHFPCR1"].ToString();
            CCHFPCR2 = row["CCHFPCR2"].ToString();
            CCHFAg = row["CCHFAg"].ToString();
            CCHFIgM = row["CCHFIgM"].ToString();
            CCHFIgG = row["CCHFIgG"].ToString();

            RVFPCR = row["RVFPCR1"].ToString();
            RVFPCR2 = row["RVFPCR2"].ToString();
            RVFAg = row["RVFAg"].ToString();
            RVFIgM = row["RVFIgM"].ToString();
            RVFIgG = row["RVFIgG"].ToString();

            LHFPCR = row["LASPCR1"].ToString();
            LHFPCR2 = row["LASPCR2"].ToString();
            LHFAg = row["LASAg"].ToString();
            LHFIgM = row["LASIgM"].ToString();
            LHFIgG = row["LASIgG"].ToString();
            if (IsCountryUS)
            {
                LabSampleTest = row["LabSampleTest"].ToString();
                FacilityLabSubmit = row["FacilityLabSubmit"].ToString();
                PersonLabSubmit = row["PersonLabSubmit"].ToString();
                PhoneLabSubmit = row["PhoneLabSubmit"].ToString();
                EmailLabSubmit = row["EmailLabSubmit"].ToString();
            }
        }

        public void CopyResult(LabResultViewModel updatedResult)
        {
            this.RecordId = updatedResult.RecordId;
            this.OtherNames = updatedResult.OtherNames;
            this.CaseID = updatedResult.CaseID;
            this.LabCaseID = updatedResult.LabCaseID;
            this.CaseRecordGuid = updatedResult.CaseRecordGuid;
            this.FieldLabSpecimenID = updatedResult.FieldLabSpecimenID;
            this.UVRIVSPBLogNumber = updatedResult.UVRIVSPBLogNumber;
            this.Village = updatedResult.Village;
            this.District = updatedResult.District;
            this.SampleType = updatedResult.SampleType;
            this.DateOnset = updatedResult.DateOnset;
            this.DateSampleCollected = updatedResult.DateSampleCollected;
            this.DateSampleTested = updatedResult.DateSampleTested;
            this.DateDeath = updatedResult.DateDeath;
            this.DaysAcute = updatedResult.DaysAcute;
            this.FinalLabClassification = updatedResult.FinalLabClassification;
            this.SampleInterpretation = updatedResult.SampleInterpretation;
            this.UniqueKey = updatedResult.UniqueKey;

            this.SUDVPCR = updatedResult.SUDVPCR;
            this.SUDVPCR2 = updatedResult.SUDVPCR2;
            this.SUDVAg = updatedResult.SUDVAg;
            this.SUDVIgM = updatedResult.SUDVIgM;
            this.SUDVIgG = updatedResult.SUDVIgG;

            this.BDBVPCR = updatedResult.BDBVPCR;
            this.BDBVPCR2 = updatedResult.BDBVPCR2;
            this.BDBVAg = updatedResult.BDBVAg;
            this.BDBVIgM = updatedResult.BDBVIgM;
            this.BDBVIgG = updatedResult.BDBVIgG;

            this.EBOVPCR = updatedResult.EBOVPCR;
            this.EBOVPCR2 = updatedResult.EBOVPCR2;
            this.EBOVAg = updatedResult.EBOVAg;
            this.EBOVIgM = updatedResult.EBOVIgM;
            this.EBOVIgG = updatedResult.EBOVIgG;

            this.MARVPCR = updatedResult.MARVPCR;
            this.MARVPCR2 = updatedResult.MARVPCR2;
            this.MARVAg = updatedResult.MARVAg;
            this.MARVIgM = updatedResult.MARVIgM;
            this.MARVIgG = updatedResult.MARVIgG;

            this.CCHFPCR = updatedResult.CCHFPCR;
            this.CCHFPCR2 = updatedResult.CCHFPCR2;
            this.CCHFAg = updatedResult.CCHFAg;
            this.CCHFIgM = updatedResult.CCHFIgM;
            this.CCHFIgG = updatedResult.CCHFIgG;

            this.RVFPCR = updatedResult.RVFPCR;
            this.RVFPCR2 = updatedResult.RVFPCR2;
            this.RVFAg = updatedResult.RVFAg;
            this.RVFIgM = updatedResult.RVFIgM;
            this.RVFIgG = updatedResult.RVFIgG;

            this.LHFPCR = updatedResult.LHFPCR;
            this.LHFPCR2 = updatedResult.LHFPCR2;
            this.LHFAg = updatedResult.LHFAg;
            this.LHFIgM = updatedResult.LHFIgM;
            this.LHFIgG = updatedResult.LHFIgG;
        }

        #region Commands

        private bool CanExecuteSaveCommand()
        {
            if (this.HasErrors || this.CaseVM.HasErrors)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        void CopyExecute(LabResultViewModel updatedResult)
        {
            CopyResult(updatedResult);
        }

        public ICommand Copy { get { return new RelayCommand<LabResultViewModel>(CopyExecute); } }

        public ICommand ToggleErrorDisplayCommand { get { return new RelayCommand(ToggleErrorDisplayCommandExecute); } }
        protected void ToggleErrorDisplayCommandExecute()
        {
            IsShowingErrorDetailPanel = !IsShowingErrorDetailPanel;
        }

        public ICommand CancelEditModeCommand { get { return new RelayCommand(CancelEditModeCommandExecute); } }
        protected void CancelEditModeCommandExecute()
        {
            CaseVM.IsShowingLabResultPanel = false;
            IsEditing = false;
            if (!IsNewRecord)
            {
                Load();
            }
            else
            {
                if (MarkedForRemoval != null)
                {
                    MarkedForRemoval(this, new EventArgs());
                }
            }
        }

        public ICommand SaveCommand { get { return new RelayCommand(SaveCommandExecute, CanExecuteSaveCommand); } }
        protected void SaveCommandExecute()
        {
            //SuppressCriticalErrors = false;
            Validate();

            if (SaveCommand.CanExecute(null))
            {
                Save();
            }
        }

        public ICommand SaveAndCloseCommand { get { return new RelayCommand(SaveAndCloseCommandExecute, CanExecuteSaveCommand); } }
        protected void SaveAndCloseCommandExecute()
        {
            //SuppressCriticalErrors = false;
            //Validate();

            if (SaveCommand.CanExecute(null))
            {
                Save();
            }

            //IsEditing = false;

            CaseVM.IsShowingLabResultPanel = false;

            ArrangeSamples(); //Fix for Issue # 17064

            IsEditing = false;
        }

        private void ArrangeSamples()
        {

            LabResultCollectionMaster LabResults = new LabResultCollectionMaster();

            var qry = CaseVM.LabResults.OrderBy(a => Convert.ToDateTime(a.DateSampleCollected));

            foreach (var item in qry)
            {
                LabResults.Add(item);
            }

            CaseVM.LabResults.Clear();

            int _sampleNumber = 1;

            foreach (var item in LabResults)
            {
                item.SampleNumber = "Sample" + " " + _sampleNumber;
                CaseVM.LabResults.Add(item);
                _sampleNumber++;
            }

            CaseVM.LabResultsView.Refresh();


        }

        #endregion
    }
}
