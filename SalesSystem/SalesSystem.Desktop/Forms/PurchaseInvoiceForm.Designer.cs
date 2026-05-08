namespace SalesSystem.Desktop.Forms
{
    partial class PurchaseInvoiceForm
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
            this.cmbSupplier = new System.Windows.Forms.ComboBox();
            this.lblSupplier = new System.Windows.Forms.Label();
            this.dtpDate = new System.Windows.Forms.DateTimePicker();
            this.lblDate = new System.Windows.Forms.Label();
            this.cmbWarehouse = new System.Windows.Forms.ComboBox();
            this.lblWarehouse = new System.Windows.Forms.Label();
            this.cmbPaymentType = new System.Windows.Forms.ComboBox();
            this.lblPaymentType = new System.Windows.Forms.Label();
            this.dgvItems = new System.Windows.Forms.DataGridView();
            this.pnlFooter = new System.Windows.Forms.Panel();
            this.txtNotes = new System.Windows.Forms.TextBox();
            this.lblNotes = new System.Windows.Forms.Label();
            this.lblSubtotal = new System.Windows.Forms.Label();
            this.lblSubtotalValue = new System.Windows.Forms.Label();
            this.lblDiscount = new System.Windows.Forms.Label();
            this.txtDiscount = new SalesSystem.Desktop.Controls.Common.MoneyTextBox();
            this.lblTax = new System.Windows.Forms.Label();
            this.txtTax = new SalesSystem.Desktop.Controls.Common.MoneyTextBox();
            this.lblTotal = new System.Windows.Forms.Label();
            this.lblTotalValue = new System.Windows.Forms.Label();
            this.lblPaid = new System.Windows.Forms.Label();
            this.txtPaid = new SalesSystem.Desktop.Controls.Common.MoneyTextBox();
            this.lblDue = new System.Windows.Forms.Label();
            this.lblDueValue = new System.Windows.Forms.Label();
            this.pnlButtons = new System.Windows.Forms.Panel();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnPost = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            
            this.pnlHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvItems)).BeginInit();
            this.pnlFooter.SuspendLayout();
            this.pnlButtons.SuspendLayout();
            this.SuspendLayout();

            this.pnlHeader.Dock = DockStyle.Top;
            this.pnlHeader.Height = 100;
            this.pnlHeader.Controls.AddRange(new Control[] { cmbSupplier, lblSupplier, dtpDate, lblDate, cmbWarehouse, lblWarehouse, cmbPaymentType, lblPaymentType });
            
            lblSupplier.Text = "المورد:"; lblSupplier.Location = new Point(20, 20);
            cmbSupplier.Location = new Point(100, 17); cmbSupplier.Size = new Size(200, 27); cmbSupplier.DropDownStyle = ComboBoxStyle.DropDownList;
            
            lblDate.Text = "التاريخ:"; lblDate.Location = new Point(320, 20);
            dtpDate.Location = new Point(400, 17); dtpDate.Size = new Size(150, 27);
            
            lblWarehouse.Text = "المستودع:"; lblWarehouse.Location = new Point(570, 20);
            cmbWarehouse.Location = new Point(650, 17); cmbWarehouse.Size = new Size(150, 27); cmbWarehouse.DropDownStyle = ComboBoxStyle.DropDownList;
            
            lblPaymentType.Text = "الدفع:"; lblPaymentType.Location = new Point(20, 60);
            cmbPaymentType.Location = new Point(100, 57); cmbPaymentType.Size = new Size(200, 27); cmbPaymentType.Items.AddRange(new object[] { "نقدي", "آجل", "مختلط" }); cmbPaymentType.DropDownStyle = ComboBoxStyle.DropDownList;

            this.dgvItems.Dock = DockStyle.Fill;
            this.dgvItems.BackgroundColor = Color.White;

            this.pnlFooter.Dock = DockStyle.Bottom;
            this.pnlFooter.Height = 150;
            this.pnlFooter.Controls.AddRange(new Control[] { txtNotes, lblNotes, lblSubtotal, lblSubtotalValue, lblDiscount, txtDiscount, lblTax, txtTax, lblTotal, lblTotalValue, lblPaid, txtPaid, lblDue, lblDueValue });
            
            lblNotes.Text = "ملاحظات:"; lblNotes.Location = new Point(20, 20);
            txtNotes.Location = new Point(100, 17); txtNotes.Size = new Size(300, 80); txtNotes.Multiline = true;
            
            int footerX = 450, footerInputX = 550, footerY = 20, footerSpacing = 35;
            
            lblSubtotal.Text = "الإجمالي الفرعي:"; lblSubtotal.Location = new Point(footerX, footerY);
            lblSubtotalValue.Location = new Point(footerInputX, footerY); lblSubtotalValue.Text = "0.00";
            
            footerY += footerSpacing;
            lblDiscount.Text = "الخصم:"; lblDiscount.Location = new Point(footerX, footerY);
            txtDiscount.Location = new Point(footerInputX, footerY-3); txtDiscount.Size = new Size(100, 27);
            txtDiscount.TextChanged += (s,e) => CalculateTotals();

            lblTax.Text = "الضريبة:"; lblTax.Location = new Point(footerInputX + 120, footerY);
            txtTax.Location = new Point(footerInputX + 180, footerY-3); txtTax.Size = new Size(100, 27);
            txtTax.TextChanged += (s,e) => CalculateTotals();

            footerY += footerSpacing;
            lblTotal.Text = "الإجمالي النهائي:"; lblTotal.Location = new Point(footerX, footerY);
            lblTotalValue.Location = new Point(footerInputX, footerY); lblTotalValue.Text = "0.00"; lblTotalValue.Font = new Font(this.Font, FontStyle.Bold);

            lblPaid.Text = "المدفوع:"; lblPaid.Location = new Point(footerInputX + 120, footerY);
            txtPaid.Location = new Point(footerInputX + 180, footerY-3); txtPaid.Size = new Size(100, 27);
            txtPaid.TextChanged += (s,e) => CalculateTotals();

            footerY += footerSpacing;
            lblDue.Text = "المتبقي:"; lblDue.Location = new Point(footerX, footerY);
            lblDueValue.Location = new Point(footerInputX, footerY); lblDueValue.Text = "0.00";

            this.pnlButtons.Dock = DockStyle.Bottom;
            this.pnlButtons.Height = 60;
            this.pnlButtons.Padding = new Padding(10);
            this.pnlButtons.Controls.AddRange(new Control[] { btnClose, btnCancel, btnPost, btnSave });
            
            btnSave.Text = "حفظ مسودة"; btnSave.Location = new Point(10, 12); btnSave.Size = new Size(100, 35); btnSave.BackColor = Color.FromArgb(52, 73, 94); btnSave.ForeColor = Color.White; btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.Click += new EventHandler(this.btnSave_Click);

            btnPost.Text = "حفظ وترحيل"; btnPost.Location = new Point(120, 12); btnPost.Size = new Size(100, 35); btnPost.BackColor = Color.FromArgb(46, 204, 113); btnPost.ForeColor = Color.White; btnPost.FlatStyle = FlatStyle.Flat;
            btnPost.Click += new EventHandler(this.btnPost_Click);

            btnCancel.Text = "إلغاء الفاتورة"; btnCancel.Location = new Point(230, 12); btnCancel.Size = new Size(100, 35); btnCancel.BackColor = Color.FromArgb(231, 76, 60); btnCancel.ForeColor = Color.White; btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.Click += new EventHandler(this.btnCancel_Click);

            btnClose.Text = "إغلاق"; btnClose.Location = new Point(780, 12); btnClose.Size = new Size(100, 35); btnClose.Click += (s,e) => this.Close();

            this.ClientSize = new System.Drawing.Size(900, 700);
            this.Controls.Add(this.dgvItems);
            this.Controls.Add(this.pnlHeader);
            this.Controls.Add(this.pnlFooter);
            this.Controls.Add(this.pnlButtons);
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.StartPosition = FormStartPosition.CenterParent;
            
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvItems)).EndInit();
            this.pnlFooter.ResumeLayout(false);
            this.pnlFooter.PerformLayout();
            this.pnlButtons.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private Panel pnlHeader, pnlFooter, pnlButtons;
        private ComboBox cmbSupplier, cmbWarehouse, cmbPaymentType;
        private Label lblSupplier, lblDate, lblWarehouse, lblPaymentType, lblNotes, lblSubtotal, lblSubtotalValue, lblDiscount, lblTax, lblTotal, lblTotalValue, lblPaid, lblDue, lblDueValue;
        private DateTimePicker dtpDate;
        private DataGridView dgvItems;
        private TextBox txtNotes;
        private SalesSystem.Desktop.Controls.Common.MoneyTextBox txtDiscount, txtTax, txtPaid;
        private Button btnSave, btnPost, btnCancel, btnClose;
    }
}
