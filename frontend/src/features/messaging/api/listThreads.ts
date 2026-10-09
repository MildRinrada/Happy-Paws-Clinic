import { staffApi } from '@/lib/api/server-client';
import type { CursorPage } from '@/types/pagination';
import type { MessageThread, ThreadListQuery } from '../types/thread.types';

/**
 * `GET /staff/threads` — "กล่องข้อความรวมทุกช่องทางต่อลูกค้าหนึ่งคน". Needs `message:read`.
 * Threads awaiting a reply first, then the most recently active. `messages` is how many of each
 * thread's newest messages to include (0 to 100).
 */
export function listThreads(query: ThreadListQuery): Promise<CursorPage<MessageThread>> {
  return staffApi().get<CursorPage<MessageThread>>('/staff/threads', { query: { ...query } });
}
