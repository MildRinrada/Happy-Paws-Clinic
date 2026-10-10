import { unstable_rethrow } from 'next/navigation';
import { authed } from '@/lib/auth/session';
import { listPendingReviews } from '../api/portalReviews';
import type { PendingReviews } from '../types/review.types';

/**
 * The signed-in owner's visits still waiting for a review. On the portal home a failed read just
 * leaves the card out (`null`), so the rest of the home page still shows. A trip to sign-in still
 * goes through.
 */
export async function loadPendingReviews(): Promise<PendingReviews | null> {
  try {
    return await authed(listPendingReviews);
  } catch (error) {
    unstable_rethrow(error);
    return null;
  }
}
