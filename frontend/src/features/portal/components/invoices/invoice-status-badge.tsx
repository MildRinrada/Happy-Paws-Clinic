import { portalInvoices } from '../../content/portal-invoices';
import type { InvoiceStatus } from '../../types/invoice.types';

const tones: Record<InvoiceStatus, string> = {
  open: 'bg-amber-50 text-amber-800',
  partially_paid: 'bg-amber-50 text-amber-800',
  paid: 'bg-brand-50 text-site-teal',
  refunded: 'bg-sky-50 text-sky-800',
  void: 'bg-ink-100 text-ink-700',
};

/** Where the bill stands: waiting, part paid, paid, refunded, or withdrawn. */
export function InvoiceStatusBadge({ status }: { status: InvoiceStatus }) {
  return (
    <span className={`shrink-0 rounded-full px-2.5 py-1 text-xs font-semibold ${tones[status]}`}>
      {portalInvoices.status[status]}
    </span>
  );
}
