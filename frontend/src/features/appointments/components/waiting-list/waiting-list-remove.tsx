'use client';

import { useState, useTransition } from 'react';
import { Button } from '@/components/ui/button';
import { removeFromWaitingListAction } from '../../actions/advanced-booking-actions';
import { waitingListText } from '../../content/advanced-booking';

interface WaitingListRemoveProps {
  entryId: string;
  petName: string;
}

/**
 * "ถอนออก", asked twice because the entry is deleted, not hidden. Removed by someone else first
 * (`404`) says so; the list refreshes either way.
 */
export function WaitingListRemove({ entryId, petName }: WaitingListRemoveProps) {
  const text = waitingListText.remove;
  const [asking, setAsking] = useState(false);
  const [error, setError] = useState<string>();
  const [pending, startRemove] = useTransition();

  function remove() {
    setError(undefined);
    startRemove(async () => {
      try {
        const result = await removeFromWaitingListAction({ id: entryId });
        if (!result.ok) setError(result.error.status === 404 ? text.gone : result.error.message);
      } catch {
        setError(waitingListText.unreachable);
      }
    });
  }

  if (!asking) {
    return (
      <Button variant="ghost" size="sm" onClick={() => setAsking(true)}>
        {text.action}
      </Button>
    );
  }

  return (
    <div className="space-y-2 rounded-md border border-red-200 bg-red-50/60 px-3 py-2">
      <p className="text-sm text-red-900">{text.question(petName)}</p>
      <div className="flex flex-wrap justify-end gap-2">
        <Button variant="secondary" size="sm" disabled={pending} onClick={() => setAsking(false)}>
          {text.keep}
        </Button>
        <Button variant="danger" size="sm" disabled={pending} onClick={remove}>
          {pending ? text.working : text.confirm}
        </Button>
      </div>
      {error && <p role="alert" className="text-sm text-red-700">{error}</p>}
    </div>
  );
}
