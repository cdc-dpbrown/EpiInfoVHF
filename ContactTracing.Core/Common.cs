using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Epi;
using Epi.Fields;
using Epi.Data;

namespace ContactTracing.Core
{
    public static class Common
    {
        private static int _daysInWindow = 21;

        //public static bool Contains(this string source, string toCheck, StringComparison comp)
        //{
        //    Contract.Requires(source != null);

        //    return source.IndexOf(toCheck, comp) >= 0;
        //}

        public static string GetCommentLegalLabel(DDLFieldOfCommentLegal commentLegalField, DataRow row, string fieldName, IDbDriver database)
        {
            if (commentLegalField != null)
            {
                string phaCode = row[fieldName].ToString();

                string columnName = commentLegalField.TextColumnName;
                string tableName = commentLegalField.SourceTableName;

                Query selectQuery = database.CreateQuery("SELECT * FROM " + tableName); // may be inefficient if called over many contacts, perhaps look at caching this table and passing it in?
                DataTable phaTable = database.Select(selectQuery);

                foreach (DataRow phaRow in phaTable.Rows)
                {
                    string rowValue = phaRow[0].ToString();
                    int dashIndex = rowValue.IndexOf("-");

                    if (dashIndex >= 0 && rowValue.Length >= dashIndex + 1)
                    {
                        string rowCode = rowValue.Substring(0, dashIndex);
                        string rowLabel = rowValue.Substring(dashIndex + 1).Trim();

                        if (rowCode.Equals(phaCode, StringComparison.OrdinalIgnoreCase))
                        {
                            return rowLabel;
                        }
                    }   
                }
            }

            return String.Empty;
        }

        /// <summary>
        /// Finds the MAC address of the NIC with maximum speed.
        /// </summary>
        /// <returns>The MAC address</returns>
        public static string GetMacAddress()
        {
            const int MIN_MAC_ADDR_LENGTH = 12;
            string macAddress = string.Empty;
            long maxSpeed = -1;

            foreach (System.Net.NetworkInformation.NetworkInterface nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                string tempMac = nic.GetPhysicalAddress().ToString();
                if (nic.Speed > maxSpeed &&
                    !string.IsNullOrEmpty(tempMac) &&
                    tempMac.Length >= MIN_MAC_ADDR_LENGTH)
                {
                    maxSpeed = nic.Speed;
                    macAddress = tempMac;
                }
            }

            return macAddress;
        }

        public static string ContactTypeConverter(int input)
        {
            Epi.WordBuilder wb = new Epi.WordBuilder(",");
            string bits = Convert.ToString(input, 2);
            switch (bits.Length)
            {
                case 1:
                    bits = "000" + bits;
                    break;
                case 2:
                    bits = "00" + bits;
                    break;
                case 3:
                    bits = "0" + bits;
                    break;
            }

            if (bits[0].Equals('1'))
            {
                wb.Add("1");
            }
            if (bits[1].Equals('1'))
            {
                wb.Add("2");
            }
            if (bits[2].Equals('1'))
            {
                wb.Add("3");
            }
            if (bits[3].Equals('1'))
            {
                wb.Add("4");
            }

            return wb.ToString();
        }

        public static int DaysInWindow
        {
            get
            {
                return _daysInWindow;
            }
            set
            {
                if (value == 21 || value == 14)
                {
                    _daysInWindow = value;
                }
                else
                {
                    throw new ApplicationException("The number of days in the contact follow-up window can only be 21 or 14. The application attempted to set a value of " + value.ToString() + ".");
                }
            }
        }

        public static void Print(FrameworkElement visual)
        {
            PrintDialog dialog = new PrintDialog();
            if (dialog.ShowDialog() == true)
            {
                System.Printing.PrintCapabilities capabilities = dialog.PrintQueue.GetPrintCapabilities(dialog.PrintTicket);
                double scale = Math.Min(capabilities.PageImageableArea.ExtentWidth / visual.ActualWidth, capabilities.PageImageableArea.ExtentHeight / visual.ActualHeight);
                Transform originalTransform = visual.LayoutTransform;
                visual.LayoutTransform = new ScaleTransform(scale, scale);
                Size sz = new Size(capabilities.PageImageableArea.ExtentWidth, capabilities.PageImageableArea.ExtentHeight);
                visual.Measure(sz);
                visual.Arrange(new Rect(new Point(capabilities.PageImageableArea.OriginWidth, capabilities.PageImageableArea.OriginHeight), sz));
                dialog.PrintVisual(visual, "Chart");
                visual.LayoutTransform = originalTransform;
            }
        }

