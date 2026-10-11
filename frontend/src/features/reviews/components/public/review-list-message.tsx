import Link from 'next/link';
import { EmptyState } from '@/components/ui/empty-state';
import { reviewsPage } from '../../content/reviews-page';
import { reviewListHref } from '../../lib/review-list-href';

interface Props {
  /** What happened, such as nothing under the filter. */
  title: string;
  /** A suggestion under the title. */
  hint?: string;
  /** Adds the link back to every review, after a filter that found nothing. */
  showAll?: boolean;
}

/** Why there are no reviews to show, and, after a filter, the way back to all of them. */
export function ReviewListMessage({ title, hint, showAll = false }: Props) {
  return (
    <EmptyState title={title}>
      {(hint || showAll) && (
        <>
          {hint && `${hint} `}
          {showAll && (
            <Link href={reviewListHref()} className="font-semibold text-site-teal underline">
              {reviewsPage.more}
            </Link>
          )}
        </>
      )}
    </EmptyState>
  );
}
