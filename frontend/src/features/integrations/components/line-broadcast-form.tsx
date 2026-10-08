'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { Field, Textarea } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import { ChosenRecipients } from '@/features/notifications/components/chosen-recipients';
import { RecipientSearch } from '@/features/notifications/components/recipient-search';
import { SendResultList } from '@/features/notifications/components/send-result-list';
import type { Recipient, SendResult } from '@/features/notifications/types/notification.types';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { broadcastLineAction } from '../actions/line-actions';
import { lineBroadcastPage } from '../content/line-broadcast';

/** The customers to announce to, and the message. A refusal (quota, LINE off) shows beside the button. */
export function LineBroadcastForm({ canSearchCustomers }: { canSearchCustomers: boolean }) {
  const text = lineBroadcastPage;
  const [recipients, setRecipients] = useState<Recipient[]>([]);
  const [message, setMessage] = useState('');
  const [result, setResult] = useState<SendResult>();
  const { errors, message: refusal, pending, submit, touch } = useFormSubmit({ conflict: formMessages.conflict, unreachable: text.unreachable });
  const full = recipients.length >= text.maxRecipients;

  function send(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setResult(undefined);
    submit(() => broadcastLineAction({ customer_ids: recipients.map((item) => item.id), message }), setResult);
  }

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader title={text.recipients} />
        <CardBody className="space-y-3">
          <p className="text-sm text-ink-500">{text.segmentNote}</p>
          {canSearchCustomers ? (
            <RecipientSearch
              chosen={recipients.map((item) => item.id)}
              onAdd={(recipient) => {
                touch();
                setRecipients((current) => (current.some((item) => item.id === recipient.id) || full ? current : [...current, recipient]));
              }}
            />
          ) : (
            <p className="text-sm text-ink-600">{text.noCustomerSearch}</p>
          )}
          <ChosenRecipients recipients={recipients} onRemove={(id) => setRecipients((current) => current.filter((item) => item.id !== id))} />
          {full && <p className="text-sm text-amber-800">{text.tooMany}</p>}
        </CardBody>
      </Card>

      <Card>
        <CardHeader title={text.message} />
        <CardBody>
          <form onSubmit={send} className="space-y-4">
            <Field label={text.message} htmlFor="broadcast-message" hint={text.messageHint} error={errors.message}>
              <Textarea
                id="broadcast-message"
                rows={5}
                maxLength={1000}
                required
                value={message}
                onChange={(event) => {
                  touch();
                  setMessage(event.target.value);
                }}
              />
            </Field>
            <div className="flex flex-wrap items-center justify-end gap-3">
              <FormStatus message={refusal ?? errors.customer_ids} savedText="" />
              <Button type="submit" disabled={pending || recipients.length === 0 || !message.trim()}>
                {pending ? text.sending : text.submit}
              </Button>
            </div>
          </form>
        </CardBody>
      </Card>

      {result && <SendResultList result={result} names={Object.fromEntries(recipients.map((item) => [item.id, item.name]))} />}
    </div>
  );
}
