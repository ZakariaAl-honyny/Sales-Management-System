namespace SalesSystem.Desktop.Forms
{
    partial class StockTransferForm
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
            this.cmbFromWarehouse = new System.Windows.Forms.ComboBox();
            this.lblFrom = new System.Windows.Forms.Label();
            this.cmbToWarehouse = new System.Windows.Forms.ComboBox();
            this.lblTo = new System.Windows.Forms.Label();
            this.dtpDate = new System.Windows.Forms.DateTimePicker();
            this.lblDate = new System.Windows.Forms.Label();
            this.dgvItems = new System.Windows.Forms.DataGridView();
            this.pnlFooter = new System.Windows.Forms.Panel();
            this.txtNotes = new System.Windows.Forms.TextBox();
            this.lblNotes = new System.Windows.Forms.Label();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            
            this.pnlHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvItems)).BeginInit();
            this.pnlFooter.SuspendLayout();
            this.SuspendLayout();

            this.pnlHeader.Dock = DockStyle.Top;
            this.pnlHeader.Height = 100;
            this.pnlHeader.Controls.AddRange(new Control[] { cmbFromWarehouse, lblFrom, cmbToWarehouse, lblTo, dtpDate, lblDate });
            
            lblFrom.Text = "من مستودع:"; lblFrom.Location = new Point(20, 20);
            cmbFromWarehouse.Location = new Point(120, 17); cmbFromWarehouse.Size = new Size(200, 27); cmbFromWarehouse.DropDownStyle = ComboBoxStyle.DropDownList;
            
            lblTo.Text = "إلى مستودع:"; lblTo.Location = new Point(350, 20);
            cmbToWarehouse.Location = new Point(450, 17); cmbToWarehouse.Size = new Size(200, 27); cmbToWarehouse.DropDownStyle = ComboBoxStyle.DropDownList;
            
            lblDate.Text = "التاريخ:"; lblDate.Location = new Point(680, 20);
            dtpDate.Location = new Point(740, 17); dtpDate.Size = new Size(130, 27);

            this.dgvItems.Dock = DockStyle.Fill;
            this.dgvItems.BackgroundColor = Color.White;

            this.pnlFooter.Dock = DockStyle.Bottom;
            this.pnlFooter.Height = 100;
            this.pnlFooter.Controls.AddRange(new Control[] { txtNotes, lblNotes, btnSave, btnClose });
            
            lblNotes.Text = "ملاحظات:"; lblNotes.Location = new Point(20, 20);
            txtNotes.Location = new Point(100, 17); txtNotes.Size = new Size(400, 60); txtNotes.Multiline = true;
            
            btnSave.Text = "تنفيذ التحويل"; btnSave.Location = new Point(550, 17); btnSave.Size = new Size(120, 40); btnSave.BackColor = Color.FromArgb(33, 150, 243); btnSave.ForeColor = Color.White; btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.Click += new EventHandler(this.btnSave_Click);
            
            btnClose.Text = "إغلاق"; btnClose.Location = new Point(780, 17); btnClose.Size = new Size(100, 40); btnClose.Click += (s,e) => this.Close();

            this.ClientSize = new System.Drawing.Size(900, 600);
            this.Controls.Add(this.dgvItems);
            this.Controls.Add(this.pnlHeader);
            this.Controls.Add(this.pnlFooter);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "تحويل مخزني";
            
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvItems)).EndInit();
            this.pnlFooter.ResumeLayout(false);
            this.pnlFooter.PerformLayout();
            this.ResumeLayout(false);
        }

        private Panel pnlHeader, pnlFooter;
        private ComboBox cmbFromWarehouse, cmbToWarehouse;
        private Label lblFrom, lblTo, lblDate, lblNotes;
        private DateTimePicker dtpDate;
        private DataGridView dgvItems;
        private TextBox txtNotes;
        private Button btnSave, btnClose;
    }
}
