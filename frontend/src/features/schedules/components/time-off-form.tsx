'use client';

import { useState, type FormEvent } from 'react';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { Field, Input, Select } from '@/components/ui/field';
import { clinicInstant } from '@/features/booking/lib/booking-dates';
import { FormFooter } from '@/features/settings/components/form-footer';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { recordTimeOffAction } from '../actions/schedule-actions';
import { schedulePage } from '../content/schedule-page';
import type { TimeOffResult as Result } from '../types/schedule.types';
import { TimeOffResult } from './time-off-result';

interface TimeOffFormProps {
  people: { id: string; name: string }[];
  defaultDay: string;
}

/** Records leave. It takes effect at once; the answer lists the appointments that need a call. */
export function TimeOffForm({ people, defaultDay }: TimeOffFormProps) {
  const text = schedulePage.timeOff;
  const [staffId, setStaffId] = useState('');
  const [from, setFrom] = useState({ day: defaultDay, time: '00:00' });
  const [to, setTo] = useState({ day: defaultDay, time: '23:59' });
  const [reason, setReason] = useState('');
  const [result, setResult] = useState<Result>();
  const { errors, message, pending, submit } = useFormSubmit(formMessages);
  const names = Object.fromEntries(people.map((person) => [person.id, person.name]));

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setResult(undefined);
    submit(
      () =>
        recordTimeOffAction({
          staff_id: staffId,
          starts_at: clinicInstant(from.day, from.time),
          ends_at: clinicInstant(to.day, to.time),
          reason: reason.trim() || undefined,
        }),
      setResult,
    );
  }

  return (
    <Card>
      <CardHeader title={text.heading} />
      <CardBody>
        <form onSubmit={save} className="space-y-4">
          <Field label={schedulePage.shift.staff} htmlFor="leave-staff" error={errors.staff_id}>
            <Select id="leave-staff" value={staffId} required onChange={(event) => setStaffId(event.target.value)}>
              <option value="" disabled>
                {schedulePage.shift.choose}
              </option>
              {people.map((person) => (
                <option key={person.id} value={person.id}>
                  {person.name}
                </option>
              ))}
            </Select>
          </Field>

          {/* One row each: a date and a time side by side do not fit two to a row in half a page. */}
          <Field label={text.from} htmlFor="leave-from" error={errors.starts_at}>
            <div className="flex flex-wrap gap-2">
              <Input id="leave-from" type="date" value={from.day} required className="max-w-44" onChange={(event) => setFrom({ ...from, day: event.target.value })} />
              <Input type="time" aria-label={text.from} value={from.time} required className="max-w-36" onChange={(event) => setFrom({ ...from, time: event.target.value })} />
            </div>
          </Field>
          <Field label={text.to} htmlFor="leave-to" error={errors.ends_at}>
            <div className="flex flex-wrap gap-2">
              <Input id="leave-to" type="date" min={from.day} value={to.day} required className="max-w-44" onChange={(event) => setTo({ ...to, day: event.target.value })} />
              <Input type="time" aria-label={text.to} value={to.time} required className="max-w-36" onChange={(event) => setTo({ ...to, time: event.target.value })} />
            </div>
          </Field>

          <Field label={text.reason} htmlFor="leave-reason" error={errors.reason}>
            <Input id="leave-reason" value={reason} maxLength={200} placeholder={text.reasonPlaceholder} onChange={(event) => setReason(event.target.value)} />
          </Field>

          {result && <TimeOffResult result={result} names={names} />}
          <FormFooter pending={pending} message={message} submitLabel={text.submit} pendingLabel={text.submitting} />
          <p className="text-xs text-ink-500">{text.note}</p>
        </form>
      </CardBody>
    </Card>
  );
}
