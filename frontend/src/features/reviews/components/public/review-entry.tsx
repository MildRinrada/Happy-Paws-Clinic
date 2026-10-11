import { SiteCard } from '@/components/site/site-card';
import { formatReviewDate } from '../../lib/review-dates';
import type { PublicReview } from '../../types/review.types';
import { StarRow } from '../shared/star-row';
import { ReviewByline } from './review-byline';
import { ReviewEntryReply } from './review-entry-reply';

/**
 * One published review on the reviews page: the stars and the date, the owner's words, who wrote
 * it, and the clinic's answer when there is one. A review with no words is the stars and who
 * wrote it. The words are drawn as text, so nothing in them can run.
 */
export function ReviewEntry({ review }: { review: PublicReview }) {
  return (
    <SiteCard as="li" tone="whiteFaint" radius="xl" padding="md">
      <figure>
        <div className="flex items-center justify-between gap-3">
          <StarRow rating={review.rating} tone="orange" />
          <time dateTime={review.created_at} className="shrink-0 text-[13px] text-ink-600">
            {formatReviewDate(review.created_at)}
          </time>
        </div>

        {review.text && (
          <blockquote className="mt-3 whitespace-pre-line break-words text-[15px] leading-relaxed text-ink-800">
            {review.text}
          </blockquote>
        )}

        <div className="mt-3">
          <ReviewByline review={review} />
        </div>

        {review.reply && <ReviewEntryReply reply={review.reply} />}
      </figure>
    </SiteCard>
  );
}
