using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Services.Api.Interfaces;

namespace SalesSystem.Desktop.Controls.Users;

public partial class UsersControl : UserControl
{
    public UsersControl()
    {
        InitializeComponent();
        this.RightToLeft = RightToLeft.Yes;
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();
        this.Name = "UsersControl";
        this.Size = new Size(800, 600);
        this.ResumeLayout(false);
    }
}



