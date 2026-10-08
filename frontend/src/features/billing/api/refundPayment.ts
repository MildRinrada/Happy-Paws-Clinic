import { staffApi } from '@/lib/api/server-client';
import type { Invoice, PaymentMethod } from '../types/billing.types';

export interface RefundPaymentInput {
  invoiceId: string;
  version: number;
  amountSatang: number;
  reason: string;
  /** Left out: the method of `paymentId`, else the bill's only tender, else cash. */
  method?: PaymentMethod;
  /** The tender being reversed, when the refund maps onto one. */
  paymentId?: string;
  /** One key per refund, reused on every retry, so a retry cannot give the money back twice. */
  idempotencyKey: string;
}

/**
 * `POST /staff/invoices/{id}/refunds`: "คืนเงินบางส่วนหรือทั้งใบ ผูกเหตุผล". Needs
 * `payment:refund`. Any refund, partial or whole, moves the bill to `refunded` for good
 * (docs/assumptions.md § 22).
 */
export function refundPayment(input: RefundPaymentInput): Promise<Invoice> {
  return staffApi().post<Invoice>(`/staff/invoices/${encodeURIComponent(input.invoiceId)}/refunds`, {
    idempotencyKey: input.idempotencyKey,
    ifMatch: input.version,
    body: {
      amount_satang: input.amountSatang,
      reason: input.reason,
      method: input.method,
      payment_id: input.paymentId,
    },
  });
}
