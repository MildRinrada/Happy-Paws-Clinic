import type { PublicReview } from '../../types/review.types';
import { ReviewCard } from './review-card';

/** Published reviews, one card each, in the order the API sends them (newest first). */
export function ReviewCardList({ reviews, className = 'mt-7' }: { reviews: PublicReview[]; className?: string }) {
  return (
    <ul className={`space-y-[18px] ${className}`}>
      {reviews.map((review) => (
        <ReviewCard key={review.id} review={review} />
      ))}
    </ul>
  );
}
