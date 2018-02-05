using System;
using System.Collections.Generic;
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
using ContactTracing.Controls;
using ContactTracing.Core;
using ContactTracing.Core.Data;
using ContactTracing.CaseView.Controls;
using ContactTracing.ViewModel;

namespace ContactTracing.CaseView.Controls.Tabs
{
    /// <summary>
    /// Interaction logic for AnalysisTab.xaml
    /// </summary>
    public partial class AnalysisTab : UserControl
    {
        private double _height = 0;

        //private readonly FileInfo PROJECT_FILE_INFO = new FileInfo(@"Projects\UgandaVHFOutbreak\UgandaVHFOutbreak.prj");
        private Popup Popup { get; set; }

        public double SvHeight
        {
            get
            {
                return this._height;
            }
            set
            {
                this._height = value - 20;
                sv.Height = SvHeight;
            }
        }

        public AnalysisTab()
        {
            InitializeComponent();
            //tblockDate.Text = DateTime.Today.ToString("MMMM d, yyyy");
        }

        private void RefreshCustomAnalysis()
        {
            try
            {
                foreach (MenuItem item in mnuCustomAnalysis.Items)
                {
                    item.Click -= item_Click;
                }

                mnuCustomAnalysis.Items.Clear();

                mnuCustomAnalysis.Visibility = System.Windows.Visibility.Collapsed;

                string[] filePaths = Directory.GetFiles(@"Projects" + System.IO.Path.DirectorySeparatorChar.ToString() + "VHF" + System.IO.Path.DirectorySeparatorChar.ToString() + "Canvases", "vhf_*.cvs7",
                                             SearchOption.TopDirectoryOnly);

                foreach (string filePath in filePaths)
                {
                    MenuItem item = new MenuItem();
                    item.Tag = filePath;
                    item.Header = filePath;
                    item.Click += item_Click;
                    mnuCustomAnalysis.Items.Add(item);
                }

                if (mnuCustomAnalysis.Items.Count > 0)
                {
                    mnuCustomAnalysis.Visibility = System.Windows.Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was a problem loading the custom analysis canvas paths. This feature has been disabled. Exception: " + ex.Message, "Problem", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (mnuCustomAnalysis.Items.Count > 0)
                {
                    mnuCustomAnalysis.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    mnuCustomAnalysis.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
        }

        private void RefreshCountries()
        {
            try
            {
                foreach (MenuItem item in cmenuCountries.Items)
                {
                    item.Click -= countryItem_Click;
                }

                cmenuCountries.Items.Clear();

                cmenuCountries.Visibility = System.Windows.Visibility.Collapsed;

                List<string> countries = new List<string>();

                EpiDataHelper dataHelper = this.DataContext as EpiDataHelper;
                if (dataHelper != null)
                {
                    foreach (CaseViewModel caseVM in dataHelper.CaseCollection)
                    {
                        if (!countries.Contains(caseVM.Country) && !String.IsNullOrEmpty(caseVM.Country.Trim()) &&
                            (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed ||
                            caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable ||
                            caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Suspect))
                        {
                            countries.Add(caseVM.Country);
                        }
                    }

                    //string[] countries = null; // get countries // Directory.GetFiles(@"Projects\VHF\Canvases", "vhf_*.cvs7",
                    //SearchOption.TopDirectoryOnly);

                    foreach (string country in countries)
                    {
                        MenuItem item = new MenuItem();
                        item.Tag = country;
                        item.Header = country;
                        item.Click += countryItem_Click;
                        cmenuCountries.Items.Add(item);
                    }

                    if (mnuCustomAnalysis.Items.Count > 0)
                    {
                        cmenuCountries.Visibility = System.Windows.Visibility.Visible;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was a problem. Exception: " + ex.Message, "Problem", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (cmenuCountries.Items.Count > 0)
                {
                    cmenuCountries.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    cmenuCountries.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
        }

        void item_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender != null)
                {
                    MenuItem item = sender as MenuItem;
                    if (item != null && item.Tag != null)
                    {
                        string fileName = item.Tag.ToString();

                        EpiDataHelper dataHelper = this.DataContext as EpiDataHelper;
                        if (dataHelper != null && dataHelper.Project != null && dataHelper.CaseForm != null)
                        {
                            Project project = dataHelper.Project;
                            View caseForm = project.Views[ContactTracing.Core.Constants.CASE_FORM_NAME];

                            System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
                            string commandText = System.IO.Path.GetDirectoryName(a.Location) + "\\AnalysisDashboard.exe";

                            XmlDocument doc = new XmlDocument();
                            doc.XmlResolver = null;
                            doc.Load(fileName);
                            XmlNode node = doc.SelectSingleNode("DashboardCanvas/dashboardHelper/projectPath");
                            node.InnerText = System.IO.Path.GetDirectoryName(a.Location) + "\\Projects\\VHF\\" + dataHelper.Project.Name + ".prj";

                            doc.Save(fileName);

                            System.Diagnostics.Process proc = new System.Diagnostics.Process();
                            proc.StartInfo.FileName = commandText;
                            proc.StartInfo.Arguments = string.Format("\"{0}\"", fileName);
                            proc.StartInfo.UseShellExecute = true;
                            proc.Start();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was a problem loading the selected custom analysis. Exception: " + ex.Message, "Problem", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            }
        }

        void countryItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender != null)
                {
                    MenuItem item = sender as MenuItem;
                    if (item != null && item.Tag != null)
                    {
                        string countryName = item.Tag.ToString();

                        Popup = new Popup();
                        FrameworkElement element = gridAnalysis.Parent as FrameworkElement;

                        if (element != null)
                        {
                            Popup.Parent = element.Parent as FrameworkElement;

                            if (Popup.Parent != null)
                            {
                                AnalysisCountryViewer analysisCountryViewer = new AnalysisCountryViewer(countryName);

                                analysisCountryViewer.Closed += analysisCountryViewer_Closed;

                                analysisCountryViewer.DataContext = this.DataContext;
                                analysisCountryViewer.MaxWidth = 990;

                                Popup.Content = analysisCountryViewer;
                                Popup.Show();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was a problem loading the selected custom analysis. Exception: " + ex.Message, "Problem", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            }
        }

        public void Compute()
        {
            analysisEpiClassAllPatients.Compute();
            labClassAllPatients.Compute();
            //  dailyStats.Compute();
            patientTestedInfo.Compute();
            lastIsoInfo.Compute();
            confirmedProbableTable.Compute();
            finalOutcomeTable.Compute();
            symptomsTable.Compute();
            ageGroupChart.Compute();
            ageTable.Compute();
            residenceTable.Compute();
            onsetLocationTable.Compute();
            finalOutcomeContactsTable.Compute();
            epiCurve.Compute();
            epiCurveSuspect.Compute();
            RefreshCustomAnalysis();
            RefreshCountries();
        }

        private void btnFreeAnalysis_Click(object sender, RoutedEventArgs e)
        {
            if (btnFreeAnalysis.ContextMenu != null)
            {
                btnFreeAnalysis.ContextMenu.PlacementTarget = btnFreeAnalysis;
                btnFreeAnalysis.ContextMenu.IsOpen = true;
            }

            e.Handled = true;
            return;
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (Popup != null && Popup.Content != null)
            {
                return;
            }
            //PrintDialog dialog = new PrintDialog();
            //if (dialog.ShowDialog() == true)
            //{
            Popup = new Popup();
            FrameworkElement element = gridAnalysis.Parent as FrameworkElement;
            if (element != null)
            {
                Popup.Parent = element.Parent as FrameworkElement;

                if (Popup.Parent != null)
                {
                    AnalysisViewer analysisViewer = new AnalysisViewer();

                    analysisViewer.Closed += analysisViewer_Closed;

                    analysisViewer.DataContext = this.DataContext;
                    analysisViewer.MaxWidth = 990;
                    analysisViewer.Compute();

                    Popup.Content = analysisViewer;
                    Popup.Show();
                }
            }
            //DocumentPaginator paginator = new AnalysisPaginator(sp);
            //dialog.PrintDocument(paginator, "Analysis"); 
            //}
        }

        void analysisViewer_Closed(object sender, EventArgs e)
        {
            if (Popup != null && Popup.Content != null)
            {
                AnalysisViewer form = Popup.Content as AnalysisViewer;
                if (form != null)
                {
                    form.Closed -= analysisViewer_Closed;
                }
            }
            //if (Popup.Content is AnalysisViewer)
            //{
            //    (Popup.Content as AnalysisViewer).Closed -= analysisViewer_Closed;
            //}
            Popup.Close();
            Popup = null;
        }

        void analysisCountryViewer_Closed(object sender, EventArgs e)
        {
            if (Popup != null && Popup.Content != null)
            {
                AnalysisCountryViewer form = Popup.Content as AnalysisCountryViewer;
                if (form != null)
                {
                    form.Closed -= analysisCountryViewer_Closed;
                }
            }
            //if (Popup.Content is AnalysisCountryViewer)
            //{
            //    (Popup.Content as AnalysisCountryViewer).Closed -= analysisCountryViewer_Closed;
            //}
            Popup.Close();
            Popup = null;
        }

        private void btnFreeFormCases_Click(object sender, RoutedEventArgs e)
        {
            //Project project = new Project(PROJECT_FILE_INFO.FullName);
            EpiDataHelper dataHelper = this.DataContext as EpiDataHelper;
            if (dataHelper != null && dataHelper.Project != null && dataHelper.CaseForm != null)
            {
                Project project = dataHelper.Project;
                View caseForm = project.Views[ContactTracing.Core.Constants.CASE_FORM_NAME];

                System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
                string commandText = System.IO.Path.GetDirectoryName(a.Location) + "\\AnalysisDashboard.exe";

                XmlDocument doc = new XmlDocument();
                doc.XmlResolver = null;

                try
                {
                doc.Load(System.IO.Path.GetDirectoryName(a.Location) + "\\Projects\\VHF\\Canvases\\Canvas_Case.cvs7");
                XmlNode node = doc.SelectSingleNode("DashboardCanvas/dashboardHelper/projectPath");
                node.InnerText = System.IO.Path.GetDirectoryName(a.Location) + "\\Projects\\VHF\\" + dataHelper.Project.Name + ".prj";
                doc.Save(System.IO.Path.GetDirectoryName(a.Location) + "\\Projects\\VHF\\Canvases\\Canvas_Case.cvs7");

                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = commandText;
                proc.StartInfo.Arguments = string.Format("\"{0}\"", "Projects\\VHF\\Canvases\\Canvas_Case.cvs7");
                proc.StartInfo.UseShellExecute = true;
                proc.Start();
                }
                catch (FileNotFoundException)
                {
                    MessageBox.Show("The canvas file for this analysis type could not be found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("There was a problem generating the analysis output. Exception: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnFreeFormCasesLab_Click(object sender, RoutedEventArgs e)
        {
            //Project project = new Project(PROJECT_FILE_INFO.FullName);
            EpiDataHelper dataHelper = this.DataContext as EpiDataHelper;
            if (dataHelper != null && dataHelper.Project != null && dataHelper.CaseForm != null)
            {
                Project project = dataHelper.Project;
                View caseForm = project.Views[ContactTracing.Core.Constants.CASE_FORM_NAME];

                System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
                string commandText = System.IO.Path.GetDirectoryName(a.Location) + "\\AnalysisDashboard.exe";

                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = commandText;

                XmlDocument doc = new XmlDocument();
                doc.XmlResolver = null;

                try
                {
                    doc.Load(System.IO.Path.GetDirectoryName(a.Location) + "\\Projects\\VHF\\Canvases\\Canvas_CaseLab.cvs7");

                    XmlNode node = doc.SelectSingleNode("DashboardCanvas/dashboardHelper/projectPath");
                    node.InnerText = System.IO.Path.GetDirectoryName(a.Location) + "\\Projects\\VHF\\" + dataHelper.Project.Name + ".prj";

                    node = doc.SelectSingleNode("DashboardCanvas/dashboardHelper/relatedDataConnections/relatedDataConnection/projectPath");
                    node.InnerText = System.IO.Path.GetDirectoryName(a.Location) + "\\Projects\\VHF\\" + dataHelper.Project.Name + ".prj";

                    doc.Save(System.IO.Path.GetDirectoryName(a.Location) + "\\Projects\\VHF\\Canvases\\Canvas_CaseLab.cvs7");

                    proc.StartInfo.Arguments = string.Format("\"{0}\"", "Projects\\VHF\\Canvases\\Canvas_CaseLab.cvs7");
                    proc.StartInfo.UseShellExecute = true;
                    proc.Start();
                }
                catch (FileNotFoundException)
                {
                    MessageBox.Show("The canvas file for this analysis type could not be found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("There was a problem generating the analysis output. Exception: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnFreeFormContacts_Click(object sender, RoutedEventArgs e)
        {
            //Project project = new Project(PROJECT_FILE_INFO.FullName);
            EpiDataHelper dataHelper = this.DataContext as EpiDataHelper;
            if (dataHelper != null && dataHelper.Project != null && dataHelper.CaseForm != null)
            {
                Project project = dataHelper.Project;
                View caseForm = project.Views[ContactTracing.Core.Constants.CASE_FORM_NAME];

                System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
                string commandText = System.IO.Path.GetDirectoryName(a.Location) + "\\AnalysisDashboard.exe";

                XmlDocument doc = new XmlDocument();
                doc.XmlResolver = null;

                try
                {
                    doc.Load(System.IO.Path.GetDirectoryName(a.Location) + "\\Projects\\VHF\\Canvases\\Canvas_Contact.cvs7");
                    XmlNode node = doc.SelectSingleNode("DashboardCanvas/dashboardHelper/projectPath");
                    node.InnerText = System.IO.Path.GetDirectoryName(a.Location) + "\\Projects\\VHF\\" + dataHelper.Project.Name + ".prj";
                    doc.Save(System.IO.Path.GetDirectoryName(a.Location) + "\\Projects\\VHF\\Canvases\\Canvas_Contact.cvs7");

                    System.Diagnostics.Process proc = new System.Diagnostics.Process();
                    proc.StartInfo.FileName = commandText;
                    proc.StartInfo.Arguments = string.Format("\"{0}\"", "Projects\\VHF\\Canvases\\Canvas_Contact.cvs7");
                    proc.StartInfo.UseShellExecute = true;
                    proc.Start();
                }
                catch (FileNotFoundException)
                {
                    MessageBox.Show("The canvas file for this analysis type could not be found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("There was a problem generating the analysis output. Exception: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnFreeFormCasesCP_Click(object sender, RoutedEventArgs e)
        {
            EpiDataHelper dataHelper = this.DataContext as EpiDataHelper;
            if (dataHelper != null && dataHelper.Project != null && dataHelper.CaseForm != null)
            {
                Project project = dataHelper.Project;
                View caseForm = project.Views[ContactTracing.Core.Constants.CASE_FORM_NAME];

                System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
                string commandText = System.IO.Path.GetDirectoryName(a.Location) + "\\AnalysisDashboard.exe";

                XmlDocument doc = new XmlDocument();
                doc.XmlResolver = null;

                try
                {
                    doc.Load(System.IO.Path.GetDirectoryName(a.Location) + "\\Projects\\VHF\\Canvases\\Canvas_CaseCP.cvs7");
                    XmlNode node = doc.SelectSingleNode("DashboardCanvas/dashboardHelper/projectPath");
                    node.InnerText = System.IO.Path.GetDirectoryName(a.Location) + "\\Projects\\VHF\\" + dataHelper.Project.Name + ".prj";
                    doc.Save(System.IO.Path.GetDirectoryName(a.Location) + "\\Projects\\VHF\\Canvases\\Canvas_CaseCP.cvs7");

                    System.Diagnostics.Process proc = new System.Diagnostics.Process();
                    proc.StartInfo.FileName = commandText;
                    proc.StartInfo.Arguments = string.Format("\"{0}\"", "Projects\\VHF\\Canvases\\Canvas_CaseCP.cvs7");
                    proc.StartInfo.UseShellExecute = true;
                    proc.Start();
                }
                catch (FileNotFoundException)
                {
                    MessageBox.Show("The canvas file for this analysis type could not be found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("There was a problem generating the analysis output. Exception: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnFreeFormCasesLabCPS_Click(object sender, RoutedEventArgs e)
        {
            EpiDataHelper dataHelper = this.DataContext as EpiDataHelper;
            if (dataHelper != null && dataHelper.Project != null && dataHelper.CaseForm != null)
            {

                string Canvas_CaseCPSName;

                if (ApplicationViewModel.Instance.CurrentRegion == Core.Enums.RegionEnum.USA)
                {
                    Canvas_CaseCPSName = "Canvas_CaseCPS_US.cvs7";
                }
                else
                {
                    Canvas_CaseCPSName = "Canvas_CaseCPS.cvs7";
                }

                //  string Canvas_CaseCPSName = "Canvas_CaseCPS.cvs7";

                Project project = dataHelper.Project;
                View caseForm = project.Views[ContactTracing.Core.Constants.CASE_FORM_NAME];

                System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
                string commandText = System.IO.Path.GetDirectoryName(a.Location) + "\\AnalysisDashboard.exe";

                XmlDocument doc = new XmlDocument();
                doc.XmlResolver = null;
                try
                {
                    doc.Load(System.IO.Path.GetDirectoryName(a.Location) + "\\Projects\\VHF\\Canvases\\" + Canvas_CaseCPSName);
                    XmlNode node = doc.SelectSingleNode("DashboardCanvas/dashboardHelper/projectPath");
                    node.InnerText = System.IO.Path.GetDirectoryName(a.Location) + "\\Projects\\VHF\\" + dataHelper.Project.Name + ".prj";
                    doc.Save(System.IO.Path.GetDirectoryName(a.Location) + "\\Projects\\VHF\\Canvases\\" + Canvas_CaseCPSName);

                    System.Diagnostics.Process proc = new System.Diagnostics.Process();
                    proc.StartInfo.FileName = commandText;
                    proc.StartInfo.Arguments = string.Format("\"{0}\"", "Projects\\VHF\\Canvases\\" + Canvas_CaseCPSName);
                    proc.StartInfo.UseShellExecute = true;
                    proc.Start();
                }
                catch (FileNotFoundException)
                {
                    MessageBox.Show("The canvas file for this analysis type could not be found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("There was a problem generating the analysis output. Exception: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnFreeFormAllPatients_Click(object sender, RoutedEventArgs e)
        {
            EpiDataHelper dataHelper = this.DataContext as EpiDataHelper;
            if (dataHelper != null && dataHelper.Project != null && dataHelper.CaseForm != null)
            {
                Project project = dataHelper.Project;
                View caseForm = project.Views[ContactTracing.Core.Constants.CASE_FORM_NAME];

                System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
                string commandText = System.IO.Path.GetDirectoryName(a.Location) + "\\AnalysisDashboard.exe";

                XmlDocument doc = new XmlDocument();
                doc.XmlResolver = null;

                try
                {
                    doc.Load(System.IO.Path.GetDirectoryName(a.Location) + "\\Projects\\VHF\\Canvases\\Canvas_Case.cvs7");
                    XmlNode node = doc.SelectSingleNode("DashboardCanvas/dashboardHelper/projectPath");
                    node.InnerText = System.IO.Path.GetDirectoryName(a.Location) + "\\Projects\\VHF\\" + dataHelper.Project.Name + ".prj";
                    doc.Save(System.IO.Path.GetDirectoryName(a.Location) + "\\Projects\\VHF\\Canvases\\Canvas_Case.cvs7");

                    System.Diagnostics.Process proc = new System.Diagnostics.Process();
                    proc.StartInfo.FileName = commandText;
                    proc.StartInfo.Arguments = string.Format("\"{0}\"", "Projects\\VHF\\Canvases\\Canvas_Case.cvs7");
                    proc.StartInfo.UseShellExecute = true;
                    proc.Start();
                }
                catch (FileNotFoundException)
                {
                    MessageBox.Show("The canvas file for this analysis type could not be found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("There was a problem generating the analysis output. Exception: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        //private void btnPrintSitrep_Click(object sender, RoutedEventArgs e)
        //{
        //    Popup = new Popup();
        //    Popup.Parent = (gridAnalysis.Parent as FrameworkElement).Parent as FrameworkElement;

        //    GuineaSitrepViewer analysisCountryViewer = new GuineaSitrepViewer();

        //    analysisCountryViewer.Closed += analysisCountryViewer_Closed;

        //    analysisCountryViewer.DataContext = this.DataContext;
        //    //analysisCountryViewer.Compute();
        //    analysisCountryViewer.MaxWidth = 990;

        //    Popup.Content = analysisCountryViewer;
        //    Popup.Show();
        //}

        private void btnPrintCountries_Click(object sender, RoutedEventArgs e)
        {
            if (btnPrintCountries.ContextMenu != null)
            {
                btnPrintCountries.ContextMenu.PlacementTarget = btnPrintCountries;
                btnPrintCountries.ContextMenu.IsOpen = true;
            }

            e.Handled = true;
            return;

            //Popup = new Popup();
            //Popup.Parent = (gridAnalysis.Parent as FrameworkElement).Parent as FrameworkElement;

            //AnalysisCountryViewer analysisCountryViewer = new AnalysisCountryViewer();

            //analysisCountryViewer.Closed += analysisCountryViewer_Closed;

            //analysisCountryViewer.DataContext = this.DataContext;
            ////analysisCountryViewer.Compute();
            //analysisCountryViewer.MaxWidth = 990;

            //Popup.Content = analysisCountryViewer;
            //Popup.Show();
        }

        //private void btnPrintSITREP_Click(object sender, RoutedEventArgs e)
        //{
        //    if (btnPrintSitrep.ContextMenu != null)
        //    {
        //        btnPrintSitrep.ContextMenu.PlacementTarget = btnPrintSitrep;
        //        btnPrintSitrep.ContextMenu.IsOpen = true;
        //    }

        //    e.Handled = true;
        //    return;
        //}

        //private void mnuSitrepA_Click(object sender, RoutedEventArgs e)
        //{
        //    Popup = new Popup();
        //    FrameworkElement element = gridAnalysis.Parent as FrameworkElement;
        //    if (element != null)
        //    {
        //        Popup.Parent = element.Parent as FrameworkElement;

        //        if (Popup.Parent != null)
        //        {
        //            EpiDataHelper dataHelper = this.DataContext as EpiDataHelper;
        //            SitrepA sitrepA = new SitrepA(dataHelper, dataHelper.Country, DateTime.Today);

        //            sitrepA.Closed += sitrepA_Closed;

        //            sitrepA.DataContext = this.DataContext;
        //            sitrepA.MaxWidth = 990;
        //            sitrepA.Compute();
        //            //analysisViewer.Compute();

        //            Popup.Content = sitrepA;
        //            Popup.Show();
        //        }
        //        else
        //        {
        //            Popup = null;
        //        }
        //    }
        //}

        //void sitrepA_Closed(object sender, EventArgs e)
        //{
        //    if (Popup != null && Popup.Content != null)
        //    {
        //        SitrepA form = Popup.Content as SitrepA;
        //        if (form != null)
        //        {
        //            form.Closed -= sitrepA_Closed;
        //        }
        //    }
        //    //if (Popup.Content is SitrepA)
        //    //{
        //    //    (Popup.Content as SitrepA).Closed -= sitrepA_Closed;
        //    //}
        //    Popup.Close();
        //    Popup = null;
        //}


        //private void mnuSitrepB_Click(object sender, RoutedEventArgs e)
        //{
        //    Popup = new Popup();
        //    FrameworkElement element = gridAnalysis.Parent as FrameworkElement;
        //    if (element != null)
        //    {
        //        Popup.Parent = element.Parent as FrameworkElement;

        //        if (Popup.Parent != null)
        //        {
        //            EpiDataHelper dataHelper = this.DataContext as EpiDataHelper;
        //            SitrepB sitrepB = new SitrepB();

        //            sitrepB.Closed += sitrepB_Closed;

        //            sitrepB.DataContext = this.DataContext;
        //            sitrepB.MaxWidth = 990;
        //            sitrepB.Compute();
        //            //analysisViewer.Compute();

        //            Popup.Content = sitrepB;
        //            Popup.Show();
        //        }
        //        else
        //        {
        //            Popup = null;
        //        }
        //    }
        //}

        //void sitrepB_Closed(object sender, EventArgs e)
        //{
        //    if (Popup != null && Popup.Content != null)
        //    {
        //        SitrepB form = Popup.Content as SitrepB;
        //        if (form != null)
        //        {
        //            form.Closed -= sitrepB_Closed;
        //        }
        //    }
        //    Popup.Close();
        //    Popup = null;
        //}
    }
}
