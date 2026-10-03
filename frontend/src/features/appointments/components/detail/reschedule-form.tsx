'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Input, Select } from '@/components/ui/field';
import { clinicDay, formatTime } from '@/lib/format/datetime';
import { appointmentDetail } from '../../content/appointment-calendar';
import type { Appointment } from '../../types/appointment.types';
import type { VetOption } from '../../types/calendar.types';

interface RescheduleFormProps {
  appointment: Appointment;
  vets: VetOption[];
  pending: boolean;
  /** "HH:mm" times the backend offered after refusing the last try. */
  suggested: string[];
  onSubmit: (day: string, time: string, staffId?: string) => void;
  onBack: () => void;
}

/** A new day and time, and optionally another vet. Starts from the current ones. */
export function RescheduleForm({ appointment, vets, pending, suggested, onSubmit, onBack }: RescheduleFormProps) {
  const text = appointmentDetail.rescheduleForm;
  const [day, setDay] = useState(clinicDay(appointment.starts_at));
  const [time, setTime] = useState(formatTime(appointment.starts_at));
  const [staffId, setStaffId] = useState('');

  return (
    <form
      className="space-y-3"
      onSubmit={(event) => {
        event.preventDefault();
        onSubmit(day, time, staffId || undefined);
      }}
    >
      <div className="grid gap-3 sm:grid-cols-2">
        <Field label={text.date} htmlFor="reschedule-day">
          <Input id="reschedule-day" type="date" value={day} required onChange={(event) => setDay(event.target.value)} />
        </Field>
        <Field label={text.time} htmlFor="reschedule-time">
          <Input id="reschedule-time" type="time" step={300} value={time} required onChange={(event) => setTime(event.target.value)} />
        </Field>
      </div>

      <Field label={text.vet} htmlFor="reschedule-vet">
        <Select id="reschedule-vet" value={staffId} onChange={(event) => setStaffId(event.target.value)}>
          <option value="">{text.sameVet}</option>
          {vets.map((vet) => (
            <option key={vet.id} value={vet.id}>
              {vet.name}
            </option>
          ))}
        </Select>
      </Field>

      {suggested.length > 0 && (
        <div className="space-y-1.5">
          <p className="text-sm text-ink-600">{text.suggested}</p>
          <div className="flex flex-wrap gap-2">
            {suggested.map((option) => (
              <Button key={option} variant="secondary" size="sm" onClick={() => setTime(option)}>
                {option}
              </Button>
            ))}
          </div>
        </div>
      )}

      <div className="flex justify-end gap-2">
        <Button variant="ghost" size="sm" onClick={onBack} disabled={pending}>
          {text.back}
        </Button>
        <Button type="submit" size="sm" disabled={pending}>
          {pending ? appointmentDetail.actions.working : text.submit}
        </Button>
      </div>
    </form>
  );
}
