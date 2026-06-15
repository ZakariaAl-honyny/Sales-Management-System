using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a classification category for Chart of Accounts (Account) entities.
/// Examples: "عام", "جملة", "مطاعم".
/// Maps to "AccountCategories" table — smallint PK.
/// </summary>
public class AccountCategory : ActivatableEntity
{
    /// <summary>
    /// smallint PK — overrides base int Id for small lookup tables.
    /// </summary>
    public new short Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    private AccountCategory() { }

    /// <summary>
    /// Factory method to create a new AccountCategory.
    /// </summary>
    /// <param name="name">The category name (required, trimmed).</param>
    /// <param name="description">Optional description.</param>
    /// <param name="createdByUserId">Optional ID of the creating user.</param>
    /// <returns>A new AccountCategory instance.</returns>
    public static AccountCategory Create(string name, string? description = null, int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم التصنيف المحاسبي مطلوب.");

        var category = new AccountCategory
        {
            Name = name.Trim(),
            Description = description?.Trim()
        };
        category.SetCreatedBy(createdByUserId);
        return category;
    }

    /// <summary>
    /// Updates the category properties.
    /// </summary>
    /// <param name="name">The updated category name (required, trimmed).</param>
    /// <param name="description">Optional updated description.</param>
    /// <param name="updatedByUserId">Optional ID of the updating user.</param>
    public void Update(string name, string? description = null, int? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم التصنيف المحاسبي مطلوب.");

        Name = name.Trim();
        Description = description?.Trim();
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }
}
