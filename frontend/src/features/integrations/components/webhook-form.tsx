'use client';

import { useId, useState, type FormEvent } from 'react';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { CheckboxGroup } from '@/components/ui/checkbox-group';
import { Field, Input } from '@/components/ui/field';
import { FormFooter } from '@/features/settings/components/form-footer';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { createWebhookAction } from '../actions/integration-actions';
import { webhookEventLabels, webhookSettingsPage } from '../content/webhook-settings';
import type { CreatedWebhook } from '../types/integration.types';
import { CreatedWebhookCard } from './created-webhook-card';

/** A new outbound webhook. Once made, its secret is shown in place of the form, this once. */
export function WebhookForm() {
  const text = webhookSettingsPage.form;
  const id = useId();
  const [url, setUrl] = useState('');
  const [description, setDescription] = useState('');
  const [events, setEvents] = useState<string[]>([]);
  const [created, setCreated] = useState<CreatedWebhook>();
  const { errors, message, pending, submit, touch } = useFormSubmit(formMessages);

  if (created) {
    return <CreatedWebhookCard webhook={created} onDone={() => setCreated(undefined)} />;
  }

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    submit(() => createWebhookAction({ url, events, description: description || undefined }), (webhook) => {
      setCreated(webhook);
      setUrl('');
      setDescription('');
      setEvents([]);
    });
  }

  return (
    <form onSubmit={save}>
      <Card>
        <CardHeader title={text.heading} />
        <CardBody className="space-y-5">
          <div className="grid gap-4 sm:grid-cols-2">
            <Field label={text.url} htmlFor={`${id}-url`} hint={text.urlHint} error={errors.url}>
              <Input
                id={`${id}-url`}
                type="url"
                inputMode="url"
                placeholder="https://"
                maxLength={2000}
                value={url}
                onChange={(event) => {
                  touch();
                  setUrl(event.target.value);
                }}
              />
            </Field>
            <Field label={text.description} htmlFor={`${id}-description`} error={errors.description}>
              <Input
                id={`${id}-description`}
                maxLength={200}
                value={description}
                onChange={(event) => {
                  touch();
                  setDescription(event.target.value);
                }}
              />
            </Field>
          </div>

          <CheckboxGroup
            legend={text.events}
            hint={text.eventsHint}
            error={errors.events}
            options={webhookEventLabels}
            selected={events}
            onChange={(next) => {
              touch();
              setEvents(next);
            }}
          />

          <FormFooter pending={pending} message={message} submitLabel={text.submit} pendingLabel={text.pending} />
        </CardBody>
      </Card>
    </form>
  );
}
