using SalesSystem.DesktopPWF.Services.Api;

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
        if (!_sessionService.IsAdmin)
            throw new UnauthorizedAccessException(
                "عذراً، هذه الشاشة مخصصة للمسؤولين فقط. لا يمكنك الوصول إليها.");
    }
}
