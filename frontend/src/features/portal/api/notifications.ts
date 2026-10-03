import { staffApi } from '@/lib/api/server-client';
import type { InboxItem } from '../types/portal.types';

export interface InboxPage {
  data: InboxItem[];
  page: { next_cursor: string | null };
  /** Every unread message, not only those on this page. */
  unread_count: number;
}

/** `GET /portal/notifications` — newest first. Reading the list marks nothing as read. */
export function listMyNotifications(unread: boolean, cursor?: string): Promise<InboxPage> {
  return staffApi().get<InboxPage>('/portal/notifications', { query: { unread, cursor, limit: 20 } });
}

/**
 * `POST /portal/notifications/read` (a contract extension, § 13). No ids marks everything; ids
 * that are not the caller's are ignored by the backend.
 */
export function markNotificationsRead(ids?: string[]): Promise<{ marked: number; unread_count: number }> {
  return staffApi().post('/portal/notifications/read', { body: ids ? { ids } : {} });
}
