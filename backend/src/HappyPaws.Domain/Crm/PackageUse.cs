using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Crm;

/// <summary>
/// Uses of one package spent on one bill line — "ตัดสิทธิ์อัตโนมัติเมื่อใช้".
/// <para>
/// Kept as rows rather than only as <see cref="CustomerPackage.UsedCount"/> so a voided bill can
/// give back exactly what it took, and so the counter can say which visit used which session.
/// A released row stays, with <see cref="ReleasedAt"/> set.
/// </para>
/// </summary>
public sealed class PackageUse : Entity
{
    private PackageUse()
    {
    }

    public string PackageId { get; private set; } = string.Empty;

    public string CustomerId { get; private set; } = string.Empty;

    public string InvoiceId { get; private set; } = string.Empty;

    /// <summary>The zero-priced line the use was written as.</summary>
    public string InvoiceLineId { get; private set; } = string.Empty;

    public string ServiceId { get; private set; } = string.Empty;

    public string? PetId { get; private set; }

    public int Quantity { get; private set; }

    public DateTimeOffset UsedAt { get; private set; }

    /// <summary>When the bill was voided and the uses returned to the package.</summary>
    public DateTimeOffset? ReleasedAt { get; private set; }

    public static PackageUse Record(
        CustomerPackage package,
        string invoiceId,
        string invoiceLineId,
        string serviceId,
        string? petId,
        int quantity,
        DateTimeOffset usedAt) =>
        new()
        {
            Id = EntityId.New(EntityId.PackageUse),
            PackageId = package.Id,
            CustomerId = package.CustomerId,
            InvoiceId = invoiceId,
            InvoiceLineId = invoiceLineId,
            ServiceId = serviceId,
            PetId = petId,
            Quantity = quantity,
            UsedAt = usedAt,
        };

    public void Release(DateTimeOffset now) => ReleasedAt ??= now;
}
