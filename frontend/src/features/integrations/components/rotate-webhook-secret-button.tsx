'use client';

import { rotateWebhookSecretAction } from '../actions/manage-integration-actions';
import { webhookListText } from '../content/webhook-list';
import type { RotatedWebhookSecret, Webhook } from '../types/integration.types';
import { ConfirmButton } from './confirm-button';

interface RotateWebhookSecretButtonProps {
  webhook: Pick<Webhook, 'id' | 'version'>;
  /** Receives the new secret, to show once. */
  onRotated: (rotated: RotatedWebhookSecret) => void;
}

/** A new signing secret, after a second press: the receiver must change to it at once. */
export function RotateWebhookSecretButton({ webhook, onRotated }: RotateWebhookSecretButtonProps) {
  const text = webhookListText.rotate;

  return (
    <ConfirmButton
      label={text.button}
      confirmText={text.confirm}
      confirmLabel={text.confirmButton}
      pendingLabel={text.pending}
      cancelLabel={webhookListText.back}
      variant="secondary"
      action={() => rotateWebhookSecretAction({ id: webhook.id, version: webhook.version })}
      onDone={onRotated}
    />
  );
}
