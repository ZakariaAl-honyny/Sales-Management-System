using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class SystemSetting : AuditableEntity
{
    public string SettingKey { get; private set; } = string.Empty;
    public string SettingValue { get; private set; } = string.Empty;
    /// <summary>
    /// Schema: SettingType tinyint — 1=String, 2=Integer, 3=Decimal, 4=Boolean.
    /// </summary>
    public byte SettingType { get; private set; } = 1;
    public string Category { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    private SystemSetting() : base() { } // EF Core - calls base constructor to init CreatedAt

    public static SystemSetting Create(
        string settingKey,
        string settingValue,
        byte settingType = 1,
        string category = "General",
        string displayName = "",
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(settingKey))
            throw new DomainException("مفتاح الإعداد مطلوب.");
        if (string.IsNullOrWhiteSpace(category))
            throw new DomainException("تصنيف الإعداد مطلوب.");
        if (settingType < 1 || settingType > 4)
            throw new DomainException("نوع الإعداد غير صالح (1=نص, 2=رقم, 3=عشري, 4=منطقي).");

        return new SystemSetting
        {
            SettingKey = settingKey.Trim(),
            SettingValue = settingValue,
            SettingType = settingType,
            Category = category,
            DisplayName = displayName,
            Description = description,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateValue(string newValue, int? updatedByUserId = null)
    {
        SettingValue = newValue;
        SetUpdatedBy(updatedByUserId);
        UpdatedAt = DateTime.UtcNow;
    }
}