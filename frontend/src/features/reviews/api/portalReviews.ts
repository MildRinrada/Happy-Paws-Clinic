import { staffApi } from '@/lib/api/server-client';
import type { PendingReviews, ReviewInput, SubmittedReview } from '../types/review.types';

/**
 * The signed-in owner's reviews of their own visits (docs/contract-extensions.md § 36). The
 * customer comes from the session; nothing here can name anyone else.
 */

/** `GET /portal/reviews/pending`: visits of the last 60 days with no review yet. */
export function listPendingReviews(): Promise<PendingReviews> {
  return staffApi().get<PendingReviews>('/portal/reviews/pending');
}

/** `POST /portal/reviews`: reviews one visit. */
export function submitPortalReview(visitId: string, input: ReviewInput): Promise<SubmittedReview> {
  return staffApi().post<SubmittedReview>('/portal/reviews', { body: { visit_id: visitId, ...input } });
}
