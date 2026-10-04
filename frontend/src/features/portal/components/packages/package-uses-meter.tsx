import { portalPackages } from '../../content/portal-packages';

/** How much of the package is left, as a bar and in words. */
export function PackageUsesMeter({ remaining, total, muted }: { remaining: number; total: number; muted: boolean }) {
  const percent = total > 0 ? Math.round((remaining / total) * 100) : 0;

  return (
    <div className="space-y-1">
      <p className="text-sm font-semibold text-ink-900">{portalPackages.remaining(remaining, total)}</p>
      <div
        role="meter"
        aria-valuemin={0}
        aria-valuemax={total}
        aria-valuenow={remaining}
        aria-label={portalPackages.remaining(remaining, total)}
        className="h-2 overflow-hidden rounded-full bg-ink-100"
      >
        <div className={`h-full rounded-full ${muted ? 'bg-ink-300' : 'bg-site-teal'}`} style={{ width: `${percent}%` }} />
      </div>
    </div>
  );
}
