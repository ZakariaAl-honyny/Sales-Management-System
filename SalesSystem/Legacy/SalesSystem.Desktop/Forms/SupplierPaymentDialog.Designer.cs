namespace SalesSystem.Desktop.Forms
{
    partial class SupplierPaymentDialog
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
            tableLayoutPanel1 = new TableLayoutPanel();
            lblSupplier = new Label();
            cmbSupplier = new ComboBox();
            lblBalance = new Label();
            lblBalanceVal = new Label();
            lblInvoice = new Label();
            cmbInvoice = new ComboBox();
            lblAmount = new Label();
            numAmount = new NumericUpDown();
            lblDate = new Label();
            dtpDate = new DateTimePicker();
            lblNotes = new Label();
            txtNotes = new TextBox();
            pnlButtons = new FlowLayoutPanel();
            btnCancel = new Button();
            btnSave = new Button();
            tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numAmount).BeginInit();
            pnlButtons.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(lblSupplier, 0, 0);
            tableLayoutPanel1.Controls.Add(cmbSupplier, 1, 0);
            tableLayoutPanel1.Controls.Add(lblBalance, 0, 1);
            tableLayoutPanel1.Controls.Add(lblBalanceVal, 1, 1);
            tableLayoutPanel1.Controls.Add(lblInvoice, 0, 2);
            tableLayoutPanel1.Controls.Add(cmbInvoice, 1, 2);
            tableLayoutPanel1.Controls.Add(lblAmount, 0, 3);
            tableLayoutPanel1.Controls.Add(numAmount, 1, 3);
            tableLayoutPanel1.Controls.Add(lblDate, 0, 4);
            tableLayoutPanel1.Controls.Add(dtpDate, 1, 4);
            tableLayoutPanel1.Controls.Add(lblNotes, 0, 5);
            tableLayoutPanel1.Controls.Add(txtNotes, 1, 5);
            tableLayoutPanel1.Dock = DockStyle.Top;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Margin = new Padding(4, 4, 4, 4);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.Padding = new Padding(12, 12, 12, 12);
            tableLayoutPanel1.RowCount = 6;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.Size = new Size(625, 438);
            tableLayoutPanel1.TabIndex = 1;
            // 
            // lblSupplier
            // 
            lblSupplier.Location = new Point(484, 12);
            lblSupplier.Margin = new Padding(4, 0, 4, 0);
            lblSupplier.Name = "lblSupplier";
            lblSupplier.Size = new Size(125, 20);
            lblSupplier.TabIndex = 0;
            lblSupplier.Text = "«·„Ê—œ:";
            // 
            // cmbSupplier
            // 
            cmbSupplier.Dock = DockStyle.Fill;
            cmbSupplier.Location = new Point(16, 16);
            cmbSupplier.Margin = new Padding(4, 4, 4, 4);
            cmbSupplier.Name = "cmbSupplier";
            cmbSupplier.Size = new Size(443, 33);
            cmbSupplier.TabIndex = 1;
            // 
            // lblBalance
            // 
            lblBalance.Location = new Point(484, 32);
            lblBalance.Margin = new Padding(4, 0, 4, 0);
            lblBalance.Name = "lblBalance";
            lblBalance.Size = new Size(125, 20);
            lblBalance.TabIndex = 2;
            lblBalance.Text = "«·—’Ìœ «·Õ«·Ì:";
            // 
            // lblBalanceVal
            // 
            lblBalanceVal.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblBalanceVal.ForeColor = Color.Red;
            lblBalanceVal.Location = new Point(391, 32);
            lblBalanceVal.Margin = new Padding(4, 0, 4, 0);
            lblBalanceVal.Name = "lblBalanceVal";
            lblBalanceVal.Size = new Size(68, 20);
            lblBalanceVal.TabIndex = 3;
            lblBalanceVal.Text = "0.00";
            // 
            // lblInvoice
            // 
            lblInvoice.Location = new Point(484, 52);
            lblInvoice.Margin = new Padding(4, 0, 4, 0);
            lblInvoice.Name = "lblInvoice";
            lblInvoice.Size = new Size(125, 20);
            lblInvoice.TabIndex = 4;
            lblInvoice.Text = "—»ÿ »›« Ê—…:";
            // 
            // cmbInvoice
            // 
            cmbInvoice.Dock = DockStyle.Fill;
            cmbInvoice.Location = new Point(16, 56);
            cmbInvoice.Margin = new Padding(4, 4, 4, 4);
            cmbInvoice.Name = "cmbInvoice";
            cmbInvoice.Size = new Size(443, 33);
            cmbInvoice.TabIndex = 5;
            // 
            // lblAmount
            // 
            lblAmount.Location = new Point(484, 72);
            lblAmount.Margin = new Padding(4, 0, 4, 0);
            lblAmount.Name = "lblAmount";
            lblAmount.Size = new Size(125, 20);
            lblAmount.TabIndex = 6;
            lblAmount.Text = "«·„»·€ «·„œ›Ê⁄:";
            // 
            // numAmount
            // 
            numAmount.DecimalPlaces = 2;
            numAmount.Dock = DockStyle.Fill;
            numAmount.Location = new Point(16, 76);
            numAmount.Margin = new Padding(4, 4, 4, 4);
            numAmount.Maximum = new decimal(new int[] { 10000000, 0, 0, 0 });
            numAmount.Name = "numAmount";
            numAmount.Size = new Size(443, 31);
            numAmount.TabIndex = 7;
            // 
            // lblDate
            // 
            lblDate.Location = new Point(484, 92);
            lblDate.Margin = new Padding(4, 0, 4, 0);
            lblDate.Name = "lblDate";
            lblDate.Size = new Size(125, 20);
            lblDate.TabIndex = 8;
            lblDate.Text = "«· «—ÌŒ:";
            // 
            // dtpDate
            // 
            dtpDate.Dock = DockStyle.Fill;
            dtpDate.Location = new Point(16, 96);
            dtpDate.Margin = new Padding(4, 4, 4, 4);
            dtpDate.Name = "dtpDate";
            dtpDate.Size = new Size(443, 31);
            dtpDate.TabIndex = 9;
            // 
            // lblNotes
            // 
            lblNotes.Location = new Point(484, 112);
            lblNotes.Margin = new Padding(4, 0, 4, 0);
            lblNotes.Name = "lblNotes";
            lblNotes.Size = new Size(125, 29);
            lblNotes.TabIndex = 10;
            lblNotes.Text = "„·«ÕŸ« :";
            // 
            // txtNotes
            // 
            txtNotes.Dock = DockStyle.Fill;
            txtNotes.Location = new Point(16, 116);
            txtNotes.Margin = new Padding(4, 4, 4, 4);
            txtNotes.Multiline = true;
            txtNotes.Name = "txtNotes";
            txtNotes.Size = new Size(443, 306);
            txtNotes.TabIndex = 11;
            // 
            // pnlButtons
            // 
            pnlButtons.Controls.Add(btnCancel);
            pnlButtons.Controls.Add(btnSave);
            pnlButtons.Dock = DockStyle.Bottom;
            pnlButtons.FlowDirection = FlowDirection.RightToLeft;
            pnlButtons.Location = new Point(0, 463);
            pnlButtons.Margin = new Padding(4, 4, 4, 4);
            pnlButtons.Name = "pnlButtons";
            pnlButtons.Padding = new Padding(12, 0, 12, 0);
            pnlButtons.Size = new Size(625, 62);
            pnlButtons.TabIndex = 0;
            // 
            // btnCancel
            // 
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.Location = new Point(16, 4);
            btnCancel.Margin = new Padding(4, 4, 4, 4);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(100, 44);
            btnCancel.TabIndex = 0;
            btnCancel.Text = "≈·€«¡";
            // 
            // btnSave
            // 
            btnSave.BackColor = Color.FromArgb(231, 76, 60);
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.ForeColor = Color.White;
            btnSave.Location = new Point(124, 4);
            btnSave.Margin = new Padding(4, 4, 4, 4);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(125, 44);
            btnSave.TabIndex = 1;
            btnSave.Text = "’—› «·”‰œ";
            btnSave.UseVisualStyleBackColor = false;
            // 
            // SupplierPaymentDialog
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(625, 525);
            Controls.Add(pnlButtons);
            Controls.Add(tableLayoutPanel1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(4, 4, 4, 4);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SupplierPaymentDialog";
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            StartPosition = FormStartPosition.CenterParent;
            Text = "”‰œ ’—› „Ê—œ";
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numAmount).EndInit();
            pnlButtons.ResumeLayout(false);
            ResumeLayout(false);
        }

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label lblSupplier, lblBalance, lblBalanceVal, lblInvoice, lblAmount, lblDate, lblNotes;
        private System.Windows.Forms.ComboBox cmbSupplier, cmbInvoice;
        private System.Windows.Forms.NumericUpDown numAmount;
        private System.Windows.Forms.DateTimePicker dtpDate;
        private System.Windows.Forms.TextBox txtNotes;
        private System.Windows.Forms.FlowLayoutPanel pnlButtons;
        private System.Windows.Forms.Button btnSave, btnCancel;
    }
}