        public static Epi.Enter.EnterUIConfig GetCaseConfig(View caseForm, View labForm)
        {
            Epi.Enter.EnterUIConfig uiConfig = new Epi.Enter.EnterUIConfig();

            uiConfig.AllowOneRecordOnly.Add(caseForm, true);
            uiConfig.AllowOneRecordOnly.Add(labForm, false);
            
            var type = uiConfig.GetType();
            PropertyInfo prop = type.GetProperty("DisableCodeTableCache");
            if (prop != null)
            {
                Dictionary<View, bool> dictionary = prop.GetValue(uiConfig, null) as Dictionary<View, bool>;
                dictionary.Add(caseForm, true);
                dictionary.Add(labForm, true);
            }

            uiConfig.ShowDashboardButton.Add(caseForm, false);
            uiConfig.ShowDashboardButton.Add(labForm, true);

            uiConfig.ShowDeleteButtons.Add(caseForm, false);
            uiConfig.ShowDeleteButtons.Add(labForm, false);

            uiConfig.ShowEditFormButton.Add(caseForm, false);
            uiConfig.ShowEditFormButton.Add(labForm, false);

            uiConfig.ShowFileMenu.Add(caseForm, false);
            uiConfig.ShowFileMenu.Add(labForm, false);

            uiConfig.ShowFindButton.Add(caseForm, false);
            uiConfig.ShowFindButton.Add(labForm, false);

            uiConfig.ShowLineListButton.Add(caseForm, false);
            uiConfig.ShowLineListButton.Add(labForm, false);

            uiConfig.ShowMapButton.Add(caseForm, false);
            uiConfig.ShowMapButton.Add(labForm, false);

            uiConfig.ShowNavButtons.Add(caseForm, false);
            uiConfig.ShowNavButtons.Add(labForm, true);

            uiConfig.ShowNewRecordButton.Add(caseForm, false);
            uiConfig.ShowNewRecordButton.Add(labForm, true);

            uiConfig.ShowOpenFormButton.Add(caseForm, false);
            uiConfig.ShowOpenFormButton.Add(labForm, false);

            uiConfig.ShowPrintButton.Add(caseForm, true);
            uiConfig.ShowPrintButton.Add(labForm, true);

            uiConfig.ShowRecordCounter.Add(caseForm, false);
            uiConfig.ShowRecordCounter.Add(labForm, true);

            uiConfig.ShowSaveRecordButton.Add(caseForm, false);
            uiConfig.ShowSaveRecordButton.Add(labForm, false);

            uiConfig.ShowToolbar.Add(caseForm, true);
            uiConfig.ShowToolbar.Add(labForm, true);

            uiConfig.ShowLinkedRecordsViewer = false;

            return uiConfig;
        }

        public static string GetHtmlHeader()
        {
            StringBuilder htmlBuilder = new StringBuilder();

            htmlBuilder.AppendLine("<!DOCTYPE html>");
            htmlBuilder.AppendLine("<html class=\"en-us no-js\" lang=\"en\" >");
            htmlBuilder.AppendLine(" <head>");
            htmlBuilder.AppendLine("  <meta http-equiv=\"X-UA-Compatible\" content=\"IE=10\" /><meta charset=\"utf-8\" /><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />");
            htmlBuilder.AppendLine("  <meta name=\"author\" content=\"Epi Info 7\" />");            
            GenerateStandardHTMLStyle(htmlBuilder);
            htmlBuilder.AppendLine("    ");

            htmlBuilder.AppendLine("  <title>" + "VHF" + "</title>");
            htmlBuilder.AppendLine(" </head>");
            htmlBuilder.AppendLine("<body>");
            return htmlBuilder.ToString();
        }

