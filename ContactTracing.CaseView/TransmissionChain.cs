using System;
using System.Collections;
using System.Collections.Generic;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ContactTracing.Core;
using ContactTracing.ViewModel;

namespace ContactTracing.CaseView
{

    public delegate void SphereSelectedHandler(int viewId, int recordId);

    public class TransmissionChain
    {
        //public event SphereSelectedHandler SphereSelected;
        private Epi.View view;
        private Epi.Data.IDbDriver db;
        private int counter;
        private double currentY;
        private Canvas transmissionCanvas;
        private Canvas transmissionDates;
        private MainWindow mainWindow;
        private DateTime firstOnset;
        private bool IsEnterOpen { get; set; }
        private bool IsLegacyDb;

        private EpiDataHelper DataHelper
        {
            get
            {
                return (mainWindow.DataContext as EpiDataHelper);
            }
        }

        private struct Position
        {
            public double Top;
            public double Left;
        }

        public TransmissionChain(Epi.View view, Epi.Data.IDbDriver db, Canvas transmissionCanvas, Canvas transmissionDates, MainWindow mainWindow)
        {
            this.db = db;
            this.view = view;
            this.transmissionCanvas = transmissionCanvas;
            this.transmissionDates = transmissionDates;
            this.mainWindow = mainWindow;
            this.IsEnterOpen = false;
        }

        public void SaveAsImage()
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.DefaultExt = ".png";
            dlg.Filter = "PNG Image (.png)|*.png";

