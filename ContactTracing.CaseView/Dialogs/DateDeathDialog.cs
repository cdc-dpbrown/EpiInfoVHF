using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ContactTracing.CaseView.Dialogs
{
    public partial class DateDeathDialog : Form
    {        
        public DateDeathDialog()
        {
            InitializeComponent();
        }

        public DateDeathDialog(System.Globalization.CultureInfo culture)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
            InitializeComponent();
        }

        public DateTime SelectedDate
        {
            get
            {
                return dtpMain.Value;
            }
            set
            {
                dtpMain.Value = value;
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
        }
    }
}
