import Link from 'next/link';
import { portalPackages } from '../../content/portal-packages';
import { portalPaths } from '../../content/portal-navigation';

/**
 * The page with no packages, which is every account today: the clinic does not sell packages
 * through the system yet. Says what would be here, and where to ask, instead of a bare "none".
 */
export function PackagesEmpty() {
  const text = portalPackages.empty;

  return (
    <section className="rounded-2xl bg-white px-6 py-10 text-center shadow-sm">
      <span aria-hidden="true" className="mx-auto flex size-14 items-center justify-center rounded-full bg-brand-50 text-site-teal">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" className="size-7">
          <path d="M3 9a2 2 0 0 0 0 4v4a1 1 0 0 0 1 1h16a1 1 0 0 0 1-1v-4a2 2 0 0 0 0-4V5a1 1 0 0 0-1-1H4a1 1 0 0 0-1 1z" />
          <path d="M14 4v14" strokeDasharray="2 2" />
        </svg>
      </span>

      <h2 className="mt-4 text-base font-semibold text-ink-900">{text.title}</h2>
      <p className="mx-auto mt-2 max-w-md text-sm text-ink-700">{text.body}</p>
      <p className="mx-auto mt-2 max-w-md text-sm text-site-muted">{text.ask}</p>

      <div className="mt-5 flex flex-wrap justify-center gap-3">
        <Link
          href={text.contact.href}
          className="inline-flex h-10 items-center rounded-xl border border-ink-200 px-4 text-sm font-semibold text-ink-700 hover:bg-ink-50"
        >
          {text.contact.label}
        </Link>
        <Link
          href={portalPaths.book}
          className="inline-flex h-10 items-center rounded-xl bg-site-orange px-4 text-sm font-bold text-white hover:brightness-95"
        >
          {text.book}
        </Link>
      </div>
    </section>
  );
}
