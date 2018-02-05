using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
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
using System.Xml;
using Epi;
using Epi.Data;
using Epi.Fields;
using ContactTracing.Core;
using ContactTracing.ViewModel;

namespace ContactTracing.LabView
{
    public partial class MainWindow : Window
    {
        private delegate void BeginLoadProjectHandler(VhfProject project);

        private CaseViewModel CurrentCase
        {
            get
            {
                if (dgCases.SelectedItem != null)
                {
                    return (dgCases.SelectedItem as CaseViewModel);
                }
                else
                {
                    return null;
                }
            }
        }
        private View CaseForm { get; set; }
        private View ContactForm { get; set; }
        private Project Project { get; set; }
        private double MaxHeight { get; set; }
        private bool ShowContactManagementCases { get; set; }
        private bool ShowContactManagementChart { get; set; }

        private LabDataHelper DataHelper
        {
            get
            {
                return ((this.DataContext) as LabDataHelper);
            }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnAddCase_Click(object sender, RoutedEventArgs e)
        {
            Epi.Enter.EnterUIConfig uiConfig = GetCaseConfig();
            Epi.Windows.Enter.EnterMainForm emf = new Epi.Windows.Enter.EnterMainForm(DataHelper.Project, DataHelper.CaseForm, uiConfig);

            emf.RecordSaved += new SaveRecordEventHandler(emfCases_RecordSaved);

            emf.ShowDialog();
            emf.RecordSaved -= new SaveRecordEventHandler(emfCases_RecordSaved);
        }

        private void btnDeleteCase_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.DialogResult result = Epi.Windows.MsgBox.ShowQuestion("Are you sure you want to delete this record? This action cannot be un-done.");
            if (result.Equals(System.Windows.Forms.DialogResult.Yes))
            {
                Dialogs.AuthCodeDialog authDialog = new Dialogs.AuthCodeDialog(ContactTracing.Core.Constants.AUTH_CODE);
                System.Windows.Forms.DialogResult authResult = authDialog.ShowDialog();
                if (authResult == System.Windows.Forms.DialogResult.OK)
                {
                    if (authDialog.IsAuthorized)
                    {
                        ((this.DataContext) as LabDataHelper).DeleteLabResult.Execute((dgCases.SelectedItem as LabResultViewModel));
                    }
                }
            }
        }

        private void btnEditCase_Click(object sender, RoutedEventArgs e)
        {
            EditCase();
        }

        void emfCases_RecordSaved(object sender, SaveRecordEventArgs e)
        {
            string labGuid = e.RecordGuid;
            if (e.Form == DataHelper.LabForm)
            {
                LabDataHelper dataHelper = this.DataContext as LabDataHelper;
                if (dataHelper != null)
                {
                    dataHelper.UpdateOrAddLabResult.Execute(labGuid);
                }
            }
            else if (e.Form == DataHelper.CaseForm)
            {
                foreach (string guid in DataHelper.GetLabGuidsForCaseGuid(e.RecordGuid))
                {
                    LabDataHelper dataHelper = this.DataContext as LabDataHelper;
                    if (dataHelper != null)
                    {
                        dataHelper.UpdateOrAddLabResult.Execute(guid);
                    }
                }
            }
            //if (e.Form == DataHelper.CaseForm)
            //{
            //    ((this.DataContext) as LabDataHelper).UpdateOrAddCase.Execute(caseGuid);
            //}
        }

