import { formatDateTime } from '@/lib/format/datetime';
import { formatBaht } from '@/lib/format/money';
import { portalInvoices } from '../../content/portal-invoices';
import type { InvoicePayment, InvoiceRefund } from '../../types/invoice.types';

/** Each payment taken and each refund given, oldest first. Nothing when there are none. */
export function InvoicePayments({ payments, refunds }: { payments: InvoicePayment[]; refunds: InvoiceRefund[] }) {
  const text = portalInvoices;
  if (payments.length === 0 && refunds.length === 0) return null;

  return (
    <div>
      <h3 className="mb-2 text-xs font-semibold text-site-muted">{text.payments.title}</h3>
      <ul className="space-y-1.5 text-sm">
        {payments.map((payment) => (
          <li key={payment.id} className="flex justify-between gap-4">
            <span className="text-ink-700">
              {text.method[payment.method] ?? payment.method}{' '}
              <span className="text-xs text-site-muted">{formatDateTime(payment.collected_at)}</span>
            </span>
            <span className="shrink-0 tabular-nums text-ink-900">{formatBaht(payment.amount_satang)}</span>
          </li>
        ))}
        {refunds.map((refund) => (
          <li key={refund.id} className="flex justify-between gap-4">
            <span className="text-ink-700">
              {text.payments.refund(refund.reason)}{' '}
              <span className="text-xs text-site-muted">{formatDateTime(refund.refunded_at)}</span>
            </span>
            <span className="shrink-0 tabular-nums text-ink-900">-{formatBaht(refund.amount_satang)}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}
