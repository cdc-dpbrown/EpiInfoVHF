using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Epi;
using Epi.Fields;
using Epi.Data;
using ContactTracing.ViewModel;
using ContactTracing.Core;
using System.Collections.ObjectModel;
using Epi.DataSets;

namespace ContactTracing.CaseView.Controls.Diagnostics
{
    /// <summary>
    /// Interaction logic for DistrictFieldTypeEditor.xaml
    /// </summary>
    public partial class DistrictFieldTypeEditor : UserControl
    {
        public EpiDataHelper DataHelper
        {
            get
            {
                return this.DataContext as EpiDataHelper;
            }
        }

        public DistrictFieldTypeEditor()
        {
            InitializeComponent();
        }

        string fieldName = "";
        string tableName = "";
        private IDbDriver Database { get; set; }
        DataTable listboxFieldItemSource = null;
        List<string> CommentLegalFieldsList = new List<string>(); //Temp storage for Comment Legal fields
        List<string> LegalFieldsList = new List<string>(); //Temp storage for legal fields.
        public void Init()
        {
            RenderableField districtField = DataHelper.CaseForm.Fields["DistrictRes"] as RenderableField;
            RenderableField scField = DataHelper.CaseForm.Fields["SCRes"] as RenderableField;
            RenderableField countryField = DataHelper.CaseForm.Fields["CountryRes"] as RenderableField;
            CreateTables();

            SelectCheckboxes();

            PopulateListboxes();

            if (districtField != null && scField != null && countryField != null)
            {
                if (districtField is DDLFieldOfCodes || districtField is DDLFieldOfLegalValues)
                {
                    checkboxDistrictDDL.IsChecked = true;
                    checkboxDistrictText.IsChecked = false;
                }
                else
                {
                    checkboxDistrictDDL.IsChecked = false;
                    checkboxDistrictText.IsChecked = true;
                }

                if (scField is DDLFieldOfLegalValues)
                {
                    checkboxSCDDL.IsChecked = true;
                    checkboxSCText.IsChecked = false;
                }
                else
                {
                    checkboxSCDDL.IsChecked = false;
                    checkboxSCText.IsChecked = true;
                }

                if (countryField is DDLFieldOfCommentLegal || countryField is DDLFieldOfLegalValues)
                {
                    checkboxCountryDDL.IsChecked = true;
                    checkboxCountryText.IsChecked = false;
                }
                else
                {
                    checkboxCountryDDL.IsChecked = false;
                    checkboxCountryText.IsChecked = true;
                }
            }
            else
            {
                throw new InvalidOperationException("Fields are missing on the form.");
            }
        }

        private void PopulateListboxes()
        {
            TableSchema.TablesDataTable codeTables = Database.GetCodeTableList(Database);
            foreach (var table in codeTables)
            {
                listboxTable.Items.Add(table.TABLE_NAME);
            }
        }

