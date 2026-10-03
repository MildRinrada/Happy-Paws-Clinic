import Link from 'next/link';
import { mainNavLinks } from '@/config/site-navigation';

/** The header's row of links, for screens wide enough to hold it. */
export function MainNav() {
  return (
    <nav aria-label="เมนูหลัก" className="hidden xl:block">
      <ul className="flex items-center gap-x-9">
        {mainNavLinks.map((link) => (
          <li key={link.label}>
            <Link href={link.href} className="text-sm text-ink-700 hover:text-site-teal">
              {link.label}
            </Link>
          </li>
        ))}
      </ul>
    </nav>
  );
}
