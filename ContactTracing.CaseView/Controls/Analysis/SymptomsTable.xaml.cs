using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ContactTracing.ViewModel;
using Epi.Data;

namespace ContactTracing.CaseView.Controls.Analysis
{
    /// <summary>
    /// Interaction logic for SymptomsTable.xaml
    /// </summary>
    public partial class SymptomsTable : AnalysisOutputBase
    {
        public class Result
        {
            public string PresentFever;
            public string PresentVomiting;
            public string PresentDiarrhea;
            public string PresentFatigue;
            public string PresentAnorexia;
            public string PresentAbPain;
            public string PresentChestPain;
            public string PresentMusclePain;
            public string PresentJointPain;
            public string PresentHeadache;
            public string PresentCough;
            public string PresentDiffBreath;
            public string PresentDiffSwallow;
            public string PresentSoreThroat;
            public string PresentJaundice;
            public string PresentConj;
            public string PresentSkinRash;
            public string PresentHiccups;
            public string PresentPainEye;
            public string PresentComa;
            public string PresentConfused;
            public string PresentHemorrhagic;

            public string AbsentFever;
            public string AbsentVomiting;
            public string AbsentDiarrhea;
            public string AbsentFatigue;
            public string AbsentAnorexia;
            public string AbsentAbPain;
            public string AbsentChestPain;
            public string AbsentMusclePain;
            public string AbsentJointPain;
            public string AbsentHeadache;
            public string AbsentCough;
            public string AbsentDiffBreath;
            public string AbsentDiffSwallow;
            public string AbsentSoreThroat;
            public string AbsentJaundice;
            public string AbsentConj;
            public string AbsentSkinRash;
            public string AbsentHiccups;
            public string AbsentPainEye;
            public string AbsentComa;
            public string AbsentConfused;
            public string AbsentHemorrhagic;

            public string UnknownFever;
            public string UnknownVomiting;
            public string UnknownDiarrhea;
            public string UnknownFatigue;
            public string UnknownAnorexia;
            public string UnknownAbPain;
            public string UnknownChestPain;
            public string UnknownMusclePain;
            public string UnknownJointPain;
            public string UnknownHeadache;
            public string UnknownCough;
            public string UnknownDiffBreath;
            public string UnknownDiffSwallow;
            public string UnknownSoreThroat;
            public string UnknownJaundice;
            public string UnknownConj;
            public string UnknownSkinRash;
            public string UnknownHiccups;
            public string UnknownPainEye;
            public string UnknownComa;
            public string UnknownConfused;
            public string UnknownHemorrhagic;
        }

        private delegate void SetGridTextHandler(Result result);

        private EpiDataHelper DataHelper
        {
            get
            {
                return (this.DataContext as EpiDataHelper);
            }
        }

        public SymptomsTable()
        {
            InitializeComponent();
        }
        public void Compute()
        {
            BackgroundWorker computeWorker = new BackgroundWorker();
            computeWorker.DoWork += new DoWorkEventHandler(computeWorker_DoWork);
            computeWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(computeWorker_RunWorkerCompleted);
            computeWorker.RunWorkerAsync(this.DataHelper);
        }