        private void SelectCheckboxes()
        {
            #region HCWPosition
            RenderableField hcwPosition = DataHelper.CaseForm.Fields["HCWPosition"] as RenderableField;

            if (hcwPosition is DDLFieldOfCommentLegal || hcwPosition is DDLFieldOfLegalValues)
            {
                checkboxHCWpositionDDL.IsChecked = true;
                checkboxHCWpositionText.IsChecked = false;
                btnHCWposition.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                checkboxHCWpositionDDL.IsChecked = false;
                checkboxHCWpositionText.IsChecked = true;
                btnHCWposition.Visibility = System.Windows.Visibility.Collapsed;
            }
            #endregion

            #region HCWFacility
            RenderableField hcwFacility = DataHelper.CaseForm.Fields["HCWFacility"] as RenderableField;

            if (hcwFacility is DDLFieldOfCommentLegal || hcwFacility is DDLFieldOfLegalValues)
            {
                checkboxHCWFacilityDDL.IsChecked = true;
                checkboxHCWFacilityText.IsChecked = false;
                btnHCWFacility.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                checkboxHCWFacilityDDL.IsChecked = false;
                checkboxHCWFacilityText.IsChecked = true;
                btnHCWFacility.Visibility = System.Windows.Visibility.Collapsed;
            }

            #endregion

            #region CurrentHospital
            RenderableField currentHospital = DataHelper.CaseForm.Fields["HospitalCurrent"] as RenderableField;

            if (currentHospital is DDLFieldOfCommentLegal || currentHospital is DDLFieldOfLegalValues)
            {
                checkboxHospitalCurrentDDL.IsChecked = true;
                checkboxHospitalCurrentText.IsChecked = false;
                btnHospitalCurrent.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                checkboxHospitalCurrentDDL.IsChecked = false;
                checkboxHospitalCurrentText.IsChecked = true;
                btnHospitalCurrent.Visibility = System.Windows.Visibility.Collapsed;
            }

            #endregion


            #region TransportType
            RenderableField transportType = DataHelper.CaseForm.Fields["TransporterType"] as RenderableField;

            if (transportType is DDLFieldOfCommentLegal || transportType is DDLFieldOfLegalValues)
            {
                checkboxTransporterTypeDDL.IsChecked = true;
                checkboxTransporterTypeText.IsChecked = false;
                btnTransporterType.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                checkboxTransporterTypeDDL.IsChecked = false;
                checkboxTransporterTypeText.IsChecked = true;
                btnTransporterType.Visibility = System.Windows.Visibility.Collapsed;
            }
            #endregion

            #region Hospital Past
            RenderableField hospitalpast1 = DataHelper.CaseForm.Fields["HospitalPast1"] as RenderableField;

            if (hospitalpast1 is DDLFieldOfCommentLegal || hospitalpast1 is DDLFieldOfLegalValues)
            {
                checkboxHospitalPastDDL.IsChecked = true;
                checkboxHospitalPastText.IsChecked = false;
                btnHospitalPast.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                checkboxHospitalPastDDL.IsChecked = false;
                checkboxHospitalPastText.IsChecked = true;
                btnHospitalPast.Visibility = System.Windows.Visibility.Collapsed;
            }
            #endregion

            #region OtherOccupDetail
            RenderableField OtherOccupDetail = DataHelper.CaseForm.Fields["OtherOccupDetail"] as RenderableField;

            if (OtherOccupDetail is DDLFieldOfCommentLegal || OtherOccupDetail is DDLFieldOfLegalValues)
            {
                checkboxOtherOccupDetailDDL.IsChecked = true;
                checkboxOtherOccupDetailText.IsChecked = false;
                btnOtherOccupDetail.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                checkboxOtherOccupDetailDDL.IsChecked = false;
                checkboxOtherOccupDetailText.IsChecked = true;
                btnOtherOccupDetail.Visibility = System.Windows.Visibility.Collapsed;
            }
            #endregion

            #region BusinessType
            RenderableField BusinessType = DataHelper.CaseForm.Fields["BusinessType"] as RenderableField;

            if (BusinessType is DDLFieldOfCommentLegal || BusinessType is DDLFieldOfLegalValues)
            {
                checkboxBusinessTypeDDL.IsChecked = true;
                checkboxBusinessTypeText.IsChecked = false;
                btnBusinessType.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                checkboxBusinessTypeDDL.IsChecked = false;
                checkboxBusinessTypeText.IsChecked = true;
                btnBusinessType.Visibility = System.Windows.Visibility.Collapsed;
            }
            #endregion

            #region BleedOtherComment
            RenderableField BleedOtherComment = DataHelper.CaseForm.Fields["BleedOtherComment"] as RenderableField;

            if (BleedOtherComment is DDLFieldOfCommentLegal || BleedOtherComment is DDLFieldOfLegalValues)
            {
                checkboxBleedOtherCommentDDL.IsChecked = true;
                checkboxBleedOtherCommentText.IsChecked = false;
                btnBleedOtherComment.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                checkboxBleedOtherCommentDDL.IsChecked = false;
                checkboxBleedOtherCommentText.IsChecked = true;
                btnBleedOtherComment.Visibility = System.Windows.Visibility.Collapsed;
            }
            #endregion
            //#region OtherHemoFinalSpecify
            //RenderableField OtherHemoFinalSpecify = DataHelper.CaseForm.Fields["OtherHemoFinalSpecify"] as RenderableField;

            ////if (OtherHemoFinalSpecify is DDLFieldOfCommentLegal || OtherHemoFinalSpecify is DDLFieldOfLegalValues)
            ////{
            ////    checkboxOtherHemoFinalSpecifyDDL.IsChecked = true;
            ////    checkboxOtherHemoFinalSpecifyText.IsChecked = false;
            ////    btnOtherHemoFinalSpecify.Visibility = System.Windows.Visibility.Visible;
            ////}
            ////else
            ////{
            ////    checkboxOtherHemoFinalSpecifyText.IsChecked = true;
            ////    checkboxOtherHemoFinalSpecifyDDL.IsChecked = false;
            ////    btnOtherHemoFinalSpecify.Visibility = System.Windows.Visibility.Collapsed;
            ////}
            //#endregion

            #region SymptOtherComment
            RenderableField SymptOtherComment = DataHelper.CaseForm.Fields["SymptOtherComment"] as RenderableField;

            if (SymptOtherComment is DDLFieldOfCommentLegal || SymptOtherComment is DDLFieldOfLegalValues)
            {
                checkboxSymptOtherCommentDDL.IsChecked = true;
                checkboxSymptOtherCommentText.IsChecked = false;
                btnSymptOtherComment.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                checkboxSymptOtherCommentDDL.IsChecked = false;
                checkboxSymptOtherCommentText.IsChecked = true;
                btnSymptOtherComment.Visibility = System.Windows.Visibility.Collapsed;
            }
            #endregion

            #region Contact Relation
            RenderableField ContactRelation1 = DataHelper.CaseForm.Fields["ContactRelation1"] as RenderableField;

            if (ContactRelation1 is DDLFieldOfCommentLegal || ContactRelation1 is DDLFieldOfLegalValues)
            {
                checkboxContactRelationDDL.IsChecked = true;
                checkboxContactRelationText.IsChecked = false;
                btnContactRelation.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                checkboxContactRelationDDL.IsChecked = false;
                checkboxContactRelationText.IsChecked = true;
                btnContactRelation.Visibility = System.Windows.Visibility.Collapsed;
            }
            #endregion

            #region Hospital Past
            RenderableField FuneralRelation1 = DataHelper.CaseForm.Fields["FuneralRelation1"] as RenderableField;

            if (FuneralRelation1 is DDLFieldOfCommentLegal || FuneralRelation1 is DDLFieldOfLegalValues)
            {
                checkboxFuneralRelationDDL.IsChecked = true;
                checkboxFuneralRelationText.IsChecked = false;
                btnFuneralRelation.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                checkboxFuneralRelationDDL.IsChecked = false;
                checkboxFuneralRelationText.IsChecked = true;
                btnFuneralRelation.Visibility = System.Windows.Visibility.Collapsed;
            }
            #endregion

            #region Hospital Before Ill Name
            RenderableField HospitalBeforeIllName = DataHelper.CaseForm.Fields["HospitalBeforeIllName"] as RenderableField;

            if (HospitalBeforeIllName is DDLFieldOfCommentLegal || HospitalBeforeIllName is DDLFieldOfLegalValues)
            {
                checkboxHospitalBeforeIllNameDDL.IsChecked = true;
                checkboxHospitalBeforeIllNameText.IsChecked = false;
                btnHospitalBeforeIllName.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                checkboxHospitalBeforeIllNameDDL.IsChecked = false;
                checkboxHospitalBeforeIllNameText.IsChecked = true;
                btnHospitalBeforeIllName.Visibility = System.Windows.Visibility.Collapsed;
            }
            #endregion
            #region Interviewer Position
            RenderableField InterviewerPosition = DataHelper.CaseForm.Fields["InterviewerPosition"] as RenderableField;

            if (InterviewerPosition is DDLFieldOfCommentLegal || InterviewerPosition is DDLFieldOfLegalValues)
            {
                checkboxInterviewerPositionDDL.IsChecked = true;
                checkboxInterviewerPositionText.IsChecked = false;
                btnInterviewerPosition.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                checkboxInterviewerPositionDDL.IsChecked = false;
                checkboxInterviewerPositionText.IsChecked = true;
                btnInterviewerPosition.Visibility = System.Windows.Visibility.Collapsed;
            }
            #endregion

            #region Interviewer Health Facility
            RenderableField InterviewerHealthFacility = DataHelper.CaseForm.Fields["InterviewerHealthFacility"] as RenderableField;

            if (InterviewerHealthFacility is DDLFieldOfCommentLegal || InterviewerHealthFacility is DDLFieldOfLegalValues)
            {
                checkboxInterviewerHealthFacilityDDL.IsChecked = true;
                checkboxInterviewerHealthFacilityText.IsChecked = false;
                btnInterviewerHealthFacility.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                checkboxInterviewerHealthFacilityDDL.IsChecked = false;
                checkboxInterviewerHealthFacilityText.IsChecked = true;
                btnInterviewerHealthFacility.Visibility = System.Windows.Visibility.Collapsed;
            }
            #endregion

            #region Proxy Relation
            RenderableField ProxyRelation = DataHelper.CaseForm.Fields["ProxyRelation"] as RenderableField;

            if (ProxyRelation is DDLFieldOfCommentLegal || ProxyRelation is DDLFieldOfLegalValues)
            {
                checkboxProxyRelationDDL.IsChecked = true;
                checkboxProxyRelationText.IsChecked = false;
                btnProxyRelation.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                checkboxProxyRelationDDL.IsChecked = false;
                checkboxProxyRelationText.IsChecked = true;
                btnProxyRelation.Visibility = System.Windows.Visibility.Collapsed;
            }
            #endregion

            #region Specify Bleeding
            RenderableField SpecifyBleeding = DataHelper.CaseForm.Fields["SpecifyBleeding"] as RenderableField;

            if (SpecifyBleeding is DDLFieldOfCommentLegal || SpecifyBleeding is DDLFieldOfLegalValues)
            {
                checkboxSpecifyBleedingDDL.IsChecked = true;
                checkboxSpecifyBleedingText.IsChecked = false;
                btnSpecifyBleeding.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                checkboxSpecifyBleedingDDL.IsChecked = false;
                checkboxSpecifyBleedingText.IsChecked = true;
                btnSpecifyBleeding.Visibility = System.Windows.Visibility.Collapsed;
            }
            #endregion

            #region Hospital Discharge
            RenderableField HospitalDischarge = DataHelper.CaseForm.Fields["HospitalDischarge"] as RenderableField;

            if (HospitalDischarge is DDLFieldOfCommentLegal || HospitalDischarge is DDLFieldOfLegalValues)
            {
                checkboxHospitalDischargeDDL.IsChecked = true;
                checkboxHospitalDischargeText.IsChecked = false;
                btnHospitalDischarge.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                checkboxHospitalDischargeDDL.IsChecked = false;
                checkboxHospitalDischargeText.IsChecked = true;
                btnHospitalDischarge.Visibility = System.Windows.Visibility.Collapsed;
            }
            #endregion

            #region Place Death Other
            RenderableField PlaceDeathOther = DataHelper.CaseForm.Fields["PlaceDeathOther"] as RenderableField;

            if (PlaceDeathOther is DDLFieldOfCommentLegal || PlaceDeathOther is DDLFieldOfLegalValues)
            {
                checkboxPlaceDeathOtherDDL.IsChecked = true;
                checkboxPlaceDeathOtherText.IsChecked = false;
                btnPlaceDeathOther.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                checkboxPlaceDeathOtherText.IsChecked = true;
                checkboxPlaceDeathOtherDDL.IsChecked = false;
                btnPlaceDeathOther.Visibility = System.Windows.Visibility.Collapsed;
            }
            #endregion



            #region Hospital Death
            RenderableField HospitalDeath = DataHelper.CaseForm.Fields["HospitalDeath"] as RenderableField;

            if (HospitalDeath is DDLFieldOfCommentLegal || HospitalDeath is DDLFieldOfLegalValues)
            {
                checkboxHospitalDeathDDL.IsChecked = true;
                checkboxHospitalDeathText.IsChecked = false;
                btnHospitalDeath.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                checkboxHospitalDeathText.IsChecked = true;
                checkboxHospitalDeathDDL.IsChecked = false;
                btnHospitalDeath.Visibility = System.Windows.Visibility.Collapsed;
            }
            #endregion

            #region Contact HCW Facility
            RenderableField ContactHCWFacility = DataHelper.ContactForm.Fields["ContactHCWFacility"] as RenderableField;

            if (ContactHCWFacility is DDLFieldOfCommentLegal || ContactHCWFacility is DDLFieldOfLegalValues)
            {
                checkboxContactHCWFacilityDDL.IsChecked = true;
                checkboxContactHCWFacilityText.IsChecked = false;
                btnContactHCWFacility.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                checkboxContactHCWFacilityText.IsChecked = true;
                checkboxContactHCWFacilityDDL.IsChecked = false;
                btnContactHCWFacility.Visibility = System.Windows.Visibility.Collapsed;
            }
            #endregion

            #region Team

            if (DataHelper.ContactForm.Fields.Exists("Team"))
            {
                RenderableField Team = DataHelper.ContactForm.Fields["Team"] as RenderableField;

                if (Team is DDLFieldOfCommentLegal || Team is DDLFieldOfLegalValues)
                {
                    checkboxTeamDDL.IsChecked = true;
                    checkboxTeamText.IsChecked = false;
                    btnTeam.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    checkboxTeamText.IsChecked = true;
                    checkboxTeamDDL.IsChecked = false;
                    btnTeam.Visibility = System.Windows.Visibility.Collapsed;
                }
                // pnlTeam.Visibility = System.Windows.Visibility.Visible;
                txtBlckTeam.Visibility = System.Windows.Visibility.Visible;
                checkboxTeamText.Visibility = System.Windows.Visibility.Visible;
                checkboxTeamDDL.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                //pnlTeam.Visibility = System.Windows.Visibility.Collapsed;
                txtBlckTeam.Visibility = System.Windows.Visibility.Collapsed;
                checkboxTeamText.Visibility = System.Windows.Visibility.Collapsed;
                checkboxTeamDDL.Visibility = System.Windows.Visibility.Collapsed;
                btnTeam.Visibility = System.Windows.Visibility.Collapsed;
            }


            #endregion
        }

