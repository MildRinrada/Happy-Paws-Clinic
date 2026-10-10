'use client';

import { useId, useState, useTransition } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Textarea } from '@/components/ui/field';
import { draftAftercareAction } from '../../actions/staff-ai-actions';
import { aiCommon } from '../../content/ai-common';
import { ownerMessageText } from '../../content/owner-message';
import { AiDraftNotice } from '../common/ai-draft-notice';
import { CopyButton } from '../common/copy-button';

/**
 * Aftercare instructions from the case, in plain Thai, to read, correct and copy into whatever
 * the clinic sends them by. Nothing is sent from here.
 */
export function AftercareDraftPanel({ consultationId }: { consultationId: string }) {
  const id = useId();
  const text = ownerMessageText.aftercare;
  const [draft, setDraft] = useState<string>();
  const [error, setError] = useState<string>();
  const [pending, startTransition] = useTransition();

  function run() {
    setError(undefined);
    startTransition(async () => {
      try {
        const result = await draftAftercareAction(consultationId);
        if (result.ok) setDraft(result.data.text);
        else setError(result.error.message);
      } catch {
        setError(aiCommon.unreachable);
      }
    });
  }

  return (
    <div className="space-y-3">
      {draft === undefined && <p className="text-sm text-ink-600">{text.intro}</p>}

      {draft !== undefined && (
        <div className="space-y-3 rounded-lg border border-amber-200 bg-amber-50/40 p-3">
          <AiDraftNotice />
          <Field label={text.textLabel} htmlFor={id}>
            <Textarea id={id} value={draft} rows={10} onChange={(event) => setDraft(event.target.value)} />
          </Field>
          <CopyButton text={draft} />
        </div>
      )}

      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}

      <Button variant={draft === undefined ? 'primary' : 'secondary'} size="sm" onClick={run} disabled={pending}>
        {pending ? aiCommon.working : draft === undefined ? text.run : text.rerun}
      </Button>
    </div>
  );
}
