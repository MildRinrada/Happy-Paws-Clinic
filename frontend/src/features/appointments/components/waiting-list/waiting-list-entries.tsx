import Link from 'next/link';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { waitingListText } from '../../content/advanced-booking';
import { appointmentPaths } from '../../lib/appointment-paths';
import type { WaitingListItem, WaitingListStatus } from '../../types/advanced-booking.types';
import { WaitingListRow } from './waiting-list-row';
import { WaitingListTabs } from './waiting-list-tabs';

interface WaitingListEntriesProps {
  entries: WaitingListItem[];
  status: WaitingListStatus;
  nextCursor?: string;
  /** True when this is not the first page, so there is a way back. */
  paged: boolean;
  vetNames: Record<string, string>;
  canOpenCustomer: boolean;
}

/** The waiting list at this branch, oldest first, one page at a time. */
export function WaitingListEntries({ entries, status, nextCursor, paged, vetNames, canOpenCustomer }: WaitingListEntriesProps) {
  const text = waitingListText.list;

  return (
    <Card>
      <CardHeader title={text.heading} />
      <CardBody className="space-y-4">
        <WaitingListTabs current={status} />
        <p className="text-sm text-ink-600">{text.note}</p>

        {entries.length === 0 ? (
          <p className="text-sm text-ink-500">{text.empty[status]}</p>
        ) : (
          <ul className="divide-y divide-ink-100">
            {entries.map((entry) => (
              <WaitingListRow key={entry.id} entry={entry} vetNames={vetNames} canOpenCustomer={canOpenCustomer} />
            ))}
          </ul>
        )}

        {(nextCursor || paged) && (
          <div className="flex justify-between gap-3 text-sm font-medium">
            {paged ? (
              <Link href={appointmentPaths.waitingListOf(status)} className="text-brand-700 hover:underline">
                {text.first}
              </Link>
            ) : (
              <span />
            )}
            {nextCursor && (
              <Link href={appointmentPaths.waitingListOf(status, nextCursor)} className="text-brand-700 hover:underline">
                {text.next}
              </Link>
            )}
          </div>
        )}
      </CardBody>
    </Card>
  );
}
