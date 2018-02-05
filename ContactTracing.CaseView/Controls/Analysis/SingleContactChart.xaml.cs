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

namespace ContactTracing.CaseView.Controls.Analysis
{
    /// <summary>
    /// Interaction logic for SingleContactChart.xaml
    /// </summary>
    public partial class SingleContactChart : UserControl
    {
        public static readonly DependencyProperty IsCountryUSProperty = DependencyProperty.Register("IsCountryUS", typeof(bool), typeof(SingleContactChart), new PropertyMetadata(false));
        public bool IsCountryUS
        {
            get
            {
                return (bool)(this.GetValue(IsCountryUSProperty));
            }
            set
            {
                this.SetValue(IsCountryUSProperty, value);
            }
        }

        public static readonly DependencyProperty ShowTemperaturesProperty = DependencyProperty.Register("ShowTemperatures", typeof(bool), typeof(SingleContactChart), new PropertyMetadata(false));
        public bool ShowTemperatures
        {
            get
            {
                return (bool)(this.GetValue(ShowTemperaturesProperty));
            }
            set
            {
                this.SetValue(ShowTemperaturesProperty, value);
            }
        }

        public SingleContactChart()
        {
            InitializeComponent();
        }
    
        //public SingleContactChart(ContactViewModel contactVM)
        //{
        //    InitializeComponent();

        //    if (contactVM.FollowUpWindowViewModel == null) { return; }

        //    FollowUpWindowViewModel followUpVM = contactVM.FollowUpWindowViewModel;

        //    Border lastBorder = new Border();

        //    int i = 0;
        //    foreach (FollowUpVisitViewModel followUpVisitVM in followUpVM.FollowUpVisits)
        //    {
        //        bool isToday = false;
        //        if (followUpVisitVM.Date == DateTime.Today)
        //        {
        //            isToday = true;
        //        }
        //        StackPanel panel = new StackPanel();

        //        Border border = new Border();
        //        border.Child = panel;
        //        border.Height = 72;

        //        Grid.SetColumn(border, i);
        //        Grid.SetRow(border, 1);
        //        //grdMain.Children.Add(border);

        //        TextBlock tblock = new TextBlock();
        //        tblock.FontSize = 10;
        //        tblock.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
        //        tblock.Text = followUpVisitVM.FollowUpVisit.Date.ToString("M/d");// kvp.Key.ToString("M/d");
        //        Grid.SetColumn(tblock, i);
        //        Grid.SetRow(tblock, 3);
        //        //grdMain.Children.Add(tblock);

        //        if (isToday)
        //        {
        //            TextBlock tblockToday = new TextBlock();
        //            tblockToday.FontSize = 8.5;
        //            tblockToday.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
        //            tblockToday.Text = Properties.Resources.Today;
        //            Grid.SetColumn(tblockToday, i);
        //            Grid.SetRow(tblockToday, 0);
        //            //grdMain.Children.Add(tblockToday);
        //        }
        //        else
        //        {
        //            TextBlock tblockDay = new TextBlock();
        //            tblockDay.FontSize = 8.5;
        //            tblockDay.Foreground = Brushes.Gray;
        //            tblockDay.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
        //            tblockDay.Text = ((i + 1).ToString());
        //            Grid.SetColumn(tblockDay, i);
        //            Grid.SetRow(tblockDay, 0);
        //            //grdMain.Children.Add(tblockDay);
        //        }

        //        Border tooltip = new Border();
        //        tooltip.BorderThickness = new Thickness(0);
        //        tooltip.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
        //        StackPanel tooltipContent = new StackPanel();
        //        tooltipContent.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
        //        tooltip.Child = tooltipContent;

        //        TextBlock tblockTTContactName = new TextBlock();
        //        tblockTTContactName.Text = contactVM.Surname + ", " + contactVM.OtherNames;
        //        tooltipContent.Children.Add(tblockTTContactName);

        //        TextBlock tblockTTDay = new TextBlock();
        //        tblockTTDay.Text = "Day " + ((i + 1).ToString()) + " : " + followUpVisitVM.FollowUpVisit.Date.ToString("M/d");
        //        tooltipContent.Children.Add(tblockTTDay);

