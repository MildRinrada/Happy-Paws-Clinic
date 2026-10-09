'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { LinkPendingDot } from '@/components/layout/link-pending-dot';
import { crmArea, type CrmTab } from '../content/crm-tabs';

/** The row of CRM pages. The current one is filled in, and says so to a screen reader. */
export function CrmTabBar({ tabs }: { tabs: CrmTab[] }) {
  const pathname = usePathname();

  return (
    <nav aria-label={crmArea.tabsLabel} className="flex flex-wrap gap-1 border-b border-ink-200 pb-3">
      {tabs.map((tab) => {
        const current = pathname === tab.href || pathname.startsWith(`${tab.href}/`);

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
