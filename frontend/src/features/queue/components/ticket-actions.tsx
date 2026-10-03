'use client';

import { useState, useTransition } from 'react';
import { Button } from '@/components/ui/button';
import type { ActionResult } from '@/lib/actions/action-result';
import {
  assignTicketAction,
  callTicketAction,
  leaveQueueAction,
  skipTicketAction,
} from '../actions/queue-actions';
import type { QueueTicket } from '../types/queue.types';

interface Props {
  ticket: QueueTicket;
  rooms: string[];
  /** What this session's scopes allow. Hides buttons; the backend is what refuses. */
  canCall: boolean;
  canOrganise: boolean;
}

/**
 * The buttons on one ticket.
 *
 * Which ones appear follows the contract's state machine: a waiting or skipped ticket can be
 * called, a waiting or called one can be skipped, and anything not yet in a room can leave. The
 * ticket's `version`
 * rides along on every press, so two people working the same board cannot both act on a ticket
 * one of them has already moved — the second press is told so and the board redraws.
 */
export function TicketActions({ ticket, rooms, canCall, canOrganise }: Props) {
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

        {canOrganise && rooms.length > 0 && ticket.status !== 'in_room' && (
          <label className="flex items-center gap-1.5 text-sm text-ink-600">
            <span className="sr-only">ห้องตรวจของคิว {ticket.number}</span>
            <select
              className="h-8 rounded-lg border border-ink-200 bg-white px-2 text-sm"
              value={ticket.room_id ?? ''}
              disabled={pending}
              onChange={(event) => {
                const roomId = event.target.value;

                if (roomId) {
                  run(() => assignTicketAction({ ...ref, roomId }));
                }
              }}
            >
              <option value="">เลือกห้อง</option>
              {rooms.map((room) => (
                <option key={room} value={room}>
                  {room}
                </option>
              ))}
            </select>
          </label>
        )}

        {canOrganise && ticket.status !== 'in_room' && (
          <Button size="sm" variant="ghost" disabled={pending} onClick={() => run(() => leaveQueueAction(ref))}>
            ออกจากคิว
          </Button>
        )}
      </div>

      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}
    </div>
  );
}
