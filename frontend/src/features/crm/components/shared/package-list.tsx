import { formatDay } from '@/features/inventory/lib/format-day';
import { packageText } from '../../content/crm-labels';
import type { CrmPackage } from '../../types/crm.types';
import { PackageStatusBadge } from './package-status-badge';

/** A customer's packages: name and state, uses left of the total, and until when. */
export function PackageList({ packages, emptyText }: { packages: CrmPackage[]; emptyText: string }) {
  if (packages.length === 0) {
    return <p className="text-sm text-ink-500">{emptyText}</p>;
  }

  return (
    <ul className="divide-y divide-ink-100 text-sm">
      {packages.map((item) => (
        <li key={item.id} className="space-y-0.5 py-2">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <span className="font-medium text-ink-900">{item.name}</span>
            <PackageStatusBadge status={item.status} />
          </div>
          <p className="text-ink-600">
            <span className="tabular-nums">{packageText.uses(item.remaining, item.total_uses)}</span>
            <span className="ml-3 text-xs text-ink-500">
              {item.expires_on ? packageText.expires(formatDay(item.expires_on)) : packageText.noExpiry}
            </span>
          </p>
        </li>
      ))}
    </ul>
  );
}
