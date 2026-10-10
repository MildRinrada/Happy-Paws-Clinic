import Link from 'next/link';
import { reviewsPage } from '../../content/reviews-page';
import { reviewPaths } from '../../lib/review-paths';

/** Older reviews, and the way back to the newest. Plain links, so a page of reviews has its own address. */
export function ReviewPager({ nextCursor, paged }: { nextCursor?: string | null; paged: boolean }) {
  if (!nextCursor && !paged) {
    return null;
  }

  return (
    <nav className="flex flex-wrap justify-between gap-3 text-sm font-medium">
      {paged ? (
        <Link href={reviewPaths.list} className="text-site-teal hover:underline">
          {reviewsPage.first}
        </Link>
      ) : (
        <span />
      )}
      {nextCursor && (
        <Link href={`${reviewPaths.list}?cursor=${encodeURIComponent(nextCursor)}`} className="text-site-teal hover:underline">
          {reviewsPage.next}
        </Link>
      )}
    </nav>
  );
}
