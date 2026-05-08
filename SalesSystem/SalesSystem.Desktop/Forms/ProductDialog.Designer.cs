namespace SalesSystem.Desktop.Forms
{
    partial class ProductDialog
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
            this.lblCode = new System.Windows.Forms.Label();
            this.txtCode = new System.Windows.Forms.TextBox();
            this.lblBarcode = new System.Windows.Forms.Label();
            this.txtBarcode = new System.Windows.Forms.TextBox();
            this.lblCategory = new System.Windows.Forms.Label();
            this.cmbCategory = new System.Windows.Forms.ComboBox();
            this.lblUnit = new System.Windows.Forms.Label();
            this.cmbUnit = new System.Windows.Forms.ComboBox();
            this.lblPurchasePrice = new System.Windows.Forms.Label();
            this.txtPurchasePrice = new SalesSystem.Desktop.Controls.Common.MoneyTextBox();
            this.lblSalePrice = new System.Windows.Forms.Label();
            this.txtSalePrice = new SalesSystem.Desktop.Controls.Common.MoneyTextBox();
            this.lblMinStock = new System.Windows.Forms.Label();
            this.txtMinStock = new System.Windows.Forms.TextBox();
            this.lblDescription = new System.Windows.Forms.Label();
            this.txtDescription = new System.Windows.Forms.TextBox();
            this.chkIsActive = new System.Windows.Forms.CheckBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            
            // Labels & Inputs
            int labelX = 20, inputX = 130, y = 20, spacing = 40;

            this.lblName.Text = "اسم المنتج:";
            this.lblName.Location = new System.Drawing.Point(labelX, y);
            this.txtName.Location = new System.Drawing.Point(inputX, y);
            this.txtName.Size = new System.Drawing.Size(250, 27);
            
            y += spacing;
            this.lblCode.Text = "الكود:";
            this.lblCode.Location = new System.Drawing.Point(labelX, y);
            this.txtCode.Location = new System.Drawing.Point(inputX, y);
            this.txtCode.Size = new System.Drawing.Size(120, 27);

            this.lblBarcode.Text = "الباركود:";
            this.lblBarcode.Location = new System.Drawing.Point(inputX + 130, y);
            this.txtBarcode.Location = new System.Drawing.Point(inputX + 190, y);
            this.txtBarcode.Size = new System.Drawing.Size(120, 27);

            y += spacing;
            this.lblCategory.Text = "التصنيف:";
            this.lblCategory.Location = new System.Drawing.Point(labelX, y);
            this.cmbCategory.Location = new System.Drawing.Point(inputX, y);
            this.cmbCategory.Size = new System.Drawing.Size(250, 27);
            this.cmbCategory.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;

            y += spacing;
            this.lblUnit.Text = "الوحدة:";
            this.lblUnit.Location = new System.Drawing.Point(labelX, y);
            this.cmbUnit.Location = new System.Drawing.Point(inputX, y);
            this.cmbUnit.Size = new System.Drawing.Size(250, 27);
            this.cmbUnit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;

            y += spacing;
            this.lblPurchasePrice.Text = "سعر الشراء:";
            this.lblPurchasePrice.Location = new System.Drawing.Point(labelX, y);
            this.txtPurchasePrice.Location = new System.Drawing.Point(inputX, y);
            this.txtPurchasePrice.Size = new System.Drawing.Size(100, 27);

            this.lblSalePrice.Text = "سعر البيع:";
            this.lblSalePrice.Location = new System.Drawing.Point(inputX + 110, y);
            this.txtSalePrice.Location = new System.Drawing.Point(inputX + 180, y);
            this.txtSalePrice.Size = new System.Drawing.Size(100, 27);

            y += spacing;
            this.lblMinStock.Text = "الحد الأدنى:";
            this.lblMinStock.Location = new System.Drawing.Point(labelX, y);
            this.txtMinStock.Location = new System.Drawing.Point(inputX, y);
            this.txtMinStock.Size = new System.Drawing.Size(100, 27);

            this.chkIsActive.Text = "نشط";
            this.chkIsActive.Location = new System.Drawing.Point(inputX + 110, y);

            y += spacing;
            this.lblDescription.Text = "الوصف:";
            this.lblDescription.Location = new System.Drawing.Point(labelX, y);
            this.txtDescription.Location = new System.Drawing.Point(inputX, y);
            this.txtDescription.Size = new System.Drawing.Size(250, 60);
            this.txtDescription.Multiline = true;

            y += 80;
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

            // ProductDialog
            this.ClientSize = new System.Drawing.Size(420, y + 60);
            this.Controls.AddRange(new System.Windows.Forms.Control[] {
                lblName, txtName, lblCode, txtCode, lblBarcode, txtBarcode,
                lblCategory, cmbCategory, lblUnit, cmbUnit,
                lblPurchasePrice, txtPurchasePrice, lblSalePrice, txtSalePrice,
                lblMinStock, txtMinStock, lblDescription, txtDescription,
                chkIsActive, btnSave, btnCancel
            });
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ProductDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblName, lblCode, lblBarcode, lblCategory, lblUnit, lblPurchasePrice, lblSalePrice, lblMinStock, lblDescription;
        private System.Windows.Forms.TextBox txtName, txtCode, txtBarcode, txtMinStock, txtDescription;
        private System.Windows.Forms.ComboBox cmbCategory, cmbUnit;
        private SalesSystem.Desktop.Controls.Common.MoneyTextBox txtPurchasePrice, txtSalePrice;
        private System.Windows.Forms.CheckBox chkIsActive;
        private System.Windows.Forms.Button btnSave, btnCancel;
    }
}
