using SalesSystem.DesktopPWF.Services.Api;
using SalesSystem.Contracts.Enums;

namespace SalesSystem.DesktopPWF.ViewModels.Base;

public abstract class AdminOnlyViewModel : ViewModelBase
{
    private readonly ISessionService _sessionService;

    protected AdminOnlyViewModel(ISessionService sessionService)
    {
        _sessionService = sessionService;
        EnsureAdminRole();
    }

    protected void EnsureAdminRole()
    {
        var role = _sessionService.GetUserRole();

        if (role != UserRole.Admin)
            throw new UnauthorizedAccessException(
                "عذراً، هذه الشاشة مخصصة للمسؤولين فقط. لا يمكنك الوصول إليها.");
    }
}
