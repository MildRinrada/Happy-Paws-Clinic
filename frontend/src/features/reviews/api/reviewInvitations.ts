import { publicApi } from '@/lib/api/server-client';
import type { ReviewInput, ReviewInvitationView, SubmittedReview } from '../types/review.types';

/**
 * The one-time review link for a customer without a portal account (docs/contract-extensions.md
 * § 36). Public: the token is the credential. Never cached, since one use changes the answer.
 */

const path = (token: string) => `/reviews/invitations/${encodeURIComponent(token)}`;

/** `GET /reviews/invitations/{token}`: what the link is for and whether it still works. */
export function getReviewInvitation(token: string): Promise<ReviewInvitationView> {
  return publicApi().get<ReviewInvitationView>(path(token));
}

/** `POST /reviews/invitations/{token}`: the review, pending until staff decide. */
export function submitInvitationReview(token: string, input: ReviewInput): Promise<SubmittedReview> {
  return publicApi().post<SubmittedReview>(path(token), { body: input });
}
