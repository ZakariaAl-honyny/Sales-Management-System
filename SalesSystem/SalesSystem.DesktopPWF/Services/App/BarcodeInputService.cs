using System.Text;
using System.Windows.Input;

namespace SalesSystem.DesktopPWF.Services.App;

public class BarcodeInputService : IBarcodeInputService
{
    private readonly StringBuilder _buffer = new();
    private DateTime _lastKeyTime = DateTime.MinValue;
    private const int ScannerTimeoutMs = 100;

    public string? ProcessKey(Key key, string? keyText)
    {
        var now = DateTime.Now;
        
        // If too much time passed, reset buffer
        if ((now - _lastKeyTime).TotalMilliseconds > ScannerTimeoutMs && _buffer.Length > 0)
        {
            _buffer.Clear();
        }

        _lastKeyTime = now;

        if (key == Key.Enter)
        {
            if (_buffer.Length > 0)
            {
                var barcode = _buffer.ToString().Trim();
                _buffer.Clear();
                return barcode;
            }
            return null;
        }

        // Use keyText if provided, otherwise fallback to simple mapping
        string text = keyText ?? GetKeyText(key);
        if (!string.IsNullOrEmpty(text))
        {
            _buffer.Append(text);
        }

        return null;
    }

    private string GetKeyText(Key key)
    {
        if (key >= Key.D0 && key <= Key.D9)
            return (key - Key.D0).ToString();
        
        if (key >= Key.NumPad0 && key <= Key.NumPad9)
            return (key - Key.NumPad0).ToString();

        if (key >= Key.A && key <= Key.Z)
            return key.ToString();

        return string.Empty;
    }

    public void Reset()
    {
        _buffer.Clear();
    }
}
