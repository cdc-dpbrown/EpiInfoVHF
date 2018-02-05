using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Epi.Data;

namespace ContactTracing.Core
{
    public static class DbLogger
    {
        public static IDbDriver Database;
        private static string _vhfVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        private static string _user = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString();
        private static string _macAddress = Core.Common.GetMacAddress();

        public static async void Log(string description)
        {
            if (Database == null || !Database.ToString().ToLower().Contains("sql")) return;

            Query insertQuery = Database.CreateQuery("INSERT INTO [UpdateLog] (UDate, VhfVer, UserName, MACADDR, Description) VALUES (CURRENT_TIMESTAMP, @VhfVer, @User, @MACADDR, @Description)");

            //insertQuery.Parameters.Add(new QueryParameter("@UDate", System.Data.DbType.DateTime2, DateTime.Now));
            insertQuery.Parameters.Add(new QueryParameter("@VhfVer", System.Data.DbType.String, _vhfVersion));
            insertQuery.Parameters.Add(new QueryParameter("@User", System.Data.DbType.String, _user));
            insertQuery.Parameters.Add(new QueryParameter("@MACADDR", System.Data.DbType.String, _macAddress));
            insertQuery.Parameters.Add(new QueryParameter("@Description", System.Data.DbType.String, description));

            await Task.Factory.StartNew( () =>
            {
                try
                {
                    int rows = Database.ExecuteNonQuery(insertQuery);
                }
                catch (Exception)
                {
                    // do nothing, don't let this crash the app
                }
            });
        }
    }
}
