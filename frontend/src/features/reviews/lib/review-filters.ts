import type { ReviewFilter } from '../types/review-list.types';
import type { PublicReview } from '../types/review.types';

/** Every filter, in the order the chips show them. */
export const REVIEW_FILTERS: readonly ReviewFilter[] = ['all', '5', '4', '3down', 'replied'];

/** Whether a review belongs under a filter. */
export function matchesReviewFilter(review: PublicReview, filter: ReviewFilter): boolean {
  switch (filter) {
    case '5':
      return review.rating === 5;
    case '4':
      return review.rating === 4;
    case '3down':
      return review.rating <= 3;
    case 'replied':
      return Boolean(review.reply);
    default:
      return true;
  }
}