        //        TextBlock tblockTTStatus = new TextBlock();
        //        if (!followUpVisitVM.Status.HasValue)
        //        {
        //            tblockTTStatus.Text = "Unknown";
        //        }
        //        else
        //        {
        //            switch (followUpVisitVM.Status.Value)
        //            {
        //                case ContactDailyStatus.Dead:
        //                    tblockTTStatus.Text = "Dead";
        //                    break;
        //                case ContactDailyStatus.NotRecorded:
        //                    tblockTTStatus.Text = "Status not recorded";
        //                    break;
        //                case ContactDailyStatus.NotSeen:
        //                    tblockTTStatus.Text = "Not seen";
        //                    break;
        //                case ContactDailyStatus.SeenNotSick:
        //                    tblockTTStatus.Text = "Seen and not sick";
        //                    break;
        //                case ContactDailyStatus.SeenSickAndIsolated:
        //                    tblockTTStatus.Text = "Seen and sick, isolated";
        //                    break;
        //                case ContactDailyStatus.SeenSickAndIsoNotFilledOut:
        //                    tblockTTStatus.Text = "Seen and sick, isolation unknown";
        //                    break;
        //                case ContactDailyStatus.SeenSickAndNotIsolated:
        //                    tblockTTStatus.Text = "Seen and sick, not isolated";
        //                    break;
        //                case ContactDailyStatus.Unknown:
        //                    tblockTTStatus.Text = "Unknown";
        //                    break;
        //            }
        //        }
        //        tooltipContent.Children.Add(tblockTTStatus);

        //        DateTime today = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59);

        //        if ((followUpVisitVM.Status.HasValue && (followUpVisitVM.Status.Value == Core.ContactDailyStatus.NotSeen)) && followUpVisitVM.FollowUpVisit.Date <= today)
        //        {
        //            panel.Background = FindResource("HatchBrush") as VisualBrush; // this.Resources["HatchBrush"] as VisualBrush;

        //            Canvas canvas = new Canvas();
        //            canvas.Height = 70;
        //            panel.ToolTip = tooltip;
        //            panel.Children.Add(canvas);

        //            TextBlock tblock1 = new TextBlock();
        //            tblock1.Text = ContactTracing.CaseView.Properties.Resources.SingleContactChartNotSeen; // "Not seen";

        //            tblock1.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
        //            tblock1.VerticalAlignment = System.Windows.VerticalAlignment.Center;

        //            tblock1.Margin = new Thickness(0, 0, 0, 0);
        //            tblock1.FontWeight = FontWeights.SemiBold;
        //            //tblock1.FontSize = tblock1.FontSize + 1;

        //            Typeface typeFace = new Typeface(new FontFamily("Global User Interface"), tblock1.FontStyle, tblock1.FontWeight, tblock1.FontStretch);
        //            FormattedText ftxt = new FormattedText(tblock1.Text, System.Globalization.CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight, typeFace, tblock1.FontSize, Brushes.Black);

        //            RotateTransform rotate = new RotateTransform(270);

        //            Canvas.SetBottom(tblock1, 0);
        //            Canvas.SetLeft(tblock1, 8);

        //            tblock1.RenderTransform = rotate;
        //            canvas.Children.Add(tblock1);
        //            border.Style = FindResource("IndividualReportBorderStyle") as Style;
        //        }
        //        else if ((followUpVisitVM.Status.HasValue && (followUpVisitVM.Status.Value == Core.ContactDailyStatus.SeenSickAndNotIsolated || followUpVisitVM.Status.Value == Core.ContactDailyStatus.SeenSickAndIsoNotFilledOut)))
        //        {
        //            panel.Background = new SolidColorBrush(Colors.Gold); // FindResource("HatchBrush") as VisualBrush; // this.Resources["HatchBrush"] as VisualBrush;

        //            Canvas canvas = new Canvas();
        //            canvas.Height = 70;
        //            panel.ToolTip = tooltip;
        //            panel.Children.Add(canvas);

