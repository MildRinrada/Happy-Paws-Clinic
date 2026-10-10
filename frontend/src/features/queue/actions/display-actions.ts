'use server';

import { revalidatePath } from 'next/cache';
import { headers } from 'next/headers';
import { z } from 'zod';
import { siteEnv } from '@/config/env';
import { qrCodeDataUrl } from '@/features/staff/lib/qr-code';
import { runAction, type ActionResult } from '@/lib/actions/action-result';
import { DISPLAY_SETUP_PATH } from '@/lib/display/display-paths';
import { createQueueDisplay, revokeQueueDisplay } from '../api/queueDisplays';
import type { DisplaySetup } from '../types/queue.types';

/**
 * Making and stopping waiting-room screens from the console (docs/contract-extensions.md § 30).
 * The setup code a create returns goes back to the form that asked for it, with a setup link and
 * its QR code, and is shown there once; nothing here keeps it.
 */

const DISPLAYS_PATH = '/queue/displays';

const displayInput = z.object({
  name: z.string().trim().min(1).max(80),
  branchId: z.string().min(1).max(40),
  /** 0 means no end: the screen works until it is revoked. */
  expiresInDays: z.number().int().min(0).max(730),
});

/**
 * The address the TV should open: this site as the person making the screen reached it (the
 * `Origin` a server action always carries), so a clinic on its own network gets a link its TV can
 * reach. The code sits after `#`, which a browser never sends to a server.
 */
async function setupUrl(code: string): Promise<string> {
  const origin = (await headers()).get('origin') ?? siteEnv.url();

  return `${origin.replace(/\/+$/, '')}${DISPLAY_SETUP_PATH}#code=${encodeURIComponent(code)}`;
}

export async function createDisplayAction(values: z.infer<typeof displayInput>): Promise<ActionResult<DisplaySetup>> {
  const { name, branchId, expiresInDays } = displayInput.parse(values);
  const expiresAt =
    expiresInDays > 0 ? new Date(Date.now() + expiresInDays * 24 * 60 * 60 * 1000).toISOString() : undefined;

  const result = await runAction(() => createQueueDisplay({ name, branch_id: branchId, expires_at: expiresAt }));

  revalidatePath(DISPLAYS_PATH);

  if (!result.ok) {
    return result;
  }

  const url = await setupUrl(result.data.setup_code);

  return { ok: true, data: { display: result.data, setupUrl: url, qrDataUrl: await qrCodeDataUrl(url) } };
}

export async function revokeDisplayAction(payload: { id: string }): Promise<ActionResult> {
  const id = z.string().min(1).max(40).parse(payload.id);
  const result = await runAction(() => revokeQueueDisplay(id));

  revalidatePath(DISPLAYS_PATH);
  return result.ok ? { ok: true, data: undefined } : result;
}
