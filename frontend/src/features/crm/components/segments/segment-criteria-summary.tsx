import { formatBaht } from '@/lib/format/money';
import { tierLabels } from '../../content/crm-labels';
import { segmentListText } from '../../content/segment-list';
import type { LoyaltyTier, Segment } from '../../types/crm.types';

/** The conditions of a group, one line each, in words. */
export function SegmentCriteriaSummary({ criteria, branchName }: { criteria: Segment['criteria']; branchName?: string }) {
  const text = segmentListText.criteria;
  const lines = [
    criteria.species?.length ? text.species(criteria.species.join(', ')) : null,
    criteria.inactive_days ? text.inactive(criteria.inactive_days) : null,
    criteria.visited_within_days ? text.within(criteria.visited_within_days) : null,
    criteria.min_spend_satang ? text.spend(formatBaht(criteria.min_spend_satang)) : null,
    criteria.tiers?.length ? text.tiers(criteria.tiers.map((tier) => tierLabels[tier as LoyaltyTier] ?? tier).join(', ')) : null,
    criteria.has_active_package != null ? text.package(criteria.has_active_package) : null,
    criteria.branch_id ? `${text.branch} (${branchName ?? criteria.branch_id})` : null,
  ].filter((line): line is string => line !== null);

  return (
    <ul className="list-inside list-disc space-y-1 text-sm text-ink-700">
      {lines.map((line) => (
        <li key={line}>{line}</li>
      ))}
    </ul>
  );
}
