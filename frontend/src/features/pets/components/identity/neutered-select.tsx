'use client';

import { useId } from 'react';
import { Field, Select } from '@/components/ui/field';
import { petIdentity } from '../../content/pet-identity';

export type NeuteredChoice = 'yes' | 'no' | 'unknown';

interface NeuteredSelectProps {
  value: NeuteredChoice;
  onChange: (value: NeuteredChoice) => void;
  error?: string;
  /** A closed record keeps the status it had. */
  disabled?: boolean;
}

/** Neutered or spayed, not yet, or not known. */
export function NeuteredSelect({ value, onChange, error, disabled }: NeuteredSelectProps) {
  const id = useId();
  const text = petIdentity.neutered;

  return (
    <Field label={text.label} htmlFor={id} error={error} hint={disabled ? text.closedHint : undefined}>
      <Select id={id} value={value} disabled={disabled} onChange={(event) => onChange(event.target.value as NeuteredChoice)}>
        {(['unknown', 'yes', 'no'] as const).map((choice) => (
          <option key={choice} value={choice}>
            {text.options[choice]}
          </option>
        ))}
      </Select>
    </Field>
  );
}
