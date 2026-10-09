'use client';

import { deleteWebhookAction } from '../actions/manage-integration-actions';
import { webhookListText } from '../content/webhook-list';
import type { Webhook } from '../types/integration.types';
import { ConfirmButton } from './confirm-button';

/** Deletes a webhook after a second press. It leaves the list; its delivery log stays readable. */
export function DeleteWebhookButton({ webhook }: { webhook: Pick<Webhook, 'id' | 'version'> }) {
  const text = webhookListText.remove;

  return (
    <ConfirmButton
      label={text.button}
      confirmText={text.confirm}
      confirmLabel={text.confirmButton}
      pendingLabel={text.pending}
      cancelLabel={webhookListText.back}
      action={() => deleteWebhookAction({ id: webhook.id, version: webhook.version })}
    />
  );
}
