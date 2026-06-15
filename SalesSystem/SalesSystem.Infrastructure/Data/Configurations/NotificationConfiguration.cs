using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Infrastructure.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Type).IsRequired();
        builder.Property(n => n.RecipientType).IsRequired().HasDefaultValue((byte)1);
        builder.Property(n => n.Title).IsRequired().HasMaxLength(200);
        builder.Property(n => n.Message).IsRequired().HasMaxLength(1000);
        builder.Property(n => n.IsRead).HasDefaultValue(false);
        builder.Property(n => n.ReferenceType).HasMaxLength(50);
        builder.Property(n => n.ReferenceId);
        builder.Property(n => n.ReadAt);

        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(n => n.UserId)
            .HasDatabaseName("IX_Notifications_UserId");

        builder.HasIndex(n => new { n.UserId, n.IsRead })
            .HasDatabaseName("IX_Notifications_UserId_IsRead");

        builder.HasIndex(n => n.RecipientType)
            .HasDatabaseName("IX_Notifications_RecipientType");

        builder.HasIndex(n => n.CreatedAt)
            .IsDescending()
            .HasDatabaseName("IX_Notifications_CreatedAt_Desc");
    }
}
