using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Epi;
using Epi.Data;
using Epi.Fields;
using Epi.ImportExport.Filters;
using ContactTracing.Core;
using ContactTracing.Core.Enums;

namespace ContactTracing.ImportExport
{
    public sealed class XmlSqlDataExporter : XmlDataExporter
    {
        public XmlSqlDataExporter(VhfProject project, bool includeContacts, RecordProcessingScope recordProcessingScope)
            : base(project, includeContacts, recordProcessingScope)
        {
            _database = "SQL Server";
        }

        protected override Query GetFormSelectQuery(View form, bool selectAll = true)
        {
            IDbDriver db = form.Project.CollectedData.GetDatabase();

            string selectQueryText = String.Empty;
            Query selectQuery = null;

            SyncFileFilters filters = null;

            int count = 0;
            if (Filters != null && Filters.ContainsKey(form.Name))
            {
                foreach (Epi.ImportExport.IRowFilterCondition fc in Filters[form.Name])
                {
                    count++;
                }
            }

            if (count > 0)
            {
                filters = Filters[form.Name];
                filters.RecordProcessingScope = Scope;
                selectQuery = filters.GetGuidSelectQuery(form);

                List<QueryParameter> paramsToAdd = selectQuery.Parameters;
                if (selectAll)
                {
                    selectQuery = db.CreateQuery(selectQuery.SqlStatement.Replace("[t].[GlobalRecordId], [t].[FKEY], [t].[RECSTATUS]", "*") + " ORDER BY [t].[GlobalRecordId]");
                }
                else
                {
                    selectQuery = db.CreateQuery(selectQuery.SqlStatement + " ORDER BY [t].[GlobalRecordId]");
                }
                selectQuery.Parameters = paramsToAdd;
                //selectQuery.Parameters.Add(new QueryParameter("@StartDate", DbType.DateTime, StartDate));
                //selectQuery.Parameters.Add(new QueryParameter("@EndDate", DbType.DateTime, EndDate));
            }
            else
            {
                string recStatusClause = "RECSTATUS = 1";

                if (Scope == Epi.RecordProcessingScope.Both)
                {
                    recStatusClause = "RECSTATUS >= 0";
                }
                else if (Scope == Epi.RecordProcessingScope.Deleted)
                {
                    recStatusClause = "RECSTATUS = 0";
                }

                //selectQueryText = "SELECT * " + form.FromViewSQL + " WHERE " + recStatusClause + " AND " + LAST_SAVE_TIME + " ORDER BY [t].[GlobalRecordId]";
                selectQueryText = "SELECT * " + form.FromViewSQL + " WHERE " + recStatusClause + " ORDER BY [t].[GlobalRecordId]";
                selectQuery = db.CreateQuery(selectQueryText);
                //selectQuery.Parameters.Add(new QueryParameter("@StartDate", DbType.DateTime, StartDate));
                //selectQuery.Parameters.Add(new QueryParameter("@EndDate", DbType.DateTime, EndDate));
            }

            return selectQuery;
        }

