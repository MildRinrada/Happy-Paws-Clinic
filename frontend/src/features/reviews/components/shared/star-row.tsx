import { reviewsPage } from '../../content/reviews-page';
import { StarGlyph } from './star-glyph';

export type StarTone = 'amber' | 'orange';

const filled: Record<StarTone, string> = {
  // The home page's review cards and the forms.
  amber: 'fill-amber-400',
  // The deeper orange of the reviews page's design.
  orange: 'fill-stat-orange',
};

/** A score as five stars, filled up to the rating. Read aloud as "4 จาก 5 ดาว". */
export function StarRow({ rating, size = 'sm', tone = 'amber' }: { rating: number; size?: 'sm' | 'lg'; tone?: StarTone }) {
  const box = size === 'lg' ? 'size-6' : 'size-4';

  return (
    <span role="img" aria-label={reviewsPage.ratingLabel(rating)} className="inline-flex items-center gap-0.5">
      {[1, 2, 3, 4, 5].map((star) => (
        <StarGlyph key={star} className={`${box} ${star <= rating ? filled[tone] : 'fill-ink-200'}`} />
      ))}
    </span>
  );
}
