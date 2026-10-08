import { staffApi } from '@/lib/api/server-client';
import type { SendNotificationInput, SendResult } from '../types/notification.types';

/**
 * `POST /staff/notifications/send` — "ส่งเดี่ยวหรือเป็นกลุ่ม · เลือก channel ได้". Needs
 * `notify:send`. Answers 200 with one result per customer, including those not reached.
 */
export function sendNotification(input: SendNotificationInput): Promise<SendResult> {
  return staffApi().post<SendResult>('/staff/notifications/send', { body: input });
}