        private void btnExportCases_Click(object sender, RoutedEventArgs e)
        {
            Dialogs.AuthCodeDialog authDialog = new Dialogs.AuthCodeDialog(ContactTracing.Core.Constants.AUTH_CODE);
            System.Windows.Forms.DialogResult authResult = authDialog.ShowDialog();
            if (authResult == System.Windows.Forms.DialogResult.OK)
            {
                if (authDialog.IsAuthorized)
                {
                    ExportCases(true);
                }
                else
                {
                    MessageBox.Show("This action is unauthorized.", "Unauthorized access", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportCases(bool exportFull = true)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();

            dlg.DefaultExt = ".csv"; // Default file extension
            dlg.Filter = "Comma separated values file|*.csv"; // Filter files by extension 

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results 
            if (result == true)
            {
                if (DataHelper.ExportCasesWithLabData(dlg.FileName, exportFull))
                {
                    MessageBox.Show("Export completed successfully. File written to:\n" + dlg.FileName, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Export failed.", "Fail", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
        }

        private void btnExportForEpi_Click(object sender, RoutedEventArgs e)
        {
            if (btnExportForEpi.ContextMenu != null)
            {
                btnExportForEpi.ContextMenu.PlacementTarget = btnExportForEpi;
                btnExportForEpi.ContextMenu.IsOpen = true;
            }

            e.Handled = true;
            return;
        }

        private bool IsAuthorized()
        {
            bool authorized = false;

            if (System.Windows.Forms.Control.ModifierKeys == System.Windows.Forms.Keys.Shift)
            {
                ContactTracing.LabView.Dialogs.AuthCodeDialog authDialog = new Dialogs.AuthCodeDialog(ContactTracing.Core.Constants.AUTH_CODE);
                System.Windows.Forms.DialogResult authResult = authDialog.ShowDialog();
                if (authResult == System.Windows.Forms.DialogResult.OK)
                {
                    if (authDialog.IsAuthorized)
                    {
                        authorized = true;
                    }
                }
            }

            return authorized;
        }

        private void btnExportForEpiLastTwo_Click(object sender, RoutedEventArgs e) 
        {
            bool allColumns = IsAuthorized();
            DateTime startDate = DateTime.Today.AddDays(-2);
            startDate = new DateTime(startDate.Year, startDate.Month, startDate.Day);
            ExportForEpi(allColumns, startDate);
        }

        private void btnExportForEpiAll_Click(object sender, RoutedEventArgs e) 
        {
            bool allColumns = IsAuthorized();
            DateTime startDate = DateTime.MinValue;
            ExportForEpi(allColumns, startDate);
        }

        private void ExportForEpi(bool allColumns, DateTime startDate)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();

            dlg.DefaultExt = ".csv"; // Default file extension
            dlg.Filter = "Data package file|*.pkg7"; // Filter files by extension 

            int count = (from lab in DataHelper.LabResultCollection
                         where String.IsNullOrEmpty(lab.FieldLabSpecimenID)
                         select lab).Count();

            if (count > 0)
            {
                MessageBox.Show("Lab data cannot be exported because some lab records are missing field lab specimen ID values. Please fill in any missing field lab specimen ID values.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results 
            if (result == true)
            {
                string password = String.Empty;

                try
                {
                    ContactTracing.ImportExport.XmlLabDataPackager xmlDP = new ContactTracing.ImportExport.XmlLabDataPackager(DataHelper.CaseForm, dlg.SafeFileName.Replace(".pkg7", String.Empty));
                    //xmlDP.GridColumnsToNull = gridColumnsToNull;

                    List<string> fieldsToNull = new List<string>();
                    //foreach (Field field in DataHelper.CaseForm.Fields)
                    //{
                    //    if (field is IDataField && field.Name != "ID" && field.Name != "FinalLabClass")
                    //    {
                    //        fieldsToNull.Add(field.Name);
                    //    }
                    //}

                    xmlDP.FieldsToNull.Add(DataHelper.CaseForm.Name, fieldsToNull);

                    if (!allColumns)
                    {
                        List<string> labFieldsToNull = new List<string>();

                        labFieldsToNull.Add("SUDVNPCT");
                        labFieldsToNull.Add("SUDVCT2");
                        labFieldsToNull.Add("SUDVAgTiter");
                        labFieldsToNull.Add("SUDVIgMTiter");
                        labFieldsToNull.Add("SUDVIgGTiter");
                        labFieldsToNull.Add("SUDVAgSumOD");
                        labFieldsToNull.Add("SUDVIgMSumOD");
                        labFieldsToNull.Add("SUDVIgGSumOD");

                        labFieldsToNull.Add("BDBVNPCT");
                        labFieldsToNull.Add("BDBVVP40CT");
                        labFieldsToNull.Add("BDBVAgTiter");
                        labFieldsToNull.Add("BDBVIgMTiter");
                        labFieldsToNull.Add("BDBVIgGTiter");
                        labFieldsToNull.Add("BDBVAgSumOD");
                        labFieldsToNull.Add("BDBVIgMSumOD");
                        labFieldsToNull.Add("BDBVIgGSumOD");

                        labFieldsToNull.Add("MARVPolCT");
                        labFieldsToNull.Add("MARVVP40CT");
                        labFieldsToNull.Add("MARVAgTiter");
                        labFieldsToNull.Add("MARVIgMTiter");
                        labFieldsToNull.Add("MARVIgGTiter");
                        labFieldsToNull.Add("MARVAgSumOD");
                        labFieldsToNull.Add("MARVIgMSumOD");
                        labFieldsToNull.Add("MARVIgGSumOD");

                        labFieldsToNull.Add("EBOVCT1");
                        labFieldsToNull.Add("EBOVCT2");
                        labFieldsToNull.Add("EBOVAgTiter");
                        labFieldsToNull.Add("EBOVIgMTiter");
                        labFieldsToNull.Add("EBOVIgGTiter");
                        labFieldsToNull.Add("EBOVAgSumOD");
                        labFieldsToNull.Add("EBOVIgMSumOD");
                        labFieldsToNull.Add("EBOVIgGSumOD");

                        labFieldsToNull.Add("RVFCT1");
                        labFieldsToNull.Add("RVFCT2");
                        labFieldsToNull.Add("RVFAgTiter");
                        labFieldsToNull.Add("RVFIgMTiter");
                        labFieldsToNull.Add("RVFIgGTiter");
                        labFieldsToNull.Add("RVFAgSumOD");
                        labFieldsToNull.Add("RVFIgMSumOD");
                        labFieldsToNull.Add("RVFIgGSumOD");

                        labFieldsToNull.Add("CCHFCT1");
                        labFieldsToNull.Add("CCHFCT2");
                        labFieldsToNull.Add("CCHFAgTiter");
                        labFieldsToNull.Add("CCHFIgMTiter");
                        labFieldsToNull.Add("CCHFIgGTiter");
                        labFieldsToNull.Add("CCHFAgSumOD");
                        labFieldsToNull.Add("CCHFIgMSumOD");
                        labFieldsToNull.Add("CCHFIgGSumOD");

                        labFieldsToNull.Add("LASCT1");
                        labFieldsToNull.Add("LASCT2");
                        labFieldsToNull.Add("LASAgTiter");
                        labFieldsToNull.Add("LASIgMTiter");
                        labFieldsToNull.Add("LASIgGTiter");
                        labFieldsToNull.Add("LASAgSumOD");
                        labFieldsToNull.Add("LASIgMSumOD");
                        labFieldsToNull.Add("LASIgGSumOD");

                        xmlDP.FieldsToNull.Add(DataHelper.LabForm.Name, labFieldsToNull);
                    }

                    Epi.ImportExport.Filters.RowFilters filters = new Epi.ImportExport.Filters.RowFilters(DataHelper.Project.CollectedData.GetDatabase());
                    filters.Add(new Epi.ImportExport.DateRowFilterCondition("[LastSaveTime] >= @LastSaveTime", "LastSaveTime", "@LastSaveTime", startDate));
                    xmlDP.Filters = new Dictionary<string, Epi.ImportExport.Filters.RowFilters>();
                    xmlDP.Filters.Add(DataHelper.CaseForm.Name, filters);

                    //CallbackSetupProgressBar(100);

                    //Dictionary<string, Epi.ImportExport.Filters.RowFilters> filters = new Dictionary<string, Epi.ImportExport.Filters.RowFilters>();
                    //filters.Add(CaseForm.Name, new Epi.ImportExport.Filters.RowFilters(Database));
                    //foreach (IRowFilterCondition rfc in rowFilterConditions)
                    //{
                    //    filters[FormName].Add(rfc);
                    //}
                    //xmlDP.Filters = filters;
                    //xmlDP.StatusChanged += new UpdateStatusEventHandler(CallbackSetStatusMessage);
                    //xmlDP.UpdateProgress += new SetProgressBarDelegate(CallbackSetProgressBar);
                    //xmlDP.ResetProgress += new SimpleEventHandler(CallbackResetProgressBar);
                    XmlDocument package = xmlDP.PackageForm();
                    package.XmlResolver = null;
                    string fileName = dlg.FileName;

                    if (true /*AppendTimestamp*/)
                    {
                        DateTime dt = DateTime.UtcNow;
                        string dateDisplayValue = string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0:s}", dt);
                        dateDisplayValue = dateDisplayValue.Replace(':', '-'); // The : must be replaced otherwise the encryption fails
                        //fileName = PackageName + "_" + dateDisplayValue;
                    }

                    Epi.ImportExport.ProjectPackagers.ExportInfo exportInfo = xmlDP.ExportInfo;
                    foreach (KeyValuePair<View, int> kvp in exportInfo.RecordsPackaged)
                    {
                        //CallbackAddStatusMessage("Form " + kvp.Key.Name + ": " + kvp.Value + " records packaged.");
                    }

                    // TODO: Remove this before release! This output is for testing purposes only!
                    //package.Save(@PackagePath + "\\" + fileName + ".xml");
                    package.Save(fileName.Replace(".pkg7", ".xml"));

                    //CallbackSetStatusMessage("Compressing package...");
                    string compressedText = Epi.ImportExport.ImportExportHelper.Zip(package.OuterXml);
                    compressedText = "[[EPIINFO7_DATAPACKAGE]]" + compressedText;

                    //CallbackSetStatusMessage("Encrypting package...");
                    Configuration.EncryptStringToFile(compressedText, fileName, password);

                    //e.Result = true;

                    MessageBox.Show("Export completed successfully. File written to:\n" + dlg.FileName, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    //e.Result = ex;
                }
            }
        }

        private void dg_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            e.Cancel = true;
        }

        private void dgCases_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //RefreshContacts();
            //RefreshExposures();            

            //if (dgCases.SelectedItems.Count > 0)
            //{
            //    btnDeleteCase.IsEnabled = true;

            //    CaseViewModel caseVM = (dgCases.SelectedItem) as CaseViewModel;
            //    DataHelper.ShowContactsForCase.Execute(caseVM);
            //    if (borderExposures.Visibility == System.Windows.Visibility.Visible)
            //    {
            //        this.Cursor = Cursors.Wait;
            //        DataHelper.ShowExposuresForCase.Execute(caseVM);
            //        this.Cursor = Cursors.Arrow;
            //    }
            //}
            //else
            //{
            //    btnDeleteCase.IsEnabled = false;
            //}
        }

        private void dgCases_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgCases.SelectedItems.Count == 1)
            {
                IInputElement element = e.MouseDevice.DirectlyOver;
                if (element != null && element is FrameworkElement)
                {
                    if (((FrameworkElement)element).Parent is DataGridCell)
                    {
                        EditCase();
                    }
                }
            }
        }

        private Epi.Enter.EnterUIConfig GetCaseConfig()
        {
            Epi.Enter.EnterUIConfig uiConfig = new Epi.Enter.EnterUIConfig();

            uiConfig.AllowOneRecordOnly.Add(DataHelper.CaseForm, true);
            uiConfig.AllowOneRecordOnly.Add(DataHelper.LabForm, false);

            uiConfig.ShowDashboardButton.Add(DataHelper.CaseForm, true);
            uiConfig.ShowDashboardButton.Add(DataHelper.LabForm, true);

            uiConfig.ShowDeleteButtons.Add(DataHelper.CaseForm, false);
            uiConfig.ShowDeleteButtons.Add(DataHelper.LabForm, false);

            uiConfig.ShowEditFormButton.Add(DataHelper.CaseForm, false);
            uiConfig.ShowEditFormButton.Add(DataHelper.LabForm, false);

            uiConfig.ShowFileMenu.Add(DataHelper.CaseForm, false);
            uiConfig.ShowFileMenu.Add(DataHelper.LabForm, false);

            uiConfig.ShowFindButton.Add(DataHelper.CaseForm, false);
            uiConfig.ShowFindButton.Add(DataHelper.LabForm, false);

            uiConfig.ShowLineListButton.Add(DataHelper.CaseForm, false);
            uiConfig.ShowLineListButton.Add(DataHelper.LabForm, false);

            uiConfig.ShowMapButton.Add(DataHelper.CaseForm, false);
            uiConfig.ShowMapButton.Add(DataHelper.LabForm, false);

            uiConfig.ShowNavButtons.Add(DataHelper.CaseForm, false);
            uiConfig.ShowNavButtons.Add(DataHelper.LabForm, true);

            uiConfig.ShowNewRecordButton.Add(DataHelper.CaseForm, false);
            uiConfig.ShowNewRecordButton.Add(DataHelper.LabForm, true);

            uiConfig.ShowOpenFormButton.Add(DataHelper.CaseForm, false);
            uiConfig.ShowOpenFormButton.Add(DataHelper.LabForm, false);

            uiConfig.ShowPrintButton.Add(DataHelper.CaseForm, true);
            uiConfig.ShowPrintButton.Add(DataHelper.LabForm, true);

            uiConfig.ShowRecordCounter.Add(DataHelper.CaseForm, false);
            uiConfig.ShowRecordCounter.Add(DataHelper.LabForm, true);

            uiConfig.ShowSaveRecordButton.Add(DataHelper.CaseForm, false);
            uiConfig.ShowSaveRecordButton.Add(DataHelper.LabForm, false);

            uiConfig.ShowToolbar.Add(DataHelper.CaseForm, true);
            uiConfig.ShowToolbar.Add(DataHelper.LabForm, true);

            uiConfig.ShowLinkedRecordsViewer = false;

            return uiConfig;
        }

        private void EditCase()
        {
            Epi.Enter.EnterUIConfig uiConfig = GetCaseConfig();

            //Epi.Windows.Enter.EnterMainForm emf = new Epi.Windows.Enter.EnterMainForm(DataHelper.Project, DataHelper.CaseForm, false, true);
            Epi.Windows.Enter.EnterMainForm emf = new Epi.Windows.Enter.EnterMainForm(DataHelper.Project, DataHelper.CaseForm, uiConfig);

            int uniqueKey = ((LabResultViewModel)dgCases.SelectedItem).UniqueKey;

            emf.LoadRecord(uniqueKey);

            emf.RecordSaved += new SaveRecordEventHandler(emfCases_RecordSaved);            

            emf.ShowDialog();
            emf.RecordSaved -= new SaveRecordEventHandler(emfCases_RecordSaved);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.Dispatcher.BeginInvoke(new SimpleEventHandler(ResizeDataGrids));
        }

        private void ResizeDataGrids()
        {
            ResizeCasePanelDataGrids();
        }

        private void ResizeCasePanelDataGrids()
        {
            //if (gridCaseManagement.Visibility == System.Windows.Visibility.Visible)
            //{
            dgCases.Visibility = System.Windows.Visibility.Collapsed;
            //dgContacts.Visibility = System.Windows.Visibility.Collapsed;
            //dgExposures.Visibility = System.Windows.Visibility.Collapsed;

            gridCaseManagement.UpdateLayout();

            double maxHeight = gridCaseManagement.ActualHeight;
            MaxHeight = maxHeight - panelCaseManagementSearch.ActualHeight - 80;

            if (MaxHeight <= 0) MaxHeight = 0;

            //if (dgContacts.MaxHeight == 0 && dgExposures.MaxHeight == 0)
            //{
            dgCases.MaxHeight = MaxHeight;
            dgCases.Height = MaxHeight;
            //}
            //else if (dgContacts.MaxHeight == 0 && dgExposures.MaxHeight != 0)
            //{
            //    dgCases.MaxHeight = MaxHeight / 2;
            //    dgExposures.MaxHeight = MaxHeight / 2;
            //}
            //else if (dgContacts.MaxHeight != 0 && dgExposures.MaxHeight == 0)
            //{
            //    dgCases.MaxHeight = MaxHeight / 2;
            //    dgContacts.MaxHeight = MaxHeight / 2;
            //}
            //else
            //{
            //    dgCases.MaxHeight = MaxHeight / 3;
            //    dgContacts.MaxHeight = MaxHeight / 3;
            //    dgExposures.MaxHeight = MaxHeight / 3;
            //}

            dgCases.Visibility = System.Windows.Visibility.Visible;
            //dgContacts.Visibility = System.Windows.Visibility.Visible;
            //dgExposures.Visibility = System.Windows.Visibility.Visible;
            //}
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            double maxHeight = gridCaseManagement.ActualHeight;
            MaxHeight = maxHeight - panelCaseManagementSearch.ActualHeight - 120;
            dgCases.Height = MaxHeight;

            System.Windows.Forms.Application.EnableVisualStyles();

            System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
            lblVersion.Content = a.GetName().Version;
        }

        private void ArrangeTestColumns()
        {
            string prefix = String.Empty;

            switch (DataHelper.VirusTestType)
            {
                case Core.Enums.VirusTestTypes.Sudan:
                    this.SUDVPCR.DisplayIndex = 18;
                    this.SUDVPCR2.DisplayIndex = 19;
                    this.SUDVAg.DisplayIndex = 20;
                    this.SUDVIgM.DisplayIndex = 21;
                    this.SUDVIgG.DisplayIndex = 22;
                    prefix = "SUDV";
                    break;
                case Core.Enums.VirusTestTypes.Marburg:
                    this.MARVPCR.DisplayIndex = 18;
                    this.MARVPCR2.DisplayIndex = 19;
                    this.MARVAg.DisplayIndex = 20;
                    this.MARVIgM.DisplayIndex = 21;
                    this.MARVIgG.DisplayIndex = 22;
                    prefix = "MARV";
                    break;
                case Core.Enums.VirusTestTypes.Ebola:
                    this.EBOVPCR.DisplayIndex = 18;
                    this.EBOVPCR2.DisplayIndex = 19;
                    this.EBOVAg.DisplayIndex = 20;
                    this.EBOVIgM.DisplayIndex = 21;
                    this.EBOVIgG.DisplayIndex = 22;
                    prefix = "EBOV";
                    break;
                case Core.Enums.VirusTestTypes.Bundibugyo:
                    this.BDBVPCR.DisplayIndex = 18;
                    this.BDBVPCR2.DisplayIndex = 19;
                    this.BDBVAg.DisplayIndex = 20;
                    this.BDBVIgM.DisplayIndex = 21;
                    this.BDBVIgG.DisplayIndex = 22;
                    prefix = "BDBV";
                    break;
                case Core.Enums.VirusTestTypes.CCHF:
                    this.CCHFPCR.DisplayIndex = 18;
                    this.CCHFPCR2.DisplayIndex = 19;
                    this.CCHFAg.DisplayIndex = 20;
                    this.CCHFIgM.DisplayIndex = 21;
                    this.CCHFIgG.DisplayIndex = 22;
                    prefix = "CCHF";
                    break;
                case Core.Enums.VirusTestTypes.Rift:
                    this.RVFPCR.DisplayIndex = 18;
                    this.RVFPCR2.DisplayIndex = 19;
                    this.RVFAg.DisplayIndex = 20;
                    this.RVFIgM.DisplayIndex = 21;
                    this.RVFIgG.DisplayIndex = 22;
                    prefix = "RVF";
                    break;
                case Core.Enums.VirusTestTypes.Lassa:
                    this.LHFPCR.DisplayIndex = 18;
                    this.LHFPCR2.DisplayIndex = 19;
                    this.LHFAg.DisplayIndex = 20;
                    this.LHFIgM.DisplayIndex = 21;
                    this.LHFIgG.DisplayIndex = 22;
                    prefix = "LHF";
                    break;
            }

            int startIndex = 23;
            foreach (DataGridColumn dgc in dgCases.Columns)
            {
                if (dgc.Header != null && !dgc.Header.ToString().StartsWith(prefix) && (
                    dgc.Header.ToString().StartsWith("LHF ") ||
                    dgc.Header.ToString().StartsWith("RVF ") ||
                    dgc.Header.ToString().StartsWith("CCHF ") ||
                    dgc.Header.ToString().StartsWith("BDBV ") ||
                    dgc.Header.ToString().StartsWith("EBOV ") ||
                    dgc.Header.ToString().StartsWith("MARV ") ||
                    dgc.Header.ToString().StartsWith("SUDV ")
                    ))
                {
                    dgc.DisplayIndex = startIndex;
                    startIndex++;
                }
            }
        }

        private void EpiDataHelper_InitialSetupRun(object sender, EventArgs e)
        {
            initialSetup.Visibility = System.Windows.Visibility.Visible;
        }

        private void initialSetup_Closed(object sender, RoutedEventArgs e)
        {
            initialSetup.Visibility = System.Windows.Visibility.Collapsed;

            if (e.RoutedEvent.Name.ToLower() == "ok")
            {
                DataHelper.FillInOutbreakData(initialSetup.OutbreakName, initialSetup.IDPrefix, initialSetup.IDSeparator,
                    initialSetup.OutbreakDate, initialSetup.IDPattern, initialSetup.Virus, initialSetup.Country, initialSetup.IsShortForm); //17040

                ArrangeTestColumns();
            }
        }

        private void LabDataHelper_CaseDataPopulated(object sender, ViewModel.Events.CaseDataPopulatedArgs e)
        {
            ArrangeTestColumns(/*e.VirusTestType*/);
            panelLoading.Visibility = System.Windows.Visibility.Collapsed;
            grdIntro.Visibility = System.Windows.Visibility.Collapsed;
            this.Cursor = Cursors.Arrow;
        }

        private void GenerateDailyHtmlFooter(StringBuilder htmlBuilder)
        {
            htmlBuilder.Append("</tbody>");
            htmlBuilder.Append("</table>");
            htmlBuilder.Append("<div style=\"page-break-before:always;\" />");
        }

        private void btnPrintDailyLabSummary_Click(object sender, RoutedEventArgs e)
        {
            string baseFileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString("N");

            StringBuilder htmlBuilder = new StringBuilder();

            htmlBuilder.Append(ContactTracing.Core.Common.GetHtmlHeader().ToString());

            int rowsGenerated = 0;
            bool firstPage = true;

            htmlBuilder.AppendLine("<table width=\"100%\" style=\"border: 0px; padding: 0px; margin: 0px; clear: left; width:100%; \">");
            htmlBuilder.AppendLine(" <tr style=\"border: 0px;\">");
            htmlBuilder.AppendLine("  <td width=\"50%\" style=\"border: 0px;\">");
            htmlBuilder.AppendLine("   <p style=\"font-size: 13pt; font-weight: bold; clear: left; text-decoration: underline;\">" + Properties.Settings.Default.HtmlPrintoutTitle + "</p>");
            htmlBuilder.AppendLine("   <p style=\"font-size: 13pt; font-weight: bold; text-decoration: underline;\">" + Properties.Resources.HTMLDailyLabSummaryHeading + "</p>");
            htmlBuilder.AppendLine("   <p style=\"font-size: 13pt;\"><span style=\"font-weight: bold;\">" + Properties.Resources.HTMLDate + "</span> " + DateTime.Now.ToShortDateString() + "</p>");
            htmlBuilder.AppendLine("  </td>");
            htmlBuilder.AppendLine(" </tr>");
            htmlBuilder.AppendLine("</table>");

            //foreach (var entry in query)
            foreach (LabResultViewModel labResultVM in DataHelper.LabResultCollection)
            {
                if (rowsGenerated == 0)
                {
                    htmlBuilder.AppendLine("<table style=\"width: 1200px; border: 4px solid black;\" align=\"left\">");
                    htmlBuilder.AppendLine("<thead>");
                    htmlBuilder.AppendLine("<tr style=\"border-top: 0px solid black;\">");
                    htmlBuilder.AppendLine("<th style=\"width: 110px;\">" + Properties.Resources.ColHeaderSurname + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 140px;\">" + Properties.Resources.ColHeaderOtherNames + "</th>");
                    //htmlBuilder.AppendLine("<th style=\"width: 60px;\">MoH/UVRI ID</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 60px;\">" + Properties.Resources.ColHeaderID + "</th>"); //17137
                    htmlBuilder.AppendLine("<th style=\"width: 60px;\">A&nbsp;g&nbsp;e</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 60px;\">S&nbsp;e&nbsp;x</th>");

                    htmlBuilder.AppendLine("<th style=\"width: 60px;\">" + Properties.Resources.ColHeaderFLSID + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 60px;\">UVRI/VSPB Log #</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 80px;\">" + Properties.Resources.ColHeaderVillage + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 80px;\">" + Properties.Resources.ColHeaderDistrict + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 70px;\">" + Properties.Resources.ColHeaderSampleType + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 40px;\">" + Properties.Resources.ColHeaderOnsetDate + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 40px;\">" + Properties.Resources.ColHeaderDateSampleCollected + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 20px;\">" + Properties.Resources.ColHeaderDaysAcute + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 40px;\">" + Properties.Resources.ColHeaderDateSampleTested + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 40px;\">" + Properties.Resources.ColHeaderDateDeath + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 70px;\">" + Properties.Resources.ColHeaderFinalLabClass + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 70px;\">" + Properties.Resources.ColHeaderSampleInterpretation + "</th>");

                    htmlBuilder.AppendLine("</tr>");
                    htmlBuilder.AppendLine("</thead>");
                    htmlBuilder.AppendLine("<tbody>");
                }

                htmlBuilder.AppendLine("<tr style=\"border-bottom: 1px solid black;\">");
                htmlBuilder.AppendLine("<td>" + Core.Common.TruncHTMLCell(labResultVM.Surname, 13) + "</td>");
                htmlBuilder.AppendLine("<td>" + Core.Common.TruncHTMLCell(labResultVM.OtherNames, 20) + "</td>");
                htmlBuilder.AppendLine("<td>" + labResultVM.CaseID + "</td>");

                if (labResultVM.Age.HasValue)
                {
                    htmlBuilder.AppendLine("<td>" + labResultVM.Age + "</td>");
                }
                else
                {
                    htmlBuilder.AppendLine("<td>&nbsp;</td>");
                }
                htmlBuilder.AppendLine("<td>" + labResultVM.Gender + "</td>");

                htmlBuilder.AppendLine("<td>" + labResultVM.FieldLabSpecimenID + "</td>");
                htmlBuilder.AppendLine("<td>" + labResultVM.UVRIVSPBLogNumber + "</td>");
                htmlBuilder.AppendLine("<td>" + Core.Common.TruncHTMLCell(labResultVM.Village, 20) + "</td>");
                htmlBuilder.AppendLine("<td>" + Core.Common.TruncHTMLCell(labResultVM.District, 20) + "</td>");
                htmlBuilder.AppendLine("<td>" + labResultVM.SampleType + "</td>");

                if (labResultVM.DateOnset.HasValue)
                {
                    htmlBuilder.AppendLine("<td>" + labResultVM.DateOnset.Value.ToShortDateString() + "</td>");
                }
                else
                {
                    htmlBuilder.AppendLine("<td>&nbsp;</td>");
                }

                if (labResultVM.DateSampleCollected.HasValue)
                {
                    htmlBuilder.AppendLine("<td>" + labResultVM.DateSampleCollected.Value.ToShortDateString() + "</td>");
                }
                else
                {
                    htmlBuilder.AppendLine("<td>&nbsp;</td>");
                }

                if (labResultVM.DaysAcute.HasValue)
                {
                    htmlBuilder.AppendLine("<td>" + labResultVM.DaysAcute + "</td>");
                }
                else
                {
                    htmlBuilder.AppendLine("<td>&nbsp;</td>");
                }

                if (labResultVM.DateSampleTested.HasValue)
                {
                    htmlBuilder.AppendLine("<td>" + labResultVM.DateSampleTested.Value.ToShortDateString() + "</td>");
                }
                else
                {
                    htmlBuilder.AppendLine("<td>&nbsp;</td>");
                }

                if (labResultVM.DateDeath.HasValue)
                {
                    htmlBuilder.AppendLine("<td>" + labResultVM.DateDeath.Value.ToShortDateString() + "</td>");
                }
                else
                {
                    htmlBuilder.AppendLine("<td>&nbsp;</td>");
                }
                htmlBuilder.AppendLine("<td>" + labResultVM.FinalLabClassification + "</td>");
                htmlBuilder.AppendLine("<td>" + labResultVM.SampleInterpretation + "</td>");


                htmlBuilder.AppendLine("</tr>");

                rowsGenerated++;

                if (firstPage && rowsGenerated == 28)
                {
                    GenerateDailyHtmlFooter(htmlBuilder);
                    rowsGenerated = 0;
                    firstPage = false;
                }
                else if (!firstPage && rowsGenerated == 34)
                {
                    GenerateDailyHtmlFooter(htmlBuilder);
                    rowsGenerated = 0;
                }
            }

            if (firstPage && rowsGenerated % 28 != 0)
            {
                GenerateDailyHtmlFooter(htmlBuilder);
                rowsGenerated = 0;
                firstPage = true;
            }
            else if (!firstPage && rowsGenerated % 34 != 0)
            {
                GenerateDailyHtmlFooter(htmlBuilder);
                rowsGenerated = 0;
                firstPage = true;
            }

            string fileName = baseFileName + ".html";

            using (System.IO.FileStream fstream = System.IO.File.OpenWrite(fileName))
            {
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter(fstream))
                {
                    sw.WriteLine(htmlBuilder.ToString());
                }
            }

            if (!String.IsNullOrEmpty(fileName))
            {
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = "\"" + fileName + "\"";
                proc.StartInfo.UseShellExecute = true;
                proc.Start();
            }
        }

        private void btnExportDailyLabSummary_Click(object sender, RoutedEventArgs e)
        {
            //string baseFileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString("N");

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();

            dlg.DefaultExt = ".csv"; // Default file extension
            dlg.Filter = "Comma separated values file|*.csv"; // Filter files by extension 

            Nullable<bool> dialogResult = dlg.ShowDialog();

            // Process save file dialog box results 
            if (dialogResult == true)
            {

                StringBuilder csvBuilder = new StringBuilder();

                string columns = String.Empty;

                if (SUDVPCR.Visibility == System.Windows.Visibility.Visible)
                {
                    columns += "SUDVPCR, SUDVPCR2, SUDVAg, SUDVIgM, SUDVIgG";
                }

                if (BDBVPCR.Visibility == System.Windows.Visibility.Visible)
                {
                    columns += "BDBVPCR, BDBVPCR2, BDBVAg, BDBVIgM, BDBVIgG";
                }

                if (EBOVPCR.Visibility == System.Windows.Visibility.Visible)
                {
                    columns += "EBOVPCR, EBOVPCR2, EBOVAg, EBOVIgM, EBOVIgG";
                }

                if (RVFPCR.Visibility == System.Windows.Visibility.Visible)
                {
                    columns += "RVFPCR, RVFPCR2, RVFAg, RVFIgM, RVFIgG";
                }

                if (CCHFPCR.Visibility == System.Windows.Visibility.Visible)
                {
                    columns += "CCHFPCR, CCHFPCR2, CCHFAg, CCHFIgM, CCHFIgG";
                }

                if (LHFPCR.Visibility == System.Windows.Visibility.Visible)
                {
                    columns += "LHFPCR, LHFPCR2, LHFAg, LHFIgM, LHFIgG";
                }

                if (MARVPCR.Visibility == System.Windows.Visibility.Visible)
                {
                    columns += "MARVPCR, MARVPCR2, MARVAg, MARVIgM, MARVIgG";
                }

                csvBuilder.AppendLine("Surname, Other Names, ID, Age, Gender, FieldLabSpecId, UVRILogNum, Village, District, SampleType, DateOnset, DateSampleCollected, DaysAcute, DateSampleTested, DateOfDeath, FinalLabClass, SampleInterpretation, " + columns);

                foreach (LabResultViewModel result in DataHelper.LabResultCollection)
                {
                    WordBuilder wb = new WordBuilder(",");
                    wb.Add(result.Surname);
                    wb.Add(result.OtherNames);
                    wb.Add(result.CaseID);
                    if (result.Age.HasValue)
                    {
                        wb.Add(result.Age.Value.ToString());
                    }
                    else
                    {
                        wb.Add(String.Empty);
                    }
                    wb.Add(result.Gender);
                    wb.Add(result.FieldLabSpecimenID);
                    wb.Add(result.UVRIVSPBLogNumber);
                    wb.Add(result.Village);
                    wb.Add(result.District);
                    wb.Add(result.SampleType);
                    if (result.DateOnset.HasValue)
                    {
                        wb.Add(result.DateOnset.Value.ToShortDateString());
                    }
                    else
                    {
                        wb.Add(String.Empty);
                    }

                    if (result.DateSampleCollected.HasValue)
                    {
                        wb.Add(result.DateSampleCollected.Value.ToShortDateString());
                    }
                    else
                    {
                        wb.Add(String.Empty);
                    }

                    if (result.DaysAcute.HasValue)
                    {
                        wb.Add(result.DaysAcute.Value.ToString());
                    }
                    else
                    {
                        wb.Add(String.Empty);
                    }

                    if (result.DateSampleTested.HasValue)
                    {
                        wb.Add(result.DateSampleTested.Value.ToShortDateString());
                    }
                    else
                    {
                        wb.Add(String.Empty);
                    }

                    if (result.DateDeath.HasValue)
                    {
                        wb.Add(result.DateDeath.Value.ToShortDateString());
                    }
                    else
                    {
                        wb.Add(String.Empty);
                    }
                    wb.Add(result.FinalLabClassification);
                    wb.Add(result.SampleInterpretation);

                    if (SUDVPCR.Visibility == System.Windows.Visibility.Visible)
                    {
                        wb.Add(result.SUDVPCR);
                        wb.Add(result.SUDVPCR2);
                        wb.Add(result.SUDVAg);
                        wb.Add(result.SUDVIgM);
                        wb.Add(result.SUDVIgG);
                    }

                    if (BDBVPCR.Visibility == System.Windows.Visibility.Visible)
                    {
                        wb.Add(result.BDBVPCR);
                        wb.Add(result.BDBVPCR2);
                        wb.Add(result.BDBVAg);
                        wb.Add(result.BDBVIgM);
                        wb.Add(result.BDBVIgG);
                    }

                    if (EBOVPCR.Visibility == System.Windows.Visibility.Visible)
                    {
                        wb.Add(result.EBOVPCR);
                        wb.Add(result.EBOVPCR2);
                        wb.Add(result.EBOVAg);
                        wb.Add(result.EBOVIgM);
                        wb.Add(result.EBOVIgG);
                    }

                    if (RVFPCR.Visibility == System.Windows.Visibility.Visible)
                    {
                        wb.Add(result.RVFPCR);
                        wb.Add(result.RVFPCR2);
                        wb.Add(result.RVFAg);
                        wb.Add(result.RVFIgM);
                        wb.Add(result.RVFIgG);
                    }

                    if (CCHFPCR.Visibility == System.Windows.Visibility.Visible)
                    {
                        wb.Add(result.CCHFPCR);
                        wb.Add(result.CCHFPCR2);
                        wb.Add(result.CCHFAg);
                        wb.Add(result.CCHFIgM);
                        wb.Add(result.CCHFIgG);
                    }

                    if (LHFPCR.Visibility == System.Windows.Visibility.Visible)
                    {
                        wb.Add(result.LHFPCR);
                        wb.Add(result.LHFPCR2);
                        wb.Add(result.LHFAg);
                        wb.Add(result.LHFIgM);
                        wb.Add(result.LHFIgG);
                    }

                    if (MARVPCR.Visibility == System.Windows.Visibility.Visible)
                    {
                        wb.Add(result.MARVPCR);
                        wb.Add(result.MARVPCR2);
                        wb.Add(result.MARVAg);
                        wb.Add(result.MARVIgM);
                        wb.Add(result.MARVIgG);
                    }

                    csvBuilder.AppendLine(wb.ToString());
                }

                string fileName = dlg.FileName;

                using (System.IO.FileStream fstream = System.IO.File.OpenWrite(fileName))
                {
                    using (System.IO.StreamWriter sw = new System.IO.StreamWriter(fstream))
                    {
                        sw.WriteLine(csvBuilder.ToString());
                    }
                }

                if (!String.IsNullOrEmpty(fileName))
                {
                    System.Diagnostics.Process proc = new System.Diagnostics.Process();
                    proc.StartInfo.FileName = "\"" + fileName + "\"";
                    proc.StartInfo.UseShellExecute = true;
                    proc.Start();
                }
            }

            //csvBuilder.AppendLine("</tr>");
            //csvBuilder.AppendLine("</thead>");
            //csvBuilder.AppendLine("<tbody>");

            //foreach (LabResultViewModel labResultVM in DataHelper.LabResultCollection)
            //{
                
                    
                

            //    csvBuilder.AppendLine("<tr style=\"border-bottom: 1px solid black;\">");
            //    csvBuilder.AppendLine("<td>" + Core.Common.TruncHTMLCell(labResultVM.Surname, 13) + "</td>");
            //    csvBuilder.AppendLine("<td>" + Core.Common.TruncHTMLCell(labResultVM.OtherNames, 20) + "</td>");
            //    csvBuilder.AppendLine("<td>" + labResultVM.CaseID + "</td>");
            //    csvBuilder.AppendLine("<td>" + labResultVM.FieldLabSpecimenID + "</td>");
            //    csvBuilder.AppendLine("<td>" + labResultVM.UVRIVSPBLogNumber + "</td>");
            //    csvBuilder.AppendLine("<td>" + Core.Common.TruncHTMLCell(labResultVM.Village, 20) + "</td>");
            //    csvBuilder.AppendLine("<td>" + Core.Common.TruncHTMLCell(labResultVM.District, 20) + "</td>");
            //    csvBuilder.AppendLine("<td>" + labResultVM.SampleType + "</td>");

            //    if (labResultVM.DateOnset.HasValue)
            //    {
            //        csvBuilder.AppendLine("<td>" + labResultVM.DateOnset.Value.ToShortDateString() + "</td>");
            //    }
            //    else
            //    {
            //        csvBuilder.AppendLine("<td>&nbsp;</td>");
            //    }

            //    if (labResultVM.DateSampleCollected.HasValue)
            //    {
            //        csvBuilder.AppendLine("<td>" + labResultVM.DateSampleCollected.Value.ToShortDateString() + "</td>");
            //    }
            //    else
            //    {
            //        csvBuilder.AppendLine("<td>&nbsp;</td>");
            //    }

            //    if (labResultVM.DaysAcute.HasValue)
            //    {
            //        csvBuilder.AppendLine("<td>" + labResultVM.DaysAcute + "</td>");
            //    }
            //    else
            //    {
            //        csvBuilder.AppendLine("<td>&nbsp;</td>");
            //    }

            //    if (labResultVM.DateSampleTested.HasValue)
            //    {
            //        csvBuilder.AppendLine("<td>" + labResultVM.DateSampleTested.Value.ToShortDateString() + "</td>");
            //    }
            //    else
            //    {
            //        csvBuilder.AppendLine("<td>&nbsp;</td>");
            //    }

            //    if (labResultVM.DateDeath.HasValue)
            //    {
            //        csvBuilder.AppendLine("<td>" + labResultVM.DateDeath.Value.ToShortDateString() + "</td>");
            //    }
            //    else
            //    {
            //        csvBuilder.AppendLine("<td>&nbsp;</td>");
            //    }
            //    csvBuilder.AppendLine("<td>" + labResultVM.FinalLabClassification + "</td>");
            //    csvBuilder.AppendLine("<td>" + labResultVM.SampleInterpretation + "</td>");


            //    csvBuilder.AppendLine("</tr>");

            //    rowsGenerated++;

            //    if (firstPage && rowsGenerated == 28)
            //    {
            //        GenerateDailyHtmlFooter(csvBuilder);
            //        rowsGenerated = 0;
            //        firstPage = false;
            //    }
            //    else if (!firstPage && rowsGenerated == 34)
            //    {
            //        GenerateDailyHtmlFooter(csvBuilder);
            //        rowsGenerated = 0;
            //    }
            //}

            //if (firstPage && rowsGenerated % 28 != 0)
            //{
            //    GenerateDailyHtmlFooter(csvBuilder);
            //    rowsGenerated = 0;
            //    firstPage = true;
            //}
            //else if (!firstPage && rowsGenerated % 34 != 0)
            //{
            //    GenerateDailyHtmlFooter(csvBuilder);
            //    rowsGenerated = 0;
            //    firstPage = true;
            //}


            //string fileName = baseFileName + ".html";

            //System.IO.FileStream fstream = System.IO.File.OpenWrite(fileName);
            //System.IO.StreamWriter sw = new System.IO.StreamWriter(fstream);
            //sw.WriteLine(csvBuilder.ToString());
            //sw.Close();
            //sw.Dispose();

            //if (!string.IsNullOrEmpty(fileName))
            //{
            //    System.Diagnostics.Process proc = new System.Diagnostics.Process();
            //    proc.StartInfo.FileName = "\"" + fileName + "\"";
            //    proc.StartInfo.UseShellExecute = true;
            //    proc.Start();
            //}
        }

        private void LabDataHelper_LabRecordAdded(object sender, ViewModel.Events.CaseDataPopulatedArgs e)
        {
            //ShowHideTestColumns(/*e.VirusTestType*/);
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            initialSetup.SetDefaults(DataHelper);
            initialSetup.Visibility = System.Windows.Visibility.Visible;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private void btnCloseProject_Click(object sender, RoutedEventArgs e)
        {
            DataHelper.OutbreakName = String.Empty;
            DataHelper.ClearCollections();
            grdIntro.Visibility = System.Windows.Visibility.Visible;
            fileScreen.Refresh();
            fileScreen.ShouldPollForFiles = true;
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            newOutbreak.Visibility = System.Windows.Visibility.Visible;
        }


        private void fileScreen_ProjectOpened(object sender, Core.Events.ProjectOpenedArgs e)
        {
            panelLoading.Visibility = System.Windows.Visibility.Visible;
            grdIntro.Visibility = System.Windows.Visibility.Collapsed;

            fileScreen.ShouldPollForFiles = false;

            this.Cursor = Cursors.Wait;

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            worker.RunWorkerAsync(e.ProjectInfo.FileInfo.FullName);
        }

        private void BeginLoadProject(VhfProject project)
        {
            LoadProject(project);
            DataHelper.RepopulateCollections();
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            VhfProject project = e.Result as VhfProject;
            if (project != null)
            {
                this.Dispatcher.BeginInvoke(new BeginLoadProjectHandler(BeginLoadProject), project);
            }
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Project project = new Project((string)e.Argument);
            e.Result = project;
        }

        private void LoadProject(VhfProject project)
        {
            //double maxHeight = gridCaseManagement.ActualHeight;
            //MaxHeight = maxHeight - panelCaseManagementSearch.ActualHeight - 128;
            //if (MaxHeight > 0)
            //{
            //    dgCases.Height = MaxHeight;
            //}
            ResizeCasePanelDataGrids();
            
            DataHelper.InitializeProject(project);
            DataHelper.SetupDatabase();
        }

        private void UpdateMetaFields(Project project)
        {
            IDbDriver db = project.CollectedData.GetDatabase();

            // 1 = text
            // 17, 18, 19 = ddl's

            Query updateQuery = db.CreateQuery("UPDATE metaFields SET FieldTypeId = 1 " +
                "WHERE (FieldTypeId = 17 OR FieldTypeId = 18 OR FieldTypeId = 19) " +
                "AND (PromptText = @PromptTextDistrict OR PromptText = @PromptTextSC)");

            updateQuery.Parameters.Add(new QueryParameter("@PromptTextDistrict", DbType.String, "District:"));
            updateQuery.Parameters.Add(new QueryParameter("@PromptTextSC", DbType.String, "Sub-County:"));

            int rows = db.ExecuteNonQuery(updateQuery);

            if (rows == 0)
            {

                // shouldn't get here
            }
        }

        private void TranslateMetaFieldsToFrench(Project project)
        {
            IDbDriver db = project.CollectedData.GetDatabase();

            Query updateQuery = db.CreateQuery("UPDATE metaFields SET PromptText = @PromptText " +
                "WHERE [Name] = @Name");
            updateQuery.Parameters.Add(new QueryParameter("@PromptText", DbType.String, Properties.Resources.ColHeaderSurname));
            updateQuery.Parameters.Add(new QueryParameter("@Name", DbType.String, "Surname"));

            int rows = db.ExecuteNonQuery(updateQuery);

            updateQuery = db.CreateQuery("UPDATE metaFields SET PromptText = @PromptText " +
                "WHERE [Name] = @Name");
            updateQuery.Parameters.Add(new QueryParameter("@PromptText", DbType.String, Properties.Resources.ColHeaderSurname));
            updateQuery.Parameters.Add(new QueryParameter("@Name", DbType.String, "SurnameLab"));

            rows = db.ExecuteNonQuery(updateQuery);

            updateQuery = db.CreateQuery("UPDATE metaFields SET PromptText = @PromptText " +
                "WHERE [Name] = @Name");
            updateQuery.Parameters.Add(new QueryParameter("@PromptText", DbType.String, Properties.Resources.ColHeaderOtherNames));
            updateQuery.Parameters.Add(new QueryParameter("@Name", DbType.String, "OtherNames"));

            rows = db.ExecuteNonQuery(updateQuery);

            updateQuery = db.CreateQuery("UPDATE metaFields SET PromptText = @PromptText " +
                "WHERE [Name] = @Name");
            updateQuery.Parameters.Add(new QueryParameter("@PromptText", DbType.String, Properties.Resources.ColHeaderOtherNames));
            updateQuery.Parameters.Add(new QueryParameter("@Name", DbType.String, "OtherNameLab"));

            rows = db.ExecuteNonQuery(updateQuery);

            updateQuery = db.CreateQuery("UPDATE metaFields SET PromptText = @PromptText " +
                "WHERE [Name] = @Name");
            updateQuery.Parameters.Add(new QueryParameter("@PromptText", DbType.String, Properties.Resources.ColHeaderAge));
            updateQuery.Parameters.Add(new QueryParameter("@Name", DbType.String, "Age"));

            rows = db.ExecuteNonQuery(updateQuery);

            updateQuery = db.CreateQuery("UPDATE metaFields SET PromptText = @PromptText " +
    "WHERE [Name] = @Name");
            updateQuery.Parameters.Add(new QueryParameter("@PromptText", DbType.String, Properties.Resources.Country));
            updateQuery.Parameters.Add(new QueryParameter("@Name", DbType.String, "CountryRes"));

            rows = db.ExecuteNonQuery(updateQuery);

            updateQuery = db.CreateQuery("UPDATE metaFields SET PromptText = @PromptText " +
                "WHERE [Name] = @Name");
            updateQuery.Parameters.Add(new QueryParameter("@PromptText", DbType.String, Properties.Resources.ColHeaderDateDeath.Replace('\n', ' ')));
            updateQuery.Parameters.Add(new QueryParameter("@Name", DbType.String, "DateDeath"));

            rows = db.ExecuteNonQuery(updateQuery);
        }

        private void newOutbreak_Closed(object sender, RoutedEventArgs e)
        {
            newOutbreak.Visibility = System.Windows.Visibility.Collapsed;

            if (newOutbreak.FileName.Contains("\\") || newOutbreak.FileName.Contains("/") || newOutbreak.FileName.Contains(":") || newOutbreak.FileName.Contains("$"))
            {
                // TODO: Use regex to validate this
                throw new InvalidOperationException("File name may not contain special characters."); // TODO: Specify whitelisted characters
            }

            if (e.RoutedEvent.Name.ToLower() == "ok")
            {
                string newProjectDatabaseName = @"Projects\VHF\vhf_" + newOutbreak.FileName.ToLower().Replace(" ", String.Empty) + ".mdb";
                string newProjectName = @"Projects\VHF\vhf_" + newOutbreak.FileName.ToLower().Replace(" ", String.Empty) + ".prj";

                bool updateMetaFields = false;
                bool translateMetaFieldsToFrench = false;

                if (File.Exists(newProjectDatabaseName))
                {
                    MessageBox.Show(Properties.Resources.ProjectNameExistsTryAgain);
                }
                else
                {
                    switch (System.Threading.Thread.CurrentThread.CurrentUICulture.ToString())
                    {
                        case "fr":
                        case "fr-FR":
                        case "fr-fr":
                        case "fr­­­­­­­­­­­–FR":
                        case "fr­­­­­­­­­­­–fr":
                        case "fr­­­­­­­­­­­­­­­­­­­­­­—FR":
                        case "fr­­­­­­­­­­­­­­­­­­­­­­—fr":
                        case "fr―­­­­­­­­­­­­­­­FR":
                        case "fr­­­­­­­­­­­­­­­­­­­­­­―fr":
                            translateMetaFieldsToFrench = true;
                            break;
                        default:
                            break;
                    }

                    if (newOutbreak.Country != "Uganda")
                    {
                        updateMetaFields = true;
                    }

                    File.Copy(@"Projects\VHF\base_vhf_lab_template.mdb", newProjectDatabaseName);

                    Util.CreateProjectFileFromDatabase(newProjectDatabaseName, true);

                    // add vhf tags to xml document
                    XmlDocument doc = new XmlDocument();
                    doc.XmlResolver = null;
                    doc.Load(newProjectName);

                    XmlNode projectNode = doc.SelectSingleNode("Project");

                    XmlElement isVhfElement = doc.CreateElement("IsVHF");
                    XmlElement isLabElement = doc.CreateElement("IsLabProject");
                    XmlElement outbreakNameElement = doc.CreateElement("OutbreakName");
                    XmlElement outbreakDateElement = doc.CreateElement("OutbreakDate");

                    isVhfElement.InnerText = "true";
                    isLabElement.InnerText = "true";
                    outbreakDateElement.InnerText = newOutbreak.OutbreakDate.Value.Ticks.ToString();
                    outbreakNameElement.InnerText = newOutbreak.OutbreakName;

                    projectNode.AppendChild(isVhfElement);
                    projectNode.AppendChild(isLabElement);
                    projectNode.AppendChild(outbreakDateElement);
                    projectNode.AppendChild(outbreakNameElement);

                    System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
                    XmlAttribute appVersionAttribute = doc.CreateAttribute("appVersion");
                    appVersionAttribute.Value = a.GetName().Version.ToString();

                    projectNode.Attributes.Append(appVersionAttribute);

                    doc.Save(newProjectName);

                    VhfProject project = new VhfProject(newProjectName);

                    if (updateMetaFields) { UpdateMetaFields(project); }
                    if (translateMetaFieldsToFrench) { TranslateMetaFieldsToFrench(project); }

                    LoadProject(project);

                    grdIntro.Visibility = System.Windows.Visibility.Collapsed;

                    DataHelper.FillInOutbreakData(newOutbreak.OutbreakName, newOutbreak.IDPrefix, newOutbreak.IDSeparator,
                        newOutbreak.OutbreakDate, newOutbreak.IDPattern, newOutbreak.Virus, newOutbreak.Country, initialSetup.IsShortForm); //17040

                    DataHelper.RepopulateCollections();
                }
            }
        }
    }
}
