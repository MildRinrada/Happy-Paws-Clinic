'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import { markPetDeceased } from '../api/markPetDeceased';
import { undoPetDeceased } from '../api/undoPetDeceased';
import { petPaths } from '../lib/pet-paths';

const closing = z.object({
  petId: z.string().min(1).max(64),
  /** An instant with its offset, or absent for "now". */
  occurredAt: z.string().max(40).optional(),
  note: z.string().trim().max(1000),
});

/** Closes the record. The answer says how many future visits were cancelled. */
export async function markDeceasedAction(values: z.infer<typeof closing>) {
  const input = closing.parse(values);
  const result = await runAction(() =>
    markPetDeceased(input.petId, { occurred_at: input.occurredAt, note: input.note || undefined }),
  );

  revalidatePath(petPaths.detail(input.petId));
  return result.ok ? { ok: true as const, data: result.data.cancelled_appointment_ids.length } : result;
}

const undoing = z.object({
  petId: z.string().min(1).max(64),
  reason: z.string().trim().min(1).max(500),
});

/**
 * Takes back a death recorded by mistake (docs/contract-extensions.md § 29). The answer lists the
 * visits the death cancelled; they stay cancelled for the counter to rebook with the owner.
 */
export async function undoDeceasedAction(values: z.infer<typeof undoing>) {
  const input = undoing.parse(values);
  const result = await runAction(() => undoPetDeceased(input.petId, input.reason));

  revalidatePath(petPaths.detail(input.petId));
  return result;
}
