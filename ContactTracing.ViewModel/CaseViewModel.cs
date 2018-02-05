using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using ContactTracing.Core;
using ContactTracing.Core.Collections;
using ContactTracing.ViewModel.Collections;
using ContactTracing.ViewModel.Events;
using Epi;
using Epi.Data;
using Epi.Fields;

namespace ContactTracing.ViewModel
{
    /// <summary>
    /// A representation of a case patient; generally, someone believed to be (or is in fact) sick with Ebola or other hemorrhagic-fever causing virus
    /// </summary>
    public class CaseViewModel : ObservableObject, INotifyDataErrorInfo, IDisposable
    {
        #region Members
        private RecordErrorDictionary _errors = new RecordErrorDictionary();
        private RecordErrorDictionary _warnings = new RecordErrorDictionary();

        private CaseViewModel _localCopy;
        private object _errorsLock = new object();
        private object _labLock = new object();
        private object _fvLock = new object();
        private bool _isShowingErrorDetailPanel = false;
        private bool _isShowingOccupationPanel = false;
        private bool _isShowingBleedingPanel = false;
        private bool _isShowingFieldValueChangesPanel = false;
        private bool _isShowingLabResultPanel = false;
        private bool _isShowingSourceCasesPanel = false;
        private bool _isLoading = false;
        private bool _isEditing = false;
        private bool _isSaving = false;
        private bool _hasUnsavedChanges = false;
        private bool _isNewRecord = false;

        private bool _isOtherOccupation = false;
        private bool _isOnsetLocationSameAsResidence = false;

        private ObservableCollection<ContactViewModel> _contacts = new ObservableCollection<ContactViewModel>();
        private bool _isInvalidId = false;
        private bool _isDuplicateId = false;
        private bool _isLocked = false;
        private string _recordId = String.Empty;
        private string _id = String.Empty;
        private string _surname = String.Empty;
        private string _otherNames = String.Empty;
        private Core.Enums.EpiCaseClassification _epiCaseDef = Core.Enums.EpiCaseClassification.None;
        private string _epiCaseClassification = String.Empty;
        //Fix for issue # 17109
        private string _recordStatusComplete = "0";
        private string _recordStatusNoCRF = "0";
        private string _recordStatusMissCRF = "0";
        private string _recordStatusPenLab = "0";
        private string _recordStatusPenOut = "0";
        private string _recordStatus = String.Empty;
        private DateTime? _dateReport = null;
        private Core.Enums.Gender _gender = Core.Enums.Gender.None;
        private string _sex = String.Empty;
        private double? _age = null;
        private double? _ageYears = null;
        private AgeUnits? _ageUnit = null;
        private DateTime? _dateOnset = null;
        private string _headOfHousehold = String.Empty;
        private string _Village = String.Empty;
        private string _VillageOnset = String.Empty;
        private string _VillageHosp = String.Empty;
        private string _SubCounty = String.Empty;
        private string _Parish = String.Empty;
        private string _SubCountyOnset = String.Empty;
        private string _SubCountyHosp = String.Empty;
        private string _CountryHosp = String.Empty;
        private string _District = String.Empty;
        private string _DistrictOnset = String.Empty;
        private string _DistrictHosp = String.Empty;
        private string _Country = String.Empty;
        private string _CountryOnset = String.Empty;
        private string _AddressRes = String.Empty;
        private string _ZipRes = String.Empty;
        private string _Citizenship = String.Empty; //17178
        private DateTime? _dateIsolationCurrent = null;
        private DateTime? _dateDischargeIso = null;
        private DateTime? _DateHospitalCurrentAdmit = null;
        private DateTime? _DateDeath = null;
        private bool _DateDeathEst = false;
        private bool _DateOnsetEst = false;
        private string _DateOnsetEstExplain = String.Empty;
        private Core.Enums.AliveDead _finalStatus = Core.Enums.AliveDead.None;
        private Core.Enums.AliveDead _initialStatus = Core.Enums.AliveDead.None;
        private string _finalCaseStatus = String.Empty;
        private string _CurrentStatus = String.Empty;
        private Core.Enums.FinalLabClassification _finalLabClass = Core.Enums.FinalLabClassification.None;
        private string _finalLabClassification = String.Empty;
        private string _IsolationCurrent = String.Empty;
        private bool _IsContact = false;
        private bool? _IsHCW = false;
        private string _PlaceOfDeath = String.Empty;
        private string _PlaceOfDeathLocalized = String.Empty;
        private DateTime? _DateLastLabSampleTested = null;
        private DateTime? _DateLastLabSampleCollected = null;
        private Core.Enums.SampleInterpretation _lastSampleInterpretation = Core.Enums.SampleInterpretation.None;
        private string _lastSampleInterpret = String.Empty;
        private string _LastSamplePCRResult = String.Empty;
        private string _CurrentHospital = String.Empty;
        private DateTime? _DateOutcomeInfoCompleted = null;
        private string _BleedUnexplainedEver = String.Empty;
        private string _SpecifyBleeding = String.Empty;
        private string _HospitalDischarge = String.Empty;
        private string _HospitalDischargeDistrict = String.Empty;
        private bool _DateDischargeIsoEst = false;
        private DateTime? _DateDischargeHospital = null;
        private bool _DateDischargeHospitalEst = false;
        private DateTime? _DateDeath2 = null;
        private bool _DateDeath2Est = false;
        private string _DateDeath2EstSpecify = String.Empty;
        private string _PlaceDeath = String.Empty;
        private string _HospitalDeath = String.Empty;
        private string _PlaceDeathOther = String.Empty;
        private string _VillageDeath = String.Empty;
        private string _SubCountyDeath = String.Empty;
        private string _DistrictDeath = String.Empty;
        private DateTime? _DateFuneral = null;
        private bool _FuneralConductedFam = false;
        private bool _FuneralConductedOutbreakTeam = false;
        private string _VillageFuneral = String.Empty;
        private string _SubCountyFuneral = String.Empty;
        private string _DistrictFuneral = String.Empty;

        private string _OriginalID = String.Empty;
        private string _PhoneNumber = String.Empty;
        private string _PhoneOwner = String.Empty;
        private string _StatusReport = String.Empty;
        private bool _OccupationFarmer = false;
        private bool _OccupationButcher = false;
        private bool _OccupationHunter = false;
        private bool _OccupationMiner = false;
        private bool _OccupationReligious = false;
        private bool _OccupationHousewife = false;
        private bool _OccupationStudent = false;
        private bool _OccupationChild = false;
        private bool _OccupationBusinessman = false;
        private bool _OccupationTransporter = false;
        //private bool _OccupationHCW = false; // Case.REPLACEME; } set { if (Case.REPLACEME != value) { Case.REPLACEME = value; RaisePropertyChanged("REPLACEME"); } } }
        private bool _OccupationTraditionalHealer = false;
        private bool _OccupationOther = false;

        private string _OccupationTransporterSpecify = String.Empty;
        private string _OccupationBusinessSpecify = String.Empty;
        private string _OccupationOtherSpecify = String.Empty;
        private string _OccupationHCWPosition = String.Empty;
        private string _OccupationHCWFacility = String.Empty;

        private double? _Latitude = null;
        private double? _Longitude = null;

        private DateTime? _DateOnsetLocalStart = null;
        private DateTime? _DateOnsetLocalEnd = null;

        private string _HospitalizedCurrent = String.Empty;
        private string _HospitalizedPast = String.Empty;

        private DateTime? _DateHospitalPastStart1 = null;
        private DateTime? _DateHospitalPastStart2 = null;

        private DateTime? _DateHospitalPastEnd1 = null;
        private DateTime? _DateHospitalPastEnd2 = null;

        private string _HospitalPast1 = String.Empty;
        private string _HospitalPast2 = String.Empty;

        private string _HospitalVillage1 = String.Empty;
        private string _HospitalVillage2 = String.Empty;

        private string _HospitalDistrict1 = String.Empty;
        private string _HospitalDistrict2 = String.Empty;

        private string _IsolationPast1 = String.Empty;
        private string _IsolationPast2 = String.Empty;

        private string _SymptomFeverFinal = String.Empty;
        private double? _SymptomFeverTempFinal = null;
        private string _SymptomFeverTempSourceFinal = String.Empty;
        private string _SymptomVomitingFinal = String.Empty;
        private string _SymptomDiarrheaFinal = String.Empty;
        private string _SymptomFatigueFinal = String.Empty;
        private string _SymptomAnorexiaFinal = String.Empty;
        private string _SymptomAbdPainFinal = String.Empty;
        private string _SymptomChestPainFinal = String.Empty;
        private string _SymptomMusclePainFinal = String.Empty;
        private string _SymptomJointPainFinal = String.Empty;
        private string _SymptomHeadacheFinal = String.Empty;
        private string _SymptomCoughFinal = String.Empty;
        private string _SymptomDiffBreatheFinal = String.Empty;
        private string _SymptomDiffSwallowFinal = String.Empty;
        private string _SymptomSoreThroatFinal = String.Empty;
        private string _SymptomJaundiceFinal = String.Empty;
        private string _SymptomConjunctivitisFinal = String.Empty;
        private string _SymptomRashFinal = String.Empty;
        private string _SymptomHiccupsFinal = String.Empty;
        private string _SymptomPainEyesFinal = String.Empty;
        private string _SymptomUnconsciousFinal = String.Empty;
        private string _SymptomConfusedFinal = String.Empty;
        private string _SymptomOtherHemoFinal = String.Empty;
        private string _SymptomOtherHemoFinalSpecify = String.Empty;

        private string _SymptomUnexplainedBleeding = String.Empty;
        private string _SymptomBleedGums = String.Empty;
        private string _SymptomBleedInjectionSite = String.Empty;
        private string _SymptomNoseBleed = String.Empty;
        private string _SymptomBloodyStool = String.Empty;
        private string _SymptomHematemesis = String.Empty;
        private string _SymptomBloodVomit = String.Empty;
        private string _SymptomCoughBlood = String.Empty;
        private string _SymptomBleedVagina = String.Empty;
        private string _SymptomBleedSkin = String.Empty;
        private string _SymptomBleedUrine = String.Empty;
        private string _SymptomOtherNonHemorrhagic = String.Empty;

        private string _SymptomFever = String.Empty;
        private double? _SymptomFeverTemp = null;
        private string _SymptomFeverTempSource = String.Empty;
        private string _SymptomVomiting = String.Empty;
        private string _SymptomDiarrhea = String.Empty;
        private string _SymptomFatigue = String.Empty;
        private string _SymptomAnorexia = String.Empty;
        private string _SymptomAbdPain = String.Empty;
        private string _SymptomChestPain = String.Empty;
        private string _SymptomMusclePain = String.Empty;
        private string _SymptomJointPain = String.Empty;
        private string _SymptomHeadache = String.Empty;
        private string _SymptomCough = String.Empty;
        private string _SymptomDiffBreathe = String.Empty;
        private string _SymptomDiffSwallow = String.Empty;
        private string _SymptomSoreThroat = String.Empty;
        private string _SymptomJaundice = String.Empty;
        private string _SymptomConjunctivitis = String.Empty;
        private string _SymptomRash = String.Empty;
        private string _SymptomHiccups = String.Empty;
        private string _SymptomPainEyes = String.Empty;
        private string _SymptomUnconscious = String.Empty;
        private string _SymptomConfused = String.Empty;
        private string _SymptomOtherHemo = String.Empty;
        private string _SymptomOtherHemoSpecify = String.Empty;
        private string _SymptomOtherNonHemorrhagicSpecify = String.Empty;

        private string _HadContact = String.Empty;
        private string _ContactName1 = String.Empty;
        private string _ContactName2 = String.Empty;
        private string _ContactName3 = String.Empty;
        private string _ContactRelation1 = String.Empty;
        private string _ContactRelation2 = String.Empty;
        private string _ContactRelation3 = String.Empty;
        private DateTime? _ContactStartDate1 = null;
        private DateTime? _ContactStartDate2 = null;
        private DateTime? _ContactStartDate3 = null;
        private DateTime? _ContactEndDate1 = null;
        private DateTime? _ContactEndDate2 = null;
        private DateTime? _ContactEndDate3 = null;
        private bool _ContactDate1Estimated = false;
        private bool _ContactDate2Estimated = false;
        private bool _ContactDate3Estimated = false;
        private string _ContactVillage1 = String.Empty;
        private string _ContactVillage2 = String.Empty;
        private string _ContactVillage3 = String.Empty;
        private string _ContactDistrict1 = String.Empty;
        private string _ContactDistrict2 = String.Empty;
        private string _ContactDistrict3 = String.Empty;
        private string _ContactCountry1 = String.Empty;
        private string _ContactCountry2 = String.Empty;
        private string _ContactCountry3 = String.Empty;
        private string _TypesOfContact1 = String.Empty;
        private string _TypesOfContact2 = String.Empty;
        private string _TypesOfContact3 = String.Empty;
        private string _ContactStatus1 = String.Empty;
        private string _ContactStatus2 = String.Empty;
        private string _ContactStatus3 = String.Empty;
        private DateTime? _ContactDeathDate1 = null;
        private DateTime? _ContactDeathDate2 = null;
        private DateTime? _ContactDeathDate3 = null;

        private string _AttendFuneral = String.Empty; // Case.AttendFuneral; } set { if (Case.AttendFuneral != value) { Case.AttendFuneral = value; RaisePropertyChanged("AttendFuneral"); } } }
        private string _FuneralNameDeceased1 = String.Empty; // Case.FuneralNameDeceased1; } set { if (Case.FuneralNameDeceased1 != value) { Case.FuneralNameDeceased1 = value; RaisePropertyChanged("FuneralNameDeceased1"); } } }
        private string _FuneralNameDeceased2 = String.Empty; // Case.FuneralNameDeceased2; } set { if (Case.FuneralNameDeceased2 != value) { Case.FuneralNameDeceased2 = value; RaisePropertyChanged("FuneralNameDeceased2"); } } }
        private string _FuneralRelationDeceased1 = String.Empty; // Case.FuneralRelationDeceased1; } set { if (Case.FuneralRelationDeceased1 != value) { Case.FuneralRelationDeceased1 = value; RaisePropertyChanged("FuneralRelationDeceased1"); } } }
        private string _FuneralRelationDeceased2 = String.Empty; // Case.FuneralRelationDeceased2; } set { if (Case.FuneralRelationDeceased2 != value) { Case.FuneralRelationDeceased2 = value; RaisePropertyChanged("FuneralRelationDeceased2"); } } }
        private DateTime? _FuneralStartDate1 = null; // Case.FuneralStartDate1; } set { if (Case.FuneralStartDate1 != value) { Case.FuneralStartDate1 = value; RaisePropertyChanged("FuneralStartDate1"); } } }
        private DateTime? _FuneralStartDate2 = null; // Case.FuneralStartDate2; } set { if (Case.FuneralStartDate2 != value) { Case.FuneralStartDate2 = value; RaisePropertyChanged("FuneralStartDate2"); } } }
        private DateTime? _FuneralEndDate1 = null; // Case.FuneralEndDate1; } set { if (Case.FuneralEndDate1 != value) { Case.FuneralEndDate1 = value; RaisePropertyChanged("FuneralEndDate1"); } } }
        private DateTime? _FuneralEndDate2 = null; // Case.FuneralEndDate2; } set { if (Case.FuneralEndDate2 != value) { Case.FuneralEndDate2 = value; RaisePropertyChanged("FuneralEndDate2"); } } }
        private string _FuneralVillage1 = String.Empty; // Case.FuneralVillage1; } set { if (Case.FuneralVillage1 != value) { Case.FuneralVillage1 = value; RaisePropertyChanged("FuneralVillage1"); } } }
        private string _FuneralVillage2 = String.Empty; // Case.FuneralVillage2; } set { if (Case.FuneralVillage2 != value) { Case.FuneralVillage2 = value; RaisePropertyChanged("FuneralVillage2"); } } }
        private string _FuneralDistrict1 = String.Empty; // Case.FuneralDistrict1; } set { if (Case.FuneralDistrict1 != value) { Case.FuneralDistrict1 = value; RaisePropertyChanged("FuneralDistrict1"); } } }
        private string _FuneralDistrict2 = String.Empty; // Case.FuneralDistrict2; } set { if (Case.FuneralDistrict2 != value) { Case.FuneralDistrict2 = value; RaisePropertyChanged("FuneralDistrict2"); } } }
        private string _FuneralTouchBody1 = String.Empty; // Case.FuneralTouchBody1; } set { if (Case.FuneralTouchBody1 != value) { Case.FuneralTouchBody1 = value; RaisePropertyChanged("FuneralTouchBody1"); } } }
        private string _FuneralTouchBody2 = String.Empty; // Case.FuneralTouchBody2; } set { if (Case.FuneralTouchBody2 != value) { Case.FuneralTouchBody2 = value; RaisePropertyChanged("FuneralTouchBody2"); } } }

        private string _Travel = String.Empty; // Case.Travel; } set { if (Case.Travel != value) { Case.Travel = value; RaisePropertyChanged("Travel"); } } }
        private string _TravelVillage = String.Empty; // Case.TravelVillage; } set { if (Case.TravelVillage != value) { Case.TravelVillage = value; RaisePropertyChanged("TravelVillage"); } } }
        private string _TravelDistrict = String.Empty; // Case.TravelDistrict; } set { if (Case.TravelDistrict != value) { Case.TravelDistrict = value; RaisePropertyChanged("TravelDistrict"); } } }
        private string _TravelCountry = String.Empty;
        private DateTime? _TravelStartDate = null; // Case.TravelStartDate; } set { if (Case.TravelStartDate != value) { Case.TravelStartDate = value; RaisePropertyChanged("TravelStartDate"); } } }
        private DateTime? _TravelEndDate = null; // Case.TravelEndDate; } set { if (Case.TravelEndDate != value) { Case.TravelEndDate = value; RaisePropertyChanged("TravelEndDate"); } } }
        private bool _TravelDateEstimated = false; // Case.TravelDateEstimated; } set { if (Case.TravelDateEstimated != value) { Case.TravelDateEstimated = value; RaisePropertyChanged("TravelDateEstimated"); } } }

        private string _HospitalBeforeIll = String.Empty; // Case.HospitalBeforeIll; } set { if (Case.HospitalBeforeIll != value) { Case.HospitalBeforeIll = value; RaisePropertyChanged("HospitalBeforeIll"); } } }
        private string _HospitalBeforeIllPatient = String.Empty; // Case.HospitalBeforeIllPatient; } set { if (Case.HospitalBeforeIllPatient != value) { Case.HospitalBeforeIllPatient = value; RaisePropertyChanged("HospitalBeforeIllPatient"); } } }
        private string _HospitalBeforeIllHospitalName = String.Empty; // Case.HospitalBeforeIllHospitalName; } set { if (Case.HospitalBeforeIllHospitalName != value) { Case.HospitalBeforeIllHospitalName = value; RaisePropertyChanged("HospitalBeforeIllHospitalName"); } } }
        private string _HospitalBeforeIllVillage = String.Empty; // Case.HospitalBeforeIllVillage; } set { if (Case.HospitalBeforeIllVillage != value) { Case.HospitalBeforeIllVillage = value; RaisePropertyChanged("HospitalBeforeIllVillage"); } } }
        private string _HospitalBeforeIllDistrict = String.Empty; // Case.HospitalBeforeIllDistrict; } set { if (Case.HospitalBeforeIllDistrict != value) { Case.HospitalBeforeIllDistrict = value; RaisePropertyChanged("HospitalBeforeIllDistrict"); } } }
        private DateTime? _HospitalBeforeIllStartDate = null; // Case.HospitalBeforeIllStartDate; } set { if (Case.HospitalBeforeIllStartDate != value) { Case.HospitalBeforeIllStartDate = value; RaisePropertyChanged("HospitalBeforeIllStartDate"); } } }
        private DateTime? _HospitalBeforeIllEndDate = null; // Case.HospitalBeforeIllEndDate; } set { if (Case.HospitalBeforeIllEndDate != value) { Case.HospitalBeforeIllEndDate = value; RaisePropertyChanged("HospitalBeforeIllEndDate"); } } }
        private bool _HospitalBeforeIllDateEstimated = false; // Case.HospitalBeforeIllDateEstimated; } set { if (Case.HospitalBeforeIllDateEstimated != value) { Case.HospitalBeforeIllDateEstimated = value; RaisePropertyChanged("HospitalBeforeIllDateEstimated"); } } }

        private string _TraditionalHealer = String.Empty; // Case.TraditionalHealer; } set { if (Case.TraditionalHealer != value) { Case.TraditionalHealer = value; RaisePropertyChanged("TraditionalHealer"); } } }
        private string _TraditionalHealerName = String.Empty; // Case.TraditionalHealerName; } set { if (Case.TraditionalHealerName != value) { Case.TraditionalHealerName = value; RaisePropertyChanged("TraditionalHealerName"); } } }
        private string _TraditionalHealerVillage = String.Empty; // Case.TraditionalHealerVillage; } set { if (Case.TraditionalHealerVillage != value) { Case.TraditionalHealerVillage = value; RaisePropertyChanged("TraditionalHealerVillage"); } } }
        private string _TraditionalHealerDistrict = String.Empty; // Case.TraditionalHealerDistrict; } set { if (Case.TraditionalHealerDistrict != value) { Case.TraditionalHealerDistrict = value; RaisePropertyChanged("TraditionalHealerDistrict"); } } }
        private DateTime? _TraditionalHealerDate = null; // Case.TraditionalHealerDate; } set { if (Case.TraditionalHealerDate != value) { Case.TraditionalHealerDate = value; RaisePropertyChanged("TraditionalHealerDate"); } } }
        private bool _TraditionalHealerDateEstimated = false; // Case.TraditionalHealerDateEstimated; } set { if (Case.TraditionalHealerDateEstimated != value) { Case.TraditionalHealerDateEstimated = value; RaisePropertyChanged("TraditionalHealerDateEstimated"); } } }

        private string _Animals = String.Empty; // Case.Animals; } set { if (Case.Animals != value) { Case.Animals = value; RaisePropertyChanged("Animals"); } } }
        private bool _AnimalBats = false; // Case.AnimalBats; } set { if (Case.AnimalBats != value) { Case.AnimalBats = value; RaisePropertyChanged("AnimalBats"); } } }
        private bool _AnimalPrimates = false; // Case.AnimalPrimates; } set { if (Case.AnimalPrimates != value) { Case.AnimalPrimates = value; RaisePropertyChanged("AnimalPrimates"); } } }
        private bool _AnimalRodents = false; // Case.AnimalRodents; } set { if (Case.AnimalRodents != value) { Case.AnimalRodents = value; RaisePropertyChanged("AnimalRodents"); } } }
        private bool _AnimalPigs = false; // Case.AnimalPigs; } set { if (Case.AnimalPigs != value) { Case.AnimalPigs = value; RaisePropertyChanged("AnimalPigs"); } } }
        private bool _AnimalBirds = false; // Case.AnimalBirds; } set { if (Case.AnimalBirds != value) { Case.AnimalBirds = value; RaisePropertyChanged("AnimalBirds"); } } }
        private bool _AnimalCows = false; // Case.AnimalCows; } set { if (Case.AnimalCows != value) { Case.AnimalCows = value; RaisePropertyChanged("AnimalCows"); } } }
        private bool _AnimalOther = false; // Case.AnimalOther; } set { if (Case.AnimalOther != value) { Case.AnimalOther = value; RaisePropertyChanged("AnimalOther"); } } }

        private string _AnimalBatsStatus = String.Empty; // Case.AnimalBatsStatus; } set { if (Case.AnimalBatsStatus != value) { Case.AnimalBatsStatus = value; RaisePropertyChanged("AnimalBatsStatus"); } } }
        private string _AnimalPrimatesStatus = String.Empty; // Case.AnimalPrimatesStatus; } set { if (Case.AnimalPrimatesStatus != value) { Case.AnimalPrimatesStatus = value; RaisePropertyChanged("AnimalPrimatesStatus"); } } }
        private string _AnimalRodentsStatus = String.Empty; // Case.AnimalRodentsStatus; } set { if (Case.AnimalRodentsStatus != value) { Case.AnimalRodentsStatus = value; RaisePropertyChanged("AnimalRodentsStatus"); } } }
        private string _AnimalPigsStatus = String.Empty; // Case.AnimalPigsStatus; } set { if (Case.AnimalPigsStatus != value) { Case.AnimalPigsStatus = value; RaisePropertyChanged("AnimalPigsStatus"); } } }
        private string _AnimalBirdsStatus = String.Empty; // Case.AnimalBirdsStatus; } set { if (Case.AnimalBirdsStatus != value) { Case.AnimalBirdsStatus = value; RaisePropertyChanged("AnimalBirdsStatus"); } } }
        private string _AnimalCowsStatus = String.Empty; // Case.AnimalCowsStatus; } set { if (Case.AnimalCowsStatus != value) { Case.AnimalCowsStatus = value; RaisePropertyChanged("AnimalCowsStatus"); } } }
        private string _AnimalOtherStatus = String.Empty; // Case.AnimalOtherStatus; } set { if (Case.AnimalOtherStatus != value) { Case.AnimalOtherStatus = value; RaisePropertyChanged("AnimalOtherStatus"); } } }
        private string _AnimalOtherComment = String.Empty; // Case.AnimalOtherComment; } set { if (Case.AnimalOtherComment != value) { Case.AnimalOtherComment = value; RaisePropertyChanged("AnimalOtherComment"); } } }

        private string _BittenTick = String.Empty; // Case.BittenTick; } set { if (Case.BittenTick != value) { Case.BittenTick = value; RaisePropertyChanged("BittenTick"); } } }

        private string _InterviewerName = String.Empty; // Case.InterviewerName; } set { if (Case.InterviewerName != value) { Case.InterviewerName = value; RaisePropertyChanged("InterviewerName"); } } }
        private string _InterviewerPhone = String.Empty; // Case.InterviewerPhone; } set { if (Case.InterviewerPhone != value) { Case.InterviewerPhone = value; RaisePropertyChanged("InterviewerPhone"); } } }
        private string _InterviewerEmail = String.Empty; // Case.InterviewerEmail; } set { if (Case.InterviewerEmail != value) { Case.InterviewerEmail = value; RaisePropertyChanged("InterviewerEmail"); } } }
        private string _InterviewerPosition = String.Empty; // Case.InterviewerPosition; } set { if (Case.InterviewerPosition != value) { Case.InterviewerPosition = value; RaisePropertyChanged("InterviewerPosition"); } } }
        private string _InterviewerDistrict = String.Empty; // Case.InterviewerDistrict; } set { if (Case.InterviewerDistrict != value) { Case.InterviewerDistrict = value; RaisePropertyChanged("InterviewerDistrict"); } } }
        private string _InterviewerHealthFacility = String.Empty; // Case.InterviewerHealthFacility; } set { if (Case.InterviewerHealthFacility != value) { Case.InterviewerHealthFacility = value; RaisePropertyChanged("InterviewerHealthFacility"); } } }
        private string _InterviewerInfoProvidedBy = String.Empty; // Case.InterviewerInfoProvidedBy; } set { if (Case.InterviewerInfoProvidedBy != value) { Case.InterviewerInfoProvidedBy = value; RaisePropertyChanged("InterviewerInfoProvidedBy"); } } }
        private string _ProxyName = String.Empty; // Case.ProxyName; } set { if (Case.ProxyName != value) { Case.ProxyName = value; RaisePropertyChanged("ProxyName"); } } }
        private string _ProxyRelation = String.Empty; // Case.ProxyRelation; } set { if (Case.ProxyRelation != value) { Case.ProxyRelation = value; RaisePropertyChanged("ProxyRelation"); } } }

        private string _CommentsOnThisPatient = String.Empty; // Case.CommentsOnThisPatient; } set { if (Case.CommentsOnThisPatient != value) { Case.CommentsOnThisPatient = value; RaisePropertyChanged("CommentsOnThisPatient"); } } }
        #endregion // Members

        #region Static Members

        public static string SampleLabel = "Sample";

        public static string Male = String.Empty;
        public static string Female = String.Empty;

        public static string Dead = String.Empty;
        public static string Alive = String.Empty;

        public static string Months = String.Empty;
        public static string Years = String.Empty;

        public static string MaleAbbr = String.Empty;
        public static string FemaleAbbr = String.Empty;

        public static string PlaceDeathCommunityValue = String.Empty;
        public static string PlaceDeathHospitalValue = String.Empty;
        public static string PlaceDeathOtherValue = String.Empty;

        public static string RecComplete = String.Empty;
        public static string RecNoCRF = String.Empty;
        public static string RecMissCRF = String.Empty;
        public static string RecPendingLab = String.Empty;
        public static string RecPendingOutcome = String.Empty;

        #endregion // Static Members

        #region Static Properties
        public static List<string> IDPrefixes = new List<string>() { "ABC" };
        public static string IDSeparator = "-";
        public static string IDPattern = "###";
        public static bool IsCountryUS = false; //17178

        #endregion // Static Properties

        #region Properties

        public bool IsOpenedInSuperUserMode { get; set; }

        public ObservableCollection<SourceCaseInfoViewModel> SourceCases { get; private set; }
        public LabResultCollectionMaster LabResults { get; private set; }
        public ICollectionView LabResultsView { get; private set; }
        public ICollectionView SourceCasesView { get; private set; }

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

        public bool IsShowingOccupationPanel
        {
            get
            {
                return _isShowingOccupationPanel;
            }
            set
            {
                _isShowingOccupationPanel = value;
                RaisePropertyChanged("IsShowingOccupationPanel");
            }
        }

        public bool IsShowingSourceCasesPanel
        {
            get
            {
                return _isShowingSourceCasesPanel;
            }
            set
            {
                _isShowingSourceCasesPanel = value;
                RaisePropertyChanged("IsShowingSourceCasesPanel");
            }
        }

        public bool IsShowingLabResultPanel
        {
            get
            {
                return _isShowingLabResultPanel;
            }
            set
            {
                _isShowingLabResultPanel = value;
                RaisePropertyChanged("IsShowingLabResultPanel");
            }
        }

        public bool IsShowingFieldValueChangesPanel
        {
            get
            {
                return _isShowingFieldValueChangesPanel;
            }
            set
            {
                _isShowingFieldValueChangesPanel = value;
                RaisePropertyChanged("IsShowingFieldValueChangesPanel");
            }
        }

        public bool IsShowingBleedingPanel
        {
            get
            {
                return _isShowingBleedingPanel;
            }
            set
            {
                _isShowingBleedingPanel = value;
                RaisePropertyChanged("IsShowingBleedingPanel");
            }
        }

        private bool IsValidating { get; set; }

        public bool SuppressValidation { get; private set; }
        public bool SuppressCriticalErrors { get; private set; }

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

        /// <summary>
        /// Gets whether the record currently has any warnings
        /// </summary>
        public virtual bool HasWarnings
        {
            get
            {
                if (SuppressValidation)
                {
                    return false;
                }
                else
                {
                    return Warnings.Any(kv => kv.Value != null && kv.Value.Count > 0);
                }
            }
        }

        /// <summary>
        /// Stores errors for each field
        /// </summary>
        public RecordErrorDictionary Errors { get { return this._errors; } }

        /// <summary>
        /// Stores warnings for each field
        /// </summary>
        public RecordErrorDictionary Warnings { get { return this._warnings; } }

        private View CaseForm { get; set; }
        private View LabForm { get; set; }

        public bool IsNewRecord
        {
            get
            {
                return _isNewRecord;
            }
            private set
            {
                if (_isNewRecord != value)
                {
                    _isNewRecord = value;
                    RaisePropertyChanged("IsNewRecord");
                }
            }
        }
        public bool IsLoading
        {
            get
            {
                return _isLoading;
            }
            private set
            {
                if (IsLoading != value)
                {
                    _isLoading = value;
                    RaisePropertyChanged("IsLoading");
                }
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
                        Load();
                        SuppressValidation = false;
                        SuppressCriticalErrors = true;
                        Validate();

                        if (LabResultsView == null || LabResultsView.SourceCollection == null)
                        {
                            LabResultsView = System.Windows.Data.CollectionViewSource.GetDefaultView(LabResults);// new System.Windows.Data.CollectionViewSource { Source = LabResults }.View;
                        }
                        if (SourceCasesView == null || SourceCasesView.SourceCollection == null)
                        {
                            SourceCasesView = System.Windows.Data.CollectionViewSource.GetDefaultView(SourceCases);// new System.Windows.Data.CollectionViewSource { Source = SourceCases }.View;
                        }
                    }
                    else
                    {
                        SuppressValidation = true;
                        SuppressCriticalErrors = true;
                        IsShowingLabResultPanel = false;

                        if (ViewerClosed != null)
                        {
                            ViewerClosed(this, new EventArgs());
                        }
                    }
                }
            }
        }
        public bool IsSaving
        {
            get
            {
                return _isSaving;
            }
            set
            {
                if (IsSaving != value)
                {
                    _isSaving = value;
                    RaisePropertyChanged("IsSaving");
                }
            }
        }
        public bool HasUnsavedChanges
        {
            get
            {
                return _hasUnsavedChanges;
            }
            set
            {
                if (_hasUnsavedChanges != value)
                {
                    _hasUnsavedChanges = value;
                    RaisePropertyChanged("HasUnsavedChanges");
                }
            }
        }

        #region 1
        public int UniqueKey { get; set; }
        public string IsContactSymbol
        {
            get
            {
                if (IsContact)
                {
                    return "‼";
                }
                else
                {
                    return String.Empty;
                }
            }
        }
        public bool IsInvalidId
        {
            get { return this._isInvalidId; }
            set
            {
                if (IsInvalidId != value)
                {
                    this._isInvalidId = value;
                    RaisePropertyChanged("IsInvalidId");
                }
            }
        }
        public bool IsDuplicateId
        {
            get { return this._isDuplicateId; }
            set
            {
                if (IsDuplicateId != value)
                {
                    this._isDuplicateId = value;
                    RaisePropertyChanged("IsDuplicateId");
                }
            }
        }
        public bool IsLocked
        {
            get
            {
                return _isLocked;
            }
            set
            {
                if (_isLocked != value)
                {
                    _isLocked = value;
                    RaisePropertyChanged("IsLocked");
                }
            }
        }
        public string Error
        {
            get { return (this as IDataErrorInfo).Error; }
        }
        public string this[string columnName]
        {
            get
            {
                String errorMessage = String.Empty;
                switch (columnName)
                {
                    case "ID":
                        if (IsInvalidId)
                        {
                            if (!SuppressCriticalErrors)
                            {
                                errorMessage = "The case ID field is incorrectly formatted.";
                            }
                        }
                        else if (IsDuplicateId)
                        {
                            errorMessage = "This ID already exists for another case.";
                        }
                        break;
                    case "EpiCaseClassification":
                        if (!SuppressCriticalErrors || !IsNewRecord)
                        {
                            if (String.IsNullOrEmpty(EpiCaseClassification))
                            {
                                errorMessage = "Epi case classification is required.";
                            }
                        }
                        break;
                    case "Age":
                        if (Age.HasValue && Age < 0)
                        {
                            errorMessage = "Age in years cannot be less than zero.";
                        }
                        else if (Age.HasValue && AgeYears.HasValue && AgeYears.Value > 130)
                        {
                            errorMessage = "Age in years should not exceed 130.";
                        }
                        break;
                    case "AgeUnit":
                        if (!SuppressCriticalErrors && Age.HasValue == true && (AgeUnit.HasValue == false && String.IsNullOrEmpty(AgeUnitString)))
                        {
                            errorMessage = "Age unit is a required field.";
                        }
                        break;
                    case "DateReport":
                        if (DateReport.HasValue && DateReport.Value > DateTime.Now)
                        {
                            errorMessage = "The report date cannot be greater than current date.";
                        }
                        break;
                    //case "StatusReport":
                    //    if (!StatusReport.Equals("1")) // alive
                    //    {
                    //        DateDeath = null;
                    //    }
                    //    break;
                    //case "IsHCW":
                    //    if (IsHCW.HasValue == false || IsHCW.Value == false)
                    //    {
                    //        OccupationHCWPosition = String.Empty;
                    //        OccupationHCWFacility = String.Empty;
                    //    }
                    //    break;
                    case "DateDeath":
                        if (DateOnset.HasValue && DateDeath.HasValue && DateOnset.Value > DateDeath.Value)
                        {
                            errorMessage = String.Format("The date of onset of symptoms ({0}) cannot occur after the date of death.", DateOnset.Value.ToShortDateString());
                        }
                        else if (DateDeath.HasValue && DateDeath.Value > DateTime.Now)
                        {
                            errorMessage = "The date of death cannot be greater than current date.";
                        }
                        else if (DateFuneral.HasValue && DateDeath.HasValue && DateDeath.Value > DateFuneral.Value)
                        {
                            errorMessage = "The date of death cannot occur after the date of the patient's funeral.";
                        }
                        else if (DateIsolationCurrent.HasValue && DateDeath.HasValue && DateIsolationCurrent.Value > DateDeath.Value)
                        {
                            errorMessage = String.Format("The date of admission to isolation ({0}) cannot occur after the date of the patient's death.", DateIsolationCurrent.Value.ToShortDateString());
                        }
                        else if (DateDeath.HasValue && DateDeath2.HasValue && DateDeath2.Value != DateDeath.Value)
                        {
                            errorMessage = String.Format("The date of death at the time of reporting ({0}) differs from the date of death entered for the patient's final outcome ({1}).", DateDeath.Value.ToShortDateString(), DateDeath2.Value.ToShortDateString());
                        }
                        break;
                    case "DateOnset":
                        if (DateOnset.HasValue && DateDeath.HasValue && DateOnset.Value > DateDeath.Value)
                        {
                            errorMessage = String.Format("The date of onset of symptoms cannot occur after the date of death ({0}).", DateDeath.Value.ToShortDateString());
                        }
                        if (DateOnset.HasValue && DateDeath2.HasValue && DateOnset.Value > DateDeath2.Value)
                        {
                            errorMessage = String.Format("The date of onset of symptoms cannot occur after the final date of death ({0}).", DateDeath2.Value.ToShortDateString());
                        }
                        if (DateOnset.HasValue && DateOnset.Value > DateTime.Now)
                        {
                            errorMessage = "The date of onset cannot be greater than current date.";
                        }
                        break;
                    case "StatusReport":
                        if (FinalCaseStatus == "2" /* Alive */ && StatusReport == "1" /* Dead */)
                        {
                            errorMessage = "'Alive' was entered for the patient's final status, but the patient is listed as 'Dead' at the time of the initial report. Please verify the patient's status.";
                        }
                        break;
                    case "DateIsolationCurrent":
                        if (DateIsolationCurrent.HasValue && DateIsolationCurrent.Value > DateTime.Now)
                        {
                            errorMessage = "The date of admission to isolation cannot be greater than current date.";
                        }
                        else if (DateIsolationCurrent.HasValue && DateDeath2.HasValue && DateIsolationCurrent.Value > DateDeath2.Value)
                        {
                            errorMessage = String.Format("The date of admission to isolation cannot occur after the final date of death ({0}).", DateDeath2.Value.ToShortDateString());
                        }
                        else if (DateIsolationCurrent.HasValue && DateDeath.HasValue && DateIsolationCurrent.Value > DateDeath.Value)
                        {
                            errorMessage = String.Format("The date of admission to isolation cannot occur after the date of death ({0}).", DateDeath.Value.ToShortDateString());
                        }
                        break;
                    case "DateDeath2":
                        if (DateDeath2.HasValue && DateDeath2.Value > DateTime.Now)
                        {
                            errorMessage = "The date of death cannot be greater than current date.";
                        }
                        if (DateOnset.HasValue && DateDeath2.HasValue && DateOnset.Value > DateDeath2.Value)
                        {
                            errorMessage = String.Format("The date of onset of symptoms cannot occur after the final date of death ({0}).", DateDeath2.Value.ToShortDateString());
                        }
                        if (DateFuneral.HasValue && DateDeath2.HasValue && DateDeath2.Value > DateFuneral.Value)
                        {
                            errorMessage = "The date of death cannot occur after the date of the patient's funeral.";
                        }
                        else if (DateDeath.HasValue && DateDeath2.HasValue && DateDeath2.Value != DateDeath.Value)
                        {
                            errorMessage = String.Format("The date of death at the time of reporting ({0}) differs from the date of death entered for the patient's final outcome ({1}).", DateDeath.Value.ToShortDateString(), DateDeath2.Value.ToShortDateString());
                        }
                        break;
                    case "DateDischargeIso":
                        if (DateDischargeIso.HasValue && DateDischargeIso.Value > DateTime.Now)
                        {
                            errorMessage = "The date of isolation cannot be greater than current date.";
                        }
                        else if (DateDischargeIso.HasValue && DateHospitalCurrentAdmit.HasValue && DateHospitalCurrentAdmit.Value > DateDischargeIso.Value)
                        {
                            errorMessage = "The date of hospitalization should not exceed the date the patient was isolated.";
                        }
                        break;
                    case "DateHospitalCurrentAdmit":
                        if (DateHospitalCurrentAdmit.HasValue && DateHospitalCurrentAdmit.Value > DateTime.Now)
                        {
                            errorMessage = "The date of hospitalization cannot be greater than current date.";
                        }
                        else if (DateDischargeIso.HasValue && DateHospitalCurrentAdmit.HasValue && DateHospitalCurrentAdmit.Value > DateDischargeIso.Value)
                        {
                            errorMessage = "The date of hospitalization should not exceed the date the patient was isolated.";
                        }
                        break;
                    //case "HospitalizedCurrent":
                    //    if (String.IsNullOrEmpty(HospitalizedCurrent) || HospitalizedCurrent != "1")
                    //    {
                    //        DateHospitalCurrentAdmit = null;
                    //        CurrentHospital = String.Empty;
                    //        DistrictHosp = String.Empty;
                    //        CountryHosp = String.Empty;
                    //        VillageHosp = String.Empty;
                    //        SubCountyHosp = String.Empty;
                    //        IsolationCurrent = String.Empty;
                    //        DateIsolationCurrent = null;
                    //    }
                    //    break;
                    //case "HospitalizedPast":
                    //    if (String.IsNullOrEmpty(HospitalizedPast) || HospitalizedPast != "1")
                    //    {
                    //        DateHospitalPastStart1 = null;
                    //        DateHospitalPastStart2 = null;
                    //        DateHospitalPastEnd1 = null;
                    //        DateHospitalPastEnd2 = null;
                    //        HospitalPast1 = String.Empty;
                    //        HospitalPast2 = String.Empty;
                    //        HospitalDistrict1 = String.Empty;
                    //        HospitalDistrict2 = String.Empty;
                    //        HospitalVillage1 = String.Empty;
                    //        HospitalVillage2 = String.Empty;
                    //        IsolationPast1 = String.Empty;
                    //        IsolationPast2 = String.Empty;
                    //    }
                    //    break;
                    case "DateOutcomeInfoCompleted":
                        if (DateOutcomeInfoCompleted.HasValue && DateOutcomeInfoCompleted.Value > DateTime.Now)
                        {
                            errorMessage = "The date of completion for outcome information should not exceed today's date.";
                        }
                        break;
                    case "DateHospitalPastStart1":
                        if (DateHospitalPastStart1.HasValue && DateHospitalPastStart1.Value > DateTime.Now)
                        {
                            errorMessage = "The start date of a prior hospitalization should not exceed today's date.";
                        }
                        break;
                    case "DateHospitalPastEnd1":
                        if (DateHospitalPastEnd1.HasValue && DateHospitalPastStart1.HasValue && DateHospitalPastEnd1 < DateHospitalPastStart1)
                        {
                            errorMessage = "Date of discharge should be equal or greater than date of admission. Please review your date.";
                        }
                        break;
                    case "DateDischargeHospital":
                        if (DateDischargeHospital.HasValue && DateDischargeHospital.Value > DateTime.Now)
                        {
                            errorMessage = "The date of discharge from the hospital should not exceed today's date.";
                        }
                        else if (DateDischargeHospital.HasValue && DateDeath.HasValue && DateDeath.Value > DateDischargeHospital.Value)
                        {
                            errorMessage = "The date of discharge from the hospital should not exceed the date of the patient's death.";
                        }
                        else if (DateDischargeHospital.HasValue && DateDeath2.HasValue && DateDeath2.Value > DateDischargeHospital.Value)
                        {
                            errorMessage = "The date of discharge from the hospital should not exceed the date of the patient's death.";
                        }
                        else if (DateDischargeHospital.HasValue && DateOnset.HasValue && DateOnset.Value > DateDischargeHospital.Value)
                        {
                            errorMessage = "The date of onset of symptoms exceeds the date the patient was discharged from the hospital.";
                        }
                        break;
                    case "FuneralEndDate1":
                        if (FuneralEndDate1.HasValue && FuneralEndDate1.Value > DateTime.Now)
                        {
                            errorMessage = "The date of funeral attendance should not exceed today's date.";
                        }
                        break;
                    case "TravelStartDate":
                        if (TravelStartDate.HasValue && TravelStartDate.Value > DateTime.Now)
                        {
                            errorMessage = "The starting date of travelling should not exceed today's date.";
                        }
                        else if (TravelEndDate.HasValue && TravelStartDate.HasValue && TravelEndDate < TravelStartDate)
                        {
                            errorMessage = "The end date for travel should be equal or greater than the start date for travel. Please review your date.";
                        }
                        break;
                    case "TravelEndDate":
                        if (TravelEndDate.HasValue && TravelStartDate.HasValue && TravelEndDate < TravelStartDate)
                        {
                            errorMessage = "The end date for travel should be equal or greater than the start date for travel. Please review your date.";
                        }
                        break;
                    case "ContactDeathDate1":
                        if (ContactDeathDate1.HasValue && ContactDeathDate1.Value > DateTime.Now)
                        {
                            errorMessage = "The date of death for this individual should not exceed today's date.";
                        }
                        break;
                    case "ContactDeathDate2":
                        if (ContactDeathDate2.HasValue && ContactDeathDate2.Value > DateTime.Now)
                        {
                            errorMessage = "The date of death for this individual should not exceed today's date.";
                        }
                        break;
                    //case "TravelEndDate":
                    //    if (TravelEndDate.HasValue && TravelEndDate.Value > DateTime.Now)
                    //    {
                    //        errorMessage = "The ending date of travelling should not exceed today's date.";
                    //    }
                    //    break;
                    case "DateFuneral":
                        if (DateFuneral.HasValue && DateFuneral.Value > DateTime.Now)
                        {
                            errorMessage = "The date of the patient's funeral should not exceed today's date.";
                        }
                        if (DateFuneral.HasValue && DateOnset.HasValue && DateOnset.Value > DateFuneral.Value)
                        {
                            errorMessage = "The date of onset of symptoms cannot occur after the date of the patient's funeral.";
                        }
                        if (DateFuneral.HasValue && DateDeath.HasValue && DateDeath.Value > DateFuneral.Value)
                        {
                            errorMessage = "The date of death cannot occur after the date of the patient's funeral.";
                        }
                        if (DateFuneral.HasValue && DateDeath2.HasValue && DateDeath2.Value > DateFuneral.Value)
                        {
                            errorMessage = "The date of death cannot occur after the date of the patient's funeral.";
                        }
                        break;
                    //Fix for issue # 17109 
                    //Added error messaging for the check boxes.
                    case "RecordStatusComplete":
                        if (RecordStatusComplete == "1")
                        {
                            if (RecordStatusMissCRF == "1" ||
                                RecordStatusNoCRF == "1" ||
                                RecordStatusPenLab == "1" ||
                                RecordStatusPenOut == "1")
                            {
                                RecordStatusComplete = "0";
                                if (RecordStatusCompletePrevVal == "1")
                                {
                                    errorMessage = "Please uncheck the other record statuses first and then mark as Complete.";
                                }

                            }
                        }
                        break;
                    case "RecordStatusMissCRF":
                        if (RecordStatusMissCRF == "1")
                            RecordStatusComplete = "0";
                        break;
                    case "RecordStatusNoCRF":
                        if (RecordStatusNoCRF == "1")
                            RecordStatusComplete = "0";
                        break;
                    case "RecordStatusPenLab":
                        if (RecordStatusPenLab == "1")
                            RecordStatusComplete = "0";
                        break;
                    case "RecordStatusPenOut":
                        if (RecordStatusPenOut == "1")
                            RecordStatusComplete = "0";
                        break;
                    case "FinalCaseStatus":
                        if ((FinalStatus == Core.Enums.AliveDead.Alive && StatusReport == "1") || (FinalCaseStatus == "2" && StatusReport == "1"))
                        {
                            errorMessage = "'Alive' was entered for the patient's final status, but the patient is listed as 'Dead' at the time of the initial report. Please verify the patient's status.";
                        }

                        if (FinalStatus == Core.Enums.AliveDead.Alive)
                        {
                            DateDeath2 = null;
                            //PlaceDeath = String.Empty;
                            //DateFuneral = null;
                            //FuneralConductedFam = false;
                            //FuneralConductedOutbreakTeam = false;
                        }
                        else
                        {
                            //HospitalDischarge = String.Empty;
                            //HospitalDischargeDistrict = String.Empty;
                            //DateDischargeHospital = null;
                            //DateDischargeIso = null;
                        }
                        break;
                }

                if (ContactStatus1 != "1" && ContactStatus1 != "3")
                {
                    // ContactDeathDate1 = null; 17134
                }

                if (ContactStatus2 != "1" && ContactStatus2 != "3")
                {
                    // ContactDeathDate2 = null; 17134
                }

                if (IsHCW.HasValue == false || IsHCW.Value == false)
                {
                    OccupationHCWPosition = String.Empty;
                    OccupationHCWFacility = String.Empty;
                }

                if (String.IsNullOrEmpty(HospitalizedCurrent) || HospitalizedCurrent != "1")
                {
                    //DateHospitalCurrentAdmit = null;
                    //CurrentHospital = String.Empty;
                    //DistrictHosp = String.Empty;
                    //CountryHosp = String.Empty;
                    //VillageHosp = String.Empty;
                    //SubCountyHosp = String.Empty;
                    //IsolationCurrent = String.Empty;
                    //DateIsolationCurrent = null;
                }

                if (String.IsNullOrEmpty(IsolationCurrent) || IsolationCurrent != "1")
                {
                    //DateIsolationCurrent = null;
                }

                if (String.IsNullOrEmpty(HospitalizedPast) || HospitalizedPast != "1")
                {
                    //DateHospitalPastStart1 = null;
                    //DateHospitalPastStart2 = null;
                    //DateHospitalPastEnd1 = null;
                    //DateHospitalPastEnd2 = null;
                    //HospitalPast1 = String.Empty;
                    //HospitalPast2 = String.Empty;
                    //HospitalDistrict1 = String.Empty;
                    //HospitalDistrict2 = String.Empty;
                    //HospitalVillage1 = String.Empty;
                    //HospitalVillage2 = String.Empty;
                    //IsolationPast1 = String.Empty;
                    //IsolationPast2 = String.Empty;
                }

                if (String.IsNullOrEmpty(HadContact) || HadContact != "1")
                {
                    //ContactName1 = String.Empty;
                    //ContactName2 = String.Empty;
                    //ContactName3 = String.Empty;

                    //ContactStartDate1 = null;
                    //ContactStartDate2 = null;
                    //ContactStartDate3 = null;

                    //ContactRelation1 = String.Empty;
                    //ContactRelation2 = String.Empty;
                    //ContactRelation3 = String.Empty;

                    //ContactEndDate1 = null;
                    //ContactEndDate2 = null;
                    //ContactEndDate3 = null;

                    //ContactVillage1 = String.Empty;
                    //ContactVillage2 = String.Empty;
                    //ContactVillage3 = String.Empty;

                    //ContactDistrict1 = String.Empty;
                    //ContactDistrict2 = String.Empty;
                    //ContactDistrict3 = String.Empty;

                    //ContactStatus1 = String.Empty;
                    //ContactStatus2 = String.Empty;
                    //ContactStatus3 = String.Empty;

                    //ContactDeathDate1 = null;
                    //ContactDeathDate2 = null;
                    //ContactDeathDate3 = null;
                }
                //if (String.IsNullOrEmpty(AttendFuneral) || AttendFuneral != "1")
                //{
                //    FuneralNameDeceased1 = String.Empty;
                //    FuneralNameDeceased2 = String.Empty;

                //    FuneralVillage1 = String.Empty;
                //    FuneralVillage2 = String.Empty;

                //    FuneralStartDate1 = null;
                //    FuneralStartDate2 = null;

                //    FuneralEndDate1 = null;
                //    FuneralEndDate2 = null;

                //    FuneralRelationDeceased1 = String.Empty;
                //    FuneralRelationDeceased2 = String.Empty;

                //    FuneralDistrict1 = String.Empty;
                //    FuneralDistrict2 = String.Empty;

                //    FuneralTouchBody1 = String.Empty;
                //    FuneralTouchBody2 = String.Empty;
                //}
                //if (String.IsNullOrEmpty(Travel) || Travel != "1")
                //{
                //    TravelVillage = String.Empty;
                //    TravelDistrict = String.Empty;
                //    TravelCountry = String.Empty;
                //    TravelDateEstimated = false;
                //    TravelStartDate = null;
                //    TravelEndDate = null;
                //}
                if (String.IsNullOrEmpty(SymptomOtherNonHemorrhagic) || SymptomOtherNonHemorrhagic != "1")
                {
                    SymptomOtherNonHemorrhagicSpecify = String.Empty;
                }
                if (String.IsNullOrEmpty(SymptomOtherHemo) || SymptomOtherHemo != "1")
                {
                    SymptomOtherHemoSpecify = String.Empty;
                }

                if (SymptomOtherHemoFinal != "1")
                {
                    SymptomOtherHemoFinalSpecify = String.Empty;
                }

                if (String.IsNullOrEmpty(InterviewerInfoProvidedBy) || InterviewerInfoProvidedBy != "2")
                {
                    // if not proxy, clear proxy fields
                    ProxyName = String.Empty;
                    ProxyRelation = String.Empty;
                }

                if (!StatusReport.Equals("1")) // alive
                {
                    DateDeath = null;
                }

                if (FinalStatus == Core.Enums.AliveDead.Alive)
                {
                    DateDeath2 = null;
                    //PlaceDeath = String.Empty;
                    //DateFuneral = null;
                    //FuneralConductedFam = false;
                    //FuneralConductedOutbreakTeam = false;
                }
                else
                {
                    //HospitalDischarge = String.Empty;
                    //HospitalDischargeDistrict = String.Empty;
                    //DateDischargeHospital = null;
                    //DateDischargeIso = null;
                }

                SetCurrentStatus();

                return errorMessage;
            }
        }

        public ObservableCollection<FieldValueChange> FieldValueChanges { get; set; }

        public ObservableCollection<ContactViewModel> Contacts
        {
            get { return _contacts; }
            private set
            {
                if (_contacts != value)
                {
                    _contacts = value;
                    RaisePropertyChanged("Contacts");
                }
            }
        }
        public string RecordId
        {
            get { return _recordId; }
            set
            {
                if (_recordId != value)
                {
                    _recordId = value;
                    RaisePropertyChanged("RecordId");
                }
            }
        }
        public string IDForSorting 
        {
            get
            {
                if (CaseViewModel.IsCountryUS)
                {
                    return OriginalID;
                }
                else
                {
                    return ID;
                }
            }
        }
        public string OriginalCaseID { get; private set; }

        public string OriginalID 
        { 
            get { return _OriginalID; } 
            set 
            { 
                if (_OriginalID != value) 
                {
                    if (CaseViewModel.IsCountryUS && CaseSecondaryIDChanging != null)
                    {
                        CaseSecondaryIDChanging(this, new FieldValueChangingEventArgs(_id, value));
                    }

                    _OriginalID = value; 
                    RaisePropertyChanged("OriginalID"); 
                    RaisePropertyChanged("IDForSorting"); 
                } 
            } 
        }

        public string ID
        {
            get
            {
                return _id;
            }
            set
            {
                if (_id != value)
                {
                    if (CaseIDChanging != null)
                    {
                        CaseIDChanging(this, new FieldValueChangingEventArgs(_id, value));
                    }
                    _id = value;
                    RaisePropertyChanged("ID");
                    RaisePropertyChanged("IDForSorting");
                }
            }
        }
        public string Surname
        {
            get { return _surname; }
            set
            {
                if (_surname != value)
                {
                    _surname = value;
                    RaisePropertyChanged("Surname");
                }
            }
        }
        public string OtherNames
        {
            get { return _otherNames; }
            set
            {
                if (_otherNames != value)
                {
                    _otherNames = value;
                    RaisePropertyChanged("OtherNames");
                }
            }
        }
        public Core.Enums.EpiCaseClassification EpiCaseDef
        {
            get { return _epiCaseDef; }
            private set
            {
                if (_epiCaseDef != value)
                {
                    if (EpiCaseDefinitionChanging != null)
                    {
                        EpiCaseDefinitionChanging(this, new EpiCaseDefinitionChangingEventArgs(_epiCaseDef, value));
                    }

                    _epiCaseDef = value;
                    RaisePropertyChanged("EpiCaseDef");

                    if (_epiCaseDef.Equals(Core.Enums.EpiCaseClassification.Confirmed))
                    {
                        EpiCaseClassification = "1";
                    }
                    else if (_epiCaseDef.Equals(Core.Enums.EpiCaseClassification.Probable))
                    {
                        EpiCaseClassification = "2";
                    }
                    else if (_epiCaseDef.Equals(Core.Enums.EpiCaseClassification.Suspect))
                    {
                        EpiCaseClassification = "3";
                    }
                    else if (_epiCaseDef.Equals(Core.Enums.EpiCaseClassification.Excluded))
                    {
                        EpiCaseClassification = "4";
                    }
                    else if (_epiCaseDef.Equals(Core.Enums.EpiCaseClassification.PUI))
                    {
                        EpiCaseClassification = "5";
                    }
                    else if (_epiCaseDef.Equals(Core.Enums.EpiCaseClassification.NotCase))
                    {
                        EpiCaseClassification = "0";
                    }
                    else
                    {
                        EpiCaseClassification = String.Empty;
                    }
                }
            }
        }

        private Core.Enums.EpiCaseClassification OriginalEpiCaseDef { get; set; }

        public string EpiCaseClassification
        {
            get { return _epiCaseClassification; }
            set
            {
                if (_epiCaseClassification != value)
                {
                    _epiCaseClassification = value;
                    RaisePropertyChanged("EpiCaseClassification");

                    switch (_epiCaseClassification)
                    {
                        case "1":
                            EpiCaseDef = Core.Enums.EpiCaseClassification.Confirmed; // "Confirmed";
                            break;
                        case "2":
                            EpiCaseDef = Core.Enums.EpiCaseClassification.Probable; // "Probable";
                            break;
                        case "3":
                            EpiCaseDef = Core.Enums.EpiCaseClassification.Suspect; // "Suspect";
                            break;
                        case "4":
                            EpiCaseDef = Core.Enums.EpiCaseClassification.Excluded; // "Excluded";
                            break;
                        case "5":
                            EpiCaseDef = Core.Enums.EpiCaseClassification.PUI; // "Person Under Investigation";
                            break;
                        case "0":
                            EpiCaseDef = Core.Enums.EpiCaseClassification.NotCase; // "Not a case";
                            break;
                        default:
                            EpiCaseDef = Core.Enums.EpiCaseClassification.None;
                            break;
                    }

                    if (EpiCaseDef != Core.Enums.EpiCaseClassification.None)
                    {
                        SuppressCriticalErrors = false;
                    }
                }
            }
        }

        //Fix for issue # 17109 Starts
        //Added following properties in ViewModel corresponding to each CheckBox and each field in data table.

        string RecordStatusCompletePrevVal = string.Empty;
        public string RecordStatusComplete
        {
            get { return _recordStatusComplete; }
            set
            {
                //if (_recordStatusComplete != value)
                //{
                RecordStatusCompletePrevVal = _recordStatusComplete;
                _recordStatusComplete = value;
                //if (value != "0")
                //{
                RecordStatus = RecComplete;
                //}

                RaisePropertyChanged("RecordStatusComplete");
                //}
            }
        }

        public string RecordStatusNoCRF
        {
            get { return _recordStatusNoCRF; }
            set
            {
                if (_recordStatusNoCRF != value)
                {
                    _recordStatusNoCRF = value;
                    if (value == "1")
                    {
                        RecordStatusComplete = "0";
                    }
                    RecordStatus = RecNoCRF;
                    RaisePropertyChanged("RecordStatusNoCRF");
                }
            }
        }
        public string RecordStatusMissCRF
        {
            get { return _recordStatusMissCRF; }
            set
            {
                if (_recordStatusMissCRF != value)
                {
                    _recordStatusMissCRF = value;
                    if (value == "1")
                    {
                        RecordStatusComplete = "0";
                    }
                    RecordStatus = RecMissCRF;
                    RaisePropertyChanged("RecordStatusMissCRF");
                }
            }
        }

        public string RecordStatusPenLab
        {
            get { return _recordStatusPenLab; }
            set
            {
                if (_recordStatusPenLab != value)
                {
                    _recordStatusPenLab = value;
                    if (value == "1")
                    {
                        RecordStatusComplete = "0";
                    }
                    RecordStatus = RecPendingLab;
                    RaisePropertyChanged("RecordStatusPenLab");
                }
            }
        }

        public string RecordStatusPenOut
        {
            get { return _recordStatusPenOut; }
            set
            {
                if (_recordStatusPenOut != value)
                {
                    _recordStatusPenOut = value;
                    if (value == "1")
                    {
                        RecordStatusComplete = "0";
                    }
                    RecordStatus = RecPendingOutcome;
                    RaisePropertyChanged("RecordStatusPenOut");
                }
            }
        }



        public string RecordStatus
        {
            get { return _recordStatus; }
            set
            {
                if (_recordStatus != value)
                {


                    WordBuilder wb = new WordBuilder(";");
                    if (RecordStatusNoCRF == "1" || RecordStatusMissCRF == "1" || RecordStatusPenLab == "1" || RecordStatusPenOut == "1")
                    {
                        if (RecordStatusNoCRF == "1")
                        {
                            wb.Add(RecNoCRF /*"No CRF"*/);
                        }
                        if (RecordStatusMissCRF == "1")
                        {
                            wb.Add(RecMissCRF /*"Missing CRF info"*/);
                        }
                        if (RecordStatusPenLab == "1")
                        {
                            wb.Add(RecPendingLab /*"Pending lab"*/);
                        }
                        if (RecordStatusPenOut == "1")
                        {
                            wb.Add(RecPendingOutcome /*"Pending outcome"*/);
                        }
                    }
                    else
                    {
                        wb.Add(RecComplete);// "Complete");
                    }
                    _recordStatus = wb.ToString();


                    RaisePropertyChanged("RecordStatus");
                }
            }
        }
        public DateTime? DateReport
        {
            get { return _dateReport; }
            set
            {
                if (_dateReport != value)
                {
                    _dateReport = value;
                    RaisePropertyChanged("DateReport");
                }
            }
        }

        private DateTime? _dob = null;

        public DateTime? DOB
        {
            get { return _dob; }
            set { _dob = value; }
        }

        private string email = string.Empty;

        public string Email
        {
            get { return email; }
            set { email = value; }
        }

        private string occupationHCWDistrict = string.Empty;

        public string OccupationHCWDistrict
        {
            get { return occupationHCWDistrict; }
            set { occupationHCWDistrict = value; }
        }

        private string occupationHCWSC = string.Empty;

        public string OccupationHCWSC
        {
            get { return occupationHCWSC; }
            set { occupationHCWSC = value; }
        }

        private string occupationHCWVillage = string.Empty;

        public string OccupationHCWVillage
        {
            get { return occupationHCWVillage; }
            set { occupationHCWVillage = value; }
        }


        public string Sex
        {
            get { return _sex; }
            set
            {
                if (_sex != value)
                {
                    _sex = value;
                    RaisePropertyChanged("Sex");

                    switch (_sex)
                    {
                        case "1":
                            Gender = Core.Enums.Gender.Male;
                            break;
                        case "2":
                            Gender = Core.Enums.Gender.Female;
                            break;
                        default:
                            Gender = Core.Enums.Gender.None;
                            break;
                    }
                }
            }
        }
        public Core.Enums.Gender Gender
        {
            get { return _gender; }
            private set
            {
                if (_gender != value)
                {
                    _gender = value;
                    RaisePropertyChanged("Gender");

                    if (_gender.Equals(Core.Enums.Gender.Male))
                    {
                        Sex = "1";
                    }
                    else if (_gender.Equals(Core.Enums.Gender.Female))
                    {
                        Sex = "2";
                    }
                    else
                    {
                        Sex = String.Empty;
                    }
                }
            }
        }
        public double? Age
        {
            get { return _age; }
            set
            {
                if (_age != value)
                {
                    this._age = value;

                    CalcAge();

                    RaisePropertyChanged("Age");
                    RaisePropertyChanged("AgeYears");
                }
            }
        }
        public double? AgeYears
        {
            get { return _ageYears; }
            private set
            {
                this._ageYears = value;
            }
        }
        public AgeUnits? AgeUnit
        {
            get { return _ageUnit; }
            set
            {
                if (_ageUnit != value)
                {
                    _ageUnit = value;

                    //if (_ageUnit.HasValue)
                    //{
                    //    if (_ageUnit.Value == AgeUnits.Months)
                    //    {
                    //        AgeUnitString = "Months";
                    //    }
                    //    else
                    //    {
                    //        AgeUnitString = "Years";
                    //    }
                    //}
                    //else
                    //{
                    //    AgeUnitString = String.Empty;
                    //}

                    RaisePropertyChanged("AgeUnit");
                    RaisePropertyChanged("AgeUnitString");
                }
            }
        }
        public string AgeUnitString
        {
            get
            {
                if (AgeUnit.HasValue)
                {
                    if (AgeUnit.Value.Equals(AgeUnits.Months))
                    {
                        return Months;
                    }
                    else
                    {
                        return Years;
                    }
                }
                else
                {
                    return String.Empty;
                }
            }
            set
            {
                if (value != AgeUnitString)
                {
                    if (value.Equals(Months, StringComparison.OrdinalIgnoreCase))
                    {
                        AgeUnit = AgeUnits.Months;
                    }
                    else if (value.Equals(Years, StringComparison.OrdinalIgnoreCase))
                    {
                        AgeUnit = AgeUnits.Years;
                    }
                    else
                    {
                        AgeUnit = null;
                    }

                    CalcAge();

                    RaisePropertyChanged("Age");
                    RaisePropertyChanged("AgeYears");
                    RaisePropertyChanged("AgeUnit");
                    RaisePropertyChanged("AgeUnitString");
                }
            }
        }
        public DateTime? DateOnset
        {
            get { return _dateOnset; }
            set
            {
                if (_dateOnset != value)
                {
                    _dateOnset = value;
                    RaisePropertyChanged("DateOnset");
                }
            }
        }
        public string HeadOfHousehold
        {
            get { return _headOfHousehold; }
            set
            {
                if (_headOfHousehold != value)
                {
                    _headOfHousehold = value;
                    RaisePropertyChanged("HeadOfHousehold");
                }
            }
        }
        public string Village
        {
            get { return _Village; }
            set
            {
                if (_Village != value)
                {
                    _Village = value;
                    if (_isOnsetLocationSameAsResidence)
                    {
                        VillageOnset = value;
                    }
                    RaisePropertyChanged("Village");
                }
            }
        }
        public string VillageOnset
        {
            get { return _VillageOnset; }
            set
            {
                if (_VillageOnset != value)
                {
                    _VillageOnset = value;
                    RaisePropertyChanged("VillageOnset");
                }
            }
        }
        public string VillageHosp
        {
            get { return _VillageHosp; }
            set
            {
                if (_VillageHosp != value)
                {
                    _VillageHosp = value;
                    RaisePropertyChanged("VillageHosp");
                }
            }
        }
        public string SubCounty
        {
            get { return _SubCounty; }
            set
            {
                if (_SubCounty != value)
                {
                    if (_isOnsetLocationSameAsResidence)
                    {
                        SubCountyOnset = value;
                    }
                    _SubCounty = value;
                    RaisePropertyChanged("SubCounty");
                }
            }
        }
        public string Parish
        {
            get { return _Parish; }
            set
            {
                if (_Parish != value)
                {
                    _Parish = value;
                    RaisePropertyChanged("Parish");
                }
            }
        }
        public string SubCountyOnset
        {
            get { return _SubCountyOnset; }
            set
            {
                if (_SubCountyOnset != value)
                {
                    _SubCountyOnset = value;
                    RaisePropertyChanged("SubCountyOnset");
                }
            }
        }
        public string SubCountyHosp
        {
            get { return _SubCountyHosp; }
            set
            {
                if (_SubCountyHosp != value)
                {
                    _SubCountyHosp = value;
                    RaisePropertyChanged("SubCountyHosp");
                }
            }
        }
        public string CountryHosp
        {
            get { return _CountryHosp; }
            set
            {
                if (_CountryHosp != value)
                {
                    _CountryHosp = value;
                    RaisePropertyChanged("CountryHosp");
                }
            }
        }
        public string District
        {
            get { return _District; }
            set
            {
                if (_District != value)
                {
                    _District = value;
                    if (_isOnsetLocationSameAsResidence)
                    {
                        DistrictOnset = value;
                    }
                    RaisePropertyChanged("District");
                }
            }
        }

        public string DistrictOnset
        {
            get { return _DistrictOnset; }
            set
            {
                if (_DistrictOnset != value)
                {
                    _DistrictOnset = value;
                    RaisePropertyChanged("DistrictOnset");
                }
            }
        }
        public string DistrictHosp
        {
            get { return _DistrictHosp; }
            set
            {
                if (_DistrictHosp != value)
                {
                    _DistrictHosp = value;
                    RaisePropertyChanged("DistrictHosp");
                }
            }
        }
        public string Country
        {
            get { return _Country; }
            set
            {
                if (_Country != value)
                {
                    _Country = value;
                    if (_isOnsetLocationSameAsResidence)
                    {
                        CountryOnset = value;
                    }
                    RaisePropertyChanged("Country");
                }
            }
        }

        public string AddressRes
        {
            get { return _AddressRes; }
            set
            {
                if (_AddressRes != value)
                {
                    _AddressRes = value;
                    RaisePropertyChanged("AddressRes");
                }
            }
        }

        public string ZipRes
        {
            get { return _ZipRes; }
            set
            {
                if (_ZipRes != value)
                {
                    _ZipRes = value;
                    RaisePropertyChanged("ZipRes");
                }
            }
        }

        //17178
        public string Citizenship
        {
            get { return _Citizenship; }
            set
            {
                _Citizenship = value;
                RaisePropertyChanged("Citizenship");
            }
        }

        //private bool _isCountryUS;

        //public bool IsCountryUS
        //{
        //    get { return _isCountryUS; }
        //    set { _isCountryUS = value; }
        //}


        public string CountryOnset
        {
            get { return _CountryOnset; }
            set
            {
                if (_CountryOnset != value)
                {
                    _CountryOnset = value;
                    RaisePropertyChanged("CountryOnset");
                }
            }
        }
        public DateTime? DateIsolationCurrent
        {
            get { return _dateIsolationCurrent; }
            set
            {
                if (_dateIsolationCurrent != value)
                {
                    _dateIsolationCurrent = value;
                    RaisePropertyChanged("DateIsolationCurrent");
                    RaisePropertyChanged("DateDeath");
                    RaisePropertyChanged("DateDeath2");
                }
            }
        }
        public DateTime? DateDischargeIso
        {
            get { return _dateDischargeIso; }
            set
            {
                if (_dateDischargeIso != value)
                {
                    _dateDischargeIso = value;
                    RaisePropertyChanged("DateDischargeIso");
                }
            }
        }
        public DateTime? DateHospitalCurrentAdmit
        {
            get { return _DateHospitalCurrentAdmit; }
            set
            {
                if (_DateHospitalCurrentAdmit != value)
                {
                    _DateHospitalCurrentAdmit = value;
                    RaisePropertyChanged("DateHospitalCurrentAdmit");
                }
            }
        }
        public DateTime? DateDeath
        {
            get { return _DateDeath; }
            set
            {
                if (_DateDeath != value)
                {
                    _DateDeath = value;
                    RaisePropertyChanged("DateDeath");
                    RaisePropertyChanged("DateDeathCurrentOrFinal");
                    RaisePropertyChanged("DateIsolationCurrent");
                    RaisePropertyChanged("DateDeath2");

                    //if (DateDeath.HasValue && !DateDeath2.HasValue)//Fix for Issue 17118.
                    //{
                    //    DateDeath2 = DateDeath;
                    //}
                }
            }
        }

        public bool ByPassEpiCaseClassificationValidation { get; set; }
        public bool DateDeathEst { get { return _DateDeathEst; } set { if (_DateDeathEst != value) { _DateDeathEst = value; RaisePropertyChanged("DateDeathEst"); } } }
        public bool DateOnsetEst { get { return _DateOnsetEst; } set { if (_DateOnsetEst != value) { _DateOnsetEst = value; RaisePropertyChanged("DateOnsetEst"); } } }
        public string DateOnsetEstExplain { get { return _DateOnsetEstExplain; } set { if (_DateOnsetEstExplain != value) { _DateOnsetEstExplain = value; RaisePropertyChanged("DateOnsetEstExplain"); } } }


        public string FinalCaseStatus
        {
            get { return _finalCaseStatus; }
            set
            {
                if (_finalCaseStatus != value)
                {
                    _finalCaseStatus = value;
                    RaisePropertyChanged("FinalCaseStatus");

                    switch (_finalCaseStatus)
                    {
                        case "1":
                            FinalStatus = Core.Enums.AliveDead.Dead;
                            break;
                        case "2":
                            FinalStatus = Core.Enums.AliveDead.Alive;
                            DateDeath2 = null;
                            RaisePropertyChanged("DateDeath2");
                            break;
                        default:
                            FinalStatus = Core.Enums.AliveDead.None;
                            DateDeath2 = null;
                            RaisePropertyChanged("DateDeath2");
                            break;
                    }

                    SetCurrentStatus();

                    RaisePropertyChanged("StatusReport");
                }
            }
        }

        public Core.Enums.AliveDead InitialStatus
        {
            get { return _initialStatus; }
            set
            {
                _initialStatus = value;
                RaisePropertyChanged("InitialStatus");

                if (_initialStatus.Equals(Core.Enums.AliveDead.Dead))
                {
                    StatusReport = "1";
                }
                else if (_initialStatus.Equals(Core.Enums.AliveDead.Alive))
                {
                    StatusReport = "2";
                }
                else
                {
                    StatusReport = String.Empty;
                }
            }
        }


        public Core.Enums.AliveDead FinalStatus
        {
            get { return _finalStatus; }
            private set
            {
                if (_finalStatus != value)
                {
                    _finalStatus = value;
                    RaisePropertyChanged("FinalStatus");

                    if (_finalStatus.Equals(Core.Enums.AliveDead.Dead))
                    {
                        FinalCaseStatus = "1";
                    }
                    else if (_finalStatus.Equals(Core.Enums.AliveDead.Alive))
                    {
                        FinalCaseStatus = "2";
                    }
                    else
                    {
                        FinalCaseStatus = String.Empty;
                    }
                }
            }
        }
        public string CurrentStatus
        {
            get { return _CurrentStatus; }
            set
            {
                if (_CurrentStatus != value)
                {
                    _CurrentStatus = value;
                    RaisePropertyChanged("CurrentStatus");
                }
            }
        }

        //public string LocalizedCurrentStatus
        //{
        //    get 
        //    {
        //        if (System.Threading.Thread.CurrentThread.CurrentCulture.ToString().Contains("fr"))
        //        {
        //            switch (CurrentStatus)
        //            {
        //                case "Alive":
        //                    return "Vivant";
        //                case "Dead":
        //                    return "Décédé";
        //                default:
        //                    return CurrentStatus;
        //            }
        //        }
        //        else
        //        {
        //            return CurrentStatus;
        //        }
        //    }
        //}

        public Core.Enums.FinalLabClassification FinalLabClass
        {
            get { return _finalLabClass; }
            private set
            {
                if (_finalLabClass != value)
                {
                    _finalLabClass = value;
                    RaisePropertyChanged("FinalLabClass");

                    if (_finalLabClass == Core.Enums.FinalLabClassification.NotCase)
                    {
                        FinalLabClassification = "0";
                    }
                    else if (_finalLabClass == Core.Enums.FinalLabClassification.ConfirmedAcute)
                    {
                        FinalLabClassification = "1";
                    }
                    else if (_finalLabClass == Core.Enums.FinalLabClassification.ConfirmedConvalescent)
                    {
                        FinalLabClassification = "2";
                    }
                    else if (_finalLabClass == Core.Enums.FinalLabClassification.Indeterminate)
                    {
                        FinalLabClassification = "3";
                    }
                    else if (_finalLabClass == Core.Enums.FinalLabClassification.NeedsFollowUpSample)
                    {
                        FinalLabClassification = "4";
                    }
                    else
                    {
                        FinalLabClassification = String.Empty;
                    }
                }
            }
        }
        public string FinalLabClassification
        {
            get { return _finalLabClassification; }
            set
            {
                if (_finalLabClassification != value)
                {
                    _finalLabClassification = value;
                    RaisePropertyChanged("FinalLabClassification");

                    switch (_finalLabClassification)
                    {
                        case "0":
                            FinalLabClass = Core.Enums.FinalLabClassification.NotCase;// "Not a Case";
                            break;
                        case "1":
                            FinalLabClass = Core.Enums.FinalLabClassification.ConfirmedAcute;// "Confirmed Acute";
                            break;
                        case "2":
                            FinalLabClass = Core.Enums.FinalLabClassification.ConfirmedConvalescent;// "Confirmed Convalescent";
                            break;
                        case "3":
                            FinalLabClass = Core.Enums.FinalLabClassification.Indeterminate;
                            break;
                        case "4":
                            FinalLabClass = Core.Enums.FinalLabClassification.NeedsFollowUpSample;
                            break;
                        default:
                            FinalLabClass = Core.Enums.FinalLabClassification.None;
                            break;
                    }
                }
            }
        }
        public string IsolationCurrent
        {
            get { return _IsolationCurrent; }
            set
            {
                if (_IsolationCurrent != value)
                {
                    _IsolationCurrent = value;
                    RaisePropertyChanged("IsolationCurrent");
                }
            }
        }
        public bool IsActive
        {
            get { return true; }
        }
        public bool IsContact
        {
            get { return _IsContact; }
            set
            {
                if (_IsContact != value)
                {
                    _IsContact = value;
                    RaisePropertyChanged("IsContact");
                    RaisePropertyChanged("IsContactSymbol");
                }
            }
        }

        //public string IsContactSymbol { get { return _IsContactSymbol; } }

        public bool? IsHCW
        {
            get { return _IsHCW; }
            set
            {
                if (_IsHCW != value)
                {
                    _IsHCW = value;
                    RaisePropertyChanged("IsHCW");
                }
            }
        }

        public bool IsOtherOccupation
        {
            get
            {
                return this._isOtherOccupation;
            }
            set
            {
                if (IsOtherOccupation != value)
                {
                    this._isOtherOccupation = value;
                    RaisePropertyChanged("IsOtherOccupation");
                }
            }
        }

        public bool IsOnsetLocationSameAsResidence
        {
            get
            {
                return this._isOnsetLocationSameAsResidence;
            }
            set
            {
                if (IsOnsetLocationSameAsResidence != value)
                {
                    this._isOnsetLocationSameAsResidence = value;
                    RaisePropertyChanged("IsOnsetLocationSameAsResidence");
                    if (this._isOnsetLocationSameAsResidence == true)
                    {
                        CountryOnset = Country;
                        DistrictOnset = District;
                        SubCountyOnset = SubCounty;
                        VillageOnset = Village;
                    }
                }
            }
        }
        public string PlaceOfDeathLocalized
        {
            get { return _PlaceOfDeathLocalized; }
            private set
            {
                if (_PlaceOfDeathLocalized != value)
                {
                    _PlaceOfDeathLocalized = value;
                    RaisePropertyChanged("PlaceOfDeathLocalized");

                    if (_PlaceOfDeathLocalized == PlaceDeathCommunityValue)
                    {
                        PlaceDeath = "1";
                    }
                    else if (_PlaceOfDeathLocalized == PlaceDeathHospitalValue)
                    {
                        PlaceDeath = "2";
                    }
                    else if (_PlaceOfDeathLocalized == PlaceDeathOtherValue)
                    {
                        PlaceDeath = "3";
                    }
                    else
                    {
                        PlaceDeath = String.Empty;
                    }
                }
            }
        }
        public string PlaceOfDeath
        {
            get { return _PlaceOfDeath; }
            set
            {
                if (_PlaceOfDeath != value)
                {
                    _PlaceOfDeath = value;
                    RaisePropertyChanged("PlaceOfDeath");

                    switch (_PlaceOfDeath)
                    {
                        case "1":
                            PlaceOfDeathLocalized = PlaceDeathCommunityValue;// "Community";
                            break;
                        case "2":
                            PlaceOfDeathLocalized = PlaceDeathHospitalValue;// "Hospital";
                            break;
                        case "3":
                            PlaceOfDeathLocalized = PlaceDeathOtherValue;// "Other";
                            break;
                        default:
                            PlaceOfDeathLocalized = String.Empty;
                            break;
                    }
                }
            }
        }
        public DateTime? DateLastLabSampleTested
        {
            get { return _DateLastLabSampleTested; }
            set
            {
                if (_DateLastLabSampleTested != value)
                {
                    _DateLastLabSampleTested = value;
                    RaisePropertyChanged("DateLastLabSampleTested");
                }
            }
        }
        public DateTime? DateLastLabSampleCollected
        {
            get { return _DateLastLabSampleCollected; }
            set
            {
                if (_DateLastLabSampleCollected != value)
                {
                    _DateLastLabSampleCollected = value;
                    RaisePropertyChanged("DateLastLabSampleCollected");
                }
            }
        }
        public string LastSampleInterpret
        {
            get { return _lastSampleInterpret; }
            private set
            {
                if (_lastSampleInterpret != value)
                {
                    _lastSampleInterpret = value;
                    RaisePropertyChanged("LastSampleInterpret");

                    switch (_lastSampleInterpret)
                    {
                        case "1":
                            LastSampleInterpretation = Core.Enums.SampleInterpretation.ConfirmedAcute;
                            break;
                        case "2":
                            LastSampleInterpretation = Core.Enums.SampleInterpretation.ConfirmedConvalescent;
                            break;
                        case "3":
                            LastSampleInterpretation = Core.Enums.SampleInterpretation.Negative;
                            break;
                        case "4":
                            LastSampleInterpretation = Core.Enums.SampleInterpretation.Indeterminate;
                            break;
                        case "5":
                            LastSampleInterpretation = Core.Enums.SampleInterpretation.NegativeNeedFollowUp;
                            break;
                        default:
                            LastSampleInterpretation = Core.Enums.SampleInterpretation.None;
                            break;
                    }
                }
            }
        }
        public Core.Enums.SampleInterpretation LastSampleInterpretation
        {
            get { return _lastSampleInterpretation; }
            private set
            {
                if (_lastSampleInterpretation != value)
                {
                    _lastSampleInterpretation = value;
                    RaisePropertyChanged("LastSampleInterpretation");

                    if (_lastSampleInterpretation == Core.Enums.SampleInterpretation.ConfirmedAcute)
                    {
                        LastSampleInterpret = "1";
                    }
                    else if (_lastSampleInterpretation == Core.Enums.SampleInterpretation.ConfirmedConvalescent)
                    {
                        LastSampleInterpret = "2";
                    }
                    else if (_lastSampleInterpretation == Core.Enums.SampleInterpretation.Negative)
                    {
                        LastSampleInterpret = "3";
                    }
                    else if (_lastSampleInterpretation == Core.Enums.SampleInterpretation.Indeterminate)
                    {
                        LastSampleInterpret = "4";
                    }
                    else if (_lastSampleInterpretation == Core.Enums.SampleInterpretation.NegativeNeedFollowUp)
                    {
                        LastSampleInterpret = "5";
                    }
                    else
                    {
                        LastSampleInterpret = String.Empty;
                    }
                }
            }
        }
        public string LastSamplePCRResult
        {
            get { return _LastSamplePCRResult; }
            set
            {
                if (_LastSamplePCRResult != value)
                {
                    _LastSamplePCRResult = value;
                    RaisePropertyChanged("LastSamplePCRResult");
                }
            }
        }
        public string CurrentHospital
        {
            get { return _CurrentHospital; }
            set
            {
                if (_CurrentHospital != value)
                {
                    _CurrentHospital = value;
                    RaisePropertyChanged("CurrentHospital");
                }
            }
        }
        public string GenderAbbreviation
        {
            get
            {
                if (this.Gender == Core.Enums.Gender.Female) { return CaseViewModel.FemaleAbbr; }
                else if (this.Gender == Core.Enums.Gender.Male) { return CaseViewModel.MaleAbbr; }
                else return String.Empty;
            }
        }

        public int? EpiWeekOnset
        {
            get
            {
                if (DateOnset.HasValue)
                {
                    DateTime MMWR__Start;
                    MMWR__Start = Core.Common.GetMMWRStart(DateOnset.Value, 1);

                    TimeSpan MMWR__DayCount = DateOnset.Value.Subtract(MMWR__Start);
                    int MMWR__Week = ((int)(MMWR__DayCount.Days / 7)) + 1;

                    return MMWR__Week;
                }
                else
                {
                    return null;
                }
            }
        }

        public DateTime? EpiCurveDisplayDate
        {
            get
            {
                if (DateOnset.HasValue)
                {
                    DateTime MMWR__Start;
                    MMWR__Start = Core.Common.GetMMWRStart(DateOnset.Value, 1);

                    TimeSpan MMWR__DayCount = DateOnset.Value.Subtract(MMWR__Start);
                    int MMWR__Week = ((int)(MMWR__DayCount.Days / 7)) + 1;

                    return MMWR__Start.AddDays((MMWR__Week * 7) - 1);
                }
                else
                {
                    return null;
                }
            }
        }
        public DateTime? EpiCurveDeathDisplayDate
        {
            get
            {
                if (DateDeathCurrentOrFinal.HasValue)
                {
                    DateTime MMWR__Start;
                    MMWR__Start = Core.Common.GetMMWRStart(DateDeathCurrentOrFinal.Value, 1);

                    TimeSpan MMWR__DayCount = DateDeathCurrentOrFinal.Value.Subtract(MMWR__Start);
                    int MMWR__Week = ((int)(MMWR__DayCount.Days / 7)) + 1;

                    return MMWR__Start.AddDays((MMWR__Week * 7) - 1);
                }
                else
                {
                    return null;
                }
            }
        }
        public Core.Enums.EpiCaseClassification EpiCurveCaseCategory
        {
            get
            {
                //if (EpiCaseDef == CaseViewModel.Confirmed)
                //{
                //    //if (FinalLabClass == "Confirmed Acute" || FinalLabClass == "Confirmed Convalescent")
                //    //{
                //    //    return FinalLabClass;
                //    //}
                //    return EpiCaseDef;
                //}
                //else if (EpiCaseDef == CaseViewModel.Probable)
                //{
                //    return EpiCaseDef;
                //}
                //else if (EpiCaseDef == CaseViewModel.Suspect)
                //{
                //    return EpiCaseDef;
                //}

                if (ApplicationViewModel.Instance.CurrentRegion == Core.Enums.RegionEnum.USA)
                {
                    if (EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed ||
                        EpiCaseDef == Core.Enums.EpiCaseClassification.Probable ||
                        EpiCaseDef == Core.Enums.EpiCaseClassification.Suspect ||
                        EpiCaseDef == Core.Enums.EpiCaseClassification.PUI)
                    {
                        return EpiCaseDef;
                    }
                }
                else
                {
                    if (EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed ||
                        EpiCaseDef == Core.Enums.EpiCaseClassification.Probable ||
                        EpiCaseDef == Core.Enums.EpiCaseClassification.Suspect)
                    {
                        return EpiCaseDef;
                    }
                }


                return Core.Enums.EpiCaseClassification.None;
            }
        }

        //public DateTime? DateDeathCurrentOrFinal { get { return Case.DateDeathCurrentOrFinal; } }

        public DateTime? DateDeathCurrentOrFinal
        {
            get
            {
                if (DateDeath.HasValue) return DateDeath;
                if (DateDeath2.HasValue) return DateDeath2;
                else return null;
            }
        }
        #endregion // 1

        public DateTime? DateOutcomeInfoCompleted { get { return _DateOutcomeInfoCompleted; } set { if (_DateOutcomeInfoCompleted != value) { _DateOutcomeInfoCompleted = value; RaisePropertyChanged("DateOutcomeInfoCompleted"); } } }
        public string BleedUnexplainedEver { get { return _BleedUnexplainedEver; } set { if (_BleedUnexplainedEver != value) { _BleedUnexplainedEver = value; RaisePropertyChanged("BleedUnexplainedEver"); } } }
        public string SpecifyBleeding { get { return _SpecifyBleeding; } set { if (_SpecifyBleeding != value) { _SpecifyBleeding = value; RaisePropertyChanged("SpecifyBleeding"); } } }
        public string HospitalDischarge { get { return _HospitalDischarge; } set { if (_HospitalDischarge != value) { _HospitalDischarge = value; RaisePropertyChanged("HospitalDischarge"); } } }
        public string HospitalDischargeDistrict { get { return _HospitalDischargeDistrict; } set { if (_HospitalDischargeDistrict != value) { _HospitalDischargeDistrict = value; RaisePropertyChanged("HospitalDischargeDistrict"); } } }
        public bool DateDischargeIsoEst { get { return _DateDischargeIsoEst; } set { if (_DateDischargeIsoEst != value) { _DateDischargeIsoEst = value; RaisePropertyChanged("DateDischargeIsoEst"); } } }
        public DateTime? DateDischargeHospital { get { return _DateDischargeHospital; } set { if (_DateDischargeHospital != value) { _DateDischargeHospital = value; RaisePropertyChanged("DateDischargeHospital"); } } }
        public bool DateDischargeHospitalEst { get { return _DateDischargeHospitalEst; } set { if (_DateDischargeHospitalEst != value) { _DateDischargeHospitalEst = value; RaisePropertyChanged("DateDischargeHospitalEst"); } } }
        public DateTime? DateDeath2
        {
            get { return _DateDeath2; }
            set
            {
                if (_DateDeath2 != value)
                {
                    _DateDeath2 = value;
                    RaisePropertyChanged("DateDeath2");
                    RaisePropertyChanged("DateDeathCurrentOrFinal");
                    RaisePropertyChanged("DateDeath");
                    RaisePropertyChanged("DateIsolationCurrent");
                }
            }
        }
        public bool DateDeath2Est { get { return _DateDeath2Est; } set { if (_DateDeath2Est != value) { _DateDeath2Est = value; RaisePropertyChanged("DateDeath2Est"); } } }
        public string DateDeath2EstSpecify { get { return _DateDeath2EstSpecify; } set { if (_DateDeath2EstSpecify != value) { _DateDeath2EstSpecify = value; RaisePropertyChanged("DateDeath2EstSpecify"); } } }
        public string PlaceDeath { get { return _PlaceDeath; } set { if (_PlaceDeath != value) { _PlaceDeath = value; RaisePropertyChanged("PlaceDeath"); } } }
        public string HospitalDeath { get { return _HospitalDeath; } set { if (_HospitalDeath != value) { _HospitalDeath = value; RaisePropertyChanged("HospitalDeath"); } } }
        public string PlaceDeathOther { get { return _PlaceDeathOther; } set { if (_PlaceDeathOther != value) { _PlaceDeathOther = value; RaisePropertyChanged("PlaceDeathOther"); } } }
        public string VillageDeath { get { return _VillageDeath; } set { if (_VillageDeath != value) { _VillageDeath = value; RaisePropertyChanged("VillageDeath"); } } }
        public string SubCountyDeath { get { return _SubCountyDeath; } set { if (_SubCountyDeath != value) { _SubCountyDeath = value; RaisePropertyChanged("SubCountyDeath"); } } }
        public string DistrictDeath { get { return _DistrictDeath; } set { if (_DistrictDeath != value) { _DistrictDeath = value; RaisePropertyChanged("DistrictDeath"); } } }
        public DateTime? DateFuneral { get { return _DateFuneral; } set { if (_DateFuneral != value) { _DateFuneral = value; RaisePropertyChanged("DateFuneral"); } } }
        public bool FuneralConductedFam { get { return _FuneralConductedFam; } set { if (_FuneralConductedFam != value) { _FuneralConductedFam = value; RaisePropertyChanged("FuneralConductedFam"); } } }
        public bool FuneralConductedOutbreakTeam { get { return _FuneralConductedOutbreakTeam; } set { if (_FuneralConductedOutbreakTeam != value) { _FuneralConductedOutbreakTeam = value; RaisePropertyChanged("FuneralConductedOutbreakTeam"); } } }
        public string VillageFuneral { get { return _VillageFuneral; } set { if (_VillageFuneral != value) { _VillageFuneral = value; RaisePropertyChanged("VillageFuneral"); } } }
        public string SubCountyFuneral { get { return _SubCountyFuneral; } set { if (_SubCountyFuneral != value) { _SubCountyFuneral = value; RaisePropertyChanged("SubCountyFuneral"); } } }
        public string DistrictFuneral { get { return _DistrictFuneral; } set { if (_DistrictFuneral != value) { _DistrictFuneral = value; RaisePropertyChanged("DistrictFuneral"); } } }
        
        public string PhoneNumber { get { return _PhoneNumber; } set { if (_PhoneNumber != value) { _PhoneNumber = value; RaisePropertyChanged("PhoneNumber"); } } }
        public string PhoneOwner { get { return _PhoneOwner; } set { if (_PhoneOwner != value) { _PhoneOwner = value; RaisePropertyChanged("PhoneOwner"); } } }
        public string StatusReport
        {
            get
            {
                return _StatusReport;
            }
            private set
            {
                if (_StatusReport != value)
                {
                    _StatusReport = value;
                    RaisePropertyChanged("StatusReport");

                    switch (_StatusReport)
                    {
                        case "1":
                            InitialStatus = Core.Enums.AliveDead.Dead;
                            break;
                        case "2":
                            InitialStatus = Core.Enums.AliveDead.Alive;
                            DateDeath = null;
                            RaisePropertyChanged("DateDeath");
                            break;
                        default:
                            InitialStatus = Core.Enums.AliveDead.None;
                            DateDeath = null;
                            RaisePropertyChanged("DateDeath");
                            break;
                    }

                    SetCurrentStatus();

                    RaisePropertyChanged("FinalCaseStatus");

                    //if (StatusReport == "1" && String.IsNullOrEmpty(FinalCaseStatus))
                    //{
                    //    FinalCaseStatus = StatusReport;
                    //}
                }
            }
        }
        public bool OccupationFarmer { get { return _OccupationFarmer; } set { if (_OccupationFarmer != value) { _OccupationFarmer = value; RaisePropertyChanged("OccupationFarmer"); } } }
        public bool OccupationButcher { get { return _OccupationButcher; } set { if (_OccupationButcher != value) { _OccupationButcher = value; RaisePropertyChanged("OccupationButcher"); } } }
        public bool OccupationHunter { get { return _OccupationHunter; } set { if (_OccupationHunter != value) { _OccupationHunter = value; RaisePropertyChanged("OccupationHunter"); } } }
        public bool OccupationMiner { get { return _OccupationMiner; } set { if (_OccupationMiner != value) { _OccupationMiner = value; RaisePropertyChanged("OccupationMiner"); } } }
        public bool OccupationReligious { get { return _OccupationReligious; } set { if (_OccupationReligious != value) { _OccupationReligious = value; RaisePropertyChanged("OccupationReligious"); } } }
        public bool OccupationHousewife { get { return _OccupationHousewife; } set { if (_OccupationHousewife != value) { _OccupationHousewife = value; RaisePropertyChanged("OccupationHousewife"); } } }
        public bool OccupationStudent { get { return _OccupationStudent; } set { if (_OccupationStudent != value) { _OccupationStudent = value; RaisePropertyChanged("OccupationStudent"); } } }
        public bool OccupationChild { get { return _OccupationChild; } set { if (_OccupationChild != value) { _OccupationChild = value; RaisePropertyChanged("OccupationChild"); } } }
        public bool OccupationBusinessman { get { return _OccupationBusinessman; } set { if (_OccupationBusinessman != value) { _OccupationBusinessman = value; RaisePropertyChanged("OccupationBusinessman"); } } }
        public bool OccupationTransporter { get { return _OccupationTransporter; } set { if (_OccupationTransporter != value) { _OccupationTransporter = value; RaisePropertyChanged("OccupationTransporter"); } } }
        //public bool OccupationHCW { get { return _REPLACEME; } set { if (_REPLACEME != value) { _REPLACEME = value; RaisePropertyChanged("REPLACEME"); } } }
        public bool OccupationTraditionalHealer { get { return _OccupationTraditionalHealer; } set { if (_OccupationTraditionalHealer != value) { _OccupationTraditionalHealer = value; RaisePropertyChanged("OccupationTraditionalHealer"); } } }
        public bool OccupationOther { get { return _OccupationOther; } set { if (_OccupationOther != value) { _OccupationOther = value; RaisePropertyChanged("OccupationOther"); } } }

        public string OccupationTransporterSpecify { get { return _OccupationTransporterSpecify; } set { if (_OccupationTransporterSpecify != value) { _OccupationTransporterSpecify = value; RaisePropertyChanged("OccupationTransporterSpecify"); } } }
        public string OccupationBusinessSpecify { get { return _OccupationBusinessSpecify; } set { if (_OccupationBusinessSpecify != value) { _OccupationBusinessSpecify = value; RaisePropertyChanged("OccupationBusinessSpecify"); } } }
        public string OccupationOtherSpecify { get { return _OccupationOtherSpecify; } set { if (_OccupationOtherSpecify != value) { _OccupationOtherSpecify = value; RaisePropertyChanged("OccupationOtherSpecify"); } } }
        public string OccupationHCWPosition { get { return _OccupationHCWPosition; } set { if (_OccupationHCWPosition != value) { _OccupationHCWPosition = value; RaisePropertyChanged("OccupationHCWPosition"); } } }
        public string OccupationHCWFacility { get { return _OccupationHCWFacility; } set { if (_OccupationHCWFacility != value) { _OccupationHCWFacility = value; RaisePropertyChanged("OccupationHCWFacility"); } } }

        public double? Latitude { get { return _Latitude; } set { if (_Latitude != value) { _Latitude = value; RaisePropertyChanged("Latitude"); } } }
        public double? Longitude { get { return _Longitude; } set { if (_Longitude != value) { _Longitude = value; RaisePropertyChanged("Longitude"); } } }

        public DateTime? DateOnsetLocalStart { get { return _DateOnsetLocalStart; } set { if (_DateOnsetLocalStart != value) { _DateOnsetLocalStart = value; RaisePropertyChanged("DateOnsetLocalStart"); } } }
        public DateTime? DateOnsetLocalEnd { get { return _DateOnsetLocalEnd; } set { if (_DateOnsetLocalEnd != value) { _DateOnsetLocalEnd = value; RaisePropertyChanged("DateOnsetLocalEnd"); } } }

        public string HospitalizedCurrent { get { return _HospitalizedCurrent; } set { if (_HospitalizedCurrent != value) { _HospitalizedCurrent = value; RaisePropertyChanged("HospitalizedCurrent"); } } }
        public string HospitalizedPast { get { return _HospitalizedPast; } set { if (_HospitalizedPast != value) { _HospitalizedPast = value; RaisePropertyChanged("HospitalizedPast"); } } }

        public DateTime? DateHospitalPastStart1 { get { return _DateHospitalPastStart1; } set { if (_DateHospitalPastStart1 != value) { _DateHospitalPastStart1 = value; RaisePropertyChanged("DateHospitalPastStart1"); } } }
        public DateTime? DateHospitalPastStart2 { get { return _DateHospitalPastStart2; } set { if (_DateHospitalPastStart2 != value) { _DateHospitalPastStart2 = value; RaisePropertyChanged("DateHospitalPastStart2"); } } }

        public DateTime? DateHospitalPastEnd1 { get { return _DateHospitalPastEnd1; } set { if (_DateHospitalPastEnd1 != value) { _DateHospitalPastEnd1 = value; RaisePropertyChanged("DateHospitalPastEnd1"); } } }
        public DateTime? DateHospitalPastEnd2 { get { return _DateHospitalPastEnd2; } set { if (_DateHospitalPastEnd2 != value) { _DateHospitalPastEnd2 = value; RaisePropertyChanged("DateHospitalPastEnd2"); } } }

        public string HospitalPast1 { get { return _HospitalPast1; } set { if (_HospitalPast1 != value) { _HospitalPast1 = value; RaisePropertyChanged("HospitalPast1"); } } }
        public string HospitalPast2 { get { return _HospitalPast2; } set { if (_HospitalPast2 != value) { _HospitalPast2 = value; RaisePropertyChanged("HospitalPast2"); } } }

        public string HospitalVillage1 { get { return _HospitalVillage1; } set { if (_HospitalVillage1 != value) { _HospitalVillage1 = value; RaisePropertyChanged("HospitalVillage1"); } } }
        public string HospitalVillage2 { get { return _HospitalVillage2; } set { if (_HospitalVillage2 != value) { _HospitalVillage2 = value; RaisePropertyChanged("HospitalVillage2"); } } }

        public string HospitalDistrict1 { get { return _HospitalDistrict1; } set { if (_HospitalDistrict1 != value) { _HospitalDistrict1 = value; RaisePropertyChanged("HospitalDistrict1"); } } }
        public string HospitalDistrict2 { get { return _HospitalDistrict2; } set { if (_HospitalDistrict2 != value) { _HospitalDistrict2 = value; RaisePropertyChanged("HospitalDistrict2"); } } }

        public string IsolationPast1 { get { return _IsolationPast1; } set { if (_IsolationPast1 != value) { _IsolationPast1 = value; RaisePropertyChanged("IsolationPast1"); } } }
        public string IsolationPast2 { get { return _IsolationPast2; } set { if (_IsolationPast2 != value) { _IsolationPast2 = value; RaisePropertyChanged("IsolationPast2"); } } }

        public string SymptomFeverFinal
        {
            get { return _SymptomFeverFinal; }
            set
            {
                if (_SymptomFeverFinal != value)
                {
                    if (!(SymptomFever == "1" && value != "1"))
                    {
                        _SymptomFeverFinal = value;
                        RaisePropertyChanged("SymptomFeverFinal");
                    }
                }
            }
        }
        public double? SymptomFeverTempFinal { get { return _SymptomFeverTempFinal; } set { if (_SymptomFeverTempFinal != value) { _SymptomFeverTempFinal = value; RaisePropertyChanged("SymptomFeverTempFinal"); } } }
        public string SymptomFeverTempSourceFinal { get { return _SymptomFeverTempSourceFinal; } set { if (_SymptomFeverTempSourceFinal != value) { _SymptomFeverTempSourceFinal = value; RaisePropertyChanged("SymptomFeverTempSourceFinal"); } } }
        public string SymptomVomitingFinal
        {
            get { return _SymptomVomitingFinal; }
            set
            {
                if (_SymptomVomitingFinal != value)
                {
                    if (!(SymptomVomiting == "1" && value != "1"))
                    {
                        _SymptomVomitingFinal = value;
                        RaisePropertyChanged("SymptomVomitingFinal");
                    }
                }
            }
        }
        public string SymptomDiarrheaFinal
        {
            get { return _SymptomDiarrheaFinal; }
            set
            {
                if (_SymptomDiarrheaFinal != value)
                {
                    if (!(SymptomDiarrhea == "1" && value != "1"))
                    {
                        _SymptomDiarrheaFinal = value;
                        RaisePropertyChanged("SymptomDiarrheaFinal");
                    }
                }
            }
        }
        public string SymptomFatigueFinal
        {
            get { return _SymptomFatigueFinal; }
            set
            {
                if (_SymptomFatigueFinal != value)
                {
                    if (!(SymptomFatigue == "1" && value != "1"))
                    {
                        _SymptomFatigueFinal = value;
                        RaisePropertyChanged("SymptomFatigueFinal");
                    }
                }
            }
        }
        public string SymptomAnorexiaFinal
        {
            get { return _SymptomAnorexiaFinal; }
            set
            {
                if (_SymptomAnorexiaFinal != value)
                {
                    if (!(SymptomAnorexia == "1" && value != "1"))
                    {
                        _SymptomAnorexiaFinal = value;
                        RaisePropertyChanged("SymptomAnorexiaFinal");
                    }
                }
            }
        }
        public string SymptomAbdPainFinal
        {
            get { return _SymptomAbdPainFinal; }
            set
            {
                if (_SymptomAbdPainFinal != value)
                {
                    if (!(SymptomAbdPain == "1" && value != "1"))
                    {
                        _SymptomAbdPainFinal = value;
                        RaisePropertyChanged("SymptomAbdPainFinal");
                    }
                }
            }
        }
        public string SymptomChestPainFinal
        {
            get { return _SymptomChestPainFinal; }
            set
            {
                if (_SymptomChestPainFinal != value)
                {
                    if (!(SymptomChestPain == "1" && value != "1"))
                    {
                        _SymptomChestPainFinal = value;
                        RaisePropertyChanged("SymptomChestPainFinal");
                    }
                }
            }
        }
        public string SymptomMusclePainFinal
        {
            get { return _SymptomMusclePainFinal; }
            set
            {
                if (_SymptomMusclePainFinal != value)
                {
                    if (!(SymptomMusclePain == "1" && value != "1"))
                    {
                        _SymptomMusclePainFinal = value; RaisePropertyChanged("SymptomMusclePainFinal");
                    }
                }
            }
        }
        public string SymptomJointPainFinal
        {
            get { return _SymptomJointPainFinal; }
            set
            {
                if (_SymptomJointPainFinal != value)
                {
                    if (!(SymptomJointPain == "1" && value != "1"))
                    {
                        _SymptomJointPainFinal = value;
                        RaisePropertyChanged("SymptomJointPainFinal");
                    }
                }
            }
        }
        public string SymptomHeadacheFinal
        {
            get { return _SymptomHeadacheFinal; }
            set
            {
                if (_SymptomHeadacheFinal != value)
                {
                    if (!(SymptomHeadache == "1" && value != "1"))
                    {
                        _SymptomHeadacheFinal = value;
                        RaisePropertyChanged("SymptomHeadacheFinal");
                    }
                }
            }
        }
        public string SymptomCoughFinal
        {
            get { return _SymptomCoughFinal; }
            set
            {
                if (_SymptomCoughFinal != value)
                {
                    if (!(SymptomCough == "1" && value != "1"))
                    {
                        _SymptomCoughFinal = value;
                        RaisePropertyChanged("SymptomCoughFinal");
                    }
                }
            }
        }
        public string SymptomDiffBreatheFinal
        {
            get { return _SymptomDiffBreatheFinal; }
            set
            {
                if (_SymptomDiffBreatheFinal != value)
                {
                    if (!(SymptomDiffBreathe == "1" && value != "1"))
                    {
                        _SymptomDiffBreatheFinal = value;
                        RaisePropertyChanged("SymptomDiffBreatheFinal");
                    }
                }
            }
        }
        public string SymptomDiffSwallowFinal
        {
            get { return _SymptomDiffSwallowFinal; }
            set
            {
                if (_SymptomDiffSwallowFinal != value)
                {
                    if (!(SymptomDiffSwallow == "1" && value != "1"))
                    {
                        _SymptomDiffSwallowFinal = value;
                        RaisePropertyChanged("SymptomDiffSwallowFinal");
                    }
                }
            }
        }
        public string SymptomSoreThroatFinal
        {
            get { return _SymptomSoreThroatFinal; }
            set
            {
                if (_SymptomSoreThroatFinal != value)
                {
                    if (!(SymptomSoreThroat == "1" && value != "1"))
                    {
                        _SymptomSoreThroatFinal = value;
                        RaisePropertyChanged("SymptomSoreThroatFinal");
                    }
                }
            }
        }
        public string SymptomJaundiceFinal
        {
            get { return _SymptomJaundiceFinal; }
            set
            {
                if (_SymptomJaundiceFinal != value)
                {
                    if (!(SymptomJaundice == "1" && value != "1"))
                    {
                        _SymptomJaundiceFinal = value;
                        RaisePropertyChanged("SymptomJaundiceFinal");
                    }
                }
            }
        }
        public string SymptomConjunctivitisFinal
        {
            get { return _SymptomConjunctivitisFinal; }
            set
            {
                if (_SymptomConjunctivitisFinal != value)
                {
                    if (!(SymptomConjunctivitis == "1" && value != "1"))
                    {
                        _SymptomConjunctivitisFinal = value;
                        RaisePropertyChanged("SymptomConjunctivitisFinal");
                    }
                }
            }
        }
        public string SymptomRashFinal
        {
            get { return _SymptomRashFinal; }
            set
            {
                if (_SymptomRashFinal != value)
                {
                    if (!(SymptomRash == "1" && value != "1"))
                    {
                        _SymptomRashFinal = value;
                        RaisePropertyChanged("SymptomRashFinal");
                    }
                }
            }
        }
        public string SymptomHiccupsFinal
        {
            get { return _SymptomHiccupsFinal; }
            set
            {
                if (_SymptomHiccupsFinal != value)
                {
                    if (!(SymptomHiccups == "1" && value != "1"))
                    {
                        _SymptomHiccupsFinal = value;
                        RaisePropertyChanged("SymptomHiccupsFinal");
                    }
                }
            }
        }
        public string SymptomPainEyesFinal
        {
            get { return _SymptomPainEyesFinal; }
            set
            {
                if (_SymptomPainEyesFinal != value)
                {
                    if (!(SymptomPainEyes == "1" && value != "1"))
                    {
                        _SymptomPainEyesFinal = value;
                        RaisePropertyChanged("SymptomPainEyesFinal");
                    }
                }
            }
        }
        public string SymptomUnconsciousFinal
        {
            get { return _SymptomUnconsciousFinal; }
            set
            {
                if (_SymptomUnconsciousFinal != value)
                {
                    if (!(SymptomUnconscious == "1" && value != "1"))
                    {
                        _SymptomUnconsciousFinal = value;
                        RaisePropertyChanged("SymptomUnconsciousFinal");
                    }

                }
            }
        }
        public string SymptomConfusedFinal
        {
            get { return _SymptomConfusedFinal; }
            set
            {
                if (_SymptomConfusedFinal != value)
                {
                    if (!(SymptomConfused == "1" && value != "1"))
                    {
                        _SymptomConfusedFinal = value;
                        RaisePropertyChanged("SymptomConfusedFinal");
                    }
                }
            }
        }
        public string SymptomOtherHemoFinal
        {
            get { return _SymptomOtherHemoFinal; }
            set
            {
                if (_SymptomOtherHemoFinal != value)
                {
                    _SymptomOtherHemoFinal = value; RaisePropertyChanged("SymptomOtherHemoFinal");
                }
            }
        }

        public string SymptomOtherHemoFinalSpecify { get { return _SymptomOtherHemoFinalSpecify; } set { if (_SymptomOtherHemoFinalSpecify != value) { _SymptomOtherHemoFinalSpecify = value; RaisePropertyChanged("SymptomOtherHemoFinalSpecify"); } } }

        public string SymptomUnexplainedBleeding { get { return _SymptomUnexplainedBleeding; } set { if (_SymptomUnexplainedBleeding != value) { _SymptomUnexplainedBleeding = value; RaisePropertyChanged("SymptomUnexplainedBleeding"); } } }
        public string SymptomBleedGums { get { return _SymptomBleedGums; } set { if (_SymptomBleedGums != value) { _SymptomBleedGums = value; RaisePropertyChanged("SymptomBleedGums"); } } }
        public string SymptomBleedInjectionSite { get { return _SymptomBleedInjectionSite; } set { if (_SymptomBleedInjectionSite != value) { _SymptomBleedInjectionSite = value; RaisePropertyChanged("SymptomBleedInjectionSite"); } } }
        public string SymptomNoseBleed { get { return _SymptomNoseBleed; } set { if (_SymptomNoseBleed != value) { _SymptomNoseBleed = value; RaisePropertyChanged("SymptomNoseBleed"); } } }
        public string SymptomBloodyStool { get { return _SymptomBloodyStool; } set { if (_SymptomBloodyStool != value) { _SymptomBloodyStool = value; RaisePropertyChanged("SymptomBloodyStool"); } } }
        public string SymptomHematemesis { get { return _SymptomHematemesis; } set { if (_SymptomHematemesis != value) { _SymptomHematemesis = value; RaisePropertyChanged("SymptomHematemesis"); } } }
        public string SymptomBloodVomit { get { return _SymptomBloodVomit; } set { if (_SymptomBloodVomit != value) { _SymptomBloodVomit = value; RaisePropertyChanged("SymptomBloodVomit"); } } }
        public string SymptomCoughBlood { get { return _SymptomCoughBlood; } set { if (_SymptomCoughBlood != value) { _SymptomCoughBlood = value; RaisePropertyChanged("SymptomCoughBlood"); } } }
        public string SymptomBleedVagina { get { return _SymptomBleedVagina; } set { if (_SymptomBleedVagina != value) { _SymptomBleedVagina = value; RaisePropertyChanged("SymptomBleedVagina"); } } }
        public string SymptomBleedSkin { get { return _SymptomBleedSkin; } set { if (_SymptomBleedSkin != value) { _SymptomBleedSkin = value; RaisePropertyChanged("SymptomBleedSkin"); } } }
        public string SymptomBleedUrine { get { return _SymptomBleedUrine; } set { if (_SymptomBleedUrine != value) { _SymptomBleedUrine = value; RaisePropertyChanged("SymptomBleedUrine"); } } }
        public string SymptomOtherNonHemorrhagic
        {
            get { return _SymptomOtherNonHemorrhagic; }
            set
            {
                if (_SymptomOtherNonHemorrhagic != value)
                {
                    _SymptomOtherNonHemorrhagic = value; RaisePropertyChanged("SymptomOtherNonHemorrhagic");

                    if (SymptomOtherNonHemorrhagic == "1" && String.IsNullOrEmpty(SymptomOtherHemoFinal))
                    {
                        SymptomOtherHemoFinal = SymptomOtherNonHemorrhagic;
                    }
                }
            }
        }

        public string SymptomFever
        {
            get { return _SymptomFever; }
            set
            {
                if (_SymptomFever != value)
                {
                    _SymptomFever = value; RaisePropertyChanged("SymptomFever");

                    if (SymptomFever == "1" && String.IsNullOrEmpty(SymptomFeverFinal))
                    {
                        SymptomFeverFinal = SymptomFever;
                    }
                }
            }
        }
        public double? SymptomFeverTemp
        {
            get { return _SymptomFeverTemp; }
            set
            {
                if (_SymptomFeverTemp != value)
                {
                    _SymptomFeverTemp = value; RaisePropertyChanged("SymptomFeverTemp");
                }
            }
        }
        public string SymptomFeverTempSource
        {
            get { return _SymptomFeverTempSource; }
            set
            {
                if (_SymptomFeverTempSource != value)
                {
                    _SymptomFeverTempSource = value; RaisePropertyChanged("SymptomFeverTempSource");
                }
            }
        }
        public string SymptomVomiting
        {
            get { return _SymptomVomiting; }
            set
            {
                if (_SymptomVomiting != value)
                {
                    _SymptomVomiting = value; RaisePropertyChanged("SymptomVomiting");

                    if (SymptomVomiting == "1" && String.IsNullOrEmpty(SymptomVomitingFinal))
                    {
                        SymptomVomitingFinal = SymptomVomiting;
                    }
                }
            }
        }
        public string SymptomDiarrhea
        {
            get { return _SymptomDiarrhea; }
            set
            {
                if (_SymptomDiarrhea != value)
                {
                    _SymptomDiarrhea = value; RaisePropertyChanged("SymptomDiarrhea");

                    if (SymptomDiarrhea == "1" && String.IsNullOrEmpty(SymptomDiarrheaFinal))
                    {
                        SymptomDiarrheaFinal = SymptomDiarrhea;
                    }
                }
            }
        }
        public string SymptomFatigue
        {
            get { return _SymptomFatigue; }
            set
            {
                if (_SymptomFatigue != value)
                {
                    _SymptomFatigue = value; RaisePropertyChanged("SymptomFatigue");

                    if (SymptomFatigue == "1" && String.IsNullOrEmpty(SymptomFatigueFinal))
                    {
                        SymptomFatigueFinal = SymptomFatigue;
                    }
                }
            }
        }
        public string SymptomAnorexia
        {
            get { return _SymptomAnorexia; }
            set
            {
                if (_SymptomAnorexia != value)
                {
                    _SymptomAnorexia = value; RaisePropertyChanged("SymptomAnorexia");

                    if (SymptomAnorexia == "1" && String.IsNullOrEmpty(SymptomAnorexiaFinal))
                    {
                        SymptomAnorexiaFinal = SymptomAnorexia;
                    }
                }
            }
        }
        public string SymptomAbdPain
        {
            get { return _SymptomAbdPain; }
            set
            {
                if (_SymptomAbdPain != value)
                {
                    _SymptomAbdPain = value; RaisePropertyChanged("SymptomAbdPain");

                    if (SymptomAbdPain == "1" && String.IsNullOrEmpty(SymptomAbdPainFinal))
                    {
                        SymptomAbdPainFinal = SymptomAbdPain;
                    }
                }
            }
        }
        public string SymptomChestPain
        {
            get { return _SymptomChestPain; }
            set
            {
                if (_SymptomChestPain != value)
                {
                    _SymptomChestPain = value; RaisePropertyChanged("SymptomChestPain");

                    if (SymptomChestPain == "1" && String.IsNullOrEmpty(SymptomChestPainFinal))
                    {
                        SymptomChestPainFinal = SymptomChestPain;
                    }
                }
            }
        }
        public string SymptomMusclePain
        {
            get { return _SymptomMusclePain; }
            set
            {
                if (_SymptomMusclePain != value)
                {
                    _SymptomMusclePain = value; RaisePropertyChanged("SymptomMusclePain");

                    if (SymptomMusclePain == "1" && String.IsNullOrEmpty(SymptomMusclePainFinal))
                    {
                        SymptomMusclePainFinal = SymptomMusclePain;
                    }
                }
            }
        }
        public string SymptomJointPain
        {
            get { return _SymptomJointPain; }
            set
            {
                if (_SymptomJointPain != value)
                {
                    _SymptomJointPain = value; RaisePropertyChanged("SymptomJointPain");

                    if (SymptomJointPain == "1" && String.IsNullOrEmpty(SymptomJointPainFinal))
                    {
                        SymptomJointPainFinal = SymptomJointPain;
                    }
                }
            }
        }
        public string SymptomHeadache
        {
            get { return _SymptomHeadache; }
            set
            {
                if (_SymptomHeadache != value)
                {
                    _SymptomHeadache = value; RaisePropertyChanged("SymptomHeadache");

                    if (SymptomHeadache == "1" && String.IsNullOrEmpty(SymptomHeadacheFinal))
                    {
                        SymptomHeadacheFinal = SymptomHeadache;
                    }
                }
            }
        }
        public string SymptomCough
        {
            get { return _SymptomCough; }
            set
            {
                if (_SymptomCough != value)
                {
                    _SymptomCough = value; RaisePropertyChanged("SymptomCough");

                    if (SymptomCough == "1" && String.IsNullOrEmpty(SymptomCoughFinal))
                    {
                        SymptomCoughFinal = SymptomCough;
                    }
                }
            }
        }
        public string SymptomDiffBreathe
        {
            get { return _SymptomDiffBreathe; }
            set
            {
                if (_SymptomDiffBreathe != value)
                {
                    _SymptomDiffBreathe = value; RaisePropertyChanged("SymptomDiffBreathe");

                    if (SymptomDiffBreathe == "1" && String.IsNullOrEmpty(SymptomDiffBreatheFinal))
                    {
                        SymptomDiffBreatheFinal = SymptomDiffBreathe;
                    }
                }
            }
        }
        public string SymptomDiffSwallow
        {
            get { return _SymptomDiffSwallow; }
            set
            {
                if (_SymptomDiffSwallow != value)
                {
                    _SymptomDiffSwallow = value; RaisePropertyChanged("SymptomDiffSwallow");

                    if (SymptomDiffSwallow == "1" && String.IsNullOrEmpty(SymptomDiffSwallowFinal))
                    {
                        SymptomDiffSwallowFinal = SymptomDiffSwallow;
                    }
                }
            }
        }
        public string SymptomSoreThroat
        {
            get { return _SymptomSoreThroat; }
            set
            {
                if (_SymptomSoreThroat != value)
                {
                    _SymptomSoreThroat = value; RaisePropertyChanged("SymptomSoreThroat");

                    if (SymptomSoreThroat == "1" && String.IsNullOrEmpty(SymptomSoreThroatFinal))
                    {
                        SymptomSoreThroatFinal = SymptomSoreThroat;
                    }
                }
            }
        }
        public string SymptomJaundice
        {
            get { return _SymptomJaundice; }
            set
            {
                if (_SymptomJaundice != value)
                {
                    _SymptomJaundice = value; RaisePropertyChanged("SymptomJaundice");

                    if (SymptomJaundice == "1" && String.IsNullOrEmpty(SymptomJaundiceFinal))
                    {
                        SymptomJaundiceFinal = SymptomJaundice;
                    }
                }
            }
        }
        public string SymptomConjunctivitis
        {
            get { return _SymptomConjunctivitis; }
            set
            {
                if (_SymptomConjunctivitis != value)
                {
                    _SymptomConjunctivitis = value; RaisePropertyChanged("SymptomConjunctivitis");

                    if (SymptomConjunctivitis == "1" && String.IsNullOrEmpty(SymptomConjunctivitisFinal))
                    {
                        SymptomConjunctivitisFinal = SymptomConjunctivitis;
                    }
                }
            }
        }
        public string SymptomRash
        {
            get { return _SymptomRash; }
            set
            {
                if (_SymptomRash != value)
                {
                    _SymptomRash = value; RaisePropertyChanged("SymptomRash");

                    if (SymptomRash == "1" && String.IsNullOrEmpty(SymptomRashFinal))
                    {
                        SymptomRashFinal = SymptomRash;
                    }
                }
            }
        }
        public string SymptomHiccups
        {
            get { return _SymptomHiccups; }
            set
            {
                if (_SymptomHiccups != value)
                {
                    _SymptomHiccups = value; RaisePropertyChanged("SymptomHiccups");

                    if (SymptomHiccups == "1" && String.IsNullOrEmpty(SymptomHiccupsFinal))
                    {
                        SymptomHiccupsFinal = SymptomHiccups;
                    }
                }
            }
        }
        public string SymptomPainEyes
        {
            get { return _SymptomPainEyes; }
            set
            {
                if (_SymptomPainEyes != value)
                {
                    _SymptomPainEyes = value; RaisePropertyChanged("SymptomPainEyes");

                    if (SymptomPainEyes == "1" && String.IsNullOrEmpty(SymptomPainEyesFinal))
                    {
                        SymptomPainEyesFinal = SymptomPainEyes;
                    }
                }
            }
        }
        public string SymptomUnconscious
        {
            get { return _SymptomUnconscious; }
            set
            {
                if (_SymptomUnconscious != value)
                {
                    _SymptomUnconscious = value; RaisePropertyChanged("SymptomUnconscious");

                    if (SymptomUnconscious == "1" && String.IsNullOrEmpty(SymptomUnconsciousFinal))
                    {
                        SymptomUnconsciousFinal = SymptomUnconscious;
                    }
                }
            }
        }
        public string SymptomConfused
        {
            get { return _SymptomConfused; }
            set
            {
                if (_SymptomConfused != value)
                {
                    _SymptomConfused = value; RaisePropertyChanged("SymptomConfused");

                    if (SymptomConfused == "1" && String.IsNullOrEmpty(SymptomConfusedFinal))
                    {
                        SymptomConfusedFinal = SymptomConfused;
                    }
                }
            }
        }
        public string SymptomOtherHemo
        {
            get { return _SymptomOtherHemo; }
            set { if (_SymptomOtherHemo != value) { _SymptomOtherHemo = value; RaisePropertyChanged("SymptomOtherHemo"); } }
        }
        public string SymptomOtherHemoSpecify
        {
            get { return _SymptomOtherHemoSpecify; }
            set { if (_SymptomOtherHemoSpecify != value) { _SymptomOtherHemoSpecify = value; RaisePropertyChanged("SymptomOtherHemoSpecify"); } }
        }
        public string SymptomOtherNonHemorrhagicSpecify
        {
            get { return _SymptomOtherNonHemorrhagicSpecify; }
            set
            {
                if (_SymptomOtherNonHemorrhagicSpecify != value)
                {
                    if (_SymptomOtherNonHemorrhagicSpecify != value)
                    {
                        _SymptomOtherNonHemorrhagicSpecify = value;
                        SymptomOtherHemoFinalSpecify = value;
                    }

                    RaisePropertyChanged("SymptomOtherNonHemorrhagicSpecify");
                }
            }
        }

        public string HadContact { get { return _HadContact; } set { if (_HadContact != value) { _HadContact = value; RaisePropertyChanged("HadContact"); } } }
        public string ContactName1 { get { return _ContactName1; } set { if (_ContactName1 != value) { _ContactName1 = value; RaisePropertyChanged("ContactName1"); } } }
        public string ContactName2 { get { return _ContactName2; } set { if (_ContactName2 != value) { _ContactName2 = value; RaisePropertyChanged("ContactName2"); } } }
        public string ContactName3 { get { return _ContactName3; } set { if (_ContactName3 != value) { _ContactName3 = value; RaisePropertyChanged("ContactName3"); } } }
        public string ContactRelation1 { get { return _ContactRelation1; } set { if (_ContactRelation1 != value) { _ContactRelation1 = value; RaisePropertyChanged("ContactRelation1"); } } }
        public string ContactRelation2 { get { return _ContactRelation2; } set { if (_ContactRelation2 != value) { _ContactRelation2 = value; RaisePropertyChanged("ContactRelation2"); } } }
        public string ContactRelation3 { get { return _ContactRelation3; } set { if (_ContactRelation3 != value) { _ContactRelation3 = value; RaisePropertyChanged("ContactRelation3"); } } }
        public DateTime? ContactStartDate1 { get { return _ContactStartDate1; } set { if (_ContactStartDate1 != value) { _ContactStartDate1 = value; RaisePropertyChanged("ContactStartDate1"); } } }
        public DateTime? ContactStartDate2 { get { return _ContactStartDate2; } set { if (_ContactStartDate2 != value) { _ContactStartDate2 = value; RaisePropertyChanged("ContactStartDate2"); } } }
        public DateTime? ContactStartDate3 { get { return _ContactStartDate3; } set { if (_ContactStartDate3 != value) { _ContactStartDate3 = value; RaisePropertyChanged("ContactStartDate3"); } } }
        public DateTime? ContactEndDate1 { get { return _ContactEndDate1; } set { if (_ContactEndDate1 != value) { _ContactEndDate1 = value; RaisePropertyChanged("ContactEndDate1"); } } }
        public DateTime? ContactEndDate2 { get { return _ContactEndDate2; } set { if (_ContactEndDate2 != value) { _ContactEndDate2 = value; RaisePropertyChanged("ContactEndDate2"); } } }
        public DateTime? ContactEndDate3 { get { return _ContactEndDate3; } set { if (_ContactEndDate3 != value) { _ContactEndDate3 = value; RaisePropertyChanged("ContactEndDate3"); } } }
        public bool ContactDate1Estimated { get { return _ContactDate1Estimated; } set { if (_ContactDate1Estimated != value) { _ContactDate1Estimated = value; RaisePropertyChanged("ContactDate1Estimated"); } } }
        public bool ContactDate2Estimated { get { return _ContactDate2Estimated; } set { if (_ContactDate2Estimated != value) { _ContactDate2Estimated = value; RaisePropertyChanged("ContactDate2Estimated"); } } }
        public bool ContactDate3Estimated { get { return _ContactDate3Estimated; } set { if (_ContactDate3Estimated != value) { _ContactDate3Estimated = value; RaisePropertyChanged("ContactDate3Estimated"); } } }
        public string ContactVillage1 { get { return _ContactVillage1; } set { if (_ContactVillage1 != value) { _ContactVillage1 = value; RaisePropertyChanged("ContactVillage1"); } } }
        public string ContactVillage2 { get { return _ContactVillage2; } set { if (_ContactVillage2 != value) { _ContactVillage2 = value; RaisePropertyChanged("ContactVillage2"); } } }
        public string ContactVillage3 { get { return _ContactVillage3; } set { if (_ContactVillage3 != value) { _ContactVillage3 = value; RaisePropertyChanged("ContactVillage3"); } } }
        public string ContactDistrict1 { get { return _ContactDistrict1; } set { if (_ContactDistrict1 != value) { _ContactDistrict1 = value; RaisePropertyChanged("ContactDistrict1"); } } }
        public string ContactDistrict2 { get { return _ContactDistrict2; } set { if (_ContactDistrict2 != value) { _ContactDistrict2 = value; RaisePropertyChanged("ContactDistrict2"); } } }
        public string ContactDistrict3 { get { return _ContactDistrict3; } set { if (_ContactDistrict3 != value) { _ContactDistrict3 = value; RaisePropertyChanged("ContactDistrict3"); } } }
        public string ContactCountry1 { get { return _ContactCountry1; } set { if (_ContactCountry1 != value) { _ContactCountry1 = value; RaisePropertyChanged("ContactCountry1"); } } }
        public string ContactCountry2 { get { return _ContactCountry2; } set { if (_ContactCountry2 != value) { _ContactCountry2 = value; RaisePropertyChanged("ContactCountry2"); } } }
        public string ContactCountry3 { get { return _ContactCountry3; } set { if (_ContactCountry3 != value) { _ContactCountry3 = value; RaisePropertyChanged("ContactCountry3"); } } }
        public string TypesOfContact1 { get { return _TypesOfContact1; } set { if (_TypesOfContact1 != value) { _TypesOfContact1 = value; RaisePropertyChanged("TypesOfContact1"); } } }
        public string TypesOfContact2 { get { return _TypesOfContact2; } set { if (_TypesOfContact2 != value) { _TypesOfContact2 = value; RaisePropertyChanged("TypesOfContact2"); } } }
        public string TypesOfContact3 { get { return _TypesOfContact3; } set { if (_TypesOfContact3 != value) { _TypesOfContact3 = value; RaisePropertyChanged("TypesOfContact3"); } } }
        public string ContactStatus1
        {
            get { return _ContactStatus1; }
            set
            {
                if (_ContactStatus1 != value)
                {
                    _ContactStatus1 = value; RaisePropertyChanged("ContactStatus1");
                    switch (_ContactStatus1)
                    {
                        case "1":
                            break;
                        case "2":
                            // ContactDeathDate1 = null; 17134
                            RaisePropertyChanged("ContactDeathDate1");
                            break;
                        default:
                            break;
                    }

                }
            }
        }
        public string ContactStatus2
        {
            get { return _ContactStatus2; }
            set
            {
                if (_ContactStatus2 != value)
                {
                    _ContactStatus2 = value; RaisePropertyChanged("ContactStatus2");
                    switch (_ContactStatus1)
                    {
                        case "1":
                            break;
                        case "2":
                            //ContactDeathDate2 = null; 17134
                            RaisePropertyChanged("ContactDeathDate2");
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        public string ContactStatus3 { get { return _ContactStatus3; } set { if (_ContactStatus3 != value) { _ContactStatus3 = value; RaisePropertyChanged("ContactStatus3"); } } }
        public DateTime? ContactDeathDate1 { get { return _ContactDeathDate1; } set { if (_ContactDeathDate1 != value) { _ContactDeathDate1 = value; RaisePropertyChanged("ContactDeathDate1"); } } }
        public DateTime? ContactDeathDate2 { get { return _ContactDeathDate2; } set { if (_ContactDeathDate2 != value) { _ContactDeathDate2 = value; RaisePropertyChanged("ContactDeathDate2"); } } }
        public DateTime? ContactDeathDate3 { get { return _ContactDeathDate3; } set { if (_ContactDeathDate3 != value) { _ContactDeathDate3 = value; RaisePropertyChanged("ContactDeathDate3"); } } }

        public string AttendFuneral { get { return _AttendFuneral; } set { if (_AttendFuneral != value) { _AttendFuneral = value; RaisePropertyChanged("AttendFuneral"); } } }
        public string FuneralNameDeceased1 { get { return _FuneralNameDeceased1; } set { if (_FuneralNameDeceased1 != value) { _FuneralNameDeceased1 = value; RaisePropertyChanged("FuneralNameDeceased1"); } } }
        public string FuneralNameDeceased2 { get { return _FuneralNameDeceased2; } set { if (_FuneralNameDeceased2 != value) { _FuneralNameDeceased2 = value; RaisePropertyChanged("FuneralNameDeceased2"); } } }
        public string FuneralRelationDeceased1 { get { return _FuneralRelationDeceased1; } set { if (_FuneralRelationDeceased1 != value) { _FuneralRelationDeceased1 = value; RaisePropertyChanged("FuneralRelationDeceased1"); } } }
        public string FuneralRelationDeceased2 { get { return _FuneralRelationDeceased2; } set { if (_FuneralRelationDeceased2 != value) { _FuneralRelationDeceased2 = value; RaisePropertyChanged("FuneralRelationDeceased2"); } } }
        public DateTime? FuneralStartDate1 { get { return _FuneralStartDate1; } set { if (_FuneralStartDate1 != value) { _FuneralStartDate1 = value; RaisePropertyChanged("FuneralStartDate1"); } } }
        public DateTime? FuneralStartDate2 { get { return _FuneralStartDate2; } set { if (_FuneralStartDate2 != value) { _FuneralStartDate2 = value; RaisePropertyChanged("FuneralStartDate2"); } } }
        public DateTime? FuneralEndDate1 { get { return _FuneralEndDate1; } set { if (_FuneralEndDate1 != value) { _FuneralEndDate1 = value; RaisePropertyChanged("FuneralEndDate1"); } } }
        public DateTime? FuneralEndDate2 { get { return _FuneralEndDate2; } set { if (_FuneralEndDate2 != value) { _FuneralEndDate2 = value; RaisePropertyChanged("FuneralEndDate2"); } } }
        public string FuneralVillage1 { get { return _FuneralVillage1; } set { if (_FuneralVillage1 != value) { _FuneralVillage1 = value; RaisePropertyChanged("FuneralVillage1"); } } }
        public string FuneralVillage2 { get { return _FuneralVillage2; } set { if (_FuneralVillage2 != value) { _FuneralVillage2 = value; RaisePropertyChanged("FuneralVillage2"); } } }
        public string FuneralDistrict1 { get { return _FuneralDistrict1; } set { if (_FuneralDistrict1 != value) { _FuneralDistrict1 = value; RaisePropertyChanged("FuneralDistrict1"); } } }
        public string FuneralDistrict2 { get { return _FuneralDistrict2; } set { if (_FuneralDistrict2 != value) { _FuneralDistrict2 = value; RaisePropertyChanged("FuneralDistrict2"); } } }
        public string FuneralTouchBody1 { get { return _FuneralTouchBody1; } set { if (_FuneralTouchBody1 != value) { _FuneralTouchBody1 = value; RaisePropertyChanged("FuneralTouchBody1"); } } }
        public string FuneralTouchBody2 { get { return _FuneralTouchBody2; } set { if (_FuneralTouchBody2 != value) { _FuneralTouchBody2 = value; RaisePropertyChanged("FuneralTouchBody2"); } } }

        public string Travel { get { return _Travel; } set { if (_Travel != value) { _Travel = value; RaisePropertyChanged("Travel"); } } }
        public string TravelVillage { get { return _TravelVillage; } set { if (_TravelVillage != value) { _TravelVillage = value; RaisePropertyChanged("TravelVillage"); } } }
        public string TravelDistrict { get { return _TravelDistrict; } set { if (_TravelDistrict != value) { _TravelDistrict = value; RaisePropertyChanged("TravelDistrict"); } } }
        public string TravelCountry { get { return _TravelCountry; } set { if (_TravelCountry != value) { _TravelCountry = value; RaisePropertyChanged("TravelCountry"); } } }
        public DateTime? TravelStartDate { get { return _TravelStartDate; } set { if (_TravelStartDate != value) { _TravelStartDate = value; RaisePropertyChanged("TravelStartDate"); } } }
        public DateTime? TravelEndDate { get { return _TravelEndDate; } set { if (_TravelEndDate != value) { _TravelEndDate = value; RaisePropertyChanged("TravelEndDate"); } } }
        public bool TravelDateEstimated { get { return _TravelDateEstimated; } set { if (_TravelDateEstimated != value) { _TravelDateEstimated = value; RaisePropertyChanged("TravelDateEstimated"); } } }

        public string HospitalBeforeIll { get { return _HospitalBeforeIll; } set { if (_HospitalBeforeIll != value) { _HospitalBeforeIll = value; RaisePropertyChanged("HospitalBeforeIll"); } } }
        public string HospitalBeforeIllPatient { get { return _HospitalBeforeIllPatient; } set { if (_HospitalBeforeIllPatient != value) { _HospitalBeforeIllPatient = value; RaisePropertyChanged("HospitalBeforeIllPatient"); } } }
        public string HospitalBeforeIllHospitalName { get { return _HospitalBeforeIllHospitalName; } set { if (_HospitalBeforeIllHospitalName != value) { _HospitalBeforeIllHospitalName = value; RaisePropertyChanged("HospitalBeforeIllHospitalName"); } } }
        public string HospitalBeforeIllVillage { get { return _HospitalBeforeIllVillage; } set { if (_HospitalBeforeIllVillage != value) { _HospitalBeforeIllVillage = value; RaisePropertyChanged("HospitalBeforeIllVillage"); } } }
        public string HospitalBeforeIllDistrict { get { return _HospitalBeforeIllDistrict; } set { if (_HospitalBeforeIllDistrict != value) { _HospitalBeforeIllDistrict = value; RaisePropertyChanged("HospitalBeforeIllDistrict"); } } }
        public DateTime? HospitalBeforeIllStartDate { get { return _HospitalBeforeIllStartDate; } set { if (_HospitalBeforeIllStartDate != value) { _HospitalBeforeIllStartDate = value; RaisePropertyChanged("HospitalBeforeIllStartDate"); } } }
        public DateTime? HospitalBeforeIllEndDate { get { return _HospitalBeforeIllEndDate; } set { if (_HospitalBeforeIllEndDate != value) { _HospitalBeforeIllEndDate = value; RaisePropertyChanged("HospitalBeforeIllEndDate"); } } }
        public bool HospitalBeforeIllDateEstimated { get { return _HospitalBeforeIllDateEstimated; } set { if (_HospitalBeforeIllDateEstimated != value) { _HospitalBeforeIllDateEstimated = value; RaisePropertyChanged("HospitalBeforeIllDateEstimated"); } } }

        public string TraditionalHealer { get { return _TraditionalHealer; } set { if (_TraditionalHealer != value) { _TraditionalHealer = value; RaisePropertyChanged("TraditionalHealer"); } } }
        public string TraditionalHealerName { get { return _TraditionalHealerName; } set { if (_TraditionalHealerName != value) { _TraditionalHealerName = value; RaisePropertyChanged("TraditionalHealerName"); } } }
        public string TraditionalHealerVillage { get { return _TraditionalHealerVillage; } set { if (_TraditionalHealerVillage != value) { _TraditionalHealerVillage = value; RaisePropertyChanged("TraditionalHealerVillage"); } } }
        public string TraditionalHealerDistrict { get { return _TraditionalHealerDistrict; } set { if (_TraditionalHealerDistrict != value) { _TraditionalHealerDistrict = value; RaisePropertyChanged("TraditionalHealerDistrict"); } } }
        public DateTime? TraditionalHealerDate { get { return _TraditionalHealerDate; } set { if (_TraditionalHealerDate != value) { _TraditionalHealerDate = value; RaisePropertyChanged("TraditionalHealerDate"); } } }
        public bool TraditionalHealerDateEstimated { get { return _TraditionalHealerDateEstimated; } set { if (_TraditionalHealerDateEstimated != value) { _TraditionalHealerDateEstimated = value; RaisePropertyChanged("TraditionalHealerDateEstimated"); } } }

        public string Animals { get { return _Animals; } set { if (_Animals != value) { _Animals = value; RaisePropertyChanged("Animals"); } } }
        public bool AnimalBats { get { return _AnimalBats; } set { if (_AnimalBats != value) { _AnimalBats = value; RaisePropertyChanged("AnimalBats"); } } }
        public bool AnimalPrimates { get { return _AnimalPrimates; } set { if (_AnimalPrimates != value) { _AnimalPrimates = value; RaisePropertyChanged("AnimalPrimates"); } } }
        public bool AnimalRodents { get { return _AnimalRodents; } set { if (_AnimalRodents != value) { _AnimalRodents = value; RaisePropertyChanged("AnimalRodents"); } } }
        public bool AnimalPigs { get { return _AnimalPigs; } set { if (_AnimalPigs != value) { _AnimalPigs = value; RaisePropertyChanged("AnimalPigs"); } } }
        public bool AnimalBirds { get { return _AnimalBirds; } set { if (_AnimalBirds != value) { _AnimalBirds = value; RaisePropertyChanged("AnimalBirds"); } } }
        public bool AnimalCows { get { return _AnimalCows; } set { if (_AnimalCows != value) { _AnimalCows = value; RaisePropertyChanged("AnimalCows"); } } }
        public bool AnimalOther { get { return _AnimalOther; } set { if (_AnimalOther != value) { _AnimalOther = value; RaisePropertyChanged("AnimalOther"); } } }

        public string AnimalBatsStatus { get { return _AnimalBatsStatus; } set { if (_AnimalBatsStatus != value) { _AnimalBatsStatus = value; RaisePropertyChanged("AnimalBatsStatus"); } } }
        public string AnimalPrimatesStatus { get { return _AnimalPrimatesStatus; } set { if (_AnimalPrimatesStatus != value) { _AnimalPrimatesStatus = value; RaisePropertyChanged("AnimalPrimatesStatus"); } } }
        public string AnimalRodentsStatus { get { return _AnimalRodentsStatus; } set { if (_AnimalRodentsStatus != value) { _AnimalRodentsStatus = value; RaisePropertyChanged("AnimalRodentsStatus"); } } }
        public string AnimalPigsStatus { get { return _AnimalPigsStatus; } set { if (_AnimalPigsStatus != value) { _AnimalPigsStatus = value; RaisePropertyChanged("AnimalPigsStatus"); } } }
        public string AnimalBirdsStatus { get { return _AnimalBirdsStatus; } set { if (_AnimalBirdsStatus != value) { _AnimalBirdsStatus = value; RaisePropertyChanged("AnimalBirdsStatus"); } } }
        public string AnimalCowsStatus { get { return _AnimalCowsStatus; } set { if (_AnimalCowsStatus != value) { _AnimalCowsStatus = value; RaisePropertyChanged("AnimalCowsStatus"); } } }
        public string AnimalOtherStatus { get { return _AnimalOtherStatus; } set { if (_AnimalOtherStatus != value) { _AnimalOtherStatus = value; RaisePropertyChanged("AnimalOtherStatus"); } } }
        public string AnimalOtherComment { get { return _AnimalOtherComment; } set { if (_AnimalOtherComment != value) { _AnimalOtherComment = value; RaisePropertyChanged("AnimalOtherComment"); } } }

        public string BittenTick { get { return _BittenTick; } set { if (_BittenTick != value) { _BittenTick = value; RaisePropertyChanged("BittenTick"); } } }

        public string InterviewerName { get { return _InterviewerName; } set { if (_InterviewerName != value) { _InterviewerName = value; RaisePropertyChanged("InterviewerName"); } } }
        public string InterviewerPhone { get { return _InterviewerPhone; } set { if (_InterviewerPhone != value) { _InterviewerPhone = value; RaisePropertyChanged("InterviewerPhone"); } } }
        public string InterviewerEmail { get { return _InterviewerEmail; } set { if (_InterviewerEmail != value) { _InterviewerEmail = value; RaisePropertyChanged("InterviewerEmail"); } } }
        public string InterviewerPosition { get { return _InterviewerPosition; } set { if (_InterviewerPosition != value) { _InterviewerPosition = value; RaisePropertyChanged("InterviewerPosition"); } } }
        public string InterviewerDistrict { get { return _InterviewerDistrict; } set { if (_InterviewerDistrict != value) { _InterviewerDistrict = value; RaisePropertyChanged("InterviewerDistrict"); } } }
        public string InterviewerHealthFacility { get { return _InterviewerHealthFacility; } set { if (_InterviewerHealthFacility != value) { _InterviewerHealthFacility = value; RaisePropertyChanged("InterviewerHealthFacility"); } } }
        public string InterviewerInfoProvidedBy { get { return _InterviewerInfoProvidedBy; } set { if (_InterviewerInfoProvidedBy != value) { _InterviewerInfoProvidedBy = value; RaisePropertyChanged("InterviewerInfoProvidedBy"); } } }
        public string ProxyName { get { return _ProxyName; } set { if (_ProxyName != value) { _ProxyName = value; RaisePropertyChanged("ProxyName"); } } }
        public string ProxyRelation { get { return _ProxyRelation; } set { if (_ProxyRelation != value) { _ProxyRelation = value; RaisePropertyChanged("ProxyRelation"); } } }

        public string CommentsOnThisPatient { get { return _CommentsOnThisPatient; } set { if (_CommentsOnThisPatient != value) { _CommentsOnThisPatient = value; RaisePropertyChanged("CommentsOnThisPatient"); } } }

        #endregion // Properties

        #region Events
        public delegate void EpiCaseDefinitionChangingHandler(object sender, EpiCaseDefinitionChangingEventArgs e);
        public delegate void FieldValueChangingHandler(object sender, FieldValueChangingEventArgs e);

        [field: NonSerialized]
        public event EpiCaseDefinitionChangingHandler EpiCaseDefinitionChanging;
        [field: NonSerialized]
        public event FieldValueChangingHandler CaseIDChanging;
        [field: NonSerialized]
        public event FieldValueChangingHandler CaseSecondaryIDChanging;
        [field: NonSerialized]
        public event EventHandler SwitchToLegacyEnter;
        [field: NonSerialized]
        public event EventHandler MarkedForRemoval;
        [field: NonSerialized]
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        [field: NonSerialized]
        public event EventHandler<DataErrorsChangedEventArgs> WarningsChanged;
        //[field: NonSerialized]
        //public event PropertyChangedEventHandler PropertyChanged;
        [field: NonSerialized]
        public event EventHandler<CaseAddedArgs> Inserted;
        [field: NonSerialized]
        public event EventHandler<CaseChangedArgs> Updated;
        [field: NonSerialized]
        public event EventHandler ViewerClosed;
        #endregion Events

        #region Constructors
        public CaseViewModel(View caseForm, View labForm)
        {
            #region Input Validation
            if (caseForm == null)
            {
                throw new ArgumentNullException("caseForm cannot be null");
            }
            else if (caseForm.Project == null)
            {
                throw new InvalidOperationException("project cannot be null");
            }
            else if (caseForm.Project.CollectedData == null)
            {
                throw new InvalidOperationException("project.CollectedData cannot be null");
            }

            if (labForm == null)
            {
                throw new ArgumentNullException("labForm cannot be null");
            }
            else if (labForm.Project == null)
            {
                throw new InvalidOperationException("project cannot be null");
            }
            else if (labForm.Project.CollectedData == null)
            {
                throw new InvalidOperationException("project.CollectedData cannot be null");
            }
            #endregion // Input Validation

            if (IDPrefixes != null && IDPrefixes.Count >= 1 && !String.IsNullOrEmpty(IDPrefixes[0]))
            {
                if (IDSeparator != null && IDSeparator.Length == 1)
                {
                    int lastIndexOf = IDPrefixes[0].LastIndexOf(IDSeparator[0]);

                    if (lastIndexOf == -1)
                    {
                        ID = IDPrefixes[0] + IDSeparator;
                    }
                    else
                    {
                        ID = IDPrefixes[0].Substring(0, lastIndexOf) + IDSeparator;
                    }
                }
            }

            Construct();
            RecordId = System.Guid.NewGuid().ToString();
            IsNewRecord = true;
            CaseForm = caseForm;
            LabForm = labForm;
        }

        public CaseViewModel(View caseForm, View labForm, DataRow row)
        {
            #region Input Validation
            if (caseForm == null)
            {
                throw new ArgumentNullException("caseForm cannot be null");
            }
            else if (caseForm.Project == null)
            {
                throw new InvalidOperationException("project cannot be null");
            }
            else if (caseForm.Project.CollectedData == null)
            {
                throw new InvalidOperationException("project.CollectedData cannot be null");
            }

            if (labForm == null)
            {
                throw new ArgumentNullException("labForm cannot be null");
            }
            else if (labForm.Project == null)
            {
                throw new InvalidOperationException("project cannot be null");
            }
            else if (labForm.Project.CollectedData == null)
            {
                throw new InvalidOperationException("project.CollectedData cannot be null");
            }

            if (row == null)
            {
                throw new ArgumentNullException("row cannot be null");
            }
            #endregion // Input Validation

            Construct();
            IsNewRecord = false;
            CaseForm = caseForm;
            LabForm = labForm;
            LoadPartial(row);
            HasUnsavedChanges = false;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="caseToCopy">The case data to copy into the case being constructed</param>
        public CaseViewModel(CaseViewModel caseToCopy)
        {
            Construct();
            CopyCase(caseToCopy);

            IsNewRecord = caseToCopy.IsNewRecord;
            HasUnsavedChanges = false;
        }

        public CaseViewModel(View caseForm, View labForm, ContactViewModel contactVM, int uniqueKey)
        {
            Construct();
            CreateCaseFromContact(contactVM, uniqueKey);
            IsNewRecord = true;
            HasUnsavedChanges = false;

            CaseForm = caseForm;
            LabForm = labForm;
        }
        #endregion // Constructors

        #region Methods
        private void Construct()
        {
            LabResults = new LabResultCollectionMaster();
            SourceCases = new ObservableCollection<SourceCaseInfoViewModel>();
            FieldValueChanges = new ObservableCollection<FieldValueChange>();

            System.Windows.Data.BindingOperations.EnableCollectionSynchronization(Errors, _errorsLock);
            System.Windows.Data.BindingOperations.EnableCollectionSynchronization(SourceCases, _labLock);
            System.Windows.Data.BindingOperations.EnableCollectionSynchronization(LabResults, _labLock);
            System.Windows.Data.BindingOperations.EnableCollectionSynchronization(FieldValueChanges, _fvLock);

            SuppressValidation = true;
        }

        public Task ValidateAsync()
        {
            return Task.Run(() => Validate());
        }

        public virtual void Validate()
        {
            if (SuppressValidation || IsValidating)
            {
                return;
            }

            IsValidating = true;

            Errors.Clear();
            Warnings.Clear();

            List<string> fieldNames = new List<string>();

            var properties = typeof(CaseViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                if (!property.Name.Equals("HasErrors") &&
                    !property.Name.Equals("IsLocked") &&
                    !property.Name.Equals("IsActive") &&
                    !property.Name.Equals("IsContact") &&
                    !property.Name.Equals("IsEditing") &&
                    !property.Name.Equals("IsSaving") &&
                    !property.Name.Equals("IsValidating") &&
                    !property.Name.EndsWith("Command") &&
                    !property.Name.Equals("IsLoading"))
                {
                    fieldNames.Add(property.Name.ToString());
                }
            }

            //fieldNames.Clear();
            //fieldNames.Add("ID");

            //List<string> fieldNames = Data.Keys.ToList();
            //foreach (KeyValuePair<string, object> kvp in _data)
            foreach (string fieldName in fieldNames)
            {
                string message = this[fieldName].Trim();
                if (!String.IsNullOrEmpty(message))
                {
                    if (fieldName == "ID")
                    {
                        if (Warnings.ContainsKey(fieldName))
                        {
                            List<string> messages = new List<string>();
                            bool success = Warnings.TryGetValue(fieldName, out messages);
                            if (success)
                            {
                                messages.Add(message);
                                OnWarningsChanged(fieldName);
                            }
                        }
                        else
                        {
                            List<string> messages = new List<string>() { message };
                            Warnings.TryAdd(fieldName, messages);
                        }
                    }
                    else
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

        public void OnWarningsChanged(string propertyName)
        {
            var handler = WarningsChanged;
            if (handler != null)
                handler(this, new DataErrorsChangedEventArgs(propertyName));
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
                return null;
            }
        }

        /// <summary>
        /// Updates the case's in-memory data with its corresponding record from the database. This method only retrieves a small 
        /// subset of the data for performance reasons.
        /// </summary>
        public void LoadPartial()
        {
            IsLoading = true;

            if (CaseForm.Project.CollectedData == null) return;

            IDbDriver db = CaseForm.Project.CollectedData.GetDatabase();
            //17178 Starts
            Query selectQuery = null;
            if (IsCountryUS)
            {
                selectQuery = db.CreateQuery("SELECT t.GlobalRecordId, ID, FirstSaveTime, LastSaveTime, OrigID, Surname, OtherNames, Age,  Gender, StatusReport, " +
               "RecComplete, RecNoCRF, RecMissingCRFInfo, DateReport, RecPendLab, RecPendOutcome, DateDeath, DateDeath2, DateOnset, DistrictOnset, SCOnset,   VillageOnset, DateDischargeIso, DateHospitalCurrentAdmit, DateIsolationCurrent, " +
               "DateIsolationCurrent, PlaceDeath, HCW, StatusAsOfCurrentDate, FinalLabClass, FinalStatus, DistrictRes, CountryOnset, AddressRes, ZipRes, Citizenship, CountryRes, VillageRes, SCRes, t.UniqueKey, EpiCaseDef, IsolationCurrent, HospitalCurrent, " +
               "DateDischargeHosp, DOB, Email, HCWposition, HCWFacility, HCWDistrict, HCWSC, HCWVillage, PhoneNumber " + //17227 Added DOB,PhoneNumber, Email, HCWposition, HCWFacility, HCWDistrict, HCWSC, HCWVillage
               CaseForm.FromViewSQL + " " +
               "WHERE t.GlobalRecordId = @GlobalRecordId");
            }
            else
            {
                selectQuery = db.CreateQuery("SELECT t.GlobalRecordId, FirstSaveTime, LastSaveTime, ID, OrigID, Surname, OtherNames, Age, AgeUnit, Gender, HeadHouse, StatusReport, " +
                               "RecComplete, RecNoCRF, RecMissingCRFInfo, DateReport, RecPendLab, RecPendOutcome, DateDeath, DateDeath2, DateOnset, DistrictOnset, SCOnset, CountryOnset, VillageOnset, DateDischargeIso, DateHospitalCurrentAdmit, DateIsolationCurrent, " +
                               "DateIsolationCurrent, PlaceDeath, HCW, StatusAsOfCurrentDate, FinalLabClass, FinalStatus, DistrictRes, CountryRes, VillageRes, SCRes, ParishRes, t.UniqueKey, EpiCaseDef, IsolationCurrent, HospitalCurrent, " +
                               "DateDischargeHosp " +
                               CaseForm.FromViewSQL + " " +
                               "WHERE t.GlobalRecordId = @GlobalRecordId");
            }
            //17178 Ends

            selectQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, RecordId));
            DataTable dt = db.Select(selectQuery);

            if (dt.Rows.Count == 1)
            {
                DataRow row = dt.Rows[0];
                LoadPartial(row);
            }
            IsLoading = false;
        }

        /// <summary>
        /// Updates the case's in-memory data with its corresponding record from the database. This method only retrieves a small 
        /// subset of the data for performance reasons.
        /// </summary>
        public void LoadPartial(DataRow row)
        {
            #region Input Validation
            if (row == null)
            {
                throw new ArgumentNullException("row cannot be null");
            }
            #endregion // Input Validation

            Surname = row["Surname"].ToString();
            ID = row["ID"].ToString();
            OriginalID = row["OrigID"].ToString();
            RecordId = row["GlobalRecordId"].ToString();
            OtherNames = row["OtherNames"].ToString();
            IsolationCurrent = row["IsolationCurrent"].ToString();
            CurrentHospital = row["HospitalCurrent"].ToString();


            StatusReport = row["StatusReport"].ToString();

            if (row["DateReport"] != DBNull.Value) DateReport = DateTime.Parse(row["DateReport"].ToString());

            PlaceOfDeath = row["PlaceDeath"].ToString();

            if (!String.IsNullOrEmpty(row["DateDischargeHosp"].ToString()))
            {
                DateDischargeHospital = DateTime.Parse(row["DateDischargeHosp"].ToString());
            }

            string hcwValue = row["HCW"].ToString();
            if (!String.IsNullOrEmpty(hcwValue))
            {
                IsHCW = bool.Parse(hcwValue);
            }

            Sex = row["Gender"].ToString();
            FinalCaseStatus = row["FinalStatus"].ToString().Trim();

            CurrentStatus = row["StatusAsOfCurrentDate"].ToString().Trim();

            bool recComplete = String.IsNullOrEmpty(row["RecComplete"].ToString()) ? false : bool.Parse(row["RecComplete"].ToString());
            bool recNoCRF = String.IsNullOrEmpty(row["RecNoCRF"].ToString()) ? false : bool.Parse(row["RecNoCRF"].ToString());
            bool recMissingCRFInfo = String.IsNullOrEmpty(row["RecMissingCRFInfo"].ToString()) ? false : bool.Parse(row["RecMissingCRFInfo"].ToString());
            bool recPendLab = String.IsNullOrEmpty(row["RecPendLab"].ToString()) ? false : bool.Parse(row["RecPendLab"].ToString());
            bool recPendOutcome = String.IsNullOrEmpty(row["RecPendOutcome"].ToString()) ? false : bool.Parse(row["RecPendOutcome"].ToString());

            if (recComplete == true)
            {
                RecordStatus = RecComplete;// "Complete";
                RecordStatusComplete = "1";
            }
            else
            {
                WordBuilder wb = new WordBuilder(";");
                if (recNoCRF == true)
                {
                    wb.Add(RecNoCRF /*"No CRF"*/);
                    RecordStatusComplete = "0";
                    RecordStatusNoCRF = "1";
                }
                if (recMissingCRFInfo == true)
                {
                    wb.Add(RecMissCRF /*"Missing CRF info"*/);
                    RecordStatusComplete = "0";
                    RecordStatusMissCRF = "1";
                }
                if (recPendLab == true)
                {
                    wb.Add(RecPendingLab /*"Pending lab"*/);
                    RecordStatusComplete = "0";
                    RecordStatusPenLab = "1";
                }
                if (recPendOutcome == true)
                {
                    wb.Add(RecPendingOutcome /*"Pending outcome"*/);
                    RecordStatusComplete = "0";
                    RecordStatusPenOut = "1";
                }
                RecordStatus = wb.ToString();
            }

            if (!String.IsNullOrEmpty(row["Age"].ToString()))
            {
                Age = double.Parse(row["Age"].ToString());
            }
            if (!String.IsNullOrEmpty(row["DateOnset"].ToString()))
            {
                DateOnset = DateTime.Parse(row["DateOnset"].ToString());
            }
            if (!String.IsNullOrEmpty(row["DateDischargeIso"].ToString()))
            {
                DateDischargeIso = DateTime.Parse(row["DateDischargeIso"].ToString());
            }
            if (!String.IsNullOrEmpty(row["DateIsolationCurrent"].ToString()))
            {
                DateIsolationCurrent = DateTime.Parse(row["DateIsolationCurrent"].ToString());
            }
            if (!String.IsNullOrEmpty(row["DateDeath"].ToString()))
            {
                DateDeath = DateTime.Parse(row["DateDeath"].ToString());
            }
            if (!String.IsNullOrEmpty(row["DateHospitalCurrentAdmit"].ToString()))
            {
                DateHospitalCurrentAdmit = DateTime.Parse(row["DateHospitalCurrentAdmit"].ToString());
            }

            DistrictOnset = row["DistrictOnset"].ToString().Trim();
            //17178 Starts
            if (IsCountryUS)
            {
                Citizenship = row["Citizenship"].ToString().Trim();
                AddressRes = row["AddressRes"].ToString().Trim();
                ZipRes = row["ZipRes"].ToString().Trim();
                if (row["DOB"] != DBNull.Value) DOB = DateTime.Parse(row["DOB"].ToString());
                Email = row["Email"].ToString();
                PhoneNumber = row["PhoneNumber"].ToString();
                OccupationHCWPosition = row["HCWposition"].ToString();
                OccupationHCWFacility = row["HCWFacility"].ToString();
                OccupationHCWDistrict = row["HCWDistrict"].ToString();
                OccupationHCWSC = row["HCWSC"].ToString();
                OccupationHCWVillage = row["HCWVillage"].ToString();
            }
            else
            {
                HeadOfHousehold = row["HeadHouse"].ToString();
                AgeUnitString = row["AgeUnit"].ToString().Trim();
                Parish = row["ParishRes"].ToString().Trim();
            }
            //17178 Ends

            CountryOnset = row["CountryOnset"].ToString().Trim();
            SubCountyOnset = row["SCOnset"].ToString().Trim();
            FinalLabClassification = row["FinalLabClass"].ToString().Trim();
            District = row["DistrictRes"].ToString().Trim();

            Country = row["CountryRes"].ToString().Trim();
            Village = row["VillageRes"].ToString().Trim();
            VillageOnset = row["VillageOnset"].ToString().Trim();
            SubCounty = row["SCRes"].ToString().Trim();
            UniqueKey = Convert.ToInt32(row["UniqueKey"]);

            EpiCaseClassification = row["EpiCaseDef"].ToString().Trim();

            if (!String.IsNullOrEmpty(row["DateDeath2"].ToString()))
            {
                DateDeath2 = DateTime.Parse(row["DateDeath2"].ToString());
            }
        }

        /// <summary>
        /// Updates the case's in-memory data with its corresponding record from the database. This method only retrieves patient
        /// outcome data.
        /// </summary>
        private void LoadOutcomeCaseData()
        {
            IDbDriver db = CaseForm.Project.CollectedData.GetDatabase();

            RenderableField field = CaseForm.Fields["DateOutcomeComp"] as RenderableField;
            if (field != null)
            {
                string tableName = field.Page.TableName;

                Query selectQuery = db.CreateQuery("SELECT * FROM [" + tableName + "] " +
                    "WHERE [GlobalRecordId] = @GlobalRecordId");
                selectQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, RecordId));
                DataTable dt = db.Select(selectQuery);

                if (dt.Rows.Count == 1)
                {
                    DataRow row = dt.Rows[0];

                    if (row["DateOutcomeComp"] != DBNull.Value)
                    {
                        DateOutcomeInfoCompleted = Convert.ToDateTime(row["DateOutcomeComp"]);
                    }
                    BleedUnexplainedEver = row["BleedUnexplainedEver"].ToString();
                    SpecifyBleeding = row["SpecifyBleeding"].ToString();
                    HospitalDischarge = row["HospitalDischarge"].ToString();
                    HospitalDischargeDistrict = row["HospitalDischargeDistrict"].ToString();

                    if (!String.IsNullOrEmpty(row["DateDischargeHosp"].ToString()))
                    {
                        DateDischargeHospital = Convert.ToDateTime(row["DateDischargeHosp"]);
                    }
                    DateDischargeHospitalEst = String.IsNullOrEmpty(row["DateDischargeHospEst"].ToString()) ? false : bool.Parse(row["DateDischargeHospEst"].ToString()); //bool.Parse(row["DateDischargeHospEst"].ToString());

                    if (!String.IsNullOrEmpty(row["DateDeath2"].ToString()))
                    {
                        DateDeath2 = Convert.ToDateTime(row["DateDeath2"]);
                    }
                    DateDeath2Est = String.IsNullOrEmpty(row["DateDeath2Estimated"].ToString()) ? false : bool.Parse(row["DateDeath2Estimated"].ToString());//bool.Parse(row["DateDeath2Estimated"].ToString());
                    DateDeath2EstSpecify = row["DateDeath2EstSpecify"].ToString();
                    PlaceDeath = row["PlaceDeath"].ToString();
                    HospitalDeath = row["HospitalDeath"].ToString();
                    PlaceDeathOther = row["PlaceDeathOther"].ToString();
                    VillageDeath = row["VillageDeath"].ToString().Trim();
                    SubCountyDeath = row["SCDeath"].ToString().Trim();
                    DistrictDeath = row["DistrictDeath"].ToString().Trim();
                    if (!String.IsNullOrEmpty(row["DateFuneral"].ToString()))
                    {
                        DateFuneral = Convert.ToDateTime(row["DateFuneral"]);
                    }
                    FuneralConductedFam = String.IsNullOrEmpty(row["FuneralConductedFam"].ToString()) ? false : bool.Parse(row["FuneralConductedFam"].ToString());//bool.Parse(row["FuneralConductedFam"].ToString());
                    FuneralConductedOutbreakTeam = String.IsNullOrEmpty(row["FuneralConducteOutTeam"].ToString()) ? false : bool.Parse(row["FuneralConducteOutTeam"].ToString());//bool.Parse(row["FuneralConducteOutTeam"].ToString());
                    VillageFuneral = row["VillageFuneral"].ToString().Trim();
                    SubCountyFuneral = row["SCFuneral"].ToString().Trim();
                    DistrictFuneral = row["DistrictFuneral"].ToString().Trim();

                    SymptomFeverFinal = row["FeverFinal"].ToString();
                    if (!String.IsNullOrEmpty(row["TempFinal"].ToString()))
                    {
                        SymptomFeverTempFinal = Double.Parse(row["TempFinal"].ToString());
                    }
                    SymptomFeverTempSourceFinal = row["TempSourceFinal"].ToString();
                    SymptomVomitingFinal = row["VomitingFinal"].ToString();
                    SymptomDiarrheaFinal = row["DiarrheaFinal"].ToString();
                    SymptomFatigueFinal = row["FatigueFinal"].ToString();
                    SymptomAnorexiaFinal = row["AnorexiaFinal"].ToString();
                    SymptomAbdPainFinal = row["AbdPainFinal"].ToString();
                    SymptomChestPainFinal = row["ChestPainFinal"].ToString();
                    SymptomMusclePainFinal = row["MusclePainFinal"].ToString();
                    SymptomJointPainFinal = row["JointPainFinal"].ToString();
                    SymptomHeadacheFinal = row["HeadacheFinal"].ToString();
                    SymptomCoughFinal = row["CoughFinal"].ToString();
                    SymptomDiffBreatheFinal = row["DiffBreatheFinal"].ToString();
                    SymptomDiffSwallowFinal = row["DiffSwallowFinal"].ToString();
                    SymptomSoreThroatFinal = row["SoreThroatFinal"].ToString();
                    SymptomJaundiceFinal = row["JaundiceFinal"].ToString();
                    SymptomConjunctivitisFinal = row["ConjunctivitisFinal"].ToString();
                    SymptomRashFinal = row["RashFinal"].ToString();
                    SymptomHiccupsFinal = row["HiccupsFinal"].ToString();
                    SymptomPainEyesFinal = row["PainEyesFinal"].ToString();
                    SymptomUnconsciousFinal = row["UnconsciousFinal"].ToString();
                    SymptomConfusedFinal = row["ConfusedFinal"].ToString();
                    SymptomOtherHemoFinal = row["OtherHemoFinal"].ToString();
                    SymptomOtherHemoFinalSpecify = row["OtherHemoFinalSpecify"].ToString();

                    CommentsOnThisPatient = row["CommentsonthisPatient"].ToString();
                }
            }
        }

        /// <summary>
        /// Updates the last sample fields for this case record
        /// </summary>
        private void RefreshLastSampleFields()
        {
            var query = from result in LabResults
                        where result.DateSampleCollected.HasValue
                        orderby result.DateSampleCollected descending
                        select result;

            if (query.Count() > 0)
            {
                LabResultViewModel labResult = query.First() as LabResultViewModel;

                if (labResult != null)
                {
                    DateLastLabSampleCollected = labResult.DateSampleCollected;
                    DateLastLabSampleTested = labResult.DateSampleTested;
                    LastSampleInterpret = labResult.SampleInterpret;
                }
            }
            else
            {
                DateLastLabSampleCollected = null;
                DateLastLabSampleTested = null;
                LastSampleInterpret = String.Empty;
            }
        }

        private void SetCurrentStatus()
        {
            // set current status
            if (String.IsNullOrEmpty(StatusReport) && String.IsNullOrEmpty(FinalCaseStatus))
            {
                CurrentStatus = String.Empty;
            }
            else if (String.IsNullOrEmpty(StatusReport) && FinalCaseStatus == "2")
            {
                CurrentStatus = Alive;
            }
            else if (String.IsNullOrEmpty(StatusReport) && FinalCaseStatus == "1")
            {
                CurrentStatus = Dead;
            }

            if (StatusReport == "2" && String.IsNullOrEmpty(FinalCaseStatus))
            {
                CurrentStatus = Alive;
            }
            else if (StatusReport == "2" && FinalCaseStatus == "2")
            {
                CurrentStatus = Alive;
            }
            else if (StatusReport == "2" && FinalCaseStatus == "1")
            {
                CurrentStatus = Dead;
            }

            if (StatusReport == "1" && String.IsNullOrEmpty(FinalCaseStatus))
            {
                CurrentStatus = Dead;
            }
            else if (StatusReport == "1" && FinalCaseStatus == "2")
            {
                CurrentStatus = Dead;
            }
            else if (StatusReport == "1" && FinalCaseStatus == "1")
            {
                CurrentStatus = Dead;
            }
        }

        /// <summary>
        /// Updates the case's in-memory data with its corresponding record from the database.
        /// </summary>
        public void Load()
        {
            LoadPartial();

            LabResults.Clear();
            SourceCases.Clear();
            IsLoading = true;
            IDbDriver db = CaseForm.Project.CollectedData.GetDatabase();

            RenderableField field1 = CaseForm.Fields["ID"] as RenderableField;
            RenderableField field2 = CaseForm.Fields["DateOnset"] as RenderableField;
            RenderableField field3 = CaseForm.Fields["HospitalizedCurrent"] as RenderableField;
            RenderableField field4 = CaseForm.Fields["Contact"] as RenderableField;
            RenderableField field5 = CaseForm.Fields["Animals"] as RenderableField;

            RenderableField fieldLabSpecId = LabForm.Fields["FieldLabSpecID"] as RenderableField;

            if (field1 != null && field2 != null && field3 != null && field4 != null && field5 != null && fieldLabSpecId != null)
            {
                //Query sourceCasesQuery = db.CreateQuery("SELECT * FROM [metaLinks] WHERE FromRecordGuid = @FromRecordGuid AND FromViewId = @FromViewId AND ToViewId = @ToViewId");
                //sourceCasesQuery.Parameters.Add(new QueryParameter("@FromRecordGuid", DbType.String, RecordId));
                //sourceCasesQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, CaseForm.Id));
                //sourceCasesQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, CaseForm.Id));
                //DataTable sourceCasesTable = new DataTable();

                Query labSelectQuery = db.CreateQuery("SELECT * " + LabForm.FromViewSQL + " WHERE RecStatus = 1 AND FKEY = @GlobalRecordId ORDER BY [DateSampleCollected]");
                labSelectQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, RecordId));
                DataTable labTable = new DataTable();

                string tableName1 = field1.Page.TableName;
                Query selectQuery1 = db.CreateQuery("SELECT * FROM [" + tableName1 + "] " +
                    "WHERE [GlobalRecordId] = @GlobalRecordId");
                selectQuery1.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, RecordId));
                DataTable dt1 = new DataTable();

                string tableName2 = field2.Page.TableName;
                Query selectQuery2 = db.CreateQuery("SELECT * FROM [" + tableName2 + "] " +
                    "WHERE [GlobalRecordId] = @GlobalRecordId");
                selectQuery2.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, RecordId));
                DataTable dt2 = new DataTable();

                string tableName3 = field3.Page.TableName;
                Query selectQuery3 = db.CreateQuery("SELECT * FROM [" + tableName3 + "] " +
                    "WHERE [GlobalRecordId] = @GlobalRecordId");
                selectQuery3.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, RecordId));
                DataTable dt3 = new DataTable();

                string tableName4 = field4.Page.TableName;
                Query selectQuery4 = db.CreateQuery("SELECT * FROM [" + tableName4 + "] " +
                    "WHERE [GlobalRecordId] = @GlobalRecordId");
                selectQuery4.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, RecordId));
                DataTable dt4 = new DataTable();

                string tableName5 = field5.Page.TableName;
                Query selectQuery5 = db.CreateQuery("SELECT * FROM [" + tableName5 + "] " +
                    "WHERE [GlobalRecordId] = @GlobalRecordId");
                selectQuery5.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, RecordId));
                DataTable dt5 = new DataTable();

                Parallel.Invoke(
                    () =>
                    {
                        LoadOutcomeCaseData();
                    },
                    () =>
                    {
                        dt3 = db.Select(selectQuery3);
                    },
                    () =>
                    {
                        dt4 = db.Select(selectQuery4);
                    },
                    () =>
                    {
                        dt5 = db.Select(selectQuery5);
                    },
                    () =>
                    {
                        dt1 = db.Select(selectQuery1);
                    },
                    () =>
                    {
                        dt2 = db.Select(selectQuery2);
                    },
                    () =>
                    {
                        labTable = db.Select(labSelectQuery);
                    },
                    () =>
                    {
                        //sourceCasesTable = db.Select(sourceCasesQuery);
                    });

                /////////////////////////////////////////////////////////////////////////////////
                /////////////////////////////////////////////////////////////////////////////////
                // Lab Results
                /////////////////////////////////////////////////////////////////////////////////
                /////////////////////////////////////////////////////////////////////////////////


                if (labTable.Rows.Count >= 1)
                {
                    int i = 1;
                    foreach (DataRow row in labTable.Rows)
                    {
                        LabResultViewModel labResult = new LabResultViewModel(LabForm);
                        labResult.CaseVM = this;
                        try
                        {
                            labResult.Load(row);
                            LabResults.Add(labResult);
                            labResult.SampleNumber = SampleLabel + " " + i.ToString();
                            i++;
                        }
                        catch (Exception)
                        {
                            // don't load it
                        }
                    }

                    RefreshLastSampleFields();
                }

                //if (sourceCasesTable.Rows.Count >= 1)
                //{
                //    foreach (DataRow row in sourceCasesTable.Rows)
                //    {
                //        SourceCaseInfoViewModel sourceCase = new SourceCaseInfoViewModel();
                //        sourceCase.ID = row["ToRecordGuid"].ToString();
                //        sourceCase.DateLastContact = (DateTime)row["LastContactDate"];
                //        if (row["Tentative"] != DBNull.Value)
                //        {
                //            int tentative = int.Parse(row["Tentative"].ToString());
                //            if (tentative == 1)
                //            {
                //                sourceCase.Tentative = true;
                //            }
                //            else
                //            {
                //                sourceCase.Tentative = false;
                //            }
                //        }
                //        if (row["IsEstimatedContactDate"] != DBNull.Value)
                //        {
                //            sourceCase.Estimated = bool.Parse(row["IsEstimatedContactDate"].ToString());
                //        }

                //        Query selectQuery = db.CreateQuery("SELECT ID, Surname, OtherNames, DistrictRes, VillageRes " + CaseForm.FromViewSQL + " WHERE t.GlobalRecordId = @GlobalRecordId");
                //        selectQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, sourceCase.ID));
                //        DataTable sourceCaseTable = db.Select(selectQuery);

                //        if (sourceCaseTable.Rows.Count == 1)
                //        {
                //            DataRow iRow = sourceCaseTable.Rows[0];
                //            sourceCase.ID = iRow["ID"].ToString();
                //            sourceCase.LastName = iRow["Surname"].ToString();
                //            sourceCase.FirstName = iRow["OtherNames"].ToString();
                //            sourceCase.Adm1 = iRow["DistrictRes"].ToString();
                //            sourceCase.Adm4 = iRow["VillageRes"].ToString();
                //        }

                //        SourceCases.Add(sourceCase);
                //    }
                //}


                /////////////////////////////////////////////////////////////////////////////////
                /////////////////////////////////////////////////////////////////////////////////
                // Page 4
                /////////////////////////////////////////////////////////////////////////////////
                /////////////////////////////////////////////////////////////////////////////////


                if (dt4.Rows.Count == 1)
                {
                    #region Table 4 (Contacts and Funerals)
                    DataRow row = dt4.Rows[0];

                    HadContact = row["Contact"].ToString();
                    ContactName1 = row["ContactName1"].ToString();
                    ContactName2 = row["ContactName2"].ToString();
                    ContactName3 = row["ContactName3"].ToString();
                    ContactRelation1 = row["ContactRelation1"].ToString();
                    ContactRelation2 = row["ContactRelation2"].ToString();
                    ContactRelation3 = row["ContactRelation3"].ToString();
                    if (row["ContactDateStart1"] != DBNull.Value) ContactStartDate1 = DateTime.Parse(row["ContactDateStart1"].ToString());
                    if (row["ContactDateStart2"] != DBNull.Value) ContactStartDate2 = DateTime.Parse(row["ContactDateStart2"].ToString());
                    if (row["ContactDateStart3"] != DBNull.Value) ContactStartDate3 = DateTime.Parse(row["ContactDateStart3"].ToString());
                    if (row["ContactDateEnd1"] != DBNull.Value) ContactEndDate1 = DateTime.Parse(row["ContactDateEnd1"].ToString());
                    if (row["ContactDateEnd2"] != DBNull.Value) ContactEndDate2 = DateTime.Parse(row["ContactDateEnd2"].ToString());
                    if (row["ContactDateEnd3"] != DBNull.Value) ContactEndDate3 = DateTime.Parse(row["ContactDateEnd3"].ToString());
                    ContactDate1Estimated = String.IsNullOrEmpty(row["EstimatedContactDate1"].ToString()) ? false : bool.Parse(row["EstimatedContactDate1"].ToString());//bool.Parse(row["EstimatedContactDate1"].ToString());
                    ContactDate2Estimated = String.IsNullOrEmpty(row["EstimatedContactDate2"].ToString()) ? false : bool.Parse(row["EstimatedContactDate2"].ToString());//bool.Parse(row["EstimatedContactDate2"].ToString());
                    ContactDate3Estimated = String.IsNullOrEmpty(row["EstimatedContactDate3"].ToString()) ? false : bool.Parse(row["EstimatedContactDate3"].ToString());//bool.Parse(row["EstimatedContactDate3"].ToString());
                    ContactVillage1 = row["ContactVillage1"].ToString().Trim();
                    ContactVillage2 = row["ContactVillage2"].ToString().Trim();
                    ContactVillage3 = row["ContactVillage3"].ToString().Trim();
                    ContactDistrict1 = row["ContactDistrict1"].ToString().Trim();
                    ContactDistrict2 = row["ContactDistrict2"].ToString().Trim();
                    ContactDistrict3 = row["ContactDistrict3"].ToString().Trim();
                    //ContactCountry1 = row["Contact"].ToString();
                    //ContactCountry2 = row["Contact"].ToString();
                    //ContactCountry3 = row["Contact"].ToString();
                    //TypesOfContact1 = row["Contact"].ToString();
                    //TypesOfContact2 = row["Contact"].ToString();
                    //TypesOfContact3 = row["Contact"].ToString();

                    WordBuilder wb = new WordBuilder(",");

                    try
                    {
                        if (String.IsNullOrEmpty(row["Contact11"].ToString()) ? false : bool.Parse(row["Contact11"].ToString())) wb.Append("1");
                        if (String.IsNullOrEmpty(row["Contact2"].ToString()) ? false : bool.Parse(row["Contact2"].ToString())) wb.Append("2");
                        if (String.IsNullOrEmpty(row["Contact3"].ToString()) ? false : bool.Parse(row["Contact3"].ToString())) wb.Append("3");
                        if (String.IsNullOrEmpty(row["Contact4"].ToString()) ? false : bool.Parse(row["Contact4"].ToString())) wb.Append("4");

                        TypesOfContact1 = wb.ToString();
                    }
                    catch (Exception)
                    {
                    }

                    try
                    {
                        wb = new WordBuilder(",");

                        if (String.IsNullOrEmpty(row["Contact12"].ToString()) ? false : bool.Parse(row["Contact12"].ToString())) wb.Append("1");
                        if (String.IsNullOrEmpty(row["Contact22"].ToString()) ? false : bool.Parse(row["Contact22"].ToString())) wb.Append("2");
                        if (String.IsNullOrEmpty(row["Contact32"].ToString()) ? false : bool.Parse(row["Contact32"].ToString())) wb.Append("3");
                        if (String.IsNullOrEmpty(row["Contact42"].ToString()) ? false : bool.Parse(row["Contact42"].ToString())) wb.Append("4");
                        TypesOfContact2 = wb.ToString();
                    }
                    catch (Exception)
                    {
                    }

                    try
                    {
                        wb = new WordBuilder(",");
                        if (String.IsNullOrEmpty(row["Contact13"].ToString()) ? false : bool.Parse(row["Contact13"].ToString())) wb.Append("1");
                        if (String.IsNullOrEmpty(row["Contact23"].ToString()) ? false : bool.Parse(row["Contact23"].ToString())) wb.Append("2");
                        if (String.IsNullOrEmpty(row["Contact33"].ToString()) ? false : bool.Parse(row["Contact33"].ToString())) wb.Append("3");
                        if (String.IsNullOrEmpty(row["Contact43"].ToString()) ? false : bool.Parse(row["Contact43"].ToString())) wb.Append("4");
                        TypesOfContact3 = wb.ToString();
                    }
                    catch (Exception)
                    {
                    }

                    ContactStatus1 = row["ContactStatus1"].ToString();
                    ContactStatus2 = row["ContactStatus2"].ToString();
                    ContactStatus3 = row["ContactStatus3"].ToString();
                    if (row["ContactDateDeath1"] != DBNull.Value) ContactDeathDate1 = DateTime.Parse(row["ContactDateDeath1"].ToString());
                    if (row["ContactDateDeath2"] != DBNull.Value) ContactDeathDate2 = DateTime.Parse(row["ContactDateDeath2"].ToString());
                    if (row["ContactDateDeath3"] != DBNull.Value) ContactDeathDate3 = DateTime.Parse(row["ContactDateDeath3"].ToString());
                    AttendFuneral = row["Funeral"].ToString();
                    FuneralNameDeceased1 = row["FuneralName1"].ToString();
                    FuneralNameDeceased2 = row["FuneralName2"].ToString();
                    FuneralRelationDeceased1 = row["FuneralRelation1"].ToString();
                    FuneralRelationDeceased2 = row["FuneralRelation2"].ToString();
                    if (row["FuneralDateStart1"] != DBNull.Value) FuneralStartDate1 = DateTime.Parse(row["FuneralDateStart1"].ToString());
                    if (row["FuneralDateStart2"] != DBNull.Value) FuneralStartDate2 = DateTime.Parse(row["FuneralDateStart2"].ToString());
                    if (row["FuneralDateEnd1"] != DBNull.Value) FuneralEndDate1 = DateTime.Parse(row["FuneralDateEnd1"].ToString());
                    if (row["FuneralDateEnd2"] != DBNull.Value) FuneralEndDate2 = DateTime.Parse(row["FuneralDateEnd2"].ToString());
                    FuneralVillage1 = row["FuneralVillage1"].ToString();
                    FuneralVillage2 = row["FuneralVillage2"].ToString();
                    FuneralDistrict1 = row["FuneralDistrict1"].ToString();
                    FuneralDistrict2 = row["FuneralDistrict2"].ToString();
                    FuneralTouchBody1 = row["FuneralTouchBody1"].ToString();
                    FuneralTouchBody2 = row["FuneralTouchBody2"].ToString();

                    Travel = row["Travel"].ToString();
                    TravelVillage = row["TravelVillage"].ToString().Trim();
                    TravelDistrict = row["TravelDistrict"].ToString().Trim();

                    if (row.Table.Columns.Contains("CountryTravelled"))
                    {
                        TravelCountry = row["CountryTravelled"].ToString().Trim();
                    }

                    if (row["TravelDateStart"] != DBNull.Value) TravelStartDate = DateTime.Parse(row["TravelDateStart"].ToString());
                    if (row["TravelDateEnd"] != DBNull.Value) TravelEndDate = DateTime.Parse(row["TravelDateEnd"].ToString());
                    TravelDateEstimated = String.IsNullOrEmpty(row["EstTravelDate"].ToString()) ? false : bool.Parse(row["EstTravelDate"].ToString());//bool.Parse(row["EstTravelDate"].ToString());

                    HospitalBeforeIll = row["HospitalBeforeIll"].ToString().Trim();
                    HospitalBeforeIllPatient = row["HospitalBeforeIllPatient"].ToString();
                    HospitalBeforeIllHospitalName = row["HospitalBeforeIllName"].ToString();
                    HospitalBeforeIllVillage = row["HospitalBeforeIllVillage"].ToString().Trim();
                    HospitalBeforeIllDistrict = row["HospitalBeforeIllDistrict"].ToString().Trim();
                    if (row["HospitalBeforeIllDateStart"] != DBNull.Value) HospitalBeforeIllStartDate = DateTime.Parse(row["HospitalBeforeIllDateStart"].ToString());
                    if (row["HospitalBeforeIllDateEnd"] != DBNull.Value) HospitalBeforeIllEndDate = DateTime.Parse(row["HospitalBeforeIllDateEnd"].ToString());
                    HospitalBeforeIllDateEstimated = String.IsNullOrEmpty(row["EstimatedHospitalBeforeIll"].ToString()) ? false : bool.Parse(row["EstimatedHospitalBeforeIll"].ToString());//bool.Parse(row["EstimatedHospitalBeforeIll"].ToString());

                    TraditionalHealer = row["TradHealer"].ToString();
                    TraditionalHealerName = row["TradHealerName"].ToString();
                    TraditionalHealerVillage = row["TradHealerVillage"].ToString().Trim();
                    TraditionalHealerDistrict = row["TradHealerDistrict"].ToString().Trim();
                    if (row["TradHealerDate"] != DBNull.Value) TraditionalHealerDate = DateTime.Parse(row["TradHealerDate"].ToString());
                    TraditionalHealerDateEstimated = String.IsNullOrEmpty(row["EstTradHealerDate"].ToString()) ? false : bool.Parse(row["EstTradHealerDate"].ToString());//bool.Parse(row["EstTradHealerDate"].ToString());
                    #endregion // Table 4 (Contacts and Funerals)
                }

                /////////////////////////////////////////////////////////////////////////////////
                /////////////////////////////////////////////////////////////////////////////////
                // Page 5
                /////////////////////////////////////////////////////////////////////////////////
                /////////////////////////////////////////////////////////////////////////////////

                //dt = Database.Select(selectQuery);

                if (dt5.Rows.Count == 1)
                {
                    #region Table 5 (Animals)
                    DataRow row = dt5.Rows[0];

                    Animals = row["Animals"].ToString();
                    AnimalBats = String.IsNullOrEmpty(row["AnimalBats"].ToString()) ? false : bool.Parse(row["AnimalBats"].ToString());//bool.Parse(row["AnimalBats"].ToString());
                    AnimalPrimates = String.IsNullOrEmpty(row["AnimalPrimates"].ToString()) ? false : bool.Parse(row["AnimalPrimates"].ToString());//bool.Parse(row["AnimalPrimates"].ToString());
                    AnimalRodents = String.IsNullOrEmpty(row["AnimalRodents"].ToString()) ? false : bool.Parse(row["AnimalRodents"].ToString());//bool.Parse(row["AnimalRodents"].ToString());
                    AnimalPigs = String.IsNullOrEmpty(row["AnimalPigs"].ToString()) ? false : bool.Parse(row["AnimalPigs"].ToString());//bool.Parse(row["AnimalPigs"].ToString());
                    AnimalBirds = String.IsNullOrEmpty(row["AnimalBirds"].ToString()) ? false : bool.Parse(row["AnimalBirds"].ToString());//bool.Parse(row["AnimalBirds"].ToString());
                    AnimalCows = String.IsNullOrEmpty(row["AnimalCows"].ToString()) ? false : bool.Parse(row["AnimalCows"].ToString());//bool.Parse(row["AnimalCows"].ToString());
                    AnimalOther = String.IsNullOrEmpty(row["AnimalOther"].ToString()) ? false : bool.Parse(row["AnimalOther"].ToString());//bool.Parse(row["AnimalOther"].ToString());
                    AnimalOtherComment = row["AnimalOtherComment"].ToString();

                    AnimalBatsStatus = row["AnimalBatsStatus"].ToString();
                    AnimalPrimatesStatus = row["AnimalPrimatesStatus"].ToString();
                    AnimalRodentsStatus = row["AnimalRodentStatus"].ToString();
                    AnimalPigsStatus = row["AnimalPigsStatus"].ToString();
                    AnimalBirdsStatus = row["AnimalBirdsStatus"].ToString();
                    AnimalCowsStatus = row["AnimalCowStatus"].ToString();
                    AnimalOtherStatus = row["AnimalOtherStatus"].ToString();

                    BittenTick = row["BittenTick"].ToString();

                    InterviewerName = row["InterviewerName"].ToString();
                    InterviewerPhone = row["InterviewerPhone"].ToString();
                    InterviewerEmail = row["InterviwerEmail"].ToString();
                    InterviewerPosition = row["InterviewerPosition"].ToString();
                    InterviewerDistrict = row["InterviewerDistrict"].ToString();
                    InterviewerHealthFacility = row["InterviewerHealthFacility"].ToString();
                    InterviewerInfoProvidedBy = row["InfoProvidedBy"].ToString();
                    ProxyName = row["ProxyName"].ToString();
                    ProxyRelation = row["ProxyRelation"].ToString();
                    #endregion // Table 5 (Animals)
                }

                /////////////////////////////////////////////////////////////////////////////////
                /////////////////////////////////////////////////////////////////////////////////
                // Page 3
                /////////////////////////////////////////////////////////////////////////////////
                /////////////////////////////////////////////////////////////////////////////////

                if (dt3.Rows.Count == 1)
                {
                    #region Table 3 (Hospitalizations)
                    DataRow row = dt3.Rows[0];

                    HospitalizedCurrent = row["HospitalizedCurrent"].ToString();

                    if (row["DateHospitalCurrentAdmit"] != DBNull.Value) DateHospitalCurrentAdmit = DateTime.Parse(row["DateHospitalCurrentAdmit"].ToString());

                    CurrentHospital = row["HospitalCurrent"].ToString();

                    VillageHosp = row["VillageHospitalCurrent"].ToString().Trim();
                    DistrictHosp = row["DistrictHospitalCurrent"].ToString().Trim();
                    SubCountyHosp = row["SCHospitalCurrent"].ToString().Trim();

                    if (row.Table.Columns.Contains("CountryHospitalCurrent"))
                    {
                        CountryHosp = row["CountryHospitalCurrent"].ToString().Trim();
                    }
                    else
                    {
                        CountryHosp = String.Empty;
                    }

                    HospitalizedPast = row["HospitalizedPast"].ToString();

                    if (row["DateHospitalPastStart1"] != DBNull.Value) DateHospitalPastStart1 = DateTime.Parse(row["DateHospitalPastStart1"].ToString());
                    if (row["DateHospitalPastEnd1"] != DBNull.Value) DateHospitalPastEnd1 = DateTime.Parse(row["DateHospitalPastEnd1"].ToString());

                    if (row["DateHospitalPastStart2"] != DBNull.Value) DateHospitalPastStart2 = DateTime.Parse(row["DateHospitalPastStart2"].ToString());
                    if (row["DateHospitalPastEnd2"] != DBNull.Value) DateHospitalPastEnd2 = DateTime.Parse(row["DateHospitalPastEnd2"].ToString());

                    HospitalPast1 = row["HospitalPast1"].ToString();
                    HospitalPast2 = row["HospitalPast2"].ToString();

                    IsolationPast1 = row["IsolationPast1"].ToString();
                    IsolationPast2 = row["IsolationPast2"].ToString();

                    HospitalVillage1 = row["VillageHospitalPast1"].ToString();
                    HospitalVillage2 = row["VillageHospitalPast2"].ToString();

                    HospitalDistrict1 = row["DistrictHospitalPast1"].ToString();
                    HospitalDistrict2 = row["DistrictHospitalPast2"].ToString();
                    #endregion // Table 3 (Hospitalizations)
                }

                /////////////////////////////////////////////////////////////////////////////////
                /////////////////////////////////////////////////////////////////////////////////
                // Page 2
                /////////////////////////////////////////////////////////////////////////////////
                /////////////////////////////////////////////////////////////////////////////////

                if (dt2.Rows.Count == 1)
                {
                    #region Table 2 (Symptoms)
                    DataRow row = dt2.Rows[0];

                    DateOnsetEst = String.IsNullOrEmpty(row["DateOnsetEstimated"].ToString()) ? false : bool.Parse(row["DateOnsetEstimated"].ToString());//bool.Parse(row["DateOnsetEstimated"].ToString());
                    DateOnsetEstExplain = row["SpecifyOnset"].ToString();

                    SymptomFever = row["Fever"].ToString();
                    if (!String.IsNullOrEmpty(row["Temp"].ToString()))
                    {
                        SymptomFeverTemp = double.Parse(row["Temp"].ToString());
                    }
                    SymptomFeverTempSource = row["TempSource"].ToString();
                    SymptomVomiting = row["Vomiting"].ToString();
                    SymptomDiarrhea = row["Diarrhea"].ToString();
                    SymptomFatigue = row["Fatigue"].ToString();
                    SymptomAnorexia = row["Anorexia"].ToString();
                    SymptomAbdPain = row["AbdPain"].ToString();
                    SymptomChestPain = row["ChestPain"].ToString();
                    SymptomMusclePain = row["MusclePain"].ToString();
                    SymptomJointPain = row["JointPain"].ToString();
                    SymptomHeadache = row["Headache"].ToString();
                    SymptomCough = row["Cough"].ToString();
                    SymptomDiffBreathe = row["DiffBreathe"].ToString();
                    SymptomDiffSwallow = row["DiffSwallow"].ToString();
                    SymptomSoreThroat = row["SoreThroat"].ToString();
                    SymptomJaundice = row["Jaundice"].ToString();
                    SymptomConjunctivitis = row["Conjunctivitis"].ToString();
                    SymptomRash = row["Rash"].ToString();
                    SymptomHiccups = row["Hiccups"].ToString();
                    SymptomPainEyes = row["PainEyes"].ToString();
                    SymptomUnconscious = row["Unconscious"].ToString();
                    SymptomConfused = row["Confused"].ToString();
                    SymptomOtherHemo = row["BleedOther"].ToString();
                    SymptomOtherHemoSpecify = row["BleedOtherComment"].ToString();
                    SymptomOtherNonHemorrhagic = row["SymptOther"].ToString();
                    SymptomOtherNonHemorrhagicSpecify = row["SymptOtherComment"].ToString();

                    SymptomUnexplainedBleeding = row["Unexplainedbleeding"].ToString();
                    SymptomBleedGums = row["BleedGums"].ToString();
                    SymptomBleedInjectionSite = row["BleedInject"].ToString();
                    SymptomNoseBleed = row["BleedNose"].ToString();
                    SymptomBloodyStool = row["BleedStool"].ToString();
                    SymptomHematemesis = row["Hematemesis"].ToString();
                    SymptomBloodVomit = row["BloodVomit"].ToString();
                    SymptomCoughBlood = row["BloodCough"].ToString();
                    SymptomBleedVagina = row["BleedVagina"].ToString();
                    SymptomBleedSkin = row["BleedSkin"].ToString();
                    SymptomBleedUrine = row["BleedUrine"].ToString();
                    #endregion // Table 2 (Symptoms)
                }

                /////////////////////////////////////////////////////////////////////////////////
                /////////////////////////////////////////////////////////////////////////////////
                // Page 1
                /////////////////////////////////////////////////////////////////////////////////
                /////////////////////////////////////////////////////////////////////////////////

                //dt = Database.Select(selectQuery);

                if (dt1.Rows.Count == 1)
                {
                    #region Table 1 (Demographics)
                    DataRow row = dt1.Rows[0];

                    OriginalID = row["OrigID"].ToString();
                    PhoneNumber = row["PhoneNumber"].ToString();
                    PhoneOwner = row["PhoneOwner"].ToString();
                    StatusReport = row["StatusReport"].ToString();
                    if (!IsCountryUS) //17178
                    {
                        Parish = row["ParishRes"].ToString();
                    }


                    OccupationFarmer = String.IsNullOrEmpty(row["Farmer"].ToString()) ? false : bool.Parse(row["Farmer"].ToString());
                    OccupationButcher = String.IsNullOrEmpty(row["Butcher"].ToString()) ? false : bool.Parse(row["Butcher"].ToString());
                    OccupationHunter = String.IsNullOrEmpty(row["Hunter"].ToString()) ? false : bool.Parse(row["Hunter"].ToString());
                    OccupationMiner = String.IsNullOrEmpty(row["Miner"].ToString()) ? false : bool.Parse(row["Miner"].ToString());
                    OccupationReligious = String.IsNullOrEmpty(row["Religiousleader"].ToString()) ? false : bool.Parse(row["Religiousleader"].ToString());
                    OccupationHousewife = String.IsNullOrEmpty(row["Housewife"].ToString()) ? false : bool.Parse(row["Housewife"].ToString());
                    OccupationStudent = String.IsNullOrEmpty(row["Student"].ToString()) ? false : bool.Parse(row["Student"].ToString());
                    OccupationChild = String.IsNullOrEmpty(row["Child"].ToString()) ? false : bool.Parse(row["Child"].ToString());
                    OccupationBusinessman = String.IsNullOrEmpty(row["Business"].ToString()) ? false : bool.Parse(row["Business"].ToString());
                    OccupationTransporter = String.IsNullOrEmpty(row["Transporter"].ToString()) ? false : bool.Parse(row["Transporter"].ToString());
                    IsHCW = String.IsNullOrEmpty(row["HCW"].ToString()) ? false : bool.Parse(row["HCW"].ToString());

                    OccupationTraditionalHealer = String.IsNullOrEmpty(row["TraditionalHealer"].ToString()) ? false : bool.Parse(row["TraditionalHealer"].ToString());
                    OccupationOther = String.IsNullOrEmpty(row["OtherOccup"].ToString()) ? false : bool.Parse(row["OtherOccup"].ToString());

                    //OccupationFarmer = bool.Parse(row["Farmer"].ToString());
                    //OccupationButcher = bool.Parse(row["Butcher"].ToString());
                    //OccupationHunter = bool.Parse(row["Hunter"].ToString());
                    //OccupationMiner = bool.Parse(row["Miner"].ToString());
                    //OccupationReligious = bool.Parse(row["Religiousleader"].ToString());
                    //OccupationHousewife = bool.Parse(row["Housewife"].ToString());
                    //OccupationStudent = bool.Parse(row["Student"].ToString());
                    //OccupationChild = bool.Parse(row["Child"].ToString());
                    //OccupationBusinessman = bool.Parse(row["Business"].ToString());
                    //OccupationTransporter = bool.Parse(row["Transporter"].ToString());
                    //IsHCW = bool.Parse(row["HCW"].ToString());
                    OccupationHCWPosition = row["HCWPosition"].ToString();
                    //OccupationTraditionalHealer = bool.Parse(row["TraditionalHealer"].ToString());
                    //OccupationOther = bool.Parse(row["OtherOccup"].ToString());
                    OccupationTransporterSpecify = row["TransporterType"].ToString();
                    OccupationBusinessSpecify = row["BusinessType"].ToString();

                    OccupationOtherSpecify = row["OtherOccupDetail"].ToString();
                    OccupationHCWFacility = row["HCWFacility"].ToString();
                    OccupationBusinessSpecify = row["BusinessType"].ToString();

                    VillageOnset = row["VillageOnset"].ToString().Trim();
                    DistrictOnset = row["DistrictOnset"].ToString().Trim();
                    SubCountyOnset = row["SCOnset"].ToString().Trim();
                    if (IsCountryUS)//17178
                    {
                        Citizenship = row["Citizenship"].ToString().Trim();
                        AddressRes = row["AddressRes"].ToString().Trim();
                        ZipRes = row["ZipRes"].ToString().Trim();
                    }

                    CountryOnset = row["CountryOnset"].ToString().Trim();

                    if (row["LatitudeOnset"] != DBNull.Value) Latitude = double.Parse(row["LatitudeOnset"].ToString());
                    if (row["LongitudeOnset"] != DBNull.Value) Longitude = double.Parse(row["LongitudeOnset"].ToString());

                    if (row["DateOnsetLocalStart"] != DBNull.Value) DateOnsetLocalStart = DateTime.Parse(row["DateOnsetLocalStart"].ToString());
                    if (row["DateOnsetLocalEnd"] != DBNull.Value) DateOnsetLocalEnd = DateTime.Parse(row["DateOnsetLocalEnd"].ToString());
                    #endregion // Table 1 (Demographics)
                }

                if (OccupationBusinessman || OccupationButcher || OccupationChild || OccupationFarmer || OccupationHousewife || OccupationHunter || OccupationMiner || OccupationOther || OccupationReligious || OccupationStudent || OccupationTraditionalHealer || OccupationTransporter)
                {
                    IsOtherOccupation = true;
                }
                else
                {
                    IsOtherOccupation = false;
                }
            }

            HasUnsavedChanges = false;

            IsLoading = false;

            FieldValueChanges.Clear();

            OriginalEpiCaseDef = EpiCaseDef;
            OriginalCaseID = ID;

            this.IsChanged = false; //defaults to false when page loads. issue#17054
            _localCopy = new CaseViewModel(this);
        }

        //Fix for 17109 - Updates the flag before adding it to CaseCollection
        public void UpdateIsNewRecord()
        {
            IsNewRecord = false;
        }

        private async void SaveAsync()
        {
            await Task.Factory.StartNew(delegate
            {
                Save();
            }).ContinueWith(t =>
            {
                _localCopy = new CaseViewModel(this);
                FieldValueChanges.Clear();
                CommandManager.InvalidateRequerySuggested();

            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        ///// <summary>
        ///// Updates the database with the in-memory representation of this case. This method only updates a subset of the records
        ///// for performance reasons.
        ///// </summary>
        //public void SavePartial()
        //{
        //    throw new NotImplementedException();

        //    //IsSaving = true;
        //    //IsSaving = false;
        //}

        /// <summary>
        /// Saves the in-memory representation of this case to disk
        /// </summary>
        public void Save()
        {
            IsSaving = true;

            bool exists = false;

            IDbDriver db = CaseForm.Project.CollectedData.GetDatabase();

            Query existsQuery = db.CreateQuery("SELECT GlobalRecordId FROM " + CaseForm.TableName + " WHERE GlobalRecordId = @GlobalRecordId");
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
                // new case record; do an INSERT
                InsertIntoDatabase();
                if (Inserted != null)
                {
                    CaseAddedArgs args = new CaseAddedArgs(this);
                    Inserted(this, args);
                }

                DbLogger.Log(String.Format(
                    "Inserted case : ID = {0}, EpiCaseDef = {1}, GUID = {2}",
                        ID, EpiCaseDef, RecordId));
            }
            else
            {
                // existing case record; do an UPDATE
                UpdateInDatabase();

                if (Updated != null)
                {
                    CaseChangedArgs args = new CaseChangedArgs(this, OriginalEpiCaseDef, OriginalCaseID);
                    Updated(this, args);
                }

                DbLogger.Log(String.Format(
                    "Updated case : ID = {0}, EpiCaseDef = {1}, PrevID = {3}, PrevEpiCaseDef = {4}, GUID = {2}",
                        ID, EpiCaseDef, RecordId, _localCopy.ID, _localCopy.EpiCaseDef));
            }

            HasUnsavedChanges = false;
            IsNewRecord = false;
            OriginalEpiCaseDef = EpiCaseDef;
            OriginalCaseID = ID;

            IsSaving = false;
        }

        /// <summary>
        /// Updates this case record's first page in the database with the in-memory representation
        /// </summary>
        private void UpdateInDatabasePage1()
        {
            RenderableField field1 = CaseForm.Fields["ID"] as RenderableField;
            IDbDriver db = CaseForm.Project.CollectedData.GetDatabase();

            if (field1 != null && field1.Page != null)
            {
                //17178 Starts
                Query updateQuery = null;
                if (IsCountryUS)
                {
                    updateQuery = db.CreateQuery("UPDATE [" + field1.Page.TableName + "] SET " +
                        "[ID] = @ID, " +
                        "[OrigID] = @OrigID, " +
                        "[StatusAsOfCurrentDate] = @StatusAsOfCurrentDate, " +
                        "[EpiCaseDef] = @EpiCaseDef, " +
                        "[DateReport] = @DateReport, " +
                        "[Surname] = @Surname, " +
                        "[OtherNames] = @OtherNames, " +
                        "[Age] = @Age, " +
                        //"[AgeUnit] = @AgeUnit, " +
                    "[RecComplete] = @RecComplete, " +
                    "[RecNoCRF] = @RecNoCRF, " +
                    "[RecMissingCRFInfo] = @RecMissingCRFInfo, " +
                    "[RecPendLab] = @RecPendLab, " +
                        "[RecPendOutcome] = @RecPendOutcome, " +
                        "[Gender] = @Gender, " +
                        "[PhoneNumber] = @PhoneNumber, " +
                        "[PhoneOwner] = @PhoneOwner, " +
                        "[StatusReport] = @StatusReport, " +
                        "[DateDeath] = @DateDeath, " +
                        //"[HeadHouse] = @HeadHouse, " +
                        "[VillageRes] = @VillageRes, " +
                        "[AddressRes] = @AddressRes, " +
                        "[ZipRes] = @ZipRes, " +
                        "[Citizenship] = @Citizenship, " +
                        "[CountryRes] = @CountryRes, " +
                        "[DistrictRes] = @DistrictRes, " +
                        "[SCRes] = @SCRes, " +
                        "[Farmer] = @Farmer, " +
                        "[Butcher] = @Butcher, " +
                        "[Hunter] = @Hunter, " +
                        "[Miner] = @Miner, " +
                        "[Religiousleader] = @Religiousleader, " +
                        "[Housewife] = @Housewife, " +
                        "[Student] = @Student, " +
                        "[Child] = @Child, " +
                        "[TraditionalHealer] = @TraditionalHealer, " +
                        "[Business] = @Business, " +
                        "[Transporter] = @Transporter, " +
                        "[HCW] = @HCW, " +
                        "[OtherOccup] = @OtherOccup, " +
                        "[BusinessType] = @BusinessType, " +
                        "[TransporterType] = @TransporterType, " +
                        "[HCWPosition] = @HCWPosition, " +
                        "[HCWFacility] = @HCWFacility, " +
                        "[OtherOccupDetail] = @OtherOccupDetail, " +
                        "[VillageOnset] = @VillageOnset, " +
                        "[CountryOnset] = @CountryOnset, " +
                        "[SCOnset] = @SCOnset, " +
                        "[DistrictOnset] = @DistrictOnset " +
                        //"[Latitude] = @Latitude, " +
                        //"[Longitude] = @Longitude " +

                        "WHERE [GlobalRecordId] = @GlobalRecordId");
                }
                else
                {
                    updateQuery = db.CreateQuery("UPDATE [" + field1.Page.TableName + "] SET " +
                         "[ID] = @ID, " +
                         "[OrigID] = @OrigID, " +
                         "[StatusAsOfCurrentDate] = @StatusAsOfCurrentDate, " +
                         "[EpiCaseDef] = @EpiCaseDef, " +
                         "[DateReport] = @DateReport, " +
                         "[Surname] = @Surname, " +
                         "[OtherNames] = @OtherNames, " +
                         "[Age] = @Age, " +
                         "[AgeUnit] = @AgeUnit, " +
                     "[RecComplete] = @RecComplete, " +
                     "[RecNoCRF] = @RecNoCRF, " +
                     "[RecMissingCRFInfo] = @RecMissingCRFInfo, " +
                     "[RecPendLab] = @RecPendLab, " +
                         "[RecPendOutcome] = @RecPendOutcome, " +
                         "[Gender] = @Gender, " +
                         "[PhoneNumber] = @PhoneNumber, " +
                         "[PhoneOwner] = @PhoneOwner, " +
                         "[StatusReport] = @StatusReport, " +
                         "[DateDeath] = @DateDeath, " +
                         "[HeadHouse] = @HeadHouse, " +
                         "[VillageRes] = @VillageRes, " +
                         "[ParishRes] = @ParishRes, " +
                         "[CountryRes] = @CountryRes, " +
                         "[DistrictRes] = @DistrictRes, " +
                         "[SCRes] = @SCRes, " +
                         "[Farmer] = @Farmer, " +
                         "[Butcher] = @Butcher, " +
                         "[Hunter] = @Hunter, " +
                         "[Miner] = @Miner, " +
                         "[Religiousleader] = @Religiousleader, " +
                         "[Housewife] = @Housewife, " +
                         "[Student] = @Student, " +
                         "[Child] = @Child, " +
                         "[TraditionalHealer] = @TraditionalHealer, " +
                         "[Business] = @Business, " +
                         "[Transporter] = @Transporter, " +
                         "[HCW] = @HCW, " +
                         "[OtherOccup] = @OtherOccup, " +
                         "[BusinessType] = @BusinessType, " +
                         "[TransporterType] = @TransporterType, " +
                         "[HCWPosition] = @HCWPosition, " +
                         "[HCWFacility] = @HCWFacility, " +
                         "[OtherOccupDetail] = @OtherOccupDetail, " +
                         "[VillageOnset] = @VillageOnset, " +
                         "[CountryOnset] = @CountryOnset, " +
                         "[SCOnset] = @SCOnset, " +
                         "[DistrictOnset] = @DistrictOnset " +
                        //"[Latitude] = @Latitude, " +
                        //"[Longitude] = @Longitude " +

                         "WHERE [GlobalRecordId] = @GlobalRecordId");
                }
                //17178 Ends

                updateQuery.Parameters.Add(new QueryParameter("@ID", DbType.String, ID));
                updateQuery.Parameters.Add(new QueryParameter("@OrigID", DbType.String, OriginalID));
                updateQuery.Parameters.Add(new QueryParameter("@StatusAsOfCurrentDate", DbType.String, CurrentStatus));

                updateQuery.Parameters.Add(new QueryParameter("@EpiCaseDef", DbType.String, EpiCaseClassification));
                if (DateReport.HasValue)
                {
                    updateQuery.Parameters.Add(new QueryParameter("@DateReport", DbType.DateTime, DateReport.Value));
                }
                else
                {
                    updateQuery.Parameters.Add(new QueryParameter("@DateReport", DbType.DateTime, DBNull.Value));
                }
                updateQuery.Parameters.Add(new QueryParameter("@Surname", DbType.String, Surname));
                updateQuery.Parameters.Add(new QueryParameter("@OtherNames", DbType.String, OtherNames));
                if (Age.HasValue)
                {
                    updateQuery.Parameters.Add(new QueryParameter("@Age", DbType.Double, Age.Value));
                }
                else
                {
                    updateQuery.Parameters.Add(new QueryParameter("@Age", DbType.Double, DBNull.Value));
                }

                if (!IsCountryUS)//17178
                {
                    if (AgeUnit.HasValue)
                    {
                        updateQuery.Parameters.Add(new QueryParameter("@AgeUnit", DbType.String, AgeUnitString));
                    }
                    else
                    {
                        updateQuery.Parameters.Add(new QueryParameter("@AgeUnit", DbType.String, String.Empty));
                    }

                }

                //Fix for issue # 17109 
                //Added fields in Update query.
                if (RecordStatusComplete == "1")
                {
                    updateQuery.Parameters.Add(new QueryParameter("@RecComplete", DbType.Boolean, true));
                }
                else
                {
                    updateQuery.Parameters.Add(new QueryParameter("@RecComplete", DbType.Boolean, false));
                }

                if (RecordStatusNoCRF == "1")
                {
                    updateQuery.Parameters.Add(new QueryParameter("@RecNoCRF", DbType.Boolean, true));
                }
                else
                {
                    updateQuery.Parameters.Add(new QueryParameter("@RecNoCRF", DbType.Boolean, false));
                }

                if (RecordStatusMissCRF == "1")
                {
                    updateQuery.Parameters.Add(new QueryParameter("@RecMissingCRFInfo", DbType.Boolean, true));
                }
                else
                {
                    updateQuery.Parameters.Add(new QueryParameter("@RecMissingCRFInfo", DbType.Boolean, false));
                }

                if (RecordStatusPenLab == "1")
                {
                    updateQuery.Parameters.Add(new QueryParameter("@RecPendLab", DbType.Boolean, true));
                }
                else
                {
                    updateQuery.Parameters.Add(new QueryParameter("@RecPendLab", DbType.Boolean, false));
                }

                if (RecordStatusPenOut == "1")
                {
                    updateQuery.Parameters.Add(new QueryParameter("@RecPendOutcome", DbType.Boolean, true));
                }
                else
                {
                    updateQuery.Parameters.Add(new QueryParameter("@RecPendOutcome", DbType.Boolean, false));
                }


                updateQuery.Parameters.Add(new QueryParameter("@Gender", DbType.String, Sex));
                updateQuery.Parameters.Add(new QueryParameter("@PhoneNumber", DbType.String, PhoneNumber));
                updateQuery.Parameters.Add(new QueryParameter("@PhoneOwner", DbType.String, PhoneOwner));
                updateQuery.Parameters.Add(new QueryParameter("@StatusReport", DbType.String, StatusReport));

                if (DateDeath.HasValue)
                {
                    updateQuery.Parameters.Add(new QueryParameter("@DateDeath", DbType.DateTime, DateDeath.Value));
                }
                else
                {
                    updateQuery.Parameters.Add(new QueryParameter("@DateDeath", DbType.DateTime, DBNull.Value));
                }



                if (IsCountryUS)
                {
                    //17178
                    updateQuery.Parameters.Add(new QueryParameter("@VillageRes", DbType.String, Village));
                    updateQuery.Parameters.Add(new QueryParameter("@AddressRes", DbType.String, AddressRes));
                    updateQuery.Parameters.Add(new QueryParameter("@ZipRes", DbType.String, ZipRes));
                    updateQuery.Parameters.Add(new QueryParameter("@Citizenship", DbType.String, Citizenship));
                }
                else
                {
                    updateQuery.Parameters.Add(new QueryParameter("@HeadHouse", DbType.String, HeadOfHousehold));
                    //17178
                    updateQuery.Parameters.Add(new QueryParameter("@VillageRes", DbType.String, Village));
                    updateQuery.Parameters.Add(new QueryParameter("@ParishRes", DbType.String, Parish));
                }

                updateQuery.Parameters.Add(new QueryParameter("@CountryRes", DbType.String, Country));
                updateQuery.Parameters.Add(new QueryParameter("@DistrictRes", DbType.String, District));
                updateQuery.Parameters.Add(new QueryParameter("@SCRes", DbType.String, SubCounty));
                updateQuery.Parameters.Add(new QueryParameter("@Farmer", DbType.Boolean, OccupationFarmer));
                updateQuery.Parameters.Add(new QueryParameter("@Butcher", DbType.Boolean, OccupationButcher));
                updateQuery.Parameters.Add(new QueryParameter("@Hunter", DbType.Boolean, OccupationHunter));
                updateQuery.Parameters.Add(new QueryParameter("@Miner", DbType.Boolean, OccupationMiner));
                updateQuery.Parameters.Add(new QueryParameter("@Religiousleader", DbType.Boolean, OccupationReligious));
                updateQuery.Parameters.Add(new QueryParameter("@Housewife", DbType.Boolean, OccupationHousewife));
                updateQuery.Parameters.Add(new QueryParameter("@Student", DbType.Boolean, OccupationStudent));
                updateQuery.Parameters.Add(new QueryParameter("@Child", DbType.Boolean, OccupationChild));
                updateQuery.Parameters.Add(new QueryParameter("@TraditionalHealer", DbType.Boolean, OccupationTraditionalHealer));
                updateQuery.Parameters.Add(new QueryParameter("@Business", DbType.Boolean, OccupationBusinessman));
                updateQuery.Parameters.Add(new QueryParameter("@Transporter", DbType.Boolean, OccupationTransporter));
                updateQuery.Parameters.Add(new QueryParameter("@HCW", DbType.Boolean, IsHCW.Value));
                updateQuery.Parameters.Add(new QueryParameter("@OtherOccup", DbType.Boolean, OccupationOther));
                updateQuery.Parameters.Add(new QueryParameter("@BusinessType", DbType.String, OccupationBusinessSpecify));
                updateQuery.Parameters.Add(new QueryParameter("@TransporterType", DbType.String, OccupationTransporterSpecify));
                updateQuery.Parameters.Add(new QueryParameter("@HCWPosition", DbType.String, OccupationHCWPosition));
                updateQuery.Parameters.Add(new QueryParameter("@HCWFacility", DbType.String, OccupationHCWFacility));
                updateQuery.Parameters.Add(new QueryParameter("@OtherOccupDetail", DbType.String, OccupationOtherSpecify));
                updateQuery.Parameters.Add(new QueryParameter("@VillageOnset", DbType.String, VillageOnset));
                //updateQuery.Parameters.Add(new QueryParameter("@ParishOnset", DbType.String, ParishOnset));
                updateQuery.Parameters.Add(new QueryParameter("@CountryOnset", DbType.String, CountryOnset));
                updateQuery.Parameters.Add(new QueryParameter("@SCOnset", DbType.String, SubCountyOnset));
                updateQuery.Parameters.Add(new QueryParameter("@DistrictOnset", DbType.String, DistrictOnset));
                //updateQuery.Parameters.Add(new QueryParameter("@Latitude", DbType.Single, DBNull.Value));
                //updateQuery.Parameters.Add(new QueryParameter("@Longitude", DbType.Single, DBNull.Value));

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
            RenderableField field = CaseForm.Fields["DateOnset"] as RenderableField;
            IDbDriver db = CaseForm.Project.CollectedData.GetDatabase();

            if (field != null && field.Page != null)
            {
                Query updateQuery = db.CreateQuery("UPDATE [" + field.Page.TableName + "] SET " +

                    "[DateOnset] = @DateOnset, " +
                    "[DateOnsetEstimated] = @DateOnsetEstimated, " +
                    "[SpecifyOnset] = @SpecifyOnset, " +
                    "[AbdPain] = @AbdPain, " +
                    "[Anorexia] = @Anorexia, " +
                    "[BleedGums] = @BleedGums, " +
                    "[BleedInject] = @BleedInject, " +
                    "[Unexplainedbleeding] = @Unexplainedbleeding, " +
                    "[BleedVagina] = @BleedVagina, " +
                    "[BleedUrine] = @BleedUrine, " +
                    "[Hematemesis] = @Hematemesis, " +
                    "[BleedStool] = @BleedStool, " +
                    "[BleedSkin] = @BleedSkin, " +
                    "[ChestPain] = @ChestPain, " +
                    "[Confused] = @Confused, " +
                    "[Conjunctivitis] = @Conjunctivitis, " +
                    "[Cough] = @Cough, " +
                    "[BloodCough] = @BloodCough, " +
                    "[Diarrhea] = @Diarrhea, " +
                    "[DiffBreathe] = @DiffBreathe, " +
                    "[DiffSwallow] = @DiffSwallow, " +
                    "[BloodVomit] = @BloodVomit, " +
                    "[Fatigue] = @Fatigue, " +
                    "[Fever] = @Fever, " +
                    "[Headache] = @Headache, " +
                    "[Hiccups] = @Hiccups, " +
                    "[Jaundice] = @Jaundice, " +
                    "[JointPain] = @JointPain, " +
                    "[MusclePain] = @MusclePain, " +
                    "[BleedNose] = @BleedNose, " +
                    "[BleedOther] = @BleedOther, " +
                    "[BleedOtherComment] = @BleedOtherComment, " +
                    "[SymptOther] = @SymptOther, " +
                    "[SymptOtherComment] = @SymptOtherComment, " +
                    "[PainEyes] = @PainEyes, " +
                    "[Rash] = @Rash, " +
                    "[SoreThroat] = @SoreThroat, " +
                    "[Unconscious] = @Unconscious, " +
                    "[Vomiting] = @Vomiting " +

                    "WHERE [GlobalRecordId] = @GlobalRecordId");

                if (DateOnset.HasValue)
                {
                    updateQuery.Parameters.Add(new QueryParameter("@DateOnset", DbType.DateTime, DateOnset.Value));
                }
                else
                {
                    updateQuery.Parameters.Add(new QueryParameter("@DateOnset", DbType.DateTime, DBNull.Value));
                }

                updateQuery.Parameters.Add(new QueryParameter("@DateOnsetEstimated", DbType.Boolean, DateOnsetEst));
                updateQuery.Parameters.Add(new QueryParameter("@SpecifyOnset", DbType.String, DateOnsetEstExplain));

                updateQuery.Parameters.Add(new QueryParameter("@AbdPain", DbType.String, SymptomAbdPain));
                updateQuery.Parameters.Add(new QueryParameter("@Anorexia", DbType.String, SymptomAnorexia));
                updateQuery.Parameters.Add(new QueryParameter("@BleedGums", DbType.String, SymptomBleedGums));
                updateQuery.Parameters.Add(new QueryParameter("@BleedInject", DbType.String, SymptomBleedInjectionSite));
                updateQuery.Parameters.Add(new QueryParameter("@Unexplainedbleeding", DbType.String, SymptomUnexplainedBleeding));
                updateQuery.Parameters.Add(new QueryParameter("@BleedVagina", DbType.String, SymptomBleedVagina));
                updateQuery.Parameters.Add(new QueryParameter("@BleedUrine", DbType.String, SymptomBleedUrine));
                updateQuery.Parameters.Add(new QueryParameter("@Hematemesis", DbType.String, SymptomHematemesis));
                updateQuery.Parameters.Add(new QueryParameter("@BleedStool", DbType.String, SymptomBloodyStool));
                updateQuery.Parameters.Add(new QueryParameter("@BleedSkin", DbType.String, SymptomBleedSkin));
                updateQuery.Parameters.Add(new QueryParameter("@ChestPain", DbType.String, SymptomChestPain));
                updateQuery.Parameters.Add(new QueryParameter("@Confused", DbType.String, SymptomConfused));
                updateQuery.Parameters.Add(new QueryParameter("@Conjunctivitis", DbType.String, SymptomConjunctivitis));
                updateQuery.Parameters.Add(new QueryParameter("@Cough", DbType.String, SymptomCough));
                updateQuery.Parameters.Add(new QueryParameter("@BloodCough", DbType.String, SymptomCoughBlood));
                updateQuery.Parameters.Add(new QueryParameter("@Diarrhea", DbType.String, SymptomDiarrhea));
                updateQuery.Parameters.Add(new QueryParameter("@DiffBreathe", DbType.String, SymptomDiffBreathe));
                updateQuery.Parameters.Add(new QueryParameter("@DiffSwallow", DbType.String, SymptomDiffSwallow));
                updateQuery.Parameters.Add(new QueryParameter("@BloodVomit", DbType.String, SymptomBloodVomit));
                updateQuery.Parameters.Add(new QueryParameter("@Fatigue", DbType.String, SymptomFatigue));
                updateQuery.Parameters.Add(new QueryParameter("@Fever", DbType.String, SymptomFever));
                updateQuery.Parameters.Add(new QueryParameter("@Headache", DbType.String, SymptomHeadache));
                updateQuery.Parameters.Add(new QueryParameter("@Hiccups", DbType.String, SymptomHiccups));
                updateQuery.Parameters.Add(new QueryParameter("@Jaundice", DbType.String, SymptomJaundice));
                updateQuery.Parameters.Add(new QueryParameter("@JointPain", DbType.String, SymptomJointPain));
                updateQuery.Parameters.Add(new QueryParameter("@MusclePain", DbType.String, SymptomMusclePain));
                updateQuery.Parameters.Add(new QueryParameter("@BleedNose", DbType.String, SymptomNoseBleed));
                updateQuery.Parameters.Add(new QueryParameter("@BleedOther", DbType.String, SymptomOtherHemo));
                updateQuery.Parameters.Add(new QueryParameter("@BleedOtherComment", DbType.String, SymptomOtherHemoSpecify));
                updateQuery.Parameters.Add(new QueryParameter("@SymptOther", DbType.String, SymptomOtherNonHemorrhagic));
                updateQuery.Parameters.Add(new QueryParameter("@SymptOtherComment", DbType.String, SymptomOtherNonHemorrhagicSpecify));
                updateQuery.Parameters.Add(new QueryParameter("@PainEyes", DbType.String, SymptomPainEyes));
                updateQuery.Parameters.Add(new QueryParameter("@Rash", DbType.String, SymptomRash));
                updateQuery.Parameters.Add(new QueryParameter("@SoreThroat", DbType.String, SymptomSoreThroat));
                updateQuery.Parameters.Add(new QueryParameter("@Unconscious", DbType.String, SymptomUnconscious));
                updateQuery.Parameters.Add(new QueryParameter("@Vomiting", DbType.String, SymptomVomiting));

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
        /// Updates this case record's third page in the database with the in-memory representation
        /// </summary>
        private void UpdateInDatabasePage3()
        {
            RenderableField field = CaseForm.Fields["HospitalizedCurrent"] as RenderableField;
            IDbDriver db = CaseForm.Project.CollectedData.GetDatabase();

            if (field != null && field.Page != null)
            {
                Query updateQuery = db.CreateQuery("UPDATE [" + field.Page.TableName + "] SET " +

                    "[HospitalizedCurrent] = @HospitalizedCurrent, " +

                    "[DateHospitalCurrentAdmit] = @DateHospitalCurrentAdmit, " +
                    "[HospitalCurrent] = @HospitalCurrent, " +
                    "[DistrictHospitalCurrent] = @DistrictHospitalCurrent, " +
                    "[SCHospitalCurrent] = @SCHospitalCurrent, " +
                    "[IsolationCurrent] = @IsolationCurrent, " +
                    "[DateIsolationCurrent] = @DateIsolationCurrent, " +

                    // hospitalized in the past

                    "[HospitalizedPast] = @HospitalizedPast, " +
                    "[DateHospitalPastStart1] = @DateHospitalPastStart1, " +
                    "[DateHospitalPastEnd1] = @DateHospitalPastEnd1, " +
                    "[DateHospitalPastStart2] = @DateHospitalPastStart2, " +
                    "[DateHospitalPastEnd2] = @DateHospitalPastEnd2, " +
                    "[HospitalPast1] = @HospitalPast1, " +
                    "[HospitalPast2] = @HospitalPast2, " +
                    "[VillageHospitalPast1] = @VillageHospitalPast1, " +
                    "[VillageHospitalPast2] = @VillageHospitalPast2, " +
                    "[DistrictHospitalPast1] = @DistrictHospitalPast1, " +
                    "[DistrictHospitalPast2] = @DistrictHospitalPast2, " +
                    "[IsolationPast1] = @IsolationPast1, " +
                    "[IsolationPast2] = @IsolationPast2 " +

                        "WHERE [GlobalRecordId] = @GlobalRecordId");

                #region Current Hospitalization
                updateQuery.Parameters.Add(new QueryParameter("@HospitalizedCurrent", DbType.String, HospitalizedCurrent));

                if (DateHospitalCurrentAdmit.HasValue)
                {
                    updateQuery.Parameters.Add(new QueryParameter("@DateHospitalCurrentAdmit", DbType.DateTime, DateHospitalCurrentAdmit.Value));
                }
                else
                {
                    updateQuery.Parameters.Add(new QueryParameter("@DateHospitalCurrentAdmit", DbType.DateTime, DBNull.Value));
                }

                updateQuery.Parameters.Add(new QueryParameter("@HospitalCurrent", DbType.String, CurrentHospital));

                updateQuery.Parameters.Add(new QueryParameter("@DistrictHospitalCurrent", DbType.String, DistrictHosp));
                updateQuery.Parameters.Add(new QueryParameter("@SCHospitalCurrent", DbType.String, SubCountyHosp));
                updateQuery.Parameters.Add(new QueryParameter("@IsolationCurrent", DbType.String, IsolationCurrent));

                if (DateIsolationCurrent.HasValue)
                {
                    updateQuery.Parameters.Add(new QueryParameter("@DateIsolationCurrent", DbType.DateTime, DateIsolationCurrent.Value));
                }
                else
                {
                    updateQuery.Parameters.Add(new QueryParameter("@DateIsolationCurrent", DbType.DateTime, DBNull.Value));
                }
                #endregion // Current Hospitalization

                // hospitalized in the past

                updateQuery.Parameters.Add(new QueryParameter("@HospitalizedPast", DbType.String, HospitalizedPast));

                if (DateHospitalPastStart1.HasValue)
                {
                    updateQuery.Parameters.Add(new QueryParameter("@DateHospitalPastStart1", DbType.DateTime, DateHospitalPastStart1.Value));
                }
                else
                {
                    updateQuery.Parameters.Add(new QueryParameter("@DateHospitalPastStart1", DbType.DateTime, DBNull.Value));
                }

                if (DateHospitalPastEnd1.HasValue)
                {
                    updateQuery.Parameters.Add(new QueryParameter("@DateHospitalPastEnd1", DbType.DateTime, DateHospitalPastEnd1.Value));
                }
                else
                {
                    updateQuery.Parameters.Add(new QueryParameter("@DateHospitalPastEnd1", DbType.DateTime, DBNull.Value));
                }

                if (DateHospitalPastStart2.HasValue)
                {
                    updateQuery.Parameters.Add(new QueryParameter("@DateHospitalPastStart2", DbType.DateTime, DateHospitalPastStart2.Value));
                }
                else
                {
                    updateQuery.Parameters.Add(new QueryParameter("@DateHospitalPastStart2", DbType.DateTime, DBNull.Value));
                }

                if (DateHospitalPastEnd2.HasValue)
                {
                    updateQuery.Parameters.Add(new QueryParameter("@DateHospitalPastEnd2", DbType.DateTime, DateHospitalPastEnd2.Value));
                }
                else
                {
                    updateQuery.Parameters.Add(new QueryParameter("@DateHospitalPastEnd2", DbType.DateTime, DBNull.Value));
                }

                updateQuery.Parameters.Add(new QueryParameter("@HospitalPast1", DbType.String, HospitalPast1));
                updateQuery.Parameters.Add(new QueryParameter("@HospitalPast2", DbType.String, HospitalPast2));

                updateQuery.Parameters.Add(new QueryParameter("@VillageHospitalPast1", DbType.String, HospitalVillage1));
                updateQuery.Parameters.Add(new QueryParameter("@VillageHospitalPast2", DbType.String, HospitalVillage2));

                updateQuery.Parameters.Add(new QueryParameter("@DistrictHospitalPast1", DbType.String, HospitalDistrict1));
                updateQuery.Parameters.Add(new QueryParameter("@DistrictHospitalPast2", DbType.String, HospitalDistrict2));

                updateQuery.Parameters.Add(new QueryParameter("@IsolationPast1", DbType.String, IsolationPast1));
                updateQuery.Parameters.Add(new QueryParameter("@IsolationPast2", DbType.String, IsolationPast2));

                updateQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, RecordId));

                int rows = db.ExecuteNonQuery(updateQuery);

                if (rows != 1)
                {
                    throw new InvalidOperationException(String.Format("Failed to update this record in page with table name \"{0}\"; no row exists in page table for {1}.", field.Page.TableName, RecordId));
                }
            }
        }

        /// <summary>
        /// Updates this case record's fourth page in the database with the in-memory representation
        /// </summary>
        private void UpdateInDatabasePage4()
        {
            RenderableField field = CaseForm.Fields["Contact"] as RenderableField;
            IDbDriver db = CaseForm.Project.CollectedData.GetDatabase();

            if (field != null && field.Page != null)
            {
                Query updateQuery = db.CreateQuery("UPDATE [" + field.Page.TableName + "] SET " +

                    "[Contact] = @Contact, " +
                    "[ContactName1] = @ContactName1, " +
                    "[ContactName2] = @ContactName2, " +
                    "[ContactName3] = @ContactName3, " +
                    "[ContactRelation1] = @ContactRelation1, " +
                    "[ContactRelation2] = @ContactRelation2, " +
                    "[ContactRelation3] = @ContactRelation3, " +
                    "[ContactDateStart1] = @ContactDateStart1, " +
                    "[ContactDateStart2] = @ContactDateStart2, " +
                    "[ContactDateStart3] = @ContactDateStart3, " +
                    "[ContactDateEnd1] = @ContactDateEnd1, " +
                    "[ContactDateEnd2] = @ContactDateEnd2, " +
                    "[ContactDateEnd3] = @ContactDateEnd3, " +
                    "[ContactVillage1] = @ContactVillage1, " +
                    "[ContactVillage2] = @ContactVillage2, " +
                    "[ContactVillage3] = @ContactVillage3, " +
                    "[ContactDistrict1] = @ContactDistrict1, " +
                    "[ContactDistrict2] = @ContactDistrict2, " +
                    "[ContactDistrict3] = @ContactDistrict3, " +
                    "[ContactStatus1] = @ContactStatus1, " +
                    "[ContactStatus2] = @ContactStatus2, " +
                    "[ContactStatus3] = @ContactStatus3, " +
                    "[ContactDateDeath1] = @ContactDateDeath1, " +
                    "[ContactDateDeath2] = @ContactDateDeath2, " +
                    "[ContactDateDeath3] = @ContactDateDeath3, " +
                    "[Funeral] = @Funeral, " +
                    "[FuneralName1] = @FuneralName1, " +
                    "[FuneralName2] = @FuneralName2, " +
                    "[FuneralRelation1] = @FuneralRelation1, " +
                    "[FuneralRelation2] = @FuneralRelation2, " +
                    "[FuneralVillage1] = @FuneralVillage1, " +
                    "[FuneralVillage2] = @FuneralVillage2, " +
                    "[FuneralDistrict1] = @FuneralDistrict1, " +
                    "[FuneralDistrict2] = @FuneralDistrict2, " +
                    "[FuneralTouchBody1] = @FuneralTouchBody1, " +
                    "[FuneralTouchBody2] = @FuneralTouchBody2, " +
                    "[FuneralDateStart1] = @FuneralDateStart1, " +
                    "[FuneralDateStart2] = @FuneralDateStart2, " +
                    "[FuneralDateEnd1] = @FuneralDateEnd1, " +
                    "[FuneralDateEnd2] = @FuneralDateEnd2, " +
                    "[Travel] = @Travel, " +
                    "[TravelVillage] = @TravelVillage, " +
                    "[TravelDistrict] = @TravelDistrict, " + //, " +
                    "[TravelDateStart] = @TravelDateStart, " +
                    "[TravelDateEnd] = @TravelDateEnd " +
                    //"[REPLACEME] = @REPLACEME, " +
                    //"[REPLACEME] = @REPLACEME, " +
                    //"[REPLACEME] = @REPLACEME, " +

                    //"[LAST] = @LAST " +

                    // TODO: Finish!

                        "WHERE [GlobalRecordId] = @GlobalRecordId");

                updateQuery.Parameters.Add(new QueryParameter("@Contact", DbType.String, HadContact));
                updateQuery.Parameters.Add(new QueryParameter("@ContactName1", DbType.String, ContactName1));
                updateQuery.Parameters.Add(new QueryParameter("@ContactName2", DbType.String, ContactName2));
                updateQuery.Parameters.Add(new QueryParameter("@ContactName3", DbType.String, ContactName3));
                updateQuery.Parameters.Add(new QueryParameter("@ContactRelation1", DbType.String, ContactRelation1));
                updateQuery.Parameters.Add(new QueryParameter("@ContactRelation2", DbType.String, ContactRelation2));
                updateQuery.Parameters.Add(new QueryParameter("@ContactRelation3", DbType.String, ContactRelation3));
                if (ContactStartDate1.HasValue) { updateQuery.Parameters.Add(new QueryParameter("@ContactDateStart1", DbType.DateTime, ContactStartDate1.Value)); } else { updateQuery.Parameters.Add(new QueryParameter("@ContactDateStart1", DbType.DateTime, DBNull.Value)); }
                if (ContactStartDate2.HasValue) { updateQuery.Parameters.Add(new QueryParameter("@ContactDateStart2", DbType.DateTime, ContactStartDate2.Value)); } else { updateQuery.Parameters.Add(new QueryParameter("@ContactDateStart2", DbType.DateTime, DBNull.Value)); }
                if (ContactStartDate3.HasValue) { updateQuery.Parameters.Add(new QueryParameter("@ContactDateStart3", DbType.DateTime, ContactStartDate3.Value)); } else { updateQuery.Parameters.Add(new QueryParameter("@ContactDateStart3", DbType.DateTime, DBNull.Value)); }
                if (ContactEndDate1.HasValue) { updateQuery.Parameters.Add(new QueryParameter("@ContactDateEnd1", DbType.DateTime, ContactEndDate1.Value)); } else { updateQuery.Parameters.Add(new QueryParameter("@ContactDateEnd1", DbType.DateTime, DBNull.Value)); }
                if (ContactEndDate2.HasValue) { updateQuery.Parameters.Add(new QueryParameter("@ContactDateEnd2", DbType.DateTime, ContactEndDate2.Value)); } else { updateQuery.Parameters.Add(new QueryParameter("@ContactDateEnd2", DbType.DateTime, DBNull.Value)); }
                if (ContactEndDate3.HasValue) { updateQuery.Parameters.Add(new QueryParameter("@ContactDateEnd3", DbType.DateTime, ContactEndDate3.Value)); } else { updateQuery.Parameters.Add(new QueryParameter("@ContactDateEnd3", DbType.DateTime, DBNull.Value)); }

                updateQuery.Parameters.Add(new QueryParameter("@ContactVillage1", DbType.String, ContactVillage1));
                updateQuery.Parameters.Add(new QueryParameter("@ContactVillage2", DbType.String, ContactVillage2));
                updateQuery.Parameters.Add(new QueryParameter("@ContactVillage3", DbType.String, ContactVillage3));

                updateQuery.Parameters.Add(new QueryParameter("@ContactDistrict1", DbType.String, ContactDistrict1));
                updateQuery.Parameters.Add(new QueryParameter("@ContactDistrict2", DbType.String, ContactDistrict2));
                updateQuery.Parameters.Add(new QueryParameter("@ContactDistrict3", DbType.String, ContactDistrict3));

                updateQuery.Parameters.Add(new QueryParameter("@ContactStatus1", DbType.String, ContactStatus1));
                updateQuery.Parameters.Add(new QueryParameter("@ContactStatus2", DbType.String, ContactStatus2));
                updateQuery.Parameters.Add(new QueryParameter("@ContactStatus3", DbType.String, ContactStatus3));

                if (ContactDeathDate1.HasValue) { updateQuery.Parameters.Add(new QueryParameter("@ContactDateDeath1", DbType.DateTime, ContactDeathDate1.Value)); } else { updateQuery.Parameters.Add(new QueryParameter("@ContactDateDeath1", DbType.DateTime, DBNull.Value)); }
                if (ContactDeathDate2.HasValue) { updateQuery.Parameters.Add(new QueryParameter("@ContactDateDeath2", DbType.DateTime, ContactDeathDate2.Value)); } else { updateQuery.Parameters.Add(new QueryParameter("@ContactDateDeath2", DbType.DateTime, DBNull.Value)); }
                if (ContactDeathDate3.HasValue) { updateQuery.Parameters.Add(new QueryParameter("@ContactDateDeath3", DbType.DateTime, ContactDeathDate3.Value)); } else { updateQuery.Parameters.Add(new QueryParameter("@ContactDateDeath3", DbType.DateTime, DBNull.Value)); }

                updateQuery.Parameters.Add(new QueryParameter("@Funeral", DbType.String, AttendFuneral));

                updateQuery.Parameters.Add(new QueryParameter("@FuneralName1", DbType.String, FuneralNameDeceased1));
                updateQuery.Parameters.Add(new QueryParameter("@FuneralName2", DbType.String, FuneralNameDeceased2));

                updateQuery.Parameters.Add(new QueryParameter("@FuneralRelation1", DbType.String, FuneralRelationDeceased1));
                updateQuery.Parameters.Add(new QueryParameter("@FuneralRelation2", DbType.String, FuneralRelationDeceased2));

                updateQuery.Parameters.Add(new QueryParameter("@FuneralVillage1", DbType.String, FuneralVillage1));
                updateQuery.Parameters.Add(new QueryParameter("@FuneralVillage2", DbType.String, FuneralVillage2));

                updateQuery.Parameters.Add(new QueryParameter("@FuneralDistrict1", DbType.String, FuneralDistrict1));
                updateQuery.Parameters.Add(new QueryParameter("@FuneralDistrict2", DbType.String, FuneralDistrict2));

                updateQuery.Parameters.Add(new QueryParameter("@FuneralTouchBody1", DbType.String, FuneralTouchBody1));
                updateQuery.Parameters.Add(new QueryParameter("@FuneralTouchBody2", DbType.String, FuneralTouchBody2));

                if (FuneralStartDate1.HasValue) { updateQuery.Parameters.Add(new QueryParameter("@FuneralDateStart1", DbType.DateTime, FuneralStartDate1.Value)); } else { updateQuery.Parameters.Add(new QueryParameter("@FuneralDateStart1", DbType.DateTime, DBNull.Value)); }
                if (FuneralStartDate2.HasValue) { updateQuery.Parameters.Add(new QueryParameter("@FuneralDateStart2", DbType.DateTime, FuneralStartDate2.Value)); } else { updateQuery.Parameters.Add(new QueryParameter("@FuneralDateStart2", DbType.DateTime, DBNull.Value)); }

                if (FuneralEndDate1.HasValue) { updateQuery.Parameters.Add(new QueryParameter("@FuneralDateEnd1", DbType.DateTime, FuneralEndDate1.Value)); } else { updateQuery.Parameters.Add(new QueryParameter("@FuneralDateEnd1", DbType.DateTime, DBNull.Value)); }
                if (FuneralEndDate2.HasValue) { updateQuery.Parameters.Add(new QueryParameter("@FuneralDateEnd2", DbType.DateTime, FuneralEndDate2.Value)); } else { updateQuery.Parameters.Add(new QueryParameter("@FuneralDateEnd2", DbType.DateTime, DBNull.Value)); }

                updateQuery.Parameters.Add(new QueryParameter("@Travel", DbType.String, Travel));

                updateQuery.Parameters.Add(new QueryParameter("@TravelVillage", DbType.String, TravelVillage));
                updateQuery.Parameters.Add(new QueryParameter("@TravelDistrict", DbType.String, TravelDistrict));

                if (TravelStartDate.HasValue) { updateQuery.Parameters.Add(new QueryParameter("@TravelDateStart", DbType.DateTime, TravelStartDate.Value)); } else { updateQuery.Parameters.Add(new QueryParameter("@TravelDateStart", DbType.DateTime, DBNull.Value)); }
                if (TravelEndDate.HasValue) { updateQuery.Parameters.Add(new QueryParameter("@TravelDateEnd", DbType.DateTime, TravelEndDate.Value)); } else { updateQuery.Parameters.Add(new QueryParameter("@TravelDateEnd", DbType.DateTime, DBNull.Value)); }

                // TODO: Add rest of this page's fields

                updateQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, RecordId));

                int rows = db.ExecuteNonQuery(updateQuery);

                if (rows != 1)
                {
                    throw new InvalidOperationException(String.Format("Failed to update this record in page with table name \"{0}\"; no row exists in page table.", field.Page.TableName));
                }
            }
        }

        /// <summary>
        /// Updates this case record's fifth page in the database with the in-memory representation
        /// </summary>
        private void UpdateInDatabasePage5()
        {
            RenderableField field = CaseForm.Fields["Animals"] as RenderableField;
            IDbDriver db = CaseForm.Project.CollectedData.GetDatabase();

            if (field != null && field.Page != null)
            {
                Query updateQuery = db.CreateQuery("UPDATE [" + field.Page.TableName + "] SET " +
                    "[Animals] = @Animals, " +
                    "[AnimalBats] = @AnimalBats, " +
                    "[AnimalPrimates] = @AnimalPrimates, " +
                    "[AnimalRodents] = @AnimalRodents, " +
                    "[AnimalPigs] = @AnimalPigs, " +
                    "[AnimalBirds] = @AnimalBirds, " +
                    "[AnimalCows] = @AnimalCows, " +
                    "[AnimalOther] = @AnimalOther, " +
                    "[AnimalOtherComment] = @AnimalOtherComment, " +
                    "[AnimalBatsStatus] = @AnimalBatsStatus, " +
                    "[AnimalPrimatesStatus] = @AnimalPrimatesStatus, " +
                    "[AnimalRodentStatus] = @AnimalRodentStatus, " +
                    "[AnimalPigsStatus] = @AnimalPigsStatus, " +
                    "[AnimalBirdsStatus] = @AnimalBirdsStatus, " +
                    "[AnimalCowStatus] = @AnimalCowStatus, " +
                    "[AnimalOtherStatus] = @AnimalOtherStatus, " +
                    "[BittenTick] = @BittenTick, " +
                    "[FinalLabClass] = @FinalLabClass, " +
                    "[InterviewerName] = @InterviewerName, " +
                    "[InterviewerPhone] = @InterviewerPhone, " +
                    "[InterviwerEmail] = @InterviwerEmail, " +
                    "[InterviewerPosition] = @InterviewerPosition, " +
                    "[InterviewerDistrict] = @InterviewerDistrict, " +
                    "[InterviewerHealthFacility] = @InterviewerHealthFacility, " +
                    "[InfoProvidedBy] = @InfoProvidedBy, " +
                    "[ProxyName] = @ProxyName, " +
                    "[ProxyRelation] = @ProxyRelation " +

                    "WHERE [GlobalRecordId] = @GlobalRecordId");

                updateQuery.Parameters.Add(new QueryParameter("@Animals", DbType.String, Animals));
                updateQuery.Parameters.Add(new QueryParameter("@AnimalBats", DbType.Boolean, AnimalBats));
                updateQuery.Parameters.Add(new QueryParameter("@AnimalPrimates", DbType.Boolean, AnimalPrimates));
                updateQuery.Parameters.Add(new QueryParameter("@AnimalRodents", DbType.Boolean, AnimalRodents));
                updateQuery.Parameters.Add(new QueryParameter("@AnimalPigs", DbType.Boolean, AnimalPigs));
                updateQuery.Parameters.Add(new QueryParameter("@AnimalBirds", DbType.Boolean, AnimalBirds));
                updateQuery.Parameters.Add(new QueryParameter("@AnimalCows", DbType.Boolean, AnimalCows));
                updateQuery.Parameters.Add(new QueryParameter("@AnimalOther", DbType.Boolean, AnimalOther));
                updateQuery.Parameters.Add(new QueryParameter("@AnimalOtherComment", DbType.String, AnimalOtherComment));
                updateQuery.Parameters.Add(new QueryParameter("@AnimalBatsStatus", DbType.String, AnimalBatsStatus));
                updateQuery.Parameters.Add(new QueryParameter("@AnimalPrimatesStatus", DbType.String, AnimalPrimatesStatus));
                updateQuery.Parameters.Add(new QueryParameter("@AnimalRodentStatus", DbType.String, AnimalRodentsStatus));
                updateQuery.Parameters.Add(new QueryParameter("@AnimalPigsStatus", DbType.String, AnimalPigsStatus));
                updateQuery.Parameters.Add(new QueryParameter("@AnimalBirdsStatus", DbType.String, AnimalBirdsStatus));
                updateQuery.Parameters.Add(new QueryParameter("@AnimalCowStatus", DbType.String, AnimalCowsStatus));
                updateQuery.Parameters.Add(new QueryParameter("@AnimalOtherStatus", DbType.String, AnimalOtherStatus));
                updateQuery.Parameters.Add(new QueryParameter("@BittenTick", DbType.String, BittenTick));
                updateQuery.Parameters.Add(new QueryParameter("@FinalLabClass", DbType.String, FinalLabClassification));
                updateQuery.Parameters.Add(new QueryParameter("@InterviewerName", DbType.String, InterviewerName));
                updateQuery.Parameters.Add(new QueryParameter("@InterviewerPhone", DbType.String, InterviewerPhone));
                updateQuery.Parameters.Add(new QueryParameter("@InterviwerEmail", DbType.String, InterviewerEmail));
                updateQuery.Parameters.Add(new QueryParameter("@InterviewerPosition", DbType.String, InterviewerPosition));
                updateQuery.Parameters.Add(new QueryParameter("@InterviewerDistrict", DbType.String, InterviewerDistrict));
                updateQuery.Parameters.Add(new QueryParameter("@InterviewerHealthFacility", DbType.String, InterviewerHealthFacility));
                updateQuery.Parameters.Add(new QueryParameter("@InfoProvidedBy", DbType.String, InterviewerInfoProvidedBy));
                updateQuery.Parameters.Add(new QueryParameter("@ProxyName", DbType.String, ProxyName));
                updateQuery.Parameters.Add(new QueryParameter("@ProxyRelation", DbType.String, ProxyRelation));

                updateQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, RecordId));

                int rows = db.ExecuteNonQuery(updateQuery);

                if (rows != 1)
                {
                    throw new InvalidOperationException(String.Format("Failed to update this record in page wit table name \"{0}\"; no row exists in page table.", field.Page.TableName));
                }
            }
        }

        /// <summary>
        /// Updates this case record's sixth page in the database with the in-memory representation
        /// </summary>
        private void UpdateInDatabasePage6()
        {
            RenderableField field = CaseForm.Fields["DateOutcomeComp"] as RenderableField;
            IDbDriver db = CaseForm.Project.CollectedData.GetDatabase();

            if (field != null && field.Page != null)
            {
                Query updateQuery = db.CreateQuery("UPDATE [" + field.Page.TableName + "] SET " +
                    "[DateOutcomeComp] = @DateOutcomeComp, " +
                    "[FinalStatus] = @FinalStatus, " +
                    "[BleedUnexplainedEver] = @BleedUnexplainedEver, " +
                    "[SpecifyBleeding] = @SpecifyBleeding, " +
                    "[HospitalDischarge] = @HospitalDischarge, " +
                    "[HospitalDischargeDistrict] = @HospitalDischargeDistrict, " +
                    "[DateDischargeIso] = @DateDischargeIso, " +
                    "[DateDischargeIsoEst] = @DateDischargeIsoEst, " +
                    "[DateDischargeHosp] = @DateDischargeHosp, " +
                    "[DateDischargeHospEst] = @DateDischargeHospEst, " +
                    "[DateDeath2] = @DateDeath2, " +
                    "[DateDeath2Estimated] = @DateDeath2Estimated, " +
                    "[DateDeath2EstSpecify] = @DateDeath2EstSpecify, " +
                    "[PlaceDeath] = @PlaceDeath, " +
                    "[HospitalDeath] = @HospitalDeath, " +
                    "[PlaceDeathOther] = @PlaceDeathOther, " +
                    "[VillageDeath] = @VillageDeath, " +
                    "[DistrictDeath] = @DistrictDeath, " +
                    "[SCDeath] = @SCDeath, " +
                    "[DateFuneral] = @DateFuneral, " +
                    "[FuneralConductedFam] = @FuneralConductedFam, " +
                    "[FuneralConducteOutTeam] = @FuneralConducteOutTeam, " +
                    "[VillageFuneral] = @VillageFuneral, " +
                    "[DistrictFuneral] = @DistrictFuneral, " +
                    "[SCFuneral] = @SCFuneral, " +

                    ///////////////////////////
                    "[AbdPainFinal] = @AbdPainFinal, " +
                    "[AnorexiaFinal] = @AnorexiaFinal, " +
                    "[ChestPainFinal] = @ChestPainFinal, " +
                    "[ConfusedFinal] = @ConfusedFinal, " +
                    "[ConjunctivitisFinal] = @ConjunctivitisFinal, " +
                    "[CoughFinal] = @CoughFinal, " +
                    "[DiarrheaFinal] = @DiarrheaFinal, " +
                    "[DiffBreatheFinal] = @DiffBreatheFinal, " +
                    "[DiffSwallowFinal] = @DiffSwallowFinal, " +
                    "[FatigueFinal] = @FatigueFinal, " +
                    "[FeverFinal] = @FeverFinal, " +
                    "[HeadacheFinal] = @HeadacheFinal, " +
                    "[HiccupsFinal] = @HiccupsFinal, " +
                    "[JaundiceFinal] = @JaundiceFinal, " +
                    "[JointPainFinal] = @JointPainFinal, " +
                    "[MusclePainFinal] = @MusclePainFinal, " +
                    "[OtherHemoFinal] = @OtherHemoFinal, " +
                    "[OtherHemoFinalSpecify] = @OtherHemoFinalSpecify, " +
                    "[PainEyesFinal] = @PainEyesFinal, " +
                    "[RashFinal] = @RashFinal, " +
                    "[SoreThroatFinal] = @SoreThroatFinal, " +
                    "[UnconsciousFinal] = @UnconsciousFinal, " +
                    "[VomitingFinal] = @VomitingFinal, " +
                    ///////////////////////////
                    "[CommentsonthisPatient] = @CommentsonthisPatient " +

                    // TODO: Add final symptoms

                    "WHERE [GlobalRecordId] = @GlobalRecordId");

                if (DateOutcomeInfoCompleted.HasValue) { updateQuery.Parameters.Add(new QueryParameter("@DateOutcomeComp", DbType.DateTime, DateOutcomeInfoCompleted.Value)); } else { updateQuery.Parameters.Add(new QueryParameter("@DateOutcomeComp", DbType.DateTime, DBNull.Value)); }
                updateQuery.Parameters.Add(new QueryParameter("@FinalStatus", DbType.String, FinalCaseStatus));
                updateQuery.Parameters.Add(new QueryParameter("@BleedUnexplainedEver", DbType.String, BleedUnexplainedEver));
                updateQuery.Parameters.Add(new QueryParameter("@SpecifyBleeding", DbType.String, SpecifyBleeding));
                updateQuery.Parameters.Add(new QueryParameter("@HospitalDischarge", DbType.String, HospitalDischarge));
                updateQuery.Parameters.Add(new QueryParameter("@HospitalDischargeDistrict", DbType.String, HospitalDischargeDistrict));
                if (DateDischargeIso.HasValue) { updateQuery.Parameters.Add(new QueryParameter("@DateDischargeIso", DbType.DateTime, DateDischargeIso.Value)); } else { updateQuery.Parameters.Add(new QueryParameter("@DateDischargeIso", DbType.DateTime, DBNull.Value)); }
                updateQuery.Parameters.Add(new QueryParameter("@DateDischargeIsoEst", DbType.Boolean, DateDischargeIsoEst));
                if (DateDischargeHospital.HasValue) { updateQuery.Parameters.Add(new QueryParameter("@DateDischargeHosp", DbType.DateTime, DateDischargeHospital.Value)); } else { updateQuery.Parameters.Add(new QueryParameter("@DateDischargeHosp", DbType.DateTime, DBNull.Value)); }
                updateQuery.Parameters.Add(new QueryParameter("@DateDischargeHospEst", DbType.Boolean, DateDischargeHospitalEst));
                if (DateDeath2.HasValue) { updateQuery.Parameters.Add(new QueryParameter("@DateDeath2", DbType.DateTime, DateDeath2.Value)); } else { updateQuery.Parameters.Add(new QueryParameter("@DateDeath2", DbType.DateTime, DBNull.Value)); }
                updateQuery.Parameters.Add(new QueryParameter("@DateDeath2Estimated", DbType.Boolean, DateDeath2Est));
                updateQuery.Parameters.Add(new QueryParameter("@DateDeath2EstSpecify", DbType.String, DateDeath2EstSpecify));
                updateQuery.Parameters.Add(new QueryParameter("@PlaceDeath", DbType.String, PlaceDeath));
                updateQuery.Parameters.Add(new QueryParameter("@HospitalDeath", DbType.String, HospitalDeath));
                updateQuery.Parameters.Add(new QueryParameter("@PlaceDeathOther", DbType.String, PlaceDeathOther));
                updateQuery.Parameters.Add(new QueryParameter("@VillageDeath", DbType.String, VillageDeath));
                updateQuery.Parameters.Add(new QueryParameter("@DistrictDeath", DbType.String, DistrictDeath));
                updateQuery.Parameters.Add(new QueryParameter("@SCDeath", DbType.String, SubCountyDeath));
                if (DateFuneral.HasValue) { updateQuery.Parameters.Add(new QueryParameter("@DateFuneral", DbType.DateTime, DateFuneral.Value)); } else { updateQuery.Parameters.Add(new QueryParameter("@DateFuneral", DbType.DateTime, DBNull.Value)); }
                updateQuery.Parameters.Add(new QueryParameter("@FuneralConductedFam", DbType.Boolean, FuneralConductedFam));
                updateQuery.Parameters.Add(new QueryParameter("@FuneralConducteOutTeam", DbType.Boolean, FuneralConductedOutbreakTeam));
                updateQuery.Parameters.Add(new QueryParameter("@VillageFuneral", DbType.String, VillageFuneral));
                updateQuery.Parameters.Add(new QueryParameter("@DistrictFuneral", DbType.String, DistrictFuneral));
                updateQuery.Parameters.Add(new QueryParameter("@SCFuneral", DbType.String, SubCountyFuneral));

                ///////////// symptoms
                updateQuery.Parameters.Add(new QueryParameter("@AbdPainFinal", DbType.String, SymptomAbdPainFinal));
                updateQuery.Parameters.Add(new QueryParameter("@AnorexiaFinal", DbType.String, SymptomAnorexiaFinal));
                updateQuery.Parameters.Add(new QueryParameter("@ChestPainFinal", DbType.String, SymptomChestPainFinal));
                updateQuery.Parameters.Add(new QueryParameter("@ConfusedFinal", DbType.String, SymptomConfusedFinal));
                updateQuery.Parameters.Add(new QueryParameter("@ConjunctivitisFinal", DbType.String, SymptomConjunctivitisFinal));
                updateQuery.Parameters.Add(new QueryParameter("@CoughFinal", DbType.String, SymptomCoughFinal));
                updateQuery.Parameters.Add(new QueryParameter("@DiarrheaFinal", DbType.String, SymptomDiarrheaFinal));
                updateQuery.Parameters.Add(new QueryParameter("@DiffBreatheFinal", DbType.String, SymptomDiffBreatheFinal));
                updateQuery.Parameters.Add(new QueryParameter("@DiffSwallowFinal", DbType.String, SymptomDiffSwallowFinal));
                updateQuery.Parameters.Add(new QueryParameter("@FatigueFinal", DbType.String, SymptomFatigueFinal));
                updateQuery.Parameters.Add(new QueryParameter("@FeverFinal", DbType.String, SymptomFeverFinal));
                updateQuery.Parameters.Add(new QueryParameter("@HeadacheFinal", DbType.String, SymptomHeadacheFinal));
                updateQuery.Parameters.Add(new QueryParameter("@HiccupsFinal", DbType.String, SymptomHiccupsFinal));
                updateQuery.Parameters.Add(new QueryParameter("@JaundiceFinal", DbType.String, SymptomJaundiceFinal));
                updateQuery.Parameters.Add(new QueryParameter("@JointPainFinal", DbType.String, SymptomJointPainFinal));
                updateQuery.Parameters.Add(new QueryParameter("@MusclePainFinal", DbType.String, SymptomMusclePainFinal));
                updateQuery.Parameters.Add(new QueryParameter("@OtherHemoFinal", DbType.String, SymptomOtherHemoFinal));
                updateQuery.Parameters.Add(new QueryParameter("@OtherHemoFinalSpecify", DbType.String, SymptomOtherHemoFinalSpecify));
                updateQuery.Parameters.Add(new QueryParameter("@PainEyesFinal", DbType.String, SymptomPainEyesFinal));
                updateQuery.Parameters.Add(new QueryParameter("@RashFinal", DbType.String, SymptomRashFinal));
                updateQuery.Parameters.Add(new QueryParameter("@SoreThroatFinal", DbType.String, SymptomSoreThroatFinal));
                updateQuery.Parameters.Add(new QueryParameter("@UnconsciousFinal", DbType.String, SymptomUnconsciousFinal));
                updateQuery.Parameters.Add(new QueryParameter("@VomitingFinal", DbType.String, SymptomVomitingFinal));
                ////////////////////////////////



                updateQuery.Parameters.Add(new QueryParameter("@CommentsonthisPatient", DbType.String, CommentsOnThisPatient));

                updateQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, RecordId));

                db.ExecuteNonQuery(updateQuery);
            }
        }

        /// <summary>
        /// Updates this case record in the database with the in-memory representation
        /// </summary>
        private void UpdateInDatabase()
        {
            // TODO: Move this logic to some lower-level data model class

            string user = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString();
            DateTime now = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, 0);
            IDbDriver db = CaseForm.Project.CollectedData.GetDatabase();

            // update base table
            Query baseTableQuery = db.CreateQuery("UPDATE " + CaseForm.TableName + " SET [LastSaveLogonName] = @LastSaveLogonName, [LastSaveTime] = @LastSaveTime WHERE [GlobalRecordId] = @GlobalRecordId");
            baseTableQuery.Parameters.Add(new QueryParameter("@LastSaveLogonName", DbType.String, user));
            baseTableQuery.Parameters.Add(new QueryParameter("@LastSaveTime", DbType.DateTime, now));
            baseTableQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, RecordId));
            db.ExecuteNonQuery(baseTableQuery);

            RenderableField field1 = CaseForm.Fields["ID"] as RenderableField;
            RenderableField field2 = CaseForm.Fields["DateOnset"] as RenderableField;
            RenderableField field3 = CaseForm.Fields["HospitalizedCurrent"] as RenderableField;
            RenderableField field4 = CaseForm.Fields["Contact"] as RenderableField;
            RenderableField field5 = CaseForm.Fields["Animals"] as RenderableField;
            RenderableField field6 = CaseForm.Fields["DateOutcomeComp"] as RenderableField;

            if (field1 != null && field2 != null && field3 != null && field4 != null && field5 != null && field6 != null)
            {
                UpdateInDatabasePage1();
                UpdateInDatabasePage2();
                UpdateInDatabasePage3();
                UpdateInDatabasePage4();
                UpdateInDatabasePage5();
                UpdateInDatabasePage6();
            }

            // update all lab records with the new ID values and whatever other values; the IF/THEN below is to use diff queries for either MS Access or SQL server
            if (db.ToString().ToLower().Contains("sql"))
            {
                // Cascade update all lab records with new ID
                RenderableField idField = LabForm.Fields["ID"] as RenderableField;
                if (idField != null)
                {
                    Query updateQuery = db.CreateQuery("UPDATE L " +
                        "SET L.[ID] = @ID " +
                        "FROM " + idField.Page.TableName + " AS L " +
                        "INNER JOIN " + LabForm.TableName + " AS LR " +
                        "ON L.GlobalRecordId = LR.GlobalRecordId " +
                        "WHERE LR.FKEY = @FKEY");
                    updateQuery.Parameters.Add(new QueryParameter("@ID", DbType.String, ID));
                    updateQuery.Parameters.Add(new QueryParameter("@FKEY", DbType.String, RecordId));
                    int rowsUpdated = db.ExecuteNonQuery(updateQuery);
                }
            }
            else
            {
                // Cascade update all lab records with new ID
                RenderableField idField = LabForm.Fields["ID"] as RenderableField;
                if (idField != null)
                {
                    Query updateQuery = db.CreateQuery("UPDATE " + LabForm.TableName +
                        " lf INNER JOIN " + idField.Page.TableName + " lf1 ON lf.GlobalRecordId = lf1.GlobalRecordId " +
                        " SET [ID] = @ID " +
                        " WHERE lf.FKEY = @FKEY");
                    updateQuery.Parameters.Add(new QueryParameter("@ID", DbType.String, ID));
                    updateQuery.Parameters.Add(new QueryParameter("@FKEY", DbType.String, RecordId));
                    int rowsUpdated = db.ExecuteNonQuery(updateQuery);
                }
            }

        }

        /// <summary>
        /// Inserts this case record into the database
        /// </summary>
        private void InsertIntoDatabase()
        {
            // TODO: Move this logic to some lower-level data model class

            string user = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString();
            DateTime now = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, 0);
            IDbDriver db = CaseForm.Project.CollectedData.GetDatabase();

            using (IDbTransaction transaction = db.OpenTransaction())
            {
                Query insertQuery = db.CreateQuery("INSERT INTO [" + CaseForm.TableName + "] (GlobalRecordId, RECSTATUS, FirstSaveLogonName, LastSaveLogonName, FirstSaveTime, LastSaveTime) VALUES (" +
                        "@GlobalRecordId, @RECSTATUS, @FirstSaveLogonName, @LastSaveLogonName, @FirstSaveTime, @LastSaveTime)");
                insertQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, RecordId));
                insertQuery.Parameters.Add(new QueryParameter("@RECSTATUS", DbType.Byte, 1));
                insertQuery.Parameters.Add(new QueryParameter("@FirstSaveLogonName", DbType.String, user));
                insertQuery.Parameters.Add(new QueryParameter("@LastSaveLogonName", DbType.String, user));
                insertQuery.Parameters.Add(new QueryParameter("@FirstSaveTime", DbType.DateTime, now));
                insertQuery.Parameters.Add(new QueryParameter("@LastSaveTime", DbType.DateTime, now));
                db.ExecuteNonQuery(insertQuery, transaction);

                //if (rows != 1)
                //{
                //    throw new InvalidOperationException("Row insert failed");
                //}

                foreach (Epi.Page page in CaseForm.Pages)
                {
                    Query pageInsertQuery = db.CreateQuery("INSERT INTO [" + page.TableName + "] (GlobalRecordId) VALUES (@GlobalRecordId)");
                    pageInsertQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, RecordId));
                    db.ExecuteNonQuery(pageInsertQuery, transaction);

                    //if (rows != 1)
                    //{
                    //    throw new InvalidOperationException("Row insert failed");
                    //}
                }

                try
                {
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    Epi.Logger.Log(String.Format(DateTime.Now + ":  " + "Case INSERT Commit Exception Type: {0}", ex.GetType()));
                    Epi.Logger.Log(String.Format(DateTime.Now + ":  " + "Case INSERT Commit Exception Message: {0}", ex.Message));
                    Epi.Logger.Log(String.Format(DateTime.Now + ":  " + "Case INSERT Commit Rollback started..."));
                    transaction.Rollback();
                    Epi.Logger.Log(String.Format(DateTime.Now + ":  " + "Case INSERT Commit Rollback was successful."));
                }

                db.CloseTransaction(transaction);
            }

            // Now that empty rows exist for all page tables, do a normal update
            UpdateInDatabase();
        }

        /// <summary>
        /// Deletes this case's record from the database
        /// </summary>
        public void Delete()
        {
            IDbDriver db = CaseForm.Project.CollectedData.GetDatabase();

            int rows = 0;
            string querySyntax = "DELETE * FROM [" + CaseForm.TableName + "] WHERE [GlobalRecordId] = @GlobalRecordId";
            if (db.ToString().ToLower().Contains("sql"))
            {
                querySyntax = "DELETE FROM [" + CaseForm.TableName + "] WHERE [GlobalRecordId] = @GlobalRecordId";
            }

            Query deleteQuery = db.CreateQuery(querySyntax);
            deleteQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, RecordId));
            rows = db.ExecuteNonQuery(deleteQuery);

            if (rows != 1)
            {
                throw new ApplicationException("A deletion was requested for case with GUID " + RecordId + " but the case no longer exists.");
            }

            // MetaLinks removal for case-source case relationships
            querySyntax = "DELETE * FROM [metaLinks] WHERE [ToRecordGuid] = @ToRecordGuid AND [ToViewId] = @ToViewId AND [FromViewId] = @FromViewId";
            if (db.ToString().ToLower().Contains("sql"))
            {
                querySyntax = "DELETE FROM [metaLinks] WHERE [ToRecordGuid] = @ToRecordGuid AND [ToViewId] = @ToViewId AND [FromViewId] = @FromViewId";
            }

            deleteQuery = db.CreateQuery(querySyntax);
            deleteQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, RecordId));
            deleteQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, CaseForm.Id));
            deleteQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, CaseForm.Id));
            db.ExecuteNonQuery(deleteQuery);

            // Case form page table removal
            foreach (Epi.Page page in CaseForm.Pages)
            {
                querySyntax = "DELETE * FROM [" + page.TableName + "] WHERE [GlobalRecordId] = @GlobalRecordId";
                if (db.ToString().ToLower().Contains("sql"))
                {
                    querySyntax = "DELETE FROM [" + page.TableName + "] WHERE [GlobalRecordId] = @GlobalRecordId";
                }

                deleteQuery = db.CreateQuery(querySyntax);
                deleteQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, RecordId));
                db.ExecuteNonQuery(deleteQuery);
            }
        }

        public override string ToString()
        {
            return ID + " : " + Surname + ", " + OtherNames + " : " + GenderAbbreviation + " : " + EpiCaseDef;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                EpiCaseDefinitionChanging = null;
                CaseIDChanging = null;
            }
            // free native resources if there are any.
        }

        private void CreateCaseFromContact(ContactViewModel contactVM, int uniqueKey)
        {
            this.AgeUnit = contactVM.AgeUnit;
            this.Age = contactVM.Age;
            this.HeadOfHousehold = contactVM.HeadOfHousehold;
            this.District = contactVM.District;

            if (contactVM.Gender.Equals(Male))
            {
                this.Gender = Core.Enums.Gender.Male;
            }
            else if (contactVM.Gender.Equals(Female))
            {
                this.Gender = Core.Enums.Gender.Female;
            }
            else
            {
                this.Gender = Core.Enums.Gender.None;
            }
            //this.Gender = contactVM.Gender;
            this.IsContact = true;
            this.OtherNames = contactVM.OtherNames;
            this.RecordId = contactVM.RecordId;
            this.UniqueKey = uniqueKey;
            //Case.UniqueKey = uniqueKey;
            this.SubCounty = contactVM.SubCounty;
            this.Surname = contactVM.Surname;
            this.Village = contactVM.Village;
            //if (contactVM.HCW == "Yes")
            //{
            //    this.IsHCW = true;
            //}
            //else if (contactVM.HCW == "No")
            //{
            //    this.IsHCW = false;
            //}
            this.DOB = contactVM.ContactDOB;
            this.AddressRes = contactVM.ContactAddress;
            this.ZipRes = contactVM.ContactZip;
            this.PhoneNumber = contactVM.Phone;
            this.Email = contactVM.ContactEmail;

            this.OccupationHCWPosition = contactVM.ContactHCWPosition;
            this.OccupationHCWFacility = contactVM.HCWFacility;
            this.OccupationHCWDistrict = contactVM.ContactHCWDistrict;
            this.OccupationHCWSC = contactVM.ContactHCWSC;
            this.OccupationHCWVillage = contactVM.ContactHCWVillage;

        }

        /// <summary>
        /// Updates this case record with data from another case record
        /// </summary>
        /// <param name="updatedCase">The source data</param>
        public void CopyCase(CaseViewModel updatedCase)
        {
            this.CaseForm = updatedCase.CaseForm;
            this.LabForm = updatedCase.LabForm;

            this.AgeUnit = updatedCase.AgeUnit;
            this.AgeUnitString = updatedCase.AgeUnitString;
            this.Age = updatedCase.Age;
            this.HeadOfHousehold = updatedCase.HeadOfHousehold;
            this.DateDeath = updatedCase.DateDeath;
            this.DateDeathEst = updatedCase.DateDeathEst;
            this.DateOnsetEst = updatedCase.DateOnsetEst;
            this.DateDischargeIso = updatedCase.DateDischargeIso;
            this.DateIsolationCurrent = updatedCase.DateIsolationCurrent;
            this.DateOnset = updatedCase.DateOnset;
            this.DateReport = updatedCase.DateReport;
            this.District = updatedCase.District;
            this.DistrictOnset = updatedCase.DistrictOnset;
            this.DistrictHosp = updatedCase.DistrictHosp;
            this.Country = updatedCase.Country;
            this.CountryOnset = updatedCase.CountryOnset;
            this.Citizenship = updatedCase.Citizenship;//17178
            this.AddressRes = updatedCase.AddressRes;
            this.ZipRes = updatedCase.ZipRes;
            this.EpiCaseDef = updatedCase.EpiCaseDef;
            this.EpiCaseClassification = updatedCase.EpiCaseClassification;
            this.FinalLabClass = updatedCase.FinalLabClass;
            this.FinalLabClassification = updatedCase.FinalLabClassification;
            this.FinalStatus = updatedCase.FinalStatus;
            this.FinalCaseStatus = updatedCase.FinalCaseStatus;
            this.CurrentStatus = updatedCase.CurrentStatus;
            this.Gender = updatedCase.Gender;
            this.Sex = updatedCase.Sex;
            this.RecordStatusComplete = updatedCase.RecordStatusComplete;
            this.RecordStatusMissCRF = updatedCase.RecordStatusMissCRF;
            this.RecordStatusNoCRF = updatedCase.RecordStatusNoCRF;
            this.RecordStatusPenLab = updatedCase.RecordStatusPenLab;
            this.RecordStatusPenOut = updatedCase.RecordStatusPenOut;
            this.ID = updatedCase.ID;
            this.IsContact = updatedCase.IsContact;
            this.OtherNames = updatedCase.OtherNames;
            this.RecordId = updatedCase.RecordId;
            this.RecordStatus = updatedCase.RecordStatus;
            this.SubCounty = updatedCase.SubCounty;
            this.SubCountyHosp = updatedCase.SubCountyHosp;
            this.Surname = updatedCase.Surname;
            this.IsolationCurrent = updatedCase.IsolationCurrent;
            this.UniqueKey = updatedCase.UniqueKey;
            this.Village = updatedCase.Village;
            this.VillageHosp = updatedCase.VillageHosp;
            this.Parish = updatedCase.Parish;
            this.IsHCW = updatedCase.IsHCW;
            this.PlaceOfDeath = updatedCase.PlaceOfDeath;
            this.PlaceOfDeathLocalized = updatedCase.PlaceOfDeathLocalized;
            this.DateLastLabSampleCollected = updatedCase.DateLastLabSampleCollected;
            this.DateLastLabSampleTested = updatedCase.DateLastLabSampleTested;
            this.LastSampleInterpretation = updatedCase.LastSampleInterpretation;
            this.LastSamplePCRResult = updatedCase.LastSamplePCRResult;
            this.CurrentHospital = updatedCase.CurrentHospital;
            this.DateHospitalCurrentAdmit = updatedCase.DateHospitalCurrentAdmit;

            this.DateOutcomeInfoCompleted = updatedCase.DateOutcomeInfoCompleted;
            this.BleedUnexplainedEver = updatedCase.BleedUnexplainedEver;
            this.SpecifyBleeding = updatedCase.SpecifyBleeding;
            this.HospitalDischarge = updatedCase.HospitalDischarge;
            this.HospitalDischargeDistrict = updatedCase.HospitalDischargeDistrict;
            this.DateDischargeIsoEst = updatedCase.DateDischargeIsoEst;
            this.DateDischargeHospital = updatedCase.DateDischargeHospital;
            this.DateDischargeHospitalEst = updatedCase.DateDischargeHospitalEst;
            this.DateDeath2 = updatedCase.DateDeath2;
            this.DateDeath2Est = updatedCase.DateDeath2Est;
            this.DateDeath2EstSpecify = updatedCase.DateDeath2EstSpecify;
            this.PlaceDeath = updatedCase.PlaceDeath;
            this.HospitalDeath = updatedCase.HospitalDeath;
            this.PlaceDeathOther = updatedCase.PlaceDeathOther;
            this.VillageDeath = updatedCase.VillageDeath;
            this.SubCountyDeath = updatedCase.SubCountyDeath;
            this.DistrictDeath = updatedCase.DistrictDeath;
            this.DateFuneral = updatedCase.DateFuneral;
            this.FuneralConductedFam = updatedCase.FuneralConductedFam;
            this.FuneralConductedOutbreakTeam = updatedCase.FuneralConductedOutbreakTeam;
            this.VillageFuneral = updatedCase.VillageFuneral;
            this.SubCountyFuneral = updatedCase.SubCountyFuneral;
            this.DistrictFuneral = updatedCase.DistrictFuneral;

            this.OriginalID = updatedCase.OriginalID;

            this.SymptomFever = updatedCase.SymptomFever;
            this.SymptomFeverTemp = updatedCase.SymptomFeverTemp;
            this.SymptomFeverTempSource = updatedCase.SymptomFeverTempSource;
            this.SymptomVomiting = updatedCase.SymptomVomiting;
            this.SymptomDiarrhea = updatedCase.SymptomDiarrhea;
            this.SymptomFatigue = updatedCase.SymptomFatigue;
            this.SymptomAnorexia = updatedCase.SymptomAnorexia;
            this.SymptomAbdPain = updatedCase.SymptomAbdPain;
            this.SymptomChestPain = updatedCase.SymptomChestPain;
            this.SymptomMusclePain = updatedCase.SymptomMusclePain;
            this.SymptomJointPain = updatedCase.SymptomJointPain;
            this.SymptomHeadache = updatedCase.SymptomHeadache;
            this.SymptomCough = updatedCase.SymptomCough;
            this.SymptomDiffBreathe = updatedCase.SymptomDiffBreathe;
            this.SymptomDiffSwallow = updatedCase.SymptomDiffSwallow;
            this.SymptomSoreThroat = updatedCase.SymptomSoreThroat;
            this.SymptomJaundice = updatedCase.SymptomJaundice;
            this.SymptomConjunctivitis = updatedCase.SymptomConjunctivitis;
            this.SymptomRash = updatedCase.SymptomRash;
            this.SymptomHiccups = updatedCase.SymptomHiccups;
            this.SymptomPainEyes = updatedCase.SymptomPainEyes;
            this.SymptomUnconscious = updatedCase.SymptomUnconscious;
            this.SymptomConfused = updatedCase.SymptomConfused;
            this.SymptomOtherHemo = updatedCase.SymptomOtherHemo;
            this.SymptomOtherHemoSpecify = updatedCase.SymptomOtherHemoSpecify;

            this.SymptomFeverFinal = updatedCase.SymptomFeverFinal;
            this.SymptomFeverTempFinal = updatedCase.SymptomFeverTempFinal;
            this.SymptomFeverTempSourceFinal = updatedCase.SymptomFeverTempSourceFinal;
            this.SymptomVomitingFinal = updatedCase.SymptomVomitingFinal;
            this.SymptomDiarrheaFinal = updatedCase.SymptomDiarrheaFinal;
            this.SymptomFatigueFinal = updatedCase.SymptomFatigueFinal;
            this.SymptomAnorexiaFinal = updatedCase.SymptomAnorexiaFinal;
            this.SymptomAbdPainFinal = updatedCase.SymptomAbdPainFinal;
            this.SymptomChestPainFinal = updatedCase.SymptomChestPainFinal;
            this.SymptomMusclePainFinal = updatedCase.SymptomMusclePainFinal;
            this.SymptomJointPainFinal = updatedCase.SymptomJointPainFinal;
            this.SymptomHeadacheFinal = updatedCase.SymptomHeadacheFinal;
            this.SymptomCoughFinal = updatedCase.SymptomCoughFinal;
            this.SymptomDiffBreatheFinal = updatedCase.SymptomDiffBreatheFinal;
            this.SymptomDiffSwallowFinal = updatedCase.SymptomDiffSwallowFinal;
            this.SymptomSoreThroatFinal = updatedCase.SymptomSoreThroatFinal;
            this.SymptomJaundiceFinal = updatedCase.SymptomJaundiceFinal;
            this.SymptomConjunctivitisFinal = updatedCase.SymptomConjunctivitisFinal;
            this.SymptomRashFinal = updatedCase.SymptomRashFinal;
            this.SymptomHiccupsFinal = updatedCase.SymptomHiccupsFinal;
            this.SymptomPainEyesFinal = updatedCase.SymptomPainEyesFinal;
            this.SymptomUnconsciousFinal = updatedCase.SymptomUnconsciousFinal;
            this.SymptomConfusedFinal = updatedCase.SymptomConfusedFinal;
            this.SymptomOtherHemoFinal = updatedCase.SymptomOtherHemoFinal;
            this.SymptomOtherHemoFinalSpecify = updatedCase.SymptomOtherHemoFinalSpecify;

            RecordStatusComplete = updatedCase.RecordStatusComplete;
            RecordStatusMissCRF = updatedCase.RecordStatusMissCRF;
            RecordStatusNoCRF = updatedCase.RecordStatusNoCRF;
            RecordStatusPenLab = updatedCase.RecordStatusPenLab;
            RecordStatusPenOut = updatedCase.RecordStatusPenOut;
            RecordStatus = updatedCase.RecordStatus;
            PhoneNumber = updatedCase.PhoneNumber;
            PhoneOwner = updatedCase.PhoneOwner;
            StatusReport = updatedCase.StatusReport;
            OccupationFarmer = updatedCase.OccupationFarmer;
            OccupationButcher = updatedCase.OccupationButcher;
            OccupationHunter = updatedCase.OccupationHunter;
            OccupationMiner = updatedCase.OccupationMiner;
            OccupationReligious = updatedCase.OccupationReligious;
            OccupationHousewife = updatedCase.OccupationHousewife;
            OccupationStudent = updatedCase.OccupationStudent;
            OccupationChild = updatedCase.OccupationChild;
            OccupationBusinessman = updatedCase.OccupationBusinessman;
            OccupationTransporter = updatedCase.OccupationTransporter;

            OccupationTraditionalHealer = updatedCase.OccupationTraditionalHealer; // { get { return _OccupationTraditionalHealer; } set { if (_OccupationTraditionalHealer != value) { _OccupationTraditionalHealer = value; RaisePropertyChanged("OccupationTraditionalHealer"); } } }
            OccupationOther = updatedCase.OccupationOther; // { get { return _OccupationOther; } set { if (_OccupationOther != value) { _OccupationOther = value; RaisePropertyChanged("OccupationOther"); } } }

            OccupationTransporterSpecify = updatedCase.OccupationTransporterSpecify; // { get { return _OccupationTransporterSpecify; } set { if (_OccupationTransporterSpecify != value) { _OccupationTransporterSpecify = value; RaisePropertyChanged("OccupationTransporterSpecify"); } } }
            OccupationBusinessSpecify = updatedCase.OccupationBusinessSpecify; // { get { return _OccupationBusinessSpecify; } set { if (_OccupationBusinessSpecify != value) { _OccupationBusinessSpecify = value; RaisePropertyChanged("OccupationBusinessSpecify"); } } }
            OccupationOtherSpecify = updatedCase.OccupationOtherSpecify; // { get { return _OccupationOtherSpecify; } set { if (_OccupationOtherSpecify != value) { _OccupationOtherSpecify = value; RaisePropertyChanged("OccupationOtherSpecify"); } } }
            OccupationHCWPosition = updatedCase.OccupationHCWPosition; // { get { return _OccupationHCWPosition; } set { if (_OccupationHCWPosition != value) { _OccupationHCWPosition = value; RaisePropertyChanged("OccupationHCWPosition"); } } }
            OccupationHCWFacility = updatedCase.OccupationHCWFacility; // { get { return _OccupationHCWFacility; } set { if (_OccupationHCWFacility != value) { _OccupationHCWFacility = value; RaisePropertyChanged("OccupationHCWFacility"); } } }

            Latitude = updatedCase.Latitude; // { get { return _Latitude; } set { if (_Latitude != value) { _Latitude = value; RaisePropertyChanged("Latitude"); } } }
            Longitude = updatedCase.Longitude; // { get { return _Longitude; } set { if (_Longitude != value) { _Longitude = value; RaisePropertyChanged("Longitude"); } } }

            DateOnsetLocalStart = updatedCase.DateOnsetLocalStart; // { get { return _DateOnsetLocalStart; } set { if (_DateOnsetLocalStart != value) { _DateOnsetLocalStart = value; RaisePropertyChanged("DateOnsetLocalStart"); } } }
            DateOnsetLocalEnd = updatedCase.DateOnsetLocalEnd; // { get { return _DateOnsetLocalEnd; } set { if (_DateOnsetLocalEnd != value) { _DateOnsetLocalEnd = value; RaisePropertyChanged("DateOnsetLocalEnd"); } } }

            HospitalizedCurrent = updatedCase.HospitalizedCurrent; // { get { return _HospitalizedCurrent; } set { if (_HospitalizedCurrent != value) { _HospitalizedCurrent = value; RaisePropertyChanged("HospitalizedCurrent"); } } }
            HospitalizedPast = updatedCase.HospitalizedPast; // { get { return _HospitalizedPast; } set { if (_HospitalizedPast != value) { _HospitalizedPast = value; RaisePropertyChanged("HospitalizedPast"); } } }

            DateHospitalPastStart1 = updatedCase.DateHospitalPastStart1; // { get { return _DateHospitalPastStart1; } set { if (_DateHospitalPastStart1 != value) { _DateHospitalPastStart1 = value; RaisePropertyChanged("DateHospitalPastStart1"); } } }
            DateHospitalPastStart2 = updatedCase.DateHospitalPastStart2; // { get { return _DateHospitalPastStart2; } set { if (_DateHospitalPastStart2 != value) { _DateHospitalPastStart2 = value; RaisePropertyChanged("DateHospitalPastStart2"); } } }

            DateHospitalPastEnd1 = updatedCase.DateHospitalPastEnd1; // { get { return _DateHospitalPastEnd1; } set { if (_DateHospitalPastEnd1 != value) { _DateHospitalPastEnd1 = value; RaisePropertyChanged("DateHospitalPastEnd1"); } } }
            DateHospitalPastEnd2 = updatedCase.DateHospitalPastEnd2; // { get { return _DateHospitalPastEnd2; } set { if (_DateHospitalPastEnd2 != value) { _DateHospitalPastEnd2 = value; RaisePropertyChanged("DateHospitalPastEnd2"); } } }

            HospitalPast1 = updatedCase.HospitalPast1; // { get { return _HospitalPast1; } set { if (_HospitalPast1 != value) { _HospitalPast1 = value; RaisePropertyChanged("HospitalPast1"); } } }
            HospitalPast2 = updatedCase.HospitalPast2; // { get { return _HospitalPast2; } set { if (_HospitalPast2 != value) { _HospitalPast2 = value; RaisePropertyChanged("HospitalPast2"); } } }

            HospitalVillage1 = updatedCase.HospitalVillage1; // { get { return _HospitalVillage1; } set { if (_HospitalVillage1 != value) { _HospitalVillage1 = value; RaisePropertyChanged("HospitalVillage1"); } } }
            HospitalVillage2 = updatedCase.HospitalVillage2; // { get { return _HospitalVillage2; } set { if (_HospitalVillage2 != value) { _HospitalVillage2 = value; RaisePropertyChanged("HospitalVillage2"); } } }

            HospitalDistrict1 = updatedCase.HospitalDistrict1; // { get { return _HospitalDistrict1; } set { if (_HospitalDistrict1 != value) { _HospitalDistrict1 = value; RaisePropertyChanged("HospitalDistrict1"); } } }
            HospitalDistrict2 = updatedCase.HospitalDistrict2; // { get { return _HospitalDistrict2; } set { if (_HospitalDistrict2 != value) { _HospitalDistrict2 = value; RaisePropertyChanged("HospitalDistrict2"); } } }

            IsolationPast1 = updatedCase.IsolationPast1; // { get { return _IsolationPast1; } set { if (_IsolationPast1 != value) { _IsolationPast1 = value; RaisePropertyChanged("IsolationPast1"); } } }
            IsolationPast2 = updatedCase.IsolationPast2; // { get { return _IsolationPast2; } set { if (_IsolationPast2 != value) { _IsolationPast2 = value; RaisePropertyChanged("IsolationPast2"); } } }

            SymptomFeverFinal = updatedCase.SymptomFeverFinal; // { get { return _SymptomFeverFinal; } set { if (_SymptomFeverFinal != value) { _SymptomFeverFinal = value; RaisePropertyChanged("SymptomFeverFinal"); } } }
            SymptomFeverTempFinal = updatedCase.SymptomFeverTempFinal; // { get { return _SymptomFeverTempFinal; } set { if (_SymptomFeverTempFinal != value) { _SymptomFeverTempFinal = value; RaisePropertyChanged("SymptomFeverTempFinal"); } } }
            SymptomFeverTempSourceFinal = updatedCase.SymptomFeverTempSourceFinal; // { get { return _SymptomFeverTempSourceFinal; } set { if (_SymptomFeverTempSourceFinal != value) { _SymptomFeverTempSourceFinal = value; RaisePropertyChanged("SymptomFeverTempSourceFinal"); } } }
            SymptomVomitingFinal = updatedCase.SymptomVomitingFinal; // { get { return _SymptomVomitingFinal; } set { if (_SymptomVomitingFinal != value) { _SymptomVomitingFinal = value; RaisePropertyChanged("SymptomVomitingFinal"); } } }
            SymptomDiarrheaFinal = updatedCase.SymptomDiarrheaFinal; // { get { return _SymptomDiarrheaFinal; } set { if (_SymptomDiarrheaFinal != value) { _SymptomDiarrheaFinal = value; RaisePropertyChanged("SymptomDiarrheaFinal"); } } }
            SymptomFatigueFinal = updatedCase.SymptomFatigueFinal; // { get { return _SymptomFatigueFinal; } set { if (_SymptomFatigueFinal != value) { _SymptomFatigueFinal = value; RaisePropertyChanged("SymptomFatigueFinal"); } } }
            SymptomAnorexiaFinal = updatedCase.SymptomAnorexiaFinal; // { get { return _SymptomAnorexiaFinal; } set { if (_SymptomAnorexiaFinal != value) { _SymptomAnorexiaFinal = value; RaisePropertyChanged("SymptomAnorexiaFinal"); } } }
            SymptomAbdPainFinal = updatedCase.SymptomAbdPainFinal; // { get { return _SymptomAbdPainFinal; } set { if (_SymptomAbdPainFinal != value) { _SymptomAbdPainFinal = value; RaisePropertyChanged("SymptomAbdPainFinal"); } } }
            SymptomChestPainFinal = updatedCase.SymptomChestPainFinal; // { get { return _SymptomChestPainFinal; } set { if (_SymptomChestPainFinal != value) { _SymptomChestPainFinal = value; RaisePropertyChanged("SymptomChestPainFinal"); } } }
            SymptomMusclePainFinal = updatedCase.SymptomMusclePainFinal; // { get { return _SymptomMusclePainFinal; } set { if (_SymptomMusclePainFinal != value) { _SymptomMusclePainFinal = value; RaisePropertyChanged("SymptomMusclePainFinal"); } } }
            SymptomJointPainFinal = updatedCase.SymptomJointPainFinal; // { get { return _SymptomJointPainFinal; } set { if (_SymptomJointPainFinal != value) { _SymptomJointPainFinal = value; RaisePropertyChanged("SymptomJointPainFinal"); } } }
            SymptomHeadacheFinal = updatedCase.SymptomHeadacheFinal; // { get { return _SymptomHeadacheFinal; } set { if (_SymptomHeadacheFinal != value) { _SymptomHeadacheFinal = value; RaisePropertyChanged("SymptomHeadacheFinal"); } } }
            SymptomCoughFinal = updatedCase.SymptomCoughFinal; // { get { return _SymptomCoughFinal; } set { if (_SymptomCoughFinal != value) { _SymptomCoughFinal = value; RaisePropertyChanged("SymptomCoughFinal"); } } }
            SymptomDiffBreatheFinal = updatedCase.SymptomDiffBreatheFinal; // { get { return _SymptomDiffBreatheFinal; } set { if (_SymptomDiffBreatheFinal != value) { _SymptomDiffBreatheFinal = value; RaisePropertyChanged("SymptomDiffBreatheFinal"); } } }
            SymptomDiffSwallowFinal = updatedCase.SymptomDiffSwallowFinal; // { get { return _SymptomDiffSwallowFinal; } set { if (_SymptomDiffSwallowFinal != value) { _SymptomDiffSwallowFinal = value; RaisePropertyChanged("SymptomDiffSwallowFinal"); } } }
            SymptomSoreThroatFinal = updatedCase.SymptomSoreThroatFinal; // { get { return _SymptomSoreThroatFinal; } set { if (_SymptomSoreThroatFinal != value) { _SymptomSoreThroatFinal = value; RaisePropertyChanged("SymptomSoreThroatFinal"); } } }
            SymptomJaundiceFinal = updatedCase.SymptomJaundiceFinal; // { get { return _SymptomJaundiceFinal; } set { if (_SymptomJaundiceFinal != value) { _SymptomJaundiceFinal = value; RaisePropertyChanged("SymptomJaundiceFinal"); } } }
            SymptomConjunctivitisFinal = updatedCase.SymptomConjunctivitisFinal; // { get { return _SymptomConjunctivitisFinal; } set { if (_SymptomConjunctivitisFinal != value) { _SymptomConjunctivitisFinal = value; RaisePropertyChanged("SymptomConjunctivitisFinal"); } } }
            SymptomRashFinal = updatedCase.SymptomRashFinal; // { get { return _SymptomRashFinal; } set { if (_SymptomRashFinal != value) { _SymptomRashFinal = value; RaisePropertyChanged("SymptomRashFinal"); } } }
            SymptomHiccupsFinal = updatedCase.SymptomHiccupsFinal; // { get { return _SymptomHiccupsFinal; } set { if (_SymptomHiccupsFinal != value) { _SymptomHiccupsFinal = value; RaisePropertyChanged("SymptomHiccupsFinal"); } } }
            SymptomPainEyesFinal = updatedCase.SymptomPainEyesFinal; // { get { return _SymptomPainEyesFinal; } set { if (_SymptomPainEyesFinal != value) { _SymptomPainEyesFinal = value; RaisePropertyChanged("SymptomPainEyesFinal"); } } }
            SymptomUnconsciousFinal = updatedCase.SymptomUnconsciousFinal; // { get { return _SymptomUnconsciousFinal; } set { if (_SymptomUnconsciousFinal != value) { _SymptomUnconsciousFinal = value; RaisePropertyChanged("SymptomUnconsciousFinal"); } } }
            SymptomConfusedFinal = updatedCase.SymptomConfusedFinal; // { get { return _SymptomConfusedFinal; } set { if (_SymptomConfusedFinal != value) { _SymptomConfusedFinal = value; RaisePropertyChanged("SymptomConfusedFinal"); } } }
            SymptomOtherHemoFinal = updatedCase.SymptomOtherHemoFinal; // { get { return _SymptomOtherHemoFinal; } set { if (_SymptomOtherHemoFinal != value) { _SymptomOtherHemoFinal = value; RaisePropertyChanged("SymptomOtherHemoFinal"); } } }
            SymptomOtherHemoFinalSpecify = updatedCase.SymptomOtherHemoFinalSpecify; // { get { return _SymptomOtherHemoFinalSpecify; } set { if (_SymptomOtherHemoFinalSpecify != value) { _SymptomOtherHemoFinalSpecify = value; RaisePropertyChanged("SymptomOtherHemoFinalSpecify"); } } }

            SymptomUnexplainedBleeding = updatedCase.SymptomUnexplainedBleeding; // { get { return _SymptomUnexplainedBleeding; } set { if (_SymptomUnexplainedBleeding != value) { _SymptomUnexplainedBleeding = value; RaisePropertyChanged("SymptomUnexplainedBleeding"); } } }
            SymptomBleedGums = updatedCase.SymptomBleedGums; // { get { return _SymptomBleedGums; } set { if (_SymptomBleedGums != value) { _SymptomBleedGums = value; RaisePropertyChanged("SymptomBleedGums"); } } }
            SymptomBleedInjectionSite = updatedCase.SymptomBleedInjectionSite; // { get { return _SymptomBleedInjectionSite; } set { if (_SymptomBleedInjectionSite != value) { _SymptomBleedInjectionSite = value; RaisePropertyChanged("SymptomBleedInjectionSite"); } } }
            SymptomNoseBleed = updatedCase.SymptomNoseBleed; // { get { return _SymptomNoseBleed; } set { if (_SymptomNoseBleed != value) { _SymptomNoseBleed = value; RaisePropertyChanged("SymptomNoseBleed"); } } }
            SymptomBloodyStool = updatedCase.SymptomBloodyStool; // { get { return _SymptomBloodyStool; } set { if (_SymptomBloodyStool != value) { _SymptomBloodyStool = value; RaisePropertyChanged("SymptomBloodyStool"); } } }
            SymptomHematemesis = updatedCase.SymptomHematemesis; // { get { return _SymptomHematemesis; } set { if (_SymptomHematemesis != value) { _SymptomHematemesis = value; RaisePropertyChanged("SymptomHematemesis"); } } }
            SymptomBloodVomit = updatedCase.SymptomBloodVomit; // { get { return _SymptomBloodVomit; } set { if (_SymptomBloodVomit != value) { _SymptomBloodVomit = value; RaisePropertyChanged("SymptomBloodVomit"); } } }
            SymptomCoughBlood = updatedCase.SymptomCoughBlood; // { get { return _SymptomCoughBlood; } set { if (_SymptomCoughBlood != value) { _SymptomCoughBlood = value; RaisePropertyChanged("SymptomCoughBlood"); } } }
            SymptomBleedVagina = updatedCase.SymptomBleedVagina; // { get { return _SymptomBleedVagina; } set { if (_SymptomBleedVagina != value) { _SymptomBleedVagina = value; RaisePropertyChanged("SymptomBleedVagina"); } } }
            SymptomBleedSkin = updatedCase.SymptomBleedSkin; // { get { return _SymptomBleedSkin; } set { if (_SymptomBleedSkin != value) { _SymptomBleedSkin = value; RaisePropertyChanged("SymptomBleedSkin"); } } }
            SymptomBleedUrine = updatedCase.SymptomBleedUrine; // { get { return _SymptomBleedUrine; } set { if (_SymptomBleedUrine != value) { _SymptomBleedUrine = value; RaisePropertyChanged("SymptomBleedUrine"); } } }
            SymptomOtherNonHemorrhagic = updatedCase.SymptomOtherNonHemorrhagic; // { get { return _SymptomOtherNonHemorrhagic; } set { if (_SymptomOtherNonHemorrhagic != value) { _SymptomOtherNonHemorrhagic = value; RaisePropertyChanged("SymptomOtherNonHemorrhagic"); } } }

            SymptomFever = updatedCase.SymptomFever; // { get { return _SymptomFever; } set { if (_SymptomFever != value) { _SymptomFever = value; RaisePropertyChanged("SymptomFever"); } } }
            SymptomFeverTemp = updatedCase.SymptomFeverTemp; // { get { return _SymptomFeverTemp; } set { if (_SymptomFeverTemp != value) { _SymptomFeverTemp = value; RaisePropertyChanged("SymptomFeverTemp"); } } }
            SymptomFeverTempSource = updatedCase.SymptomFeverTempSource; // { get { return _SymptomFeverTempSource; } set { if (_SymptomFeverTempSource != value) { _SymptomFeverTempSource = value; RaisePropertyChanged("SymptomFeverTempSource"); } } }
            SymptomVomiting = updatedCase.SymptomVomiting; // { get { return _SymptomVomiting; } set { if (_SymptomVomiting != value) { _SymptomVomiting = value; RaisePropertyChanged("SymptomVomiting"); } } }
            SymptomDiarrhea = updatedCase.SymptomDiarrhea; // { get { return _SymptomDiarrhea; } set { if (_SymptomDiarrhea != value) { _SymptomDiarrhea = value; RaisePropertyChanged("SymptomDiarrhea"); } } }
            SymptomFatigue = updatedCase.SymptomFatigue; // { get { return _SymptomFatigue; } set { if (_SymptomFatigue != value) { _SymptomFatigue = value; RaisePropertyChanged("SymptomFatigue"); } } }
            SymptomAnorexia = updatedCase.SymptomAnorexia; // { get { return _SymptomAnorexia; } set { if (_SymptomAnorexia != value) { _SymptomAnorexia = value; RaisePropertyChanged("SymptomAnorexia"); } } }
            SymptomAbdPain = updatedCase.SymptomAbdPain; // { get { return _SymptomAbdPain; } set { if (_SymptomAbdPain != value) { _SymptomAbdPain = value; RaisePropertyChanged("SymptomAbdPain"); } } }
            SymptomChestPain = updatedCase.SymptomChestPain; // { get { return _SymptomChestPain; } set { if (_SymptomChestPain != value) { _SymptomChestPain = value; RaisePropertyChanged("SymptomChestPain"); } } }
            SymptomMusclePain = updatedCase.SymptomMusclePain; // { get { return _SymptomMusclePain; } set { if (_SymptomMusclePain != value) { _SymptomMusclePain = value; RaisePropertyChanged("SymptomMusclePain"); } } }
            SymptomJointPain = updatedCase.SymptomJointPain; // { get { return _SymptomJointPain; } set { if (_SymptomJointPain != value) { _SymptomJointPain = value; RaisePropertyChanged("SymptomJointPain"); } } }
            SymptomHeadache = updatedCase.SymptomHeadache; // { get { return _SymptomHeadache; } set { if (_SymptomHeadache != value) { _SymptomHeadache = value; RaisePropertyChanged("SymptomHeadache"); } } }
            SymptomCough = updatedCase.SymptomCough; // { get { return _SymptomCough; } set { if (_SymptomCough != value) { _SymptomCough = value; RaisePropertyChanged("SymptomCough"); } } }
            SymptomDiffBreathe = updatedCase.SymptomDiffBreathe; // { get { return _SymptomDiffBreathe; } set { if (_SymptomDiffBreathe != value) { _SymptomDiffBreathe = value; RaisePropertyChanged("SymptomDiffBreathe"); } } }
            SymptomDiffSwallow = updatedCase.SymptomDiffSwallow; // { get { return _SymptomDiffSwallow; } set { if (_SymptomDiffSwallow != value) { _SymptomDiffSwallow = value; RaisePropertyChanged("SymptomDiffSwallow"); } } }
            SymptomSoreThroat = updatedCase.SymptomSoreThroat; // { get { return _SymptomSoreThroat; } set { if (_SymptomSoreThroat != value) { _SymptomSoreThroat = value; RaisePropertyChanged("SymptomSoreThroat"); } } }
            SymptomJaundice = updatedCase.SymptomJaundice; // { get { return _SymptomJaundice; } set { if (_SymptomJaundice != value) { _SymptomJaundice = value; RaisePropertyChanged("SymptomJaundice"); } } }
            SymptomConjunctivitis = updatedCase.SymptomConjunctivitis; // { get { return _SymptomConjunctivitis; } set { if (_SymptomConjunctivitis != value) { _SymptomConjunctivitis = value; RaisePropertyChanged("SymptomConjunctivitis"); } } }
            SymptomRash = updatedCase.SymptomRash; // { get { return _SymptomRash; } set { if (_SymptomRash != value) { _SymptomRash = value; RaisePropertyChanged("SymptomRash"); } } }
            SymptomHiccups = updatedCase.SymptomHiccups; // { get { return _SymptomHiccups; } set { if (_SymptomHiccups != value) { _SymptomHiccups = value; RaisePropertyChanged("SymptomHiccups"); } } }
            SymptomPainEyes = updatedCase.SymptomPainEyes; // { get { return _SymptomPainEyes; } set { if (_SymptomPainEyes != value) { _SymptomPainEyes = value; RaisePropertyChanged("SymptomPainEyes"); } } }
            SymptomUnconscious = updatedCase.SymptomUnconscious; // { get { return _SymptomUnconscious; } set { if (_SymptomUnconscious != value) { _SymptomUnconscious = value; RaisePropertyChanged("SymptomUnconscious"); } } }
            SymptomConfused = updatedCase.SymptomConfused; // { get { return _SymptomConfused; } set { if (_SymptomConfused != value) { _SymptomConfused = value; RaisePropertyChanged("SymptomConfused"); } } }
            SymptomOtherHemo = updatedCase.SymptomOtherHemo; // { get { return _SymptomOtherHemo; } set { if (_SymptomOtherHemo != value) { _SymptomOtherHemo = value; RaisePropertyChanged("SymptomOtherHemo"); } } }
            SymptomOtherHemoSpecify = updatedCase.SymptomOtherHemoSpecify; // { get { return _SymptomOtherHemoSpecify; } set { if (_SymptomOtherHemoSpecify != value) { _SymptomOtherHemoSpecify = value; RaisePropertyChanged("SymptomOtherHemoSpecify"); } } }
            SymptomOtherNonHemorrhagicSpecify = updatedCase.SymptomOtherNonHemorrhagicSpecify; // { get { return _SymptomOtherNonHemorrhagicSpecify; } set { if (_SymptomOtherNonHemorrhagicSpecify != value) { _SymptomOtherNonHemorrhagicSpecify = value; RaisePropertyChanged("SymptomOtherNonHemorrhagicSpecify"); } } }

            HadContact = updatedCase.HadContact; // { get { return _HadContact; } set { if (_HadContact != value) { _HadContact = value; RaisePropertyChanged("HadContact"); } } }
            ContactName1 = updatedCase.ContactName1; // { get { return _ContactName1; } set { if (_ContactName1 != value) { _ContactName1 = value; RaisePropertyChanged("ContactName1"); } } }
            ContactName2 = updatedCase.ContactName2; // { get { return _ContactName2; } set { if (_ContactName2 != value) { _ContactName2 = value; RaisePropertyChanged("ContactName2"); } } }
            ContactName3 = updatedCase.ContactName3; // { get { return _ContactName3; } set { if (_ContactName3 != value) { _ContactName3 = value; RaisePropertyChanged("ContactName3"); } } }
            ContactRelation1 = updatedCase.ContactRelation1; // { get { return _ContactRelation1; } set { if (_ContactRelation1 != value) { _ContactRelation1 = value; RaisePropertyChanged("ContactRelation1"); } } }
            ContactRelation2 = updatedCase.ContactRelation2; // { get { return _ContactRelation2; } set { if (_ContactRelation2 != value) { _ContactRelation2 = value; RaisePropertyChanged("ContactRelation2"); } } }
            ContactRelation3 = updatedCase.ContactRelation3; // { get { return _ContactRelation3; } set { if (_ContactRelation3 != value) { _ContactRelation3 = value; RaisePropertyChanged("ContactRelation3"); } } }
            ContactStartDate1 = updatedCase.ContactStartDate1; // { get { return _ContactStartDate1; } set { if (_ContactStartDate1 != value) { _ContactStartDate1 = value; RaisePropertyChanged("ContactStartDate1"); } } }
            ContactStartDate2 = updatedCase.ContactStartDate2; // { get { return _ContactStartDate2; } set { if (_ContactStartDate2 != value) { _ContactStartDate2 = value; RaisePropertyChanged("ContactStartDate2"); } } }
            ContactStartDate3 = updatedCase.ContactStartDate3; // { get { return _ContactStartDate3; } set { if (_ContactStartDate3 != value) { _ContactStartDate3 = value; RaisePropertyChanged("ContactStartDate3"); } } }
            ContactEndDate1 = updatedCase.ContactEndDate1; // { get { return _ContactEndDate1; } set { if (_ContactEndDate1 != value) { _ContactEndDate1 = value; RaisePropertyChanged("ContactEndDate1"); } } }
            ContactEndDate2 = updatedCase.ContactEndDate2; // { get { return _ContactEndDate2; } set { if (_ContactEndDate2 != value) { _ContactEndDate2 = value; RaisePropertyChanged("ContactEndDate2"); } } }
            ContactEndDate3 = updatedCase.ContactEndDate3; // { get { return _ContactEndDate3; } set { if (_ContactEndDate3 != value) { _ContactEndDate3 = value; RaisePropertyChanged("ContactEndDate3"); } } }
            ContactDate1Estimated = updatedCase.ContactDate1Estimated; // { get { return _ContactDate1Estimated; } set { if (_ContactDate1Estimated != value) { _ContactDate1Estimated = value; RaisePropertyChanged("ContactDate1Estimated"); } } }
            ContactDate2Estimated = updatedCase.ContactDate2Estimated; // { get { return _ContactDate2Estimated; } set { if (_ContactDate2Estimated != value) { _ContactDate2Estimated = value; RaisePropertyChanged("ContactDate2Estimated"); } } }
            ContactDate3Estimated = updatedCase.ContactDate3Estimated; // { get { return _ContactDate3Estimated; } set { if (_ContactDate3Estimated != value) { _ContactDate3Estimated = value; RaisePropertyChanged("ContactDate3Estimated"); } } }
            ContactVillage1 = updatedCase.ContactVillage1; // { get { return _ContactVillage1; } set { if (_ContactVillage1 != value) { _ContactVillage1 = value; RaisePropertyChanged("ContactVillage1"); } } }
            ContactVillage2 = updatedCase.ContactVillage2; // { get { return _ContactVillage2; } set { if (_ContactVillage2 != value) { _ContactVillage2 = value; RaisePropertyChanged("ContactVillage2"); } } }
            ContactVillage3 = updatedCase.ContactVillage3; // { get { return _ContactVillage3; } set { if (_ContactVillage3 != value) { _ContactVillage3 = value; RaisePropertyChanged("ContactVillage3"); } } }
            ContactDistrict1 = updatedCase.ContactDistrict1; // { get { return _ContactDistrict1; } set { if (_ContactDistrict1 != value) { _ContactDistrict1 = value; RaisePropertyChanged("ContactDistrict1"); } } }
            ContactDistrict2 = updatedCase.ContactDistrict2; // { get { return _ContactDistrict2; } set { if (_ContactDistrict2 != value) { _ContactDistrict2 = value; RaisePropertyChanged("ContactDistrict2"); } } }
            ContactDistrict3 = updatedCase.ContactDistrict3; // { get { return _ContactDistrict3; } set { if (_ContactDistrict3 != value) { _ContactDistrict3 = value; RaisePropertyChanged("ContactDistrict3"); } } }
            ContactCountry1 = updatedCase.ContactCountry1; // { get { return _ContactCountry1; } set { if (_ContactCountry1 != value) { _ContactCountry1 = value; RaisePropertyChanged("ContactCountry1"); } } }
            ContactCountry2 = updatedCase.ContactCountry2; // { get { return _ContactCountry2; } set { if (_ContactCountry2 != value) { _ContactCountry2 = value; RaisePropertyChanged("ContactCountry2"); } } }
            ContactCountry3 = updatedCase.ContactCountry3; // { get { return _ContactCountry3; } set { if (_ContactCountry3 != value) { _ContactCountry3 = value; RaisePropertyChanged("ContactCountry3"); } } }
            TypesOfContact1 = updatedCase.TypesOfContact1; // { get { return _TypesOfContact1; } set { if (_TypesOfContact1 != value) { _TypesOfContact1 = value; RaisePropertyChanged("TypesOfContact1"); } } }
            TypesOfContact2 = updatedCase.TypesOfContact2; // { get { return _TypesOfContact2; } set { if (_TypesOfContact2 != value) { _TypesOfContact2 = value; RaisePropertyChanged("TypesOfContact2"); } } }
            TypesOfContact3 = updatedCase.TypesOfContact3; // { get { return _TypesOfContact3; } set { if (_TypesOfContact3 != value) { _TypesOfContact3 = value; RaisePropertyChanged("TypesOfContact3"); } } }
            ContactStatus1 = updatedCase.ContactStatus1; // { get { return _ContactStatus1; } set { if (_ContactStatus1 != value) { _ContactStatus1 = value; RaisePropertyChanged("ContactStatus1"); } } }
            ContactStatus2 = updatedCase.ContactStatus2; // { get { return _ContactStatus2; } set { if (_ContactStatus2 != value) { _ContactStatus2 = value; RaisePropertyChanged("ContactStatus2"); } } }
            ContactStatus3 = updatedCase.ContactStatus3; // { get { return _ContactStatus3; } set { if (_ContactStatus3 != value) { _ContactStatus3 = value; RaisePropertyChanged("ContactStatus3"); } } }
            ContactDeathDate1 = updatedCase.ContactDeathDate1; // { get { return _ContactDeathDate1; } set { if (_ContactDeathDate1 != value) { _ContactDeathDate1 = value; RaisePropertyChanged("ContactDeathDate1"); } } }
            ContactDeathDate2 = updatedCase.ContactDeathDate2; // { get { return _ContactDeathDate2; } set { if (_ContactDeathDate2 != value) { _ContactDeathDate2 = value; RaisePropertyChanged("ContactDeathDate2"); } } }
            ContactDeathDate3 = updatedCase.ContactDeathDate3; // { get { return _ContactDeathDate3; } set { if (_ContactDeathDate3 != value) { _ContactDeathDate3 = value; RaisePropertyChanged("ContactDeathDate3"); } } }

            AttendFuneral = updatedCase.AttendFuneral; // { get { return _AttendFuneral; } set { if (_AttendFuneral != value) { _AttendFuneral = value; RaisePropertyChanged("AttendFuneral"); } } }
            FuneralNameDeceased1 = updatedCase.FuneralNameDeceased1; // { get { return _FuneralNameDeceased1; } set { if (_FuneralNameDeceased1 != value) { _FuneralNameDeceased1 = value; RaisePropertyChanged("FuneralNameDeceased1"); } } }
            FuneralNameDeceased2 = updatedCase.FuneralNameDeceased2; // { get { return _FuneralNameDeceased2; } set { if (_FuneralNameDeceased2 != value) { _FuneralNameDeceased2 = value; RaisePropertyChanged("FuneralNameDeceased2"); } } }
            FuneralRelationDeceased1 = updatedCase.FuneralRelationDeceased1; // { get { return _FuneralRelationDeceased1; } set { if (_FuneralRelationDeceased1 != value) { _FuneralRelationDeceased1 = value; RaisePropertyChanged("FuneralRelationDeceased1"); } } }
            FuneralRelationDeceased2 = updatedCase.FuneralRelationDeceased2; // { get { return _FuneralRelationDeceased2; } set { if (_FuneralRelationDeceased2 != value) { _FuneralRelationDeceased2 = value; RaisePropertyChanged("FuneralRelationDeceased2"); } } }
            FuneralStartDate1 = updatedCase.FuneralStartDate1; // { get { return _FuneralStartDate1; } set { if (_FuneralStartDate1 != value) { _FuneralStartDate1 = value; RaisePropertyChanged("FuneralStartDate1"); } } }
            FuneralStartDate2 = updatedCase.FuneralStartDate2; // { get { return _FuneralStartDate2; } set { if (_FuneralStartDate2 != value) { _FuneralStartDate2 = value; RaisePropertyChanged("FuneralStartDate2"); } } }
            FuneralEndDate1 = updatedCase.FuneralEndDate1; // { get { return _FuneralEndDate1; } set { if (_FuneralEndDate1 != value) { _FuneralEndDate1 = value; RaisePropertyChanged("FuneralEndDate1"); } } }
            FuneralEndDate2 = updatedCase.FuneralEndDate2; // { get { return _FuneralEndDate2; } set { if (_FuneralEndDate2 != value) { _FuneralEndDate2 = value; RaisePropertyChanged("FuneralEndDate2"); } } }
            FuneralVillage1 = updatedCase.FuneralVillage1; // { get { return _FuneralVillage1; } set { if (_FuneralVillage1 != value) { _FuneralVillage1 = value; RaisePropertyChanged("FuneralVillage1"); } } }
            FuneralVillage2 = updatedCase.FuneralVillage2; // { get { return _FuneralVillage2; } set { if (_FuneralVillage2 != value) { _FuneralVillage2 = value; RaisePropertyChanged("FuneralVillage2"); } } }
            FuneralDistrict1 = updatedCase.FuneralDistrict1; // { get { return _FuneralDistrict1; } set { if (_FuneralDistrict1 != value) { _FuneralDistrict1 = value; RaisePropertyChanged("FuneralDistrict1"); } } }
            FuneralDistrict2 = updatedCase.FuneralDistrict2; // { get { return _FuneralDistrict2; } set { if (_FuneralDistrict2 != value) { _FuneralDistrict2 = value; RaisePropertyChanged("FuneralDistrict2"); } } }
            FuneralTouchBody1 = updatedCase.FuneralTouchBody1; // { get { return _FuneralTouchBody1; } set { if (_FuneralTouchBody1 != value) { _FuneralTouchBody1 = value; RaisePropertyChanged("FuneralTouchBody1"); } } }
            FuneralTouchBody2 = updatedCase.FuneralTouchBody2; // { get { return _FuneralTouchBody2; } set { if (_FuneralTouchBody2 != value) { _FuneralTouchBody2 = value; RaisePropertyChanged("FuneralTouchBody2"); } } }

            Travel = updatedCase.Travel; // { get { return _Travel; } set { if (_Travel != value) { _Travel = value; RaisePropertyChanged("Travel"); } } }
            TravelVillage = updatedCase.TravelVillage; // { get { return _TravelVillage; } set { if (_TravelVillage != value) { _TravelVillage = value; RaisePropertyChanged("TravelVillage"); } } }
            TravelDistrict = updatedCase.TravelDistrict; // { get { return _TravelDistrict; } set { if (_TravelDistrict != value) { _TravelDistrict = value; RaisePropertyChanged("TravelDistrict"); } } }
            TravelCountry = updatedCase.TravelCountry; // { get { return _TravelCountry; } set { if (_TravelCountry != value) { _TravelCountry = value; RaisePropertyChanged("TravelCountry"); } } }
            TravelStartDate = updatedCase.TravelStartDate; // { get { return _TravelStartDate; } set { if (_TravelStartDate != value) { _TravelStartDate = value; RaisePropertyChanged("TravelStartDate"); } } }
            TravelEndDate = updatedCase.TravelEndDate; // { get { return _TravelEndDate; } set { if (_TravelEndDate != value) { _TravelEndDate = value; RaisePropertyChanged("TravelEndDate"); } } }
            TravelDateEstimated = updatedCase.TravelDateEstimated; // { get { return _TravelDateEstimated; } set { if (_TravelDateEstimated != value) { _TravelDateEstimated = value; RaisePropertyChanged("TravelDateEstimated"); } } }

            HospitalBeforeIll = updatedCase.HospitalBeforeIll; // { get { return _HospitalBeforeIll; } set { if (_HospitalBeforeIll != value) { _HospitalBeforeIll = value; RaisePropertyChanged("HospitalBeforeIll"); } } }
            HospitalBeforeIllPatient = updatedCase.HospitalBeforeIllPatient; // { get { return _HospitalBeforeIllPatient; } set { if (_HospitalBeforeIllPatient != value) { _HospitalBeforeIllPatient = value; RaisePropertyChanged("HospitalBeforeIllPatient"); } } }
            HospitalBeforeIllHospitalName = updatedCase.HospitalBeforeIllHospitalName; // { get { return _HospitalBeforeIllHospitalName; } set { if (_HospitalBeforeIllHospitalName != value) { _HospitalBeforeIllHospitalName = value; RaisePropertyChanged("HospitalBeforeIllHospitalName"); } } }
            HospitalBeforeIllVillage = updatedCase.HospitalBeforeIllVillage; // { get { return _HospitalBeforeIllVillage; } set { if (_HospitalBeforeIllVillage != value) { _HospitalBeforeIllVillage = value; RaisePropertyChanged("HospitalBeforeIllVillage"); } } }
            HospitalBeforeIllDistrict = updatedCase.HospitalBeforeIllDistrict; // { get { return _HospitalBeforeIllDistrict; } set { if (_HospitalBeforeIllDistrict != value) { _HospitalBeforeIllDistrict = value; RaisePropertyChanged("HospitalBeforeIllDistrict"); } } }
            HospitalBeforeIllStartDate = updatedCase.HospitalBeforeIllStartDate; // { get { return _HospitalBeforeIllStartDate; } set { if (_HospitalBeforeIllStartDate != value) { _HospitalBeforeIllStartDate = value; RaisePropertyChanged("HospitalBeforeIllStartDate"); } } }
            HospitalBeforeIllEndDate = updatedCase.HospitalBeforeIllEndDate; // { get { return _HospitalBeforeIllEndDate; } set { if (_HospitalBeforeIllEndDate != value) { _HospitalBeforeIllEndDate = value; RaisePropertyChanged("HospitalBeforeIllEndDate"); } } }
            HospitalBeforeIllDateEstimated = updatedCase.HospitalBeforeIllDateEstimated; // { get { return _HospitalBeforeIllDateEstimated; } set { if (_HospitalBeforeIllDateEstimated != value) { _HospitalBeforeIllDateEstimated = value; RaisePropertyChanged("HospitalBeforeIllDateEstimated"); } } }

            TraditionalHealer = updatedCase.TraditionalHealer; // { get { return _TraditionalHealer; } set { if (_TraditionalHealer != value) { _TraditionalHealer = value; RaisePropertyChanged("TraditionalHealer"); } } }
            TraditionalHealerName = updatedCase.TraditionalHealerName; // { get { return _TraditionalHealerName; } set { if (_TraditionalHealerName != value) { _TraditionalHealerName = value; RaisePropertyChanged("TraditionalHealerName"); } } }
            TraditionalHealerVillage = updatedCase.TraditionalHealerVillage; // { get { return _TraditionalHealerVillage; } set { if (_TraditionalHealerVillage != value) { _TraditionalHealerVillage = value; RaisePropertyChanged("TraditionalHealerVillage"); } } }
            TraditionalHealerDistrict = updatedCase.TraditionalHealerDistrict; // { get { return _TraditionalHealerDistrict; } set { if (_TraditionalHealerDistrict != value) { _TraditionalHealerDistrict = value; RaisePropertyChanged("TraditionalHealerDistrict"); } } }
            TraditionalHealerDate = updatedCase.TraditionalHealerDate; // { get { return _TraditionalHealerDate; } set { if (_TraditionalHealerDate != value) { _TraditionalHealerDate = value; RaisePropertyChanged("TraditionalHealerDate"); } } }
            TraditionalHealerDateEstimated = updatedCase.TraditionalHealerDateEstimated; // { get { return _TraditionalHealerDateEstimated; } set { if (_TraditionalHealerDateEstimated != value) { _TraditionalHealerDateEstimated = value; RaisePropertyChanged("TraditionalHealerDateEstimated"); } } }

            Animals = updatedCase.Animals; // { get { return _Animals; } set { if (_Animals != value) { _Animals = value; RaisePropertyChanged("Animals"); } } }
            AnimalBats = updatedCase.AnimalBats; // { get { return _AnimalBats; } set { if (_AnimalBats != value) { _AnimalBats = value; RaisePropertyChanged("AnimalBats"); } } }
            AnimalPrimates = updatedCase.AnimalPrimates; // { get { return _AnimalPrimates; } set { if (_AnimalPrimates != value) { _AnimalPrimates = value; RaisePropertyChanged("AnimalPrimates"); } } }
            AnimalRodents = updatedCase.AnimalRodents; // { get { return _AnimalRodents; } set { if (_AnimalRodents != value) { _AnimalRodents = value; RaisePropertyChanged("AnimalRodents"); } } }
            AnimalPigs = updatedCase.AnimalPigs; // { get { return _AnimalPigs; } set { if (_AnimalPigs != value) { _AnimalPigs = value; RaisePropertyChanged("AnimalPigs"); } } }
            AnimalBirds = updatedCase.AnimalBirds; // { get { return _AnimalBirds; } set { if (_AnimalBirds != value) { _AnimalBirds = value; RaisePropertyChanged("AnimalBirds"); } } }
            AnimalCows = updatedCase.AnimalCows; // { get { return _AnimalCows; } set { if (_AnimalCows != value) { _AnimalCows = value; RaisePropertyChanged("AnimalCows"); } } }
            AnimalOther = updatedCase.AnimalOther; // { get { return _AnimalOther; } set { if (_AnimalOther != value) { _AnimalOther = value; RaisePropertyChanged("AnimalOther"); } } }

            AnimalBatsStatus = updatedCase.AnimalBatsStatus; // { get { return _AnimalBatsStatus; } set { if (_AnimalBatsStatus != value) { _AnimalBatsStatus = value; RaisePropertyChanged("AnimalBatsStatus"); } } }
            AnimalPrimatesStatus = updatedCase.AnimalPrimatesStatus; // { get { return _AnimalPrimatesStatus; } set { if (_AnimalPrimatesStatus != value) { _AnimalPrimatesStatus = value; RaisePropertyChanged("AnimalPrimatesStatus"); } } }
            AnimalRodentsStatus = updatedCase.AnimalRodentsStatus; // { get { return _AnimalRodentsStatus; } set { if (_AnimalRodentsStatus != value) { _AnimalRodentsStatus = value; RaisePropertyChanged("AnimalRodentsStatus"); } } }
            AnimalPigsStatus = updatedCase.AnimalPigsStatus; // { get { return _AnimalPigsStatus; } set { if (_AnimalPigsStatus != value) { _AnimalPigsStatus = value; RaisePropertyChanged("AnimalPigsStatus"); } } }
            AnimalBirdsStatus = updatedCase.AnimalBirdsStatus; // { get { return _AnimalBirdsStatus; } set { if (_AnimalBirdsStatus != value) { _AnimalBirdsStatus = value; RaisePropertyChanged("AnimalBirdsStatus"); } } }
            AnimalCowsStatus = updatedCase.AnimalCowsStatus; // { get { return _AnimalCowsStatus; } set { if (_AnimalCowsStatus != value) { _AnimalCowsStatus = value; RaisePropertyChanged("AnimalCowsStatus"); } } }
            AnimalOtherStatus = updatedCase.AnimalOtherStatus; // { get { return _AnimalOtherStatus; } set { if (_AnimalOtherStatus != value) { _AnimalOtherStatus = value; RaisePropertyChanged("AnimalOtherStatus"); } } }
            AnimalOtherComment = updatedCase.AnimalOtherComment; // { get { return _AnimalOtherComment; } set { if (_AnimalOtherComment != value) { _AnimalOtherComment = value; RaisePropertyChanged("AnimalOtherComment"); } } }

            BittenTick = updatedCase.BittenTick; // { get { return _BittenTick; } set { if (_BittenTick != value) { _BittenTick = value; RaisePropertyChanged("BittenTick"); } } }

            InterviewerName = updatedCase.InterviewerName; // { get { return _InterviewerName; } set { if (_InterviewerName != value) { _InterviewerName = value; RaisePropertyChanged("InterviewerName"); } } }
            InterviewerPhone = updatedCase.InterviewerPhone; // { get { return _InterviewerPhone; } set { if (_InterviewerPhone != value) { _InterviewerPhone = value; RaisePropertyChanged("InterviewerPhone"); } } }
            InterviewerEmail = updatedCase.InterviewerEmail; // { get { return _InterviewerEmail; } set { if (_InterviewerEmail != value) { _InterviewerEmail = value; RaisePropertyChanged("InterviewerEmail"); } } }
            InterviewerPosition = updatedCase.InterviewerPosition; // { get { return _InterviewerPosition; } set { if (_InterviewerPosition != value) { _InterviewerPosition = value; RaisePropertyChanged("InterviewerPosition"); } } }
            InterviewerDistrict = updatedCase.InterviewerDistrict; // { get { return _InterviewerDistrict; } set { if (_InterviewerDistrict != value) { _InterviewerDistrict = value; RaisePropertyChanged("InterviewerDistrict"); } } }
            InterviewerHealthFacility = updatedCase.InterviewerHealthFacility; // { get { return _InterviewerHealthFacility; } set { if (_InterviewerHealthFacility != value) { _InterviewerHealthFacility = value; RaisePropertyChanged("InterviewerHealthFacility"); } } }
            InterviewerInfoProvidedBy = updatedCase.InterviewerInfoProvidedBy; // { get { return _InterviewerInfoProvidedBy; } set { if (_InterviewerInfoProvidedBy != value) { _InterviewerInfoProvidedBy = value; RaisePropertyChanged("InterviewerInfoProvidedBy"); } } }
            ProxyName = updatedCase.ProxyName; // { get { return _ProxyName; } set { if (_ProxyName != value) { _ProxyName = value; RaisePropertyChanged("ProxyName"); } } }
            ProxyRelation = updatedCase.ProxyRelation; // { get { return _ProxyRelation; } set { if (_ProxyRelation != value) { _ProxyRelation = value; RaisePropertyChanged("ProxyRelation"); } } }

            DOB = updatedCase.DOB;
            Email = updatedCase.Email;
            PhoneNumber = updatedCase.PhoneNumber;
            OccupationHCWPosition = updatedCase.OccupationHCWPosition;
            OccupationHCWFacility = updatedCase.OccupationHCWFacility;
            OccupationHCWDistrict = updatedCase.OccupationHCWDistrict;
            OccupationHCWSC = updatedCase.OccupationHCWSC;
            OccupationHCWVillage = updatedCase.OccupationHCWVillage;

            CommentsOnThisPatient = updatedCase.CommentsOnThisPatient;
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (!IsLoading && !IsLocked && (!IsNewRecord || !SuppressCriticalErrors))
            {
                if (e.PropertyName != "HasUnsavedChanges" &&
                    e.PropertyName != "NeedsCollectionDeletion" &&
                    e.PropertyName != "IsActive" &&
                    e.PropertyName != "IsContact" &&
                    e.PropertyName != "IsEditing" &&
                    e.PropertyName != "IsSaving" &&
                    e.PropertyName != "IsLoading" &&
                    e.PropertyName != "IsLocked" &&
                    e.PropertyName != "IsNewRecord" &&
                    e.PropertyName != "IsValidating" &&
                    e.PropertyName != "IsOtherOccupation" &&
                    e.PropertyName != "IsShowingErrorDetailPanel" &&
                    e.PropertyName != "IsShowingBleedingPanel" &&
                    e.PropertyName != "IsShowingErrorDetailPanel" &&
                    e.PropertyName != "IsShowingFieldValueChangesPanel" &&
                    e.PropertyName != "IsShowingLabResultPanel" &&
                    e.PropertyName != "IsActive" &&
                    e.PropertyName != "LabResultsView" &&
                    e.PropertyName != "SuppressValidation" &&
                    e.PropertyName != "SuppressCriticalErrors" &&
                    e.PropertyName != "IsInvalidId" &&
                    e.PropertyName != "FieldValueChanges" &&
                    e.PropertyName != "IsContact")
                {
                    HasUnsavedChanges = true;

                    if (_localCopy != null &&
                        e.PropertyName != "AgeUnitString" &&
                        e.PropertyName != "AgeYears" &&
                        e.PropertyName != "EpiCaseClassification" &&
                        e.PropertyName != "FinalCaseStatus")
                    {
                        Core.FieldValueChange fieldValueChange = new FieldValueChange();
                        fieldValueChange.FieldName = e.PropertyName;

                        object origValue = _localCopy.GetType().GetProperty(e.PropertyName).GetValue(_localCopy);
                        object newValue = this.GetType().GetProperty(e.PropertyName).GetValue(this);

                        if (origValue != null)
                        {
                            fieldValueChange.OriginalValue = origValue.ToString();
                        }
                        else
                        {
                            fieldValueChange.OriginalValue = String.Empty;
                        }

                        if (newValue != null)
                        {
                            fieldValueChange.NewValue = newValue.ToString();
                        }
                        else
                        {
                            fieldValueChange.NewValue = String.Empty;
                        }

                        FieldValueChange changeToRemove = null;
                        foreach (FieldValueChange change in FieldValueChanges)
                        {
                            if (change.FieldName.Equals(e.PropertyName))
                            {
                                changeToRemove = change;
                                break;
                            }
                        }

                        if (changeToRemove != null)
                        {
                            FieldValueChanges.Remove(changeToRemove);
                        }

                        if (fieldValueChange.NewValue != fieldValueChange.OriginalValue)
                        {
                            FieldValueChanges.Add(fieldValueChange);
                        }
                    }
                    if (!e.PropertyName.Equals("Age") && !e.PropertyName.Equals("AgeYears") && !e.PropertyName.Equals("RecordStatus"))
                    //&&
                    //    !(e.PropertyName.Equals("RecordStatusComplete") || e.PropertyName.Equals("RecordStatusMissCRF") ||
                    //    e.PropertyName.Equals("RecordStatusNoCRF") || e.PropertyName.Equals("RecordStatusPenLab") ||
                    //e.PropertyName.Equals("RecordStatusPenOut")))
                    {
                        Validate();
                    }
                }
            }
        }

        private void CalcAge()
        {
            if (_age.HasValue == false)
            {
                //AgeUnit = null;
            }
            if (Age.HasValue)
            {
                if (AgeUnit == AgeUnits.Years)
                {
                    AgeYears = Math.Round(this.Age.Value, 2);
                }
                else if (AgeUnit == AgeUnits.Months && Age.HasValue)
                {
                    double newAge = (Age.Value / 12);
                    AgeYears = Math.Round(newAge, 2);
                }
                //else if (AgeUnit == null) { throw new ApplicationException("Age unit cannot be null in Age setter."); } // form updated to force AgeUnit if Age has a value, so this is no longer needed
                else if (IsCountryUS)//17224
                {
                    AgeYears = this.Age;
                }
                else
                {
                    AgeYears = null;
                }
            }
        }

        private bool CanExecuteSwitchToLegacyEnterCommand()
        {
            if (this.HasErrors && this.IsOpenedInSuperUserMode && !this.IsNewRecord)
            {
                return true;
            }
            else
            {
                //bypasspage validation when record is new and there is no change. 
                //Issue#17054
                if (IsNewRecord && !this.IsChanged)
                {
                    ByPassEpiCaseClassificationValidation = true;
                }
                else
                {
                    ByPassEpiCaseClassificationValidation = false;
                }
                return CanExecuteSaveCommand();
            }
        }

        private bool CanExecuteSaveCommand()
        {
            if (this.HasErrors || this.IsSaving || this.IsLoading || this.IsLocked)
            {
                return false;
            }
            else if (String.IsNullOrEmpty(EpiCaseClassification) && !ByPassEpiCaseClassificationValidation)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        #endregion // Methods

        #region Commands


        public ICommand AddLabResultCommand { get { return new RelayCommand(AddLabResultCommandExecute); } }
        private void AddLabResultCommandExecute()
        {
            LabResultViewModel result = new LabResultViewModel(LabForm);
            result.RecordId = System.Guid.NewGuid().ToString();
            result.CaseVM = this;
            result.SampleNumber = SampleLabel + " " + (LabResults.Count + 1).ToString();
            result.IsNewRecord = true;
            LabResults.Add(result);
            LabResultsView.MoveCurrentTo(result);
            LabResultsView.Refresh();
            RaisePropertyChanged("LabResultsView");
            IsShowingLabResultPanel = true;
            result.IsEditing = true;
        }

        public ICommand LabResultSelectedCommand { get { return new RelayCommand<LabResultViewModel>(LabResultSelectedCommandExecute); } }
        private void LabResultSelectedCommandExecute(LabResultViewModel result)
        {
            LabResultsView.MoveCurrentTo(result);
            result.Load();

            RaisePropertyChanged("LabResultsView");
            IsShowingLabResultPanel = true;
            result.IsEditing = true;
        }

        public ICommand LabResultClosedCommand { get { return new RelayCommand<LabResultViewModel>(LabResultClosedCommandExecute); } }
        private void LabResultClosedCommandExecute(LabResultViewModel result)
        {
            RefreshLastSampleFields();
            LabResultsView.MoveCurrentTo(null);
            IsShowingLabResultPanel = false;
            result.IsEditing = false;
        }


        public ICommand AddSourceCaseCommand { get { return new RelayCommand(AddSourceCaseCommandExecute); } }
        private void AddSourceCaseCommandExecute()
        {
            SourceCaseInfoViewModel sourceCase = new SourceCaseInfoViewModel();
            SourceCases.Add(sourceCase);
            //CaseViewModel sourceCase = new CaseViewModel(CaseForm, LabForm);
            //SourceCases.Add(sourceCase);
            SourceCasesView.MoveCurrentTo(sourceCase);
            RaisePropertyChanged("SourceCasesView");
            IsShowingSourceCasesPanel = true;
        }

        public ICommand SourceCaseSelectedCommand { get { return new RelayCommand<SourceCaseInfoViewModel>(SourceCaseSelectedCommandExecute); } }
        private void SourceCaseSelectedCommandExecute(SourceCaseInfoViewModel sourceCase)
        {
            SourceCasesView.MoveCurrentTo(sourceCase);
            //result.Load();
            ////SourceCasesView.Refresh();
            RaisePropertyChanged("SourceCasesView");
            IsShowingSourceCasesPanel = true;
        }

        public ICommand SourceCaseClosedCommand { get { return new RelayCommand<SourceCaseInfoViewModel>(SourceCaseClosedCommandExecute); } }
        private void SourceCaseClosedCommandExecute(SourceCaseInfoViewModel sourceCase)
        {
            //RefreshLastSampleFields();
            SourceCasesView.MoveCurrentTo(null);
            IsShowingSourceCasesPanel = false;
        }




        public ICommand ToggleValueChangesDisplayCommand { get { return new RelayCommand(ToggleValueChangesDisplayCommandExecute); } }
        protected void ToggleValueChangesDisplayCommandExecute()
        {
            IsShowingFieldValueChangesPanel = !IsShowingFieldValueChangesPanel;
        }

        public ICommand ToggleBleedingDisplayCommand { get { return new RelayCommand(ToggleBleedingDisplayCommandExecute); } }
        protected void ToggleBleedingDisplayCommandExecute()
        {
            IsShowingBleedingPanel = !IsShowingBleedingPanel;
        }

        public ICommand ToggleOccupationsDisplayCommand { get { return new RelayCommand(ToggleOccupationsDisplayCommandExecute); } }
        protected void ToggleOccupationsDisplayCommandExecute()
        {
            IsShowingOccupationPanel = !IsShowingOccupationPanel;
        }

        public ICommand ToggleErrorDisplayCommand { get { return new RelayCommand(ToggleErrorDisplayCommandExecute); } }
        protected void ToggleErrorDisplayCommandExecute()
        {
            IsShowingErrorDetailPanel = !IsShowingErrorDetailPanel;
        }

        public ICommand CancelEditModeCommand { get { return new RelayCommand(CancelEditModeCommandExecute); } }
        protected void CancelEditModeCommandExecute()
        {
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

        public ICommand SwitchToLegacyEnterCommand { get { return new RelayCommand(SwitchToLegacyEnterCommandExecute, CanExecuteSwitchToLegacyEnterCommand); } }
        protected void SwitchToLegacyEnterCommandExecute()
        {
            if (IsNewRecord)
            {
                if (SaveCommand.CanExecute(null))
                {
                    if (IsChanged) //Save only when there is change on the shortform.Issue#17054
                    {
                        Save();

                        _localCopy = new CaseViewModel(this);
                        FieldValueChanges.Clear();

                        IsEditing = false;
                        Load();
                    }
                    else
                    {
                        IsEditing = false; //causes close of short case form.
                    }

                    if (SwitchToLegacyEnter != null)
                    {
                        SwitchToLegacyEnter(this, new EventArgs());
                    }
                }
            }
            else
            {
                if (SwitchToLegacyEnterCommand.CanExecute(null))
                {
                    Save();

                    _localCopy = new CaseViewModel(this);
                    FieldValueChanges.Clear();

                    IsEditing = false;
                    Load();

                    if (SwitchToLegacyEnter != null)
                    {
                        SwitchToLegacyEnter(this, new EventArgs());
                    }
                }
            }
        }

        public ICommand SaveCommand { get { return new RelayCommand(SaveCommandExecute, CanExecuteSaveCommand); } }
        protected void SaveCommandExecute()
        {
            SuppressCriticalErrors = false;
            Validate();

            if (SaveCommand.CanExecute(null))
            {
                //Save();
                SaveAsync();
            }
        }

        public ICommand SaveAndCloseCommand { get { return new RelayCommand(SaveAndCloseCommandExecute, CanExecuteSaveCommand); } }
        protected void SaveAndCloseCommandExecute()
        {
            SuppressCriticalErrors = false;
            Validate();
            if (SaveCommand.CanExecute(null))
            {
                Save();
                IsEditing = false;
                _localCopy = new CaseViewModel(this);
                FieldValueChanges.Clear();
            }
        }
        #endregion // Commands
    }
}
