import { Badge } from '@/components/ui/badge';
import { tierLabels, tierTones } from '../../content/crm-labels';
import { loyaltyPage } from '../../content/loyalty-page';
import { tierProgressPercent } from '../../lib/tier-progress';
import { formatWholeBaht } from '../../lib/whole-baht';
import type { CustomerLoyalty } from '../../types/crm.types';

/** The membership level, how close the next one is, and the rules the points follow. */
export function TierProgress({ loyalty }: { loyalty: CustomerLoyalty }) {
  const text = loyaltyPage.summary;
  const percent = tierProgressPercent(loyalty);
  const next = loyalty.next_tier;

  return (
    <div className="space-y-2">
      <p className="text-sm text-ink-600">{text.tier}</p>
      <Badge tone={tierTones[loyalty.tier]}>{tierLabels[loyalty.tier]}</Badge>

      {next && percent !== null ? (
        <div className="space-y-1">
          <div
            role="meter"
            aria-valuemin={0}
            aria-valuemax={100}
            aria-valuenow={percent}
            aria-label={text.toNext(next.points_needed, tierLabels[next.tier])}
            className="h-2 overflow-hidden rounded-full bg-ink-100"
          >
            <div className="h-full rounded-full bg-brand-500" style={{ width: `${percent}%` }} />
          </div>
          <p className="text-sm text-ink-700">{text.toNext(next.points_needed, tierLabels[next.tier])}</p>
        </div>
      ) : (
        <p className="text-sm text-ink-700">{text.top}</p>
      )}

      <p className="text-xs text-ink-500">
        {text.rules(formatWholeBaht(loyalty.rules.satang_per_point), formatWholeBaht(loyalty.rules.point_value_satang))}
      </p>
    </div>
  );
}
