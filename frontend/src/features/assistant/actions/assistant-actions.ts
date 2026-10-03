'use server';

import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import { chat } from '../api/assistant';

const conversation = z
  .array(z.object({ role: z.enum(['user', 'assistant']), text: z.string().trim().min(1).max(1000) }))
  .min(1)
  .max(20);

/**
 * Sends the conversation to the clinic's assistant. The browser never talks to the API directly:
 * this runs on the server, like every other call, so the API's address and limits stay behind it.
 */
export async function sendChatAction(messages: z.infer<typeof conversation>) {
  const parsed = conversation.parse(messages);
  const result = await runAction(() => chat(parsed));

  if (!result.ok) return result;

  // Only paths on this site become buttons, whatever came back.
  const actions = result.data.actions.filter((action) => action.href.startsWith('/') && !action.href.startsWith('//'));

  return { ok: true as const, data: { reply: result.data.reply, actions } };
}
