namespace SalesSystem.Desktop.Controls.Placeholders;

public partial class TransfersControl : BaseModuleControl
{
    public TransfersControl()
    {
        var lbl = new System.Windows.Forms.Label
        {
            Text = "تحويل المخزون — قريباً",
            Dock = System.Windows.Forms.DockStyle.Fill,
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
            Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold)
        };
        this.Controls.Add(lbl);
    }

    protected override void RegisterSubscriptions() { }
}
