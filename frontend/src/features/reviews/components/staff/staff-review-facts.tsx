import { formatDateTime } from '@/lib/format/datetime';
import { staffReviewsText } from '../../content/staff-reviews';
import { formatVisitDay } from '../../lib/review-dates';
import type { StaffReview } from '../../types/review.types';

/**
 * Who wrote a review and about which visit, for staff only: the full name and the pet's name never
 * reach the public page, which shows the name below "แสดงบนเว็บว่า".
 */
export function StaffReviewFacts({ review }: { review: StaffReview }) {
  const text = staffReviewsText;
  const { visit, moderation } = review;

  return (
    <ul className="space-y-0.5 text-xs text-ink-600">
      <li className="break-words">
        {text.from(review.customer.name ?? review.customer.id)} | {text.shownAs(review.display_name)}
      </li>
      <li className="break-words">{text.visit(visit.pet_name ?? '', formatVisitDay(visit.visit_on), visit.branch_name ?? '')}</li>
      <li>
        {text.written(formatDateTime(review.created_at))} | {text.source[review.source]}
      </li>
      {moderation.moderated_at && moderation.moderated_by_staff_id && (
        <li className="break-words">{text.moderated(formatDateTime(moderation.moderated_at), moderation.moderated_by_staff_id)}</li>
      )}
      {moderation.reject_reason_label && <li className="text-ink-800">{text.rejectedFor(moderation.reject_reason_label)}</li>}
    </ul>
  );
}
