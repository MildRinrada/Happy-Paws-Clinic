'use client';

import { useState, useTransition, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Input, Select } from '@/components/ui/field';
import { fieldErrors } from '@/lib/actions/field-errors';
import { recordAllergyAction } from '../../actions/pet-actions';
import { petProfile } from '../../content/pet-profile';
import type { AllergySeverity } from '../../types/pet.types';

/** Records an allergy. Opens from a button so a stray tap does not start a clinical entry. */
export function AddAllergyForm({ petId }: { petId: string }) {
  const text = petProfile.allergies;
  const [open, setOpen] = useState(false);
  const [substance, setSubstance] = useState('');
  const [severity, setSeverity] = useState<AllergySeverity>('moderate');
  const [reaction, setReaction] = useState('');
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [message, setMessage] = useState<string>();
  const [pending, startTransition] = useTransition();

  function reset() {
    setOpen(false);
    setSubstance('');
    setSeverity('moderate');
    setReaction('');
    setErrors({});
    setMessage(undefined);
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setErrors({});
    setMessage(undefined);

    startTransition(async () => {
      const result = await recordAllergyAction({ petId, substance, severity, reaction });

      if (result.ok) {
        reset();
        return;
      }

      const byField = fieldErrors(result.error);
      setErrors(byField);
      if (Object.keys(byField).length === 0) setMessage(result.error.message);
    });
  }

  if (!open) {
    return (
      <Button variant="secondary" size="sm" onClick={() => setOpen(true)}>
        {text.add}
      </Button>
    );
  }

  return (
    <form onSubmit={submit} className="space-y-3 rounded-lg border border-ink-200 bg-ink-50/60 p-4">
      {message && <p role="alert" className="text-sm text-red-700">{message}</p>}

      <Field label={text.form.substance} htmlFor="allergy-substance" error={errors.substance}>
        <Input
          id="allergy-substance"
          value={substance}
          placeholder={text.form.substancePlaceholder}
          maxLength={200}
          required
          autoFocus
          onChange={(event) => setSubstance(event.target.value)}
        />
      </Field>

      <Field label={text.form.severity} htmlFor="allergy-severity" hint={text.form.severityHint} error={errors.severity}>
        <Select id="allergy-severity" value={severity} onChange={(event) => setSeverity(event.target.value as AllergySeverity)}>
          {(['mild', 'moderate', 'severe'] as const).map((level) => (
            <option key={level} value={level}>
              {text.severity[level]}
            </option>
          ))}
        </Select>
      </Field>

      <Field label={text.form.reaction} htmlFor="allergy-reaction" error={errors.reaction}>
        <Input id="allergy-reaction" value={reaction} maxLength={500} onChange={(event) => setReaction(event.target.value)} />
      </Field>

      <div className="flex justify-end gap-2">
        <Button variant="ghost" size="sm" onClick={reset} disabled={pending}>
          {text.form.cancel}
        </Button>
        <Button type="submit" size="sm" disabled={pending}>
          {pending ? text.form.working : text.form.submit}
        </Button>
      </div>
    </form>
  );
}
