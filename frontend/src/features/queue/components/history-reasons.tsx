import { queueHistory } from '../content/queue-history';
import type { QueueTicket } from '../types/queue.types';

/**
 * Why a ticket left and why it was raised, as the counter typed them (docs/contract-extensions.md
 * § 31). Staff only: the backend sends neither to the portal or to a waiting-room screen.
 */
export function HistoryReasons({ ticket }: { ticket: QueueTicket }) {
  if (!ticket.left_reason && !ticket.priority_reason) {
    return <span className="text-ink-400">{queueHistory.none}</span>;
  }

  return (
    <div className="space-y-1 text-xs">
      {ticket.left_reason && <p className="break-words">{queueHistory.leftReason(ticket.left_reason)}</p>}
      {ticket.priority_reason && <p className="break-words">{queueHistory.priorityReason(ticket.priority_reason)}</p>}
    </div>
  );
}
