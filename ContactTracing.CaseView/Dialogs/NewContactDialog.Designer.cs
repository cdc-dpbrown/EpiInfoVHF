namespace ContactTracing.CaseView.Dialogs
{
    partial class NewContactDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewContactDialog));
            this.txtRelationship = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.dtpContactDate = new System.Windows.Forms.DateTimePicker();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.cbxCon1 = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.cbxCon4 = new System.Windows.Forms.CheckBox();
            this.cbxCon3 = new System.Windows.Forms.CheckBox();
            this.cbxCon2 = new System.Windows.Forms.CheckBox();
            this.cbxEstimated = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtRelationship
            // 
            resources.ApplyResources(this.txtRelationship, "txtRelationship");
            this.txtRelationship.Name = "txtRelationship";
            this.txtRelationship.TextChanged += new System.EventHandler(this.txtRelationship_TextChanged);
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // dtpContactDate
            // 
            resources.ApplyResources(this.dtpContactDate, "dtpContactDate");
            this.dtpContactDate.Name = "dtpContactDate";
            // 
            // btnOK
            // 
            resources.ApplyResources(this.btnOK, "btnOK");
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Name = "btnOK";
            this.btnOK.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            resources.ApplyResources(this.btnCancel, "btnCancel");
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // cbxCon1
            // 
            resources.ApplyResources(this.cbxCon1, "cbxCon1");
            this.cbxCon1.Name = "cbxCon1";
            this.cbxCon1.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.cbxCon4);
            this.groupBox1.Controls.Add(this.cbxCon3);
            this.groupBox1.Controls.Add(this.cbxCon2);
            this.groupBox1.Controls.Add(this.cbxCon1);
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // cbxCon4
            // 
            resources.ApplyResources(this.cbxCon4, "cbxCon4");
            this.cbxCon4.Name = "cbxCon4";
            this.cbxCon4.UseVisualStyleBackColor = true;
            // 
            // cbxCon3
            // 
            resources.ApplyResources(this.cbxCon3, "cbxCon3");
            this.cbxCon3.Name = "cbxCon3";
            this.cbxCon3.UseVisualStyleBackColor = true;
            // 
            // cbxCon2
            // 
            resources.ApplyResources(this.cbxCon2, "cbxCon2");
            this.cbxCon2.Name = "cbxCon2";
            this.cbxCon2.UseVisualStyleBackColor = true;
            // 
            // cbxEstimated
            // 
            resources.ApplyResources(this.cbxEstimated, "cbxEstimated");
            this.cbxEstimated.Name = "cbxEstimated";
            this.cbxEstimated.UseVisualStyleBackColor = true;
            // 
            // NewContactDialog
            // 
            this.AcceptButton = this.btnOK;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.Controls.Add(this.cbxEstimated);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.txtRelationship);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.dtpContactDate);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "NewContactDialog";
            this.ShowIcon = false;
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtRelationship;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DateTimePicker dtpContactDate;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.CheckBox cbxCon1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox cbxCon4;
        private System.Windows.Forms.CheckBox cbxCon3;
        private System.Windows.Forms.CheckBox cbxCon2;
        private System.Windows.Forms.CheckBox cbxEstimated;
    }
}