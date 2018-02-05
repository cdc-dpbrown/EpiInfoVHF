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
    /// Interaction logic for CountryNameEditor.xaml
    /// </summary>
    public partial class CountryNameEditor : UserControl
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

        public CountryNameEditor(EpiDataHelper dataHelper)
        {
            InitializeComponent();

            Database = dataHelper.Database;
            DataHelper = dataHelper;

            IDbDriver db = DataHelper.Database; // lazy
            if (!db.TableExists("codeCountryList"))
            {
                List<Epi.Data.TableColumn> tableColumns = new List<Epi.Data.TableColumn>();
                tableColumns.Add(new Epi.Data.TableColumn("COUNTRY", GenericDbColumnType.String, 255, true, false));
                db.CreateTable("codeCountryList", tableColumns);
            }

            Query selectQuery = Database.CreateQuery("SELECT * FROM [codeCountryList] ORDER BY COUNTRY");
            bool exists = Database.TableExists("codeCountryList");
            dg.ItemsSource = (Database.Select(selectQuery)).DefaultView;
        }

        private void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (dg.ItemsSource != null)
            {
                DataView dv = dg.ItemsSource as DataView;
                if (dv != null)
                {
                    DataTable dt = dv.Table;
                    if (dt != null && Database != null)
                    {
                        try
                        {
                            DataHelper.SendMessageForAwaitAll();
                            string querySyntax = "DELETE * FROM [codeCountryList]";
                            if (Database.ToString().ToLower().Contains("sql"))
                            {
                                querySyntax = "DELETE FROM [codeCountryList]";
                            }

                            Query deleteQuery = Database.CreateQuery(querySyntax);
                            Database.ExecuteNonQuery(deleteQuery);

                            DataRow[] rows = dt.Select(String.Empty, "COUNTRY", DataViewRowState.CurrentRows);

                            foreach (DataRow row in rows)
                            {
                                Query insertQuery = Database.CreateQuery("INSERT INTO [codeCountryList] (COUNTRY) VALUES (" +
                                    "@COUNTRY)");
                                insertQuery.Parameters.Add(new QueryParameter("@COUNTRY", DbType.String, row["COUNTRY"].ToString()));
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
