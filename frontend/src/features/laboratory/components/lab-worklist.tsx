import Link from 'next/link';
import { buttonClass } from '@/components/ui/button';
import { EmptyState } from '@/components/ui/empty-state';
import { labPage } from '../content/lab-page';
import { labHref, type LabFilters } from '../lib/lab-filters';
import type { LabWorklistView } from '../lib/load-lab-worklist';
import { LabRequestCard } from './lab-request-card';

interface LabWorklistProps {
  view: LabWorklistView;
  filters: LabFilters;
  canRecord: boolean;
  canReview: boolean;
  canOpenCase: boolean;
}

/** One page of requests, newest first, with a link to the next. */
export function LabWorklist({ view, filters, ...can }: LabWorklistProps) {
  if (view.rows.length === 0) {
    return <EmptyState title={labPage.empty[filters.tab]}>{labPage.empty.hint}</EmptyState>;
  }

  return (
    <div className="space-y-3">
      {view.rows.map((row) => (
        <LabRequestCard key={row.request.id} row={row} {...can} />
      ))}
      {view.nextCursor && (
        <Link href={labHref({ ...filters, cursor: view.nextCursor })} className={buttonClass('secondary', 'sm')}>
          {labPage.nextPage}
        </Link>
      )}
    </div>
  );
}
