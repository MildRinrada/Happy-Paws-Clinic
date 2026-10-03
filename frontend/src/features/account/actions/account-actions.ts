'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction, type ActionResult } from '@/lib/actions/action-result';
import { ApiError } from '@/lib/api/api-error';
import { changePassword } from '../api/changePassword';
import { revokeSession } from '../api/sessions';
import { updateMe } from '../api/updateMe';

/**
 * The account page's own changes: profile, message channels, password and devices. The backend
 * checks every rule (who may change what, which password is right, which address is taken); these
 * check the shape and pass the answer back.
 */

function refresh() {
  // The name is in the portal's header, so every page under it is stale.
  revalidatePath('/portal', 'layout');
}

const profileInput = z.object({
  version: z.number().int().nonnegative().optional(),
  displayName: z.string().trim().min(1).max(200),
  email: z.string().trim().max(256),
  phone: z.string().trim().max(30),
  currentPassword: z.string().max(256).optional(),
});

export async function updateProfileAction(values: z.infer<typeof profileInput>) {
  const input = profileInput.parse(values);
  const result = await runAction(() =>
    updateMe(input.version, {
      display_name: input.displayName,
      email: input.email || undefined,
      phone: input.phone,
      current_password: input.currentPassword || undefined,
    }),
  );

  refresh();
  return result.ok ? { ok: true as const, version: result.data.version } : result;
}

const channelsInput = z.object({
  version: z.number().int().nonnegative().optional(),
  email: z.boolean(),
  sms: z.boolean(),
  line: z.boolean(),
});

export async function updateChannelsAction(values: z.infer<typeof channelsInput>) {
  const { version, ...notifications } = channelsInput.parse(values);
  const result = await runAction(() => updateMe(version, { notifications }));

  refresh();
  return result.ok ? { ok: true as const, version: result.data.version } : result;
}

const passwordInput = z.object({
  currentPassword: z.string().min(1).max(256),
  newPassword: z.string().min(8).max(256),
});

/**
 * Not `runAction`: that reads any 401 as "the session is over" and sends the user to sign in.
 * Here a 401 means the current password was wrong, and the answer belongs on that field. The
 * middleware refreshed the session for this very request, so an expired session is not the cause.
 */
export async function changePasswordAction(values: z.infer<typeof passwordInput>): Promise<ActionResult> {
  const input = passwordInput.parse(values);

  try {
    await changePassword(input.currentPassword, input.newPassword);
    return { ok: true, data: undefined };
  } catch (error) {
    if (!(error instanceof ApiError)) throw error;

    return {
      ok: false,
      error: {
        code: error.code,
        message: error.message,
        field: error.status === 401 ? 'current_password' : error.field,
        status: error.status,
      },
    };
  }
}

export async function revokeSessionAction(values: { sessionId: string }) {
  const { sessionId } = z.object({ sessionId: z.string().min(1).max(64) }).parse(values);
  const result = await runAction(() => revokeSession(sessionId));

  revalidatePath('/portal/account');
  return result;
}
