'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { integrationsArea, type IntegrationsNavItem } from '../content/integrations-nav';

/** The row of "เชื่อมต่อระบบ" pages under the settings tabs. */
export function IntegrationsNav({ items }: { items: IntegrationsNavItem[] }) {
  const pathname = usePathname();

  return (
    <nav aria-label={integrationsArea.navLabel} className="flex flex-wrap gap-1">
      {items.map((item) => {
        const current = pathname === item.href || pathname.startsWith(`${item.href}/`);

        return (
          <Link
            key={item.href}
            href={item.href}
            aria-current={current ? 'page' : undefined}
            className={`rounded-full px-3 py-1 text-sm transition-colors ${
              current ? 'bg-ink-900 text-white' : 'text-ink-600 hover:bg-ink-100'
            }`}
          >
            {item.label}
          </Link>
        );
      })}
    </nav>
  );
}
