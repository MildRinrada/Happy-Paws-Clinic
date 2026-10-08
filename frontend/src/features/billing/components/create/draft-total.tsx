import { formatBaht } from '@/lib/format/money';
import { createInvoicePage } from '../../content/create-invoice-page';

/** The lines typed so far, added up. A preview: the bill's totals are the backend's. */
export function DraftTotal({ subtotalSatang }: { subtotalSatang: number }) {
  const text = createInvoicePage.totals;

  return (
    <div className="flex flex-wrap items-baseline justify-between gap-2 rounded-lg bg-ink-50 px-3 py-2">
      <span className="text-sm text-ink-600">{text.subtotal}</span>
      <span className="font-semibold tabular-nums text-ink-900">{formatBaht(subtotalSatang)}</span>
      <p className="w-full text-xs text-ink-500">{text.hint}</p>
    </div>
  );
}
