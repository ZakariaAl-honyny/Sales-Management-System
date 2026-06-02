namespace SalesSystem.DesktopPWF.Services.App;

public interface ISoundService
{
    void PlaySuccess();
    void PlayError();
    void PlayWarning();
    void PlayNotification();
}
