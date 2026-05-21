namespace SalesSystem.Desktop.Forms
{
    partial class CustomerPaymentDialog
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.lblCustomer = new System.Windows.Forms.Label();
            this.cmbCustomer = new System.Windows.Forms.ComboBox();
            this.lblBalance = new System.Windows.Forms.Label();
            this.lblBalanceVal = new System.Windows.Forms.Label();
            this.lblInvoice = new System.Windows.Forms.Label();
            this.cmbInvoice = new System.Windows.Forms.ComboBox();
            this.lblAmount = new System.Windows.Forms.Label();
            this.numAmount = new System.Windows.Forms.NumericUpDown();
            this.lblDate = new System.Windows.Forms.Label();
            this.dtpDate = new System.Windows.Forms.DateTimePicker();
            this.lblNotes = new System.Windows.Forms.Label();
            this.txtNotes = new System.Windows.Forms.TextBox();
            this.pnlButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            
            ((System.ComponentModel.ISupportInitialize)(this.numAmount)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            this.pnlButtons.SuspendLayout();
            this.SuspendLayout();

            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.lblCustomer, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.cmbCustomer, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.lblBalance, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.lblBalanceVal, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.lblInvoice, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.cmbInvoice, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.lblAmount, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.numAmount, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.lblDate, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.dtpDate, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.lblNotes, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this.txtNotes, 1, 5);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanel1.Height = 350;
            this.tableLayoutPanel1.Padding = new System.Windows.Forms.Padding(10);
            this.tableLayoutPanel1.RowCount = 6;

            this.lblCustomer.Text = "«·⁄„Ì·:";
            this.lblBalance.Text = "«·—’Ìœ «·Õ«·Ì:";
            this.lblBalanceVal.Text = "0.00";
            this.lblBalanceVal.ForeColor = System.Drawing.Color.Blue;
            this.lblBalanceVal.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblInvoice.Text = "—»ÿ »›« Ê—…:";
            this.lblAmount.Text = "«·„»·€ «·„œ›Ê⁄:";
            this.lblDate.Text = "«· «—ÌŒ:";
            this.lblNotes.Text = "„·«ÕŸ« :";

            this.cmbCustomer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbInvoice.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numAmount.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dtpDate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtNotes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtNotes.Multiline = true;
            this.txtNotes.Height = 100;

            this.numAmount.Maximum = 10000000;
            this.numAmount.DecimalPlaces = 2;

            this.pnlButtons.Controls.Add(this.btnCancel);
            this.pnlButtons.Controls.Add(this.btnSave);
            this.pnlButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlButtons.Height = 50;
            this.pnlButtons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.pnlButtons.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);

            this.btnSave.Text = "Õ›Ÿ «·”‰œ";
            this.btnSave.Width = 100;
            this.btnSave.Height = 35;
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(46, 204, 113);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.FlatStyle = FlatStyle.Flat;

            this.btnCancel.Text = "≈·€«¡";
            this.btnCancel.Width = 80;
            this.btnCancel.Height = 35;
            this.btnCancel.FlatStyle = FlatStyle.Flat;

            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(500, 420);
            this.Controls.Add(this.pnlButtons);
            this.Controls.Add(this.tableLayoutPanel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "”‰œ ﬁ»÷ ⁄„Ì·";

            ((System.ComponentModel.ISupportInitialize)(this.numAmount)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.pnlButtons.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label lblCustomer, lblBalance, lblBalanceVal, lblInvoice, lblAmount, lblDate, lblNotes;
        private System.Windows.Forms.ComboBox cmbCustomer, cmbInvoice;
        private System.Windows.Forms.NumericUpDown numAmount;
        private System.Windows.Forms.DateTimePicker dtpDate;
        private System.Windows.Forms.TextBox txtNotes;
        private System.Windows.Forms.FlowLayoutPanel pnlButtons;
        private System.Windows.Forms.Button btnSave, btnCancel;
    }
}
