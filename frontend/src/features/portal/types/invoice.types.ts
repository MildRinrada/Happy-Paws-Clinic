/**
 * A bill as `GET /portal/invoices` returns it. Every amount is an integer number of satang and every
 * total is the backend's own: the page never adds lines up itself.
 */

export type InvoiceStatus = 'open' | 'partially_paid' | 'paid' | 'refunded' | 'void';

export type PaymentMethod = 'cash' | 'card' | 'promptpay';

export interface InvoiceLine {
  id: string;
  kind: string;
  description: string;
  quantity: number;
  unit_price_satang: number;
  line_total_satang: number;
}

export interface InvoiceDiscount {
  id: string;
  amount_satang: number;
  percent?: number | null;
  reason: string;
  applied_at: string;
}

export interface InvoicePayment {
  id: string;
  method: PaymentMethod;
  amount_satang: number;
  collected_at: string;
}

export interface InvoiceRefund {
  id: string;
  method: PaymentMethod;
  amount_satang: number;
  reason: string;
  refunded_at: string;
}

export interface InvoiceReceipt {
  id: string;
  number: string;
  is_tax_invoice: boolean;
  amount_satang: number;
  issued_at: string;
}

export interface Invoice {
  id: string;
  number: string;
  status: InvoiceStatus;
  branch_id: string;
  pet_id?: string | null;
  issued_at: string;
  note?: string | null;
  subtotal_satang: number;
  discount_satang: number;
  total_satang: number;
  paid_satang: number;
  refunded_satang: number;
  balance_satang: number;
  lines: InvoiceLine[];
  discounts: InvoiceDiscount[];
  payments: InvoicePayment[];
  refunds: InvoiceRefund[];
  receipt?: InvoiceReceipt | null;
  voided_at?: string | null;
  void_reason?: string | null;
}

export interface InvoicePage {
  data: Invoice[];
  page: { next_cursor?: string | null };
}