        void SetGridText(Result result)
        {
            tblockPresentFever.Text = result.PresentFever.ToString();
            tblockPresentVomiting.Text = result.PresentVomiting.ToString();
            tblockPresentDiarrhea.Text = result.PresentDiarrhea.ToString();
            tblockPresentFatigue.Text = result.PresentFatigue.ToString();
            tblockPresentAnorexia.Text = result.PresentAnorexia.ToString();
            tblockPresentAbPain.Text = result.PresentAbPain.ToString();
            tblockPresentChestPain.Text = result.PresentChestPain.ToString();
            tblockPresentMusclePain.Text = result.PresentMusclePain.ToString();
            tblockPresentJointPain.Text = result.PresentJointPain.ToString();
            tblockPresentHeadache.Text = result.PresentHeadache.ToString();
            tblockPresentCough.Text = result.PresentCough.ToString();
            tblockPresentDiffBreath.Text = result.PresentDiffBreath.ToString();
            tblockPresentDiffSwallow.Text = result.PresentDiffSwallow.ToString();
            tblockPresentSoreThroat.Text = result.PresentSoreThroat.ToString();
            tblockPresentJaundice.Text = result.PresentJaundice.ToString();
            tblockPresentConj.Text = result.PresentConj.ToString();
            tblockPresentSkinRash.Text = result.PresentSkinRash.ToString();
            tblockPresentHiccups.Text = result.PresentHiccups.ToString();
            tblockPresentPainEye.Text = result.PresentPainEye.ToString();
            tblockPresentComa.Text = result.PresentComa.ToString();
            tblockPresentConfused.Text = result.PresentConfused.ToString();
            tblockPresentHemorrhagic.Text = result.PresentHemorrhagic.ToString();

            tblockAbsentFever.Text = result.AbsentFever.ToString();
            tblockAbsentVomiting.Text = result.AbsentVomiting.ToString();
            tblockAbsentDiarrhea.Text = result.AbsentDiarrhea.ToString();
            tblockAbsentFatigue.Text = result.AbsentFatigue.ToString();
            tblockAbsentAnorexia.Text = result.AbsentAnorexia.ToString();
            tblockAbsentAbPain.Text = result.AbsentAbPain.ToString();
            tblockAbsentChestPain.Text = result.AbsentChestPain.ToString();
            tblockAbsentMusclePain.Text = result.AbsentMusclePain.ToString();
            tblockAbsentJointPain.Text = result.AbsentJointPain.ToString();
            tblockAbsentHeadache.Text = result.AbsentHeadache.ToString();
            tblockAbsentCough.Text = result.AbsentCough.ToString();
            tblockAbsentDiffBreath.Text = result.AbsentDiffBreath.ToString();
            tblockAbsentDiffSwallow.Text = result.AbsentDiffSwallow.ToString();
            tblockAbsentSoreThroat.Text = result.AbsentSoreThroat.ToString();
            tblockAbsentJaundice.Text = result.AbsentJaundice.ToString();
            tblockAbsentConj.Text = result.AbsentConj.ToString();
            tblockAbsentSkinRash.Text = result.AbsentSkinRash.ToString();
            tblockAbsentHiccups.Text = result.AbsentHiccups.ToString();
            tblockAbsentPainEye.Text = result.AbsentPainEye.ToString();
            tblockAbsentComa.Text = result.AbsentComa.ToString();
            tblockAbsentConfused.Text = result.AbsentConfused.ToString();
            tblockAbsentHemorrhagic.Text = result.AbsentHemorrhagic.ToString();

            tblockUnknownFever.Text = result.UnknownFever.ToString();
            tblockUnknownVomiting.Text = result.UnknownVomiting.ToString();
            tblockUnknownDiarrhea.Text = result.UnknownDiarrhea.ToString();
            tblockUnknownFatigue.Text = result.UnknownFatigue.ToString();
            tblockUnknownAnorexia.Text = result.UnknownAnorexia.ToString();
            tblockUnknownAbPain.Text = result.UnknownAbPain.ToString();
            tblockUnknownChestPain.Text = result.UnknownChestPain.ToString();
            tblockUnknownMusclePain.Text = result.UnknownMusclePain.ToString();
            tblockUnknownJointPain.Text = result.UnknownJointPain.ToString();
            tblockUnknownHeadache.Text = result.UnknownHeadache.ToString();
            tblockUnknownCough.Text = result.UnknownCough.ToString();
            tblockUnknownDiffBreath.Text = result.UnknownDiffBreath.ToString();
            tblockUnknownDiffSwallow.Text = result.UnknownDiffSwallow.ToString();
            tblockUnknownSoreThroat.Text = result.UnknownSoreThroat.ToString();
            tblockUnknownJaundice.Text = result.UnknownJaundice.ToString();
            tblockUnknownConj.Text = result.UnknownConj.ToString();
            tblockUnknownSkinRash.Text = result.UnknownSkinRash.ToString();
            tblockUnknownHiccups.Text = result.UnknownHiccups.ToString();
            tblockUnknownPainEye.Text = result.UnknownPainEye.ToString();
            tblockUnknownComa.Text = result.UnknownComa.ToString();
            tblockUnknownConfused.Text = result.UnknownConfused.ToString();
            tblockUnknownHemorrhagic.Text = result.UnknownHemorrhagic.ToString();
        }

