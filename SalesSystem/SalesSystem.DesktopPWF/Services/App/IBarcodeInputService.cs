using System.Windows.Input;

namespace SalesSystem.DesktopPWF.Services.App;

public interface IBarcodeInputService
{
    /// <summary>
    /// Processes a key event and returns the barcode if completed (Enter pressed), 
    /// otherwise returns null.
    /// </summary>
    string? ProcessKey(Key key, string? keyText);
    
    /// <summary>
    /// Resets the current buffer.
    /// </summary>
    void Reset();
}
