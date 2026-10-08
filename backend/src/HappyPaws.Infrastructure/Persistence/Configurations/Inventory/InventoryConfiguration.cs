using HappyPaws.Domain.Clinics;
using HappyPaws.Domain.Inventory;
using HappyPaws.Domain.Prescriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyPaws.Infrastructure.Persistence.Configurations.Inventory;

/// <summary>
/// The catalogue. Its one rule that has to live in the database is that a name, as matched, is
/// unique: a prescription must match at most one item, and two people creating the same drug at the
/// same moment cannot see each other's uncommitted row.
/// </summary>
internal sealed class InventoryItemConfiguration : EntityConfiguration<InventoryItem>
{
    /// <summary>Named, because <see cref="PostgresErrorTranslator"/> turns a violation into <c>inventory_item_exists</c>.</summary>
    public const string NameKeyIndex = "ux_inventory_items_name_key";

    protected override void ConfigureEntity(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("inventory_items", table =>
        {
            table.HasCheckConstraint("ck_inventory_items_price_not_negative", "unit_price_satang >= 0");
            table.HasCheckConstraint("ck_inventory_items_reorder_not_negative", "reorder_level >= 0");
        });

        builder.Property(item => item.Name).HasMaxLength(InventoryItem.NameMax).IsRequired();
        builder.Property(item => item.NameKey).HasMaxLength(InventoryItem.NameMax).IsRequired();
        builder.Property(item => item.Unit).HasMaxLength(InventoryItem.UnitMax).IsRequired();
        builder.Property(item => item.CreatedByStaffId).HasMaxLength(40).IsRequired();
        builder.Property(item => item.Kind).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(item => item.NameKey).IsUnique().HasDatabaseName(NameKeyIndex);

        // "ค้นหายา" — contains-search on the name, the same trigram index the customer search uses.
        builder.HasIndex(item => item.Name)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops")
            .HasDatabaseName("ix_inventory_items_name_trgm");
    }
}

/// <summary>
/// One lot on one shelf. The balance can never go below zero, whatever writes it — the ledger and
/// the lock keep it right, and this check is what makes "never negative" a property of the table.
/// </summary>
internal sealed class StockLotConfiguration : EntityConfiguration<StockLot>
{
    protected override void ConfigureEntity(EntityTypeBuilder<StockLot> builder)
    {
        builder.ToTable("stock_lots", table =>
            table.HasCheckConstraint("ck_stock_lots_quantity_not_negative", "quantity_on_hand >= 0"));

        builder.Property(lot => lot.ItemId).HasMaxLength(40).IsRequired();
        builder.Property(lot => lot.BranchId).HasMaxLength(40).IsRequired();
        builder.Property(lot => lot.LotNumber).HasMaxLength(StockLot.LotNumberMax);
        builder.Property(lot => lot.ExpiresOn).HasColumnType("date");

        // One row per lot per shelf. Nulls not distinct, so stock counted in without a lot number or
        // expiry lands in one lot rather than a new one each time.
        builder.HasIndex(lot => new { lot.ItemId, lot.BranchId, lot.LotNumber, lot.ExpiresOn })
            .IsUnique()
            .AreNullsDistinct(false)
            .HasDatabaseName("ux_stock_lots_item_branch_lot");

        // The alert list: what expires soonest at a branch.
        builder.HasIndex(lot => new { lot.BranchId, lot.ExpiresOn });

        builder.HasOne<InventoryItem>().WithMany()
            .HasForeignKey(lot => lot.ItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(lot => lot.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>The ledger. Rows are only ever inserted; see <see cref="StockMovement"/>.</summary>
internal sealed class StockMovementConfiguration : EntityConfiguration<StockMovement>
{
    protected override void ConfigureEntity(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movements");

        builder.Property(movement => movement.ItemId).HasMaxLength(40).IsRequired();
        builder.Property(movement => movement.BranchId).HasMaxLength(40).IsRequired();
        builder.Property(movement => movement.LotId).HasMaxLength(40).IsRequired();
        builder.Property(movement => movement.RecordedByStaffId).HasMaxLength(40).IsRequired();
        builder.Property(movement => movement.Reference).HasMaxLength(StockMovement.ReferenceMax);
        builder.Property(movement => movement.Note).HasMaxLength(StockMovement.NoteMax);
        builder.Property(movement => movement.PurchaseOrderId).HasMaxLength(40);
        builder.Property(movement => movement.PrescriptionId).HasMaxLength(40);
        builder.Property(movement => movement.TransferId).HasMaxLength(40);
        builder.Property(movement => movement.Kind).HasConversion<string>().HasMaxLength(20).IsRequired();

        // An item's history at a branch, newest first.
        builder.HasIndex(movement => new { movement.ItemId, movement.BranchId, movement.OccurredAt });

        // "Which lot did this prescription come out of" — for a recall.
        builder.HasIndex(movement => movement.PrescriptionId).HasFilter("prescription_id IS NOT NULL");

        builder.HasOne<StockLot>().WithMany()
            .HasForeignKey(movement => movement.LotId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryItem>().WithMany()
            .HasForeignKey(movement => movement.ItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(movement => movement.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PurchaseOrder>().WithMany()
            .HasForeignKey(movement => movement.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Prescription>().WithMany()
            .HasForeignKey(movement => movement.PrescriptionId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PurchaseOrderConfiguration : EntityConfiguration<PurchaseOrder>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("purchase_orders");

        builder.Property(order => order.Number).HasMaxLength(32).IsRequired();
        builder.Property(order => order.BranchId).HasMaxLength(40).IsRequired();
        builder.Property(order => order.SupplierName).HasMaxLength(PurchaseOrder.SupplierMax).IsRequired();
        builder.Property(order => order.Note).HasMaxLength(PurchaseOrder.NoteMax);
        builder.Property(order => order.OrderedByStaffId).HasMaxLength(40).IsRequired();
        builder.Property(order => order.ExpectedOn).HasColumnType("date");
        builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(order => order.Number).IsUnique();

        // "On order" on the stock list: open orders at a branch.
        builder.HasIndex(order => new { order.BranchId, order.Status });

        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(order => order.BranchId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(order => order.Lines)
            .WithOne()
            .HasForeignKey(line => line.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(order => order.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
    {
        builder.ToTable("purchase_order_lines", table =>
        {
            table.HasCheckConstraint("ck_purchase_order_lines_quantity_positive", "quantity > 0");
            table.HasCheckConstraint(
                "ck_purchase_order_lines_received_within_ordered",
                "received_quantity >= 0 AND received_quantity <= quantity");
        });

        builder.HasKey(line => line.Id);

        builder.Property(line => line.Id).HasMaxLength(40);
        builder.Property(line => line.PurchaseOrderId).HasMaxLength(40).IsRequired();
        builder.Property(line => line.ItemId).HasMaxLength(40).IsRequired();

        builder.HasIndex(line => line.ItemId);

        builder.HasOne<InventoryItem>().WithMany()
            .HasForeignKey(line => line.ItemId).OnDelete(DeleteBehavior.Restrict);
    }
}
