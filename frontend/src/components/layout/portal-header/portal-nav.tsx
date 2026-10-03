'use client';

import Link from 'next/link';
import { portalHeader, portalNavLinks } from '@/features/portal/content/portal-navigation';
import { useIsCurrent } from './use-current-section';

/** The portal's menu as a row in the header, on screens wide enough. A phone uses the bottom bar. */
export function PortalNav() {
  const isCurrent = useIsCurrent();

  return (
    <nav aria-label={portalHeader.navLabel} className="hidden lg:block">
      <ul className="flex items-center gap-x-8">
        {portalNavLinks.map((link) => {
          const current = isCurrent(link.href);

          return (
            <li key={link.href}>
              <Link
                href={link.href}
                aria-current={current ? 'page' : undefined}
                className={`border-b-2 py-1 text-sm transition-colors ${
                  current
                    ? 'border-site-teal font-semibold text-site-teal'
                    : 'border-transparent text-ink-700 hover:text-site-teal'
                }`}
              >
                {link.label}
              </Link>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}
