using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Services;

public sealed class DialogService : IDialogService
{
    public bool Confirm(string message, string title = "تأكيد")
    {
        var result = MessageBox.Show(
            message, 
            title, 
            MessageBoxButtons.YesNo, 
            MessageBoxIcon.Question, 
            MessageBoxDefaultButton.Button2,
            MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

        return result == DialogResult.Yes;
    }
}
