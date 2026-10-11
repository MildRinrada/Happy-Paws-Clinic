import { reviewFilters, reviewsPage } from '../../content/reviews-page';
import { reviewListHref } from '../../lib/review-list-href';
import type { ReviewList, ReviewListQuery } from '../../types/review-list.types';
import { OlderReviewsLink } from './older-reviews-link';
import { ReviewEntryList } from './review-entry-list';
import { ReviewFilterNav } from './review-filter-nav';
import { ReviewListMessage } from './review-list-message';

interface Props {
  /** `null` when the reviews could not be read. */
  list: ReviewList | null;
  query: ReviewListQuery;
}

/**
 * The right column: the filter chips, the cards, and the button to older reviews when there are
 * more. With nothing published there is nothing to filter, so the chips are left out and a short
 * message says reviews appear here once the clinic has read them.
 */
export function ReviewListing({ list, query }: Props) {
  if (list === null) {
    return <ReviewListMessage title={reviewsPage.unavailable} />;
  }

  if (list.summary.count === 0) {
    return <ReviewListMessage title={reviewsPage.empty} hint={reviewsPage.emptyHint} />;
  }

  const { filter } = query;

  return (
    <div className="space-y-[18px]">
      <ReviewFilterNav current={filter} />
      {list.coversLatest !== undefined && (
        <p className="text-sm text-site-muted">{reviewFilters.coversLatest(list.coversLatest)}</p>
      )}

      {list.items.length === 0 ? (
        <ReviewListMessage title={reviewFilters.empty[filter]} showAll={filter !== 'all'} />
      ) : (
        <ReviewEntryList reviews={list.items} />
      )}

      {list.nextCursor && (
        <OlderReviewsLink href={reviewListHref({ filter, cursor: list.nextCursor })} />
      )}
    </div>
  );
}
