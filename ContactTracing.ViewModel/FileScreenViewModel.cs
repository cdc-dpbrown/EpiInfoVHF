using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Text;
using System.Windows.Input;
using System.Xml;
using ContactTracing.Core;
using ContactTracing.Core.Data;

namespace ContactTracing.ViewModel
{
    public class FileScreenViewModel : ObservableObject
    {
        private ObservableCollection<ProjectInfo> _projects = new ObservableCollection<ProjectInfo>();

        public ApplicationType ApplicationType { get; set; }

        private Timer UpdateTimer { get; set; }

        private bool _shouldPollForFiles = true;
        public bool ShouldPollForFiles
        {
            get
            {
                return _shouldPollForFiles;
            }
            set
            {
                _shouldPollForFiles = value;
            }
        }

        public ObservableCollection<ProjectInfo> Projects
        {
            get { return this._projects; }
            private set
            {
                this._projects = value;
                RaisePropertyChanged("Projects");
            }
        }

        private object _projectsLock = new object();

        public FileScreenViewModel()
        {
            System.Windows.Data.BindingOperations.EnableCollectionSynchronization(Projects, _projectsLock);

            this.UpdateTimer = new Timer(6000);
            this.UpdateTimer.Elapsed += UpdateTimer_Elapsed;
            this.UpdateTimer.Start();
        }

        private void UpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!ShouldPollForFiles)
            {
                return;
            }

            PopulateCollections();
        }

        public void ClearCollections()
        {
            lock (_projectsLock)
            {
                foreach (ProjectInfo info in Projects)
                {
                    info.Dispose();
                }
                this.Projects.Clear();
            }
        }

        public async void PopulateCollections()
        {
            bool fadeDelay = true;
            string[] filePaths = Directory.GetFiles(@"Projects" + Path.DirectorySeparatorChar.ToString() + "VHF", "vhf_*.prj",
                                            SearchOption.AllDirectories);

            if (filePaths.Length > 5)
            {
                fadeDelay = false;
            }

            await Task.Factory.StartNew(delegate
            {
                lock (_projectsLock)
                {
                    foreach (string filePath in filePaths)
                    {
                        bool fileFound = false;

                        foreach (ProjectInfo info in Projects)
                        {
                            FileInfo fi = new FileInfo(filePath);
                            if (info.FileInfo.FullName.Equals(fi.FullName, StringComparison.OrdinalIgnoreCase))
                            {
                                fileFound = true;
                                break;
                            }
                        }

                        if (fileFound)
                        {
                            continue;
                        }

                        XmlDocument doc = new XmlDocument();
                        doc.XmlResolver = null;
                        try
                        {
                            doc.Load(filePath);
                        }
                        catch (XmlException)
                        {
                            // invalid XML, skip the project
                            continue;
                        }

                        XmlNode projectNode = doc.SelectSingleNode("Project");

                        string fileName = filePath;
                        bool isVHF = false;
                        bool isLabDb = false;
                        DateTime? outbreakDate = null;
                        string outbreakName = String.Empty;
                        string connectionString = String.Empty;
                        string projectName = String.Empty;
                        string culture = String.Empty;
                        Guid id = new Guid();

                        id = new Guid(projectNode.Attributes["id"].Value);
                        projectName = projectNode.Attributes["name"].Value;

                        foreach (XmlElement element in projectNode.ChildNodes)
                        {
                            switch (element.Name)
                            {
                                case "IsVHF":
                                    isVHF = bool.Parse(element.InnerText);
                                    break;
                                case "IsLabProject":
                                    isLabDb = bool.Parse(element.InnerText);
                                    break;
                                case "OutbreakName":
                                    outbreakName = element.InnerText;
                                    break;
                                case "OutbreakDate":
                                    outbreakDate = new DateTime(long.Parse(element.InnerText));
                                    break;
                                case "Culture":
                                    culture = element.InnerText;
                                    break;
                            }
                        }

                        if (isVHF)
                        {
                            ProjectInfo projectInfo = new ProjectInfo(fileName, outbreakName, String.Empty, isVHF, outbreakDate, culture);

                            projectName = projectName.Replace("vhf_", String.Empty);

                            projectInfo.Name = projectName;
                            projectInfo.Id = id;
                            projectInfo.IsExistingProject = true;
                            projectInfo.Reg_culture = createCultureText(culture);

                            if (!isLabDb && ApplicationType == Core.ApplicationType.Epi)
                            {
                                bool found = false;

                                foreach (ProjectInfo info in Projects)
                                {
                                    if (info.FileInfo.FullName.Equals(projectInfo.FileInfo.FullName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        found = true;
                                        break;
                                    }
                                }

                                if (!found)
                                {
                                    Projects.Add(projectInfo);

                                    if (fadeDelay)
                                    {
                                        System.Threading.Thread.Sleep(125);
                                    }
                                }
                            }
                            else if (isLabDb && ApplicationType == Core.ApplicationType.Lab)
                            {
                                Projects.Add(projectInfo);
                            }
                        }
                    }

                    // when deleting a project, this code will get rid of the items in the list... don't need this currently
                    //try 
                    //{
                    //    List<ProjectInfo> projectsToRemove = new List<ProjectInfo>();
                    //    foreach (ProjectInfo info in Projects)
                    //    {
                    //        bool foundProject = false;
                    //        foreach (string fileName in filePaths)
                    //        {
                    //            FileInfo fi = new FileInfo(fileName);
                    //            if (info.FileInfo.FullName.Equals(fi.FullName, StringComparison.OrdinalIgnoreCase))
                    //            {
                    //                foundProject = true;
                    //                break;
                    //            }
                    //        }

                    //        if (!foundProject)
                    //        {
                    //            projectsToRemove.Add(info);
                    //        }
                    //    }

                    //    foreach (ProjectInfo info in projectsToRemove)
                    //    {
                    //        Projects.Remove(info);
                    //    }
                    //}
                    //catch (Exception)
                    //{
                    //    // do nothing
                    //}
                }
            });
        }

        private string createCultureText(string culture)
        {
            if (culture == null) culture = "";         
            // if (culture == "") return culture;

            string resultText;

            if (ApplicationViewModel.Instance.FileScreenDictionary.ContainsKey(culture.ToLower()))
            {
                resultText = ApplicationViewModel.Instance.FileScreenDictionary[culture.ToLower()];
            }
            else
                resultText = "Unknown Region and Culture";

            return resultText;
        }
    }
}
