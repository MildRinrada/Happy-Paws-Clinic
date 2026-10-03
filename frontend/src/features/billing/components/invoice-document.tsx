import { formatDateTime } from '@/lib/format/datetime';
import { formatBaht } from '@/lib/format/money';
import { paymentMethodLabel } from '../lib/invoice-labels';
import type { Invoice } from '../types/billing.types';

function Row({ label, value, strong = false }: { label: string; value: string; strong?: boolean }) {
  return (
    <div className={`flex justify-between gap-6 ${strong ? 'font-semibold text-ink-900' : 'text-ink-700'}`}>
      <dt>{label}</dt>
      <dd className="tabular-nums">{value}</dd>
    </div>
  );
}

/**
 * The bill itself: what was charged, what came off, what has been paid.
 *
 * Every figure is the backend's. Nothing here adds a column up — the totals are printed from the
 * fields that carry them, so the screen cannot disagree with the receipt.
 */
export function InvoiceDocument({ invoice }: { invoice: Invoice }) {
  return (
    <div className="space-y-5">
      <table className="w-full text-sm">
        <thead className="border-b border-ink-200 text-left text-ink-500">
          <tr>
            <th scope="col" className="py-2 font-medium">รายการ</th>
            <th scope="col" className="py-2 text-right font-medium">จำนวน</th>
            <th scope="col" className="py-2 text-right font-medium">ราคาต่อหน่วย</th>
            <th scope="col" className="py-2 text-right font-medium">รวม</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-ink-100">
          {invoice.lines.map((line) => (
            <tr key={line.id}>
              <td className="py-2.5 text-ink-900">{line.description}</td>
              <td className="py-2.5 text-right tabular-nums text-ink-700">{line.quantity}</td>
              <td className="py-2.5 text-right tabular-nums text-ink-700">{formatBaht(line.unit_price_satang)}</td>
              <td className="py-2.5 text-right tabular-nums text-ink-900">{formatBaht(line.line_total_satang)}</td>
            </tr>
          ))}
        </tbody>
      </table>

      <dl className="ml-auto max-w-xs space-y-1.5 text-sm">
        <Row label="รวมก่อนส่วนลด" value={formatBaht(invoice.subtotal_satang)} />
        {invoice.discount_satang > 0 && <Row label="ส่วนลด" value={`− ${formatBaht(invoice.discount_satang)}`} />}
        <Row label="ยอดรวม" value={formatBaht(invoice.total_satang)} strong />
        <Row label="ชำระแล้ว" value={formatBaht(invoice.paid_satang)} />
        {invoice.refunded_satang > 0 && <Row label="คืนเงิน" value={formatBaht(invoice.refunded_satang)} />}
        {/* A voided bill is owed nothing. Its balance is still the arithmetic remainder, and printing
            that as "outstanding" would send a cashier to collect on a bill that no longer stands. */}
        {invoice.status !== 'void' && (
          <div className="border-t border-ink-200 pt-1.5">
            <Row label="ค้างชำระ" value={formatBaht(invoice.balance_satang)} strong />
          </div>
        )}
      </dl>

      {invoice.discounts.length > 0 && (
        <section aria-label="ส่วนลด" className="text-sm">
          <h3 className="mb-1.5 font-medium text-ink-700">ส่วนลด</h3>
          <ul className="space-y-1 text-ink-600">
            {invoice.discounts.map((discount) => (
              <li key={discount.id}>
                {formatBaht(discount.amount_satang)} · {discount.reason} · อนุมัติโดย {discount.approved_by_staff_id}
              </li>
            ))}
          </ul>
        </section>
      )}

      {invoice.payments.length > 0 && (
        <section aria-label="การรับชำระ" className="text-sm">
          <h3 className="mb-1.5 font-medium text-ink-700">การรับชำระ</h3>
          <ul className="space-y-1 text-ink-600">
            {invoice.payments.map((payment) => (
              <li key={payment.id} className="flex justify-between gap-4">
                <span>
                  {formatDateTime(payment.collected_at)} · {paymentMethodLabel[payment.method]}
                  {payment.reference && ` · อ้างอิง ${payment.reference}`}
                </span>
                <span className="tabular-nums text-ink-800">{formatBaht(payment.amount_satang)}</span>
              </li>
            ))}
          </ul>
        </section>
      )}

      {invoice.refunds.length > 0 && (
        <section aria-label="การคืนเงิน" className="text-sm">
          <h3 className="mb-1.5 font-medium text-ink-700">การคืนเงิน</h3>
          <ul className="space-y-1 text-ink-600">
            {invoice.refunds.map((refund) => (
              <li key={refund.id} className="flex justify-between gap-4">
                <span>
                  {formatDateTime(refund.refunded_at)} · {paymentMethodLabel[refund.method]} · {refund.reason}
                </span>
                <span className="tabular-nums text-ink-800">{formatBaht(refund.amount_satang)}</span>
              </li>
            ))}
          </ul>
        </section>
      )}
    </div>
  );
}
