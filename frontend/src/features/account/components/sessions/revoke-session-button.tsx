'use client';

import { useState, useTransition } from 'react';
import { Button } from '@/components/ui/button';
import { formMessages } from '@/features/settings/content/form-messages';
import { revokeMySessionAction } from '../../actions/my-account-actions';
import { sessionSettings } from '../../content/session-settings';

/**
 * Signs one other device out, after a second press: the person on that device loses their place
 * mid-task, so a stray tap must not do it. The list refreshes from the server once it is gone.
 */
export function RevokeSessionButton({ sessionId }: { sessionId: string }) {
  const text = sessionSettings;
  const [confirming, setConfirming] = useState(false);
  const [error, setError] = useState<string>();
  const [pending, startTransition] = useTransition();

  function revoke() {
    setError(undefined);
    startTransition(async () => {
      try {
        const result = await revokeMySessionAction({ sessionId });
        if (!result.ok) setError(result.error.message);
      } catch {
        setError(formMessages.unreachable);
      }
    });
  }

  if (!confirming) {
    return (
      <Button variant="danger" size="sm" onClick={() => setConfirming(true)}>
        {text.revoke}
      </Button>
    );
  }

  return (
    <div className="flex flex-col items-start gap-1.5 sm:items-end">
      <div className="flex flex-wrap gap-2">
        <Button variant="ghost" size="sm" disabled={pending} onClick={() => setConfirming(false)}>
          {text.cancel}
        </Button>
        <Button variant="danger" size="sm" disabled={pending} onClick={revoke}>
          {pending ? text.revoking : text.confirm}
        </Button>
      </div>
      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}
    </div>
  );
}
