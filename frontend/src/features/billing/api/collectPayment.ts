import { staffApi } from '@/lib/api/server-client';
import type { Invoice, PaymentMethod } from '../types/billing.types';

export interface CollectPaymentInput {
  invoiceId: string;
  version: number;
  method: PaymentMethod;
  amountSatang: number;
  /** The slip or transaction number, for a card or PromptPay tender. */
  reference?: string;
  /** The same key on a retry returns the payment already taken instead of taking it twice. */
  idempotencyKey: string;
}

/**
 * `POST /staff/invoices/{id}/payments` — "เงินสด บัตร PromptPay · แบ่งจ่ายหลายวิธีได้". One call is
 * one tender; a split payment is two calls. Needs `payment:collect`.
 */
export function collectPayment(input: CollectPaymentInput): Promise<Invoice> {
  return staffApi().post<Invoice>(`/staff/invoices/${encodeURIComponent(input.invoiceId)}/payments`, {
    idempotencyKey: input.idempotencyKey,
    ifMatch: input.version,
    body: { method: input.method, amount_satang: input.amountSatang, reference: input.reference },
  });
}
