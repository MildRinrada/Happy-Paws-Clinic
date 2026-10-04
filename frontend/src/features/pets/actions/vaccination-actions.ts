'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import { recordVaccination } from '../api/petRecords';
import { petPaths } from '../lib/pet-paths';

const day = z.string().regex(/^\d{4}-\d{2}-\d{2}$/);

const newDose = z.object({
  petId: z.string().min(1).max(64),
  vaccineName: z.string().trim().min(1).max(200),
  lotNumber: z.string().trim().min(1).max(100),
  vialExpiresOn: day,
  /** An instant with its offset, or absent for "now". */
  administeredAt: z.string().max(40).optional(),
  nextDueOn: day.optional(),
  validForDays: z.number().int().min(0).max(1095).optional(),
  manufacturer: z.string().trim().max(200),
  note: z.string().trim().max(1000),
});

/** Records a dose. The next one is due on the date given, after the days given, or in a year. */
export async function recordVaccinationAction(values: z.infer<typeof newDose>) {
  const input = newDose.parse(values);
  const result = await runAction(() =>
    recordVaccination(input.petId, {
      vaccine_name: input.vaccineName,
      lot_number: input.lotNumber,
      vial_expires_on: input.vialExpiresOn,
      administered_at: input.administeredAt,
      next_due_on: input.nextDueOn,
      valid_for_days: input.validForDays,
      manufacturer: input.manufacturer || undefined,
      note: input.note || undefined,
    }),
  );

  revalidatePath(petPaths.detail(input.petId));
  return result;
}
