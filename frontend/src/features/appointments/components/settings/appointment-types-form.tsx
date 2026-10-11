'use client';

import { useId, useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { SectionFooter } from '@/features/settings/components/section-footer';
import { useUnsavedChanges } from '@/features/settings/hooks/use-unsaved-changes';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { replaceAppointmentTypesAction } from '../../actions/appointment-settings-actions';
import { appointmentSettingsText } from '../../content/appointment-settings';
import { MAX_TYPES, draftsFrom, emptyType, toTypeInput, typeProblems, type TypeDraft } from '../../lib/appointment-type-draft';
import type { AppointmentTypes } from '../../types/appointment-settings.types';
import { AppointmentTypeRow } from './appointment-type-row';

const FORM_ID = 'appointment-types-form';

/**
 * The whole set of appointment types, written in one go by the settings save bar. The form starts from the stored set and
 * sends the version it was read at as `If-Match`; once saved it shows the set the backend answered
 * with, and sends that version on the next save.
 */
export function AppointmentTypesForm({ stored: initial }: { stored: AppointmentTypes }) {
  const text = appointmentSettingsText.types;
  const idPrefix = useId();
  const [types, setTypes] = useState<TypeDraft[]>(() => draftsFrom(initial.types));
  const [version, setVersion] = useState(initial.version);
  const [storedCount, setStoredCount] = useState<number>();
  const [checked, setChecked] = useState(false);
  const { errors, message, saved, pending, submit, touch, clear } = useFormSubmit(appointmentSettingsText.messages);
  const problems = typeProblems(types);
  // The backend names the type in its own sentence ("ระยะเวลาของ dental ต้อง..."), not by field.
  const refusal = message ?? Object.values(errors)[0];
  const unfixed = checked && problems.some(Boolean);
  const changes = useUnsavedChanges({
    formId: FORM_ID,
    section: text.heading,
    value: types,
    pending,
    failed: Boolean(refusal) || unfixed,
    onReset: (stored) => {
      clear();
      setChecked(false);
      setTypes(stored);
    },
  });

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
        const next = draftsFrom(stored.types);
        setVersion(stored.version);
        setStoredCount(stored.types.length);
        setTypes(next);
        setChecked(false);
        changes.markSaved(next);
      },
    );
  }

  return (
    <form id={FORM_ID} onSubmit={save} noValidate className="space-y-4">
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

      <SectionFooter pending={pending} message={unfixed ? text.fix : refusal} inBar={changes.inBar} submitLabel={text.submit} />
    </form>
  );
}
