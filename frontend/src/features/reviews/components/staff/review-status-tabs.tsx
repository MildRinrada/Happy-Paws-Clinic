import Link from 'next/link';
import { staffReviewsText } from '../../content/staff-reviews';
import { reviewPaths } from '../../lib/review-paths';
import type { ReviewStatus } from '../../types/review.types';

/** Pending, published, not published: one link per tab, so each tab has its own address. */
export function ReviewStatusTabs({ current }: { current: ReviewStatus }) {
  const text = staffReviewsText;

  return (
    <nav aria-label={text.tabsLabel} className="-mx-1 flex flex-wrap gap-1">
      {text.tabs.map((tab) => {
        const active = tab.status === current;

        return (
          <Link
            key={tab.status}
            href={`${reviewPaths.staff}?status=${tab.status}`}
            aria-current={active ? 'page' : undefined}
            className={`rounded-lg px-3 py-1.5 text-sm font-medium ${
              active ? 'bg-brand-600 text-white' : 'text-ink-700 hover:bg-ink-100'
            }`}
          >
            {tab.label}
          </Link>
        );
      })}
    </nav>
  );
}
