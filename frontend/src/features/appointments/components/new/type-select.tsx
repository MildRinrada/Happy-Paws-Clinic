'use client';

import { useId } from 'react';
import { Field, Select } from '@/components/ui/field';
import { newAppointment } from '../../content/appointment-calendar';
import type { AppointmentTypeInput } from '../../types/appointment-settings.types';

interface TypeSelectProps {
  types: AppointmentTypeInput[];
  value: string;
  onChange: (code: string) => void;
}

/**
 * The appointment type, from `GET /staff/appointment-types`. The counter may book every type,
 * including the ones customers cannot book online. A type with its own length says so, because the
 * free times above are worked out from the service's length.
 */
export function TypeSelect({ types, value, onChange }: TypeSelectProps) {
  const text = newAppointment.type;
  const id = useId();
  const chosen = types.find((type) => type.code === value);

  return (
    <Field label={text.label} htmlFor={id} hint={chosen?.duration_minutes ? text.durationNote(chosen.duration_minutes) : text.hint}>
      <Select id={id} value={value} onChange={(event) => onChange(event.target.value)}>
        {types.map((type) => (
          <option key={type.code} value={type.code}>
            {type.duration_minutes ? `${type.name} (${text.minutes(type.duration_minutes)})` : type.name}
          </option>
        ))}
      </Select>
    </Field>
  );
}
