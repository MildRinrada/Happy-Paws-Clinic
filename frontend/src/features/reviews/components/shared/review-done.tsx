import { reviewForm } from '../../content/review-form';
import type { SubmittedReview } from '../../types/review.types';
import { StarRow } from './star-row';

/** After sending: thanks, and that it shows on the site only once staff have read it. */
export function ReviewDone({ review }: { review: SubmittedReview }) {
  return (
    <div role="status" className="space-y-2 rounded-2xl bg-emerald-50 px-5 py-4 text-sm text-emerald-900">
      <p className="text-base font-semibold">{reviewForm.doneTitle}</p>
      <StarRow rating={review.rating} />
      <p>{reviewForm.doneBody}</p>
    </div>
  );
}
