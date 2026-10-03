'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Input } from '@/components/ui/field';
import { addDiagnosisAction } from '../../actions/consultation-actions';
import { consultationPage } from '../../content/consultation-page';
import { usePartSubmit } from '../../hooks/use-part-submit';

/** Adds a diagnosis. The first one is primary unless the vet says otherwise. */
export function DiagnosisForm({ consultationId, hasPrimary }: { consultationId: string; hasPrimary: boolean }) {
  const text = consultationPage.diagnoses.fields;
  const [description, setDescription] = useState('');
  const [code, setCode] = useState('');
  const [note, setNote] = useState('');
  const [isPrimary, setIsPrimary] = useState(!hasPrimary);
  const { errors, message, pending, submit } = usePartSubmit();

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    submit(
      () => addDiagnosisAction({ consultationId, description, isPrimary, code, note }),
      () => {
        setDescription('');
        setCode('');
        setNote('');
        setIsPrimary(false);
      },
    );
  }

  return (
    <form onSubmit={save} className="space-y-3 border-t border-ink-100 pt-4">
      {message && <p role="alert" className="text-sm text-red-700">{message}</p>}
      <div className="grid gap-3 sm:grid-cols-[1fr_10rem]">
        <Field label={text.description} htmlFor="diagnosis-description" error={errors.description}>
          <Input id="diagnosis-description" value={description} maxLength={500} required onChange={(event) => setDescription(event.target.value)} />
        </Field>
        <Field label={text.code} htmlFor="diagnosis-code" error={errors.code}>
          <Input id="diagnosis-code" value={code} maxLength={40} onChange={(event) => setCode(event.target.value)} />
        </Field>
      </div>
      <Field label={text.note} htmlFor="diagnosis-note" error={errors.note}>
        <Input id="diagnosis-note" value={note} maxLength={500} onChange={(event) => setNote(event.target.value)} />
      </Field>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <label className="flex items-center gap-2 text-sm text-ink-700">
          <input type="checkbox" checked={isPrimary} onChange={(event) => setIsPrimary(event.target.checked)} className="size-4 accent-brand-600" />
          {text.primary}
        </label>
        <Button type="submit" disabled={pending}>
          {pending ? consultationPage.saving : consultationPage.add}
        </Button>
      </div>
    </form>
  );
}
