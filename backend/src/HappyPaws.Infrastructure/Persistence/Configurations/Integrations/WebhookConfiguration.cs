using HappyPaws.Domain.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Integrations;

/// <summary>
/// Outbound webhooks. The signing secret is <c>secret_ciphertext</c>: encrypted with the API's
/// Data Protection keys, which are not in this database.
/// </summary>
internal sealed class WebhookSubscriptionConfiguration : EntityConfiguration<WebhookSubscription>
{
    protected override void ConfigureEntity(EntityTypeBuilder<WebhookSubscription> builder)
    {
        builder.ToTable("webhook_subscriptions");

        builder.Property(subscription => subscription.Url).HasMaxLength(WebhookUrl.MaxLength).IsRequired();
        builder.Property(subscription => subscription.Description).HasMaxLength(WebhookSubscription.DescriptionMaxLength);
        builder.Property(subscription => subscription.SecretCiphertext).HasMaxLength(1_000).IsRequired();
        builder.Property(subscription => subscription.Active).IsRequired();
        builder.Property(subscription => subscription.CreatedByStaffId).HasMaxLength(40).IsRequired();

        builder.PrimitiveCollection(subscription => subscription.Events)
            .HasField("_events")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("events")
            .IsRequired();
    }
}

/// <summary>
/// The outbox. <c>body</c> is <c>text</c>, not <c>jsonb</c>: it is signed, and jsonb would hand
/// back different bytes from the ones that were stored.
/// </summary>
internal sealed class WebhookEventConfiguration : EntityConfiguration<WebhookEvent>
{
    protected override void ConfigureEntity(EntityTypeBuilder<WebhookEvent> builder)
    {
        builder.ToTable("webhook_events");

        builder.Property(webhookEvent => webhookEvent.Type).HasMaxLength(60).IsRequired();
        builder.Property(webhookEvent => webhookEvent.Body).HasColumnType("text").IsRequired();
        builder.Property(webhookEvent => webhookEvent.OccurredAt).IsRequired();

        // The job's queue: what has not been fanned out yet, oldest first. Partial, because
        // dispatched rows are the vast majority and the job never reads them again.
        builder.HasIndex(webhookEvent => webhookEvent.OccurredAt)
            .HasFilter("dispatched_at IS NULL")
            .HasDatabaseName("ix_webhook_events_undispatched");
    }
}

/// <summary>One event to one webhook. Deleted with its event, and with its webhook.</summary>
internal sealed class WebhookDeliveryConfiguration : EntityConfiguration<WebhookDelivery>
{
    protected override void ConfigureEntity(EntityTypeBuilder<WebhookDelivery> builder)
    {
        builder.ToTable("webhook_deliveries");

        builder.Property(delivery => delivery.SubscriptionId).HasMaxLength(40).IsRequired();
        builder.Property(delivery => delivery.EventId).HasMaxLength(40).IsRequired();
        builder.Property(delivery => delivery.EventType).HasMaxLength(60).IsRequired();
        builder.Property(delivery => delivery.Status).HasConversion<string>().HasMaxLength(12).IsRequired();
        builder.Property(delivery => delivery.Attempts).IsRequired();
        builder.Property(delivery => delivery.LastError).HasMaxLength(WebhookDelivery.ErrorMaxLength);
        builder.Property(delivery => delivery.LastResponseBody).HasMaxLength(WebhookDelivery.ResponseBodyMaxLength);

        builder.HasOne<WebhookSubscription>()
            .WithMany()
            .HasForeignKey(delivery => delivery.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<WebhookEvent>()
            .WithMany()
            .HasForeignKey(delivery => delivery.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // One delivery per event per webhook, whatever the fan-out does.
        builder.HasIndex(delivery => new { delivery.EventId, delivery.SubscriptionId }).IsUnique();

        // The sender's queue: pending rows by when they are due.
        builder.HasIndex(delivery => delivery.NextAttemptAt)
            .HasFilter("status = 'Pending'")
            .HasDatabaseName("ix_webhook_deliveries_due");

        // The delivery log of one webhook, newest first.
        builder.HasIndex(delivery => new { delivery.SubscriptionId, delivery.CreatedAt, delivery.Id });
    }
}
