import type { CustomerLoyalty } from '../types/crm.types';

/**
 * How far along the way to the next level the customer is, 0 to 100, read from the backend's own
 * figures: the next level's threshold, and how many points it says are still needed. `null` at the
 * top level.
 */
export function tierProgressPercent(loyalty: CustomerLoyalty): number | null {
  if (!loyalty.next_tier) return null;

  const floor = loyalty.rules.tiers.find((tier) => tier.tier === loyalty.tier)?.min_points ?? 0;
  const ceiling = loyalty.rules.tiers.find((tier) => tier.tier === loyalty.next_tier?.tier)?.min_points;

  if (ceiling === undefined || ceiling <= floor) return 0;

  const reached = ceiling - loyalty.next_tier.points_needed - floor;

  return Math.min(100, Math.max(0, Math.round((reached / (ceiling - floor)) * 100)));
}
