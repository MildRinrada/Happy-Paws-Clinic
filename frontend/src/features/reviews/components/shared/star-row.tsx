import { reviewsPage } from '../../content/reviews-page';

/** A score as five stars, filled up to the rating. Read aloud as "4 จาก 5 ดาว". */
export function StarRow({ rating, size = 'sm' }: { rating: number; size?: 'sm' | 'lg' }) {
  const box = size === 'lg' ? 'size-6' : 'size-4';

  return (
    <span role="img" aria-label={reviewsPage.ratingLabel(rating)} className="inline-flex items-center gap-0.5">
      {[1, 2, 3, 4, 5].map((star) => (
        <svg
          key={star}
          aria-hidden="true"
          viewBox="0 0 20 20"
          className={`${box} ${star <= rating ? 'fill-amber-400' : 'fill-ink-200'}`}
        >
          <path d="M10 1.5l2.6 5.3 5.9.9-4.3 4.1 1 5.8L10 14.9l-5.2 2.7 1-5.8L1.5 7.7l5.9-.9L10 1.5z" />
        </svg>
      ))}
    </span>
  );
}
