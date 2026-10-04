import { formatLongDate } from '@/lib/format/datetime';
import { portalPackages } from '../../content/portal-packages';
import type { PackageView } from '../../lib/load-packages';
import { PackageStatusBadge } from './package-status-badge';
import { PackageUsesMeter } from './package-uses-meter';

/** One package: its name and state, what is left, who and what it covers, and until when. */
export function PackageCard({ item }: { item: PackageView }) {
  const text = portalPackages;
  const finished = item.status !== 'active';

  return (
    <article className="space-y-3 rounded-2xl bg-white p-5 shadow-sm">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <h3 className="text-base font-semibold text-ink-900">{item.name}</h3>
          <p className="text-sm text-site-muted">{item.petName ? text.forPet(item.petName) : text.anyPet}</p>
        </div>
        <PackageStatusBadge status={item.status} />
      </div>

      <PackageUsesMeter remaining={item.remaining} total={item.total} muted={finished} />

      {item.serviceNames.length > 0 && <p className="text-sm text-ink-700">{text.services(item.serviceNames.join(', '))}</p>}

      <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-site-muted">
        <span className={item.expiresSoon ? 'font-semibold text-amber-800' : undefined}>
          {item.expiresOn ? text.expires(formatLongDate(item.expiresOn)) : text.noExpiry}
          {item.expiresSoon && ` (${text.expiresSoon})`}
        </span>
        <span>{text.purchased(formatLongDate(item.purchasedAt))}</span>
      </div>
    </article>
  );
}
