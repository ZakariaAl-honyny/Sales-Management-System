using SalesSystem.Desktop.Models;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Services;

public sealed class SessionService : ISessionService
{
    private readonly object _lock = new();
    private UserSession? _current;

    public UserSession? Current 
    { 
        get { lock (_lock) return _current; }
        private set { lock (_lock) _current = value; }
    }

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

