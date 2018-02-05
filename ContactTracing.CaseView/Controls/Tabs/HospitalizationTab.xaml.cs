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
using ContactTracing.Controls;
using ContactTracing.Core;
using ContactTracing.ViewModel;
using Epi;
using Epi.Enter;
using Epi.Windows.Enter;

namespace ContactTracing.CaseView.Controls.Tabs
{
    /// <summary>
    /// Interaction logic for HospitalizationTab.xaml
    /// </summary>
    public partial class HospitalizationTab : UserControl
    {
        private Popup Popup;
        private double _height = 0;

        public HospitalizationTab()
        {
            InitializeComponent();
        }

        public double DgHeight
        {
            get
            {
                return this._height;
            }
            set
            {
                this._height = value;
                dgHospitalizations.Height = DgHeight;
            }
        }

        private EpiDataHelper DataHelper
        {
            get
            {
                return (this.DataContext) as EpiDataHelper;
            }
        }

        private void btnPrintHospitalizations_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext != null)
            {
                string baseFileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString("N");
                Dictionary<int, Boundry> ba = DataHelper.BoundaryAggregation;
                StringBuilder htmlBuilder = new StringBuilder();
                IMultiValueConverter dateConverter = new Converters.DateConverter();
                htmlBuilder.Append(ContactTracing.Core.Common.GetHtmlHeader().ToString());

                Dictionary<string, List<DailyCheckViewModel>> followUpDictionary = new Dictionary<string, List<DailyCheckViewModel>>();

                EpiDataHelper dataHelper = this.DataContext as EpiDataHelper;

                if (dataHelper != null)
                {
                    ListCollectionView lcv = dataHelper.IsolatedCollectionView as ListCollectionView;
                    if (lcv != null)
                    {
                        var query = from caseVM in (lcv).Cast<CaseViewModel>()
                                    where caseVM.CurrentHospital != null && caseVM.ID != null
                                    orderby caseVM.CurrentHospital, caseVM.ID
                                    select caseVM;
                        //group dailyCheck by string.Concat("<span style=\"font-weight: bold;\">Village:</span> ", dailyCheck.ContactVM.Village);

                        int rowsGenerated = 0;
                        bool firstPage = true;

                        htmlBuilder.AppendLine("<table width=\"100%\" style=\"border: 0px; padding: 0px; margin: 0px; clear: left; width:100%; \">");
                        htmlBuilder.AppendLine(" <tr style=\"border: 0px;\">");
                        htmlBuilder.AppendLine("  <td width=\"50%\" style=\"border: 0px;\">");
                        htmlBuilder.AppendLine("   <p style=\"font-size: 13pt; font-weight: bold; clear: left; text-decoration: underline;\">" + Properties.Settings.Default.HtmlPrintoutTitle + "</p>");
                        htmlBuilder.AppendLine("   <p style=\"font-size: 13pt; font-weight: bold; text-decoration: underline;\">" + Properties.Resources.HTMLIsoPatientsListHeading + "</p>");
                        htmlBuilder.AppendLine("   <p style=\"font-size: 13pt;\"><span style=\"font-weight: bold;\">" + Properties.Resources.HTMLDate + "</span> " + DateTime.Now.ToShortDateString() + "</p>");
                        htmlBuilder.AppendLine("  </td>");
                        htmlBuilder.AppendLine(" </tr>");
                        htmlBuilder.AppendLine("</table>");

                        htmlBuilder.AppendLine("<p>" + String.Format(Properties.Resources.HTMLIsoInstructions, "&#x2713;") + "</p>");

                        foreach (CaseViewModel caseVM in query)
                        {
                            if (rowsGenerated == 0)
                            {
                                htmlBuilder.AppendLine("<table style=\"width: 1200px; border: 4px solid black;\" align=\"left\">");
                                htmlBuilder.AppendLine("<thead>");
                                htmlBuilder.AppendLine("<tr style=\"border-top: 0px solid black;\">");

                                if (ApplicationViewModel.Instance.CurrentRegion == Core.Enums.RegionEnum.USA)
                                {
                                    htmlBuilder.AppendLine("<th style=\"width: 110px;\">" + Properties.Resources.ColHeaderOriginalID + "</th>");
                                }
                                else
                                {
                                    htmlBuilder.AppendLine("<th style=\"width: 80px;\">" + Properties.Resources.ColHeaderID + "</th>");
                                    htmlBuilder.AppendLine("<th style=\"width: 30px;\">" + Properties.Resources.HTMLColHeaderOtherID + "</th>");
                                } 

                                htmlBuilder.AppendLine("<th style=\"width: 110px;\">" + Properties.Resources.ColHeaderSurname + "</th>");
                                htmlBuilder.AppendLine("<th style=\"width: 130px;\">" + Properties.Resources.ColHeaderOtherNames + "</th>");
                                htmlBuilder.AppendLine("<th style=\"width: 110px;\">" + Properties.Resources.ColHeaderEpiCaseDef + "</th>");
                                htmlBuilder.AppendLine("<th style=\"width: 15px;\">" + Properties.Resources.HTMLColHeaderSexNarrow + "</th>");
                                htmlBuilder.AppendLine("<th style=\"width: 15px;\">" + Properties.Resources.HTMLColHeaderAgeNarrow + "</th>");
                                htmlBuilder.AppendLine("<th style=\"width: 100px;\">" + Properties.Resources.ColHeaderHealthFacility + "</th>");
                                htmlBuilder.AppendLine("<th style=\"width: 40px;\">" + Properties.Resources.ColHeaderOnsetDate + "</th>");
                                htmlBuilder.AppendLine("<th style=\"width: 40px;\">" + Properties.Resources.ColHeaderIsoAdmittedAlt + "</th>");
                                htmlBuilder.AppendLine("<th style=\"width: 40px;\">" + Properties.Resources.ColHeaderDateLastSampleCollected + "</th>");
                                htmlBuilder.AppendLine("<th style=\"width: 60px;\">" + Properties.Resources.ColHeaderPCRResultLast + "</th>");
                                htmlBuilder.AppendLine("<th style=\"width: 40px;\">" + Properties.Resources.HTMLColHeaderCollectSampleToday + "</th>");
                                htmlBuilder.AppendLine("<th style=\"width: 40px;\">" + Properties.Resources.HTMLColHeaderDischarged + "</th>");
                                htmlBuilder.AppendLine("<th style=\"width: 40px;\">" + Properties.Resources.HTMLColHeaderDateDischarge + "</th>");
                                htmlBuilder.AppendLine("<th style=\"width: 40px;\">" + Properties.Resources.HTMLColHeaderDied + "</th>");
                                htmlBuilder.AppendLine("<th style=\"width: 40px;\">" + Properties.Resources.ColHeaderDeathDate + "</th>");
                                //htmlBuilder.AppendLine("<th style=\"width: 110px; border-right: 4px solid black;\">" + Properties.Resources.ColHeaderNotes + "</th>");
                                htmlBuilder.AppendLine("</tr>");
                                htmlBuilder.AppendLine("</thead>");
                                htmlBuilder.AppendLine("<tbody>");
                            }

                            htmlBuilder.AppendLine("<tr style=\"border-bottom: 1px solid black;\">");

                            if (ApplicationViewModel.Instance.CurrentRegion != Core.Enums.RegionEnum.USA)
                            {
                                htmlBuilder.AppendLine("<td>" + caseVM.ID + "</td>");
                            }
                            htmlBuilder.AppendLine("<td>" + caseVM.OriginalID + "</td>");
                            htmlBuilder.AppendLine("<td>" + Core.Common.TruncHTMLCell(caseVM.Surname, 13) + "</td>");
                            htmlBuilder.AppendLine("<td>" + Core.Common.TruncHTMLCell(caseVM.OtherNames, 20) + "</td>");

                            IValueConverter caseClassconverter = new Converters.EpiCaseClassificationConverter();
                            htmlBuilder.AppendLine("<td>" + caseClassconverter.Convert(caseVM.EpiCaseDef, null, null, null).ToString() + "</td>");
                            //htmlBuilder.AppendLine("<td style=\"text-align: center;\">" + caseVM.GenderAbbreviation + "</td>");

                            if (caseVM.Gender.Equals(Core.Enums.Gender.Male))
                            {
                                htmlBuilder.AppendLine("<td style=\"text-align: center;\">" + Properties.Resources.MaleSymbol + "</td>");
                            }
                            else if (caseVM.Gender.Equals(Core.Enums.Gender.Female))
                            {
                                htmlBuilder.AppendLine("<td style=\"text-align: center;\">" + Properties.Resources.FemaleSymbol + "</td>");
                            }
                            else
                            {
                                htmlBuilder.AppendLine("<td>&nbsp;</td>");
                            }

                            if (caseVM.AgeYears.HasValue)
                            {
                                htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + caseVM.AgeYears + "</td>");
                            }
                            else
                            {
                                htmlBuilder.AppendLine("<td>&nbsp;</td>");
                            }

                            htmlBuilder.AppendLine("<td>" + Core.Common.TruncHTMLCell(caseVM.CurrentHospital, 20) + "</td>");

                            if (caseVM.DateOnset.HasValue)
                            {
                                string[] parmsValues = { caseVM.DateOnset.Value.ToString(), DataHelper.ApplicationCulture };
                                var dateonset = dateConverter.Convert(parmsValues, null, null, null);
                                htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + dateonset + "</td>");

                                //htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + caseVM.DateOnset.Value.ToString("d/M/yy") + "</td>");
                            }
                            else
                            {
                                htmlBuilder.AppendLine("<td>&nbsp;</td>");
                            }

                            if (caseVM.DateIsolationCurrent.HasValue)
                            {
                                string[] parmsValues = { caseVM.DateIsolationCurrent.Value.ToString(), DataHelper.ApplicationCulture };
                                var dateisolation = dateConverter.Convert(parmsValues, null, null, null);
                                htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + dateisolation + "</td>");
                                //htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + caseVM.DateIsolationCurrent.Value.ToString("d/M/yy") + "</td>");

                            }
                            else
                            {
                                htmlBuilder.AppendLine("<td>&nbsp;</td>");
                            }

                            if (caseVM.DateLastLabSampleCollected.HasValue)
                            {
                                string[] parmsValues = { caseVM.DateLastLabSampleCollected.Value.ToString(), DataHelper.ApplicationCulture };
                                var datesamplecollected = dateConverter.Convert(parmsValues, null, null, null);
                                htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + datesamplecollected + "</td>");
                                //htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + caseVM.DateLastLabSampleCollected.Value.ToString("d/M/yy") + "</td>");

                            }
                            else
                            {
                                htmlBuilder.AppendLine("<td>&nbsp;</td>");
                            }

