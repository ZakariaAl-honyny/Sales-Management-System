using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class UserBranchConfiguration : IEntityTypeConfiguration<UserBranch>
{
    public void Configure(EntityTypeBuilder<UserBranch> builder)
    {
        builder.ToTable("UserBranches");
        builder.HasKey(ub => ub.Id);

        // UserId — FK to Users
        builder.Property(ub => ub.UserId).IsRequired();
        builder.HasIndex(ub => new { ub.UserId, ub.BranchId }).IsUnique();

        // BranchId — FK to Branches
        builder.Property(ub => ub.BranchId).IsRequired();

        // Navigation: User
        builder.HasOne(ub => ub.User)
            .WithMany(u => u.UserBranches)
            .HasForeignKey(ub => ub.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Navigation: Branch
        builder.HasOne(ub => ub.Branch)
            .WithMany()
            .HasForeignKey(ub => ub.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}