        private void CreateTables()
        {
            Database = DataHelper.Database;

            IDbDriver db = DataHelper.Database;
            if (!db.TableExists("codehcwposition1"))
            {
                List<Epi.Data.TableColumn> tableColumns = new List<Epi.Data.TableColumn>();
                tableColumns.Add(new Epi.Data.TableColumn("hcwposition", GenericDbColumnType.String, 255, true, false));
                db.CreateTable("codehcwposition1", tableColumns);
            }

            if (!db.TableExists("codehcwfacility1"))
            {
                List<Epi.Data.TableColumn> tableColumns = new List<Epi.Data.TableColumn>();
                tableColumns.Add(new Epi.Data.TableColumn("hcwfacility", GenericDbColumnType.String, 255, true, false));
                db.CreateTable("codehcwfacility1", tableColumns);
            }

            if (!db.TableExists("codehospitalcurrent1"))
            {
                List<Epi.Data.TableColumn> tableColumns = new List<Epi.Data.TableColumn>();
                tableColumns.Add(new Epi.Data.TableColumn("hospitalcurrent", GenericDbColumnType.String, 255, true, false));
                db.CreateTable("codehospitalcurrent1", tableColumns);
            }

            if (!db.TableExists("codeCountryList"))
            {
                List<Epi.Data.TableColumn> tableColumns = new List<Epi.Data.TableColumn>();
                tableColumns.Add(new Epi.Data.TableColumn("COUNTRY", GenericDbColumnType.String, 255, true, false));
                db.CreateTable("codeCountryList", tableColumns);
            }

            if (!db.TableExists("codetransportertype1"))
            {
                List<Epi.Data.TableColumn> tableColumns = new List<Epi.Data.TableColumn>();
                tableColumns.Add(new Epi.Data.TableColumn("TransporterType", GenericDbColumnType.String, 255, true, false));
                db.CreateTable("codetransportertype1", tableColumns);
            }

            if (!db.TableExists("codehospitalpast1"))
            {
                List<Epi.Data.TableColumn> tableColumns = new List<Epi.Data.TableColumn>();
                tableColumns.Add(new Epi.Data.TableColumn("HospitalPast", GenericDbColumnType.String, 255, true, false));
                db.CreateTable("codehospitalpast1", tableColumns);
            }

            if (!db.TableExists("codeotheroccupdetail1"))
            {
                List<Epi.Data.TableColumn> tableColumns = new List<Epi.Data.TableColumn>();
                tableColumns.Add(new Epi.Data.TableColumn("OtherOccupDetail", GenericDbColumnType.String, 255, true, false));
                db.CreateTable("codeotheroccupdetail1", tableColumns);
            }

            if (!db.TableExists("codebusinesstype1"))
            {
                List<Epi.Data.TableColumn> tableColumns = new List<Epi.Data.TableColumn>();
                tableColumns.Add(new Epi.Data.TableColumn("BusinessType", GenericDbColumnType.String, 255, true, false));
                db.CreateTable("codebusinesstype1", tableColumns);
            }

            if (!db.TableExists("codebleedothercomment1"))
            {
                List<Epi.Data.TableColumn> tableColumns = new List<Epi.Data.TableColumn>();
                tableColumns.Add(new Epi.Data.TableColumn("BleedOtherComment", GenericDbColumnType.String, 255, true, false));
                db.CreateTable("codebleedothercomment1", tableColumns);
            }

            if (!db.TableExists("codesymptothercomment1"))
            {
                List<Epi.Data.TableColumn> tableColumns = new List<Epi.Data.TableColumn>();
                tableColumns.Add(new Epi.Data.TableColumn("SymptOtherComment", GenericDbColumnType.String, 255, true, false));
                db.CreateTable("codesymptothercomment1", tableColumns);
            }
            if (!db.TableExists("codecontactrelation1"))
            {
                List<Epi.Data.TableColumn> tableColumns = new List<Epi.Data.TableColumn>();
                tableColumns.Add(new Epi.Data.TableColumn("ContactRelation", GenericDbColumnType.String, 255, true, false));
                db.CreateTable("codecontactrelation1", tableColumns);
            }
            if (!db.TableExists("codefuneralrelation1"))
            {
                List<Epi.Data.TableColumn> tableColumns = new List<Epi.Data.TableColumn>();
                tableColumns.Add(new Epi.Data.TableColumn("FuneralRelation", GenericDbColumnType.String, 255, true, false));
                db.CreateTable("codefuneralrelation1", tableColumns);
            }
            if (!db.TableExists("codehospitalbeforeillname1"))
            {
                List<Epi.Data.TableColumn> tableColumns = new List<Epi.Data.TableColumn>();
                tableColumns.Add(new Epi.Data.TableColumn("HospitalBeforeIllName", GenericDbColumnType.String, 255, true, false));
                db.CreateTable("codehospitalbeforeillname1", tableColumns);
            }
            if (!db.TableExists("codeinterviewerposition1"))
            {
                List<Epi.Data.TableColumn> tableColumns = new List<Epi.Data.TableColumn>();
                tableColumns.Add(new Epi.Data.TableColumn("InterviewerPosition", GenericDbColumnType.String, 255, true, false));
                db.CreateTable("codeinterviewerposition1", tableColumns);
            }
            if (!db.TableExists("codeinterviewerhealthfacility1"))
            {
                List<Epi.Data.TableColumn> tableColumns = new List<Epi.Data.TableColumn>();
                tableColumns.Add(new Epi.Data.TableColumn("InterviewerHealthFacility", GenericDbColumnType.String, 255, true, false));
                db.CreateTable("codeinterviewerhealthfacility1", tableColumns);
            }
            if (!db.TableExists("codeproxyrelation1"))
            {
                List<Epi.Data.TableColumn> tableColumns = new List<Epi.Data.TableColumn>();
                tableColumns.Add(new Epi.Data.TableColumn("ProxyRelation", GenericDbColumnType.String, 255, true, false));
                db.CreateTable("codeproxyrelation1", tableColumns);
            }
            if (!db.TableExists("codespecifybleeding1"))
            {
                List<Epi.Data.TableColumn> tableColumns = new List<Epi.Data.TableColumn>();
                tableColumns.Add(new Epi.Data.TableColumn("SpecifyBleeding", GenericDbColumnType.String, 255, true, false));
                db.CreateTable("codespecifybleeding1", tableColumns);
            }

            if (!db.TableExists("codehospitaldischarge1"))
            {
                List<Epi.Data.TableColumn> tableColumns = new List<Epi.Data.TableColumn>();
                tableColumns.Add(new Epi.Data.TableColumn("HospitalDischarge", GenericDbColumnType.String, 255, true, false));
                db.CreateTable("codehospitaldischarge1", tableColumns);
            }
            if (!db.TableExists("codehospitaldeath1"))
            {
                List<Epi.Data.TableColumn> tableColumns = new List<Epi.Data.TableColumn>();
                tableColumns.Add(new Epi.Data.TableColumn("HospitalDeath", GenericDbColumnType.String, 255, true, false));
                db.CreateTable("codehospitaldeath1", tableColumns);
            }
            if (!db.TableExists("codeplacedeathother1"))
            {
                List<Epi.Data.TableColumn> tableColumns = new List<Epi.Data.TableColumn>();
                tableColumns.Add(new Epi.Data.TableColumn("PlaceDeathOther", GenericDbColumnType.String, 255, true, false));
                db.CreateTable("codeplacedeathother1", tableColumns);
            }
            //if (!db.TableExists("codeotherhemofinalspecify1"))
            //{
            //    List<Epi.Data.TableColumn> tableColumns = new List<Epi.Data.TableColumn>();
            //    tableColumns.Add(new Epi.Data.TableColumn("OtherHemoFinalSpecify", GenericDbColumnType.String, 255, true, false));
            //    db.CreateTable("codeotherhemofinalspecify1", tableColumns);
            //}
            if (!db.TableExists("codecontacthcwfacility1"))
            {
                List<Epi.Data.TableColumn> tableColumns = new List<Epi.Data.TableColumn>();
                tableColumns.Add(new Epi.Data.TableColumn("ContactHCWFacility", GenericDbColumnType.String, 255, true, false));
                db.CreateTable("codecontacthcwfacility1", tableColumns);
            }
            if (!db.TableExists("codeteam1"))
            {
                List<Epi.Data.TableColumn> tableColumns = new List<Epi.Data.TableColumn>();
                tableColumns.Add(new Epi.Data.TableColumn("Team", GenericDbColumnType.String, 255, true, false));
                db.CreateTable("codeteam1", tableColumns);
            }
        }

