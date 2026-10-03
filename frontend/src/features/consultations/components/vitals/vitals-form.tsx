'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Input } from '@/components/ui/field';
import { recordVitalsAction } from '../../actions/consultation-actions';
import { consultationPage } from '../../content/consultation-page';
import { usePartSubmit } from '../../hooks/use-part-submit';

const toNumber = (value: string) => (value.trim() === '' ? undefined : Number(value));

/** Weight, temperature, pulse and breathing. Any one of them is enough to save. */
export function VitalsForm({ consultationId }: { consultationId: string }) {
  const text = consultationPage.vitals;
  const empty = { weight: '', temperature: '', heartRate: '', respiratoryRate: '', note: '' };
  const [values, setValues] = useState(empty);
  const [localError, setLocalError] = useState<string>();
  const { errors, message, pending, submit } = usePartSubmit();

  const set = (field: keyof typeof empty) => (value: string) => setValues((current) => ({ ...current, [field]: value }));

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const readings = [values.weight, values.temperature, values.heartRate, values.respiratoryRate];
    if (readings.every((value) => value.trim() === '')) {
      setLocalError(text.empty);
      return;
    }
    setLocalError(undefined);

    submit(
      () =>
        recordVitalsAction({
          consultationId,
          weightKg: toNumber(values.weight),
          temperatureC: toNumber(values.temperature),
          heartRateBpm: toNumber(values.heartRate),
          respiratoryRateBpm: toNumber(values.respiratoryRate),
          note: values.note,
        }),
      () => setValues(empty),
    );
  }

  const fields = [
    { key: 'weight', label: text.fields.weight, step: '0.01', error: errors.weight_kg },
    { key: 'temperature', label: text.fields.temperature, step: '0.1', error: errors.temperature_c },
    { key: 'heartRate', label: text.fields.heartRate, step: '1', error: errors.heart_rate_bpm },
    { key: 'respiratoryRate', label: text.fields.respiratoryRate, step: '1', error: errors.respiratory_rate_bpm },
  ] as const;

  return (
    <form onSubmit={save} className="space-y-3">
      {(localError || message) && <p role="alert" className="text-sm text-red-700">{localError ?? message}</p>}
      {/* items-end: a label that wraps to two lines must not push its box out of line. */}
      <div className="grid grid-cols-2 items-end gap-3 sm:grid-cols-4">
        {fields.map((field) => (
          <Field key={field.key} label={field.label} htmlFor={`vitals-${field.key}`} error={field.error}>
            <Input
              id={`vitals-${field.key}`}
              type="number"
              inputMode="decimal"
              min={0}
              step={field.step}
              value={values[field.key]}
              onChange={(event) => set(field.key)(event.target.value)}
            />
          </Field>
        ))}
      </div>
      <div className="flex flex-wrap items-end gap-3">
        <div className="min-w-0 flex-1">
          <Field label={text.fields.note} htmlFor="vitals-note">
            <Input id="vitals-note" value={values.note} maxLength={500} onChange={(event) => set('note')(event.target.value)} />
          </Field>
        </div>
        <Button type="submit" disabled={pending}>
          {pending ? consultationPage.saving : consultationPage.save}
        </Button>
      </div>
    </form>
  );
}
