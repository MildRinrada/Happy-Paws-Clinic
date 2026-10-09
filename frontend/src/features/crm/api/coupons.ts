import { staffApi } from '@/lib/api/server-client';
import type { CursorPage } from '@/types/pagination';
import type { Coupon, CouponStatus } from '../types/crm.types';

export interface CouponQuery {
  status?: CouponStatus;
  branch?: string;
  q?: string;
  cursor?: string;
}

/**
 * `GET /staff/coupons`. Needs `crm:read`. Newest first, each with its uses and status today. Branch
 * staff get their branches' coupons and the clinic-wide ones. docs/contract-extensions.md § 25.
 */
export function listCoupons(query: CouponQuery = {}): Promise<CursorPage<Coupon>> {
  return staffApi().get<CursorPage<Coupon>>('/staff/coupons', {
    query: { status: query.status, branch: query.branch, q: query.q || undefined, cursor: query.cursor, limit: 25 },
  });
}

/**
 * `POST /staff/coupons/{id}/deactivate`. Needs `crm:manage`. The next redemption is refused with
 * `coupon_deactivated`; switching off twice is `409 state_conflict`. § 25.
 */
export function deactivateCoupon(id: string, reason?: string): Promise<Coupon> {
  return staffApi().post<Coupon>(`/staff/coupons/${encodeURIComponent(id)}/deactivate`, { body: { reason } });
}
