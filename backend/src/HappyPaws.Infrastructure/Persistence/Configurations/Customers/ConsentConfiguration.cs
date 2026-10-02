using HappyPaws.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Customers;

/// <summary>
/// Consent rows.
/// <para>
/// No unique constraint on <c>(customer_id, purpose)</c>, and that is the point: the table holds
/// every decision ever taken, not the current one. The index below is the "what is the position
/// now" lookup, which runs in front of every outbound message.
/// </para>
/// </summary>
internal sealed class CustomerConsentConfiguration : EntityConfiguration<CustomerConsent>
{
    protected override void ConfigureEntity(EntityTypeBuilder<CustomerConsent> builder)
    {
        builder.ToTable("customer_consents");

        builder.Property(consent => consent.CustomerId).HasMaxLength(40).IsRequired();
        builder.Property(consent => consent.RecordedByUserId).HasMaxLength(40);
        builder.Property(consent => consent.PolicyVersion).HasMaxLength(40);
        builder.Property(consent => consent.Note).HasMaxLength(1_000);

        builder.Property(consent => consent.Purpose)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(consent => consent.Source)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.HasIndex(consent => new { consent.CustomerId, consent.Purpose, consent.RecordedAt })
            .IsDescending(false, false, true);

        builder.HasOne<Customer>().WithMany()
            .HasForeignKey(consent => consent.CustomerId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class DataSubjectRequestConfiguration : EntityConfiguration<DataSubjectRequest>
{
    protected override void ConfigureEntity(EntityTypeBuilder<DataSubjectRequest> builder)
    {
        builder.ToTable("data_subject_requests");

        builder.Property(request => request.CustomerId).HasMaxLength(40).IsRequired();
        builder.Property(request => request.ClosedByUserId).HasMaxLength(40);
        builder.Property(request => request.Detail).HasMaxLength(4_000);
        builder.Property(request => request.Resolution).HasMaxLength(4_000);

        builder.Property(request => request.Type)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(request => request.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        // The worklist: outstanding requests, oldest first.
        builder.HasIndex(request => new { request.Status, request.RaisedAt });
        builder.HasIndex(request => request.CustomerId);

        builder.HasOne<Customer>().WithMany()
            .HasForeignKey(request => request.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}
