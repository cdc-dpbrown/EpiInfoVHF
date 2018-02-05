using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ContactTracing.SyncFileViewer
{
    public class AnalysisViewModel : ObservableObject
    {
        private readonly XElement _syncFile;

        public AnalysisViewModel(XElement syncFile)
        {
            _syncFile = syncFile;

            foreach (XElement element in _syncFile.Elements("Form"))
            {
                if (element.Attribute("Name").Value.Equals("CaseInformationForm", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (XElement record in element.Element("Data").Elements("Record"))
                    {
                        if (record.Element("EpiCaseDef") != null)
                        {
                            switch (record.Element("EpiCaseDef").Value)
                            {
                                case "0":
                                    _epiCaseDef0++;
                                    break;
                                case "1":
                                    _epiCaseDef1++;
                                    break;
                                case "2":
                                    _epiCaseDef2++;
                                    break;
                                case "3":
                                    _epiCaseDef3++;
                                    break;
                                case "4":
                                    _epiCaseDef4++;
                                    break;
                            }
                        }
                        if (record.Element("FinalLabClass") != null)
                        {
                            switch (record.Element("FinalLabClass").Value)
                            {
                                case "0":
                                    _finalLabClass0++;
                                    break;
                                case "1":
                                    _finalLabClass1++;
                                    break;
                                case "2":
                                    _finalLabClass2++;
                                    break;
                                case "3":
                                    _finalLabClass3++;
                                    break;
                                case "4":
                                    _finalLabClass4++;
                                    break;
                                case "5":
                                    _finalLabClass5++;
                                    break;
                            }
                        }
                        if (record.Attribute("RecStatus") != null)
                        {
                            switch (record.Attribute("RecStatus").Value)
                            {
                                case "0":
                                    _recStatus0++;
                                    break;
                                case "1":
                                    _recStatus1++;
                                    break;
                            }
                        }
                    }

                    EpiCaseDefT = EpiCaseDef0 + EpiCaseDef1 + EpiCaseDef2 + EpiCaseDef3 + EpiCaseDef4;

                    RaisePropertyChanged("EpiCaseDef0");
                    RaisePropertyChanged("EpiCaseDef1");
                    RaisePropertyChanged("EpiCaseDef2");
                    RaisePropertyChanged("EpiCaseDef3");
                    RaisePropertyChanged("EpiCaseDef4");

                    FinalLabClassT = FinalLabClass0 + FinalLabClass1 + FinalLabClass2 + FinalLabClass3 + FinalLabClass4 + FinalLabClass5;

                    RaisePropertyChanged("FinalLabClass0");
                    RaisePropertyChanged("FinalLabClass1");
                    RaisePropertyChanged("FinalLabClass2");
                    RaisePropertyChanged("FinalLabClass3");
                    RaisePropertyChanged("FinalLabClass4");
                    RaisePropertyChanged("FinalLabClass5");

                    RaisePropertyChanged("RecStatus0");
                    RaisePropertyChanged("RecStatus1");

                }
            }
        }

        private int _recStatus0 = 0;
        private int _recStatus1 = 0;

        private int _epiCaseDef0 = 0;
        private int _epiCaseDef1 = 0;
        private int _epiCaseDef2 = 0;
        private int _epiCaseDef3 = 0;
        private int _epiCaseDef4 = 0;
        private int _epiCaseDefM = 0;
        private int _epiCaseDefT = 0;

        private int _finalLabClass0 = 0;
        private int _finalLabClass1 = 0;
        private int _finalLabClass2 = 0;
        private int _finalLabClass3 = 0;
        private int _finalLabClass4 = 0;
        private int _finalLabClass5 = 0;
        private int _finalLabClassT = 0;
        
        public int RecStatus0 { get { return _recStatus0; } set { _recStatus0 = value; RaisePropertyChanged("RecStatus0"); } }
        public int RecStatus1 { get { return _recStatus1; } set { _recStatus1 = value; RaisePropertyChanged("RecStatus1"); } }

        public int EpiCaseDef0
        {
            get
            {
                return _epiCaseDef0;
            }
            set
            {
                _epiCaseDef0 = value;
                RaisePropertyChanged("EpiCaseDef0");
            }
        }

        public int EpiCaseDef1
        {
            get
            {
                return _epiCaseDef1;
            }
            set
            {
                _epiCaseDef1 = value;
                RaisePropertyChanged("EpiCaseDef1");
            }
        }

        public int EpiCaseDef2
        {
            get
            {
                return _epiCaseDef2;
            }
            set
            {
                _epiCaseDef2 = value;
                RaisePropertyChanged("EpiCaseDef2");
            }
        }

        public int EpiCaseDef3
        {
            get
            {
                return _epiCaseDef3;
            }
            set
            {
                _epiCaseDef3 = value;
                RaisePropertyChanged("EpiCaseDef3");
            }
        }

        public int EpiCaseDef4
        {
            get
            {
                return _epiCaseDef4;
            }
            set
            {
                _epiCaseDef4 = value;
                RaisePropertyChanged("EpiCaseDef4");
            }
        }

        public int EpiCaseDefM
        {
            get
            {
                return _epiCaseDefM;
            }
            set
            {
                _epiCaseDefM = value;
                RaisePropertyChanged("EpiCaseDefM");
            }
        }

        public int EpiCaseDefT
        {
            get
            {
                return _epiCaseDefT;
            }
            set
            {
                _epiCaseDefT = value;
                RaisePropertyChanged("EpiCaseDefT");
            }
        }




        public int FinalLabClass0
        {
            get
            {
                return _finalLabClass0;
            }
            set
            {
                _finalLabClass0 = value;
                RaisePropertyChanged("FinalLabClass0");
            }
        }

        public int FinalLabClass1
        {
            get
            {
                return _finalLabClass1;
            }
            set
            {
                _finalLabClass1 = value;
                RaisePropertyChanged("FinalLabClass1");
            }
        }

        public int FinalLabClass2
        {
            get
            {
                return _finalLabClass2;
            }
            set
            {
                _finalLabClass2 = value;
                RaisePropertyChanged("FinalLabClass2");
            }
        }

        public int FinalLabClass3
        {
            get
            {
                return _finalLabClass3;
            }
            set
            {
                _finalLabClass3 = value;
                RaisePropertyChanged("FinalLabClass3");
            }
        }

        public int FinalLabClass4
        {
            get
            {
                return _finalLabClass4;
            }
            set
            {
                _finalLabClass4 = value;
                RaisePropertyChanged("FinalLabClass4");
            }
        }

        public int FinalLabClass5
        {
            get
            {
                return _finalLabClass5;
            }
            set
            {
                _finalLabClass5 = value;
                RaisePropertyChanged("FinalLabClass5");
            }
        }

        public int FinalLabClassT
        {
            get
            {
                return _finalLabClassT;
            }
            set
            {
                _finalLabClassT = value;
                RaisePropertyChanged("FinalLabClassT");
            }
        }
    }
}
