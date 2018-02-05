using System;
using System.Collections.Generic;
using System.Data;
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
using ContactTracing.ViewModel;
using Epi.Data;

namespace ContactTracing.CaseView.Controls
{
    /// <summary>
    /// Interaction logic for LabRecordsForCase.xaml
    /// </summary>
    public partial class LabRecordsForCase : UserControl
    {
        public event EventHandler Closed;

        public LabRecordsForCase(CaseViewModel c)
        {
            InitializeComponent();
            panelCase.DataContext = c;
            panelCaseHeader.DataContext = c;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            if (this.Closed != null)
            {
                Closed(this, new EventArgs());
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            dg.MaxHeight = this.ActualHeight / 2;
        }

        private void LabResultActionsRowControl_DeleteRequested(object sender, EventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete this lab record?", "Confirm deletion", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                Dialogs.AuthCodeDialog authDialog = new Dialogs.AuthCodeDialog(ContactTracing.Core.Constants.AUTH_CODE);
                System.Windows.Forms.DialogResult authResult = authDialog.ShowDialog();
                if (authResult == System.Windows.Forms.DialogResult.OK)
                {
                    if (authDialog.IsAuthorized)
                    {
                        try
                        {
                            if (this.DataContext != null)
                            {
                                EpiDataHelper DataHelper = this.DataContext as EpiDataHelper;

                                if (DataHelper != null)
                                {
                                    LabResultViewModel r = dg.SelectedItem as LabResultViewModel;
                                    if (r != null)
                                    {
                                        string guid = r.RecordId;

                                        IDbDriver db = DataHelper.Project.CollectedData.GetDatabase();

                                        #region Soft deletion code (Unused)
                                        /* SOFT DELETE */
                                        //int rows = 0;
                                        //string querySyntax = "UPDATE [" + DataHelper.LabForm.TableName + "] SET RecStatus = 0 WHERE [GlobalRecordId] = @GlobalRecordId";
                                        //Query labUpdateQuery = db.CreateQuery(querySyntax);
                                        //labUpdateQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", System.Data.DbType.String, guid));
                                        //rows = db.ExecuteNonQuery(labUpdateQuery);

                                        //CaseViewModel c = DataHelper.GetCaseVM(r.CaseRecordGuid);

                                        //if (rows == 1)
                                        //{
                                        //    Core.DbLogger.Log(String.Format(
                                        //        "Soft-deleted lab : Case ID = {0}, Case EpiCaseDef = {1}, FLSID = {2}, Lab GUID = {3}",
                                        //            c.ID, c.EpiCaseDef, r.FieldLabSpecimenID, r.RecordId));
                                        //}
                                        //else if (rows == 0)
                                        //{
                                        //    Core.DbLogger.Log(String.Format(
                                        //        "Lab soft-deletion attempted but no lab record found in database : GUID = {0}",
                                        //            guid));
                                        //}
                                        //else if (rows > 1)
                                        //{
                                        //    Core.DbLogger.Log(String.Format(
                                        //        "Lab soft-deletion affected {0} records for GUID {1}. Duplicate labs may be present.",
                                        //            rows.ToString(), guid));
                                        //}
                                        #endregion Soft deletion code

                                        /* HARD DELETE */
                                        using (IDbTransaction transaction = db.OpenTransaction())
                                        {
                                            Query deleteQuery = db.CreateQuery("DELETE FROM " + DataHelper.LabForm.TableName + " WHERE GlobalRecordId = @GlobalRecordId");
                                            deleteQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", System.Data.DbType.String, guid));
                                            int rows = db.ExecuteNonQuery(deleteQuery, transaction);

                                            foreach (Epi.Page page in DataHelper.LabForm.Pages)
                                            {
                                                deleteQuery = db.CreateQuery("DELETE FROM " + page.TableName + " WHERE GlobalRecordId = @GlobalRecordId");
                                                deleteQuery.Parameters.Add(new QueryParameter("@GlobalRecordId", System.Data.DbType.String, guid));
                                                db.ExecuteNonQuery(deleteQuery, transaction);
                                            }

                                            try
                                            {
                                                transaction.Commit();
                                            }
                                            catch (Exception ex0)
                                            {
                                                ContactTracing.Core.DbLogger.Log("Lab record deletion failed on commit. Transaction rolled back. Exception: " + ex0.Message);

                                                try
                                                {
                                                    transaction.Rollback();
                                                }
                                                catch (Exception ex1)
                                                {
                                                    ContactTracing.Core.DbLogger.Log("Lab record deletion failed on commit rollback. Transaction rolled back. Exception: " + ex1.Message);
                                                }
                                            }

                                            CaseViewModel c = DataHelper.GetCaseVM(r.CaseRecordGuid);
                                            DataHelper.PopulateLabRecordsForCase.Execute(c);
                                            db.CloseTransaction(transaction);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(String.Format("An exception occurred while trying to delete a lab record. Message: {0}", ex.Message));//. Case ID: {0}. Please give this message to the application developer.\n{1}", caseVM.ID, ex.Message));
                            ContactTracing.Core.DbLogger.Log("Lab record deletion failed. Exception: " + ex.Message);
                        }
                    }
                }
            }
        }
    }
}
