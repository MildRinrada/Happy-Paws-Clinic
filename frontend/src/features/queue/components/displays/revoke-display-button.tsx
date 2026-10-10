'use client';

import { ConfirmButton } from '@/features/integrations/components/confirm-button';
import { revokeDisplayAction } from '../../actions/display-actions';
import { queueDisplays } from '../../content/queue-displays';

/** Stops a screen after a second press. Its next read of the board is refused. */
export function RevokeDisplayButton({ id }: { id: string }) {
  const text = queueDisplays.revoke;

  return (
    <ConfirmButton
      label={text.button}
      confirmText={text.confirm}
      confirmLabel={text.confirmButton}
      pendingLabel={text.pending}
      cancelLabel={text.back}
      action={() => revokeDisplayAction({ id })}
    />
  );
}
