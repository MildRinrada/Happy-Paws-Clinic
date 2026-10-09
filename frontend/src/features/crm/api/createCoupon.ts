import { staffApi } from '@/lib/api/server-client';
import type { Coupon, NewCouponInput } from '../types/crm.types';

/** `POST /staff/coupons`. Needs `crm:manage`. Refusal: `409 coupon_code_taken`. */
export function createCoupon(input: NewCouponInput): Promise<Coupon> {
  return staffApi().post<Coupon>('/staff/coupons', { body: input });
}
