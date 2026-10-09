import { staffApi } from '@/lib/api/server-client';
import type { ChatThread } from '../types/chat.types';

/** How many messages the chat page shows at once. */
export const CHAT_PAGE_SIZE = 50;

/**
 * `GET /portal/thread` (contract extension § 24) — the signed-in owner's own conversation, newest
 * first. A `cursor` from the previous page reads older messages. Reading creates nothing.
 */
export function getMyThread(cursor?: string): Promise<ChatThread> {
  return staffApi().get<ChatThread>('/portal/thread', { query: { limit: CHAT_PAGE_SIZE, cursor } });
}
