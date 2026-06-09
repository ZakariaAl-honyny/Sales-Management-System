using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class ChequeConfiguration : IEntityTypeConfiguration<Cheque>
{
    public void Configure(EntityTypeBuilder<Cheque> builder)
    {
        builder.ToTable("Cheques");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ChequeNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.BankName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.IssueDate)
            .IsRequired();

        builder.Property(x => x.MaturityDate)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(ChequeStatus.Pending);

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.CustomerPaymentId)
            .IsRequired(false);

        builder.Property(x => x.SupplierPaymentId)
            .IsRequired(false);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.HasOne(x => x.CustomerPayment)
            .WithMany()
            .HasForeignKey(x => x.CustomerPaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SupplierPayment)
            .WithMany()
            .HasForeignKey(x => x.SupplierPaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ChequeNumber)
            .HasDatabaseName("IX_Cheques_ChequeNumber");

        builder.HasQueryFilter(x => x.IsActive);
    }
}
