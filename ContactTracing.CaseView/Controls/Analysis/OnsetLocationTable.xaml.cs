using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using ContactTracing.Core;
using ContactTracing.ViewModel;

namespace ContactTracing.CaseView.Controls.Analysis
{
    /// <summary>
    /// Interaction logic for OnsetLocationTable.xaml
    /// </summary>
    public partial class OnsetLocationTable : AnalysisOutputBase
    {
        private delegate void SetHeaderGridTextHandler();
        private delegate void SetDistrictGridTextHandler(string districtName, double count, int row);
        private delegate void SetCountyGridTextHandler(string countyName, double count, int row);

        private EpiDataHelper DataHelper
        {
            get
            {
                return (this.DataContext as EpiDataHelper);
            }
        }

        public void Compute()
        {
            grdMain.Children.Clear();

            BackgroundWorker computeWorker = new BackgroundWorker();
            computeWorker.DoWork += new DoWorkEventHandler(computeWorker_DoWork);
            computeWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(computeWorker_RunWorkerCompleted);
            computeWorker.RunWorkerAsync(this.DataHelper);
        }

        public OnsetLocationTable()
        {
            InitializeComponent();
        }

        private void SetHeaderGridText()
        {
            grdMain.RowDefinitions.Add(new RowDefinition());

            Border border1 = new Border();
            border1.Style = this.Resources["borderColumnHeaderStyle"] as Style;
            Grid.SetColumn(border1, 0);

            Border border2 = new Border();
            border2.Style = this.Resources["borderColumnHeaderStyle"] as Style;
            Grid.SetColumn(border2, 1);

            Border border3 = new Border();
            border3.Style = this.Resources["borderColumnHeaderStyle"] as Style;
            Grid.SetColumn(border3, 2);

            Border border4 = new Border();
            border4.Style = this.Resources["borderColumnHeaderStyle"] as Style;
            Grid.SetColumn(border4, 3);

            TextBlock tblock1 = new TextBlock();
            tblock1.Style = this.Resources["styleHeader"] as Style;
            tblock1.Text = Properties.Resources.ColHeaderCountry;
            Grid.SetColumn(tblock1, 0);

            TextBlock tblock2 = new TextBlock();
            tblock2.Style = this.Resources["styleHeader"] as Style;
            tblock2.Text = DataHelper.Adm1;
            Grid.SetColumn(tblock2, 1);

            TextBlock tblock3 = new TextBlock();
            tblock3.Style = this.Resources["styleHeader"] as Style;
            tblock3.Text = DataHelper.Adm2;
            Grid.SetColumn(tblock3, 2);

            TextBlock tblock4 = new TextBlock();
            tblock4.Style = this.Resources["styleHeader"] as Style;
            tblock4.Text = Properties.Resources.Count;
            Grid.SetColumn(tblock4, 3);

            grdMain.Children.Add(border1);
            grdMain.Children.Add(border2);
            grdMain.Children.Add(border3);
            grdMain.Children.Add(border4);

            grdMain.Children.Add(tblock1);
            grdMain.Children.Add(tblock2);
            grdMain.Children.Add(tblock3);
            grdMain.Children.Add(tblock4);
        }

        private void SetDistrictGridText(string district, double count, int row)
        {
            grdMain.RowDefinitions.Add(new RowDefinition());

            Border border1 = new Border();
            border1.Style = this.Resources["borderColumnHeaderStyle"] as Style;
            Grid.SetColumn(border1, 0);
            Grid.SetRow(border1, row);

            Border border2 = new Border();
            border2.Style = this.Resources["borderColumnHeaderStyle"] as Style;
            Grid.SetColumn(border2, 1);
            Grid.SetRow(border2, row);

            Border border3 = new Border();
            border3.Style = this.Resources["borderColumnHeaderStyle"] as Style;
            Grid.SetColumn(border3, 2);
            Grid.SetRow(border3, row);

            Border border4 = new Border();
            border4.Style = this.Resources["borderCellStyle"] as Style;
            Grid.SetColumn(border4, 3);
            Grid.SetRow(border4, row);

            TextBlock tblock1 = new TextBlock();
            tblock1.Style = this.Resources["styleHeader"] as Style;
            tblock1.Text = String.Empty;
            Grid.SetColumn(tblock1, 0);
            Grid.SetRow(tblock1, row);

            TextBlock tblock2 = new TextBlock();
            tblock2.Style = this.Resources["styleHeader"] as Style;
            tblock2.Text = district;
            Grid.SetColumn(tblock2, 1);
            Grid.SetRow(tblock2, row);

            TextBlock tblock3 = new TextBlock();
            tblock3.Style = this.Resources["styleHeader"] as Style;
            tblock3.Text = String.Empty;
            Grid.SetColumn(tblock3, 2);
            Grid.SetRow(tblock3, row);

            TextBlock tblock4 = new TextBlock();
            tblock4.Style = this.Resources["styleBody"] as Style;
            tblock4.Text = count.ToString();
            Grid.SetColumn(tblock4, 3);
            Grid.SetRow(tblock4, row);

            grdMain.Children.Add(border1);
            grdMain.Children.Add(border2);
            grdMain.Children.Add(border3);
            grdMain.Children.Add(border4);

            grdMain.Children.Add(tblock1);
            grdMain.Children.Add(tblock2);
            grdMain.Children.Add(tblock3);
            grdMain.Children.Add(tblock4);
        }

