'use client';

import { useId, useState } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Textarea } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { leaveQueueAction } from '../actions/queue-actions';
import { queueBoard } from '../content/queue-board';
import type { QueueTicket } from '../types/queue.types';
import { QueueDialog } from './queue-dialog';

const text = queueBoard.leave;

/** Take a ticket off the board as `left`, with an optional reason kept on the ticket. */
export function LeaveDialog({ ticket }: { ticket: QueueTicket }) {
  const id = useId();
  const [reason, setReason] = useState('');
  const { errors, message, pending, submit } = useFormSubmit(queueBoard.messages);

  return (
    <QueueDialog trigger={text.trigger} title={text.title(ticket.number)} onOpen={() => setReason('')}>
      {(close) => (
        <>
          <p className="text-sm text-ink-600">{text.body}</p>

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
              variant="danger"
              disabled={pending}
              onClick={() =>
                submit(() => leaveQueueAction({ ticketId: ticket.id, version: ticket.version, reason }), close)
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
