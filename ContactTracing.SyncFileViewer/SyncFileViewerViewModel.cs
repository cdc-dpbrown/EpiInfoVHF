using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Xml.Linq;

namespace ContactTracing.SyncFileViewer
{
    public sealed class SyncFileViewerViewModel : ObservableObject
    {
        #region Members
        private XElement _syncFile;
        private XElement _caseForm;
        private XElement _caseData;

        private XElement _contactForm;
        private XElement _contactData;

        private XElement _labForm;
        private XElement _labData;

        private XElement _linksData;

        private XElement _followUpsData;

        private string _syncFileName = String.Empty;

        private AnalysisViewModel _analysisViewModel = null;

        private string _sourceDatabase = String.Empty;
        private string _dateGenerated = String.Empty;
        private string _dateGeneratedUtc = String.Empty;
        private string _epiVersion = String.Empty;
        private string _vhfVersion = String.Empty;
        private string _fileID = String.Empty;
        private string _startDate = String.Empty;
        private string _endDate = String.Empty;
        private string _region = String.Empty;

        #endregion // Members

        #region Properties

        public string SourceDatabase { get { return _sourceDatabase; } set { _sourceDatabase = value; RaisePropertyChanged("SourceDatabase"); } }
        public string DateGenerated { get { return _dateGenerated; } set { _dateGenerated = value; RaisePropertyChanged("DateGenerated"); } }
        public string DateGeneratedUtc { get { return _dateGeneratedUtc; } set { _dateGeneratedUtc = value; RaisePropertyChanged("DateGeneratedUtc"); } }
        public string VhfVersion { get { return _vhfVersion; } set { _vhfVersion = value; RaisePropertyChanged("VhfVersion"); } }
        public string EpiVersion { get { return _epiVersion; } set { _epiVersion = value; RaisePropertyChanged("EpiVersion"); } }
        public string FileID { get { return _fileID; } set { _fileID = value; RaisePropertyChanged("FileID"); } }

        public string StartDate { get { return _startDate; } set { _startDate = value; RaisePropertyChanged("StartDate"); } }
        public string EndDate { get { return _endDate; } set { _endDate = value; RaisePropertyChanged("EndDate"); } }

        public string Region { get { return _region; } set { _region = value; RaisePropertyChanged("Region"); } }

        public AnalysisViewModel AnalysisViewModel
        {
            get
            {
                return _analysisViewModel;
            }
            set
            {
                _analysisViewModel = value;
                RaisePropertyChanged("AnalysisViewModel");
            }
        }

        public string SyncFileName
        {
            get
            {
                return _syncFileName;
            }
            set
            {
                _syncFileName = value;
                RaisePropertyChanged("SyncFileName");
            }
        }

        public XElement SyncFile
        {
            get
            {
                return _syncFile;
            }
            private set
            {
                _syncFile = value;
                RaisePropertyChanged("SyncFile");
            }
        }

        public XElement CaseForm
        {
            get
            {
                return _caseForm;
            }
            private set
            {
                _caseForm = value;
                RaisePropertyChanged("CaseForm");
            }
        }

        public XElement CaseData
        {
            get
            {
                return _caseData;
            }
            private set
            {
                _caseData = value;
                RaisePropertyChanged("CaseData");
            }
        }

        public XElement ContactForm
        {
            get
            {
                return _contactForm;
            }
            private set
            {
                _contactForm = value;
                RaisePropertyChanged("ContactForm");
            }
        }

        public XElement ContactData
        {
            get
            {
                return _contactData;
            }
            private set
            {
                _contactData = value;
                RaisePropertyChanged("ContactData");
            }
        }

        public XElement LabForm
        {
            get
            {
                return _labForm;
            }
            private set
            {
                _labForm = value;
                RaisePropertyChanged("LabForm");
            }
        }

        public XElement LabData
        {
            get
            {
                return _labData;
            }
            private set
            {
                _labData = value;
                RaisePropertyChanged("LabData");
            }
        }

        public XElement LinksData
        {
            get
            {
                return _linksData;
            }
            private set
            {
                _linksData = value;
                RaisePropertyChanged("LinksData");
            }
        }

        public XElement FollowUpsData
        {
            get
            {
                return _followUpsData;
            }
            private set
            {
                _followUpsData = value;
                RaisePropertyChanged("FollowUpsData");
            }
        }

