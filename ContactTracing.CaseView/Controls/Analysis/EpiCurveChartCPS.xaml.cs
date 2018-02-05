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
using ContactTracing.Core;
using ContactTracing.ViewModel;
using ComponentArt.Win.DataVisualization.Charting;

namespace ContactTracing.CaseView.Controls.Analysis
{
    /// <summary>
    /// Interaction logic for EpiCurveChartCPS.xaml
    /// </summary>
    public partial class EpiCurveChartCPS : AnalysisOutputBase
    {
        public static readonly DependencyProperty ChartWidthProperty = DependencyProperty.Register("ChartWidthProperty", typeof(double), typeof(EpiCurveChartCPS), new PropertyMetadata(double.PositiveInfinity));
        public double ChartWidth
        {
            get
            {
                return (double)(this.GetValue(ChartWidthProperty));
            }
            set
            {
                this.SetValue(ChartWidthProperty, value);
            }

        }
        private EpiDataHelper DataHelper
        {
            get
            {
                return (this.DataContext as EpiDataHelper);
            }
        }
        public EpiCurveChartCPS()
        {
            InitializeComponent();
        }

        public void Compute()
        {



            DataHelper.RefreshEpiCurveData();

            int highestCount = 0;


            XYColumnChartData xyDataPUI = null;
            double chartDataYTotal;
            int chartDataSkip = 0;


            if (ApplicationViewModel.Instance.CurrentRegion == Core.Enums.RegionEnum.USA)
            {
                chartDataSkip = 4;
            }
            else
            {
                chartDataSkip = 3;
            }

            for (int i = 0; i < DataHelper.EpiCurveDataPointCollectionCPS.Count - 1; i = i + chartDataSkip)
            {
                XYColumnChartData xyDataC = DataHelper.EpiCurveDataPointCollectionCPS[i];
                XYColumnChartData xyDataP = DataHelper.EpiCurveDataPointCollectionCPS[i + 1];
                XYColumnChartData xyDataS = DataHelper.EpiCurveDataPointCollectionCPS[i + 2];

                if (ApplicationViewModel.Instance.CurrentRegion == Core.Enums.RegionEnum.USA)
                    xyDataPUI = DataHelper.EpiCurveDataPointCollectionCPS[i + 3];

                if (ApplicationViewModel.Instance.CurrentRegion == Core.Enums.RegionEnum.USA)
                    chartDataYTotal = xyDataC.Y + xyDataP.Y + xyDataS.Y + xyDataPUI.Y;
                else
                    chartDataYTotal = xyDataC.Y + xyDataP.Y + xyDataS.Y;


                if (chartDataYTotal > highestCount)
                {
                    highestCount = (int)(chartDataYTotal);
                }
            }

            //foreach (XYColumnChartData xyData in DataHelper.EpiCurveDataPointCollection)
            //{
            //    if (xyData.Y > highestCount)
            //    {
            //        highestCount = (int)xyData.Y;
            //    }
            //}

            numberCoordinates.From = 0;
            numberCoordinates.To = highestCount;

            if (highestCount > 70)
            {
                numberCoordinates.Step = 5;
                numberCoordinates.To = highestCount + (highestCount % 5);
            }
            else if (highestCount > 36)
            {
                numberCoordinates.Step = 2;

                if (highestCount % 2 == 1)
                {
                    numberCoordinates.To = highestCount + 1;
                }
            }
            else
            {
                numberCoordinates.Step = 1;
                if (highestCount <= 10)
                {
                    numberCoordinates.NumberOfPoints = highestCount + 1;
                }
            }



            tblockNumber.Text = "...";

            EpiDataHelper dataHelper = this.DataContext as EpiDataHelper;
            if (dataHelper != null)
            {

                if (ApplicationViewModel.Instance.CurrentRegion == Core.Enums.RegionEnum.USA)
                {
                    var query = from caseVM in dataHelper.CaseCollection
                                where caseVM.DateOnset.HasValue == false &&
                                (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed ||
                                    caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable ||
                                    caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Suspect ||
                                    caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.PUI)
                                select caseVM;

                    tblockNumber.Text = query.Count().ToString();
                }
                else
                {
                    var query = from caseVM in dataHelper.CaseCollection
                                where caseVM.DateOnset.HasValue == false &&
                                (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed ||
                                    caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable ||
                                    caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Suspect)
                                select caseVM;

                    tblockNumber.Text = query.Count().ToString();
                }


                //  tblockNumber.Text = query.Count().ToString();        

            }

            xyChart.MaxWidth = System.Windows.SystemParameters.PrimaryScreenWidth - 40;
        }

        private void xyChart_DataStructureCreated(object sender, EventArgs e)
        {
            string sName = "";

            int i = 0;
            foreach (Series s0 in xyChart.DataSeries)
            {
                if (s0.Label != null)
                {
                    sName = s0.Label.Split('.')[1];
                    //if (Settings.ShowLegendVarNames == false)
                    //{
                    //    int index = sName.IndexOf(" = ");
                    //    s0.Label = sName.Substring(index + 3);
                    //}
                    //else
                    //{
                    s0.Label = sName;
                    switch (i)
                    {
                        case 0:
                            s0.Color = Color.FromRgb(199, 41, 1);
                            break;
                        case 1:
                            s0.Color = Color.FromRgb(1, 110, 151);
                            break;
                        case 2:
                            s0.Color = Color.FromRgb(89, 132, 39);
                            break;
                        case 3:
                            s0.Color = Color.FromRgb(119, 44, 77);
                            break;

                    }
                    i++;
                    //}
                }
            }

            Size textSize = new Size();
            Size chartSize = new Size();

            tblockYAxisLabel.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            textSize = new Size(tblockYAxisLabel.DesiredSize.Width, tblockYAxisLabel.DesiredSize.Height);

            xyChart.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            chartSize = new Size(xyChart.DesiredSize.Width, xyChart.DesiredSize.Height);

            tblockYAxisLabel.Padding = new Thickness(((chartSize.Height - 22) / 2) - (textSize.Width / 2), 2, 0, 2);

        }

        private void xyChart_AnimationCompleted(object sender, EventArgs e)
        {

        }
    }
}
