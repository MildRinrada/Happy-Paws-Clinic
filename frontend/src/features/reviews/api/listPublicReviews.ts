import { publicApi } from '@/lib/api/server-client';
import type { PublicReviewPage } from '../types/review.types';

/** How long Next may reuse the public list before asking again, in seconds. */
export const PUBLIC_REVIEWS_REVALIDATE = 300;

/**
 * `GET /reviews?limit=&cursor=`: published reviews, newest first, with the summary over every
 * published review. Public. Not in the API contract (docs/contract-extensions.md § 36).
 */
export function listPublicReviews(limit: number, cursor?: string): Promise<PublicReviewPage> {
  return publicApi().get<PublicReviewPage>('/reviews', {
    query: { limit, cursor },
    revalidate: PUBLIC_REVIEWS_REVALIDATE,
  });
}
