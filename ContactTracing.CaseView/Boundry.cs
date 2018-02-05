using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactTracing.Core
{
    public class Boundry
    {
        public string Name { set; get; }
        public string ColumnName { set; get; }
        public string ObjectResolution { set; get; }
        public string CaseObjectResolution { set; get; }
        
        public string CaseObjectValue(object instance) 
        {
            return GetValue(instance, CaseObjectResolution);
        }

        public string ContactObjectValue(object instance)
        {
            return GetValue(instance, ObjectResolution.Replace("ContactVM.",""));
        }

        public Boundry(string name, string columnName = "", string objectResolution = "", string caseObjectResolution = "")
        {
            Name = name;
            ColumnName = columnName;
            ObjectResolution = objectResolution;
            CaseObjectResolution = caseObjectResolution;
        }

        private string GetValue(object instance, string objectResolution)
        {
            object returnValue = null;
            System.Reflection.PropertyInfo neoProp;
            var neoType = instance.GetType();
            neoProp = neoType.GetProperty(objectResolution);
            returnValue = neoProp.GetValue(instance, null);
            if(returnValue == null)
            {
                return string.Empty;
            }
            else
            {
                return returnValue.ToString();
            }
        }
    }
}
