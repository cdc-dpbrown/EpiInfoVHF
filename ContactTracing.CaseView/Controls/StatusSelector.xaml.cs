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
using ContactTracing.ViewModel.Events;

namespace ContactTracing.CaseView.Controls
{
    /// <summary>
    /// Interaction logic for StatusSelector.xaml
    /// </summary>
    public partial class StatusSelector : UserControl
    {
        public event DailyCheckStatusChangedEventHandler StatusChanged;

        public delegate void DailyCheckStatusChangedEventHandler(object sender, DailyCheckStatusChangedEventArgs e);

        public StatusSelector()
        {
            InitializeComponent();
        }

        //public DailyCheckViewModel DailyCheck
        //{
        //    get { return (DailyCheckViewModel)GetValue(DailyCheckProperty); }
        //    set { SetValue(DailyCheckProperty, value); }
        //}

        ////public static readonly DependencyProperty DailyCheckProperty = DependencyProperty.Register("DailyCheck", typeof(DailyCheckViewModel), typeof(StatusSelector), new FrameworkPropertyMetadata(null, OnDailyCheckPropertyChanged));
        //public static readonly DependencyProperty DailyCheckProperty = DependencyProperty.Register("DailyCheck", typeof(DailyCheckViewModel), typeof(StatusSelector), new FrameworkPropertyMetadata(OnDailyCheckPropertyChanged));

        //private static void OnDailyCheckPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        //{
        //}


        //private void CheckBox_Checked(object sender, RoutedEventArgs e)
        //{
        //    DeCheck = false;
        //    CheckBox checkbox = (sender as CheckBox);
        //    if (checkbox.Name.Equals("chkbxN"))
        //    {
        //        Reset();
        //    }

        //    if (checkbox != null)
        //    {
        //        bool found = false;
        //        foreach (UIElement element in panelSelectors.Children)
        //        {
        //            if (element == checkbox)
        //            {
        //                found = true;
        //                break;
        //            }
        //        }

        //        if (found)
        //        {
        //            // make checkboxes mutually exclusive
        //            foreach (UIElement element in panelSelectors.Children)
        //            {
        //                if (element is CheckBox && (element as CheckBox) != checkbox)
        //                {
        //                    CheckBox iCheckbox = (element as CheckBox);
        //                    //iCheckbox.IsChecked = null;
        //                    iCheckbox.IsChecked = false;
        //                }
        //            }
        //        }

        //        if (chkbx2.IsChecked == false)
        //        {
        //            // if the user changes the status from 'sick' to something else, then make sure the iso checkboxes get unchecked
        //            if (chkbx5.IsChecked == true) chkbx5.IsChecked = false;
        //            if (chkbx6.IsChecked == true) chkbx6.IsChecked = false;
        //        }
        //        else if (chkbx2.IsChecked == true)
        //        {
        //            if (chkbx5.IsChecked == false && chkbx6.IsChecked == false) chkbx7.IsChecked = true;
        //            if (chkbx5 == checkbox) chkbx6.IsChecked = false;
        //            if (chkbx6 == checkbox) chkbx5.IsChecked = false;
        //        }

        //        if (StatusChanged != null)
        //        {
        //            DailyCheckStatusChangedEventArgs args = new DailyCheckStatusChangedEventArgs(this);
        //            StatusChanged(DataContext as DailyCheckViewModel, args);
        //        }
        //    }
        //}

        public void Reset()
        {
            DailyCheckViewModel dcVM = DataContext as DailyCheckViewModel;
            if (dcVM != null)
            {
                dcVM.IsStatusUnknown = true;
            }
        }

        private DailyCheckViewModel DailyCheck
        {
            get
            {
                return (this.DataContext as DailyCheckViewModel);
            }
        }

        private bool DeCheck { get; set; }

        private bool IsLoading { get; set; }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            IsLoading = true;

            if (DailyCheck == null)
            {
                Epi.Logger.Log(DateTime.Now + ":  " +
                    System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString() + ": " +
                    "Status selector detected null DailyCheckViewModel in UserControl_Loaded(). Exiting method.");
                return;
            }

