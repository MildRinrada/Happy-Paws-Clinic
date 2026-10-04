'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { Field, Select } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { updateBookingSettingsAction } from '../../actions/clinic-rules-actions';
import { rulesSettings, SLOT_CHOICES } from '../../content/rules-settings';
import type { BookingSettings, BookingSettingsInput } from '../../types/settings.types';
import { NumberField } from './number-field';

/** Slot length, lead time, cancellation cutoff and how far ahead booking opens. */
export function BookingRulesForm({ settings }: { settings: BookingSettings }) {
  const text = rulesSettings.booking;
  const { version: initialVersion, ...initial } = settings;
  const [values, setValues] = useState<BookingSettingsInput>(initial);
  const [version, setVersion] = useState(initialVersion);
  const { errors, message, saved, pending, submit, touch } = useFormSubmit(formMessages);

  const set = (field: keyof BookingSettingsInput) => (value: number) => {
    touch();
    setValues((current) => ({ ...current, [field]: value }));
  };

  // A clinic set up before these choices existed keeps its own length in the list.
  const slots = SLOT_CHOICES.includes(values.slot_minutes) ? SLOT_CHOICES : [...SLOT_CHOICES, values.slot_minutes].sort((a, b) => a - b);

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    submit(() => updateBookingSettingsAction({ version, values }), (stored) => setVersion(stored.version));
  }

  return (
    <Card>
      <CardHeader title={text.heading} />
      <CardBody>
        <form onSubmit={save} className="space-y-4">
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

          <div className="flex flex-wrap items-center justify-end gap-3">
            <FormStatus message={message} saved={saved} savedText={formMessages.saved} />
            <Button type="submit" disabled={pending}>
              {pending ? formMessages.saving : formMessages.save}
            </Button>
          </div>
        </form>
      </CardBody>
    </Card>
  );
}
