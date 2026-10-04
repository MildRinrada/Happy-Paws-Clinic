'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import { deactivateStaffMember, inviteStaffMember, updateStaffMember } from '../api/manage-staff';

/**
 * The staff page's changes to people. The schemas only bound the input's shape; whether an email is
 * taken, a role exists or the caller may hand it out is the backend's answer, returned per field.
 */

function refresh() {
  revalidatePath('/settings/staff');
}

const id = z.string().min(1).max(64);
const version = z.number().int().nonnegative();
const names = z.array(z.string().min(1).max(64)).max(50);
const branchIds = z.array(z.string().min(1).max(40)).max(50);

const invitation = z.object({
  email: z.string().trim().max(256),
  display_name: z.string().trim().max(200),
  staff_type: z.string().trim().max(64),
  roles: names,
  branches: branchIds.optional(),
  license_no: z.string().trim().max(64).optional(),
});

/** Invites, or re-invites the same address, and hands back where the link went and until when. */
export async function inviteStaffAction(values: z.infer<typeof invitation>) {
  const parsed = invitation.parse(values);
  const result = await runAction(() => inviteStaffMember(parsed));

  refresh();
  return result.ok
    ? { ok: true as const, data: { email: result.data.email ?? parsed.email, expiresAt: result.data.invitation_expires_at } }
    : result;
}

const change = z.object({
  id,
  version,
  values: z.object({
    staff_type: z.string().trim().max(64).optional(),
    roles: names.optional(),
    branches: branchIds.optional(),
    license_no: z.string().trim().max(64).optional(),
    department: z.string().max(32).optional(),
  }),
});

export async function updateStaffAction(payload: z.infer<typeof change>) {
  const parsed = change.parse(payload);
  const result = await runAction(() => updateStaffMember(parsed.id, parsed.version, parsed.values));

  refresh();
  return result.ok ? { ok: true as const, data: undefined } : result;
}

/** No `If-Match`: the endpoint takes none, and pressing it twice is harmless. */
export async function deactivateStaffAction(payload: { id: string }) {
  const parsed = z.object({ id }).parse(payload);
  const result = await runAction(() => deactivateStaffMember(parsed.id));

  refresh();
  return result.ok ? { ok: true as const, data: undefined } : result;
}
