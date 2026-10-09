import { staffApi } from '@/lib/api/server-client';
import type { ChatMessage } from '../types/chat.types';

/**
 * `POST /portal/thread/messages` (contract extension § 24) — the owner writes to the clinic. The
 * first message opens the conversation. Answers 201 with the stored message; under the messaging
 * rate limit, so a burst is refused with `rate_limited`.
 */
export function writeToClinic(text: string): Promise<ChatMessage> {
  return staffApi().post<ChatMessage>('/portal/thread/messages', { body: { text } });
}
