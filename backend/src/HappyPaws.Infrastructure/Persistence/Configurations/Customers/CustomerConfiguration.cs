using HappyPaws.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Customers;

internal sealed class CustomerConfiguration : EntityConfiguration<Customer>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");

        builder.Property(customer => customer.UserId).HasMaxLength(40);
        builder.Property(customer => customer.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(customer => customer.Phone).HasMaxLength(32).IsRequired();
        builder.Property(customer => customer.Email).HasMaxLength(256);
        builder.Property(customer => customer.MergedIntoCustomerId).HasMaxLength(40);

        // The counter's duplicate check ("ตรวจซ้ำด้วยเบอร์โทร") is a lookup, not a constraint — two
        // people in one household legitimately share a number, and the merge endpoint exists
        // precisely because duplicates happen.
        builder.HasIndex(customer => customer.Phone);

        // One portal account maps to at most one customer record in this instance. Partial, so the
        // many customers with no account yet do not all collide on null.
        builder.HasIndex(customer => customer.UserId)
            .IsUnique()
            .HasFilter("user_id IS NOT NULL");

        // Search reads live records only, so the filter it applies is the one indexed. No foreign
        // key to the survivor: a tombstone pointing at a row that was itself later merged is a
        // chain the application follows, not a constraint the database should enforce.
        builder.HasIndex(customer => customer.MergedIntoCustomerId)
            .HasFilter("merged_into_customer_id IS NOT NULL");

        // "ค้นด้วยชื่อ" runs ILIKE '%…%' on this column, which no b-tree can serve. Trigram is what
        // makes an infix match indexable; without it the counter's search box is a sequential scan
        // of every customer the clinic has ever had.
        builder.HasIndex(customer => customer.DisplayName)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops")
            .HasDatabaseName("ix_customers_display_name_trgm");
    }
}
