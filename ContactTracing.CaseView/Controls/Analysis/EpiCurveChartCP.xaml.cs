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
    /// Interaction logic for EpiCurveChartCP.xaml
    /// </summary>
    public partial class EpiCurveChartCP : AnalysisOutputBase
    {
        public static readonly DependencyProperty ChartWidthProperty = DependencyProperty.Register("ChartWidthProperty", typeof(double), typeof(EpiCurveChartCP), new PropertyMetadata(double.PositiveInfinity));
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
        public EpiCurveChartCP()
        {
            InitializeComponent();
        }

        public void Compute()
        {
            DataHelper.RefreshEpiCurveData();

            int highestCount = 0;

            for (int i = 0; i < DataHelper.EpiCurveDataPointCollectionCP.Count - 1; i = i + 2)
            {
                XYColumnChartData xyDataC = DataHelper.EpiCurveDataPointCollectionCP[i];
                XYColumnChartData xyDataP = DataHelper.EpiCurveDataPointCollectionCP[i + 1];

                if ((xyDataC.Y + xyDataP.Y)  > highestCount) 
                {
                    highestCount = (int)(xyDataC.Y + xyDataP.Y);
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
                var query = from caseVM in dataHelper.CaseCollection
                            where caseVM.DateOnset.HasValue == false &&
                            (caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Confirmed || caseVM.EpiCaseDef == Core.Enums.EpiCaseClassification.Probable)
                            select caseVM;

                tblockNumber.Text = query.Count().ToString();
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