        protected override void WriteFormData(XmlWriter writer, View form)
        {
            MinorProgress = 0;
            OnMinorProgressChanged();

            List<string> fieldsToNull = GetFieldsToNull(form.Name).ToList();

            writer.WriteStartElement("Form");

            writer.WriteAttributeString("Name", form.Name);
            writer.WriteAttributeString("Pages", form.Pages.Count.ToString());
            writer.WriteAttributeString("IsRelatedForm", form.IsRelatedView.ToString());

            WriteFormMetadata(writer, form);

            writer.WriteStartElement("Data");

            IDbDriver db = Project.CollectedData.GetDatabase();

            Query selectQuery = GetFormSelectQuery(form);

            //Query countQuery = db.CreateQuery("SELECT COUNT(*) FROM " + form.TableName + " WHERE " + recStatusClause + " AND ((LastSaveTime >= @StartDate AND LastSaveTime <= @EndDate) OR LastSaveTime IS NULL)");
            Query countQuery = db.CreateQuery("SELECT COUNT(*) " + form.FromViewSQL + " " + Filters[form.Name].GetWhereClause(form));// WHERE " + recStatusClause + " AND ((LastSaveTime >= @StartDate AND LastSaveTime <= @EndDate) OR LastSaveTime IS NULL)");
            //countQuery.Parameters.Add(new QueryParameter("@StartDate", DbType.DateTime, StartDate));
            //countQuery.Parameters.Add(new QueryParameter("@EndDate", DbType.DateTime, EndDate));

            countQuery.Parameters = GetFormSelectQuery(form).Parameters;

            double totalRecords = Convert.ToDouble(db.ExecuteScalar(countQuery));
            double inc = 100 / totalRecords;

            List<string> labGuids = null;

            if (Filters.ContainsKey(Core.Constants.CASE_FORM_NAME) && Filters[Core.Constants.CASE_FORM_NAME].Count() > 0 && _caseGuids != null)
            {
                if (form.Name.Equals(Core.Constants.CONTACT_FORM_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    _contactGuids = new List<string>();

                    Query guidSelectQuery = db.CreateQuery("SELECT C.GlobalRecordId, M.FromRecordGuid FROM (ContactEntryForm C INNER JOIN metaLinks M ON C.GlobalRecordId = M.ToRecordGuid) WHERE M.ToViewId = @ToViewId AND M.FromViewId = 1");
                    guidSelectQuery.Parameters.Add(new QueryParameter("@ToViewId", DbType.Int32, ContactFormId));

                    using (IDataReader reader = db.ExecuteReader(guidSelectQuery))
                    {
                        while (reader.Read())
                        {
                            string caseGuid = reader["FromRecordGuid"].ToString();

                            if (_caseGuids.Contains(caseGuid))
                            {
                                _contactGuids.Add(reader["GlobalRecordId"].ToString());
                            }
                        }
                    }
                }
                else if (form.Name.Equals(Core.Constants.LAB_FORM_NAME))
                {
                    labGuids = new List<string>();

                    Query guidSelectQuery = db.CreateQuery("SELECT L.GlobalRecordId, L.FKEY FROM (CaseInformationForm C INNER JOIN LaboratoryResultsForm L ON C.GlobalRecordId = L.FKEY)");

                    using (IDataReader reader = db.ExecuteReader(guidSelectQuery))
                    {
                        while (reader.Read())
                        {
                            string labGuid = reader["GlobalRecordId"].ToString();
                            string caseGuid = reader["FKEY"].ToString();

                            if (_caseGuids.Contains(caseGuid))
                            {
                                labGuids.Add(reader["GlobalRecordId"].ToString());
                            }
                        }
                    }
                }
            }

            bool isCaseForm = form.Name.Equals(Core.Constants.CASE_FORM_NAME, StringComparison.OrdinalIgnoreCase);
            if (isCaseForm) { _caseGuids = new HashSet<string>(); }

            using (IDataReader reader = db.ExecuteReader(selectQuery))
            {
                //int i = 1;
                while (reader.Read())
                {
                    string recordGuid = reader["GlobalRecordId"].ToString();
                    string lastSaveTimeStr = String.Empty;
                    long? lastSaveTimeLong = null;
                    DateTime? lastSaveTime = null;

                    if (reader["LastSaveTime"] != DBNull.Value)
                    {
                        lastSaveTime = Convert.ToDateTime(reader["LastSaveTime"]);
                        lastSaveTimeLong = lastSaveTime.Value.Ticks;
                        lastSaveTimeStr = lastSaveTimeLong.ToString();

                        if (lastSaveTime < StartDate || lastSaveTime > EndDate)
                        {
                            MinorProgress += inc;
                            OnMinorProgressChanged();
                            OnMinorStatusChanged(String.Format("{0} of records exported from " + form.Name + "...", (MinorProgress / 100).ToString("P0")));

                            if (isCaseForm)
                            {
                                // we want to add the GUID here so related records (e.g. contacts and labs) don't get excluded because
                                // their case wasn't in the date range.
                                _caseGuids.Add(recordGuid);
                            }

                            continue;
                        }
                    }

                    if (form.Name.Equals(Core.Constants.CONTACT_FORM_NAME, StringComparison.OrdinalIgnoreCase) && _contactGuids != null && !_contactGuids.Contains(recordGuid)) { continue; }
                    if (form.Name.Equals(Core.Constants.LAB_FORM_NAME, StringComparison.OrdinalIgnoreCase) && labGuids != null && !labGuids.Contains(recordGuid)) { continue; }

                    if (isCaseForm)
                    {
                        _caseGuids.Add(recordGuid);
                    }

                    writer.WriteStartElement("Record");
                    writer.WriteAttributeString("Id", recordGuid);
                    writer.WriteAttributeString("FKEY", reader["FKEY"] == DBNull.Value ? String.Empty : reader["FKEY"].ToString());
                    writer.WriteAttributeString("FirstSaveUserId", reader["FirstSaveLogonName"].ToString());
                    writer.WriteAttributeString("LastSaveUserId", reader["LastSaveLogonName"].ToString());
                    if (reader["FirstSaveTime"] != DBNull.Value)
                    {
                        writer.WriteAttributeString("FirstSaveTime", Convert.ToDateTime(reader["FirstSaveTime"]).Ticks.ToString());
                    }
                    else
                    {
                        writer.WriteAttributeString("FirstSaveTime", String.Empty);
                    }
                    writer.WriteAttributeString("LastSaveTime", lastSaveTimeStr);
                    writer.WriteAttributeString("RecStatus", reader["RecStatus"].ToString());

                    foreach (IDataField dataField in form.Fields.DataFields)
                    {
                        RenderableField field = dataField as RenderableField;

                        if (field == null || dataField is UniqueKeyField || fieldsToNull.Contains(field.Name))
                        {
                            continue;
                        }
                        else
                        {
                            if (reader[field.Name] != DBNull.Value && !String.IsNullOrEmpty(reader[field.Name].ToString()))
                            {
                                writer.WriteStartElement(field.Name);
                                WriteFieldValue(writer, reader, field);
                                writer.WriteEndElement();
                            }
                        }
                    }

                    writer.WriteEndElement(); // record
                    
                    MinorProgress += inc;
                    OnMinorProgressChanged();

                    OnMinorStatusChanged(String.Format("{0} of records exported from " + form.Name + "...", (MinorProgress / 100).ToString("P0")));
                    //OnMinorStatusChanged(String.Format("{1} ({0}) of records exported from " + form.Name + "...", i.ToString(), (MinorProgress / 100).ToString("P0")));

                    ExportInfo.RecordsPackaged[form]++;

                    //i++;
                }
            }

            writer.WriteEndElement(); // data element

            writer.WriteEndElement(); // form element
        }
    }
}
