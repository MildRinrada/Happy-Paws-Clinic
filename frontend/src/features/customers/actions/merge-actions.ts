'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction, type ActionResult } from '@/lib/actions/action-result';
import { mergeCustomers } from '../api/mergeCustomers';
import { searchCustomers } from '../api/searchCustomers';
import { customerPaths } from '../lib/customer-paths';
import type { Customer, MergeResult } from '../types/customer.types';

/**
 * Folding a duplicate customer record into the one open on screen. The search reuses the
 * register's own (`GET /staff/customers?q=`, which never lists merged records); the merge is
 * `customer:merge`, checked again by the backend.
 */

const id = z.string().min(1).max(64);

/** How many matches the picker shows. A duplicate is found by a precise query, not by paging. */
const CANDIDATE_LIMIT = 10;

const candidateQuery = z.object({ q: z.string().trim().min(1).max(100), customerId: id });

/** Customers matching the query, without the record being kept. */
export async function findMergeCandidatesAction(
  values: z.infer<typeof candidateQuery>,
): Promise<ActionResult<Customer[]>> {
  const { q, customerId } = candidateQuery.parse(values);
  const result = await runAction(() => searchCustomers({ q, limit: CANDIDATE_LIMIT }));

  return result.ok ? { ok: true, data: result.data.data.filter((customer) => customer.id !== customerId) } : result;
}

const mergeInput = z.object({
  survivingId: id,
  version: z.number().int().nonnegative(),
  duplicateId: id,
});

/** Merges, then refreshes both records' pages and the register, which no longer lists the duplicate. */
export async function mergeCustomerAction(values: z.infer<typeof mergeInput>): Promise<ActionResult<MergeResult>> {
  const { survivingId, version, duplicateId } = mergeInput.parse(values);
  const result = await runAction(() => mergeCustomers(survivingId, version, duplicateId));

  if (result.ok) {
    revalidatePath(customerPaths.detail(survivingId));
    revalidatePath(customerPaths.detail(duplicateId));
    revalidatePath(customerPaths.list);
  }

  return result;
}
