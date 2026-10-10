import { EmptyState } from '@/components/ui/empty-state';
import { dueBackPage } from '../../content/due-back-page';
import type { DueBackCustomer } from '../../types/due-back.types';
import { PageLinks } from '../shared/page-links';
import { DueBackRow } from './due-back-row';

interface DueBackListProps {
  rows: DueBackCustomer[];
  nextHref?: string;
  firstHref?: string;
}

/** One page of the call list, most recently due first. */
export function DueBackList({ rows, nextHref, firstHref }: DueBackListProps) {
  const text = dueBackPage;

  return (
    <div className="space-y-3">
      {rows.length === 0 ? (
        <EmptyState title={text.empty} />
      ) : (
        <ul className="divide-y divide-ink-100 rounded-xl border border-ink-200 bg-white px-4">
          {rows.map((row) => (
            <DueBackRow key={row.customer_id} row={row} />
          ))}
        </ul>
      )}
      <PageLinks nextHref={nextHref} firstHref={firstHref} nextLabel={text.next} firstLabel={text.first} />
    </div>
  );
}
