using SalesSystem.Domain.Common;

namespace SalesSystem.Domain.Entities;

public class Customer : BaseEntity
{
    public string? Code { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public decimal OpeningBalance { get; private set; }
    public decimal CurrentBalance { get; private set; }
    public string? CreatedBy { get; private set; }
    public string? UpdatedBy { get; private set; }

    private Customer() { }

    public static Customer Create(
        string name,
        decimal openingBalance = 0,
        string? code = null,
        string? phone = null,
        string? email = null,
        string? address = null,
        string? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        return new Customer
        {
            Name = name,
            OpeningBalance = openingBalance,
            CurrentBalance = openingBalance,
            Code = code,
            Phone = phone,
            Email = email,
            Address = address,
            CreatedBy = createdBy
        };
    }

    public void IncreaseBalance(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive.", nameof(amount));
        CurrentBalance += amount;
    }

    public void DecreaseBalance(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive.", nameof(amount));
        CurrentBalance -= amount;
    }

    public void Update(
        string name,
        string? code,
        string? phone,
        string? email,
        string? address,
        string? updatedBy)
    {
        Name = name;
        Code = code;
        Phone = phone;
        Email = email;
        Address = address;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }
}