using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ContactTracing.Core;

namespace ContactTracing.CaseView
{
    class BoundryAggregation
    {
        private Dictionary<int, Boundry> _boundryAggregation = new Dictionary<int, Boundry>();

        public BoundryAggregation(string superBoundry, params string[] subBountryList)
        {
            if (_boundryAggregation == null)
            {
                _boundryAggregation = new Dictionary<int, Boundry>();
            }

            if (superBoundry == "USA")
            {
                if (_boundryAggregation.Count == 0)
                {
                    _boundryAggregation.Add(3, new Boundry(subBountryList[0], "ContactDistrict", "ContactVM.District", caseObjectResolution: "District"));//STATE
                    _boundryAggregation.Add(2, new Boundry(subBountryList[1], "ContactSC", "ContactVM.SubCounty", caseObjectResolution: "SubCounty"));//COUNTY
                    _boundryAggregation.Add(0, new Boundry(subBountryList[3], "ContactVillage", "ContactVM.Village", caseObjectResolution: "Village"));//CITY
                    _boundryAggregation.Add(-1, new Boundry("Zip Code", "ContactZipCode", "ContactVM.ZipCode", caseObjectResolution: "ZipRes"));
                    _boundryAggregation.Add(-2, new Boundry("Address", "ContactAddress", "ContactVM.Address", caseObjectResolution: "AddressRes"));
                }
            }
            else
            {
                if (_boundryAggregation.Count == 0)
                {
                    _boundryAggregation.Add(3, new Boundry(subBountryList[0], "ContactDistrict", "ContactVM.District"));
                    _boundryAggregation.Add(2, new Boundry(subBountryList[1], "ContactSC", "ContactVM.SubCounty"));
                    _boundryAggregation.Add(0, new Boundry(subBountryList[3], "ContactVillage", "ContactVM.Village"));
                }
            }
        }

        public Dictionary<int, Boundry> BoundaryAggregation
        {
            get
            {
                return _boundryAggregation;
            }
        }
    }
}
