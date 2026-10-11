import type { PublicReview } from '../../types/review.types';
import { ReviewEntry } from './review-entry';

/** The reviews page's cards, in the order the list came (newest first). */
export function ReviewEntryList({ reviews }: { reviews: PublicReview[] }) {
  return (
    <ul className="space-y-[18px]">
      {reviews.map((review) => (
        <ReviewEntry key={review.id} review={review} />
      ))}
    </ul>
  );
}
