'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { searchCustomers } from '@/features/customers/api/searchCustomers';
import { runAction } from '@/lib/actions/action-result';
import { addCarer, removeCarer } from '../api/petCarers';
import { petPaths } from '../lib/pet-paths';

const id = z.string().min(1).max(64);

/** The customer lookup behind "add a carer". Needs `customer:read`. */
export async function searchCarerCandidatesAction(query: string) {
  const q = z.string().trim().min(2).max(100).parse(query);
  return runAction(async () => (await searchCustomers({ q, limit: 8 })).data);
}

const newCarer = z.object({ petId: id, customerId: id, mayViewHistory: z.boolean() });

/** Links a customer to the pet. Linking someone already there updates their history permission. */
export async function addCarerAction(values: z.infer<typeof newCarer>) {
  const input = newCarer.parse(values);
  const result = await runAction(() =>
    addCarer(input.petId, { customer_id: input.customerId, may_view_history: input.mayViewHistory }),
  );

  revalidatePath(petPaths.detail(input.petId));
  return result;
}

const removal = z.object({ petId: id, customerId: id });

/** Closes a carer's link. The primary owner cannot be removed this way; the backend says so. */
export async function removeCarerAction(values: z.infer<typeof removal>) {
  const input = removal.parse(values);
  const result = await runAction(() => removeCarer(input.petId, input.customerId));

  revalidatePath(petPaths.detail(input.petId));
  return result;
}
