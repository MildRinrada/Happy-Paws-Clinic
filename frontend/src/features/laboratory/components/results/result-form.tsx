'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { recordLabResultsAction } from '../../actions/lab-actions';
import { labPage } from '../../content/lab-page';
import { otherErrors } from '../../lib/field-error';
import { checkResultDrafts, emptyResult, type ResultDraft } from '../../lib/result-drafts';
import { ResultRowList } from './result-row-list';

interface ResultFormProps {
  labRequestId: string;
  /** A request that has results already gets "เพิ่มผล" rather than "ลงผล". */
  hasResults: boolean;
}

/**
 * Enters results against a request, closed until asked for so the worklist stays short. Labs
 * report in parts, so this adds to what is there; a value saved cannot be taken back.
 */
export function ResultForm({ labRequestId, hasResults }: ResultFormProps) {
  const text = labPage.resultForm;
  const [open, setOpen] = useState(false);
  const [drafts, setDrafts] = useState<ResultDraft[]>(() => [emptyResult()]);
  const [rowErrors, setRowErrors] = useState<Record<string, string>>({});
  const [localMessage, setLocalMessage] = useState<string>();
  const { errors, message, saved, pending, submit } = useFormSubmit(labPage.messages);
  // A refusal that names a row (`results[1].unit`) has no box of its own here; say it beside the button.
  const refusal = localMessage ?? message ?? otherErrors(errors, [])[0];

  function close() {
    setOpen(false);
    setDrafts([emptyResult()]);
    setRowErrors({});
    setLocalMessage(undefined);
  }

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const checked = checkResultDrafts(drafts);
    setRowErrors(checked.ok ? {} : checked.rowErrors);
    setLocalMessage(checked.ok ? undefined : checked.message);
    if (!checked.ok) return;

    submit(() => recordLabResultsAction({ labRequestId, results: checked.results }), close);
  }

  if (!open) {
    return (
      <div className="flex flex-wrap items-center gap-3">
        <Button variant="secondary" size="sm" onClick={() => setOpen(true)}>
          {hasResults ? text.more : text.open}
        </Button>
        {saved && (
          <p role="status" className="text-sm text-emerald-700">
            {text.saved}
          </p>
        )}
      </div>
    );
  }

  return (
    <form onSubmit={save} className="space-y-3 border-t border-ink-100 pt-3">
      <ResultRowList idPrefix={`result-${labRequestId}`} drafts={drafts} errors={rowErrors} onChange={setDrafts} />
      <p className="text-xs text-ink-500">{text.hint}</p>
      <div className="flex flex-wrap items-center justify-end gap-3">
        {refusal && (
          <p role="alert" className="text-sm text-red-700">
            {refusal}
          </p>
        )}
        <Button variant="ghost" size="sm" disabled={pending} onClick={close}>
          {text.cancel}
        </Button>
        <Button type="submit" size="sm" disabled={pending}>
          {pending ? text.working : text.submit}
        </Button>
      </div>
    </form>
  );
}
