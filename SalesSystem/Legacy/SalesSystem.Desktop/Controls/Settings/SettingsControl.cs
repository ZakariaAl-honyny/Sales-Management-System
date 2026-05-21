using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Controls.Settings;

[System.ComponentModel.DesignerCategory("Code")]
public partial class SettingsControl : UserControl
{
    public SettingsControl()
    {
        InitializeComponent();
        this.RightToLeft = RightToLeft.Yes;
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();
        this.Name = "SettingsControl";
        this.Size = new Size(800, 600);
        this.ResumeLayout(false);
    }
}



