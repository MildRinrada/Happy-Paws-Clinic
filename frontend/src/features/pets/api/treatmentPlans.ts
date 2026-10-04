import { staffApi } from '@/lib/api/server-client';
import type { NewTreatmentPlan, TreatmentPlan } from '../types/pet.types';

/**
 * `POST /staff/pets/{id}/treatment-plans` — needs `consult:write`. With `book` it books every
 * session as an appointment, all or nothing, so it carries an `Idempotency-Key` like every POST
 * that creates appointments. There is no read for plans: the answer is the only copy the page gets.
 */
export function createTreatmentPlan(petId: string, plan: NewTreatmentPlan, idempotencyKey: string): Promise<TreatmentPlan> {
  return staffApi().post<TreatmentPlan>(`/staff/pets/${encodeURIComponent(petId)}/treatment-plans`, {
    body: plan,
    idempotencyKey,
  });
}
