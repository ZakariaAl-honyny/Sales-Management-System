using System.Drawing;
using System.Drawing.Drawing2D;

namespace SalesSystem.Desktop.Helpers;

public static class ThemeHelper
{
    // Institutional Professional Palette (Modern Flat Colors)
    public static readonly Color Primary = ColorTranslator.FromHtml("#0D6EFD");   // Modern Blue
    public static readonly Color Success = ColorTranslator.FromHtml("#198754");   // Eye-friendly Green
    public static readonly Color Danger = ColorTranslator.FromHtml("#DC3545");    // Muted Red
    public static readonly Color Warning = ColorTranslator.FromHtml("#FFC107");   // Amber
    public static readonly Color Neutral = Color.FromArgb(108, 117, 125);         // Gray
    public static readonly Color Secondary = ColorTranslator.FromHtml("#F8F9FA"); // Light Gray
    public static readonly Color BorderColor = ColorTranslator.FromHtml("#DEE2E6");
    
    public static readonly Color SidebarBg = Color.FromArgb(33, 43, 54);
    public static readonly Color ContentBg = Color.White;
    public static readonly Color TextPrimary = ColorTranslator.FromHtml("#212529");
    public static readonly Color TextSecondary = Color.FromArgb(108, 117, 125);
    public static readonly Color TextDarkGray = Color.FromArgb(50, 50, 50);

    // Standard Dimensions
    public static readonly int StandardControlHeight = 35;
    public static readonly int StandardButtonWidth = 100;
    
    public enum ButtonType { Primary, Success, Danger, Warning, Neutral, Secondary, Ghost }

    public static void ApplyMainFormStyle(Form form)
    {
        form.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
        form.BackColor = Color.White;
        form.RightToLeft = RightToLeft.Yes;
        form.RightToLeftLayout = true;
    }

    public static void ApplyButtonStyle(Button btn, ButtonType type = ButtonType.Primary)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.Cursor = Cursors.Hand;
        btn.Height = StandardControlHeight;
        btn.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btn.Padding = new Padding(12, 0, 12, 0);
        btn.AutoSize = true;
        btn.MinimumSize = new Size(StandardButtonWidth, StandardControlHeight);
        btn.FlatAppearance.BorderSize = 0;

        switch (type)
        {
            case ButtonType.Primary:
                btn.BackColor = Primary;
                btn.ForeColor = Color.White;
                break;
            case ButtonType.Success:
                btn.BackColor = Success;
                btn.ForeColor = Color.White;
                break;
            case ButtonType.Danger:
                btn.BackColor = Danger;
                btn.ForeColor = Color.White;
                break;
            case ButtonType.Secondary:
                btn.BackColor = Secondary;
                btn.ForeColor = TextPrimary;
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = BorderColor;
                break;
            case ButtonType.Ghost:
                btn.BackColor = Color.Transparent;
                btn.ForeColor = TextSecondary;
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = BorderColor;
                break;
            case ButtonType.Neutral:
                btn.BackColor = Neutral;
                btn.ForeColor = Color.White;
                break;
            default:
                btn.BackColor = Secondary;
                btn.ForeColor = TextPrimary;
                break;
        }

        btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(btn.BackColor, 0.1f);
        if (type == ButtonType.Ghost || type == ButtonType.Secondary)
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 240, 240);
    }

    public static void ApplyToolbarStyle(Panel toolbar)
    {
        toolbar.Padding = new Padding(12);
        toolbar.BackColor = Color.White;
        toolbar.Height = 65;
        
        // Use a bottom border for the toolbar
        toolbar.Paint += (s, e) => {
            using var pen = new Pen(BorderColor, 1);
            e.Graphics.DrawLine(pen, 0, toolbar.Height - 1, toolbar.Width, toolbar.Height - 1);
        };
    }

    public static void ApplyDataGridViewStyle(DataGridView dgv)
    {
        dgv.BackgroundColor = Color.White;
        dgv.BorderStyle = BorderStyle.None;
        dgv.GridColor = Color.FromArgb(240, 240, 240);
        dgv.RowHeadersVisible = false;
        dgv.AllowUserToResizeRows = false;
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgv.MultiSelect = false;
        dgv.ReadOnly = true;
        dgv.EnableHeadersVisualStyles = false;
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgv.Margin = new Padding(0);

        // Header Style
        dgv.ColumnHeadersHeight = 45;
        dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary;
        dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(248, 249, 250);

        // Rows Style
        dgv.DefaultCellStyle.BackColor = Color.White;
        dgv.DefaultCellStyle.ForeColor = TextDarkGray; // Replaced bright blue with eye-friendly dark gray
        dgv.DefaultCellStyle.Font = new Font("Segoe UI", 10F);
        dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 244, 253);
        dgv.DefaultCellStyle.SelectionForeColor = Primary;
        dgv.DefaultCellStyle.Padding = new Padding(5, 0, 5, 0);
        dgv.RowTemplate.Height = 40;

        dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(252, 253, 254);
    }

    public static void ApplySearchBoxStyle(TextBox txt)
    {
        txt.Height = StandardControlHeight;
        txt.Font = new Font("Segoe UI", 11F);
        txt.BorderStyle = BorderStyle.FixedSingle;
    }

    public static void ApplyDialogStyle(Form form)
    {
        ApplyMainFormStyle(form);
        form.StartPosition = FormStartPosition.CenterParent;
        form.FormBorderStyle = FormBorderStyle.FixedDialog;
        form.MaximizeBox = false;
        form.MinimizeBox = false;
        form.Padding = new Padding(20);
    }
}
