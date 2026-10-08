import type { SendResult } from '@/features/notifications/types/notification.types';
import { staffApi } from '@/lib/api/server-client';
import type { LineBroadcastInput } from '../types/line.types';

/**
 * `POST /staff/integrations/line/broadcast` — "ประกาศถึงกลุ่มลูกค้าที่คัดไว้ · จำกัดโควตา". Needs
 * `notify:broadcast`. Marketing: only customers who agreed to it receive the message. Refusals:
 * `409 quota_exceeded`, `409 line_not_configured`.
 */
export function broadcastLine(input: LineBroadcastInput): Promise<SendResult> {
  return staffApi().post<SendResult>('/staff/integrations/line/broadcast', { body: input });
}
