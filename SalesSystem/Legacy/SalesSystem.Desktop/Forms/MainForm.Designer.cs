namespace SalesSystem.Desktop.Forms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;


        private void InitializeComponent()
        {
            this.pnlSidebar = new System.Windows.Forms.Panel();
            this.pnlSidebarHeader = new System.Windows.Forms.Panel();
            this.lblStoreName = new System.Windows.Forms.Label();
            this.flpNav = new System.Windows.Forms.FlowLayoutPanel();
            this.pnlTopBar = new System.Windows.Forms.Panel();
            this.lblUserRole = new System.Windows.Forms.Label();
            this.lblUserName = new System.Windows.Forms.Label();
            this.btnLogout = new System.Windows.Forms.Button();
            this.pnlContent = new System.Windows.Forms.Panel();
            this.pnlSidebar.SuspendLayout();
            this.pnlSidebarHeader.SuspendLayout();
            this.pnlTopBar.SuspendLayout();
            this.SuspendLayout();
            
            // pnlSidebar
            this.pnlSidebar.BackColor = System.Drawing.Color.FromArgb(33, 43, 54);
            this.pnlSidebar.Controls.Add(this.flpNav);
            this.pnlSidebar.Controls.Add(this.pnlSidebarHeader);
            this.pnlSidebar.Dock = System.Windows.Forms.DockStyle.Right;
            this.pnlSidebar.Location = new System.Drawing.Point(1060, 0);
            this.pnlSidebar.Name = "pnlSidebar";
            this.pnlSidebar.Size = new System.Drawing.Size(220, 800);
            this.pnlSidebar.TabIndex = 0;
            
            // pnlSidebarHeader
            this.pnlSidebarHeader.Controls.Add(this.lblStoreName);
            this.pnlSidebarHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlSidebarHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlSidebarHeader.Name = "pnlSidebarHeader";
            this.pnlSidebarHeader.Size = new System.Drawing.Size(220, 80);
            this.pnlSidebarHeader.TabIndex = 0;
            
            // lblStoreName
            this.lblStoreName.ForeColor = System.Drawing.Color.White;
            this.lblStoreName.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblStoreName.Location = new System.Drawing.Point(0, 0);
            this.lblStoreName.Name = "lblStoreName";
            this.lblStoreName.Size = new System.Drawing.Size(220, 80);
            this.lblStoreName.Text = "نظام المبيعات";
            this.lblStoreName.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            
            // flpNav
            this.flpNav.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpNav.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpNav.Location = new System.Drawing.Point(0, 80);
            this.flpNav.Name = "flpNav";
            this.flpNav.Padding = new System.Windows.Forms.Padding(10);
            this.flpNav.Size = new System.Drawing.Size(220, 720);
            this.flpNav.TabIndex = 1;
            
            // pnlTopBar
            this.pnlTopBar.BackColor = System.Drawing.Color.White;
            this.pnlTopBar.Controls.Add(this.lblUserRole);
            this.pnlTopBar.Controls.Add(this.lblUserName);
            this.pnlTopBar.Controls.Add(this.btnLogout);
            this.pnlTopBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTopBar.Location = new System.Drawing.Point(0, 0);
            this.pnlTopBar.Name = "pnlTopBar";
            this.pnlTopBar.Padding = new System.Windows.Forms.Padding(15, 5, 15, 5);
            this.pnlTopBar.Size = new System.Drawing.Size(1060, 60);
            this.pnlTopBar.TabIndex = 1;
            
            // btnLogout
            this.btnLogout.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(67)))), ((int)(((byte)(54)))));
            this.btnLogout.FlatAppearance.BorderSize = 0;
            this.btnLogout.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLogout.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnLogout.ForeColor = System.Drawing.Color.White;
            this.btnLogout.Location = new System.Drawing.Point(20, 15);
            this.btnLogout.Name = "btnLogout";
            this.btnLogout.Size = new System.Drawing.Size(100, 32);
            this.btnLogout.TabIndex = 0;
            this.btnLogout.Text = "تسجيل الخروج";
            this.btnLogout.UseVisualStyleBackColor = false;
            this.btnLogout.Click += new System.EventHandler(this.btnLogout_Click);

            // lblUserName
            this.lblUserName.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblUserName.Location = new System.Drawing.Point(130, 15);
            this.lblUserName.Name = "lblUserName";
            this.lblUserName.Size = new System.Drawing.Size(200, 32);
            this.lblUserName.TabIndex = 1;
            this.lblUserName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            
            // lblUserRole
            this.lblUserRole.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblUserRole.ForeColor = System.Drawing.Color.Gray;
            this.lblUserRole.Location = new System.Drawing.Point(340, 15);
            this.lblUserRole.Name = "lblUserRole";
            this.lblUserRole.Size = new System.Drawing.Size(100, 32);
            this.lblUserRole.TabIndex = 2;
            this.lblUserRole.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            
            // pnlContent
            this.pnlContent.BackColor = System.Drawing.Color.FromArgb(240, 242, 245);
            this.pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlContent.Location = new System.Drawing.Point(0, 48);
            this.pnlContent.Name = "pnlContent";
            this.pnlContent.Size = new System.Drawing.Size(1060, 752);
            this.pnlContent.TabIndex = 2;
            
            // MainForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1280, 800);
            this.Controls.Add(this.pnlContent);
            this.Controls.Add(this.pnlTopBar);
            this.Controls.Add(this.pnlSidebar);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "نظام إدارة المبيعات";
            this.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.pnlSidebar.ResumeLayout(false);
            this.pnlSidebarHeader.ResumeLayout(false);
            this.pnlTopBar.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Panel pnlSidebar;
        private System.Windows.Forms.Panel pnlSidebarHeader;
        private System.Windows.Forms.Label lblStoreName;
        private System.Windows.Forms.FlowLayoutPanel flpNav;
        private System.Windows.Forms.Panel pnlTopBar;
        private System.Windows.Forms.Button btnLogout;
        private System.Windows.Forms.Label lblUserName;
        private System.Windows.Forms.Label lblUserRole;
        private System.Windows.Forms.Panel pnlContent;
    }
}



