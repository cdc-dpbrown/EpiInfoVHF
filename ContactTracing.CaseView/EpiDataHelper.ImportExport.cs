using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Timers;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml;
using Epi;
using Epi.Data;
using Epi.Fields;
using ContactTracing.Core;
using ContactTracing.Core.Enums;
using ContactTracing.Core.Data;
using ContactTracing.ViewModel;
using ContactTracing.ViewModel.Collections;
using ContactTracing.ViewModel.Events;
using NPOI.HSSF.UserModel;
using System.IO;
using System.Text;
using ContactTracing.CaseView.Converters;

namespace ContactTracing.CaseView
{
    public partial class EpiDataHelper
    {
        #region Commands
        public ICommand ShowSqlToMdbCopierCommand { get { return new RelayCommand(ShowSqlToMdbCopierExecute, CanExecuteMdbCopyCommand); } }
        private void ShowSqlToMdbCopierExecute()
        {
            SqlToMdbCopierViewModel = new SqlToMdbCopierViewModel(Project, Country, ApplicationCulture, OutbreakName, OutbreakDate.Value);
            SqlToMdbCopierViewModel.IsDisplaying = true;
        }

        private bool CanExecuteMdbCopyCommand()
        {
            if (!IsMultiUser) return false;
            if (!OutbreakDate.HasValue) return false;
            if (!ApplicationCulture.Equals("en-US", StringComparison.OrdinalIgnoreCase)) return false;

            return CanExecuteExportCommand();
        }

        public ICommand ShowSyncFileExporterCommand { get { return new RelayCommand(ShowSyncFileExporterExecute, CanExecuteExportCommand); } }
        private void ShowSyncFileExporterExecute()
        {
            ExportSyncFileViewModel = new ExportSyncFileViewModel(Project, CurrentUser, MacAddress, IsCountryUS);
            ExportSyncFileViewModel.IsDisplaying = true;
        }

        private bool CanExecuteExportCommand()
        {
            return CanExecuteRepopulateCollectionsCommand(true);
        }
        #endregion // Commands

        #region Methods
        private void SyncCaseData(string fileName)
        {
            TaskbarProgressValue = 0;
            TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;

            ContactTracing.ImportExport.XmlDataImporter importer = new ImportExport.XmlDataImporter(Project, RecordProcessingScope.Both);

            if (IsMicrosoftSQLDatabase && !(Database is Epi.Data.Office.OleDbDatabase))
            {
                importer = new ImportExport.XmlSqlDataImporter(Project, RecordProcessingScope.Both);
            }

            importer.Delete = true;
            
            importer.MinorProgressChanged += unpackager_UpdateProgress;

            SyncStatus = "Importing data from " + fileName + "...";
            importer.Import(fileName);

            importer.MinorProgressChanged -= unpackager_UpdateProgress;
        }

        //private void SyncCaseData(XmlDocument doc)
        //{
        //    TaskbarProgressValue = 0;

        //    bool includesContacts = false;
        //    bool includesLinks = false;

        //    XmlDocument caseDoc = doc.Clone() as XmlDocument;
        //    if (caseDoc != null)
        //    {
        //        caseDoc.XmlResolver = null;
        //        XmlNode linkNode = null;
        //        XmlNode followUpNode = null;

        //        foreach (XmlNode node in caseDoc.ChildNodes[0].ChildNodes)
        //        {
        //            if (node.Name.Equals("Links", StringComparison.OrdinalIgnoreCase))
        //            {
        //                linkNode = node;
        //                includesLinks = true;
        //            }
        //            else if (node.Name.Equals("ContactFollowUps", StringComparison.OrdinalIgnoreCase))
        //            {
        //                followUpNode = node;
        //            }
        //            else if (node.Attributes["Name"].Value.ToString().StartsWith("Contact", StringComparison.OrdinalIgnoreCase))
        //            {
        //                includesContacts = true;
        //            }
        //        }

        //        //foreach (XmlNode node in nodesToRemove)
        //        //{
        //        //    caseDoc.ChildNodes[0].RemoveChild(node);
        //        //}

        //        Epi.ImportExport.ProjectPackagers.XmlDataUnpackager unpackager = new Epi.ImportExport.ProjectPackagers.XmlDataUnpackager(CaseForm, caseDoc);
        //        unpackager.StatusChanged += unpackager_StatusChanged;
        //        unpackager.UpdateProgress += unpackager_UpdateProgress;

        //        SendMessageForAwaitAll();

        //        try
        //        {
        //            unpackager.Unpackage();
        //        }
        //        catch (Exception ex)
        //        {
        //            if (SyncProblemsDetected != null)
        //            {
        //                SyncProblemsDetected(ex, new EventArgs());
        //            }
        //        }
        //        finally
        //        {
        //            SendMessageForUnAwaitAll();
        //            unpackager.StatusChanged -= unpackager_StatusChanged;
        //            unpackager.UpdateProgress -= unpackager_UpdateProgress;
        //            unpackager = null;
        //        }

        //        #region Import Contact Data

        //        if (includesContacts)
        //        {
        //            // commented this out because the current behavior already imports the contact form...

        //            //unpackager = new ContactTracing.ImportExport.ProjectPackagers.XmlCaseDataUnpackager(ContactForm, caseDoc);
        //            //unpackager.StatusChanged += unpackager_StatusChanged;
        //            //unpackager.UpdateProgress += unpackager_UpdateProgress;

        //            //SendMessageForAwaitAll();

        //            //try
        //            //{
        //            //    unpackager.Unpackage();
        //            //}
        //            //catch (Exception ex)
        //            //{
        //            //    if (SyncProblemsDetected != null)
        //            //    {
        //            //        SyncProblemsDetected(ex, new EventArgs());
        //            //    }
        //            //}
        //            //finally
        //            //{
        //            //    SendMessageForUnAwaitAll();
        //            //    unpackager.StatusChanged -= unpackager_StatusChanged;
        //            //    unpackager.UpdateProgress -= unpackager_UpdateProgress;
        //            //    unpackager = null;
        //            //}
        //        }

        //        #endregion // Import Contact Data

        //        #region Import Link Data

        //        if (includesLinks && linkNode != null)
        //        {
        //            SendMessageForAwaitAll();

        //            TaskbarProgressValue = 0;
        //            SyncStatus = "Synchronizing relationship data...";

        //            double inc = 1.0 / linkNode.ChildNodes.Count;

        //            try
        //            {
        //                foreach (XmlNode node in linkNode.ChildNodes)
        //                {
        //                    if (!String.IsNullOrEmpty(node.InnerText))
        //                    {
        //                        string fromRecordGuid = node.SelectSingleNode("FromRecordGuid").InnerText;
        //                        string toRecordGuid = node.SelectSingleNode("ToRecordGuid").InnerText;

        //                        int fromViewId = Int32.Parse(node.SelectSingleNode("FromViewId").InnerText);
        //                        int toViewId = Int32.Parse(node.SelectSingleNode("ToViewId").InnerText);

        //                        Query selectQuery = Database.CreateQuery("SELECT * FROM [metaLinks] WHERE [FromRecordGuid] = @FromRecordGuid AND " +
        //                            "[ToRecordGuid] = @ToRecordGuid AND [FromViewId] = @FromViewId AND [ToViewId] = @ToViewId");

        //                        selectQuery.Parameters.Add(new QueryParameter("@FromRecordGuid", DbType.String, fromRecordGuid));
        //                        selectQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, toRecordGuid));
        //                        selectQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, fromViewId));
        //                        selectQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, toViewId));

        //                        DataTable destinationLinkTable = Database.Select(selectQuery);

        //                        bool linkExists = false;
        //                        if (destinationLinkTable.Rows.Count >= 1)
        //                        {
        //                            linkExists = true;
        //                        }

        //                        DateTime lastContactDate = DateTime.MinValue;
        //                        string lastContactDateString = node.SelectSingleNode("LastContactDate").InnerText;
        //                        long lastContactDateTicks = long.Parse(lastContactDateString);

        //                        lastContactDate = new DateTime(lastContactDateTicks);
        //                        lastContactDate = new DateTime(lastContactDate.Year,
        //                            lastContactDate.Month,
        //                            lastContactDate.Day,
        //                            lastContactDate.Hour,
        //                            lastContactDate.Minute,
        //                            lastContactDate.Second);

        //                        int contactType = int.Parse(node.SelectSingleNode("ContactType").InnerText);
        //                        string relationshipType = node.SelectSingleNode("RelationshipType").InnerText;

        //                        if (String.IsNullOrEmpty(relationshipType.Trim()) && linkExists)
        //                        {
        //                            relationshipType = destinationLinkTable.Rows[0]["RelationshipType"].ToString();
        //                        }

        //                        object tentative = DBNull.Value;

        //                        string tentativeString = node.SelectSingleNode("Tentative").InnerText;
        //                        if (!String.IsNullOrEmpty(tentativeString))
        //                        {
        //                            tentative = int.Parse(tentativeString);
        //                        }
        //                        else if (linkExists && destinationLinkTable.Rows[0]["Tentative"] != DBNull.Value)
        //                        {
        //                            tentative = int.Parse(destinationLinkTable.Rows[0]["Tentative"].ToString());
        //                        }

        //                        bool isEstimated = bool.Parse(node.SelectSingleNode("IsEstimatedContactDate").InnerText);

        //                        object day1 = DBNull.Value;
        //                        object day2 = DBNull.Value;
        //                        object day3 = DBNull.Value;
        //                        object day4 = DBNull.Value;
        //                        object day5 = DBNull.Value;
        //                        object day6 = DBNull.Value;
        //                        object day7 = DBNull.Value;
        //                        object day8 = DBNull.Value;
        //                        object day9 = DBNull.Value;
        //                        object day10 = DBNull.Value;
        //                        object day11 = DBNull.Value;
        //                        object day12 = DBNull.Value;
        //                        object day13 = DBNull.Value;
        //                        object day14 = DBNull.Value;
        //                        object day15 = DBNull.Value;
        //                        object day16 = DBNull.Value;
        //                        object day17 = DBNull.Value;
        //                        object day18 = DBNull.Value;
        //                        object day19 = DBNull.Value;
        //                        object day20 = DBNull.Value;
        //                        object day21 = DBNull.Value;

