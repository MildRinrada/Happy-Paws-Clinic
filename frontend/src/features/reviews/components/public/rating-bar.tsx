import { reviewsPage } from '../../content/reviews-page';

/** One row of the summary: "5 ดาว", a bar as long as its share, and the count. */
export function RatingBar({ stars, count, total }: { stars: number; count: number; total: number }) {
  const share = total > 0 ? Math.round((count / total) * 100) : 0;

  return (
    <li className="flex items-center gap-3 text-sm text-ink-700">
      <span aria-hidden="true" className="w-12 shrink-0">
        {reviewsPage.stars(stars)}
      </span>
      <span aria-hidden="true" className="h-2 min-w-0 flex-1 overflow-hidden rounded-full bg-ink-100">
        <span className="block h-full rounded-full bg-amber-400" style={{ width: `${share}%` }} />
      </span>
      <span aria-hidden="true" className="w-10 shrink-0 text-right tabular-nums">
        {count.toLocaleString('th-TH')}
      </span>
      <span className="sr-only">{reviewsPage.barLabel(stars, count)}</span>
    </li>
  );
}
