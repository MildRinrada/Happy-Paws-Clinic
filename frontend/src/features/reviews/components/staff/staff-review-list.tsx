import type { ReactNode } from 'react';
import { Card, CardBody } from '@/components/ui/card';
import { PageLinks } from '@/features/crm/components/shared/page-links';
import { staffReviewsText } from '../../content/staff-reviews';
import type { StaffReview } from '../../types/review.types';
import { StaffReviewEntry } from './staff-review-entry';

interface StaffReviewListProps {
  reviews: StaffReview[];
  tabs: ReactNode;
  nextHref?: string;
  firstHref?: string;
}

/** One tab of reviews, newest written first, with paging. */
export function StaffReviewList({ reviews, tabs, nextHref, firstHref }: StaffReviewListProps) {
  const text = staffReviewsText;

  return (
    <Card>
      <CardBody className="space-y-4">
        {tabs}
        {reviews.length === 0 ? (
          <p className="text-sm text-ink-500">{text.empty}</p>
        ) : (
          <ul className="divide-y divide-ink-100">
            {reviews.map((review) => (
              <StaffReviewEntry key={review.id} initial={review} />
            ))}
          </ul>
        )}
        <PageLinks nextHref={nextHref} firstHref={firstHref} nextLabel={text.next} firstLabel={text.first} />
      </CardBody>
    </Card>
  );
}