        public static void GenerateStandardHTMLStyle(StringBuilder htmlBuilder)
        {
            htmlBuilder.AppendLine("<style type=\"text/css\">");
            htmlBuilder.AppendLine("body ");
            htmlBuilder.AppendLine("{");
            htmlBuilder.AppendLine("	font-family: 'Segoe UI', Calibri, Arial, sans-serif;");
            htmlBuilder.AppendLine("	margin: 0px;");
            htmlBuilder.AppendLine("}");
            htmlBuilder.AppendLine("");
            htmlBuilder.AppendLine("table      { width: 200px; margin: 1px;");
            htmlBuilder.AppendLine("             border-collapse: collapse; ");
            htmlBuilder.AppendLine("             page-break-inside:auto; }");
            htmlBuilder.AppendLine("tr    { page-break-inside:avoid; page-break-after:auto }");
            htmlBuilder.AppendLine("thead { display:table-header-group }");
            htmlBuilder.AppendLine("tfoot { display:table-footer-group }");
            htmlBuilder.AppendLine("th, td     { border: 1px solid black; page-break-inside:avoid; page-break-after:auto; }");
            htmlBuilder.AppendLine("th ");
            htmlBuilder.AppendLine("{	");
            htmlBuilder.AppendLine("	border-top: 1px solid black;");
            htmlBuilder.AppendLine("	border-left: 1px solid black;");
            htmlBuilder.AppendLine("	border-right: 1px solid black;");
            htmlBuilder.AppendLine("	border-bottom: 4px solid black;");
            htmlBuilder.AppendLine("	padding: 4px;");
            htmlBuilder.AppendLine("	font-size: 9pt;");
            htmlBuilder.AppendLine("	min-width: 15px;");
            htmlBuilder.AppendLine("}");
            htmlBuilder.AppendLine("td ");
            htmlBuilder.AppendLine("{");
            htmlBuilder.AppendLine("	padding: 3px;");
            htmlBuilder.AppendLine("	font-size: 9pt;");
            htmlBuilder.AppendLine("	text-wrap: normal;");
            htmlBuilder.AppendLine("	text-overflow: ellipsis;");
            htmlBuilder.AppendLine("}");
            htmlBuilder.AppendLine("@media print {");
            htmlBuilder.AppendLine("@page {size:landscape;}");
            htmlBuilder.AppendLine("}");
            htmlBuilder.AppendLine("</style>");
        }

        public static string TruncHTMLCell(string cellContents, int charactersToKeep)
        {
            if (cellContents.Length <= charactersToKeep) return cellContents;
            else return (cellContents.Substring(0, charactersToKeep) + "...");
        }

        public static DateTime GetMMWRStart(int gregorianYear, int firstDayOfWeek = 0)
        {
            DateTime dateResult;
            DateTime dateYearBegin = new DateTime(gregorianYear, 1, 1);
            DayOfWeek firstDayOfYear = (DayOfWeek)dateYearBegin.DayOfWeek;

            if (firstDayOfYear <= (DayOfWeek)firstDayOfWeek + 3)
            {
                dateResult = dateYearBegin.AddDays(firstDayOfWeek - (int)firstDayOfYear);
            }
            else
            {
                dateResult = dateYearBegin.AddDays(7 - firstDayOfWeek - (int)firstDayOfYear);
            }

            return dateResult;
        }

        public static DateTime GetMMWRStart(DateTime dateTime, int firstDayOfWeek = 0)
        {
            DateTime MMWRStart_YearMinusOne = Common.GetMMWRStart(dateTime.Year - 1, firstDayOfWeek);
            DateTime MMWRStart = Common.GetMMWRStart(dateTime.Year, firstDayOfWeek);
            DateTime MMWRStart_YearPlusOne = Common.GetMMWRStart(dateTime.Year + 1, firstDayOfWeek);

            int delta = MMWRStart.Subtract(dateTime).Days;
            int delta_PlusOne = MMWRStart_YearPlusOne.Subtract(dateTime).Days;

            if (delta > 0)
            {
                return MMWRStart_YearMinusOne;
            }
            else if (delta_PlusOne > 0)
            {
                return MMWRStart;
            }
            else
            {
                return MMWRStart_YearPlusOne;
            }
        }

        public static void SaveAsImage(FrameworkElement visual)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.DefaultExt = ".png";
            dlg.Filter = "PNG Image (.png)|*.png|JPEG Image (.jpg)|*.jpg";

            if (dlg.ShowDialog().Value)
            {
                BitmapSource img = (BitmapSource)ToImageSource(visual);

                FileStream stream = new FileStream(dlg.FileName, FileMode.Create);
                BitmapEncoder encoder = null; // new BitmapEncoder();

                if (dlg.SafeFileName.ToLower().EndsWith(".png"))
                {
                    encoder = new PngBitmapEncoder();
                }
                else
                {
                    encoder = new JpegBitmapEncoder();
                }

                encoder.Frames.Add(BitmapFrame.Create(img));
                encoder.Save(stream);
                stream.Close();
            }
        }

        public static ImageSource ToImageSource(FrameworkElement obj)
        {
            Transform transform = obj.LayoutTransform;
            Thickness margin = obj.Margin;
            obj.Margin = new Thickness(0, 0, margin.Right - margin.Left, margin.Bottom - margin.Top);
            Size size = new Size(obj.ActualWidth, obj.ActualHeight);
            obj.Measure(size);
            obj.Arrange(new Rect(size));
            RenderTargetBitmap bmp = new RenderTargetBitmap((int)obj.ActualWidth, (int)obj.ActualHeight, 96, 96, PixelFormats.Pbgra32);

            bmp.Render(obj);
            obj.LayoutTransform = transform;
            obj.Margin = margin;
            return bmp;
        }

