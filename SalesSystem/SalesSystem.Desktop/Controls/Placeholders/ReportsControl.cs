using SalesSystem.Desktop.Controls.Common;

namespace SalesSystem.Desktop.Controls.Placeholders;

public partial class ReportsControl : BaseModuleControl
{
    private FlowLayoutPanel _flpReports;

    public ReportsControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this._flpReports = new System.Windows.Forms.FlowLayoutPanel();
        this.SuspendLayout();

        this._flpReports.Dock = System.Windows.Forms.DockStyle.Fill;
        this._flpReports.Padding = new System.Windows.Forms.Padding(20);
        this._flpReports.AutoScroll = true;
        this._flpReports.RightToLeft = RightToLeft.Yes;

        AddReportButton("تقرير المبيعات", Color.FromArgb(52, 152, 219));
        AddReportButton("تقرير المشتريات", Color.FromArgb(46, 204, 113));
        AddReportButton("تقرير المخزون", Color.FromArgb(241, 196, 15));
        AddReportButton("كشف حساب عميل", Color.FromArgb(155, 89, 182));
        AddReportButton("كشف حساب مورد", Color.FromArgb(231, 76, 60));
        AddReportButton("أرباح وخسائر", Color.FromArgb(52, 73, 94));

        this.Controls.Add(this._flpReports);
        this.RightToLeft = RightToLeft.Yes;
        this.Size = new System.Drawing.Size(1000, 700);
        this.ResumeLayout(false);
    }

    private void AddReportButton(string title, Color color)
    {
        var btn = new Button
        {
            Text = title,
            Size = new Size(200, 100),
            BackColor = color,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font(this.Font.FontFamily, 12, FontStyle.Bold),
            Margin = new Padding(10)
        };
        btn.Click += (s, e) => MessageBox.Show($"سيتم تفعيل {title} في الإصدار القادم", "قريباً", MessageBoxButtons.OK, MessageBoxIcon.Information);
        _flpReports.Controls.Add(btn);
    }

    protected override void RegisterSubscriptions() { }
}
