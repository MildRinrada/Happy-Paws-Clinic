import { reviewsPage } from '../../content/reviews-page';

/** How long a bar is, in percent. A score someone gave always shows at least a sliver. */
function barWidth(count: number, total: number): number {
  if (total <= 0 || count <= 0) {
    return 0;
  }
  return Math.max(Math.round((count / total) * 100), 3);
}

/** One row of the summary: "5 ดาว", a dark green bar as long as its share, and the count. */
export function RatingBar({ stars, count, total }: { stars: number; count: number; total: number }) {
  return (
    <li className="flex items-center gap-3 text-sm text-ink-700">
      <span aria-hidden="true" className="w-10 shrink-0">
        {reviewsPage.stars(stars)}
      </span>
      <span aria-hidden="true" className="h-2 min-w-0 flex-1 overflow-hidden rounded-full bg-ink-200">
        <span className="block h-full rounded-full bg-site-forest" style={{ width: `${barWidth(count, total)}%` }} />
      </span>
      <span aria-hidden="true" className="w-9 shrink-0 text-right tabular-nums">
        {count.toLocaleString('th-TH')}
      </span>
      <span className="sr-only">{reviewsPage.barLabel(stars, count)}</span>
    </li>
  );
}