        public static DataTable JoinPageTables(IDbDriver db, View vw)
        {
            DataTable unfilteredTable = new DataTable();

            // Get the form's field count, adding base table fields plus GUID field for each page. If less than 255, use SQL relate; otherwise, >255 exceeds OLE field capacity and we need to use a less efficient method
            if (vw.Fields.DataFields.Count + vw.Pages.Count + 5 < 255 && vw.Pages.Count < 15)
            {
                unfilteredTable = db.Select(db.CreateQuery("SELECT * " + vw.FromViewSQL));

                if (unfilteredTable.Columns["RecStatus"] == null && unfilteredTable.Columns["t.RecStatus"] != null)
                {
                    unfilteredTable.Columns["t.RecStatus"].ColumnName = "RecStatus";
                }

                if (unfilteredTable.Columns.Contains("t.GlobalRecordId"))
                {
                    unfilteredTable.Columns["t.GlobalRecordId"].ColumnName = "GlobalRecordId";
                }

                foreach (Epi.Page page in vw.Pages)
                {
                    string pageGUIDName = page.TableName + "." + "GlobalRecordId";
                    if (unfilteredTable.Columns.Contains(pageGUIDName))
                    {
                        unfilteredTable.Columns.Remove(pageGUIDName);
                    }
                }
            }
            else
            {
                DataTable viewTable = new DataTable();
                viewTable = db.GetTableData(vw.TableName, "GlobalRecordId, UniqueKey, RECSTATUS");
                viewTable.TableName = vw.TableName;

                DataTable relatedTable = new DataTable("relatedTable");

                //int total = 0;
                //// Get totals for percent completion calculation
                //foreach (Epi.Page page in vw.Pages)
                //{
                //    string pageColumnsToSelect = String.Empty;
                //    foreach (Field field in page.Fields)
                //    {
                //        if (field is RenderableField && field is IDataField)
                //        {
                //            total++;
                //        }
                //    }
                //}

                //total = total * RecordCount;

                //int counter = 0;

                foreach (Epi.Page page in vw.Pages)
                {
                    List<string> pageColumnsToSelect = new List<string>();
                    foreach (Field field in page.Fields)
                    {
                        if (field is RenderableField && field is IDataField)
                        {
                            pageColumnsToSelect.Add(field.Name);
                        }
                    }
                    pageColumnsToSelect.Add("GlobalRecordId");

                    DataTable pageTable = db.GetTableData(page.TableName, pageColumnsToSelect);
                    pageTable.TableName = page.TableName;

                    foreach (DataColumn dc in pageTable.Columns)
                    {
                        if (dc.ColumnName != "GlobalRecordId")
                        {
                            viewTable.Columns.Add(dc.ColumnName, dc.DataType);
                        }
                    }

                    //try
                    //{
                        // assume GlobalUniqueId column is unique and try to make it the primary key.
                        DataColumn[] parentPrimaryKeyColumns = new DataColumn[1];
                        parentPrimaryKeyColumns[0] = viewTable.Columns["GlobalRecordId"];
                        viewTable.PrimaryKey = parentPrimaryKeyColumns;
                    //}
                    //catch (Exception)
                    //{
                    //}

                    foreach (DataRow row in pageTable.Rows)
                    {
                        DataRow viewRow = viewTable.Rows.Find(row["GlobalRecordId"].ToString());
                        viewRow.BeginEdit();
                        if (viewRow["GlobalRecordId"].ToString().Equals(row["GlobalRecordId"].ToString()))
                        {
                            foreach (DataColumn dc in pageTable.Columns)
                            {
                                if (dc.ColumnName == "GlobalRecordId")
                                {
                                    continue;
                                }

                                if (row[dc.ColumnName] != DBNull.Value)
                                {
                                    viewRow[dc.ColumnName] = row[dc];
                                }
                                //counter++;

                                //if (counter % 200 == 0 && inputs != null)
                                //{
                                //    if (counter > total)
                                //    {
                                //        counter = total;
                                //    }
                                //    inputs.UpdateGadgetProgress(((double)counter / (double)total) * 100);
                                //    if (total == 0)
                                //    {
                                //        inputs.UpdateGadgetStatus(SharedStrings.DASHBOARD_GADGET_STATUS_RELATING_PAGES_NO_PROGRESS);
                                //    }
                                //    else
                                //    {
                                //        inputs.UpdateGadgetStatus(string.Format(SharedStrings.DASHBOARD_GADGET_STATUS_RELATING_PAGES, ((double)counter / (double)total).ToString("P0")));
                                //    }
                                //    if (inputs.IsRequestCancelled())
                                //    {
                                //        return null;
                                //    }
                                //}
                            }
                        }
                        viewRow.EndEdit();
                    }
                }
                unfilteredTable = viewTable;
            }
            return unfilteredTable;
        }
    }
}
