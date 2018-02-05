using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Epi.Data;

namespace ContactTracing.CaseView.Controls.Diagnostics
{
    /// <summary>
    /// Interaction logic for DistrictNameEditor.xaml
    /// </summary>
    public partial class DistrictNameEditor : UserControl
    {
        public event EventHandler Closed;
        public bool isOK;

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            if (this.Closed != null)
            {
                this.isOK = false;
                Closed(this, new EventArgs());
            }
        }

        private IDbDriver Database { get; set; }
        private EpiDataHelper DataHelper { get; set; }

        public DistrictNameEditor(EpiDataHelper dataHelper)
        {
            InitializeComponent();

            isOK = false;

            Database = dataHelper.Database;
            DataHelper = dataHelper;

            Query selectQuery = Database.CreateQuery("SELECT * FROM [codeDistrictSubCountyList] ORDER BY DISTRICT, SUBCOUNTIES");
            DataTable dt = Database.Select(selectQuery);
            dt.Columns[0].ColumnName = DataHelper.Adm1.ToUpper();
            dt.Columns[1].ColumnName = DataHelper.Adm2.ToUpper();
            dg.ItemsSource = (dt).DefaultView;
        }

        private void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (dg.ItemsSource != null)
            {
                DataView dv = dg.ItemsSource as DataView;
                if (dv != null && dv.Table != null)
                {
                    DataTable dt = dv.Table;
                    if (dt != null && Database != null)
                    {
                        try
                        {
                            DataHelper.SendMessageForAwaitAll();
                            string querySyntax = "DELETE * FROM [codeDistrictSubCountyList]";
                            if (Database.ToString().ToLower().Contains("sql"))
                            {
                                querySyntax = "DELETE FROM [codeDistrictSubCountyList]";
                            }

                            Query deleteQuery = Database.CreateQuery(querySyntax);
                            Database.ExecuteNonQuery(deleteQuery);

                            string col1 = dt.Columns[0].ColumnName;
                            string col2 = dt.Columns[1].ColumnName;
                            DataRow[] rows = dt.Select(String.Empty, col1 + ", " + col2, DataViewRowState.CurrentRows);

                            foreach (DataRow row in rows)
                            {
                                Query insertQuery = Database.CreateQuery("INSERT INTO [codeDistrictSubCountyList] (DISTRICT, SUBCOUNTIES, DISTCODE) VALUES (" +
                                    "@DISTRICT, @SUBCOUNTIES, @DISTCODE)");
                                insertQuery.Parameters.Add(new QueryParameter("@DISTRICT", DbType.String, row[0].ToString()));
                                insertQuery.Parameters.Add(new QueryParameter("@SUBCOUNTIES", DbType.String, row[1].ToString()));
                                insertQuery.Parameters.Add(new QueryParameter("@DISTCODE", DbType.String, row[2].ToString()));
                                Database.ExecuteNonQuery(insertQuery);
                            }

                            MessageBox.Show("Changes were committed to the database successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                            if (this.Closed != null)
                            {
                                isOK = true;
                                Closed(this, new EventArgs());
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Changes were not committed to the database successfully. Error: " + ex.Message, "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        finally
                        {
                            DataHelper.SendMessageForUnAwaitAll();
                        }
                    }
                }
            }
        }
    }
}
