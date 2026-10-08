import { formatLongDate } from '@/lib/format/datetime';
import { formatBaht } from '@/lib/format/money';
import { portalInvoices } from '../../content/portal-invoices';
import { isOwed } from '../../lib/invoice-rules';
import type { Invoice } from '../../types/invoice.types';
import { InvoiceLines } from './invoice-lines';
import { InvoicePayments } from './invoice-payments';
import { InvoiceReceipt } from './invoice-receipt';
import { InvoiceStatusBadge } from './invoice-status-badge';
import { InvoiceTotals } from './invoice-totals';

/**
 * One bill. Closed, it is the number, the date, the pet, the total and its state, with what is
 * still owed in amber. Opened, it is the whole bill: lines, sums, payments and the receipt. The
 * list already carries every detail, so opening needs no further request.
 */
export function InvoiceCard({ invoice, petName }: { invoice: Invoice; petName?: string }) {
  const text = portalInvoices;
  const owed = isOwed(invoice);

  return (
    <details className={`group rounded-2xl bg-white shadow-sm ${owed ? 'ring-1 ring-amber-300' : ''}`}>
      <summary className="flex cursor-pointer list-none items-start gap-4 p-4 sm:p-5 [&::-webkit-details-marker]:hidden">
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            <p className="text-sm font-semibold text-ink-900">{text.number(invoice.number)}</p>
            <InvoiceStatusBadge status={invoice.status} />
          </div>
          <p className="mt-1 flex flex-wrap gap-x-3 text-xs text-site-muted">
            <span>{formatLongDate(invoice.issued_at)}</span>
            {petName && <span>{text.forPet(petName)}</span>}
          </p>
        </div>

        <div className="shrink-0 text-right">
          <p className={`font-display text-lg font-bold tabular-nums ${invoice.status === 'void' ? 'text-site-muted line-through' : 'text-ink-900'}`}>
            {formatBaht(invoice.total_satang)}
          </p>
          {owed && <p className="text-xs font-semibold text-amber-800">{text.balanceDue(formatBaht(invoice.balance_satang))}</p>}
          <p className="mt-1 text-xs font-semibold text-site-teal group-open:hidden">{text.showDetail}</p>
        </div>
      </summary>

      <div className="space-y-4 border-t border-ink-100 p-4 sm:p-5">
        {invoice.status === 'void' && invoice.void_reason && <p className="text-sm text-ink-700">{text.voided(invoice.void_reason)}</p>}
        <InvoiceLines lines={invoice.lines} />
        <InvoiceTotals invoice={invoice} />
        <InvoicePayments payments={invoice.payments} refunds={invoice.refunds} />
        <InvoiceReceipt receipt={invoice.receipt} paid={invoice.status === 'paid'} />
        {invoice.note && <p className="text-xs text-site-muted">{text.note(invoice.note)}</p>}
      </div>
    </details>
  );
}
