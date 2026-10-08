using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Notifications;

internal sealed class NotificationDeliveryConfiguration : EntityConfiguration<NotificationDelivery>
{
    protected override void ConfigureEntity(EntityTypeBuilder<NotificationDelivery> builder)
    {
        builder.ToTable("notification_deliveries");

        builder.Property(item => item.CustomerId).HasMaxLength(40).IsRequired();
        builder.Property(item => item.TemplateKey).HasMaxLength(60).IsRequired();
        builder.Property(item => item.Channel).HasConversion<string>().HasMaxLength(10);
        builder.Property(item => item.Recipient).HasMaxLength(80);
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(item => item.ErrorCode).HasMaxLength(NotificationDelivery.ErrorCodeMax);
        builder.Property(item => item.FallbackFrom).HasConversion<string>().HasMaxLength(10);
        builder.Property(item => item.Source).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(item => item.BatchId).HasMaxLength(40);
        builder.Property(item => item.SentByStaffId).HasMaxLength(40);
        builder.Property(item => item.BranchId).HasMaxLength(40);

        // The log page reads newest first; the id breaks ties for the cursor.
        builder.HasIndex(item => new { item.CreatedAt, item.Id });

        // "What did we send this owner" and the per-batch answer of a send.
        builder.HasIndex(item => new { item.CustomerId, item.CreatedAt });
        builder.HasIndex(item => item.BatchId).HasFilter("batch_id IS NOT NULL");

        // The monthly broadcast count reads only sent broadcasts; the partial index is that set.
        builder.HasIndex(item => item.CreatedAt)
            .HasFilter("source = 'Broadcast' AND status = 'Sent'")
            .HasDatabaseName("ix_notification_deliveries_broadcasts_sent");

        // A customer's erasure takes their delivery history with it, as it does their inbox.
        builder.HasOne<Customer>().WithMany()
            .HasForeignKey(item => item.CustomerId).OnDelete(DeleteBehavior.Cascade);
    }
}
