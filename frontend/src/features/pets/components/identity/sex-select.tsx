'use client';

import { useId } from 'react';
import { Field, Select } from '@/components/ui/field';
import { petIdentity } from '../../content/pet-identity';
import type { PetSex } from '../../types/pet.types';

interface SexSelectProps {
  value: PetSex;
  onChange: (sex: PetSex) => void;
  error?: string;
}

/** Male, female, or not known yet. "Not known" is a real answer, not a missing one. */
export function SexSelect({ value, onChange, error }: SexSelectProps) {
  const id = useId();
  const text = petIdentity.sex;

  return (
    <Field label={text.label} htmlFor={id} error={error}>
      <Select id={id} value={value} onChange={(event) => onChange(event.target.value as PetSex)}>
        {(['unknown', 'male', 'female'] as const).map((sex) => (
          <option key={sex} value={sex}>
            {text.options[sex]}
          </option>
        ))}
      </Select>
    </Field>
  );
}
