import { staffApi } from '@/lib/api/server-client';
import type { Invoice } from '../types/billing.types';

/**
 * `POST /staff/invoices/{id}/void` — cancels a bill that has taken no money. Needs `invoice:write`.
 * Not in the API reference: see docs/contract-extensions.md § 3.
 */
export function voidInvoice(invoiceId: string, version: number, reason: string): Promise<Invoice> {
  return staffApi().post<Invoice>(`/staff/invoices/${encodeURIComponent(invoiceId)}/void`, {
    ifMatch: version,
    body: { reason },
  });
}
