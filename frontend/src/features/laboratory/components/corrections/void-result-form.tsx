'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Textarea } from '@/components/ui/field';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { voidLabResultAction } from '../../actions/correction-actions';
import { labPage } from '../../content/lab-page';
import type { LabResult } from '../../types/lab.types';

interface VoidResultFormProps {
  labRequestId: string;
  version: number;
  result: LabResult;
  onDone: () => void;
}

/** Strikes one value out, with the reason on record. For a value that should never have been entered. */
export function VoidResultForm({ labRequestId, version, result, onDone }: VoidResultFormProps) {
  const text = labPage.correct;
  const [reason, setReason] = useState('');
  const [missing, setMissing] = useState(false);
  const { errors, message, pending, submit } = useFormSubmit(labPage.messages);

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setMissing(!reason.trim());
    if (!reason.trim()) return;

    submit(() => voidLabResultAction({ labRequestId, resultId: result.id, version, reason }), onDone);
  }

  return (
    <form onSubmit={save} className="space-y-3">
      <Field label={text.reason} htmlFor={`void-${result.id}-reason`} error={missing ? text.noReason : errors.reason}>
        <Textarea
          id={`void-${result.id}-reason`}
          rows={2}
          value={reason}
          maxLength={500}
          placeholder={text.voidReasonPlaceholder}
          onChange={(event) => setReason(event.target.value)}
        />
      </Field>
      <p className="text-xs text-ink-500">{text.voidHint}</p>
      <div className="flex flex-wrap items-center justify-end gap-2">
        {message && (
          <p role="alert" className="text-sm text-red-700">
            {message}
          </p>
        )}
        <Button variant="ghost" size="sm" disabled={pending} onClick={onDone}>
          {text.cancel}
        </Button>
        <Button type="submit" variant="danger" size="sm" disabled={pending}>
          {pending ? text.working : text.voidSubmit}
        </Button>
      </div>
    </form>
  );
}
