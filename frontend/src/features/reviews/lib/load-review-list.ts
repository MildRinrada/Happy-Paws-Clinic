import { listPublicReviews } from '../api/listPublicReviews';
import type { ReviewList, ReviewListQuery } from '../types/review-list.types';
import type { PublicReviewPage } from '../types/review.types';
import { matchesReviewFilter } from './review-filters';

/** How many reviews a page of every review shows. */
export const REVIEW_PAGE_SIZE = 10;

/** The most `GET /reviews` gives in one read (its `limit` is 1 to 100), which is all a filter can look at for now. */
export const REVIEW_FILTER_SCAN = 100;

/**
 * One page of the public reviews list for a filter and a page, or `null` when the API cannot be
 * reached (the page then says so instead of failing).
 *
 * Every read of the list goes through here. Every review (`all`) pages with the API's own cursor.
 * The API has no filters yet (`rating`, `rating_max` and `has_reply` on `GET /reviews` await the
 * owner's approval), so for now a filter reads the newest 100 reviews, keeps the ones that match,
 * and offers no next page; a cursor is ignored. When more than 100 are published, `coversLatest`
 * says so. Once the API can filter, only this body changes: pass the filter on and return its
 * `next_cursor`, and the page already draws the next-page button.
 */
export async function loadReviewList({ filter, cursor }: ReviewListQuery): Promise<ReviewList | null> {
  let page: PublicReviewPage;
  try {
    page = filter === 'all'
      ? await listPublicReviews(REVIEW_PAGE_SIZE, cursor)
      : await listPublicReviews(REVIEW_FILTER_SCAN);
  } catch {
    return null;
  }

  if (filter === 'all') {
    return { items: page.data, summary: page.summary, nextCursor: page.page.next_cursor ?? undefined };
  }

  const more = Boolean(page.page.next_cursor) || page.summary.count > page.data.length;

  return {
    items: page.data.filter((review) => matchesReviewFilter(review, filter)),
    summary: page.summary,
    coversLatest: more ? page.data.length : undefined,
  };
}