        private void SetCountyGridText(string county, double count, int row)
        {
            grdMain.RowDefinitions.Add(new RowDefinition());

            Border border1 = new Border();
            border1.Style = this.Resources["borderColumnHeaderStyle"] as Style;
            Grid.SetColumn(border1, 0);
            Grid.SetRow(border1, row);

            Border border2 = new Border();
            border2.Style = this.Resources["borderColumnHeaderStyle"] as Style;
            Grid.SetColumn(border2, 1);
            Grid.SetRow(border2, row);

            Border border3 = new Border();
            border3.Style = this.Resources["borderColumnHeaderStyle"] as Style;
            Grid.SetColumn(border3, 2);
            Grid.SetRow(border3, row);

            Border border4 = new Border();
            border4.Style = this.Resources["borderCellStyle"] as Style;
            Grid.SetColumn(border4, 3);
            Grid.SetRow(border4, row);

            TextBlock tblock1 = new TextBlock();
            tblock1.Style = this.Resources["styleHeader"] as Style;
            tblock1.Text = String.Empty;
            Grid.SetColumn(tblock1, 0);
            Grid.SetRow(tblock1, row);

            TextBlock tblock2 = new TextBlock();
            tblock2.Style = this.Resources["styleHeader"] as Style;
            tblock2.Text = String.Empty;
            Grid.SetColumn(tblock2, 1);
            Grid.SetRow(tblock2, row);

            TextBlock tblock3 = new TextBlock();
            tblock3.Style = this.Resources["styleHeader"] as Style;
            tblock3.Text = county;
            Grid.SetColumn(tblock3, 2);
            Grid.SetRow(tblock3, row);

            TextBlock tblock4 = new TextBlock();
            tblock4.Style = this.Resources["styleBody"] as Style;
            tblock4.Text = count.ToString();
            Grid.SetColumn(tblock4, 3);
            Grid.SetRow(tblock4, row);

            grdMain.Children.Add(border1);
            grdMain.Children.Add(border2);
            grdMain.Children.Add(border3);
            grdMain.Children.Add(border4);

            grdMain.Children.Add(tblock1);
            grdMain.Children.Add(tblock2);
            grdMain.Children.Add(tblock3);
            grdMain.Children.Add(tblock4);
        }

        private void SetCountryGridText(string country, double count, int row)
        {
            grdMain.RowDefinitions.Add(new RowDefinition());

            Border border1 = new Border();
            border1.Style = this.Resources["borderColumnHeaderStyle"] as Style;
            Grid.SetColumn(border1, 0);
            Grid.SetRow(border1, row);

            Border border2 = new Border();
            border2.Style = this.Resources["borderColumnHeaderStyle"] as Style;
            Grid.SetColumn(border2, 1);
            Grid.SetRow(border2, row);

            Border border3 = new Border();
            border3.Style = this.Resources["borderColumnHeaderStyle"] as Style;
            Grid.SetColumn(border3, 2);
            Grid.SetRow(border3, row);

            Border border4 = new Border();
            border4.Style = this.Resources["borderCellStyle"] as Style;
            Grid.SetColumn(border4, 3);
            Grid.SetRow(border4, row);

            TextBlock tblock1 = new TextBlock();
            tblock1.Style = this.Resources["styleHeader"] as Style;
            tblock1.Text = country;
            Grid.SetColumn(tblock1, 0);
            Grid.SetRow(tblock1, row);

            TextBlock tblock2 = new TextBlock();
            tblock2.Style = this.Resources["styleHeader"] as Style;
            tblock2.Text = String.Empty;
            Grid.SetColumn(tblock2, 1);
            Grid.SetRow(tblock2, row);

            TextBlock tblock3 = new TextBlock();
            tblock3.Style = this.Resources["styleHeader"] as Style;
            tblock3.Text = String.Empty;
            Grid.SetColumn(tblock3, 2);
            Grid.SetRow(tblock3, row);

            TextBlock tblock4 = new TextBlock();
            tblock4.Style = this.Resources["styleBody"] as Style;
            tblock4.Text = count.ToString();
            Grid.SetColumn(tblock4, 3);
            Grid.SetRow(tblock4, row);

            grdMain.Children.Add(border1);
            grdMain.Children.Add(border2);
            grdMain.Children.Add(border3);
            grdMain.Children.Add(border4);

            grdMain.Children.Add(tblock1);
            grdMain.Children.Add(tblock2);
            grdMain.Children.Add(tblock3);
            grdMain.Children.Add(tblock4);
        }

