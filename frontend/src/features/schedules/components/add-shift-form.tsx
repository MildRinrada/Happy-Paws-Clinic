'use client';

import { useState, type FormEvent } from 'react';
import { Checkbox } from '@/components/ui/checkbox';
import { CheckboxGroup } from '@/components/ui/checkbox-group';
import { Field, Input, Select } from '@/components/ui/field';
import { clinicInstant } from '@/features/booking/lib/booking-dates';
import { WEEKDAY_LABELS } from '@/features/clinic/lib/weekdays';
import type { Weekday } from '@/features/clinic/types/clinic.types';
import { FormFooter } from '@/features/settings/components/form-footer';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { createShiftsAction } from '../actions/schedule-actions';
import { schedulePage } from '../content/schedule-page';
import { weeklyRule } from '../lib/weekly-rule';

interface AddShiftFormProps {
  /** Who can be rostered here. */
  people: { id: string; name: string }[];
  branchId: string;
  /** The day the form starts on. */
  defaultDay: string;
  /** The person the form starts on, when the click that opened it named one. */
  defaultStaffId?: string;
  /** Called once the shifts are on the roster, to shut the dialog. */
  onDone: () => void;
}

const WEEKDAYS = Object.keys(WEEKDAY_LABELS) as Weekday[];
const weekdayOf = (day: string) => WEEKDAYS[(new Date(`${day}T00:00:00Z`).getUTCDay() + 6) % 7]!;

/**
 * Puts one shift, or a weekly run of them, on the roster at the branch in view. The backend
 * creates all of them or none, and lists every clash when it refuses. Lives in the roster's
 * dialog; once saved the dialog shuts and the roster shows the new shifts.
 */
export function AddShiftForm({ people, branchId, defaultDay, defaultStaffId, onDone }: AddShiftFormProps) {
  const text = schedulePage.shift;
  const [staffId, setStaffId] = useState(defaultStaffId ?? '');
  const [day, setDay] = useState(defaultDay);
  const [hours, setHours] = useState({ start: '09:00', end: '18:00' });
  const [withBreak, setWithBreak] = useState(false);
  const [rest, setRest] = useState({ start: '12:00', end: '13:00' });
  const [repeat, setRepeat] = useState(false);
  const [days, setDays] = useState<string[]>([weekdayOf(defaultDay)]);
  const [until, setUntil] = useState('');
  const [noDays, setNoDays] = useState(false);
  const { errors, message, pending, submit } = useFormSubmit(formMessages);

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setNoDays(repeat && days.length === 0);
    if (repeat && days.length === 0) return;

    submit(
      () =>
        createShiftsAction({
          staff_id: staffId,
          branch_id: branchId,
          starts_at: clinicInstant(day, hours.start),
          ends_at: clinicInstant(day, hours.end),
          break: withBreak ? { starts_at: clinicInstant(day, rest.start), ends_at: clinicInstant(day, rest.end) } : undefined,
          rrule: repeat ? weeklyRule(days as Weekday[], until) : undefined,
        }),
      onDone,
    );
  }

  return (
    <form onSubmit={save} className="space-y-4">
      <Field label={text.staff} htmlFor="shift-staff" error={errors.staff_id}>
        <Select id="shift-staff" value={staffId} required onChange={(event) => setStaffId(event.target.value)}>
          <option value="" disabled>
            {text.choose}
          </option>
          {people.map((person) => (
            <option key={person.id} value={person.id}>
              {person.name}
            </option>
          ))}
        </Select>
      </Field>

      <div className="grid gap-4 sm:grid-cols-3">
        <Field label={text.date} htmlFor="shift-date" error={errors.starts_at ?? errors.branch_id}>
          <Input id="shift-date" type="date" value={day} required onChange={(event) => setDay(event.target.value)} />
        </Field>
        <Field label={text.start} htmlFor="shift-start">
          <Input id="shift-start" type="time" value={hours.start} required onChange={(event) => setHours({ ...hours, start: event.target.value })} />
        </Field>
        <Field label={text.end} htmlFor="shift-end" error={errors.ends_at}>
          <Input id="shift-end" type="time" value={hours.end} required onChange={(event) => setHours({ ...hours, end: event.target.value })} />
        </Field>
      </div>

      <Checkbox label={text.withBreak} checked={withBreak} onChange={(event) => setWithBreak(event.target.checked)} />
      {withBreak && (
        <div className="grid gap-4 sm:grid-cols-3">
          <Field label={text.breakStart} htmlFor="shift-break-start" error={errors.break}>
            <Input id="shift-break-start" type="time" value={rest.start} required onChange={(event) => setRest({ ...rest, start: event.target.value })} />
          </Field>
          <Field label={text.breakEnd} htmlFor="shift-break-end">
            <Input id="shift-break-end" type="time" value={rest.end} required onChange={(event) => setRest({ ...rest, end: event.target.value })} />
          </Field>
        </div>
      )}

      <Checkbox label={text.repeat} hint={text.repeatHint} checked={repeat} onChange={(event) => setRepeat(event.target.checked)} />
      {repeat && (
        <div className="space-y-4 rounded-lg border border-ink-100 bg-ink-50/60 p-3">
          <CheckboxGroup
            legend={text.repeatDays}
            error={noDays ? text.pickDay : undefined}
            options={WEEKDAY_LABELS}
            selected={days}
            onChange={setDays}
          />
          <Field label={text.until} htmlFor="shift-until" error={errors.rrule}>
            <Input id="shift-until" type="date" min={day} value={until} required className="max-w-44" onChange={(event) => setUntil(event.target.value)} />
          </Field>
        </div>
      )}

      <FormFooter pending={pending} message={message} submitLabel={text.submit} pendingLabel={text.submitting} />
    </form>
  );
}
