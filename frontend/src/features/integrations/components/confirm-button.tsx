'use client';

import { useId, useState, useTransition } from 'react';
import { Button } from '@/components/ui/button';
import { formMessages } from '@/features/settings/content/form-messages';
import type { ActionResult } from '@/lib/actions/action-result';

interface ConfirmButtonProps<T> {
  label: string;
  /** What happens if the person goes on, shown beside the second button. */
  confirmText: string;
  confirmLabel: string;
  pendingLabel: string;
  cancelLabel: string;
  variant?: 'danger' | 'secondary';
  action: () => Promise<ActionResult<T>>;
  onDone?: (data: T) => void;
}

/**
 * A button that asks once more before it acts, saying what will happen. Used where a stray tap
 * would break something outside the clinic: a receiver's secret, a key in use, a calendar link.
 */
export function ConfirmButton<T>({
  label,
  confirmText,
  confirmLabel,
  pendingLabel,
  cancelLabel,
  variant = 'danger',
  action,
  onDone,
}: ConfirmButtonProps<T>) {
  const id = useId();
  const [confirming, setConfirming] = useState(false);
  const [error, setError] = useState<string>();
  const [pending, startTransition] = useTransition();

  function run() {
    setError(undefined);
    startTransition(async () => {
      try {
        const result = await action();
        if (result.ok) {
          onDone?.(result.data);
        } else {
          setError(result.error.code === 'version_conflict' ? formMessages.conflict : result.error.message);
        }
      } catch {
        setError(formMessages.unreachable);
      }
      setConfirming(false);
    });
  }

  return (
    <span className="flex flex-col items-start gap-1.5">
      {confirming ? (
        <span className="flex flex-wrap items-center gap-2">
          <span id={`${id}-what`} className="max-w-sm text-xs text-ink-600">
            {confirmText}
          </span>
          <Button variant="ghost" size="sm" disabled={pending} onClick={() => setConfirming(false)}>
            {cancelLabel}
          </Button>
          <Button variant={variant} size="sm" disabled={pending} aria-describedby={`${id}-what`} onClick={run}>
            {pending ? pendingLabel : confirmLabel}
          </Button>
        </span>
      ) : (
        <Button variant={variant} size="sm" onClick={() => setConfirming(true)}>
          {label}
        </Button>
      )}
      {error && (
        <span role="alert" className="text-xs text-red-700">
          {error}
        </span>
      )}
    </span>
  );
}
