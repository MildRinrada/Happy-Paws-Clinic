import { staffApi } from '@/lib/api/server-client';
import type { Invoice } from '../types/billing.types';

/**
 * `GET /staff/invoices/{id}` — needs `invoice:write`.
 * Not in the API reference: see docs/contract-extensions.md § 7.
 */
export function getInvoice(invoiceId: string): Promise<Invoice> {
  return staffApi().get<Invoice>(`/staff/invoices/${encodeURIComponent(invoiceId)}`);
}
