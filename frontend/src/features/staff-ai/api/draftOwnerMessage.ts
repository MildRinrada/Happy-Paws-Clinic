import { staffApi } from '@/lib/api/server-client';
import type { OwnerMessageDraft } from '../types/staff-ai.types';

export type OwnerMessageRequest =
  | { kind: 'aftercare'; consultation_id: string }
  | { kind: 'thread_reply'; thread_id: string };

/**
 * `POST /staff/ai/drafts/owner-message`. Needs `message:write` and `ai:use`, and `consult:read` for
 * aftercare or `message:read` for a reply. Never sends anything.
 */
export function draftOwnerMessage(request: OwnerMessageRequest): Promise<OwnerMessageDraft> {
  return staffApi().post<OwnerMessageDraft>('/staff/ai/drafts/owner-message', { body: request });
}
