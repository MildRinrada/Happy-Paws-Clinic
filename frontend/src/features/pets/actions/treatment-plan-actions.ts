'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import { createTreatmentPlan } from '../api/treatmentPlans';
import { petPaths } from '../lib/pet-paths';

const id = z.string().min(1).max(64);

const newPlan = z.object({
  petId: id,
  serviceId: id,
  firstStartsAt: z.string().min(1).max(40),
  title: z.string().trim().min(1).max(200),
  sessions: z.number().int().min(2).max(52),
  intervalDays: z.number().int().min(1).max(365),
  staffId: id.optional(),
  branchId: id.optional(),
  notes: z.string().trim().max(2000),
  book: z.boolean(),
  idempotencyKey: z.uuid(),
});

/** Plans a course of visits and, unless told not to, books every one of them. */
export async function createTreatmentPlanAction(values: z.infer<typeof newPlan>) {
  const input = newPlan.parse(values);
  const result = await runAction(() =>
    createTreatmentPlan(
      input.petId,
      {
        service_id: input.serviceId,
        first_starts_at: input.firstStartsAt,
        title: input.title,
        sessions: input.sessions,
        interval_days: input.intervalDays,
        staff_id: input.staffId,
        branch_id: input.branchId,
        notes: input.notes || undefined,
        book: input.book,
      },
      input.idempotencyKey,
    ),
  );

  revalidatePath(petPaths.detail(input.petId));
  return result;
}
