import { portalPackages } from '../../content/portal-packages';
import type { PackageStatus } from '../../types/package.types';

const tones: Record<PackageStatus, string> = {
  pending_payment: 'bg-amber-50 text-amber-800',
  active: 'bg-brand-50 text-site-teal',
  used_up: 'bg-ink-100 text-ink-700',
  expired: 'bg-red-50 text-red-700',
  cancelled: 'bg-ink-100 text-ink-700',
};

/** Whether the package can still be used. */
export function PackageStatusBadge({ status }: { status: PackageStatus }) {
  return (
    <span className={`shrink-0 rounded-full px-2.5 py-1 text-xs font-semibold ${tones[status]}`}>
      {portalPackages.status[status]}
    </span>
  );
}
