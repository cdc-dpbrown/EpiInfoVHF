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

namespace ContactTracing.CaseView.Controls.Analysis
{
    /// <summary>
    /// Interaction logic for CountryDistrictResTables.xaml
    /// </summary>
    public partial class CountryDistrictResTables : UserControl
    {
        public static readonly DependencyProperty StartIndexProperty = DependencyProperty.Register("StartIndex", typeof(int), typeof(CountryDistrictResTables), new PropertyMetadata(0));
        public static readonly DependencyProperty EndIndexProperty = DependencyProperty.Register("EndIndex", typeof(int), typeof(CountryDistrictResTables), new PropertyMetadata(4));

        public int StartIndex
        {
            get
            {
                return (int)(this.GetValue(StartIndexProperty));
            }
            set
            {
                this.SetValue(StartIndexProperty, value);
            }
        }

        public int EndIndex
        {
            get
            {
                return (int)(this.GetValue(EndIndexProperty));
            }
            set
            {
                this.SetValue(EndIndexProperty, value);
            }
        }

        public CountryDistrictResTables()
        {
            InitializeComponent();
        }

        private EpiDataHelper DataHelper
        {
            get
            {
                return this.DataContext as EpiDataHelper;
            }
        }

        private DateTime _displayDate = DateTime.Now;

        public DateTime DisplayDate
        {
            get
            {
                return _displayDate; // (DateTime)(this.GetValue(DisplayDateProperty));
            }
            set
            {
                this._displayDate = value; //this.SetValue(DisplayDateProperty, value);
                object element = this.FindName("tblockCurrentDate");
                if (element != null)
                {
                    TextBlock t = element as TextBlock;
                    if (t != null)
                    {
                        t.Text = value.ToString("dd/MM/yyyy HH:mm");
                    }
                }
            }
        }

        public void Compute(string countryName)
        {
            if (String.IsNullOrEmpty(countryName) || this.DataContext == null)
            {
                return;
            }

            EpiDataHelper dataHelper = this.DataContext as EpiDataHelper;

            if (dataHelper != null)
            {
                txtCountryHeading.Text = countryName;
                txtCountryHeading.FontSize = txtCountryHeading.FontSize + 5;

                // GRAND TOTAL
                var query = from caseVM in dataHelper.CaseCollection
                            where caseVM.Country == countryName &&
                            (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed ||
                            caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable ||
                            caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Suspect ||
                            caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.PUI)
                            select caseVM;

                int grandtotal = query.Count();

                // CONFIRMED TOTAL
                query = from caseVM in dataHelper.CaseCollection
                        where caseVM.Country == countryName &&
                            (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed)
                        select caseVM;

                int confirmedtotal = query.Count();

                // PROBABLE TOTAL
                query = from caseVM in dataHelper.CaseCollection
                        where caseVM.Country == countryName &&
                        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable)
                        select caseVM;

                int probabletotal = query.Count();

                // SUSPECT TOTAL
                query = from caseVM in dataHelper.CaseCollection
                        where caseVM.Country == countryName &&
                        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Suspect)
                        select caseVM;
               
                int suspecttotal = query.Count();

                // PUI TOTAL    
                query = from caseVM in dataHelper.CaseCollection
                        where caseVM.Country == countryName &&
                        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.PUI)
                        select caseVM;

                int puiTotal = query.Count();


