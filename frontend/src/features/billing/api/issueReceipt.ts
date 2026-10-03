import { staffApi } from '@/lib/api/server-client';
import type { Invoice } from '../types/billing.types';

export interface IssueReceiptInput {
  invoiceId: string;
  /** Set, with a tax id, for a ใบกำกับภาษี rather than a plain ใบเสร็จ. */
  taxInvoice?: { taxId: string; legalName: string; billingAddress: string };
}

/**
 * `POST /staff/invoices/{id}/receipt` — "ออกใบเสร็จ/ใบกำกับภาษี · ส่งเข้า LINE". Needs
 * `receipt:issue`. One invoice has one receipt: asking again returns the one already issued.
 */
export function issueReceipt(input: IssueReceiptInput): Promise<Invoice> {
  return staffApi().post<Invoice>(`/staff/invoices/${encodeURIComponent(input.invoiceId)}/receipt`, {
    body: input.taxInvoice
      ? {
          is_tax_invoice: true,
          tax_id: input.taxInvoice.taxId,
          legal_name: input.taxInvoice.legalName,
          billing_address: input.taxInvoice.billingAddress,
        }
      : {},
  });
}
