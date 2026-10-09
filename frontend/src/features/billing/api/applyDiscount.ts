import { staffApi } from '@/lib/api/server-client';
import type { Invoice } from '../types/billing.types';

interface DiscountTarget {
  invoiceId: string;
  version: number;
}

/** A discount keyed in by hand: a reason and an approver, and exactly one of amount or percent. */
export interface ManualDiscountInput extends DiscountTarget {
  reason: string;
  approvedByStaffId: string;
  /** Exactly one of these two. A percent is taken off the subtotal and rounded down to the satang. */
  amountSatang?: number;
  percent?: number;
}

/** A coupon code. The backend fills in the reason and approver and works out the amount. */
export interface CouponDiscountInput extends DiscountTarget {
  couponCode: string;
}

/** Loyalty points, 1 point = ฿1. The caller is recorded as the approver. */
export interface PointsDiscountInput extends DiscountTarget {
  points: number;
}

export type ApplyDiscountInput = ManualDiscountInput | CouponDiscountInput | PointsDiscountInput;

/** The body is exactly one of the three shapes the endpoint takes. */
function discountBody(input: ApplyDiscountInput) {
  if ('couponCode' in input) {
    return { coupon_code: input.couponCode };
  }

  if ('points' in input) {
    return { points: input.points };
  }

  return {
    reason: input.reason,
    approved_by_staff_id: input.approvedByStaffId,
    amount_satang: input.amountSatang,
    percent: input.percent,
  };
}

/**
 * `POST /staff/invoices/{id}/discounts`: "ส่วนลดต้องมีเหตุผลและผู้อนุมัติ", or a coupon, or points.
 * Needs `invoice:discount`. Sent with `If-Match`, so a bill that gained a line since the page
 * loaded answers `409 version_conflict` instead of taking a discount against a total nobody saw.
 */
export function applyDiscount(input: ApplyDiscountInput): Promise<Invoice> {
  return staffApi().post<Invoice>(`/staff/invoices/${encodeURIComponent(input.invoiceId)}/discounts`, {
    ifMatch: input.version,
    body: discountBody(input),
  });
}