            if (DailyCheck.Status.HasValue)
            {
                switch (DailyCheck.Status)
                {
                    case ContactDailyStatus.SeenNotSick:
                        chkbx1.IsChecked = true;
                        chkbx2.IsChecked = false;
                        chkbx3.IsChecked = false;
                        chkbx4.IsChecked = false;
                        chkbxN.IsChecked = false;

                        chkbx5.IsChecked = false;
                        chkbx6.IsChecked = false;
                        chkbx7.IsChecked = false;
                        break;
                    // we should not be displaying anyone in this list with this status...
                    case ContactDailyStatus.Dead:
                    case ContactDailyStatus.SeenSickAndIsolated:
                        chkbx1.IsChecked = false;
                        chkbx2.IsChecked = true;
                        chkbx3.IsChecked = false;
                        chkbx4.IsChecked = false;
                        chkbxN.IsChecked = false;

                        chkbx5.IsChecked = true;
                        chkbx6.IsChecked = false;
                        chkbx7.IsChecked = false;

                        Epi.Logger.Log(DateTime.Now + ":  " +
                            System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString() + ": " +
                            "Contact " + DailyCheck.ContactID + " was loaded into StatusSelector with a status of dead or sick/iso.");
                        break;
                    case ContactDailyStatus.SeenSickAndIsoNotFilledOut:
                        chkbx1.IsChecked = false;
                        chkbx2.IsChecked = true;
                        chkbx3.IsChecked = false;
                        chkbx4.IsChecked = false;
                        chkbxN.IsChecked = false;

                        chkbx5.IsChecked = false;
                        chkbx6.IsChecked = false;
                        chkbx7.IsChecked = true;
                        break;
                    case ContactDailyStatus.SeenSickAndNotIsolated:
                        chkbx1.IsChecked = false;
                        chkbx2.IsChecked = true;
                        chkbx3.IsChecked = false;
                        chkbx4.IsChecked = false;
                        chkbxN.IsChecked = false;

                        chkbx5.IsChecked = false;
                        chkbx6.IsChecked = true;
                        chkbx7.IsChecked = false;
                        break;
                    case ContactDailyStatus.NotSeen:
                        chkbx1.IsChecked = false;
                        chkbx2.IsChecked = false;
                        chkbx3.IsChecked = true;
                        chkbx4.IsChecked = false;
                        chkbxN.IsChecked = false;

                        chkbx5.IsChecked = false;
                        chkbx6.IsChecked = false;
                        chkbx7.IsChecked = false;
                        break;
                    case ContactDailyStatus.NotRecorded:
                        chkbx1.IsChecked = false;
                        chkbx2.IsChecked = false;
                        chkbx3.IsChecked = false;
                        chkbx4.IsChecked = true;
                        chkbxN.IsChecked = false;

                        chkbx5.IsChecked = false;
                        chkbx6.IsChecked = false;
                        chkbx7.IsChecked = false;
                        break;
                    default:
                        chkbx1.IsChecked = false;
                        chkbx2.IsChecked = false;
                        chkbx3.IsChecked = false;
                        chkbx4.IsChecked = false;
                        chkbxN.IsChecked = true;

                        chkbx5.IsChecked = false;
                        chkbx6.IsChecked = false;
                        chkbx7.IsChecked = false;
                        break;
                }
            }

            IsLoading = false;
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            //if (DeCheck)
            //{
            //    Reset();
            //}
            //DeCheck = true;
        }

        public void ResetIso()
        {
            DailyCheck.Status = ContactDailyStatus.SeenSickAndIsoNotFilledOut;
            chkbx1.IsChecked = false;
            chkbx2.IsChecked = true;
            chkbx3.IsChecked = false;
            chkbx4.IsChecked = false;
            chkbxN.IsChecked = false;

            chkbx5.IsChecked = false;
            chkbx6.IsChecked = false;
            chkbx7.IsChecked = true;
            Update();
        }

