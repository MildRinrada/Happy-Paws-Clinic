import { EmptyLine } from '@/features/portal/components/shared/empty-line';
import { portalReviews } from '../../content/portal-reviews';
import type { PendingReviews } from '../../types/review.types';
import { PendingVisitCard } from './pending-visit-card';

/** Every visit still waiting for a review, newest first; the newest opens with its form showing. */
export function PendingVisitList({ pending }: { pending: PendingReviews }) {
  if (pending.data.length === 0) {
    return <EmptyLine>{portalReviews.empty}</EmptyLine>;
  }

  return (
    <ul className="space-y-4">
      {pending.data.map((visit, index) => (
        <PendingVisitCard key={visit.visit_id} visit={visit} firstName={pending.first_name} startOpen={index === 0} />
      ))}
    </ul>
  );
}
