import { askPage } from '../../content/ask-page';

/** Used out of the limit, as a bar and in words. The bar turns amber near the end of the allowance. */
export function UsageMeter({ used, limit }: { used: number; limit: number }) {
  const share = limit > 0 ? Math.min(1, used / limit) : 1;

  return (
    <div className="space-y-1.5">
      <p className="text-sm font-medium text-ink-900">{askPage.usage.used(used, limit)}</p>
      <div
        className="h-2 overflow-hidden rounded-full bg-ink-100"
        role="progressbar"
        aria-valuemin={0}
        aria-valuemax={limit}
        aria-valuenow={used}
        aria-label={askPage.usage.title}
      >
        <div className={`h-full ${share >= 0.9 ? 'bg-amber-500' : 'bg-brand-600'}`} style={{ width: `${Math.round(share * 100)}%` }} />
      </div>
    </div>
  );
}
