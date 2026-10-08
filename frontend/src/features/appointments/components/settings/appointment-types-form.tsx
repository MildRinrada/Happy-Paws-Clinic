'use client';

import { useId, useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { FormStatus } from '@/components/ui/form-status';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { replaceAppointmentTypesAction } from '../../actions/appointment-settings-actions';
import { appointmentSettingsText } from '../../content/appointment-settings';
import { MAX_TYPES, defaultTypes, draftsFrom, emptyType, toTypeInput, typeProblems, type TypeDraft } from '../../lib/appointment-type-draft';
import { AppointmentTypeRow } from './appointment-type-row';

/**
 * The whole set of appointment types, written in one go. The stored set cannot be read, so the form
 * starts from the four the contract names and says so; once saved it shows the set the backend
 * answered with, and sends that version as `If-Match` on the next save.
 */
export function AppointmentTypesForm() {
  const text = appointmentSettingsText.types;
  const idPrefix = useId();
  const [types, setTypes] = useState<TypeDraft[]>(defaultTypes);
  const [version, setVersion] = useState<number>();
  const [storedCount, setStoredCount] = useState<number>();
  const [checked, setChecked] = useState(false);
  const [understood, setUnderstood] = useState(false);
  const { errors, message, saved, pending, submit, touch } = useFormSubmit(appointmentSettingsText.messages);
  const problems = typeProblems(types);
  // The backend names the type in its own sentence ("ระยะเวลาของ dental ต้อง..."), not by field.
  const refusal = message ?? Object.values(errors)[0];

  function change(next: TypeDraft[]) {
    setTypes(next);
    touch();
  }

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setChecked(true);
    if (problems.some(Boolean)) return;

    submit(
      () => replaceAppointmentTypesAction({ version, types: types.map(toTypeInput) }),
      (stored) => {
        setVersion(stored.version);
        setStoredCount(stored.types.length);
        setTypes(draftsFrom(stored.types));
        setChecked(false);
        setUnderstood(false);
      },
    );
  }

  return (
    <form onSubmit={save} noValidate className="space-y-4">
      {saved && storedCount !== undefined && (
        <p role="status" className="rounded-lg border border-emerald-200 bg-emerald-50 px-3.5 py-2.5 text-sm text-emerald-900">
          {text.stored(storedCount)}
        </p>
      )}

      <ol className="space-y-3">
        {types.map((type, index) => (
          <AppointmentTypeRow
            key={type.key}
            type={type}
            index={index}
            idPrefix={idPrefix}
            problem={checked ? problems[index] : undefined}
            disabled={pending}
            onChange={(next) => change(types.map((other, at) => (at === index ? next : other)))}
            onRemove={() => change(types.filter((_, at) => at !== index))}
          />
        ))}
      </ol>

      {types.length < MAX_TYPES ? (
        <Button
          variant="secondary"
          size="sm"
          disabled={pending}
          onClick={() => {
            // A row added a moment ago is not wrong yet; the next save checks every row again.
            setChecked(false);
            change([...types, emptyType()]);
          }}
        >
          {text.add}
        </Button>
      ) : (
        <p className="text-sm text-ink-500">{text.max}</p>
      )}

      <div className="space-y-3 border-t border-ink-100 pt-4">
        <Checkbox label={text.acknowledge} checked={understood} onChange={(event) => setUnderstood(event.target.checked)} />
        <div className="flex flex-wrap items-center justify-end gap-3">
          <FormStatus message={checked && problems.some(Boolean) ? text.fix : refusal} savedText="" />
          <Button type="submit" disabled={!understood || pending}>
            {pending ? appointmentSettingsText.submitting : text.submit}
          </Button>
        </div>
      </div>
    </form>
  );
}
