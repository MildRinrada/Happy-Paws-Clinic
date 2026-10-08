'use server';

import { z } from 'zod';
import { runAction, type ActionResult } from '@/lib/actions/action-result';
import { listDrugOptions } from '../api/listDrugOptions';
import type { DrugOptions } from '../types/drug-option.types';

const search = z.object({
  q: z.string().trim().max(200),
  branchId: z.string().min(1).max(64),
});

/**
 * The prescriber's drug search, read as the vet types. A read through an action because the
 * session's token stays on the server; the backend decides which branch the vet may see.
 */
export async function searchDrugOptionsAction(input: z.infer<typeof search>): Promise<ActionResult<DrugOptions>> {
  const parsed = search.parse(input);

  return runAction(() => listDrugOptions({ q: parsed.q || undefined, branch: parsed.branchId, limit: 8 }));
}
