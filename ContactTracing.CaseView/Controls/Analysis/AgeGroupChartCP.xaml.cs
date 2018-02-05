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
    /// Interaction logic for AgeGroupChartCP.xaml
    /// </summary>
    public partial class AgeGroupChartCP : UserControl
    {
        public static readonly DependencyProperty ChartWidthProperty = DependencyProperty.Register("ChartWidthProperty", typeof(double), typeof(AgeGroupChartCP), new PropertyMetadata(double.PositiveInfinity));
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
        public AgeGroupChartCP()
        {
            InitializeComponent();
        }

        public void Compute()
        {
            DataHelper.RefreshAgeGroupData.Execute(true);
            xyChart.MaxWidth = System.Windows.SystemParameters.PrimaryScreenWidth - 40;
        }

        private void xyChart_DataStructureCreated(object sender, EventArgs e)
        {
            if (this.DataContext != null)
            {
                EpiDataHelper dataHelper = this.DataContext as EpiDataHelper;
                if (dataHelper != null && dataHelper.Project != null)
                {
                    string sName = "";

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
                            //}
                        }
                    }

                    Size textSize = new Size();
                    Size chartSize = new Size();

                    tblockYAxisLabel.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
                    textSize = new Size(tblockYAxisLabel.DesiredSize.Width, tblockYAxisLabel.DesiredSize.Height);

                    xyChart.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

                    chartSize = new Size(xyChart.DesiredSize.Width, xyChart.DesiredSize.Height);

                    //tblockYAxisLabel.Padding = new Thickness(((chartSize.Height - 22) / 2) - (textSize.Width / 2), 2, 0, 2);

                    xyChart.Orientation = Orientation.Horizontal;
                    labelXAxis.Orientation = ChartLabelOrientation.Vertical;
                    labelXAxis.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    labelYAxis.Orientation = ChartLabelOrientation.Horizontal;

                    tblockXAxisLabel.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
                    textSize = new Size(tblockXAxisLabel.DesiredSize.Width, tblockXAxisLabel.DesiredSize.Height);

                    xyChart.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
                    chartSize = new Size(xyChart.DesiredSize.Width, xyChart.DesiredSize.Height);

                    tblockXAxisLabel.Padding = new Thickness(((chartSize.Height + 80) / 2) - (textSize.Width / 2), 2, 0, 2);
                }
            }
        }

        private void xyChart_AnimationCompleted(object sender, EventArgs e)
        {

        }
    }
}