        //public ICollectionView Cases { get; private set; }
        public ICollectionView CasesOfContact { get; private set; }
        //public ICollectionView Contacts { get; private set; }
        public ICollectionView ContactsOfCase { get; private set; }
        //public ICollectionView Relationships { get; private set; }
        //public ICollectionView FollowUps { get; private set; }

        #endregion // Properties

        public SyncFileViewerViewModel()
        {
        }

        public void LoadSyncFile(XElement doc)
        {
            SyncFile = doc;

            SourceDatabase = String.Empty;

            if (doc.Attribute("SourceDbType") != null)
            {
                SourceDatabase = doc.Attribute("SourceDbType").Value;
            }
            if (doc.Attribute("CreatedDate") != null)
            {
                DateGenerated = doc.Attribute("CreatedDate").Value;
            }
            if (doc.Attribute("CreatedUtc") != null)
            {
                DateGeneratedUtc = doc.Attribute("CreatedUtc").Value;
            }
            if (doc.Attribute("Version") != null)
            {
                EpiVersion = doc.Attribute("Version").Value;
            }
            if (doc.Attribute("VhfVersion") != null)
            {
                VhfVersion = doc.Attribute("VhfVersion").Value;
            }
            if (doc.Attribute("Id") != null)
            {
                FileID = doc.Attribute("Id").Value;
            }
            if (doc.Attribute("Region") != null)
            {
                Region = doc.Attribute("Region").Value;
            }

            if (doc.Attribute("StartDate") != null)
            {
                StartDate = doc.Attribute("StartDate").Value;
            }
            if (doc.Attribute("EndDate") != null)
            {
                EndDate = doc.Attribute("EndDate").Value;
            }

            foreach (XElement element in doc.Elements("Form"))
            {
                if (element.Attribute("Name").Value.Equals("CaseInformationForm", StringComparison.OrdinalIgnoreCase))
                {
                    CaseForm = element;

                    // because sync files created from MDB-based projects have <record>s broken up by page it's necessary to rebuild them here

                    if (SyncFile.Attribute("SourceDbType") != null && SyncFile.Attribute("SourceDbType").Value.Equals("Access"))
                    {
                        CaseData = RebuildCaseRecords();
                    }
                    else
                    {
                        CaseData = CaseForm.Element("Data");
                    }
                }
                else if (element.Attribute("Name").Value.Equals("ContactEntryForm", StringComparison.OrdinalIgnoreCase))
                {
                    ContactForm = element;
                    ContactData = ContactForm.Element("Data");
                }
                else if (element.Attribute("Name").Value.Equals("LaboratoryResultsForm", StringComparison.OrdinalIgnoreCase))
                {
                    LabForm = element;
                    LabData = LabForm.Element("Data");
                }
            }

            LinksData = doc.Element("Links");

            FollowUpsData = doc.Element("ContactFollowUps");

            AnalysisViewModel = new AnalysisViewModel(SyncFile);
        }

        private XElement RebuildCaseRecords()
        {
            XElement newData = new XElement("Data");

            XElement oldData = CaseForm.Element("Data");

            Dictionary<string, XElement> recordDictionary = new Dictionary<string, XElement>();

            foreach (XElement record in oldData.Elements("Record"))
            {
                string guid = record.Attribute("Id").Value;

                if (recordDictionary.ContainsKey(guid))
                {
                    foreach (XElement field in record.Elements())
                    {
                        recordDictionary[guid].Add(new XElement(field.Name, field.Value));
                            
                    }
                }
                else
                {
                    XElement newElement = new XElement("Record");

                    newElement.Add(new XAttribute("Id", guid),
                            new XAttribute("FKEY", record.Attribute("FKEY").Value),
                            new XAttribute("FirstSaveUserId", record.Attribute("FirstSaveUserId").Value),
                            new XAttribute("LastSaveUserId", record.Attribute("LastSaveUserId").Value),
                            new XAttribute("FirstSaveTime", record.Attribute("FirstSaveTime").Value),
                            new XAttribute("LastSaveTime", record.Attribute("LastSaveTime").Value),
                            new XAttribute("RecStatus", record.Attribute("RecStatus").Value));

                    foreach (XElement field in record.Elements())
                    {
                        newElement.Add(new XElement(field.Name, field.Value));
                    }
                    
                    recordDictionary.Add(guid, newElement);
                }
            }

            foreach (KeyValuePair<string, XElement> kvp in recordDictionary)
            {
                newData.Add(kvp.Value);
            }

            return newData;
        }
    }
}
