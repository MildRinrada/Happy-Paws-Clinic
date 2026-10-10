import type { DueBackReason } from '../types/due-back.types';

/** Links the CRM pages hand out. */
export const crmPaths = {
  loyalty: (customerId?: string) =>
    customerId ? `/crm/loyalty?customer_id=${encodeURIComponent(customerId)}` : '/crm/loyalty',
  invoice: (invoiceId: string) => `/billing/${encodeURIComponent(invoiceId)}`,
  segments: '/crm/segments',
  segment: (segmentId: string) => `/crm/segments/${encodeURIComponent(segmentId)}`,
  customerPackages: (customerId?: string) =>
    customerId ? `/crm/customer-packages?customer_id=${encodeURIComponent(customerId)}` : '/crm/customer-packages',
  customer: (customerId: string) => `/customers/${encodeURIComponent(customerId)}`,

  /** The call list of docs/contract-extensions.md § 33, on one reason and, past the first page, a cursor. */
  dueBack: (reason: DueBackReason, cursor?: string) =>
    `/crm/due-back?${new URLSearchParams({ reason, ...(cursor ? { cursor } : {}) }).toString()}`,

  /** The LINE broadcast page, already pointed at a segment. The page reads these three back. */
  broadcastToSegment: (segment: { id: string; name: string; reachable: number }) =>
    `/notifications/broadcast?${new URLSearchParams({
      segment_id: segment.id,
      segment_name: segment.name,
      reachable: String(segment.reachable),
    }).toString()}`,
};
