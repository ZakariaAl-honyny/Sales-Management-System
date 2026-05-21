using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class SystemSetting : BaseEntity
{
    public string SettingKey { get; private set; } = string.Empty;
    public string SettingValue { get; private set; } = string.Empty;
    public string DataType { get; private set; } = "string";
    public string Category { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int? UpdatedBy { get; private set; }

    private SystemSetting() : base() { } // EF Core - calls base constructor to init CreatedAt

    public static SystemSetting Create(
        string settingKey,
        string settingValue,
        string dataType = "string",
        string category = "General",
        string displayName = "",
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(settingKey))
            throw new DomainException("مفتاح الإعداد مطلوب.");

        return new SystemSetting
        {
            SettingKey = settingKey.Trim(),
            SettingValue = settingValue,
            DataType = dataType,
            Category = category,
            DisplayName = displayName,
            Description = description,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateValue(string newValue, int? updatedBy = null)
    {
        SettingValue = newValue;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }
}