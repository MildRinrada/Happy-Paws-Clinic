import { formatLongDate } from '@/lib/format/datetime';
import { portalInvoices } from '../../content/portal-invoices';
import type { InvoiceReceipt as Receipt } from '../../types/invoice.types';

/**
 * The receipt's number and date, and whether it is a tax invoice. The portal has no copy of the
 * document itself to download; a paid bill without one says where to ask.
 */
export function InvoiceReceipt({ receipt, paid }: { receipt?: Receipt | null; paid: boolean }) {
  const text = portalInvoices.receipt;

  if (!receipt) {
    return paid ? <p className="text-xs text-site-muted">{text.none}</p> : null;
  }

  return (
    <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-sm">
      <span className="font-semibold text-ink-900">{text.title}</span>
      <span className="text-ink-700">{text.number(receipt.number)}</span>
      {receipt.is_tax_invoice && (
        <span className="rounded-full bg-brand-50 px-2 py-0.5 text-xs font-semibold text-site-teal">{text.taxInvoice}</span>
      )}
      <span className="text-xs text-site-muted">{text.issued(formatLongDate(receipt.issued_at))}</span>
    </div>
  );
}
