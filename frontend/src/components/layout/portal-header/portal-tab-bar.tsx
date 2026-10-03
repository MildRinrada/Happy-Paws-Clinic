'use client';

import Link from 'next/link';
import { portalHeader, portalNavLinks } from '@/features/portal/content/portal-navigation';
import { PortalIcon } from './portal-icon';
import { useIsCurrent } from './use-current-section';

/**
 * The portal's menu on a phone: a bar fixed to the bottom of the screen, within reach of a thumb.
 * Hidden from the width where the header has room for the menu itself.
 */
export function PortalTabBar() {
  const isCurrent = useIsCurrent();

  return (
    <nav
      aria-label={portalHeader.navLabel}
      className="fixed inset-x-0 bottom-0 z-20 border-t border-ink-200 bg-white pb-[env(safe-area-inset-bottom)] lg:hidden"
    >
      <ul className="mx-auto grid max-w-md grid-cols-4">
        {portalNavLinks.map((link) => {
          const current = isCurrent(link.href);

          return (
            <li key={link.href}>
              <Link
                href={link.href}
                aria-current={current ? 'page' : undefined}
                className={`flex flex-col items-center gap-0.5 py-2 text-[11px] ${
                  current ? 'font-semibold text-site-teal' : 'text-ink-500'
                }`}
              >
                <PortalIcon name={link.icon} />
                {link.short}
              </Link>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}
