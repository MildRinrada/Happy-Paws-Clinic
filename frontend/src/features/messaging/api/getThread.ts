import { listThreads } from './listThreads';
import type { MessageThread } from '../types/thread.types';

/** The newest messages a conversation opens with. Older ones are not paged in yet. */
export const THREAD_MESSAGE_LIMIT = 100;

/**
 * One thread with its newest messages. The contract has a single read, so this is
 * `GET /staff/threads?thread_id=` narrowed to one; null when it is gone or at another branch.
 */
export async function getThread(threadId: string): Promise<MessageThread | null> {
  const page = await listThreads({ thread_id: threadId, messages: THREAD_MESSAGE_LIMIT, limit: 1 });
  return page.data[0] ?? null;
}
