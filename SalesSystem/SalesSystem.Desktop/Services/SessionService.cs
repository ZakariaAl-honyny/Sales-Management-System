using SalesSystem.Desktop.Models;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Services;

public sealed class SessionService : ISessionService
{
    public UserSession? Current { get; private set; }
    public bool IsAuthenticated => Current != null;

    public void SignIn(UserSession session)
    {
        Current = session;
    }

    public void SignOut()
    {
        Current = null;
    }
}
