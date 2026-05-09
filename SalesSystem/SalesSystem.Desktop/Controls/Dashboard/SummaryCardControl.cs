namespace SalesSystem.Desktop.Controls.Dashboard;

public class SummaryCardControl : UserControl
{
    private Label lblTitle = null!;
    private Label lblValue = null!;
    private PictureBox picIcon = null!;

    public SummaryCardControl(string title, string initialValue, Color backColor)
    {
        InitializeComponent(title, initialValue, backColor);
    }

    private void InitializeComponent(string title, string initialValue, Color backColor)
    {
        this.Size = new Size(220, 100);
        this.BackColor = backColor;
        this.Padding = new Padding(10);
        this.ForeColor = Color.White;

        lblTitle = new Label { 
            Text = title, 
            Dock = DockStyle.Top, 
            Height = 25, 
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            TextAlign = ContentAlignment.TopRight
        };

        lblValue = new Label { 
            Text = initialValue, 
            Dock = DockStyle.Fill, 
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            TextAlign = ContentAlignment.BottomLeft
        };

        this.Controls.Add(lblValue);
        this.Controls.Add(lblTitle);
        
        // Add subtle border/radius if needed via painting, but keep it simple for now
    }

    public void UpdateValue(string value)
    {
        if (this.InvokeRequired) this.Invoke(() => lblValue.Text = value);
        else lblValue.Text = value;
    }
}
