import { EmptyState } from '@/components/ui/empty-state';
import { queueHistory } from '../content/queue-history';
import type { TicketView } from '../types/queue.types';
import { HistoryRow } from './history-row';

const columns = queueHistory.columns;

/** A day's tickets in the backend's call order, which for a finished day is close to arrival order. */
export function HistoryTable({ tickets }: { tickets: TicketView[] }) {
  if (tickets.length === 0) {
    return <EmptyState title={queueHistory.empty} />;
  }

  return (
    <div className="overflow-x-auto rounded-xl border border-ink-200 bg-white">
      <table className="w-full text-left text-sm">
        <thead className="border-b border-ink-200 bg-ink-50 text-ink-600">
          <tr>
            {Object.values(columns).map((label) => (
              <th key={label} scope="col" className="px-3 py-2 font-medium">
                {label}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-ink-100">
          {tickets.map((view) => (
            <HistoryRow key={view.ticket.id} view={view} />
          ))}
        </tbody>
      </table>
    </div>
  );
}
