'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Input, Select } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { recordVaccinationAction } from '../../actions/vaccination-actions';
import { petForms } from '../../content/pet-forms';
import { recordVaccination } from '../../content/record-vaccination';

type NextDueMode = keyof typeof recordVaccination.nextDueModes;

const empty = { vaccineName: '', lotNumber: '', vialExpiresOn: '', administeredAt: '', manufacturer: '', note: '' };

/**
 * Records a dose: the vaccine, the lot and the vial's expiry, which a recall searches by. The next
 * dose defaults to a year on; the vet may give a date, a length of cover, or none at all.
 */
export function RecordVaccinationForm({ petId }: { petId: string }) {
  const text = recordVaccination;
  const [open, setOpen] = useState(false);
  const [values, setValues] = useState(empty);
  const [mode, setMode] = useState<NextDueMode>('auto');
  const [nextDueOn, setNextDueOn] = useState('');
  const [validForDays, setValidForDays] = useState('');
  const { errors, message, saved, pending, submit, touch } = useFormSubmit(petForms);

  const set = (field: keyof typeof empty, value: string) => {
    touch();
    setValues((current) => ({ ...current, [field]: value }));
  };

  function close() {
    setOpen(false);
    setValues(empty);
    setMode('auto');
    setNextDueOn('');
    setValidForDays('');
  }

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    submit(
      () =>
        recordVaccinationAction({
          petId,
          ...values,
          // A `datetime-local` value is clinic wall time; Bangkok keeps one offset all year.
          administeredAt: values.administeredAt ? `${values.administeredAt}:00+07:00` : undefined,
          nextDueOn: mode === 'date' ? nextDueOn : undefined,
          validForDays: mode === 'none' ? 0 : mode === 'days' ? Number(validForDays) : undefined,
        }),
      () => {
        setValues(empty);
        setMode('auto');
        setNextDueOn('');
        setValidForDays('');
      },
    );
  }

  if (!open) {
    return (
      <div className="space-y-2">
        <FormStatus saved={saved} savedText={text.saved} />
        <Button variant="secondary" size="sm" onClick={() => setOpen(true)}>
          {text.open}
        </Button>
      </div>
    );
  }

  return (
    <form onSubmit={save} className="space-y-3 rounded-lg border border-ink-200 bg-ink-50/60 p-4">
      <Field label={text.vaccineName} htmlFor="vaccine-name" error={errors.vaccine_name}>
        <Input
          id="vaccine-name"
          value={values.vaccineName}
          placeholder={text.vaccineNamePlaceholder}
          maxLength={200}
          required
          autoFocus
          aria-invalid={Boolean(errors.vaccine_name)}
          onChange={(event) => set('vaccineName', event.target.value)}
        />
      </Field>

      <div className="grid gap-3 sm:grid-cols-2">
        <Field label={text.lotNumber} htmlFor="vaccine-lot" error={errors.lot_number}>
          <Input
            id="vaccine-lot"
            value={values.lotNumber}
            maxLength={100}
            required
            aria-invalid={Boolean(errors.lot_number)}
            onChange={(event) => set('lotNumber', event.target.value)}
          />
        </Field>
        <Field label={text.vialExpiresOn} htmlFor="vaccine-expiry" error={errors.vial_expires_on}>
          <Input
            id="vaccine-expiry"
            type="date"
            value={values.vialExpiresOn}
            required
            aria-invalid={Boolean(errors.vial_expires_on)}
            onChange={(event) => set('vialExpiresOn', event.target.value)}
          />
        </Field>
      </div>

      <Field label={text.administeredAt} htmlFor="vaccine-given" hint={text.administeredAtHint} error={errors.administered_at}>
        <Input
          id="vaccine-given"
          type="datetime-local"
          value={values.administeredAt}
          aria-invalid={Boolean(errors.administered_at)}
          onChange={(event) => set('administeredAt', event.target.value)}
        />
      </Field>

      <Field label={text.nextDue} htmlFor="vaccine-next-mode">
        <Select id="vaccine-next-mode" value={mode} onChange={(event) => { touch(); setMode(event.target.value as NextDueMode); }}>
          {(Object.keys(text.nextDueModes) as NextDueMode[]).map((key) => (
            <option key={key} value={key}>
              {text.nextDueModes[key]}
            </option>
          ))}
        </Select>
      </Field>

      {mode === 'date' && (
        <Field label={text.nextDueOn} htmlFor="vaccine-next-on" error={errors.next_due_on}>
          <Input
            id="vaccine-next-on"
            type="date"
            value={nextDueOn}
            required
            aria-invalid={Boolean(errors.next_due_on)}
            onChange={(event) => { touch(); setNextDueOn(event.target.value); }}
          />
        </Field>
      )}

      {mode === 'days' && (
        <Field label={text.validForDays} htmlFor="vaccine-next-days" hint={text.validForDaysHint} error={errors.valid_for_days}>
          <Input
            id="vaccine-next-days"
            type="number"
            min={1}
            max={1095}
            value={validForDays}
            required
            className="max-w-32"
            aria-invalid={Boolean(errors.valid_for_days)}
            onChange={(event) => { touch(); setValidForDays(event.target.value); }}
          />
        </Field>
      )}

      <Field label={text.manufacturer} htmlFor="vaccine-maker" error={errors.manufacturer}>
        <Input id="vaccine-maker" value={values.manufacturer} maxLength={200} onChange={(event) => set('manufacturer', event.target.value)} />
      </Field>

      <Field label={text.note} htmlFor="vaccine-note" error={errors.note}>
        <Input id="vaccine-note" value={values.note} maxLength={1000} onChange={(event) => set('note', event.target.value)} />
      </Field>

      <div className="flex flex-wrap items-center justify-end gap-2">
        <FormStatus message={message} saved={saved} savedText={text.saved} />
        <Button variant="ghost" size="sm" onClick={close} disabled={pending}>
          {petForms.cancel}
        </Button>
        <Button type="submit" size="sm" disabled={pending}>
          {pending ? petForms.saving : petForms.save}
        </Button>
      </div>
    </form>
  );
}
