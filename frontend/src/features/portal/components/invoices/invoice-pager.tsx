import Link from 'next/link';
import { portalInvoices } from '../../content/portal-invoices';
import { portalPaths } from '../../content/portal-navigation';

interface InvoicePagerProps {
  unpaidOnly: boolean;
  nextCursor?: string;
  /** True when this is not the first page, so there is a newest page to go back to. */
  paged: boolean;
}

/** Older bills, and back to the newest. The API pages forward only, so there is no "previous". */
export function InvoicePager({ unpaidOnly, nextCursor, paged }: InvoicePagerProps) {
  const base = unpaidOnly ? `${portalPaths.invoices}?unpaid=true` : portalPaths.invoices;
  const join = unpaidOnly ? '&' : '?';

  if (!nextCursor && !paged) return null;

  return (
    <div className="flex justify-between gap-4 text-sm font-semibold">
      {paged ? (
        <Link href={base} className="text-site-teal hover:underline">
          {portalInvoices.newest}
        </Link>
      ) : (
        <span />
      )}
      {nextCursor && (
        <Link href={`${base}${join}cursor=${encodeURIComponent(nextCursor)}`} className="text-site-teal hover:underline">
          {portalInvoices.older}
        </Link>
      )}
    </div>
  );
}