        private void Update()
        {
            if (IsLoading) return;

            if (StatusChanged != null)
            {
                DailyCheckStatusChangedEventArgs args = new DailyCheckStatusChangedEventArgs(this);

                DailyCheckViewModel dc = DataContext as DailyCheckViewModel;
                if (dc != null)
                {
                    StatusChanged(dc, args);

                    string friendlyStatus = "'not marked'/unknown";

                    if (dc.Status.HasValue)
                    {
                        switch (Convert.ToInt16(dc.Status.Value))
                        {
                            case 0:
                                friendlyStatus = "'seen and not sick'";
                                break;
                            case 1:
                                friendlyStatus = "'seen, sick, and isolated'";
                                break;
                            case 2:
                                friendlyStatus = "'seen and sick, not isolated'";
                                break;
                            case 3:
                                friendlyStatus = "'seen and sick, isolated not filled out'";
                                break;
                            case 4:
                                friendlyStatus = "'not seen'";
                                break;
                            case 5:
                                friendlyStatus = "'not recorded'";
                                break;
                        }
                    }

                    Epi.Logger.Log(DateTime.Now + ":  " +
                    System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString() + ": " +
                    "Contact " + dc.ContactID + " was updated in the daily or previous follow-up list on day " + dc.Day.ToString() + " to status: " + friendlyStatus);
                }
            }
        }

        private void chkbx1_Checked(object sender, RoutedEventArgs e)
        {
            DailyCheck.Status = ContactDailyStatus.SeenNotSick;
            chkbx1.IsChecked = true;
            chkbx2.IsChecked = false;
            chkbx3.IsChecked = false;
            chkbx4.IsChecked = false;
            chkbxN.IsChecked = false;

            chkbx5.IsChecked = false;
            chkbx6.IsChecked = false;
            chkbx7.IsChecked = false;
            Update();
        }

        private void chkbx2_Checked(object sender, RoutedEventArgs e)
        {
            DailyCheck.Status = ContactDailyStatus.SeenSickAndIsoNotFilledOut;
            chkbx1.IsChecked = false;
            chkbx2.IsChecked = true;
            chkbx3.IsChecked = false;
            chkbx4.IsChecked = false;
            chkbxN.IsChecked = false;

            chkbx5.IsChecked = false;
            chkbx6.IsChecked = false;
            chkbx7.IsChecked = true;
            Update();
        }

        private void chkbx3_Checked(object sender, RoutedEventArgs e)
        {
            DailyCheck.Status = ContactDailyStatus.NotSeen;
            chkbx1.IsChecked = false;
            chkbx2.IsChecked = false;
            chkbx3.IsChecked = true;
            chkbx4.IsChecked = false;
            chkbxN.IsChecked = false;

            chkbx5.IsChecked = false;
            chkbx6.IsChecked = false;
            chkbx7.IsChecked = false;
            Update();
        }

        private void chkbx4_Checked(object sender, RoutedEventArgs e)
        {
            DailyCheck.Status = ContactDailyStatus.NotRecorded;
            chkbx1.IsChecked = false;
            chkbx2.IsChecked = false;
            chkbx3.IsChecked = false;
            chkbx4.IsChecked = true;
            chkbxN.IsChecked = false;

            chkbx5.IsChecked = false;
            chkbx6.IsChecked = false;
            chkbx7.IsChecked = false;
            Update();
        }

        private void chkbxN_Checked(object sender, RoutedEventArgs e)
        {
            DailyCheck.Status = null;
            chkbx1.IsChecked = false;
            chkbx2.IsChecked = false;
            chkbx3.IsChecked = false;
            chkbx4.IsChecked = false;
            chkbxN.IsChecked = true;

            chkbx5.IsChecked = false;
            chkbx6.IsChecked = false;
            chkbx7.IsChecked = false;
            Update();
        }

