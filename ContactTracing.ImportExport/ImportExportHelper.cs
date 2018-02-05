using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Epi;
using Epi.Data;
using ContactTracing.Core;

namespace ContactTracing.ImportExport
{
    public static class ImportExportHelper
    {
        private static void UpdateMetaFields(Project project, string countryName = "")
        {
            IDbDriver db = project.CollectedData.GetDatabase();

            // 1 = text
            // 17, 18, 19 = ddl's

            Query updateQuery = db.CreateQuery("UPDATE [metaFields] SET FieldTypeId = 1 " +
                "WHERE (FieldTypeId = 17 OR FieldTypeId = 18 OR FieldTypeId = 19) " +
                "AND (PromptText = @PromptTextDistrict OR PromptText = @PromptTextSC)");

            updateQuery.Parameters.Add(new QueryParameter("@PromptTextDistrict", DbType.String, "District:"));
            updateQuery.Parameters.Add(new QueryParameter("@PromptTextSC", DbType.String, "Sub-County:"));

            int rows = db.ExecuteNonQuery(updateQuery);

            if (rows == 0)
            {
                // shouldn't get here
            }

            #region Wipe out districts
            string querySyntax = "DELETE * FROM [codeDistrictSubCountyList]";
            if (db.ToString().ToLower().Contains("sql"))
            {
                querySyntax = "DELETE FROM [codeDistrictSubCountyList]";
            }

            Query deleteQuery = db.CreateQuery(querySyntax);
            db.ExecuteNonQuery(deleteQuery);
            #endregion // Wipe out districts

            updateQuery = db.CreateQuery("UPDATE [metaFields] " +
                "SET PromptText = 'Viral Hemorrhagic Fever Outbreak Laboratory Diagnostic Specimens and Results Form' " +
                "WHERE FieldId = 230 OR FieldId = 590");
            rows = db.ExecuteNonQuery(updateQuery);

            updateQuery = db.CreateQuery("UPDATE [metaFields] SET [ControlLeftPositionPercentage] = @CLPP WHERE [Name] = @Name");
            updateQuery.Parameters.Add(new QueryParameter("@CLPP", DbType.Double, 0.01));
            updateQuery.Parameters.Add(new QueryParameter("@Name", DbType.String, "CRFTitle"));
            rows = db.ExecuteNonQuery(updateQuery);

            updateQuery = db.CreateQuery("UPDATE [metaFields] " +
                "SET PromptText = 'Viral Hemorrhagic Fever Contact Information Entry Form' " +
                "WHERE FieldId = 345");
            rows = db.ExecuteNonQuery(updateQuery);

            updateQuery = db.CreateQuery("UPDATE [metaFields] " +
                "SET PromptText = @CountryName " +
                "WHERE FieldId = 4");
            updateQuery.Parameters.Add(new QueryParameter("@CountryName", DbType.String, countryName + " Viral Hemorrhagic Fever Case Investigation Form"));
            rows = db.ExecuteNonQuery(updateQuery);

            if (rows == 0)
            {
                // shouldn't get here
            }

            updateQuery = db.CreateQuery("UPDATE metaPages " +
                "SET BackgroundId = 0");
            rows = db.ExecuteNonQuery(updateQuery);

            if (rows == 0)
            {
                // shouldn't get here
            }
        }

        public static VhfProject CreateNewOutbreak(string country, string cultureValue, string newProjectName, string newProjectDatabaseName, string outbreakDateTicks, string outbreakName) 
        {
            bool updateMetaFields = false;

            if (country.Equals("USA", StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(@"Projects\VHF\base_vhf_template_us.mdb", newProjectDatabaseName);
            }
            else
            {
                switch (cultureValue)
                {
                    case "fr":
                    case "fr-FR":
                    case "fr-fr":
                    case "fr­­­­­­­­­­­–FR":
                    case "fr­­­­­­­­­­­–fr":
                    case "fr­­­­­­­­­­­­­­­­­­­­­­—FR":
                    case "fr­­­­­­­­­­­­­­­­­­­­­­—fr":
                    case "fr―­­­­­­­­­­­­­­­FR":
                    case "fr­­­­­­­­­­­­­­­­­­­­­­―fr":
                        File.Copy(@"Projects\VHF\base_vhf_template_fr.mdb", newProjectDatabaseName);
                        break;
                    default:
                        File.Copy(@"Projects\VHF\base_vhf_template.mdb", newProjectDatabaseName);

                        if (!country.Equals("Uganda", StringComparison.OrdinalIgnoreCase))
                        {
                            updateMetaFields = true;
                        }
                        break;
                }
            }
            Epi.Util.CreateProjectFileFromDatabase(newProjectDatabaseName, true);

            // add vhf tags to xml document
            XmlDocument doc = new XmlDocument();
            doc.XmlResolver = null;
            doc.Load(newProjectName);

            XmlNode projectNode = doc.SelectSingleNode("Project");

            XmlElement isVhfElement = doc.CreateElement("IsVHF");
            XmlElement isLabElement = doc.CreateElement("IsLabProject");
            XmlElement outbreakNameElement = doc.CreateElement("OutbreakName");
            XmlElement outbreakDateElement = doc.CreateElement("OutbreakDate");
            XmlElement cultureElement = doc.CreateElement("Culture");

            isVhfElement.InnerText = "true";
            isLabElement.InnerText = "false";
            outbreakDateElement.InnerText = outbreakDateTicks;
            outbreakNameElement.InnerText = outbreakName;
            cultureElement.InnerText = cultureValue;

            projectNode.AppendChild(isVhfElement);
            projectNode.AppendChild(isLabElement);
            projectNode.AppendChild(outbreakDateElement);
            projectNode.AppendChild(outbreakNameElement);
            projectNode.AppendChild(cultureElement);

            System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
            XmlAttribute appVersionAttribute = doc.CreateAttribute("appVersion");
            appVersionAttribute.Value = a.GetName().Version.ToString();

            projectNode.Attributes.Append(appVersionAttribute);

            doc.Save(newProjectName);

            VhfProject project = new VhfProject(newProjectName);

            if (updateMetaFields)
            {
                if (System.Threading.Thread.CurrentThread.CurrentUICulture.ToString().Equals("en-US", StringComparison.OrdinalIgnoreCase))
                {
                    UpdateMetaFields(project, country);
                }
                else
                {
                    UpdateMetaFields(project);
                }
            }

            return project;
        }
    }
}
