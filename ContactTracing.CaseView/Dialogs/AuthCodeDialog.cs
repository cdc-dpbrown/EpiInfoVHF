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
    public partial class AuthCodeDialog : Form
    {
        public bool IsAuthorized { get; set; }
        private string AuthCode { get; set; }

        public AuthCodeDialog(string code)
        {
            InitializeComponent();
            AuthCode = code;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (txtCode.Text == AuthCode)
            {
                this.DialogResult = System.Windows.Forms.DialogResult.OK;
                IsAuthorized = true;
            }
            else
            {
                this.DialogResult = System.Windows.Forms.DialogResult.None;
                IsAuthorized = false;
            }
        }
    }
}
