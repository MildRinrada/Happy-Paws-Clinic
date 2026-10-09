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

  /** The LINE broadcast page, already pointed at a segment. The page reads these three back. */
  broadcastToSegment: (segment: { id: string; name: string; reachable: number }) =>
    `/notifications/broadcast?${new URLSearchParams({
      segment_id: segment.id,
      segment_name: segment.name,
      reachable: String(segment.reachable),
    }).toString()}`,
};
