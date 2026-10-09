import { staffApi } from '@/lib/api/server-client';
import type { ThreadMessage } from '../types/thread.types';

/**
 * `POST /staff/threads/{id}/messages` — "ตอบกลับ · ระบบเลือกช่องทางที่ลูกค้าใช้ล่าสุด". Needs
 * `message:write`. Answers 201 with the reply and its `delivery`, also when it reached no one.
 */
export function replyToThread(threadId: string, text: string): Promise<ThreadMessage> {
  return staffApi().post<ThreadMessage>(`/staff/threads/${encodeURIComponent(threadId)}/messages`, { body: { text } });
}
