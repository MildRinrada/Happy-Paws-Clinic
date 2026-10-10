import { staffApi } from '@/lib/api/server-client';
import type { CreatedQueueDisplay, QueueDisplaySummary } from '../types/queue.types';

/**
 * Waiting-room screens, all `queue:write`. A contract extension (docs/contract-extensions.md § 30).
 */

/** `GET /staff/queue/displays?branch=` — the screens at a branch, newest first. Never a token or code. */
export async function listQueueDisplays(branchId: string): Promise<QueueDisplaySummary[]> {
  return (await staffApi().get<{ data: QueueDisplaySummary[] }>('/staff/queue/displays', { query: { branch: branchId } })).data;
}

export interface QueueDisplayInput {
  name: string;
  branch_id: string;
  expires_at?: string;
}

/** `POST /staff/queue/displays` — the answer carries the setup code, once. */
export function createQueueDisplay(input: QueueDisplayInput): Promise<CreatedQueueDisplay> {
  return staffApi().post<CreatedQueueDisplay>('/staff/queue/displays', { body: input });
}

/** `DELETE /staff/queue/displays/{id}` — the screen's next request is refused. */
export function revokeQueueDisplay(id: string): Promise<void> {
  return staffApi().delete<void>(`/staff/queue/displays/${encodeURIComponent(id)}`);
}
