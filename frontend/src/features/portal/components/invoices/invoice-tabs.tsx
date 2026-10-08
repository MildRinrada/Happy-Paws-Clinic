import Link from 'next/link';
import { portalInvoices } from '../../content/portal-invoices';
import { portalPaths } from '../../content/portal-navigation';

interface InvoiceTabsProps {
  unpaidOnly: boolean;
  unpaidCount: number;
  /** More unpaid bills than the count could hold. */
  more: boolean;
}

/** "ทั้งหมด" and "ค้างชำระ" as two links; the one in view is filled. */
export function InvoiceTabs({ unpaidOnly, unpaidCount, more }: InvoiceTabsProps) {
  const tabs = [
    { label: portalInvoices.tabs.all, href: portalPaths.invoices, current: !unpaidOnly },
    { label: portalInvoices.tabs.unpaid(unpaidCount, more), href: `${portalPaths.invoices}?unpaid=true`, current: unpaidOnly },
  ];

  return (
    <nav className="flex gap-2">
      {tabs.map((tab) => (
        <Link
          key={tab.href}
          href={tab.href}
          aria-current={tab.current ? 'page' : undefined}
          className={`inline-flex h-9 items-center rounded-full px-4 text-sm font-medium ${
            tab.current ? 'bg-site-teal text-white' : 'bg-white text-ink-700 shadow-sm hover:text-site-teal'
          }`}
        >
          {tab.label}
        </Link>
      ))}
    </nav>
  );
}