        private void chkbx1_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox cbox = sender as CheckBox;
            if (cbox != null)
            {
                if (IsThisCheckBoxTheOnlyOneCheckedInMain(cbox))
                {
                    DailyCheck.Status = null;
                    Update();
                }
            }
        }

        private void chkbx2_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox cbox = sender as CheckBox;
            if (cbox != null)
            {
                if (IsThisCheckBoxTheOnlyOneCheckedInMain(cbox))
                {
                    DailyCheck.Status = null;
                    Update();
                }
            }
        }

        private void chkbx3_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox cbox = sender as CheckBox;
            if (cbox != null)
            {
                if (IsThisCheckBoxTheOnlyOneCheckedInMain(cbox))
                {
                    DailyCheck.Status = null;
                    Update();
                }
            }
        }

        private void chkbx4_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox cbox = sender as CheckBox;
            if (cbox != null)
            {
                if (IsThisCheckBoxTheOnlyOneCheckedInMain(cbox))
                {
                    DailyCheck.Status = null;
                    Update();
                }
            }
        }

        private void chkbxN_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox cbox = sender as CheckBox;
            if (cbox != null)
            {
                if (IsThisCheckBoxTheOnlyOneCheckedInMain(cbox))
                {
                    DailyCheck.Status = null;
                    Update();
                }
            }
        }

        private void chkbx5_Checked(object sender, RoutedEventArgs e)
        {
            DailyCheck.Status = ContactDailyStatus.SeenSickAndIsolated;
            chkbx1.IsChecked = false;
            chkbx2.IsChecked = true;
            chkbx3.IsChecked = false;
            chkbx4.IsChecked = false;
            chkbxN.IsChecked = false;

            chkbx5.IsChecked = true;
            chkbx6.IsChecked = false;
            chkbx7.IsChecked = false;
            Update();
        }

        private void chkbx6_Checked(object sender, RoutedEventArgs e)
        {
            DailyCheck.Status = ContactDailyStatus.SeenSickAndNotIsolated;
            chkbx1.IsChecked = false;
            chkbx2.IsChecked = true;
            chkbx3.IsChecked = false;
            chkbx4.IsChecked = false;
            chkbxN.IsChecked = false;

            chkbx5.IsChecked = false;
            chkbx6.IsChecked = true;
            chkbx7.IsChecked = false;
            Update();
        }

        private void chkbx7_Checked(object sender, RoutedEventArgs e)
        {
            DailyCheck.Status = ContactDailyStatus.SeenSickAndIsoNotFilledOut;
            chkbx1.IsChecked = false;
            chkbx2.IsChecked = true;
            chkbx3.IsChecked = false;
            chkbx4.IsChecked = false;
            chkbxN.IsChecked = false;

            chkbx5.IsChecked = false;
            chkbx6.IsChecked = false;
            chkbx7.IsChecked = true;
            Update();
        }

        private void chkbx5_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox cbox = sender as CheckBox;
            if (cbox != null)
            {
                if (IsThisCheckBoxTheOnlyOneCheckedInMain(cbox))
                {
                    DailyCheck.Status = ContactDailyStatus.SeenSickAndIsoNotFilledOut;
                    Update();
                }
            }
        }

        private void chkbx6_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox cbox = sender as CheckBox;
            if (cbox != null)
            {
                if (IsThisCheckBoxTheOnlyOneCheckedInMain(cbox))
                {
                    DailyCheck.Status = ContactDailyStatus.SeenSickAndIsoNotFilledOut;
                    Update();
                }
            }
        }

        private void chkbx7_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox cbox = sender as CheckBox;
            if (cbox != null)
            {
                if (IsThisCheckBoxTheOnlyOneCheckedInMain(cbox))
                {
                    DailyCheck.Status = ContactDailyStatus.SeenSickAndIsoNotFilledOut;
                    Update();
                }
            }
        }

        private bool IsThisCheckBoxTheOnlyOneCheckedInMain(CheckBox checkbox)
        {
            bool anyOthersChecked = false;

            foreach (UIElement element in panelSelectors.Children)
            {
                if (element != null && element is CheckBox)
                {
                    CheckBox c = element as CheckBox;
                    if (c != null && c == checkbox)
                    {
                        continue;
                    }

                    if (c.IsChecked == true)
                    {
                        anyOthersChecked = true;
                        break;
                    }
                }
            }
            return !anyOthersChecked;
        }

        private bool IsThisCheckBoxTheOnlyOneCheckedInIao(CheckBox checkbox)
        {
            bool anyOthersChecked = false;

            foreach (UIElement element in panelYN.Children)
            {
                if (element != null && element is CheckBox)
                {
                    CheckBox c = element as CheckBox;
                    if (c != null && c == checkbox)
                    {
                        continue;
                    }

                    if (c.IsChecked == true)
                    {
                        anyOthersChecked = true;
                        break;
                    }
                }
            }
            return !anyOthersChecked;
        }
    }
}
