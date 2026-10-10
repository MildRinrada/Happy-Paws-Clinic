'use client';

import { useId, useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Input } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { updatePetAction } from '../../actions/pet-edit-actions';
import { editPet } from '../../content/edit-pet';
import { petForms } from '../../content/pet-forms';
import { neuteredChoice } from '../../lib/describe-pet';
import type { PetSummary } from '../../types/pet.types';
import { IdentityFields, type IdentityValues } from '../identity/identity-fields';

interface EditPetFormProps {
  pet: PetSummary;
  onClose: () => void;
}

/**
 * Every field of the card at once, filled with what is on record. The backend saves and audits
 * only what differs; the version read with the page guards against a second editor.
 */
export function EditPetForm({ pet, onClose }: EditPetFormProps) {
  const text = editPet;
  const id = useId();
  const [name, setName] = useState(pet.name);
  const [species, setSpecies] = useState(pet.species);
  const [breed, setBreed] = useState(pet.breed ?? '');
  const [microchip, setMicrochip] = useState(pet.microchip_number ?? '');
  const [identity, setIdentity] = useState<IdentityValues>({
    sex: pet.sex ?? 'unknown',
    birthDate: pet.birth_date ?? '',
    birthDateEstimated: Boolean(pet.birth_date_estimated),
    neutered: neuteredChoice(pet.neutered),
  });
  const { errors, message, saved, pending, submit, touch } = useFormSubmit(petForms);

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    submit(() => updatePetAction({ petId: pet.id, version: pet.version, name, species, breed, microchip, ...identity }));
  }

  return (
    <form onSubmit={save} onChange={touch} className="space-y-4">
      <p className="text-sm text-ink-500">{pet.is_deceased ? text.closedNote : text.intro}</p>

      <div className="grid gap-4 sm:grid-cols-2">
        <Field label={text.fields.name} htmlFor={`${id}-name`} error={errors.name}>
          <Input id={`${id}-name`} value={name} maxLength={120} required onChange={(event) => setName(event.target.value)} />
        </Field>
        <Field label={text.fields.species} htmlFor={`${id}-species`} error={errors.species}>
          <Input id={`${id}-species`} value={species} maxLength={60} required onChange={(event) => setSpecies(event.target.value)} />
        </Field>
        <Field label={text.fields.breed} htmlFor={`${id}-breed`} hint={text.fields.clearHint} error={errors.breed}>
          <Input id={`${id}-breed`} value={breed} maxLength={120} onChange={(event) => setBreed(event.target.value)} />
        </Field>
        <Field label={text.fields.microchip} htmlFor={`${id}-chip`} hint={text.fields.clearHint} error={errors.microchip_number}>
          <Input
            id={`${id}-chip`}
            value={microchip}
            maxLength={40}
            inputMode="numeric"
            aria-invalid={Boolean(errors.microchip_number)}
            onChange={(event) => setMicrochip(event.target.value)}
          />
        </Field>
        <IdentityFields values={identity} onChange={setIdentity} errors={errors} neuteredLocked={pet.is_deceased} />
      </div>

      <p className="text-xs text-ink-500">{text.ownerFields}</p>

      <div className="flex flex-wrap items-center justify-end gap-2">
        <FormStatus message={message} saved={saved} savedText={text.saved} />
        <Button variant="ghost" size="sm" disabled={pending} onClick={onClose}>
          {petForms.cancel}
        </Button>
        <Button type="submit" size="sm" disabled={pending}>
          {pending ? petForms.saving : petForms.save}
        </Button>
      </div>
    </form>
  );
}
