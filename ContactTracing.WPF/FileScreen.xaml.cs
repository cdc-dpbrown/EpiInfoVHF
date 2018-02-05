using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ContactTracing.Core;
using ContactTracing.ViewModel;

namespace ContactTracing.Controls
{
    /// <summary>
    /// Interaction logic for FileScreen.xaml
    /// </summary>
    public partial class FileScreen : UserControl
    {
        public FileScreen()
        {
            InitializeComponent();
            ShouldPollForFiles = true;
        }

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
                FileScreenViewModel fsVM = this.DataContext as FileScreenViewModel;
                if (fsVM != null)
                {
                    fsVM.ShouldPollForFiles = ShouldPollForFiles;
                }
            }
        }

        public static readonly DependencyProperty ApplicationTypeProperty = DependencyProperty.Register("ApplicationType", typeof(ApplicationType), typeof(FileScreen));
        public ApplicationType ApplicationType
        {
            get
            {
                return (ApplicationType)(this.GetValue(ApplicationTypeProperty));
            }
            set
            {
                this.SetValue(ApplicationTypeProperty, value);
            }
        }

        private void ProjectsSummaryView_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e != null && e.OriginalSource != null)
            {
                Grid grdProject = e.OriginalSource as Grid;
                if (grdProject != null && grdProject.DataContext != null)
                {
                    Core.Data.ProjectInfo info = grdProject.DataContext as Core.Data.ProjectInfo;

                    if (ProjectOpened != null && info != null && info.IsShowingConnectionEditor == false)
                    {
                        bool isSuperUser = false;
                        if (System.Windows.Forms.Control.ModifierKeys == System.Windows.Forms.Keys.Control)
                        {
                            isSuperUser = true;
                        }

                        Core.Events.ProjectOpenedArgs args = new Core.Events.ProjectOpenedArgs(info, isSuperUser);
                        ProjectOpened(this, args);
                        ShouldPollForFiles = false;
                    }
                }
            }
        }

        public event ProjectOpenedHandler ProjectOpened;

        public delegate void ProjectOpenedHandler(object sender, Core.Events.ProjectOpenedArgs e);

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Refresh();

            System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
            lblVersion.Content = a.GetName().Version;
        }

        public void Refresh()
        {
            FileScreenViewModel fsVM = this.DataContext as FileScreenViewModel;
            if (fsVM != null)
            {
                fsVM.ApplicationType = this.ApplicationType;
                fsVM.ClearCollections();
                fsVM.PopulateCollections();
            }
        }
    }
}
