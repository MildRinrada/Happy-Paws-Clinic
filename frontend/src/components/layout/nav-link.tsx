'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import type { ReactNode } from 'react';

/** A sidebar link that knows when it is the current page, and says so to a screen reader. */
export function NavLink({ href, children }: { href: string; children: ReactNode }) {
  const pathname = usePathname();
  const current = pathname === href || pathname.startsWith(`${href}/`);

  return (
    <Link
      href={href}
      aria-current={current ? 'page' : undefined}
      className={`block rounded-lg px-3 py-2 text-sm font-medium transition-colors ${
        current ? 'bg-brand-600 text-white' : 'text-ink-700 hover:bg-ink-100'
      }`}
    >
      {children}
    </Link>
  );
}
