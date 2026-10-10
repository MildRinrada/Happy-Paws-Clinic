'use client';

import { useId } from 'react';
import { SignInField } from '@/features/authentication/components/sign-in/sign-in-field';
import { guestBookingPage } from '../../content/guest-booking-page';
import type { NewPet } from '../../types/booking.types';
import { ChoiceCard } from '../choice-card';

const OTHER = 'other';

interface NewPetFieldsProps {
  value: NewPet;
  /** Which species button is chosen: one of the listed names, or `other` for a typed one. */
  speciesChoice: string;
  onChange: (value: NewPet, speciesChoice: string) => void;
}

/** A first-time pet: its name, and its species from the common few or typed in. */
export function NewPetFields({ value, speciesChoice, onChange }: NewPetFieldsProps) {
  const text = guestBookingPage.pet;
  const nameId = useId();
  const otherId = useId();
  const groupName = useId();

  return (
    <div className="space-y-4 rounded-xl border border-ink-100 p-4">
      <SignInField
        id={nameId}
        label={text.nameLabel}
        placeholder={text.namePlaceholder}
        value={value.name}
        onChange={(event) => onChange({ ...value, name: event.target.value }, speciesChoice)}
        maxLength={120}
        required
      />

      <fieldset className="space-y-2">
        <legend className="mb-2 text-[15px] text-ink-700">{text.species}</legend>
        <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
          {text.speciesOptions.map((species) => (
            <ChoiceCard
              key={species}
              name={groupName}
              value={species}
              checked={speciesChoice === species}
              onChange={() => onChange({ ...value, species }, species)}
              title={species}
            />
          ))}
          <ChoiceCard
            name={groupName}
            value={OTHER}
            checked={speciesChoice === OTHER}
            onChange={() => onChange({ ...value, species: '' }, OTHER)}
            title={text.otherSpecies}
          />
        </div>
      </fieldset>

      {speciesChoice === OTHER && (
        <SignInField
          id={otherId}
          label={text.otherLabel}
          placeholder={text.otherPlaceholder}
          value={value.species}
          onChange={(event) => onChange({ ...value, species: event.target.value }, OTHER)}
          maxLength={60}
          required
        />
      )}
    </div>
  );
}
