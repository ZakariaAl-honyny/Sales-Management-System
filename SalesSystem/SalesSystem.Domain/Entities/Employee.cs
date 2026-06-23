using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents an employee who works for the organisation.
/// Contact information (Name, Phone, Email, Address) is stored directly on this entity.
/// Each employee may optionally be linked to a Department and a Chart of Accounts Account
/// (for custody / advance tracking purposes).
/// </summary>
public class Employee : ActivatableEntity
{
    /// <summary>
    /// Employee name (required). This field holds the primary display name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Employee phone number (optional).
    /// </summary>
    public string? Phone { get; private set; }

    /// <summary>
    /// Employee email address (optional).
    /// </summary>
    public string? Email { get; private set; }

    /// <summary>
    /// Employee physical address (optional).
    /// </summary>
    public string? Address { get; private set; }

    /// <summary>
    /// FK to the Department the employee belongs to (optional).
    /// </summary>
    public short? DepartmentId { get; private set; }

    /// <summary>
    /// Navigation property to the linked Department.
    /// </summary>
    public virtual Department? Department { get; private set; }

    /// <summary>
    /// FK to the Chart of Accounts Account used for custody / advance tracking (optional).
    /// Auto-created when needed.
    /// </summary>
    public int? AccountId { get; private set; }

    /// <summary>
    /// Navigation property to the linked Account.
    /// </summary>
    public virtual Account? Account { get; private set; }

    /// <summary>
    /// Unique employee number (user-facing, distinct from the auto-increment Id).
    /// </summary>
    public int EmployeeNo { get; private set; }

    /// <summary>
    /// Date the employee was hired.
    /// </summary>
    public DateTime HireDate { get; private set; }

    /// <summary>
    /// Current salary amount. Defaults to 0.
    /// </summary>
    public decimal Salary { get; private set; }

    /// <summary>
    /// Optional free-text notes about the employee.
    /// </summary>
    public string? Notes { get; private set; }

    private Employee() { } // EF Core

    /// <summary>
    /// Factory method to create a new Employee with direct contact information.
    /// </summary>
    /// <param name="name">Employee name (required).</param>
    /// <param name="employeeNo">Unique employee number (required, must be &gt; 0).</param>
    /// <param name="hireDate">Date of hire (required).</param>
    /// <param name="phone">Optional phone number.</param>
    /// <param name="email">Optional email address.</param>
    /// <param name="address">Optional physical address.</param>
    /// <param name="notes">Free-text notes (optional).</param>
    /// <param name="departmentId">FK to Department (optional).</param>
    /// <param name="salary">Monthly salary amount (default 0).</param>
    /// <param name="createdByUserId">ID of the user creating this record.</param>
    /// <returns>A new Employee instance.</returns>
    /// <exception cref="DomainException">If any guard clause fails.</exception>
    public static Employee Create(
        string name,
        int employeeNo,
        DateTime hireDate,
        string? phone = null,
        string? email = null,
        string? address = null,
        string? notes = null,
        short? departmentId = null,
        decimal salary = 0,
        int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم الموظف مطلوب.");
        if (employeeNo <= 0)
            throw new DomainException("رقم الموظف يجب أن يكون أكبر من صفر.");
        if (hireDate == default)
            throw new DomainException("تاريخ التعيين مطلوب.");
        if (salary < 0)
            throw new DomainException("الراتب لا يمكن أن يكون سالباً.");

        var employee = new Employee
        {
            Name = name.Trim(),
            EmployeeNo = employeeNo,
            HireDate = hireDate,
            Phone = phone?.Trim(),
            Email = email?.Trim(),
            Address = address?.Trim(),
            Notes = notes?.Trim(),
            DepartmentId = departmentId,
            Salary = salary,
            IsActive = true
        };
        employee.SetCreatedBy(createdByUserId);
        return employee;
    }

    /// <summary>
    /// Updates mutable fields of the employee including contact information.
    /// Only non-null values are applied — null means "keep current value".
    /// </summary>
    /// <param name="name">Employee name (required).</param>
    /// <param name="phone">New phone number (null = keep current).</param>
    /// <param name="email">New email address (null = keep current).</param>
    /// <param name="address">New physical address (null = keep current).</param>
    /// <param name="departmentId">New department ID (pass 0 or negative to keep current).</param>
    /// <param name="salary">New salary (pass null to keep current).</param>
    /// <param name="notes">New notes (pass null to keep current).</param>
    /// <param name="updatedByUserId">ID of the user performing the update.</param>
    /// <exception cref="DomainException">If any guard clause fails.</exception>
    public void Update(
        string name,
        string? phone = null,
        string? email = null,
        string? address = null,
        short? departmentId = null,
        decimal? salary = null,
        string? notes = null,
        int? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم الموظف مطلوب.");
        if (departmentId.HasValue && departmentId.Value <= 0)
            throw new DomainException("معرّف القسم غير صالح.");
        if (salary.HasValue && salary.Value < 0)
            throw new DomainException("الراتب لا يمكن أن يكون سالباً.");

        Name = name.Trim();
        Phone = phone?.Trim() ?? Phone;
        Email = email?.Trim() ?? Email;
        Address = address?.Trim() ?? Address;
        DepartmentId = departmentId ?? DepartmentId;
        if (salary.HasValue)
            Salary = salary.Value;
        Notes = notes?.Trim() ?? Notes;
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }

    /// <summary>
    /// Links this employee to a Chart of Accounts Account (e.g. for custody/advance tracking).
    /// </summary>
    /// <param name="accountId">The Account ID to link (must be &gt; 0).</param>
    /// <param name="updatedByUserId">ID of the user performing the update.</param>
    /// <exception cref="DomainException">If accountId is not valid.</exception>
    public void SetAccountId(int accountId, int? updatedByUserId = null)
    {
        if (accountId <= 0)
            throw new DomainException("معرّف الحساب غير صالح.");

        AccountId = accountId;
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }
}
