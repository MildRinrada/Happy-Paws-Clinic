import { Badge } from '@/components/ui/badge';
import { OpenCaseButton } from '@/features/consultations/components/open-case-button';
import { formatTime, minutesSince } from '@/lib/format/datetime';
import { priorityLabel, statusLabel } from '../lib/ticket-labels';
import type { TicketView, VetOption } from '../types/queue.types';
import { TicketActions } from './ticket-actions';

interface Props {
  view: TicketView;
  rooms: string[];
  vets: VetOption[];
  canCall: boolean;
  canOrganise: boolean;
  canConsult: boolean;
}

/** One ticket on the board: the number large, because that is what gets read out. */
export function TicketCard({ view, rooms, vets, canCall, canOrganise, canConsult }: Props) {
  const { ticket } = view;
  const priority = priorityLabel[ticket.priority];
  const status = statusLabel[ticket.status];
  const waited = minutesSince(ticket.issued_at);

  return (
    <article
      className={`rounded-xl border bg-white p-4 ${
        ticket.priority === 'emergency' ? 'border-red-300 ring-1 ring-red-200' : 'border-ink-200'
      }`}
    >
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="font-display text-3xl font-semibold leading-none tracking-tight text-ink-900">
            {ticket.number}
          </p>
          <p className="mt-2 font-medium text-ink-900">{view.petName ?? ticket.pet_id}</p>
          {view.petDetail && <p className="text-sm text-ink-500">{view.petDetail}</p>}
        </div>

        <div className="flex shrink-0 flex-col items-end gap-1.5">
          <Badge tone={status.tone}>{status.text}</Badge>
          {priority && <Badge tone={priority.tone}>{priority.text}</Badge>}
          {/* The contract: "แสดงเป็นแถบเตือนแดงทุกหน้าที่เกี่ยวข้อง". */}
          {view.hasAllergy && <Badge tone="red">มีประวัติแพ้ยา</Badge>}
        </div>
      </div>

      {/* min-w-0 + truncate: a name that could not be resolved falls back to an id, and an id is
          long enough to run into the next column if it is allowed to. */}
      <dl className="mt-3 grid grid-cols-2 gap-x-4 gap-y-1 text-sm">
        <div className="min-w-0">
          <dt className="text-ink-500">บริการ</dt>
          <dd className="truncate text-ink-800">{view.serviceName ?? ticket.service_id}</dd>
        </div>
        <div className="min-w-0">
          <dt className="text-ink-500">ออกบัตร</dt>
          <dd className="truncate text-ink-800">
            {formatTime(ticket.issued_at)} น. · รอ {waited} นาที
          </dd>
        </div>
        <div className="min-w-0">
          <dt className="text-ink-500">สัตวแพทย์</dt>
          <dd className="truncate text-ink-800" title={view.vetName ?? ticket.staff_id}>
            {view.vetName ?? ticket.staff_id ?? 'ยังไม่ระบุ'}
          </dd>
        </div>
        <div className="min-w-0">
          <dt className="text-ink-500">ห้องตรวจ</dt>
          <dd className="truncate text-ink-800">{ticket.room_id ?? 'ยังไม่ระบุ'}</dd>
        </div>
      </dl>

      {ticket.skip_count > 0 && (
        <p className="mt-2 text-sm text-amber-800">ถูกข้ามแล้ว {ticket.skip_count} ครั้ง</p>
      )}

      <div className="mt-3 border-t border-ink-100 pt-3">
        <TicketActions
          ticket={ticket}
          vetName={view.vetName}
          rooms={rooms}
          vets={vets}
          canCall={canCall}
          canOrganise={canOrganise}
        />
        {/* A vet may fetch the patient themselves: opening the case is the call (contract-extensions § 8). */}
        {canConsult && ['waiting', 'called', 'in_room'].includes(ticket.status) && (
          <div className="mt-2">
            <OpenCaseButton ticketId={ticket.id} resume={ticket.status === 'in_room'} />
          </div>
        )}
      </div>
    </article>
  );
}
