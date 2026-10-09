'use client';

import { useId, useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Input, Textarea } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { markDeceasedAction } from '../../actions/deceased-actions';
import { petForms } from '../../content/pet-forms';
import { recordDeath } from '../../content/record-death';

interface RecordDeathFormProps {
  petId: string;
  onCancel: () => void;
  /** Called with how many future visits were cancelled. */
  onDone: (cancelled: number) => void;
}

/** The confirmation step: what closing the record does, when it happened, and an optional note. */
export function RecordDeathForm({ petId, onCancel, onDone }: RecordDeathFormProps) {
  const text = recordDeath;
  const id = useId();
  const [occurredAt, setOccurredAt] = useState('');
  const [note, setNote] = useState('');
  const [futureError, setFutureError] = useState<string>();
  const { errors, message, pending, submit } = useFormSubmit(petForms);

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    // A `datetime-local` value is clinic wall time; Bangkok keeps one offset all year.
    const at = occurredAt ? `${occurredAt}:00+07:00` : undefined;
    const future = at !== undefined && new Date(at).getTime() > Date.now();
    setFutureError(future ? text.futureTime : undefined);
    if (future) return;

    submit(() => markDeceasedAction({ petId, occurredAt: at, note }), onDone);
  }

  return (
    <form onSubmit={save} className="space-y-3 rounded-lg border border-red-200 bg-red-50/60 p-4">
      <p className="text-sm font-medium text-red-900">{text.intro}</p>
      <ul className="list-disc space-y-1 pl-5 text-sm text-red-900">
        {text.effects.map((effect) => (
          <li key={effect}>{effect}</li>
        ))}
      </ul>

      <Field label={text.occurredAt} htmlFor={`${id}-at`} hint={text.occurredAtHint} error={futureError ?? errors.occurred_at}>
        <Input id={`${id}-at`} type="datetime-local" value={occurredAt} onChange={(event) => setOccurredAt(event.target.value)} />
      </Field>

      <Field label={text.note} htmlFor={`${id}-note`} hint={text.noteHint} error={errors.note}>
        <Textarea id={`${id}-note`} value={note} maxLength={1000} onChange={(event) => setNote(event.target.value)} />
      </Field>

      <div className="flex flex-wrap items-center justify-end gap-2">
        <FormStatus message={message} savedText="" />
        <Button variant="ghost" size="sm" disabled={pending} onClick={onCancel}>
          {text.cancel}
        </Button>
        <Button type="submit" variant="danger" size="sm" disabled={pending}>
          {pending ? text.working : text.confirm}
        </Button>
      </div>
    </form>
  );
}
