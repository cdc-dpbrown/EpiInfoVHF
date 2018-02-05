using System;
using System.Collections.Generic;
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

namespace ContactTracing.CaseView.Controls.Printing
{
    /// <summary>
    /// Interaction logic for CaseReportFormPage2.xaml
    /// </summary>
    public partial class CaseReportFormPage2 : UserControl
    {
        public CaseReportFormPage2()
        {
            InitializeComponent();
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            CaseViewModel caseVM = (this.DataContext as CaseViewModel);

            if (caseVM != null)
            {
                txtCaseID.Text = caseVM.ID;
                txtCaseName.Text = caseVM.OtherNames + " " + caseVM.Surname;

                switch (caseVM.HadContact)
                {
                    case "1":
                        checkboxKnownContactYes.IsChecked = true;
                        break;
                    case "2":
                        checkboxKnownContactNo.IsChecked = true;
                        break;
                    case "3":
                        checkboxKnownContactUnk.IsChecked = true;
                        break;
                }

                tblockNameSoureCase1.Text = caseVM.ContactName1;
                tblockNameSoureCase2.Text = caseVM.ContactName2;
                tblockNameSoureCase3.Text = caseVM.ContactName3;

                tblockRelationToPatient1.Text = caseVM.ContactRelation1;
                tblockRelationToPatient2.Text = caseVM.ContactRelation2;
                tblockRelationToPatient3.Text = caseVM.ContactRelation3;

                tblockVillage1.Text = caseVM.ContactVillage1;
                tblockVillage2.Text = caseVM.ContactVillage2;
                tblockVillage3.Text = caseVM.ContactVillage3;

                tblockDistrict1.Text = caseVM.ContactDistrict1;
                tblockDistrict2.Text = caseVM.ContactDistrict2;
                tblockDistrict3.Text = caseVM.ContactDistrict3;

                dateHospFrom1.DataContext = caseVM.ContactStartDate1;
                dateHospFrom2.DataContext = caseVM.ContactStartDate2;
                dateHospFrom3.DataContext = caseVM.ContactStartDate3;

                dateHospTo1.DataContext = caseVM.ContactEndDate1;
                dateHospTo2.DataContext = caseVM.ContactEndDate2;
                dateHospTo3.DataContext = caseVM.ContactEndDate3;

                tblockContactTypes1.Text = caseVM.TypesOfContact1;
                tblockContactTypes2.Text = caseVM.TypesOfContact2;
                tblockContactTypes3.Text = caseVM.TypesOfContact3;

                switch (caseVM.ContactStatus1)
                {
                    case "1":
                        tblockDead1.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case "2":
                        tblockAlive1.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case "3":
                        tblockDead1.Visibility = System.Windows.Visibility.Visible;
                        tblockAlive1.Visibility = System.Windows.Visibility.Visible;
                        break;
                }

                switch (caseVM.ContactStatus2)
                {
                    case "1":
                        tblockDead2.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case "2":
                        tblockAlive2.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case "3":
                        tblockDead2.Visibility = System.Windows.Visibility.Visible;
                        tblockAlive2.Visibility = System.Windows.Visibility.Visible;
                        break;
                }

                switch (caseVM.ContactStatus3)
                {
                    case "1":
                        tblockDead3.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case "2":
                        tblockAlive3.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case "3":
                        tblockDead3.Visibility = System.Windows.Visibility.Visible;
                        tblockAlive3.Visibility = System.Windows.Visibility.Visible;
                        break;
                }


                if (caseVM.ContactDeathDate1.HasValue) tblockDateDeath1.Text = caseVM.ContactDeathDate1.Value.ToShortDateString();
                if (caseVM.ContactDeathDate2.HasValue) tblockDateDeath2.Text = caseVM.ContactDeathDate2.Value.ToShortDateString();
                if (caseVM.ContactDeathDate3.HasValue) tblockDateDeath3.Text = caseVM.ContactDeathDate3.Value.ToShortDateString();

                switch (caseVM.AttendFuneral)
                {
                    case "1":
                        checkboxAttendFuneralYes.IsChecked = true;
                        break;
                    case "2":
                        checkboxAttendFuneralNo.IsChecked = true;
                        break;
                    case "3":
                        checkboxAttendFuneralUnk.IsChecked = true;
                        break;
                }

                tblockName1.Text = caseVM.FuneralNameDeceased1;
                tblockName2.Text = caseVM.FuneralNameDeceased2;

                tblockRelationToPatientFuneral1.Text = caseVM.FuneralRelationDeceased1;
                tblockRelationToPatientFuneral2.Text = caseVM.FuneralRelationDeceased2;

                tblockVillageFuneral1.Text = caseVM.FuneralVillage1;
                tblockVillageFuneral2.Text = caseVM.FuneralVillage2;

                tblockDistrictFuneral1.Text = caseVM.FuneralDistrict1;
                tblockDistrictFuneral2.Text = caseVM.FuneralDistrict2;

                switch (caseVM.FuneralTouchBody1)
                {
                    case "1":
                        tblockParticipateYes1.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case "2":
                        tblockParticipateNo1.Visibility = System.Windows.Visibility.Visible;
                        break;
                }

                switch (caseVM.FuneralTouchBody2)
                {
                    case "1":
                        tblockParticipateYes2.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case "2":
                        tblockParticipateNo2.Visibility = System.Windows.Visibility.Visible;
                        break;
                }

                dateFuneralFrom1.DataContext = caseVM.FuneralStartDate1;
                dateFuneralFrom2.DataContext = caseVM.FuneralStartDate2;
                dateFuneralTo1.DataContext = caseVM.FuneralEndDate1;
                dateFuneralTo2.DataContext = caseVM.FuneralEndDate2;

                switch (caseVM.Travel)
                {
                    case "1":
                        checkboxQ3Yes.IsChecked = true;
                        break;
                    case "2":
                        checkboxQ3No.IsChecked = true;
                        break;
                    case "3":
                        checkboxQ3Unk.IsChecked = true;
                        break;
                }

                txtTravelDistrict.Text = caseVM.TravelDistrict;
                txtTravelVillage.Text = caseVM.TravelVillage;

                dateTravelFrom.DataContext = caseVM.TravelStartDate;
                dateTravelTo.DataContext = caseVM.TravelEndDate;

                switch (caseVM.HospitalBeforeIll)
                {
                    case "1":
                        checkboxQ4Yes.IsChecked = true;
                        break;
                    case "2":
                        checkboxQ4No.IsChecked = true;
                        break;
                    case "3":
                        checkboxQ4Unk.IsChecked = true;
                        break;
                }

                txtVisit1.Text = caseVM.HospitalBeforeIllPatient;
                dateHVisitFrom.DataContext = caseVM.HospitalBeforeIllStartDate;
                dateHVisitTo.DataContext = caseVM.HospitalBeforeIllEndDate;
                txtHFName.Text = caseVM.HospitalBeforeIllHospitalName;
                txtHFVillage.Text = caseVM.HospitalBeforeIllVillage;
                txtHFDistrict.Text = caseVM.HospitalBeforeIllDistrict;

                switch (caseVM.TraditionalHealer)
                {
                    case "1":
                        checkboxQ5Yes.IsChecked = true;
                        break;
                    case "2":
                        checkboxQ5No.IsChecked = true;
                        break;
                    case "3":
                        checkboxQ5Unk.IsChecked = true;
                        break;
                }

                txtNameOfHealer.Text = caseVM.TraditionalHealerName;
                txtHealerVillage.Text = caseVM.TraditionalHealerVillage;
                txtHealerDistrict.Text = caseVM.TraditionalHealerDistrict;
                dateHVisitHealer.DataContext = caseVM.TraditionalHealerDate;

                switch (caseVM.Animals)
                {
                    case "1":
                        checkboxQ6Yes.IsChecked = true;
                        break;
                    case "2":
                        checkboxQ6No.IsChecked = true;
                        break;
                    case "3":
                        checkboxQ6Unk.IsChecked = true;
                        break;
                }

                checkboxAnimalBats.IsChecked = caseVM.AnimalBats;
                checkboxAnimalBirds.IsChecked = caseVM.AnimalBirds;
                checkboxAnimalCows.IsChecked = caseVM.AnimalCows;
                checkboxAnimalOther.IsChecked = caseVM.AnimalOther;
                checkboxAnimalPigs.IsChecked = caseVM.AnimalPigs;
                checkboxAnimalPrimates.IsChecked = caseVM.AnimalPrimates;
                checkboxAnimalRodents.IsChecked = caseVM.AnimalRodents;

                checkboxAnimalBatsHealthy.IsChecked = caseVM.AnimalBatsStatus == "1" ? true : false;
                checkboxAnimalBirdsHealthy.IsChecked = caseVM.AnimalBirdsStatus == "1" ? true : false;
                checkboxAnimalCowsHealthy.IsChecked = caseVM.AnimalCowsStatus == "1" ? true : false;
                checkboxAnimalOtherHealthy.IsChecked = caseVM.AnimalOtherStatus == "1" ? true : false;
                checkboxAnimalPigsHealthy.IsChecked = caseVM.AnimalPigsStatus == "1" ? true : false;
                checkboxAnimalPrimatesHealthy.IsChecked = caseVM.AnimalPrimatesStatus == "1" ? true : false;
                checkboxAnimalRodentsHealthy.IsChecked = caseVM.AnimalRodentsStatus == "1" ? true : false;

                checkboxAnimalBatsDead.IsChecked = caseVM.AnimalBatsStatus == "2" ? true : false;
                checkboxAnimalBirdsDead.IsChecked = caseVM.AnimalBirdsStatus == "2" ? true : false;
                checkboxAnimalCowsDead.IsChecked = caseVM.AnimalCowsStatus == "2" ? true : false;
                checkboxAnimalOtherDead.IsChecked = caseVM.AnimalOtherStatus == "2" ? true : false;
                checkboxAnimalPigsDead.IsChecked = caseVM.AnimalPigsStatus == "2" ? true : false;
                checkboxAnimalPrimatesDead.IsChecked = caseVM.AnimalPrimatesStatus == "2" ? true : false;
                checkboxAnimalRodentsDead.IsChecked = caseVM.AnimalRodentsStatus == "2" ? true : false;

                txtOtherAnimalSpecify.Text = caseVM.AnimalOtherComment;

                switch (caseVM.BittenTick)
                {
                    case "1":
                        checkboxQ7Yes.IsChecked = true;
                        break;
                    case "2":
                        checkboxQ7No.IsChecked = true;
                        break;
                    case "3":
                        checkboxQ7Unk.IsChecked = true;
                        break;
                }

                txtInterviewerName.Text = caseVM.InterviewerName;
                txtInterviewerDistrict.Text = caseVM.InterviewerDistrict;
                txtInterviewerEmail.Text = caseVM.InterviewerEmail;
                txtInterviewerHealthFacility.Text = caseVM.InterviewerHealthFacility;
                txtInterviewerPhone.Text = caseVM.InterviewerPhone;
                txtInterviewerPosition.Text = caseVM.InterviewerPosition;

                txtProxyName.Text = caseVM.ProxyName;
                txtProxyRelation.Text = caseVM.ProxyRelation;

                switch (caseVM.InterviewerInfoProvidedBy)
                {
                    case "1":
                        checkboxPatient.IsChecked = true;
                        break;
                    case "2":
                        checkboxProxy.IsChecked = true;
                        break;
                }
            }
        }
    }
}
