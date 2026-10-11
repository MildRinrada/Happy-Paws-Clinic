import { useId } from 'react';
import { SiteCard } from '@/components/site/site-card';
import { reviewsPage } from '../../content/reviews-page';
import type { ReviewSummary as Summary } from '../../types/review.types';
import { StarScore } from '../shared/star-score';
import { RatingBar } from './rating-bar';

/**
 * The grey card of figures: the average, its stars, how many reviews, and how the scores spread,
 * over published reviews only, as the API computes them. Drawn only when something is published:
 * an empty summary would say nothing honest.
 */
export function ReviewSummary({ summary }: { summary: Summary }) {
  const headingId = useId();
  const text = reviewsPage;

  if (summary.count === 0 || summary.average == null) {
    return null;
  }

  const average = summary.average.toLocaleString('th-TH', { minimumFractionDigits: 1, maximumFractionDigits: 1 });

  return (
    <SiteCard as="section" tone="muted" radius="xl" padding="md" ariaLabelledBy={headingId}>
      <h2 id={headingId} className="text-sm font-semibold text-ink-700">
        {text.average}
      </h2>
      <p className="mt-3 flex items-baseline gap-2">
        <span className="text-[48px] font-semibold leading-none tracking-tight text-site-forest">{average}</span>
        <span className="text-[15px] text-ink-600">{text.outOf}</span>
      </p>
      <div className="mt-4">
        <StarScore value={summary.average} label={text.averageLabel(average)} />
      </div>
      <p className="mt-2.5 text-sm text-ink-700">{text.count(summary.count)}</p>

      <ul className="mt-4 space-y-2">
        {[5, 4, 3, 2, 1].map((stars) => (
          <RatingBar key={stars} stars={stars} count={summary.by_rating[String(stars)] ?? 0} total={summary.count} />
        ))}
      </ul>
    </SiteCard>
  );
}
