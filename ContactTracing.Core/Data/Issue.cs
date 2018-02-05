using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContactTracing.Core;

namespace ContactTracing.Core.Data
{
    public class Issue : ObservableObject
    {
        private string _id = String.Empty;
        private string _problem = String.Empty;
        private string _code = String.Empty;

        public string ID
        {
            get
            {
                return this._id;
            }
            private set
            {
                if (this._id != value)
                {
                    this._id = value;
                    RaisePropertyChanged("ID");
                }
            }
        }
        public string Problem
        {
            get
            {
                return this._problem;
            }
            private set
            {
                if (this._problem != value)
                {
                    this._problem = value;
                    RaisePropertyChanged("Problem");
                }
            }
        }

        public string Code
        {
            get
            {
                return this._code;
            }
            private set
            {
                if (this._code != value)
                {
                    this._code = value;
                    RaisePropertyChanged("Code");
                }
            }
        }

        public Issue(string id, string code, string problem)
        {
            ID = id;
            Code = code;
            Problem = problem;
        }
    }
}
