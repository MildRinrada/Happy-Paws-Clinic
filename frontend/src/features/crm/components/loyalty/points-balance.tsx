import { loyaltyPage } from '../../content/loyalty-page';
import { formatWholeBaht } from '../../lib/whole-baht';
import type { CustomerLoyalty } from '../../types/crm.types';

/** How many points the customer can spend now, and what they are worth off a bill. */
export function PointsBalance({ loyalty }: { loyalty: CustomerLoyalty }) {
  const text = loyaltyPage.summary;

  return (
    <div className="space-y-1">
      <p className="text-sm text-ink-600">{text.balance}</p>
      <p className="font-display text-3xl font-semibold tabular-nums text-ink-900">{text.points(loyalty.points_balance)}</p>
      <p className="text-sm text-ink-700">{text.worth(formatWholeBaht(loyalty.points_balance * loyalty.rules.point_value_satang))}</p>
      <p className="text-xs text-ink-500">{text.lifetime(loyalty.lifetime_points)}</p>
    </div>
  );
}
