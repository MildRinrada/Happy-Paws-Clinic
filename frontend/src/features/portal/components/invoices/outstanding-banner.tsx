import { formatBaht } from '@/lib/format/money';
import { portalInvoices } from '../../content/portal-invoices';
import type { InvoicesView } from '../../lib/load-invoices';

/**
 * What the owner still owes across every unpaid bill, at the top of the page, and how to pay it.
 * A quiet line instead when nothing is owed.
 */
export function OutstandingBanner({ outstanding }: { outstanding: InvoicesView['outstanding'] }) {
  const text = portalInvoices.outstanding;

  if (outstanding.count === 0) {
    return <p className="rounded-2xl bg-white px-5 py-4 text-sm font-semibold text-site-teal shadow-sm">{text.none}</p>;
  }

  return (
    <section className="rounded-2xl bg-site-sun px-5 py-4 text-ink-900 shadow-sm">
      <div className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1">
        <h2 className="text-sm font-semibold">{text.title}</h2>
        <p className="font-display text-2xl font-bold tabular-nums">{formatBaht(outstanding.balanceSatang)}</p>
      </div>
      <div className="mt-1 flex flex-wrap justify-between gap-x-4 gap-y-1 text-sm">
        <p>{text.howToPay}</p>
        <p className="text-ink-700">{text.count(outstanding.count, outstanding.more)}</p>
      </div>
    </section>
  );
}
