'use client';

import { useId, useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Textarea } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { undoDeceasedAction } from '../../actions/deceased-actions';
import { petForms } from '../../content/pet-forms';
import { undoDeath } from '../../content/undo-death';
import type { CancelledVisit } from '../../types/pet.types';
import { EffectList } from './effect-list';

interface UndoDeathFormProps {
  petId: string;
  onCancel: () => void;
  /** Called with the visits the death cancelled, which stay cancelled. */
  onDone: (cancelled: CancelledVisit[]) => void;
}

/** The confirmation step: what undoing does and does not do, and the reason, which is required. */
export function UndoDeathForm({ petId, onCancel, onDone }: UndoDeathFormProps) {
  const text = undoDeath;
  const id = useId();
  const [reason, setReason] = useState('');
  const [missing, setMissing] = useState(false);
  const { errors, message, pending, submit } = useFormSubmit(petForms);

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const blank = reason.trim() === '';
    setMissing(blank);
    if (blank) return;

    submit(() => undoDeceasedAction({ petId, reason }), (result) => onDone(result.cancelled_appointments));
  }

  return (
    <form onSubmit={save} className="space-y-3 rounded-lg border border-amber-200 bg-amber-50/60 p-4">
      <p className="text-sm font-medium text-amber-900">{text.intro}</p>
      <EffectList title={text.will} items={text.effects} />
      <EffectList title={text.wont} items={text.nonEffects} />

      <Field
        label={text.reason}
        htmlFor={`${id}-reason`}
        hint={text.reasonHint}
        error={missing ? text.reasonRequired : errors.reason}
      >
        <Textarea
          id={`${id}-reason`}
          value={reason}
          maxLength={500}
          required
          placeholder={text.reasonPlaceholder}
          aria-invalid={missing || Boolean(errors.reason)}
          onChange={(event) => setReason(event.target.value)}
        />
      </Field>

      <div className="flex flex-wrap items-center justify-end gap-2">
        <FormStatus message={message} savedText="" />
        <Button variant="ghost" size="sm" disabled={pending} onClick={onCancel}>
          {text.cancel}
        </Button>
        <Button type="submit" size="sm" disabled={pending}>
          {pending ? text.working : text.confirm}
        </Button>
      </div>
    </form>
  );
}
