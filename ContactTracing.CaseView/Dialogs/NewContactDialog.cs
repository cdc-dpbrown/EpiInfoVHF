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
    public partial class NewContactDialog : Form
    {
        public NewContactDialog()
        {
            InitializeComponent();
        }

        public NewContactDialog(System.Globalization.CultureInfo culture)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
            InitializeComponent();
        }

        public string Relationship
        {
            get
            {
                return txtRelationship.Text;
            }
            set
            {
                txtRelationship.Text = value;
            }
        }

        public int? ContactType
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                if (cbxCon1.Checked == true) sb.Append("1");
                else sb.Append("0");
                if (cbxCon2.Checked == true) sb.Append("1");
                else sb.Append("0");
                if (cbxCon3.Checked == true) sb.Append("1");
                else sb.Append("0");
                if (cbxCon4.Checked == true) sb.Append("1");
                else sb.Append("0");

                return Convert.ToInt32(sb.ToString(), 2);
            }
            set
            {
                string bits = String.Empty;
                if (value.HasValue)
                {
                    bits = Convert.ToString(value.Value, 2);
                }
                switch (bits.Length)
                {
                    case 0:
                        bits = "0000";
                        break;
                    case 1:
                        bits = "000" + bits;
                        break;
                    case 2:
                        bits = "00" + bits;
                        break;
                    case 3:
                        bits = "0" + bits;
                        break;
                }

                if (bits[0].Equals('1'))
                {
                    cbxCon1.Checked = true;
                }
                else
                {
                    cbxCon1.Checked = false;
                }

                if (bits[1].Equals('1'))
                {
                    cbxCon2.Checked = true;
                }
                else
                {
                    cbxCon2.Checked = false;
                }

                if (bits[2].Equals('1'))
                {
                    cbxCon3.Checked = true;
                }
                else
                {
                    cbxCon3.Checked = false;
                }

                if (bits[3].Equals('1'))
                {
                    cbxCon4.Checked = true;
                }
                else
                {
                    cbxCon4.Checked = false;
                }
            }
        }

        public DateTime ContactDate
        {
            get
            {
                return dtpContactDate.Value;
            }
            set
            {
                dtpContactDate.Value = value;                
            }
        }

        public bool IsEstimated
        {
            get
            {
                return cbxEstimated.Checked;
            }
            set
            {
                cbxEstimated.Checked = value;
            }
        }

        private void CheckForInputSufficiency()
        {
            //if (/*cmbContactType.SelectedIndex >= 0 &&*/ txtRelationship.Text.Trim().Length > 0)
            //{
            btnOK.Enabled = true;
            //}
            //else
            //{
                //btnOK.Enabled = false;
            //}
        }

        private void cmbContactType_SelectedIndexChanged(object sender, EventArgs e)
        {
            CheckForInputSufficiency();
        }

        private void txtRelationship_TextChanged(object sender, EventArgs e)
        {
            CheckForInputSufficiency();
        }
    }
}
