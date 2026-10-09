using HappyPaws.Domain.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Messaging;

/// <summary><c>thread_messages</c>: what customers wrote and what staff answered.</summary>
internal sealed class ThreadMessageConfiguration : EntityConfiguration<ThreadMessage>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ThreadMessage> builder)
    {
        builder.ToTable("thread_messages");

        builder.Property(message => message.ThreadId).HasMaxLength(40).IsRequired();
        builder.Property(message => message.Direction).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(message => message.Channel).HasConversion<string>().HasMaxLength(10);
        builder.Property(message => message.Kind).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(message => message.Text).HasMaxLength(ThreadMessage.InboundTextMax);
        builder.Property(message => message.ExternalId).HasMaxLength(ThreadMessage.ExternalIdMax);
        builder.Property(message => message.SentByStaffId).HasMaxLength(40);
        builder.Property(message => message.SentAt).IsRequired();
        builder.Property(message => message.DeliveryStatus).HasConversion<string>().HasMaxLength(10);
        builder.Property(message => message.DeliveryErrorCode).HasMaxLength(60);
        builder.Property(message => message.FallbackFrom).HasConversion<string>().HasMaxLength(10);

        // A conversation is read newest first.
        builder.HasIndex(message => new { message.ThreadId, message.SentAt });

        // A webhook LINE delivers twice is stored once: the channel's own message id is unique.
        builder.HasIndex(message => new { message.Channel, message.ExternalId })
            .IsUnique()
            .HasFilter("external_id IS NOT NULL");

        builder.HasOne<MessageThread>().WithMany()
            .HasForeignKey(message => message.ThreadId).OnDelete(DeleteBehavior.Restrict);
    }
}
