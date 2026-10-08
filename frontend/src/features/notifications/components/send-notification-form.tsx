'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { Field, Select } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { sendNotificationAction } from '../actions/send-notification-actions';
import { channelChoices, sendableTemplates, sendPage } from '../content/send-page';
import type { Recipient, SendChannel, SendResult } from '../types/notification.types';
import { ChosenRecipients } from './chosen-recipients';
import { RecipientSearch } from './recipient-search';
import { SendResultList } from './send-result-list';
import { VariableFields } from './variable-fields';

/**
 * Who to send to, which message, its values, and the channel. The answer lists every customer,
 * including the ones skipped for consent or for having no address on that channel.
 */
export function SendNotificationForm({ canSearchCustomers }: { canSearchCustomers: boolean }) {
  const [templateKey, setTemplateKey] = useState(sendableTemplates[0]!.key);
  const [values, setValues] = useState<Record<string, string>>({});
  const [channel, setChannel] = useState<SendChannel>('auto');
  const [recipients, setRecipients] = useState<Recipient[]>([]);
  const [result, setResult] = useState<SendResult>();
  const { errors, message, pending, submit, touch } = useFormSubmit({ conflict: formMessages.conflict, unreachable: sendPage.unreachable });

  const template = sendableTemplates.find((item) => item.key === templateKey) ?? sendableTemplates[0]!;
  const full = recipients.length >= sendPage.maxRecipients;

  function add(recipient: Recipient) {
    touch();
    setRecipients((current) => (current.some((item) => item.id === recipient.id) || full ? current : [...current, recipient]));
  }

  function send(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setResult(undefined);
    submit(
      () =>
        sendNotificationAction({
          template: template.key,
          customer_ids: recipients.map((recipient) => recipient.id),
          channel,
          variables: Object.fromEntries(template.variables.map((variable) => [variable.name, values[variable.name] ?? ''])),
        }),
      setResult,
    );
  }

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader title={sendPage.recipients} />
        <CardBody className="space-y-3">
          {canSearchCustomers ? <RecipientSearch chosen={recipients.map((item) => item.id)} onAdd={add} /> : <p className="text-sm text-ink-600">{sendPage.noCustomerSearch}</p>}
          <ChosenRecipients recipients={recipients} onRemove={(id) => setRecipients((current) => current.filter((item) => item.id !== id))} />
          {full && <p className="text-sm text-amber-800">{sendPage.tooMany}</p>}
          {errors.customer_ids && (
            <p role="alert" className="text-sm text-red-700">
              {errors.customer_ids}
            </p>
          )}
        </CardBody>
      </Card>

      <Card>
        <CardHeader title={sendPage.heading} />
        <CardBody>
          <form onSubmit={send} className="space-y-4">
            <div className="grid gap-4 sm:grid-cols-2">
              <Field label={sendPage.template} htmlFor="send-template" error={errors.template}>
                <Select
                  id="send-template"
                  value={templateKey}
                  onChange={(event) => {
                    touch();
                    setTemplateKey(event.target.value);
                    setValues({});
                  }}
                >
                  {sendableTemplates.map((item) => (
                    <option key={item.key} value={item.key}>
                      {item.label}
                    </option>
                  ))}
                </Select>
              </Field>

              <Field label={sendPage.channel} htmlFor="send-channel" hint={sendPage.channelHint} error={errors.channel}>
                <Select id="send-channel" value={channel} onChange={(event) => setChannel(event.target.value as SendChannel)}>
                  {channelChoices.map((choice) => (
                    <option key={choice.value} value={choice.value}>
                      {choice.label}
                    </option>
                  ))}
                </Select>
              </Field>
            </div>

            <VariableFields
              template={template}
              values={values}
              errors={errors}
              onChange={(name, value) => {
                touch();
                setValues((current) => ({ ...current, [name]: value }));
              }}
            />

            <div className="flex flex-wrap items-center justify-end gap-3">
              <FormStatus message={message} savedText="" />
              <Button type="submit" disabled={pending || recipients.length === 0}>
                {pending ? sendPage.sending : sendPage.submit}
              </Button>
            </div>
          </form>
        </CardBody>
      </Card>

      {result && <SendResultList result={result} names={Object.fromEntries(recipients.map((item) => [item.id, item.name]))} />}
    </div>
  );
}
