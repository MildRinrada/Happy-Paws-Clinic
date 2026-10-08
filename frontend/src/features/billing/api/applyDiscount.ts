import { staffApi } from '@/lib/api/server-client';
import type { Invoice } from '../types/billing.types';

export interface ApplyDiscountInput {
  invoiceId: string;
  version: number;
  reason: string;
  approvedByStaffId: string;
  /** Exactly one of these two. A percent is taken off the subtotal and rounded down to the satang. */
  amountSatang?: number;
  percent?: number;
}

/**
 * `POST /staff/invoices/{id}/discounts`: "ส่วนลดต้องมีเหตุผลและผู้อนุมัติ". Needs
 * `invoice:discount`. Sent with `If-Match`, so a bill that gained a line since the page loaded
 * answers `409 version_conflict` instead of taking a discount against a total nobody saw.
 */
export function applyDiscount(input: ApplyDiscountInput): Promise<Invoice> {
  return staffApi().post<Invoice>(`/staff/invoices/${encodeURIComponent(input.invoiceId)}/discounts`, {
    ifMatch: input.version,
    body: {
      reason: input.reason,
      approved_by_staff_id: input.approvedByStaffId,
      amount_satang: input.amountSatang,
      percent: input.percent,
    },
  });
}