        void computeWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
        }

        void computeWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            this.Dispatcher.BeginInvoke(new SetHeaderGridTextHandler(SetHeaderGridText));

            EpiDataHelper DataHelper = e.Argument as EpiDataHelper;

            if (DataHelper != null)
            {

                List<string> countries = new List<string>();

                foreach (CaseViewModel caseVM in DataHelper.CaseCollection)
                {
                    if (!String.IsNullOrEmpty(caseVM.CountryOnset) && !countries.Contains(caseVM.CountryOnset) && (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed || caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable))
                    {
                        countries.Add(caseVM.CountryOnset);
                    }
                }

                int row = 1;
                foreach (string country in countries)
                {
                    SortedDictionary<string, List<string>> districtDictionary = new SortedDictionary<string, List<string>>();

                    double count = (from caseVM in DataHelper.CaseCollection
                                    where caseVM.CountryOnset == country && !String.IsNullOrEmpty(caseVM.CountryOnset) &&
                                    (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed || caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable)
                                    select caseVM).Count();

                    this.Dispatcher.BeginInvoke(new SetDistrictGridTextHandler(SetCountryGridText), country, count, row);
                    row++;

                    foreach (CaseViewModel caseVM in DataHelper.CaseCollection)
                    {
                        if (caseVM.CountryOnset == country)
                        {
                            if (!districtDictionary.ContainsKey(caseVM.DistrictOnset) && !String.IsNullOrEmpty(caseVM.DistrictOnset))
                            {
                                if (!String.IsNullOrEmpty(caseVM.SubCountyOnset))
                                {
                                    List<string> tc = new List<string>() { caseVM.SubCountyOnset };
                                    districtDictionary.Add(caseVM.DistrictOnset, tc);
                                }
                                else
                                {
                                    districtDictionary.Add(caseVM.DistrictOnset, new List<string>());
                                }
                            }
                            else if (!String.IsNullOrEmpty(caseVM.DistrictOnset))
                            {
                                List<string> tc = districtDictionary[caseVM.DistrictOnset];
                                if (!tc.Contains(caseVM.SubCountyOnset) && !String.IsNullOrEmpty(caseVM.SubCountyOnset))
                                {
                                    districtDictionary[caseVM.DistrictOnset].Add(caseVM.SubCountyOnset);
                                }
                            }
                        }
                    }

                    foreach (KeyValuePair<string, List<string>> kvp in districtDictionary)
                    {
                        string district = kvp.Key;
                        List<string> counties = kvp.Value;

                        double districtCount = (from caseVM in DataHelper.CaseCollection
                                                where caseVM.DistrictOnset == district && !String.IsNullOrEmpty(caseVM.DistrictOnset) && caseVM.CountryOnset.Equals(country) &&
                                                (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed || caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable)
                                                select caseVM).Count();

                        this.Dispatcher.BeginInvoke(new SetDistrictGridTextHandler(SetDistrictGridText), district, districtCount, row);
                        row++;

                        foreach (string county in counties)
                        {
                            double countyCount = (from caseVM in DataHelper.CaseCollection
                                                  where caseVM.DistrictOnset == district && !String.IsNullOrEmpty(caseVM.DistrictOnset) && caseVM.SubCountyOnset == county && !String.IsNullOrEmpty(caseVM.SubCountyOnset) && caseVM.CountryOnset.Equals(country) &&
                                                  (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed || caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable)
                                                  select caseVM).Count();

                            this.Dispatcher.BeginInvoke(new SetCountyGridTextHandler(SetCountyGridText), county, countyCount, row);
                            row++;
                        }
                    }
                }
            }
        }
    }
}
