'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import type { ReactNode } from 'react';
import { LinkPendingDot } from './link-pending-dot';

interface NavLinkProps {
  href: string;
  /** The section the link stands for, when it opens one page of several (settings opens its first tab). */
  section?: string;
  children: ReactNode;
}

/** A sidebar link that knows when it is the current page, and says so to a screen reader. */
export function NavLink({ href, section = href, children }: NavLinkProps) {
  const pathname = usePathname();
  const current = pathname === section || pathname.startsWith(`${section}/`);

  return (
    <Link
      href={href}
      aria-current={current ? 'page' : undefined}
      className={`block rounded-lg px-3 py-2 text-sm font-medium transition-colors ${
        current ? 'bg-brand-600 text-white' : 'text-ink-700 hover:bg-ink-100'
      }`}
    >
      {children}
      <LinkPendingDot />
    </Link>
  );
}
