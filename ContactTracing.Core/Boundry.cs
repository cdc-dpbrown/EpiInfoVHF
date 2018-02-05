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

        public Boundry(string name, string columnName, string objectResolution)
        {
            Name = name;
            ColumnName = columnName;
            ObjectResolution = objectResolution;
        }
    }
}