                // CONFIRMED ALIVE
                query = from caseVM in dataHelper.CaseCollection
                        where/*caseVM.Country == countryName &&*/
                        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed) &&
                        (caseVM.CurrentStatus == Properties.Resources.Alive)
                        select caseVM;

                int confirmedAliveTotal = query.Count();

                // PROBABLE ALIVE
                query = from caseVM in dataHelper.CaseCollection
                        where caseVM.Country == countryName &&
                        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable) &&
                        (caseVM.CurrentStatus == Properties.Resources.Alive)
                        select caseVM;

                int probableAliveTotal = query.Count();

                // SUSPECT ALIVE
                query = from caseVM in dataHelper.CaseCollection
                        where caseVM.Country == countryName &&
                        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Suspect) &&
                        (caseVM.CurrentStatus == Properties.Resources.Alive)
                        select caseVM;

                int suspectAliveTotal = query.Count();

                // PUI ALIVE    
                query = from caseVM in dataHelper.CaseCollection
                        where caseVM.Country == countryName &&
                        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.PUI) &&
                        (caseVM.CurrentStatus == Properties.Resources.Alive)
                        select caseVM;

                int puiAliveTotal = query.Count();

                // CONFIRMED DEAD
                query = from caseVM in dataHelper.CaseCollection
                        where caseVM.Country == countryName &&
                        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed) &&
                        (caseVM.CurrentStatus == Properties.Resources.Dead)
                        select caseVM;

                int confirmedDeadTotal = query.Count();

                // PROBABLE DEAD
                query = from caseVM in dataHelper.CaseCollection
                        where caseVM.Country == countryName &&
                        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable) &&
                        (caseVM.CurrentStatus == Properties.Resources.Dead)
                        select caseVM;

                int probableDeadTotal = query.Count();

                // SUSPECT DEAD
                query = from caseVM in dataHelper.CaseCollection
                        where caseVM.Country == countryName &&
                        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Suspect) &&
                        (caseVM.CurrentStatus == Properties.Resources.Dead)
                        select caseVM;

                int suspectDeadTotal = query.Count();

                //  PUI  DEAD    
                query = from caseVM in dataHelper.CaseCollection
                        where caseVM.Country == countryName &&
                        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.PUI) &&
                        (caseVM.CurrentStatus == Properties.Resources.Dead)
                        select caseVM;

                int puiDeadTotal = query.Count();





                // CONFIRMED UNK
                query = from caseVM in dataHelper.CaseCollection
                        where caseVM.Country == countryName &&
                        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed) &&
                        (caseVM.CurrentStatus == String.Empty)
                        select caseVM;

                int confirmedUnkTotal = query.Count();

                // PROBABLE UNK
                query = from caseVM in dataHelper.CaseCollection
                        where caseVM.Country == countryName &&
                        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable) &&
                        (caseVM.CurrentStatus == String.Empty)
                        select caseVM;

                int probableUnkTotal = query.Count();

                // SUSPECT UNK
                query = from caseVM in dataHelper.CaseCollection
                        where caseVM.Country == countryName &&
                        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Suspect) &&
                        (caseVM.CurrentStatus == String.Empty)
                        select caseVM;

                int suspectUnkTotal = query.Count();

                //  PUI UNK     
                query = from caseVM in dataHelper.CaseCollection
                        where caseVM.Country == countryName &&
                        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.PUI) &&
                        (caseVM.CurrentStatus == String.Empty)
                        select caseVM;

                int puiUnkTotal = query.Count();



                #region Grid and Borders
                Grid gridCountry = new Grid();
                gridCountry.RowDefinitions.Add(new RowDefinition());
                gridCountry.RowDefinitions.Add(new RowDefinition());
                gridCountry.RowDefinitions.Add(new RowDefinition());
                gridCountry.RowDefinitions.Add(new RowDefinition());
                gridCountry.RowDefinitions.Add(new RowDefinition());
                gridCountry.RowDefinitions.Add(new RowDefinition());
                gridCountry.ColumnDefinitions.Add(new ColumnDefinition());
                gridCountry.ColumnDefinitions.Add(new ColumnDefinition());
                gridCountry.ColumnDefinitions.Add(new ColumnDefinition());
                gridCountry.ColumnDefinitions.Add(new ColumnDefinition());
                gridCountry.ColumnDefinitions.Add(new ColumnDefinition());

                gridCountry.ColumnDefinitions[0].Width = new GridLength(240);
                gridCountry.ColumnDefinitions[1].Width = new GridLength(80);
                gridCountry.ColumnDefinitions[2].Width = new GridLength(80);
                gridCountry.ColumnDefinitions[3].Width = new GridLength(190);
                gridCountry.ColumnDefinitions[4].Width = new GridLength(90);

                if (ContactTracing.Core.ApplicationViewModel.Instance.CurrentRegion == Core.Enums.RegionEnum.International)
                {
                    gridCountry.RowDefinitions[4].Height = new GridLength(0);
                }

                Border border1 = new Border();
                border1.Style = this.Resources["borderColumnHeaderStyle"] as Style;
                Grid.SetColumn(border1, 0);
                gridCountry.Children.Add(border1);

                Border border2 = new Border();
                border2.Style = this.Resources["borderColumnHeaderStyle"] as Style;
                Grid.SetColumn(border2, 1);
                gridCountry.Children.Add(border2);

                Border border3 = new Border();
                border3.Style = this.Resources["borderColumnHeaderStyle"] as Style;
                Grid.SetColumn(border3, 2);
                gridCountry.Children.Add(border3);

                Border border4 = new Border();
                border4.Style = this.Resources["borderColumnHeaderStyle"] as Style;
                Grid.SetColumn(border4, 3);
                gridCountry.Children.Add(border4);

                Border border5 = new Border();
                border5.Style = this.Resources["borderColumnHeaderStyle"] as Style;
                Grid.SetColumn(border5, 4);
                gridCountry.Children.Add(border5);




                Border border1c = new Border();
                border1c.Style = this.Resources["borderRowHeaderStyle"] as Style;
                Grid.SetColumn(border1c, 0);
                Grid.SetRow(border1c, 1);
                gridCountry.Children.Add(border1c);

                Border border2c = new Border();
                border2c.Style = this.Resources["borderCellAltStyle"] as Style;
                Grid.SetColumn(border2c, 1);
                Grid.SetRow(border2c, 1);
                gridCountry.Children.Add(border2c);

                Border border3c = new Border();
                border3c.Style = this.Resources["borderCellAltStyle"] as Style;
                Grid.SetColumn(border3c, 2);
                Grid.SetRow(border3c, 1);
                gridCountry.Children.Add(border3c);

                Border border4c = new Border();
                border4c.Style = this.Resources["borderCellAltStyle"] as Style;
                Grid.SetColumn(border4c, 3);
                Grid.SetRow(border4c, 1);
                gridCountry.Children.Add(border4c);

                Border border5c = new Border();
                border5c.Style = this.Resources["borderCellAltStyle"] as Style;
                Grid.SetColumn(border5c, 4);
                Grid.SetRow(border5c, 1);
                gridCountry.Children.Add(border5c);



                Border border1p = new Border();
                border1p.Style = this.Resources["borderRowHeaderStyle"] as Style;
                Grid.SetColumn(border1p, 0);
                Grid.SetRow(border1p, 2);
                gridCountry.Children.Add(border1p);

                Border border2p = new Border();
                border2p.Style = this.Resources["borderCellAltStyle"] as Style;
                Grid.SetColumn(border2p, 1);
                Grid.SetRow(border2p, 2);
                gridCountry.Children.Add(border2p);

                Border border3p = new Border();
                border3p.Style = this.Resources["borderCellAltStyle"] as Style;
                Grid.SetColumn(border3p, 2);
                Grid.SetRow(border3p, 2);
                gridCountry.Children.Add(border3p);

                Border border4p = new Border();
                border4p.Style = this.Resources["borderCellAltStyle"] as Style;
                Grid.SetColumn(border4p, 3);
                Grid.SetRow(border4p, 2);
                gridCountry.Children.Add(border4p);

                Border border5p = new Border();
                border5p.Style = this.Resources["borderCellAltStyle"] as Style;
                Grid.SetColumn(border5p, 4);
                Grid.SetRow(border5p, 2);
                gridCountry.Children.Add(border5p);




                Border border1t = new Border();
                border1t.Style = this.Resources["borderRowHeaderStyle"] as Style;
                Grid.SetColumn(border1t, 0);
                Grid.SetRow(border1t, 3);
                gridCountry.Children.Add(border1t);

                Border border2t = new Border();
                border2t.Style = this.Resources["borderCellAltStyle"] as Style;
                Grid.SetColumn(border2t, 1);
                Grid.SetRow(border2t, 3);
                gridCountry.Children.Add(border2t);

                Border border3t = new Border();
                border3t.Style = this.Resources["borderCellAltStyle"] as Style;
                Grid.SetColumn(border3t, 2);
                Grid.SetRow(border3t, 3);
                gridCountry.Children.Add(border3t);

                Border border4t = new Border();
                border4t.Style = this.Resources["borderCellAltStyle"] as Style;
                Grid.SetColumn(border4t, 3);
                Grid.SetRow(border4t, 3);
                gridCountry.Children.Add(border4t);

                Border border5t = new Border();
                border5t.Style = this.Resources["borderCellAltStyle"] as Style;
                Grid.SetColumn(border5t, 4);
                Grid.SetRow(border5t, 3);
                gridCountry.Children.Add(border5t);



                Border border1s = new Border();
                border1s.Style = this.Resources["borderRowHeaderStyle"] as Style;
                Grid.SetColumn(border1s, 0);
                Grid.SetRow(border1s, 4);
                gridCountry.Children.Add(border1s);

                Border border2s = new Border();
                border2s.Style = this.Resources["borderCellAltStyle"] as Style;
                Grid.SetColumn(border2s, 1);
                Grid.SetRow(border2s, 4);
                gridCountry.Children.Add(border2s);

                Border border3s = new Border();
                border3s.Style = this.Resources["borderCellAltStyle"] as Style;
                Grid.SetColumn(border3s, 2);
                Grid.SetRow(border3s, 4);
                gridCountry.Children.Add(border3s);

                Border border4s = new Border();
                border4s.Style = this.Resources["borderCellAltStyle"] as Style;
                Grid.SetColumn(border4s, 3);
                Grid.SetRow(border4s, 4);
                gridCountry.Children.Add(border4s);

                Border border5s = new Border();
                border5s.Style = this.Resources["borderCellAltStyle"] as Style;
                Grid.SetColumn(border5s, 4);
                Grid.SetRow(border5s, 4);
                gridCountry.Children.Add(border5s);



                Border border1pui = new Border();
                border1pui.Style = this.Resources["borderRowHeaderStyle"] as Style;
                Grid.SetColumn(border1pui, 0);
                Grid.SetRow(border1pui, 5);
                gridCountry.Children.Add(border1pui);

                Border border2pui = new Border();
                border2pui.Style = this.Resources["borderCellStyle"] as Style;
                Grid.SetColumn(border2pui, 1);
                Grid.SetRow(border2pui, 5);
                gridCountry.Children.Add(border2pui);

                Border border3pui = new Border();
                border3pui.Style = this.Resources["borderCellStyle"] as Style;
                Grid.SetColumn(border3pui, 2);
                Grid.SetRow(border3pui, 5);
                gridCountry.Children.Add(border3pui);

                Border border4pui = new Border();
                border4pui.Style = this.Resources["borderCellStyle"] as Style;
                Grid.SetColumn(border4pui, 3);
                Grid.SetRow(border4pui, 5);
                gridCountry.Children.Add(border4pui);

                Border border5pui = new Border();
                border5pui.Style = this.Resources["borderCellStyle"] as Style;
                Grid.SetColumn(border5pui, 4);
                Grid.SetRow(border5pui, 5);
                gridCountry.Children.Add(border5pui);



                #endregion // Grid and Borders




                TextBlock tblock1 = new TextBlock();
                tblock1.Text = Properties.Resources.AnalysisEpiClassification;
                tblock1.Style = this.Resources["styleHeader"] as Style;
                Grid.SetRow(tblock1, 0);
                Grid.SetColumn(tblock1, 0);
                gridCountry.Children.Add(tblock1);

                TextBlock tblock2 = new TextBlock();
                tblock2.Text = Properties.Resources.Count;
                tblock2.Style = this.Resources["styleHeader"] as Style;
                Grid.SetRow(tblock2, 0);
                Grid.SetColumn(tblock2, 1);
                gridCountry.Children.Add(tblock2);

                TextBlock tblock3 = new TextBlock();
                tblock3.Text = Properties.Resources.Alive;
                tblock3.Style = this.Resources["styleHeader"] as Style;
                Grid.SetRow(tblock3, 0);
                Grid.SetColumn(tblock3, 3);
                gridCountry.Children.Add(tblock3);

                TextBlock tblock4 = new TextBlock();
                tblock4.Text = Properties.Resources.Dead;
                tblock4.Style = this.Resources["styleHeader"] as Style;
                Grid.SetRow(tblock4, 0);
                Grid.SetColumn(tblock4, 2);
                gridCountry.Children.Add(tblock4);

                TextBlock tblock5 = new TextBlock();
                tblock5.Text = Properties.Resources.AliveDeadUnknown;
                tblock5.Style = this.Resources["styleHeader"] as Style;
                Grid.SetRow(tblock5, 0);
                Grid.SetColumn(tblock5, 4);
                gridCountry.Children.Add(tblock5);








                //  CONFIRMED ROW START  ====================================================
                TextBlock tblock1c = new TextBlock();
                tblock1c.Text = Properties.Resources.Confirmed;
                tblock1c.Style = this.Resources["styleRowHeader"] as Style;
                Grid.SetRow(tblock1c, 1);
                Grid.SetColumn(tblock1c, 0);
                gridCountry.Children.Add(tblock1c);

                TextBlock tblock2c = new TextBlock();
                tblock2c.Text = confirmedtotal.ToString();
                tblock2c.Style = this.Resources["styleBody"] as Style;
                Grid.SetRow(tblock2c, 1);
                Grid.SetColumn(tblock2c, 1);
                gridCountry.Children.Add(tblock2c);

                TextBlock tblock3c = new TextBlock();
                tblock3c.Text = confirmedAliveTotal.ToString() + " (" + ((double)(confirmedAliveTotal) / (double)(confirmedtotal)).ToString("P1") + ")";
                tblock3c.Style = this.Resources["styleBody"] as Style;
                Grid.SetRow(tblock3c, 1);
                Grid.SetColumn(tblock3c, 3);
                gridCountry.Children.Add(tblock3c);

                TextBlock tblock4c = new TextBlock();
                tblock4c.Text = confirmedDeadTotal.ToString() + " (" + ((double)(confirmedDeadTotal) / (double)(confirmedtotal)).ToString("P1") + ")";
                tblock4c.Style = this.Resources["styleBody"] as Style;
                Grid.SetRow(tblock4c, 1);
                Grid.SetColumn(tblock4c, 2);
                gridCountry.Children.Add(tblock4c);

                TextBlock tblock5c = new TextBlock();
                tblock5c.Text = confirmedUnkTotal.ToString() + " (" + ((double)(confirmedUnkTotal) / (double)(confirmedtotal)).ToString("P1") + ")";
                tblock5c.Style = this.Resources["styleBody"] as Style;
                Grid.SetRow(tblock5c, 1);
                Grid.SetColumn(tblock5c, 4);
                gridCountry.Children.Add(tblock5c);













                //  Probable   ROW START  ====================================================
                TextBlock tblock1p = new TextBlock();
                tblock1p.Text = Properties.Resources.Probable;
                tblock1p.Style = this.Resources["styleRowHeader"] as Style;
                Grid.SetRow(tblock1p, 2);
                Grid.SetColumn(tblock1p, 0);
                gridCountry.Children.Add(tblock1p);

                TextBlock tblock2p = new TextBlock();
                tblock2p.Text = probabletotal.ToString();
                tblock2p.Style = this.Resources["styleBody"] as Style;
                Grid.SetRow(tblock2p, 2);
                Grid.SetColumn(tblock2p, 1);
                gridCountry.Children.Add(tblock2p);

                TextBlock tblock3p = new TextBlock();
                tblock3p.Text = probableAliveTotal.ToString() + " (" + ((double)(probableAliveTotal) / (double)(probabletotal)).ToString("P1") + ")" + " " + Properties.Resources.UnderInvestigation + "";
                tblock3p.Style = this.Resources["styleBody"] as Style;
                Grid.SetRow(tblock3p, 2);
                Grid.SetColumn(tblock3p, 3);
                gridCountry.Children.Add(tblock3p);

                TextBlock tblock4p = new TextBlock();
                tblock4p.Text = probableDeadTotal.ToString() + " (" + ((double)(probableDeadTotal) / (double)(probabletotal)).ToString("P1") + ")";
                tblock4p.Style = this.Resources["styleBody"] as Style;
                Grid.SetRow(tblock4p, 2);
                Grid.SetColumn(tblock4p, 2);
                gridCountry.Children.Add(tblock4p);

                TextBlock tblock5p = new TextBlock();
                tblock5p.Text = probableUnkTotal.ToString() + " (" + ((double)(probableUnkTotal) / (double)(probabletotal)).ToString("P1") + ")";
                tblock5p.Style = this.Resources["styleBody"] as Style;
                Grid.SetRow(tblock5p, 2);
                Grid.SetColumn(tblock5p, 4);
                gridCountry.Children.Add(tblock5p);


















                //  SUSPECT  ROW START  ====================================================    
                TextBlock tblock1s = new TextBlock();
                tblock1s.Text = Properties.Resources.Suspect;
                tblock1s.Style = this.Resources["styleRowHeader"] as Style;
                Grid.SetRow(tblock1s, 3);
                Grid.SetColumn(tblock1s, 0);
                gridCountry.Children.Add(tblock1s);

                TextBlock tblock2s = new TextBlock();
                tblock2s.Text = suspecttotal.ToString();
                tblock2s.Style = this.Resources["styleBody"] as Style;
                Grid.SetRow(tblock2s, 3);
                Grid.SetColumn(tblock2s, 1);
                gridCountry.Children.Add(tblock2s);

                TextBlock tblock3s = new TextBlock();
                tblock3s.Text = suspectAliveTotal.ToString() + " (" + ((double)(suspectAliveTotal) / (double)(suspecttotal)).ToString("P1") + ")" + " " + Properties.Resources.UnderInvestigation + "";
                tblock3s.Style = this.Resources["styleBody"] as Style;
                Grid.SetRow(tblock3s, 3);
                Grid.SetColumn(tblock3s, 3);
                gridCountry.Children.Add(tblock3s);

                TextBlock tblock4s = new TextBlock();
                tblock4s.Text = suspectDeadTotal.ToString() + " (" + ((double)(suspectDeadTotal) / (double)(suspecttotal)).ToString("P1") + ")";
                tblock4s.Style = this.Resources["styleBody"] as Style;
                Grid.SetRow(tblock4s, 3);
                Grid.SetColumn(tblock4s, 2);
                gridCountry.Children.Add(tblock4s);

                TextBlock tblock5s = new TextBlock();
                tblock5s.Text = suspectUnkTotal.ToString() + " (" + ((double)(suspectUnkTotal) / (double)(suspecttotal)).ToString("P1") + ")";
                tblock5s.Style = this.Resources["styleBody"] as Style;
                Grid.SetRow(tblock5s, 3);
                Grid.SetColumn(tblock5s, 4);
                gridCountry.Children.Add(tblock5s);


                //  PUI    ROW START  ====================================================    
                TextBlock tblock1pui = new TextBlock();
                tblock1pui.Text = Properties.Resources.PUI;
                tblock1pui.Style = this.Resources["styleRowHeader"] as Style;
                Grid.SetRow(tblock1pui, 4);
                Grid.SetColumn(tblock1pui, 0);
                gridCountry.Children.Add(tblock1pui);

                TextBlock tblock2pui = new TextBlock();
                tblock2pui.Text = puiTotal.ToString();
                tblock2pui.Style = this.Resources["styleBody"] as Style;
                Grid.SetRow(tblock2pui, 4);
                Grid.SetColumn(tblock2pui, 1);
                gridCountry.Children.Add(tblock2pui);

                TextBlock tblock3pui = new TextBlock();
                tblock3pui.Text = puiAliveTotal.ToString() + " (" + ((double)(puiAliveTotal) / (double)(puiTotal)).ToString("P1") + ")" + " " + Properties.Resources.UnderInvestigation + "";
                tblock3pui.Style = this.Resources["styleBody"] as Style;
                Grid.SetRow(tblock3pui, 4);
                Grid.SetColumn(tblock3pui, 3);
                gridCountry.Children.Add(tblock3pui);

                TextBlock tblock4pui = new TextBlock();
                tblock4pui.Text = puiDeadTotal.ToString() + " (" + ((double)(puiDeadTotal) / (double)(puiTotal)).ToString("P1") + ")";
                tblock4pui.Style = this.Resources["styleBody"] as Style;
                Grid.SetRow(tblock4pui, 4);
                Grid.SetColumn(tblock4pui, 2);
                gridCountry.Children.Add(tblock4pui);

                TextBlock tblock5pui = new TextBlock();
                tblock5pui.Text = puiUnkTotal.ToString() + " (" + ((double)(puiUnkTotal) / (double)(puiTotal)).ToString("P1") + ")";
                tblock5pui.Style = this.Resources["styleBody"] as Style;
                Grid.SetRow(tblock5pui, 4);
                Grid.SetColumn(tblock5pui, 4);
                gridCountry.Children.Add(tblock5pui);



                //  TOTALS  ROW START  ====================================================
                TextBlock tblock1t = new TextBlock();
                tblock1t.Text = "Total";
                tblock1t.Style = this.Resources["styleRowHeader"] as Style;
                tblock1t.FontWeight = FontWeights.Bold;
                Grid.SetRow(tblock1t, 5);
                Grid.SetColumn(tblock1t, 0);
                gridCountry.Children.Add(tblock1t);


                TextBlock tblock2t = new TextBlock();
                tblock2t.Text = (confirmedtotal + probabletotal + suspecttotal + puiTotal      ).ToString();
                tblock2t.Style = this.Resources["styleBody"] as Style;
                tblock2t.FontWeight = FontWeights.Bold;
                Grid.SetRow(tblock2t, 5);
                Grid.SetColumn(tblock2t, 1);
                gridCountry.Children.Add(tblock2t);

                TextBlock tblock3t = new TextBlock();
                tblock3t.Text = (confirmedAliveTotal + probableAliveTotal + suspectAliveTotal  + puiAliveTotal  ).ToString() + " (" + ((double)(confirmedAliveTotal + probableAliveTotal + suspectAliveTotal) / (double)grandtotal).ToString("P1") + ")";
                tblock3t.Style = this.Resources["styleBody"] as Style;
                tblock3t.FontWeight = FontWeights.Bold;
                Grid.SetRow(tblock3t, 5);
                Grid.SetColumn(tblock3t, 3);
                gridCountry.Children.Add(tblock3t);

                TextBlock tblock4t = new TextBlock();
                tblock4t.Text = (confirmedDeadTotal + probableDeadTotal + suspectDeadTotal  + puiDeadTotal    ).ToString() + " (" + ((double)(confirmedDeadTotal + probableDeadTotal + suspectDeadTotal) / (double)grandtotal).ToString("P1") + ")";
                tblock4t.Style = this.Resources["styleBody"] as Style;
                tblock4t.FontWeight = FontWeights.Bold;
                Grid.SetRow(tblock4t, 5);
                Grid.SetColumn(tblock4t, 2);
                gridCountry.Children.Add(tblock4t);
                //    epicaseallpatients      
                //       add row for pui    
                //       hard to iscountryUS      

                TextBlock tblock5t = new TextBlock();
                tblock5t.Text = (confirmedUnkTotal + probableUnkTotal + suspectUnkTotal  +  puiUnkTotal    ).ToString() + " (" + ((double)(confirmedUnkTotal + probableUnkTotal + suspectUnkTotal) / (double)grandtotal).ToString("P1") + ")";
                tblock5t.Style = this.Resources["styleBody"] as Style;
                tblock5t.FontWeight = FontWeights.Bold;
                Grid.SetRow(tblock5t, 5);
                Grid.SetColumn(tblock5t, 4);
                gridCountry.Children.Add(tblock5t);

                panelMain.Children.Add(gridCountry);

                if (StartIndex > 0)
                {
                    gridCountry.Visibility = Visibility.Collapsed;
                    txtCountryHeading.Visibility = System.Windows.Visibility.Collapsed;
                    tblockCurrentDate.Visibility = System.Windows.Visibility.Collapsed;
                }

                List<string> districts = new List<string>();

                foreach (CaseViewModel caseVM in DataHelper.CaseCollection)
                {
                    if (!districts.Contains(caseVM.District) && !String.IsNullOrEmpty(caseVM.District.Trim()) &&
                        caseVM.Country.Equals(countryName) &&
                        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed ||
                        caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable ||
                        caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Suspect || 
                        caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.PUI))
                    {
                        districts.Add(caseVM.District);
                    }
                }

                for (int i = 0; i < districts.Count; i++)
                //foreach (string district in districts)
                {
                    string district = districts[i];

                    if (i >= StartIndex && i <= EndIndex)
                    {
                        AddGrid(district, countryName);
                    }
                }
            }
        }

        private void AddGrid(string districtName, string countryName)
        {
            EpiDataHelper dataHelper = this.DataContext as EpiDataHelper;

            if (dataHelper != null)
            {
                StackPanel sp = new StackPanel();
                sp.Orientation = Orientation.Horizontal;

                TextBlock tblock = new TextBlock();
                tblock.Style = this.Resources["styleTableTitle"] as Style;
                tblock.Text = districtName;

                TextBlock tblockDt = new TextBlock();
                tblockDt.Style = this.Resources["styleTableDateHeading"] as Style;
                tblockDt.Text = DisplayDate.ToString("dd/MM/yyyy HH:mm");

                sp.Children.Add(tblock);
                sp.Children.Add(tblockDt);
                panelMain.Children.Add(sp);

                #region Calculations
                // GRAND TOTAL
                var query = from caseVM in dataHelper.CaseCollection
                            where caseVM.Country == countryName && caseVM.District == districtName &&
                            (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed || caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable ||
                            caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Suspect || caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.PUI)
                            select caseVM;

                int grandtotal = query.Count();

                // CONFIRMED TOTAL
                query = from caseVM in dataHelper.CaseCollection
                        where caseVM.Country == countryName && caseVM.District == districtName &&
                        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed)
                        select caseVM;

                int confirmedtotal = query.Count();

                // PROBABLE TOTAL
                query = from caseVM in dataHelper.CaseCollection
                        where caseVM.Country == countryName && caseVM.District == districtName &&
                        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable)
                        select caseVM;

                int probabletotal = query.Count();

                // SUSPECT TOTAL
                query = from caseVM in dataHelper.CaseCollection
                        where caseVM.Country == countryName && caseVM.District == districtName &&
                        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Suspect)
                        select caseVM;

                int suspecttotal = query.Count();

                // PUI TOTAL
                query = from caseVM in dataHelper.CaseCollection
                        where caseVM.Country == countryName && caseVM.District == districtName &&
                        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.PUI)
                        select caseVM;

                int puitotal = query.Count();



                // CONFIRMED ALIVE
                query = from caseVM in dataHelper.CaseCollection
                        where caseVM.Country == countryName && caseVM.District == districtName &&
                        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed) &&
                        (caseVM.CurrentStatus == Properties.Resources.Alive)
                        select caseVM;

                int confirmedAliveTotal = query.Count();

                // PROBABLE ALIVE
                query = from caseVM in dataHelper.CaseCollection
                        where caseVM.Country == countryName && caseVM.District == districtName &&
                        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable) &&
                        (caseVM.CurrentStatus == Properties.Resources.Alive)
                        select caseVM;

                int probableAliveTotal = query.Count();

                // SUSPECT ALIVE
                query = from caseVM in dataHelper.CaseCollection
                        where caseVM.Country == countryName && caseVM.District == districtName &&
                        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Suspect) &&
                        (caseVM.CurrentStatus == Properties.Resources.Alive)
                        select caseVM;

                int suspectAliveTotal = query.Count();

                // PUI ALIVE
                query = from caseVM in dataHelper.CaseCollection
                        where caseVM.Country == countryName && caseVM.District == districtName &&
                        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.PUI) &&
                        (caseVM.CurrentStatus == Properties.Resources.Alive)
                        select caseVM;

                int puiAliveTotal = query.Count();





                // CONFIRMED DEAD
                query = from caseVM in dataHelper.CaseCollection
                        where caseVM.Country == countryName && caseVM.District == districtName &&
                        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed) &&
                        (caseVM.CurrentStatus == Properties.Resources.Dead)
                        select caseVM;

                int confirmedDeadTotal = query.Count();

                // PROBABLE DEAD
                query = from caseVM in dataHelper.CaseCollection
                        where caseVM.Country == countryName && caseVM.District == districtName &&
                        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable) &&
                        (caseVM.CurrentStatus == Properties.Resources.Dead)
                        select caseVM;

                int probableDeadTotal = query.Count();

                // SUSPECT DEAD
                query = from caseVM in dataHelper.CaseCollection
                        where caseVM.Country == countryName && caseVM.District == districtName &&
                        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Suspect) &&
                        (caseVM.CurrentStatus == Properties.Resources.Dead)
                        select caseVM;

                int suspectDeadTotal = query.Count();

                // PUI DEAD
                query = from caseVM in dataHelper.CaseCollection
                        where caseVM.Country == countryName && caseVM.District == districtName &&
                        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.PUI) &&
                        (caseVM.CurrentStatus == Properties.Resources.Dead)
                        select caseVM;

                int puiDeadTotal = query.Count();





                // CONFIRMED UNK
                query = from caseVM in dataHelper.CaseCollection
                        where caseVM.Country == countryName && caseVM.District == districtName &&
                        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed) &&
                        (caseVM.CurrentStatus == String.Empty)
                        select caseVM;

                int confirmedUnkTotal = query.Count();

                // PROBABLE UNK
                query = from caseVM in dataHelper.CaseCollection
                        where caseVM.Country == countryName && caseVM.District == districtName &&
                        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable) &&
                        (caseVM.CurrentStatus == String.Empty)
                        select caseVM;

                int probableUnkTotal = query.Count();

                // SUSPECT UNK
                query = from caseVM in dataHelper.CaseCollection
                        where caseVM.Country == countryName && caseVM.District == districtName &&
                        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Suspect) &&
                        (caseVM.CurrentStatus == String.Empty)
                        select caseVM;

                int suspectUnkTotal = query.Count();

                // PUI UNK
                query = from caseVM in dataHelper.CaseCollection
                        where caseVM.Country == countryName && caseVM.District == districtName &&
                        (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.PUI) &&
                        (caseVM.CurrentStatus == String.Empty)
                        select caseVM;

                int puiUnkTotal = query.Count();
                #endregion // Calculations


                #region Grid and Borders
                Grid gridCountry = new Grid();
                gridCountry.RowDefinitions.Add(new RowDefinition());
                gridCountry.RowDefinitions.Add(new RowDefinition());
                gridCountry.RowDefinitions.Add(new RowDefinition());
                gridCountry.RowDefinitions.Add(new RowDefinition());
                gridCountry.RowDefinitions.Add(new RowDefinition());
                gridCountry.RowDefinitions.Add(new RowDefinition());
                gridCountry.ColumnDefinitions.Add(new ColumnDefinition());
                gridCountry.ColumnDefinitions.Add(new ColumnDefinition());
                gridCountry.ColumnDefinitions.Add(new ColumnDefinition());
                gridCountry.ColumnDefinitions.Add(new ColumnDefinition());
                gridCountry.ColumnDefinitions.Add(new ColumnDefinition());

                gridCountry.ColumnDefinitions[0].Width = new GridLength(240);
                gridCountry.ColumnDefinitions[1].Width = new GridLength(80);
                gridCountry.ColumnDefinitions[2].Width = new GridLength(80);
                gridCountry.ColumnDefinitions[3].Width = new GridLength(190);
                gridCountry.ColumnDefinitions[4].Width = new GridLength(90);

                if (ContactTracing.Core.ApplicationViewModel.Instance.CurrentRegion == Core.Enums.RegionEnum.International)
                {
                    gridCountry.RowDefinitions[4].Height = new GridLength(0);
                }

                Border border1 = new Border();
                border1.Style = this.Resources["borderColumnHeaderStyle"] as Style;
                Grid.SetColumn(border1, 0);
                gridCountry.Children.Add(border1);

                Border border2 = new Border();
                border2.Style = this.Resources["borderColumnHeaderStyle"] as Style;
                Grid.SetColumn(border2, 1);
                gridCountry.Children.Add(border2);

                Border border3 = new Border();
                border3.Style = this.Resources["borderColumnHeaderStyle"] as Style;
                Grid.SetColumn(border3, 2);
                gridCountry.Children.Add(border3);

                Border border4 = new Border();
                border4.Style = this.Resources["borderColumnHeaderStyle"] as Style;
                Grid.SetColumn(border4, 3);
                gridCountry.Children.Add(border4);

                Border border5 = new Border();
                border5.Style = this.Resources["borderColumnHeaderStyle"] as Style;
                Grid.SetColumn(border5, 4);
                gridCountry.Children.Add(border5);




                Border border1c = new Border();
                border1c.Style = this.Resources["borderRowHeaderStyle"] as Style;
                Grid.SetColumn(border1c, 0);
                Grid.SetRow(border1c, 1);
                gridCountry.Children.Add(border1c);

                Border border2c = new Border();
                border2c.Style = this.Resources["borderCellAltStyle"] as Style;
                Grid.SetColumn(border2c, 1);
                Grid.SetRow(border2c, 1);
                gridCountry.Children.Add(border2c);

                Border border3c = new Border();
                border3c.Style = this.Resources["borderCellAltStyle"] as Style;
                Grid.SetColumn(border3c, 2);
                Grid.SetRow(border3c, 1);
                gridCountry.Children.Add(border3c);

                Border border4c = new Border();
                border4c.Style = this.Resources["borderCellAltStyle"] as Style;
                Grid.SetColumn(border4c, 3);
                Grid.SetRow(border4c, 1);
                gridCountry.Children.Add(border4c);

                Border border5c = new Border();
                border5c.Style = this.Resources["borderCellAltStyle"] as Style;
                Grid.SetColumn(border5c, 4);
                Grid.SetRow(border5c, 1);
                gridCountry.Children.Add(border5c);



                Border border1p = new Border();
                border1p.Style = this.Resources["borderRowHeaderStyle"] as Style;
                Grid.SetColumn(border1p, 0);
                Grid.SetRow(border1p, 2);
                gridCountry.Children.Add(border1p);

                Border border2p = new Border();
                border2p.Style = this.Resources["borderCellAltStyle"] as Style;
                Grid.SetColumn(border2p, 1);
                Grid.SetRow(border2p, 2);
                gridCountry.Children.Add(border2p);

                Border border3p = new Border();
                border3p.Style = this.Resources["borderCellAltStyle"] as Style;
                Grid.SetColumn(border3p, 2);
                Grid.SetRow(border3p, 2);
                gridCountry.Children.Add(border3p);

                Border border4p = new Border();
                border4p.Style = this.Resources["borderCellAltStyle"] as Style;
                Grid.SetColumn(border4p, 3);
                Grid.SetRow(border4p, 2);
                gridCountry.Children.Add(border4p);

                Border border5p = new Border();
                border5p.Style = this.Resources["borderCellAltStyle"] as Style;
                Grid.SetColumn(border5p, 4);
                Grid.SetRow(border5p, 2);
                gridCountry.Children.Add(border5p);




                



                Border border1s = new Border();
                border1s.Style = this.Resources["borderRowHeaderStyle"] as Style;
                Grid.SetColumn(border1s, 0);
                Grid.SetRow(border1s, 3);
                gridCountry.Children.Add(border1s);

                Border border2s = new Border();
                border2s.Style = this.Resources["borderCellAltStyle"] as Style;
                Grid.SetColumn(border2s, 1);
                Grid.SetRow(border2s, 3);
                gridCountry.Children.Add(border2s);

                Border border3s = new Border();
                border3s.Style = this.Resources["borderCellAltStyle"] as Style;
                Grid.SetColumn(border3s, 2);
                Grid.SetRow(border3s, 3);
                gridCountry.Children.Add(border3s);

                Border border4s = new Border();
                border4s.Style = this.Resources["borderCellAltStyle"] as Style;
                Grid.SetColumn(border4s, 3);
                Grid.SetRow(border4s, 3);
                gridCountry.Children.Add(border4s);

                Border border5s = new Border();
                border5s.Style = this.Resources["borderCellAltStyle"] as Style;
                Grid.SetColumn(border5s, 4);
                Grid.SetRow(border5s, 3);
                gridCountry.Children.Add(border5s);




                Border border1pui = new Border();
                border1pui.Style = this.Resources["borderRowHeaderStyle"] as Style;
                Grid.SetColumn(border1pui, 0);
                Grid.SetRow(border1pui, 4);
                gridCountry.Children.Add(border1pui);

                Border border2pui = new Border();
                border2pui.Style = this.Resources["borderCellAltStyle"] as Style;
                Grid.SetColumn(border2pui, 1);
                Grid.SetRow(border2pui, 4);
                gridCountry.Children.Add(border2pui);

                Border border3pui = new Border();
                border3pui.Style = this.Resources["borderCellAltStyle"] as Style;
                Grid.SetColumn(border3pui, 2);
                Grid.SetRow(border3pui, 4);
                gridCountry.Children.Add(border3pui);

                Border border4pui = new Border();
                border4pui.Style = this.Resources["borderCellAltStyle"] as Style;
                Grid.SetColumn(border4pui, 3);
                Grid.SetRow(border4pui, 4);
                gridCountry.Children.Add(border4pui);

                Border border5pui = new Border();
                border5pui.Style = this.Resources["borderCellAltStyle"] as Style;
                Grid.SetColumn(border5pui, 4);
                Grid.SetRow(border5pui, 4);
                gridCountry.Children.Add(border5pui);


                Border border1t = new Border();
                border1t.Style = this.Resources["borderRowHeaderStyle"] as Style;
                Grid.SetColumn(border1t, 0);
                Grid.SetRow(border1t, 5);
                gridCountry.Children.Add(border1t);

                Border border2t = new Border();
                border2t.Style = this.Resources["borderCellStyle"] as Style;
                Grid.SetColumn(border2t, 1);
                Grid.SetRow(border2t, 5);
                gridCountry.Children.Add(border2t);

                Border border3t = new Border();
                border3t.Style = this.Resources["borderCellStyle"] as Style;
                Grid.SetColumn(border3t, 2);
                Grid.SetRow(border3t, 5);
                gridCountry.Children.Add(border3t);

                Border border4t = new Border();
                border4t.Style = this.Resources["borderCellStyle"] as Style;
                Grid.SetColumn(border4t, 3);
                Grid.SetRow(border4t, 5);
                gridCountry.Children.Add(border4t);

                Border border5t = new Border();
                border5t.Style = this.Resources["borderCellStyle"] as Style;
                Grid.SetColumn(border5t, 4);
                Grid.SetRow(border5t, 5);
                gridCountry.Children.Add(border5t);
                #endregion // Grid and Borders



                TextBlock tblock1 = new TextBlock();
                tblock1.Text = Properties.Resources.AnalysisEpiClassification;
                tblock1.Style = this.Resources["styleHeader"] as Style;
                Grid.SetRow(tblock1, 0);
                Grid.SetColumn(tblock1, 0);
                gridCountry.Children.Add(tblock1);

                TextBlock tblock2 = new TextBlock();
                tblock2.Text = Properties.Resources.Count;
                tblock2.Style = this.Resources["styleHeader"] as Style;
                Grid.SetRow(tblock2, 0);
                Grid.SetColumn(tblock2, 1);
                gridCountry.Children.Add(tblock2);

                TextBlock tblock3 = new TextBlock();
                tblock3.Text = Properties.Resources.Alive;
                tblock3.Style = this.Resources["styleHeader"] as Style;
                Grid.SetRow(tblock3, 0);
                Grid.SetColumn(tblock3, 3);
                gridCountry.Children.Add(tblock3);

                TextBlock tblock4 = new TextBlock();
                tblock4.Text = Properties.Resources.Dead;
                tblock4.Style = this.Resources["styleHeader"] as Style;
                Grid.SetRow(tblock4, 0);
                Grid.SetColumn(tblock4, 2);
                gridCountry.Children.Add(tblock4);

                TextBlock tblock5 = new TextBlock();
                tblock5.Text = Properties.Resources.AliveDeadUnknown;
                tblock5.Style = this.Resources["styleHeader"] as Style;
                Grid.SetRow(tblock5, 0);
                Grid.SetColumn(tblock5, 4);
                gridCountry.Children.Add(tblock5);









                TextBlock tblock1c = new TextBlock();
                tblock1c.Text = Properties.Resources.Confirmed;
                tblock1c.Style = this.Resources["styleRowHeader"] as Style;
                Grid.SetRow(tblock1c, 1);
                Grid.SetColumn(tblock1c, 0);
                gridCountry.Children.Add(tblock1c);

                TextBlock tblock2c = new TextBlock();
                tblock2c.Text = confirmedtotal.ToString();
                tblock2c.Style = this.Resources["styleBody"] as Style;
                Grid.SetRow(tblock2c, 1);
                Grid.SetColumn(tblock2c, 1);
                gridCountry.Children.Add(tblock2c);

                TextBlock tblock3c = new TextBlock();
                tblock3c.Text = confirmedAliveTotal.ToString();
                tblock3c.Style = this.Resources["styleBody"] as Style;
                Grid.SetRow(tblock3c, 1);
                Grid.SetColumn(tblock3c, 3);
                gridCountry.Children.Add(tblock3c);

                TextBlock tblock4c = new TextBlock();
                tblock4c.Text = confirmedDeadTotal.ToString();
                tblock4c.Style = this.Resources["styleBody"] as Style;
                Grid.SetRow(tblock4c, 1);
                Grid.SetColumn(tblock4c, 2);
                gridCountry.Children.Add(tblock4c);

                TextBlock tblock5c = new TextBlock();
                tblock5c.Text = confirmedUnkTotal.ToString();
                tblock5c.Style = this.Resources["styleBody"] as Style;
                Grid.SetRow(tblock5c, 1);
                Grid.SetColumn(tblock5c, 4);
                gridCountry.Children.Add(tblock5c);














                TextBlock tblock1p = new TextBlock();
                tblock1p.Text = Properties.Resources.Probable;
                tblock1p.Style = this.Resources["styleRowHeader"] as Style;
                Grid.SetRow(tblock1p, 2);
                Grid.SetColumn(tblock1p, 0);
                gridCountry.Children.Add(tblock1p);

                TextBlock tblock2p = new TextBlock();
                tblock2p.Text = probabletotal.ToString();
                tblock2p.Style = this.Resources["styleBody"] as Style;
                Grid.SetRow(tblock2p, 2);
                Grid.SetColumn(tblock2p, 1);
                gridCountry.Children.Add(tblock2p);

                TextBlock tblock3p = new TextBlock();
                tblock3p.Text = probableAliveTotal.ToString() + " " + Properties.Resources.UnderInvestigation + "";
                tblock3p.Style = this.Resources["styleBody"] as Style;
                Grid.SetRow(tblock3p, 2);
                Grid.SetColumn(tblock3p, 3);
                gridCountry.Children.Add(tblock3p);

                TextBlock tblock4p = new TextBlock();
                tblock4p.Text = probableDeadTotal.ToString();
                tblock4p.Style = this.Resources["styleBody"] as Style;
                Grid.SetRow(tblock4p, 2);
                Grid.SetColumn(tblock4p, 2);
                gridCountry.Children.Add(tblock4p);

                TextBlock tblock5p = new TextBlock();
                tblock5p.Text = probableUnkTotal.ToString();
                tblock5p.Style = this.Resources["styleBody"] as Style;
                Grid.SetRow(tblock5p, 2);
                Grid.SetColumn(tblock5p, 4);
                gridCountry.Children.Add(tblock5p);


















                TextBlock tblock1s = new TextBlock();
                tblock1s.Text = Properties.Resources.Suspect;
                tblock1s.Style = this.Resources["styleRowHeader"] as Style;
                Grid.SetRow(tblock1s, 3);
                Grid.SetColumn(tblock1s, 0);
                gridCountry.Children.Add(tblock1s);

                TextBlock tblock2s = new TextBlock();
                tblock2s.Text = suspecttotal.ToString();
                tblock2s.Style = this.Resources["styleBody"] as Style;
                Grid.SetRow(tblock2s, 3);
                Grid.SetColumn(tblock2s, 1);
                gridCountry.Children.Add(tblock2s);

                TextBlock tblock3s = new TextBlock();
                tblock3s.Text = suspectAliveTotal.ToString() + " " + Properties.Resources.UnderInvestigation + "";
                tblock3s.Style = this.Resources["styleBody"] as Style;
                Grid.SetRow(tblock3s, 3);
                Grid.SetColumn(tblock3s, 3);
                gridCountry.Children.Add(tblock3s);

                TextBlock tblock4s = new TextBlock();
                tblock4s.Text = suspectDeadTotal.ToString();
                tblock4s.Style = this.Resources["styleBody"] as Style;
                Grid.SetRow(tblock4s, 3);
                Grid.SetColumn(tblock4s, 2);
                gridCountry.Children.Add(tblock4s);

                TextBlock tblock5s = new TextBlock();
                tblock5s.Text = suspectUnkTotal.ToString();
                tblock5s.Style = this.Resources["styleBody"] as Style;
                Grid.SetRow(tblock5s, 3);
                Grid.SetColumn(tblock5s, 4);
                gridCountry.Children.Add(tblock5s);






                TextBlock tblock1pui = new TextBlock();
                tblock1pui.Text = Properties.Resources.PUI;
                tblock1pui.Style = this.Resources["styleRowHeader"] as Style;
                Grid.SetRow(tblock1pui, 4);
                Grid.SetColumn(tblock1pui, 0);
                gridCountry.Children.Add(tblock1pui);

                TextBlock tblock2pui = new TextBlock();
                tblock2pui.Text = puitotal.ToString();
                tblock2pui.Style = this.Resources["styleBody"] as Style;
                Grid.SetRow(tblock2pui, 4);
                Grid.SetColumn(tblock2pui, 1);
                gridCountry.Children.Add(tblock2pui);

                TextBlock tblock3pui = new TextBlock();
                tblock3pui.Text = puiAliveTotal.ToString() + " " + Properties.Resources.UnderInvestigation + "";
                tblock3pui.Style = this.Resources["styleBody"] as Style;
                Grid.SetRow(tblock3pui, 4);
                Grid.SetColumn(tblock3pui, 3);
                gridCountry.Children.Add(tblock3pui);

                TextBlock tblock4pui = new TextBlock();
                tblock4pui.Text = puiDeadTotal.ToString();
                tblock4pui.Style = this.Resources["styleBody"] as Style;
                Grid.SetRow(tblock4pui, 4);
                Grid.SetColumn(tblock4pui, 2);
                gridCountry.Children.Add(tblock4pui);

                TextBlock tblock5pui = new TextBlock();
                tblock5pui.Text = puiUnkTotal.ToString();
                tblock5pui.Style = this.Resources["styleBody"] as Style;
                Grid.SetRow(tblock5pui, 4);
                Grid.SetColumn(tblock5pui, 4);
                gridCountry.Children.Add(tblock5pui);




                TextBlock tblock1t = new TextBlock();
                tblock1t.Text = "Total";
                tblock1t.Style = this.Resources["styleRowHeader"] as Style;
                tblock1t.FontWeight = FontWeights.Bold;
                tblock1t.Margin = new Thickness(0);
                Grid.SetRow(tblock1t, 5);
                Grid.SetColumn(tblock1t, 0);
                gridCountry.Children.Add(tblock1t);

                TextBlock tblock2t = new TextBlock();
                tblock2t.Text = (confirmedtotal + probabletotal + suspecttotal + puitotal).ToString();
                tblock2t.Style = this.Resources["styleBody"] as Style;
                tblock2t.FontWeight = FontWeights.Bold;
                Grid.SetRow(tblock2t, 5);
                Grid.SetColumn(tblock2t, 1);
                gridCountry.Children.Add(tblock2t);

                TextBlock tblock3t = new TextBlock();
                tblock3t.Text = (confirmedAliveTotal + probableAliveTotal + suspectAliveTotal + puiAliveTotal).ToString() + " (" + ((double)(confirmedAliveTotal + probableAliveTotal + suspectAliveTotal + puiAliveTotal) / (double)grandtotal).ToString("P1") + ")";
                tblock3t.Style = this.Resources["styleBody"] as Style;
                tblock3t.FontWeight = FontWeights.Bold;
                Grid.SetRow(tblock3t, 5);
                Grid.SetColumn(tblock3t, 3);
                gridCountry.Children.Add(tblock3t);

                TextBlock tblock4t = new TextBlock();
                tblock4t.Text = (confirmedDeadTotal + probableDeadTotal + suspectDeadTotal + puiDeadTotal).ToString() + " (" + ((double)(confirmedDeadTotal + probableDeadTotal + suspectDeadTotal + puiDeadTotal) / (double)grandtotal).ToString("P1") + ")";
                tblock4t.Style = this.Resources["styleBody"] as Style;
                tblock4t.FontWeight = FontWeights.Bold;
                Grid.SetRow(tblock4t, 5);
                Grid.SetColumn(tblock4t, 2);
                gridCountry.Children.Add(tblock4t);

                TextBlock tblock5t = new TextBlock();
                tblock5t.Text = (confirmedUnkTotal + probableUnkTotal + suspectUnkTotal + puiUnkTotal).ToString() + " (" + ((double)(confirmedUnkTotal + probableUnkTotal + suspectUnkTotal + puiUnkTotal) / (double)grandtotal).ToString("P1") + ")";
                tblock5t.Style = this.Resources["styleBody"] as Style;
                tblock5t.FontWeight = FontWeights.Bold;
                Grid.SetRow(tblock5t, 5);
                Grid.SetColumn(tblock5t, 4);
                gridCountry.Children.Add(tblock5t);

                foreach (UIElement element in gridCountry.Children)
                {
                    TextBlock tb = element as TextBlock;
                    if (tb != null)
                    {
                        tb.Margin = new Thickness(4, 2, 4, 2);
                    }
                }

                panelMain.Children.Add(gridCountry);
            }
        }
    }
}
