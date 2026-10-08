'use client';

import { useState, useTransition } from 'react';
import { Button } from '@/components/ui/button';
import { unlinkLineAction } from '../../actions/line-actions';
import { customerLine } from '../../content/customer-line';

/** Takes the LINE address off the record after a second press. Messages then go by SMS or email. */
export function UnlinkLineButton({ customerId, version }: { customerId: string; version: number }) {
  const text = customerLine;
  const [confirming, setConfirming] = useState(false);
  const [error, setError] = useState<string>();
  const [pending, startTransition] = useTransition();

  function unlink() {
    setError(undefined);
    startTransition(async () => {
      try {
        const result = await unlinkLineAction({ customerId, version });
        if (!result.ok) setError(result.error.code === 'version_conflict' ? text.conflict : result.error.message);
      } catch {
        setError(text.unreachable);
      }
      setConfirming(false);
    });
  }

  return (
    <div className="space-y-1.5">
      {confirming ? (
        <div className="flex flex-wrap items-center gap-2">
          <span className="text-xs text-ink-600">{text.confirmUnlink}</span>
          <Button variant="ghost" size="sm" disabled={pending} onClick={() => setConfirming(false)}>
            {text.cancel}
          </Button>
          <Button variant="danger" size="sm" disabled={pending} onClick={unlink}>
            {pending ? text.unlinking : text.confirmUnlinkButton}
          </Button>
        </div>
      ) : (
        <Button variant="danger" size="sm" onClick={() => setConfirming(true)}>
          {text.unlink}
        </Button>
      )}
      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}
    </div>
  );
}
