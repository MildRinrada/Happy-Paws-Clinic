'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { LinkPendingDot } from '@/components/layout/link-pending-dot';
import { inventoryArea, type InventoryTab } from '../content/inventory-tabs';

/** The row of stock pages. The current one is filled in, and says so to a screen reader. */
export function InventoryTabBar({ tabs }: { tabs: InventoryTab[] }) {
  const pathname = usePathname();

  return (
    <nav aria-label={inventoryArea.tabsLabel} className="no-print flex flex-wrap gap-1 border-b border-ink-200 pb-3">
      {tabs.map((tab) => {
        const current = tab.exact ? pathname === tab.href : pathname === tab.href || pathname.startsWith(`${tab.href}/`);

        return (
          <Link
            key={tab.href}
            href={tab.href}
            aria-current={current ? 'page' : undefined}
            className={`rounded-lg px-3 py-1.5 text-sm font-medium transition-colors ${
              current ? 'bg-brand-50 text-brand-800 ring-1 ring-brand-200' : 'text-ink-600 hover:bg-ink-100'
            }`}
          >
            {tab.label}
            <LinkPendingDot />
          </Link>
        );
      })}
    </nav>
  );
}
