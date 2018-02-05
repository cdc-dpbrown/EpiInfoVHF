using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Epi;
using Epi.Data;
using Epi.Fields;
using Epi.ImportExport.Filters;
using ContactTracing.Core;
using ContactTracing.Core.Enums;

namespace ContactTracing.ImportExport
{
    public class XmlSqlDataImporter : XmlDataImporter
    {
        #region Members
        private object _followUpLock = new object();
        #endregion // Members

        #region Events
        #endregion // Events

        #region Properties
        #endregion // Properties

        #region Constructors
        public XmlSqlDataImporter(VhfProject project, RecordProcessingScope scope)
            : base(project, scope)
        {

        }
        #endregion // Constructors

        protected override void UpdateRecord(IDbConnection conn, IDbDriver db, View form, XElement record, string globalRecordId)
        {
            if (form.Pages.Count == 1)
            {
                Page page = form.Pages[0];
                UpdatePageData(conn, form, page, db, record, globalRecordId);
            }
            else
            {
                Parallel.ForEach(form.Pages, page =>
                {
                    using (SqlConnection connection = db.GetConnection() as SqlConnection)
                    {
                        connection.Open();
                        UpdatePageData(connection, form, page, db, record, globalRecordId);
                    }
                });
            }
        }
        
        protected override bool CreateNewRow(IDbConnection conn, View form, XElement record, string guid, string fkey = "", string recStatus = "1", string firstSaveId = "", string lastSaveId = "", DateTime? firstSaveTime = null, DateTime? lastSaveTime = null)
        {
            SqlConnection connection = conn as SqlConnection;

            IDbDriver db = Project.CollectedData.GetDatabase();
            StringBuilder sb = new StringBuilder();
            sb.Append(" insert into ");
            sb.Append(db.InsertInEscape(form.TableName));
            sb.Append(StringLiterals.SPACE);
            sb.Append(StringLiterals.SPACE);

            WordBuilder fields = new WordBuilder(",");
            fields.Append("[GlobalRecordId]");

            if (!String.IsNullOrEmpty(fkey)) { fields.Append("[FKEY]"); }
            if (!String.IsNullOrEmpty(recStatus)) { fields.Append("[RecStatus]"); }
            if (!String.IsNullOrEmpty(firstSaveId)) { fields.Append("[FirstSaveLogonName]"); }
            if (!String.IsNullOrEmpty(lastSaveId)) { fields.Append("[LastSaveLogonName]"); }
            if (firstSaveTime.HasValue)
            {
                firstSaveTime = new DateTime(firstSaveTime.Value.Year,
                firstSaveTime.Value.Month,
                firstSaveTime.Value.Day,
                firstSaveTime.Value.Hour,
                firstSaveTime.Value.Minute,
                firstSaveTime.Value.Second);
                fields.Append("[FirstSaveTime]");
            }
            if (lastSaveTime.HasValue)
            {
                lastSaveTime = new DateTime(lastSaveTime.Value.Year,
                lastSaveTime.Value.Month,
                lastSaveTime.Value.Day,
                lastSaveTime.Value.Hour,
                lastSaveTime.Value.Minute,
                lastSaveTime.Value.Second);
                fields.Append("[LastSaveTime]");
            }

            sb.Append("(" + fields.ToString() + ")");
            sb.Append(" values (");

            List<QueryParameter> parameters = new List<QueryParameter>();
            WordBuilder values = new WordBuilder(",");
            values.Append("'" + guid + "'");

            if (!String.IsNullOrEmpty(fkey))
            {
                values.Append("@FKEY");
                parameters.Add(new QueryParameter("@FKEY", DbType.String, fkey));
            }
            if (!String.IsNullOrEmpty(recStatus))
            {
                values.Append("@RecStatus");
                parameters.Add(new QueryParameter("@RecStatus", DbType.Int32, Convert.ToInt32(recStatus)));
            }
            if (!String.IsNullOrEmpty(firstSaveId))
            {
                values.Append("@FirstSaveLogonName");
                parameters.Add(new QueryParameter("@FirstSaveLogonName", DbType.String, firstSaveId));
            }
            if (!String.IsNullOrEmpty(lastSaveId))
            {
                values.Append("@LastSaveLogonName");
                parameters.Add(new QueryParameter("@LastSaveLogonName", DbType.String, lastSaveId));
            }
            if (firstSaveTime.HasValue)
            {
                values.Append("@FirstSaveTime");
                parameters.Add(new QueryParameter("@FirstSaveTime", DbType.DateTime, firstSaveTime));
            }
            if (lastSaveTime.HasValue)
            {
                values.Append("@LastSaveTime");
                parameters.Add(new QueryParameter("@LastSaveTime", DbType.DateTime, lastSaveTime));
            }

            sb.Append(values.ToString());
            sb.Append(") ");
            Epi.Data.Query insertQuery = db.CreateQuery(sb.ToString());
            insertQuery.Parameters = parameters;

            using (SqlTransaction transaction = connection.BeginTransaction("SampleTransaction"))
            {
                try
                {
                    using (IDbCommand baseTableCommand = GetCommand(insertQuery.SqlStatement, conn, insertQuery.Parameters))
                    {
                        baseTableCommand.Transaction = transaction;
                        object baseObj = baseTableCommand.ExecuteNonQuery();
                    }

                    //foreach (Page page in form.Pages)
                    //{
                    //    sb = new StringBuilder();
                    //    sb.Append(" insert into ");
                    //    sb.Append(db.InsertInEscape(page.TableName));
                    //    sb.Append(StringLiterals.SPACE);
                    //    sb.Append(StringLiterals.SPACE);
                    //    sb.Append("([GlobalRecordId])");
                    //    sb.Append(" values (");
                    //    sb.Append("'" + guid + "'");
                    //    sb.Append(") ");
                    //    insertQuery = db.CreateQuery(sb.ToString());

                    //    using (IDbCommand pageTableCommand = GetCommand(insertQuery.SqlStatement, conn, insertQuery.Parameters))
                    //    {
                    //        pageTableCommand.Transaction = transaction;
                    //        object pageObj = pageTableCommand.ExecuteNonQuery();
                    //    }
                    //}

                    foreach (Page page in form.Pages)
                    {
                        WordBuilder wbFieldNames = new WordBuilder(", ");
                        WordBuilder wbParamNames = new WordBuilder(", ");
                        List<QueryParameter> pageInsertParameters = new List<QueryParameter>();

                        foreach (RenderableField field in page.Fields)
                        {
                            if (field is IDataField && record.Element(field.Name) != null)
                            {
                                wbFieldNames.Add(field.Name);
                                wbParamNames.Add("@" + field.Name);

                                pageInsertParameters.Add(
                                    GetQueryParameterForField(field,
                                        FormatFieldData(form, field.Name, record.Element(field.Name).Value),
                                        form,
                                        page));
                            }
                        }

                        wbFieldNames.Add("GlobalRecordId");
                        wbParamNames.Add("@GlobalRecordId");
                        pageInsertParameters.Add(new QueryParameter("@GlobalRecordId", DbType.String, guid));

                        Query inserteQuery = db.CreateQuery("INSERT INTO " + page.TableName + " (" + wbFieldNames.ToString() + ") VALUES (" + wbParamNames.ToString() + ")");

                        foreach (QueryParameter parameter in pageInsertParameters)
                        {
                            inserteQuery.Parameters.Add(parameter);
                        }

                        using (IDbCommand pageTableCommand = GetCommand(inserteQuery.SqlStatement, conn, inserteQuery.Parameters))
                        {
                            pageTableCommand.Transaction = transaction;
                            object obj = pageTableCommand.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    Epi.Logger.Log(String.Format(DateTime.Now + ":  " + "Commit Exception Type: {0}", ex.GetType()));
                    Epi.Logger.Log(String.Format(DateTime.Now + ":  " + "Commit Exception Message: {0}", ex.Message));

                    try
                    {
                        transaction.Rollback();
                    }
                    catch (Exception ex2)
                    {
                        Epi.Logger.Log(String.Format(DateTime.Now + ":  " + "Rollback Exception Type: {0}", ex2.GetType()));
                        Epi.Logger.Log(String.Format(DateTime.Now + ":  " + "Rollback Exception Message: {0}", ex2.Message));
                    }

                    return false;
                }
            }

            return true;
        }
    }
}
