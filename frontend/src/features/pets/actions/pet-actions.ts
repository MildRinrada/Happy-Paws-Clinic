'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { customerPaths } from '@/features/customers/lib/customer-paths';
import { runAction } from '@/lib/actions/action-result';
import { createPet } from '../api/createPet';
import { recordAllergy } from '../api/petRecords';
import { petPaths } from '../lib/pet-paths';

const newPet = z.object({
  customerId: z.string().min(1).max(64),
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
 * Registers a pet under the customer whose page the counter is on. The HN comes back in the
 * answer, issued by the server; a chip already on another patient is refused on its field. Sex,
 * birth date and neuter status are optional (docs/contract-extensions.md § 28).
 */
export async function registerPetAction(values: z.infer<typeof newPet>) {
  const input = newPet.parse(values);
  const result = await runAction(() =>
    createPet({
      name: input.name,
      species: input.species,
      primary_customer_id: input.customerId,
      breed: input.breed || undefined,
      microchip_number: input.microchip || undefined,
      sex: input.sex === 'unknown' ? undefined : input.sex,
      birth_date: input.birthDate || undefined,
      birth_date_estimated: input.birthDate ? input.birthDateEstimated : undefined,
      neutered: input.neutered === 'unknown' ? undefined : input.neutered === 'yes',
    }),
  );

  revalidatePath(customerPaths.detail(input.customerId));
  return result;
}

const newAllergy = z.object({
  petId: z.string().min(1).max(64),
  substance: z.string().trim().min(1).max(200),
  severity: z.enum(['mild', 'moderate', 'severe']),
  reaction: z.string().trim().max(500),
});

/** Records an allergy. It shows on every screen about this pet, and the prescription check reads it. */
export async function recordAllergyAction(values: z.infer<typeof newAllergy>) {
  const input = newAllergy.parse(values);
  const result = await runAction(() =>
    recordAllergy(input.petId, {
      substance: input.substance,
      severity: input.severity,
      reaction: input.reaction || undefined,
    }),
  );

  revalidatePath(petPaths.detail(input.petId));
  return result;
}
