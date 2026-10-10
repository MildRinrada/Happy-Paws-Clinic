'use client';

import { useId, useState, useTransition, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Textarea } from '@/components/ui/field';
import { useOfferFormDraft } from '@/features/consultations/hooks/use-form-draft';
import { draftSoapAction } from '../../actions/staff-ai-actions';
import { aiCommon } from '../../content/ai-common';
import { soapDraftText } from '../../content/soap-draft';
import type { SoapDraft } from '../../types/staff-ai.types';
import { SoapDraftPreview } from './soap-draft-preview';

const MAX_NOTES = 4000;

/**
 * Short notes in, a draft of the case's fields out. "ใส่ลงในฟอร์ม" hands the draft to the forms on
 * the page; the vet saves through them as usual. Nothing here writes to the case.
 */
export function SoapDraftPanel({ consultationId }: { consultationId: string }) {
  const id = useId();
  const text = soapDraftText;
  const offer = useOfferFormDraft();
  const [notes, setNotes] = useState('');
  const [draft, setDraft] = useState<SoapDraft>();
  const [error, setError] = useState<string>();
  const [applied, setApplied] = useState(false);
  const [pending, startTransition] = useTransition();

  function run(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (notes.trim() === '') {
      setError(text.emptyNotes);
      return;
    }

    setError(undefined);
    setApplied(false);
    startTransition(async () => {
      try {
        const result = await draftSoapAction({ consultationId, notes });
        if (result.ok) setDraft(result.data);
        else setError(result.error.message);
      } catch {
        setError(aiCommon.unreachable);
      }
    });
  }

  function apply() {
    if (!draft || !offer) return;

    const { examination, ...notesFields } = draft.draft;
    offer({ notes: notesFields, findings: examination });
    setApplied(true);
  }

  return (
    <div className="space-y-3">
      <p className="text-sm text-ink-600">{text.intro}</p>

      <form onSubmit={run} className="space-y-3">
        <Field label={text.notesLabel} htmlFor={id} hint={text.notesHint} error={error}>
          <Textarea
            id={id}
            value={notes}
            rows={4}
            maxLength={MAX_NOTES}
            placeholder={text.notesPlaceholder}
            aria-invalid={error ? true : undefined}
            aria-describedby={error ? `${id}-error` : undefined}
            onChange={(event) => setNotes(event.target.value)}
          />
        </Field>
        <Button type="submit" size="sm" disabled={pending}>
          {pending ? aiCommon.working : text.run}
        </Button>
      </form>

      {draft && (
        <div className="space-y-3">
          <SoapDraftPreview draft={draft} />
          <div className="flex flex-wrap items-center gap-2">
            <Button size="sm" onClick={apply} disabled={!offer}>
              {text.apply}
            </Button>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => {
                setDraft(undefined);
                setApplied(false);
              }}
            >
              {text.discard}
            </Button>
          </div>
          {applied && (
            <p role="status" className="text-sm text-emerald-700">
              {text.applied}
            </p>
          )}
        </div>
      )}
    </div>
  );
}
