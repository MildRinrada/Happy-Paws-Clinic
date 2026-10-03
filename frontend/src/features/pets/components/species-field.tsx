'use client';

import { useState } from 'react';
import { Field, Input, Select } from '@/components/ui/field';
import { registerPet } from '../content/register-pet';

const OTHER = '__other';

interface SpeciesFieldProps {
  value: string;
  onChange: (species: string) => void;
  error?: string;
}

/**
 * The common species as a list, and a box for anything else. The API takes any name up to 60
 * characters, so "อื่น ๆ" is a real choice rather than a dead end.
 */
export function SpeciesField({ value, onChange, error }: SpeciesFieldProps) {
  const text = registerPet;
  const [other, setOther] = useState(false);

  return (
    <div className="space-y-3">
      <Field label={text.fields.species} htmlFor="pet-species" error={other ? undefined : error}>
        <Select
          id="pet-species"
          value={other ? OTHER : value}
          onChange={(event) => {
            const choice = event.target.value;
            setOther(choice === OTHER);
            onChange(choice === OTHER ? '' : choice);
          }}
        >
          {text.species.map((species) => (
            <option key={species} value={species}>
              {species}
            </option>
          ))}
          <option value={OTHER}>{text.otherOption}</option>
        </Select>
      </Field>

      {other && (
        <Field label={text.fields.otherSpecies} htmlFor="pet-species-other" error={error}>
          <Input
            id="pet-species-other"
            value={value}
            onChange={(event) => onChange(event.target.value)}
            maxLength={60}
            required
            autoFocus
            aria-invalid={Boolean(error)}
          />
        </Field>
      )}
    </div>
  );
}
