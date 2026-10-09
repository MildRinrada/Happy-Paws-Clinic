'use client';

import { useState, useTransition } from 'react';
import { Button } from '@/components/ui/button';
import type { ActionResult } from '@/lib/actions/action-result';
import { assignTicketAction, callTicketAction, skipTicketAction } from '../actions/queue-actions';
import { queueBoard } from '../content/queue-board';
import type { QueueTicket, VetOption } from '../types/queue.types';
import { AssignSelect } from './assign-select';
import { LeaveDialog } from './leave-dialog';
import { PriorityDialog } from './priority-dialog';

interface Props {
  ticket: QueueTicket;
  /** The vet's name as the board resolved it, for a vet no longer in the pick-list. */
  vetName?: string;
  rooms: string[];
  vets: VetOption[];
  /** What this session's scopes allow. Hides buttons; the backend is what refuses. */
  canCall: boolean;
  canOrganise: boolean;
}

/**
 * The buttons on one ticket.
 *
 * Which ones appear follows the contract's state machine: a waiting or skipped ticket can be
 * called, a waiting or called one can be skipped, and anything not yet in a room can be given a
 * vet or a room, or leave. Urgency can change for anything still on the board. The ticket's
 * `version` rides along on every press, so two people working the same board cannot both act on
 * a ticket one of them has already moved — the second press is told so and the board redraws.
 */
export function TicketActions({ ticket, vetName, rooms, vets, canCall, canOrganise }: Props) {
  const [pending, startTransition] = useTransition();
  const [error, setError] = useState<string | null>(null);

  const ref = { ticketId: ticket.id, version: ticket.version };

  function run(action: () => Promise<ActionResult<QueueTicket>>) {
    setError(null);
    startTransition(async () => {
      const result = await action();

      if (!result.ok) {
        setError(result.error.message);
      }
    });
  }

  const awaitingCall = ticket.status === 'waiting' || ticket.status === 'skipped';
  const beforeRoom = ticket.status !== 'in_room';

  // A vet put on the ticket who is not in today's list (moved branch, or the list is unreadable)
  // still shows as chosen, rather than the select silently reading "จ่ายหมอ".
  const vetChoices = vets.map((vet) => ({ value: vet.id, label: vet.name }));
  if (ticket.staff_id && !vets.some((vet) => vet.id === ticket.staff_id)) {
    vetChoices.unshift({ value: ticket.staff_id, label: vetName ?? ticket.staff_id });
  }

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap items-center gap-2">
        {canCall && awaitingCall && (
          <Button size="sm" disabled={pending} onClick={() => run(() => callTicketAction(ref))}>
            เรียกคิว
          </Button>
        )}

        {/* Not offered on a ticket already skipped: the backend refuses skipped → skipped, so a
            double press cannot inflate the count of times an owner failed to come forward. */}
        {canCall && (ticket.status === 'waiting' || ticket.status === 'called') && (
          <Button size="sm" variant="secondary" disabled={pending} onClick={() => run(() => skipTicketAction(ref))}>
            ข้าม · ยังไม่มา
          </Button>
        )}

        {canOrganise && beforeRoom && vets.length > 0 && (
          <AssignSelect
            label={queueBoard.vet.label(ticket.number)}
            placeholder={queueBoard.vet.placeholder}
            value={ticket.staff_id ?? ''}
            options={vetChoices}
            disabled={pending}
            onPick={(staffId) => run(() => assignTicketAction({ ...ref, staffId }))}
          />
        )}

        {canOrganise && beforeRoom && rooms.length > 0 && (
          <AssignSelect
            label={queueBoard.room.label(ticket.number)}
            placeholder={queueBoard.room.placeholder}
            value={ticket.room_id ?? ''}
            options={rooms.map((room) => ({ value: room, label: room }))}
            disabled={pending}
            onPick={(roomId) => run(() => assignTicketAction({ ...ref, roomId }))}
          />
        )}

        {canOrganise && <PriorityDialog ticket={ticket} />}

        {canOrganise && beforeRoom && <LeaveDialog ticket={ticket} />}
      </div>

      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}
    </div>
  );
}
