'use client';

import { useState, useTransition } from 'react';
import { Button } from '@/components/ui/button';
import { formMessages } from '@/features/settings/content/form-messages';
import { deleteAutomationAction } from '../actions/automation-actions';
import { automationsPage } from '../content/automations-page';

/** Deletes a rule after a second press. Its history stays; the rule leaves the list. */
export function DeleteAutomationButton({ id, version }: { id: string; version: number }) {
  const text = automationsPage.manage;
  const [confirming, setConfirming] = useState(false);
  const [error, setError] = useState<string>();
  const [pending, startTransition] = useTransition();

  function remove() {
    setError(undefined);
    startTransition(async () => {
      try {
        const result = await deleteAutomationAction({ id, version });
        if (!result.ok) {
          setError(result.error.code === 'version_conflict' ? formMessages.conflict : result.error.message);
          setConfirming(false);
        }
      } catch {
        setError(formMessages.unreachable);
      }
    });
  }

  if (!confirming) {
    return (
      <span className="flex flex-col gap-1">
        <Button variant="danger" size="sm" onClick={() => setConfirming(true)}>
          {text.remove}
        </Button>
        {error && (
          <span role="alert" className="text-xs text-red-700">
            {error}
          </span>
        )}
      </span>
    );
  }

  return (
    <span className="flex flex-wrap items-center gap-2">
      <span className="text-xs text-ink-600">{text.confirmRemove}</span>
      <Button variant="ghost" size="sm" disabled={pending} onClick={() => setConfirming(false)}>
        {text.cancel}
      </Button>
      <Button variant="danger" size="sm" disabled={pending} onClick={remove}>
        {pending ? text.removing : text.confirmRemoveButton}
      </Button>
    </span>
  );
}
