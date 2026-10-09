'use client';

import { useState, useTransition } from 'react';
import { Button } from '@/components/ui/button';
import { formMessages } from '@/features/settings/content/form-messages';
import { redeliverWebhookAction } from '../actions/manage-integration-actions';
import { deliveryLogPage } from '../content/webhook-settings';

/**
 * Sends a failed delivery again, with the same event. One press queues it; the backend refuses a
 * second while it is still queued, so a double press never sends it twice.
 */
export function RedeliverButton({ webhookId, deliveryId }: { webhookId: string; deliveryId: string }) {
  const [queued, setQueued] = useState(false);
  const [error, setError] = useState<string>();
  const [pending, startTransition] = useTransition();

  function redeliver() {
    setError(undefined);
    startTransition(async () => {
      try {
        const result = await redeliverWebhookAction({ id: webhookId, deliveryId });
        if (result.ok) setQueued(true);
        else setError(result.error.message);
      } catch {
        setError(formMessages.unreachable);
      }
    });
  }

  if (queued) {
    return (
      <span role="status" className="text-xs text-ink-600">
        {deliveryLogPage.redelivered}
      </span>
    );
  }

  return (
    <span className="flex flex-col items-start gap-1">
      <Button variant="secondary" size="sm" disabled={pending} onClick={redeliver}>
        {pending ? deliveryLogPage.redelivering : deliveryLogPage.redeliver}
      </Button>
      {error && (
        <span role="alert" className="text-xs text-red-700">
          {error}
        </span>
      )}
    </span>
  );
}
