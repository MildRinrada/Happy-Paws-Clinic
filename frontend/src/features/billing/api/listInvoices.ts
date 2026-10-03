import { staffApi } from '@/lib/api/server-client';
import type { CursorPage } from '@/types/pagination';
import type { Invoice, InvoiceStatus } from '../types/billing.types';

export interface ListInvoicesQuery {
  branchId: string;
  status?: InvoiceStatus;
  cursor?: string;
  limit?: number;
}

/**
 * `GET /staff/invoices` — the counter's working list, newest first. Needs `invoice:write`.
 * Not in the API reference: see docs/contract-extensions.md § 7.
 */
export function listInvoices(query: ListInvoicesQuery): Promise<CursorPage<Invoice>> {
  return staffApi().get<CursorPage<Invoice>>('/staff/invoices', {
    query: { branch: query.branchId, status: query.status, cursor: query.cursor, limit: query.limit ?? 25 },
  });
}
