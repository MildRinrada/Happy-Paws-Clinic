'use client';

import { useId } from 'react';
import { Checkbox } from '@/components/ui/checkbox';
import { Field, Input } from '@/components/ui/field';
import { clinicDay } from '@/lib/format/datetime';
import { petIdentity } from '../../content/pet-identity';

interface BirthDateFieldsProps {
  /** `YYYY-MM-DD`, or empty when not known. */
  value: string;
  estimated: boolean;
  onChange: (value: string) => void;
  onEstimatedChange: (estimated: boolean) => void;
  error?: string;
  estimatedError?: string;
}

/** The birth date, never later than today in the clinic, and whether it is a guess. */
export function BirthDateFields({ value, estimated, onChange, onEstimatedChange, error, estimatedError }: BirthDateFieldsProps) {
  const id = useId();
  const text = petIdentity.birthDate;
  const today = clinicDay(new Date());
  const future = value !== '' && value > today;

  return (
    <div className="space-y-2">
      <Field label={text.label} htmlFor={id} hint={text.hint} error={future ? text.future : error}>
        <Input
          id={id}
          type="date"
          value={value}
          max={today}
          min="1900-01-01"
          onChange={(event) => onChange(event.target.value)}
          aria-invalid={future || Boolean(error)}
        />
      </Field>
      {value !== '' && (
        <Checkbox
          label={text.estimated}
          hint={estimatedError ?? text.estimatedHint}
          checked={estimated}
          onChange={(event) => onEstimatedChange(event.target.checked)}
        />
      )}
    </div>
  );
}
