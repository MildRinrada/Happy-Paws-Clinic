import Link from 'next/link';

/** "← back to the list" above a page title. */
export function BackLink({ href, children }: { href: string; children: string }) {
  return (
    <Link href={href} className="inline-flex items-center gap-1 text-sm text-ink-600 hover:text-brand-700">
      <span aria-hidden>←</span>
      {children}
    </Link>
  );
}
