import { ApiError } from '@/lib/api/api-error';
import { getReviewInvitation } from '../api/reviewInvitations';
import type { ReviewInvitationView } from '../types/review.types';

export type InvitationLoad =
  | { kind: 'found'; invitation: ReviewInvitationView }
  | { kind: 'missing' }
  | { kind: 'unavailable' };

/**
 * What a review link opens: the invitation, "not one of ours" (404, malformed or unknown alike), or
 * "the API is away", which the page says plainly instead of failing.
 */
export async function loadInvitation(token: string): Promise<InvitationLoad> {
  try {
    return { kind: 'found', invitation: await getReviewInvitation(token) };
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      return { kind: 'missing' };
    }

    return { kind: 'unavailable' };
  }
}
