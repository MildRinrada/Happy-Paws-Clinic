import Link from 'next/link';
import { mainNavLinks } from '@/config/site-navigation';
import { MobileAccountLinks } from './mobile-account-links';

const item = 'block rounded-lg px-3 py-2.5 text-ink-800 hover:bg-ink-50';

/**
 * The same links as `MainNav`, folded behind a menu button on screens too narrow for the row.
 *
 * A <details> element, so it opens and closes with no script and works before the page hydrates.
 */
export function MobileNav() {
  return (
    <details className="relative xl:hidden">
      <summary
        aria-label="เมนู"
        className="flex size-11 cursor-pointer list-none items-center justify-center rounded-lg text-ink-800 hover:bg-ink-100 [&::-webkit-details-marker]:hidden"
      >
        <svg viewBox="0 0 24 24" aria-hidden="true" className="size-6" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
          <path d="M4 7h16M4 12h16M4 17h16" />
        </svg>
      </summary>

      <nav
        aria-label="เมนูหลัก"
        className="absolute right-0 top-full z-20 mt-2 w-60 rounded-xl border border-ink-200 bg-white p-2 shadow-lg"
      >
        <ul>
          {mainNavLinks.map((link) => (
            <li key={link.label}>
              <Link href={link.href} className={item}>
                {link.label}
              </Link>
            </li>
          ))}

          {/* On a phone the header has no room for the account links, so the menu carries them. */}
          <MobileAccountLinks itemClassName={item} />
        </ul>
      </nav>
    </details>
  );
}
