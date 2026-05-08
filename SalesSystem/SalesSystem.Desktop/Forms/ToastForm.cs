using SalesSystem.Desktop.Models;

namespace SalesSystem.Desktop.Forms;

public sealed class ToastForm : Form
{
    private readonly Notification _notification;
    private readonly System.Windows.Forms.Timer _timer;

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
            return cp;
        }
    }

    public ToastForm(Notification notification)
    {
        _notification = notification;
        
        this.FormBorderStyle = FormBorderStyle.None;
        this.ShowInTaskbar = false;
        this.TopMost = true;
        this.Size = new Size(300, 60);
        this.RightToLeft = RightToLeft.Yes;

        this.BackColor = notification.Type switch
        {
            NotificationType.Success => Color.FromArgb(46, 204, 113),
            NotificationType.Error => Color.FromArgb(231, 76, 60),
            NotificationType.Warning => Color.FromArgb(230, 126, 34),
            _ => Color.Gray
        };

        var lbl = new Label
        {
            Text = notification.Message,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(10)
        };
        this.Controls.Add(lbl);

        _timer = new System.Windows.Forms.Timer { Interval = notification.Duration };
        _timer.Tick += (s, e) => { _timer.Stop(); this.Close(); };
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        PositionToast();
        _timer.Start();
    }

    private void PositionToast()
    {
        var workingArea = Screen.FromControl(this).WorkingArea;
        this.Location = new Point(
            workingArea.Right - this.Width - 20,
            workingArea.Bottom - this.Height - 20
        );
    }
}
