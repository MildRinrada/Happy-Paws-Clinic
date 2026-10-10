'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import { updatePet } from '../api/updatePet';
import { petPaths } from '../lib/pet-paths';
import type { PetChanges } from '../types/pet.types';

const card = z.object({
  petId: z.string().min(1).max(64),
  version: z.number().int().nonnegative(),
  name: z.string().trim().min(1).max(120),
  species: z.string().trim().min(1).max(60),
  breed: z.string().trim().max(120),
  microchip: z.string().trim().max(40),
  sex: z.enum(['male', 'female', 'unknown']),
  /** `YYYY-MM-DD`, or empty for "not known". */
  birthDate: z.union([z.string().regex(/^\d{4}-\d{2}-\d{2}$/), z.literal('')]),
  birthDateEstimated: z.boolean(),
  neutered: z.enum(['yes', 'no', 'unknown']),
});

/**
 * Saves the whole identity card (docs/contract-extensions.md § 28). Every field is sent; the
 * backend changes and audits only what differs. Empty breed or chip clears it, and an empty birth
 * date or "unknown" neuter status goes through `clear`.
 */
export async function updatePetAction(values: z.infer<typeof card>) {
  const input = card.parse(values);
  const clear: PetChanges['clear'] = [];
  if (!input.birthDate) clear.push('birth_date');
  if (input.neutered === 'unknown') clear.push('neutered');

  const changes: PetChanges = {
    name: input.name,
    species: input.species,
    breed: input.breed,
    microchip_number: input.microchip,
    sex: input.sex,
    ...(input.birthDate ? { birth_date: input.birthDate, birth_date_estimated: input.birthDateEstimated } : {}),
    ...(input.neutered === 'unknown' ? {} : { neutered: input.neutered === 'yes' }),
    ...(clear.length > 0 ? { clear } : {}),
  };

  const result = await runAction(() => updatePet(input.petId, changes, input.version));

  revalidatePath(petPaths.detail(input.petId));
  return result;
}