        //                        if (node.SelectSingleNode("Day1") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day1").InnerText))
        //                            {
        //                                day1 = short.Parse(node.SelectSingleNode("Day1").InnerText);
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day1"] != DBNull.Value)
        //                            {
        //                                day1 = short.Parse(destinationLinkTable.Rows[0]["Day1"].ToString());
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day2") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day2").InnerText))
        //                            {
        //                                day2 = short.Parse(node.SelectSingleNode("Day2").InnerText);
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day2"] != DBNull.Value)
        //                            {
        //                                day2 = short.Parse(destinationLinkTable.Rows[0]["Day2"].ToString());
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day3") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day3").InnerText))
        //                            {
        //                                day3 = short.Parse(node.SelectSingleNode("Day3").InnerText);
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day3"] != DBNull.Value)
        //                            {
        //                                day3 = short.Parse(destinationLinkTable.Rows[0]["Day3"].ToString());
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day4") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day4").InnerText))
        //                            {
        //                                day4 = short.Parse(node.SelectSingleNode("Day4").InnerText);
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day4"] != DBNull.Value)
        //                            {
        //                                day4 = short.Parse(destinationLinkTable.Rows[0]["Day4"].ToString());
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day5") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day5").InnerText))
        //                            {
        //                                day5 = short.Parse(node.SelectSingleNode("Day5").InnerText);
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day5"] != DBNull.Value)
        //                            {
        //                                day5 = short.Parse(destinationLinkTable.Rows[0]["Day5"].ToString());
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day6") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day6").InnerText))
        //                            {
        //                                day6 = short.Parse(node.SelectSingleNode("Day6").InnerText);
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day6"] != DBNull.Value)
        //                            {
        //                                day6 = short.Parse(destinationLinkTable.Rows[0]["Day6"].ToString());
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day7") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day7").InnerText))
        //                            {
        //                                day7 = short.Parse(node.SelectSingleNode("Day7").InnerText);
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day7"] != DBNull.Value)
        //                            {
        //                                day7 = short.Parse(destinationLinkTable.Rows[0]["Day7"].ToString());
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day8") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day8").InnerText))
        //                            {
        //                                day8 = short.Parse(node.SelectSingleNode("Day8").InnerText);
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day8"] != DBNull.Value)
        //                            {
        //                                day8 = short.Parse(destinationLinkTable.Rows[0]["Day8"].ToString());
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day9") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day9").InnerText))
        //                            {
        //                                day9 = short.Parse(node.SelectSingleNode("Day9").InnerText);
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day9"] != DBNull.Value)
        //                            {
        //                                day9 = short.Parse(destinationLinkTable.Rows[0]["Day9"].ToString());
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day10") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day10").InnerText))
        //                            {
        //                                day10 = short.Parse(node.SelectSingleNode("Day10").InnerText);
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day10"] != DBNull.Value)
        //                            {
        //                                day10 = short.Parse(destinationLinkTable.Rows[0]["Day10"].ToString());
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day11") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day11").InnerText))
        //                            {
        //                                day11 = short.Parse(node.SelectSingleNode("Day11").InnerText);
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day11"] != DBNull.Value)
        //                            {
        //                                day11 = short.Parse(destinationLinkTable.Rows[0]["Day11"].ToString());
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day12") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day12").InnerText))
        //                            {
        //                                day12 = short.Parse(node.SelectSingleNode("Day12").InnerText);
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day12"] != DBNull.Value)
        //                            {
        //                                day12 = short.Parse(destinationLinkTable.Rows[0]["Day12"].ToString());
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day13") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day13").InnerText))
        //                            {
        //                                day13 = short.Parse(node.SelectSingleNode("Day13").InnerText);
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day13"] != DBNull.Value)
        //                            {
        //                                day13 = short.Parse(destinationLinkTable.Rows[0]["Day13"].ToString());
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day14") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day14").InnerText))
        //                            {
        //                                day14 = short.Parse(node.SelectSingleNode("Day14").InnerText);
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day14"] != DBNull.Value)
        //                            {
        //                                day14 = short.Parse(destinationLinkTable.Rows[0]["Day14"].ToString());
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day15") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day15").InnerText))
        //                            {
        //                                day15 = short.Parse(node.SelectSingleNode("Day15").InnerText);
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day15"] != DBNull.Value)
        //                            {
        //                                day15 = short.Parse(destinationLinkTable.Rows[0]["Day15"].ToString());
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day16") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day16").InnerText))
        //                            {
        //                                day16 = short.Parse(node.SelectSingleNode("Day16").InnerText);
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day16"] != DBNull.Value)
        //                            {
        //                                day16 = short.Parse(destinationLinkTable.Rows[0]["Day16"].ToString());
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day17") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day17").InnerText))
        //                            {
        //                                day17 = short.Parse(node.SelectSingleNode("Day17").InnerText);
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day17"] != DBNull.Value)
        //                            {
        //                                day17 = short.Parse(destinationLinkTable.Rows[0]["Day17"].ToString());
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day18") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day18").InnerText))
        //                            {
        //                                day18 = short.Parse(node.SelectSingleNode("Day18").InnerText);
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day18"] != DBNull.Value)
        //                            {
        //                                day18 = short.Parse(destinationLinkTable.Rows[0]["Day18"].ToString());
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day19") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day19").InnerText))
        //                            {
        //                                day19 = short.Parse(node.SelectSingleNode("Day19").InnerText);
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day19"] != DBNull.Value)
        //                            {
        //                                day19 = short.Parse(destinationLinkTable.Rows[0]["Day19"].ToString());
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day20") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day20").InnerText))
        //                            {
        //                                day20 = short.Parse(node.SelectSingleNode("Day20").InnerText);
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day20"] != DBNull.Value)
        //                            {
        //                                day20 = short.Parse(destinationLinkTable.Rows[0]["Day20"].ToString());
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day21") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day21").InnerText))
        //                            {
        //                                day21 = short.Parse(node.SelectSingleNode("Day21").InnerText);
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day21"] != DBNull.Value)
        //                            {
        //                                day21 = short.Parse(destinationLinkTable.Rows[0]["Day21"].ToString());
        //                            }
        //                        }

        //                        string day1Notes = String.Empty;
        //                        string day2Notes = String.Empty;
        //                        string day3Notes = String.Empty;
        //                        string day4Notes = String.Empty;
        //                        string day5Notes = String.Empty;
        //                        string day6Notes = String.Empty;
        //                        string day7Notes = String.Empty;
        //                        string day8Notes = String.Empty;
        //                        string day9Notes = String.Empty;
        //                        string day10Notes = String.Empty;
        //                        string day11Notes = String.Empty;
        //                        string day12Notes = String.Empty;
        //                        string day13Notes = String.Empty;
        //                        string day14Notes = String.Empty;
        //                        string day15Notes = String.Empty;
        //                        string day16Notes = String.Empty;
        //                        string day17Notes = String.Empty;
        //                        string day18Notes = String.Empty;
        //                        string day19Notes = String.Empty;
        //                        string day20Notes = String.Empty;
        //                        string day21Notes = String.Empty;

        //                        if (node.SelectSingleNode("Day1Notes") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day1Notes").InnerText))
        //                            {
        //                                day1Notes = node.SelectSingleNode("Day1Notes").InnerText;
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day1Notes"] != DBNull.Value)
        //                            {
        //                                day1Notes = destinationLinkTable.Rows[0]["Day1Notes"].ToString();
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day2Notes") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day2Notes").InnerText))
        //                            {
        //                                day2Notes = node.SelectSingleNode("Day2Notes").InnerText;
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day2Notes"] != DBNull.Value)
        //                            {
        //                                day2Notes = destinationLinkTable.Rows[0]["Day2Notes"].ToString();
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day3Notes") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day3Notes").InnerText))
        //                            {
        //                                day3Notes = node.SelectSingleNode("Day3Notes").InnerText;
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day3Notes"] != DBNull.Value)
        //                            {
        //                                day3Notes = destinationLinkTable.Rows[0]["Day3Notes"].ToString();
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day4Notes") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day4Notes").InnerText))
        //                            {
        //                                day4Notes = node.SelectSingleNode("Day4Notes").InnerText;
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day4Notes"] != DBNull.Value)
        //                            {
        //                                day4Notes = destinationLinkTable.Rows[0]["Day4Notes"].ToString();
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day5Notes") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day5Notes").InnerText))
        //                            {
        //                                day5Notes = node.SelectSingleNode("Day5Notes").InnerText;
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day5Notes"] != DBNull.Value)
        //                            {
        //                                day5Notes = destinationLinkTable.Rows[0]["Day5Notes"].ToString();
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day6Notes") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day6Notes").InnerText))
        //                            {
        //                                day6Notes = node.SelectSingleNode("Day6Notes").InnerText;
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day6Notes"] != DBNull.Value)
        //                            {
        //                                day6Notes = destinationLinkTable.Rows[0]["Day6Notes"].ToString();
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day7Notes") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day7Notes").InnerText))
        //                            {
        //                                day7Notes = node.SelectSingleNode("Day7Notes").InnerText;
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day7Notes"] != DBNull.Value)
        //                            {
        //                                day7Notes = destinationLinkTable.Rows[0]["Day7Notes"].ToString();
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day8Notes") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day8Notes").InnerText))
        //                            {
        //                                day8Notes = node.SelectSingleNode("Day8Notes").InnerText;
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day8Notes"] != DBNull.Value)
        //                            {
        //                                day8Notes = destinationLinkTable.Rows[0]["Day8Notes"].ToString();
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day9Notes") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day9Notes").InnerText))
        //                            {
        //                                day9Notes = node.SelectSingleNode("Day9Notes").InnerText;
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day9Notes"] != DBNull.Value)
        //                            {
        //                                day9Notes = destinationLinkTable.Rows[0]["Day9Notes"].ToString();
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day10Notes") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day10Notes").InnerText))
        //                            {
        //                                day10Notes = node.SelectSingleNode("Day10Notes").InnerText;
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day10Notes"] != DBNull.Value)
        //                            {
        //                                day10Notes = destinationLinkTable.Rows[0]["Day10Notes"].ToString();
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day11Notes") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day11Notes").InnerText))
        //                            {
        //                                day11Notes = node.SelectSingleNode("Day11Notes").InnerText;
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day11Notes"] != DBNull.Value)
        //                            {
        //                                day11Notes = destinationLinkTable.Rows[0]["Day11Notes"].ToString();
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day12Notes") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day12Notes").InnerText))
        //                            {
        //                                day12Notes = node.SelectSingleNode("Day12Notes").InnerText;
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day12Notes"] != DBNull.Value)
        //                            {
        //                                day12Notes = destinationLinkTable.Rows[0]["Day12Notes"].ToString();
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day13Notes") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day13Notes").InnerText))
        //                            {
        //                                day13Notes = node.SelectSingleNode("Day13Notes").InnerText;
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day13Notes"] != DBNull.Value)
        //                            {
        //                                day13Notes = destinationLinkTable.Rows[0]["Day13Notes"].ToString();
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day14Notes") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day14Notes").InnerText))
        //                            {
        //                                day14Notes = node.SelectSingleNode("Day14Notes").InnerText;
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day14Notes"] != DBNull.Value)
        //                            {
        //                                day14Notes = destinationLinkTable.Rows[0]["Day14Notes"].ToString();
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day15Notes") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day15Notes").InnerText))
        //                            {
        //                                day15Notes = node.SelectSingleNode("Day15Notes").InnerText;
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day15Notes"] != DBNull.Value)
        //                            {
        //                                day15Notes = destinationLinkTable.Rows[0]["Day15Notes"].ToString();
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day16Notes") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day16Notes").InnerText))
        //                            {
        //                                day16Notes = node.SelectSingleNode("Day16Notes").InnerText;
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day16Notes"] != DBNull.Value)
        //                            {
        //                                day16Notes = destinationLinkTable.Rows[0]["Day16Notes"].ToString();
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day17Notes") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day17Notes").InnerText))
        //                            {
        //                                day17Notes = node.SelectSingleNode("Day17Notes").InnerText;
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day17Notes"] != DBNull.Value)
        //                            {
        //                                day17Notes = destinationLinkTable.Rows[0]["Day17Notes"].ToString();
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day18Notes") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day18Notes").InnerText))
        //                            {
        //                                day18Notes = node.SelectSingleNode("Day18Notes").InnerText;
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day18Notes"] != DBNull.Value)
        //                            {
        //                                day18Notes = destinationLinkTable.Rows[0]["Day18Notes"].ToString();
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day19Notes") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day19Notes").InnerText))
        //                            {
        //                                day19Notes = node.SelectSingleNode("Day19Notes").InnerText;
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day19Notes"] != DBNull.Value)
        //                            {
        //                                day19Notes = destinationLinkTable.Rows[0]["Day19Notes"].ToString();
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day20Notes") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day20Notes").InnerText))
        //                            {
        //                                day20Notes = node.SelectSingleNode("Day20Notes").InnerText;
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day20Notes"] != DBNull.Value)
        //                            {
        //                                day20Notes = destinationLinkTable.Rows[0]["Day20Notes"].ToString();
        //                            }
        //                        }

        //                        if (node.SelectSingleNode("Day21Notes") != null)
        //                        {
        //                            if (!String.IsNullOrEmpty(node.SelectSingleNode("Day21Notes").InnerText))
        //                            {
        //                                day21Notes = node.SelectSingleNode("Day21Notes").InnerText;
        //                            }
        //                            else if (linkExists && destinationLinkTable.Rows[0]["Day21Notes"] != DBNull.Value)
        //                            {
        //                                day21Notes = destinationLinkTable.Rows[0]["Day21Notes"].ToString();
        //                            }
        //                        }

        //                        if (linkExists)
        //                        {
        //                            Query updateQuery = Database.CreateQuery("UPDATE [metaLinks] SET " +
        //                                "[LastContactDate] = @LastContactDate, " +
        //                                "[ContactType] = @ContactType, " +
        //                                "[RelationshipType] = @RelationshipType, " +
        //                                "[Tentative] = @Tentative, " +
        //                                "[IsEstimatedContactDate] = @IsEstimatedContactDate, " +
        //                                "[Day1] = @Day1, " +
        //                                "[Day2] = @Day2, " +
        //                                "[Day3] = @Day3, " +
        //                                "[Day4] = @Day4, " +
        //                                "[Day5] = @Day5, " +
        //                                "[Day6] = @Day6, " +
        //                                "[Day7] = @Day7, " +
        //                                "[Day8] = @Day8, " +
        //                                "[Day9] = @Day9, " +
        //                                "[Day10] = @Day10, " +
        //                                "[Day11] = @Day11, " +
        //                                "[Day12] = @Day12, " +
        //                                "[Day13] = @Day13, " +
        //                                "[Day14] = @Day14, " +
        //                                "[Day15] = @Day15, " +
        //                                "[Day16] = @Day16, " +
        //                                "[Day17] = @Day17, " +
        //                                "[Day18] = @Day18, " +
        //                                "[Day19] = @Day19, " +
        //                                "[Day20] = @Day20, " +
        //                                "[Day21] = @Day21, " +
        //                                "[Day1Notes] = @Day1Notes, " +
        //                                "[Day2Notes] = @Day2Notes, " +
        //                                "[Day3Notes] = @Day3Notes, " +
        //                                "[Day4Notes] = @Day4Notes, " +
        //                                "[Day5Notes] = @Day5Notes, " +
        //                                "[Day6Notes] = @Day6Notes, " +
        //                                "[Day7Notes] = @Day7Notes, " +
        //                                "[Day8Notes] = @Day8Notes, " +
        //                                "[Day9Notes] = @Day9Notes, " +
        //                                "[Day10Notes] = @Day10Notes, " +
        //                                "[Day11Notes] = @Day11Notes, " +
        //                                "[Day12Notes] = @Day12Notes, " +
        //                                "[Day13Notes] = @Day13Notes, " +
        //                                "[Day14Notes] = @Day14Notes, " +
        //                                "[Day15Notes] = @Day15Notes, " +
        //                                "[Day16Notes] = @Day16Notes, " +
        //                                "[Day17Notes] = @Day17Notes, " +
        //                                "[Day18Notes] = @Day18Notes, " +
        //                                "[Day19Notes] = @Day19Notes, " +
        //                                "[Day20Notes] = @Day20Notes, " +
        //                                "[Day21Notes] = @Day21Notes " +
        //                        "WHERE [ToRecordGuid] = @ToRecordGuid AND [FromRecordGuid] = @FromRecordGuid AND [ToViewId] = @ToViewId AND " +
        //                        "[FromViewId] = @FromViewId");

        //                            updateQuery.Parameters.Add(new QueryParameter("@LastContactDate", DbType.DateTime, lastContactDate));
        //                            updateQuery.Parameters.Add(new QueryParameter("@ContactType", DbType.Int32, contactType));
        //                            updateQuery.Parameters.Add(new QueryParameter("@RelationshipType", DbType.String, relationshipType));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Tentative", DbType.Byte, tentative));
        //                            updateQuery.Parameters.Add(new QueryParameter("@IsEstimatedContactDate", DbType.Boolean, isEstimated));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day1", DbType.Byte, day1));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day2", DbType.Byte, day2));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day3", DbType.Byte, day3));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day4", DbType.Byte, day4));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day5", DbType.Byte, day5));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day6", DbType.Byte, day6));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day7", DbType.Byte, day7));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day8", DbType.Byte, day8));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day9", DbType.Byte, day9));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day10", DbType.Byte, day10));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day11", DbType.Byte, day11));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day12", DbType.Byte, day12));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day13", DbType.Byte, day13));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day14", DbType.Byte, day14));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day15", DbType.Byte, day15));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day16", DbType.Byte, day16));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day17", DbType.Byte, day17));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day18", DbType.Byte, day18));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day19", DbType.Byte, day19));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day20", DbType.Byte, day20));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day21", DbType.Byte, day21));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day1Notes", DbType.String, day1Notes));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day2Notes", DbType.String, day2Notes));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day3Notes", DbType.String, day3Notes));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day4Notes", DbType.String, day4Notes));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day5Notes", DbType.String, day5Notes));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day6Notes", DbType.String, day6Notes));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day7Notes", DbType.String, day7Notes));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day8Notes", DbType.String, day8Notes));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day9Notes", DbType.String, day9Notes));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day10Notes", DbType.String, day10Notes));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day11Notes", DbType.String, day11Notes));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day12Notes", DbType.String, day12Notes));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day13Notes", DbType.String, day13Notes));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day14Notes", DbType.String, day14Notes));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day15Notes", DbType.String, day15Notes));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day16Notes", DbType.String, day16Notes));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day17Notes", DbType.String, day17Notes));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day18Notes", DbType.String, day18Notes));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day19Notes", DbType.String, day19Notes));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day20Notes", DbType.String, day20Notes));
        //                            updateQuery.Parameters.Add(new QueryParameter("@Day21Notes", DbType.String, day21Notes));

        //                            updateQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, toRecordGuid));
        //                            updateQuery.Parameters.Add(new QueryParameter("@FromRecordGuid", DbType.String, fromRecordGuid));
        //                            updateQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, toViewId));
        //                            updateQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, fromViewId));

        //                            int rows = Database.ExecuteNonQuery(updateQuery);
        //                        }
        //                        else
        //                        {
        //                            // row didn't exist before, so insert instead

        //                            Query insertQuery = Database.CreateQuery("INSERT INTO [metaLinks] (" +
        //                            "[ToRecordGuid], " +
        //                            "[FromRecordGuid], " +
        //                            "[ToViewId], " +
        //                            "[FromViewId], " +
        //                            "[LastContactDate], " +
        //                            "[ContactType], " +
        //                            "[RelationshipType], " +
        //                            "[Tentative], " +
        //                            "[IsEstimatedContactDate], " +
        //                            "[Day1], " +
        //                            "[Day2], " +
        //                            "[Day3], " +
        //                            "[Day4], " +
        //                            "[Day5], " +
        //                            "[Day6], " +
        //                            "[Day7], " +
        //                            "[Day8], " +
        //                            "[Day9], " +
        //                            "[Day10], " +
        //                            "[Day11], " +
        //                            "[Day12], " +
        //                            "[Day13], " +
        //                            "[Day14], " +
        //                            "[Day15], " +
        //                            "[Day16], " +
        //                            "[Day17], " +
        //                            "[Day18], " +
        //                            "[Day19], " +
        //                            "[Day20], " +
        //                            "[Day21], " +
        //                            "[Day1Notes], " +
        //                            "[Day2Notes], " +
        //                            "[Day3Notes], " +
        //                            "[Day4Notes], " +
        //                            "[Day5Notes], " +
        //                            "[Day6Notes], " +
        //                            "[Day7Notes], " +
        //                            "[Day8Notes], " +
        //                            "[Day9Notes], " +
        //                            "[Day10Notes], " +
        //                            "[Day11Notes], " +
        //                            "[Day12Notes], " +
        //                            "[Day13Notes], " +
        //                            "[Day14Notes], " +
        //                            "[Day15Notes], " +
        //                            "[Day16Notes], " +
        //                            "[Day17Notes], " +
        //                            "[Day18Notes], " +
        //                            "[Day19Notes], " +
        //                            "[Day20Notes], " +
        //                            "[Day21Notes]) VALUES (" +
        //                            "@ToRecordGuid, " +
        //                            "@FromRecordGuid, " +
        //                            "@ToViewId, " +
        //                            "@FromViewId, " +
        //                            "@LastContactDate, " +
        //                            "@ContactType, " +
        //                            "@RelationshipType, " +
        //                            "@Tentative, " +
        //                            "@IsEstimatedContactDate, " +
        //                            "@Day1, " +
        //                            "@Day2, " +
        //                            "@Day3, " +
        //                            "@Day4, " +
        //                            "@Day5, " +
        //                            "@Day6, " +
        //                            "@Day7, " +
        //                            "@Day8, " +
        //                            "@Day9, " +
        //                            "@Day10, " +
        //                            "@Day11, " +
        //                            "@Day12, " +
        //                            "@Day13, " +
        //                            "@Day14, " +
        //                            "@Day15, " +
        //                            "@Day16, " +
        //                            "@Day17, " +
        //                            "@Day18, " +
        //                            "@Day19, " +
        //                            "@Day20, " +
        //                            "@Day21, " +
        //                            "@Day1Notes, " +
        //                            "@Day2Notes, " +
        //                            "@Day3Notes, " +
        //                            "@Day4Notes, " +
        //                            "@Day5Notes, " +
        //                            "@Day6Notes, " +
        //                            "@Day7Notes, " +
        //                            "@Day8Notes, " +
        //                            "@Day9Notes, " +
        //                            "@Day10Notes, " +
        //                            "@Day11Notes, " +
        //                            "@Day12Notes, " +
        //                            "@Day13Notes, " +
        //                            "@Day14Notes, " +
        //                            "@Day15Notes, " +
        //                            "@Day16Notes, " +
        //                            "@Day17Notes, " +
        //                            "@Day18Notes, " +
        //                            "@Day19Notes, " +
        //                            "@Day20Notes, " +
        //                            "@Day21Notes) ");

        //                            insertQuery.Parameters.Add(new QueryParameter("@ToRecordGuid", DbType.String, toRecordGuid));
        //                            insertQuery.Parameters.Add(new QueryParameter("@FromRecordGuid", DbType.String, fromRecordGuid));
        //                            insertQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, toViewId));
        //                            insertQuery.Parameters.Add(new QueryParameter("@FromViewId", DbType.Int32, fromViewId));

        //                            insertQuery.Parameters.Add(new QueryParameter("@LastContactDate", DbType.DateTime, lastContactDate));
        //                            insertQuery.Parameters.Add(new QueryParameter("@ContactType", DbType.Int32, contactType));
        //                            insertQuery.Parameters.Add(new QueryParameter("@RelationshipType", DbType.String, relationshipType));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Tentative", DbType.Byte, tentative));
        //                            insertQuery.Parameters.Add(new QueryParameter("@IsEstimatedContactDate", DbType.Boolean, isEstimated));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day1", DbType.Byte, day1));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day2", DbType.Byte, day2));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day3", DbType.Byte, day3));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day4", DbType.Byte, day4));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day5", DbType.Byte, day5));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day6", DbType.Byte, day6));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day7", DbType.Byte, day7));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day8", DbType.Byte, day8));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day9", DbType.Byte, day9));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day10", DbType.Byte, day10));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day11", DbType.Byte, day11));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day12", DbType.Byte, day12));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day13", DbType.Byte, day13));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day14", DbType.Byte, day14));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day15", DbType.Byte, day15));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day16", DbType.Byte, day16));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day17", DbType.Byte, day17));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day18", DbType.Byte, day18));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day19", DbType.Byte, day19));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day20", DbType.Byte, day20));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day21", DbType.Byte, day21));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day1Notes", DbType.String, day1Notes));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day2Notes", DbType.String, day2Notes));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day3Notes", DbType.String, day3Notes));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day4Notes", DbType.String, day4Notes));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day5Notes", DbType.String, day5Notes));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day6Notes", DbType.String, day6Notes));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day7Notes", DbType.String, day7Notes));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day8Notes", DbType.String, day8Notes));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day9Notes", DbType.String, day9Notes));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day10Notes", DbType.String, day10Notes));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day11Notes", DbType.String, day11Notes));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day12Notes", DbType.String, day12Notes));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day13Notes", DbType.String, day13Notes));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day14Notes", DbType.String, day14Notes));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day15Notes", DbType.String, day15Notes));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day16Notes", DbType.String, day16Notes));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day17Notes", DbType.String, day17Notes));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day18Notes", DbType.String, day18Notes));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day19Notes", DbType.String, day19Notes));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day20Notes", DbType.String, day20Notes));
        //                            insertQuery.Parameters.Add(new QueryParameter("@Day21Notes", DbType.String, day21Notes));
        //                            int rows = Database.ExecuteNonQuery(insertQuery);
        //                            Contract.Assert(rows == 1);
        //                        }

        //                        TaskbarProgressValue = TaskbarProgressValue + inc;
        //                    }
        //                    //foreach (XmlNode node in linkNode.ChildNodes)
        //                    //{

        //                    //}
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                if (SyncProblemsDetected != null)
        //                {
        //                    SyncProblemsDetected(ex, new EventArgs());
        //                }
        //            }
        //            finally
        //            {
        //                SendMessageForUnAwaitAll();
        //            }
        //        }

        //        #endregion // Import Link Data

        //        #region Import Follow-up Data

        //        if (followUpNode != null)
        //        {
        //            SendMessageForAwaitAll();

        //            TaskbarProgressValue = 0;
        //            SyncStatus = "Synchronizing contact follow-up data...";

        //            var followUpRows = new List<ImportExport.FollowUpRow>();
        //            Query selectQuery = Database.CreateQuery("SELECT * FROM [metaHistory]");
        //            DataTable followUpsTable = Database.Select(selectQuery);

        //            var inc = 1.0 / (double)(followUpNode.ChildNodes.Count + followUpsTable.Rows.Count);

        //            #region Populate destination data
        //            foreach (DataRow row in followUpsTable.Rows)
        //            {
        //                string guid = row["ContactGUID"].ToString();
        //                DateTime date = Convert.ToDateTime(row["FollowUpDate"]);
        //                int? status = null;
        //                string note = row["Note"].ToString();
        //                double? temp1 = null;
        //                double? temp2 = null;

        //                if (row["StatusOnDate"] != DBNull.Value)
        //                {
        //                    status = Convert.ToInt32(row["StatusOnDate"]);
        //                }
        //                if (row["Temp1"] != DBNull.Value)
        //                {
        //                    temp1 = Convert.ToDouble(row["Temp1"], CultureInfo.InvariantCulture);
        //                }
        //                if (row["Temp2"] != DBNull.Value)
        //                {
        //                    temp2 = Convert.ToDouble(row["Temp2"], CultureInfo.InvariantCulture);
        //                }

        //                var followUpRow = new ImportExport.FollowUpRow
        //                {
        //                    ContactGUID = new Guid(guid),
        //                    FollowUpDate = date,
        //                    StatusOnDate = status,
        //                    Note = note,
        //                    Temp1 = temp1,
        //                    Temp2 = temp2
        //                };

        //                followUpRows.Add(followUpRow);

        //                TaskbarProgressValue += inc;
        //            }
        //            #endregion // Populate destination data

        //            foreach (XmlNode node in followUpNode.ChildNodes)
        //            {
        //                if (!String.IsNullOrEmpty(node.InnerText))
        //                {
        //                    string guid = node.SelectSingleNode("ContactGUID").InnerText;
        //                    DateTime date = DateTime.Parse(node.SelectSingleNode("FollowUpDate").InnerText, CultureInfo.InvariantCulture);
        //                    int? status = null;
        //                    string note = node.SelectSingleNode("Note").InnerText;
        //                    double? temp1 = null;
        //                    double? temp2 = null;

        //                    if (!String.IsNullOrEmpty(node.SelectSingleNode("StatusOnDate").InnerText))
        //                    {
        //                        status = Int32.Parse(node.SelectSingleNode("StatusOnDate").InnerText);
        //                    }

        //                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Temp1").InnerText))
        //                    {
        //                        temp1 = Double.Parse(node.SelectSingleNode("Temp1").InnerText, CultureInfo.InvariantCulture);
        //                    }

        //                    if (!String.IsNullOrEmpty(node.SelectSingleNode("Temp2").InnerText))
        //                    {
        //                        temp2 = Double.Parse(node.SelectSingleNode("Temp2").InnerText, CultureInfo.InvariantCulture);
        //                    }

        //                    ImportExport.FollowUpRow existingRow = null;

        //                    foreach (ImportExport.FollowUpRow row in followUpRows)
        //                    {
        //                        if (row.ContactGUID.ToString().Equals(guid, StringComparison.OrdinalIgnoreCase) &&
        //                            row.FollowUpDate.Year == date.Year &&
        //                            row.FollowUpDate.Month == date.Month &&
        //                            row.FollowUpDate.Day == date.Day)
        //                        {
        //                            // found a match
        //                            existingRow = row;
        //                            break;
        //                        }
        //                    }

        //                    TaskbarProgressValue = TaskbarProgressValue + inc;

        //                    if (existingRow != null)
        //                    {
        //                        // update
        //                        string updateQueryText = "UPDATE [metaHistory] SET ";
        //                        var parameters = new List<QueryParameter>();

        //                        var wb = new WordBuilder(", ");

        //                        if (status != existingRow.StatusOnDate && status.HasValue)
        //                        {
        //                            wb.Add("StatusOnDate = @StatusOnDate");
        //                            parameters.Add(new QueryParameter("@StatusOnDate", DbType.Int16, status));
        //                        }

        //                        if (note != existingRow.Note && !String.IsNullOrEmpty(note))
        //                        {
        //                            wb.Add("[Note] = @Note");
        //                            parameters.Add(new QueryParameter("@Note", DbType.String, note));
        //                        }

        //                        if (temp1 != existingRow.Temp1 && temp1.HasValue)
        //                        {
        //                            wb.Add("Temp1 = @Temp1");
        //                            parameters.Add(new QueryParameter("@Temp1", DbType.Double, temp1));
        //                        }

        //                        if (temp2 != existingRow.Temp2 && temp2.HasValue)
        //                        {
        //                            wb.Add("Temp2 = @Temp2");
        //                            parameters.Add(new QueryParameter("@Temp2", DbType.Double, temp2));
        //                        }

        //                        if (parameters.Count == 0)
        //                        {
        //                            continue; // no values changed, so don't update anything
        //                        }

        //                        updateQueryText += wb.ToString();
        //                        updateQueryText += " WHERE ContactGUID = @ContactGUID AND FollowUpDate = @FollowUpDate";

        //                        parameters.Add(new QueryParameter("@ContactGUID", DbType.Guid, new Guid(guid)));
        //                        parameters.Add(new QueryParameter("@FollowUpDate", DbType.DateTime, date));

        //                        Query updateQuery = Database.CreateQuery(updateQueryText);
        //                        foreach (QueryParameter parameter in parameters)
        //                        {
        //                            updateQuery.Parameters.Add(parameter);
        //                        }

        //                        int rows = Database.ExecuteNonQuery(updateQuery);

        //                        if (rows == 0)
        //                        {
        //                            throw new InvalidOperationException();
        //                            // if we got here, then there's a problem with the query, because we "found a match" earlier and now the UPDATE is telling us
        //                            // that no rows were updated...
        //                        }

        //                        //Query updateQuery = Database.CreateQuery()
        //                    }
        //                    else
        //                    {
        //                        // append
        //                        Query insertQuery = Database.CreateQuery("INSERT INTO [metaHistory] (ContactGUID, FollowUpDate, StatusOnDate, [Note], Temp1, Temp2) VALUES (" +
        //                            "@ContactGuid, @FollowUpDate, @StatusOnDate, @Note, @Temp1, @Temp2)");
        //                        insertQuery.Parameters.Add(new QueryParameter("@ContactGuid", DbType.Guid, new Guid(guid)));
        //                        insertQuery.Parameters.Add(new QueryParameter("@FollowUpDate", DbType.DateTime, date));
        //                        insertQuery.Parameters.Add(status.HasValue
        //                            ? new QueryParameter("@StatusOnDate", DbType.Int16, status.Value)
        //                            : new QueryParameter("@StatusOnDate", DbType.Int16, DBNull.Value));
        //                        insertQuery.Parameters.Add(new QueryParameter("@Note", DbType.String, note));

        //                        insertQuery.Parameters.Add(temp1.HasValue
        //                            ? new QueryParameter("@Temp1", DbType.Double, temp1.Value)
        //                            : new QueryParameter("@Temp1", DbType.Double, DBNull.Value));

        //                        insertQuery.Parameters.Add(temp2.HasValue
        //                            ? new QueryParameter("@Temp2", DbType.Double, temp2.Value)
        //                            : new QueryParameter("@Temp2", DbType.Double, DBNull.Value));

        //                        int rows = Database.ExecuteNonQuery(insertQuery);
        //                        Contract.Assert(rows == 1);
        //                    }
        //                }
        //            }
        //        }
        //        #endregion Import Follow-up Data
        //    }
        //}

        public void SyncCaseDataStart(string filePath)
        {
            if (IsLoadingProjectData || IsSendingServerUpdates || IsWaitingOnOtherClients)
            {
                return;
            }

            if (String.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            IsDataSyncing = true;

            Task.Factory.StartNew(
                () =>
                {
                    SyncCaseData(filePath);
                },
                 System.Threading.CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).ContinueWith(
                 delegate
                 {
                     System.IO.File.Delete(filePath);

                     string gzPath = filePath.Substring(0, filePath.Length - 4) + ".gz";
                     System.IO.File.Delete(gzPath);

                     TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                     TaskbarProgressValue = 0;

                     sw.Stop();
                     System.Diagnostics.Debug.Print("Import completed in " + sw.Elapsed.TotalMilliseconds.ToString() + " milliseconds.");

                     IsDataSyncing = false;
                     RepopulateCollections(false);
                     SendMessageForDataImported();

                     CommandManager.InvalidateRequerySuggested();

                 }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        //public void SyncCaseDataStart(XmlDocument doc)
        //{
        //    if (IsLoadingProjectData || IsSendingServerUpdates || IsWaitingOnOtherClients)
        //    {
        //        return;
        //    }

        //    if (doc == null)
        //    {
        //        throw new ArgumentNullException("doc");
        //    }

        //    IsDataSyncing = true;

        //    Task.Factory.StartNew(
        //        () =>
        //        {
        //            SyncCaseData(doc);
        //        },
        //         System.Threading.CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).ContinueWith(
        //         delegate
        //         {
        //             TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
        //             TaskbarProgressValue = 0;

        //             IsDataSyncing = false;
        //             RepopulateCollections(false);
        //             SendMessageForDataImported();

        //             CommandManager.InvalidateRequerySuggested();

        //         }, TaskScheduler.FromCurrentSynchronizationContext());
        //}

        public void GenerateExcelDailyFollowUp(ObservableCollection<DailyCheckViewModel> collection, DateTime? dpPrev, string fileName = "")
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
            SyncStatus = "Starting data export...";
            ShowingDataExporterText = "Exporting Data";
            IsExportingData = true;
            IsShowingDataExporter = true;
            TaskbarProgressValue = 0;
            TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
            SendMessageForAwaitAll();
            Task.Factory.StartNew(
                () =>
                {
                    var row = sheet.CreateRow(rowIndex);
                    row.CreateCell(0).SetCellValue(Properties.Resources.ColHeaderContactID);
                    row.CreateCell(1).SetCellValue(Properties.Resources.ColHeaderSurname);
                    row.CreateCell(2).SetCellValue(Properties.Resources.ColHeaderOtherNames);
                    row.CreateCell(3).SetCellValue(Properties.Resources.ColHeaderGender);
                    row.CreateCell(4).SetCellValue(Properties.Resources.ColHeaderAge);

                    row.CreateCell(5).SetCellValue(Adm1); // district
                    row.CreateCell(6).SetCellValue(Adm2); // subcounty
                    row.CreateCell(7).SetCellValue(Adm3); // parish
                    row.CreateCell(8).SetCellValue(Properties.Resources.ColHeaderVillage);

                    row.CreateCell(9).SetCellValue(Properties.Resources.ColHeaderDateLastContact);
                    row.CreateCell(10).SetCellValue(Properties.Resources.ColHeaderDateLastFollowUp);
                    row.CreateCell(11).SetCellValue(Properties.Resources.ColHeaderDay);
                    row.CreateCell(12).SetCellValue(Properties.Resources.HTMLColHeaderDateLastSeen);
                    row.CreateCell(13).SetCellValue(Properties.Resources.ColHeaderTeam);
                    row.CreateCell(14).SetCellValue("Source Case ID");
                    row.CreateCell(15).SetCellValue("Source Case EpiCaseDef");
                    row.CreateCell(16).SetCellValue("Source Case Name");
                    row.CreateCell(17).SetCellValue(Properties.Resources.ColHeaderHeadHousehold);
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
                        row.CreateCell(7).SetCellValue(dailyCheck.ContactVM.Parish);
                        row.CreateCell(8).SetCellValue(dailyCheck.ContactVM.Village);

                        row.CreateCell(9).SetCellValue(dailyCheck.ContactVM.DateOfLastContact.Value.ToShortDateString());
                        row.CreateCell(10).SetCellValue(dailyCheck.ContactVM.DateOfLastFollowUp.Value.ToShortDateString());
                        row.CreateCell(11).SetCellValue(dailyCheck.Day.ToString());
                        row.CreateCell(12).SetCellValue(dateLastSeen);
                        row.CreateCell(13).SetCellValue(dailyCheck.ContactVM.Team);
                        row.CreateCell(14).SetCellValue(dailyCheck.ContactVM.LastSourceCase.ID);
                        row.CreateCell(15).SetCellValue(caseClassConverter.Convert(dailyCheck.ContactVM.LastSourceCase.EpiCaseDef, null, null, null).ToString());
                        row.CreateCell(16).SetCellValue(dailyCheck.ContactVM.LastSourceCase.OtherNames + " " + dailyCheck.ContactVM.LastSourceCase.Surname);
                        row.CreateCell(17).SetCellValue(dailyCheck.ContactVM.HeadOfHousehold);
                        row.CreateCell(18).SetCellValue(dailyCheck.ContactVM.Phone);
                        row.CreateCell(19).SetCellValue(dailyCheck.ContactVM.HCWFacility);
                        row.CreateCell(20).SetCellValue(String.Empty);
                        row.CreateCell(21).SetCellValue(String.Empty);
                        rowIndex++;
                    }

                    DateTime? originalDate = dpPrev;
                    DateTime dt = DateTime.Now;
                    DateTime minDate = dt.AddDays(-1 * ContactTracing.Core.Common.DaysInWindow);

                    List<ContactViewModel> collected = new List<ContactViewModel>();

                    DateTime incDate = minDate.AddDays(-600);
                    while (incDate < DateTime.Today)
                    {
                        incDate = incDate.AddDays(1);
                        ShowContactsForDateforFollowup.Execute(incDate);
                        
                        var query = from prevCheck in PrevFollowUpCollection
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
                                // float progress = (float)Math.Round((rowIndex + 1) / (float)10000, 2);                                
                                // SyncStatus = String.Format("Processing  records {0}...", (rowIndex+1).ToString());
                                // TaskbarProgressValue = progress;// TaskbarProgressValue+(rowIndex+1) / (float)1000;

                                row = sheet.CreateRow(rowIndex);
                                row.CreateCell(0).SetCellValue(dailyCheck.ContactVM.ContactID);
                                row.CreateCell(1).SetCellValue(dailyCheck.ContactVM.Surname);
                                row.CreateCell(2).SetCellValue(dailyCheck.ContactVM.OtherNames);
                                row.CreateCell(3).SetCellValue(dailyCheck.ContactVM.GenderAbbreviation);
                                row.CreateCell(4).SetCellValue(dailyCheck.ContactVM.AgeYears.ToString());

                                row.CreateCell(5).SetCellValue(dailyCheck.ContactVM.District);
                                row.CreateCell(6).SetCellValue(dailyCheck.ContactVM.SubCounty);
                                row.CreateCell(7).SetCellValue(dailyCheck.ContactVM.Parish);
                                row.CreateCell(8).SetCellValue(dailyCheck.ContactVM.Village);

                                row.CreateCell(9).SetCellValue(dailyCheck.ContactVM.DateOfLastContact.Value.ToShortDateString());
                                row.CreateCell(10).SetCellValue(dailyCheck.ContactVM.DateOfLastFollowUp.Value.ToShortDateString());
                                row.CreateCell(11).SetCellValue(day);
                                row.CreateCell(12).SetCellValue(dateLastSeen);
                                row.CreateCell(13).SetCellValue(dailyCheck.ContactVM.Team);
                                row.CreateCell(14).SetCellValue(dailyCheck.ContactVM.LastSourceCase.ID);
                                row.CreateCell(15).SetCellValue(caseClassConverter.Convert(dailyCheck.ContactVM.LastSourceCase.EpiCaseDef, null, null, null).ToString());
                                row.CreateCell(16).SetCellValue(dailyCheck.ContactVM.LastSourceCase.OtherNames + " " + dailyCheck.ContactVM.LastSourceCase.Surname);
                                row.CreateCell(17).SetCellValue(dailyCheck.ContactVM.HeadOfHousehold);
                                row.CreateCell(18).SetCellValue(dailyCheck.ContactVM.Phone);
                                row.CreateCell(19).SetCellValue(dailyCheck.ContactVM.HCWFacility);
                                row.CreateCell(20).SetCellValue(String.Empty);
                                row.CreateCell(21).SetCellValue(String.Empty);
                                rowIndex++;
                            }
                        }
                    }

                    dpPrev = originalDate;
                    // TaskbarProgressValue = 100;                 
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
                    SyncStatus = String.Format("Exporting  records {0}...", TaskbarProgressValue.ToString());
                },
                 System.Threading.CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).ContinueWith(
                 delegate
                 {
                     SendMessageForUnAwaitAll();
                     IsExportingData = false;
                     TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                     SyncStatus = "Export complete.";

                 }, TaskScheduler.FromCurrentSynchronizationContext());
            // return dpPrev;
        }

        public void GenerateExcelDailyFollowUpforUS(ObservableCollection<DailyCheckViewModel> collection, DateTime? dpPrev, string fileName = "")
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
            SyncStatus = "Starting data export...";
            ShowingDataExporterText = "Exporting Data";
            IsShowingDataExporter = true;
            IsExportingData = true;
            TaskbarProgressValue = 0;
            SendMessageForAwaitAll();
            //float numerator = 0.0f;
            TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
            Task.Factory.StartNew(
                () =>
                {
                    var row = sheet.CreateRow(rowIndex);
                    row.CreateCell(0).SetCellValue("CDC ID");
                    row.CreateCell(1).SetCellValue("State/Local ID");
                    row.CreateCell(2).SetCellValue(Properties.Resources.ColHeaderSurname);
                    row.CreateCell(3).SetCellValue(Properties.Resources.ColHeaderOtherNames);
                    row.CreateCell(4).SetCellValue(Properties.Resources.ColHeaderGender);
                    row.CreateCell(5).SetCellValue(Properties.Resources.ColHeaderAge);

                    row.CreateCell(6).SetCellValue(Adm1); // district
                    row.CreateCell(7).SetCellValue(Adm2); // subcounty
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

                    DateTime? originalDate = dpPrev;
                    DateTime dt = DateTime.Now;
                    DateTime minDate = dt.AddDays(-1 * ContactTracing.Core.Common.DaysInWindow);
                    List<ContactViewModel> collected = new List<ContactViewModel>();

                    DateTime incDate = minDate.AddDays(-600);
                    while (incDate < DateTime.Today)
                    {
                        incDate = incDate.AddDays(1);
                        ShowContactsForDateforFollowup.Execute(incDate);
                        //  numerator += 0.05f;
                        //  incDate = incDate.AddDays(1);

                        //ShowContactsForDateforFollowup.Execute(incDate);

                        var query = from prevCheck in PrevFollowUpCollection
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
                                //float progress = (float)Math.Round((rowIndex + 1) / (float)10000, 2);
                                //SyncStatus = String.Format("Processing  records {0}...", (rowIndex + 1).ToString());
                                //TaskbarProgressValue = progress;// TaskbarProgressValue+(rowIndex+1) / (float)1000;                                                                                    
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

                    dpPrev = originalDate;
                    // TaskbarProgressValue = 100;
                    SyncStatus = String.Format("Exporting  records {0}...", TaskbarProgressValue.ToString());
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
                },
                 System.Threading.CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).ContinueWith(
                 delegate
                 {
                     SendMessageForUnAwaitAll();
                     IsExportingData = false;
                     TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                     SyncStatus = "Export complete.";

                 }, TaskScheduler.FromCurrentSynchronizationContext());
            // return dpPrev;
        }


        public void Print21DayFollowUp(ObservableCollection<ContactViewModel> collection)
        {
            string baseFileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString("N");
            Dictionary<int, Boundry> ba = BoundaryAggregation;
            StringBuilder htmlBuilder = new StringBuilder();
            IMultiValueConverter dateConverter = new DateConverter();
            htmlBuilder.Append(ContactTracing.Core.Common.GetHtmlHeader().ToString());
            DateTime dt = DateTime.Now;
            DateTime minDate = dt.AddDays(-1 * ContactTracing.Core.Common.DaysInWindow);

            var query = from contact in collection
                        where contact.FollowUpWindowViewModel != null && String.IsNullOrEmpty(contact.FinalOutcome)
                        //orderby contact.Surname, contact.OtherNames
                        //&& contact.FollowUpWindowViewModel.WindowStartDate >= minDate
                        group contact by string.Concat("<span style=\"font-weight: bold;\">" + Adm4 + "</span> ", contact.Village);

            int rowsGenerated = 0;
            IsExportingData = true;
            bool firstPage = true;
            TaskbarProgressValue = 0;
            SyncStatus = "Starting data print...";
            ShowingDataExporterText = "Printing Data";
            IsShowingDataExporter = true;
            SendMessageForAwaitAll();
            TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
            Task.Factory.StartNew(
                () =>
                {
                    float numerator = 0.0f;
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
                        numerator += 1.0f;
                        foreach (var contact in entry)
                        {
                            if (rowsGenerated == 0)
                            {
                                //htmlBuilder.AppendLine("<p style=\"font-weight: bold; clear: left;\">" + entry.Key + ". LC1 Chairman: " + contact.LC1Chairman + "</p>");

                                if (IsCountryUS)
                                {
                                    htmlBuilder.AppendLine("<p style=\"clear: left;\">" +
                                        entry.Key +
                                        "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<span style=\"font-weight: bold;\">" + Adm2 + "</span> " +
                                        contact.SubCounty +
                                        "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<span style=\"font-weight: bold;\">" + Adm1 + "</span> " +
                                        contact.District +
                                        "<br /></p>");
                                }
                                else
                                {
                                    htmlBuilder.AppendLine("<p style=\"clear: left;\">" +
                                        entry.Key +
                                        "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<span style=\"font-weight: bold;\">" + Adm2 + "</span> " +
                                        contact.SubCounty +
                                        "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<span style=\"font-weight: bold;\">" + Adm1 + "</span> " +
                                        contact.District +
                                        "<br /><span style=\"font-weight: bold;\">" + Properties.Resources.HTMLLC1ChairmanHeading + "</span> " + contact.LC1Chairman + "</p>");
                                }

                                htmlBuilder.AppendLine("<table style=\"width: 1200px; border: 4px solid black;\" align=\"left\">");
                                htmlBuilder.AppendLine("<thead>");
                                htmlBuilder.AppendLine("<tr>");

                                if (IsCountryUS)
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

                                if (IsCountryUS == false)
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

                            if (IsCountryUS)
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
                                if (IsCountryUS)
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

                            if (IsCountryUS)
                            {
                                htmlBuilder.AppendLine("<td style=\"font-size: 11pt;\">" + contact.ContactCDCID + "</td>");
                                htmlBuilder.AppendLine("<td style=\"font-size: 11pt;\">" + contact.ContactStateID + "</td>");
                            }
                            else
                            {
                                htmlBuilder.AppendLine("<td style=\"font-size: 11pt;\">" + contact.ContactID + "</td>");
                            }

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
                            if (contact.AgeYears.HasValue)
                            {
                                htmlBuilder.AppendLine("<td>" + contact.AgeYears + "</td>");
                            }
                            else
                            {
                                htmlBuilder.AppendLine("<td>&nbsp;</td>");
                            }
                            string[] parms = { contact.FollowUpWindowViewModel.WindowStartDate.AddDays(-1).ToString(), ApplicationCulture };
                            var windowstartdate = dateConverter.Convert(parms, null, null, null);
                            htmlBuilder.AppendLine("<td>" + windowstartdate + "</td>");
                            htmlBuilder.AppendLine("<td>" + contact.LastSourceCase.Surname + " " + contact.LastSourceCase.OtherNames + "</td>");

                            if (IsCountryUS)
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
                        float progress = (float)Math.Round(numerator / (float)query.Count(), 2);
                        TaskbarProgressValue = progress;

                        SyncStatus = String.Format("Printing  records {0}...", progress.ToString("P0"));
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
                },
                 System.Threading.CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).ContinueWith(
                 delegate
                 {
                     SendMessageForUnAwaitAll();

                     TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None;

                     IsExportingData = false;
                     IsShowingDataExporter = false;
                     SyncStatus = "Printing complete.";

                 }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public void PrintDailyFollowUp(ObservableCollection<DailyCheckViewModel> collection, DateTime? dpPrev, Core.Enums.LocationType locationType = Core.Enums.LocationType.Village)
        {
            string baseFileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString("N");
            Dictionary<int, Boundry> ba = BoundaryAggregation;
            DateTime? originalDate = dpPrev;
            StringBuilder htmlBuilder = new StringBuilder();

            htmlBuilder.Append(ContactTracing.Core.Common.GetHtmlHeader().ToString());

            DateTime dt = DateTime.Now;

            //DateTime minDate = dt.AddDays(-21);
            DateTime minDate = dt.AddDays(-1 * ContactTracing.Core.Common.DaysInWindow);

            if (collection == PrevFollowUpCollection)
            {
                if (!dpPrev.HasValue)
                {
                    return;
                }
                minDate = dpPrev.Value;
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
            IsShowingDataExporter = true;
            IsExportingData = true;
            ShowingDataExporterText = "Printing Data";
            SyncStatus = "Starting data print...";
            TaskbarProgressValue = 0;
            SendMessageForAwaitAll();
            TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
            Task.Factory.StartNew(
            () =>
            {
                int recordCount = 0;

                while (incDate < DateTime.Today)
                {
                    if (TaskbarProgressValue > 1) TaskbarProgressValue = 0;
                    recordCount++;
                    incDate = incDate.AddDays(1);

                    ShowContactsForDateforFollowup.Execute(incDate);
                    SyncStatus = String.Format("Processing record {0}", recordCount);
                    TaskbarProgressValue = TaskbarProgressValue + 0.01;

                    query = GetPrevCheck(indexDate, minDate, locationType);

                    foreach (var entry in query)
                    {
                        if (!followUpDictionary.ContainsKey(entry.Key))
                        {
                            followUpDictionary.Add(entry.Key, new List<DailyCheckViewModel>());
                        }
                        foreach (var dailyCheck in entry)
                        {
                            if (TaskbarProgressValue > 1) TaskbarProgressValue = 0;
                            TaskbarProgressValue = TaskbarProgressValue + 0.0001;
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

                // dpPrev.SelectedDate = originalDate; // DateTime.Today.AddDays(-1); // reset

                int rowsGenerated = 0;
                bool firstPage = true;

                TaskbarProgressValue = 0;
                double progressBarIncrementFraction = 1.0 / followUpDictionary.Count;
                int followupCount = 0;

                foreach (KeyValuePair<string, List<DailyCheckViewModel>> kvp in followUpDictionary)
                {
                    followupCount++;

                    htmlBuilder.AppendLine("<table width=\"100%\" style=\"border: 0px; padding: 0px; margin: 0px; clear: left; width:100%; \">");
                    htmlBuilder.AppendLine(" <tr style=\"border: 0px;\">");
                    htmlBuilder.AppendLine("  <td width=\"50%\" style=\"border: 0px;\">");
                    htmlBuilder.AppendLine("   <p style=\"font-size: 13pt; font-weight: bold; clear: left; text-decoration: underline;\">" + Properties.Settings.Default.HtmlPrintoutTitle + "</p>");
                    htmlBuilder.AppendLine("   <p style=\"font-size: 13pt; font-weight: bold; text-decoration: underline;\">" + Properties.Resources.HTMLContactTracingDailyFollowUpheader + "</p>");

                    if (collection == PrevFollowUpCollection)
                    {
                        htmlBuilder.AppendLine("   <p style=\"font-size: 13pt;\"><span style=\"font-weight: bold;\">" + Properties.Resources.HTMLDate + "</span> " + dpPrev.Value.ToShortDateString() + "</p>");
                    }
                    else
                    {
                        htmlBuilder.AppendLine("   <p style=\"font-size: 13pt;\"><span style=\"font-weight: bold;\">" + Properties.Resources.HTMLDate + "</span> " + DateTime.Now.ToShortDateString() + "</p>");
                    }

                    TaskbarProgressValue = TaskbarProgressValue + progressBarIncrementFraction;
                    SyncStatus = String.Format("Printing group {0} ...", followupCount);
                    System.Threading.Thread.Sleep(500);

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

                    if (IsCountryUS == false)
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

                            if (IsCountryUS)
                            {
                                if (locationType == Core.Enums.LocationType.Village)
                                {
                                    htmlBuilder.AppendLine("<p style=\"clear: left;\">" +
                                        kvp.Key +
                                        "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<span style=\"font-weight: bold;\">" + Adm2 + "</span> " +
                                        dailyCheck.ContactVM.SubCounty +
                                        "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<span style=\"font-weight: bold;\">" + Adm1 + "</span> " +
                                        dailyCheck.ContactVM.District +
                                        "<br/></p>");
                                }
                                else if (locationType == Core.Enums.LocationType.SubCounty)
                                {
                                    htmlBuilder.AppendLine("<p style=\"clear: left;\">" +
                                        kvp.Key +
                                        "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<span style=\"font-weight: bold;\">" + Adm1 + "</span> " +
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
                                        "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<span style=\"font-weight: bold;\">" + Adm2 + "</span> " +
                                        dailyCheck.ContactVM.SubCounty +
                                        "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<span style=\"font-weight: bold;\">" + Adm1 + "</span> " +
                                        dailyCheck.ContactVM.District +
                                        "<br /><span style=\"font-weight: bold;\">" + Properties.Resources.HTMLLC1ChairmanHeading + "</span> " + dailyCheck.ContactVM.LC1Chairman + "</p>");
                                }
                                else if (locationType == Core.Enums.LocationType.SubCounty)
                                {
                                    htmlBuilder.AppendLine("<p style=\"clear: left;\">" +
                                        kvp.Key +
                                        "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<span style=\"font-weight: bold;\">" + Adm1 + "</span> " +
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

                            if (IsCountryUS == false)
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

                            if (IsCountryUS == false)
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

                        //SyncStatus = String.Format("Printing  records {0}...", TaskbarProgressValue.ToString());
                        htmlBuilder.AppendLine("<tr style=\"border-bottom: 1px solid black; height: 32px;\">");
                        //bool pastDue = (dailyCheck.ContactVM.FollowUpWindowViewModel.WindowStartDate < minDate.AddDays(-20));
                        bool pastDue = dailyCheck.ContactVM.FollowUpWindowViewModel.WindowEndDate < DateTime.Today;

                        if (IsCountryUS == false)
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

                        string[] parmsValues = { dailyCheck.ContactVM.FollowUpWindowViewModel.WindowStartDate.AddDays(-1).ToString(), ApplicationCulture };
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
                            if (collection == DailyFollowUpCollection)
                            {
                                htmlBuilder.AppendLine("<td style=\"text-align: right;\">" + dailyCheck.Day + "</td>");
                            }
                            else if (collection == PrevFollowUpCollection && dpPrev.HasValue)
                            {
                                TimeSpan ts = dpPrev.Value - dailyCheck.ContactVM.FollowUpWindowViewModel.WindowStartDate;
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

                        if (IsCountryUS)
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

                if (collection == PrevFollowUpCollection)
                {
                    ShowContactsForDateforFollowup.Execute(dpPrev);
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
            },
                 System.Threading.CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).ContinueWith(
                 delegate
                 {
                     SendMessageForUnAwaitAll();

                     TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                     TaskbarProgressValue = 1;
                     IsExportingData = false;
                     IsShowingDataExporter = false;
                     SyncStatus = "Print complete.";

                 }, TaskScheduler.FromCurrentSynchronizationContext());
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

        /// <summary>
        /// Creates a CSV File for analysis
        /// </summary>
        /// <param name="fileName">The path and name of the file to be generated</param>
        /// <param name="exportFull">false</param>
        /// <param name="convertCommentLegalValues">false</param>
        /// <param name="convertFieldPrompts">false</param>
        public void ExportCasesForAnalysisStart(string fileName, bool exportFull = false, bool convertCommentLegalValues = false, bool convertFieldPrompts = false)
        {
            if (IsLoadingProjectData || IsSendingServerUpdates || IsWaitingOnOtherClients)
            {
                return;
            }

            if (String.IsNullOrEmpty(fileName.Trim()))
            {
                throw new ArgumentNullException("fileName");
            }

            IsExportingData = true;
            ShowingDataExporterText = "EXPORTING DATA";
            IsShowingDataExporter = true;
            SyncStatus = "Starting data export...";

            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();

            SendMessageForAwaitAll();

            Task.Factory.StartNew(
                () =>
                {
                    #region CSV Export
                    DataTable casesTable = new DataTable("casesTable");
                    casesTable.CaseSensitive = true;
                    casesTable = ContactTracing.Core.Common.JoinPageTables(Database, CaseForm);
                    DataRow[] rows = casesTable.Select("RECSTATUS = 0");
                    foreach (DataRow row in rows)
                    {
                        casesTable.Rows.Remove(row);
                    }
                    casesTable.AcceptChanges();

                    if (IsCountryUS)
                    {
                        Epi.Util.SortColumnsByTabOrder(casesTable, CaseForm);

                        casesTable.Columns.Remove("FirstSaveLogonName");
                        casesTable.Columns.Remove("FKEY");
                        casesTable.Columns.Remove("FirstSaveTime");
                        casesTable.Columns.Remove("LastSaveLogonName");
                        casesTable.Columns.Remove("LastSaveTime");
                        //casesTable.Columns.Remove("ID");
                        if (casesTable.Columns.Contains("DistrictResCode"))
                            casesTable.Columns.Remove("DistrictResCode");
                        if (casesTable.Columns.Contains("DistrictOnsetCode"))
                            casesTable.Columns.Remove("DistrictOnsetCode");
                        if (casesTable.Columns.Contains("DistrictCodeHospitalCurrent"))
                            casesTable.Columns.Remove("DistrictCodeHospitalCurrent");
                        if (casesTable.Columns.Contains("DistrictCodeHospitalPast1"))
                            casesTable.Columns.Remove("DistrictCodeHospitalPast1");
                        if (casesTable.Columns.Contains("DistrictCodeHospitalPast2"))
                            casesTable.Columns.Remove("DistrictCodeHospitalPast2");

                        if (casesTable.Columns.Contains("ContactDistrict1Code"))
                            casesTable.Columns.Remove("ContactDistrict1Code");
                        if (casesTable.Columns.Contains("ContactDistrict3Code"))
                            casesTable.Columns.Remove("ContactDistrict3Code");
                        if (casesTable.Columns.Contains("FuneralDistrict2Code"))
                            casesTable.Columns.Remove("FuneralDistrict2Code");
                        if (casesTable.Columns.Contains("HospitalBeforeIllDistrictCode"))
                            casesTable.Columns.Remove("HospitalBeforeIllDistrictCode");
                        if (casesTable.Columns.Contains("ContactDistrict2Code"))
                            casesTable.Columns.Remove("ContactDistrict2Code");
                        if (casesTable.Columns.Contains("FuneralDistrict1Code"))
                            casesTable.Columns.Remove("FuneralDistrict1Code");
                        if (casesTable.Columns.Contains("TravelDistrictCode"))
                            casesTable.Columns.Remove("TravelDistrictCode");
                        if (casesTable.Columns.Contains("TradHealerDistrictCode"))
                            casesTable.Columns.Remove("TradHealerDistrictCode");
                        if (casesTable.Columns.Contains("InterviewerDistrictCode"))
                            casesTable.Columns.Remove("InterviewerDistrictCode");
                        if (casesTable.Columns.Contains("DistrictDeathCode"))
                            casesTable.Columns.Remove("DistrictDeathCode");
                        if (casesTable.Columns.Contains("FuneralDistrictCode"))
                            casesTable.Columns.Remove("FuneralDistrictCode");
                        if (casesTable.Columns.Contains("HospitalDischargeDistrictCode"))
                            casesTable.Columns.Remove("HospitalDischargeDistrictCode");
                    }
                    casesTable.Columns["GlobalRecordId"].SetOrdinal(0);
                    casesTable.Columns["DateReport"].SetOrdinal(6);

                    DataTable labTable = new DataTable("labTable");
                    labTable.CaseSensitive = true;
                    labTable = ContactTracing.Core.Common.JoinPageTables(Database, Project.Views["LaboratoryResultsForm"]);

                    SendMessageForUnAwaitAll(); // no need to keep connected users waiting after this point, given the process is now done with the database

                    labTable.Columns.Remove("FirstSaveLogonName");
                    labTable.Columns.Remove("FirstSaveTime");
                    labTable.Columns.Remove("LastSaveLogonName");
                    labTable.Columns.Remove("LastSaveTime");

                    if (!exportFull)
                    {
                        if (VirusTestType == VirusTestTypes.Ebola)
                        {
                            labTable.Columns.Remove("SUDVNPPCR");
                            labTable.Columns.Remove("SUDVPCR2");
                            labTable.Columns.Remove("SUDVAg");
                            labTable.Columns.Remove("SUDVIgM");
                            labTable.Columns.Remove("SUDVIgG");

                            labTable.Columns.Remove("BDBVNPPCR");
                            labTable.Columns.Remove("BDBVVP40PCR");
                            labTable.Columns.Remove("BDBVAg");
                            labTable.Columns.Remove("BDBVIgM");
                            labTable.Columns.Remove("BDBVIgG");

                            labTable.Columns.Remove("MARVPolPCR");
                            labTable.Columns.Remove("MARVVP40PCR");
                            labTable.Columns.Remove("MARVAg");
                            labTable.Columns.Remove("MARVIgM");
                            labTable.Columns.Remove("MARVIgG");

                            labTable.Columns.Remove("LASPCR1");
                            labTable.Columns.Remove("LASPCR2");
                            labTable.Columns.Remove("LASAg");
                            labTable.Columns.Remove("LASIgM");
                            labTable.Columns.Remove("LASIgG");

                            labTable.Columns.Remove("CCHFPCR1");
                            labTable.Columns.Remove("CCHFPCR2");
                            labTable.Columns.Remove("CCHFAg");
                            labTable.Columns.Remove("CCHFIgM");
                            labTable.Columns.Remove("CCHFIgG");

                            labTable.Columns.Remove("RVFPCR1");
                            labTable.Columns.Remove("RVFPCR2");
                            labTable.Columns.Remove("RVFAg");
                            labTable.Columns.Remove("RVFIgM");
                            labTable.Columns.Remove("RVFIgG");
                        }

                        labTable.Columns.Remove("EBOVCT1");
                        labTable.Columns.Remove("EBOVCT2");
                        labTable.Columns.Remove("EBOVAgTiter");
                        labTable.Columns.Remove("EBOVIgMTiter");
                        labTable.Columns.Remove("EBOVIgGTiter");
                        labTable.Columns.Remove("EBOVAgSumOD");
                        labTable.Columns.Remove("EBOVIgMSumOD");
                        labTable.Columns.Remove("EBOVIgGSumOD");

                        labTable.Columns.Remove("SUDVIgGSumOD");
                        labTable.Columns.Remove("SUDVNPCT");
                        labTable.Columns.Remove("SUDVCT2");
                        labTable.Columns.Remove("SUDVAgTiter");
                        labTable.Columns.Remove("SUDVIgMTiter");
                        labTable.Columns.Remove("SUDVIgGTiter");
                        labTable.Columns.Remove("SUDVAgSumOD");
                        labTable.Columns.Remove("SUDVIgMSumOD");
                        //labTable.Columns.Remove("SUDVIgGSumOD");

                        labTable.Columns.Remove("BDBVNPCT");
                        labTable.Columns.Remove("BDBVVP40CT");
                        labTable.Columns.Remove("BDBVAgTiter");
                        labTable.Columns.Remove("BDBVIgMTiter");
                        labTable.Columns.Remove("BDBVIgGTiter");
                        labTable.Columns.Remove("BDBVAgSumOD");
                        labTable.Columns.Remove("BDBVIgMSumOD");
                        labTable.Columns.Remove("BDBVIgGSumOD");

                        labTable.Columns.Remove("CCHFCT1");
                        labTable.Columns.Remove("CCHFCT2");
                        labTable.Columns.Remove("CCHFAgTiter");
                        labTable.Columns.Remove("CCHFIgMTiter");
                        labTable.Columns.Remove("CCHFIgGTiter");
                        labTable.Columns.Remove("CCHFAgSumOD");
                        labTable.Columns.Remove("CCHFIgMSumOD");
                        labTable.Columns.Remove("CCHFIgGSumOD");

                        labTable.Columns.Remove("LASCT1");
                        labTable.Columns.Remove("LASCT2");
                        labTable.Columns.Remove("LASAgTiter");
                        labTable.Columns.Remove("LASIgMTiter");
                        labTable.Columns.Remove("LASIgGTiter");
                        labTable.Columns.Remove("LASAgSumOD");
                        labTable.Columns.Remove("LASIgMSumOD");
                        labTable.Columns.Remove("LASIgGSumOD");

                        labTable.Columns.Remove("RVFCT1");
                        labTable.Columns.Remove("RVFCT2");
                        labTable.Columns.Remove("RVFAgTiter");
                        labTable.Columns.Remove("RVFIgMTiter");
                        labTable.Columns.Remove("RVFIgGTiter");
                        labTable.Columns.Remove("RVFAgSumOD");
                        labTable.Columns.Remove("RVFIgMSumOD");
                        labTable.Columns.Remove("RVFIgGSumOD");

                        labTable.Columns.Remove("MARVPolCT");
                        labTable.Columns.Remove("MARVVP40CT");
                        labTable.Columns.Remove("MARVAgTiter");
                        labTable.Columns.Remove("MARVIgMTiter");
                        labTable.Columns.Remove("MARVIgGTiter");
                        labTable.Columns.Remove("MARVAgSumOD");
                        labTable.Columns.Remove("MARVIgMSumOD");
                        labTable.Columns.Remove("MARVIgGSumOD");
                    }

                    Dictionary<string, List<string>> commentLegalLookup = new Dictionary<string, List<string>>();

                    if (convertCommentLegalValues)
                    {
                        foreach (Field field in CaseForm.Fields)
                        {
                            if (field is DDLFieldOfCommentLegal)
                            {
                                DDLFieldOfCommentLegal ddl = (field as DDLFieldOfCommentLegal);

                                List<string> values = new List<string>();
                                if (Database.TableExists(ddl.SourceTable))
                                {
                                    DataTable dt = Database.GetTableData(ddl.SourceTable);

                                    foreach (DataRow row in dt.Rows)
                                    {
                                        if (!values.Contains(row[ddl.TextColumnName].ToString()))
                                        {
                                            values.Add(row[ddl.TextColumnName].ToString());
                                        }
                                    }
                                }

                                commentLegalLookup.Add(ddl.Name, values);
                            }
                        }
                    }

                    DataView dv = new DataView(labTable);
                    //ExportCasesForAnalysisStart(fileName, exportFull, convertCommentLegalValues, convertFieldPrompts);
                    float numerator = 0.0f;

                    //System.Windows.Forms.ProgressBar progBar = new System.Windows.Forms.ProgressBar();
                    //progBar.Location = new System.Drawing.Point(20, 20);
                    //progBar.Name = "ProgressBar";
                    //progBar.Width = 200;
                    //progBar.Height = 30;

                    TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;

                    DataColumn dc1 = new DataColumn("TotalContactsListed", typeof(int));
                    //DataColumn dc2 = new DataColumn("ActiveContactCount", typeof(int));
                    DataColumn dc3 = new DataColumn("ThisCaseIsAlsoContact", typeof(bool));
                    //DataColumn dc4 = new DataColumn("FinalOutcomeAsContact", typeof(int));
                    //DataColumn dc5 = new DataColumn("DateLastContactAsContact", typeof(DateTime));
                    //DataColumn dc6 = new DataColumn("LastSourceCaseAsContact", typeof(string));

                    casesTable.Columns.Add(dc1);
                    //casesTable.Columns.Add(dc2);
                    casesTable.Columns.Add(dc3);
                    //casesTable.Columns.Add(dc4);
                    //casesTable.Columns.Add(dc5);
                    //casesTable.Columns.Add(dc6);

                    dc1.SetOrdinal(1);
                    //dc2.SetOrdinal(2);
                    dc3.SetOrdinal(3);
                    //dc4.SetOrdinal(4);
                    //dc5.SetOrdinal(5);
                    //dc5.SetOrdinal(6);

                    //if (IsSuperUser)
                    //{
                    //casesTable.Columns.Add(new DataColumn("TotalContactCount", typeof(int)));
                    //casesTable.Columns.Add(new DataColumn("ThisCaseIsAlsoContact", typeof(bool)));
                    //casesTable.Columns.Add(new DataColumn("FinalOutcomeAsContact", typeof(int)));
                    //casesTable.Columns.Add(new DataColumn("DateLastContactAsContact", typeof(DateTime)));
                    //casesTable.Columns.Add(new DataColumn("LastSourceCaseAsContact", typeof(string)));
                    //}

                    foreach (DataRow row in casesTable.Rows)
                    {
                        numerator += 1.0f;
                        float progress = (float)Math.Round(numerator / (float)casesTable.Rows.Count, 2);
                        //progBar.Value = (int)progress;

                        SyncStatus = String.Format("Processing lab records {0}...", progress.ToString("P0"));

                        TaskbarProgressValue = progress;

                        string guid = row["GlobalRecordId"].ToString();
                        dv.RowFilter = "FKEY = '" + guid + "'";
                        int rowCount = 1;

                        if (convertCommentLegalValues)
                        {
                            #region Comment Legal Labels
                            // deal with comment legal values
                            for (int i = 0; i < casesTable.Columns.Count; i++)
                            {
                                DataColumn dc = casesTable.Columns[i];
                                Field field = null;
                                if (CaseForm.Fields.DataFields.Contains(dc.ColumnName))
                                {
                                    field = CaseForm.Fields[dc.ColumnName];
                                }
                                else if (LabForm.Fields.DataFields.Contains(dc.ColumnName))
                                {
                                    field = CaseForm.Fields[dc.ColumnName];
                                }

                                if (field != null && field is DDLFieldOfCommentLegal)
                                {
                                    DDLFieldOfCommentLegal ddl = field as DDLFieldOfCommentLegal;
                                    string fieldName = ddl.Name;
                                    List<string> values = commentLegalLookup[fieldName];

                                    string cellValue = row[dc].ToString();

                                    foreach (string value in values)
                                    {
                                        int position = value.IndexOf('-');
                                        string leftPart = value.Substring(0, position);

                                        if (leftPart == cellValue)
                                        {
                                            cellValue = value;
                                            row[dc] = cellValue;
                                            break;
                                        }
                                    }
                                }
                            }
                            #endregion // Comment Legal Labels
                        }

                        //if (IsSuperUser)
                        //{
                        CaseViewModel caseVM = GetCaseVM(guid);
                        if (caseVM != null && caseVM.Contacts != null)
                        {
                            row["TotalContactsListed"] = caseVM.Contacts.Count;

                            //int activeContacts = 0;

                            //foreach (ContactViewModel contactVM in caseVM.Contacts)
                            //{
                            //    if (contactVM.IsActive == true)
                            //    {
                            //        activeContacts++;
                            //    }
                            //}

                            //row["ActiveContactCount"] = activeContacts;

                            if (caseVM.IsContact)
                            {
                                row["ThisCaseIsAlsoContact"] = true;

                                //ContactViewModel contactVM = GetContactVM(guid);
                                //if (contactVM != null && !String.IsNullOrEmpty(contactVM.FinalOutcome) && contactVM.DateOfLastContact.HasValue && contactVM.LastSourceCase != null)
                                //{
                                //    row["FinalOutcomeAsContact"] = contactVM.FinalOutcome;
                                //    row["DateLastContactAsContact"] = contactVM.DateOfLastContact;
                                //    row["LastSourceCaseAsContact"] = contactVM.LastSourceCase.ID;
                                //}
                                //else
                                //{
                                //    row["FinalOutcomeAsContact"] = DBNull.Value;
                                //    row["DateLastContactAsContact"] = DBNull.Value;
                                //    row["LastSourceCaseAsContact"] = String.Empty;
                                //}
                            }
                            else
                            {
                                row["ThisCaseIsAlsoContact"] = false;
                            }
                        }
                        else
                        {
                            row["TotalContactsListed"] = 0;
                        }
                        //}

                        foreach (DataRowView rowView in dv)
                        {
                            #region Lab Records
                            DataRow labRow = rowView.Row;
                            foreach (DataColumn dc in labTable.Columns)
                            {
                                if (dc.ColumnName.Equals("GlobalRecordId") ||
                                    dc.ColumnName.Equals("FKEY") ||
                                    dc.ColumnName.Equals("UniqueKey") ||
                                    dc.ColumnName.ToLower().Equals("recstatus"))
                                {
                                    continue;
                                }

                                if (!exportFull)
                                {
                                    if (dc.ColumnName.Equals("SUDVNPCT") ||
                                    dc.ColumnName.Equals("SUDVCT2") ||
                                    dc.ColumnName.Equals("SUDVAgTiter") ||
                                    dc.ColumnName.Equals("SUDVIgMTiter") ||
                                    dc.ColumnName.Equals("SUDVIgGTiter") ||
                                    dc.ColumnName.Equals("SUDVAgSumOD") ||
                                    dc.ColumnName.Equals("SUDVIgMSumOD") ||
                                    dc.ColumnName.Equals("SUDVIgGSumOD") ||

                                    dc.ColumnName.Equals("BDBVNPCT") ||
                                    dc.ColumnName.Equals("BDBVVP40CT") ||
                                    dc.ColumnName.Equals("BDBVAgTiter") ||
                                    dc.ColumnName.Equals("BDBVIgMTiter") ||
                                    dc.ColumnName.Equals("BDBVIgGTiter") ||
                                    dc.ColumnName.Equals("BDBVAgSumOD") ||
                                    dc.ColumnName.Equals("BDBVIgMSumOD") ||
                                    dc.ColumnName.Equals("BDBVIgGSumOD") ||

                                    dc.ColumnName.Equals("MARVPolCT") ||
                                    dc.ColumnName.Equals("MARVVP40CT") ||
                                    dc.ColumnName.Equals("MARVAgTiter") ||
                                    dc.ColumnName.Equals("MARVIgMTiter") ||
                                    dc.ColumnName.Equals("MARVIgGTiter") ||
                                    dc.ColumnName.Equals("MARVAgSumOD") ||
                                    dc.ColumnName.Equals("MARVIgMSumOD") ||
                                    dc.ColumnName.Equals("MARVIgGSumOD"))
                                    {
                                        continue;
                                    }
                                }

                                string newColumnName = dc.ColumnName + rowCount;
                                if (!casesTable.Columns.Contains(newColumnName))
                                {
                                    casesTable.Columns.Add(new DataColumn(newColumnName, dc.DataType));
                                }
                                row[newColumnName] = labRow[dc.ColumnName];
                            }

                            rowCount++;
                            #endregion // Lab Records
                        }
                    }

                    bool exportResult = ExportView(casesTable.DefaultView, fileName, convertFieldPrompts, true);
                    #endregion
                },
                 System.Threading.CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).ContinueWith(
                 delegate
                 {
                     SendMessageForUnAwaitAll();

                     TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                     SyncStatus = "Export complete.";
                     IsExportingData = false;

                 }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        /// <summary>
        /// Creates a CSV File for analysis
        /// </summary>
        /// <param name="fileName">The path and name of the file to be generated</param>
        /// <param name="exportFull">false</param>
        /// <param name="convertCommentLegalValues">false</param>
        /// <param name="convertFieldPrompts">false</param>
        public void ExportContactsForAnalysisStart(string fileName, bool exportFull = false, bool convertCommentLegalValues = false, bool convertFieldPrompts = false)
        {
            if (IsLoadingProjectData || IsSendingServerUpdates || IsWaitingOnOtherClients)
            {
                return;
            }

            if (String.IsNullOrEmpty(fileName.Trim()))
            {
                throw new ArgumentNullException("fileName");
            }

            //FinishedExportingMessageVisible = false;
            SyncStatus = "Starting data export...";
            ShowingDataExporterText = "EXPORTING DATA";
            IsExportingData = true;
            IsShowingDataExporter = true;

            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();

            //SendMessageForAwaitAll();

            Task.Factory.StartNew(
                () =>
                {
                    //doc = CreateCaseSyncFile(includeCases, includeCaseExposures, includeContacts);

                    #region CSV Export
                    DataTable contactsTable = new DataTable("contactsTable");
                    contactsTable.Columns.Add(new DataColumn("GlobalRecordId", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("ThisContactIsAlsoCase", typeof(bool)));
                    contactsTable.Columns.Add(new DataColumn("ID", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Surname", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("OtherNames", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Gender", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Age", typeof(double)));
                    contactsTable.Columns.Add(new DataColumn("HeadHousehold", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Village", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Parish", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("District", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("SubCounty", typeof(string)));

                    contactsTable.Columns.Add(new DataColumn("SourceCaseID", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("SourceCase", typeof(string)));

                    contactsTable.Columns.Add(new DataColumn("RelationshipToCase", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("ContactTypes", typeof(string)));

                    contactsTable.Columns.Add(new DataColumn("DateLastContact", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("DateOfLastFollowUp", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("TotalSourceCases", typeof(int)));
                    contactsTable.Columns.Add(new DataColumn("CommunityPoliticalLeader", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Phone", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("HCW", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("HCFacility", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Team", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("FinalOutcome", typeof(string)));

                    contactsTable.Columns.Add(new DataColumn("DateCSVExported", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("FollowedToday", typeof(bool)));
                    contactsTable.Columns.Add(new DataColumn("FollowedYesterday", typeof(bool)));
                    contactsTable.Columns.Add(new DataColumn("FollowedDayBeforeYesterday", typeof(bool)));

                    contactsTable.Columns.Add(new DataColumn("Day1", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day2", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day3", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day4", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day5", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day6", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day7", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day8", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day9", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day10", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day11", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day12", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day13", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day14", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day15", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day16", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day17", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day18", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day19", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day20", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day21", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp1_1", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp1_2", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp1_3", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp1_4", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp1_5", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp1_6", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp1_7", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp1_8", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp1_9", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp1_10", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp1_11", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp1_12", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp1_13", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp1_14", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp1_15", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp1_16", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp1_17", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp1_18", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp1_19", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp1_20", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp1_21", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp2_1", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp2_2", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp2_3", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp2_4", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp2_5", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp2_6", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp2_7", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp2_8", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp2_9", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp2_10", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp2_11", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp2_12", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp2_13", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp2_14", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp2_15", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp2_16", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp2_17", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp2_18", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp2_19", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp2_20", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Temp2_21", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day1Notes", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day2Notes", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day3Notes", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day4Notes", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day5Notes", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day6Notes", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day7Notes", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day8Notes", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day9Notes", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day10Notes", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day11Notes", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day12Notes", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day13Notes", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day14Notes", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day15Notes", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day16Notes", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day17Notes", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day18Notes", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day19Notes", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day20Notes", typeof(string)));
                    contactsTable.Columns.Add(new DataColumn("Day21Notes", typeof(string)));

                    float numerator = 0.0f;

                    TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;

                    DateTime today = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
                    DateTime yesterday = (new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 0, 0, 0)).AddDays(-1);
                    DateTime dayBeforeYesterday = (new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 0, 0, 0)).AddDays(-2);

                    foreach (ContactViewModel contactVM in this.ContactCollection)
                    {
                        string Day1 = null, Day2 = null, Day3 = null, Day4 = null, Day5 = null, Day6 = null, Day7 = null,
                            Day8 = null, Day9 = null, Day10 = null, Day11 = null, Day12 = null, Day13 = null, Day14 = null,
                            Day15 = null, Day16 = null, Day17 = null, Day18 = null, Day19 = null, Day20 = null, Day21 = null,
                            Day1Notes = null, Day2Notes = null, Day3Notes = null, Day4Notes = null, Day5Notes = null, Day6Notes = null,
                            Day7Notes = null, Day8Notes = null, Day9Notes = null, Day10Notes = null, Day11Notes = null,
                            Day12Notes = null, Day13Notes = null, Day14Notes = null, Day15Notes = null, Day16Notes = null,
                            Day17Notes = null, Day18Notes = null, Day19Notes = null, Day20Notes = null, Day21Notes = null,
                            Temp1_1 = null, Temp1_2 = null, Temp1_3 = null, Temp1_4 = null, Temp1_5 = null, Temp1_6 = null,
                            Temp1_7 = null, Temp1_8 = null, Temp1_9 = null, Temp1_10 = null, Temp1_11 = null, Temp1_12 = null,
                            Temp1_13 = null, Temp1_14 = null, Temp1_15 = null, Temp1_16 = null, Temp1_17 = null, Temp1_18 = null,
                            Temp1_19 = null, Temp1_20 = null, Temp1_21 = null,
                            Temp2_1 = null, Temp2_2 = null, Temp2_3 = null, Temp2_4 = null, Temp2_5 = null, Temp2_6 = null,
                            Temp2_7 = null, Temp2_8 = null, Temp2_9 = null, Temp2_10 = null, Temp2_11 = null, Temp2_12 = null,
                            Temp2_13 = null, Temp2_14 = null, Temp2_15 = null, Temp2_16 = null, Temp2_17 = null, Temp2_18 = null,
                            Temp2_19 = null, Temp2_20 = null, Temp2_21 = null;
                        string[] Days = new string[21];
                        numerator += 1.0f;
                        int links = this.metaLinksDataTable.Rows.Count;
                        DataRow[] metaLinksDataRows = this.metaLinksDataTable.Select("ToRecordGuid = '" + contactVM.RecordId + "' AND LastContactDate = '" + contactVM.DateOfLastContact + "'");
                        if (metaLinksDataRows.Length != 0)
                        {
                            Day1 = metaLinksDataRows[0]["Day1"].ToString();
                            Day2 = metaLinksDataRows[0]["Day2"].ToString();
                            Day3 = metaLinksDataRows[0]["Day3"].ToString();
                            Day4 = metaLinksDataRows[0]["Day4"].ToString();
                            Day5 = metaLinksDataRows[0]["Day5"].ToString();
                            Day6 = metaLinksDataRows[0]["Day6"].ToString();
                            Day7 = metaLinksDataRows[0]["Day7"].ToString();
                            Day8 = metaLinksDataRows[0]["Day8"].ToString();
                            Day9 = metaLinksDataRows[0]["Day9"].ToString();
                            Day10 = metaLinksDataRows[0]["Day10"].ToString();
                            Day11 = metaLinksDataRows[0]["Day11"].ToString();
                            Day12 = metaLinksDataRows[0]["Day12"].ToString();
                            Day13 = metaLinksDataRows[0]["Day13"].ToString();
                            Day14 = metaLinksDataRows[0]["Day14"].ToString();
                            Day15 = metaLinksDataRows[0]["Day15"].ToString();
                            Day16 = metaLinksDataRows[0]["Day16"].ToString();
                            Day17 = metaLinksDataRows[0]["Day17"].ToString();
                            Day18 = metaLinksDataRows[0]["Day18"].ToString();
                            Day19 = metaLinksDataRows[0]["Day19"].ToString();
                            Day20 = metaLinksDataRows[0]["Day20"].ToString();
                            Day21 = metaLinksDataRows[0]["Day21"].ToString();

                            Days = new string[] { Day1, Day2, Day3, Day4, Day5, Day6, Day7, Day8, Day9, Day10,
                                            Day11, Day12, Day13, Day14, Day15, Day16, Day17, Day18, Day19, Day20, Day21 };
                            for (int j = 0; j < 21; j++)
                            {
                                if (Days[j].Equals("0"))
                                    Days[j] = Properties.Resources.FollowupStatus0;
                                else if (Days[j].Equals("1"))
                                    Days[j] = Properties.Resources.FollowupStatus1;
                                else if (Days[j].Equals("2"))
                                    Days[j] = Properties.Resources.FollowupStatus2;
                                else if (Days[j].Equals("3"))
                                    Days[j] = Properties.Resources.FollowupStatus3;
                                else if (Days[j].Equals("4"))
                                    Days[j] = Properties.Resources.FollowupStatus4;
                                else if (Days[j].Equals("5"))
                                    Days[j] = Properties.Resources.FollowupStatus5;
                            }

                            Day1Notes = metaLinksDataRows[0]["Day1Notes"].ToString();
                            Day2Notes = metaLinksDataRows[0]["Day2Notes"].ToString();
                            Day3Notes = metaLinksDataRows[0]["Day3Notes"].ToString();
                            Day4Notes = metaLinksDataRows[0]["Day4Notes"].ToString();
                            Day5Notes = metaLinksDataRows[0]["Day5Notes"].ToString();
                            Day6Notes = metaLinksDataRows[0]["Day6Notes"].ToString();
                            Day7Notes = metaLinksDataRows[0]["Day7Notes"].ToString();
                            Day8Notes = metaLinksDataRows[0]["Day8Notes"].ToString();
                            Day9Notes = metaLinksDataRows[0]["Day9Notes"].ToString();
                            Day10Notes = metaLinksDataRows[0]["Day10Notes"].ToString();
                            Day11Notes = metaLinksDataRows[0]["Day11Notes"].ToString();
                            Day12Notes = metaLinksDataRows[0]["Day12Notes"].ToString();
                            Day13Notes = metaLinksDataRows[0]["Day13Notes"].ToString();
                            Day14Notes = metaLinksDataRows[0]["Day14Notes"].ToString();
                            Day15Notes = metaLinksDataRows[0]["Day15Notes"].ToString();
                            Day16Notes = metaLinksDataRows[0]["Day16Notes"].ToString();
                            Day17Notes = metaLinksDataRows[0]["Day17Notes"].ToString();
                            Day18Notes = metaLinksDataRows[0]["Day18Notes"].ToString();
                            Day19Notes = metaLinksDataRows[0]["Day19Notes"].ToString();
                            Day20Notes = metaLinksDataRows[0]["Day20Notes"].ToString();
                            Day21Notes = metaLinksDataRows[0]["Day21Notes"].ToString();
                            Temp1_1 = metaLinksDataRows[0]["Temp1_1"].ToString();
                            Temp1_2 = metaLinksDataRows[0]["Temp1_2"].ToString();
                            Temp1_3 = metaLinksDataRows[0]["Temp1_3"].ToString();
                            Temp1_4 = metaLinksDataRows[0]["Temp1_4"].ToString();
                            Temp1_5 = metaLinksDataRows[0]["Temp1_5"].ToString();
                            Temp1_6 = metaLinksDataRows[0]["Temp1_6"].ToString();
                            Temp1_7 = metaLinksDataRows[0]["Temp1_7"].ToString();
                            Temp1_8 = metaLinksDataRows[0]["Temp1_8"].ToString();
                            Temp1_9 = metaLinksDataRows[0]["Temp1_9"].ToString();
                            Temp1_10 = metaLinksDataRows[0]["Temp1_10"].ToString();
                            Temp1_11 = metaLinksDataRows[0]["Temp1_11"].ToString();
                            Temp1_12 = metaLinksDataRows[0]["Temp1_12"].ToString();
                            Temp1_13 = metaLinksDataRows[0]["Temp1_13"].ToString();
                            Temp1_14 = metaLinksDataRows[0]["Temp1_14"].ToString();
                            Temp1_15 = metaLinksDataRows[0]["Temp1_15"].ToString();
                            Temp1_16 = metaLinksDataRows[0]["Temp1_16"].ToString();
                            Temp1_17 = metaLinksDataRows[0]["Temp1_17"].ToString();
                            Temp1_18 = metaLinksDataRows[0]["Temp1_18"].ToString();
                            Temp1_19 = metaLinksDataRows[0]["Temp1_19"].ToString();
                            Temp1_20 = metaLinksDataRows[0]["Temp1_20"].ToString();
                            Temp1_21 = metaLinksDataRows[0]["Temp1_21"].ToString();
                            Temp2_1 = metaLinksDataRows[0]["Temp2_1"].ToString();
                            Temp2_2 = metaLinksDataRows[0]["Temp2_2"].ToString();
                            Temp2_3 = metaLinksDataRows[0]["Temp2_3"].ToString();
                            Temp2_4 = metaLinksDataRows[0]["Temp2_4"].ToString();
                            Temp2_5 = metaLinksDataRows[0]["Temp2_5"].ToString();
                            Temp2_6 = metaLinksDataRows[0]["Temp2_6"].ToString();
                            Temp2_7 = metaLinksDataRows[0]["Temp2_7"].ToString();
                            Temp2_8 = metaLinksDataRows[0]["Temp2_8"].ToString();
                            Temp2_9 = metaLinksDataRows[0]["Temp2_9"].ToString();
                            Temp2_10 = metaLinksDataRows[0]["Temp2_10"].ToString();
                            Temp2_11 = metaLinksDataRows[0]["Temp2_11"].ToString();
                            Temp2_12 = metaLinksDataRows[0]["Temp2_12"].ToString();
                            Temp2_13 = metaLinksDataRows[0]["Temp2_13"].ToString();
                            Temp2_14 = metaLinksDataRows[0]["Temp2_14"].ToString();
                            Temp2_15 = metaLinksDataRows[0]["Temp2_15"].ToString();
                            Temp2_16 = metaLinksDataRows[0]["Temp2_16"].ToString();
                            Temp2_17 = metaLinksDataRows[0]["Temp2_17"].ToString();
                            Temp2_18 = metaLinksDataRows[0]["Temp2_18"].ToString();
                            Temp2_19 = metaLinksDataRows[0]["Temp2_19"].ToString();
                            Temp2_20 = metaLinksDataRows[0]["Temp2_20"].ToString();
                            Temp2_21 = metaLinksDataRows[0]["Temp2_21"].ToString();
                        }
                        float progress = (float)Math.Round(numerator / (float)this.ContactCollection.Count, 2);
                        TaskbarProgressValue = progress;

                        SyncStatus = String.Format("Processing contact records {0}...", progress.ToString("P0"));

                        string dateLastContact = String.Empty;
                        string dateLastFollowUp = Properties.Resources.Never;
                        string sourceCaseName = String.Empty;
                        string sourceCaseID = String.Empty;
                        string relationshipToCase = String.Empty;
                        string contactTypes = String.Empty;

                        bool followedToday = false;
                        bool followedYesterday = false;
                        bool followedDayBeforeYesterday = false;

                        if (contactVM.HasFinalOutcome == false)
                        {
                            if (contactVM.FollowUpWindowViewModel != null) // this should NEVER be null... but in case it is
                            {
                                foreach (FollowUpVisitViewModel fuVM in contactVM.FollowUpWindowViewModel.FollowUpVisits)
                                {
                                    if (fuVM.IsSeen /*fuVM.Seen == SeenType.Seen*/)
                                    {
                                        DateTime fuDate = new DateTime(fuVM.Date.Year, fuVM.Date.Month, fuVM.Date.Day, 0, 0, 0);
                                        if (fuDate.Ticks == today.Ticks)
                                        {
                                            followedToday = true;
                                        }
                                        if (fuDate.Ticks == yesterday.Ticks)
                                        {
                                            followedYesterday = true;
                                        }
                                        if (fuDate.Ticks == dayBeforeYesterday.Ticks)
                                        {
                                            followedDayBeforeYesterday = true;
                                        }
                                    }
                                }
                            }
                        }

                        int totalSourceCases = 0;

                        foreach (CaseContactPairViewModel ccpVM in this.ContactLinkCollection)
                        {
                            if (ccpVM.ContactVM == contactVM)
                            {
                                totalSourceCases++;
                            }
                        }

                        if (contactVM.LastSourceCase != null && contactVM.DateOfLastContact.HasValue)
                        {
                            sourceCaseName = contactVM.LastSourceCase.Surname + " " + contactVM.LastSourceCase.OtherNames;
                            sourceCaseID = contactVM.LastSourceCase.ID;
                            dateLastContact = contactVM.DateOfLastContact.Value.ToShortDateString();
                            relationshipToCase = contactVM.RelationshipToLastSourceCase;
                            contactTypes = contactVM.LastSourceCaseContactTypes;
                        }

                        if (contactVM.DateOfLastFollowUp.HasValue)
                        {
                            dateLastFollowUp = contactVM.DateOfLastFollowUp.Value.ToShortDateString();
                        }

                        contactsTable.Rows.Add(contactVM.RecordId, contactVM.IsCase,
                            contactVM.ContactID, contactVM.Surname, contactVM.OtherNames, contactVM.Gender, contactVM.Age,
                            contactVM.HeadOfHousehold, contactVM.Village, contactVM.Parish, contactVM.District, contactVM.SubCounty, sourceCaseID,
                            sourceCaseName, relationshipToCase, contactTypes, dateLastContact, dateLastFollowUp, totalSourceCases,
                            contactVM.LC1Chairman, contactVM.Phone, contactVM.HCW, contactVM.HCWFacility,
                            contactVM.Team, contactVM.FinalOutcome,
                            DateTime.Today.ToShortDateString(),
                            followedToday, followedYesterday, followedDayBeforeYesterday,
                            Days[0], Days[1], Days[2], Days[3], Days[4], Days[5], Days[6], Days[7], Days[8], Days[9],
                            Days[10], Days[11], Days[12], Days[13], Days[14], Days[15], Days[16], Days[17], Days[18], Days[19], Days[20],
                            Temp1_1, Temp1_2, Temp1_3, Temp1_4, Temp1_5, Temp1_6, Temp1_7, Temp1_8, Temp1_9, Temp1_10,
                            Temp1_11, Temp1_12, Temp1_13, Temp1_14, Temp1_15, Temp1_16, Temp1_17, Temp1_18, Temp1_19, Temp1_20, Temp1_21,
                            Temp2_1, Temp2_2, Temp2_3, Temp2_4, Temp2_5, Temp2_6, Temp2_7, Temp2_8, Temp2_9, Temp2_10,
                            Temp2_11, Temp2_12, Temp2_13, Temp2_14, Temp2_15, Temp2_16, Temp2_17, Temp2_18, Temp2_19, Temp2_20, Temp2_21,
                            Day1Notes, Day2Notes, Day3Notes, Day4Notes, Day5Notes, Day6Notes, Day7Notes, Day8Notes, Day9Notes, Day10Notes,
                            Day11Notes, Day12Notes, Day13Notes, Day14Notes, Day15Notes, Day16Notes, Day17Notes, Day18Notes, Day19Notes, Day20Notes, Day21Notes);
                    }

                    bool exportResult = ExportView(contactsTable.DefaultView, fileName, false, true);
                    #endregion
                },
                 System.Threading.CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).ContinueWith(
                 delegate
                 {
                     //SendMessageForUnAwaitAll();

                     TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None;

                     IsExportingData = false;
                     SyncStatus = "Export complete.";

                 }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        /// <summary>
        /// Creates a CSV File for analysis for US Contact Form
        /// </summary>
        /// <param name="fileName">The path and name of the file to be generated</param>
        /// <param name="exportFull">false</param>
        /// <param name="convertCommentLegalValues">false</param>
        /// <param name="convertFieldPrompts">false</param>
        public void ExportContactsForAnalysisStartforUS(string fileName, bool exportFull = false, bool convertCommentLegalValues = false, bool convertFieldPrompts = false)
        {
            if (IsLoadingProjectData || IsSendingServerUpdates || IsWaitingOnOtherClients)
            {
                return;
            }

            if (String.IsNullOrEmpty(fileName.Trim()))
            {
                throw new ArgumentNullException("fileName");
            }

            //FinishedExportingMessageVisible = false;
            SyncStatus = "Starting data export...";
            ShowingDataExporterText = "Exporting Data";
            IsExportingData = true;
            IsShowingDataExporter = true;

            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();

            //SendMessageForAwaitAll();

            Task.Factory.StartNew(
                () =>
                {
                    DataTable contacttables = new DataTable("contactsTable");
                    contacttables.CaseSensitive = true;
                    contacttables = ContactTracing.Core.Common.JoinPageTables(Database, ContactForm);
                    contacttables.Columns.Remove("FirstSaveLogonName");
                    contacttables.Columns.Remove("FirstSaveTime");
                    contacttables.Columns.Remove("LastSaveLogonName");
                    contacttables.Columns.Remove("LastSaveTime");
                    if (contacttables.Columns.Contains("DistrictCodeContact"))
                    {
                        contacttables.Columns.Remove("DistrictCodeContact");
                    }
                    contacttables.Columns.Remove("FKEY");
                    if (contacttables.Columns.Contains("AdminOverride"))
                        contacttables.Columns.Remove("AdminOverride");
                    DataColumn dc3 = new DataColumn("TotalSourceCases", typeof(int));
                    DataColumn dc1 = new DataColumn("ThisContactIsAlsoCase", typeof(bool));
                    contacttables.Columns.Add(dc1);
                    contacttables.Columns.Add(dc3);
                    contacttables.Columns["ContactCDCID"].ColumnName = "CDC ID";
                    contacttables.Columns["ContactStateID"].ColumnName = "State/Local Id";
                    contacttables.Columns["ContactSurname"].ColumnName = "Last Name";
                    contacttables.Columns["ContactOtherNames"].ColumnName = "First Name";
                    contacttables.Columns["ContactGender"].ColumnName = "Gender";
                    contacttables.Columns["ContactAge"].ColumnName = "Age";
                    contacttables.Columns["ContactAddress"].ColumnName = "Address";
                    contacttables.Columns["ContactZip"].ColumnName = "Zip";
                    contacttables.Columns["ContactDOB"].ColumnName = "DOB";
                    contacttables.Columns["ContactVillage"].ColumnName = "City";
                    contacttables.Columns["ContactDistrict"].ColumnName = "State";
                    contacttables.Columns["ContactSC"].ColumnName = "County";
                    contacttables.Columns["ContactPhone"].ColumnName = "Phone";
                    contacttables.Columns["ContactHCW"].ColumnName = "HCW";
                    contacttables.Columns["ContactHCWFacility"].ColumnName = "HCWFacility";
                    contacttables.Columns.Add(new DataColumn("SourceCaseID", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("SourceCase", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("RelationshipToCase", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("ContactTypes", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("DateLastContact", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("DateOfLastFollowUp", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("DateCSVExported", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("FollowedToday", typeof(bool)));
                    contacttables.Columns.Add(new DataColumn("FollowedYesterday", typeof(bool)));
                    contacttables.Columns.Add(new DataColumn("FollowedDayBeforeYesterday", typeof(bool)));

                    contacttables.Columns.Add(new DataColumn("Day1", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day2", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day3", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day4", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day5", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day6", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day7", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day8", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day9", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day10", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day11", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day12", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day13", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day14", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day15", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day16", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day17", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day18", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day19", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day20", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day21", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp1_1", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp1_2", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp1_3", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp1_4", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp1_5", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp1_6", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp1_7", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp1_8", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp1_9", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp1_10", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp1_11", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp1_12", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp1_13", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp1_14", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp1_15", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp1_16", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp1_17", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp1_18", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp1_19", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp1_20", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp1_21", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp2_1", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp2_2", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp2_3", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp2_4", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp2_5", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp2_6", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp2_7", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp2_8", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp2_9", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp2_10", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp2_11", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp2_12", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp2_13", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp2_14", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp2_15", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp2_16", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp2_17", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp2_18", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp2_19", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp2_20", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Temp2_21", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day1Notes", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day2Notes", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day3Notes", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day4Notes", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day5Notes", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day6Notes", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day7Notes", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day8Notes", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day9Notes", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day10Notes", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day11Notes", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day12Notes", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day13Notes", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day14Notes", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day15Notes", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day16Notes", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day17Notes", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day18Notes", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day19Notes", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day20Notes", typeof(string)));
                    contacttables.Columns.Add(new DataColumn("Day21Notes", typeof(string)));
                    float numerator = 0.0f;
                    TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;

                    foreach (DataRow row in contacttables.Rows)
                    {
                        numerator += 1.0f;
                        float progress = (float)Math.Round(numerator / (float)contacttables.Rows.Count, 2);
                        //progBar.Value = (int)progress;

                        SyncStatus = String.Format("Processing contact records {0}...", progress.ToString("P0"));

                        TaskbarProgressValue = progress;
                        DateTime today = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
                        DateTime yesterday = (new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 0, 0, 0)).AddDays(-1);
                        DateTime dayBeforeYesterday = (new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 0, 0, 0)).AddDays(-2);
                        string guid = row["GlobalRecordId"].ToString();
                        ContactViewModel contactVM = GetContactVM(guid);
                        if (contactVM != null)
                        {
                            string dateLastContact = String.Empty;
                            string dateLastFollowUp = Properties.Resources.Never;
                            string sourceCaseName = String.Empty;
                            string sourceCaseID = String.Empty;
                            string relationshipToCase = String.Empty;
                            string contactTypes = String.Empty;

                            bool followedToday = false;
                            bool followedYesterday = false;
                            bool followedDayBeforeYesterday = false;


                            if (contactVM.HasFinalOutcome == false)
                            {
                                if (contactVM.FollowUpWindowViewModel != null) // this should NEVER be null... but in case it is
                                {
                                    foreach (FollowUpVisitViewModel fuVM in contactVM.FollowUpWindowViewModel.FollowUpVisits)
                                    {
                                        if (fuVM.IsSeen /*fuVM.Seen == SeenType.Seen*/)
                                        {
                                            DateTime fuDate = new DateTime(fuVM.Date.Year, fuVM.Date.Month, fuVM.Date.Day, 0, 0, 0);
                                            if (fuDate.Ticks == today.Ticks)
                                            {
                                                followedToday = true;
                                            }
                                            if (fuDate.Ticks == yesterday.Ticks)
                                            {
                                                followedYesterday = true;
                                            }
                                            if (fuDate.Ticks == dayBeforeYesterday.Ticks)
                                            {
                                                followedDayBeforeYesterday = true;
                                            }
                                        }
                                    }
                                }
                            }

                            int totalSourceCases = 0;

                            foreach (CaseContactPairViewModel ccpVM in this.ContactLinkCollection)
                            {
                                if (ccpVM.ContactVM == contactVM)
                                {
                                    totalSourceCases++;
                                }
                            }

                            if (contactVM.LastSourceCase != null && contactVM.DateOfLastContact.HasValue)
                            {
                                sourceCaseName = contactVM.LastSourceCase.Surname + " " + contactVM.LastSourceCase.OtherNames;
                                sourceCaseID = contactVM.LastSourceCase.OriginalID;
                                dateLastContact = contactVM.DateOfLastContact.Value.ToShortDateString();
                                relationshipToCase = contactVM.RelationshipToLastSourceCase;
                                contactTypes = contactVM.LastSourceCaseContactTypes;
                            }
                            if (contactVM.DateOfLastFollowUp.HasValue)
                            {
                                dateLastFollowUp = contactVM.DateOfLastFollowUp.Value.ToShortDateString();
                            }

                            row["ContactTypes"] = contactTypes;
                            row["ThisContactIsAlsoCase"] = contactVM.IsCase;
                            row["SourceCaseID"] = sourceCaseID;
                            row["SourceCase"] = sourceCaseName;
                            row["RelationshipToCase"] = relationshipToCase;
                            row["DateLastContact"] = dateLastContact;
                            row["DateOfLastFollowUp"] = dateLastFollowUp;
                            row["TotalSourceCases"] = totalSourceCases;
                            row["DateCSVExported"] = DateTime.Today.ToShortDateString();
                            row["FollowedToday"] = followedToday;
                            row["FollowedYesterday"] = followedYesterday;
                            row["FollowedDayBeforeYesterday"] = followedDayBeforeYesterday;

                            int links = this.metaLinksDataTable.Rows.Count;
                            DataRow[] metaLinksDataRows = this.metaLinksDataTable.Select("ToRecordGuid = '" + contactVM.RecordId + "' AND LastContactDate = '" + contactVM.DateOfLastContact + "'");
                            string Day1 = metaLinksDataRows[0]["Day1"].ToString();
                            string Day2 = metaLinksDataRows[0]["Day2"].ToString();
                            string Day3 = metaLinksDataRows[0]["Day3"].ToString();
                            string Day4 = metaLinksDataRows[0]["Day4"].ToString();
                            string Day5 = metaLinksDataRows[0]["Day5"].ToString();
                            string Day6 = metaLinksDataRows[0]["Day6"].ToString();
                            string Day7 = metaLinksDataRows[0]["Day7"].ToString();
                            string Day8 = metaLinksDataRows[0]["Day8"].ToString();
                            string Day9 = metaLinksDataRows[0]["Day9"].ToString();
                            string Day10 = metaLinksDataRows[0]["Day10"].ToString();
                            string Day11 = metaLinksDataRows[0]["Day11"].ToString();
                            string Day12 = metaLinksDataRows[0]["Day12"].ToString();
                            string Day13 = metaLinksDataRows[0]["Day13"].ToString();
                            string Day14 = metaLinksDataRows[0]["Day14"].ToString();
                            string Day15 = metaLinksDataRows[0]["Day15"].ToString();
                            string Day16 = metaLinksDataRows[0]["Day16"].ToString();
                            string Day17 = metaLinksDataRows[0]["Day17"].ToString();
                            string Day18 = metaLinksDataRows[0]["Day18"].ToString();
                            string Day19 = metaLinksDataRows[0]["Day19"].ToString();
                            string Day20 = metaLinksDataRows[0]["Day20"].ToString();
                            string Day21 = metaLinksDataRows[0]["Day21"].ToString();

                            string[] Days = { Day1, Day2, Day3, Day4, Day5, Day6, Day7, Day8, Day9, Day10,
                                            Day11, Day12, Day13, Day14, Day15, Day16, Day17, Day18, Day19, Day20, Day21 };
                            for (int j = 0; j < 21; j++)
                            {
                                if (Days[j].Equals("0"))
                                    Days[j] = Properties.Resources.FollowupStatus0;
                                else if (Days[j].Equals("1"))
                                    Days[j] = Properties.Resources.FollowupStatus1;
                                else if (Days[j].Equals("2"))
                                    Days[j] = Properties.Resources.FollowupStatus2;
                                else if (Days[j].Equals("3"))
                                    Days[j] = Properties.Resources.FollowupStatus3;
                                else if (Days[j].Equals("4"))
                                    Days[j] = Properties.Resources.FollowupStatus4;
                                else if (Days[j].Equals("5"))
                                    Days[j] = Properties.Resources.FollowupStatus5;
                            }

                            string Day1Notes = metaLinksDataRows[0]["Day1Notes"].ToString();
                            string Day2Notes = metaLinksDataRows[0]["Day2Notes"].ToString();
                            string Day3Notes = metaLinksDataRows[0]["Day3Notes"].ToString();
                            string Day4Notes = metaLinksDataRows[0]["Day4Notes"].ToString();
                            string Day5Notes = metaLinksDataRows[0]["Day5Notes"].ToString();
                            string Day6Notes = metaLinksDataRows[0]["Day6Notes"].ToString();
                            string Day7Notes = metaLinksDataRows[0]["Day7Notes"].ToString();
                            string Day8Notes = metaLinksDataRows[0]["Day8Notes"].ToString();
                            string Day9Notes = metaLinksDataRows[0]["Day9Notes"].ToString();
                            string Day10Notes = metaLinksDataRows[0]["Day10Notes"].ToString();
                            string Day11Notes = metaLinksDataRows[0]["Day11Notes"].ToString();
                            string Day12Notes = metaLinksDataRows[0]["Day12Notes"].ToString();
                            string Day13Notes = metaLinksDataRows[0]["Day13Notes"].ToString();
                            string Day14Notes = metaLinksDataRows[0]["Day14Notes"].ToString();
                            string Day15Notes = metaLinksDataRows[0]["Day15Notes"].ToString();
                            string Day16Notes = metaLinksDataRows[0]["Day16Notes"].ToString();
                            string Day17Notes = metaLinksDataRows[0]["Day17Notes"].ToString();
                            string Day18Notes = metaLinksDataRows[0]["Day18Notes"].ToString();
                            string Day19Notes = metaLinksDataRows[0]["Day19Notes"].ToString();
                            string Day20Notes = metaLinksDataRows[0]["Day20Notes"].ToString();
                            string Day21Notes = metaLinksDataRows[0]["Day21Notes"].ToString();
                            string Temp1_1 = metaLinksDataRows[0]["Temp1_1"].ToString();
                            string Temp1_2 = metaLinksDataRows[0]["Temp1_2"].ToString();
                            string Temp1_3 = metaLinksDataRows[0]["Temp1_3"].ToString();
                            string Temp1_4 = metaLinksDataRows[0]["Temp1_4"].ToString();
                            string Temp1_5 = metaLinksDataRows[0]["Temp1_5"].ToString();
                            string Temp1_6 = metaLinksDataRows[0]["Temp1_6"].ToString();
                            string Temp1_7 = metaLinksDataRows[0]["Temp1_7"].ToString();
                            string Temp1_8 = metaLinksDataRows[0]["Temp1_8"].ToString();
                            string Temp1_9 = metaLinksDataRows[0]["Temp1_9"].ToString();
                            string Temp1_10 = metaLinksDataRows[0]["Temp1_10"].ToString();
                            string Temp1_11 = metaLinksDataRows[0]["Temp1_11"].ToString();
                            string Temp1_12 = metaLinksDataRows[0]["Temp1_12"].ToString();
                            string Temp1_13 = metaLinksDataRows[0]["Temp1_13"].ToString();
                            string Temp1_14 = metaLinksDataRows[0]["Temp1_14"].ToString();
                            string Temp1_15 = metaLinksDataRows[0]["Temp1_15"].ToString();
                            string Temp1_16 = metaLinksDataRows[0]["Temp1_16"].ToString();
                            string Temp1_17 = metaLinksDataRows[0]["Temp1_17"].ToString();
                            string Temp1_18 = metaLinksDataRows[0]["Temp1_18"].ToString();
                            string Temp1_19 = metaLinksDataRows[0]["Temp1_19"].ToString();
                            string Temp1_20 = metaLinksDataRows[0]["Temp1_20"].ToString();
                            string Temp1_21 = metaLinksDataRows[0]["Temp1_21"].ToString();
                            string Temp2_1 = metaLinksDataRows[0]["Temp2_1"].ToString();
                            string Temp2_2 = metaLinksDataRows[0]["Temp2_2"].ToString();
                            string Temp2_3 = metaLinksDataRows[0]["Temp2_3"].ToString();
                            string Temp2_4 = metaLinksDataRows[0]["Temp2_4"].ToString();
                            string Temp2_5 = metaLinksDataRows[0]["Temp2_5"].ToString();
                            string Temp2_6 = metaLinksDataRows[0]["Temp2_6"].ToString();
                            string Temp2_7 = metaLinksDataRows[0]["Temp2_7"].ToString();
                            string Temp2_8 = metaLinksDataRows[0]["Temp2_8"].ToString();
                            string Temp2_9 = metaLinksDataRows[0]["Temp2_9"].ToString();
                            string Temp2_10 = metaLinksDataRows[0]["Temp2_10"].ToString();
                            string Temp2_11 = metaLinksDataRows[0]["Temp2_11"].ToString();
                            string Temp2_12 = metaLinksDataRows[0]["Temp2_12"].ToString();
                            string Temp2_13 = metaLinksDataRows[0]["Temp2_13"].ToString();
                            string Temp2_14 = metaLinksDataRows[0]["Temp2_14"].ToString();
                            string Temp2_15 = metaLinksDataRows[0]["Temp2_15"].ToString();
                            string Temp2_16 = metaLinksDataRows[0]["Temp2_16"].ToString();
                            string Temp2_17 = metaLinksDataRows[0]["Temp2_17"].ToString();
                            string Temp2_18 = metaLinksDataRows[0]["Temp2_18"].ToString();
                            string Temp2_19 = metaLinksDataRows[0]["Temp2_19"].ToString();
                            string Temp2_20 = metaLinksDataRows[0]["Temp2_20"].ToString();
                            string Temp2_21 = metaLinksDataRows[0]["Temp2_21"].ToString();

                            row["Day1"] = Days[0];
                            row["Day2"] = Days[1];
                            row["Day3"] = Days[2];
                            row["Day4"] = Days[3];
                            row["Day5"] = Days[4];
                            row["Day6"] = Days[5];
                            row["Day7"] = Days[6];
                            row["Day8"] = Days[7];
                            row["Day9"] = Days[8];
                            row["Day10"] = Days[9];
                            row["Day11"] = Days[10];
                            row["Day12"] = Days[11];
                            row["Day13"] = Days[12];
                            row["Day14"] = Days[13];
                            row["Day15"] = Days[14];
                            row["Day16"] = Days[15];
                            row["Day17"] = Days[16];
                            row["Day18"] = Days[17];
                            row["Day19"] = Days[18];
                            row["Day20"] = Days[19];
                            row["Day21"] = Days[20];

                            row["Day1Notes"] = Day1Notes;
                            row["Day2Notes"] = Day2Notes;
                            row["Day3Notes"] = Day3Notes;
                            row["Day4Notes"] = Day4Notes;
                            row["Day5Notes"] = Day5Notes;
                            row["Day6Notes"] = Day6Notes;
                            row["Day7Notes"] = Day7Notes;
                            row["Day8Notes"] = Day8Notes;
                            row["Day9Notes"] = Day9Notes;
                            row["Day10Notes"] = Day10Notes;
                            row["Day11Notes"] = Day11Notes;
                            row["Day12Notes"] = Day12Notes;
                            row["Day13Notes"] = Day13Notes;
                            row["Day14Notes"] = Day14Notes;
                            row["Day15Notes"] = Day15Notes;
                            row["Day16Notes"] = Day16Notes;
                            row["Day17Notes"] = Day17Notes;
                            row["Day18Notes"] = Day18Notes;
                            row["Day19Notes"] = Day19Notes;
                            row["Day20Notes"] = Day20Notes;
                            row["Day21Notes"] = Day21Notes;
                            row["Temp1_1"] = Temp1_1;
                            row["Temp1_2"] = Temp1_2;
                            row["Temp1_3"] = Temp1_3;
                            row["Temp1_4"] = Temp1_4;
                            row["Temp1_5"] = Temp1_5;
                            row["Temp1_6"] = Temp1_6;
                            row["Temp1_7"] = Temp1_7;
                            row["Temp1_8"] = Temp1_8;
                            row["Temp1_9"] = Temp1_9;
                            row["Temp1_10"] = Temp1_10;
                            row["Temp1_11"] = Temp1_11;
                            row["Temp1_12"] = Temp1_12;
                            row["Temp1_13"] = Temp1_13;
                            row["Temp1_14"] = Temp1_14;
                            row["Temp1_15"] = Temp1_15;
                            row["Temp1_16"] = Temp1_16;
                            row["Temp1_17"] = Temp1_17;
                            row["Temp1_18"] = Temp1_18;
                            row["Temp1_19"] = Temp1_19;
                            row["Temp1_20"] = Temp1_20;
                            row["Temp2_21"] = Temp2_21;
                            row["Temp2_1"] = Temp2_1;
                            row["Temp2_2"] = Temp2_2;
                            row["Temp2_3"] = Temp2_3;
                            row["Temp2_4"] = Temp2_4;
                            row["Temp2_5"] = Temp2_5;
                            row["Temp2_6"] = Temp2_6;
                            row["Temp2_7"] = Temp2_7;
                            row["Temp2_8"] = Temp2_8;
                            row["Temp2_9"] = Temp2_9;
                            row["Temp2_10"] = Temp2_10;
                            row["Temp2_11"] = Temp2_11;
                            row["Temp2_12"] = Temp2_12;
                            row["Temp2_13"] = Temp2_13;
                            row["Temp2_14"] = Temp2_14;
                            row["Temp2_15"] = Temp2_15;
                            row["Temp2_16"] = Temp2_16;
                            row["Temp2_17"] = Temp2_17;
                            row["Temp2_18"] = Temp2_18;
                            row["Temp2_19"] = Temp2_19;
                            row["Temp2_20"] = Temp2_20;
                            row["Temp2_21"] = Temp2_21;
                        }
                    }
                    bool exportResult = ExportView(contacttables.DefaultView, fileName, false, true);

                },
                 System.Threading.CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).ContinueWith(
                 delegate
                 {
                     //SendMessageForUnAwaitAll();

                     TaskbarProgressState = System.Windows.Shell.TaskbarItemProgressState.None;

                     IsExportingData = false;
                     SyncStatus = "Export complete.";

                 }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        #endregion // Methods
    }
}
