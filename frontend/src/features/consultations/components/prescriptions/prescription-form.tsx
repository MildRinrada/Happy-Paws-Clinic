'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Input } from '@/components/ui/field';
import { addPrescriptionAction } from '../../actions/consultation-actions';
import { consultationPage } from '../../content/consultation-page';
import { usePartSubmit } from '../../hooks/use-part-submit';
import { AllergyConflict, type ConflictInfo } from './allergy-conflict';

const empty = { drugName: '', dose: '', frequency: '', durationDays: '', quantity: '', instructions: '' };

/**
 * Writes a prescription. The backend checks it against the patient's allergies; a match stops the
 * form with the allergy named, and the vet either changes the drug or gives a reason to go ahead.
 */
export function PrescriptionForm({ consultationId }: { consultationId: string }) {
  const text = consultationPage.prescriptions.fields;
  const [values, setValues] = useState(empty);
  const [conflict, setConflict] = useState<ConflictInfo>();
  const [key, setKey] = useState(() => crypto.randomUUID());
  const { errors, message, pending, submit } = usePartSubmit();

  const set = (field: keyof typeof empty) => (value: string) => {
    setValues((current) => ({ ...current, [field]: value }));
    if (field === 'drugName') setConflict(undefined);
  };

  function send(acknowledgeAllergy?: string) {
    submit(
      () =>
        addPrescriptionAction({
          consultationId,
          drugName: values.drugName,
          dose: values.dose,
          frequency: values.frequency,
          durationDays: Number(values.durationDays),
          quantity: Number(values.quantity),
          instructions: values.instructions,
          acknowledgeAllergy,
          idempotencyKey: key,
        }),
      () => {
        setValues(empty);
        setConflict(undefined);
        setKey(crypto.randomUUID());
      },
      (refused) => {
        // Nothing was written, so the next try is a request of its own.
        setKey(crypto.randomUUID());
        if (refused.error.code !== 'allergy_conflict') return false;
        setConflict({ message: refused.error.message, overridable: refused.error.details?.overridable === true });
        return true;
      },
    );
  }

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    send();
  }

  return (
    <form onSubmit={save} className="space-y-3 border-t border-ink-100 pt-4">
      {message && <p role="alert" className="text-sm text-red-700">{message}</p>}
      <div className="grid gap-3 sm:grid-cols-2">
        <Field label={text.drug} htmlFor="rx-drug" error={errors.drug_name}>
          <Input id="rx-drug" value={values.drugName} maxLength={200} required onChange={(event) => set('drugName')(event.target.value)} />
        </Field>
        <Field label={text.dose} htmlFor="rx-dose" error={errors.dose}>
          <Input id="rx-dose" value={values.dose} placeholder={text.dosePlaceholder} maxLength={100} required onChange={(event) => set('dose')(event.target.value)} />
        </Field>
        <Field label={text.frequency} htmlFor="rx-frequency" error={errors.frequency}>
          <Input id="rx-frequency" value={values.frequency} placeholder={text.frequencyPlaceholder} maxLength={100} required onChange={(event) => set('frequency')(event.target.value)} />
        </Field>
        <div className="grid grid-cols-2 gap-3">
          <Field label={text.days} htmlFor="rx-days" error={errors.duration_days}>
            <Input id="rx-days" type="number" min={1} step={1} value={values.durationDays} required onChange={(event) => set('durationDays')(event.target.value)} />
          </Field>
          <Field label={text.quantity} htmlFor="rx-quantity" error={errors.quantity}>
            <Input id="rx-quantity" type="number" min={1} step={1} value={values.quantity} required onChange={(event) => set('quantity')(event.target.value)} />
          </Field>
        </div>
      </div>
      <Field label={text.instructions} htmlFor="rx-instructions" error={errors.instructions}>
        <Input id="rx-instructions" value={values.instructions} maxLength={500} onChange={(event) => set('instructions')(event.target.value)} />
      </Field>

      {conflict ? (
        <AllergyConflict conflict={conflict} pending={pending} onConfirm={(reason) => send(reason)} />
      ) : (
        <div className="flex justify-end">
          <Button type="submit" disabled={pending}>
            {pending ? consultationPage.saving : consultationPage.add}
          </Button>
        </div>
      )}
    </form>
  );
}
