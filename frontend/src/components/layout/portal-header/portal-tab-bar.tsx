'use client';

import Link from 'next/link';
import { portalHeader, portalNavLinks } from '@/features/portal/content/portal-navigation';
import { PortalIcon } from './portal-icon';
import { useIsCurrent } from './use-current-section';

/**
 * The portal's menu on a phone: a bar fixed to the bottom of the screen, within reach of a thumb.
 * The current tab is green with its icon filled in. Hidden from the width where the header has
 * room for the menu itself.
 */
export function PortalTabBar() {
  const isCurrent = useIsCurrent();

  return (
    <nav
      aria-label={portalHeader.navLabel}
      className="fixed inset-x-0 bottom-0 z-20 border-t border-ink-200 bg-white pb-[env(safe-area-inset-bottom)] lg:hidden"
    >
      <ul className="mx-auto grid max-w-md grid-cols-4 px-2">
        {portalNavLinks.map((link) => {
          const current = isCurrent(link.href);

          return (
            <li key={link.href}>
              <Link
                href={link.href}
                aria-current={current ? 'page' : undefined}
                className={`flex flex-col items-center gap-3 pb-3.5 pt-2 font-sans text-xs leading-[18px] ${
                  current ? 'font-semibold text-brand-600' : 'text-ink-600'
                }`}
              >
                <PortalIcon name={link.icon} solid={current} />
                {link.short}
              </Link>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}
