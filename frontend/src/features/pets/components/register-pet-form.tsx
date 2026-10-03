'use client';

import { useState, useTransition, type FormEvent } from 'react';
import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Field, Input } from '@/components/ui/field';
import { fieldErrors } from '@/lib/actions/field-errors';
import { registerPetAction } from '../actions/pet-actions';
import { registerPet } from '../content/register-pet';
import type { PetSummary } from '../types/pet.types';
import { SpeciesField } from './species-field';

interface RegisterPetFormProps {
  customerId: string;
  onSaved: (pet: PetSummary) => void;
  onCancel: () => void;
}

/** Name, species, and optionally breed and chip. The list above re-reads itself once it saves. */
export function RegisterPetForm({ customerId, onSaved, onCancel }: RegisterPetFormProps) {
  const text = registerPet;
  const [name, setName] = useState('');
  const [species, setSpecies] = useState(text.species[0] ?? '');
  const [breed, setBreed] = useState('');
  const [microchip, setMicrochip] = useState('');
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [message, setMessage] = useState<string>();
  const [pending, startTransition] = useTransition();

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setErrors({});
    setMessage(undefined);

    startTransition(async () => {
      try {
        const result = await registerPetAction({ customerId, name, species, breed, microchip });

        if (result.ok) {
          onSaved(result.data);
          return;
        }

        const byField = fieldErrors(result.error);
        setErrors(byField);
        if (Object.keys(byField).length === 0) setMessage(result.error.message);
      } catch {
        setMessage(text.unreachable);
      }
    });
  }

  return (
    <form onSubmit={submit} className="space-y-4">
      <p className="text-sm text-ink-500">{text.hint}</p>
      {message && <Alert>{message}</Alert>}

      <div className="grid gap-4 sm:grid-cols-2">
        <Field label={text.fields.name} htmlFor="pet-name" error={errors.name}>
          <Input
            id="pet-name"
            value={name}
            onChange={(event) => setName(event.target.value)}
            maxLength={120}
            required
            autoFocus
            aria-invalid={Boolean(errors.name)}
          />
        </Field>

        <SpeciesField value={species} onChange={setSpecies} error={errors.species} />

        <Field label={text.fields.breed} htmlFor="pet-breed" error={errors.breed}>
          <Input
            id="pet-breed"
            value={breed}
            onChange={(event) => setBreed(event.target.value)}
            placeholder={text.fields.breedPlaceholder}
            maxLength={120}
          />
        </Field>

        <Field label={text.fields.microchip} htmlFor="pet-chip" error={errors.microchip_number}>
          <Input
            id="pet-chip"
            value={microchip}
            onChange={(event) => setMicrochip(event.target.value)}
            inputMode="numeric"
            maxLength={40}
            aria-invalid={Boolean(errors.microchip_number)}
          />
        </Field>
      </div>

      <div className="flex justify-end gap-2">
        <Button variant="ghost" onClick={onCancel} disabled={pending}>
          {text.cancel}
        </Button>
        <Button type="submit" disabled={pending}>
          {pending ? text.submitting : text.submit}
        </Button>
      </div>
    </form>
  );
}
