import type { PublicReview, ReviewSummary } from './review.types';

/**
 * The chips over the public list: every review, five stars, four stars, three stars or fewer, or
 * the ones the clinic answered. `?filter=` in the address.
 */
export type ReviewFilter = 'all' | '5' | '4' | '3down' | 'replied';

/** What the reader asked the public reviews page for: `?filter=&cursor=`. */
export interface ReviewListQuery {
  filter: ReviewFilter;
  /** Where the next page of older reviews starts, from the page before. */
  cursor?: string;
}

/** One page of the public reviews list, as `loadReviewList` gives it. */
export interface ReviewList {
  /** Newest first. */
  items: PublicReview[];
  /** Over every published review, whatever the filter. */
  summary: ReviewSummary;
  /** Present when there are older reviews to show on another page. */
  nextCursor?: string;
  /**
   * Set when a filter could only look at the newest this many reviews and more are published, so
   * the page can say the filter does not reach further back.
   */
  coversLatest?: number;
}
