'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction, type ActionResult } from '@/lib/actions/action-result';
import { listMyNotifications, markNotificationsRead } from '../api/notifications';
import { writeToClinic } from '../api/writeToClinic';
import { portalPaths } from '../content/portal-navigation';
import type { ChatMessage } from '../types/chat.types';

const writeInput = z.object({ text: z.string().max(1000) });

/** The inbox copy of a clinic answer: the template the backend gives a thread reply. */
const REPLY_TEMPLATE = 'thread.reply';

/** How many pages of unread notifications to look through for answers. 20 to a page. */
const MAX_PAGES = 5;

/** Sends the owner's message, then re-reads the chat so it shows in place. */
export async function writeToClinicAction(values: z.infer<typeof writeInput>): Promise<ActionResult<ChatMessage>> {
  const { text } = writeInput.parse(values);
  const result = await runAction(() => writeToClinic(text));

  revalidatePath(portalPaths.messages);
  return result;
}

/**
 * Marks the clinic's answers read in the web inbox, once the owner has them on screen in the chat.
 *
 * The thread has no "read" of its own for the owner (contract extension § 24): every answer also
 * arrives in the inbox as a `thread.reply` notification, and that copy's read mark is what the
 * bell counts. So the chat clears those, through the inbox's own endpoints, and nothing else.
 */
export async function markRepliesReadAction(): Promise<ActionResult<{ marked: number }>> {
  return runAction(async () => {
    const ids: string[] = [];
    let cursor: string | undefined;

    for (let page = 0; page < MAX_PAGES; page++) {
      const inbox = await listMyNotifications(true, cursor);
      ids.push(...inbox.data.filter((item) => item.template === REPLY_TEMPLATE).map((item) => item.id));
      cursor = inbox.page.next_cursor ?? undefined;
      if (!cursor) break;
    }

    if (ids.length === 0) return { marked: 0 };

    const result = await markNotificationsRead(ids);
    revalidatePath('/portal', 'layout');
    return { marked: result.marked };
  });
}
