import { staffApi } from '@/lib/api/server-client';
import type { AccountSession } from '../types/account.types';

/** `GET /me/sessions` — every device signed in to this account. A bare array, not `{ data }`. */
export function listSessions(): Promise<AccountSession[]> {
  return staffApi().get<AccountSession[]>('/me/sessions');
}

/** `DELETE /me/sessions/{id}` — signs that device out. */
export function revokeSession(sessionId: string): Promise<void> {
  return staffApi().delete<void>(`/me/sessions/${encodeURIComponent(sessionId)}`);
}
