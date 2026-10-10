import { listPublicReviews } from '../api/listPublicReviews';
import type { PublicReviewPage } from '../types/review.types';

/**
 * A page of published reviews for a public page, or `null` if the API cannot be reached.
 *
 * The public site must still render when the API is briefly away; the home page then leaves its
 * review column out, the same as when nothing is published, and never falls back to sample quotes.
 */
export async function loadPublicReviews(limit: number, cursor?: string): Promise<PublicReviewPage | null> {
  try {
    return await listPublicReviews(limit, cursor);
  } catch {
    return null;
  }
}
