import { portalInvoices } from '../../content/portal-invoices';
import { isOwed } from '../../lib/invoice-rules';
import type { Invoice } from '../../types/invoice.types';
import { AmountRow } from './amount-row';

/**
 * The bill's sums, every one as the backend sent it. Discounts are listed by their reason, so the
 * owner sees why the total is lower. Rows that would read zero are left out.
 */
export function InvoiceTotals({ invoice }: { invoice: Invoice }) {
  const text = portalInvoices;
  const owed = isOwed(invoice);

  return (
    <dl className="space-y-1.5 rounded-xl bg-site-soft px-4 py-3">
      {invoice.discounts.length > 0 && <AmountRow label={text.totals.subtotal} satang={invoice.subtotal_satang} />}
      {invoice.discounts.map((discount) => (
        <AmountRow key={discount.id} label={text.discountReason(discount.reason)} satang={discount.amount_satang} minus />
      ))}
      <AmountRow label={text.totals.total} satang={invoice.total_satang} strong />
      {invoice.paid_satang > 0 && <AmountRow label={text.totals.paid} satang={invoice.paid_satang} />}
      {invoice.refunded_satang > 0 && <AmountRow label={text.totals.refunded} satang={invoice.refunded_satang} />}
      {owed && <AmountRow label={text.totals.balance} satang={invoice.balance_satang} strong due />}
    </dl>
  );
}
