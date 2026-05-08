namespace SalesSystem.Desktop.Forms
{
    partial class CustomerDialog
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblName = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.lblPhone = new System.Windows.Forms.Label();
            this.txtPhone = new System.Windows.Forms.TextBox();
            this.lblEmail = new System.Windows.Forms.Label();
            this.txtEmail = new System.Windows.Forms.TextBox();
            this.lblAddress = new System.Windows.Forms.Label();
            this.txtAddress = new System.Windows.Forms.TextBox();
            this.lblOpeningBalance = new System.Windows.Forms.Label();
            this.txtOpeningBalance = new SalesSystem.Desktop.Controls.Common.MoneyTextBox();
            this.chkIsActive = new System.Windows.Forms.CheckBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            
            int labelX = 20, inputX = 130, y = 20, spacing = 40;

            this.lblName.Text = "اسم العميل:";
            this.lblName.Location = new System.Drawing.Point(labelX, y);
            this.txtName.Location = new System.Drawing.Point(inputX, y);
            this.txtName.Size = new System.Drawing.Size(250, 27);
            
            y += spacing;
            this.lblPhone.Text = "رقم الهاتف:";
            this.lblPhone.Location = new System.Drawing.Point(labelX, y);
            this.txtPhone.Location = new System.Drawing.Point(inputX, y);
            this.txtPhone.Size = new System.Drawing.Size(250, 27);

            y += spacing;
            this.lblEmail.Text = "البريد الإلكتروني:";
            this.lblEmail.Location = new System.Drawing.Point(labelX, y);
            this.txtEmail.Location = new System.Drawing.Point(inputX, y);
            this.txtEmail.Size = new System.Drawing.Size(250, 27);

            y += spacing;
            this.lblAddress.Text = "العنوان:";
            this.lblAddress.Location = new System.Drawing.Point(labelX, y);
            this.txtAddress.Location = new System.Drawing.Point(inputX, y);
            this.txtAddress.Size = new System.Drawing.Size(250, 27);

            y += spacing;
            this.lblOpeningBalance.Text = "الرصيد الافتتاحي:";
            this.lblOpeningBalance.Location = new System.Drawing.Point(labelX, y);
            this.txtOpeningBalance.Location = new System.Drawing.Point(inputX, y);
            this.txtOpeningBalance.Size = new System.Drawing.Size(100, 27);

            this.chkIsActive.Text = "نشط";
            this.chkIsActive.Location = new System.Drawing.Point(210, y);

            y += 60;
            this.btnSave.Text = "حفظ";
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(46, 204, 113);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Location = new System.Drawing.Point(280, y);
            this.btnSave.Size = new System.Drawing.Size(100, 35);
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);

            this.btnCancel.Text = "إلغاء";
            this.btnCancel.BackColor = System.Drawing.Color.FromArgb(149, 165, 166);
            this.btnCancel.ForeColor = System.Drawing.Color.White;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Location = new System.Drawing.Point(170, y);
            this.btnCancel.Size = new System.Drawing.Size(100, 35);
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

            this.ClientSize = new System.Drawing.Size(420, y + 60);
            this.Controls.AddRange(new System.Windows.Forms.Control[] {
                lblName, txtName, lblPhone, txtPhone, lblEmail, txtEmail,
                lblAddress, txtAddress, lblOpeningBalance, txtOpeningBalance,
                chkIsActive, btnSave, btnCancel
            });
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CustomerDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblName, lblPhone, lblEmail, lblAddress, lblOpeningBalance;
        private System.Windows.Forms.TextBox txtName, txtPhone, txtEmail, txtAddress;
        private SalesSystem.Desktop.Controls.Common.MoneyTextBox txtOpeningBalance;
        private System.Windows.Forms.CheckBox chkIsActive;
        private System.Windows.Forms.Button btnSave, btnCancel;
    }
}
