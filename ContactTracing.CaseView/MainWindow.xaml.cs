using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Linq.Dynamic;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
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
using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Epi;
using Epi.Data;
using Epi.Fields;
using ContactTracing.Controls;
using ContactTracing.Core;
using ContactTracing.Core.Data;
using ContactTracing.CaseView.Controls;
using ContactTracing.ViewModel;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.HSSF.Util;
using NPOI.POIFS.FileSystem;
using NPOI.HPSF;
using ContactTracing.CaseView.Controls.Diagnostics;
using System.Threading;
using ContactTracing.Core.Enums;
using ContactTracing.CaseView.Converters;



// TODO: Class needs a total and complete re-write

namespace ContactTracing.CaseView
{



    public class AntipatternCultureInfo
    {
        public AntipatternCultureInfo(string cultureText)
        {
            cultureInfo = new CultureInfo(cultureText);
        }

        public string DisplayName { get; set; }
        public string Name { get; set; }
        private CultureInfo cultureInfo;

        public CultureInfo CultureInfo
        {
            get { return cultureInfo; }

        }

    }

    //public class CultureLanguageDictionaryWrapper
    //{

    //    private Dictionary<string, string> dict;

    //    public Dictionary<string, string> Dict
    //    {
    //        get { return dict; }
    //        set { dict = value; }
    //    }

    //    public CultureLanguageDictionaryWrapper()
    //    {

    //        dict = new Dictionary<string, string>();

    //        ApplicationViewModel.Instance.TestProp = 77;

    //        dict.Add("en-us", "English");
    //        dict.Add("en", "English (United States)");
    //        dict.Add("fr", "French");
    //        dict.Add("fr-fr", "French");


    //    }

    //}

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private delegate void BeginLoadProjectHandler(VhfProject project);
        private delegate void ExceptionMessagingHandler(Exception ex);
        private delegate void DuplicateIdDetectedHandler(object sender, ViewModel.Events.DuplicateIdDetectedArgs e);

        private Popup Popup { get; set; }
        private TransmissionChain TransmissionChain { get; set; }
        private IDbDriver Database { get; set; }
        private View CaseForm { get; set; }
        private View ContactForm { get; set; }
        private DataView FollowUpView { get; set; }
        private Project Project { get; set; }
        private double MaxHeight { get; set; }
        private bool ShowContactManagementCases { get; set; }
        private bool ShowContactManagementChart { get; set; }

        private bool IsSuperUser
        {
            get
            {
                return this.DataHelper.IsSuperUser;
            }
            set
            {
                this.DataHelper.IsSuperUser = value;
            }
        }

        private int _contactType;

        public int ContactType
        {
            get
            {
                if (DataHelper.IsCountryUS)
                {
                    _contactType = 0;
                }
                else
                {
                    _contactType = 1;
                }
                return _contactType;
            }
            //set { _contactType = value; }
        }


        private EpiDataHelper DataHelper
        {
            get
            {
                return ((this.DataContext) as EpiDataHelper);
            }
        }

        public event EventHandler ChangeCulture;


        public MainWindow()
        {
            InitializeComponent();

            ShowContactManagementCases = true;
            ShowContactManagementChart = false;
            IsSuperUser = false;

            transmissionScroller.ScrollChanged += transmissionScroller_ScrollChanged;

            this.Loaded += MainWindow_Loaded;

        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

            ApplicationViewModel.RegionChanged += ApplicationViewModel_RegionChanged;
            //Dictionary<string, string> isUS = new Dictionary<string, string>();
            //isUS.Add("USA", "United States of America");
            //isUS.Add("non-US", "Another Country");
            //  cmboRegion.ItemsSource = isUS;

            //pathPlusNewOutbreak.IsEnabled = false;
            //pathCircleNewOutbreak.IsEnabled = false;
            //textBlockNewOutbreak.IsEnabled = false;

            //double opacity = 0.2;

            //pathPlusNewOutbreak.Opacity = opacity;
            //pathCircleNewOutbreak.Opacity = opacity;
            //textBlockNewOutbreak.Opacity = opacity;

            string cultureText;
            // Populate the culture drop-down    
            List<AntipatternCultureInfo> supportedCultures = new List<AntipatternCultureInfo>();

            // TODO:  these can be loaded from a "supported cultures" tahle later    

            AntipatternCultureInfo aci = new AntipatternCultureInfo("en-us");
            aci.DisplayName = "English";     // We override the normal DisplayName for en-us ( English (United States ) ) to preserve compat
            // with existg datasets     
            aci.Name = aci.CultureInfo.Name;

            supportedCultures.Add(aci);

            aci = new AntipatternCultureInfo("fr");
            aci.DisplayName = aci.CultureInfo.DisplayName;
            aci.Name = aci.CultureInfo.Name;

            supportedCultures.Add(aci);


            // Set up dropdown to read properties from the CultureInfo class  
            cmboCulture.ItemsSource = supportedCultures;
            cmboCulture.DisplayMemberPath = "DisplayName";
            cmboCulture.SelectedValuePath = "Name";

            // Select the drop down item that matches the current culture
            foreach (AntipatternCultureInfo item in cmboCulture.ItemsSource)
            {
                cultureText = Thread.CurrentThread.CurrentCulture.Name;

                if (item.Name == cultureText)
                {

                    cmboCulture.SelectedItem = item;
                    break;

                }


                //if (ContactTracing.CaseView.Properties.Settings.Default.Region == "USA")
                //{



                //    radioRegion1.IsChecked = true;


                //}
                //else
                //{
                //    radioRegion2.IsChecked = true;
                //}

            }


            radioRegion1.Checked -= radioRegion1_Checked;
            radioRegion2.Checked -= radioRegion2_Checked;

            if (ContactTracing.CaseView.Properties.Settings.Default.Region == RegionEnum.USA)
            {
                radioRegion1.IsChecked = true;
                txtSelectLanguage.Visibility = System.Windows.Visibility.Collapsed;
                cmboCulture.Visibility = System.Windows.Visibility.Collapsed;
            }

            else if (ContactTracing.CaseView.Properties.Settings.Default.Region == RegionEnum.International)
            {
                radioRegion2.IsChecked = true;
                txtSelectLanguage.Visibility = System.Windows.Visibility.Visible;
                cmboCulture.Visibility = System.Windows.Visibility.Visible;


            }

            radioRegion1.Checked += radioRegion1_Checked;
            radioRegion2.Checked += radioRegion2_Checked;


            checkControlsStatus();

            //bool shouldEnable = radioRegion1.IsChecked == true || cmboCulture.SelectedItem != null;


            ////  double opacity = 0.2;

            //if (shouldEnable)
            //{
            //    pathPlusNewOutbreak.IsEnabled = shouldEnable;
            //    pathCircleNewOutbreak.IsEnabled = shouldEnable;
            //    textBlockNewOutbreak.IsEnabled = shouldEnable;


            //    pathPlusNewOutbreak.Visibility = System.Windows.Visibility.Visible;
            //    pathCircleNewOutbreak.Visibility = System.Windows.Visibility.Visible;
            //    textBlockNewOutbreak.Visibility = System.Windows.Visibility.Visible;

            //    fileScreen.IsEnabled = true;




            //    //  opacity = 1;
            //}
            //else
            //{

            //    pathPlusNewOutbreak.IsEnabled = shouldEnable;
            //    pathCircleNewOutbreak.IsEnabled = shouldEnable;
            //    textBlockNewOutbreak.IsEnabled = shouldEnable;


            //    pathPlusNewOutbreak.Visibility = System.Windows.Visibility.Collapsed;
            //    pathCircleNewOutbreak.Visibility = System.Windows.Visibility.Collapsed;
            //    textBlockNewOutbreak.Visibility = System.Windows.Visibility.Collapsed;

            //    fileScreen.IsEnabled = false;


            //}

            // pathPlusNewOutbreak.Opacity = opacity;
            // pathCircleNewOutbreak.Opacity = opacity;
            // textBlockNewOutbreak.Opacity = opacity;


        }

        void ApplicationViewModel_RegionChanged(object sender, Core.Events.RegionChangedEventArgs e)
        {
            //  throw new NotImplementedException();
        }

        void transmissionScroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            double change = e.HorizontalChange;
            double offset = e.HorizontalOffset;
            TransmissionDates.Margin = new Thickness(-1 * offset, 0, 0, 0);
        }

        private void dataGridMain_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            e.Cancel = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.Application.EnableVisualStyles();

            System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
            lblVersion.Content = a.GetName().Version; // "0.9.4.0 Release Candidate 6"; 
        }

        private void btnAddCase_Click(object sender, RoutedEventArgs e)
        {
            //if (this.DataHelper.Country == "Liberia" || this.DataHelper.Country == "Sierra Leone" || this.DataHelper.Country == "Guinea")
            //{
            //    // now handling this through MVVM for these two
            //    //return;
            //    DataHelper.IsShortForm = true;
            //}

            if (DataHelper.IsShortForm) //17040
            {
                return;
            }

            if (DataHelper.IsCheckingServerForUpdates || DataHelper.IsWaitingOnOtherClients || DataHelper.IsSendingServerUpdates || DataHelper.IsLoadingProjectData || DataHelper.IsExportingData)
            {
                MessageBox.Show("Waiting on server updates. Please try again later.", "Waiting", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (DataHelper.IsConnected == false)
            {
                MessageBox.Show("Connection to the database has been lost.", "Lost connection", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Epi.Enter.EnterUIConfig uiConfig = Core.Common.GetCaseConfig(DataHelper.CaseForm, DataHelper.LabForm); //GetCaseConfig();

            if (uiConfig == null)
            {
                throw new InvalidOperationException("Enter UI config cannot be null.");
            }

            try
            {
                Epi.Windows.Enter.EnterMainForm emf = new Epi.Windows.Enter.EnterMainForm(DataHelper.Project, DataHelper.CaseForm, uiConfig);

                emf.RecordSaved += new SaveRecordEventHandler(emfCases_NewRecordSaved);

                emf.ShowDialog();
                emf.RecordSaved -= new SaveRecordEventHandler(emfCases_NewRecordSaved);
                emf.Dispose();
                emf = null;
                GC.Collect(0, GCCollectionMode.Forced, true);

            }
            catch (NullReferenceException)
            {
                GC.Collect(0, GCCollectionMode.Forced, true);
                MessageBox.Show("There was a problem with Epi Info 7. Please close and re-open the application to proceed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        public void emfCases_NewRecordSaved(object sender, SaveRecordEventArgs e)
        {
            string caseGuid = e.RecordGuid;
            if (e.Form == DataHelper.CaseForm)
            {
                DataHelper.UpdateOrAddCase.Execute(caseGuid);
                DataHelper.SendMessageForAddCase(caseGuid);
                if (borderExposures.Visibility == Visibility.Visible && dgCases.SelectedItems.Count > 0)
                {
                    CaseViewModel caseVM = dgCases.SelectedItem as CaseViewModel;
                    if (caseVM != null)
                    {
                        DataHelper.ShowSourceCasesForCase.Execute(caseVM);
                    }
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
                        DataHelper.SendMessageForAddCase(caseGuid);

                        if (borderExposures.Visibility == Visibility.Visible && dgCases.SelectedItems.Count > 0)
                        {
                            CaseViewModel caseVM = dgCases.SelectedItem as CaseViewModel;
                            if (caseVM != null)
                            {
                                DataHelper.ShowSourceCasesForCase.Execute(caseVM);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Epi.Windows.MsgBox.ShowException(ex);
                        return;
                    }
                }
            }
        }

        public void emfCases_RecordSaved(object sender, SaveRecordEventArgs e)
        {
            if (e.RecordGuid == null || String.IsNullOrEmpty(e.RecordGuid) || e.Form == null)
            {
                return;
            }

            string caseGuid = e.RecordGuid;
            if (e.Form == DataHelper.CaseForm)
            {
                DataHelper.UpdateOrAddCase.Execute(caseGuid);
                DataHelper.SendMessageForUpdateCase(caseGuid);

                if (borderExposures.Visibility == Visibility.Visible && dgCases.SelectedItems.Count > 0)
                {
                    CaseViewModel caseVM = dgCases.SelectedItem as CaseViewModel;
                    if (caseVM != null)
                    {
                        DataHelper.ShowSourceCasesForCase.Execute(caseVM);
                    }
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

                        if (borderExposures.Visibility == Visibility.Visible && dgCases.SelectedItems.Count > 0)
                        {
                            CaseViewModel caseVM = dgCases.SelectedItem as CaseViewModel;
                            if (caseVM != null)
                            {
                                DataHelper.ShowSourceCasesForCase.Execute(caseVM);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Epi.Windows.MsgBox.ShowException(ex);
                        return;
                    }
                }
            }
        }

        private void dgCases_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgCases.SelectedItems.Count > 0)
            {
                CaseViewModel caseVM = dgCases.SelectedItem as CaseViewModel;
                if (caseVM != null)
                {
                    DataHelper.ShowContactsForCase.Execute(caseVM);
                    if (borderExposures.Visibility == System.Windows.Visibility.Visible)
                    {
                        this.Cursor = Cursors.Wait;
                        DataHelper.ShowSourceCasesForCase.Execute(caseVM);
                        this.Cursor = Cursors.Arrow;
                    }
                }
            }
            else
            {
                DataHelper.CurrentSourceCaseCollection.Clear();
                DataHelper.CurrentContactLinkCollection.Clear();
            }
        }

        private void btnAddNewContact_Click(object sender, RoutedEventArgs e)
        {
            if (DataHelper.IsCheckingServerForUpdates || DataHelper.IsWaitingOnOtherClients || DataHelper.IsSendingServerUpdates || DataHelper.IsLoadingProjectData || DataHelper.IsExportingData)
            {
                MessageBox.Show("Waiting on server updates. Please try again later.", "Waiting", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (DataHelper.ContactFormType == 1)
            {
                if (dgCases.SelectedItems.Count == 1 && Popup == null)
                {
                    CaseViewModel caseVM = dgCases.SelectedItem as CaseViewModel;

                    if (caseVM == null)
                    {
                        return;
                    }

                    if (caseVM.IsLocked)
                    {
                        MessageBox.Show("Either this case or contacts of this case are locked for editing. Please wait until other users have released this lock before proceeding.", "Record locked", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    Popup = new Popup();
                    Popup.Parent = grdMain;

                    ContactEntryForm contactForm = new ContactEntryForm(DataHelper, caseVM, IsSuperUser);

                    contactForm.Closed += newContactForm_Closed;

                    contactForm.DataContext = this.DataContext;
                    contactForm.MaxWidth = 620;
                    contactForm.MaxHeight = 730;

                    //contactForm.LoadContactData(contactVM);

                    Popup.Content = contactForm;
                    Popup.Show();
                }
            }
            else if (DataHelper.ContactFormType == 0)
            {
                if (dgCases.SelectedItems.Count == 1)
                {
                    Epi.Windows.Enter.EnterMainForm emf = new Epi.Windows.Enter.EnterMainForm(DataHelper.Project, DataHelper.ContactForm, false, true);
                    emf.RecordSaved += new SaveRecordEventHandler(emfContacts_RecordSaved);
                    emf.ShowMenuStrip = false;
                    emf.ShowToolStrip = false;

                    emf.ShowDialog();
                    emf.RecordSaved -= new SaveRecordEventHandler(emfContacts_RecordSaved);

                    System.Threading.Thread.CurrentThread.CurrentUICulture = System.Threading.Thread.CurrentThread.CurrentCulture;
                }
            }
        }

        private void btnAddExistingContact_Click(object sender, RoutedEventArgs e)
        {
            if (DataHelper.IsCheckingServerForUpdates || DataHelper.IsWaitingOnOtherClients || DataHelper.IsSendingServerUpdates || DataHelper.IsLoadingProjectData || DataHelper.IsExportingData)
            {
                MessageBox.Show("Waiting on server updates. Please try again later.", "Waiting", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (dgCases.SelectedItems.Count == 1)
            {
                CaseViewModel caseVM = dgCases.SelectedItem as CaseViewModel;

                if (caseVM == null)
                {
                    return;
                }

                if (caseVM.IsLocked)
                {
                    MessageBox.Show("Either this case or contacts of this case are locked for editing. Please wait until other users have released this lock before proceeding.", "Record locked", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                else
                {
                    DataHelper.SendMessageForLockCase(caseVM);
                    existingContact.CaseVM = caseVM;
                    existingContact.Visibility = System.Windows.Visibility.Visible;
                }
            }
        }

        void existingContact_Click(object sender, RoutedEventArgs e)
        {
            CaseContactPairViewModel ccp = new CaseContactPairViewModel();
            ccp.CaseVM = dgCases.SelectedItem as CaseViewModel;

            if (ccp.CaseVM != null)
            {
                if (e.RoutedEvent.Name == "OK")
                {
                    if (!existingContact.DateLastContact.HasValue)
                    {
                        MessageBox.Show(Properties.Resources.ErrorNoDateLastContact, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    ccp.ContactVM = existingContact.ContactVM;
                    ccp.ContactRecordId = existingContact.ContactVM.RecordId;
                    ccp.Relationship = existingContact.Relationship;
                    ccp.DateLastContact = existingContact.DateLastContact.Value;
                    ccp.ContactType = existingContact.ContactType;
                    ccp.IsContactDateEstimated = existingContact.IsEstimated;

                    try
                    {
                        DataHelper.AddContact.Execute(ccp);
                    }
                    catch (Exception ex)
                    {
                        DbLogger.Log(String.Format(
                                "Adding existing contact to case failed. Exception type: {0}. Message: {1}",
                                ex.GetType().ToString(),
                                ex.Message
                                ));

                        if (ApplicationViewModel.Instance.CurrentRegion == RegionEnum.International)
                        {
                            MessageBox.Show(String.Format(
                                "[International mode] version {0}\n\nAn exception occurred while trying to add an existing contact to the case with ID {1}. Please give this message to the application developer.\n\nException type: {2}\n\n{3}\n\n{4}",
                                System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                                ccp.CaseVM.ID,
                                ex.GetType().ToString(),
                                ex.Message,
                                ex.StackTrace), Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            MessageBox.Show(String.Format(
                                "[US mode] version {0}\n\nAn exception occurred while trying to add an existing contact to the case with State ID {1}. Please give this message to the application developer.\n\nException type: {2}\n\n{3}\n\n{4}",
                                System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                                ccp.CaseVM.OriginalID,
                                ex.GetType().ToString(),
                                ex.Message,
                                ex.StackTrace), Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }

                    if (DataHelper.ShowCasesForContact.CanExecute(null))
                    {
                        DataHelper.ShowCasesForContact.Execute(ccp.ContactVM);
                    }
                }

                DataHelper.SendMessageForUnlockCase(ccp.CaseVM);
                existingContact.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                throw new InvalidOperationException("CaseVM is null in existingContact_Click");
            }
        }

        private void DeleteCase(CaseViewModel caseVM)
        {
            DataHelper.SendMessageForLockCase(caseVM);
            bool locked = true;

            System.Windows.Forms.DialogResult result = Epi.Windows.MsgBox.ShowQuestion(Properties.Resources.QuestionDeleteCaseAreYouSure);

            if (result.Equals(System.Windows.Forms.DialogResult.Yes))
            {
                Dialogs.AuthCodeDialog authDialog = new Dialogs.AuthCodeDialog(ContactTracing.Core.Constants.AUTH_CODE);
                System.Windows.Forms.DialogResult authResult = authDialog.ShowDialog();
                if (authResult == System.Windows.Forms.DialogResult.OK)
                {
                    if (authDialog.IsAuthorized)
                    {
                        try
                        {
                            string caseGuid = caseVM.RecordId;
                            if (this.DataContext != null)
                            {
                                EpiDataHelper dataHelper = this.DataContext as EpiDataHelper;
                                if (dataHelper != null)
                                {
                                    dataHelper.DeleteCase.Execute(caseGuid);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            //MessageBox.Show(String.Format("An exception occurred while trying to delete a case. Case ID: {0}. Please give this message to the application developer.\n{1}", caseVM.ID, ex.Message));
                            if (ApplicationViewModel.Instance.CurrentRegion == RegionEnum.International)
                            {
                                MessageBox.Show(String.Format(
                                    "[International mode]\n\nAn exception occurred while trying to delete a case. Case ID: {0}. Please give this message to the application developer.\n\nException type: {1}\n\n{2}\n\n{3}",
                                    caseVM.ID,
                                    ex.GetType().ToString(),
                                    ex.Message,
                                    ex.StackTrace), Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                            else
                            {
                                MessageBox.Show(String.Format(
                                    "[US mode]\n\nAn exception occurred while trying to delete a case. Case ID: {0}. Please give this message to the application developer.\n\nException type: {1}\n\n{2}\n\n{3}",
                                    caseVM.OriginalID,
                                    ex.GetType().ToString(),
                                    ex.Message,
                                    ex.StackTrace), Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        finally
                        {
                            DataHelper.SendMessageForUnlockCase(caseVM);
                            locked = false;
                        }
                    }
                }
            }

            if (locked)
            {
                DataHelper.SendMessageForUnlockCase(caseVM);
            }
        }

        private bool MatchFound(string value, string term)
        {
            if (term.StartsWith("\"") && term.EndsWith("\""))
            {
                term = term.Remove(0, 1);
                term = term.Remove(term.Length - 1, 1);
            }

            if (value.Equals(term))
            {
                return true;
            }
            else if (term.StartsWith("*") && term.EndsWith("*"))
            {
                if (value.Contains(term.Replace("*", String.Empty)))
                {
                    return true;
                }
            }
            else if (term.StartsWith("*"))
            {
                if (value.EndsWith(term.Replace("*", String.Empty)))
                {
                    return true;
                }
            }
            else if (term.EndsWith("*"))
            {
                if (value.StartsWith(term.Replace("*", String.Empty)))
                {
                    return true;
                }
            }

            return false;
        }

        private bool OperatorSearch(string value, string columnName, string term, string op)
        {
            string valueStr = value;
            double valueDbl;

            double newTermDbl;

            bool success = false;

            switch (op)
            {
                case "=":
                    if (MatchFound(valueStr, term)) return true;
                    break;
                case ">":
                    success = double.TryParse(term, out newTermDbl);
                    if (!success) return false;

                    success = double.TryParse(valueStr, out valueDbl);
                    if (!success) return false;

                    if (valueDbl > newTermDbl) return true;
                    break;
                case "<":
                    success = double.TryParse(term, out newTermDbl);
                    if (!success) return false;

                    success = double.TryParse(valueStr, out valueDbl);
                    if (!success) return false;

                    if (valueDbl < newTermDbl) return true;
                    break;
                case ">=":
                    success = double.TryParse(term, out newTermDbl);
                    if (!success) return false;

                    success = double.TryParse(valueStr, out valueDbl);
                    if (!success) return false;

                    if (valueDbl >= newTermDbl) return true;
                    break;
                case "<=":
                    success = double.TryParse(term, out newTermDbl);
                    if (!success) return false;

                    success = double.TryParse(valueStr, out valueDbl);
                    if (!success) return false;

                    if (valueDbl <= newTermDbl) return true;
                    break;
            }

            return false;
        }

        private void btnEditCase_Click(object sender, RoutedEventArgs e)
        {
            EditCase();
        }

        //private void QuickEditCase(CaseViewModel caseVM)
        //{
        //    return; 
        //    Popup = new Popup();
        //    Popup.Parent = grdMain;

        //    DataHelper.LoadExtendedCaseData(caseVM);

        //    QuickCaseEntryForm quickCaseForm = new QuickCaseEntryForm(caseVM);
        //    quickCaseForm.DataContext = this.DataContext;

        //    quickCaseForm.Closed += quickCaseForm_Closed;

        //    quickCaseForm.DataContext = this.DataContext;
        //    quickCaseForm.MaxWidth = 1010;

        //    Popup.Content = quickCaseForm;
        //    Popup.Show();
        //}

        private void EditContact(ContactViewModel contactVM)
        {
            if (DataHelper.IsCheckingServerForUpdates || DataHelper.IsWaitingOnOtherClients || DataHelper.IsSendingServerUpdates || DataHelper.IsLoadingProjectData || DataHelper.IsExportingData)
            {
                MessageBox.Show("Waiting on server updates. Please try again later.", "Waiting", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (DataHelper.IsConnected == false)
            {
                MessageBox.Show("Connection to the database has been lost.", "Lost connection", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (contactVM == null)
            {
                return;
            }

            if (contactVM.IsLocked)
            {
                MessageBox.Show("Either this contact or source cases of this contact are locked for editing. Please wait until other users have released this lock before proceeding.", "Record locked", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (DataHelper.ContactFormType == 1)
            {
                if (Popup == null)
                {
                    Popup = new Popup();
                    Popup.Parent = grdMain;

                    ContactEntryForm contactForm = new ContactEntryForm(DataHelper, contactVM, IsSuperUser);

                    contactForm.Closed += contactForm_Closed;

                    contactForm.DataContext = this.DataContext;
                    contactForm.MaxWidth = 620;
                    contactForm.MaxHeight = 730;

                    try
                    {
                        contactForm.LoadContactData(contactVM);
                        Popup.Content = contactForm;
                    }
                    catch (Exception ex)
                    {
                        contactForm.Closed -= contactForm_Closed;
                        Popup = null; // VHF-341 - this fixes the issue where the contact form will fail to appear after receiving this error message
                        MessageBox.Show("An error occurred while loading data into the contact entry form for contact number " + contactVM.ContactID + ". Exception message: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    Popup.Show();
                }
            }
            else if (DataHelper.ContactFormType == 0)
            {
                // old code that leverages Epi Info 7 ENTER

                DataHelper.SendMessageForLockContact(contactVM);
                Epi.Windows.Enter.EnterMainForm emf = null;

                try
                {
                    emf = new Epi.Windows.Enter.EnterMainForm(DataHelper.Project, DataHelper.ContactForm, false, true);
                }
                catch (NullReferenceException)
                {
                    DataHelper.SendMessageForUnlockContact(contactVM);
                    MessageBox.Show("There was a problem with Epi Info 7. Please close and re-open the application to proceed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                int uniqueKey = contactVM.UniqueKey;

                emf.LoadRecord(uniqueKey);

                emf.RecordSaved += new SaveRecordEventHandler(emfContacts_RecordSaved);
                emf.ShowMenuStrip = false;
                emf.ShowToolStrip = false;

                emf.ShowDialog();
                emf.RecordSaved -= new SaveRecordEventHandler(emfContacts_RecordSaved);

                System.Threading.Thread.CurrentThread.CurrentUICulture = System.Threading.Thread.CurrentThread.CurrentCulture;

                DataHelper.SendMessageForUnlockContact(contactVM);
            }
        }

        void emfContacts_RecordSaved(object sender, EventArgs saveRecordEventArgs)
        {
            if (saveRecordEventArgs is SaveRecordEventArgs)
            {
                if (((SaveRecordEventArgs)saveRecordEventArgs).Form.CurrentRecordId == 0)
                {
                    return;
                }
            }

            if (DataHelper.DoesContactExist(sender as string))
            {
                CaseContactPairViewModel ccp = new CaseContactPairViewModel();
                ccp.ContactRecordId = sender as string;
                ccp.CaseVM = (dgCases.SelectedItem as CaseViewModel);
                ccp.ContactVM = (dgContacts.SelectedItem as ContactViewModel);
                try
                {
                    ((this.DataContext) as EpiDataHelper).UpdateOrAddContact.Execute(ccp);
                }
                catch (Exception ex)
                {
                    //MessageBox.Show("An exception occured while trying to run an update/add routine on contact " + ccp.ContactVM.RecordId + " for case " + ccp.CaseVM.ID + ". Please contact the application developer with this message.\n" + ex.Message);

                    if (ApplicationViewModel.Instance.CurrentRegion == RegionEnum.International)
                    {
                        MessageBox.Show(String.Format(
                            "[International mode] version {0}\n\nAn exception occured while trying to run an update/add routine on contact {1} for case {2}. Please contact the application developer with this message.\n\nException type: {3}\n\n{4}\n\n{5}",
                            System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                            ccp.ContactVM.RecordId,
                            ccp.CaseVM.ID,
                            ex.GetType().ToString(),
                            ex.Message,
                            ex.StackTrace), Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show(String.Format(
                            "[US mode] version {0}\n\nAn exception occured while trying to run an update/add routine on contact {1} for case {2}. Please contact the application developer with this message.\n\nException type: {3}\n\n{4}\n\n{5}",
                            System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                            ccp.ContactVM.RecordId,
                            ccp.CaseVM.OriginalID,
                            ex.GetType().ToString(),
                            ex.Message,
                            ex.StackTrace), Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                Dialogs.NewContactDialog cDialog = new Dialogs.NewContactDialog(System.Threading.Thread.CurrentThread.CurrentCulture);
                if (cDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    CaseContactPairViewModel ccp = new CaseContactPairViewModel();
                    ccp.Relationship = cDialog.Relationship;
                    ccp.ContactType = cDialog.ContactType;
                    ccp.ContactRecordId = sender as string;
                    ccp.CaseVM = (dgCases.SelectedItem as CaseViewModel);
                    ccp.DateLastContact = cDialog.ContactDate;
                    ccp.IsContactDateEstimated = cDialog.IsEstimated;
                    ((this.DataContext) as EpiDataHelper).UpdateOrAddContact.Execute(ccp);
                }
                else
                {
                    try
                    {
                        DataHelper.DeleteContact.Execute(sender as string);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("An exception occured while trying to delete the contact with GlobalRecordId " + (sender as string) + ". Please contact the application developer with this message.\n" + ex.Message);
                    }
                }
            }
            DataHelper.RepopulateCollections();
        }

        //void quickCaseForm_Closed(object sender, EventArgs e)
        //{
        //    if (Popup.Content is QuickCaseEntryForm)
        //    {
        //        (Popup.Content as QuickCaseEntryForm).Closed -= quickCaseForm_Closed;
        //    }
        //    Popup.Close();
        //    Popup = null;
        //}

        void contactForm_Closed(object sender, EventArgs e)
        {
            if (Popup != null && Popup.Content != null)
            {
                ContactEntryForm form = Popup.Content as ContactEntryForm;
                if (form != null)
                {
                    form.Closed -= contactForm_Closed;
                }
            }
            Popup.Close();
            Popup = null;
        }

        void dataChecker_Closed(object sender, EventArgs e)
        {
            if (Popup != null && Popup.Content != null)
            {
                Controls.Diagnostics.DataChecker form = Popup.Content as Controls.Diagnostics.DataChecker;
                if (form != null)
                {
                    form.Closed -= dataChecker_Closed;
                }
            }
            //if (Popup.Content is Controls.Diagnostics.DataChecker)
            //{
            //    (Popup.Content as Controls.Diagnostics.DataChecker).Closed -= dataChecker_Closed;
            //}
            Popup.Close();
            Popup = null;
        }

        void districts_Closed(object sender, EventArgs e)
        {
            if (Popup != null && Popup.Content != null)
            {
                Controls.Diagnostics.DistrictNameEditor form = Popup.Content as Controls.Diagnostics.DistrictNameEditor;
                if (form != null)
                {
                    form.Closed -= districts_Closed;
                    //DataHelper.InitializeProject(new Epi.Project(DataHelper.Project.FullName));
                    //DataHelper.RepopulateCollections(false);
                }
            }
            //if (Popup.Content is Controls.Diagnostics.DistrictNameEditor)
            //{
            //    (Popup.Content as Controls.Diagnostics.DistrictNameEditor).Closed -= districts_Closed;
            //}
            Popup.Close();
            Popup = null;

            // If new locations were just added to drop-downs, RepopulateCollections
            bool districtNameEditorAndOK = false;
            try
            {
                districtNameEditorAndOK = ((ContactTracing.CaseView.Controls.Diagnostics.DistrictNameEditor)sender).isOK;
            }
            catch
            {

            }
            if (districtNameEditorAndOK)
            {
                DataHelper.Districts.Clear();
                DataHelper.SubCounties.Clear();
                DataHelper.DistrictsSubCounties.Clear();
                DataHelper.RepopulateCollections(false);
            }
        }

        void countries_Closed(object sender, EventArgs e)
        {
            if (Popup != null && Popup.Content != null)
            {
                Controls.Diagnostics.CountryNameEditor form = Popup.Content as Controls.Diagnostics.CountryNameEditor;
                if (form != null)
                {
                    form.Closed -= countries_Closed;
                }
            }
            //if (Popup.Content is Controls.Diagnostics.CountryNameEditor)
            //{
            //    (Popup.Content as Controls.Diagnostics.CountryNameEditor).Closed -= countries_Closed;
            //}
            Popup.Close();
            Popup = null;

            // If new countries were just added to drop-downs, RepopulateCollections
            bool countryNameEditorAndOK = false;
            try
            {
                countryNameEditorAndOK = ((ContactTracing.CaseView.Controls.Diagnostics.CountryNameEditor)sender).isOK;
            }
            catch
            {

            }
            if (countryNameEditorAndOK)
            {
                DataHelper.Countries.Clear();
                DataHelper.RepopulateCollections(false);
            }
        }

        void newContactForm_Closed(object sender, EventArgs e)
        {
            if (Popup != null && Popup.Content != null)
            {
                ContactEntryForm form = Popup.Content as ContactEntryForm;
                if (form != null)
                {
                    form.Closed -= newContactForm_Closed;
                }
            }

            Popup.Close();
            Popup = null;
        }

        void printSortForm_Print(object sender, EventArgs e)
        {
            if (Popup != null && Popup.Content != null)
            {
                FilterSortDropdown filterSortForm = Popup.Content as FilterSortDropdown;

                if (filterSortForm != null)
                {
                    PrintDailyFollowUp(filterSortForm);
                }
            }
        }

        void printSortForm_Print21DayFollowUp(object sender, EventArgs e)
        {
            if (Popup != null && Popup.Content != null)
            {
                FilterSortDropdown filterSortForm = Popup.Content as FilterSortDropdown;
                if (filterSortForm != null)
                {
                    Print21DayFollowUp(DataHelper.ContactCollection, filterSortForm);
                }
            }
        }

        void printSortForm_Closed(object sender, EventArgs e)
        {
            if (Popup != null && Popup.Content != null)
            {
                FilterSortSelectForm form = Popup.Content as FilterSortSelectForm;
                if (form != null)
                {
                    form.Closed -= printSortForm_Closed;
                }
            }

            Popup.Close();
            Popup = null;
        }

        private void AddCase()
        {
            if (DataHelper.IsCheckingServerForUpdates || DataHelper.IsWaitingOnOtherClients || DataHelper.IsSendingServerUpdates || DataHelper.IsLoadingProjectData || DataHelper.IsExportingData)
            {
                MessageBox.Show("Waiting on server updates. Please try again later.", "Waiting", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            CaseViewModel caseVM = dgCases.SelectedItem as CaseViewModel;

            if (caseVM == null)
            {
                // we should never arrive here, but have had strange error reports, so adding this as a precaution
                return;
            }

            if (caseVM.IsLocked)
            {
                MessageBox.Show("Either this case or contacts of this case are locked for editing. Please wait until other users have released this lock before proceeding.", "Record locked", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            else
            {
                Epi.Enter.EnterUIConfig uiConfig = Core.Common.GetCaseConfig(DataHelper.CaseForm, DataHelper.LabForm); //GetCaseConfig();

                DataHelper.SendMessageForLockCase(caseVM);

                Epi.Windows.Enter.EnterMainForm emf = null;

                try
                {
                    emf = new Epi.Windows.Enter.EnterMainForm(DataHelper.Project, DataHelper.CaseForm, uiConfig);
                }
                catch (NullReferenceException)
                {
                    DataHelper.SendMessageForUnlockCase(caseVM);
                    MessageBox.Show("There was a problem with Epi Info 7. Please close and re-open the application to proceed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    GC.Collect(0, GCCollectionMode.Forced, true);
                    return;
                }
                epiDataHelper.CaseCollection.Remove(caseVM); //deletes the un-needed row in CaseCollection when data is not being entered in shortcaseform #17054
                emf.RecordSaved += new SaveRecordEventHandler(emfCases_RecordSaved);
                emf.ShowDialog();
                emf.RecordSaved -= new SaveRecordEventHandler(emfCases_RecordSaved);
                emf.Dispose();
                emf = null;
                GC.Collect(0, GCCollectionMode.Forced, true);

                System.Threading.Thread.CurrentThread.CurrentUICulture = System.Threading.Thread.CurrentThread.CurrentCulture;

                DataHelper.SendMessageForUnlockCase(caseVM);
            }
        }

        private void EditCase(CaseViewModel cvm = null)
        {
            if (DataHelper.IsCheckingServerForUpdates || DataHelper.IsWaitingOnOtherClients || DataHelper.IsSendingServerUpdates || DataHelper.IsLoadingProjectData || DataHelper.IsExportingData)
            {
                MessageBox.Show("Waiting on server updates. Please try again later.", "Waiting", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            CaseViewModel caseVM = cvm;
            if (cvm == null)
            {
                caseVM = dgCases.SelectedItem as CaseViewModel;
            }



            if (caseVM == null)
            {
                // we should never arrive here, but have had strange error reports, so adding this as a precaution
                return;
            }

            if (caseVM.IsLocked)
            {
                MessageBox.Show("Either this case or contacts of this case are locked for editing. Please wait until other users have released this lock before proceeding.", "Record locked", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            else
            {
                Epi.Enter.EnterUIConfig uiConfig = Core.Common.GetCaseConfig(DataHelper.CaseForm, DataHelper.LabForm); //GetCaseConfig();

                DataHelper.SendMessageForLockCase(caseVM);

                Epi.Windows.Enter.EnterMainForm emf = null;

                try
                {
                    emf = new Epi.Windows.Enter.EnterMainForm(DataHelper.Project, DataHelper.CaseForm, uiConfig);
                }
                catch (NullReferenceException)
                {
                    DataHelper.SendMessageForUnlockCase(caseVM);
                    MessageBox.Show("There was a problem with Epi Info 7. Please close and re-open the application to proceed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    GC.Collect(0, GCCollectionMode.Forced, true);
                    return;
                }

                int uniqueKey = caseVM.UniqueKey;

                emf.LoadRecord(uniqueKey);
                emf.RecordSaved += new SaveRecordEventHandler(emfCases_RecordSaved);
                emf.ShowDialog();
                emf.RecordSaved -= new SaveRecordEventHandler(emfCases_RecordSaved);
                emf.Dispose();
                emf = null;
                GC.Collect(0, GCCollectionMode.Forced, true);

                System.Threading.Thread.CurrentThread.CurrentUICulture = System.Threading.Thread.CurrentThread.CurrentCulture;

                DataHelper.SendMessageForUnlockCase(caseVM);
            }
        }

        private void EditExposure()
        {
            CaseViewModel caseVM = ((CaseExposurePairViewModel)dgExposures.SelectedItem).ExposedCaseVM;

            if (caseVM.IsLocked)
            {
                MessageBox.Show("Either this case or contacts of this case are locked for editing. Please wait until other users have released this lock before proceeding.", "Record locked", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            else
            {
                DataHelper.SendMessageForLockCase(caseVM);
                Epi.Windows.Enter.EnterMainForm emf = null;

                try
                {
                    emf = new Epi.Windows.Enter.EnterMainForm(DataHelper.Project, DataHelper.CaseForm, false, true);
                }
                catch (NullReferenceException)
                {
                    DataHelper.SendMessageForUnlockCase(caseVM);
                    MessageBox.Show("There was a problem with Epi Info 7. Please close and re-open the application to proceed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                int uniqueKey = caseVM.UniqueKey;

                emf.LoadRecord(uniqueKey);

                emf.RecordSaved += new SaveRecordEventHandler(emfCases_RecordSaved);
                emf.ShowMenuStrip = false;
                emf.ShowToolStrip = false;

                emf.ShowDialog();
                emf.RecordSaved -= new SaveRecordEventHandler(emfCases_RecordSaved);

                DataHelper.SendMessageForUnlockCase(caseVM);
            }
        }

        private void btnEditContact_Click(object sender, RoutedEventArgs e)
        {
            CaseContactPairViewModel ccpVM = dgContacts.SelectedItem as CaseContactPairViewModel;
            if (ccpVM != null && ccpVM.ContactVM != null)
            {
                EditContact(ccpVM.ContactVM);
            }
        }

        private void btnPrint21Day_Click(object sender, RoutedEventArgs e)
        {
            ContactViewModel selectedContact = dgAllContacts.SelectedItem as ContactViewModel;

            if (selectedContact != null)
            {
                string baseFileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString("N");
                string imageFileName = baseFileName + ".png";
                BitmapSource img = (BitmapSource)ContactTracing.Core.Common.ToImageSource(panelChart);
                FileStream stream = new FileStream(imageFileName, FileMode.Create);
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                IMultiValueConverter dateConverter = new Converters.DateConverter();
                IValueConverter FinalOutcomeConverter = new Converters.FinalOutcomeConverter();
                encoder.Frames.Add(BitmapFrame.Create(img));
                encoder.Save(stream);
                stream.Close();

                StringBuilder htmlBuilder = new StringBuilder();

                htmlBuilder.AppendLine(ContactTracing.Core.Common.GetHtmlHeader().ToString());

                htmlBuilder.AppendLine("<h2>");
                htmlBuilder.AppendLine("Individual Report");
                htmlBuilder.AppendLine("<h2/>");

                htmlBuilder.AppendLine("<table style=\"width: 700px;\">");

                htmlBuilder.AppendLine(" <tr>");
                htmlBuilder.AppendLine("  <th colspan=\"2\">Patient information:</th>");
                htmlBuilder.AppendLine(" </tr>");

                if (ApplicationViewModel.Instance.CurrentRegion == Core.Enums.RegionEnum.USA)
                {
                    htmlBuilder.AppendLine(" <tr>");
                    htmlBuilder.AppendLine("  <td>" + Properties.Resources.ColHeaderID + ":</td>");
                    htmlBuilder.AppendLine("  <td>" + selectedContact.ContactCDCID + "</td>");
                    htmlBuilder.AppendLine(" </tr>");

                    htmlBuilder.AppendLine(" <tr>");
                    htmlBuilder.AppendLine("  <td>" + Properties.Resources.ColHeaderOriginalID + ":</td>");
                    htmlBuilder.AppendLine("  <td>" + selectedContact.ContactStateID + "</td>");
                    htmlBuilder.AppendLine(" </tr>");
                }

                htmlBuilder.AppendLine(" <tr>");
                htmlBuilder.AppendLine("  <td style=\"width: 250px;\">" + Properties.Resources.ColHeaderSurname + ":</td>");
                htmlBuilder.AppendLine("  <td style=\"width: 250px;\">" + selectedContact.Surname + "</td>");
                htmlBuilder.AppendLine(" </tr>");

                htmlBuilder.AppendLine(" <tr>");
                htmlBuilder.AppendLine("  <td>" + Properties.Resources.ColHeaderOtherNames + ":</td>");
                htmlBuilder.AppendLine("  <td>" + selectedContact.OtherNames + "</td>");
                htmlBuilder.AppendLine(" </tr>");

                htmlBuilder.AppendLine(" <tr>");
                htmlBuilder.AppendLine("  <td>Gender:</td>");
                htmlBuilder.AppendLine("  <td>" + selectedContact.Gender + "</td>");
                htmlBuilder.AppendLine(" </tr>");

                htmlBuilder.AppendLine(" <tr>");
                htmlBuilder.AppendLine("  <td>Age:</td>");
                if (selectedContact.AgeYears.HasValue)
                {
                    htmlBuilder.AppendLine("  <td>" + selectedContact.AgeYears + "</td>");
                }
                else
                {
                    htmlBuilder.AppendLine("  <td>&nbsp;</td>");
                }

                StringBuilder sb = new StringBuilder();

                foreach (CaseViewModel caseVM in DataHelper.CaseCollection)
                {
                    if (caseVM.Contacts.Contains(selectedContact))
                    {
                        sb.AppendLine(caseVM.ID + "&nbsp;" + caseVM.Surname + "&nbsp;" + caseVM.OtherNames + "<br />");
                    }
                }

                htmlBuilder.AppendLine(" <tr>");
                htmlBuilder.AppendLine("  <td>Cases had Contact With:</td>");
                htmlBuilder.AppendLine("  <td>" + sb.ToString() + "</td>");
                htmlBuilder.AppendLine(" </tr>");

                htmlBuilder.AppendLine(" <tr>");
                htmlBuilder.AppendLine("  <td>Date of Last Contact:</td>");

                string[] parmsValues = { selectedContact.DateOfLastContact.Value.ToString(), DataHelper.ApplicationCulture };
                var dateoflastcontact = dateConverter.Convert(parmsValues, null, null, null);

                htmlBuilder.AppendLine("  <td>" + dateoflastcontact + "</td>");
                //htmlBuilder.AppendLine("  <td>" + selectedContact.DateOfLastContact.Value.ToString("dd/MM/yyyy") + "</td>");
                htmlBuilder.AppendLine(" </tr>");

                parmsValues[0] = selectedContact.DateOfLastFollowUp.Value.ToString();
                var dateoflastfollowup = dateConverter.Convert(parmsValues, null, null, null);

                htmlBuilder.AppendLine(" <tr>");
                htmlBuilder.AppendLine("  <td>Date of Last Follow-up:</td>");
                //htmlBuilder.AppendLine("  <td>" + selectedContact.DateOfLastFollowUp.Value.ToString("dd/MM/yyyyy") + "</td>");
                htmlBuilder.AppendLine("  <td>" + dateoflastfollowup + "</td>");
                htmlBuilder.AppendLine(" </tr>");

                htmlBuilder.AppendLine(" <tr>");
                htmlBuilder.AppendLine("  <td>Final Outcome:</td>");
                //htmlBuilder.AppendLine("  <td>" + FinalOutcomeDisplay + "</td>");
                htmlBuilder.AppendLine("  <td>" + FinalOutcomeConverter.Convert(selectedContact.FinalOutcome, null, null, null) + "</td>");
                htmlBuilder.AppendLine(" </tr>");

                htmlBuilder.AppendLine(" <tr>");
                htmlBuilder.AppendLine("  <td>" + DataHelper.Adm4 + ":" + "</td>");
                htmlBuilder.AppendLine("  <td>" + selectedContact.Village + "</td>");
                htmlBuilder.AppendLine(" </tr>");

                htmlBuilder.AppendLine(" <tr>");
                htmlBuilder.AppendLine("  <td>" + DataHelper.Adm2 + ":" + "</td>");
                htmlBuilder.AppendLine("  <td>" + selectedContact.SubCounty + "</td>");
                htmlBuilder.AppendLine(" </tr>");

                htmlBuilder.AppendLine(" <tr>");
                htmlBuilder.AppendLine("  <td>" + DataHelper.Adm1 + ":" + "</td>");
                htmlBuilder.AppendLine("  <td>" + selectedContact.District + "</td>");
                htmlBuilder.AppendLine(" </tr>");

                htmlBuilder.AppendLine("</table>");

                htmlBuilder.AppendLine("<p style=\"height: 15px; clear: left;\" />");

                htmlBuilder.AppendLine("<img src=\"" + imageFileName + "\" />");
                htmlBuilder.AppendLine("</body>");
                htmlBuilder.AppendLine("</html>");

                string fileName = baseFileName + ".html";//GetHTMLLineListing();

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

        private string ParsePhoneNumber(string phoneNumber)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in phoneNumber)
            {
                int digit;
                bool result = int.TryParse(c.ToString(), out digit);
                if (result)
                {
                    sb.Append(c.ToString());
                }
            }

            return sb.ToString();
        }

        private void GenerateExcelDailyFollowUp(ObservableCollection<DailyCheckViewModel> collection, string fileName = "")
        {
            string baseFileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString("N") + ".xls";
            if (!String.IsNullOrEmpty(fileName))
            {
                baseFileName = fileName;
            }

            var workbook = new HSSFWorkbook();
            var sheet = workbook.CreateSheet("FollowUps");
            IValueConverter caseClassConverter = new Converters.EpiCaseClassificationConverter();
            // Add header labels
            var rowIndex = 0;
            var row = sheet.CreateRow(rowIndex);
            row.CreateCell(0).SetCellValue(Properties.Resources.ColHeaderContactID);
            row.CreateCell(1).SetCellValue(Properties.Resources.ColHeaderSurname);
            row.CreateCell(2).SetCellValue(Properties.Resources.ColHeaderOtherNames);
            row.CreateCell(3).SetCellValue(Properties.Resources.ColHeaderGender);
            row.CreateCell(4).SetCellValue(Properties.Resources.ColHeaderAge);

            row.CreateCell(5).SetCellValue(DataHelper.Adm1); // district
            row.CreateCell(6).SetCellValue(DataHelper.Adm2); // subcounty
            row.CreateCell(7).SetCellValue(Properties.Resources.ColHeaderVillage);

            row.CreateCell(8).SetCellValue(Properties.Resources.ColHeaderDateLastContact);
            row.CreateCell(9).SetCellValue(Properties.Resources.ColHeaderDateLastFollowUp);
            row.CreateCell(10).SetCellValue(Properties.Resources.ColHeaderDay);
            row.CreateCell(11).SetCellValue(Properties.Resources.HTMLColHeaderDateLastSeen);
            row.CreateCell(12).SetCellValue(Properties.Resources.ColHeaderTeam);
            row.CreateCell(13).SetCellValue("Source Case ID");
            row.CreateCell(14).SetCellValue("Source Case EpiCaseDef");
            row.CreateCell(15).SetCellValue("Source Case Name");
            row.CreateCell(16).SetCellValue(Properties.Resources.ColHeaderHeadHousehold);
            row.CreateCell(17).SetCellValue(Properties.Resources.ColHeaderPhone);
            row.CreateCell(18).SetCellValue(Properties.Resources.HTMLColHeaderHCWHealthFacility);
            row.CreateCell(19).SetCellValue(Properties.Resources.HTMLColHeaderStatus);
            row.CreateCell(20).SetCellValue(Properties.Resources.HTMLColHeaderNotes);
            rowIndex++;

            int indexDate = Core.Common.DaysInWindow - 1;

            // Add data rows
            foreach (DailyCheckViewModel dailyCheck in collection)
            {
                bool pastDue = dailyCheck.ContactVM.FollowUpWindowViewModel.WindowEndDate < DateTime.Today;

                FollowUpVisitViewModel lastCTVisitVM = null;
                foreach (FollowUpVisitViewModel fuVM in dailyCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits)
                {
                    if (fuVM.IsSeen /*fuVM.Seen == SeenType.Seen*/)
                    {
                        lastCTVisitVM = fuVM;
                    }

                    if (fuVM.FollowUpVisit.Date.Day == DateTime.Now.Day && fuVM.FollowUpVisit.Date.Month == DateTime.Now.Month && fuVM.FollowUpVisit.Date.Year == DateTime.Now.Year)
                    {
                        break;
                    }
                }

                FollowUpVisitViewModel lastDayVM = dailyCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate];

                //htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + dailyCheck.ContactVM.FollowUpWindowViewModel.WindowStartDate.AddDays(-1).ToString("d/M/yy") + "</td>");
                //htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + lastDayVM.FollowUpVisit.Date.ToString("d/M/yy") + "</td>");

                //if (pastDue)
                //{
                //    htmlBuilder.AppendLine("<td style=\"text-align: right;\">&gt;21</td>");
                //}
                //else
                //{
                //    if (collection == DataHelper.DailyFollowUpCollection)
                //    {

                //        htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + dailyCheck.Day + "</td>");                            
                //    }
                //    else if (collection == DataHelper.PrevFollowUpCollection && dpPrev.SelectedDate.HasValue)
                //    {
                //        TimeSpan ts = dpPrev.SelectedDate.Value - dailyCheck.ContactVM.FollowUpWindowViewModel.WindowStartDate;
                //        htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + (ts.Days + 1).ToString() + "</td>");
                //    }                        
                //}

                //if (lastCTVisitVM != null)
                //{
                //    htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + lastCTVisitVM.FollowUpVisit.Date.ToString("d/M/yy") + "</td>");
                //}
                //else
                //{
                //    htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + Properties.Resources.Never + "</td>");
                //}

                string dateLastSeen = Properties.Resources.Never;

                if (lastCTVisitVM != null)
                {
                    dateLastSeen = lastCTVisitVM.FollowUpVisit.Date.ToShortDateString();
                }

                row = sheet.CreateRow(rowIndex);
                row.CreateCell(0).SetCellValue(dailyCheck.ContactVM.ContactID);
                row.CreateCell(1).SetCellValue(dailyCheck.ContactVM.Surname);
                row.CreateCell(2).SetCellValue(dailyCheck.ContactVM.OtherNames);
                row.CreateCell(3).SetCellValue(dailyCheck.ContactVM.GenderAbbreviation);
                row.CreateCell(4).SetCellValue(dailyCheck.ContactVM.AgeYears.ToString());

                row.CreateCell(5).SetCellValue(dailyCheck.ContactVM.District);
                row.CreateCell(6).SetCellValue(dailyCheck.ContactVM.SubCounty);
                row.CreateCell(7).SetCellValue(dailyCheck.ContactVM.Village);

                row.CreateCell(8).SetCellValue(dailyCheck.ContactVM.DateOfLastContact.Value.ToShortDateString());
                row.CreateCell(9).SetCellValue(dailyCheck.ContactVM.DateOfLastFollowUp.Value.ToShortDateString());
                row.CreateCell(10).SetCellValue(dailyCheck.Day.ToString());
                row.CreateCell(11).SetCellValue(dateLastSeen);
                row.CreateCell(12).SetCellValue(dailyCheck.ContactVM.Team);
                row.CreateCell(13).SetCellValue(dailyCheck.ContactVM.LastSourceCase.ID);
                row.CreateCell(14).SetCellValue(caseClassConverter.Convert(dailyCheck.ContactVM.LastSourceCase.EpiCaseDef, null, null, null).ToString());
                row.CreateCell(15).SetCellValue(dailyCheck.ContactVM.LastSourceCase.OtherNames + " " + dailyCheck.ContactVM.LastSourceCase.Surname);
                row.CreateCell(16).SetCellValue(dailyCheck.ContactVM.HeadOfHousehold);
                row.CreateCell(17).SetCellValue(dailyCheck.ContactVM.Phone);
                row.CreateCell(18).SetCellValue(dailyCheck.ContactVM.HCWFacility);
                row.CreateCell(19).SetCellValue(String.Empty);
                row.CreateCell(20).SetCellValue(String.Empty);
                rowIndex++;
            }

            DateTime? originalDate = dpPrev.SelectedDate;
            DateTime dt = DateTime.Now;
            DateTime minDate = dt.AddDays(-1 * ContactTracing.Core.Common.DaysInWindow);

            //if (collection == DataHelper.PrevFollowUpCollection)
            //{
            //    if (!dpPrev.SelectedDate.HasValue)
            //    {
            //        return;
            //    }
            //    minDate = dpPrev.SelectedDate.Value;
            //}

            List<ContactViewModel> collected = new List<ContactViewModel>();

            DateTime incDate = minDate.AddDays(-600);
            while (incDate < DateTime.Today)
            {
                incDate = incDate.AddDays(1);

                DataHelper.ShowContactsForDateforFollowup.Execute(incDate);

                var query = from prevCheck in DataHelper.PrevFollowUpCollection
                            where
                            (!prevCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate].Status.HasValue || prevCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate].Status == ContactDailyStatus.NotSeen ||
                            prevCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate].Status == ContactDailyStatus.NotRecorded)
                            &&
                            !prevCheck.ContactVM.HasFinalOutcome &&
                            prevCheck.ContactVM.FollowUpWindowViewModel.WindowStartDate < minDate
                            select prevCheck;

                foreach (var entry in query)
                {
                    DailyCheckViewModel dailyCheck = entry as DailyCheckViewModel;

                    if (dailyCheck != null && !collection.Contains(dailyCheck) && !collected.Contains(dailyCheck.ContactVM))
                    {
                        collected.Add(dailyCheck.ContactVM);
                        bool pastDue = dailyCheck.ContactVM.FollowUpWindowViewModel.WindowEndDate < DateTime.Today;

                        FollowUpVisitViewModel lastCTVisitVM = null;
                        foreach (FollowUpVisitViewModel fuVM in dailyCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits)
                        {
                            if (fuVM.IsSeen)
                            {
                                lastCTVisitVM = fuVM;
                            }

                            if (fuVM.FollowUpVisit.Date.Day == DateTime.Now.Day && fuVM.FollowUpVisit.Date.Month == DateTime.Now.Month && fuVM.FollowUpVisit.Date.Year == DateTime.Now.Year)
                            {
                                break;
                            }
                        }

                        FollowUpVisitViewModel lastDayVM = dailyCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate];

                        string dateLastSeen = Properties.Resources.Never;

                        if (lastCTVisitVM != null)
                        {
                            dateLastSeen = lastCTVisitVM.FollowUpVisit.Date.ToShortDateString();
                        }

                        string day = dailyCheck.Day.ToString();
                        if (dailyCheck.Day == -1)
                        {
                            day = ">" + Core.Common.DaysInWindow.ToString();
                        }

                        row = sheet.CreateRow(rowIndex);
                        row.CreateCell(0).SetCellValue(dailyCheck.ContactVM.ContactID);
                        row.CreateCell(1).SetCellValue(dailyCheck.ContactVM.Surname);
                        row.CreateCell(2).SetCellValue(dailyCheck.ContactVM.OtherNames);
                        row.CreateCell(3).SetCellValue(dailyCheck.ContactVM.GenderAbbreviation);
                        row.CreateCell(4).SetCellValue(dailyCheck.ContactVM.AgeYears.ToString());

                        row.CreateCell(5).SetCellValue(dailyCheck.ContactVM.District);
                        row.CreateCell(6).SetCellValue(dailyCheck.ContactVM.SubCounty);
                        row.CreateCell(7).SetCellValue(dailyCheck.ContactVM.Village);

                        row.CreateCell(8).SetCellValue(dailyCheck.ContactVM.DateOfLastContact.Value.ToShortDateString());
                        row.CreateCell(9).SetCellValue(dailyCheck.ContactVM.DateOfLastFollowUp.Value.ToShortDateString());
                        row.CreateCell(10).SetCellValue(day);
                        row.CreateCell(11).SetCellValue(dateLastSeen);
                        row.CreateCell(12).SetCellValue(dailyCheck.ContactVM.Team);
                        row.CreateCell(13).SetCellValue(dailyCheck.ContactVM.LastSourceCase.ID);
                        row.CreateCell(14).SetCellValue(caseClassConverter.Convert(dailyCheck.ContactVM.LastSourceCase.EpiCaseDef, null, null, null).ToString());
                        row.CreateCell(15).SetCellValue(dailyCheck.ContactVM.LastSourceCase.OtherNames + " " + dailyCheck.ContactVM.LastSourceCase.Surname);
                        row.CreateCell(16).SetCellValue(dailyCheck.ContactVM.HeadOfHousehold);
                        row.CreateCell(17).SetCellValue(dailyCheck.ContactVM.Phone);
                        row.CreateCell(18).SetCellValue(dailyCheck.ContactVM.HCWFacility);
                        row.CreateCell(19).SetCellValue(String.Empty);
                        row.CreateCell(20).SetCellValue(String.Empty);
                        rowIndex++;
                    }
                }
            }

            dpPrev.SelectedDate = originalDate;

            // Save the Excel spreadsheet to a file on the web server's file system
            using (var fileData = new FileStream(baseFileName, FileMode.Create))
            {
                workbook.Write(fileData);
            }

            if (!string.IsNullOrEmpty(baseFileName))
            {
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = "\"" + baseFileName + "\"";
                proc.StartInfo.UseShellExecute = true;
                proc.Start();
            }
        }

        private void GenerateExcelDailyFollowUpforUS(ObservableCollection<DailyCheckViewModel> collection, string fileName = "")
        {
            string baseFileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString("N") + ".xls";
            if (!String.IsNullOrEmpty(fileName))
            {
                baseFileName = fileName;
            }

            var workbook = new HSSFWorkbook();
            var sheet = workbook.CreateSheet("FollowUps");
            IValueConverter caseClassConverter = new Converters.EpiCaseClassificationConverter();
            // Add header labels
            var rowIndex = 0;
            var row = sheet.CreateRow(rowIndex);
            row.CreateCell(0).SetCellValue("CDC ID");
            row.CreateCell(1).SetCellValue("State/Local ID");
            row.CreateCell(2).SetCellValue(Properties.Resources.ColHeaderSurname);
            row.CreateCell(3).SetCellValue(Properties.Resources.ColHeaderOtherNames);
            row.CreateCell(4).SetCellValue(Properties.Resources.ColHeaderGender);
            row.CreateCell(5).SetCellValue(Properties.Resources.ColHeaderAge);

            row.CreateCell(6).SetCellValue(DataHelper.Adm1); // district
            row.CreateCell(7).SetCellValue(DataHelper.Adm2); // subcounty
            row.CreateCell(8).SetCellValue(Properties.Resources.ColHeaderVillage);

            row.CreateCell(9).SetCellValue(Properties.Resources.ColHeaderDateLastContact);
            row.CreateCell(10).SetCellValue(Properties.Resources.ColHeaderDateLastFollowUp);
            row.CreateCell(11).SetCellValue(Properties.Resources.ColHeaderDay);
            row.CreateCell(12).SetCellValue(Properties.Resources.HTMLColHeaderDateLastSeen);
            row.CreateCell(13).SetCellValue(Properties.Resources.ColHeaderTeam);
            row.CreateCell(14).SetCellValue("Source Case ID");
            row.CreateCell(15).SetCellValue("Source Case EpiCaseDef");
            row.CreateCell(16).SetCellValue("Source Case Name");
            row.CreateCell(17).SetCellValue("Address");
            row.CreateCell(18).SetCellValue(Properties.Resources.ColHeaderPhone);
            row.CreateCell(19).SetCellValue(Properties.Resources.HTMLColHeaderHCWHealthFacility);
            row.CreateCell(20).SetCellValue(Properties.Resources.HTMLColHeaderStatus);
            row.CreateCell(21).SetCellValue(Properties.Resources.HTMLColHeaderNotes);
            rowIndex++;

            int indexDate = Core.Common.DaysInWindow - 1;

            // Add data rows
            foreach (DailyCheckViewModel dailyCheck in collection)
            {
                bool pastDue = dailyCheck.ContactVM.FollowUpWindowViewModel.WindowEndDate < DateTime.Today;

                FollowUpVisitViewModel lastCTVisitVM = null;
                foreach (FollowUpVisitViewModel fuVM in dailyCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits)
                {
                    if (fuVM.IsSeen /*fuVM.Seen == SeenType.Seen*/)
                    {
                        lastCTVisitVM = fuVM;
                    }

                    if (fuVM.FollowUpVisit.Date.Day == DateTime.Now.Day && fuVM.FollowUpVisit.Date.Month == DateTime.Now.Month && fuVM.FollowUpVisit.Date.Year == DateTime.Now.Year)
                    {
                        break;
                    }
                }

                FollowUpVisitViewModel lastDayVM = dailyCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate];

                //htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + dailyCheck.ContactVM.FollowUpWindowViewModel.WindowStartDate.AddDays(-1).ToString("d/M/yy") + "</td>");
                //htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + lastDayVM.FollowUpVisit.Date.ToString("d/M/yy") + "</td>");

                //if (pastDue)
                //{
                //    htmlBuilder.AppendLine("<td style=\"text-align: right;\">&gt;21</td>");
                //}
                //else
                //{
                //    if (collection == DataHelper.DailyFollowUpCollection)
                //    {

                //        htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + dailyCheck.Day + "</td>");                            
                //    }
                //    else if (collection == DataHelper.PrevFollowUpCollection && dpPrev.SelectedDate.HasValue)
                //    {
                //        TimeSpan ts = dpPrev.SelectedDate.Value - dailyCheck.ContactVM.FollowUpWindowViewModel.WindowStartDate;
                //        htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + (ts.Days + 1).ToString() + "</td>");
                //    }                        
                //}

                //if (lastCTVisitVM != null)
                //{
                //    htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + lastCTVisitVM.FollowUpVisit.Date.ToString("d/M/yy") + "</td>");
                //}
                //else
                //{
                //    htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + Properties.Resources.Never + "</td>");
                //}

                string dateLastSeen = Properties.Resources.Never;

                if (lastCTVisitVM != null)
                {
                    dateLastSeen = lastCTVisitVM.FollowUpVisit.Date.ToShortDateString();
                }

                row = sheet.CreateRow(rowIndex);
                row.CreateCell(0).SetCellValue(dailyCheck.ContactVM.ContactCDCID);
                row.CreateCell(1).SetCellValue(dailyCheck.ContactVM.ContactStateID);
                row.CreateCell(2).SetCellValue(dailyCheck.ContactVM.Surname);
                row.CreateCell(3).SetCellValue(dailyCheck.ContactVM.OtherNames);
                row.CreateCell(4).SetCellValue(dailyCheck.ContactVM.GenderAbbreviation);
                row.CreateCell(5).SetCellValue(dailyCheck.ContactVM.AgeYears.ToString());

                row.CreateCell(6).SetCellValue(dailyCheck.ContactVM.District);
                row.CreateCell(7).SetCellValue(dailyCheck.ContactVM.SubCounty);
                row.CreateCell(8).SetCellValue(dailyCheck.ContactVM.Village);

                row.CreateCell(9).SetCellValue(dailyCheck.ContactVM.DateOfLastContact.Value.ToShortDateString());
                row.CreateCell(10).SetCellValue(dailyCheck.ContactVM.DateOfLastFollowUp.Value.ToShortDateString());
                row.CreateCell(11).SetCellValue(dailyCheck.Day.ToString());
                row.CreateCell(12).SetCellValue(dateLastSeen);
                row.CreateCell(13).SetCellValue(dailyCheck.ContactVM.Team);
                row.CreateCell(14).SetCellValue(dailyCheck.ContactVM.LastSourceCase.OriginalID);
                row.CreateCell(15).SetCellValue(caseClassConverter.Convert(dailyCheck.ContactVM.LastSourceCase.EpiCaseDef, null, null, null).ToString());
                row.CreateCell(16).SetCellValue(dailyCheck.ContactVM.LastSourceCase.OtherNames + " " + dailyCheck.ContactVM.LastSourceCase.Surname);
                row.CreateCell(17).SetCellValue(dailyCheck.ContactVM.ContactAddress);
                row.CreateCell(18).SetCellValue(dailyCheck.ContactVM.Phone);
                row.CreateCell(19).SetCellValue(dailyCheck.ContactVM.HCWFacility);
                row.CreateCell(20).SetCellValue(String.Empty);
                row.CreateCell(21).SetCellValue(String.Empty);
                rowIndex++;
            }

            DateTime? originalDate = dpPrev.SelectedDate;
            DateTime dt = DateTime.Now;
            DateTime minDate = dt.AddDays(-1 * ContactTracing.Core.Common.DaysInWindow);

            //if (collection == DataHelper.PrevFollowUpCollection)
            //{
            //    if (!dpPrev.SelectedDate.HasValue)
            //    {
            //        return;
            //    }
            //    minDate = dpPrev.SelectedDate.Value;
            //}

            List<ContactViewModel> collected = new List<ContactViewModel>();

            DateTime incDate = minDate.AddDays(-600);
            while (incDate < DateTime.Today)
            {
                incDate = incDate.AddDays(1);

                DataHelper.ShowContactsForDateforFollowup.Execute(incDate);

                var query = from prevCheck in DataHelper.PrevFollowUpCollection
                            where
                            (!prevCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate].Status.HasValue || prevCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate].Status == ContactDailyStatus.NotSeen ||
                            prevCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate].Status == ContactDailyStatus.NotRecorded)
                            &&
                            !prevCheck.ContactVM.HasFinalOutcome &&
                            prevCheck.ContactVM.FollowUpWindowViewModel.WindowStartDate < minDate
                            select prevCheck;

                foreach (var entry in query)
                {
                    DailyCheckViewModel dailyCheck = entry as DailyCheckViewModel;

                    if (dailyCheck != null && !collection.Contains(dailyCheck) && !collected.Contains(dailyCheck.ContactVM))
                    {
                        collected.Add(dailyCheck.ContactVM);
                        bool pastDue = dailyCheck.ContactVM.FollowUpWindowViewModel.WindowEndDate < DateTime.Today;

                        FollowUpVisitViewModel lastCTVisitVM = null;
                        foreach (FollowUpVisitViewModel fuVM in dailyCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits)
                        {
                            if (fuVM.IsSeen)
                            {
                                lastCTVisitVM = fuVM;
                            }

                            if (fuVM.FollowUpVisit.Date.Day == DateTime.Now.Day && fuVM.FollowUpVisit.Date.Month == DateTime.Now.Month && fuVM.FollowUpVisit.Date.Year == DateTime.Now.Year)
                            {
                                break;
                            }
                        }

                        FollowUpVisitViewModel lastDayVM = dailyCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate];

                        string dateLastSeen = Properties.Resources.Never;

                        if (lastCTVisitVM != null)
                        {
                            dateLastSeen = lastCTVisitVM.FollowUpVisit.Date.ToShortDateString();
                        }

                        string day = dailyCheck.Day.ToString();
                        if (dailyCheck.Day == -1)
                        {
                            day = ">" + Core.Common.DaysInWindow.ToString();
                        }

                        row = sheet.CreateRow(rowIndex);
                        row.CreateCell(0).SetCellValue(dailyCheck.ContactVM.ContactCDCID);
                        row.CreateCell(1).SetCellValue(dailyCheck.ContactVM.ContactStateID);
                        row.CreateCell(2).SetCellValue(dailyCheck.ContactVM.Surname);
                        row.CreateCell(3).SetCellValue(dailyCheck.ContactVM.OtherNames);
                        row.CreateCell(4).SetCellValue(dailyCheck.ContactVM.GenderAbbreviation);
                        row.CreateCell(5).SetCellValue(dailyCheck.ContactVM.AgeYears.ToString());
                        row.CreateCell(6).SetCellValue(dailyCheck.ContactVM.District);
                        row.CreateCell(7).SetCellValue(dailyCheck.ContactVM.SubCounty);
                        row.CreateCell(8).SetCellValue(dailyCheck.ContactVM.Village);

                        row.CreateCell(9).SetCellValue(dailyCheck.ContactVM.DateOfLastContact.Value.ToShortDateString());
                        row.CreateCell(10).SetCellValue(dailyCheck.ContactVM.DateOfLastFollowUp.Value.ToShortDateString());
                        row.CreateCell(11).SetCellValue(day);
                        row.CreateCell(12).SetCellValue(dateLastSeen);
                        row.CreateCell(13).SetCellValue(dailyCheck.ContactVM.Team);
                        row.CreateCell(14).SetCellValue(dailyCheck.ContactVM.LastSourceCase.ID);
                        row.CreateCell(15).SetCellValue(caseClassConverter.Convert(dailyCheck.ContactVM.LastSourceCase.EpiCaseDef, null, null, null).ToString());
                        row.CreateCell(16).SetCellValue(dailyCheck.ContactVM.LastSourceCase.OtherNames + " " + dailyCheck.ContactVM.LastSourceCase.Surname);
                        row.CreateCell(17).SetCellValue(dailyCheck.ContactVM.ContactAddress);
                        row.CreateCell(18).SetCellValue(dailyCheck.ContactVM.Phone);
                        row.CreateCell(19).SetCellValue(dailyCheck.ContactVM.HCWFacility);
                        row.CreateCell(20).SetCellValue(String.Empty);
                        row.CreateCell(21).SetCellValue(String.Empty);
                        rowIndex++;
                    }
                }
            }

            dpPrev.SelectedDate = originalDate;

            // Save the Excel spreadsheet to a file on the web server's file system
            using (var fileData = new FileStream(baseFileName, FileMode.Create))
            {
                workbook.Write(fileData);
            }

            if (!string.IsNullOrEmpty(baseFileName))
            {
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = "\"" + baseFileName + "\"";
                proc.StartInfo.UseShellExecute = true;
                proc.Start();
            }
        }

        private IEnumerable<DailyCheckViewModel> FilterCollection(ObservableCollection<DailyCheckViewModel> collection, DateTime minDate, int filterBoundryAggregateLevel, string filterBoundry, string filterTeam, string filterFacility, DateTime? addedSince, DateTime? seenSince)
        {
            var query = from contact in collection
                        where FilterCollectionWhere(contact, minDate, filterBoundryAggregateLevel, filterBoundry, filterTeam, filterFacility, addedSince, seenSince)
                        select contact;

            return query;
        }

        private IEnumerable<ContactViewModel> FilterCollection(ObservableCollection<ContactViewModel> collection, DateTime minDate, int filterBoundryAggregateLevel, string filterBoundry, string filterTeam, string filterFacility, DateTime? addedSince, DateTime? seenSince)
        {
            var query = from contact in collection
                        where FilterCollectionWhere(contact, minDate, filterBoundryAggregateLevel, filterBoundry, filterTeam, filterFacility, addedSince, seenSince)
                        select contact;

            return query;
        }

        private bool FilterCollectionWhere(DailyCheckViewModel contact, DateTime minDate, int filterBoundryAggregateLevel, string filterBoundry, string filterTeam, string filterFacility, DateTime? addedSince, DateTime? seenSince)
        {
            if (contact.ContactVM.FollowUpWindowViewModel.WindowStartDate < minDate) return false;
            if (contact.ContactVM.HasFinalOutcome) return false;

            if (addedSince != null && addedSince > contact.ContactVM.FirstSaveTime) return false;
            if (seenSince != null && seenSince > contact.ContactVM.DateOfLastFollowUp) return false;

            if (string.IsNullOrEmpty(filterTeam) == false && filterTeam != contact.ContactVM.Team) return false;
            if (string.IsNullOrEmpty(filterFacility) == false && filterFacility != contact.ContactVM.HCWFacility) return false;

            if (DataHelper.FilterSelectedBoundry == 0)
            {
                if (filterBoundry != contact.ContactVM.Village) return false;
            }
            else if (DataHelper.FilterSelectedBoundry == 2)
            {
                if (filterBoundry != contact.ContactVM.SubCounty) return false;
            }
            else if (DataHelper.FilterSelectedBoundry == 3)
            {
                if (filterBoundry != contact.ContactVM.District) return false;
            }

            return true;
        }

        private bool FilterCollectionWhere(ContactViewModel contact, DateTime minDate, int filterBoundryAggregateLevel, string filterBoundry, string filterTeam, string filterFacility, DateTime? addedSince, DateTime? seenSince)
        {
            if (contact.FollowUpWindowViewModel.WindowStartDate < minDate) return false;
            if (contact.HasFinalOutcome) return false;

            if (addedSince != null && addedSince > contact.FirstSaveTime) return false;
            if (seenSince != null && seenSince > contact.DateOfLastFollowUp) return false;

            if (string.IsNullOrEmpty(filterTeam) == false && filterTeam != contact.Team) return false;
            if (string.IsNullOrEmpty(filterFacility) == false && filterFacility != contact.HCWFacility) return false;

            if (DataHelper.FilterSelectedBoundry == 0)
            {
                if (filterBoundry != contact.Village) return false;
            }
            else if (DataHelper.FilterSelectedBoundry == 2)
            {
                if (filterBoundry != contact.SubCounty) return false;
            }
            else if (DataHelper.FilterSelectedBoundry == 3)
            {
                if (filterBoundry != contact.District) return false;
            }

            return true;
        }

        private IEnumerable<IGrouping<string, DailyCheckViewModel>> GetDailyCheck(ObservableCollection<DailyCheckViewModel> collection, DateTime minDate, ContactTracing.Core.Enums.LocationType locationType = Core.Enums.LocationType.Village)
        {
            if (locationType == Core.Enums.LocationType.Village)
            {
                var query = from dailyCheck in collection
                            where dailyCheck.ContactVM.FollowUpWindowViewModel.WindowStartDate >= minDate && !dailyCheck.ContactVM.HasFinalOutcome
                            group dailyCheck by String.Concat("<span style=\"font-weight: bold;\">" + DataHelper.Adm4 + ":</span> ", dailyCheck.ContactVM.Village);
                return query as IEnumerable<IGrouping<string, DailyCheckViewModel>>;
            }
            else if (locationType == Core.Enums.LocationType.District)
            {
                var query = from dailyCheck in collection
                            where dailyCheck.ContactVM.FollowUpWindowViewModel.WindowStartDate >= minDate && !dailyCheck.ContactVM.HasFinalOutcome
                            group dailyCheck by String.Concat("<span style=\"font-weight: bold;\">" + DataHelper.Adm1 + ":</span> ", dailyCheck.ContactVM.District);
                return query as IEnumerable<IGrouping<string, DailyCheckViewModel>>;
            }
            else if (locationType == Core.Enums.LocationType.SubCounty)
            {
                var query = from dailyCheck in collection
                            where dailyCheck.ContactVM.FollowUpWindowViewModel.WindowStartDate >= minDate && !dailyCheck.ContactVM.HasFinalOutcome
                            group dailyCheck by String.Concat("<span style=\"font-weight: bold;\">" + DataHelper.Adm2 + ":</span> ", dailyCheck.ContactVM.SubCounty);
                return query as IEnumerable<IGrouping<string, DailyCheckViewModel>>;
            }
            else
            {
                throw new NotImplementedException("The specified location field is unavailable for contact tracing.");
            }
        }


        private List<DailyCheckViewModel> GetDailyCheckList(ObservableCollection<DailyCheckViewModel> collection, DateTime minDate, string filterClause, string sortClause)
        {
            List<DailyCheckViewModel> returnList = new List<DailyCheckViewModel>();
            string key = string.Empty;

            String whereClause = "ContactVM.FollowUpWindowViewModel.WindowStartDate >= DATETIME(" + minDate.Year + "," + minDate.Month + "," + minDate.Day + ") and ContactVM.HasFinalOutcome == false";
            if (string.IsNullOrEmpty(filterClause) == false)
            {
                filterClause = filterClause.TrimStart();
                if (filterClause.StartsWith("and "))
                {
                    filterClause = filterClause.Substring(4);
                }
                whereClause = whereClause + " and " + filterClause;
            }

            String orderClause = sortClause;

            if (orderClause == "")
            {
                var dynGroup2 = collection.Where(whereClause);
                return dynGroup2.ToList<DailyCheckViewModel>();
            }
            else
            {
                var dynGroup2 = collection.Where(whereClause).OrderBy(orderClause);
                return dynGroup2.ToList<DailyCheckViewModel>();
            }
        }

        private SortedDictionary<string, List<ContactViewModel>> GetContactDictionary(ObservableCollection<ContactViewModel> collection, DateTime minDate, ContactTracing.Core.Enums.LocationType locationType = Core.Enums.LocationType.Village, bool isSortOnTeam = false)
        {
            SortedDictionary<string, List<ContactViewModel>> followUpDictionary = new SortedDictionary<string, List<ContactViewModel>>();
            string key = string.Empty;

            if (isSortOnTeam == false && locationType == Core.Enums.LocationType.District)
            {
                var grouped = from dcvm in collection
                              where dcvm.FollowUpWindowViewModel.WindowStartDate >= minDate && !dcvm.HasFinalOutcome
                              group dcvm by new { district = dcvm.District } into newGroup
                              select newGroup;

                foreach (var entry in grouped)
                {
                    key = String.Concat("<span style=\"font-weight: bold;\">" + DataHelper.Adm1 + ":</span> ", entry.Key.district, "&nbsp;&nbsp;&nbsp;");
                    if (!followUpDictionary.ContainsKey(key))
                    {
                        followUpDictionary.Add(key, entry.ToList());
                    }
                }
            }
            else if (isSortOnTeam == false && locationType == Core.Enums.LocationType.SubCounty)
            {
                var grouped = from dcvm in collection
                              where dcvm.FollowUpWindowViewModel.WindowStartDate >= minDate && !dcvm.HasFinalOutcome
                              group dcvm by new { district = dcvm.District, subcountry = dcvm.SubCounty } into newGroup
                              select newGroup;

                foreach (var entry in grouped)
                {
                    key = String.Concat("<span style=\"font-weight: bold;\">" + DataHelper.Adm1 + ":</span> ", entry.Key.district, "&nbsp;&nbsp;&nbsp;");
                    key = key + String.Concat("<span style=\"font-weight: bold;\">" + DataHelper.Adm2 + ":</span> ", entry.Key.subcountry);
                    if (!followUpDictionary.ContainsKey(key))
                    {
                        followUpDictionary.Add(key, entry.ToList());
                    }
                }
            }
            else if (isSortOnTeam == false && locationType == Core.Enums.LocationType.Village)
            {
                var grouped = from dcvm in collection
                              where dcvm.FollowUpWindowViewModel.WindowStartDate >= minDate && !dcvm.HasFinalOutcome
                              group dcvm by new { district = dcvm.District, subcountry = dcvm.SubCounty, villiage = dcvm.Village } into newGroup
                              select newGroup;

                foreach (var entry in grouped)
                {
                    key = String.Concat("<span style=\"font-weight: bold;\">" + DataHelper.Adm1 + ":</span> ", entry.Key.district, "&nbsp;&nbsp;&nbsp;");
                    key = key + String.Concat("<span style=\"font-weight: bold;\">" + DataHelper.Adm2 + ":</span> ", entry.Key.subcountry, "&nbsp;&nbsp;&nbsp;");
                    key = key + String.Concat("<span style=\"font-weight: bold;\">" + DataHelper.Adm4 + ":</span> ", entry.Key.villiage);
                    if (!followUpDictionary.ContainsKey(key))
                    {
                        followUpDictionary.Add(key, entry.ToList());
                    }
                }
            }
            else if (isSortOnTeam == true && locationType == Core.Enums.LocationType.District)
            {
                var grouped = from dcvm in collection
                              where dcvm.FollowUpWindowViewModel.WindowStartDate >= minDate && !dcvm.HasFinalOutcome
                              group dcvm by new { team = dcvm.Team, district = dcvm.District } into newGroup
                              select newGroup;

                foreach (var entry in grouped)
                {
                    key = String.Concat("<span style=\"font-weight: bold;\">" + Properties.Resources.HTMLTeam + "</span> ", entry.Key.team, "<br>");
                    key = key + String.Concat("<span style=\"font-weight: bold;\">" + DataHelper.Adm1 + ":</span> ", entry.Key.district);
                    if (!followUpDictionary.ContainsKey(key))
                    {
                        followUpDictionary.Add(key, entry.ToList());
                    }
                }
            }
            else if (isSortOnTeam == true && locationType == Core.Enums.LocationType.SubCounty)
            {
                var grouped = from dcvm in collection
                              where dcvm.FollowUpWindowViewModel.WindowStartDate >= minDate && !dcvm.HasFinalOutcome
                              group dcvm by new { team = dcvm.Team, district = dcvm.District, subcountry = dcvm.SubCounty } into newGroup
                              select newGroup;

                foreach (var entry in grouped)
                {
                    key = String.Concat("<span style=\"font-weight: bold;\">" + Properties.Resources.HTMLTeam + "</span> ", entry.Key.team, "<br>");
                    key = key + String.Concat("<span style=\"font-weight: bold;\">" + DataHelper.Adm1 + ":</span> ", entry.Key.district, "&nbsp;&nbsp;&nbsp;");
                    key = key + String.Concat("<span style=\"font-weight: bold;\">" + DataHelper.Adm2 + ":</span> ", entry.Key.subcountry);
                    if (!followUpDictionary.ContainsKey(key))
                    {
                        followUpDictionary.Add(key, entry.ToList());
                    }
                }
            }
            else if (isSortOnTeam == true && locationType == Core.Enums.LocationType.Village)
            {
                var grouped = from dcvm in collection
                              where dcvm.FollowUpWindowViewModel.WindowStartDate >= minDate && !dcvm.HasFinalOutcome
                              group dcvm by new { team = dcvm.Team, district = dcvm.District, subcountry = dcvm.SubCounty, villiage = dcvm.Village } into newGroup
                              select newGroup;

                foreach (var entry in grouped)
                {
                    key = String.Concat("<span style=\"font-weight: bold;\">" + Properties.Resources.HTMLTeam + "</span> ", entry.Key.team, "<br>");
                    key = key + String.Concat("<span style=\"font-weight: bold;\">" + DataHelper.Adm1 + ":</span> ", entry.Key.district, "&nbsp;&nbsp;&nbsp;");
                    key = key + String.Concat("<span style=\"font-weight: bold;\">" + DataHelper.Adm2 + ":</span> ", entry.Key.subcountry, "&nbsp;&nbsp;&nbsp;");
                    key = key + String.Concat("<span style=\"font-weight: bold;\">" + DataHelper.Adm4 + ":</span> ", entry.Key.villiage);
                    if (!followUpDictionary.ContainsKey(key))
                    {
                        followUpDictionary.Add(key, entry.ToList());
                    }
                }
            }
            else
            {
                throw new NotImplementedException("The specified location field is unavailable for contact tracing.");
            }

            return followUpDictionary;
        }

        private IEnumerable<IGrouping<string, DailyCheckViewModel>> GetPrevCheck(int indexDate, DateTime minDate, Core.Enums.LocationType locationType = Core.Enums.LocationType.Village)
        {
            if (locationType == Core.Enums.LocationType.Village)
            {
                var query = from prevCheck in DataHelper.PrevFollowUpCollection
                            where
                            (!prevCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate].Status.HasValue || prevCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate].Status == ContactDailyStatus.NotSeen ||
                            prevCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate].Status == ContactDailyStatus.NotRecorded)
                            &&
                            !prevCheck.ContactVM.HasFinalOutcome &&
                            prevCheck.ContactVM.FollowUpWindowViewModel.WindowStartDate < minDate
                            group prevCheck by string.Concat("<span style=\"font-weight: bold;\">" + DataHelper.Adm4 + ":</span> ", prevCheck.ContactVM.Village);
                return query as IEnumerable<IGrouping<string, DailyCheckViewModel>>;
            }
            else if (locationType == Core.Enums.LocationType.District)
            {
                var query = from prevCheck in DataHelper.PrevFollowUpCollection
                            where
                            (!prevCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate].Status.HasValue || prevCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate].Status == ContactDailyStatus.NotSeen ||
                            prevCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate].Status == ContactDailyStatus.NotRecorded)
                            &&
                            !prevCheck.ContactVM.HasFinalOutcome &&
                            prevCheck.ContactVM.FollowUpWindowViewModel.WindowStartDate < minDate
                            group prevCheck by string.Concat("<span style=\"font-weight: bold;\">" + DataHelper.Adm1 + ":</span> ", prevCheck.ContactVM.District);
                return query as IEnumerable<IGrouping<string, DailyCheckViewModel>>;
            }
            else if (locationType == Core.Enums.LocationType.SubCounty)
            {
                var query = from prevCheck in DataHelper.PrevFollowUpCollection
                            where
                            (!prevCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate].Status.HasValue || prevCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate].Status == ContactDailyStatus.NotSeen ||
                            prevCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate].Status == ContactDailyStatus.NotRecorded)
                            &&
                            !prevCheck.ContactVM.HasFinalOutcome &&
                            prevCheck.ContactVM.FollowUpWindowViewModel.WindowStartDate < minDate
                            group prevCheck by string.Concat("<span style=\"font-weight: bold;\">" + DataHelper.Adm2 + ":</span> ", prevCheck.ContactVM.SubCounty);
                return query as IEnumerable<IGrouping<string, DailyCheckViewModel>>;
            }
            else
            {
                throw new NotImplementedException("The specified location field is unavailable for contact tracing.");
            }
        }

        private List<DailyCheckViewModel> GetPrevCheckList(int indexDate, DateTime minDate, string filterClause, string sortClause)
        {
            List<DailyCheckViewModel> returnList = new List<DailyCheckViewModel>();
            string key = string.Empty;

            var skippedQuery = from dcvm in DataHelper.PrevFollowUpCollection
                               where (
                               dcvm.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate].Status.HasValue == false ||
                               dcvm.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate].Status == ContactDailyStatus.NotSeen ||
                               dcvm.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate].Status == ContactDailyStatus.NotRecorded)
                               &&
                               dcvm.ContactVM.HasFinalOutcome == false &&
                               dcvm.ContactVM.FollowUpWindowViewModel.WindowStartDate < minDate
                               select dcvm;

            returnList = skippedQuery.ToList<DailyCheckViewModel>();

            if (returnList.Count() == 0)
            {
                return returnList;
            }

            if (returnList == null || returnList.Count == 0)
            {
                return returnList;
            }
            else
            {
                if (sortClause != "" && filterClause != "")
                {
                    var dynList2 = returnList.Where(filterClause).OrderBy(sortClause);
                    return dynList2.ToList<DailyCheckViewModel>();
                }
                else if (sortClause == "")
                {
                    var dynList2 = returnList.Where(filterClause);
                    return dynList2.ToList<DailyCheckViewModel>();
                }
                else if (filterClause == "")
                {
                    var dynList2 = returnList.OrderBy(sortClause);
                    return dynList2.ToList<DailyCheckViewModel>();
                }
                else
                {
                    return returnList;
                }
            }
        }

        private void btnPrintDailyFollowUp_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Forms.Control.ModifierKeys == System.Windows.Forms.Keys.Alt)
            {
                Popup = new Popup();
                Popup.Parent = grdMain;

                FilterSortDropdown printSortForm = new FilterSortDropdown(DataHelper, IsSuperUser);
                printSortForm.Collection = DataHelper.DailyFollowUpCollection;
                printSortForm.Closed += printSortForm_Closed;
                printSortForm.Print += printSortForm_Print;
                printSortForm.DataContext = this.DataContext;
                printSortForm.MaxWidth = 607;
                printSortForm.MaxHeight = 436;
                Popup.Content = printSortForm;
                Popup.Show();

                e.Handled = true;
                return;
            }
            else
            {
                DataHelper.PrintDailyFollowUp(DataHelper.DailyFollowUpCollection, dpPrev.SelectedDate, Core.Enums.LocationType.Village);
            }
        }

        private void btnPrintDailyFollowUpPrev_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Forms.Control.ModifierKeys == System.Windows.Forms.Keys.Alt)
            {
                Popup = new Popup();
                Popup.Parent = grdMain;

                FilterSortDropdown printSortForm = new FilterSortDropdown(DataHelper, IsSuperUser);
                printSortForm.Collection = DataHelper.PrevFollowUpCollection;
                printSortForm.Closed += printSortForm_Closed;
                printSortForm.Print += printSortForm_Print;
                printSortForm.DataContext = this.DataContext;
                printSortForm.MaxWidth = 607;
                printSortForm.MaxHeight = 436;
                Popup.Content = printSortForm;
                Popup.Show();

                e.Handled = true;
                return;
            }
            else
            {
                DataHelper.PrintDailyFollowUp(DataHelper.PrevFollowUpCollection, dpPrev.SelectedDate, Core.Enums.LocationType.Village);
            }
        }

        private void PrintDailyFollowUp(ObservableCollection<DailyCheckViewModel> collection, Core.Enums.LocationType locationType = Core.Enums.LocationType.Village)
        {
            string baseFileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString("N");
            Dictionary<int, Boundry> ba = DataHelper.BoundaryAggregation;
            DateTime? originalDate = dpPrev.SelectedDate;
            StringBuilder htmlBuilder = new StringBuilder();

            htmlBuilder.Append(ContactTracing.Core.Common.GetHtmlHeader().ToString());

            DateTime dt = DateTime.Now;

            //DateTime minDate = dt.AddDays(-21);
            DateTime minDate = dt.AddDays(-1 * ContactTracing.Core.Common.DaysInWindow);

            if (collection == DataHelper.PrevFollowUpCollection)
            {
                if (!dpPrev.SelectedDate.HasValue)
                {
                    return;
                }
                minDate = dpPrev.SelectedDate.Value;
            }

            SortedDictionary<string, List<DailyCheckViewModel>> followUpDictionary = new SortedDictionary<string, List<DailyCheckViewModel>>();


            var query = GetDailyCheck(collection, minDate, locationType);
            /*from dailyCheck in collection
                    where dailyCheck.ContactVM.FollowUpWindowViewModel.WindowStartDate >= minDate && !dailyCheck.ContactVM.HasFinalOutcome
                    group dailyCheck by String.Concat("<span style=\"font-weight: bold;\">Village:</span> ", dailyCheck.ContactVM.Village);*/


            foreach (var entry in query)
            {
                if (!followUpDictionary.ContainsKey(entry.Key))
                {
                    followUpDictionary.Add(entry.Key, entry.ToList());
                }
            }

            int indexDate = Core.Common.DaysInWindow - 1;

            // this is really terrible, but the requirement came up for this too late to build it in properly and
            // there isn't enough time to do it right
            DateTime incDate = minDate.AddDays(-600);
            while (incDate < DateTime.Today)
            {
                incDate = incDate.AddDays(1);

                DataHelper.ShowContactsForDateforFollowup.Execute(incDate);

                query = GetPrevCheck(indexDate, minDate, locationType);

                foreach (var entry in query)
                {
                    if (!followUpDictionary.ContainsKey(entry.Key))
                    {
                        followUpDictionary.Add(entry.Key, new List<DailyCheckViewModel>());
                    }
                    foreach (var dailyCheck in entry)
                    {
                        List<DailyCheckViewModel> dcList = followUpDictionary[entry.Key];
                        bool found = false;

                        foreach (DailyCheckViewModel dcVM in dcList)
                        {
                            if (dcVM.ContactVM == dailyCheck.ContactVM)
                            {
                                found = true;
                            }
                        }

                        if (!found)
                        {
                            DailyCheckViewModel dcVM = dailyCheck as DailyCheckViewModel;
                            if (dcVM != null)
                            {
                                followUpDictionary[entry.Key].Add(dcVM);
                            }
                        }
                    }
                }
            }

            dpPrev.SelectedDate = originalDate; // DateTime.Today.AddDays(-1); // reset

            int rowsGenerated = 0;
            bool firstPage = true;

            //foreach (var entry in query)
            foreach (KeyValuePair<string, List<DailyCheckViewModel>> kvp in followUpDictionary)
            {
                htmlBuilder.AppendLine("<table width=\"100%\" style=\"border: 0px; padding: 0px; margin: 0px; clear: left; width:100%; \">");
                htmlBuilder.AppendLine(" <tr style=\"border: 0px;\">");
                htmlBuilder.AppendLine("  <td width=\"50%\" style=\"border: 0px;\">");
                htmlBuilder.AppendLine("   <p style=\"font-size: 13pt; font-weight: bold; clear: left; text-decoration: underline;\">" + Properties.Settings.Default.HtmlPrintoutTitle + "</p>");
                htmlBuilder.AppendLine("   <p style=\"font-size: 13pt; font-weight: bold; text-decoration: underline;\">" + Properties.Resources.HTMLContactTracingDailyFollowUpheader + "</p>");

                if (collection == DataHelper.PrevFollowUpCollection)
                {
                    htmlBuilder.AppendLine("   <p style=\"font-size: 13pt;\"><span style=\"font-weight: bold;\">" + Properties.Resources.HTMLDate + "</span> " + dpPrev.SelectedDate.Value.ToShortDateString() + "</p>");
                }
                else
                {
                    htmlBuilder.AppendLine("   <p style=\"font-size: 13pt;\"><span style=\"font-weight: bold;\">" + Properties.Resources.HTMLDate + "</span> " + DateTime.Now.ToShortDateString() + "</p>");
                }

                htmlBuilder.AppendLine("  </td>");
                //htmlBuilder.AppendLine("  <td width=\"50%\" style=\"border: 0px;\">");
                //htmlBuilder.AppendLine("   <p style=\"font-size: 13pt; font-weight: bold; clear: left; text-decoration: underline;\">Team:</p>");
                //htmlBuilder.AppendLine("   <p style=\"font-size: 13pt; font-weight: bold; text-decoration: underline;\">Team Leader:</p>");
                //htmlBuilder.AppendLine("  </td>");
                htmlBuilder.AppendLine(" </tr>");
                htmlBuilder.AppendLine("</table>");

                htmlBuilder.AppendLine("<table width=\"100%\" style=\"border: 0px; padding: 0px; margin: 0px; clear: left; width:100%; \">");
                htmlBuilder.AppendLine(" <tr style=\"border: 0px;\">");
                htmlBuilder.AppendLine("  <td width=\"50%\" style=\"border: 0px;\">");
                htmlBuilder.AppendLine("  <ul style=\"font-size: 11pt;\">");
                htmlBuilder.AppendLine("   <li>" + String.Format(Properties.Resources.HTML21DayInstructions1, "&#x2713;") + "</li>");
                htmlBuilder.AppendLine("   <li>" + String.Format(Properties.Resources.HTML21DayInstructions2, "&#x2717;") + "</li>");
                htmlBuilder.AppendLine("   <li>" + String.Format(Properties.Resources.HTML21DayInstructions3, "–") + "</li>");
                htmlBuilder.AppendLine("  </ul>");
                htmlBuilder.AppendLine("  </td>");
                htmlBuilder.AppendLine("  </td>");
                htmlBuilder.AppendLine("  <td width=\"50%\" style=\"border: 0px;\">");
                htmlBuilder.AppendLine("   <p style=\"font-size: 13pt; font-weight: bold; clear: left;\">" + Properties.Resources.HTMLTeam + "</p>");

                if (ApplicationViewModel.Instance.CurrentRegion != Core.Enums.RegionEnum.USA)
                {
                    htmlBuilder.AppendLine("   <p style=\"font-size: 13pt; font-weight: bold; \">" + Properties.Resources.HTMLTeamLeader + "</p>");
                }

                htmlBuilder.AppendLine("  </td>");
                htmlBuilder.AppendLine(" </tr>");
                htmlBuilder.AppendLine("</table>");

                //foreach (var dailyCheck in entry)
                foreach (DailyCheckViewModel dailyCheck in kvp.Value)
                {
                    if (rowsGenerated == 0)
                    {
                        //htmlBuilder.AppendLine("<p style=\"font-weight: bold; clear: left;\">" + entry.Key + ", Sub county: " + dailyCheck.ContactVM.SubCounty + ", District: " + dailyCheck.ContactVM.District + ". LC1 Chairman: " + dailyCheck.ContactVM.LC1Chairman + "</p>");
                        //htmlBuilder.AppendLine("<p style=\"font-weight: bold; clear: left;\">" + kvp.Key + ", Sub county: " + dailyCheck.ContactVM.SubCounty + ", District: " + dailyCheck.ContactVM.District + ". LC1 Chairman: " + dailyCheck.ContactVM.LC1Chairman + "</p>");

                        if (ApplicationViewModel.Instance.CurrentRegion == Core.Enums.RegionEnum.USA)
                        {
                            if (locationType == Core.Enums.LocationType.Village)
                            {
                                htmlBuilder.AppendLine("<p style=\"clear: left;\">" +
                                    kvp.Key +
                                    "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<span style=\"font-weight: bold;\">" + DataHelper.Adm2 + "</span> " +
                                    dailyCheck.ContactVM.SubCounty +
                                    "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<span style=\"font-weight: bold;\">" + DataHelper.Adm1 + "</span> " +
                                    dailyCheck.ContactVM.District +
                                    "<br/></p>");
                            }
                            else if (locationType == Core.Enums.LocationType.SubCounty)
                            {
                                htmlBuilder.AppendLine("<p style=\"clear: left;\">" +
                                    kvp.Key +
                                    "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<span style=\"font-weight: bold;\">" + DataHelper.Adm1 + "</span> " +
                                    dailyCheck.ContactVM.District +
                                    "<br /></p>");
                            }
                            else if (locationType == Core.Enums.LocationType.District)
                            {
                                htmlBuilder.AppendLine("<p style=\"clear: left;\">" +
                                    kvp.Key + "</p>");
                            }
                        }
                        else
                        {
                            if (locationType == Core.Enums.LocationType.Village)
                            {
                                htmlBuilder.AppendLine("<p style=\"clear: left;\">" +
                                    kvp.Key +
                                    "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<span style=\"font-weight: bold;\">" + DataHelper.Adm2 + "</span> " +
                                    dailyCheck.ContactVM.SubCounty +
                                    "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<span style=\"font-weight: bold;\">" + DataHelper.Adm1 + "</span> " +
                                    dailyCheck.ContactVM.District +
                                    "<br /><span style=\"font-weight: bold;\">" + Properties.Resources.HTMLLC1ChairmanHeading + "</span> " + dailyCheck.ContactVM.LC1Chairman + "</p>");
                            }
                            else if (locationType == Core.Enums.LocationType.SubCounty)
                            {
                                htmlBuilder.AppendLine("<p style=\"clear: left;\">" +
                                    kvp.Key +
                                    "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<span style=\"font-weight: bold;\">" + DataHelper.Adm1 + "</span> " +
                                    dailyCheck.ContactVM.District +
                                    "<br /><span style=\"font-weight: bold;\">" + Properties.Resources.HTMLLC1ChairmanHeading + "</span> " + dailyCheck.ContactVM.LC1Chairman + "</p>");
                            }
                            else if (locationType == Core.Enums.LocationType.District)
                            {
                                htmlBuilder.AppendLine("<p style=\"clear: left;\">" +
                                    kvp.Key + "</p>");
                            }
                        }
                        htmlBuilder.AppendLine("<table style=\"width: 1200px; border: 4px solid black;\" align=\"left\">");
                        htmlBuilder.AppendLine("<thead>");
                        htmlBuilder.AppendLine("<tr style=\"border-top: 0px solid black;\">");

                        if (ApplicationViewModel.Instance.CurrentRegion != Core.Enums.RegionEnum.USA)
                        {
                            htmlBuilder.AppendLine("<th style=\"width: 10px;\">" + Properties.Resources.ColHeaderContactID + "</th>");
                        }
                        else
                        {
                            htmlBuilder.AppendLine("<th style=\"width: 10px;\">" + Properties.Resources.ColHeaderID + "</th>");
                            htmlBuilder.AppendLine("<th style=\"width: 10px;\">" + Properties.Resources.ColHeaderOriginalID + "</th>");
                        }

                        htmlBuilder.AppendLine("<th style=\"width: 110px;\">" + Properties.Resources.ColHeaderSurname + "</th>");
                        htmlBuilder.AppendLine("<th style=\"width: 140px;\">" + Properties.Resources.ColHeaderOtherNames + "</th>");
                        htmlBuilder.AppendLine("<th style=\"width: 15px;\">" + Properties.Resources.HTMLColHeaderSexNarrow + "</th>");
                        htmlBuilder.AppendLine("<th style=\"width: 15px;\">" + Properties.Resources.HTMLColHeaderAgeNarrow + "</th>");
                        htmlBuilder.AppendLine("<th style=\"width: 40px;\">" + Properties.Resources.ColHeaderDateLastContact + "</th>");
                        htmlBuilder.AppendLine("<th style=\"width: 40px;\">" + Properties.Resources.ColHeaderDateLastFollowUp + "</th>");
                        htmlBuilder.AppendLine("<th style=\"width: 15px;\">" + Properties.Resources.HTMLColHeaderDay + "</th>");
                        htmlBuilder.AppendLine("<th style=\"width: 40px;\">" + Properties.Resources.HTMLColHeaderDateLastSeen + "</th>");
                        //htmlBuilder.AppendLine("<th>" + Properties.Resources.ColHeaderRiskLevel + "</th>");
                        htmlBuilder.AppendLine("<th style=\"width: 140px;\">" + Properties.Resources.SourceCase + "</th>");

                        if (ApplicationViewModel.Instance.CurrentRegion != Core.Enums.RegionEnum.USA)
                        {
                            htmlBuilder.AppendLine("<th style=\"width: 100px;\">" + Properties.Resources.ColHeaderHeadHousehold + "</th>");
                        }
                        else
                        {
                            htmlBuilder.AppendLine("<th style=\"width: 100px;\">" + Properties.Resources.Address + "</th>");
                        }

                        //htmlBuilder.AppendLine("<th>LC1 chairman</th>");
                        htmlBuilder.AppendLine("<th style=\"width: 70px;\">" + Properties.Resources.ColHeaderPhone + "</th>");
                        //htmlBuilder.AppendLine("<th>Healthcare worker?</th>");
                        htmlBuilder.AppendLine("<th style=\"width: 140px; border-right: 4px solid black;\">" + Properties.Resources.HTMLColHeaderHCWHealthFacility + "</th>");
                        //htmlBuilder.AppendLine("<th>" + Properties.Resources.HTMLColHeaderSeenNarrow + "</th>");
                        //htmlBuilder.AppendLine("<th>" + Properties.Resources.HTMLColHeaderSickNarrow + "</th>");
                        htmlBuilder.AppendLine("<th>" + Properties.Resources.HTMLColHeaderStatus + "</th>");

                        htmlBuilder.AppendLine("<th style=\"width: 170px;\">" + Properties.Resources.HTMLColHeaderNotes + "</th>");
                        htmlBuilder.AppendLine("</tr>");
                        htmlBuilder.AppendLine("</thead>");
                        htmlBuilder.AppendLine("<tbody>");
                    }

                    bool hasConfirmedSourceCase = false;
                    //if (dailycheck.day > -1 && dailycheck.day < 22)
                    //{
                    //    if (dailycheck.casevm.epicaseclassification.equals("1"))
                    //        hasconfirmedsourcecase = true;
                    //    else
                    //    {
                    //        datetime today = datetime.today;
                    //        datetime minimumdate = dt.adddays(-1 * contacttracing.core.common.daysinwindow);
                    //        foreach (caseviewmodel cavm in datahelper.casecollection)
                    //        {
                    //            if (hasconfirmedsourcecase)
                    //                break;
                    //            if (cavm.epicaseclassification.equals("1"))
                    //            {
                    //                foreach (contactviewmodel covm in cavm.contacts)
                    //                {
                    //                    if (covm.contactid.equals(dailycheck.contactid))
                    //                    {
                    //                        string caseguid = cavm.recordid;
                    //                        string contactguid = covm.recordid;
                    //                        datarow mldr = datahelper.metalinksdatatable.select(
                    //                            "fromrecordguid = '" + caseguid + "' and torecordguid = '" + contactguid + "'")[0];
                    //                        if ((datetime)mldr["lastcontactdate"] >= minimumdate)
                    //                        {
                    //                            hasconfirmedsourcecase = true;
                    //                            break;
                    //                        }
                    //                    }
                    //                }
                    //            }
                    //        }
                    //    }
                    //}

                    htmlBuilder.AppendLine("<tr style=\"border-bottom: 1px solid black; height: 32px;\">");
                    //bool pastDue = (dailyCheck.ContactVM.FollowUpWindowViewModel.WindowStartDate < minDate.AddDays(-20));
                    bool pastDue = dailyCheck.ContactVM.FollowUpWindowViewModel.WindowEndDate < DateTime.Today;

                    if (ApplicationViewModel.Instance.CurrentRegion != Core.Enums.RegionEnum.USA)
                    {
                        htmlBuilder.AppendLine("<td style=\"font-size: 7.5pt;\">" + dailyCheck.ContactVM.ContactID + "</td>");
                    }
                    else
                    {
                        htmlBuilder.AppendLine("<td style=\"font-size: 7.5pt;\">" + dailyCheck.ContactVM.ContactCDCID + "</td>");
                        htmlBuilder.AppendLine("<td style=\"font-size: 7.5pt;\">" + dailyCheck.ContactVM.ContactStateID + "</td>");
                    }

                    if (pastDue)
                    {
                        htmlBuilder.AppendLine("<td style=\"font-size: 10pt;\">" + Core.Common.TruncHTMLCell("*" + dailyCheck.ContactVM.Surname, 13) + "</td>");
                    }
                    else
                    {
                        htmlBuilder.AppendLine("<td style=\"font-size: 10pt;\">" + Core.Common.TruncHTMLCell(dailyCheck.ContactVM.Surname, 13) + "</td>");
                    }
                    htmlBuilder.AppendLine("<td style=\"font-size: 10pt;\">" + Core.Common.TruncHTMLCell(dailyCheck.ContactVM.OtherNames, 20) + "</td>");
                    htmlBuilder.AppendLine("<td style=\"text-align: center;\">" + dailyCheck.ContactVM.GenderAbbreviation + "</td>");

                    if (dailyCheck.ContactVM.AgeYears.HasValue)
                    {
                        htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + dailyCheck.ContactVM.AgeYears + "</td>");
                    }
                    else
                    {
                        htmlBuilder.AppendLine("<td>&nbsp;</td>");
                    }

                    FollowUpVisitViewModel lastCTVisitVM = null;
                    foreach (FollowUpVisitViewModel fuVM in dailyCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits)
                    {
                        if (fuVM.IsSeen /*fuVM.Seen == SeenType.Seen*/)
                        {
                            lastCTVisitVM = fuVM;
                        }

                        if (fuVM.FollowUpVisit.Date.Day == DateTime.Now.Day && fuVM.FollowUpVisit.Date.Month == DateTime.Now.Month && fuVM.FollowUpVisit.Date.Year == DateTime.Now.Year)
                        {
                            break;
                        }
                    }

                    FollowUpVisitViewModel lastDayVM = dailyCheck.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate];

                    IMultiValueConverter dateConverter = new Converters.DateConverter();

                    string[] parmsValues = { dailyCheck.ContactVM.FollowUpWindowViewModel.WindowStartDate.AddDays(-1).ToString(), DataHelper.ApplicationCulture };
                    var windowstartdate = dateConverter.Convert(parmsValues, null, null, null);

                    //htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + dailyCheck.ContactVM.FollowUpWindowViewModel.WindowStartDate.AddDays(-1).ToString("d/M/yy") + "</td>");
                    htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + windowstartdate + "</td>");
                    parmsValues[0] = lastDayVM.FollowUpVisit.Date.ToString();
                    var followupvisitdate = dateConverter.Convert(parmsValues, null, null, null);

                    //htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + lastDayVM.FollowUpVisit.Date.ToString("d/M/yy") + "</td>");
                    htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + followupvisitdate + "</td>");

                    if (pastDue)
                    {
                        htmlBuilder.AppendLine("<td style=\"text-align: right;\">&gt;21</td>");
                    }
                    else
                    {
                        if (collection == DataHelper.DailyFollowUpCollection)
                        {
                            htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + dailyCheck.Day + "</td>");
                        }
                        else if (collection == DataHelper.PrevFollowUpCollection && dpPrev.SelectedDate.HasValue)
                        {
                            TimeSpan ts = dpPrev.SelectedDate.Value - dailyCheck.ContactVM.FollowUpWindowViewModel.WindowStartDate;
                            htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + (ts.Days + 1).ToString() + "</td>");
                        }
                    }

                    if (lastCTVisitVM != null)
                    {
                        //htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + lastCTVisitVM.FollowUpVisit.Date.ToString("d/M/yy") + "</td>");
                        parmsValues[0] = lastCTVisitVM.FollowUpVisit.Date.ToString();
                        followupvisitdate = dateConverter.Convert(parmsValues, null, null, null);
                        htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + followupvisitdate + "</td>");
                    }
                    else
                    {
                        htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + Properties.Resources.Never + "</td>");
                    }

                    htmlBuilder.AppendLine("<td>" + dailyCheck.CaseSurname + " " + dailyCheck.CaseOtherNames);
                    if (hasConfirmedSourceCase)
                    {
                        htmlBuilder.AppendLine(" (C)");
                    }
                    htmlBuilder.AppendLine("</td>");

                    if (ApplicationViewModel.Instance.CurrentRegion == Core.Enums.RegionEnum.USA)
                    {
                        string fullUSAddress = string.Empty;
                        fullUSAddress += ba.ContainsKey(-2) && ba[-2].ContactObjectValue(dailyCheck.ContactVM) != "" ? ba[-2].ContactObjectValue(dailyCheck.ContactVM) + ", " : "";
                        fullUSAddress = fullUSAddress.Trim().TrimEnd(new char[] { ',' });
                        htmlBuilder.AppendLine("<td>" + Core.Common.TruncHTMLCell(fullUSAddress, 20) + "</td>");
                    }
                    else
                    {
                        htmlBuilder.AppendLine("<td>" + Core.Common.TruncHTMLCell(dailyCheck.ContactVM.HeadOfHousehold, 20) + "</td>");
                    }

                    htmlBuilder.AppendLine("<td>" + ParsePhoneNumber(dailyCheck.ContactVM.Phone) + "</td>");
                    htmlBuilder.AppendLine("<td style=\"border-right: 4px solid black;\">" + Core.Common.TruncHTMLCell(dailyCheck.ContactVM.HCWFacility, 20) + "</td>");
                    htmlBuilder.AppendLine("<td></td>");
                    htmlBuilder.AppendLine("<td></td>");
                    htmlBuilder.AppendLine("</tr>");

                    rowsGenerated++;

                    if (firstPage && rowsGenerated == 14)
                    {
                        GenerateDailyHtmlFooter(htmlBuilder);
                        rowsGenerated = 0;
                        firstPage = false;
                    }
                    else if (!firstPage && rowsGenerated == 20)
                    {
                        GenerateDailyHtmlFooter(htmlBuilder);
                        rowsGenerated = 0;
                    }
                }

                if (firstPage && rowsGenerated % 14 != 0)
                {
                    GenerateDailyHtmlFooter(htmlBuilder);
                    rowsGenerated = 0;
                    firstPage = true;
                }
                else if (!firstPage && rowsGenerated % 20 != 0)
                {
                    GenerateDailyHtmlFooter(htmlBuilder);
                    rowsGenerated = 0;
                    firstPage = true;
                }
            }

            if (collection == DataHelper.PrevFollowUpCollection)
            {
                DataHelper.ShowContactsForDateforFollowup.Execute(dpPrev.SelectedDate);
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

        private void StratificationCheck(DailyCheckViewModel dcvm, ref DailyCheckViewModel dcvmLast, string orderClause, ref bool isStratification, ref bool isCovertStratification)
        {
            if (dcvmLast == null)
            {
                isStratification = true;
                isCovertStratification = true;
                dcvmLast = dcvm;
                return;
            }

            isStratification = false;
            isCovertStratification = false;
            string[] ar = orderClause.Split(new char[] { ',' });
            List<string> ocs = ar.ToList<string>();
            object standing = null;
            object neo = null;
            System.Reflection.PropertyInfo standingProp;
            System.Reflection.PropertyInfo neoProp;
            var standingType = dcvmLast.ContactVM.GetType();
            var neoType = dcvm.ContactVM.GetType();

            foreach (string oc in ocs)
            {
                standingProp = standingType.GetProperty(oc.Replace("ContactVM.", ""));
                neoProp = neoType.GetProperty(oc.Replace("ContactVM.", ""));

                if (standingProp != null && neoProp != null)
                {
                    standing = standingProp.GetValue(dcvmLast.ContactVM, null);
                    neo = neoProp.GetValue(dcvm.ContactVM, null);

                    if (standing is string && neo is string)
                    {
                        standing = ((string)standing).Replace(' ', '_');
                        neo = ((string)neo).Replace(' ', '_');

                        if ((string)standing != (string)neo)
                        {
                            dcvmLast = dcvm;
                            isStratification = true;
                            return;
                        }
                    }
                }
            }

            Dictionary<int, Boundry> ba = DataHelper.BoundaryAggregation;
            if (ba.ContainsKey(0) && ocs.Contains(ba[0].ObjectResolution) == false)
                ocs.Add(ba[0].ObjectResolution);
            if (ba.ContainsKey(1) && ocs.Contains(ba[1].ObjectResolution) == false)
                ocs.Add(ba[1].ObjectResolution);
            if (ba.ContainsKey(2) && ocs.Contains(ba[2].ObjectResolution) == false)
                ocs.Add(ba[2].ObjectResolution);
            if (ba.ContainsKey(3) && ocs.Contains(ba[3].ObjectResolution) == false)
                ocs.Add(ba[3].ObjectResolution);

            foreach (string oc in ocs)
            {
                standingProp = standingType.GetProperty(oc.Replace("ContactVM.", ""));
                neoProp = neoType.GetProperty(oc.Replace("ContactVM.", ""));

                if (standingProp != null && neoProp != null)
                {
                    standing = standingProp.GetValue(dcvmLast.ContactVM, null);
                    neo = neoProp.GetValue(dcvm.ContactVM, null);

                    if (standing is string && neo is string)
                    {
                        standing = ((string)standing).Replace(' ', '_');
                        neo = ((string)neo).Replace(' ', '_');

                        if ((string)standing != (string)neo)
                        {
                            dcvmLast = dcvm;
                            isCovertStratification = true;
                            return;
                        }
                    }
                }
            }

            dcvmLast = dcvm;
        }

        private void StratificationCheck(ContactViewModel contact, ref ContactViewModel contactLast, string orderClause, ref bool isStratification, ref bool isCovertStratification)
        {
            if (contactLast == null)
            {
                isStratification = true;
                isCovertStratification = true;
                contactLast = contact;
                return;
            }

            isStratification = false;
            isCovertStratification = false;
            string[] ar = orderClause.Split(new char[] { ',' });
            List<string> ocs = ar.ToList<string>();
            object standing = null;
            object neo = null;
            System.Reflection.PropertyInfo standingProp;
            System.Reflection.PropertyInfo neoProp;
            var standingType = contactLast.GetType();
            var neoType = contact.GetType();

            foreach (string oc in ocs)
            {
                standingProp = standingType.GetProperty(oc.Replace("ContactVM.", ""));
                neoProp = neoType.GetProperty(oc.Replace("ContactVM.", ""));

                if (standingProp != null && neoProp != null)
                {
                    standing = standingProp.GetValue(contactLast, null);
                    neo = neoProp.GetValue(contact, null);

                    if (standing is string && neo is string)
                    {
                        standing = ((string)standing).Replace(' ', '_');
                        neo = ((string)neo).Replace(' ', '_');

                        if ((string)standing != (string)neo)
                        {
                            contactLast = contact;
                            isStratification = true;
                            return;
                        }
                    }
                }
            }

            Dictionary<int, Boundry> ba = DataHelper.BoundaryAggregation;
            if (ba.ContainsKey(0) && ocs.Contains(ba[0].ObjectResolution) == false)
                ocs.Add(ba[0].ObjectResolution);
            if (ba.ContainsKey(1) && ocs.Contains(ba[1].ObjectResolution) == false)
                ocs.Add(ba[1].ObjectResolution);
            if (ba.ContainsKey(2) && ocs.Contains(ba[2].ObjectResolution) == false)
                ocs.Add(ba[2].ObjectResolution);
            if (ba.ContainsKey(3) && ocs.Contains(ba[3].ObjectResolution) == false)
                ocs.Add(ba[3].ObjectResolution);

            foreach (string oc in ocs)
            {
                standingProp = standingType.GetProperty(oc.Replace("ContactVM.", ""));
                neoProp = neoType.GetProperty(oc.Replace("ContactVM.", ""));

                if (standingProp != null && neoProp != null)
                {
                    standing = standingProp.GetValue(contactLast, null);
                    neo = neoProp.GetValue(contact, null);

                    if (standing is string && neo is string)
                    {
                        standing = ((string)standing).Replace(' ', '_');
                        neo = ((string)neo).Replace(' ', '_');

                        if ((string)standing != (string)neo)
                        {
                            contactLast = contact;
                            isCovertStratification = true;
                            return;
                        }
                    }
                }
            }

            contactLast = contact;
        }


        private string FormAggregateSortExpression(List<string> selectedSortKeys, string sortClause)
        {
            Dictionary<int, Boundry> ba = DataHelper.BoundaryAggregation;

            int neoKey;
            bool boundryInSort = false;
            foreach (string key in selectedSortKeys)
            {
                if (int.TryParse(key, out neoKey))
                {
                    boundryInSort = true;
                }
            }

            if (sortClause == "")
            {
                if (ba.ContainsKey(3))
                {
                    sortClause = sortClause + ba[3].ObjectResolution + ",";
                }
                if (ba.ContainsKey(2))
                {
                    sortClause = sortClause + ba[2].ObjectResolution + ",";
                }
                if (ba.ContainsKey(1))
                {
                    sortClause = sortClause + ba[1].ObjectResolution + ",";
                }
                if (ba.ContainsKey(0))
                {
                    sortClause = sortClause + ba[0].ObjectResolution + ",";
                }

                sortClause = sortClause.TrimEnd(new char[] { ',' });
            }
            else
            {
                selectedSortKeys.Reverse();
                int lastAggregateKey = -1;
                List<string> aggregateSort = new List<string>();

                foreach (string key in selectedSortKeys)
                {
                    if (int.TryParse(key, out neoKey))
                    {
                        if (lastAggregateKey >= ba.Count)
                        {
                            continue;
                        }

                        lastAggregateKey = neoKey;

                        while (lastAggregateKey <= ba.Count)
                        {
                            if (ba.ContainsKey(lastAggregateKey))
                            {
                                aggregateSort.Add(ba[lastAggregateKey].ObjectResolution);
                            }
                            lastAggregateKey++;
                        }
                    }
                    else
                    {
                        aggregateSort.Add(key);
                    }
                }

                aggregateSort.Reverse();

                if (boundryInSort == false)
                {
                    if (ba.ContainsKey(3))
                    {
                        aggregateSort.Add(ba[3].ObjectResolution);
                    }
                    if (ba.ContainsKey(2))
                    {
                        aggregateSort.Add(ba[2].ObjectResolution);
                    }
                    if (ba.ContainsKey(1))
                    {
                        aggregateSort.Add(ba[1].ObjectResolution);
                    }
                    if (ba.ContainsKey(0))
                    {
                        aggregateSort.Add(ba[0].ObjectResolution);
                    }
                }

                sortClause = aggregateSort.Aggregate((i, j) => i + "," + j);
            }
            return sortClause;
        }

        private void MarkupGridRow(ObservableCollection<DailyCheckViewModel> collection, StringBuilder htmlBuilder, int indexDate, DailyCheckViewModel dcvm)
        {
            htmlBuilder.AppendLine("<tbody>");
            htmlBuilder.AppendLine("<tr style=\"border-bottom: 1px solid black; height: 32px;\">");
            bool pastDue = dcvm.ContactVM.FollowUpWindowViewModel.WindowEndDate < DateTime.Today;

            if (ApplicationViewModel.Instance.CurrentRegion != Core.Enums.RegionEnum.USA)
            {
                htmlBuilder.AppendLine("<td style=\"font-size: 7.5pt;\">" + dcvm.ContactVM.ContactID + "</td>");
            }
            else
            {
                htmlBuilder.AppendLine("<td style=\"font-size: 7.5pt;\">" + dcvm.ContactVM.ContactCDCID + "</td>");
                htmlBuilder.AppendLine("<td style=\"font-size: 7.5pt;\">" + dcvm.ContactVM.ContactStateID + "</td>");
            }

            if (pastDue)
            {
                htmlBuilder.AppendLine("<td style=\"font-size: 10pt;\">" + Core.Common.TruncHTMLCell("*" + dcvm.ContactVM.Surname, 13) + "</td>");
            }
            else
            {
                htmlBuilder.AppendLine("<td style=\"font-size: 10pt;\">" + Core.Common.TruncHTMLCell(dcvm.ContactVM.Surname, 13) + "</td>");
            }

            htmlBuilder.AppendLine("<td style=\"font-size: 10pt;\">" + Core.Common.TruncHTMLCell(dcvm.ContactVM.OtherNames, 20) + "</td>");
            htmlBuilder.AppendLine("<td style=\"text-align: center;\">" + dcvm.ContactVM.GenderAbbreviation + "</td>");

            if (dcvm.ContactVM.AgeYears.HasValue)
            {
                htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + dcvm.ContactVM.AgeYears + "</td>");
            }
            else
            {
                htmlBuilder.AppendLine("<td>&nbsp;</td>");
            }

            FollowUpVisitViewModel lastCTVisitVM = null;

            foreach (FollowUpVisitViewModel fuVM in dcvm.ContactVM.FollowUpWindowViewModel.FollowUpVisits)
            {
                if (fuVM.IsSeen)
                {
                    lastCTVisitVM = fuVM;
                }

                if (fuVM.FollowUpVisit.Date.Day == DateTime.Now.Day && fuVM.FollowUpVisit.Date.Month == DateTime.Now.Month && fuVM.FollowUpVisit.Date.Year == DateTime.Now.Year)
                {
                    break;
                }
            }

            FollowUpVisitViewModel lastDayVM = dcvm.ContactVM.FollowUpWindowViewModel.FollowUpVisits[indexDate];


            IMultiValueConverter dateConverter = new Converters.DateConverter();

            string[] parmsValues = { dcvm.ContactVM.FollowUpWindowViewModel.WindowStartDate.AddDays(-1).ToString(), DataHelper.ApplicationCulture };
            var windowstartdate = dateConverter.Convert(parmsValues, null, null, null);
            //htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + dcvm.ContactVM.FollowUpWindowViewModel.WindowStartDate.AddDays(-1).ToString("d/M/yy") + "</td>");
            htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + windowstartdate + "</td>");
            parmsValues[0] = lastDayVM.FollowUpVisit.Date.ToString();

            var followupdate = dateConverter.Convert(parmsValues, null, null, null);
            //htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + lastDayVM.FollowUpVisit.Date.ToString("d/M/yy") + "</td>");
            htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + followupdate + "</td>");

            if (pastDue)
            {
                htmlBuilder.AppendLine("<td style=\"text-align: right;\">&gt;21</td>");
            }
            else
            {
                if (collection == DataHelper.DailyFollowUpCollection)
                {
                    htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + dcvm.Day + "</td>");
                }
                else if (collection == DataHelper.PrevFollowUpCollection && dpPrev.SelectedDate.HasValue)
                {
                    TimeSpan ts = dpPrev.SelectedDate.Value - dcvm.ContactVM.FollowUpWindowViewModel.WindowStartDate;
                    htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + (ts.Days + 1).ToString() + "</td>");
                }
            }

            if (lastCTVisitVM != null)
            {
                parmsValues[0] = lastCTVisitVM.FollowUpVisit.Date.ToString();
                followupdate = dateConverter.Convert(parmsValues, null, null, null);
                //htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + lastCTVisitVM.FollowUpVisit.Date.ToString("d/M/yy") + "</td>");
                htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + followupdate + "</td>");
            }
            else
            {
                htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + Properties.Resources.Never + "</td>");
            }

            htmlBuilder.AppendLine("<td>" + dcvm.CaseSurname + " " + dcvm.CaseOtherNames + "</td>");

            if (ApplicationViewModel.Instance.CurrentRegion == Core.Enums.RegionEnum.USA)
            {
                Dictionary<int, Boundry> ba = DataHelper.BoundaryAggregation;
                string fullUSAddress = string.Empty;
                fullUSAddress += ba.ContainsKey(-2) && ba[-2].ContactObjectValue(dcvm.ContactVM) != "" ? ba[-2].ContactObjectValue(dcvm.ContactVM) + ", " : "";
                fullUSAddress = fullUSAddress.Trim().TrimEnd(new char[] { ',' });
                htmlBuilder.AppendLine("<td>" + Core.Common.TruncHTMLCell(fullUSAddress, 20) + "</td>");
            }
            else
            {
                htmlBuilder.AppendLine("<td>" + Core.Common.TruncHTMLCell(dcvm.ContactVM.HeadOfHousehold, 20) + "</td>");
            }

            htmlBuilder.AppendLine("<td>" + ParsePhoneNumber(dcvm.ContactVM.Phone) + "</td>");
            htmlBuilder.AppendLine("<td style=\"border-right: 4px solid black;\">" + Core.Common.TruncHTMLCell(dcvm.ContactVM.HCWFacility, 20) + "</td>");
            htmlBuilder.AppendLine("<td></td>");
            htmlBuilder.AppendLine("<td></td>");
            htmlBuilder.AppendLine("</tr>");
            htmlBuilder.AppendLine("</tr>");
            htmlBuilder.AppendLine("<!--end of row-->");
        }

        private void MarkupGridHeader(StringBuilder htmlBuilder)
        {
            htmlBuilder.AppendLine("<table style=\"width: 1200px; border: 4px solid black;\" align=\"left\">");
            htmlBuilder.AppendLine("<thead>");
            htmlBuilder.AppendLine("<tr style=\"border-top: 0px solid black;\">");

            if (ApplicationViewModel.Instance.CurrentRegion != Core.Enums.RegionEnum.USA)
            {
                htmlBuilder.AppendLine("<th style=\"width: 10px;\">" + Properties.Resources.ColHeaderContactID + "</th>");
            }
            else
            {
                htmlBuilder.AppendLine("<th style=\"width: 10px;\">" + Properties.Resources.ColHeaderID + "</th>");
                htmlBuilder.AppendLine("<th style=\"width: 10px;\">" + Properties.Resources.ColHeaderOriginalID + "</th>");
            }

            htmlBuilder.AppendLine("<th style=\"width: 110px;\">" + Properties.Resources.ColHeaderSurname + "</th>");
            htmlBuilder.AppendLine("<th style=\"width: 140px;\">" + Properties.Resources.ColHeaderOtherNames + "</th>");
            htmlBuilder.AppendLine("<th style=\"width: 15px;\">" + Properties.Resources.HTMLColHeaderSexNarrow + "</th>");
            htmlBuilder.AppendLine("<th style=\"width: 15px;\">" + Properties.Resources.HTMLColHeaderAgeNarrow + "</th>");
            htmlBuilder.AppendLine("<th style=\"width: 40px;\">" + Properties.Resources.ColHeaderDateLastContact + "</th>");
            htmlBuilder.AppendLine("<th style=\"width: 40px;\">" + Properties.Resources.ColHeaderDateLastFollowUp + "</th>");
            htmlBuilder.AppendLine("<th style=\"width: 15px;\">" + Properties.Resources.HTMLColHeaderDay + "</th>");
            htmlBuilder.AppendLine("<th style=\"width: 40px;\">" + Properties.Resources.HTMLColHeaderDateLastSeen + "</th>");
            htmlBuilder.AppendLine("<th style=\"width: 140px;\">" + Properties.Resources.SourceCase + "</th>");

            if (ApplicationViewModel.Instance.CurrentRegion != Core.Enums.RegionEnum.USA)
            {
                htmlBuilder.AppendLine("<th style=\"width: 100px;\">" + Properties.Resources.ColHeaderHeadHousehold + "</th>");
            }
            else
            {
                htmlBuilder.AppendLine("<th style=\"width: 100px;\">" + Properties.Resources.Address + "</th>");
            }

            htmlBuilder.AppendLine("<th style=\"width: 70px;\">" + Properties.Resources.ColHeaderPhone + "</th>");
            htmlBuilder.AppendLine("<th style=\"width: 140px; border-right: 4px solid black;\">" + Properties.Resources.HTMLColHeaderHCWHealthFacility + "</th>");
            htmlBuilder.AppendLine("<th>" + Properties.Resources.HTMLColHeaderStatus + "</th>");

            htmlBuilder.AppendLine("<th style=\"width: 170px;\">" + Properties.Resources.HTMLColHeaderNotes + "</th>");
            htmlBuilder.AppendLine("</tr>");
            htmlBuilder.AppendLine("</thead>");
        }

        private void MarkupGridRow21(StringBuilder htmlBuilder, ContactViewModel contact)
        {
            htmlBuilder.AppendLine("<tr>");

            if (DataHelper.IsCountryUS)
            {
                htmlBuilder.AppendLine("<td colspan=\"10\" style=\"vertical-align: top;\"><small>" + Properties.Resources.HTMLColHeaderNotes + "</small></td>");
            }
            else
            {
                htmlBuilder.AppendLine("<td colspan=\"9\" style=\"vertical-align: top;\"><small>" + Properties.Resources.HTMLColHeaderNotes + "</small></td>");
            }

            DateTime? startDate = null;
            foreach (FollowUpVisitViewModel fuVM in contact.FollowUpWindowViewModel.FollowUpVisits)
            {
                if (!startDate.HasValue)
                {
                    startDate = fuVM.FollowUpVisit.Date;
                }
                //17197
                if (DataHelper.IsCountryUS)
                {

                    htmlBuilder.AppendLine("<td style=\"text-align: center;\">" + fuVM.FollowUpVisit.Date.Month + "<br/>" + fuVM.FollowUpVisit.Date.Day + "<br/>" + fuVM.FollowUpVisit.Date.Year.ToString().Substring(2) + "</td>");
                }
                else
                {
                    htmlBuilder.AppendLine("<td style=\"text-align: center;\">" + fuVM.FollowUpVisit.Date.Day + "<br/>" + fuVM.FollowUpVisit.Date.Month + "<br/>" + fuVM.FollowUpVisit.Date.Year.ToString().Substring(2) + "</td>");
                }
            }
            htmlBuilder.AppendLine("</tr>");
            htmlBuilder.AppendLine("<tr style=\"border-bottom: 4px solid black;\">");

            bool pastDue = false;
            if (contact.FollowUpWindowViewModel.WindowEndDate < DateTime.Today)
            {
                pastDue = true;
            }

            if (DataHelper.IsCountryUS)
            {
                htmlBuilder.AppendLine("<td style=\"font-size: 11pt;\">" + contact.ContactCDCID + "</td>");
                htmlBuilder.AppendLine("<td style=\"font-size: 11pt;\">" + contact.ContactStateID + "</td>");
            }
            else
            {
                htmlBuilder.AppendLine("<td style=\"font-size: 11pt;\">" + contact.ContactID + "</td>");
            }

            if (pastDue)
            {
                htmlBuilder.AppendLine("<td style=\"font-size: 11pt;\">* " + contact.Surname + "</td>");
            }
            else
            {
                htmlBuilder.AppendLine("<td style=\"font-size: 11pt;\">" + contact.Surname + "</td>");
            }

            htmlBuilder.AppendLine("<td style=\"font-size: 11pt;\">" + contact.OtherNames + "</td>");

            if (contact.Gender.Equals(Core.Enums.Gender.Male.ToString()))
            {
                htmlBuilder.AppendLine("<td>" + Properties.Resources.MaleSymbol + "</td>");
            }
            else if (contact.Gender.Equals(Core.Enums.Gender.Female.ToString()))
            {
                htmlBuilder.AppendLine("<td>" + Properties.Resources.FemaleSymbol + "</td>");
            }
            else
            {
                htmlBuilder.AppendLine("<td>&nbsp;</td>");
            }

            if (contact.AgeYears.HasValue)
            {
                htmlBuilder.AppendLine("<td>" + contact.AgeYears + "</td>");
            }
            else
            {
                htmlBuilder.AppendLine("<td>&nbsp;</td>");
            }
            //htmlBuilder.AppendLine("<td>" + startDate.Value.ToShortDateString() + "</td>");
            htmlBuilder.AppendLine("<td>" + contact.FollowUpWindowViewModel.WindowStartDate.AddDays(-1).ToShortDateString() + "</td>");
            //htmlBuilder.AppendLine("<td>Medium</td>");
            htmlBuilder.AppendLine("<td>" + contact.LastSourceCase.Surname + " " + contact.LastSourceCase.OtherNames + "</td>");

            if (DataHelper.IsCountryUS)
            {
                htmlBuilder.AppendLine("<td>" + contact.ContactAddress + "</td>");
            }
            else
            {
                htmlBuilder.AppendLine("<td>" + contact.HeadOfHousehold + "</td>");
            }

            htmlBuilder.AppendLine("<td>" + contact.Phone + "</td>");

            foreach (FollowUpVisitViewModel fuVM in contact.FollowUpWindowViewModel.FollowUpVisits)
            {
                htmlBuilder.AppendLine("<td style=\"text-align: center;\">");

                if (fuVM.Status.HasValue)
                {
                    if (fuVM.Status == ContactDailyStatus.SeenNotSick)
                    {
                        htmlBuilder.AppendLine("&#x2713;");
                    }
                    else if (fuVM.Status == ContactDailyStatus.SeenSickAndIsolated || fuVM.Status == ContactDailyStatus.SeenSickAndIsoNotFilledOut || fuVM.Status == ContactDailyStatus.SeenSickAndNotIsolated)
                    {
                        htmlBuilder.AppendLine("&#x2717;");
                    }
                    else if (fuVM.Status == ContactDailyStatus.NotSeen)
                    {
                        htmlBuilder.AppendLine("-");
                    }
                    else
                    {
                        htmlBuilder.AppendLine("&nbsp;");
                    }
                }
                else
                {
                    htmlBuilder.AppendLine("&nbsp;");
                }

                htmlBuilder.AppendLine("</td>");
            }
            htmlBuilder.AppendLine("</tr>");
            htmlBuilder.AppendLine("<!--end of row-->");
        }

        private void MarkupGridHeader21(StringBuilder htmlBuilder)
        {
            htmlBuilder.AppendLine("<table style=\"width: 1200px;  border: 4px solid black;\" align=\"left\">");
            htmlBuilder.AppendLine("<thead>");
            htmlBuilder.AppendLine("<tr>");

            if (DataHelper.IsCountryUS == false)
            {
                htmlBuilder.AppendLine("<th style=\"width: 10px;\">" + Properties.Resources.ColHeaderContactID + "</th>");
            }
            else
            {
                htmlBuilder.AppendLine("<th style=\"width: 10px;\">" + Properties.Resources.ColHeaderID + "</th>");
                htmlBuilder.AppendLine("<th style=\"width: 10px;\">" + Properties.Resources.ColHeaderOriginalID + "</th>");
            }

            htmlBuilder.AppendLine("<th style=\"width: 140px;\">" + Properties.Resources.ColHeaderSurname + "</th>");
            htmlBuilder.AppendLine("<th style=\"width: 140px;\">" + Properties.Resources.ColHeaderOtherNames + "</th>");
            htmlBuilder.AppendLine("<th>" + Properties.Resources.HTMLColHeaderSex + "</th>");
            htmlBuilder.AppendLine("<th>" + Properties.Resources.ColHeaderAge + "</th>");
            htmlBuilder.AppendLine("<th style=\"width: 70px;\">" + Properties.Resources.HTMLColHeaderDateLastContact + "</th>");
            htmlBuilder.AppendLine("<th style=\"width: 110px;\">" + Properties.Resources.SourceCase + "</th>");

            if (DataHelper.IsCountryUS == false)
            {
                htmlBuilder.AppendLine("<th style=\"width: 100px;\">" + Properties.Resources.HTMLColHeaderHeadHousehold + "</th>");
            }
            else
            {
                htmlBuilder.AppendLine("<th style=\"width: 100px;\">" + Properties.Resources.Address + "</th>");
            }

            htmlBuilder.AppendLine("<th style=\"width: 80px;\">" + Properties.Resources.ColHeaderPhone + "</th>");
            htmlBuilder.AppendLine("<th>1</th>");
            htmlBuilder.AppendLine("<th>2</th>");
            htmlBuilder.AppendLine("<th>3</th>");
            htmlBuilder.AppendLine("<th>4</th>");
            htmlBuilder.AppendLine("<th>5</th>");
            htmlBuilder.AppendLine("<th>6</th>");
            htmlBuilder.AppendLine("<th>7</th>");
            htmlBuilder.AppendLine("<th>8</th>");
            htmlBuilder.AppendLine("<th>9</th>");
            htmlBuilder.AppendLine("<th>10</th>");
            htmlBuilder.AppendLine("<th>11</th>");
            htmlBuilder.AppendLine("<th>12</th>");
            htmlBuilder.AppendLine("<th>13</th>");
            htmlBuilder.AppendLine("<th>14</th>");

            if (Core.Common.DaysInWindow == 21)
            {
                htmlBuilder.AppendLine("<th>15</th>");
                htmlBuilder.AppendLine("<th>16</th>");
                htmlBuilder.AppendLine("<th>17</th>");
                htmlBuilder.AppendLine("<th>18</th>");
                htmlBuilder.AppendLine("<th>19</th>");
                htmlBuilder.AppendLine("<th>20</th>");
                htmlBuilder.AppendLine("<th>21</th>");
            }

            htmlBuilder.AppendLine("</tr>");
            htmlBuilder.AppendLine("</thead>");
            htmlBuilder.AppendLine("<tbody>");
        }

        private void MarkupStratification(string sortClause, StringBuilder htmlBuilder, ContactViewModel contact)
        {
            string teamString = "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;";

            if (sortClause.Split(new char[] { ',' }).Contains<string>("Team"))
            {
                teamString = contact.Team;
            }

            htmlBuilder.AppendLine("</tbody></table>");

            htmlBuilder.AppendLine("<table style=\"width: 1200px; border: 0px; padding: 0px; margin: 0px; clear: left; \">");
            htmlBuilder.AppendLine("    <tr style=\"border: 0px;\">");
            htmlBuilder.AppendLine("        <td width=\"50%\" style=\"border: 0px; font-size: 13pt;\">");
            htmlBuilder.AppendLine("            <span style=\"font-weight: bold;\">" + DataHelper.Adm4 + ":</span> " + contact.Village + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
            htmlBuilder.AppendLine("            <span style=\"font-weight: bold;\">" + DataHelper.Adm2 + ":</span> " + contact.SubCounty + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
            htmlBuilder.AppendLine("            <span style=\"font-weight: bold;\">" + DataHelper.Adm1 + ":</span> " + contact.District + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
            htmlBuilder.AppendLine("        </td>");
            htmlBuilder.AppendLine("        <td width=\"50%\" style=\"border: 0px;font-size: 13pt; text-align:right\">");
            htmlBuilder.AppendLine("            <span style=\"font-weight: bold;\">" + "Team(s):" + "</span> " + teamString);
            htmlBuilder.AppendLine("        </td>");
            htmlBuilder.AppendLine("    </tr>");
            htmlBuilder.AppendLine("</table>");
        }

        private void MarkupStratification(string sortClause, StringBuilder htmlBuilder, DailyCheckViewModel dcvm)
        {
            string teamString = "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;";

            if (sortClause.Split(new char[] { ',' }).Contains<string>("Team"))
            {
                teamString = dcvm.ContactVM.Team;
            }

            htmlBuilder.AppendLine("</tbody></table>");

            htmlBuilder.AppendLine("<table style=\"width: 1200px; border: 0px; padding: 0px; margin: 0px; clear: left; \">");
            htmlBuilder.AppendLine("    <tr style=\"border: 0px;\">");
            htmlBuilder.AppendLine("        <td width=\"50%\" style=\"border: 0px; font-size: 13pt;\">");
            htmlBuilder.AppendLine("            <span style=\"font-weight: bold;\">" + DataHelper.Adm4 + ":</span> " + dcvm.ContactVM.Village + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
            htmlBuilder.AppendLine("            <span style=\"font-weight: bold;\">" + DataHelper.Adm2 + ":</span> " + dcvm.ContactVM.SubCounty + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
            htmlBuilder.AppendLine("            <span style=\"font-weight: bold;\">" + DataHelper.Adm1 + ":</span> " + dcvm.ContactVM.District + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
            htmlBuilder.AppendLine("        </td>");
            htmlBuilder.AppendLine("        <td width=\"50%\" style=\"border: 0px;font-size: 13pt; text-align:right\">");
            htmlBuilder.AppendLine("            <span style=\"font-weight: bold;\">" + "Team(s):" + "</span> " + teamString);
            htmlBuilder.AppendLine("        </td>");
            htmlBuilder.AppendLine("    </tr>");
            htmlBuilder.AppendLine("</table>");
        }

        private void MarkupReportHeader(string filterClause, string sortClause, StringBuilder htmlBuilder, ObservableCollection<DailyCheckViewModel> collection = null)
        {
            string reportHeaderSortDisplayString = UserSortString(sortClause);
            string reportHeaderFilterDisplayString = UserFilterString(filterClause);

            htmlBuilder.AppendLine("<table style=\"width: 1200px; border: 0px; padding: 0px; margin: 0px; clear: left; \">");
            htmlBuilder.AppendLine("    <tr style=\"border: 0px;\">");
            htmlBuilder.AppendLine("        <td style=\"border: 0px; font-size: 13pt;\">");
            htmlBuilder.AppendLine("            <span style=\"font-weight: bold; text-decoration: underline;\">VHF " + Properties.Resources.HTMLContactTracingDailyFollowUpheader + "</span>");
            htmlBuilder.AppendLine("        </td>");
            htmlBuilder.AppendLine("        <td style=\"border: 0px;font-size: 13pt; text-align:right\">");
            if (reportHeaderFilterDisplayString != "")
            {
                htmlBuilder.AppendLine("            <span style=\"font-weight: bold;\">" + "Filter: " + "</span>");
                htmlBuilder.AppendLine(reportHeaderFilterDisplayString);
            }
            htmlBuilder.AppendLine("        </td>");
            htmlBuilder.AppendLine("    </tr>");
            htmlBuilder.AppendLine("    <tr style=\"border: 0px;\">");
            htmlBuilder.AppendLine("        <td style=\"border: 0px; font-size: 13pt;\">");

            if (collection == DataHelper.PrevFollowUpCollection)
            {
                htmlBuilder.AppendLine("   <span style=\"font-weight: bold;\">" + Properties.Resources.HTMLDate + "</span> " + dpPrev.SelectedDate.Value.ToShortDateString());
            }
            else
            {
                htmlBuilder.AppendLine("   <span style=\"font-weight: bold;\">" + Properties.Resources.HTMLDate + "</span> " + DateTime.Now.ToShortDateString());
            }

            htmlBuilder.AppendLine("        </td>");
            htmlBuilder.AppendLine("        <td style=\"border: 0px;font-size: 13pt; text-align:right\">");
            if (reportHeaderSortDisplayString != "")
            {
                htmlBuilder.AppendLine("            <span style=\"font-weight: bold;\">" + "Sort: " + "</span>");
                htmlBuilder.AppendLine(reportHeaderSortDisplayString);
            }
            htmlBuilder.AppendLine("        </td>");
            htmlBuilder.AppendLine("    </tr>");
            htmlBuilder.AppendLine("</table>");

            htmlBuilder.AppendLine("<table width=\"100%\" style=\"border: 0px; padding: 0px; margin: 0px; clear: left; width:100%; \">");
            htmlBuilder.AppendLine(" <tr style=\"border: 0px;\">");
            htmlBuilder.AppendLine("  <td width=\"50%\" style=\"border: 0px;\">");
            htmlBuilder.AppendLine("  <ul style=\"font-size: 11pt;\">");
            htmlBuilder.AppendLine("   <li>" + String.Format(Properties.Resources.HTML21DayInstructions1, "&#x2713;") + "</li>");
            htmlBuilder.AppendLine("   <li>" + String.Format(Properties.Resources.HTML21DayInstructions2, "&#x2717;") + "</li>");
            htmlBuilder.AppendLine("   <li>" + String.Format(Properties.Resources.HTML21DayInstructions3, "–") + "</li>");
            htmlBuilder.AppendLine("  </ul>");
            htmlBuilder.AppendLine(" </tr>");
            htmlBuilder.AppendLine("</table>");
        }

        private void MarkupZeroFound(string filterClause, string sortClause, StringBuilder htmlBuilder)
        {
            string reportHeaderSortDisplayString = UserSortString(sortClause);
            string reportHeaderFilterDisplayString = UserFilterString(filterClause);

            htmlBuilder.AppendLine("<table style=\"width: 1200px; border: 0px; padding: 0px; margin: 0px; clear: left; \">");
            htmlBuilder.AppendLine("    <tr style=\"border: 0px;\">");
            htmlBuilder.AppendLine("        <td style=\"border: 0px;font-size: 13pt; text-align:left\">");

            if (reportHeaderFilterDisplayString != "")
            {
                htmlBuilder.AppendLine("            <span style=\"font-weight: bold;\">" + "Filter: " + "</span>");
                htmlBuilder.AppendLine(reportHeaderFilterDisplayString);
            }

            htmlBuilder.AppendLine("        </td>");
            htmlBuilder.AppendLine("    </tr>");
            htmlBuilder.AppendLine("    <tr style=\"border: 0px;\">");
            htmlBuilder.AppendLine("        <td style=\"border: 0px;font-size: 13pt; text-align:left\">");
            htmlBuilder.AppendLine("            <span style=\"font-weight: bold;\">" + "Zero (0) records found." + "</span>");
            htmlBuilder.AppendLine("        </td>");
            htmlBuilder.AppendLine("    </tr>");
            htmlBuilder.AppendLine("</table>");
        }

        private string UserSortString(string sortClause)
        {
            Dictionary<int, Boundry> ba = DataHelper.BoundaryAggregation;

            string[] sortArray = sortClause.Split(new char[] { ',' });

            string reportHeaderSortDisplayString = string.Empty;

            foreach (string sort in sortArray)
            {
                if (reportHeaderSortDisplayString != string.Empty)
                {
                    reportHeaderSortDisplayString += " then ";
                }

                var name = from boundry in ba
                           where boundry.Value.ObjectResolution.Contains(sort)
                           select boundry.Value.Name;

                List<string> names = name.ToList<string>();

                if (names.Count > 0)
                {
                    reportHeaderSortDisplayString += names[0];
                }
                else
                {
                    reportHeaderSortDisplayString += sort.Replace("ContactVM.", "");
                }
            }
            return reportHeaderSortDisplayString;
        }

        private string UserFilterString(string filterClause)
        {
            Dictionary<int, Boundry> ba = DataHelper.BoundaryAggregation;

            foreach (Boundry boundry in ba.Values)
            {
                filterClause = filterClause.Replace(boundry.ObjectResolution, boundry.Name);
            }

            if (filterClause.Contains(" or "))
            {
                filterClause = "<span style=\"font-weight: bold;\">" + "(" + "</span>" + filterClause;
            }

            filterClause = filterClause.Replace(" or ", "<span style=\"font-weight: bold;\">" + ") or (" + "</span>");
            filterClause = filterClause.Replace("ContactVM.", "").Replace("==", "equals").Replace("!=", "not equal");

            if (filterClause.Contains(" or "))
            {
                filterClause = filterClause + "<span style=\"font-weight: bold;\">" + ")" + "</span>";
            }

            return filterClause;
        }

        private void GenerateDailyHtmlFooter(StringBuilder htmlBuilder, ref int rowsGenerated)
        {
            rowsGenerated = 0;
            GenerateDailyHtmlFooter(htmlBuilder);
        }

        private void GenerateDailyHtmlFooter(StringBuilder htmlBuilder)
        {
            htmlBuilder.Append("</tbody>");
            htmlBuilder.Append("</table>");

            if (Core.Common.DaysInWindow == 14)
            {
                htmlBuilder.Append("<p style=\"clear: left; font-size: 8pt; margin-top: 4px;\">" + Properties.Resources.HTMLPast14DayFootnote + "</p>");
            }
            else
            {
                htmlBuilder.Append("<p style=\"clear: left; font-size: 8pt; margin-top: 4px;\">" + Properties.Resources.HTMLPast21DayFootnote + "</p>");
            }
            htmlBuilder.Append("<div style=\"page-break-before:always;\" />");
        }

        private void btnExcelReport_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog dlg = new System.Windows.Forms.SaveFileDialog();
            dlg.AutoUpgradeEnabled = true;
            dlg.DefaultExt = ".xls";
            dlg.Filter = "Excel 97-2003 Workbook |*.xls";
            dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            System.Windows.Forms.DialogResult result = dlg.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                if (!DataHelper.IsCountryUS)
                    DataHelper.GenerateExcelDailyFollowUp(DataHelper.DailyFollowUpCollection, dpPrev.SelectedDate, dlg.FileName);
                else
                    DataHelper.GenerateExcelDailyFollowUpforUS(DataHelper.DailyFollowUpCollection, dpPrev.SelectedDate, dlg.FileName);
            }
        }

        private void btnPrint21DayFollowUp_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Forms.Control.ModifierKeys == System.Windows.Forms.Keys.Alt)
            {
                Popup = new Popup();
                Popup.Parent = grdMain;

                FilterSortDropdown printSortForm = new FilterSortDropdown(DataHelper, IsSuperUser);
                printSortForm.Collection = DataHelper.ContactCollection;
                printSortForm.Closed += printSortForm_Closed;
                printSortForm.Print += printSortForm_Print21DayFollowUp;
                printSortForm.DataContext = this.DataContext;
                printSortForm.MaxWidth = 607;
                printSortForm.MaxHeight = 436;
                Popup.Content = printSortForm;
                Popup.Show();

                e.Handled = true;
                return;
            }
            else
            {
                DataHelper.Print21DayFollowUp(DataHelper.ContactCollection);
            }
        }

        private void Print21DayFollowUp(ObservableCollection<ContactViewModel> collection)
        {
            string baseFileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString("N");
            Dictionary<int, Boundry> ba = DataHelper.BoundaryAggregation;
            StringBuilder htmlBuilder = new StringBuilder();
            IMultiValueConverter dateConverter = new DateConverter();
            htmlBuilder.Append(ContactTracing.Core.Common.GetHtmlHeader().ToString());
            DateTime dt = DateTime.Now;
            DateTime minDate = dt.AddDays(-1 * ContactTracing.Core.Common.DaysInWindow);

            var query = from contact in collection
                        where contact.FollowUpWindowViewModel != null && String.IsNullOrEmpty(contact.FinalOutcome)
                        //orderby contact.Surname, contact.OtherNames
                        //&& contact.FollowUpWindowViewModel.WindowStartDate >= minDate
                        group contact by string.Concat("<span style=\"font-weight: bold;\">" + DataHelper.Adm4 + "</span> ", contact.Village);

            int rowsGenerated = 0;
            bool firstPage = true;

            foreach (var entry in query)
            {
                htmlBuilder.AppendLine("<table width=\"100%\" style=\"border: 0px; padding: 0px; margin: 0px; clear: left; width:100%; \">");
                htmlBuilder.AppendLine(" <tr style=\"border: 0px;\">");
                htmlBuilder.AppendLine("  <td width=\"50%\" style=\"border: 0px;\">");
                //htmlBuilder.AppendLine("   <p style=\"font-size: 13pt; font-weight: bold; clear: left; text-decoration: underline;\">Uganda Viral Hemorrhagic Fever</p>");
                //htmlBuilder.AppendLine("   <p style=\"font-size: 13pt; font-weight: bold; text-decoration: underline;\">Contact Tracing 21-day Follow-up List</p>");
                htmlBuilder.AppendLine("   <p style=\"font-size: 13pt; font-weight: bold; clear: left; text-decoration: underline;\">" + Properties.Settings.Default.HtmlPrintoutTitle + "</p>");

                if (Core.Common.DaysInWindow == 14)
                {
                    htmlBuilder.AppendLine("   <p style=\"font-size: 13pt; font-weight: bold; text-decoration: underline;\">" + Properties.Resources.HTMLContactTracingFollowUpListHeading14Days + "</p>");
                }
                else
                {
                    htmlBuilder.AppendLine("   <p style=\"font-size: 13pt; font-weight: bold; text-decoration: underline;\">" + Properties.Resources.HTMLContactTracingFollowUpListHeading21Days + "</p>");
                }
                htmlBuilder.AppendLine("   <p style=\"font-size: 13pt;\"><span style=\"font-weight: bold;\">" + Properties.Resources.HTMLDatePrinted + "</span> " + DateTime.Now.ToShortDateString() + "</p>");
                htmlBuilder.AppendLine("  </td>");
                htmlBuilder.AppendLine("  <td width=\"50%\" style=\"border: 0px;\">");
                //htmlBuilder.AppendLine("   <p style=\"font-size: 13pt; font-weight: bold; clear: left;\">Team:</p>");
                //htmlBuilder.AppendLine("   <p style=\"font-size: 13pt; font-weight: bold; \">Team Leader:</p>");
                htmlBuilder.AppendLine("  </td>");
                htmlBuilder.AppendLine(" </tr>");
                htmlBuilder.AppendLine("</table>");

                htmlBuilder.AppendLine("<table width=\"100%\" style=\"border: 0px; padding: 0px; margin: 0px; clear: left; width:100%; \">");
                htmlBuilder.AppendLine(" <tr style=\"border: 0px;\">");
                htmlBuilder.AppendLine("  <td width=\"50%\" style=\"border: 0px;\">");
                htmlBuilder.AppendLine("<ul style=\"font-size: 11pt;\">");
                htmlBuilder.AppendLine("<li>" + String.Format(Properties.Resources.HTML21DayInstructions1, "&#x2713;") + "</li>");
                htmlBuilder.AppendLine("<li>" + String.Format(Properties.Resources.HTML21DayInstructions2, "&#x2717;") + "</li>");
                htmlBuilder.AppendLine("<li>" + String.Format(Properties.Resources.HTML21DayInstructions3, "–") + "</li>");
                htmlBuilder.AppendLine("</ul>");
                htmlBuilder.AppendLine("  </td>");
                htmlBuilder.AppendLine("  </td>");
                htmlBuilder.AppendLine("  <td width=\"50%\" style=\"border: 0px;\">");
                htmlBuilder.AppendLine("   <p style=\"font-size: 13pt; font-weight: bold; clear: left;\">" + Properties.Resources.HTMLTeam + "</p>");
                htmlBuilder.AppendLine("   <p style=\"font-size: 13pt; font-weight: bold; \">" + Properties.Resources.HTMLTeamLeader + "</p>");
                htmlBuilder.AppendLine("  </td>");
                htmlBuilder.AppendLine(" </tr>");
                htmlBuilder.AppendLine("</table>");

                foreach (var contact in entry)
                {
                    if (rowsGenerated == 0)
                    {
                        //htmlBuilder.AppendLine("<p style=\"font-weight: bold; clear: left;\">" + entry.Key + ". LC1 Chairman: " + contact.LC1Chairman + "</p>");

                        if (DataHelper.IsCountryUS)
                        {
                            htmlBuilder.AppendLine("<p style=\"clear: left;\">" +
                                entry.Key +
                                "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<span style=\"font-weight: bold;\">" + DataHelper.Adm2 + "</span> " +
                                contact.SubCounty +
                                "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<span style=\"font-weight: bold;\">" + DataHelper.Adm1 + "</span> " +
                                contact.District +
                                "<br /></p>");
                        }
                        else
                        {
                            htmlBuilder.AppendLine("<p style=\"clear: left;\">" +
                                entry.Key +
                                "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<span style=\"font-weight: bold;\">" + DataHelper.Adm2 + "</span> " +
                                contact.SubCounty +
                                "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<span style=\"font-weight: bold;\">" + DataHelper.Adm1 + "</span> " +
                                contact.District +
                                "<br /><span style=\"font-weight: bold;\">" + Properties.Resources.HTMLLC1ChairmanHeading + "</span> " + contact.LC1Chairman + "</p>");
                        }

                        htmlBuilder.AppendLine("<table style=\"width: 1200px; border: 4px solid black;\" align=\"left\">");
                        htmlBuilder.AppendLine("<thead>");
                        htmlBuilder.AppendLine("<tr>");

                        if (DataHelper.IsCountryUS)
                        {
                            htmlBuilder.AppendLine("<th style=\"width: 10px;\">" + Properties.Resources.ColHeaderID + "</th>");
                            htmlBuilder.AppendLine("<th style=\"width: 10px;\">" + Properties.Resources.ColHeaderOriginalID + "</th>");
                        }
                        else
                        {
                            htmlBuilder.AppendLine("<th style=\"width: 10px;\">" + Properties.Resources.ColHeaderContactID + "</th>");
                        }

                        htmlBuilder.AppendLine("<th style=\"width: 140px;\">" + Properties.Resources.ColHeaderSurname + "</th>");
                        htmlBuilder.AppendLine("<th style=\"width: 140px;\">" + Properties.Resources.ColHeaderOtherNames + "</th>");
                        htmlBuilder.AppendLine("<th>" + Properties.Resources.HTMLColHeaderSex + "</th>");
                        htmlBuilder.AppendLine("<th>" + Properties.Resources.ColHeaderAge + "</th>");
                        htmlBuilder.AppendLine("<th style=\"width: 70px;\">" + Properties.Resources.HTMLColHeaderDateLastContact + "</th>");
                        htmlBuilder.AppendLine("<th style=\"width: 110px;\">" + Properties.Resources.SourceCase + "</th>");

                        if (DataHelper.IsCountryUS == false)
                        {
                            htmlBuilder.AppendLine("<th style=\"width: 100px;\">" + Properties.Resources.HTMLColHeaderHeadHousehold + "</th>");
                        }
                        else
                        {
                            htmlBuilder.AppendLine("<th style=\"width: 100px;\">" + Properties.Resources.Address + "</th>");
                        }

                        htmlBuilder.AppendLine("<th style=\"width: 80px;\">" + Properties.Resources.ColHeaderPhone + "</th>");
                        htmlBuilder.AppendLine("<th>1</th>");
                        htmlBuilder.AppendLine("<th>2</th>");
                        htmlBuilder.AppendLine("<th>3</th>");
                        htmlBuilder.AppendLine("<th>4</th>");
                        htmlBuilder.AppendLine("<th>5</th>");
                        htmlBuilder.AppendLine("<th>6</th>");
                        htmlBuilder.AppendLine("<th>7</th>");
                        htmlBuilder.AppendLine("<th>8</th>");
                        htmlBuilder.AppendLine("<th>9</th>");
                        htmlBuilder.AppendLine("<th>10</th>");
                        htmlBuilder.AppendLine("<th>11</th>");
                        htmlBuilder.AppendLine("<th>12</th>");
                        htmlBuilder.AppendLine("<th>13</th>");
                        htmlBuilder.AppendLine("<th>14</th>");
                        if (Core.Common.DaysInWindow == 21)
                        {
                            htmlBuilder.AppendLine("<th>15</th>");
                            htmlBuilder.AppendLine("<th>16</th>");
                            htmlBuilder.AppendLine("<th>17</th>");
                            htmlBuilder.AppendLine("<th>18</th>");
                            htmlBuilder.AppendLine("<th>19</th>");
                            htmlBuilder.AppendLine("<th>20</th>");
                            htmlBuilder.AppendLine("<th>21</th>");
                        }
                        htmlBuilder.AppendLine("</tr>");
                        htmlBuilder.AppendLine("</thead>");
                        htmlBuilder.AppendLine("<tbody>");
                    }

                    htmlBuilder.AppendLine("<tr>");

                    if (DataHelper.IsCountryUS)
                    {
                        htmlBuilder.AppendLine("<td colspan=\"10\" style=\"vertical-align: top;\"><small>" + Properties.Resources.HTMLColHeaderNotes + "</small></td>");
                    }
                    else
                    {
                        htmlBuilder.AppendLine("<td colspan=\"9\" style=\"vertical-align: top;\"><small>" + Properties.Resources.HTMLColHeaderNotes + "</small></td>");
                    }

                    DateTime? startDate = null;
                    foreach (FollowUpVisitViewModel fuVM in contact.FollowUpWindowViewModel.FollowUpVisits)
                    {
                        if (!startDate.HasValue)
                        {
                            startDate = fuVM.FollowUpVisit.Date;
                        }
                        //17197
                        if (DataHelper.IsCountryUS)
                        {

                            htmlBuilder.AppendLine("<td style=\"text-align: center;\">" + fuVM.FollowUpVisit.Date.Month + "<br/>" + fuVM.FollowUpVisit.Date.Day + "<br/>" + fuVM.FollowUpVisit.Date.Year.ToString().Substring(2) + "</td>");
                        }
                        else
                        {
                            htmlBuilder.AppendLine("<td style=\"text-align: center;\">" + fuVM.FollowUpVisit.Date.Day + "<br/>" + fuVM.FollowUpVisit.Date.Month + "<br/>" + fuVM.FollowUpVisit.Date.Year.ToString().Substring(2) + "</td>");

                        }
                    }
                    htmlBuilder.AppendLine("</tr>");
                    htmlBuilder.AppendLine("<tr style=\"border-bottom: 4px solid black;\">");

                    if (DataHelper.IsCountryUS)
                    {
                        htmlBuilder.AppendLine("<td style=\"font-size: 11pt;\">" + contact.ContactCDCID + "</td>");
                        htmlBuilder.AppendLine("<td style=\"font-size: 11pt;\">" + contact.ContactStateID + "</td>");
                    }
                    else
                    {
                        htmlBuilder.AppendLine("<td style=\"font-size: 11pt;\">" + contact.ContactID + "</td>");
                    }

                    //if (contact.FollowUpWindowViewModel.WindowStartDate >= minDate)
                    bool pastDue = false;
                    if (contact.FollowUpWindowViewModel.WindowEndDate < DateTime.Today)
                    {
                        pastDue = true;
                    }

                    if (pastDue)
                    {
                        htmlBuilder.AppendLine("<td style=\"font-size: 11pt;\">* " + contact.Surname + "</td>");
                    }
                    else
                    {
                        htmlBuilder.AppendLine("<td style=\"font-size: 11pt;\">" + contact.Surname + "</td>");
                    }

                    htmlBuilder.AppendLine("<td style=\"font-size: 11pt;\">" + contact.OtherNames + "</td>");

                    if (contact.Gender.Equals(Core.Enums.Gender.Male.ToString()))
                    {
                        htmlBuilder.AppendLine("<td>" + Properties.Resources.MaleSymbol + "</td>");
                    }
                    else if (contact.Gender.Equals(Core.Enums.Gender.Female.ToString()))
                    {
                        htmlBuilder.AppendLine("<td>" + Properties.Resources.FemaleSymbol + "</td>");
                    }
                    else
                    {
                        htmlBuilder.AppendLine("<td>&nbsp;</td>");
                    }
                    //switch (contact.Gender)
                    //{
                    //    case "Male":
                    //        htmlBuilder.AppendLine("<td>M</td>");
                    //        break;
                    //    case "Female":
                    //        htmlBuilder.AppendLine("<td>F</td>");
                    //        break;
                    //}

                    if (contact.AgeYears.HasValue)
                    {
                        htmlBuilder.AppendLine("<td>" + contact.AgeYears + "</td>");
                    }
                    else
                    {
                        htmlBuilder.AppendLine("<td>&nbsp;</td>");
                    }
                    //htmlBuilder.AppendLine("<td>" + startDate.Value.ToShortDateString() + "</td>");
                    //htmlBuilder.AppendLine("<td>" + contact.FollowUpWindowViewModel.WindowStartDate.AddDays(-1).ToShortDateString() + "</td>");
                    string[] parms = { contact.FollowUpWindowViewModel.WindowStartDate.AddDays(-1).ToString(), DataHelper.ApplicationCulture };
                    var windowstartdate = dateConverter.Convert(parms, null, null, null);
                    htmlBuilder.AppendLine("<td>" + windowstartdate + "</td>");
                    //htmlBuilder.AppendLine("<td>Medium</td>");
                    htmlBuilder.AppendLine("<td>" + contact.LastSourceCase.Surname + " " + contact.LastSourceCase.OtherNames + "</td>");

                    if (DataHelper.IsCountryUS)
                    {
                        htmlBuilder.AppendLine("<td>" + contact.ContactAddress + "</td>");
                    }
                    else
                    {
                        htmlBuilder.AppendLine("<td>" + contact.HeadOfHousehold + "</td>");
                    }

                    htmlBuilder.AppendLine("<td>" + contact.Phone + "</td>");

                    foreach (FollowUpVisitViewModel fuVM in contact.FollowUpWindowViewModel.FollowUpVisits)
                    {
                        htmlBuilder.AppendLine("<td style=\"text-align: center;\">");

                        if (fuVM.Status.HasValue)
                        {
                            if (fuVM.Status == ContactDailyStatus.SeenNotSick)
                            {
                                htmlBuilder.AppendLine("&#x2713;");
                            }
                            else if (fuVM.Status == ContactDailyStatus.SeenSickAndIsolated || fuVM.Status == ContactDailyStatus.SeenSickAndIsoNotFilledOut || fuVM.Status == ContactDailyStatus.SeenSickAndNotIsolated)
                            {
                                htmlBuilder.AppendLine("&#x2717;");
                            }
                            else if (fuVM.Status == ContactDailyStatus.NotSeen)
                            {
                                htmlBuilder.AppendLine("-");
                            }
                            else
                            {
                                htmlBuilder.AppendLine("&nbsp;");
                            }
                        }
                        else
                        {
                            htmlBuilder.AppendLine("&nbsp;");
                        }

                        htmlBuilder.AppendLine("</td>");
                    }
                    htmlBuilder.AppendLine("</tr>");
                    rowsGenerated++;

                    if (firstPage && rowsGenerated == 5)
                    {
                        htmlBuilder.Append("</tbody>");
                        htmlBuilder.Append("</table>");
                        if (Core.Common.DaysInWindow == 21)
                        {
                            htmlBuilder.Append("<p style=\"clear: left; font-size: 8pt; margin-top: 4px;\">" + Properties.Resources.HTMLPast21DayFootnote + "</p>");
                        }
                        else if (Core.Common.DaysInWindow == 14)
                        {
                            htmlBuilder.Append("<p style=\"clear: left; font-size: 8pt; margin-top: 4px;\">" + Properties.Resources.HTMLPast14DayFootnote + "</p>");
                        }
                        htmlBuilder.Append("<div style=\"page-break-before:always;\" />");
                        rowsGenerated = 0;
                        firstPage = false;
                    }
                    else if (!firstPage && rowsGenerated == 7)
                    {
                        htmlBuilder.Append("</tbody>");
                        htmlBuilder.Append("</table>");
                        if (Core.Common.DaysInWindow == 21)
                        {
                            htmlBuilder.Append("<p style=\"clear: left; font-size: 8pt; margin-top: 4px;\">" + Properties.Resources.HTMLPast21DayFootnote + "</p>");
                        }
                        else if (Core.Common.DaysInWindow == 14)
                        {
                            htmlBuilder.Append("<p style=\"clear: left; font-size: 8pt; margin-top: 4px;\">" + Properties.Resources.HTMLPast14DayFootnote + "</p>");
                        }
                        htmlBuilder.Append("<div style=\"page-break-before:always;\" />");
                        rowsGenerated = 0;
                    }
                }

                if (firstPage && rowsGenerated % 5 != 0)
                {
                    htmlBuilder.Append("</tbody>");
                    htmlBuilder.Append("</table>");
                    if (Core.Common.DaysInWindow == 21)
                    {
                        htmlBuilder.Append("<p style=\"clear: left; font-size: 8pt; margin-top: 4px;\">" + Properties.Resources.HTMLPast21DayFootnote + "</p>");
                    }
                    else if (Core.Common.DaysInWindow == 14)
                    {
                        htmlBuilder.Append("<p style=\"clear: left; font-size: 8pt; margin-top: 4px;\">" + Properties.Resources.HTMLPast14DayFootnote + "</p>");
                    }
                    htmlBuilder.Append("<div style=\"page-break-before:always;\" />");
                    rowsGenerated = 0;
                    firstPage = true;
                }
                else if (!firstPage && rowsGenerated % 7 != 0)
                {
                    htmlBuilder.Append("</tbody>");
                    htmlBuilder.Append("</table>");
                    if (Core.Common.DaysInWindow == 21)
                    {
                        htmlBuilder.Append("<p style=\"clear: left; font-size: 8pt; margin-top: 4px;\">" + Properties.Resources.HTMLPast21DayFootnote + "</p>");
                    }
                    else if (Core.Common.DaysInWindow == 14)
                    {
                        htmlBuilder.Append("<p style=\"clear: left; font-size: 8pt; margin-top: 4px;\">" + Properties.Resources.HTMLPast14DayFootnote + "</p>");
                    }
                    htmlBuilder.Append("<div style=\"page-break-before:always;\" />");
                    rowsGenerated = 0;
                    firstPage = true;
                }
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


        private void btnEditDailyContact_Click(object sender, RoutedEventArgs e)
        {
            if (dgDaily.SelectedItems.Count == 1)
            {
                DailyCheckViewModel dcVM = dgDaily.SelectedItem as DailyCheckViewModel;
                if (dcVM != null && dcVM.ContactVM.IsActive)
                {
                    EditContact(dcVM.ContactVM);
                }
            }
        }

        private void btnAddContact_Click(object sender, RoutedEventArgs e)
        {
            //Database = DataHelper.Project.CollectedData.GetDatabase();
            //string ContactFormTableName = DataHelper.ContactForm.TableName;
            //string userID = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString();

            //foreach (CaseViewModel caseVM in DataHelper.CaseCollection)
            ////Parallel.ForEach(DataHelper.CaseCollection, caseVM =>
            //{
            //    if (caseVM.Contacts.Count == 0 && !String.IsNullOrEmpty(caseVM.ID) &&
            //        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable || caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Suspect || caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed)
            //        )
            //    {

            //        Random rNumContacts = new Random();
            //        int numContacts = rNumContacts.Next(1, 30);

            //        for (int i = 0; i < numContacts; i++)
            //        {
            //            DateTime dtNow = DateTime.Now;

            //            Guid guid = System.Guid.NewGuid();

            //            List<string> lastNames = new List<string>() { "Smith", "Johnson", "Williams", "Brown", "Jones", "Miller", "Hatfield", "McCoy", "Davis", "Garcia", "Rodriguez", "Wilson", "Martinez", "Anderson", "Taylor", "Thomas", "Hernandez", "Moore", "Martin", "Jackson", "Thompson", "White", "Lopez", "Lee", "Gonzalez", "Harris", "Clark", "Lewis", "Robinson", "Walker", "Perez", "Hall", "Young", "Allen", "Sanchez", "Wright", "King", "Scott", "Green", "Baker", "Adams", "Nelson", "Hill", "Ramirez", "Campbell", "Mitchell", "Roberts", "Carter", "Phillips", "Evans", "Turner", "Torres", "Parker", "Collins", "Edwards", "Stewart", "Flores", "Morris", "Nguyen", "Murphy", "Rivera", "Cook", "Rogers", "Morgan", "Peterson", "Cooper", "Reed", "Bailey", "Bell", "Gomez", "Kelley", "Howard", "Ward", "Cox", "Diaz", "Richardson", "Wood", "Watson", "Brooks", "Bennet", "Gray", "James", "Reyes", "Cruz", "Hughes", "Price", "Myers", "Long", "Foster", "Sanders", "Ross", "Morales", "Powell", "Sullivan", "Russell", "Ortiz", "Jenkins", "Guiterrez", "Perry", "Butler", "Barnes", "Fisher", "Henderson", "Coleman", "Simmons", "Patterson", "Jordan", "Reynolds", "Hamilton", "Graham", "Kim", "Gonzalez", "Alexander", "Ramos", "Wallace", "Griffin", "West", "Cole", "Hayes", "Chavez", "Gibson", "Bryant", "Ellis", "Stevens", "Murray", "Ford", "Marshall", "Owens", "McDonald", "Harrison", "Ruiz", "Kennedy", "Wells", "Alvarez", "Woods", "Mendoza", "Castillo", "Olson", "Webb", "Washington", "Tucker", "Freeman", "Burns", "Henry", "Vasquez", "Snyder", "Simpson", "Crawford", "Jimenez", "Porter", "Mason", "Shaw", "Gordon", "Wagner", "Hunter", "Romero", "Hicks", "Dixon", "Hunt", "Palmer", "Robertson", "Black", "Holmes", "Stone", "Meyer", "Boyd", "Mills", "Warren", "Fox", "Rose", "Rice", "Moreno", "Schmidt", "Patel", "Ferguson", "Nichols", "Herrera", "Medina", "Ryan", "Fernandez", "Weaver", "Daniels", "Stephens", "Gardner", "Payne", "Kelley", "Dunn", "Pierce", "Arnold", "Tran", "Spencer", "Peters", "Hawkins", "Grant", "Hansen", "Castro", "Hoffman", "Hart", "Elliot", "Cunningham", "Knight", "Bradley", "Carroll", "Hudson", "Duncan", "Armstrong", "Berry", "Andrews", "Johnston", "Ray", "Lane", "Riley", "Carpenter", "Perkins", "Aguilar", "Silva", "Richards", "Willis", "Matthews", "Chapman", "Lawrence", "Garza", "Vargas", "Watkins", "Wheeler", "Larson", "Carlson", "Harper", "George", "Greene", "Burke", "Guzman", "Morrison", "Munoz", "Jacobs", "O'Brien", "Lawson", "Franklin", "Lynch", "Bishop", "Carr", "Salazar", "Austin", "Mendez", "Gilbert", "Jensen", "Williamson", "Montgomery", "Harvey", "Oliver", "Howell", "Dean", "Hanson", "Weber", "Garrett", "Sims", "Burton", "Fuller", "Soto", "Welch", "Chen", "Schultz", "Walters", "Reid", "Fields", "Walsh", "Little", "Fowler", "Bowman", "Davidson", "May", "Day", "Schneider", "Newman", "Brewer", "Lucas", "Holland", "Wong", "Banks", "Santos", "Curtis", "Pearson", "Delgado", "Valdez", "Pena", "Rios", "Douglas", "Sandoval", "Barrett", "Hopkins", "Keller", "Guerrero", "Stanley", "Bates", "Alvarado", "Beck", "Ortega", "Wade", "Estrada", "Contreras", "Barnett", "Caldwell", "Santiago", "Lambert", "Powers", "Chambers", "Nunez", "Craig", "Leonard", "Lowe", "Rhodes", "Byrd", "Gregory", "Shelton", "Frazier", "Becker", "Maldonado", "Fleming", "Vega", "Sutton", "Cohen", "Jennings", "Parks", "McDaniel", "Watts", "Barker", "Norris", "Vaughn", "Vazquez", "Holt", "Schwartz", "Steele", "Benson", "Neal", "Dominguez", "Horton", "Terry", "Wolfe", "Hale", "Lyons", "Graves", "Haynes", "Miles", "Park", "Warner", "Padilla", "Bush", "Thornton", "McCarthy", "Mann", "Zimmerman", "Erickson", "Fletcher", "McKinney", "Page", "Dawson", "Joseph", "Marquez", "Reeves", "Klein", "Espinoza", "Baldwin", "Moran", "Love", "Robbins", "Higgins", "Ball", "Cortez", "Le", "Griffith", "Bowen", "Sharp", "Cummings", "Ramsey", "Hardy", "Swanson", "Barber", "Acosta", "Luna", "Chandler", "Blair", "Daniel", "Cross", "Simon", "Dennis", "O'Connor", "Quinn", "Gross", "Navarro", "Moss", "Fitzgerald", "Doyle", "McLaughlin", "Rojas", "Rodgers", "Stevenson", "Singh", "Yang", "Figuroa", "Harmon", "Newton", "Paul", "Manning", "Garner", "McGee", "Reese", "Francis", "Burgess", "Adkins", "Goodman", "Curry", "Brady", "Christensen", "Potter", "Walton", "Molina", "Webster", "Fischer", "Campos", "Avila", "Sherman", "Todd", "Chang", "Blake", "Malone", "Wolf", "Hodges", "Juarez", "Gill", "Farmer", "Hines", "Gallagher", "Duran", "Hubbard", "Cannon", "Miranda", "Wang", "Saunders", "Tate", "Mack", "Hammond", "Carrillo", "Townsend", "Wise", "Ingram", "Barton", "Mejia", "Ayala", "Schroeder", "Hampton", "Rowe", "Parsons", "Frank", "Waters", "Strickland", "Osborne", "Maxwell", "Chan", "Deleon", "Norman", "Harrington", "Casey", "Patton", "Logan", "Bowers", "Mueller", "Glover", "Floyd", "Hartman", "Buchanan", "Cobb", "French", "Kramer", "McCormick", "Clarke", "Tyler", "Gibbs", "Moody", "Conner", "Sparks", "McGuire", "Leon", "Bauer", "Norton", "Pope", "Flynn", "Hogan", "Robles", "Salinas", "Yates", "Lindsey", "Lloyd", "Marsh", "McBride", "Owen", "Solis", "Pham", "Lang", "Pratt", "Lara", "Brock", "Ballard", "Trujillo", "Shaffer", "Drake", "Roman", "Aguirre", "Morton", "Stokes", "Lamb", "Pacheco", "Patrick", "Cochran", "Shepherd", "Cain", "Burnett", "Hess", "Li", "Cervantes", "Olsen", "Briggs", "Ochoa", "Cabrera", "Velasquez", "Montoya", "Roth", "Meyers", "Cardenas", "Fuentes", "Weiss", "Hoover", "Wilkins", "Nicholson", "Underwood", "Short", "Carson", "Morrow", "Colon", "Holloway", "Summers", "Bryan", "Peterson", "McKenzie", "Serrano", "Wilcox", "Carey", "Clayton", "Poole", "Calderon", "Gallegos", "Greer", "Rivas", "Guerra", "Decker", "Collier", "Wall", "Whitaker", "Bass", "Flowers", "Davenport", "Conley", "Houston", "Huff", "Copeland", "Hood", "Monroe", "Massey", "Roberson", "Combs" };
            //            List<string> maleFirstNames = new List<string>() { "John", "Jason", "Tony", "Mohammed", "Ming", "James", "David", "Zachary", "José", "Serge", "Larry", "Jake", "Nathan", "Mitchell", "Luis", "Michael", "César", "Ralph", "Omar", "Nicholas", "Victor", "Pete", "Anthony", "Ted", "Charles", "Hurley", "Scott", "Mac", "Casey", "Chuck", "Leon", "Tino", "Steven", "Stephen", "Nick", "Marius", "D'Marcus", "Oscar", "Vang", "Eric", "Bob", "Robert", "Samuel", "Roberto", "Craig", "Frank", "Pierre", "Albert", "Stuart", "Walt", "Ryan", "Henry", "Gerald", "Enrique", "Brett", "Jeffrey", "Randy", "Blake", "Christopher", "Thomas", "Chad", "Matthew", "Alan", "Dan", "Don", "Timothy", "Terry", "Carlos", "Joey", "Vladimir", "Asbjørn", "Sebastian", "Reidar", "Joachim", "March", "Kjell", "Sigurd", "Terrance", "Hiroto" };
            //            List<string> femaleFirstNames = new List<string>() { "Krista", "Ingrid", "Sophia", "Keisha", "Hanna", "Isabel", "Leia", "Kristina", "Malena", "Crystal", "Polly", "Ana", "Ashley", "Anna", "Yashira", "Jamilla", "Natali", "Sara", "Susan", "Tenisha", "Nakia", "Tiffany", "Sandra", "Julia", "Brianna", "Miriam", "Carol", "Paula", "Carmen", "Kayla", "Anette", "Samantha", "Jessica", "Elizabeth", "Stephanie", "Melanie", "Chrissy", "April", "Liv", "Helene", "Sarah", "Jasmine", "Tonya", "Nikita", "Roberta", "Rose", "Ruth", "Maria", "Tina", "Vivian", "Sally", "Nikolina", "Andrea", "Meredith", "Grace", "Jensina", "Jenny", "Jennifer", "Claire", "Manuela", "Elana", "Freya", "Disa", "Frida", "Pernilla", "Karen", "Kari", "Dagmar", "Camilla", "Kristianna", "Angela", "Elisa", "Hannah", "LaKesha", "Maryan", "Alexandra", "Olava", "Teresa", "Janne", "Gabrielle", "Alexa", "Mercedes", "Tatiana", "Fatima", "Elle", "Indira", "Rose", "Anette", "Chrissy", "Laura", "Janet", "Amelia", "Emily" };

            //            ContactViewModel contact = new ContactViewModel();

            //            Random rName = new Random();
            //            int maleNameIndex = rName.Next(0, maleFirstNames.Count - 1);
            //            int femaleNameIndex = rName.Next(0, femaleFirstNames.Count - 1);

            //            Random rSex = new Random();
            //            int sex = rSex.Next(1, 3);

            //            Random rSurname = new Random();
            //            int surnameIndex = rSurname.Next(0, lastNames.Count - 1);

            //            string queryGender = String.Empty;

            //            if (sex == 1)
            //            {
            //                contact.OtherNames = maleFirstNames[maleNameIndex];
            //                contact.Gender = Properties.Resources.Male;
            //                queryGender = "1";
            //            }
            //            else
            //            {
            //                contact.OtherNames = femaleFirstNames[femaleNameIndex];
            //                contact.Gender = Properties.Resources.Female;
            //                queryGender = "2";
            //            }

            //            contact.Surname = lastNames[surnameIndex];
            //            contact.FinalOutcome = String.Empty;
            //            contact.FirstSaveTime = dtNow;

            //            Query insertQuery = Database.CreateQuery("INSERT INTO [" + DataHelper.ContactForm.TableName + "] (GlobalRecordId, RECSTATUS, FirstSaveLogonName, LastSaveLogonName, FirstSaveTime, LastSaveTime) VALUES (" +
            //                "@GlobalRecordId, @RECSTATUS, @FirstSaveLogonName, @LastSaveLogonName, @FirstSaveTime, @LastSaveTime)");
            //            insertQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, guid.ToString()));
            //            insertQuery.Parameters.Add(new QueryParameter("@RECSTATUS", DbType.Byte, 1));
            //            insertQuery.Parameters.Add(new QueryParameter("@FirstSaveLogonName", DbType.String, userID));
            //            insertQuery.Parameters.Add(new QueryParameter("@LastSaveLogonName", DbType.String, userID));
            //            insertQuery.Parameters.Add(new QueryParameter("@FirstSaveTime", DbType.DateTime2, dtNow));
            //            insertQuery.Parameters.Add(new QueryParameter("@LastSaveTime", DbType.DateTime2, dtNow));
            //            Database.ExecuteNonQuery(insertQuery);

            //            foreach (Epi.Page page in DataHelper.ContactForm.Pages)
            //            {
            //                // contact form has only one page, so we can get away with this code for the time being.
            //                insertQuery = Database.CreateQuery("INSERT INTO [" + page.TableName + "] (GlobalRecordId, ContactSurname, ContactOtherNames, " +
            //                    "ContactGender, ContactAge, ContactAgeUnit, ContactHeadHouse, ContactVillage, ContactDistrict, ContactSC, LC1, " +
            //                    "ContactPhone, ContactHCW, ContactHCWFacility, RiskLevel, FinalOutcome) VALUES (" +
            //                "@GlobalRecordId, @ContactSurname, @ContactOtherNames, @ContactGender, @ContactAge, @ContactAgeUnit, @ContactHeadHouse, @ContactVillage, @ContactDistrict, " +
            //                "@ContactSC, @LC1, @ContactPhone, @ContactHCW, @ContactHCWFacility, @ContactRiskLevel, @ContactFinalOutcome)");
            //                insertQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, guid.ToString()));
            //                insertQuery.Parameters.Add(new QueryParameter("@ContactSurname", DbType.String, contact.Surname));
            //                insertQuery.Parameters.Add(new QueryParameter("@ContactOtherNames", DbType.String, contact.OtherNames));
            //                insertQuery.Parameters.Add(new QueryParameter("@ContactGender", DbType.String, queryGender));
            //                if (contact.AgeYears.HasValue)
            //                {
            //                    insertQuery.Parameters.Add(new QueryParameter("@ContactAge", DbType.String, contact.AgeYears));
            //                }
            //                else
            //                {
            //                    insertQuery.Parameters.Add(new QueryParameter("@ContactAge", DbType.String, DBNull.Value));
            //                }
            //                insertQuery.Parameters.Add(new QueryParameter("@ContactAgeUnit", DbType.String, ""));
            //                insertQuery.Parameters.Add(new QueryParameter("@ContactHeadHouse", DbType.String, contact.HeadOfHousehold));
            //                insertQuery.Parameters.Add(new QueryParameter("@ContactVillage", DbType.String, contact.Village));
            //                insertQuery.Parameters.Add(new QueryParameter("@ContactDistrict", DbType.String, contact.District));
            //                insertQuery.Parameters.Add(new QueryParameter("@ContactSC", DbType.String, contact.SubCounty));
            //                insertQuery.Parameters.Add(new QueryParameter("@LC1", DbType.String, contact.LC1Chairman));
            //                insertQuery.Parameters.Add(new QueryParameter("@ContactPhone", DbType.String, contact.Phone));
            //                insertQuery.Parameters.Add(new QueryParameter("@ContactHCW", DbType.String, ""));
            //                insertQuery.Parameters.Add(new QueryParameter("@ContactHCWFacility", DbType.String, contact.HCWFacility));
            //                insertQuery.Parameters.Add(new QueryParameter("@ContactRiskLevel", DbType.String, contact.RiskLevel));
            //                insertQuery.Parameters.Add(new QueryParameter("@ContactFinalOutcome", DbType.String, contact.FinalOutcome));
            //                Database.ExecuteNonQuery(insertQuery);
            //            }

            //            // use relationship info, save case-contact pair AND contact
            //            CaseContactPairViewModel CaseContactPair = new CaseContactPairViewModel();
            //            CaseContactPair.Relationship = "Friend";
            //            CaseContactPair.ContactRecordId = guid.ToString();
            //            //CaseContactPair.CaseVM = (dgCases.SelectedItem as CaseViewModel);
            //            CaseContactPair.CaseVM = caseVM;
            //            CaseContactPair.ContactType = 7;
            //            CaseContactPair.IsContactDateEstimated = true;
            //            CaseContactPair.DateLastContact = DateTime.Now;
            //            CaseContactPair.ContactVM = contact;
            //            CaseContactPair.ContactVM.Contact.RecordId = guid.ToString();

            //            //CaseContactPair.ContactVM.Contact.FollowUpWindowViewModel = new FollowUpWindowViewModel(CaseContactPair.DateLastContact, contact, (dgCases.SelectedItem as CaseViewModel));
            //            CaseContactPair.ContactVM.Contact.FollowUpWindowViewModel = new FollowUpWindowViewModel(CaseContactPair.DateLastContact, contact, caseVM);
            //            DataHelper.UpdateOrAddContact.Execute(CaseContactPair);
            //        }
            //    }
            //    //});
            //}

            //return;

            // TODO: Re-enable

            if (btnAddContact.ContextMenu != null)
            {
                btnAddContact.ContextMenu.PlacementTarget = btnAddContact;
                btnAddContact.ContextMenu.IsOpen = true;
            }

            e.Handled = true;
            return;
        }

        private void dg_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            e.Cancel = true;
        }

        private void dgAllContacts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgAllContacts.SelectedItems.Count > 0)
            {
                ContactViewModel contactVM = dgAllContacts.SelectedItem as ContactViewModel;
                if (contactVM != null)
                {
                    DataHelper.ShowCasesForContact.Execute(contactVM);
                    //RefreshIndividualChart();
                }
            }
            else
            {
                txtChartHeading.Text = String.Empty;
                //panelChart.Children.Clear();
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.Dispatcher.BeginInvoke(new SimpleEventHandler(ResizeDataGrids));
            this.Dispatcher.BeginInvoke(new SimpleEventHandler(ResizeAnalysis));
            this.Dispatcher.BeginInvoke(new SimpleEventHandler(ResizeHospitalizations));
        }

        private void ResizeContactPanelDataGrids()
        {
            if (panelAllContacts.Visibility == System.Windows.Visibility.Visible && gridContacts.Visibility == System.Windows.Visibility.Visible)
            {
                dgAllContacts.Visibility = System.Windows.Visibility.Collapsed;
                dgExposuresForContact.Visibility = System.Windows.Visibility.Collapsed;

                gridContacts.UpdateLayout();

                double maxHeight = gridContacts.ActualHeight;
                MaxHeight = maxHeight - panelContactManagementSearch.ActualHeight - 260;

                if (MaxHeight <= 0) MaxHeight = 0;

                if (ShowContactManagementCases && ShowContactManagementChart)
                {
                    dgAllContacts.MaxHeight = MaxHeight / 1.5;
                    dgAllContacts.Height = MaxHeight / 1.5;
                    dgExposuresForContact.Height = MaxHeight / 3;
                    dgExposuresForContact.MaxHeight = MaxHeight / 3;

                    panelChart.Visibility = System.Windows.Visibility.Visible;
                    borderChartHeading.Visibility = System.Windows.Visibility.Visible;
                    borderExposuresForContact.Visibility = System.Windows.Visibility.Visible;
                    dgAllContacts.Visibility = System.Windows.Visibility.Visible;
                    dgExposuresForContact.Visibility = System.Windows.Visibility.Visible;
                }
                else if (ShowContactManagementCases && !ShowContactManagementChart)
                {
                    dgAllContacts.MaxHeight = MaxHeight / 1.2;
                    dgAllContacts.Height = MaxHeight / 1.2;
                    dgExposuresForContact.Height = (MaxHeight / 5) + 110;
                    dgExposuresForContact.MaxHeight = (MaxHeight / 5) + 110;

                    panelChart.Visibility = System.Windows.Visibility.Collapsed;
                    borderChartHeading.Visibility = System.Windows.Visibility.Collapsed;
                    borderExposuresForContact.Visibility = System.Windows.Visibility.Visible;
                    dgAllContacts.Visibility = System.Windows.Visibility.Visible;
                    dgExposuresForContact.Visibility = System.Windows.Visibility.Visible;
                }
                else if (!ShowContactManagementCases && ShowContactManagementChart)
                {
                    dgAllContacts.MaxHeight = MaxHeight;
                    dgAllContacts.Height = MaxHeight;
                    dgExposuresForContact.Height = 0;
                    dgExposuresForContact.MaxHeight = 0;

                    panelChart.Visibility = System.Windows.Visibility.Visible;
                    borderChartHeading.Visibility = System.Windows.Visibility.Visible;
                    borderExposuresForContact.Visibility = System.Windows.Visibility.Collapsed;
                    dgAllContacts.Visibility = System.Windows.Visibility.Visible;
                    dgExposuresForContact.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    dgAllContacts.MaxHeight = MaxHeight + 170;
                    dgAllContacts.Height = MaxHeight + 170;
                    dgExposuresForContact.Height = 0;
                    dgExposuresForContact.MaxHeight = 0;

                    panelChart.Visibility = System.Windows.Visibility.Collapsed;
                    borderChartHeading.Visibility = System.Windows.Visibility.Collapsed;
                    borderExposuresForContact.Visibility = System.Windows.Visibility.Collapsed;
                    dgAllContacts.Visibility = System.Windows.Visibility.Visible;
                    dgExposuresForContact.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
        }

        private void ResizeDailyPanelDataGrids()
        {
            if (panelDailyContacts.Visibility == System.Windows.Visibility.Visible && gridContacts.Visibility == System.Windows.Visibility.Visible)
            {
                dgDaily.Visibility = System.Windows.Visibility.Collapsed;

                gridContacts.UpdateLayout();

                double maxHeight = gridContacts.ActualHeight;
                MaxHeight = maxHeight - panelContactManagementSearch.ActualHeight - 140;

                if (MaxHeight <= 0) MaxHeight = 0;

                dgDaily.MaxHeight = MaxHeight;
                dgDaily.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void ResizePrevPanelDataGrids()
        {
            if (panelPrevContacts.Visibility == System.Windows.Visibility.Visible && gridContacts.Visibility == System.Windows.Visibility.Visible)
            {
                dgPrev.Visibility = System.Windows.Visibility.Collapsed;

                gridContacts.UpdateLayout();

                double maxHeight = gridContacts.ActualHeight;
                MaxHeight = maxHeight - panelContactManagementSearch.ActualHeight - 140;

                if (MaxHeight <= 0) MaxHeight = 0;

                dgPrev.MaxHeight = MaxHeight;
                dgPrev.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void ResizeTransmissionPanel()
        {
            transmissionScroller.MaxHeight = this.ActualHeight - 260;
        }

        private void ResizeCasePanelDataGrids()
        {
            if (gridCaseManagement.Visibility == System.Windows.Visibility.Visible)
            {
                dgCases.Visibility = System.Windows.Visibility.Collapsed;
                dgContacts.Visibility = System.Windows.Visibility.Collapsed;
                dgExposures.Visibility = System.Windows.Visibility.Collapsed;

                gridCaseManagement.UpdateLayout();

                double maxHeight = gridCaseManagement.ActualHeight;
                MaxHeight = maxHeight - panelCaseManagementSearch.ActualHeight - 128;

                if (MaxHeight <= 0) MaxHeight = 0;

                if (dgContacts.MaxHeight == 0 && dgExposures.MaxHeight == 0)
                {
                    dgCases.MaxHeight = MaxHeight + 50;
                    dgCases.Height = MaxHeight + 50;
                }
                else if (dgContacts.MaxHeight == 0 && dgExposures.MaxHeight != 0)
                {
                    dgCases.MaxHeight = MaxHeight / 2;
                    dgExposures.MaxHeight = MaxHeight / 2;
                }
                else if (dgContacts.MaxHeight != 0 && dgExposures.MaxHeight == 0)
                {
                    dgCases.MaxHeight = MaxHeight / 2;
                    dgContacts.MaxHeight = MaxHeight / 2;
                }
                else
                {
                    dgCases.MaxHeight = MaxHeight / 3;
                    dgContacts.MaxHeight = MaxHeight / 3;
                    dgExposures.MaxHeight = MaxHeight / 3;
                }

                dgCases.Visibility = System.Windows.Visibility.Visible;
                dgContacts.Visibility = System.Windows.Visibility.Visible;
                dgExposures.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void ResizeDataGrids()
        {
            ResizeCasePanelDataGrids();
            ResizeContactPanelDataGrids();
            ResizeDailyPanelDataGrids();
            ResizePrevPanelDataGrids();
            ResizeTransmissionPanel();
        }

        private void ResizeAnalysis()
        {
            double maxHeight = gridAnalysis.ActualHeight;
            maxHeight = maxHeight - panelCaseManagementSearch.ActualHeight - 30;
            if (maxHeight >= 0)
            {
                gridAnalysis.SvHeight = maxHeight;
            }
        }

        private void ResizeHospitalizations()
        {
            gridHospitalizations.DgHeight = this.ActualHeight - 317;
        }

        private void btnAddExposure_Click(object sender, RoutedEventArgs e)
        {
            if (dgCases.SelectedItems.Count == 1)
            {
                CaseViewModel caseVM = dgCases.SelectedItem as CaseViewModel;

                if (caseVM == null)
                {
                    return;
                }

                if (caseVM.IsLocked)
                {
                    MessageBox.Show("Either this case or contacts of this case are locked for editing. Please wait until other users have released this lock before proceeding.", "Record locked", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                else
                {
                    existingCase.ExposedCaseVM = caseVM;

                    DataHelper.SendMessageForLockCase(caseVM);

                    DataHelper.SearchExistingCasesText = String.Empty;
                    DataHelper.SearchExistingCases.Execute(null);
                    existingCase.Visibility = System.Windows.Visibility.Visible;
                }
            }
        }

        private void existingCase_Click(object sender, RoutedEventArgs e)
        {
            CaseExposurePairViewModel cep = new CaseExposurePairViewModel();

            cep.SourceCaseVM = dgCases.SelectedItem as CaseViewModel;

            if (cep.SourceCaseVM != null)
            {
                if (e.RoutedEvent.Name == "OK")
                {
                    if (!existingCase.DateLastContact.HasValue)
                    {
                        MessageBox.Show(Properties.Resources.ErrorNoDateLastContact, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    cep.ExposedCaseVM = existingCase.CaseVM;
                    cep.Relationship = existingCase.Relationship;
                    cep.DateLastContact = existingCase.DateLastContact.Value;
                    cep.ContactType = existingCase.ContactType;
                    cep.IsTentative = existingCase.IsTentative;
                    cep.IsContactDateEstimated = existingCase.IsEstimated;

                    try
                    {
                        DataHelper.AddExposure.Execute(cep);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(String.Format("An exception occurred while trying to add a source case to a case. Source case ID: {0}, Exposed case ID: {1}. Please give this message to the application developer.\n{2}", cep.SourceCaseVM.ID, cep.ExposedCaseVM.ID, ex.Message));
                    }
                }

                DataHelper.SendMessageForUnlockCase(cep.SourceCaseVM);
                existingCase.Visibility = System.Windows.Visibility.Collapsed;
                DataHelper.SearchExistingCasesText = String.Empty;
                DataHelper.SearchExistingCases.Execute(null);
            }
            else
            {
                throw new ApplicationException("SourceCaseVM cannot be null in existingCase_Click");
            }
        }

        private void btnCollapse_Click(object sender, RoutedEventArgs e)
        {
            if (btnCollapse.IsChecked == false)
            {
                btnCollapse.IsChecked = true;
                e.Handled = true;
                return;
            }
            btnExpandContacts.IsChecked = false;
            btnExpandExposures.IsChecked = false;

            dgCases.MaxHeight = MaxHeight;
            dgCases.Height = MaxHeight;
            dgContacts.MaxHeight = 0;
            dgExposures.MaxHeight = 0;

            borderContacts.Visibility = System.Windows.Visibility.Collapsed;
            borderExposures.Visibility = System.Windows.Visibility.Collapsed;

            ResizeCasePanelDataGrids();
        }

        private void btnExpandExposures_Click(object sender, RoutedEventArgs e)
        {
            if (btnExpandExposures.IsChecked == false)
            {
                btnExpandExposures.IsChecked = true;
                e.Handled = true;
                return;
            }
            btnExpandContacts.IsChecked = false;
            btnCollapse.IsChecked = false;

            dgCases.MaxHeight = MaxHeight / 2;
            dgContacts.MaxHeight = 0;
            dgExposures.MaxHeight = MaxHeight / 2;

            borderExposures.Visibility = System.Windows.Visibility.Visible;
            borderContacts.Visibility = System.Windows.Visibility.Collapsed;

            if (dgCases.SelectedItems.Count > 0)
            {
                CaseViewModel caseVM = (dgCases.SelectedItem) as CaseViewModel;
                if (caseVM != null)
                {
                    DataHelper.ShowSourceCasesForCase.Execute(caseVM);
                }
            }
        }

        private void btnExpandContacts_Click(object sender, RoutedEventArgs e)
        {
            if (btnExpandContacts.IsChecked == false)
            {
                btnExpandContacts.IsChecked = true;
                e.Handled = true;
                return;
            }
            btnExpandExposures.IsChecked = false;
            btnCollapse.IsChecked = false;

            dgCases.MaxHeight = MaxHeight / 2;
            dgContacts.MaxHeight = MaxHeight / 2;
            dgExposures.MaxHeight = 0;

            borderContacts.Visibility = System.Windows.Visibility.Visible;
            borderExposures.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void btnCollapseAll_Click(object sender, RoutedEventArgs e)
        {
            if (btnCollapseAll.IsChecked == false)
            {
                btnCollapseAll.IsChecked = true;
                e.Handled = true;
                return;
            }
            btnExpandCases.IsChecked = false;
            btnExpandChart.IsChecked = false;

            ShowContactManagementCases = false;
            ShowContactManagementChart = false;

            ResizeContactPanelDataGrids();
        }

        private void btnExpandChart_Click(object sender, RoutedEventArgs e)
        {
            if (btnExpandChart.IsChecked == false)
            {
                btnExpandChart.IsChecked = true;
                e.Handled = true;
                return;
            }
            btnExpandCases.IsChecked = false;
            btnCollapseAll.IsChecked = false;

            ShowContactManagementChart = true;
            ShowContactManagementCases = false;

            ResizeContactPanelDataGrids();
        }

        private void btnExpandCases_Click(object sender, RoutedEventArgs e)
        {
            if (btnExpandCases.IsChecked == false)
            {
                btnExpandCases.IsChecked = true;
                e.Handled = true;
                return;
            }
            btnExpandChart.IsChecked = false;
            btnCollapseAll.IsChecked = false;

            ShowContactManagementChart = false;
            ShowContactManagementCases = true;

            ResizeContactPanelDataGrids();
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
                        //if (DataHelper.Country != "Liberia" && DataHelper.Country != "Sierra Leone" && DataHelper.Country != "Guinea")
                        //{
                        //    //EditCase();
                        //    DataHelper.IsShortForm = true;
                        //}
                        if (!DataHelper.IsShortForm) //17040
                        {
                            EditCase();
                        }
                        else
                        {
                            CaseViewModel caseVM = dgCases.SelectedItem as CaseViewModel;
                            DataHelper.IsShortFormOpened = true;//VHF-256
                            if (caseVM != null)
                            {
                                if (caseVM.IsLocked == false)
                                {
                                    if (DataHelper.ToggleShortCaseReportFormCommand.CanExecute(null))
                                    {
                                        this.Cursor = Cursors.Wait;
                                        shortCaseform.DataContext = null;//VHF-278
                                        //shortCaseform.DataContext = caseVM;
                                        DataHelper.ToggleShortCaseReportFormCommand.Execute(caseVM); //VHF-260
                                        caseVM.IsOpenedInSuperUserMode = this.DataHelper.IsSuperUser;
                                        this.Cursor = Cursors.Arrow;
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
                }

            }
        }

        private void dgCases_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            //if (dgCases.SelectedItems.Count == 1)
            //{
            //    IInputElement element = e.MouseDevice.DirectlyOver;
            //    if (element != null && element is FrameworkElement)
            //    {
            //        if (((FrameworkElement)element).Parent is DataGridCell)
            //        {
            //            CaseViewModel caseVM = (dgCases.SelectedItem as CaseViewModel);
            //            QuickEditCase(caseVM);
            //        }
            //    }
            //}
        }

        private void dgAllContacts_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgAllContacts.SelectedItems.Count == 1)
            {
                IInputElement element = e.MouseDevice.DirectlyOver;
                if (element != null && element is FrameworkElement)
                {
                    if (((FrameworkElement)element).Parent is DataGridCell)
                    {
                        ContactViewModel contactVM = dgAllContacts.SelectedItem as ContactViewModel;
                        if (contactVM != null)
                        {
                            EditContact(contactVM);
                        }
                    }
                }
            }
        }

        private void dgContacts_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgContacts.SelectedItems.Count == 1)
            {
                IInputElement element = e.MouseDevice.DirectlyOver;
                if (element != null && element is FrameworkElement)
                {
                    if (((FrameworkElement)element).Parent is DataGridCell && dgContacts.SelectedItem is CaseContactPairViewModel)
                    {
                        ContactViewModel contactVM = (dgContacts.SelectedItem as CaseContactPairViewModel).ContactVM;
                        if (contactVM != null)
                        {
                            EditContact(contactVM);
                        }
                    }
                }
            }
        }
        //VHF-260
        private void dpShortForm_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (borderExposures.Visibility == Visibility.Visible && dgCases.SelectedItems.Count > 0)
            {
                CaseViewModel caseVM = dgCases.SelectedItem as CaseViewModel;
                if (caseVM != null)
                {
                    DataHelper.ShowSourceCasesForCase.Execute(caseVM);
                }
            }
        }

        private void dgExposures_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgExposures.SelectedItems.Count == 1)
            {
                //if (DataHelper.Country != "Liberia" && DataHelper.Country != "Sierra Leone" && DataHelper.Country != "Guinea")
                if (!DataHelper.IsShortForm) //17040
                {
                    IInputElement element = e.MouseDevice.DirectlyOver;
                    if (element != null && element is FrameworkElement)
                    {
                        if (((FrameworkElement)element).Parent is DataGridCell)
                        {
                            EditExposure();
                        }
                    }
                }
                else
                {
                    CaseViewModel caseVM = ((CaseExposurePairViewModel)dgExposures.SelectedItem).ExposedCaseVM; //.SourceCaseVM; //VHF-260
                    DataHelper.IsShortFormOpened = true; //VHF-256
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
        }

        private void btnExportForAnalysis_Click(object sender, RoutedEventArgs e)
        {
            bool full = false;

            if (System.Windows.Forms.Control.ModifierKeys == System.Windows.Forms.Keys.Shift)
            {
                Dialogs.AuthCodeDialog authDialog = new Dialogs.AuthCodeDialog(ContactTracing.Core.Constants.AUTH_CODE);
                System.Windows.Forms.DialogResult authResult = authDialog.ShowDialog();
                if (authResult == System.Windows.Forms.DialogResult.OK)
                {
                    if (authDialog.IsAuthorized)
                    {
                        full = true;
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
            ExportCases(full, false, false);
        }

        private void btnExportForViewing_Click(object sender, RoutedEventArgs e)
        {
            bool full = false;

            if (System.Windows.Forms.Control.ModifierKeys == System.Windows.Forms.Keys.Shift)
            {
                Dialogs.AuthCodeDialog authDialog = new Dialogs.AuthCodeDialog(ContactTracing.Core.Constants.AUTH_CODE);
                System.Windows.Forms.DialogResult authResult = authDialog.ShowDialog();
                if (authResult == System.Windows.Forms.DialogResult.OK)
                {
                    if (authDialog.IsAuthorized)
                    {
                        full = true;
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
            ExportCases(full, true, true);
        }

        private void btnDataTools_Click(object sender, RoutedEventArgs e)
        {
            if (btnDataTools.ContextMenu != null)
            {
                btnDataTools.ContextMenu.PlacementTarget = btnDataTools;
                btnDataTools.ContextMenu.IsOpen = true;
            }

            e.Handled = true;
            return;
        }

        private void btnExportCases_Click(object sender, RoutedEventArgs e)
        {
            if (btnExport.ContextMenu != null)
            {
                btnExport.ContextMenu.PlacementTarget = btnExport;
                btnExport.ContextMenu.IsOpen = true;
            }

            e.Handled = true;
            return;
        }

        private void ExportCases(bool exportFull = true, bool convertCommentLegalValues = false, bool convertFieldPrompts = false)
        {
            System.Windows.Forms.SaveFileDialog dlg = new System.Windows.Forms.SaveFileDialog();
            dlg.DefaultExt = ".csv"; // Default file extension
            dlg.Filter = "Comma separated values file|*.csv"; // Filter files by extension 
            dlg.AutoUpgradeEnabled = true;

            // Show save file dialog box
            System.Windows.Forms.DialogResult result = dlg.ShowDialog();

            // Process save file dialog box results 
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                DataHelper.ExportCasesForAnalysisStart(dlg.FileName, exportFull, convertCommentLegalValues, convertFieldPrompts);
            }

            //try
            //{
            //    System.Diagnostics.Process proc = new System.Diagnostics.Process();
            //    proc.StartInfo.FileName = "\"" + dlg.FileName + "\"";
            //    proc.StartInfo.UseShellExecute = true;

            //    System.Threading.Thread.Sleep(4000);

            //    proc.Start();
            //}
            //catch (FileNotFoundException)
            //{
            //    MessageBox.Show("The file was not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            //}
        }

        private void btnImportCases_Click(object sender, RoutedEventArgs e)
        {
            //if (DataHelper.IsCountryUS)
            //{
            ImportCaseSyncFile();
            //}
            //else
            //{
            //    if (btnImport.ContextMenu != null)
            //    {
            //        btnImport.ContextMenu.PlacementTarget = btnImport;
            //        btnImport.ContextMenu.IsOpen = true;
            //    }

            //    e.Handled = true;
            //    return;
            //}
        }

        //private void btnExportCases2_Click(object sender, RoutedEventArgs e)
        //{
        //    string PackageName = "Export1";
        //    string PackagePath = @"C:\Users\knu1\Desktop";
        //    string password = String.Empty;

        //    try
        //    {
        //        ContactTracing.ImportExport.XmlLabDataPackager xmlDP = new ContactTracing.ImportExport.XmlLabDataPackager(DataHelper.CaseForm, PackageName);
        //        //xmlDP.GridColumnsToNull = gridColumnsToNull;
        //        //xmlDP.FieldsToNull = fieldsToNull;

        //        List<string> fieldsToNull = new List<string>();
        //        //foreach (Field field in DataHelper.CaseForm.Fields)
        //        //{
        //        //    if (field is IDataField && field.Name != "ID")
        //        //    {
        //        //        fieldsToNull.Add(field.Name);
        //        //    }
        //        //}

        //        xmlDP.FieldsToNull.Add(DataHelper.CaseForm.Name, fieldsToNull);

        //        //CallbackSetupProgressBar(100);

        //        Dictionary<string, Epi.ImportExport.Filters.RowFilters> filters = new Dictionary<string, Epi.ImportExport.Filters.RowFilters>();
        //        //filters.Add(CaseForm.Name, new Epi.ImportExport.Filters.RowFilters(Database));
        //        //foreach (IRowFilterCondition rfc in rowFilterConditions)
        //        //{
        //        //    filters[FormName].Add(rfc);
        //        //}
        //        //xmlDP.Filters = filters;
        //        //xmlDP.StatusChanged += new UpdateStatusEventHandler(CallbackSetStatusMessage);
        //        //xmlDP.UpdateProgress += new SetProgressBarDelegate(CallbackSetProgressBar);
        //        //xmlDP.ResetProgress += new SimpleEventHandler(CallbackResetProgressBar);
        //        XmlDocument package = xmlDP.PackageForm();

        //        string fileName = PackageName;

        //        if (true /*AppendTimestamp*/)
        //        {
        //            DateTime dt = DateTime.UtcNow;
        //            string dateDisplayValue = string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0:s}", dt);
        //            dateDisplayValue = dateDisplayValue.Replace(':', '-'); // The : must be replaced otherwise the encryption fails
        //            fileName = PackageName + "_" + dateDisplayValue;
        //        }

        //        Epi.ImportExport.ProjectPackagers.ExportInfo exportInfo = xmlDP.ExportInfo;
        //        foreach (KeyValuePair<View, int> kvp in exportInfo.RecordsPackaged)
        //        {
        //            //CallbackAddStatusMessage("Form " + kvp.Key.Name + ": " + kvp.Value + " records packaged.");
        //        }

        //        // TODO: Remove this before release! This output is for testing purposes only!
        //        package.Save(@PackagePath + "\\" + fileName + ".xml");

        //        //CallbackSetStatusMessage("Compressing package...");
        //        string compressedText = Epi.ImportExport.ImportExportHelper.Zip(package.OuterXml);
        //        compressedText = "[[EPIINFO7_DATAPACKAGE]]" + compressedText;

        //        //CallbackSetStatusMessage("Encrypting package...");
        //        Configuration.EncryptStringToFile(compressedText, @PackagePath + "\\" + fileName + ".pkg7", password);

        //        //e.Result = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        //e.Result = ex;
        //    }
        //}

        private void btnExportAllContacts_Click(object sender, RoutedEventArgs e)
        {
            if (btnExportContacts.ContextMenu != null)
            {
                btnExportContacts.ContextMenu.PlacementTarget = btnExportContacts;
                btnExportContacts.ContextMenu.IsOpen = true;
            }

            e.Handled = true;
            return;
        }

        private void btnExportExposuresForContact_Click(object sender, RoutedEventArgs e)
        {
            if (!(dgExposuresForContact.DataContext is DataView)) { return; }

            System.Windows.Forms.SaveFileDialog dlg = new System.Windows.Forms.SaveFileDialog();
            dlg.AutoUpgradeEnabled = true;
            dlg.DefaultExt = ".csv"; // Default file extension
            dlg.Filter = "Comma separated values file (.csv)|*.csv"; // Filter files by extension 

            // Show save file dialog box
            System.Windows.Forms.DialogResult result = dlg.ShowDialog();

            // Process save file dialog box results 
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                // Save document 
                string fileName = dlg.FileName;

                bool exportResult = DataHelper.ExportCases(dlg.FileName);

                if (exportResult)
                {
                    MessageBox.Show(String.Format(Properties.Resources.ExportSuccessFileWrittenTo, fileName), Properties.Resources.Success, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(Properties.Resources.ExportFailed, Properties.Resources.Fail, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
        }

        private void subTabDailyContacts_MouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            if (subTabDailyContacts.IsChecked == false)
            {
                subTabDailyContacts.IsChecked = true;
                e.Handled = true;
                return;
            }

            subTabPrevContacts.IsChecked = false;
            subTabAllContacts.IsChecked = false;

            panelAllContacts.Visibility = System.Windows.Visibility.Collapsed;
            panelAllContactsButtons.Visibility = System.Windows.Visibility.Collapsed;
            panelDailyContacts.Visibility = System.Windows.Visibility.Visible;
            panelPrevContacts.Visibility = System.Windows.Visibility.Collapsed;

            DataHelper.ShowContactsForDate.Execute(DateTime.Today);

            this.Dispatcher.BeginInvoke(new SimpleEventHandler(ResizeDataGrids));
        }

        private void subTabAllContacts_MouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            if (subTabAllContacts.IsChecked == false)
            {
                subTabAllContacts.IsChecked = true;
                e.Handled = true;
                return;
            }

            ShowAllContactsSubTab();
        }

        private void ShowAllContactsSubTab()
        {
            subTabDailyContacts.IsChecked = false;
            subTabPrevContacts.IsChecked = false;
            subTabAllContacts.IsChecked = true;

            panelAllContacts.Visibility = System.Windows.Visibility.Visible;
            panelAllContactsButtons.Visibility = System.Windows.Visibility.Visible;
            panelDailyContacts.Visibility = System.Windows.Visibility.Collapsed;
            panelPrevContacts.Visibility = System.Windows.Visibility.Collapsed;
            this.Dispatcher.BeginInvoke(new SimpleEventHandler(ResizeDataGrids));
        }

        private void subTabPrevContacts_MouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            if (subTabPrevContacts.IsChecked == false)
            {
                subTabPrevContacts.IsChecked = true;
                e.Handled = true;
                return;
            }

            subTabDailyContacts.IsChecked = false;
            subTabAllContacts.IsChecked = false;

            panelAllContacts.Visibility = System.Windows.Visibility.Collapsed;
            panelAllContactsButtons.Visibility = System.Windows.Visibility.Collapsed;
            panelDailyContacts.Visibility = System.Windows.Visibility.Collapsed;
            panelPrevContacts.Visibility = System.Windows.Visibility.Visible;

            if (dpPrev.SelectedDate.HasValue)
            {
                DataHelper.ShowContactsForDate.Execute(dpPrev.SelectedDate);
            }

            this.Dispatcher.BeginInvoke(new SimpleEventHandler(ResizeDataGrids));
        }

        private void SetTabButtons(TabButton tb)
        {
            // TODO: Move these things to their own tab header custom control
            foreach (UIElement element in grdMenuItems.Children)
            {
                TabButton button = element as TabButton;
                if (button != null && button != tb)
                {
                    button.IsChecked = false;
                }
            }
            if (grdIntro != null && grdIntro.Visibility == System.Windows.Visibility.Collapsed)
            {
                this.Dispatcher.BeginInvoke(new SimpleEventHandler(ResizeDataGrids));
            }
        }

        private void btnTab_Checked(object sender, RoutedEventArgs e)
        {
            // TODO: Move these things to their own tab header custom control
            TabButton tb = sender as TabButton;
            if (tb != null)
            {
                string content = tb.Content == null ? String.Empty : tb.Content.ToString();

                SetTabButtons(tb);

                if (tb.Content != null && content.Equals("Isolated Patients", StringComparison.OrdinalIgnoreCase))
                {
                    epiDataHelper.SetDefaultIsolationViewFilter();
                }

                //if (tb.Content != null && (content.Equals("Contacts", StringComparison.OrdinalIgnoreCase) || content.Equals("Case Management", StringComparison.OrdinalIgnoreCase) ||
                //    content.Equals("Sujets contacts", StringComparison.OrdinalIgnoreCase) || content.Equals("Cas", StringComparison.OrdinalIgnoreCase)))
                //{
                //    epiDataHelper.RepopulateCollections(true); // VHF-211 // VHF-100
                //}

                if (gridAnalysis != null && tb.AssociatedPage == gridAnalysis)
                {
                    this.Dispatcher.BeginInvoke(new SimpleEventHandler(ResizeAnalysis));
                    gridAnalysis.Compute();
                }
                if (gridAnalysis != null && tb.AssociatedPage == gridHospitalizations)
                {
                    this.Dispatcher.BeginInvoke(new SimpleEventHandler(ResizeHospitalizations));
                }
                else if (gridTransmission != null && tb.AssociatedPage == gridTransmission)
                {
                    this.Dispatcher.BeginInvoke(new SimpleEventHandler(ResizeTransmissionPanel));
                    if (TransmissionChain == null)
                    {
                        try
                        {
                            TransmissionChain = new TransmissionChain(((EpiDataHelper)DataContext).CaseForm, ((EpiDataHelper)DataContext).Project.CollectedData.GetDbDriver(), TransmissionCanvas, TransmissionDates, this);
                            TransmissionChain.Initialize();
                        }
                        catch (Exception)
                        {
                        }
                    }
                    else
                    {
                        TransmissionChain.Initialize();
                    }
                }
            }
        }

        private void btnTab_Unchecked(object sender, RoutedEventArgs e)
        {
            // TODO: Move these things to their own tab header custom control
            int checkedItems = 0;
            foreach (UIElement element in grdMenuItems.Children)
            {
                TabButton tb = element as TabButton;
                if (tb != null)
                {
                    if (tb.IsChecked == true) checkedItems++;
                }
            }

            if (checkedItems == 0)
            {
                // TabButton is derived from ToggleButton, and in this case, we don't want the user to be able to 'uncheck'
                // a tab item... so handle that here
                TabButton tb = sender as TabButton;
                if (tb != null)
                {
                    tb.IsChecked = true;
                    e.Handled = true;
                }
            }
        }

        private void txtFollowUpNotes_LostFocus(object sender, RoutedEventArgs e)
        {
            if (dgDaily.SelectedItem != null)
            {
                DailyCheckViewModel dcVM = dgDaily.SelectedItem as DailyCheckViewModel;
                if (dcVM != null)
                {
                    TextBox tbox = sender as TextBox;
                    if (tbox != null)
                    {
                        dcVM.Notes = tbox.Text;
                        if (dcVM.Notes == null) dcVM.Notes = String.Empty;
                        KeyValuePair<DateTime, DailyCheckViewModel> kvpDC = new KeyValuePair<DateTime, DailyCheckViewModel>(DateTime.Today, dcVM);
                        DataHelper.UpdateDaily.Execute(kvpDC);
                    }
                }
            }
        }

        private void txtTemp1_LostFocus(object sender, RoutedEventArgs e)
        {
            if (dgDaily.SelectedItem != null)
            {
                DailyCheckViewModel dcVM = dgDaily.SelectedItem as DailyCheckViewModel;
                if (dcVM != null)
                {
                    TextBox tbox = sender as TextBox;
                    if (tbox != null)
                    {
                        try
                        {
                            double temperature = Convert.ToDouble(tbox.Text);
                            if (temperature >= 10.0 && temperature <= 999.0)
                            {
                                dcVM.Temp1 = temperature;
                            }
                            else
                            {
                                ShowTemperateOutOfRangeDialog(temperature);
                                dcVM.Temp1 = null;
                            }
                        }
                        catch
                        {
                            try
                            {
                                double temperature = Convert.ToDouble(tbox.Text, new CultureInfo("en-US"));
                                if (temperature >= 10.0 && temperature <= 999.0)
                                {
                                    dcVM.Temp1 = temperature;
                                }
                                else
                                {
                                    ShowTemperateOutOfRangeDialog(temperature);
                                    dcVM.Temp1 = null;
                                }
                            }
                            catch
                            {
                                dcVM.Temp1 = null;
                            }
                        }
                        KeyValuePair<DateTime, DailyCheckViewModel> kvpDC = new KeyValuePair<DateTime, DailyCheckViewModel>(DateTime.Today, dcVM);
                        DataHelper.UpdateDaily.Execute(kvpDC);
                    }
                }
            }
        }

        private void ShowTemperateOutOfRangeDialog(double value)
        {
            MessageBox.Show("The temperature " + value.ToString() + " was invalid. Please enter a temperature between 10.0 and 999.0 and try again.", "Invalid input", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void txtTemp2_LostFocus(object sender, RoutedEventArgs e)
        {
            if (dgDaily.SelectedItem != null)
            {
                DailyCheckViewModel dcVM = dgDaily.SelectedItem as DailyCheckViewModel;
                if (dcVM != null)
                {
                    TextBox tbox = sender as TextBox;
                    if (tbox != null)
                    {
                        try
                        {
                            double temperature = Convert.ToDouble(tbox.Text);
                            if (temperature >= 10.0 && temperature <= 999.0)
                            {
                                dcVM.Temp2 = temperature;
                            }
                            else
                            {
                                ShowTemperateOutOfRangeDialog(temperature);
                                dcVM.Temp2 = null;
                            }
                        }
                        catch
                        {
                            try
                            {
                                double temperature = Convert.ToDouble(tbox.Text, new CultureInfo("en-US"));
                                if (temperature >= 10.0 && temperature <= 999.0)
                                {
                                    dcVM.Temp2 = temperature;
                                }
                                else
                                {
                                    ShowTemperateOutOfRangeDialog(temperature);
                                    dcVM.Temp2 = null;
                                }
                            }
                            catch
                            {
                                dcVM.Temp2 = null;
                            }
                        }
                        KeyValuePair<DateTime, DailyCheckViewModel> kvpDC = new KeyValuePair<DateTime, DailyCheckViewModel>(DateTime.Today, dcVM);
                        DataHelper.UpdateDaily.Execute(kvpDC);
                    }
                }
            }
        }

        private void txtPrevFollowUpNotes_LostFocus(object sender, RoutedEventArgs e)
        {
            if (dpPrev.SelectedDate.HasValue && dgPrev.SelectedItem != null)
            {
                DailyCheckViewModel dcVM = dgPrev.SelectedItem as DailyCheckViewModel;
                if (dcVM != null)
                {
                    TextBox tbox = sender as TextBox;
                    if (tbox != null)
                    {
                        dcVM.Notes = tbox.Text;
                        if (dcVM.Notes == null) dcVM.Notes = String.Empty;
                        KeyValuePair<DateTime, DailyCheckViewModel> kvpDC = new KeyValuePair<DateTime, DailyCheckViewModel>(dpPrev.SelectedDate.Value, dcVM);
                        DataHelper.UpdateDaily.Execute(kvpDC);
                    }
                }
            }
        }

        private void txtPrevTemp1_LostFocus(object sender, RoutedEventArgs e)
        {
            if (dpPrev.SelectedDate.HasValue && dgPrev.SelectedItem != null)
            {
                DailyCheckViewModel dcVM = dgPrev.SelectedItem as DailyCheckViewModel;
                if (dcVM != null)
                {
                    TextBox tbox = sender as TextBox;
                    if (tbox != null)
                    {
                        try
                        {
                            double temperature = Convert.ToDouble(tbox.Text);
                            if (temperature >= 10.0 && temperature <= 999.0)
                            {
                                dcVM.Temp1 = temperature;
                            }
                            else
                            {
                                ShowTemperateOutOfRangeDialog(temperature);
                                dcVM.Temp1 = null;
                            }
                        }
                        catch
                        {
                            try
                            {
                                double temperature = Convert.ToDouble(tbox.Text, new CultureInfo("en-US"));
                                if (temperature >= 10.0 && temperature <= 999.0)
                                {
                                    dcVM.Temp1 = temperature;
                                }
                                else
                                {
                                    ShowTemperateOutOfRangeDialog(temperature);
                                    dcVM.Temp1 = null;
                                }
                            }
                            catch
                            {
                                dcVM.Temp1 = null;
                            }
                        }
                        KeyValuePair<DateTime, DailyCheckViewModel> kvpDC = new KeyValuePair<DateTime, DailyCheckViewModel>(dpPrev.SelectedDate.Value, dcVM);
                        DataHelper.UpdateDaily.Execute(kvpDC);
                    }
                }
            }
        }

        private void txtPrevTemp2_LostFocus(object sender, RoutedEventArgs e)
        {
            if (dpPrev.SelectedDate.HasValue && dgPrev.SelectedItem != null)
            {
                DailyCheckViewModel dcVM = dgPrev.SelectedItem as DailyCheckViewModel;
                if (dcVM != null)
                {
                    TextBox tbox = sender as TextBox;
                    if (tbox != null)
                    {
                        try
                        {
                            double temperature = Convert.ToDouble(tbox.Text);
                            if (temperature >= 10.0 && temperature <= 999.0)
                            {
                                dcVM.Temp2 = temperature;
                            }
                            else
                            {
                                ShowTemperateOutOfRangeDialog(temperature);
                                dcVM.Temp2 = null;
                            }
                        }
                        catch
                        {
                            try
                            {
                                double temperature = Convert.ToDouble(tbox.Text, new CultureInfo("en-US"));
                                if (temperature >= 10.0 && temperature <= 999.0)
                                {
                                    dcVM.Temp2 = temperature;
                                }
                                else
                                {
                                    ShowTemperateOutOfRangeDialog(temperature);
                                    dcVM.Temp2 = null;
                                }
                            }
                            catch
                            {
                                dcVM.Temp2 = null;
                            }
                        }
                        KeyValuePair<DateTime, DailyCheckViewModel> kvpDC = new KeyValuePair<DateTime, DailyCheckViewModel>(dpPrev.SelectedDate.Value, dcVM);
                        DataHelper.UpdateDaily.Execute(kvpDC);
                    }
                }
            }
        }

        private void convertToContact_Click(object sender, RoutedEventArgs e)
        {
            CaseExposurePairViewModel cepVM = new CaseExposurePairViewModel();
            cepVM.SourceCaseVM = convertToContact.CaseVM;
            cepVM.ExposedCaseVM = dgCases.SelectedItem as CaseViewModel;

            if (cepVM.ExposedCaseVM != null)
            {
                if (e.RoutedEvent.Name == "OK")
                {
                    CaseViewModel selectedCaseVM = dgCases.SelectedItem as CaseViewModel;
                    if (selectedCaseVM != null)
                    {
                        if (convertToContact.CaseVM.RecordId == selectedCaseVM.RecordId)
                        {
                            DataHelper.SendMessageForUnlockCase(cepVM.ExposedCaseVM);
                            MessageBox.Show(Properties.Resources.CannotConvertSameIdentity, Properties.Resources.Fail, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            e.Handled = true;
                            return;
                        }

                        if (!convertToContact.DateLastContact.HasValue)
                        {
                            DataHelper.SendMessageForUnlockCase(cepVM.ExposedCaseVM);
                            MessageBox.Show(Properties.Resources.ErrorNoDateLastContactGeneric, Properties.Resources.Fail, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            e.Handled = true;
                            return;
                        }


                        cepVM.Relationship = convertToContact.Relationship;
                        cepVM.DateLastContact = convertToContact.DateLastContact.Value;
                        cepVM.IsContactDateEstimated = convertToContact.IsEstimated;
                        cepVM.ContactType = convertToContact.ContactType;
                        try
                        {
                            if (cepVM.SourceCaseVM.IsLocked || cepVM.ExposedCaseVM.IsLocked)
                            {
                                DataHelper.SendMessageForUnlockCase(cepVM.ExposedCaseVM);
                                MessageBox.Show("Either this case or source cases of this contact are locked for editing. Please wait until other users have released this lock before proceeding.", "Record locked", MessageBoxButton.OK, MessageBoxImage.Information);
                                return;
                            }

                            DataHelper.ConvertCaseToContactWithNewSource.Execute(cepVM);
                        }
                        catch (Exception ex)
                        {
                            DataHelper.SendMessageForUnlockCase(cepVM.ExposedCaseVM);
                            MessageBox.Show(String.Format("An exception occurred while trying to convert a case to a contact with a new source. Source case ID: {0}, Exposed case ID: {1}. Please give this message to the application developer.\n{2}", cepVM.SourceCaseVM.ID, cepVM.ExposedCaseVM.ID, ex.Message));
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("selectedCaseVM cannot be null in convertToContact_Click");
                    }
                }

                DataHelper.SendMessageForUnlockCase(cepVM.ExposedCaseVM);

                convertToContact.Visibility = System.Windows.Visibility.Collapsed;
                DataHelper.SearchExistingCasesText = String.Empty;
                DataHelper.SearchExistingCases.Execute(null);
            }
            else
            {
                throw new InvalidOperationException("cepVM.ExposedCaseVM cannot be null in convertToContact_Click");
            }
        }

        private void dpPrev_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            DatePicker dp = (sender as DatePicker);
            if (dp != null && dp.SelectedDate != null)
            {
                tblockPrevDate.Text = dp.SelectedDate.Value.ToShortDateString();
                DataHelper.ShowContactsForDate.Execute(dp.SelectedDate);
            }
        }

        private void dgDaily_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgDaily.SelectedItems.Count == 1)
            {
                IInputElement element = e.MouseDevice.DirectlyOver;
                if (element != null && element is FrameworkElement)
                {
                    if (((FrameworkElement)element).Parent is DataGridCell)
                    {
                        DailyCheckViewModel dcVM = dgDaily.SelectedItem as DailyCheckViewModel;
                        if (dcVM != null)
                        {
                            EditContact(dcVM.ContactVM);
                        }
                    }
                }
            }
        }

        private void dgPrev_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgPrev.SelectedItems.Count == 1)
            {
                IInputElement element = e.MouseDevice.DirectlyOver;
                if (element != null && element is FrameworkElement)
                {
                    if (((FrameworkElement)element).Parent is DataGridCell)
                    {
                        DailyCheckViewModel dcVM = dgPrev.SelectedItem as DailyCheckViewModel;
                        if (dcVM != null)
                        {
                            EditContact(dcVM.ContactVM);
                        }
                    }
                }
            }
        }

        private void importFromLab_Closed(object sender, RoutedEventArgs e)
        {
            if (e.RoutedEvent.Name == "Close")
            {
            }

            importFromLab.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void transmission_saveClick(object sender, MouseButtonEventArgs e)
        {
            TransmissionChain.SaveAsImage();
        }

        private void transmission_printClick(object sender, MouseButtonEventArgs e)
        {
            TransmissionChain.Print();
        }

        private void PopulateCollections()
        {
            DataHelper.RepopulateCollections(true); //VHF-253
            //with default 'false' the values of drop downs were empty when settings were 
            //changed from longform to shortform. 

            if (DataHelper.VirusTestType == Core.Enums.VirusTestTypes.Rift)
            {
                tabButtonContacts.Visibility = System.Windows.Visibility.Collapsed;
                tabButtonTransmissionChain.Visibility = System.Windows.Visibility.Collapsed;
            }

            dpPrev.DisplayDateEnd = DateTime.Now.AddDays(1);

            try
            {
                TransmissionChain = new TransmissionChain(((EpiDataHelper)DataContext).CaseForm, ((EpiDataHelper)DataContext).Project.CollectedData.GetDbDriver(), TransmissionCanvas, TransmissionDates, this);
            }
            catch (Exception)
            {
            }
        }

        private void EpiDataHelper_InitialSetupRun(object sender, ViewModel.Events.InitialSetupArgs e)
        {
            if (e.ShowSetupScreen)
            {
                initialSetup.IncludeNewProjectDetails = false;
                initialSetup.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                PopulateCollections();
            }
        }

        private void initialSetup_Closed(object sender, RoutedEventArgs e)
        {
            initialSetup.Visibility = System.Windows.Visibility.Collapsed;

            if (e.RoutedEvent.Name.ToLower() == "ok")
            {
                DataHelper.FillInOutbreakData(initialSetup.OutbreakName, initialSetup.IDPrefix, initialSetup.IDSeparator,
                    initialSetup.OutbreakDate, initialSetup.IDPattern, initialSetup.Virus, initialSetup.Country, initialSetup.IsShortForm, ContactType); //17040

                PopulateCollections();
            }
        }

        //private void btnEditCaseContactLink_Click(object sender, RoutedEventArgs e)
        //{
        //    if (dgAllContacts.SelectedItems.Count == 1 && dgExposuresForContact.SelectedItems.Count == 1)
        //    {
        //        CaseContactPairViewModel ccpVM = dgExposuresForContact.SelectedItem as CaseContactPairViewModel;
        //        Dialogs.NewContactDialog cDialog = new Dialogs.NewContactDialog(System.Threading.Thread.CurrentThread.CurrentCulture);
        //        cDialog.Relationship = ccpVM.Relationship;
        //        cDialog.ContactType = ccpVM.ContactType;
        //        cDialog.ContactDate = ccpVM.DateLastContact;
        //        cDialog.IsEstimated = ccpVM.IsContactDateEstimated;

        //        System.Windows.Forms.DialogResult result = cDialog.ShowDialog();

        //        if (result == System.Windows.Forms.DialogResult.OK)
        //        {
        //            if (ccpVM.CaseVM.IsLocked || ccpVM.ContactVM.IsLocked)
        //            {
        //                MessageBox.Show("One or more of the selected records are locked for editing. Please wait until other users have released this lock before proceeding.", "Record locked", MessageBoxButton.OK, MessageBoxImage.Information);
        //                return;
        //            }

        //            ccpVM.Relationship = cDialog.Relationship;
        //            ccpVM.ContactType = cDialog.ContactType;
        //            ccpVM.IsContactDateEstimated = cDialog.IsEstimated;
        //            ccpVM.DateLastContact = cDialog.ContactDate;
        //            try
        //            {
        //                DataHelper.UpdateCaseContactLink.Execute(ccpVM);
        //                DataHelper.ShowCasesForContact.Execute(ccpVM.ContactVM);
        //            }
        //            catch (Exception ex)
        //            {
        //                MessageBox.Show(String.Format("An exception occurred while trying to save an edited case-contact relationship. Case ID: {0}, Contact ID: {1}. Please give this message to the application developer.\n{2}", ccpVM.CaseVM.ID, ccpVM.ContactVM.ContactID, ex.Message));
        //            }
        //        }
        //    }
        //}

        private void contactFormPrint_Click(object sender, RoutedEventArgs e)
        {
            System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
            string fileName = "\\Projects\\VHF\\Resources\\ContactForm." + System.Threading.Thread.CurrentThread.CurrentUICulture.ToString() + ".pdf";

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
                System.Windows.MessageBox.Show("The contact listing form file could not be found. Please contact the application developer.");
            }
        }

        private void caseFormPrint_Click(object sender, RoutedEventArgs e)
        {
            System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
            string fileName = "\\Projects\\VHF\\Resources\\CRF." + System.Threading.Thread.CurrentThread.CurrentUICulture.ToString() + ".pdf";

            string fullFileName = System.IO.Path.GetDirectoryName(a.Location) + fileName;
            if (System.IO.File.Exists(fullFileName))
            {
                string commandText = fullFileName;// System.IO.Path.GetDirectoryName(a.Location) + fileName; //"\\Projects\\VHF\\CaseReportForm.doc";

                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = commandText;
                proc.StartInfo.UseShellExecute = true;
                proc.Start();
            }
            else
            {
                System.Windows.MessageBox.Show("The case report form file could not be found. Please contact the application developer.");
            }
        }

        private void dgExposuresForContact_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgExposuresForContact.SelectedItems.Count == 1)
            {
                //if (DataHelper.Country != "Liberia" && DataHelper.Country != "Sierra Leone" && DataHelper.Country != "Guinea")
                if (!DataHelper.IsShortForm) //17040
                {
                    IInputElement element = e.MouseDevice.DirectlyOver;
                    if (element != null && element is FrameworkElement)
                    {
                        if (((FrameworkElement)element).Parent is DataGridCell)
                        {
                            CaseViewModel caseVM = ((CaseContactPairViewModel)dgExposuresForContact.SelectedItem).CaseVM;

                            DataHelper.SendMessageForLockCase(caseVM);

                            Epi.Enter.EnterUIConfig uiConfig = Core.Common.GetCaseConfig(DataHelper.CaseForm, DataHelper.LabForm); //GetCaseConfig();
                            Epi.Windows.Enter.EnterMainForm emf = new Epi.Windows.Enter.EnterMainForm(DataHelper.Project, DataHelper.CaseForm, uiConfig);

                            int uniqueKey = caseVM.UniqueKey;

                            emf.LoadRecord(uniqueKey);
                            emf.RecordSaved += new SaveRecordEventHandler(emfCases_RecordSaved);
                            emf.ShowDialog();
                            emf.RecordSaved -= new SaveRecordEventHandler(emfCases_RecordSaved);

                            DataHelper.SendMessageForUnlockCase(caseVM);
                        }
                    }
                }
                else
                {
                    CaseViewModel caseVM = ((CaseContactPairViewModel)dgExposuresForContact.SelectedItem).CaseVM;
                    if (caseVM != null)
                    {
                        if (caseVM.IsLocked == false)
                        {
                            if (DataHelper.ToggleShortCaseReportFormCommand.CanExecute(null))
                            {
                                DataHelper.IsShortFormOpened = true; //VHF-256
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
        }

        private void EpiDataHelper_CaseDeleted(object sender, ViewModel.Events.CaseDeletedArgs e)
        {
            if (e.ContactVM != null && !String.IsNullOrEmpty(e.ContactVM.FinalOutcome))
            {
                //MessageBoxResult result = MessageBox.Show("This case also exists as an inactivated contact. Do you wish to clear the contact's final outcome to re-activate it?", "Re-activate contact", MessageBoxButton.YesNo, MessageBoxImage.Question);
                //if (result == MessageBoxResult.Yes)
                //{
                e.ContactVM.FinalOutcome = String.Empty;
                DataHelper.ReActivateContact(e.ContactVM);
                e.ContactVM.IsActive = true;
                e.ContactVM.IsCase = false;
                //}
            }
        }

        private void EpiDataHelper_DuplicateIdDetected(object sender, ViewModel.Events.DuplicateIdDetectedArgs e)
        {
            this.Dispatcher.BeginInvoke(new DuplicateIdDetectedHandler(DuplicateIDsDetected), sender, e);
        }

        private void DuplicateIDsDetected(object sender, ViewModel.Events.DuplicateIdDetectedArgs e)
        {
            WordBuilder wb = new WordBuilder(",");

            foreach (CaseViewModel caseVM in e.DuplicateCases)
            {
                wb.Add(caseVM.ID);
            }

            string warning = "Warning: The following cases have duplicate ID values: " + wb.ToString();

            MessageBox.Show(warning, Properties.Resources.Warning, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (DataHelper.IsShowingDatabaseUpgrade)
            {
                MessageBox.Show("A database upgrade is in progress. The application should not be closed until the upgrade has been completed.", "Warning", MessageBoxButton.OK, MessageBoxImage.Information);
                e.Cancel = true;
                return;
            }

            ContactTracing.CaseView.Properties.Settings.Default.Save();
            DataHelper.CloseProject();
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            initialSetup.SetDefaults(DataHelper);
            initialSetup.IncludeNewProjectDetails = false;
            initialSetup.Visibility = System.Windows.Visibility.Visible;
        }

        private void statusControl_StatusChanged(object sender, ViewModel.Events.DailyCheckStatusChangedEventArgs e)
        {
            if (panelDailyContacts.Visibility != Visibility.Visible && panelPrevContacts.Visibility != Visibility.Visible) return;

            DailyCheckViewModel dcVM = sender as DailyCheckViewModel;

            if (dcVM != null)
            {
                KeyValuePair<DateTime, DailyCheckViewModel> kvpDC = new KeyValuePair<DateTime, DailyCheckViewModel>();

                if (panelDailyContacts.Visibility == System.Windows.Visibility.Visible)
                {
                    kvpDC = new KeyValuePair<DateTime, DailyCheckViewModel>(DateTime.Today, dcVM);
                }
                else
                {
                    if (!dpPrev.SelectedDate.HasValue)
                    {
                        dpPrev.SelectedDate = DateTime.Today;
                    }
                    kvpDC = new KeyValuePair<DateTime, DailyCheckViewModel>(dpPrev.SelectedDate.Value, dcVM);
                }

                DataHelper.UpdateDaily.Execute(kvpDC);
                DataHelper.SendMessageForUpdateDaily(kvpDC.Value.ContactVM.RecordId, false);

                if (dcVM.Status.HasValue && dcVM.Status.Value == Core.ContactDailyStatus.SeenSickAndIsolated)
                {
                    if (DataHelper.DoesCaseExist(dcVM.ContactVM.RecordId))
                    {
                        // SHOULD NEVER ARRIVE HERE
                        MessageBox.Show(Properties.Resources.ErrorAlreadyExistsAsCase, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                        //dcVM.IsSeenUnknown = true;
                        dcVM.Status = null;
                        DataHelper.UpdateDaily.Execute(kvpDC);
                        DataHelper.SendMessageForUpdateDaily(kvpDC.Value.ContactVM.RecordId, true);
                        return;
                    }

                    System.Windows.Forms.DialogResult result = Epi.Windows.MsgBox.ShowQuestion(Properties.Resources.QuestionConvertContactToCase);
                    if (result.Equals(System.Windows.Forms.DialogResult.Yes))
                    {
                        ContactConversionInfo info = new ContactConversionInfo(dcVM.ContactVM, ContactFinalOutcome.Isolated, kvpDC.Key);
                        try
                        {
                            DataHelper.ConvertContactToCase.Execute(info);
                            MessageBox.Show(Properties.Resources.MsgContactToCaseConversionComplete, Properties.Resources.ConversionComplete, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, Properties.Resources.ApplicationExceptionOccurred, MessageBoxButton.OK, MessageBoxImage.Error);
                            Logger.Log(DateTime.Now + ":  " +
                System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString() + ": " +
                "Exception thrown in statusControl_StatusChanged() method. Message: " + ex.Message);

                            dcVM.Status = null;
                            try
                            {
                                DataHelper.UpdateDaily.Execute(kvpDC);
                                DataHelper.SendMessageForUpdateDaily(kvpDC.Value.ContactVM.RecordId, true);
                            }
                            catch (Exception udEx)
                            {
                                MessageBox.Show(udEx.Message, Properties.Resources.ApplicationExceptionOccurred, MessageBoxButton.OK, MessageBoxImage.Error);
                                Logger.Log(DateTime.Now + ":  " +
                    System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString() + ": " +
                    "Exception thrown in UpdateDailyExecute() method. Message: " + udEx.Message);
                            }
                        }
                    }
                    else
                    {
                        //dcVM.IsSeenAndSickNotIsolated = true;
                        StatusSelector selector = e.Source as StatusSelector;
                        if (selector != null)
                        {
                            selector.ResetIso();
                        }
                        dcVM.Status = Core.ContactDailyStatus.SeenSickAndIsoNotFilledOut;
                        DataHelper.UpdateDaily.Execute(kvpDC);

                        MessageBox.Show("Please select an isolation status for this contact.", "Select", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void EpiDataHelper_CaseSwitchToLegacyEnter(object sender, EventArgs e)
        {
            CaseViewModel c = sender as CaseViewModel;
            if (c != null)
            {
                if (c.ByPassEpiCaseClassificationValidation) //AddCase if No Data is entered on shortForm Issue# 17054
                {
                    AddCase();
                }
                else
                {
                    EditCase(c);
                }

            }
        }

        private void EpiDataHelper_CaseDataPopulated(object sender, ViewModel.Events.CaseDataPopulatedArgs e)
        {
            this.Dispatcher.BeginInvoke(new SimpleEventHandler(CaseDataPopulatedHandler));
        }

        private void CaseDataPopulatedHandler()
        {
            ResizeDataGrids();

            if (DataHelper == null || DataHelper.Project == null || DataHelper.Project.CollectedData == null)
            {
                grdIntro.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                grdIntro.Visibility = System.Windows.Visibility.Collapsed;

                if (tabButtonAnalysis.IsChecked == true)
                {
                    gridAnalysis.Compute();
                }
                else if (tabButtonContacts.IsChecked == true)
                {
                    ShowAllContactsSubTab();
                }
            }
            this.Cursor = Cursors.Arrow;
        }

        private void EpiDataHelper_IssueDataPopulated(object sender, EventArgs e)
        {
            //this.Cursor = Cursors.Arrow;
        }

        private void EpiDataHelper_FollowUpVisitUpdated(object sender, EventArgs e)
        {
            //this.Dispatcher.BeginInvoke(new SimpleEventHandler(RefreshIndividualChart));
        }

        private void EpiDataHelper_RefreshRequired(object sender, EventArgs e)
        {
            this.Dispatcher.BeginInvoke(new SimpleEventHandler(RefreshDatabase));
        }

        private void EpiDataHelper_SyncProblemsDetected(object sender, EventArgs e)
        {
            if (sender is Exception)
            {
                Exception ex = sender as Exception;
                if (ex != null)
                {
                    this.Dispatcher.BeginInvoke(new ExceptionMessagingHandler(DisplaySyncProblems), ex);
                }
            }
            else
            {
                this.Dispatcher.BeginInvoke(new SimpleEventHandler(DisplaySyncProblems));
            }
        }

        private void DisplaySyncProblems()
        {
            MessageBox.Show("Problems were detected during this data sync operation.");
        }

        private void DisplaySyncProblems(Exception ex)
        {
            MessageBox.Show("Problems were detected during this data sync operation. Exception message: " + ex.Message);
        }

        private void RefreshDatabase()
        {
            DataHelper.RepopulateCollections(false);
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //  newOutbreak.Country = (string)cmboRegion.SelectedValue;
            if (shouldEnableNewOutbreak) newOutbreak.Visibility = System.Windows.Visibility.Visible;
        }

        private void fileScreen_ProjectOpened(object sender, Core.Events.ProjectOpenedArgs e)
        {
            if (e.ProjectInfo.IsConnected == false)
            {
                MessageBox.Show("No connection to the database was detected. Please check that you are connected to the proper network and that all networking hardware is operational.", "Disconnected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!String.IsNullOrEmpty(e.ProjectInfo.Culture))
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = CaseView.Properties.Resources.Culture;
                System.Threading.Thread.CurrentThread.CurrentUICulture = CaseView.Properties.Resources.Culture;


                bool actualTest = e.ProjectInfo.Culture.Equals(CaseView.Properties.Resources.Culture.ToString(), StringComparison.OrdinalIgnoreCase);

                bool tempFRTest = e.ProjectInfo.Culture.Contains("fr") && CaseView.Properties.Resources.Culture.ToString().Contains("fr");

                bool passed = false;

                if (actualTest)
                {
                    passed = true;
                }
                else
                {
                    if (tempFRTest)
                    {
                        passed = true;
                    }
                }


                //  if (e.ProjectInfo.Culture != CaseView.Properties.Resources.Culture.ToString())
                if (passed == false)
                {


                    /**
                     * Error:  The project you are trying to open was created for the following settings:
                     * <linebreak>  
                     * Region: [Value]; Language: [Value]. 
                     * <linebreak>                   
                     * However, the application is currently set to these settings:
                     * <linebreak>  
                     * Region: [Value]; Language: [Value]. 
                     * <linebreak>  
                     * Please choose the correct region and language settings before opening the project.
                     **/

                    MessageBox.Show(string.Format("{0}\n\n" + ApplicationViewModel.Instance.CultureLanguageDictionary[e.ProjectInfo.Culture.ToLower()] + "." +
                              "\n\n{1}\n\n" + ApplicationViewModel.Instance.CultureLanguageDictionary[System.Threading.Thread.CurrentThread.CurrentUICulture.ToString().ToLower()] + "." +
                              "\n\n{2} ",
                              Properties.Resources.WrongCultureError1,
                                Properties.Resources.WrongCultureError3,
                                Properties.Resources.WrongCultureError5
                                    ), "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    //  MessageBox.Show(errorText);

                    //    String.Format("Warning: The project being opened was created using {0} culture settings, " +
                    //"but the current thread is set to {1}. This is not recommended. Please change the application's " +
                    //"culture settings to match those of the project.",
                    //    e.ProjectInfo.Culture,
                    //    System.Threading.Thread.CurrentThread.CurrentUICulture.ToString()), "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    return;
                }
            }

            if (e.SuperUser)
            {
                IsSuperUser = true;
                //contactIdColumn.Visibility = System.Windows.Visibility.Visible;
                Dialogs.AuthCodeDialog authDialog = new Dialogs.AuthCodeDialog(ContactTracing.Core.Constants.SUPER_USER_CODE);
                System.Windows.Forms.DialogResult authResult = authDialog.ShowDialog();
                if (authResult == System.Windows.Forms.DialogResult.OK)
                {
                    if (authDialog.IsAuthorized)
                    {
                        btnDataTools.Visibility = System.Windows.Visibility.Visible;
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
            else
            {
                IsSuperUser = false;
                //contactIdColumn.Visibility = System.Windows.Visibility.Collapsed;
                btnDataTools.Visibility = System.Windows.Visibility.Collapsed;
            }

            grdIntro.Visibility = System.Windows.Visibility.Collapsed;

            this.Cursor = Cursors.Wait;

            fileScreen.ShouldPollForFiles = false;

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            worker.RunWorkerAsync(e.ProjectInfo.FileInfo.FullName);
        }

        private void BeginLoadProject(VhfProject project)
        {
            if (project.CollectedData.GetDatabase() is Epi.Data.Office.OleDbDatabase && project.FilePath.StartsWith(@"\\"))
            {
                MessageBox.Show("Outbreak databases using local storage file formats (*.mdb) cannot be opened when they reside on a network drive. For multi-user functionality please migrate to Microsoft SQL Server.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                Database = project.CollectedData.GetDatabase();

                if (Database.FullName.Contains("[MS Access]") && !Database.TableExists("metaHistory"))
                {
                    Database.CompactDatabase();
                }
            }
            catch { }

            //Project project = new Project(e.ProjectInfo.FileInfo.FullName);
            LoadProject(project);
            DataHelper.RepopulateCollections(true);

            if (DataHelper.VirusTestType == Core.Enums.VirusTestTypes.Rift)
            {
                tabButtonContacts.Visibility = System.Windows.Visibility.Collapsed;
                tabButtonTransmissionChain.Visibility = System.Windows.Visibility.Collapsed;
            }

            dpPrev.DisplayDateEnd = DateTime.Now.AddDays(1);

            try
            {
                TransmissionChain = new TransmissionChain(((EpiDataHelper)DataContext).CaseForm, ((EpiDataHelper)DataContext).Project.CollectedData.GetDbDriver(), TransmissionCanvas, TransmissionDates, this);
            }
            catch (Exception)
            {
            }
            //grdIntro.Visibility = System.Windows.Visibility.Collapsed;
            //panelLoading.Visibility = System.Windows.Visibility.Collapsed;
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            VhfProject project = e.Result as VhfProject;
            if (project != null)
            {
                this.Dispatcher.BeginInvoke(new BeginLoadProjectHandler(BeginLoadProject), project);
            }
            else
            {
                this.Dispatcher.BeginInvoke(new SimpleEventHandler(CloseProject));
            }
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            //Project project = new Project(e.ProjectInfo.FileInfo.FullName);
            try
            {
                VhfProject project = new VhfProject((string)e.Argument);
                e.Result = project;
            }
            catch (Epi.GeneralException)
            {
                MessageBox.Show("No connection to the database was detected. Please check that you are connected to the proper network and that all networking hardware is operational.", "Disconnected", MessageBoxButton.OK, MessageBoxImage.Information);
                e.Result = null;
            }
            catch (InvalidOperationException)
            {
                MessageBox.Show("No connection to the database was detected. Please check that you are connected to the proper network and that all networking hardware is operational.", "Disconnected", MessageBoxButton.OK, MessageBoxImage.Information);
                e.Result = null;
            }
        }

        private void LoadProject(VhfProject project)
        {
            double maxHeight = gridCaseManagement.ActualHeight;
            MaxHeight = maxHeight - panelCaseManagementSearch.ActualHeight - 128;
            if (MaxHeight > 0)
            {
                dgCases.Height = MaxHeight;
                dgDaily.MaxHeight = MaxHeight;
                dgPrev.MaxHeight = MaxHeight;
                gridAnalysis.SvHeight = MaxHeight + 100;
            }

            existingContact.DataContext = this.DataContext;
            existingCase.DataContext = this.DataContext;
            //importFromLab.DataContext = this.DataContext;
            DataHelper.InitializeProject(project);
            DataHelper.SetupDatabase();
        }

        private void newOutbreak_Closed(object sender, RoutedEventArgs e)
        {
            newOutbreak.Visibility = System.Windows.Visibility.Collapsed;

            Match m = Regex.Match(newOutbreak.FileName, "^[a-zA-Z0-9_]*$");
            if (!m.Success)
            {
                MessageBox.Show("Invalid file name detected for this outbreak. File names may only contain letters, numbers, and underscores. The operation has been halted.", Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (e.RoutedEvent.Name.ToLower() == "ok")
            {
                string newProjectDatabaseName = @"Projects\VHF\vhf_" + newOutbreak.FileName.ToLower().Replace(" ", String.Empty) + ".mdb";
                string newProjectName = @"Projects\VHF\vhf_" + newOutbreak.FileName.ToLower().Replace(" ", String.Empty) + ".prj";

                string cultureValue = System.Threading.Thread.CurrentThread.CurrentUICulture.ToString();
                if (File.Exists(newProjectDatabaseName))
                {
                    MessageBox.Show(Properties.Resources.ProjectNameExistsTryAgain);
                }
                else
                {
                    VhfProject project = ContactTracing.ImportExport.ImportExportHelper.CreateNewOutbreak(newOutbreak.Country, cultureValue, newProjectName, newProjectDatabaseName, newOutbreak.OutbreakDate.Value.Ticks.ToString(), newOutbreak.OutbreakName);
                    //if (newOutbreak.Country == "USA")
                    //{
                    //    File.Copy(@"Projects\VHF\base_vhf_template_us.mdb", newProjectDatabaseName);
                    //}
                    //else
                    //{
                    //    switch (cultureValue)
                    //    {
                    //        case "fr":
                    //        case "fr-FR":
                    //        case "fr-fr":
                    //        case "fr­­­­­­­­­­­–FR":
                    //        case "fr­­­­­­­­­­­–fr":
                    //        case "fr­­­­­­­­­­­­­­­­­­­­­­—FR":
                    //        case "fr­­­­­­­­­­­­­­­­­­­­­­—fr":
                    //        case "fr―­­­­­­­­­­­­­­­FR":
                    //        case "fr­­­­­­­­­­­­­­­­­­­­­­―fr":
                    //            File.Copy(@"Projects\VHF\base_vhf_template_fr.mdb", newProjectDatabaseName);
                    //            break;
                    //        default:
                    //            File.Copy(@"Projects\VHF\base_vhf_template.mdb", newProjectDatabaseName);

                    //            if (newOutbreak.Country != "Uganda")
                    //            {
                    //                updateMetaFields = true;
                    //            }
                    //            break;
                    //    }
                    //}
                    //Epi.Util.CreateProjectFileFromDatabase(newProjectDatabaseName, true);

                    //// add vhf tags to xml document
                    //XmlDocument doc = new XmlDocument();
                    //doc.XmlResolver = null;
                    //doc.Load(newProjectName);

                    //XmlNode projectNode = doc.SelectSingleNode("Project");

                    //XmlElement isVhfElement = doc.CreateElement("IsVHF");
                    //XmlElement isLabElement = doc.CreateElement("IsLabProject");
                    //XmlElement outbreakNameElement = doc.CreateElement("OutbreakName");
                    //XmlElement outbreakDateElement = doc.CreateElement("OutbreakDate");
                    //XmlElement cultureElement = doc.CreateElement("Culture");

                    //isVhfElement.InnerText = "true";
                    //isLabElement.InnerText = "false";
                    //outbreakDateElement.InnerText = newOutbreak.OutbreakDate.Value.Ticks.ToString();
                    //outbreakNameElement.InnerText = newOutbreak.OutbreakName;
                    //cultureElement.InnerText = cultureValue;

                    //projectNode.AppendChild(isVhfElement);
                    //projectNode.AppendChild(isLabElement);
                    //projectNode.AppendChild(outbreakDateElement);
                    //projectNode.AppendChild(outbreakNameElement);
                    //projectNode.AppendChild(cultureElement);

                    //System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
                    //XmlAttribute appVersionAttribute = doc.CreateAttribute("appVersion");
                    //appVersionAttribute.Value = a.GetName().Version.ToString();

                    //projectNode.Attributes.Append(appVersionAttribute);

                    //doc.Save(newProjectName);

                    //VhfProject project = new VhfProject(newProjectName);

                    //if (updateMetaFields)
                    //{
                    //    if (System.Threading.Thread.CurrentThread.CurrentUICulture.ToString().Equals("en-US", StringComparison.OrdinalIgnoreCase))
                    //    {
                    //        UpdateMetaFields(project, newOutbreak.Country);
                    //    }
                    //    else
                    //    {
                    //        UpdateMetaFields(project);
                    //    }
                    //}

                    //DataHelper.Adm1 = newOutbreak.Adm1;
                    //DataHelper.Adm2 = newOutbreak.Adm2;
                    //DataHelper.Adm3 = newOutbreak.Adm3;
                    //DataHelper.Adm4 = newOutbreak.Adm4;

                    LoadProject(project);
                    DataHelper.UpdateAdministrativeBoundaries();

                    grdIntro.Visibility = System.Windows.Visibility.Collapsed;

                    Logger.Log(DateTime.Now + ":  " +
                                    System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString() + ": " +
                                    "New outbreak database created.");

                    btnDataTools.Visibility = System.Windows.Visibility.Collapsed;
                    IsSuperUser = false;

                    DataHelper.FillInOutbreakData(newOutbreak.OutbreakName, newOutbreak.IDPrefix, newOutbreak.IDSeparator,
                        newOutbreak.OutbreakDate, newOutbreak.IDPattern, newOutbreak.Virus, newOutbreak.Country, newOutbreak.IsShortForm, ContactType); //17040

                    DataHelper.RepopulateCollections(true);

                    DataHelper.Adm1 = newOutbreak.Adm1;
                    DataHelper.Adm2 = newOutbreak.Adm2;
                    DataHelper.Adm3 = newOutbreak.Adm3;
                    DataHelper.Adm4 = newOutbreak.Adm4;
                }
            }
        }


        //private void UpdateMetaFields(Project project, string countryName = "")
        //{
        //    IDbDriver db = project.CollectedData.GetDatabase();

        //    // 1 = text
        //    // 17, 18, 19 = ddl's

        //    Query updateQuery = db.CreateQuery("UPDATE [metaFields] SET FieldTypeId = 1 " +
        //        "WHERE (FieldTypeId = 17 OR FieldTypeId = 18 OR FieldTypeId = 19) " +
        //        "AND (PromptText = @PromptTextDistrict OR PromptText = @PromptTextSC)");

        //    updateQuery.Parameters.Add(new QueryParameter("@PromptTextDistrict", DbType.String, "District:"));
        //    updateQuery.Parameters.Add(new QueryParameter("@PromptTextSC", DbType.String, "Sub-County:"));

        //    int rows = db.ExecuteNonQuery(updateQuery);

        //    if (rows == 0)
        //    {
        //        // shouldn't get here
        //    }

        //    #region Wipe out districts
        //    string querySyntax = "DELETE * FROM [codeDistrictSubCountyList]";
        //    if (db.ToString().ToLower().Contains("sql"))
        //    {
        //        querySyntax = "DELETE FROM [codeDistrictSubCountyList]";
        //    }

        //    Query deleteQuery = db.CreateQuery(querySyntax);
        //    db.ExecuteNonQuery(deleteQuery);
        //    #endregion // Wipe out districts

        //    updateQuery = db.CreateQuery("UPDATE [metaFields] " +
        //        "SET PromptText = 'Viral Hemorrhagic Fever Outbreak Laboratory Diagnostic Specimens and Results Form' " +
        //        "WHERE FieldId = 230 OR FieldId = 590");
        //    rows = db.ExecuteNonQuery(updateQuery);

        //    updateQuery = db.CreateQuery("UPDATE [metaFields] SET [ControlLeftPositionPercentage] = @CLPP WHERE [Name] = @Name");
        //    updateQuery.Parameters.Add(new QueryParameter("@CLPP", DbType.Double, 0.01));
        //    updateQuery.Parameters.Add(new QueryParameter("@Name", DbType.String, "CRFTitle"));
        //    rows = db.ExecuteNonQuery(updateQuery);

        //    updateQuery = db.CreateQuery("UPDATE [metaFields] " +
        //        "SET PromptText = 'Viral Hemorrhagic Fever Contact Information Entry Form' " +
        //        "WHERE FieldId = 345");
        //    rows = db.ExecuteNonQuery(updateQuery);

        //    updateQuery = db.CreateQuery("UPDATE [metaFields] " +
        //        "SET PromptText = @CountryName " +
        //        "WHERE FieldId = 4");
        //    updateQuery.Parameters.Add(new QueryParameter("@CountryName", DbType.String, countryName + " Viral Hemorrhagic Fever Case Investigation Form"));
        //    rows = db.ExecuteNonQuery(updateQuery);

        //    if (rows == 0)
        //    {
        //        // shouldn't get here
        //    }

        //    updateQuery = db.CreateQuery("UPDATE metaPages " +
        //        "SET BackgroundId = 0");
        //    rows = db.ExecuteNonQuery(updateQuery);

        //    if (rows == 0)
        //    {
        //        // shouldn't get here
        //    }
        //}

        private void btnCloseProject_Click(object sender, RoutedEventArgs e)
        {
            CloseProject();
        }

        private void CloseProject()
        {
            DataHelper.OutbreakName = String.Empty;
            DataHelper.CloseProject();
            grdIntro.Visibility = System.Windows.Visibility.Visible;
            tabButtonCases.IsChecked = true;
            fileScreen.Refresh();
            fileScreen.ShouldPollForFiles = true;
            this.Cursor = Cursors.Arrow;
        }

        private void carc_ListLabSamplesRequested(object sender, EventArgs e)
        {
            if (Popup == null && sender != null)
            {
                CaseActionsRowControl carc = sender as CaseActionsRowControl;
                if (carc != null)
                {
                    CaseViewModel caseVM = carc.DataContext as CaseViewModel;

                    if (caseVM != null)
                    {
                        Popup = new Popup();
                        Popup.Parent = grdMain;

                        LabRecordsForCase labRecordsPanel = new LabRecordsForCase(caseVM);
                        labRecordsPanel.Width = this.ActualWidth - 50;
                        labRecordsPanel.MinWidth = 790;
                        labRecordsPanel.MaxWidth = 1900;
                        labRecordsPanel.Height = this.ActualHeight - 100;
                        labRecordsPanel.DataContext = this.DataContext;

                        DataHelper.PopulateLabRecordsForCase.Execute(caseVM);
                        //labRecordsPanel.Closed += new EventHandler(labRecordsPanel_Closed);
                        labRecordsPanel.Closed += labRecordsPanel_Closed;

                        //DataHelper.LoadExtendedCaseData(caseVM);
                        //labRecordsPanel.DataContext = caseVM;

                        Popup.Content = labRecordsPanel;
                        Popup.Show();
                    }
                }
            }
        }

        void labRecordsPanel_Closed(object sender, EventArgs e)
        {
            Popup.Close();
            Popup = null;
        }

        private void carc_DeleteRequested(object sender, RoutedEventArgs e)
        {
            if (DataHelper.IsCheckingServerForUpdates || DataHelper.IsWaitingOnOtherClients || DataHelper.IsSendingServerUpdates || DataHelper.IsLoadingProjectData || DataHelper.IsExportingData)
            {
                MessageBox.Show("Waiting on server updates. Please try again later.", "Waiting", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (sender != null)
            {
                CaseActionsRowControl carc = sender as CaseActionsRowControl;
                if (carc != null)
                {
                    CaseViewModel caseVM = carc.DataContext as CaseViewModel;

                    if (caseVM != null)
                    {
                        DeleteCase(caseVM);
                    }
                }
            }
        }

        private void carc_PrintFullFormRequested(object sender, EventArgs e)
        {
            if (Popup == null && sender != null && sender is CaseActionsRowControl && (sender as CaseActionsRowControl).DataContext is CaseViewModel)
            {
                CaseActionsRowControl carc = sender as CaseActionsRowControl;
                if (carc != null && carc.DataContext != null)
                {
                    CaseViewModel caseVM = carc.DataContext as CaseViewModel;

                    if (caseVM != null)
                    {
                        Popup = new Popup();
                        Popup.Parent = grdMain;

                        CaseReportFormViewer formViewer = new CaseReportFormViewer();
                        formViewer.Width = this.ActualWidth - 50;
                        formViewer.MinWidth = 790;
                        formViewer.MaxWidth = 1050;
                        formViewer.Height = this.ActualHeight - 100;
                        formViewer.Closed += new EventHandler(formViewer_Closed);

                        //DataHelper.LoadExtendedCaseData(caseVM);
                        caseVM.Load();
                        formViewer.DataContext = caseVM;

                        Popup.Content = formViewer;
                        Popup.Show();
                    }
                }
            }
        }

        private void carc_PrintOutcomeFormRequested(object sender, EventArgs e)
        {
            if (Popup == null && sender != null && sender is CaseActionsRowControl && (sender as CaseActionsRowControl).DataContext is CaseViewModel)
            {
                CaseActionsRowControl carc = sender as CaseActionsRowControl;
                if (carc != null && carc.DataContext != null)
                {
                    CaseViewModel caseVM = carc.DataContext as CaseViewModel;

                    if (caseVM != null)
                    {
                        Popup = new Popup();
                        Popup.Parent = grdMain;

                        PatientOutcomeFormViewer formViewer = new PatientOutcomeFormViewer();
                        formViewer.Width = this.ActualWidth - 50;
                        formViewer.MinWidth = 790;
                        formViewer.MaxWidth = 1050;
                        formViewer.Height = this.ActualHeight - 100;
                        formViewer.Closed += new EventHandler(formViewer_Closed);

                        //DataHelper.LoadOutcomeCaseData(caseVM);
                        caseVM.Load();
                        formViewer.DataContext = caseVM;

                        Popup.Content = formViewer;
                        Popup.Show();
                    }
                }
            }
        }

        private void formViewer_Closed(object sender, EventArgs e)
        {
            if (Popup != null && Popup.Content != null)
            {
                PatientOutcomeFormViewer form = Popup.Content as PatientOutcomeFormViewer;
                if (form != null)
                {
                    form.Closed -= formViewer_Closed;
                }
            }
            //if (Popup.Content is PatientOutcomeFormViewer)
            //{
            //    (Popup.Content as PatientOutcomeFormViewer).Closed -= new EventHandler(formViewer_Closed);
            //}
            Popup.Close();
            Popup = null;
        }

        private void carc_ForceUnlockRequested(object sender, EventArgs e)
        {
            if (IsSuperUser)
            {
                DataHelper.SendMessageForUnlockCase(sender as CaseViewModel);
            }
            else
            {
                MessageBox.Show("Only a system administrator can forcibly unlock this record.", "Unable to unlock", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
        }

        //private void carc_ListExposureRelationshipsRequested(object sender, EventArgs e)
        //{
        //    if (Popup == null)
        //    {
        //        CaseViewModel caseVM = ((sender as CaseActionsRowControl).DataContext as CaseViewModel);

        //        if (caseVM == null)
        //        {
        //            return;
        //        }

        //        Popup = new Popup();
        //        Popup.Parent = grdMain;


        //        ExposureRelationshipsForCase exposureRelationshipsPanel = new ExposureRelationshipsForCase(caseVM);
        //        exposureRelationshipsPanel.Width = this.ActualWidth - 50;
        //        exposureRelationshipsPanel.MinWidth = 790;
        //        exposureRelationshipsPanel.MaxWidth = 1900;
        //        exposureRelationshipsPanel.Height = this.ActualHeight - 100;
        //        exposureRelationshipsPanel.DataContext = this.DataContext;

        //        //DataHelper.PopulateLabRecordsForCase.Execute(caseVM);
        //        //labRecordsPanel.Closed += new EventHandler(labRecordsPanel_Closed);
        //        exposureRelationshipsPanel.Closed += exposureRelationshipsPanel_Closed;
        //        //DataHelper.LoadExtendedCaseData(caseVM);
        //        //labRecordsPanel.DataContext = caseVM;

        //        Popup.Content = exposureRelationshipsPanel;
        //        Popup.Show();
        //    }
        //}

        //void exposureRelationshipsPanel_Closed(object sender, EventArgs e)
        //{
        //    if (Popup != null)
        //    {
        //        Popup.Close();
        //        Popup = null;
        //    }
        //}

        private void carc_ConversionToContactRequested(object sender, EventArgs e)
        {
            if (sender != null && sender is CaseActionsRowControl && (sender as CaseActionsRowControl).DataContext is CaseViewModel)
            {
                CaseActionsRowControl carc = sender as CaseActionsRowControl;
                if (carc != null && carc.DataContext != null)
                {
                    CaseViewModel caseVM = carc.DataContext as CaseViewModel;
                    if (caseVM != null)
                    {
                        if (DataHelper.GetContactVM(caseVM.RecordId) != null)
                        {
                            MessageBox.Show(Properties.Resources.CaseAlreadyExistsAsContact, Properties.Resources.CannotConvertToContact, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            return;
                        }

                        if (!(caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.NotCase)) // TODO: re-test
                        {
                            MessageBox.Show(Properties.Resources.OnlyConvertNotACase, Properties.Resources.CannotConvertToContact, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            return;
                        }

                        if (DataHelper.CaseHasExposures(caseVM) == false)
                        {
                            DataHelper.SendMessageForLockCase(caseVM);
                            DataHelper.SearchExistingCasesText = String.Empty;
                            DataHelper.SearchExistingCases.Execute(null);
                            convertToContact.Visibility = System.Windows.Visibility.Visible;
                        }
                        else
                        {
                            try
                            {
                                DataHelper.SendMessageForLockCase(caseVM);

                                this.Cursor = Cursors.Wait;
                                DataHelper.ConvertCaseToContactWithExistingSources.Execute(caseVM);
                                this.Cursor = Cursors.Arrow;
                            }
                            catch (ApplicationException ex)
                            {
                                MessageBox.Show("An exception occurred during the conversion of the case with ID " + caseVM.ID + ". Please contact the application developer with this message.\n" + ex.Message);
                            }
                            finally
                            {
                                DataHelper.SendMessageForUnlockCase(caseVM);
                            }
                        }
                    }
                }
            }
        }

        private void clarc_EditLinkRequested(object sender, EventArgs e)
        {
            if (sender != null && sender is ContactLinkActionsRowControl && (sender as ContactLinkActionsRowControl).DataContext is CaseContactPairViewModel)
            {
                ContactLinkActionsRowControl clarc = sender as ContactLinkActionsRowControl;
                if (clarc != null && clarc.DataContext != null)
                {
                    CaseContactPairViewModel ccpVM = clarc.DataContext as CaseContactPairViewModel;
                    if (ccpVM != null)
                    {
                        if (ccpVM.CaseVM.IsLocked || ccpVM.ContactVM.IsLocked)
                        {
                            MessageBox.Show("Either this case or contacts of this case are locked for editing. Please wait until other users have released this lock before proceeding.", "Record locked", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }

                        DataHelper.SendMessageForLockCase(ccpVM.CaseVM);

                        Dialogs.NewContactDialog cDialog = new Dialogs.NewContactDialog(System.Threading.Thread.CurrentThread.CurrentCulture);
                        cDialog.Relationship = ccpVM.Relationship;
                        cDialog.ContactType = ccpVM.ContactType;
                        cDialog.ContactDate = ccpVM.DateLastContact;
                        cDialog.IsEstimated = ccpVM.IsContactDateEstimated;

                        System.Windows.Forms.DialogResult result = cDialog.ShowDialog();

                        if (result == System.Windows.Forms.DialogResult.OK)
                        {
                            ccpVM.Relationship = cDialog.Relationship;
                            ccpVM.ContactType = cDialog.ContactType;
                            ccpVM.IsContactDateEstimated = cDialog.IsEstimated;
                            ccpVM.DateLastContact = cDialog.ContactDate;
                            try
                            {
                                DataHelper.UpdateCaseContactLink.Execute(ccpVM);
                                DataHelper.ShowCasesForContact.Execute(ccpVM.ContactVM);
                                DataHelper.SendMessageForUpdateCaseToContactRelationship(ccpVM.CaseVM.RecordId);
                                //RefreshIndividualChart();
                            }
                            catch (ApplicationException ex)
                            {
                                MessageBox.Show("An exception occurred during editing of a link between case " + ccpVM.CaseID + " and contact " + ccpVM.ContactVM.ContactID + ". Please contact the application developer with this message.\n" + ex.Message);
                            }
                        }

                        DataHelper.SendMessageForUnlockCase(ccpVM.CaseVM);
                    }
                }
            }
        }

        private void clarc_UnlinkContact(object sender, EventArgs e)
        {
            if (sender != null)
            {
                ContactLinkActionsRowControl clarc = sender as ContactLinkActionsRowControl;
                if (clarc != null && clarc.DataContext != null)
                {
                    CaseContactPairViewModel ccpVM = clarc.DataContext as CaseContactPairViewModel;

                    if (ccpVM == null)
                    {
                        return;
                    }

                    if (ccpVM.CaseVM.IsLocked || ccpVM.ContactVM.IsLocked)
                    {
                        MessageBox.Show("Either this case or contacts of this case are locked for editing. Please wait until other users have released this lock before proceeding.", "Record locked", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    DataHelper.SendMessageForLockContact(ccpVM.ContactVM);

                    System.Windows.Forms.DialogResult result = Epi.Windows.MsgBox.ShowQuestion(Properties.Resources.QuestionUnlinkAreYouSure);

                    if (result == System.Windows.Forms.DialogResult.OK || result == System.Windows.Forms.DialogResult.Yes)
                    {
                        Dialogs.AuthCodeDialog authDialog = new Dialogs.AuthCodeDialog(ContactTracing.Core.Constants.AUTH_CODE);
                        System.Windows.Forms.DialogResult authResult = authDialog.ShowDialog();
                        if (authResult == System.Windows.Forms.DialogResult.OK)
                        {
                            if (authDialog.IsAuthorized)
                            {
                                //CaseContactPairViewModel ccpVM = ((CaseContactPairViewModel)dgContacts.SelectedItem);

                                bool isContactForOtherCases = false;
                                foreach (CaseContactPairViewModel iccpVM in DataHelper.ContactLinkCollection)
                                {
                                    if (iccpVM.ContactVM.RecordId == ccpVM.ContactVM.RecordId && iccpVM.CaseVM.RecordId != ccpVM.CaseVM.RecordId)
                                    {
                                        isContactForOtherCases = true;
                                    }
                                }

                                try
                                {
                                    if (isContactForOtherCases)
                                    {
                                        DataHelper.DeleteContactLink.Execute(ccpVM);
                                    }
                                    else
                                    {
                                        System.Windows.Forms.DialogResult deleteResult = Epi.Windows.MsgBox.ShowQuestion(Properties.Resources.QuestionLastLinkWillBeDeleted);

                                        if (deleteResult == System.Windows.Forms.DialogResult.OK || deleteResult == System.Windows.Forms.DialogResult.Yes)
                                        {
                                            DataHelper.DeleteContactLink.Execute(ccpVM);
                                            DataHelper.DeleteContact.Execute(ccpVM.ContactVM.RecordId);
                                        }
                                    }
                                }
                                catch (ApplicationException ex)
                                {
                                    MessageBox.Show("An exception occurred while trying to unlink contact " + ccpVM.ContactVM.ContactID + ". Please contact the application developer with this message.\n" + ex.Message);
                                }

                                // remove later
                                if (ApplicationViewModel.Instance.CurrentRegion == RegionEnum.USA)
                                {
                                    DataHelper.RepopulateCollections(false);
                                }
                            }
                        }
                    }

                    DataHelper.SendMessageForUnlockContact(ccpVM.ContactVM);
                }
            }
        }

        private void caseLinkActions_EditLinkRequested(object sender, EventArgs e)
        {
            if (sender != null)
            {
                CaseLinkActionsRowControl clarc = sender as CaseLinkActionsRowControl;
                if (clarc != null && clarc.DataContext != null)
                {
                    CaseExposurePairViewModel cepVM = clarc.DataContext as CaseExposurePairViewModel;

                    if (cepVM == null)
                    {
                        return;
                    }

                    Dialogs.NewCaseDialog cDialog = new Dialogs.NewCaseDialog(System.Threading.Thread.CurrentThread.CurrentCulture);
                    cDialog.Relationship = cepVM.Relationship;
                    cDialog.ContactType = cepVM.ContactType;
                    cDialog.ContactDate = cepVM.DateLastContact;
                    cDialog.IsEstimated = cepVM.IsContactDateEstimated;
                    cDialog.IsTentative = cepVM.IsTentative;

                    System.Windows.Forms.DialogResult result = cDialog.ShowDialog();

                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        cepVM.Relationship = cDialog.Relationship;
                        cepVM.ContactType = cDialog.ContactType;
                        cepVM.IsContactDateEstimated = cDialog.IsEstimated;
                        cepVM.DateLastContact = cDialog.ContactDate;
                        cepVM.IsTentative = cDialog.IsTentative;
                        try
                        {
                            DataHelper.UpdateCaseExposureLink.Execute(cepVM);
                        }
                        catch (ApplicationException ex)
                        {
                            MessageBox.Show("An exception occurred while editing a case-source case link (" + cepVM.SourceCaseVM.ID + ", " + cepVM.ExposedCaseVM.ID + "). Please contact the application developer with this message.\n" + ex.Message);
                        }
                    }
                }
            }
        }

        private void caseLinkActions_UnlinkContact(object sender, EventArgs e)
        {
            if (sender != null)
            {
                CaseLinkActionsRowControl clarc = sender as CaseLinkActionsRowControl;
                if (clarc != null && clarc.DataContext != null)
                {
                    CaseExposurePairViewModel cep = clarc.DataContext as CaseExposurePairViewModel;
                    if (cep != null)
                    {
                        System.Windows.Forms.DialogResult result = Epi.Windows.MsgBox.ShowQuestion(Properties.Resources.QuestionUnlinkExposureAreYouSure);

                        if (result == System.Windows.Forms.DialogResult.OK || result == System.Windows.Forms.DialogResult.Yes)
                        {
                            Dialogs.AuthCodeDialog authDialog = new Dialogs.AuthCodeDialog(ContactTracing.Core.Constants.AUTH_CODE);
                            System.Windows.Forms.DialogResult authResult = authDialog.ShowDialog();
                            if (authResult == System.Windows.Forms.DialogResult.OK)
                            {
                                if (authDialog.IsAuthorized)
                                {
                                    try
                                    {
                                        DataHelper.DeleteExposureLink.Execute(cep);
                                    }
                                    catch (ApplicationException ex)
                                    {
                                        MessageBox.Show("An exception occurred while trying to unlink a source case from its case (" + cep.SourceCaseVM.ID + ", " + cep.ExposedCaseVM.ID + "). Please contact the application developer with this message.\n" + ex.Message);
                                    }

                                    DataHelper.RepopulateCollections(false);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void contact_arc_DeleteRequested(object sender, EventArgs e)
        {
            if (sender != null && sender is ContactActionsRowControl && (sender as ContactActionsRowControl).DataContext is ContactViewModel)
            {
                ContactActionsRowControl carc = sender as ContactActionsRowControl;
                if (carc != null && carc.DataContext != null)
                {
                    ContactViewModel contactVM = carc.DataContext as ContactViewModel;

                    if (contactVM != null)
                    {
                        if (contactVM.IsLocked)
                        {
                            MessageBox.Show("Either this contact or source cases of this contact are locked for editing. Please wait until other users have released this lock before proceeding.", "Record locked", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                        else
                        {
                            DataHelper.SendMessageForLockContact(contactVM);

                            System.Windows.Forms.DialogResult result = Epi.Windows.MsgBox.ShowQuestion(Properties.Resources.QuestionDeleteContactAreYouSure);
                            if (result.Equals(System.Windows.Forms.DialogResult.Yes))
                            {
                                Dialogs.AuthCodeDialog authDialog = new Dialogs.AuthCodeDialog(ContactTracing.Core.Constants.AUTH_CODE);
                                System.Windows.Forms.DialogResult authResult = authDialog.ShowDialog();
                                if (authResult == System.Windows.Forms.DialogResult.OK)
                                {
                                    if (authDialog.IsAuthorized)
                                    {
                                        try
                                        {
                                            string contactGuid = contactVM.RecordId;
                                            if (this.DataContext != null && this.DataContext is EpiDataHelper)
                                            {
                                                ((this.DataContext) as EpiDataHelper).DeleteContact.Execute(contactGuid);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            MessageBox.Show("An exception occurred while trying to delete the contact " + contactVM.RecordId + ". Please contact the application developer with this message.\n" + ex.Message);
                                        }
                                    }
                                }
                            }
                            DataHelper.SendMessageForUnlockContact(contactVM);
                        }
                    }
                }
            }
        }

        private void contact_arc_ConversionToCaseIsoRequested(object sender, EventArgs e)
        {
            if (sender != null)
            {
                ContactActionsRowControl carc = sender as ContactActionsRowControl;
                if (carc != null && carc.DataContext != null)
                {
                    ContactViewModel contactVM = carc.DataContext as ContactViewModel;

                    if (contactVM == null)
                    {
                        return;
                    }

                    if (contactVM.IsLocked)
                    {
                        MessageBox.Show("Either this contact or source cases of this contact are locked for editing. Please wait until other users have released this lock before proceeding.", "Record locked", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    else
                    {
                        DataHelper.SendMessageForLockContact(contactVM);

                        try
                        {
                            MessageBoxResult result = MessageBox.Show(Properties.Resources.QuestionConvertContactToCase, Properties.Resources.CannotConvertToCase, MessageBoxButton.YesNo, MessageBoxImage.Question);

                            if (result == MessageBoxResult.Yes)
                            {
                                if (contactVM.IsLocked)
                                {
                                    MessageBox.Show("Either this contact or source cases of this contact are locked for editing. Please wait until other users have released this lock before proceeding.", "Record locked", MessageBoxButton.OK, MessageBoxImage.Information);
                                    return;
                                }

                                if (DataHelper.GetCaseVM(contactVM.RecordId) != null)
                                {
                                    DataHelper.SendMessageForUnlockContact(contactVM);
                                    MessageBox.Show(Properties.Resources.ContactAlreadyExistsAsCase, Properties.Resources.CannotConvertToCase, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                    return;
                                }

                                Dialogs.DateDialog dateDialog = new Dialogs.DateDialog(System.Threading.Thread.CurrentThread.CurrentCulture);
                                System.Windows.Forms.DialogResult dateResult = dateDialog.ShowDialog();

                                if (dateResult == System.Windows.Forms.DialogResult.OK)
                                {
                                    DateTime isoDate = new DateTime(dateDialog.SelectedDate.Year, dateDialog.SelectedDate.Month, dateDialog.SelectedDate.Day);

                                    if (contactVM.DateOfLastFollowUp.HasValue)
                                    {
                                        if (isoDate.AddDays(1) > contactVM.DateOfLastFollowUp.Value)
                                        {
                                            MessageBoxResult dateConfirmResult = MessageBox.Show(
                                                String.Format("The selected date of isolation ({0}) occurs after the contact's 21-day follow-up window, which ends on {1}. Are you sure {0} is the correct date?", isoDate.ToShortDateString(), contactVM.DateOfLastFollowUp.Value.ToShortDateString()), "Confirm date", MessageBoxButton.YesNo, MessageBoxImage.Question);

                                            if (dateConfirmResult == MessageBoxResult.No)
                                            {
                                                DataHelper.SendMessageForUnlockContact(contactVM);
                                                return;
                                            }
                                        }
                                    }

                                    this.Cursor = Cursors.Wait;
                                    ContactConversionInfo info = new ContactConversionInfo(contactVM, ContactFinalOutcome.Isolated, isoDate);
                                    DataHelper.ConvertContactToCase.Execute(info);
                                    this.Cursor = Cursors.Arrow;

                                    MessageBox.Show(Properties.Resources.ConversionToCaseComplete, Properties.Resources.Success, MessageBoxButton.OK, MessageBoxImage.Information);

                                    // Refresh chart
                                    //if (dgAllContacts.SelectedItems.Count == 1)
                                    //{
                                    //    RefreshIndividualChart();
                                    //}
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(String.Format("An exception occurred while trying to convert contact {0} to a case with a status of sick and isolated. Please contact the application developer with this message.\n{1}", contactVM.RecordId, ex.Message));
                        }

                        DataHelper.SendMessageForUnlockContact(contactVM);
                    }
                }
            }
        }

        private void contact_arc_ConversionToCasePrevSickRequested(object sender, EventArgs e)
        {
            if (sender != null)
            {
                ContactActionsRowControl carc = sender as ContactActionsRowControl;
                if (carc != null && carc.DataContext != null)
                {
                    ContactViewModel contactVM = carc.DataContext as ContactViewModel;

                    if (contactVM == null)
                    {
                        return;
                    }

                    if (contactVM.IsLocked)
                    {
                        MessageBox.Show("Either this contact or source cases of this contact are locked for editing. Please wait until other users have released this lock before proceeding.", "Record locked", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    try
                    {
                        MessageBoxResult result = MessageBox.Show(Properties.Resources.QuestionConvertContactToCase, Properties.Resources.ConvertToCase, MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            if (contactVM.IsLocked)
                            {
                                MessageBox.Show("Either this contact or source cases of this contact are locked for editing. Please wait until other users have released this lock before proceeding.", "Record locked", MessageBoxButton.OK, MessageBoxImage.Information);
                                return;
                            }
                            else
                            {
                                DataHelper.SendMessageForLockContact(contactVM);
                            }

                            if (DataHelper.GetCaseVM(contactVM.RecordId) != null)
                            {
                                MessageBox.Show(Properties.Resources.ContactAlreadyExistsAsCase, Properties.Resources.CannotConvertToCase, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                return;
                            }

                            this.Cursor = Cursors.Wait;
                            ContactConversionInfo info = new ContactConversionInfo(contactVM, ContactFinalOutcome.Dropped);
                            DataHelper.ConvertContactToCase.Execute(info);
                            this.Cursor = Cursors.Arrow;

                            DataHelper.SendMessageForUnlockContact(contactVM);
                            MessageBox.Show(Properties.Resources.ConversionToCaseComplete, Properties.Resources.Success, MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        DataHelper.SendMessageForUnlockContact(contactVM);
                        MessageBox.Show(String.Format("An exception occurred while trying to convert contact {0} to a case. Please contact the application developer with this message.\n{1}", contactVM.RecordId, ex.Message));
                    }
                }
            }
        }

        private void contact_arc_ConversionToCaseDiedRequested(object sender, EventArgs e)
        {
            if (sender != null)
            {
                ContactActionsRowControl carc = sender as ContactActionsRowControl;
                if (carc != null && carc.DataContext != null)
                {
                    ContactViewModel contactVM = carc.DataContext as ContactViewModel;

                    if (contactVM == null)
                    {
                        return;
                    }

                    try
                    {
                        MessageBoxResult result = MessageBox.Show(Properties.Resources.QuestionConvertContactToCase, Properties.Resources.CannotConvertToCase, MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            if (contactVM.IsLocked)
                            {
                                MessageBox.Show("Either this contact or source cases of this contact are locked for editing. Please wait until other users have released this lock before proceeding.", "Record locked", MessageBoxButton.OK, MessageBoxImage.Information);
                                return;
                            }
                            else
                            {
                                DataHelper.SendMessageForLockContact(contactVM);
                            }

                            if (DataHelper.GetCaseVM(contactVM.RecordId) != null)
                            {
                                DataHelper.SendMessageForUnlockContact(contactVM);
                                MessageBox.Show(Properties.Resources.ContactAlreadyExistsAsCase, Properties.Resources.CannotConvertToCase, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                return;
                            }

                            Dialogs.DateDeathDialog dateDialog = new Dialogs.DateDeathDialog(System.Threading.Thread.CurrentThread.CurrentCulture);
                            System.Windows.Forms.DialogResult dateResult = dateDialog.ShowDialog();

                            if (dateResult == System.Windows.Forms.DialogResult.OK)
                            {
                                DateTime deathDate = new DateTime(dateDialog.SelectedDate.Year, dateDialog.SelectedDate.Month, dateDialog.SelectedDate.Day);

                                if (contactVM.DateOfLastFollowUp.HasValue)
                                {
                                    if (deathDate.AddDays(1) > contactVM.DateOfLastFollowUp.Value)
                                    {
                                        MessageBoxResult dateConfirmResult = MessageBox.Show(
                                            String.Format("The selected date of death ({0}) occurs after the contact's 21-day follow-up window, which ends on {1}. Are you sure {0} is the correct date?", deathDate.ToShortDateString(), contactVM.DateOfLastFollowUp.Value.ToShortDateString()), "Confirm date", MessageBoxButton.YesNo, MessageBoxImage.Question);

                                        if (dateConfirmResult == MessageBoxResult.No)
                                        {
                                            DataHelper.SendMessageForUnlockContact(contactVM);
                                            return;
                                        }
                                    }
                                }

                                this.Cursor = Cursors.Wait;
                                ContactConversionInfo info = new ContactConversionInfo(contactVM, ContactFinalOutcome.Isolated, deathDate, true);
                                DataHelper.ConvertContactToCase.Execute(info);
                                this.Cursor = Cursors.Arrow;

                                DataHelper.SendMessageForUnlockContact(contactVM);
                                MessageBox.Show(Properties.Resources.ConversionToCaseComplete, Properties.Resources.Success, MessageBoxButton.OK, MessageBoxImage.Information);

                                // Refresh chart
                                //if (dgAllContacts.SelectedItems.Count == 1)
                                //{
                                //    RefreshIndividualChart();
                                //}
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DataHelper.SendMessageForUnlockContact(contactVM);
                        MessageBox.Show(String.Format("An exception occurred while trying to convert contact {0} to a case with a status of sick and isolated. Please contact the application developer with this message.\n{1}", contactVM.RecordId, ex.Message));
                    }
                }
            }
        }

        private void mnuDataChecker_Click(object sender, RoutedEventArgs e)
        {
            Popup = new Popup();
            Popup.Parent = grdMain;

            ContactTracing.CaseView.Controls.Diagnostics.DataChecker dataChecker = new ContactTracing.CaseView.Controls.Diagnostics.DataChecker();

            dataChecker.Closed += dataChecker_Closed;

            dataChecker.DataContext = this.DataContext;
            dataChecker.MaxWidth = 990;

            Popup.Content = dataChecker;
            Popup.Show();
        }

        private void mnuCountryNameEditor_Click(object sender, RoutedEventArgs e)
        {
            RenderableField countryField = DataHelper.CaseForm.Fields["CountryRes"] as RenderableField;

            if (countryField != null && !(countryField is DDLFieldOfLegalValues || countryField is DDLFieldOfCodes))
            {
                MessageBox.Show("Before drop-down values for the country field can be added, the country field type must be changed from free text to a drop-down list using the 'Administrative Location Field Type Editor' tool.");
                return;
            }

            Popup = new Popup();
            Popup.Parent = grdMain;

            ContactTracing.CaseView.Controls.Diagnostics.CountryNameEditor countries = new ContactTracing.CaseView.Controls.Diagnostics.CountryNameEditor(this.DataHelper);

            countries.Closed += countries_Closed;
            countries.MaxWidth = 800;
            countries.MaxHeight = 700;

            Popup.Content = countries;
            Popup.Show();
        }

        private void mnuDistrictNameEditor_Click(object sender, RoutedEventArgs e)
        {
            RenderableField districtField = DataHelper.CaseForm.Fields["DistrictRes"] as RenderableField;
            RenderableField scField = DataHelper.CaseForm.Fields["SCRes"] as RenderableField;

            if (districtField != null && scField != null && !(districtField is DDLFieldOfLegalValues || districtField is DDLFieldOfCodes))
            {
                MessageBox.Show("Before drop-down values for the administrative location fields can be added, the administrative location field types must be changed from free text to a drop-down list using the 'Administrative Location Field Type Editor' tool.");
                return;
            }

            Popup = new Popup();
            Popup.Parent = grdMain;

            ContactTracing.CaseView.Controls.Diagnostics.DistrictNameEditor districts = new ContactTracing.CaseView.Controls.Diagnostics.DistrictNameEditor(this.DataHelper);

            districts.Closed += districts_Closed;
            districts.MaxWidth = 800;
            districts.MaxHeight = 700;

            Popup.Content = districts;
            Popup.Show();
        }

        private void btnPrintCasesMissingContacts_Click(object sender, RoutedEventArgs e)
        {
            string baseFileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString("N");

            StringBuilder htmlBuilder = new StringBuilder();
            IMultiValueConverter dateConverter = new Converters.DateConverter();
            htmlBuilder.Append(ContactTracing.Core.Common.GetHtmlHeader().ToString());

            int rowsGenerated = 0;
            bool firstPage = true;

            htmlBuilder.AppendLine("<table width=\"100%\" style=\"border: 0px; padding: 0px; margin: 0px; clear: left; width:100%; \">");
            htmlBuilder.AppendLine(" <tr style=\"border: 0px;\">");
            htmlBuilder.AppendLine("  <td width=\"50%\" style=\"border: 0px;\">");
            htmlBuilder.AppendLine("   <p style=\"font-size: 13pt; font-weight: bold; clear: left; text-decoration: underline;\">" + Properties.Settings.Default.HtmlPrintoutTitle + "</p>");
            htmlBuilder.AppendLine("   <p style=\"font-size: 13pt; font-weight: bold; clear: left; text-decoration: underline;\">" + Properties.Resources.HtmlCasesWithoutContactsHeading + "</p>");
            htmlBuilder.AppendLine("  </td>");
            htmlBuilder.AppendLine(" </tr>");
            htmlBuilder.AppendLine("</table>");

            Dictionary<int, Boundry> ba = DataHelper.BoundaryAggregation;

            foreach (CaseViewModel c in DataHelper.CasesWithoutContactsCollectionView)
            {
                if (rowsGenerated == 0)
                {
                    htmlBuilder.AppendLine("<table style=\"width: 1200px; border: 4px solid black;\" align=\"left\">");
                    htmlBuilder.AppendLine("<thead>");
                    htmlBuilder.AppendLine("<tr style=\"border-top: 0px solid black;\">");
                    htmlBuilder.AppendLine("<th style=\"width: 110px;\">" + Properties.Resources.ColHeaderID + "</th>");
                    if (DataHelper.IsCountryUS)
                    {
                        htmlBuilder.AppendLine("<th style=\"width: 110px;\">" + Properties.Resources.ColHeaderOriginalID + "</th>");
                    }
                    htmlBuilder.AppendLine("<th style=\"width: 110px;\">" + Properties.Resources.ColHeaderSurname + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 140px;\">" + Properties.Resources.ColHeaderOtherNames + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 110px;\">" + Properties.Resources.ColHeaderEpiCaseDef + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 15px;\">" + Properties.Resources.HTMLColHeaderSexNarrow + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 15px;\">" + Properties.Resources.HTMLColHeaderAgeNarrow + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 40px;\">" + Properties.Resources.ColHeaderCurrentStatus + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 40px;\">" + Properties.Resources.ColHeaderDeathDate + "</th>");

                    if (DataHelper.IsCountryUS)
                    {
                        htmlBuilder.AppendLine("<th style=\"width: 140px;\">" + Properties.Resources.Address + "</th>");
                    }
                    else
                    {
                        htmlBuilder.AppendLine("<th style=\"width: 140px;\">" + DataHelper.Adm4 + "</th>");
                        htmlBuilder.AppendLine("<th style=\"width: 140px;\">" + DataHelper.Adm2 + "</th>");
                        htmlBuilder.AppendLine("<th style=\"width: 140px;\">" + DataHelper.Adm1 + "</th>");
                    }

                    htmlBuilder.AppendLine("<th style=\"width: 40px;\">" + Properties.Resources.ColHeaderIsoAdmitted + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 40px;\">" + Properties.Resources.ColHeaderIsoDischarge + "</th>");

                    htmlBuilder.AppendLine("</tr>");
                    htmlBuilder.AppendLine("</thead>");
                    htmlBuilder.AppendLine("<tbody>");
                }

                htmlBuilder.AppendLine("<tr style=\"border-bottom: 1px solid black; height: 32px;\">");
                //bool pastDue = (dailyCheck.ContactVM.FollowUpWindowViewModel.WindowStartDate < minDate.AddDays(-20)
                htmlBuilder.AppendLine("<td style=\"font-size: 10pt;\">" + c.ID + "</td>");

                if (DataHelper.IsCountryUS)
                {
                    htmlBuilder.AppendLine("<td style=\"font-size: 10pt;\">" + c.OriginalID + "</td>");
                }

                htmlBuilder.AppendLine("<td style=\"font-size: 10pt;\">" + Core.Common.TruncHTMLCell(c.Surname, 13) + "</td>");

                htmlBuilder.AppendLine("<td style=\"font-size: 10pt;\">" + Core.Common.TruncHTMLCell(c.OtherNames, 20) + "</td>");
                IValueConverter caseClassConverter = new Converters.EpiCaseClassificationConverter();
                htmlBuilder.AppendLine("<td style=\"font-size: 10pt;\">" + caseClassConverter.Convert(c.EpiCaseDef, null, null, null) + "</td>");
                htmlBuilder.AppendLine("<td style=\"text-align: center;\">" + c.GenderAbbreviation + "</td>");

                if (c.AgeYears.HasValue)
                {
                    htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + c.AgeYears + "</td>");
                }
                else
                {
                    htmlBuilder.AppendLine("<td>&nbsp;</td>");
                }

                htmlBuilder.AppendLine("<td style=\"text-align: center;\">" + c.CurrentStatus + "</td>");



                if (c.DateDeathCurrentOrFinal.HasValue)
                {
                    string[] parmsValues = { c.DateDeathCurrentOrFinal.Value.ToString(), DataHelper.ApplicationCulture };
                    var datedeathorfinal = dateConverter.Convert(parmsValues, null, null, null);

                    htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + datedeathorfinal + "</td>");
                    //htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + c.DateDeathCurrentOrFinal.Value.ToString("d/M/yy") + "</td>");
                }
                else
                {
                    htmlBuilder.AppendLine("<td>&nbsp;</td>");
                }

                if (DataHelper.IsCountryUS)
                {
                    string fullUSAddress = string.Empty;

                    fullUSAddress += ba.ContainsKey(-2) && ba[-2].CaseObjectValue(c) != "" ? ba[-2].CaseObjectValue(c) + ", " : "";
                    fullUSAddress += ba.ContainsKey(0) && ba[0].CaseObjectValue(c) != "" ? ba[0].CaseObjectValue(c) + ", " : "";
                    fullUSAddress += ba.ContainsKey(1) && ba[1].CaseObjectValue(c) != "" ? ba[1].CaseObjectValue(c) + ", " : "";
                    //fullUSAddress += ba.ContainsKey(2) && ba[2].CaseObjectValue(c) != "" ? ba[2].CaseObjectValue(c) + ", " : "";
                    fullUSAddress += ba.ContainsKey(3) && ba[3].CaseObjectValue(c) != "" ? ba[3].CaseObjectValue(c) + ", " : "";
                    fullUSAddress += ba.ContainsKey(-1) && ba[-1].CaseObjectValue(c) != "" ? ba[-1].CaseObjectValue(c) + "," : "";
                    fullUSAddress = fullUSAddress.Trim().TrimEnd(new char[] { ',' });

                    htmlBuilder.AppendLine("<td style=\"text-align: left;\">" + fullUSAddress + "</td>");
                }
                else
                {
                    htmlBuilder.AppendLine("<td style=\"text-align: center;\">" + c.Village + "</td>");
                    htmlBuilder.AppendLine("<td style=\"text-align: center;\">" + c.SubCounty + "</td>");
                    htmlBuilder.AppendLine("<td style=\"text-align: center;\">" + c.District + "</td>");
                }

                if (c.DateIsolationCurrent.HasValue)
                {
                    string[] parmsValues = { c.DateIsolationCurrent.Value.ToString(), DataHelper.ApplicationCulture };
                    var dateisolationcurrent = dateConverter.Convert(parmsValues, null, null, null);

                    htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + dateisolationcurrent + "</td>");
                    //htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + c.DateIsolationCurrent.Value.ToString("d/M/yy") + "</td>");
                }
                else
                {
                    htmlBuilder.AppendLine("<td>&nbsp;</td>");
                }

                if (c.DateDischargeIso.HasValue)
                {
                    string[] parmsValues = { c.DateDischargeIso.Value.ToString(), DataHelper.ApplicationCulture };
                    var datedischargeiso = dateConverter.Convert(parmsValues, null, null, null);

                    htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + datedischargeiso + "</td>");
                    //htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + c.DateDischargeIso.Value.ToString("d/M/yy") + "</td>");
                }
                else
                {
                    htmlBuilder.AppendLine("<td>&nbsp;</td>");
                }

                htmlBuilder.AppendLine("</tr>");

                rowsGenerated++;

                if (firstPage && rowsGenerated == 19)
                {
                    htmlBuilder.Append("</tbody>");
                    htmlBuilder.Append("</table>");
                    htmlBuilder.Append("<div style=\"page-break-before:always;\" />");
                    rowsGenerated = 0;
                    firstPage = false;
                }
                else if (!firstPage && rowsGenerated == 22)
                {
                    htmlBuilder.Append("</tbody>");
                    htmlBuilder.Append("</table>");
                    htmlBuilder.Append("<div style=\"page-break-before:always;\" />");
                    rowsGenerated = 0;
                }
            }

            if (firstPage && rowsGenerated % 19 != 0)
            {
                htmlBuilder.Append("</tbody>");
                htmlBuilder.Append("</table>");
                htmlBuilder.Append("<div style=\"page-break-before:always;\" />");
                rowsGenerated = 0;
                firstPage = true;
            }
            else if (!firstPage && rowsGenerated % 22 != 0)
            {
                htmlBuilder.Append("</tbody>");
                htmlBuilder.Append("</table>");
                htmlBuilder.Append("<div style=\"page-break-before:always;\" />");
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

        private void btnPrintAliveSuspectProbableCases_Click(object sender, RoutedEventArgs e)
        {
            string baseFileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString("N");

            StringBuilder htmlBuilder = new StringBuilder();
            IMultiValueConverter dateConverter = new Converters.DateConverter();
            htmlBuilder.Append(ContactTracing.Core.Common.GetHtmlHeader().ToString());

            int rowsGenerated = 0;
            bool firstPage = true;

            htmlBuilder.AppendLine("<table width=\"100%\" style=\"border: 0px; padding: 0px; margin: 0px; clear: left; width:100%; \">");
            htmlBuilder.AppendLine(" <tr style=\"border: 0px;\">");
            htmlBuilder.AppendLine("  <td width=\"50%\" style=\"border: 0px;\">");
            htmlBuilder.AppendLine("   <p style=\"font-size: 13pt; font-weight: bold; clear: left; text-decoration: underline;\">" + Properties.Settings.Default.HtmlPrintoutTitle + "</p>");
            htmlBuilder.AppendLine("   <p style=\"font-size: 13pt; font-weight: bold; clear: left; text-decoration: underline;\">" + Properties.Resources.HtmlAliveSuspectProbableCaseRecordsHeading + "</p>");

            htmlBuilder.AppendLine("  </td>");
            htmlBuilder.AppendLine(" </tr>");
            htmlBuilder.AppendLine("</table>");

            var query = from c in DataHelper.CaseCollection
                        where c.CurrentStatus.Equals(Properties.Resources.Alive.Trim()) &&
                        (c.EpiCaseClassification.Equals("3") || c.EpiCaseClassification.Equals("2"))
                        select c;

            Dictionary<int, Boundry> ba = DataHelper.BoundaryAggregation;

            foreach (CaseViewModel c in query)
            {
                if (rowsGenerated == 0)
                {
                    htmlBuilder.AppendLine("<table style=\"width: 1200px; border: 4px solid black;\" align=\"left\">");
                    htmlBuilder.AppendLine("<thead>");
                    htmlBuilder.AppendLine("<tr style=\"border-top: 0px solid black;\">");
                    htmlBuilder.AppendLine("<th style=\"width: 110px;\">" + Properties.Resources.ColHeaderID + "</th>");
                    if (DataHelper.IsCountryUS)
                    {
                        htmlBuilder.AppendLine("<th style=\"width: 110px;\">" + Properties.Resources.ColHeaderOriginalID + "</th>");
                    }
                    htmlBuilder.AppendLine("<th style=\"width: 110px;\">" + Properties.Resources.ColHeaderSurname + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 140px;\">" + Properties.Resources.ColHeaderOtherNames + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 110px;\">" + Properties.Resources.ColHeaderEpiCaseDef + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 15px;\">" + Properties.Resources.HTMLColHeaderSexNarrow + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 15px;\">" + Properties.Resources.HTMLColHeaderAgeNarrow + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 40px;\">" + Properties.Resources.ColHeaderOnsetDate + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 40px;\">" + Properties.Resources.ColHeaderCurrentStatus + "</th>");

                    if (DataHelper.IsCountryUS)
                    {
                        htmlBuilder.AppendLine("<th style=\"width: 140px;\">" + Properties.Resources.Address + "</th>");
                    }
                    else
                    {
                        htmlBuilder.AppendLine("<th style=\"width: 140px;\">" + DataHelper.Adm4 + "</th>");
                        htmlBuilder.AppendLine("<th style=\"width: 140px;\">" + DataHelper.Adm2 + "</th>");
                        htmlBuilder.AppendLine("<th style=\"width: 140px;\">" + DataHelper.Adm1 + "</th>");
                    }

                    htmlBuilder.AppendLine("<th style=\"width: 40px;\">" + Properties.Resources.ColHeaderIsoAdmitted + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 40px;\">" + Properties.Resources.ColHeaderIsoDischarge + "</th>");

                    htmlBuilder.AppendLine("</tr>");
                    htmlBuilder.AppendLine("</thead>");
                    htmlBuilder.AppendLine("<tbody>");
                }

                htmlBuilder.AppendLine("<tr style=\"border-bottom: 1px solid black; height: 32px;\">");
                //bool pastDue = (dailyCheck.ContactVM.FollowUpWindowViewModel.WindowStartDate < minDate.AddDays(-20)
                htmlBuilder.AppendLine("<td style=\"font-size: 10pt;\">" + c.ID + "</td>");
                if (DataHelper.IsCountryUS)
                {
                    htmlBuilder.AppendLine("<td style=\"font-size: 10pt;\">" + c.OriginalID + "</td>");
                }

                htmlBuilder.AppendLine("<td style=\"font-size: 10pt;\">" + Core.Common.TruncHTMLCell(c.Surname, 13) + "</td>");

                htmlBuilder.AppendLine("<td style=\"font-size: 10pt;\">" + Core.Common.TruncHTMLCell(c.OtherNames, 20) + "</td>");
                IValueConverter caseClassConverter = new Converters.EpiCaseClassificationConverter();
                htmlBuilder.AppendLine("<td style=\"font-size: 10pt;\">" + caseClassConverter.Convert(c.EpiCaseDef, null, null, null) + "</td>");
                htmlBuilder.AppendLine("<td style=\"text-align: center;\">" + c.GenderAbbreviation + "</td>");

                if (c.AgeYears.HasValue)
                {
                    htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + c.AgeYears + "</td>");
                }
                else
                {
                    htmlBuilder.AppendLine("<td>&nbsp;</td>");
                }

                if (c.DateOnset.HasValue)
                {
                    string[] parmsValues = { c.DateOnset.Value.ToString(), DataHelper.ApplicationCulture };
                    var dateonset = dateConverter.Convert(parmsValues, null, null, null);
                    //htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + c.DateOnset.Value.ToString("d/M/yy") + "</td>");
                    htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + dateonset + "</td>");
                }
                else
                {
                    htmlBuilder.AppendLine("<td>&nbsp;</td>");
                }

                htmlBuilder.AppendLine("<td style=\"text-align: center;\">" + c.CurrentStatus + "</td>");

                if (DataHelper.IsCountryUS)
                {
                    string fullUSAddress = string.Empty;

                    fullUSAddress += ba.ContainsKey(-2) && ba[-2].CaseObjectValue(c) != "" ? ba[-2].CaseObjectValue(c) + ", " : "";
                    fullUSAddress += ba.ContainsKey(0) && ba[0].CaseObjectValue(c) != "" ? ba[0].CaseObjectValue(c) + ", " : "";
                    fullUSAddress += ba.ContainsKey(1) && ba[1].CaseObjectValue(c) != "" ? ba[1].CaseObjectValue(c) + ", " : "";
                    //fullUSAddress += ba.ContainsKey(2) && ba[2].CaseObjectValue(c) != "" ? ba[2].CaseObjectValue(c) + ", " : "";
                    fullUSAddress += ba.ContainsKey(3) && ba[3].CaseObjectValue(c) != "" ? ba[3].CaseObjectValue(c) + ", " : "";
                    fullUSAddress += ba.ContainsKey(-1) && ba[-1].CaseObjectValue(c) != "" ? ba[-1].CaseObjectValue(c) + "," : "";
                    fullUSAddress = fullUSAddress.Trim().TrimEnd(new char[] { ',' });

                    htmlBuilder.AppendLine("<td style=\"text-align: left;\">" + fullUSAddress + "</td>");
                }
                else
                {
                    htmlBuilder.AppendLine("<td style=\"text-align: center;\">" + c.Village + "</td>");
                    htmlBuilder.AppendLine("<td style=\"text-align: center;\">" + c.SubCounty + "</td>");
                    htmlBuilder.AppendLine("<td style=\"text-align: center;\">" + c.District + "</td>");
                }

                if (c.DateIsolationCurrent.HasValue)
                {
                    string[] parmsValues = { c.DateIsolationCurrent.Value.ToString(), DataHelper.ApplicationCulture };
                    var dateisolation = dateConverter.Convert(parmsValues, null, null, null);
                    //htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + c.DateOnset.Value.ToString("d/M/yy") + "</td>");
                    htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + dateisolation + "</td>");
                }
                else
                {
                    htmlBuilder.AppendLine("<td>&nbsp;</td>");
                }

                if (c.DateDischargeIso.HasValue)
                {
                    string[] parmsValues = { c.DateDischargeIso.Value.ToString(), DataHelper.ApplicationCulture };
                    var dateisolation = dateConverter.Convert(parmsValues, null, null, null);

                    //htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + c.DateDischargeIso.Value.ToString("d/M/yy") + "</td>");
                    htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + dateisolation + "</td>");
                }
                else
                {
                    htmlBuilder.AppendLine("<td>&nbsp;</td>");
                }

                htmlBuilder.AppendLine("</tr>");

                rowsGenerated++;

                if (firstPage && rowsGenerated == 19)
                {
                    htmlBuilder.Append("</tbody>");
                    htmlBuilder.Append("</table>");
                    htmlBuilder.Append("<div style=\"page-break-before:always;\" />");
                    rowsGenerated = 0;
                    firstPage = false;
                }
                else if (!firstPage && rowsGenerated == 22)
                {
                    htmlBuilder.Append("</tbody>");
                    htmlBuilder.Append("</table>");
                    htmlBuilder.Append("<div style=\"page-break-before:always;\" />");
                    rowsGenerated = 0;
                }
            }

            if (firstPage && rowsGenerated % 19 != 0)
            {
                htmlBuilder.Append("</tbody>");
                htmlBuilder.Append("</table>");
                htmlBuilder.Append("<div style=\"page-break-before:always;\" />");
                rowsGenerated = 0;
                firstPage = true;
            }
            else if (!firstPage && rowsGenerated % 22 != 0)
            {
                htmlBuilder.Append("</tbody>");
                htmlBuilder.Append("</table>");
                htmlBuilder.Append("<div style=\"page-break-before:always;\" />");
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

        private void btnPrintCurrentCases_Click(object sender, RoutedEventArgs e)
        {
            string baseFileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString("N");

            StringBuilder htmlBuilder = new StringBuilder();
            IMultiValueConverter dateConverter = new Converters.DateConverter();
            htmlBuilder.Append(ContactTracing.Core.Common.GetHtmlHeader().ToString());

            int rowsGenerated = 0;
            bool firstPage = true;

            htmlBuilder.AppendLine("<table width=\"100%\" style=\"border: 0px; padding: 0px; margin: 0px; clear: left; width:100%; \">");
            htmlBuilder.AppendLine(" <tr style=\"border: 0px;\">");
            htmlBuilder.AppendLine("  <td width=\"50%\" style=\"border: 0px;\">");
            htmlBuilder.AppendLine("   <p style=\"font-size: 13pt; font-weight: bold; clear: left; text-decoration: underline;\">" + Properties.Settings.Default.HtmlPrintoutTitle + "</p>");
            htmlBuilder.AppendLine("   <p style=\"font-size: 13pt; font-weight: bold; clear: left; text-decoration: underline;\">" + Properties.Resources.HtmlAllCaseRecordsHeading + "</p>");

            htmlBuilder.AppendLine("  </td>");
            htmlBuilder.AppendLine(" </tr>");
            htmlBuilder.AppendLine("</table>");

            Dictionary<int, Boundry> ba = DataHelper.BoundaryAggregation;

            foreach (CaseViewModel c in DataHelper.CaseCollection)
            {
                if (rowsGenerated == 0)
                {
                    htmlBuilder.AppendLine("<table style=\"width: 1200px; border: 4px solid black;\" align=\"left\">");
                    htmlBuilder.AppendLine("<thead>");
                    htmlBuilder.AppendLine("<tr style=\"border-top: 0px solid black;\">");
                    htmlBuilder.AppendLine("<th style=\"width: 110px;\">" + Properties.Resources.ColHeaderID + "</th>");

                    if (DataHelper.IsCountryUS)
                    {
                        htmlBuilder.AppendLine("<th style=\"width: 110px;\">" + Properties.Resources.ColHeaderOriginalID + "</th>");
                    }

                    htmlBuilder.AppendLine("<th style=\"width: 110px;\">" + Properties.Resources.ColHeaderSurname + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 140px;\">" + Properties.Resources.ColHeaderOtherNames + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 110px;\">" + Properties.Resources.ColHeaderEpiCaseDef + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 15px;\">" + Properties.Resources.HTMLColHeaderSexNarrow + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 15px;\">" + Properties.Resources.HTMLColHeaderAgeNarrow + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 40px;\">" + Properties.Resources.ColHeaderOnsetDate + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 40px;\">" + Properties.Resources.ColHeaderCurrentStatus + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 40px;\">" + Properties.Resources.ColHeaderDeathDate + "</th>");

                    if (DataHelper.IsCountryUS)
                    {
                        htmlBuilder.AppendLine("<th style=\"width: 140px;\">" + Properties.Resources.Address + "</th>");
                    }
                    else
                    {
                        htmlBuilder.AppendLine("<th style=\"width: 140px;\">" + DataHelper.Adm4 + "</th>");
                        htmlBuilder.AppendLine("<th style=\"width: 140px;\">" + DataHelper.Adm2 + "</th>");
                        htmlBuilder.AppendLine("<th style=\"width: 140px;\">" + DataHelper.Adm1 + "</th>");
                    }

                    htmlBuilder.AppendLine("<th style=\"width: 40px;\">" + Properties.Resources.ColHeaderIsoAdmitted + "</th>");
                    htmlBuilder.AppendLine("<th style=\"width: 40px;\">" + Properties.Resources.ColHeaderIsoDischarge + "</th>");

                    htmlBuilder.AppendLine("</tr>");
                    htmlBuilder.AppendLine("</thead>");
                    htmlBuilder.AppendLine("<tbody>");
                }

                htmlBuilder.AppendLine("<tr style=\"border-bottom: 1px solid black; height: 32px;\">");
                //bool pastDue = (dailyCheck.ContactVM.FollowUpWindowViewModel.WindowStartDate < minDate.AddDays(-20)
                htmlBuilder.AppendLine("<td style=\"font-size: 10pt;\">" + c.ID + "</td>");

                if (DataHelper.IsCountryUS)
                {
                    htmlBuilder.AppendLine("<td style=\"font-size: 10pt;\">" + c.OriginalID + "</td>");
                }

                htmlBuilder.AppendLine("<td style=\"font-size: 10pt;\">" + Core.Common.TruncHTMLCell(c.Surname, 13) + "</td>");

                htmlBuilder.AppendLine("<td style=\"font-size: 10pt;\">" + Core.Common.TruncHTMLCell(c.OtherNames, 20) + "</td>");
                IValueConverter caseClassConverter = new Converters.EpiCaseClassificationConverter();
                htmlBuilder.AppendLine("<td style=\"font-size: 10pt;\">" + caseClassConverter.Convert(c.EpiCaseDef, null, null, null) + "</td>");
                //htmlBuilder.AppendLine("<td style=\"text-align: center;\">" + c.GenderAbbreviation + "</td>");

                if (c.Gender.Equals(Core.Enums.Gender.Male))
                {
                    htmlBuilder.AppendLine("<td style=\"text-align: center;\">" + Properties.Resources.MaleSymbol + "</td>");
                }
                else if (c.Gender.Equals(Core.Enums.Gender.Female))
                {
                    htmlBuilder.AppendLine("<td style=\"text-align: center;\">" + Properties.Resources.FemaleSymbol + "</td>");
                }
                else
                {
                    htmlBuilder.AppendLine("<td>&nbsp;</td>");
                }

                if (c.AgeYears.HasValue)
                {
                    htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + c.AgeYears + "</td>");
                }
                else
                {
                    htmlBuilder.AppendLine("<td>&nbsp;</td>");
                }

                if (c.DateOnset.HasValue)
                {
                    string[] parmsValues = { c.DateOnset.Value.ToString(), DataHelper.ApplicationCulture };
                    var dateonset = dateConverter.Convert(parmsValues, null, null, null);
                    //htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + c.DateOnset.Value.ToString("d/M/yy") + "</td>");
                    htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + dateonset + "</td>");
                }
                else
                {
                    htmlBuilder.AppendLine("<td>&nbsp;</td>");
                }

                htmlBuilder.AppendLine("<td style=\"text-align: center;\">" + c.CurrentStatus + "</td>");

                if (c.DateDeathCurrentOrFinal.HasValue)
                {
                    string[] parmsValues = { c.DateDeathCurrentOrFinal.Value.ToString(), DataHelper.ApplicationCulture };
                    var datedeath = dateConverter.Convert(parmsValues, null, null, null);
                    htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + datedeath + "</td>");
                    //htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + c.DateDeathCurrentOrFinal.Value.ToString("d/M/yy") + "</td>");

                }
                else
                {
                    htmlBuilder.AppendLine("<td>&nbsp;</td>");
                }

                if (DataHelper.IsCountryUS)
                {
                    string fullUSAddress = string.Empty;

                    fullUSAddress += ba.ContainsKey(-2) && ba[-2].CaseObjectValue(c) != "" ? ba[-2].CaseObjectValue(c) + ", " : "";
                    fullUSAddress += ba.ContainsKey(0) && ba[0].CaseObjectValue(c) != "" ? ba[0].CaseObjectValue(c) + ", " : "";
                    fullUSAddress += ba.ContainsKey(1) && ba[1].CaseObjectValue(c) != "" ? ba[1].CaseObjectValue(c) + ", " : "";
                    //fullUSAddress += ba.ContainsKey(2) && ba[2].CaseObjectValue(c) != "" ? ba[2].CaseObjectValue(c) + ", " : "";
                    fullUSAddress += ba.ContainsKey(3) && ba[3].CaseObjectValue(c) != "" ? ba[3].CaseObjectValue(c) + ", " : "";
                    fullUSAddress += ba.ContainsKey(-1) && ba[-1].CaseObjectValue(c) != "" ? ba[-1].CaseObjectValue(c) + "," : "";
                    fullUSAddress = fullUSAddress.Trim().TrimEnd(new char[] { ',' });

                    htmlBuilder.AppendLine("<td style=\"text-align: left;\">" + fullUSAddress + "</td>");
                }
                else
                {
                    htmlBuilder.AppendLine("<td style=\"text-align: center;\">" + c.Village + "</td>");
                    htmlBuilder.AppendLine("<td style=\"text-align: center;\">" + c.SubCounty + "</td>");
                    htmlBuilder.AppendLine("<td style=\"text-align: center;\">" + c.District + "</td>");
                }

                if (c.DateIsolationCurrent.HasValue)
                {
                    string[] parmsValues = { c.DateIsolationCurrent.Value.ToString(), DataHelper.ApplicationCulture };
                    var dateisolation = dateConverter.Convert(parmsValues, null, null, null);
                    //htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + c.DateIsolationCurrent.Value.ToString("d/M/yy") + "</td>");
                    htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + dateisolation + "</td>");

                }
                else
                {
                    htmlBuilder.AppendLine("<td>&nbsp;</td>");
                }

                if (c.DateDischargeIso.HasValue)
                {
                    string[] parmsValues = { c.DateDischargeIso.Value.ToString(), DataHelper.ApplicationCulture };
                    var dateisolation = dateConverter.Convert(parmsValues, null, null, null);
                    htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + dateisolation + "</td>");
                    //                    htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + c.DateDischargeIso.Value.ToString("d/M/yy") + "</td>");

                }
                else
                {
                    htmlBuilder.AppendLine("<td>&nbsp;</td>");
                }

                htmlBuilder.AppendLine("</tr>");

                rowsGenerated++;

                if (firstPage && rowsGenerated == 19)
                {
                    htmlBuilder.Append("</tbody>");
                    htmlBuilder.Append("</table>");
                    htmlBuilder.Append("<div style=\"page-break-before:always;\" />");
                    rowsGenerated = 0;
                    firstPage = false;
                }
                else if (!firstPage && rowsGenerated == 22)
                {
                    htmlBuilder.Append("</tbody>");
                    htmlBuilder.Append("</table>");
                    htmlBuilder.Append("<div style=\"page-break-before:always;\" />");
                    rowsGenerated = 0;
                }
            }

            if (firstPage && rowsGenerated % 19 != 0)
            {
                htmlBuilder.Append("</tbody>");
                htmlBuilder.Append("</table>");
                htmlBuilder.Append("<div style=\"page-break-before:always;\" />");
                rowsGenerated = 0;
                firstPage = true;
            }
            else if (!firstPage && rowsGenerated % 22 != 0)
            {
                htmlBuilder.Append("</tbody>");
                htmlBuilder.Append("</table>");
                htmlBuilder.Append("<div style=\"page-break-before:always;\" />");
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

        private void btnPrintCases_Click(object sender, RoutedEventArgs e)
        {
            if (btnPrintCases.ContextMenu != null)
            {
                btnPrintCases.ContextMenu.PlacementTarget = btnPrintCases;
                btnPrintCases.ContextMenu.IsOpen = true;
            }

            e.Handled = true;
            return;
        }

        private void btnExportContactsAnalysis_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog dlg = new System.Windows.Forms.SaveFileDialog();
            dlg.AutoUpgradeEnabled = true;
            dlg.DefaultExt = ".csv"; // Default file extension
            dlg.Filter = "Comma separated values file |*.csv"; // Filter files by extension 

            // Show save file dialog box
            System.Windows.Forms.DialogResult result = dlg.ShowDialog();

            // Process save file dialog box results 
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                // Save document 
                string fileName = dlg.FileName;

                //                bool exportResult = DataHelper.ExportContacts(dlg.FileName);
                bool exportResult = true;
                if (DataHelper.IsCountryUS)
                    DataHelper.ExportContactsForAnalysisStartforUS(dlg.FileName);
                else
                    DataHelper.ExportContactsForAnalysisStart(dlg.FileName);

                if (exportResult)
                {
                    //                    MessageBox.Show(String.Format(Properties.Resources.ExportSuccessFileWrittenTo, fileName), Properties.Resources.Success, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    //                    MessageBox.Show(Properties.Resources.ExportFailed, Properties.Resources.Fail, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }

                //try
                //{
                //    System.Threading.Thread.Sleep(500);

                //    System.Diagnostics.Process proc = new System.Diagnostics.Process();
                //    proc.StartInfo.FileName = "\"" + fileName + "\"";
                //    proc.StartInfo.UseShellExecute = true;
                //    proc.Start();
                //}
                //catch (FileNotFoundException)
                //{
                //    MessageBox.Show("The file was not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                //}
            }
        }

        private void btnImportLab_Click(object sender, RoutedEventArgs e)
        {
            if (DataHelper.GetDuplicateCasesBasedOnID().Count() > 0)
            {
                MessageBox.Show(Properties.Resources.ErrorDuplicateIDs);
            }
            else
            {
                importFromLab.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void ImportCaseSyncFile()
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.AutoUpgradeEnabled = true;
            openFileDialog.CheckFileExists = true;
            openFileDialog.DefaultExt = "sync";
            //openFileDialog.Filter = "Epi Info VHF Case Sync File|*.ecs;*.sync";
            openFileDialog.Filter = "Epi Info VHF Case Sync File|*.sync";

            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            System.Windows.Forms.DialogResult result = openFileDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                XmlDocument doc = new XmlDocument();
                doc.XmlResolver = null;
                try
                {
                    string fileName = openFileDialog.FileName;

                    if (fileName.EndsWith(".sync", StringComparison.OrdinalIgnoreCase))
                    {
                        // new code path
                        try
                        {
                            Epi.Configuration.DecryptFile(fileName, fileName + ".gz", "vQ@6L'<J3?)~5=vQnwh(2ic;>.<=dknF&/TZ4Uu!$78", "", "", 1000);
                            FileInfo fi = new FileInfo(fileName + ".gz");
                            Epi.ImportExport.ImportExportHelper.DecompressDataPackage(fi);

                            RegionEnum currentRegion = ApplicationViewModel.Instance.CurrentRegion;
                            RegionEnum syncFileRegion = RegionEnum.None;

                            using (FileStream stream = new FileStream(fileName + ".mdb", FileMode.Open, FileAccess.Read))
                            {
                                XmlReaderSettings settings = new XmlReaderSettings() { Async = false, CheckCharacters = false };

                                using (XmlReader reader = XmlReader.Create(stream, settings))
                                {
                                    while (reader.Read())
                                    {
                                        if (reader.Name.Equals("DataPackage", StringComparison.OrdinalIgnoreCase))
                                        {
                                            try
                                            {
                                                //if (reader.GetAttribute("VhfVersion").StartsWith("0.9.4.", StringComparison.OrdinalIgnoreCase)) syncFileRegion = RegionEnum.International;
                                                /*else*/
                                                if (reader.GetAttribute("Region").Equals("International", StringComparison.OrdinalIgnoreCase)) syncFileRegion = RegionEnum.International;
                                                else if (reader.GetAttribute("Region").Equals("USA", StringComparison.OrdinalIgnoreCase)) syncFileRegion = RegionEnum.USA;
                                            }
                                            catch (ArgumentOutOfRangeException)
                                            {
                                                syncFileRegion = RegionEnum.None;
                                            }
                                            catch (NullReferenceException)
                                            {
                                                syncFileRegion = RegionEnum.International;
                                            }

                                            if (syncFileRegion != currentRegion)
                                            {
                                                //throw new InvalidOperationException("The region is invalid.");
                                                MessageBox.Show("The region the selected .sync file was created with is incompatible with the current application settings.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                                return;
                                            }

                                            break;
                                        }
                                    }
                                }
                            }

                            DataHelper.SyncCaseDataStart(fileName + ".mdb");
                        }
                        catch (System.Security.Cryptography.CryptographicException ex)
                        {
                            Epi.Windows.MsgBox.ShowError(String.Format("An error was encountered while attempting to open the .sync file. The .sync file may be corrupt. The import cannot proceed. Exception: {0}", ex.Message));
                        }
                    }
                    //else
                    //{
                    //    // old code path
                    //    string compressedText = Epi.Configuration.DecryptFileToString(fileName, "vQ@6L'<J3?)~5=vQnwh(2ic;>.<=dknF&/TZ4Uu!$78", "", "", 1000);
                    //    string uncompressedText = String.Empty;
                    //    if (compressedText.StartsWith("[[EPIINFO7_VHF_CASE_SYNC_FILE__0937]]", StringComparison.OrdinalIgnoreCase))
                    //    {
                    //        uncompressedText = Epi.ImportExport.ImportExportHelper.UnZip(compressedText.Substring(37));
                    //        doc.LoadXml(uncompressedText);
                    //        DataHelper.SyncCaseDataStart(doc);
                    //    }
                    //}
                }
                catch (Exception ex)
                {
                    Epi.Windows.MsgBox.ShowException(ex);
                    return;
                }

                try
                {
                    //DataHelper.SyncCaseDataStart(doc);
                }
                catch (Exception ex)
                {
                    Epi.Windows.MsgBox.ShowException(ex);
                }
                finally
                {
                }
            }
        }

        private void btnImportCaseSyncFile_Click(object sender, RoutedEventArgs e)
        {
            ImportCaseSyncFile();
        }

        private void ForceUnlockCaseRequested(object sender, MouseButtonEventArgs e)
        {
            if (IsSuperUser)
            {
                MessageBoxResult result = MessageBox.Show("Are you sure you want to try a force unlock of this record? (Note: If this record was only locked as a result of a lock on one of its contacts, then a force unlock will not work.)", "Force unlock", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    DataHelper.SendMessageForUnlockCase(dgCases.SelectedItem as CaseViewModel);
                    MessageBox.Show("An unlock message was sent to the database. Please refresh.", "Refresh required", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void ForceUnlockContactRequested(object sender, MouseButtonEventArgs e)
        {
            if (IsSuperUser)
            {
                MessageBoxResult result = MessageBox.Show("Are you sure you want to try a force unlock of this record? (Note: If this record was only locked as a result of a lock on one of its source cases, then a force unlock will not work.)", "Force unlock", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    DataHelper.SendMessageForUnlockContact(dgAllContacts.SelectedItem as ContactViewModel);
                    MessageBox.Show("An unlock message was sent to the database. Please refresh.", "Refresh required", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void mnuFieldTypeEditor_Click(object sender, RoutedEventArgs e)
        {
            Popup = new Popup();
            Popup.Parent = grdMain;

            DistrictFieldTypeEditor fieldEditor = new DistrictFieldTypeEditor();

            fieldEditor.Closed += fieldTypeEditor_Closed;

            fieldEditor.DataContext = this.DataContext;
            fieldEditor.MaxWidth = 660;
            fieldEditor.MaxHeight = 930;

            Popup.Content = fieldEditor;
            fieldEditor.Init();
            Popup.Show();
        }

        void fieldTypeEditor_Closed(object sender, EventArgs e)
        {
            if (Popup != null && Popup.Content != null)
            {
                DistrictFieldTypeEditor form = Popup.Content as DistrictFieldTypeEditor;
                if (form != null)
                {
                    form.Closed -= fieldTypeEditor_Closed;
                }
            }
            Popup.Close();
            Popup = null;
        }

        //private void cmboCulture_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    ComboBoxItem item = cmboCulture.SelectedItem as ComboBoxItem;
        //    string newCulture = item.Content.ToString();

        //    ContactTracing.CaseView.Properties.Settings.Default.Culture = newCulture;
        //    ContactTracing.CaseView.Properties.Settings.Default.Save();

        //    MessageBoxResult result = MessageBox.Show("Culture change requires an application restart. Proceed? ", "Culture Change", MessageBoxButton.YesNo, MessageBoxImage.Information);  
        //    if (result == MessageBoxResult.Yes)
        //    {
        //        App.ChangeCulture(newCulture);    
        //    }
        //    else
        //    {
        //        cmboCulture.SelectedIndex = 0;
        //    }
        //}

        private void Print21DayFollowUp(ObservableCollection<ContactViewModel> collection, FilterSortDropdown optionsForm)
        {
            string filterClause = optionsForm.FilterClause;
            string sortClause = optionsForm.SortClause;
            bool isSortBoundyAggregate = optionsForm.IsBoundryAggregate;
            bool isInclusive = optionsForm.IsInclusive;
            int sortBoundry = 0;
            Core.Enums.LocationType locationType = (Core.Enums.LocationType)sortBoundry;

            string baseFileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString("N");

            StringBuilder htmlBuilder = new StringBuilder();

            htmlBuilder.Append(ContactTracing.Core.Common.GetHtmlHeader().ToString());

            DateTime dt = DateTime.Now;
            DateTime minDate = dt.AddDays(-1 * ContactTracing.Core.Common.DaysInWindow);

            List<ContactViewModel> contactList = new List<ContactViewModel>();
            string key = string.Empty;

            String whereClause = "FollowUpWindowViewModel != null and (FinalOutcome == null or FinalOutcome == \"\")";

            if (string.IsNullOrEmpty(filterClause) == false)
            {
                filterClause = filterClause.TrimStart();

                if (filterClause.StartsWith("and "))
                {
                    filterClause = filterClause.Substring(4);
                }
                whereClause = whereClause + " and " + filterClause;
            }

            Dictionary<int, Boundry> ba = DataHelper.BoundaryAggregation;

            if (isSortBoundyAggregate)
            {
                sortClause = FormAggregateSortExpression(optionsForm.SelectedSortKeys, sortClause);
            }
            else
            {
                if (sortClause == "")
                {
                    sortClause = ba[0].ObjectResolution;
                }
            }

            sortClause = sortClause.Replace("ContactVM.", "");
            whereClause = whereClause.Replace("ContactVM.", "");

            if (sortClause == "")
            {
                var dynGroup2 = collection.Where(whereClause);
                contactList = dynGroup2.ToList<ContactViewModel>();
            }
            else
            {
                var dynGroup2 = collection.Where(whereClause).OrderBy(sortClause);
                contactList = dynGroup2.ToList<ContactViewModel>();
            }

            int rowsGenerated = 0;
            bool firstPage = true;
            bool isStratification = true;
            bool isCovertStratification = false;
            ContactViewModel contactLast = null;

            foreach (var contact in contactList)
            {
                StratificationCheck(contact, ref contactLast, sortClause, ref isStratification, ref isCovertStratification);

                if (isStratification)
                {
                    if (htmlBuilder.ToString().TrimEnd().EndsWith("<!--end of row-->"))
                    {
                        GenerateDailyHtmlFooter(htmlBuilder, ref rowsGenerated);
                    }
                    MarkupReportHeader(filterClause, sortClause, htmlBuilder);
                    firstPage = true;
                }

                if (isStratification || isCovertStratification || rowsGenerated == 0)
                {
                    MarkupStratification(sortClause, htmlBuilder, contact);
                    MarkupGridHeader21(htmlBuilder);
                }

                MarkupGridRow21(htmlBuilder, contact);
                rowsGenerated++;

                if (firstPage && rowsGenerated == 5)
                {
                    GenerateDailyHtmlFooter(htmlBuilder, ref rowsGenerated);
                    firstPage = false;
                }
                else if (!firstPage && rowsGenerated == 7)
                {
                    GenerateDailyHtmlFooter(htmlBuilder, ref rowsGenerated);
                }
            }

            if (htmlBuilder.ToString().TrimEnd().EndsWith("<!--end of row-->"))
            {
                GenerateDailyHtmlFooter(htmlBuilder, ref rowsGenerated);
            }

            string fileName = baseFileName + ".html";

            if (contactList.Count == 0)
            {
                MarkupZeroFound(filterClause, sortClause, htmlBuilder);
            }

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

        bool shouldEnableNewOutbreak;

        private void checkControlsStatus()
        {

            shouldEnableNewOutbreak = radioRegion1.IsChecked == true || cmboCulture.SelectedItem != null;


            double opacity = 0;

            if (shouldEnableNewOutbreak)
            {


                opacity = 1;

                pathPlusNewOutbreak.Opacity = opacity;
                pathCircleNewOutbreak.Opacity = opacity;
                textBlockNewOutbreak.Opacity = opacity;

                pathPlusNewOutbreak.IsEnabled = shouldEnableNewOutbreak;
                pathCircleNewOutbreak.IsEnabled = shouldEnableNewOutbreak;
                textBlockNewOutbreak.IsEnabled = shouldEnableNewOutbreak;


                //pathPlusNewOutbreak.Visibility = System.Windows.Visibility.Visible;
                //pathCircleNewOutbreak.Visibility = System.Windows.Visibility.Visible;
                //textBlockNewOutbreak.Visibility = System.Windows.Visibility.Visible;

                fileScreen.IsEnabled = true;


            }
            else
            {

                opacity = .2;

                pathPlusNewOutbreak.Opacity = opacity;
                pathCircleNewOutbreak.Opacity = opacity;
                textBlockNewOutbreak.Opacity = opacity;


                pathPlusNewOutbreak.IsEnabled = shouldEnableNewOutbreak;
                pathCircleNewOutbreak.IsEnabled = shouldEnableNewOutbreak;
                textBlockNewOutbreak.IsEnabled = shouldEnableNewOutbreak;


                //pathPlusNewOutbreak.Visibility = System.Windows.Visibility.Collapsed;
                //pathCircleNewOutbreak.Visibility = System.Windows.Visibility.Collapsed;
                //textBlockNewOutbreak.Visibility = System.Windows.Visibility.Collapsed;

                fileScreen.IsEnabled = false;


            }
        }


        private void PrintDailyFollowUp(FilterSortDropdown optionsForm)
        {
            ObservableCollection<DailyCheckViewModel> collection = (ObservableCollection<DailyCheckViewModel>)optionsForm.Collection;
            string filterClause = optionsForm.FilterClause;
            string sortClause = optionsForm.SortClause;
            bool isSortBoundyAggregate = optionsForm.IsBoundryAggregate;
            bool isInclusive = optionsForm.IsInclusive;
            int sortBoundry = 0;
            Core.Enums.LocationType locationType = (Core.Enums.LocationType)sortBoundry;

            string baseFileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString("N");

            DateTime? originalDate = dpPrev.SelectedDate;
            StringBuilder htmlBuilder = new StringBuilder();

            htmlBuilder.Append(ContactTracing.Core.Common.GetHtmlHeader().ToString());

            DateTime dt = DateTime.Now;
            DateTime minDate = dt.AddDays(-1 * ContactTracing.Core.Common.DaysInWindow);

            if (collection == DataHelper.PrevFollowUpCollection)
            {
                if (!dpPrev.SelectedDate.HasValue)
                {
                    return;
                }
                minDate = dpPrev.SelectedDate.Value;
            }

            Dictionary<int, Boundry> ba = DataHelper.BoundaryAggregation;

            if (isSortBoundyAggregate)
            {
                sortClause = FormAggregateSortExpression(optionsForm.SelectedSortKeys, sortClause);
            }
            else
            {
                if (sortClause == "")
                {
                    sortClause = ba[0].ObjectResolution;
                }
                else
                {
                    sortClause = sortClause + "," + ba[0].ObjectResolution;
                }
            }

            List<DailyCheckViewModel> dcvmList = GetDailyCheckList(collection, minDate, filterClause, sortClause);

            int indexDate = Core.Common.DaysInWindow - 1;

            if (isInclusive)
            {
                DateTime incDate = minDate.AddDays(-600);
                while (incDate < DateTime.Today)
                {
                    incDate = incDate.AddDays(1);

                    DataHelper.ShowContactsForDateforFollowup.Execute(incDate);

                    var previousList = GetPrevCheckList(indexDate, minDate, filterClause, sortClause);

                    if (previousList != null)
                    {
                        foreach (var previous in previousList)
                        {
                            bool found = false;
                            foreach (DailyCheckViewModel dcVM in dcvmList)
                            {
                                if (dcVM.ContactVM == previous.ContactVM)
                                {
                                    found = true;
                                }
                            }

                            if (!found)
                            {
                                dcvmList.Add(previous);
                            }
                        }
                    }
                }

                var dynGroup2 = dcvmList.OrderBy(sortClause);
                dcvmList = dynGroup2.ToList<DailyCheckViewModel>();
            }

            dpPrev.SelectedDate = originalDate;

            int rowsGenerated = 0;
            bool firstPage = true;
            bool isStratification = true;
            bool isCovertStratification = false;
            DailyCheckViewModel dcvmLast = null;

            foreach (DailyCheckViewModel dcvm in dcvmList)
            {
                StratificationCheck(dcvm, ref dcvmLast, sortClause, ref isStratification, ref isCovertStratification);

                if (isStratification)
                {
                    if (htmlBuilder.ToString().TrimEnd().EndsWith("<!--end of row-->"))
                    {
                        GenerateDailyHtmlFooter(htmlBuilder, ref rowsGenerated);
                    }

                    MarkupReportHeader(filterClause, sortClause, htmlBuilder, collection);
                    firstPage = true;
                }

                if (isStratification || isCovertStratification || rowsGenerated == 0)
                {
                    MarkupStratification(sortClause, htmlBuilder, dcvm);
                    MarkupGridHeader(htmlBuilder);
                }

                MarkupGridRow(collection, htmlBuilder, indexDate, dcvm);
                rowsGenerated++;

                if (firstPage && rowsGenerated == 17)
                {
                    GenerateDailyHtmlFooter(htmlBuilder, ref rowsGenerated);
                    firstPage = false;
                }
                else if (!firstPage && rowsGenerated == 23)
                {
                    GenerateDailyHtmlFooter(htmlBuilder, ref rowsGenerated);
                }
            }

            if (htmlBuilder.ToString().TrimEnd().EndsWith("<!--end of row-->"))
            {
                GenerateDailyHtmlFooter(htmlBuilder, ref rowsGenerated);
            }

            if (collection == DataHelper.PrevFollowUpCollection)
            {
                DataHelper.ShowContactsForDateforFollowup.Execute(dpPrev.SelectedDate);
            }

            string fileName = baseFileName + ".html";

            if (dcvmList.Count == 0)
            {
                MarkupZeroFound(filterClause, sortClause, htmlBuilder);
            }

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

        private void cmboRegion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            pathPlusNewOutbreak.IsEnabled = true;
            pathCircleNewOutbreak.IsEnabled = true;
            textBlockNewOutbreak.IsEnabled = true;

            pathPlusNewOutbreak.Opacity = 1;
            pathCircleNewOutbreak.Opacity = 1;
            textBlockNewOutbreak.Opacity = 1;
        }

        private void cmboCulture_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AntipatternCultureInfo item = cmboCulture.SelectedItem as AntipatternCultureInfo;
            string newCulture = item.Name.ToLower();

            //MessageBoxResult result;

            if (newCulture == Thread.CurrentThread.CurrentCulture.Name.ToLower())
            {
                return;
            }

            //if (ApplicationViewModel.Instance.CultureLanguageDictionary.ContainsKey(Thread.CurrentThread.CurrentCulture.Name.ToLower()) &&
            //     ApplicationViewModel.Instance.CultureLanguageDictionary.ContainsKey(item.CultureInfo.Name.ToLower()))
            //{

            //    MessageBoxResult mResult = MessageBox.Show(
            //        string.Format(
            //           "You are about to change the application language \n" +
            //           "to  " +
            //           "{0}   \n" +
            //           "This change requires a restart of the application.  \n " +
            //           "Would you like to proceed? \n \n" +
            //           "On clicking the 'Yes' button, the application screen will refresh and you will be able to view the application in {0}. ",
            //            /*ApplicationViewModel.Instance.CultureLanguageDictionary[Thread.CurrentThread.CurrentCulture.Name.ToLower()],*/
            //              ApplicationViewModel.Instance.CultureLanguageDictionary[item.CultureInfo.Name.ToLower()]),
            //           "Culture Change",
            //           MessageBoxButton.YesNo, MessageBoxImage.Information);
            //    result = mResult;
            //}
            //else
            //{

            //    MessageBoxResult mResult = MessageBox.Show("This change requires an application restart.  Continue?", "Culture Change",
            //       MessageBoxButton.YesNo, MessageBoxImage.Information);
            //    result = mResult;
            //}


            //if (result == MessageBoxResult.Yes)
            //{

            ContactTracing.CaseView.Properties.Settings.Default.Culture = newCulture;
            ContactTracing.CaseView.Properties.Settings.Default.Save();

            App.ChangeCulture(newCulture);
            //}
            //else
            //{
            //    cmboCulture.SelectionChanged -= cmboCulture_SelectionChanged;
            //    cmboCulture.SelectedIndex = -1;
            //    cmboCulture.SelectionChanged += cmboCulture_SelectionChanged;
            //}
        }



        private void radioRegion1_Checked(object sender, RoutedEventArgs e)
        {

            checkControlsStatus();
            // Setter raises RegionChanged Event that other components should listen for    
            ApplicationViewModel.Instance.CurrentRegion = RegionEnum.USA;


            // epiDataHelper.ApplicationRegion = "USA";
            ContactTracing.CaseView.Properties.Settings.Default.Region = RegionEnum.USA;
            ContactTracing.CaseView.Properties.Settings.Default.Save();

            ContactTracing.CaseView.Properties.Settings.Default.Culture = "en";
            ContactTracing.CaseView.Properties.Settings.Default.Save();



            App.ChangeCulture("en");


            if (txtSelectLanguage != null)
            {
                txtSelectLanguage.Visibility = System.Windows.Visibility.Collapsed;
                cmboCulture.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void radioRegion2_Checked(object sender, RoutedEventArgs e)
        {

            checkControlsStatus();
            // Setter raises RegionChanged Event that other components should listen for            
            ApplicationViewModel.Instance.CurrentRegion = RegionEnum.International;

            // epiDataHelper.ApplicationRegion = "International";
            ContactTracing.CaseView.Properties.Settings.Default.Region = RegionEnum.International;
            ContactTracing.CaseView.Properties.Settings.Default.Save();

            if (txtSelectLanguage != null)
            {
                txtSelectLanguage.Visibility = System.Windows.Visibility.Visible;
                cmboCulture.Visibility = System.Windows.Visibility.Visible;
            }

        }



    }
}