namespace HappyPaws.Domain.Crm;

/// <summary>A customer's points as of one moment.</summary>
/// <param name="Balance">Points that can be spent now. Can be below zero only after a refund took back points already spent.</param>
/// <param name="Lifetime">Earned less reversed. The level is judged on this; spending, expiry and adjustments do not move it.</param>
/// <param name="Expired">Points that ran out unspent.</param>
/// <param name="NextExpiringPoints">How many spendable points expire soonest, or zero when none expire.</param>
/// <param name="NextExpiryAt">When those points expire, or null.</param>
public sealed record LoyaltyPosition(
    int Balance,
    int Lifetime,
    int Expired = 0,
    int NextExpiringPoints = 0,
    DateTimeOffset? NextExpiryAt = null);

/// <summary>
/// Works a customer's points out of the ledger, honouring expiry.
/// <para>
/// Every row that adds points is a parcel with its own expiry (or none). A row that takes points
/// away spends the parcels that are still alive at that moment, the soonest to expire first, and
/// parcels that never expire last. What a taking row cannot cover is a debt that the next points
/// added settle first: that is how a refund of points already spent shows. A parcel that reaches its
/// expiry with points left loses them.
/// </para>
/// <para>
/// When no row carries an expiry this is exactly the sum of the rows, which is what the ledger
/// always meant before expiry existed.
/// </para>
/// </summary>
public static class LoyaltyBalance
{
    public static LoyaltyPosition Compute(IEnumerable<LoyaltyEntry> entries, DateTimeOffset now)
    {
        var parcels = new List<Parcel>();
        var debt = 0;
        var lifetime = 0;

        foreach (var entry in entries.OrderBy(entry => entry.OccurredAt).ThenBy(entry => entry.Id, StringComparer.Ordinal))
        {
            if (entry.Kind is LoyaltyEntryKind.Earn or LoyaltyEntryKind.Reverse)
            {
                lifetime += entry.Points;
            }

            if (entry.Points > 0)
            {
                var added = entry.Points;
                var settled = Math.Min(debt, added);
                debt -= settled;
                added -= settled;

                if (added > 0)
                {
                    parcels.Add(new Parcel(added, entry.ExpiresAt));
                }

                continue;
            }

            var owed = -entry.Points;
            var at = entry.OccurredAt;

            foreach (var parcel in parcels
                .Where(parcel => parcel.Remaining > 0 && parcel.IsAliveAt(at))
                .OrderBy(parcel => parcel.ExpiresAt ?? DateTimeOffset.MaxValue))
            {
                var taken = Math.Min(owed, parcel.Remaining);
                parcel.Remaining -= taken;
                owed -= taken;

                if (owed == 0)
                {
                    break;
                }
            }

            debt += owed;
        }

        var alive = parcels.Where(parcel => parcel.Remaining > 0 && parcel.IsAliveAt(now)).ToList();
        var expired = parcels.Where(parcel => parcel.Remaining > 0 && !parcel.IsAliveAt(now)).Sum(parcel => parcel.Remaining);
        var next = alive.Where(parcel => parcel.ExpiresAt is not null).Min(parcel => parcel.ExpiresAt);
        var expiring = next is null ? 0 : alive.Where(parcel => parcel.ExpiresAt == next).Sum(parcel => parcel.Remaining);

        return new LoyaltyPosition(alive.Sum(parcel => parcel.Remaining) - debt, lifetime, expired, expiring, next);
    }

    private sealed class Parcel(int remaining, DateTimeOffset? expiresAt)
    {
        public int Remaining { get; set; } = remaining;

        public DateTimeOffset? ExpiresAt { get; } = expiresAt;

        public bool IsAliveAt(DateTimeOffset moment) => ExpiresAt is not { } expiry || expiry > moment;
    }
}