        //            if (followUpVisitVM.Status.Value == Core.ContactDailyStatus.SeenSickAndNotIsolated)
        //            {
        //                TextBlock tblock1 = new TextBlock();
        //                if (CaseViewModel.IsCountryUS)
        //                {
        //                    StringBuilder sb = new StringBuilder();
        //                    if (followUpVisitVM.Temp1.ToString() != "0")
        //                        sb.Append("\r\nT1: " + followUpVisitVM.Temp1.ToString().Trim());
        //                    if (followUpVisitVM.Temp2.ToString() != "0")
        //                        sb.Append("\r\nT2: " + followUpVisitVM.Temp2.ToString().Trim());
        //                    tblock1.Text = Properties.Resources.SingleContactChartSickNotIsolated + sb.ToString();
        //                    double length = double.Parse("-" + tblock1.Text.Length);
        //                    Canvas.SetBottom(tblock1, length);  
        //                }// "Not seen";
        //                else
        //                {
        //                    tblock1.Text = Properties.Resources.SingleContactChartSickNotIsolated;
        //                    Canvas.SetBottom(tblock1, -19);
        //                }

        //                tblock1.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
        //                tblock1.VerticalAlignment = System.Windows.VerticalAlignment.Center;
        //                tblock1.TextAlignment = TextAlignment.Center;

        //                tblock1.Margin = new Thickness(0, 0, 0, 0);
        //                tblock1.FontWeight = FontWeights.SemiBold;                        
        //                Typeface typeFace = new Typeface(new FontFamily("Global User Interface"), tblock1.FontStyle, tblock1.FontWeight, tblock1.FontStretch);
        //                FormattedText ftxt = new FormattedText(tblock1.Text, System.Globalization.CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight, typeFace, tblock1.FontSize, Brushes.Black);
        //                RotateTransform rotate = new RotateTransform(270);                                            
        //                Canvas.SetLeft(tblock1, 8);
        //                tblock1.RenderTransform = rotate;
        //                canvas.Children.Add(tblock1);
        //            }
        //            border.Style = FindResource("IndividualReportBorderStyle") as Style;
        //        }
        //        else if (!followUpVisitVM.Status.HasValue || followUpVisitVM.Status == Core.ContactDailyStatus.NotRecorded)// || followUpVisitVM.Seen == SeenType.NotSeen || (followUpVisitVM.Seen == SeenType.Seen && followUpVisitVM.Sick == SicknessType.NotRecorded))
        //        {
        //            panel.Background = new SolidColorBrush(Colors.White);
        //            border.Style = FindResource("IndividualReportBorderStyle") as Style;
        //            panel.ToolTip = tooltip;
        //        }
        //        else if (followUpVisitVM.Status.HasValue && (followUpVisitVM.Status.Value == ContactDailyStatus.SeenNotSick || followUpVisitVM.Status.Value == ContactDailyStatus.SeenSickAndNotIsolated || followUpVisitVM.Status.Value == ContactDailyStatus.SeenSickAndIsoNotFilledOut))
        //        {
        //            border.Style = FindResource("IndividualReportBorderStyle") as Style;
        //            panel.Background = new SolidColorBrush(Color.FromRgb(45, 166, 81));// Colors.ForestGreen);
        //            panel.ToolTip = tooltip;
        //            if (CaseViewModel.IsCountryUS)
        //            {
                        
