'use client';

import { Field, Select } from '@/components/ui/field';
import type { PetChoice } from '../../actions/create-invoice-actions';
import { createInvoicePage } from '../../content/create-invoice-page';

interface Props {
  pets: PetChoice[];
  value: string;
  error?: string;
  onChange: (value: string) => void;
}

/** Which of the customer's pets the bill is for. Optional: a bag of food is for nobody in particular. */
export function PetSelect({ pets, value, error, onChange }: Props) {
  const text = createInvoicePage.pet;

  return (
    <Field label={text.label} htmlFor="invoice-pet" error={error}>
      <Select id="invoice-pet" value={value} onChange={(event) => onChange(event.target.value)}>
        <option value="">{text.none}</option>
        {pets.map((pet) => (
          <option key={pet.id} value={pet.id}>
            {pet.name} ({pet.species})
          </option>
        ))}
      </Select>
    </Field>
  );
}
