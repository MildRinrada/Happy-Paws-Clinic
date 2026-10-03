import Link from 'next/link';
import { signOut } from '@/features/authentication/actions/sign-in';
import { portalHeader } from '@/features/portal/content/portal-navigation';

const item = 'block w-full rounded-lg px-3 py-2.5 text-left text-sm text-ink-800 hover:bg-ink-50';

/**
 * The owner's name, opening a small menu: back to the clinic's site, and sign out.
 *
 * A <details> element, so it opens and closes with no script, like the public site's phone menu.
 */
export function AccountMenu({ displayName }: { displayName: string }) {
  const initial = displayName.trim().charAt(0) || '?';

  return (
    <details className="relative">
      <summary
        aria-label={portalHeader.accountMenuLabel}
        className="flex cursor-pointer list-none items-center gap-2 rounded-full py-1 pl-1 pr-2 hover:bg-ink-100 [&::-webkit-details-marker]:hidden"
      >
        <span className="flex size-8 items-center justify-center rounded-full bg-site-teal text-sm font-bold text-white">
          {initial}
        </span>
        <span className="hidden max-w-[140px] truncate text-sm text-ink-800 sm:block">{displayName}</span>
      </summary>

      <div className="absolute right-0 top-full z-30 mt-2 w-56 rounded-xl border border-ink-200 bg-white p-2 shadow-lg">
        <p className="truncate px-3 py-2 text-sm font-semibold text-ink-900">{displayName}</p>
        <ul>
          {portalHeader.accountLinks.map((link) => (
            <li key={link.href}>
              <Link href={link.href} className={item}>
                {link.label}
              </Link>
            </li>
          ))}
          <li>
            <form action={signOut}>
              <button type="submit" className={item}>
                {portalHeader.signOut}
              </button>
            </form>
          </li>
        </ul>
      </div>
    </details>
  );
}
