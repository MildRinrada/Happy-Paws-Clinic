'use client';

import { useState, type FormEvent } from 'react';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { Field, Select } from '@/components/ui/field';
import { SectionFooter } from '@/features/settings/components/section-footer';
import { formMessages } from '@/features/settings/content/form-messages';
import { useUnsavedChanges } from '@/features/settings/hooks/use-unsaved-changes';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { updateBookingSettingsAction } from '../../actions/clinic-rules-actions';
import { rulesSettings, SLOT_CHOICES } from '../../content/rules-settings';
import type { BookingSettings, BookingSettingsInput } from '../../types/settings.types';
import { NumberField } from './number-field';

const FORM_ID = 'booking-rules-form';

/** Slot length, lead time, cancellation cutoff and how far ahead booking opens. Saved by the save bar. */
export function BookingRulesForm({ settings }: { settings: BookingSettings }) {
  const text = rulesSettings.booking;
  const { version: initialVersion, ...initial } = settings;
  const [values, setValues] = useState<BookingSettingsInput>(initial);
  const [version, setVersion] = useState(initialVersion);
  const { errors, message, saved, pending, submit, touch, clear } = useFormSubmit(formMessages);
  const changes = useUnsavedChanges({
    formId: FORM_ID,
    section: text.heading,
    value: values,
    pending,
    failed: Boolean(message) || Object.keys(errors).length > 0,
    onReset: (stored) => {
      clear();
      setValues(stored);
    },
  });

  const set = (field: keyof BookingSettingsInput) => (value: number) => {
    touch();
    setValues((current) => ({ ...current, [field]: value }));
  };

  // A clinic set up before these choices existed keeps its own length in the list.
  const slots = SLOT_CHOICES.includes(values.slot_minutes) ? SLOT_CHOICES : [...SLOT_CHOICES, values.slot_minutes].sort((a, b) => a - b);

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const sent = values;

    submit(
      () => updateBookingSettingsAction({ version, values: sent }),
      (stored) => {
        setVersion(stored.version);
        changes.markSaved(sent);
      },
    );
  }

  return (
    <Card>
      <CardHeader title={text.heading} />
      <CardBody>
        <form id={FORM_ID} onSubmit={save} className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-2">
            <Field label={text.slotMinutes} htmlFor="rules-slot" hint={text.slotMinutesHint} error={errors.slot_minutes}>
              <Select id="rules-slot" value={values.slot_minutes} className="max-w-40" onChange={(event) => set('slot_minutes')(Number(event.target.value))}>
                {slots.map((minutes) => (
                  <option key={minutes} value={minutes}>
                    {text.minutes(minutes)}
                  </option>
                ))}
              </Select>
            </Field>

            <NumberField
              id="rules-lead"
              label={text.leadTimeMinutes}
              hint={text.leadTimeMinutesHint}
              error={errors.lead_time_minutes}
              value={values.lead_time_minutes}
              min={0}
              max={43200}
              onChange={set('lead_time_minutes')}
            />

            <NumberField
              id="rules-cutoff"
              label={text.cancellationCutoffHours}
              hint={text.cancellationCutoffHoursHint}
              error={errors.cancellation_cutoff_hours}
              value={values.cancellation_cutoff_hours}
              min={0}
              max={720}
              onChange={set('cancellation_cutoff_hours')}
            />

            <NumberField
              id="rules-advance"
              label={text.maxAdvanceDays}
              error={errors.max_advance_days}
              value={values.max_advance_days}
              min={1}
              max={365}
              onChange={set('max_advance_days')}
            />

            <NumberField
              id="rules-walk-in"
              label={text.walkInSlotsPerHour}
              hint={text.walkInSlotsPerHourHint}
              error={errors.walk_in_slots_per_hour}
              value={values.walk_in_slots_per_hour}
              min={0}
              max={60}
              onChange={set('walk_in_slots_per_hour')}
            />
          </div>

          <SectionFooter pending={pending} message={message} saved={saved} inBar={changes.inBar} />
        </form>
      </CardBody>
    </Card>
  );
}
