'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import { createRole, replaceRolePermissions } from '../api/manage-roles';

/**
 * Creating a role and rewriting one's scopes. A role change reaches everyone holding it on their
 * next access token, so nothing else needs refreshing here but this page.
 */

function refresh() {
  revalidatePath('/settings/staff');
}

const scopes = z.array(z.string().min(1).max(64)).max(200);

const newRole = z.object({ name: z.string().max(64), scopes });

export async function createRoleAction(values: z.infer<typeof newRole>) {
  const parsed = newRole.parse(values);
  const result = await runAction(() => createRole(parsed));

  refresh();
  return result.ok ? { ok: true as const, data: undefined } : result;
}

const replacement = z.object({ id: z.string().min(1).max(64), version: z.number().int().nonnegative(), scopes });

/** An empty set is allowed: emptying a role is how a clinic retires one, there being no delete. */
export async function replaceRolePermissionsAction(payload: z.infer<typeof replacement>) {
  const parsed = replacement.parse(payload);
  const result = await runAction(() => replaceRolePermissions(parsed.id, parsed.version, parsed.scopes));

  refresh();
  return result.ok ? { ok: true as const, data: undefined } : result;
}
