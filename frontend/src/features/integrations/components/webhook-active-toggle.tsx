'use client';

import { useState, useTransition } from 'react';
import { Button } from '@/components/ui/button';
import { formMessages } from '@/features/settings/content/form-messages';
import { updateWebhookAction } from '../actions/manage-integration-actions';
import { webhookListText } from '../content/webhook-list';
import type { Webhook } from '../types/integration.types';
import { ConfirmButton } from './confirm-button';

/**
 * Switches a webhook on or off. Off asks once, because it also stops what is waiting to be sent;
 * on is one press. The list re-reads itself afterwards.
 */
export function WebhookActiveToggle({ webhook }: { webhook: Pick<Webhook, 'id' | 'version' | 'active'> }) {
  const text = webhookListText.toggle;
  const [error, setError] = useState<string>();
  const [pending, startTransition] = useTransition();

  if (webhook.active) {
    return (
      <ConfirmButton
        label={text.disable}
        confirmText={text.confirm}
        confirmLabel={text.confirmButton}
        pendingLabel={text.pending}
        cancelLabel={webhookListText.back}
        variant="secondary"
        action={() => updateWebhookAction({ id: webhook.id, version: webhook.version, values: { active: false } })}
      />
    );
  }

  function enable() {
    setError(undefined);
    startTransition(async () => {
      try {
        const result = await updateWebhookAction({ id: webhook.id, version: webhook.version, values: { active: true } });
        if (!result.ok) setError(result.error.code === 'version_conflict' ? formMessages.conflict : result.error.message);
      } catch {
        setError(formMessages.unreachable);
      }
    });
  }

  return (
    <span className="flex flex-col items-start gap-1.5">
      <Button variant="secondary" size="sm" disabled={pending} onClick={enable}>
        {pending ? text.pending : text.enable}
      </Button>
      {error && (
        <span role="alert" className="text-xs text-red-700">
          {error}
        </span>
      )}
    </span>
  );
}