            if (dlg.ShowDialog().Value)
            {
                BitmapSource img = (BitmapSource)ToImageSource(transmissionCanvas);

                FileStream stream = new FileStream(dlg.FileName, FileMode.Create);
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(img));
                encoder.Save(stream);
                stream.Close();
                MessageBox.Show(ContactTracing.CaseView.Properties.Resources.TransChainImageSaved);
            }
        }

        public ImageSource ToImageSource(FrameworkElement obj)
        {
            // Save current canvas transform
            System.Windows.Media.Transform transform = obj.LayoutTransform;

            // fix margin offset as well
            Thickness margin = obj.Margin;
            obj.Margin = new Thickness(0, 0,
                 margin.Right - margin.Left, margin.Bottom - margin.Top);

            // Get the size of canvas
            Size size = new Size(obj.ActualWidth, obj.ActualHeight);

            // force control to Update
            obj.Measure(size);
            obj.Arrange(new Rect(size));

            RenderTargetBitmap bmp = new RenderTargetBitmap(
                (int)obj.ActualWidth, (int)obj.ActualHeight, 96, 96, PixelFormats.Pbgra32);

            bmp.Render(obj);

            // return values as they were before
            obj.LayoutTransform = transform;
            obj.Margin = margin;
            return bmp;
        }

        public void Print()
        {
            /*PrintDialog dialog = new PrintDialog();
            if (dialog.ShowDialog() == true)
            {
                dialog.PrintVisual(transmissionCanvas, "Transmission Chain");
            }*/

            if (firstOnset != null)
            {
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog().Value)
                {
                    printDialog.PrintTicket.PageOrientation = System.Printing.PageOrientation.Landscape;
                    printDialog.PrintTicket.PageMediaSize = new System.Printing.PageMediaSize(System.Printing.PageMediaSizeName.ISOA4);
                    DocumentPaginator paginator = new TransmissionChainPaginator(transmissionCanvas, firstOnset);
                    printDialog.PrintDocument(paginator, "Transmission_Chain");
                }
            }
        }

        private void EditCase(int recordId)
        {
            Epi.Enter.EnterUIConfig uiConfig = Core.Common.GetCaseConfig(DataHelper.CaseForm, DataHelper.LabForm); 

            Epi.Windows.Enter.EnterMainForm emf = new Epi.Windows.Enter.EnterMainForm(this.view.Project, this.view, uiConfig);

            emf.LoadRecord(recordId);

            emf.RecordSaved += new Epi.SaveRecordEventHandler(mainWindow.emfCases_RecordSaved);
            emf.ShowDialog();
            emf.RecordSaved -= new Epi.SaveRecordEventHandler(mainWindow.emfCases_RecordSaved);
        }

        public void Initialize()
        {
            try
            {
                IsLegacyDb = this.view.Pages[4].Id > 5;

                string uniqueKeys = "";
                string parens = "(((";
                string joins = "";
                foreach (Epi.View epiView in view.Project.Views)
                {
                    uniqueKeys += "t" + epiView.Id + ".UniqueKey as Key" + epiView.Id + ", ";
                    parens += "(";
                    joins += "left outer join " + epiView.TableName + " t" + epiView.Id + " on m.ToRecordGuid = t" + epiView.Id + ".GlobalRecordId) ";
                }
                if (IsLegacyDb)
                {
                    parens += "(";
                    joins += " LEFT JOIN caseinformationform1 AS c ON t1.GlobalRecordId = c.GlobalRecordId) LEFT JOIN caseinformationform9 AS c9 ON t1.GlobalRecordId = c9.GlobalRecordId) LEFT JOIN caseinformationform3 AS c3 ON t1.GlobalRecordId = c3.GlobalRecordId) LEFT JOIN caseinformationform2 AS c2 ON t1.GlobalRecordId = c2.GlobalRecordId)";
                }
                else
                {
                    joins += " LEFT JOIN caseinformationform1 AS c ON t1.GlobalRecordId = c.GlobalRecordId) LEFT JOIN caseinformationform4 AS c9 ON t1.GlobalRecordId = c9.GlobalRecordId) LEFT JOIN caseinformationform3 AS c2 ON t1.GlobalRecordId = c2.GlobalRecordId) ";
                }
                uniqueKeys = uniqueKeys.Substring(0, uniqueKeys.Length - 2) + " ";

                Epi.Data.Query query;
                if (db.ToString().ToLower().Contains("sql"))
                {
                    query = db.CreateQuery(@"Select DateOnset, DateDeath, DateIsolationCurrent, EpiCaseDef, Tentative, Surname, Othernames, ID, Gender, Age, LastContactDate, RelationshipType, HCW, FromRecordGuid, ToRecordGuid, FromViewId, ToViewId, " + uniqueKeys + " from " + parens + "metaLinks m " + joins + " where m.ToRecordGuid not in (select fromrecordguid from metalinks ml inner join caseinformationform1 crf on ml.torecordguid = crf.globalrecordid where crf.epicasedef <> '0' and ToViewId <> 4) and DateOnset is not null and EpiCaseDef <> '0' ORDER BY DateOnset");
                }
                else
                {
                    query = db.CreateQuery(@"Select DateOnset, DateDeath, DateIsolationCurrent, EpiCaseDef, Tentative, Surname, Othernames, ID, Gender, Age, LastContactDate, RelationshipType, HCW, FromRecordGuid, ToRecordGuid, FromViewId, ToViewId, " + uniqueKeys + " from " + parens + "metaLinks m " + joins + " where m.ToRecordGuid not in (select fromrecordguid from metalinks ml inner join caseinformationform1 crf on ml.torecordguid = crf.globalrecordid where crf.epicasedef <> '0' and ToViewId <> 4) and DateOnset <> null and EpiCaseDef <> '0' ORDER BY DateOnset");
                }
                DataTable dt = db.Select(query);

                if (dt.Rows.Count > 0)
                {
                    List<string> uniqueGuids = new List<string>();

                    currentY = 10;
                    firstOnset = (DateTime)dt.Rows[0]["DateOnset"];
                    for (int days = 0; days < 256; days += 7)
                    {
                        TextBlock tb = new TextBlock();
                        tb.Text = "|" + firstOnset.AddDays(days).ToString("dd-MMM");
                        tb.FontWeight = FontWeights.SemiBold;
                        tb.Foreground = Brushes.White;
                        tb.FontSize = 14;
                        Canvas.SetTop(tb, 0);
                        Canvas.SetLeft(tb, (days * Constants.TRANS_CHAIN_SCALE) + 10);
                        transmissionDates.Children.Add(tb);
                    }

                    foreach (DataRow row in dt.Rows)
                    {
                        if (!uniqueGuids.Contains(row["ToRecordGuid"].ToString()))
                        {
                            double dateDiff = ((DateTime)row["DateOnset"]).Subtract(firstOnset).TotalDays;

                            uniqueGuids.Add(row["ToRecordGuid"].ToString());
                            Hashtable nodes = new Hashtable();
                            List<string> connectors = new List<string>();
                            counter = 1;
                            bool derivedOther = false;
                            DateTime otherDate;
                            if (row["DateDeath"] == DBNull.Value && row["DateIsolationCurrent"] != DBNull.Value)
                            {
                                otherDate = (DateTime)row["DateIsolationCurrent"];
                            }
                            else if (row["DateDeath"] != DBNull.Value && row["DateIsolationCurrent"] == DBNull.Value)
                            {
                                otherDate = (DateTime)row["DateDeath"];
                            }
                            else if (row["DateDeath"] != DBNull.Value && row["DateIsolationCurrent"] != DBNull.Value)
                            {
                                otherDate = (DateTime)row["DateDeath"] < (DateTime)row["DateIsolationCurrent"] ? (DateTime)row["DateDeath"] : (DateTime)row["DateIsolationCurrent"];
                            }
                            else
                            {
                                otherDate = ((DateTime)row["DateOnset"]).AddDays(10);
                                derivedOther = true;
                            }
                            double daysIll = otherDate.Subtract((DateTime)row["DateOnset"]).TotalDays;
                            if (daysIll < 0)
                                daysIll = 0;

                            Rectangle center = AddRectangleToCanvas(Colors.Red, daysIll * Constants.TRANS_CHAIN_SCALE/2, 40, "Current Record: " + row["ToRecordGuid"], true, new SphereInfo(row["ToRecordGuid"].ToString(), view.Id, (int)row["Key1"],(DateTime)row["DateOnset"], otherDate, derivedOther, row["Surname"].ToString() + " " + row["Othernames"].ToString(), row["ID"].ToString(), row["Gender"].ToString(), row["Age"].ToString(), row["DateDeath"] != DBNull.Value, (bool)row["HCW"]));
                            //Rectangle center = AddRectangleToCanvas(Colors.Red, daysIll * Constants.TRANS_CHAIN_SCALE / 2, 40, "Current Record: " + row["ToRecordGuid"], true, new SphereInfo(row["ToRecordGuid"].ToString(), view.Id, (DateTime)row["DateOnset"], otherDate, derivedOther, row["Surname"].ToString() + " " + row["Othernames"].ToString(), row["ID"].ToString(), row["Gender"].ToString(), row["Age"].ToString(), row["DateDeath"] != DBNull.Value, (bool)row["HCW"]));
                            double circleTop = currentY > 10 ? currentY + 107 : currentY;
                            double circleLeft = 10 + (Constants.TRANS_CHAIN_SCALE * dateDiff);

                            Canvas.SetTop(center, circleTop);// (MainCanvas.ActualHeight / 2) - 25);
                            Canvas.SetLeft(center, circleLeft);// (MainCanvas.ActualWidth / 2) - 25);
                            nodes.Add(row["ToRecordGuid"].ToString(), center);
                            AddSpheres(false, row["ToRecordGuid"].ToString(), center, nodes, connectors, 175, 40, (DateTime)row["DateOnset"]);
                            AddConnections(nodes, connectors);

                            TextBlock tb1 = new TextBlock();
                            tb1.Text = row["ID"].ToString();
                            tb1.MouseEnter += new MouseEventHandler(tb_MouseEnter);
                            tb1.MouseLeave += new MouseEventHandler(tb_MouseLeave);
                            tb1.Tag = center;
                            Canvas.SetTop(tb1, circleTop + 5);
                            Canvas.SetLeft(tb1, circleLeft + 10);
                            Canvas.SetZIndex(tb1, 300);
                            tb1.Cursor = Cursors.Hand;
                            AddVisualToCanvas(tb1);

                            TextBlock tb2 = new TextBlock();
                            tb2.Text = row["Surname"].ToString() + " " + row["Othernames"].ToString();
                            tb2.MouseEnter += new MouseEventHandler(tb_MouseEnter);
                            tb2.MouseLeave += new MouseEventHandler(tb_MouseLeave);
                            tb2.Tag = center;
                            Canvas.SetTop(tb2, circleTop + 20);
                            Canvas.SetLeft(tb2, circleLeft + 10);
                            Canvas.SetZIndex(tb2, 300);
                            tb2.Cursor = Cursors.Hand;
                            AddVisualToCanvas(tb2);

                            string sex = string.Empty;
                            if (row["Gender"].ToString().Equals("1"))
                            {
                                sex = ContactTracing.CaseView.Properties.Resources.Male;
                            }
                            else if (row["Gender"].ToString().Equals("2"))
                            {
                                sex = ContactTracing.CaseView.Properties.Resources.Female;
                            }

                            TextBlock tb3 = new TextBlock();
                            tb3.Text = sex + ", " + Properties.Resources.ColHeaderAge + ": " + row["Age"].ToString();
                            tb3.MouseEnter += new MouseEventHandler(tb_MouseEnter);
                            tb3.MouseLeave += new MouseEventHandler(tb_MouseLeave);
                            tb3.Tag = center;
                            Canvas.SetTop(tb3, circleTop + 35);
                            Canvas.SetLeft(tb3, circleLeft + 10);
                            Canvas.SetZIndex(tb3, 300);
                            tb3.Cursor = Cursors.Hand;
                            AddVisualToCanvas(tb3);

                            string end;
                            if (row["DateDeath"] != DBNull.Value)
                            {
                                end = " " + Properties.Resources.Death + ": ";
                            }
                            else
                            {
                                end = " Iso: ";
                            }

                            TextBlock tb4 = new TextBlock();
                            if (derivedOther)
                            {
                                tb4.Text = Properties.Resources.Onset + ": " + ((DateTime)row["DateOnset"]).ToString("dd-MM");
                            }
                            else
                            {
                                tb4.Text = Properties.Resources.Onset + ": " + ((DateTime)row["DateOnset"]).ToString("dd-MM") + end + otherDate.ToString("dd-MM");
                            }
                            tb4.MouseEnter += new MouseEventHandler(tb_MouseEnter);
                            tb4.MouseLeave += new MouseEventHandler(tb_MouseLeave);
                            tb4.Tag = center;
                            Canvas.SetTop(tb4, circleTop + 50);
                            Canvas.SetLeft(tb4, circleLeft + 10);
                            Canvas.SetZIndex(tb4, 300);
                            tb4.Cursor = Cursors.Hand;
                            AddVisualToCanvas(tb4);

                            if (row["DateDeath"] != DBNull.Value)
                            {
                                TextBlock tbd = new TextBlock();
                                tbd.Text = "+";
                                tbd.FontWeight = FontWeights.UltraBold;
                                tbd.FontSize = 25;
                                Canvas.SetTop(tbd, circleTop + 50);
                                Canvas.SetLeft(tbd, circleLeft);
                                Canvas.SetZIndex(tbd, 300);
                                tbd.Cursor = Cursors.Hand;
                                AddVisualToCanvas(tbd);
                            }

                            if ((bool)row["HCW"])
                            {
                                TextBlock tbh = new TextBlock();
                                tbh.Text = "H";
                                tbh.FontWeight = FontWeights.UltraBold;
                                tbh.FontSize = 16;
                                Canvas.SetTop(tbh, circleTop + 59);
                                if (row["DateDeath"] != DBNull.Value)
                                {
                                    Canvas.SetLeft(tbh, circleLeft + 15);
                                }
                                else
                                {
                                    Canvas.SetLeft(tbh, circleLeft + 2);
                                }
                                Canvas.SetZIndex(tbh, 300);
                                tbh.Cursor = Cursors.Hand;
                                AddVisualToCanvas(tbh);
                            }
                        }
                    }
                    transmissionCanvas.Height = currentY + 200;
                }
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show("An error occurred during production of the transmission chain. Please make sure each case's date of onset occurs prior to any date of death. Exception: " + ex.Message);
            }
            catch (Exception ex)
            {
                int x = 5;
                x++;
            }
        }

        private void AddConnections(Hashtable nodes, List<string> connectors)
        {
            foreach (string connector in connectors)
            {
                Rectangle from = (Rectangle)nodes[connector.Split(',')[0]];
                Rectangle to = (Rectangle)nodes[connector.Split(',')[1]];
                bool tentative = false;
                string lastContactDate = string.Empty;
                string relationship = string.Empty;
                if (connector.Split(',').Count() > 2)
                {
                    tentative = connector.Split(',')[2].Equals("1");
                }
                if (connector.Split(',').Count() > 3)
                {
                    lastContactDate = connector.Split(',')[3];
                    if (!string.IsNullOrEmpty(lastContactDate))
                    {
                        lastContactDate = DateTime.Parse(lastContactDate).ToString("dd MMM");
                    }
                }
                if (connector.Split(',').Count() > 4)
                {
                    relationship = connector.Split(',')[4];
                }
                Line line = new Line();
                line.X1 = Canvas.GetLeft(from) + (from.Width);
                line.Y1 = Canvas.GetTop(from) + (from.Height / 2);

                if (Canvas.GetLeft(to) < line.X1)
                {
                    if ((Canvas.GetLeft(to) + to.Width) < line.X1)
                    {
                        line.X2 = Canvas.GetLeft(to) + to.Width;// +8;
                    }
                    else
                    {
                        line.X2 = line.X1;
                    }
                }
                else
                {
                    line.X2 = Canvas.GetLeft(to);// -5;
                }
                if (Canvas.GetTop(to) < Canvas.GetTop(from))
                {
                    line.Y2 = Canvas.GetTop(to) + to.Height;
                }
                else if (Canvas.GetTop(to) > Canvas.GetTop(from))
                {
                    line.Y2 = Canvas.GetTop(to);
                }
                else
                {
                    line.Y2 = Canvas.GetTop(to) +(to.Height / 2);
                }
                line.Stroke = Brushes.DarkGray;
                line.StrokeThickness = 1;
                line.StrokeEndLineCap = PenLineCap.Triangle;
                line.Tag = to;

                if (tentative)
                {
                    line.StrokeDashArray = new DoubleCollection { 3 };
                }

                Point endPoint = GetVectorEndPoint(line, 6);
                line.X2 = endPoint.X;
                line.Y2 = endPoint.Y;
                Point arrowStartPoint = GetVectorEndPoint(line, 2);
                Line arrowHead = new Line();
                arrowHead.X1 = arrowStartPoint.X;
                arrowHead.Y1 = arrowStartPoint.Y;
                arrowHead.X2 = line.X2;
                arrowHead.Y2 = line.Y2;
                arrowHead.Stroke = Brushes.DarkGray;
                arrowHead.StrokeThickness = 20;
                arrowHead.StrokeEndLineCap = PenLineCap.Triangle;
                arrowHead.Tag = to;
                arrowHead.ToolTip = lastContactDate + " - " + relationship;

                transmissionCanvas.Children.Add(line);
                transmissionCanvas.Children.Add(arrowHead);
            }
        }

        private Point GetVectorEndPoint(Line line, double radius)
        {
            double adjacent = line.Y2 - line.Y1;
            double opposite = line.X1 - line.X2;
            double angle = Math.Atan(opposite / adjacent);
            Point point;
            if (line.Y2 >= line.Y1)
            {
                point = new Point(line.X2 + (radius * Math.Sin(angle)), line.Y2 - (radius * Math.Cos(angle)));
            }
            else
            {
                point = new Point(line.X2 - (radius * Math.Sin(angle)), line.Y2 + (radius * Math.Cos(angle)));
            }
            return point;
        }

        private DataTable GetToData(string toRecordGuid)
        {
            string uniqueKeys = "";
            string parens = "(((";
            string joins = "";
            foreach (Epi.View epiView in view.Project.Views)
            {
                uniqueKeys += "t" + epiView.Id + ".UniqueKey as Key" + epiView.Id + ", ";
                parens += "(";
                joins += "left outer join " + epiView.TableName + " t" + epiView.Id + " on m.FromRecordGuid = t" + epiView.Id + ".GlobalRecordId) ";
            }
            if (IsLegacyDb)
            {
                parens += "(";
                joins += " LEFT JOIN caseinformationform1 AS c ON t1.GlobalRecordId = c.GlobalRecordId) LEFT JOIN caseinformationform9 AS c9 ON t1.GlobalRecordId = c9.GlobalRecordId) LEFT JOIN caseinformationform3 AS c3 ON t1.GlobalRecordId = c3.GlobalRecordId) LEFT JOIN caseinformationform2 AS c2 ON t1.GlobalRecordId = c2.GlobalRecordId)";
            }
            else
            {
                joins += " LEFT JOIN caseinformationform1 AS c ON t1.GlobalRecordId = c.GlobalRecordId) LEFT JOIN caseinformationform4 AS c9 ON t1.GlobalRecordId = c9.GlobalRecordId) LEFT JOIN caseinformationform3 AS c2 ON t1.GlobalRecordId = c2.GlobalRecordId) ";
            }
            uniqueKeys = uniqueKeys.Substring(0, uniqueKeys.Length - 2) + " ";

            Epi.Data.Query query;
            if (db.ToString().ToLower().Contains("sql"))
            {
                query = db.CreateQuery(@"Select DateOnset, DateDeath, DateIsolationCurrent, EpiCaseDef, Tentative, Surname, Othernames, ID, Gender, Age, LastContactDate, RelationshipType, HCW, FromRecordGuid, ToRecordGuid, FromViewId, ToViewId, " + uniqueKeys + " from " + parens + "metaLinks m " + joins + " where m.ToRecordGuid = @GlobalRecordId and DateOnset is not null and EpiCaseDef <> '0' and ToViewId <> 4 ORDER BY DateOnset");
            }
            else
            {
                query = db.CreateQuery(@"Select DateOnset, DateDeath, DateIsolationCurrent, EpiCaseDef, Tentative, Surname, Othernames, ID, Gender, Age, LastContactDate, RelationshipType, HCW, FromRecordGuid, ToRecordGuid, FromViewId, ToViewId, " + uniqueKeys + " from " + parens + "metaLinks m " + joins + " where m.ToRecordGuid = @GlobalRecordId and DateOnset <> null and EpiCaseDef <> '0' and ToViewId <> 4 ORDER BY DateOnset");
            }
            query.Parameters.Add(new Epi.Data.QueryParameter("@GlobalRecordId", DbType.StringFixedLength, toRecordGuid));
            return db.Select(query);
        }

        private void AddSpheres(bool rotate, string globalRecordId, Rectangle center, Hashtable nodes, List<string> connectors, double connectorLength, double radius, DateTime currentDate)
        {
            List<Rectangle> localCircles = new List<Rectangle>();
            DataTable toLinks = GetToData(globalRecordId);

            foreach (DataRow link in toLinks.Rows)
            {
                // Ugly hack to get TC working again
                // TODO: Find out why this field is coming through blank to begin with and correct at the source; for now, this seems to get TC working though
                if (link["Key" + link["ToViewId"].ToString()] == null || link["Key" + link["ToViewId"].ToString()] == DBNull.Value)
                {
                    continue;
                }

                Rectangle circle;

                if (nodes.ContainsKey(link["FromRecordGuid"]))
                {
                    circle = (Rectangle)nodes[link["FromRecordGuid"]];
                }
                else
                {
                    counter++;

                    bool derivedOther = false;

                    DateTime otherDate;
                    if (link["DateDeath"] == DBNull.Value && link["DateIsolationCurrent"] != DBNull.Value)
                    {
                        otherDate = (DateTime)link["DateIsolationCurrent"];
                    }
                    else if (link["DateDeath"] != DBNull.Value && link["DateIsolationCurrent"] == DBNull.Value)
                    {
                        otherDate = (DateTime)link["DateDeath"];
                    }
                    else if (link["DateDeath"] != DBNull.Value && link["DateIsolationCurrent"] != DBNull.Value)
                    {
                        otherDate = (DateTime)link["DateDeath"] < (DateTime)link["DateIsolationCurrent"] ? (DateTime)link["DateDeath"] : (DateTime)link["DateIsolationCurrent"];
                    }
                    else
                    {
                        otherDate = ((DateTime)link["DateOnset"]).AddDays(10);
                        derivedOther = true;
                    }

                    double daysIll = otherDate.Subtract((DateTime)link["DateOnset"]).TotalDays;
                    if (daysIll < 0)
                        daysIll = 0;

                    circle = AddRectangleToCanvas(Colors.White, daysIll * Constants.TRANS_CHAIN_SCALE / 2, radius, "Onset Date: " + link["DateOnset"].ToString(), link["EpiCaseDef"].ToString().Equals("1"), new SphereInfo(link["FromRecordGuid"].ToString(), (int)link["ToViewId"], (int)link["Key" + link["ToViewId"].ToString()], (DateTime)link["DateOnset"], otherDate, derivedOther, link["Surname"].ToString() + " " + link["Othernames"].ToString(), link["ID"].ToString(), link["Gender"].ToString(), link["Age"].ToString(), link["DateDeath"] != DBNull.Value, (bool)link["HCW"]));
                    //circle = AddRectangleToCanvas(Colors.White, daysIll * Constants.TRANS_CHAIN_SCALE / 2, radius, "Onset Date: " + link["DateOnset"].ToString(), link["EpiCaseDef"].ToString().Equals("1"), new SphereInfo(link["FromRecordGuid"].ToString(), (int)link["ToViewId"], (DateTime)link["DateOnset"], otherDate, derivedOther, link["Surname"].ToString() + " " + link["Othernames"].ToString(), link["ID"].ToString(), link["Gender"].ToString(), link["Age"].ToString(), link["DateDeath"] != DBNull.Value, (bool)link["HCW"]));
                    nodes.Add(link["FromRecordGuid"], circle);
                    localCircles.Add(circle);
                }
                if (!connectors.Contains(link["FromRecordGuid"] + "," + globalRecordId) && !connectors.Contains(globalRecordId + "," + link["FromRecordGuid"]))
                {
                    connectors.Add(circle.Tag == null ? circle.ToString() : globalRecordId + "," + circle.Tag.ToString() + "," + link["Tentative"] + "," + link["LastContactDate"] + "," + link["RelationshipType"]);
                }
            }

            Rectangle prevCircle = null;
            int localCircleCounter = 0;
            foreach (Rectangle circle in localCircles)
            {
                Position position = GetChildLocation(rotate, localCircleCounter, Canvas.GetTop(center) + (center.Height / 2), Canvas.GetLeft(center) + (center.Height / 2), connectorLength, localCircles.Count, ((SphereInfo)circle.Tag).OnsetDate.Subtract(currentDate).TotalDays);

                double circleTop = position.Top - (circle.Height / 2);
                double circleLeft = position.Left - (circle.Height / 2);

                Canvas.SetTop(circle, circleTop);
                Canvas.SetLeft(circle, circleLeft);

                TextBlock tb1 = new TextBlock();
                tb1.Text = ((SphereInfo)circle.Tag).CaseId;
                tb1.MouseEnter += new MouseEventHandler(tb_MouseEnter);
                tb1.MouseLeave += new MouseEventHandler(tb_MouseLeave);
                tb1.Tag = circle;
                Canvas.SetTop(tb1, circleTop + 5);
                Canvas.SetLeft(tb1, circleLeft + 10);
                Canvas.SetZIndex(tb1, 300);
                tb1.Cursor = Cursors.Hand;
                AddVisualToCanvas(tb1);

                TextBlock tb2 = new TextBlock();
                tb2.Text = ((SphereInfo)circle.Tag).Name;
                tb2.MouseEnter += new MouseEventHandler(tb_MouseEnter);
                tb2.MouseLeave += new MouseEventHandler(tb_MouseLeave);
                tb2.Tag = circle;
                Canvas.SetTop(tb2, circleTop + 20);
                Canvas.SetLeft(tb2, circleLeft + 10);
                Canvas.SetZIndex(tb2, 300);
                tb2.Cursor = Cursors.Hand;
                AddVisualToCanvas(tb2);

                string sex = string.Empty;
                if (((SphereInfo)circle.Tag).Sex.Equals("1"))
                {
                    sex = ContactTracing.CaseView.Properties.Resources.Male;
                }
                else if (((SphereInfo)circle.Tag).Sex.Equals("2"))
                {
                    sex = ContactTracing.CaseView.Properties.Resources.Female;
                }

                TextBlock tb3 = new TextBlock();
                tb3.Text = sex + ", " + Properties.Resources.ColHeaderAge + ": " + ((SphereInfo)circle.Tag).Age;
                tb3.MouseEnter += new MouseEventHandler(tb_MouseEnter);
                tb3.MouseLeave += new MouseEventHandler(tb_MouseLeave);
                tb3.Tag = circle;
                Canvas.SetTop(tb3, circleTop + 35);
                Canvas.SetLeft(tb3, circleLeft + 10);
                Canvas.SetZIndex(tb3, 300);
                tb3.Cursor = Cursors.Hand;
                AddVisualToCanvas(tb3);

                string end;
                if (((SphereInfo)circle.Tag).Dead)
                {
                    end = " " + Properties.Resources.Death + ": ";
                }
                else
                {
                    end = " Iso: ";
                }

                TextBlock tb4 = new TextBlock();
                if (((SphereInfo)circle.Tag).DerivedOther)
                {
                    tb4.Text = Properties.Resources.Onset + ": " + ((SphereInfo)circle.Tag).OnsetDate.ToString("dd-MM");
                }
                else
                {
                    tb4.Text = Properties.Resources.Onset + ": " + ((SphereInfo)circle.Tag).OnsetDate.ToString("dd-MM") + end + ((SphereInfo)circle.Tag).OtherDate.ToString("dd-MM");
                }
                tb4.MouseEnter += new MouseEventHandler(tb_MouseEnter);
                tb4.MouseLeave += new MouseEventHandler(tb_MouseLeave);
                tb4.Tag = circle;
                Canvas.SetTop(tb4, circleTop + 50);
                Canvas.SetLeft(tb4, circleLeft + 10);
                Canvas.SetZIndex(tb4, 300);
                tb4.Cursor = Cursors.Hand;
                AddVisualToCanvas(tb4);

                if (((SphereInfo)circle.Tag).Dead)
                {
                    TextBlock tbd = new TextBlock();
                    tbd.Text = "+";
                    tbd.FontWeight = FontWeights.UltraBold;
                    tbd.FontSize = 25;
                    Canvas.SetTop(tbd, circleTop + 50);
                    Canvas.SetLeft(tbd, circleLeft);
                    Canvas.SetZIndex(tbd, 300);
                    tbd.Cursor = Cursors.Hand;
                    AddVisualToCanvas(tbd);
                }

                if (((SphereInfo)circle.Tag).HealthCareWorker)
                {
                    TextBlock tbh = new TextBlock();
                    tbh.Text = "H";
                    tbh.FontWeight = FontWeights.UltraBold;
                    tbh.FontSize = 16;
                    Canvas.SetTop(tbh, circleTop + 59);
                    if (((SphereInfo)circle.Tag).Dead)
                    {
                        Canvas.SetLeft(tbh, circleLeft + 15);
                    }
                    else
                    {
                        Canvas.SetLeft(tbh, circleLeft + 2);
                    }
                    Canvas.SetZIndex(tbh, 300);
                    tbh.Cursor = Cursors.Hand;
                    AddVisualToCanvas(tbh);
                }

                AddSpheres(!rotate, circle.Tag.ToString(), circle, nodes, connectors, connectorLength, radius, ((SphereInfo)circle.Tag).OnsetDate);
                prevCircle = circle;
                localCircleCounter++;
            }
        }

        void tb_MouseLeave(object sender, MouseEventArgs e)
        {
            ellipse_MouseLeave(((TextBlock)sender).Tag, e);
        }

        void tb_MouseEnter(object sender, MouseEventArgs e)
        {
            ellipse_MouseEnter(((TextBlock)sender).Tag, e);
        }

        private Position GetChildLocation(bool rotate, int childNumber, double parentTop, double parentLeft, double radius, int totalChildren, double dateDiff)
        {
            double angle = (2.0 * System.Math.PI * childNumber / totalChildren);
            if (rotate)
            {
                angle += System.Math.PI / 4;
            }
            Position position;
            position.Top = parentTop + (107 * childNumber) + 107;// radius * System.Math.Sin(angle);
            if (currentY + 107 >= position.Top)
            {
                position.Top = currentY + 107;
            }
            if (position.Top > currentY)
            {
                currentY = position.Top;
            }

            position.Left = parentLeft + (Constants.TRANS_CHAIN_SCALE * dateDiff);// -radius * System.Math.Cos(angle);
            return position;
        }

        #region Rendering

        private Rectangle AddRectangleToCanvas(Color color, double radiusX, double radiusY, string toolTip, bool confirmed, SphereInfo tag)
        {
            Rectangle ellipse = new Rectangle();
            ellipse.RadiusX = 5;
            ellipse.RadiusY = 5;
            ellipse.StrokeThickness = 0.5;
            ellipse.Stroke = Brushes.Black;
            if (!confirmed)
            {
                ellipse.StrokeDashArray = new DoubleCollection { 3 };
            }
            else
            {
                ellipse.StrokeThickness = 2;
            }
            //ellipse.ToolTip = toolTip;
            ellipse.Tag = tag;
            ellipse.Fill = Brushes.White;// GetDefaultBrush();
            ellipse.Width = radiusX * 2;
            ellipse.Height = radiusY * 2;
            ellipse.Cursor = Cursors.Hand;
            ellipse.MouseDown += new MouseButtonEventHandler(ellipse_MouseDown);
            ellipse.MouseEnter += new MouseEventHandler(ellipse_MouseEnter);
            ellipse.MouseLeave += new MouseEventHandler(ellipse_MouseLeave);
            Canvas.SetZIndex(ellipse, 99);

            AddVisualToCanvas(ellipse);

            return ellipse;
        }

        void ellipse_MouseLeave(object sender, MouseEventArgs e)
        {
            Rectangle ellipse = (Rectangle)sender;
            foreach (UIElement element in transmissionCanvas.Children)
            {
                if (element is Line)
                {
                    if (((Line)element).Tag != null)
                    {
                        if (((Line)element).Tag == ellipse)
                        {
                            ((Line)element).Stroke = Brushes.DarkGray;
                            if (((Line)element).StrokeThickness < 20)
                                ((Line)element).StrokeThickness = 1;
                        }
                    }
                }
            }
        }

        void ellipse_MouseEnter(object sender, MouseEventArgs e)
        {
            Rectangle ellipse = (Rectangle)sender;
            foreach (UIElement element in transmissionCanvas.Children)
            {
                if (element is Line)
                {
                    if (((Line)element).Tag != null)
                    {
                        if (((Line)element).Tag == ellipse)
                        {
                            ((Line)element).Stroke = Brushes.Red;
                            if (((Line)element).StrokeThickness < 20)
                                ((Line)element).StrokeThickness = 3;
                        }
                    }
                }
            }
        }

        private void AddVisualToCanvas(FrameworkElement visual)
        {

            transmissionCanvas.Children.Add(visual);

            visual.IsHitTestVisible = true;
        }

        private void ellipse_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsEnterOpen)
            {
                int recordId = ((SphereInfo)((System.Windows.Shapes.Rectangle)e.Source).Tag).RecordId;
                System.ComponentModel.BackgroundWorker enterLoader = new System.ComponentModel.BackgroundWorker();
                enterLoader.DoWork += new System.ComponentModel.DoWorkEventHandler(enterLoader_DoWork);
                enterLoader.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(enterLoader_RunWorkerCompleted);

                transmissionCanvas.Cursor = Cursors.Wait;
                ((FrameworkElement)sender).Cursor = Cursors.Wait;

                enterLoader.RunWorkerAsync(new KeyValuePair<int, object>(recordId, sender));
            }
        }

        void enterLoader_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            EditCase(((KeyValuePair<int, object>)e.Result).Key);
            transmissionCanvas.Cursor = Cursors.Arrow;
            ((FrameworkElement)(((KeyValuePair<int, object>)e.Result).Value)).Cursor = Cursors.Hand;
            IsEnterOpen = false;
        }

        void enterLoader_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            IsEnterOpen = true;
            System.Threading.Thread.Sleep(10);
            e.Result = e.Argument;
        }

        #endregion

    }

    public class SphereInfo
    {
        public string GlobalRecordId { get; set; }
        public int ViewId { get; set; }
        public int RecordId { get; set; }
        public DateTime OnsetDate { get; set; }
        public DateTime OtherDate { get; set; }
        public string Name { get; set; }
        public string CaseId { get; set; }
        public string Sex { get; set; }
        public string Age { get; set; }
        public bool Dead { get; set; }
        public bool DerivedOther { get; set; }
        public bool HealthCareWorker { get; set; }

        public SphereInfo(string globalRecordId, int viewId, int recordId, DateTime onsetDate, DateTime otherDate, bool derivedOther, string name, string caseId, string sex, string age, bool dead, bool healthCareWorker)
        {
            this.GlobalRecordId = globalRecordId;
            this.ViewId = viewId;
            this.RecordId = recordId;
            this.OnsetDate = onsetDate;
            this.OtherDate = otherDate;
            this.Name = name;
            this.CaseId = caseId;
            this.Sex = sex;
            this.Age = age;
            this.Dead = dead;
            this.DerivedOther = derivedOther;
            this.HealthCareWorker = healthCareWorker;
        }

        public override string ToString()
        {
            return this.GlobalRecordId;
        }

    }

}