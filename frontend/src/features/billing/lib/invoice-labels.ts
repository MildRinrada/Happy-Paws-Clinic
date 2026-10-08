import type { BadgeTone } from '@/components/ui/badge';
import type { InvoiceStatus, PaymentMethod } from '../types/billing.types';

export const invoiceStatusLabel: Record<InvoiceStatus, { text: string; tone: BadgeTone }> = {
  open: { text: 'ค้างชำระ', tone: 'amber' },
  partially_paid: { text: 'ชำระบางส่วน', tone: 'blue' },
  paid: { text: 'ชำระแล้ว', tone: 'green' },
  refunded: { text: 'คืนเงินแล้ว', tone: 'neutral' },
  void: { text: 'ยกเลิก', tone: 'neutral' },
};

export const paymentMethodLabel: Record<PaymentMethod, string> = {
  cash: 'เงินสด',
  card: 'บัตร',
  promptpay: 'PromptPay',
};

/** A bill still takes money only in these two states. Everything else is settled or cancelled. */
export function isCollectible(status: InvoiceStatus): boolean {
  return status === 'open' || status === 'partially_paid';
}

/** Money can go back once some has come in and the bill is not void (docs/assumptions.md § 22). */
export function isRefundable(status: InvoiceStatus): boolean {
  return status === 'partially_paid' || status === 'paid' || status === 'refunded';
}
