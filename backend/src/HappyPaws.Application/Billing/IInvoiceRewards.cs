using HappyPaws.Domain.Billing;

namespace HappyPaws.Application.Billing;

/// <summary>A catalogue service about to be put on a bill.</summary>
/// <param name="ServiceId">The catalogue service (<c>svc_...</c>).</param>
/// <param name="Description">What the line says.</param>
/// <param name="Quantity">How many.</param>
/// <param name="UnitPriceSatang">The price per unit the customer would pay without a package.</param>
/// <param name="SourceId">The treatment or service row the line comes from.</param>
public sealed record ServiceLineRequest(
    string ServiceId,
    string Description,
    int Quantity,
    long UnitPriceSatang,
    string? SourceId);

/// <summary>
/// What packages, coupons and loyalty points do to a bill. Implemented by the CRM module
/// (<c>Application/Crm/InvoiceRewards.cs</c>).
/// <para>
/// A port in Billing rather than calls into CRM from every billing handler, so the cash desk has
/// one seam to read: each method runs inside the caller's transaction, so a bill and the packages,
/// coupons and points it moved land together or not at all.
/// </para>
/// </summary>
public interface IInvoiceRewards
{
    /// <summary>
    /// Adds a catalogue service to a bill being raised. The quantity the customer's packages cover
    /// goes on as lines priced zero and spends their uses; the rest goes on at the given price.
    /// </summary>
    Task AddServiceLineAsync(Invoice invoice, ServiceLineRequest line, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>Takes a coupon off the bill as a discount and counts the use.</summary>
    Task<InvoiceDiscount> RedeemCouponAsync(
        Invoice invoice,
        string couponCode,
        string staffId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    /// <summary>Spends the customer's points as a discount on the bill.</summary>
    Task<InvoiceDiscount> RedeemPointsAsync(
        Invoice invoice,
        int points,
        string staffId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    /// <summary>After a payment: earns points and activates a package once the bill is <c>paid</c>.</summary>
    Task PaymentCollectedAsync(Invoice invoice, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>After a refund: takes back points for the money returned and cancels a package sold on the bill.</summary>
    Task RefundedAsync(Invoice invoice, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>After a void: gives back package uses, coupon uses and spent points, and cancels a package sold on the bill.</summary>
    Task VoidedAsync(Invoice invoice, DateTimeOffset now, CancellationToken cancellationToken = default);
}