        //                StringBuilder sb = new StringBuilder();
        //                if (followUpVisitVM.Temp1.ToString() != "0")
        //                    sb.Append("\r\nT1: " + followUpVisitVM.Temp1.ToString().Trim());
        //                if (followUpVisitVM.Temp2.ToString() != "0")
        //                    sb.Append("\r\nT2: " + followUpVisitVM.Temp2.ToString().Trim());
        //                if (!string.IsNullOrEmpty(sb.ToString()))
        //                {
        //                    Canvas canvas = new Canvas();
        //                    canvas.Height = 70;
        //                    panel.ToolTip = tooltip;
        //                    panel.Children.Add(canvas);
        //                    TextBlock tblock1 = new TextBlock();
        //                    tblock1.Text = sb.ToString();
        //                    double length = double.Parse("-" + tblock1.Text.Length);
        //                    Canvas.SetBottom(tblock1, length);
        //                    tblock1.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
        //                    tblock1.VerticalAlignment = System.Windows.VerticalAlignment.Center;
        //                    tblock1.TextAlignment = TextAlignment.Center;
        //                    tblock1.Margin = new Thickness(0, 0, 0, 0);
        //                    tblock1.FontWeight = FontWeights.SemiBold;
        //                    Typeface typeFace = new Typeface(new FontFamily("Global User Interface"), tblock1.FontStyle, tblock1.FontWeight, tblock1.FontStretch);
        //                    FormattedText ftxt = new FormattedText(tblock1.Text, System.Globalization.CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight, typeFace, tblock1.FontSize, Brushes.Black);
        //                    RotateTransform rotate = new RotateTransform(270);
        //                    Canvas.SetLeft(tblock1, 8);

        //                    tblock1.RenderTransform = rotate;
        //                    canvas.Children.Add(tblock1);
        //                }
        //            };
        //        }
        //        else
        //        {
        //            Canvas canvas = new Canvas();
        //            canvas.Height = 70;
        //            panel.ToolTip = tooltip;
        //            panel.Children.Add(canvas);

        //            TextBlock tblock1 = new TextBlock();
        //            if (followUpVisitVM.Status.Value == ContactDailyStatus.Dead)
        //            {
        //                tblock1.Text = ContactTracing.CaseView.Properties.Resources.Dead; //"Sick and" + Environment.NewLine + "Isolated";
        //                Canvas.SetBottom(tblock1, 0);
        //                Canvas.SetLeft(tblock1, 8);
        //            }
        //            else
        //            {
        //                if (CaseViewModel.IsCountryUS)
        //                {
        //                    StringBuilder sb = new StringBuilder();
        //                    if (followUpVisitVM.Temp1.ToString() != "0")
        //                        sb.Append("\r\nT1: " + followUpVisitVM.Temp1.ToString().Trim());
        //                    if (followUpVisitVM.Temp2.ToString() != "0")
        //                        sb.Append("\r\nT2: " + followUpVisitVM.Temp2.ToString().Trim());
        //                    tblock1.Text = Properties.Resources.SingleContactChartSickIsolated + sb.ToString();
        //                    double length = double.Parse("-" + tblock1.Text.Length);                          
        //                    Canvas.SetBottom(tblock1, length);
        //                }
        //                else
        //                {
        //                    tblock1.Text = Properties.Resources.SingleContactChartSickIsolated;
        //                    Canvas.SetBottom(tblock1, -16);
        //                }
        //                Canvas.SetLeft(tblock1, 8);
        //            }
        //            tblock1.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
        //            tblock1.VerticalAlignment = System.Windows.VerticalAlignment.Center;
        //            tblock1.TextAlignment = TextAlignment.Center;

        //            tblock1.Margin = new Thickness(0, 0, 0, 0);
        //            tblock1.FontWeight = FontWeights.SemiBold;
        //            //tblock1.FontSize = tblock1.FontSize + 1;

        //            Typeface typeFace = new Typeface(new FontFamily("Global User Interface"), tblock1.FontStyle, tblock1.FontWeight, tblock1.FontStretch);
        //            FormattedText ftxt = new FormattedText(tblock1.Text, System.Globalization.CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight, typeFace, tblock1.FontSize, Brushes.Black);

        //            RotateTransform rotate = new RotateTransform(270);

                    

        //            tblock1.RenderTransform = rotate;
        //            canvas.Children.Add(tblock1);

        //            border.Style = FindResource("IndividualReportBorderStyle") as Style;
        //            panel.Background = new SolidColorBrush(Colors.Tomato);
        //        }

        //        if (isToday)
        //        {
        //            border.BorderThickness = new Thickness(3, 3, 2, 3);
        //        }

        //        i++;
        //        lastBorder = border;
        //    }
        //    lastBorder.BorderThickness = new Thickness(1);
        //}
    }
}