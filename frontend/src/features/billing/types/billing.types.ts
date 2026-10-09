/** The contract's invoice state machine: open → partially_paid → paid → refunded, with void. */
export type InvoiceStatus = 'open' | 'partially_paid' | 'paid' | 'refunded' | 'void';

/** "เงินสด บัตร PromptPay". */
export type PaymentMethod = 'cash' | 'card' | 'promptpay';

/** What a line is for. `medicine` is a dispensed drug (or one rung up by hand); `other` claims nothing. */
export type InvoiceLineKind = 'service' | 'treatment' | 'medicine' | 'other';

export interface InvoiceLine {
  id: string;
  kind: InvoiceLineKind;
  description: string;
  quantity: number;
  unit_price_satang: number;
  line_total_satang: number;
  /** The prescription a `medicine` line was charged from, when it came from a dispense. */
  source_id?: string;
}

export interface InvoiceDiscount {
  id: string;
  amount_satang: number;
  percent?: number;
  reason: string;
  approved_by_staff_id: string;
  applied_by_staff_id: string;
  applied_at: string;
}

export interface InvoicePayment {
  id: string;
  method: PaymentMethod;
  amount_satang: number;
  reference?: string;
  collected_by_staff_id: string;
  collected_at: string;
}

export interface InvoiceRefund {
  id: string;
  /** The tender being reversed, when the refund was tied to one. */
  payment_id?: string;
  method: PaymentMethod;
  amount_satang: number;
  reason: string;
  refunded_by_staff_id: string;
  refunded_at: string;
}

export interface Receipt {
  id: string;
  number: string;
  is_tax_invoice: boolean;
  amount_satang: number;
  tax_id?: string;
  legal_name?: string;
  billing_address?: string;
  issued_at: string;
}

/**
 * A bill as the API returns it.
 *
 * Every total is sent by the backend and none is computed here. Two programs adding up the same
 * lines is two chances to disagree about what a customer owes, and the place that disagreement
 * would surface is this desk, with a queue behind it.
 */
export interface Invoice {
  id: string;
  version: number;
  number: string;
  status: InvoiceStatus;
  branch_id: string;
  customer_id: string;
  pet_id?: string;
  consultation_id?: string;
  issued_at: string;
  issued_by_staff_id: string;
  note?: string;
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
  receipt?: Receipt;
  voided_at?: string;
  void_reason?: string;
}

/** One row of `POST /staff/invoices`. Kind defaults to `other` on the backend when left out. */
export interface InvoiceLineInput {
  description: string;
  quantity: number;
  unit_price_satang: number;
  kind?: InvoiceLineKind;
  /** The catalogue service a `service` line was picked from. Lets a package cover it. */
  source_id?: string;
}