                            htmlBuilder.AppendLine("<td>" + caseVM.LastSamplePCRResult + "</td>");
                            htmlBuilder.AppendLine("<td></td>");
                            htmlBuilder.AppendLine("<td></td>");
                            htmlBuilder.AppendLine("<td></td>");
                            htmlBuilder.AppendLine("<td></td>");
                            htmlBuilder.AppendLine("<td></td>");
                            //htmlBuilder.AppendLine("<td></td>");

                            rowsGenerated++;

                            if (firstPage && rowsGenerated == 22)
                            {
                                GenerateHtmlFooter(htmlBuilder);
                                rowsGenerated = 0;
                                firstPage = false;
                            }
                            else if (!firstPage && rowsGenerated == 28)
                            {
                                GenerateHtmlFooter(htmlBuilder);
                                rowsGenerated = 0;
                            }
                        }

                        if (firstPage && rowsGenerated % 28 != 0)
                        {
                            GenerateHtmlFooter(htmlBuilder);
                            rowsGenerated = 0;
                            firstPage = true;
                        }
                        else if (!firstPage && rowsGenerated % 34 != 0)
                        {
                            GenerateHtmlFooter(htmlBuilder);
                            rowsGenerated = 0;
                            firstPage = true;
                        }

                        string fileName = baseFileName + ".html";

                        System.IO.FileStream fstream = System.IO.File.OpenWrite(fileName);
                        System.IO.StreamWriter sw = new System.IO.StreamWriter(fstream);
                        sw.WriteLine(htmlBuilder.ToString());
                        sw.Close();
                        sw.Dispose();

                        if (!string.IsNullOrEmpty(fileName))
                        {
                            System.Diagnostics.Process proc = new System.Diagnostics.Process();
                            proc.StartInfo.FileName = "\"" + fileName + "\"";
                            proc.StartInfo.UseShellExecute = true;
                            proc.Start();
                        }
                    }
                }
            }
        }

        private void GenerateHtmlFooter(StringBuilder htmlBuilder)
        {
            htmlBuilder.Append("</tbody>");
            htmlBuilder.Append("</table>");
            //htmlBuilder.Append("<p style=\"clear: left; font-size: 8pt; margin-top: 4px;\">* Contact is past 21st day of follow-up but was not seen on the 21st day.  Please check on what the contact’s health status was on their 21st day of follow-up.</p>");
            htmlBuilder.Append("<div style=\"page-break-before:always;\" />");
        }        

        private void EditCase()
        {
            CaseViewModel caseVM = ((CaseViewModel)dgHospitalizations.SelectedItem);
            if (caseVM.IsLocked)
            {
                MessageBox.Show("Either this case or contacts of this case are locked for editing. Please wait until other users have released this lock before proceeding.", "Record locked", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            DataHelper.SendMessageForLockCase(caseVM);

            Epi.Enter.EnterUIConfig uiConfig = Core.Common.GetCaseConfig(DataHelper.CaseForm, DataHelper.LabForm);
            Epi.Windows.Enter.EnterMainForm emf = new Epi.Windows.Enter.EnterMainForm(DataHelper.Project, DataHelper.CaseForm, uiConfig);

            int uniqueKey = caseVM.UniqueKey;

            emf.LoadRecord(uniqueKey);
            emf.RecordSaved += new SaveRecordEventHandler(emfCases_RecordSaved);
            emf.ShowDialog();
            emf.RecordSaved -= new SaveRecordEventHandler(emfCases_RecordSaved);

            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Threading.Thread.CurrentThread.CurrentCulture;

            DataHelper.SendMessageForUnlockCase(caseVM);
        }

        private void dgHospitalizations_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //if (DataHelper.Country != "Liberia" && DataHelper.Country != "Sierra Leone" && DataHelper.Country != "Guinea")
            if (!DataHelper.IsShortForm) //17040
                EditCase();
            else
            {
                CaseViewModel caseVM = dgHospitalizations.SelectedItem as CaseViewModel;

                if (caseVM != null)
                {
                    if (caseVM.IsLocked == false)
                    {
                        if (DataHelper.ToggleShortCaseReportFormCommand.CanExecute(null))
                        {
                            DataHelper.ToggleShortCaseReportFormCommand.Execute(caseVM);//VHF-260
                        }
                    }
                    else
                    {
                        MessageBox.Show("Either this case or contacts of this case are locked for editing. Please wait until other users have released this lock before proceeding.", "Record locked", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                }
            }
        }

        public void emfCases_RecordSaved(object sender, SaveRecordEventArgs e)
        {
            string caseGuid = e.RecordGuid;
            if (e.Form == DataHelper.CaseForm)
            {
                EpiDataHelper dataHelper = this.DataContext as EpiDataHelper;
                if (dataHelper != null)
                {
                    dataHelper.UpdateOrAddCase.Execute(caseGuid);
                }
            }
            else if (e.Form == DataHelper.LabForm || e.Form.Name.Equals(DataHelper.LabForm.Name))
            {
                try
                {
                    caseGuid = DataHelper.GetCaseGuidForLabRecord(e.RecordGuid);
                }
                catch (Exception ex)
                {
                    Epi.Windows.MsgBox.ShowException(ex);
                    return;
                }

                if (!String.IsNullOrEmpty(caseGuid))
                {
                    try
                    {
                        DataHelper.UpdateOrAddCase.Execute(caseGuid);
                        DataHelper.SendMessageForUpdateCase(caseGuid);
                    }
                    catch (Exception ex)
                    {
                        Epi.Windows.MsgBox.ShowException(ex);
                        return;
                    }
                }
            }
        }

        private void outcomeFormPrint_Click(object sender, RoutedEventArgs e)
        {
            System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
            string fileName = "\\Projects\\VHF\\Resources\\OutcomeForm." + System.Threading.Thread.CurrentThread.CurrentUICulture.ToString() + ".pdf";

            string fullFileName = System.IO.Path.GetDirectoryName(a.Location) + fileName;
            if (System.IO.File.Exists(fullFileName))
            {
                string commandText = fullFileName; // System.IO.Path.GetDirectoryName(a.Location) + fileName;// "\\Projects\\VHF\\ContactForm.docx";

                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = commandText;
                proc.StartInfo.UseShellExecute = true;
                proc.Start();
            }
            else
            {
                System.Windows.MessageBox.Show("This PDF file could not be found. Please contact the application developer.");
            }
        }

        void formViewer_Closed(object sender, EventArgs e)
        {
            if (Popup != null)
            {
                Popup.Close();
                Popup = null;
            }
        }

        private void carc_PrintFullFormRequested(object sender, EventArgs e)
        {
            if (dgHospitalizations.SelectedItems.Count >= 1 && Popup == null)
            {
                //docViewer.Visibility = System.Windows.Visibility.Visible;

                Grid grid = this.Parent as Grid;
                if (grid != null)
                {
                    CaseViewModel caseVM = dgHospitalizations.SelectedItem as CaseViewModel;
                    if (caseVM != null)
                    {
                        Popup = new Popup();
                        Popup.Parent = grid;

                        CaseReportFormViewer fullFormViewer = new CaseReportFormViewer();
                        fullFormViewer.Width = this.ActualWidth - 50;
                        fullFormViewer.MinWidth = 790;
                        fullFormViewer.MaxWidth = 1050;
                        fullFormViewer.Height = this.ActualHeight + 50;
                        fullFormViewer.Closed += new EventHandler(formViewer_Closed);

                        //DataHelper.LoadOutcomeCaseData(caseVM);
                        caseVM.Load();
                        fullFormViewer.DataContext = caseVM;

                        Popup.Content = fullFormViewer;
                        Popup.Show();
                    }
                }
                else
                {
                    throw new InvalidOperationException("Missing grid element.");
                }
            }
        }

        private void carc_PrintOutcomeFormRequested(object sender, EventArgs e)
        {
            if (dgHospitalizations.SelectedItems.Count >= 1 && Popup == null)
            {
                //docViewer.Visibility = System.Windows.Visibility.Visible;

                Grid grid = this.Parent as Grid;
                if (grid != null)
                {
                    Popup = new Popup();
                    Popup.Parent = grid;

                    PatientOutcomeFormViewer formViewer = new PatientOutcomeFormViewer();
                    formViewer.Width = this.ActualWidth - 50;
                    formViewer.MinWidth = 790;
                    formViewer.MaxWidth = 1050;
                    formViewer.Height = this.ActualHeight + 50;
                    formViewer.Closed += new EventHandler(formViewer_Closed);

                    CaseViewModel caseVM = dgHospitalizations.SelectedItem as CaseViewModel;
                    //DataHelper.LoadOutcomeCaseData(caseVM);
                    caseVM.Load();
                    formViewer.DataContext = caseVM;

                    Popup.Content = formViewer;
                    Popup.Show();
                }
                else
                {
                    throw new InvalidOperationException("Missing grid element.");
                }
            }
        }

        private void CaseActionsRowControl_ListLabSamplesRequested(object sender, EventArgs e)
        {
            if (Popup == null && sender != null)
            {
                CaseActionsRowControl carc = sender as CaseActionsRowControl;
                if (carc != null && carc.DataContext != null)
                {
                    CaseViewModel caseVM = carc.DataContext as CaseViewModel;

                    if (caseVM != null)
                    {
                        Grid grid = this.Parent as Grid;

                        if (grid != null)
                        {
                            Popup = new Popup();
                            Popup.Parent = grid;

                            LabRecordsForCase labRecordsPanel = new LabRecordsForCase(caseVM);
                            labRecordsPanel.Width = this.ActualWidth - 50;
                            labRecordsPanel.MinWidth = 790;
                            labRecordsPanel.MaxWidth = 1900;
                            labRecordsPanel.Height = this.ActualHeight - 100;
                            labRecordsPanel.DataContext = this.DataContext;

                            DataHelper.PopulateLabRecordsForCase.Execute(caseVM);
                            //labRecordsPanel.Closed += new EventHandler(labRecordsPanel_Closed);
                            labRecordsPanel.Closed += formViewer_Closed;

                            //DataHelper.LoadExtendedCaseData(caseVM);
                            //labRecordsPanel.DataContext = caseVM;

                            Popup.Content = labRecordsPanel;
                            Popup.Show();
                        }
                        else
                        {
                            throw new InvalidOperationException("Missing grid element in ListLabSamples");
                        }
                    }
                }
            }
        }
    }
}
