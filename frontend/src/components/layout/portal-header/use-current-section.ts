'use client';

import { usePathname } from 'next/navigation';
import { PORTAL_HOME_HREF } from '@/features/portal/content/portal-navigation';

/**
 * Whether a menu link is the section being viewed. The portal's home is `/portal` itself, which
 * every other portal page starts with, so it matches only exactly.
 */
export function useIsCurrent() {
  const pathname = usePathname();

  return (href: string) =>
    href === PORTAL_HOME_HREF ? pathname === href : pathname === href || pathname.startsWith(`${href}/`);
}
