using SalesSystem.Desktop.Models;

namespace SalesSystem.Desktop.Services.Interfaces;

public interface ISessionService
{
    UserSession? Current { get; }
    bool IsAuthenticated { get; }
    void SignIn(UserSession session);
    void SignOut();
}
