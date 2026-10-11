import type { ReviewList, ReviewListQuery } from '../../types/review-list.types';
import { ReviewListing } from './review-listing';
import { ReviewSummary } from './review-summary';
import { WriteReviewCard } from './write-review-card';

interface Props {
  /** `null` when the reviews could not be read. */
  list: ReviewList | null;
  query: ReviewListQuery;
}

/**
 * Under the banner: the average and the card to write a review in a narrow column at the left,
 * the reviews at the right. On a narrow screen they stack as the average, the reviews, then the
 * card to write one, so the reviews come before the request to add one. With nothing published,
 * or the API away, the average is left out and the card moves to the top of its column.
 */
export function ReviewsBoard({ list, query }: Props) {
  const withSummary = list !== null && list.summary.count > 0 && list.summary.average != null;

  return (
    <div className="grid gap-8 lg:grid-cols-[268px_minmax(0,1fr)] lg:grid-rows-[auto_1fr] lg:gap-x-[30px] lg:gap-y-4">
      {withSummary && (
        <div className="lg:col-start-1 lg:row-start-1">
          <ReviewSummary summary={list.summary} />
        </div>
      )}
      <div className="min-w-0 lg:col-start-2 lg:row-span-2 lg:row-start-1">
        <ReviewListing list={list} query={query} />
      </div>
      <div className={`lg:col-start-1 lg:self-start ${withSummary ? 'lg:row-start-2' : 'lg:row-start-1'}`}>
        <WriteReviewCard />
      </div>
    </div>
  );
}
