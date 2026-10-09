using HappyPaws.Domain.Customers;
using HappyPaws.Domain.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Messaging;

/// <summary><c>message_threads</c>: one row per customer who has written to the clinic.</summary>
internal sealed class MessageThreadConfiguration : EntityConfiguration<MessageThread>
{
    protected override void ConfigureEntity(EntityTypeBuilder<MessageThread> builder)
    {
        builder.ToTable("message_threads");

        builder.Property(thread => thread.CustomerId).HasMaxLength(40).IsRequired();
        builder.Property(thread => thread.BranchId).HasMaxLength(40);
        builder.Property(thread => thread.LastMessageAt).IsRequired();
        builder.Property(thread => thread.LastInboundAt);
        builder.Property(thread => thread.LastInboundChannel).HasConversion<string>().HasMaxLength(10);
        builder.Property(thread => thread.LastReplyAt);

        builder.Ignore(thread => thread.AwaitingReply);

        // One conversation per customer, whatever the channel. Two first messages racing to open it
        // meet here, and the loser appends to the winner's thread.
        builder.HasIndex(thread => thread.CustomerId).IsUnique();

        // The inbox's order.
        builder.HasIndex(thread => thread.LastMessageAt);
        builder.HasIndex(thread => thread.BranchId);

        builder.HasOne<Customer>().WithMany()
            .HasForeignKey(thread => thread.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}
