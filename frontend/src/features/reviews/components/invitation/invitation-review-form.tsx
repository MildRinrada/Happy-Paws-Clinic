'use client';

import { useState } from 'react';
import { submitInvitationReviewAction } from '../../actions/submit-review-actions';
import type { SubmittedReview } from '../../types/review.types';
import { ReviewDone } from '../shared/review-done';
import { ReviewForm } from '../shared/review-form';

/** The review form behind a one-time link, and the thanks once it is sent. */
export function InvitationReviewForm({ token }: { token: string }) {
  const [done, setDone] = useState<SubmittedReview>();

  if (done) {
    return <ReviewDone review={done} />;
  }

  return <ReviewForm send={(input) => submitInvitationReviewAction({ token, ...input })} onDone={setDone} />;
}
