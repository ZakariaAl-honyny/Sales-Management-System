namespace SalesSystem.Desktop.Forms
{
    partial class SupplierPaymentDialog
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblSupplier = new System.Windows.Forms.Label();
            this.cmbSupplier = new System.Windows.Forms.ComboBox();
            this.lblAmount = new System.Windows.Forms.Label();
            this.numAmount = new System.Windows.Forms.NumericUpDown();
            this.lblMethod = new System.Windows.Forms.Label();
            this.cmbMethod = new System.Windows.Forms.ComboBox();
            this.lblDate = new System.Windows.Forms.Label();
            this.dtpDate = new System.Windows.Forms.DateTimePicker();
            this.lblNotes = new System.Windows.Forms.Label();
            this.txtNotes = new System.Windows.Forms.TextBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.numAmount)).BeginInit();
            this.SuspendLayout();

            lblSupplier.Text = "المورد:"; lblSupplier.Location = new Point(20, 20);
            cmbSupplier.Location = new Point(120, 17); cmbSupplier.Size = new Size(240, 27); cmbSupplier.DropDownStyle = ComboBoxStyle.DropDownList;

            lblAmount.Text = "المبلغ:"; lblAmount.Location = new Point(20, 60);
            numAmount.Location = new Point(120, 57); numAmount.Size = new Size(120, 27); numAmount.Maximum = 1000000; numAmount.DecimalPlaces = 2;

            lblMethod.Text = "طريقة الدفع:"; lblMethod.Location = new Point(20, 100);
            cmbMethod.Location = new Point(120, 97); cmbMethod.Size = new Size(120, 27); cmbMethod.DropDownStyle = ComboBoxStyle.DropDownList;

            lblDate.Text = "التاريخ:"; lblDate.Location = new Point(20, 140);
            dtpDate.Location = new Point(120, 137); dtpDate.Size = new Size(120, 27);

            lblNotes.Text = "ملاحظات:"; lblNotes.Location = new Point(20, 180);
            txtNotes.Location = new Point(120, 177); txtNotes.Size = new Size(240, 60); txtNotes.Multiline = true;

            btnSave.Text = "حفظ"; btnSave.Location = new Point(120, 260); btnSave.Size = new Size(100, 35); btnSave.BackColor = Color.FromArgb(46, 204, 113); btnSave.ForeColor = Color.White; btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.Click += new EventHandler(this.btnSave_Click);

            btnCancel.Text = "إلغاء"; btnCancel.Location = new Point(230, 260); btnCancel.Size = new Size(100, 35); btnCancel.Click += (s,e) => this.Close();

            this.ClientSize = new System.Drawing.Size(400, 320);
            this.Controls.AddRange(new Control[] { lblSupplier, cmbSupplier, lblAmount, numAmount, lblMethod, cmbMethod, lblDate, dtpDate, lblNotes, txtNotes, btnSave, btnCancel });
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "سند صرف لمورد";
            ((System.ComponentModel.ISupportInitialize)(this.numAmount)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private Label lblSupplier, lblAmount, lblMethod, lblDate, lblNotes;
        private ComboBox cmbSupplier, cmbMethod;
        private NumericUpDown numAmount;
        private DateTimePicker dtpDate;
        private TextBox txtNotes;
        private Button btnSave, btnCancel;
    }
}
