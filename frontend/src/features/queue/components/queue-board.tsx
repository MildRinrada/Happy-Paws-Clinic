import { EmptyState } from '@/components/ui/empty-state';
import type { QueueView, TicketStatus, TicketView } from '../types/queue.types';
import { TicketCard } from './ticket-card';

interface Props {
  view: QueueView;
  rooms: string[];
  canCall: boolean;
  canOrganise: boolean;
  /** `consult:write`: the ticket card offers to open the case. */
  canConsult: boolean;
}

interface Column {
  title: string;
  hint: string;
  statuses: TicketStatus[];
}

/**
 * Three columns, left to right, the way a patient moves through the clinic. A skipped ticket stays
 * in the first: the contract's "เลื่อนไปท้ายแถว" makes it a waiting state, not an exit.
 */
const columns: Column[] = [
  { title: 'รอเรียก', hint: 'เรียงตามลำดับที่จะถูกเรียก', statuses: ['waiting', 'skipped'] },
  { title: 'กำลังเรียก', hint: 'เรียกแล้ว รอเข้าห้อง', statuses: ['called'] },
  { title: 'ในห้องตรวจ', hint: 'เปิดเคสแล้ว', statuses: ['in_room'] },
];

/**
 * Today's queue for one branch.
 *
 * Tickets are drawn in the order the API returned them and never re-sorted here. That order is
 * the clinic's rule — urgency, then not-skipped, then number — and it lives in one query on the
 * backend so the counter's board and an owner's "how many ahead of me" cannot disagree.
 */
export function QueueBoard({ view, rooms, canCall, canOrganise, canConsult }: Props) {
  if (view.tickets.length === 0) {
    return <EmptyState title="ยังไม่มีคิววันนี้">ออกบัตรคิวให้ลูกค้า walk-in หรือเช็กอินนัดหมายเพื่อเริ่มคิว</EmptyState>;
  }

  return (
    <div className="grid gap-5 lg:grid-cols-3">
      {columns.map((column) => {
        const tickets = view.tickets.filter((item: TicketView) => column.statuses.includes(item.ticket.status));

        return (
          <section key={column.title} aria-label={column.title} className="space-y-3">
            <header className="flex items-baseline justify-between">
              <div>
                <h2 className="font-display text-lg font-semibold text-ink-900">{column.title}</h2>
                <p className="text-sm text-ink-500">{column.hint}</p>
              </div>
              <span className="rounded-full bg-ink-100 px-2.5 py-0.5 text-sm font-medium text-ink-700">
                {tickets.length}
              </span>
            </header>

            {tickets.length === 0 ? (
              <p className="rounded-xl border border-dashed border-ink-200 px-4 py-6 text-center text-sm text-ink-500">
                ไม่มีคิว
              </p>
            ) : (
              tickets.map((item) => (
                <TicketCard
                  key={item.ticket.id}
                  view={item}
                  rooms={rooms}
                  vets={view.vets}
                  canCall={canCall}
                  canOrganise={canOrganise}
                  canConsult={canConsult}
                />
              ))
            )}
          </section>
        );
      })}
    </div>
  );
}
