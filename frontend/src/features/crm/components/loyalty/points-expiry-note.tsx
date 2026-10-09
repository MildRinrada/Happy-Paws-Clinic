import { formatDay } from '@/features/inventory/lib/format-day';
import { clinicDay } from '@/lib/format/datetime';
import { pointsExpiryText } from '../../content/points-adjust';
import type { CustomerLoyalty } from '../../types/crm.types';

/** Which points expire next and when, what has already run out, and the rule for points given now. */
export function PointsExpiryNote({ loyalty }: { loyalty: CustomerLoyalty }) {
  const text = pointsExpiryText;
  const months = loyalty.rules.points_expire_after_months;
  const next = loyalty.next_expiry;

  if (!next && !loyalty.expired_points && !months) {
    return null;
  }

  return (
    <ul className="space-y-0.5 text-xs text-ink-600">
      {next && <li className="text-amber-800">{text.next(next.points, formatDay(clinicDay(next.expires_at)))}</li>}
      {loyalty.expired_points > 0 && <li>{text.expired(loyalty.expired_points)}</li>}
      {months && <li>{text.rule(months)}</li>}
    </ul>
  );
}
