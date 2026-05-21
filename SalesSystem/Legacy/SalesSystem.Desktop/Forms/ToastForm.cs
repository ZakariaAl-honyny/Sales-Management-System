using SalesSystem.Desktop.Models;
using System.Drawing.Drawing2D;

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
        this.Size = new Size(350, 70);
        this.RightToLeft = RightToLeft.Yes;
        this.BackColor = Color.White; // Base back color

        var accentColor = notification.Type switch
        {
            NotificationType.Success => Color.FromArgb(46, 204, 113),
            NotificationType.Error => Color.FromArgb(231, 76, 60),
            NotificationType.Warning => Color.FromArgb(230, 126, 34),
            _ => Color.FromArgb(52, 152, 219)
        };

        // Custom painting for a modern look
        this.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(accentColor);
            // Side accent bar
            e.Graphics.FillRectangle(brush, this.Width - 10, 0, 10, this.Height);
            
            // Border
            using var pen = new Pen(Color.FromArgb(220, 220, 220), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
        };

        var lbl = new Label
        {
            Text = notification.Message,
            ForeColor = Color.FromArgb(33, 43, 54),
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(20, 10, 20, 10)
        };
        this.Controls.Add(lbl);

        _timer = new System.Windows.Forms.Timer { Interval = notification.Duration > 0 ? notification.Duration : 3000 };
        _timer.Tick += (s, e) => { _timer.Stop(); FadeOut(); };
    }

    private async void FadeOut()
    {
        for (double i = 1.0; i >= 0; i -= 0.1)
        {
            this.Opacity = i;
            await Task.Delay(20);
        }
        this.Close();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        PositionToast();
        this.Opacity = 0;
        FadeIn();
    }

    private async void FadeIn()
    {
        _timer.Start();
        for (double i = 0; i <= 1.0; i += 0.1)
        {
            this.Opacity = i;
            await Task.Delay(20);
        }
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