        void computeWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e != null && e.Result != null)
            {
                Result result = e.Result as Result;
                if (result != null)
                {
                    this.Dispatcher.BeginInvoke(new SetGridTextHandler(SetGridText), result);
                }
            }
        }

        void computeWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Result result = new Result();
            EpiDataHelper DataHelper = e.Argument as EpiDataHelper;

            if (DataHelper != null)
            {

                string format = "P1";

                double totalConfirmedProbableCases = (from caseVM in DataHelper.CaseCollection
                                                      where caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed || caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable
                                                      select caseVM).Count();

                Epi.Fields.DDLFieldOfCommentLegal feverField = DataHelper.Project.Views[ContactTracing.Core.Constants.CASE_FORM_NAME].Fields["Fever"] as Epi.Fields.DDLFieldOfCommentLegal;
                Epi.Fields.DDLFieldOfCommentLegal caseDefField = DataHelper.Project.Views[ContactTracing.Core.Constants.CASE_FORM_NAME].Fields["EpiCaseDef"] as Epi.Fields.DDLFieldOfCommentLegal;

                if (feverField != null && caseDefField != null && feverField.Page != null && caseDefField.Page != null)
                {
                    Epi.Page symptomPage = feverField.Page;
                    Epi.Page firstPage = caseDefField.Page;

                    IDbDriver db = DataHelper.Project.CollectedData.GetDatabase();
                    Query selectQuery = db.CreateQuery("SELECT * FROM " + symptomPage.TableName + " symp INNER JOIN " + firstPage.TableName + " fp ON symp.GlobalRecordId = fp.GlobalRecordId WHERE fp.EpiCaseDef = '1' OR fp.EpiCaseDef = '2'");
                    DataTable table = db.Select(selectQuery);

                    double count = (from DataRow row in table.Rows
                                    where row["Fever"].ToString() == "1"
                                    select row).Count();
                    result.PresentFever = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Vomiting"].ToString() == "1"
                             select row).Count();
                    result.PresentVomiting = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Diarrhea"].ToString() == "1"
                             select row).Count();
                    result.PresentDiarrhea = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Fatigue"].ToString() == "1"
                             select row).Count();
                    result.PresentFatigue = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Anorexia"].ToString() == "1"
                             select row).Count();
                    result.PresentAnorexia = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["AbdPain"].ToString() == "1"
                             select row).Count();
                    result.PresentAbPain = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["ChestPain"].ToString() == "1"
                             select row).Count();
                    result.PresentChestPain = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["MusclePain"].ToString() == "1"
                             select row).Count();
                    result.PresentMusclePain = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["JointPain"].ToString() == "1"
                             select row).Count();
                    result.PresentJointPain = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Headache"].ToString() == "1"
                             select row).Count();
                    result.PresentHeadache = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Cough"].ToString() == "1"
                             select row).Count();
                    result.PresentCough = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["DiffBreathe"].ToString() == "1"
                             select row).Count();
                    result.PresentDiffBreath = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["DiffSwallow"].ToString() == "1"
                             select row).Count();
                    result.PresentDiffSwallow = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["SoreThroat"].ToString() == "1"
                             select row).Count();
                    result.PresentSoreThroat = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Jaundice"].ToString() == "1"
                             select row).Count();
                    result.PresentJaundice = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Conjunctivitis"].ToString() == "1"
                             select row).Count();
                    result.PresentConj = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Rash"].ToString() == "1"
                             select row).Count();
                    result.PresentSkinRash = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Hiccups"].ToString() == "1"
                             select row).Count();
                    result.PresentHiccups = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["PainEyes"].ToString() == "1"
                             select row).Count();
                    result.PresentPainEye = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Unconscious"].ToString() == "1"
                             select row).Count();
                    result.PresentComa = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Confused"].ToString() == "1"
                             select row).Count();
                    result.PresentConfused = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Unexplainedbleeding"].ToString() == "1"
                             select row).Count();
                    result.PresentHemorrhagic = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";



















                    count = (from DataRow row in table.Rows
                             where row["Fever"].ToString() == "2"
                             select row).Count();
                    result.AbsentFever = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Vomiting"].ToString() == "2"
                             select row).Count();
                    result.AbsentVomiting = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Diarrhea"].ToString() == "2"
                             select row).Count();
                    result.AbsentDiarrhea = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Fatigue"].ToString() == "2"
                             select row).Count();
                    result.AbsentFatigue = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Anorexia"].ToString() == "2"
                             select row).Count();
                    result.AbsentAnorexia = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["AbdPain"].ToString() == "2"
                             select row).Count();
                    result.AbsentAbPain = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["ChestPain"].ToString() == "2"
                             select row).Count();
                    result.AbsentChestPain = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["MusclePain"].ToString() == "2"
                             select row).Count();
                    result.AbsentMusclePain = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["JointPain"].ToString() == "2"
                             select row).Count();
                    result.AbsentJointPain = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Headache"].ToString() == "2"
                             select row).Count();
                    result.AbsentHeadache = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Cough"].ToString() == "2"
                             select row).Count();
                    result.AbsentCough = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["DiffBreathe"].ToString() == "2"
                             select row).Count();
                    result.AbsentDiffBreath = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["DiffSwallow"].ToString() == "2"
                             select row).Count();
                    result.AbsentDiffSwallow = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["SoreThroat"].ToString() == "2"
                             select row).Count();
                    result.AbsentSoreThroat = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Jaundice"].ToString() == "2"
                             select row).Count();
                    result.AbsentJaundice = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Conjunctivitis"].ToString() == "2"
                             select row).Count();
                    result.AbsentConj = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Rash"].ToString() == "2"
                             select row).Count();
                    result.AbsentSkinRash = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Hiccups"].ToString() == "2"
                             select row).Count();
                    result.AbsentHiccups = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["PainEyes"].ToString() == "2"
                             select row).Count();
                    result.AbsentPainEye = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Unconscious"].ToString() == "2"
                             select row).Count();
                    result.AbsentComa = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Confused"].ToString() == "2"
                             select row).Count();
                    result.AbsentConfused = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Unexplainedbleeding"].ToString() == "2"
                             select row).Count();
                    result.AbsentHemorrhagic = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";















                    count = (from DataRow row in table.Rows
                             where row["Fever"].ToString() == "3" || String.IsNullOrEmpty(row["Fever"].ToString())
                             select row).Count();
                    result.UnknownFever = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Vomiting"].ToString() == "3" || String.IsNullOrEmpty(row["Vomiting"].ToString())
                             select row).Count();
                    result.UnknownVomiting = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Diarrhea"].ToString() == "3" || String.IsNullOrEmpty(row["Diarrhea"].ToString())
                             select row).Count();
                    result.UnknownDiarrhea = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Fatigue"].ToString() == "3" || String.IsNullOrEmpty(row["Fatigue"].ToString())
                             select row).Count();
                    result.UnknownFatigue = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Anorexia"].ToString() == "3" || String.IsNullOrEmpty(row["Anorexia"].ToString())
                             select row).Count();
                    result.UnknownAnorexia = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["AbdPain"].ToString() == "3" || String.IsNullOrEmpty(row["AbdPain"].ToString())
                             select row).Count();
                    result.UnknownAbPain = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["ChestPain"].ToString() == "3" || String.IsNullOrEmpty(row["ChestPain"].ToString())
                             select row).Count();
                    result.UnknownChestPain = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["MusclePain"].ToString() == "3" || String.IsNullOrEmpty(row["MusclePain"].ToString())
                             select row).Count();
                    result.UnknownMusclePain = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["JointPain"].ToString() == "3" || String.IsNullOrEmpty(row["JointPain"].ToString())
                             select row).Count();
                    result.UnknownJointPain = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Headache"].ToString() == "3" || String.IsNullOrEmpty(row["Headache"].ToString())
                             select row).Count();
                    result.UnknownHeadache = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Cough"].ToString() == "3" || String.IsNullOrEmpty(row["Cough"].ToString())
                             select row).Count();
                    result.UnknownCough = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["DiffBreathe"].ToString() == "3" || String.IsNullOrEmpty(row["DiffBreathe"].ToString())
                             select row).Count();
                    result.UnknownDiffBreath = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["DiffSwallow"].ToString() == "3" || String.IsNullOrEmpty(row["DiffSwallow"].ToString())
                             select row).Count();
                    result.UnknownDiffSwallow = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["SoreThroat"].ToString() == "3" || String.IsNullOrEmpty(row["SoreThroat"].ToString())
                             select row).Count();
                    result.UnknownSoreThroat = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Jaundice"].ToString() == "3" || String.IsNullOrEmpty(row["Jaundice"].ToString())
                             select row).Count();
                    result.UnknownJaundice = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Conjunctivitis"].ToString() == "3" || String.IsNullOrEmpty(row["Conjunctivitis"].ToString())
                             select row).Count();
                    result.UnknownConj = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Rash"].ToString() == "3" || String.IsNullOrEmpty(row["Rash"].ToString())
                             select row).Count();
                    result.UnknownSkinRash = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Hiccups"].ToString() == "3" || String.IsNullOrEmpty(row["Hiccups"].ToString())
                             select row).Count();
                    result.UnknownHiccups = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["PainEyes"].ToString() == "3" || String.IsNullOrEmpty(row["PainEyes"].ToString())
                             select row).Count();
                    result.UnknownPainEye = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Unconscious"].ToString() == "3" || String.IsNullOrEmpty(row["Unconscious"].ToString())
                             select row).Count();
                    result.UnknownComa = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Confused"].ToString() == "3" || String.IsNullOrEmpty(row["Confused"].ToString())
                             select row).Count();
                    result.UnknownConfused = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";

                    count = (from DataRow row in table.Rows
                             where row["Unexplainedbleeding"].ToString() == "3" || String.IsNullOrEmpty(row["Unexplainedbleeding"].ToString())
                             select row).Count();
                    result.UnknownHemorrhagic = count + " (" + (count / totalConfirmedProbableCases).ToString(format) + ")";



                    e.Result = result;
                }
            }
        }
    }
}
