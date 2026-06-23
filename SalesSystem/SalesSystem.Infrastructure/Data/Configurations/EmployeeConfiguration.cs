using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");
        builder.HasKey(e => e.Id);

        // ─── Property Configurations ──────────────────────

        // Direct contact fields (replaces Party relationship)
        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Phone)
            .HasMaxLength(20);

        builder.Property(e => e.Email)
            .HasMaxLength(100);

        builder.Property(e => e.Address)
            .HasMaxLength(500);

        builder.Property(e => e.EmployeeNo)
            .IsRequired();

        builder.Property(e => e.HireDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(e => e.Salary)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        // ─── Indexes ──────────────────────────────────────
        builder.HasIndex(e => e.EmployeeNo)
            .IsUnique()
            .HasDatabaseName("IX_Employees_EmployeeNo");

        builder.HasIndex(e => e.Name)
            .HasDatabaseName("IX_Employees_Name");

        builder.HasIndex(e => e.Phone)
            .HasDatabaseName("IX_Employees_Phone");

        builder.HasIndex(e => e.DepartmentId)
            .HasDatabaseName("IX_Employees_DepartmentId");

        builder.HasIndex(e => e.AccountId)
            .HasDatabaseName("IX_Employees_AccountId");

        // ─── Foreign Keys ─────────────────────────────────
        builder.HasOne(e => e.Department)
            .WithMany()
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne<Account>(e => e.Account)
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // ─── Query Filter ─────────────────────────────────
        builder.HasQueryFilter(e => e.IsActive);
    }
}
