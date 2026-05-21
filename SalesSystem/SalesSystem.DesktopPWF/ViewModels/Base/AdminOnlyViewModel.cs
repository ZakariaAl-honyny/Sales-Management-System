using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.Contracts.Enums;

namespace SalesSystem.DesktopPWF.ViewModels.Base;

public abstract class AdminOnlyViewModel : ViewModelBase
{
    protected AdminOnlyViewModel()
    {
        EnsureAdminRole();
    }

    protected void EnsureAdminRole()
    {
        var session = App.GetService<ISessionService>();
        var role = session.GetUserRole();

        if (role != UserRole.Admin)
            throw new UnauthorizedAccessException(
                "عذراً، هذه الشاشة مخصصة للمسؤولين فقط. لا يمكنك الوصول إليها.");
    }
}