        private void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (dg.Visibility == System.Windows.Visibility.Visible)
            {
                if (dg.Items.Count == 1)
                { 
                    return;
                }

                bool hasIncorrectValues = false;
                if (checkboxCodedField.IsChecked == true)
                {
                    if (dg.ItemsSource != null)
                    {
                        DataView dv = dg.ItemsSource as DataView;
                        if (dv != null)
                        {
                            DataTable dt = dv.Table;
                            if (dt != null && Database != null)
                            {
                                hasIncorrectValues = HasIncorrectFormat(dt);
                            }

                        }
                    }
                }
                if (!hasIncorrectValues)
                {
                    SavetheGrid();

                    gridAdminLocation.Visibility = System.Windows.Visibility.Visible;
                    gridValueEditor.Visibility = System.Windows.Visibility.Collapsed;
                    dg.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
            else
            {
                if (checkboxSCDDL.IsChecked == true && checkboxDistrictText.IsChecked == true)
                {
                    MessageBox.Show("Invalid combination. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                UpdateTypes();


                VhfProject newProject = new VhfProject(DataHelper.Project.FullName);
                DataHelper.InitializeProject(newProject);
                DataHelper.RepopulateCollections(false);

                if (this.Closed != null)
                {
                    isOK = true;
                    Closed(this, new EventArgs());
                }

            }
        }

        private void UpdateTypes()
        {
            List<string> countryFieldNames = new List<string>() { "ContactCountry1", "ContactCountry2", "ContactCountry3", "CountryDeath", "CountryFuneral", "CountryHospitalCurrent", "CountryHospitalPast1", "CountryHospitalPast2", "CountryOnset", "CountryRes", "CountryTravelled", "FuneralCountry1", "FuneralCountry2", "HospitalBeforeIllCountry" };
            List<string> adm1FieldNames = new List<string>() { "DistrictDeath", "DistrictFuneral", "DistrictHospitalCurrent", "DistrictHospitalPast1", "DistrictHospitalPast2", "DistrictOnset", "DistrictRes", "ContactDistrict", "HospitalDischargeDistrict", "InterviewerDistrict", "TradHealerDistrict", "HospitalBeforeIllDistrict", "TravelDistrict", "FuneralDistrict1", "FuneralDistrict2", "ContactDistrict1", "ContactDistrict2", "ContactDistrict3" };
            List<string> adm2FieldNames = new List<string>() { "SCOnset", "SCRes", "SCDeath", "SCFuneral", "SCHospitalCurrent", "ContactSC" };

            if (checkboxDistrictText.IsChecked == true && checkboxSCText.IsChecked == true)
            {
                #region Set all to text
                List<string> listOfFields = new List<string>();
                listOfFields.AddRange(adm1FieldNames);
                listOfFields.AddRange(adm2FieldNames);

                // make both fields text
                foreach (string fieldName in listOfFields)
                {
                    Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId WHERE Name = @Name");
                    updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, 1));
                    updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, fieldName));
                    DataHelper.Database.ExecuteNonQuery(updateQuery);
                }
                #endregion // Set all to text
            }
            else if (checkboxDistrictDDL.IsChecked == true && checkboxSCText.IsChecked == true)
            {
                #region Set District to LV, SC to Text
                // district is legal values
                foreach (string districtFieldName in adm1FieldNames)
                {
                    Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId, SourceTableName = @SourceTableName, TextColumnName = @TextColumnName, CodeColumnName = @CodeColumnName, Sort = @Sort " +
                        " WHERE Name = @Name");
                    updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, 17));
                    updateQuery.Parameters.Add(new QueryParameter("@SourceTableName", System.Data.DbType.String, "codeDistrictSubcountyList"));
                    updateQuery.Parameters.Add(new QueryParameter("@TextColumnName", System.Data.DbType.String, "DISTRICT"));
                    updateQuery.Parameters.Add(new QueryParameter("@CodeColumnName", System.Data.DbType.String, "DISTRICT"));
                    updateQuery.Parameters.Add(new QueryParameter("@Sort", System.Data.DbType.Boolean, true));
                    updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, districtFieldName));
                    DataHelper.Database.ExecuteNonQuery(updateQuery);
                }

                // SC remains text
                foreach (string scFieldName in adm2FieldNames)
                {
                    Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId WHERE Name = @Name");
                    updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, 1));
                    updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, scFieldName));
                    DataHelper.Database.ExecuteNonQuery(updateQuery);
                }
                #endregion // Set District to LV, SC to Text
            }
            else if (checkboxDistrictDDL.IsChecked == true && checkboxSCDDL.IsChecked == true)
            {
                #region Cascading codes
                // district is codes, SC is legal values
                foreach (string districtFieldName in adm1FieldNames)
                {
                    int fieldTypeId = 17;
                    bool cascade = false;
                    string cascadingFieldName = String.Empty;
                    int cascadingFieldId = -1;

                    if (districtFieldName.Equals("DistrictOnset", StringComparison.OrdinalIgnoreCase) ||
                        districtFieldName.Equals("DistrictRes", StringComparison.OrdinalIgnoreCase) ||
                        districtFieldName.Equals("DistrictDeath", StringComparison.OrdinalIgnoreCase) ||
                        districtFieldName.Equals("DistrictFuneral", StringComparison.OrdinalIgnoreCase) ||
                        districtFieldName.Equals("DistrictHospitalCurrent", StringComparison.OrdinalIgnoreCase) ||
                        districtFieldName.Equals("ContactDistrict", StringComparison.OrdinalIgnoreCase))
                    {
                        fieldTypeId = 18;
                        cascadingFieldName = districtFieldName.Replace("District", "SC");

                        Query selectQuery = DataHelper.Database.CreateQuery("SELECT FieldId FROM [metaFields] WHERE Name = @Name");
                        selectQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, cascadingFieldName));
                        DataTable dt = DataHelper.Database.Select(selectQuery);
                        if (dt.Rows.Count > 0)
                        {
                            cascadingFieldId = Convert.ToInt32(dt.Rows[0][0]);
                            cascade = true;
                        }
                    }

                    Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId, RelateCondition = @RelateCondition, SourceTableName = @SourceTableName, TextColumnName = @TextColumnName, CodeColumnName = @CodeColumnName, Sort = @Sort " +
                        " WHERE Name = @Name");
                    updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, fieldTypeId));

                    if (cascade)
                    {
                        updateQuery.Parameters.Add(new QueryParameter("@RelateCondition", System.Data.DbType.String, "SUBCOUNTIES:" + cascadingFieldId));
                    }
                    else
                    {
                        updateQuery.Parameters.Add(new QueryParameter("@RelateCondition", System.Data.DbType.String, String.Empty));
                    }
                    updateQuery.Parameters.Add(new QueryParameter("@SourceTableName", System.Data.DbType.String, "codeDistrictSubCountyList"));
                    updateQuery.Parameters.Add(new QueryParameter("@TextColumnName", System.Data.DbType.String, "DISTRICT"));
                    updateQuery.Parameters.Add(new QueryParameter("@CodeColumnName", System.Data.DbType.String, String.Empty));
                    updateQuery.Parameters.Add(new QueryParameter("@Sort", System.Data.DbType.Boolean, true));
                    updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, districtFieldName));
                    DataHelper.Database.ExecuteNonQuery(updateQuery);
                }

                foreach (string scFieldName in adm2FieldNames)
                {
                    Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId, SourceTableName = @SourceTableName, TextColumnName = @TextColumnName, CodeColumnName = @CodeColumnName, Sort = @Sort " +
                        " WHERE Name = @Name");
                    updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, 17));
                    updateQuery.Parameters.Add(new QueryParameter("@SourceTableName", System.Data.DbType.String, "codeDistrictSubCountyList"));
                    updateQuery.Parameters.Add(new QueryParameter("@TextColumnName", System.Data.DbType.String, "SUBCOUNTIES"));
                    updateQuery.Parameters.Add(new QueryParameter("@CodeColumnName", System.Data.DbType.String, "SUBCOUNTIES"));
                    updateQuery.Parameters.Add(new QueryParameter("@Sort", System.Data.DbType.Boolean, true));
                    updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, scFieldName));
                    DataHelper.Database.ExecuteNonQuery(updateQuery);
                }
                #endregion // Cascading codes
            }

            #region Country

            if (checkboxCountryDDL.IsChecked == false)
            {
                foreach (string countryFieldName in countryFieldNames)
                {
                    Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId WHERE Name = @Name");
                    updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, 1));
                    updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, countryFieldName));
                    DataHelper.Database.ExecuteNonQuery(updateQuery);
                }
            }
            else
            {
                int fieldTypeId = ReadFieldTypeID("CountryRes");
                string countryField = "COUNTRY";
                if (CommentLegalFieldsList.Contains(countryField.ToLower()))
                {
                    fieldTypeId = 19;
                }

                if (LegalFieldsList.Contains(countryField.ToLower()))
                {
                    fieldTypeId = 17;
                }

                foreach (string countryFieldName in countryFieldNames)
                {
                    Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId, SourceTableName = @SourceTableName, TextColumnName = @TextColumnName, CodeColumnName = @CodeColumnName, Sort = @Sort " +
                        " WHERE Name = @Name");
                    updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, fieldTypeId));
                    updateQuery.Parameters.Add(new QueryParameter("@SourceTableName", System.Data.DbType.String, "codeCountryList"));
                    updateQuery.Parameters.Add(new QueryParameter("@TextColumnName", System.Data.DbType.String, "COUNTRY"));
                    updateQuery.Parameters.Add(new QueryParameter("@CodeColumnName", System.Data.DbType.String, "COUNTRY"));
                    updateQuery.Parameters.Add(new QueryParameter("@Sort", System.Data.DbType.Boolean, true));
                    updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, countryFieldName));
                    DataHelper.Database.ExecuteNonQuery(updateQuery);
                }

                IDbDriver db = DataHelper.Database; // lazy
                if (!db.TableExists("codeCountryList"))
                {
                    List<Epi.Data.TableColumn> tableColumns = new List<Epi.Data.TableColumn>();
                    tableColumns.Add(new Epi.Data.TableColumn("COUNTRY", GenericDbColumnType.String, 255, true, false));
                    db.CreateTable("codeCountryList", tableColumns);
                }
            }

            #endregion // Country

            #region HcwPosition

            if (checkboxHCWpositionDDL.IsChecked == false)
            {
                Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, 1));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "hcwposition"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);

            }
            else
            {
                string currentFieldName = "hcwposition";
                string tableName = "code" + currentFieldName + "1";

                UpdateFieldTypeToDropdown(currentFieldName, tableName);

            }



            #endregion // hcwposition

            #region HcwFacitlity

            if (checkboxHCWFacilityDDL.IsChecked == false)
            {

                Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, 1));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "hcwfacility"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);

            }
            else
            {

                string currentFieldName = "hcwfacility";
                string tableName = "code" + currentFieldName + "1";

                UpdateFieldTypeToDropdown(currentFieldName, tableName);


            }



            #endregion // hcwposition

            #region CurrentHospital

            if (checkboxHospitalCurrentDDL.IsChecked == false)
            {

                Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, 1));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "hospitalcurrent"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);

            }
            else
            {

                string currentFieldName = "hospitalcurrent";
                string tableName = "code" + currentFieldName + "1";

                UpdateFieldTypeToDropdown(currentFieldName, tableName);
                //int fieldTypeId = 17;
                //if (CommentLegalFieldsList.Contains("hospitalcurrent"))
                //{
                //    fieldTypeId = 18;
                //}

                //Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId, SourceTableName = @SourceTableName, TextColumnName = @TextColumnName, CodeColumnName = @CodeColumnName, Sort = @Sort " +
                //    " WHERE Name = @Name");
                //updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, fieldTypeId));
                //updateQuery.Parameters.Add(new QueryParameter("@SourceTableName", System.Data.DbType.String, "codehospitalcurrent1"));
                //updateQuery.Parameters.Add(new QueryParameter("@TextColumnName", System.Data.DbType.String, "hospitalcurrent"));
                //updateQuery.Parameters.Add(new QueryParameter("@CodeColumnName", System.Data.DbType.String, "hospitalcurrent"));
                //updateQuery.Parameters.Add(new QueryParameter("@Sort", System.Data.DbType.Boolean, true));
                //updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "hospitalcurrent"));
                //DataHelper.Database.ExecuteNonQuery(updateQuery);

            }



            #endregion // currenthospital

            #region Trasnporter Type

            if (checkboxTransporterTypeDDL.IsChecked == false)
            {

                Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, 1));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "TransporterType"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);

            }
            else
            {

                string currentFieldName = "transportertype";
                string tableName = "code" + currentFieldName + "1";

                UpdateFieldTypeToDropdown(currentFieldName, tableName);


                //int fieldTypeId = 17;
                //if (CommentLegalFieldsList.Contains("transportertype"))
                //{
                //    fieldTypeId = 18;
                //}

                //Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId, SourceTableName = @SourceTableName, TextColumnName = @TextColumnName, CodeColumnName = @CodeColumnName, Sort = @Sort " +
                //    " WHERE Name = @Name");
                //updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, fieldTypeId));
                //updateQuery.Parameters.Add(new QueryParameter("@SourceTableName", System.Data.DbType.String, "codetransportertype1"));
                //updateQuery.Parameters.Add(new QueryParameter("@TextColumnName", System.Data.DbType.String, "TransporterType"));
                //updateQuery.Parameters.Add(new QueryParameter("@CodeColumnName", System.Data.DbType.String, "TransporterType"));
                //updateQuery.Parameters.Add(new QueryParameter("@Sort", System.Data.DbType.Boolean, true));
                //updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "TransporterType"));
                //DataHelper.Database.ExecuteNonQuery(updateQuery);

            }



            #endregion // TransporterType

            #region Hospital Past

            if (checkboxHospitalPastDDL.IsChecked == false)
            {

                Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, 1));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "HospitalPast1"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);

                updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, 1));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "HospitalPast2"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);

            }
            else
            {

                int fieldTypeId = ReadFieldTypeID("hospitalpast1"); ;
                if (CommentLegalFieldsList.Contains("hospitalpast"))
                {
                    fieldTypeId = 19;
                }

                if (LegalFieldsList.Contains("hospitalpast") || fieldTypeId == 1)//Providing default value, if DropDown Items have not been added. 
                {
                    fieldTypeId = 17;
                }

                Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId, SourceTableName = @SourceTableName, TextColumnName = @TextColumnName, CodeColumnName = @CodeColumnName, Sort = @Sort " +
                    " WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, fieldTypeId));
                updateQuery.Parameters.Add(new QueryParameter("@SourceTableName", System.Data.DbType.String, "codehospitalpast1"));
                updateQuery.Parameters.Add(new QueryParameter("@TextColumnName", System.Data.DbType.String, "HospitalPast"));
                updateQuery.Parameters.Add(new QueryParameter("@CodeColumnName", System.Data.DbType.String, "HospitalPast"));
                updateQuery.Parameters.Add(new QueryParameter("@Sort", System.Data.DbType.Boolean, true));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "HospitalPast1"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);

                updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId, SourceTableName = @SourceTableName, TextColumnName = @TextColumnName, CodeColumnName = @CodeColumnName, Sort = @Sort " +
                    " WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, fieldTypeId));
                updateQuery.Parameters.Add(new QueryParameter("@SourceTableName", System.Data.DbType.String, "codehospitalpast1"));
                updateQuery.Parameters.Add(new QueryParameter("@TextColumnName", System.Data.DbType.String, "HospitalPast"));
                updateQuery.Parameters.Add(new QueryParameter("@CodeColumnName", System.Data.DbType.String, "HospitalPast"));
                updateQuery.Parameters.Add(new QueryParameter("@Sort", System.Data.DbType.Boolean, true));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "HospitalPast2"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);

            }



            #endregion // TransporterType

            #region OtherOccupDetail

            if (checkboxOtherOccupDetailDDL.IsChecked == false)
            {

                Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, 1));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "OtherOccupDetail"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);

            }
            else
            {
                string currentFieldName = "otheroccupdetail";
                string tableName = "code" + currentFieldName + "1";

                UpdateFieldTypeToDropdown(currentFieldName, tableName);

                //int fieldTypeId = 17;
                //if (CommentLegalFieldsList.Contains("otheroccupdetail"))
                //{
                //    fieldTypeId = 18;
                //}

                //Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId, SourceTableName = @SourceTableName, TextColumnName = @TextColumnName, CodeColumnName = @CodeColumnName, Sort = @Sort " +
                //    " WHERE Name = @Name");
                //updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, fieldTypeId));
                //updateQuery.Parameters.Add(new QueryParameter("@SourceTableName", System.Data.DbType.String, "codeotheroccupdetail1"));
                //updateQuery.Parameters.Add(new QueryParameter("@TextColumnName", System.Data.DbType.String, "OtherOccupDetail"));
                //updateQuery.Parameters.Add(new QueryParameter("@CodeColumnName", System.Data.DbType.String, "OtherOccupDetail"));
                //updateQuery.Parameters.Add(new QueryParameter("@Sort", System.Data.DbType.Boolean, true));
                //updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "OtherOccupDetail"));
                //DataHelper.Database.ExecuteNonQuery(updateQuery);

            }



            #endregion

            #region BusinessType

            if (checkboxBusinessTypeDDL.IsChecked == false)
            {

                Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, 1));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "BusinessType"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);

            }
            else
            {
                string currentFieldName = "businesstype";
                string tableName = "code" + currentFieldName + "1";

                UpdateFieldTypeToDropdown(currentFieldName, tableName);

            }



            #endregion

            #region BleedOtherComment

            if (checkboxBleedOtherCommentDDL.IsChecked == false)
            {

                Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, 1));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "BleedOtherComment"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);

            }
            else
            {
                string currentFieldName = "bleedothercomment";
                string tableName = "code" + currentFieldName + "1";

                UpdateFieldTypeToDropdown(currentFieldName, tableName);

                //int fieldTypeId = 17;
                //if (CommentLegalFieldsList.Contains("bleedothercomment"))
                //{
                //    fieldTypeId = 18;
                //}

                //Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId, SourceTableName = @SourceTableName, TextColumnName = @TextColumnName, CodeColumnName = @CodeColumnName, Sort = @Sort " +
                //    " WHERE Name = @Name");
                //updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, fieldTypeId));
                //updateQuery.Parameters.Add(new QueryParameter("@SourceTableName", System.Data.DbType.String, "codebleedothercomment1"));
                //updateQuery.Parameters.Add(new QueryParameter("@TextColumnName", System.Data.DbType.String, "BleedOtherComment"));
                //updateQuery.Parameters.Add(new QueryParameter("@CodeColumnName", System.Data.DbType.String, "BleedOtherComment"));
                //updateQuery.Parameters.Add(new QueryParameter("@Sort", System.Data.DbType.Boolean, true));
                //updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "BleedOtherComment"));
                //DataHelper.Database.ExecuteNonQuery(updateQuery);

            }



            #endregion


            #region SymptOtherComment

            if (checkboxSymptOtherCommentDDL.IsChecked == false)
            {

                Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, 1));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "SymptOtherComment"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);

                updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, 1));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "OtherHemoFinalSpecify"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);

            }
            else
            {
                int fieldTypeId = ReadFieldTypeID("symptothercomment"); ;
                if (CommentLegalFieldsList.Contains("symptothercomment"))
                {
                    fieldTypeId = 19;
                }

                if (LegalFieldsList.Contains("symptothercomment") || fieldTypeId == 1) //Providing default value, if DropDown Items have not been added. 
                {
                    fieldTypeId = 17;
                }


                Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId, SourceTableName = @SourceTableName, TextColumnName = @TextColumnName, CodeColumnName = @CodeColumnName, Sort = @Sort " +
                    " WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, fieldTypeId));
                updateQuery.Parameters.Add(new QueryParameter("@SourceTableName", System.Data.DbType.String, "codesymptothercomment1"));
                updateQuery.Parameters.Add(new QueryParameter("@TextColumnName", System.Data.DbType.String, "SymptOtherComment"));
                updateQuery.Parameters.Add(new QueryParameter("@CodeColumnName", System.Data.DbType.String, "SymptOtherComment"));
                updateQuery.Parameters.Add(new QueryParameter("@Sort", System.Data.DbType.Boolean, true));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "SymptOtherComment"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);

                updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId, SourceTableName = @SourceTableName, TextColumnName = @TextColumnName, CodeColumnName = @CodeColumnName, Sort = @Sort " +
                    " WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, fieldTypeId));
                updateQuery.Parameters.Add(new QueryParameter("@SourceTableName", System.Data.DbType.String, "codesymptothercomment1"));
                updateQuery.Parameters.Add(new QueryParameter("@TextColumnName", System.Data.DbType.String, "SymptOtherComment"));
                updateQuery.Parameters.Add(new QueryParameter("@CodeColumnName", System.Data.DbType.String, "SymptOtherComment"));
                updateQuery.Parameters.Add(new QueryParameter("@Sort", System.Data.DbType.Boolean, true));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "OtherHemoFinalSpecify"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);

            }



            #endregion

            #region Contact Relation

            if (checkboxContactRelationDDL.IsChecked == false)
            {

                Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, 1));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "ContactRelation1"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);

                updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, 1));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "ContactRelation2"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);



                updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, 1));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "ContactRelation3"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);

            }
            else
            {
                int fieldTypeId = ReadFieldTypeID("contactrelation1"); ;
                if (CommentLegalFieldsList.Contains("contactrelation"))
                {
                    fieldTypeId = 19;
                }

                if (LegalFieldsList.Contains("contactrelation") || fieldTypeId == 1)//Providing default value, if DropDown Items have not been added. 
                {
                    fieldTypeId = 17;
                }

                Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId, SourceTableName = @SourceTableName, TextColumnName = @TextColumnName, CodeColumnName = @CodeColumnName, Sort = @Sort " +
                    " WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, fieldTypeId));
                updateQuery.Parameters.Add(new QueryParameter("@SourceTableName", System.Data.DbType.String, "codecontactrelation1"));
                updateQuery.Parameters.Add(new QueryParameter("@TextColumnName", System.Data.DbType.String, "ContactRelation"));
                updateQuery.Parameters.Add(new QueryParameter("@CodeColumnName", System.Data.DbType.String, "ContactRelation"));
                updateQuery.Parameters.Add(new QueryParameter("@Sort", System.Data.DbType.Boolean, true));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "ContactRelation1"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);

                updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId, SourceTableName = @SourceTableName, TextColumnName = @TextColumnName, CodeColumnName = @CodeColumnName, Sort = @Sort " +
                    " WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, fieldTypeId));
                updateQuery.Parameters.Add(new QueryParameter("@SourceTableName", System.Data.DbType.String, "codecontactrelation1"));
                updateQuery.Parameters.Add(new QueryParameter("@TextColumnName", System.Data.DbType.String, "ContactRelation"));
                updateQuery.Parameters.Add(new QueryParameter("@CodeColumnName", System.Data.DbType.String, "ContactRelation"));
                updateQuery.Parameters.Add(new QueryParameter("@Sort", System.Data.DbType.Boolean, true));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "ContactRelation2"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);

                updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId, SourceTableName = @SourceTableName, TextColumnName = @TextColumnName, CodeColumnName = @CodeColumnName, Sort = @Sort " +
              " WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, fieldTypeId));
                updateQuery.Parameters.Add(new QueryParameter("@SourceTableName", System.Data.DbType.String, "codecontactrelation1"));
                updateQuery.Parameters.Add(new QueryParameter("@TextColumnName", System.Data.DbType.String, "ContactRelation"));
                updateQuery.Parameters.Add(new QueryParameter("@CodeColumnName", System.Data.DbType.String, "ContactRelation"));
                updateQuery.Parameters.Add(new QueryParameter("@Sort", System.Data.DbType.Boolean, true));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "ContactRelation3"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);
            }



            #endregion

            #region Funeral Relation

            if (checkboxContactRelationDDL.IsChecked == false)
            {

                Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, 1));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "FuneralRelation1"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);

                updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, 1));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "FuneralRelation2"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);

            }
            else
            {
                int fieldTypeId = ReadFieldTypeID("funeralrelation1"); ;
                if (CommentLegalFieldsList.Contains("funeralrelation"))
                {
                    fieldTypeId = 19;
                }

                if (LegalFieldsList.Contains("funeralrelation") || fieldTypeId == 1)//Providing default value, if DropDown Items have not been added. 
                {
                    fieldTypeId = 17;
                }

                Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId, SourceTableName = @SourceTableName, TextColumnName = @TextColumnName, CodeColumnName = @CodeColumnName, Sort = @Sort " +
                    " WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, fieldTypeId));
                updateQuery.Parameters.Add(new QueryParameter("@SourceTableName", System.Data.DbType.String, "codefuneralrelation1"));
                updateQuery.Parameters.Add(new QueryParameter("@TextColumnName", System.Data.DbType.String, "FuneralRelation"));
                updateQuery.Parameters.Add(new QueryParameter("@CodeColumnName", System.Data.DbType.String, "FuneralRelation"));
                updateQuery.Parameters.Add(new QueryParameter("@Sort", System.Data.DbType.Boolean, true));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "FuneralRelation1"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);

                updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId, SourceTableName = @SourceTableName, TextColumnName = @TextColumnName, CodeColumnName = @CodeColumnName, Sort = @Sort " +
                    " WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, fieldTypeId));
                updateQuery.Parameters.Add(new QueryParameter("@SourceTableName", System.Data.DbType.String, "codefuneralrelation1"));
                updateQuery.Parameters.Add(new QueryParameter("@TextColumnName", System.Data.DbType.String, "FuneralRelation"));
                updateQuery.Parameters.Add(new QueryParameter("@CodeColumnName", System.Data.DbType.String, "FuneralRelation"));
                updateQuery.Parameters.Add(new QueryParameter("@Sort", System.Data.DbType.Boolean, true));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "FuneralRelation2"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);

            }



            #endregion


            #region Hospital Before Ill Name

            if (checkboxHospitalBeforeIllNameDDL.IsChecked == false)
            {

                Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, 1));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "HospitalBeforeIllName"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);


            }
            else
            {

                string currentFieldName = "hospitalbeforeillname";
                string tableName = "code" + currentFieldName + "1";

                UpdateFieldTypeToDropdown(currentFieldName, tableName);
                //int fieldTypeId = 17;
                //if (CommentLegalFieldsList.Contains("hospitalbeforeillname"))
                //{
                //    fieldTypeId = 18;
                //}

                //Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId, SourceTableName = @SourceTableName, TextColumnName = @TextColumnName, CodeColumnName = @CodeColumnName, Sort = @Sort " +
                //    " WHERE Name = @Name");
                //updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, fieldTypeId));
                //updateQuery.Parameters.Add(new QueryParameter("@SourceTableName", System.Data.DbType.String, "codehospitalbeforeillname1"));
                //updateQuery.Parameters.Add(new QueryParameter("@TextColumnName", System.Data.DbType.String, "HospitalBeforeIllName"));
                //updateQuery.Parameters.Add(new QueryParameter("@CodeColumnName", System.Data.DbType.String, "HospitalBeforeIllName"));
                //updateQuery.Parameters.Add(new QueryParameter("@Sort", System.Data.DbType.Boolean, true));
                //updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "HospitalBeforeIllName"));
                //DataHelper.Database.ExecuteNonQuery(updateQuery);



            }



            #endregion

            #region Interviewer Position

            if (checkboxInterviewerPositionDDL.IsChecked == false)
            {

                Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, 1));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "InterviewerPosition"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);


            }
            else
            {

                string currentFieldName = "interviewerposition";
                string tableName = "code" + currentFieldName + "1";

                UpdateFieldTypeToDropdown(currentFieldName, tableName);

                //int fieldTypeId = 17;
                //if (CommentLegalFieldsList.Contains("interviewerposition"))
                //{
                //    fieldTypeId = 18;
                //}
                //Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId, SourceTableName = @SourceTableName, TextColumnName = @TextColumnName, CodeColumnName = @CodeColumnName, Sort = @Sort " +
                //    " WHERE Name = @Name");
                //updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, fieldTypeId));
                //updateQuery.Parameters.Add(new QueryParameter("@SourceTableName", System.Data.DbType.String, "codeinterviewerposition1"));
                //updateQuery.Parameters.Add(new QueryParameter("@TextColumnName", System.Data.DbType.String, "InterviewerPosition"));
                //updateQuery.Parameters.Add(new QueryParameter("@CodeColumnName", System.Data.DbType.String, "InterviewerPosition"));
                //updateQuery.Parameters.Add(new QueryParameter("@Sort", System.Data.DbType.Boolean, true));
                //updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "InterviewerPosition"));
                //DataHelper.Database.ExecuteNonQuery(updateQuery);



            }



            #endregion

            #region Interviewer Health Facility

            if (checkboxInterviewerHealthFacilityDDL.IsChecked == false)
            {

                Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, 1));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "InterviewerHealthFacility"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);


            }
            else
            {
                string currentFieldName = "interviewerhealthfacility";
                string tableName = "code" + currentFieldName + "1";

                UpdateFieldTypeToDropdown(currentFieldName, tableName);


                //int fieldTypeId = 17;
                //if (CommentLegalFieldsList.Contains("interviewerhealthfacility"))
                //{
                //    fieldTypeId = 18;
                //}

                //Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId, SourceTableName = @SourceTableName, TextColumnName = @TextColumnName, CodeColumnName = @CodeColumnName, Sort = @Sort " +
                //    " WHERE Name = @Name");
                //updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, fieldTypeId));
                //updateQuery.Parameters.Add(new QueryParameter("@SourceTableName", System.Data.DbType.String, "codeinterviewerhealthfacility1"));
                //updateQuery.Parameters.Add(new QueryParameter("@TextColumnName", System.Data.DbType.String, "InterviewerHealthFacility"));
                //updateQuery.Parameters.Add(new QueryParameter("@CodeColumnName", System.Data.DbType.String, "InterviewerHealthFacility"));
                //updateQuery.Parameters.Add(new QueryParameter("@Sort", System.Data.DbType.Boolean, true));
                //updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "InterviewerHealthFacility"));
                //DataHelper.Database.ExecuteNonQuery(updateQuery);



            }



            #endregion

            #region Specify Bleeding

            if (checkboxSpecifyBleedingDDL.IsChecked == false)
            {

                Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, 1));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "SpecifyBleeding"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);
            }
            else
            {

                string currentFieldName = "specifybleeding";
                string tableName = "code" + currentFieldName + "1";

                UpdateFieldTypeToDropdown(currentFieldName, tableName);



                //int fieldTypeId = 17;
                //if (CommentLegalFieldsList.Contains("specifybleeding"))
                //{
                //    fieldTypeId = 18;
                //}
                //Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId, SourceTableName = @SourceTableName, TextColumnName = @TextColumnName, CodeColumnName = @CodeColumnName, Sort = @Sort " +
                //    " WHERE Name = @Name");
                //updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, fieldTypeId));
                //updateQuery.Parameters.Add(new QueryParameter("@SourceTableName", System.Data.DbType.String, "codespecifybleeding1"));
                //updateQuery.Parameters.Add(new QueryParameter("@TextColumnName", System.Data.DbType.String, "SpecifyBleeding"));
                //updateQuery.Parameters.Add(new QueryParameter("@CodeColumnName", System.Data.DbType.String, "SpecifyBleeding"));
                //updateQuery.Parameters.Add(new QueryParameter("@Sort", System.Data.DbType.Boolean, true));
                //updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "SpecifyBleeding"));
                //DataHelper.Database.ExecuteNonQuery(updateQuery);
            }



            #endregion

            #region Proxy Relation

            if (checkboxProxyRelationDDL.IsChecked == false)
            {
                Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, 1));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "ProxyRelation"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);
            }
            else
            {
                string currentFieldName = "proxyrelation";
                string tableName = "code" + currentFieldName + "1";

                UpdateFieldTypeToDropdown(currentFieldName, tableName);

                //int fieldTypeId = 17;
                //if (CommentLegalFieldsList.Contains("proxyrelation"))
                //{
                //    fieldTypeId = 18;
                //}

                //Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId, SourceTableName = @SourceTableName, TextColumnName = @TextColumnName, CodeColumnName = @CodeColumnName, Sort = @Sort " +
                //    " WHERE Name = @Name");
                //updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, fieldTypeId));
                //updateQuery.Parameters.Add(new QueryParameter("@SourceTableName", System.Data.DbType.String, "codeproxyrelation1"));
                //updateQuery.Parameters.Add(new QueryParameter("@TextColumnName", System.Data.DbType.String, "ProxyRelation"));
                //updateQuery.Parameters.Add(new QueryParameter("@CodeColumnName", System.Data.DbType.String, "ProxyRelation"));
                //updateQuery.Parameters.Add(new QueryParameter("@Sort", System.Data.DbType.Boolean, true));
                //updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "ProxyRelation"));
                //DataHelper.Database.ExecuteNonQuery(updateQuery);
            }



            #endregion

            #region Hospital Discharge

            if (checkboxHospitalDischargeDDL.IsChecked == false)
            {
                Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, 1));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "HospitalDischarge"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);
            }
            else
            {
                string currentFieldName = "hospitaldischarge";
                string tableName = "code" + currentFieldName + "1";

                UpdateFieldTypeToDropdown(currentFieldName, tableName);

                //int fieldTypeId = 17;
                //if (CommentLegalFieldsList.Contains("hospitaldischarge"))
                //{
                //    fieldTypeId = 18;
                //}

                //Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId, SourceTableName = @SourceTableName, TextColumnName = @TextColumnName, CodeColumnName = @CodeColumnName, Sort = @Sort " +
                //    " WHERE Name = @Name");
                //updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, fieldTypeId));
                //updateQuery.Parameters.Add(new QueryParameter("@SourceTableName", System.Data.DbType.String, "codehospitaldischarge1"));
                //updateQuery.Parameters.Add(new QueryParameter("@TextColumnName", System.Data.DbType.String, "HospitalDischarge"));
                //updateQuery.Parameters.Add(new QueryParameter("@CodeColumnName", System.Data.DbType.String, "HospitalDischarge"));
                //updateQuery.Parameters.Add(new QueryParameter("@Sort", System.Data.DbType.Boolean, true));
                //updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "HospitalDischarge"));
                //DataHelper.Database.ExecuteNonQuery(updateQuery);
            }



            #endregion

            #region Hospital Death

            if (checkboxHospitalDeathDDL.IsChecked == false)
            {
                Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, 1));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "HospitalDeath"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);
            }
            else
            {
                string currentFieldName = "hospitaldeath";
                string tableName = "code" + currentFieldName + "1";

                UpdateFieldTypeToDropdown(currentFieldName, tableName);
                //int fieldTypeId = 17;
                //if (CommentLegalFieldsList.Contains("hospitaldeath"))
                //{
                //    fieldTypeId = 18;
                //}

                //Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId, SourceTableName = @SourceTableName, TextColumnName = @TextColumnName, CodeColumnName = @CodeColumnName, Sort = @Sort " +
                //    " WHERE Name = @Name");
                //updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, fieldTypeId));
                //updateQuery.Parameters.Add(new QueryParameter("@SourceTableName", System.Data.DbType.String, "codehospitaldeath1"));
                //updateQuery.Parameters.Add(new QueryParameter("@TextColumnName", System.Data.DbType.String, "HospitalDeath"));
                //updateQuery.Parameters.Add(new QueryParameter("@CodeColumnName", System.Data.DbType.String, "HospitalDeath"));
                //updateQuery.Parameters.Add(new QueryParameter("@Sort", System.Data.DbType.Boolean, true));
                //updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "HospitalDeath"));
                //DataHelper.Database.ExecuteNonQuery(updateQuery);
            }



            #endregion

            #region Place Death Other

            if (checkboxPlaceDeathOtherDDL.IsChecked == false)
            {
                Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, 1));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "PlaceDeathOther"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);
            }
            else
            {
                string currentFieldName = "placedeathother";
                string tableName = "code" + currentFieldName + "1";

                UpdateFieldTypeToDropdown(currentFieldName, tableName);

                //int fieldTypeId = 17;
                //if (CommentLegalFieldsList.Contains("placedeathother"))
                //{
                //    fieldTypeId = 18;
                //}

                //Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId, SourceTableName = @SourceTableName, TextColumnName = @TextColumnName, CodeColumnName = @CodeColumnName, Sort = @Sort " +
                //    " WHERE Name = @Name");
                //updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, fieldTypeId));
                //updateQuery.Parameters.Add(new QueryParameter("@SourceTableName", System.Data.DbType.String, "codeplacedeathother1"));
                //updateQuery.Parameters.Add(new QueryParameter("@TextColumnName", System.Data.DbType.String, "PlaceDeathOther"));
                //updateQuery.Parameters.Add(new QueryParameter("@CodeColumnName", System.Data.DbType.String, "PlaceDeathOther"));
                //updateQuery.Parameters.Add(new QueryParameter("@Sort", System.Data.DbType.Boolean, true));
                //updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "PlaceDeathOther"));
                //DataHelper.Database.ExecuteNonQuery(updateQuery);
            }



            #endregion



            #region ContactHCWFacility

            if (checkboxContactHCWFacilityDDL.IsChecked == false)
            {
                Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, 1));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "ContactHCWFacility"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);
            }
            else
            {
                string currentFieldName = "contacthcwfacility";
                string tableName = "code" + currentFieldName + "1";

                UpdateFieldTypeToDropdown(currentFieldName, tableName);

                //int fieldTypeId = 17;
                //if (CommentLegalFieldsList.Contains("contacthcwfacility"))
                //{
                //    fieldTypeId = 18;
                //}

                //Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId, SourceTableName = @SourceTableName, TextColumnName = @TextColumnName, CodeColumnName = @CodeColumnName, Sort = @Sort " +
                //    " WHERE Name = @Name");
                //updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, fieldTypeId));
                //updateQuery.Parameters.Add(new QueryParameter("@SourceTableName", System.Data.DbType.String, "codecontacthcwfacility1"));
                //updateQuery.Parameters.Add(new QueryParameter("@TextColumnName", System.Data.DbType.String, "ContactHCWFacility"));
                //updateQuery.Parameters.Add(new QueryParameter("@CodeColumnName", System.Data.DbType.String, "ContactHCWFacility"));
                //updateQuery.Parameters.Add(new QueryParameter("@Sort", System.Data.DbType.Boolean, true));
                //updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "ContactHCWFacility"));
                //DataHelper.Database.ExecuteNonQuery(updateQuery);
            }



            #endregion

            #region Team

            if (checkboxTeamDDL.IsChecked == false)
            {
                Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId WHERE Name = @Name");
                updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, 1));
                updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "Team"));
                DataHelper.Database.ExecuteNonQuery(updateQuery);
            }
            else
            {
                string currentFieldName = "team";
                string tableName = "code" + currentFieldName + "1";

                UpdateFieldTypeToDropdown(currentFieldName, tableName);

                //int fieldTypeId = 17;
                //if (CommentLegalFieldsList.Contains("team"))
                //{
                //    fieldTypeId = 18;
                //}

                //Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId, SourceTableName = @SourceTableName, TextColumnName = @TextColumnName, CodeColumnName = @CodeColumnName, Sort = @Sort " +
                //    " WHERE Name = @Name");
                //updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, fieldTypeId));
                //updateQuery.Parameters.Add(new QueryParameter("@SourceTableName", System.Data.DbType.String, "codeteam1"));
                //updateQuery.Parameters.Add(new QueryParameter("@TextColumnName", System.Data.DbType.String, "Team"));
                //updateQuery.Parameters.Add(new QueryParameter("@CodeColumnName", System.Data.DbType.String, "Team"));
                //updateQuery.Parameters.Add(new QueryParameter("@Sort", System.Data.DbType.Boolean, true));
                //updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, "Team"));
                //DataHelper.Database.ExecuteNonQuery(updateQuery);
            }



            #endregion
        }

        private void UpdateFieldTypeToDropdown(string currentfieldname, string tableName)
        {
            int fieldTypeId = ReadFieldTypeID(currentfieldname);
            if (CommentLegalFieldsList.Contains(currentfieldname))
            {
                fieldTypeId = 19;
            }

            if (LegalFieldsList.Contains(currentfieldname))
            {
                fieldTypeId = 17;
            }

            Query updateQuery = DataHelper.Database.CreateQuery("UPDATE [metaFields] SET FieldTypeId = @FieldTypeId, SourceTableName = @SourceTableName, TextColumnName = @TextColumnName, CodeColumnName = @CodeColumnName, Sort = @Sort " +
                " WHERE Name = @Name");
            updateQuery.Parameters.Add(new QueryParameter("@FieldTypeId", System.Data.DbType.Int32, fieldTypeId));
            updateQuery.Parameters.Add(new QueryParameter("@SourceTableName", System.Data.DbType.String, tableName));
            updateQuery.Parameters.Add(new QueryParameter("@TextColumnName", System.Data.DbType.String, currentfieldname));
            updateQuery.Parameters.Add(new QueryParameter("@CodeColumnName", System.Data.DbType.String, currentfieldname));
            updateQuery.Parameters.Add(new QueryParameter("@Sort", System.Data.DbType.Boolean, true));
            updateQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, currentfieldname));
            DataHelper.Database.ExecuteNonQuery(updateQuery);
        }


        private void SavetheGrid()
        {
            // tableName = "code" + fieldName + "1";
            //bool isInputValuesValidated = false;
            if (dg.ItemsSource != null)
            {
                DataView dv = dg.ItemsSource as DataView;
                if (dv != null)
                {
                    DataTable dt = dv.Table;
                    if (dt != null && Database != null)
                    {

                        try
                        {
                            DataHelper.SendMessageForAwaitAll();
                            string querySyntax = "DELETE * FROM [" + tableName + "]";
                            if (Database.ToString().ToLower().Contains("sql"))
                            {
                                querySyntax = "DELETE FROM [" + tableName + "]";
                            }

                            Query deleteQuery = Database.CreateQuery(querySyntax);
                            Database.ExecuteNonQuery(deleteQuery);


                            if (fieldName.ToLower() == "district")
                            {
                                string col1 = dt.Columns[0].ColumnName;
                                string col2 = dt.Columns[1].ColumnName;
                                DataRow[] rows = dt.Select(String.Empty, col1 + ", " + col2, DataViewRowState.CurrentRows);

                                foreach (DataRow row in rows)
                                {
                                    Query insertQuery = Database.CreateQuery("INSERT INTO [codeDistrictSubCountyList] (DISTRICT, SUBCOUNTIES, DISTCODE) VALUES (" +
                                        "@DISTRICT, @SUBCOUNTIES, @DISTCODE)");
                                    insertQuery.Parameters.Add(new QueryParameter("@DISTRICT", DbType.String, row[0].ToString()));
                                    insertQuery.Parameters.Add(new QueryParameter("@SUBCOUNTIES", DbType.String, row[1].ToString()));
                                    insertQuery.Parameters.Add(new QueryParameter("@DISTCODE", DbType.String, row[2].ToString()));
                                    Database.ExecuteNonQuery(insertQuery);
                                }
                            }
                            else
                            {
                                if ((bool)checkboxCodedField.IsChecked)
                                {
                                    CommentLegalFieldsList.Add(fieldName);
                                    LegalFieldsList.Remove(fieldName);
                                }
                                else
                                {
                                    CommentLegalFieldsList.Remove(fieldName);
                                    LegalFieldsList.Add(fieldName);
                                }

                                DataRow[] rows = dt.Select(String.Empty, fieldName, DataViewRowState.CurrentRows);
                                foreach (DataRow row in rows)
                                {
                                    Query insertQuery = Database.CreateQuery("INSERT INTO [" + tableName + "] (" + fieldName + ") VALUES (" +
                                        "@" + fieldName + ")");
                                    insertQuery.Parameters.Add(new QueryParameter("@" + fieldName + "", DbType.String, row[fieldName].ToString()));
                                    Database.ExecuteNonQuery(insertQuery);
                                }
                            }


                            MessageBox.Show("Changes were committed to the database successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Changes were not committed to the database successfully. Error: " + ex.Message, "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        finally
                        {
                            DataHelper.SendMessageForUnAwaitAll();
                        }

                    }
                }
            }
        }

        private bool HasIncorrectFormat(DataTable dt)
        {
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string inputvalue = dt.Rows[i][0].ToString();
                if (!inputvalue.Contains("-"))
                {
                    MessageBox.Show("Please separate comment from legal value with a hypen (-):" + inputvalue);
                    return true;
                }
            }
            return false;
        }

        private void btnadminFields_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            spAdminLocation.Visibility = System.Windows.Visibility.Visible;
            spCaseContact.Visibility = System.Windows.Visibility.Collapsed;
            btnadminFields.IsChecked = true;
            btnCaseContactFields.IsChecked = false;
        }

        private void btnCaseContactFields_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            spAdminLocation.Visibility = System.Windows.Visibility.Collapsed;
            spCaseContact.Visibility = System.Windows.Visibility.Visible;
            btnadminFields.IsChecked = false;
            btnCaseContactFields.IsChecked = true;
        }

        private void mnuDistrictNameEditor_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // TODO: Add event handler implementation here.
        }

        private void mnuCountryNameEditor_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Add event handler implementation here.
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            gridAdminLocation.Visibility = System.Windows.Visibility.Collapsed;
            gridValueEditor.Visibility = System.Windows.Visibility.Visible;
            fieldName = ((Button)sender).Name.Replace("btn", "").ToLower();
            if (fieldName == "country")
            {
                tableName = "codeCountryList";
                btnCreatListFromExisting.Visibility = System.Windows.Visibility.Visible;
                checkboxCodedField.Visibility = System.Windows.Visibility.Visible;
                SetCommentLegalCheckbox("CountryRes");
            }
            else if (fieldName == "district")
            {
                tableName = "codeDistrictSubCountyList";
                if (checkboxDistrictDDL.IsChecked == true)
                {
                    fieldName = "DISTRICT";
                }
                else
                {
                    fieldName = "SUBCOUNTIES";
                }
                btnCreatListFromExisting.Visibility = System.Windows.Visibility.Collapsed;
                checkboxCodedField.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                tableName = "code" + fieldName.ToLower() + "1";
                btnCreatListFromExisting.Visibility = System.Windows.Visibility.Visible;



                SetCommentLegalCheckbox(fieldName);

                checkboxCodedField.Visibility = System.Windows.Visibility.Visible;
            }

            string queryText = "SELECT * FROM [" + tableName + "] ORDER BY " + fieldName;

            if (checkboxSCDDL.IsChecked == true && fieldName.Equals("DISTRICT", StringComparison.OrdinalIgnoreCase))
            {
                queryText = queryText + ", SUBCOUNTIES";
            }

            Query selectQuery = Database.CreateQuery(queryText);
            dg.ItemsSource = null;
            dg.ItemsSource = (Database.Select(selectQuery)).DefaultView;

            dg.Visibility = System.Windows.Visibility.Visible;
        }

        private void SetCommentLegalCheckbox(string fieldName)
        {
            if (CommentLegalFieldsList.Contains(fieldName.ToLower()))
            {
                checkboxCodedField.IsChecked = true;
            }
            else if (LegalFieldsList.Contains(fieldName.ToLower()))
            {
                checkboxCodedField.IsChecked = false;
            }
            else
            {
                switch (fieldName.ToLower())
                {
                    case "hospitalpast":
                    case "funeralrelation":
                    case "contactrelation":
                        fieldName += "1";
                        break;
                    default:
                        break;
                }
                int typeId = ReadFieldTypeID(fieldName);

                if (typeId == 19)
                {
                    checkboxCodedField.IsChecked = true;
                }
                else
                {
                    checkboxCodedField.IsChecked = false;
                }
            }
        }

        private int ReadFieldTypeID(string fieldName)
        {
            int typeId = -1;

            Query selectQuery = DataHelper.Database.CreateQuery("SELECT FieldTypeId FROM [metaFields] WHERE Name = @Name");
            selectQuery.Parameters.Add(new QueryParameter("@Name", System.Data.DbType.String, fieldName));
            DataTable dt = DataHelper.Database.Select(selectQuery);


            if (dt != null && dt.Rows.Count > 0)
            {
                typeId = Convert.ToInt32(dt.Rows[0][0].ToString());
            }
            return typeId;
        }

        private void checkboxCodedFields_Checked(object sender, RoutedEventArgs e)
        {
            if (checkboxCodedField.IsChecked == true)
            {
                tblockCodedNote.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                tblockCodedNote.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void checkbox_Checked(object sender, RoutedEventArgs e)
        {

            RadioButton chkBox = (RadioButton)sender;
            string chkBoxName = chkBox.Name;

            if (checkboxDistrictText.IsChecked == true && checkboxSCDDL.IsChecked == true)
            {
                MessageBox.Show("Invalid combination. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                checkboxDistrictText.IsChecked = false;
                checkboxSCDDL.IsChecked = false;
                return;
            }

            if (chkBoxName.Contains("SC") || chkBoxName.Contains("District"))
            {
                if (checkboxSCDDL.IsChecked == true || checkboxDistrictDDL.IsChecked == true)
                {
                    chkBoxName = "checkboxDistrictDDL";
                }
            }

            if (chkBoxName.Contains("SC"))
            {
                chkBoxName = chkBoxName.Replace("SC", "District");
            }


            string btnName = "btn" + chkBoxName.Replace("checkbox", "").Replace("DDL", "").Replace("Text", ""); ;
            Button btn = (Button)this.FindName(btnName);
            if (chkBox.IsChecked != null)
            {
                if (!chkBoxName.Contains("DDL"))
                {
                    btn.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    btn.Visibility = System.Windows.Visibility.Visible;
                }
            }
        }
        private void btnTransporterTypeEdit_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            gridAdminLocation.Visibility = System.Windows.Visibility.Collapsed;
            gridValueEditor.Visibility = System.Windows.Visibility.Visible;
        }
        private void btnCreatListFromExisting_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            spCreateList.Visibility = System.Windows.Visibility.Visible;
            dg.Visibility = System.Windows.Visibility.Collapsed;
            btnCreatListFromExisting.Visibility = System.Windows.Visibility.Collapsed;
            spBtnsOkCancel.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void btnCreate_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (listboxFieldItemSource == null)
            {
                MessageBox.Show("Value table must be selected before creating a list.");
                return;
            }
            DataTable dt = listboxFieldItemSource;
            dt.Columns[0].ColumnName = fieldName;

            dg.ItemsSource = null;
            dg.ItemsSource = dt.AsDataView();

            spCreateList.Visibility = System.Windows.Visibility.Collapsed;
            dg.Visibility = System.Windows.Visibility.Visible;
            btnCreatListFromExisting.Visibility = System.Windows.Visibility.Visible;
            spBtnsOkCancel.Visibility = System.Windows.Visibility.Visible;
        }

        private void btnCreateCancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            spCreateList.Visibility = System.Windows.Visibility.Collapsed;
            dg.Visibility = System.Windows.Visibility.Visible;
            btnCreatListFromExisting.Visibility = System.Windows.Visibility.Visible;
            spBtnsOkCancel.Visibility = System.Windows.Visibility.Visible;
        }

        public event EventHandler Closed;
        public bool isOK;

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            if (dg.Visibility == System.Windows.Visibility.Visible)
            {
                gridAdminLocation.Visibility = System.Windows.Visibility.Visible;
                gridValueEditor.Visibility = System.Windows.Visibility.Collapsed;
                dg.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                if (this.Closed != null)
                {
                    isOK = true;
                    Closed(this, new EventArgs());
                }
            }

        }


        private void listboxTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((ListBox)sender).SelectedItem == null)
            {
                return;
            }
            string selectedCodeTable = ((ListBox)sender).SelectedItem.ToString();
            if (selectedCodeTable != "codeDistrictSubCountyList")
            {
                listboxFieldItemSource = Database.GetTableData(selectedCodeTable);
                listboxField.ItemsSource = null;
                listboxField.DisplayMemberPath = listboxFieldItemSource.Columns[0].ColumnName;
                listboxField.ItemsSource = listboxFieldItemSource.AsDataView();
            }
            else
            {
                MessageBox.Show("The items of this table cannot be populated.");
            }

        }

    }
}
