'use client';

import { useId, useState, type FormEvent } from 'react';
import { CheckboxGroup } from '@/components/ui/checkbox-group';
import { Field, Input } from '@/components/ui/field';
import { FormFooter } from '@/features/settings/components/form-footer';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { updateWebhookAction } from '../actions/manage-integration-actions';
import { webhookListText } from '../content/webhook-list';
import { webhookEventLabels, webhookSettingsPage } from '../content/webhook-settings';
import type { Webhook } from '../types/integration.types';

interface WebhookEditFormProps {
  webhook: Webhook;
  onClose: () => void;
}

/** Changes a webhook's URL, description or events. Only what changed is sent. */
export function WebhookEditForm({ webhook, onClose }: WebhookEditFormProps) {
  const labels = webhookSettingsPage.form;
  const text = webhookListText.editForm;
  const id = useId();
  const [url, setUrl] = useState(webhook.url);
  const [description, setDescription] = useState(webhook.description ?? '');
  const [events, setEvents] = useState<string[]>(webhook.events);
  const { errors, message, pending, submit, touch } = useFormSubmit(formMessages);

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const sameEvents = events.length === webhook.events.length && events.every((type) => webhook.events.includes(type));
    const values = {
      url: url.trim() !== webhook.url ? url.trim() : undefined,
      description: description.trim() !== (webhook.description ?? '') ? description.trim() : undefined,
      events: sameEvents ? undefined : events,
    };

    submit(() => updateWebhookAction({ id: webhook.id, version: webhook.version, values }), onClose);
  }

  return (
    <form onSubmit={save} className="space-y-5" aria-label={text.heading}>
      <div className="grid gap-4 sm:grid-cols-2">
        <Field label={labels.url} htmlFor={`${id}-url`} hint={text.urlHint} error={errors.url}>
          <Input
            id={`${id}-url`}
            type="url"
            inputMode="url"
            maxLength={2000}
            value={url}
            onChange={(event) => {
              touch();
              setUrl(event.target.value);
            }}
          />
        </Field>
        <Field label={labels.description} htmlFor={`${id}-description`} hint={text.descriptionHint} error={errors.description}>
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
        legend={labels.events}
        hint={labels.eventsHint}
        error={errors.events}
        options={webhookEventLabels}
        selected={events}
        onChange={(next) => {
          touch();
          setEvents(next);
        }}
      />

      <FormFooter pending={pending} message={message} onCancel={onClose} />
    </form>
  );
}
