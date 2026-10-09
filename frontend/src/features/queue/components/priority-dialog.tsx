'use client';

import { useId, useState } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Select, Textarea } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { setTicketPriorityAction } from '../actions/queue-actions';
import { queueBoard } from '../content/queue-board';
import type { QueueTicket, TicketPriority } from '../types/queue.types';
import { QueueDialog } from './queue-dialog';

const text = queueBoard.priority;
const levels: TicketPriority[] = ['normal', 'urgent', 'emergency'];

/**
 * Raise or lower a ticket's urgency after it was issued (`POST /staff/queue/{id}/priority`).
 * The number and the status stay; only the ticket's place in the call order moves.
 */
export function PriorityDialog({ ticket }: { ticket: QueueTicket }) {
  const id = useId();
  const [priority, setPriority] = useState<TicketPriority>(ticket.priority);
  const [reason, setReason] = useState('');
  const { errors, message, pending, submit } = useFormSubmit(queueBoard.messages);

  const needsReason = priority !== 'normal';
  const ready = priority !== ticket.priority && (!needsReason || reason.trim().length > 0);

  return (
    <QueueDialog
      trigger={text.trigger}
      title={text.title(ticket.number)}
      onOpen={() => {
        setPriority(ticket.priority);
        setReason('');
      }}
    >
      {(close) => (
        <>
          <Field label={text.field} htmlFor={`${id}-priority`} error={errors.priority}>
            <Select
              id={`${id}-priority`}
              value={priority}
              onChange={(event) => setPriority(event.target.value as TicketPriority)}
            >
              {levels.map((level) => (
                <option key={level} value={level}>
                  {text.options[level]}
                </option>
              ))}
            </Select>
          </Field>

          <Field label={text.reason} htmlFor={`${id}-reason`} hint={text.reasonHint} error={errors.reason}>
            <Textarea
              id={`${id}-reason`}
              rows={2}
              maxLength={300}
              value={reason}
              onChange={(event) => setReason(event.target.value)}
            />
          </Field>

          <FormStatus message={message} savedText="" />

          <div className="flex justify-end gap-2">
            <Button variant="ghost" onClick={close}>
              {queueBoard.messages.cancel}
            </Button>
            <Button
              disabled={!ready || pending}
              onClick={() =>
                submit(
                  () => setTicketPriorityAction({ ticketId: ticket.id, version: ticket.version, priority, reason }),
                  close,
                )
              }
            >
              {pending ? text.submitting : text.submit}
            </Button>
          </div>
        </>
      )}
    </QueueDialog>
  );
}
