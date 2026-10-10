'use server';

import { z } from 'zod';
import { runAction, type ActionResult } from '@/lib/actions/action-result';
import { writeDisplayToken } from '@/lib/display/display-session';
import { claimQueueDisplay } from '../api/displayBoard';

/**
 * The TV's half of setting up a screen (docs/contract-extensions.md § 30).
 *
 * The setup code goes to the backend, the token comes back here and goes straight into the
 * screen's httpOnly cookie. The browser is told only which screen it now is: the token is never
 * in the page, in a URL or in the browser's history.
 */

const code = z.string().trim().min(1).max(20);

export async function claimDisplayAction(payload: { code: string }): Promise<ActionResult<{ name: string }>> {
  const typed = code.parse(payload.code);
  const result = await runAction(() => claimQueueDisplay(typed));

  if (!result.ok) {
    return result;
  }

  await writeDisplayToken(result.data.token, result.data.expires_at);

  return { ok: true, data: { name: result.data.name } };
}
