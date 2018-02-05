
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ContactTracing.Core.Enums;      

namespace ContactTracing.Core.Events
{
    public class RegionChangedEventArgs : EventArgs
    {
        private RegionEnum regionEnum;

        public RegionEnum RegionEnum
        {
            get { return regionEnum; }
            set { regionEnum = value; }
        }

        public RegionChangedEventArgs(RegionEnum regionEnum )
        {

            this.regionEnum = regionEnum;
        }
    }
}        



