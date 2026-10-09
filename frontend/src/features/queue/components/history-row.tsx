import { Badge } from '@/components/ui/badge';
import { formatTime } from '@/lib/format/datetime';
import { queueHistory } from '../content/queue-history';
import { priorityLabel, statusLabel } from '../lib/ticket-labels';
import type { TicketView } from '../types/queue.types';

const text = queueHistory;

/** One ticket of a past or finished day, as a table row. */
export function HistoryRow({ view }: { view: TicketView }) {
  const { ticket } = view;
  const status = statusLabel[ticket.status];
  const priority = priorityLabel[ticket.priority];

  return (
    <tr className="align-top">
      <td className="px-3 py-2.5 font-display text-lg font-semibold text-ink-900">{ticket.number}</td>
      <td className="max-w-48 px-3 py-2.5">
        <p className="truncate font-medium text-ink-900">{view.petName ?? ticket.pet_id}</p>
        {view.petDetail && <p className="truncate text-xs text-ink-500">{view.petDetail}</p>}
      </td>
      <td className="max-w-40 truncate px-3 py-2.5 text-ink-800">{view.serviceName ?? ticket.service_id}</td>
      <td className="px-3 py-2.5">
        <div className="flex flex-wrap gap-1">
          <Badge tone={status.tone}>{status.text}</Badge>
          {priority && <Badge tone={priority.tone}>{priority.text}</Badge>}
        </div>
        {ticket.skip_count > 0 && <p className="mt-1 text-xs text-amber-800">{text.skipped(ticket.skip_count)}</p>}
      </td>
      <td className="max-w-40 truncate px-3 py-2.5 text-ink-800">{view.vetName ?? ticket.staff_id ?? text.none}</td>
      <td className="px-3 py-2.5 text-ink-800">{ticket.room_id ?? text.none}</td>
      <td className="px-3 py-2.5 text-ink-800">{formatTime(ticket.issued_at)}</td>
      <td className="px-3 py-2.5 text-ink-800">{ticket.called_at ? formatTime(ticket.called_at) : text.none}</td>
    </tr>
  );
}
