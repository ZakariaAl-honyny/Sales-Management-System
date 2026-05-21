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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.lblTransferNoLabel = new System.Windows.Forms.Label();
            this.lblTransferNo = new System.Windows.Forms.Label();
            this.lblDate = new System.Windows.Forms.Label();
            this.dtpDate = new System.Windows.Forms.DateTimePicker();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblSource = new System.Windows.Forms.Label();
            this.cmbSource = new System.Windows.Forms.ComboBox();
            this.lblDest = new System.Windows.Forms.Label();
            this.cmbDest = new System.Windows.Forms.ComboBox();
            this.dgvItems = new System.Windows.Forms.DataGridView();
            this.pnlFooter = new System.Windows.Forms.Panel();
            this.txtNotes = new System.Windows.Forms.TextBox();
            this.pnlButtons = new System.Windows.Forms.Panel();
            this.btnAddItem = new System.Windows.Forms.Button();
            this.btnRemoveItem = new System.Windows.Forms.Button();
            this.btnSaveDraft = new System.Windows.Forms.Button();
            this.btnPost = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            
            this.pnlHeader.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvItems)).BeginInit();
            this.pnlFooter.SuspendLayout();
            this.pnlButtons.SuspendLayout();
            this.SuspendLayout();

            this.pnlHeader.Controls.Add(this.tableLayoutPanel1);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Height = 120;
            this.pnlHeader.Padding = new System.Windows.Forms.Padding(10);

            this.tableLayoutPanel1.ColumnCount = 6;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 34F));
            this.tableLayoutPanel1.Controls.Add(this.lblTransferNoLabel, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.lblTransferNo, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.lblDate, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.dtpDate, 3, 0);
            this.tableLayoutPanel1.Controls.Add(this.lblStatus, 5, 0);
            this.tableLayoutPanel1.Controls.Add(this.lblSource, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.cmbSource, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.lblDest, 2, 1);
            this.tableLayoutPanel1.Controls.Add(this.cmbDest, 3, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.RowCount = 2;

            this.lblTransferNoLabel.Text = "—ﬁ„ «· ÕÊÌ·:";
            this.lblTransferNo.Text = "ÃœÌœ";
            this.lblDate.Text = "«· «—ÌŒ:";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblStatus.BackColor = System.Drawing.Color.MediumPurple;
            this.lblStatus.ForeColor = System.Drawing.Color.White;
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblSource.Text = "„‰ „” Êœ⁄:";
            this.lblDest.Text = "≈·Ï „” Êœ⁄:";

            this.dgvItems.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvItems.BackgroundColor = System.Drawing.Color.White;

            this.pnlFooter.Controls.Add(this.txtNotes);
            this.pnlFooter.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlFooter.Height = 100;
            this.pnlFooter.Padding = new System.Windows.Forms.Padding(10);

            this.txtNotes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtNotes.Multiline = true;
            this.txtNotes.PlaceholderText = "„·«ÕŸ«  «· ÕÊÌ·...";

            this.pnlButtons.Controls.Add(this.btnClose);
            this.pnlButtons.Controls.Add(this.btnPost);
            this.pnlButtons.Controls.Add(this.btnSaveDraft);
            this.pnlButtons.Controls.Add(this.btnRemoveItem);
            this.pnlButtons.Controls.Add(this.btnAddItem);
            this.pnlButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlButtons.Height = 60;

            this.btnAddItem.Text = "≈÷«›… ’‰›";
            this.btnRemoveItem.Text = "Õ–› ’‰›";
            this.btnSaveDraft.Text = "Õ›Ÿ „”Êœ…";
            this.btnPost.Text = " —ÕÌ·";
            this.btnPost.BackColor = System.Drawing.Color.FromArgb(46, 204, 113);
            this.btnPost.ForeColor = System.Drawing.Color.White;
            this.btnClose.Text = "≈€·«ﬁ";

            this.btnAddItem.Dock = System.Windows.Forms.DockStyle.Right; this.btnAddItem.Width = 120;
            this.btnRemoveItem.Dock = System.Windows.Forms.DockStyle.Right; this.btnRemoveItem.Width = 120;
            this.btnSaveDraft.Dock = System.Windows.Forms.DockStyle.Left; this.btnSaveDraft.Width = 120;
            this.btnPost.Dock = System.Windows.Forms.DockStyle.Left; this.btnPost.Width = 120;
            this.btnClose.Dock = System.Windows.Forms.DockStyle.Left; this.btnClose.Width = 80;

            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 600);
            this.Controls.Add(this.dgvItems);
            this.Controls.Add(this.pnlButtons);
            this.Controls.Add(this.pnlFooter);
            this.Controls.Add(this.pnlHeader);
            this.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;

            this.btnAddItem.Click += new System.EventHandler(this.btnAddItem_Click);
            this.btnRemoveItem.Click += new System.EventHandler(this.btnRemoveItem_Click);
            this.btnSaveDraft.Click += new System.EventHandler(this.btnSaveDraft_Click);
            this.btnPost.Click += new System.EventHandler(this.btnPost_Click);
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);

            this.pnlHeader.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvItems)).EndInit();
            this.pnlFooter.ResumeLayout(false);
            this.pnlFooter.PerformLayout();
            this.pnlButtons.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Panel pnlHeader, pnlFooter, pnlButtons;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label lblTransferNoLabel, lblTransferNo, lblDate, lblStatus, lblSource, lblDest;
        private System.Windows.Forms.DateTimePicker dtpDate;
        private System.Windows.Forms.ComboBox cmbSource, cmbDest;
        private System.Windows.Forms.DataGridView dgvItems;
        private System.Windows.Forms.TextBox txtNotes;
        private System.Windows.Forms.Button btnAddItem, btnRemoveItem, btnSaveDraft, btnPost, btnClose;
    }
}
