'use client';

import { useState } from 'react';
import { staffReviewsText } from '../../content/staff-reviews';
import type { StaffReview } from '../../types/review.types';
import { StarRow } from '../shared/star-row';
import { PublishReviewButton } from './publish-review-button';
import { RejectReviewForm } from './reject-review-form';
import { ReplyReviewForm } from './reply-review-form';
import { ReviewStatusBadge } from './review-status-badge';
import { StaffReviewFacts } from './staff-review-facts';

/**
 * One review on the moderation page: the score and the words exactly as the customer wrote them
 * (there is no way to edit them), who wrote it, and what staff may do with it now.
 */
export function StaffReviewEntry({ initial }: { initial: StaffReview }) {
  const [review, setReview] = useState(initial);
  const rejected = review.status === 'rejected';

  return (
    <li className="space-y-3 py-4">
      <div className="flex flex-wrap items-center gap-3">
        <StarRow rating={review.rating} />
        <ReviewStatusBadge status={review.status} />
      </div>

      <p className="whitespace-pre-line break-words text-sm text-ink-900">{review.text ?? staffReviewsText.noText}</p>

      <StaffReviewFacts review={review} />

      {!rejected && (
        <div className="flex flex-wrap items-start gap-3">
          {review.status === 'pending' && <PublishReviewButton review={review} onChange={setReview} />}
          <RejectReviewForm key={review.version} review={review} onChange={setReview} />
        </div>
      )}

      {!rejected && <ReplyReviewForm key={`reply-${review.version}`} review={review} onChange={setReview} />}
    </li>
  );
}
