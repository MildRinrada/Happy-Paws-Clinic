import { formatBaht } from '@/lib/format/money';
import { portalInvoices } from '../../content/portal-invoices';
import type { InvoiceLine } from '../../types/invoice.types';

/** What was charged, one row per line: the item, quantity times unit price, and the line's total. */
export function InvoiceLines({ lines }: { lines: InvoiceLine[] }) {
  const text = portalInvoices.lines;

  return (
    <div>
      <h3 className="mb-2 text-xs font-semibold text-site-muted">{text.title}</h3>
      <ul className="divide-y divide-ink-100">
        {lines.map((line) => (
          <li key={line.id} className="flex justify-between gap-4 py-2 text-sm">
            <div className="min-w-0">
              <p className="text-ink-900">{line.description}</p>
              <p className="text-xs text-site-muted">{text.quantity(line.quantity, formatBaht(line.unit_price_satang))}</p>
            </div>
            <p className="shrink-0 tabular-nums text-ink-900">{formatBaht(line.line_total_satang)}</p>
          </li>
        ))}
      </ul>
    </div>
  );
}
