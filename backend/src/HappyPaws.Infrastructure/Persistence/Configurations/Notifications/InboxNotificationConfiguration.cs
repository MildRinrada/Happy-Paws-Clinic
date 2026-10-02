using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Notifications;

internal sealed class InboxNotificationConfiguration : EntityConfiguration<InboxNotification>
{
    protected override void ConfigureEntity(EntityTypeBuilder<InboxNotification> builder)
    {
        builder.ToTable("inbox_notifications");

        builder.Property(item => item.CustomerId).HasMaxLength(40).IsRequired();
        builder.Property(item => item.TemplateKey).HasMaxLength(60).IsRequired();
        builder.Property(item => item.Title).HasMaxLength(200).IsRequired();
        builder.Property(item => item.Body).HasMaxLength(1_000).IsRequired();
        builder.Property(item => item.ReferenceId).HasMaxLength(40);

        builder.HasIndex(item => new { item.CustomerId, item.CreatedAt });
        builder.HasIndex(item => item.CustomerId).HasFilter("read_at IS NULL");

        builder.HasOne<Customer>().WithMany()
            .HasForeignKey(item => item.CustomerId).OnDelete(DeleteBehavior.Cascade);
    }
}
