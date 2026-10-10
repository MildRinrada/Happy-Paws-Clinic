import { reviewsPage } from '../../content/reviews-page';
import type { ReviewSummary as Summary } from '../../types/review.types';
import { StarRow } from '../shared/star-row';
import { RatingBar } from './rating-bar';

/**
 * The average and how the scores spread, over published reviews only, as the API computes them.
 * Drawn only when something is published: an empty summary would say nothing honest.
 */
export function ReviewSummary({ summary }: { summary: Summary }) {
  const text = reviewsPage;

  if (summary.count === 0 || summary.average == null) {
    return null;
  }

  return (
    <section aria-label={text.average} className="grid gap-6 rounded-2xl bg-site-soft p-6 sm:grid-cols-[auto_1fr] sm:items-center">
      <div className="text-center sm:pr-6">
        <p className="text-sm text-site-muted">{text.average}</p>
        <p className="mt-1 text-4xl font-semibold text-ink-900">
          {summary.average.toLocaleString('th-TH', { minimumFractionDigits: 1, maximumFractionDigits: 1 })}
          <span className="ml-1 text-base font-normal text-site-muted">{text.outOf}</span>
        </p>
        <div className="mt-2 flex justify-center">
          <StarRow rating={Math.round(summary.average)} size="lg" />
        </div>
        <p className="mt-1 text-sm text-site-muted">{text.count(summary.count)}</p>
      </div>

      <ul className="space-y-2">
        {[5, 4, 3, 2, 1].map((stars) => (
          <RatingBar key={stars} stars={stars} count={summary.by_rating[String(stars)] ?? 0} total={summary.count} />
        ))}
      </ul>
    </section>
  );
}
