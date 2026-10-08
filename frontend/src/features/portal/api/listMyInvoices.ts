import { staffApi } from '@/lib/api/server-client';
import type { InvoicePage } from '../types/invoice.types';

/**
 * `GET /portal/invoices` — the owner's own bills, newest first. `unpaid` keeps only what is still
 * owed (`open` and `partially_paid`). Each bill comes whole: lines, discounts, payments, refunds
 * and its receipt, so there is no separate read for one bill.
 */
export function listMyInvoices(unpaid: boolean, limit: number, cursor?: string): Promise<InvoicePage> {
  return staffApi().get<InvoicePage>('/portal/invoices', { query: { unpaid, limit, cursor } });
}
