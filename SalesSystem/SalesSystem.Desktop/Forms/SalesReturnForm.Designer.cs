namespace SalesSystem.Desktop.Forms
{
    partial class SalesReturnForm
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
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.cmbCustomer = new System.Windows.Forms.ComboBox();
            this.lblCustomer = new System.Windows.Forms.Label();
            this.cmbWarehouse = new System.Windows.Forms.ComboBox();
            this.lblWarehouse = new System.Windows.Forms.Label();
            this.dtpDate = new System.Windows.Forms.DateTimePicker();
            this.lblDate = new System.Windows.Forms.Label();
            this.dgvItems = new System.Windows.Forms.DataGridView();
            this.pnlFooter = new System.Windows.Forms.Panel();
            this.txtNotes = new System.Windows.Forms.TextBox();
            this.lblNotes = new System.Windows.Forms.Label();
            this.lblTotal = new System.Windows.Forms.Label();
            this.lblTotalValue = new System.Windows.Forms.Label();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            
            this.pnlHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvItems)).BeginInit();
            this.pnlFooter.SuspendLayout();
            this.SuspendLayout();

            this.pnlHeader.Dock = DockStyle.Top;
            this.pnlHeader.Height = 80;
            this.pnlHeader.Controls.AddRange(new Control[] { cmbCustomer, lblCustomer, cmbWarehouse, lblWarehouse, dtpDate, lblDate });
            
            lblCustomer.Text = "العميل:"; lblCustomer.Location = new Point(20, 20);
            cmbCustomer.Location = new Point(100, 17); cmbCustomer.Size = new Size(200, 27); cmbCustomer.DropDownStyle = ComboBoxStyle.DropDownList;
            
            lblWarehouse.Text = "المستودع:"; lblWarehouse.Location = new Point(320, 20);
            cmbWarehouse.Location = new Point(400, 17); cmbWarehouse.Size = new Size(150, 27); cmbWarehouse.DropDownStyle = ComboBoxStyle.DropDownList;
            
            lblDate.Text = "التاريخ:"; lblDate.Location = new Point(570, 20);
            dtpDate.Location = new Point(630, 17); dtpDate.Size = new Size(130, 27);

            this.dgvItems.Dock = DockStyle.Fill;
            this.dgvItems.BackgroundColor = Color.White;

            this.pnlFooter.Dock = DockStyle.Bottom;
            this.pnlFooter.Height = 100;
            this.pnlFooter.Controls.AddRange(new Control[] { txtNotes, lblNotes, lblTotal, lblTotalValue, btnSave, btnClose });
            
            lblNotes.Text = "ملاحظات:"; lblNotes.Location = new Point(20, 20);
            txtNotes.Location = new Point(100, 17); txtNotes.Size = new Size(400, 60); txtNotes.Multiline = true;
            
            lblTotal.Text = "إجمالي المرتجع:"; lblTotal.Location = new Point(520, 20); lblTotal.Font = new Font(this.Font, FontStyle.Bold);
            lblTotalValue.Text = "0.00"; lblTotalValue.Location = new Point(650, 20); lblTotalValue.Font = new Font(this.Font, FontStyle.Bold);
            
            btnSave.Text = "حفظ المرتجع"; btnSave.Location = new Point(520, 50); btnSave.Size = new Size(120, 35); btnSave.BackColor = Color.FromArgb(231, 76, 60); btnSave.ForeColor = Color.White; btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.Click += new EventHandler(this.btnSave_Click);
            
            btnClose.Text = "إغلاق"; btnClose.Location = new Point(780, 50); btnClose.Size = new Size(100, 35); btnClose.Click += (s,e) => this.Close();

            this.ClientSize = new System.Drawing.Size(900, 600);
            this.Controls.Add(this.dgvItems);
            this.Controls.Add(this.pnlHeader);
            this.Controls.Add(this.pnlFooter);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "مرتجع مبيعات";
            
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvItems)).EndInit();
            this.pnlFooter.ResumeLayout(false);
            this.pnlFooter.PerformLayout();
            this.ResumeLayout(false);
        }

        private Panel pnlHeader, pnlFooter;
        private ComboBox cmbCustomer, cmbWarehouse;
        private Label lblCustomer, lblWarehouse, lblDate, lblNotes, lblTotal, lblTotalValue;
        private DateTimePicker dtpDate;
        private DataGridView dgvItems;
        private TextBox txtNotes;
        private Button btnSave, btnClose;
    }
}
