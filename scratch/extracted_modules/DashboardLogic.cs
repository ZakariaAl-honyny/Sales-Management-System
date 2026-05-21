    public class DashboardControl : UserControl
    {
        private readonly HttpClient _httpClient;
        
        private Label lblSalesToday = null!;
        private Label lblPurchasesToday = null!;
        private Label lblReceivables = null!;
        private Label lblPayables = null!;
        private Label lblLowStock = null!;

        public DashboardControl(HttpClient httpClient)
        {
            _httpClient = httpClient;
            InitializeComponent();
            Load += async (_, _) => await LoadDataAsync();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(245, 246, 250); // لون مؤسسي هادئ
            RightToLeft = RightToLeft.Yes;

            var title = new Label
            {
                Text = "نظرة عامة على النظام",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(20, 20, 20, 0),
                ForeColor = Color.FromArgb(33, 43, 54)
            };

            var flowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                AutoScroll = true
            };

            // إنشاء البطاقات بتصميم احترافي
            lblSalesToday = new Label { Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.DarkGreen, AutoSize = true };
            flowPanel.Controls.Add(CreateCard("مبيعات اليوم", lblSalesToday));

            lblPurchasesToday = new Label { Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.DarkBlue, AutoSize = true };
            flowPanel.Controls.Add(CreateCard("مشتريات اليوم", lblPurchasesToday));

            lblReceivables = new Label { Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.DarkOrange, AutoSize = true };
            flowPanel.Controls.Add(CreateCard("ديون العملاء (لنا)", lblReceivables));

            lblPayables = new Label { Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.DarkRed, AutoSize = true };
            flowPanel.Controls.Add(CreateCard("ديون الموردين (علينا)", lblPayables));

            lblLowStock = new Label { Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.Goldenrod, AutoSize = true };
            flowPanel.Controls.Add(CreateCard("نواقص المخزون", lblLowStock));

            Controls.Add(flowPanel);
            Controls.Add(title);
        }

        private Panel CreateCard(string title, Label valueLabel)
        {
            var card = new Panel
            {
                Width = 250,
                Height = 120,
                BackColor = Color.White,
                Margin = new Padding(10),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblTitle = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                ForeColor = Color.Gray,
                Padding = new Padding(10),
                Height = 40
            };

            valueLabel.Dock = DockStyle.Fill;
            valueLabel.TextAlign = ContentAlignment.MiddleCenter;

            card.Controls.Add(valueLabel);
            card.Controls.Add(lblTitle);
            return card;
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponseDto<DashboardSummaryDto>>("api/dashboard/summary");
                if (response != null && response.IsSuccess && response.Data != null)
                {
                    lblSalesToday.Text = $"{response.Data.TotalSalesToday:N2} SAR";
                    lblPurchasesToday.Text = $"{response.Data.TotalPurchasesToday:N2} SAR";
                    lblReceivables.Text = $"{response.Data.TotalReceivables:N2} SAR";
                    lblPayables.Text = $"{response.Data.TotalPayables:N2} SAR";
                    lblLowStock.Text = $"{response.Data.LowStockItemsCount} صنف";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل لوحة القيادة: {ex.Message}");
            }
        }
    }
}

ثانياً: الإعدادات والنسخ الاحتياطي (Settings & Backup)
تعتمد هذه الميزة على قراءة وتحديث جدول StoreSettings الموجود مسبقاً، وتنفيذ أمر SQL خام لأخذ نسخة احتياطية.
1. طبقة Application (الخدمة)
في SalesSystem.Application/Services/SettingsService.cs:
C#
using Microsoft.EntityFrameworkCore;
using SalesSystem.Application.Abstractions.Services;
using SalesSystem.Application.Common;
using SalesSystem.Domain.Entities;
using SalesSystem.Infrastructure.Persistence;

namespace SalesSystem.Application.Services
{
    public class StoreSettingsDto
    {
        public string StoreName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public decimal DefaultTaxRate { get; set; }
    }

    public interface ISettingsService
    {
        Task<Result<StoreSettingsDto>> GetSettingsAsync(CancellationToken cancellationToken = default);
        Task<Result> UpdateSettingsAsync(StoreSettingsDto request, CancellationToken cancellationToken = default);
        Task<Result<string>> BackupDatabaseAsync(CancellationToken cancellationToken = default);
    }

    public class SettingsService : ISettingsService
    {
        private readonly SalesSystemDbContext _context;

        public SettingsService(SalesSystemDbContext context)
        {
            _context = context;
        }

        public async Task<Result<StoreSettingsDto>> GetSettingsAsync(CancellationToken cancellationToken = default)
        {
            var settings = await _context.StoreSettings.FirstOrDefaultAsync(cancellationToken);
            if (settings == null) return Result<StoreSettingsDto>.Failure("Settings not found.");

            return Result<StoreSettingsDto>.Success(new StoreSettingsDto
            {
                StoreName = settings.StoreName,
                Phone = settings.Phone,
                Address = settings.Address,
                DefaultTaxRate = settings.DefaultTaxRate
            });
        }

        public async Task<Result> UpdateSettingsAsync(StoreSettingsDto request, CancellationToken cancellationToken = default)
        {
            var settings = await _context.StoreSettings.FirstOrDefaultAsync(cancellationToken);
            if (settings == null) return Result.Failure("Settings not found.");

            settings.StoreName = request.StoreName;
            settings.Phone = request.Phone;
            settings.Address = request.Address;
            settings.DefaultTaxRate = request.DefaultTaxRate;
            settings.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result<string>> BackupDatabaseAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // مسار الحفظ الافتراضي في خادم قاعدة البيانات
                string backupFolder = @"C:\SalesSystemBackups";
                if (!Directory.Exists(backupFolder)) Directory.CreateDirectory(backupFolder);

                string fileName = $"SalesSystemDb_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                string fullPath = Path.Combine(backupFolder, fileName);

                // تنفيذ أمر النسخ الاحتياطي الخاص بـ SQL Server
                string sqlCommand = $"BACKUP DATABASE [SalesSystemDb] TO DISK = '{fullPath}'";
                await _context.Database.ExecuteSqlRawAsync(sqlCommand, cancellationToken);

                return Result<string>.Success(fullPath);
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"فشل النسخ الاحتياطي: {ex.Message}");
            }
        }
    }
