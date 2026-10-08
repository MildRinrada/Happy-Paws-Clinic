'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { listLineFollowers } from '@/features/integrations/api/listLineFollowers';
import type { LineFollowerPage } from '@/features/integrations/types/line.types';
import { runAction, type ActionResult } from '@/lib/actions/action-result';
import { linkLineAddress, unlinkLineAddress } from '../api/lineAddress';
import { customerPaths } from '../lib/customer-paths';
import type { CustomerLine } from '../types/customer.types';

/**
 * The counter's LINE picker (docs/contract-extensions.md § 18). The follower list is read only
 * when the picker is opened, so the names of people who are not customers are not loaded with
 * every customer page. The backend checks the scope, the follower and the record again.
 */

const listInput = z.object({ q: z.string().trim().max(100).optional(), cursor: z.string().max(400).optional() });

export async function listLineFollowersAction(values: z.input<typeof listInput>): Promise<ActionResult<LineFollowerPage>> {
  const input = listInput.parse(values);

  return runAction(() => listLineFollowers(input));
}

const linkInput = z.object({
  customerId: z.string().min(1).max(64),
  version: z.number().int().nonnegative(),
  lineUserId: z.string().min(1).max(64),
});

export async function linkLineAction(values: z.infer<typeof linkInput>): Promise<ActionResult<CustomerLine>> {
  const { customerId, version, lineUserId } = linkInput.parse(values);
  const result = await runAction(() => linkLineAddress(customerId, version, lineUserId));

  revalidatePath(customerPaths.detail(customerId));
  return result;
}

const unlinkInput = z.object({ customerId: z.string().min(1).max(64), version: z.number().int().nonnegative() });

export async function unlinkLineAction(values: z.infer<typeof unlinkInput>): Promise<ActionResult> {
  const { customerId, version } = unlinkInput.parse(values);
  const result = await runAction(() => unlinkLineAddress(customerId, version));

  revalidatePath(customerPaths.detail(customerId));
  return result.ok ? { ok: true, data: undefined } : result;
}
