import { staffApi } from '@/lib/api/server-client';
import type { CursorPage } from '@/types/pagination';
import type { Delivery, DeliveryLogQuery } from '../types/notification.types';

/**
 * `GET /staff/notifications/log` — "ส่งสำเร็จ/ล้มเหลว · fallback ไป SMS เมื่อไหร่". Needs
 * `notify:read`. Newest first; a branch-bound reader sees their branches plus clinic-wide rows.
 */
export function getDeliveryLog(query: DeliveryLogQuery): Promise<CursorPage<Delivery>> {
  return staffApi().get<CursorPage<Delivery>>('/staff/notifications/log', {
    query: { ...query, limit: 30 },
  });
}
