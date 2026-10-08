import { staffApi } from '@/lib/api/server-client';
import type { NotificationTemplate } from '../types/notification.types';

/** `GET /staff/notifications/templates` — needs `notify:manage`. Short and fixed, so returned whole. */
export async function listTemplates(): Promise<NotificationTemplate[]> {
  return (await staffApi().get<{ data: NotificationTemplate[] }>('/staff/notifications/templates')).data;
}
