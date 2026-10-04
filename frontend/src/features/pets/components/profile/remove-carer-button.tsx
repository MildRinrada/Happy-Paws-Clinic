'use client';

import { useState, useTransition } from 'react';
import { Button } from '@/components/ui/button';
import { removeCarerAction } from '../../actions/carer-actions';
import { carers } from '../../content/carers';
import { petForms } from '../../content/pet-forms';

/**
 * Withdraws one carer's access. Two presses, because the person loses access to the pet on their
 * next request. Their past visits stay on record.
 */
export function RemoveCarerButton({ petId, customerId }: { petId: string; customerId: string }) {
  const text = carers;
  const [confirming, setConfirming] = useState(false);
  const [error, setError] = useState<string>();
  const [pending, startTransition] = useTransition();

  function remove() {
    setError(undefined);
    startTransition(async () => {
      try {
        const result = await removeCarerAction({ petId, customerId });
        if (!result.ok) setError(result.error.message);
      } catch {
        setError(petForms.unreachable);
      }
    });
  }

  if (!confirming) {
    return (
      <Button variant="ghost" size="sm" onClick={() => setConfirming(true)}>
        {text.remove}
      </Button>
    );
  }

  return (
    <div className="w-full space-y-2 rounded-lg bg-red-50 px-3 py-2">
      <p className="text-xs text-red-800">{text.removeHint}</p>
      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}
      <div className="flex justify-end gap-2">
        <Button variant="ghost" size="sm" disabled={pending} onClick={() => setConfirming(false)}>
          {petForms.cancel}
        </Button>
        <Button variant="danger" size="sm" disabled={pending} onClick={remove}>
          {pending ? text.removing : text.confirmRemove}
        </Button>
      </div>
    </div>
  );
}
