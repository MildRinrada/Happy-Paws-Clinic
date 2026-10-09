'use client';

import { revokeApiKeyAction } from '../actions/manage-integration-actions';
import { apiKeyListText } from '../content/api-key-list';
import { ConfirmButton } from './confirm-button';

/** Revokes a key after a second press. Whatever uses it is refused from its next request. */
export function RevokeApiKeyButton({ id }: { id: string }) {
  const text = apiKeyListText.revoke;

  return (
    <ConfirmButton
      label={text.button}
      confirmText={text.confirm}
      confirmLabel={text.confirmButton}
      pendingLabel={text.pending}
      cancelLabel={text.back}
      action={() => revokeApiKeyAction({ id })}
    />
  );
}
