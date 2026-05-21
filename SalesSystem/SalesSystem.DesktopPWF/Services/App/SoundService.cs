using System.Media;

namespace SalesSystem.DesktopPWF.Services.App;

public class SoundService : ISoundService
{
    public void PlaySuccess()
    {
        // Asterisk is a pleasant "ding" sound in Windows
        SystemSounds.Asterisk.Play();
    }

    public void PlayError()
    {
        SystemSounds.Hand.Play();
    }

    public void PlayNotification()
    {
        SystemSounds.Beep.Play();
    }
}
