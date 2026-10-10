import { reviewsPage } from '../../content/reviews-page';
import { formatReviewDate } from '../../lib/review-dates';
import type { PublicReview } from '../../types/review.types';
import { StarRow } from '../shared/star-row';
import { ReviewReply } from './review-reply';

/**
 * One published review, in the look the home page's testimonials always had: the owner's words in
 * quote marks, then who they are. Who they are is a first name or "ลูกค้า" and the pet's species,
 * nothing more. The words are drawn as text, so nothing in them can run.
 */
export function ReviewCard({ review }: { review: PublicReview }) {
  return (
    <li className="rounded-2xl bg-site-soft px-[22px] py-6 text-site-muted">
      <figure>
        <div className="flex items-center justify-between gap-3">
          <StarRow rating={review.rating} />
          <span className="text-xs">{formatReviewDate(review.created_at)}</span>
        </div>

        {review.text && (
          <blockquote className="mt-3 whitespace-pre-line break-words text-[15px] leading-[19px]">
            {`“${review.text}”`}
          </blockquote>
        )}

        <figcaption className="mt-4 flex items-center gap-3.5 text-[15px]">
          <span aria-hidden="true" className="size-10 shrink-0 rounded-full bg-site-orange" />
          <span className="min-w-0 break-words">
            {review.display_name} | {reviewsPage.owner(review.pet_species)}
          </span>
        </figcaption>

        {review.reply && <ReviewReply reply={review.reply} />}
      </figure>
    </li>
  );
}
