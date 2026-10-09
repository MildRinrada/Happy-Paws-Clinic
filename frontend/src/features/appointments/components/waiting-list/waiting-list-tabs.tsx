import Link from 'next/link';
import { waitingListText } from '../../content/advanced-booking';
import { appointmentPaths } from '../../lib/appointment-paths';
import type { WaitingListStatus } from '../../types/advanced-booking.types';

const STATUSES: WaitingListStatus[] = ['waiting', 'lapsed'];

/** Still waiting, or past their last day. The choice is in the address (`?status=`). */
export function WaitingListTabs({ current }: { current: WaitingListStatus }) {
  const text = waitingListText.list.tabs;

  return (
    <nav className="flex gap-1.5" aria-label={waitingListText.list.heading}>
      {STATUSES.map((status) => (
        <Link
          key={status}
          href={appointmentPaths.waitingListOf(status)}
          aria-current={status === current ? 'page' : undefined}
          className={`rounded-full px-3 py-1 text-sm font-medium ${
            status === current ? 'bg-brand-600 text-white' : 'bg-ink-100 text-ink-700 hover:bg-ink-200'
          }`}
        >
          {text[status]}
        </Link>
      ))}
    </nav>
  );
}
