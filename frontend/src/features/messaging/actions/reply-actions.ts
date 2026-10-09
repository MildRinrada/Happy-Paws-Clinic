'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction, type ActionResult } from '@/lib/actions/action-result';
import { replyToThread } from '../api/replyToThread';
import { MESSAGES_PATH } from '../lib/messages-query';
import type { ThreadMessage } from '../types/thread.types';

const replyInput = z.object({
  thread_id: z.string().min(1).max(40),
  text: z.string().max(1000),
});

/** Sends a reply, then refreshes the inbox so the thread shows it with its delivery result. */
export async function replyToThreadAction(values: z.infer<typeof replyInput>): Promise<ActionResult<ThreadMessage>> {
  const input = replyInput.parse(values);
  const result = await runAction(() => replyToThread(input.thread_id, input.text));

  revalidatePath(MESSAGES_PATH);
  return result;
}
