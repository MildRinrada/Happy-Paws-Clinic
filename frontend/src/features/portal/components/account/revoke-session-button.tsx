'use client';

import { useState, useTransition } from 'react';
import { revokeSessionAction } from '@/features/account/actions/account-actions';
import { portalAccount } from '../../content/portal-account';

/** Signs one other device out. The list refreshes from the server once it is gone. */
export function RevokeSessionButton({ sessionId }: { sessionId: string }) {
  const [pending, startTransition] = useTransition();
  const [error, setError] = useState<string>();

  function revoke() {
    setError(undefined);
    startTransition(async () => {
      try {
        const result = await revokeSessionAction({ sessionId });
        if (!result.ok) setError(result.error.message);
      } catch {
        setError(portalAccount.unreachable);
      }
    });
  }

  return (
    <span className="flex flex-col items-end gap-1">
      <button type="button" onClick={revoke} disabled={pending} className="text-xs font-semibold text-red-700 hover:underline disabled:opacity-50">
        {pending ? portalAccount.sessions.revoking : portalAccount.sessions.revoke}
      </button>
      {error && (
        <span role="alert" className="text-xs text-red-700">
          {error}
        </span>
      )}
    </span>
  );
}
