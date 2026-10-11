import { reviewsPage } from '../../content/reviews-page';
import type { PublicReview } from '../../types/review.types';

/**
 * Who wrote it, under the words: the first name or "ลูกค้า" in bold, then the pet's species and,
 * when the API sends it, the service. Nothing that would say who the owner is.
 */
export function ReviewByline({ review }: { review: PublicReview }) {
  return (
    <figcaption className="break-words text-sm text-ink-600">
      <span className="font-semibold text-ink-900">{review.display_name}</span> {reviewsPage.owner(review.pet_species)}
      {review.service_name && ` ${review.service_name}`}
    </figcaption>
  );
}
