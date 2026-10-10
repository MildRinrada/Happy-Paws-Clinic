'use client';

import { submitPortalReviewAction } from '../../actions/submit-review-actions';
import type { SubmittedReview } from '../../types/review.types';
import { ReviewForm } from '../shared/review-form';

interface PortalReviewFormProps {
  visitId: string;
  firstName?: string | null;
  onDone: (review: SubmittedReview) => void;
}

/** The review form for one of the signed-in owner's visits. */
export function PortalReviewForm({ visitId, firstName, onDone }: PortalReviewFormProps) {
  return (
    <ReviewForm
      firstName={firstName}
      send={(input) => submitPortalReviewAction({ visit_id: visitId, ...input })}
      onDone={onDone}
    />
  );
}
