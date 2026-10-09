import { staffApi } from '@/lib/api/server-client';
import type { PointsAdjustment } from '../types/crm.types';

/**
 * `POST /staff/loyalty/{customer_id}/adjustments`. Needs `crm:manage` and an `Idempotency-Key`.
 * Signed points with a reason; taking more than the balance is `409 insufficient_points`.
 * docs/contract-extensions.md § 25.
 */
export function adjustPoints(
  customerId: string,
  input: { points: number; reason: string },
  idempotencyKey: string,
): Promise<PointsAdjustment> {
  return staffApi().post<PointsAdjustment>(`/staff/loyalty/${encodeURIComponent(customerId)}/adjustments`, {
    idempotencyKey,
    body: input,
  });
}
