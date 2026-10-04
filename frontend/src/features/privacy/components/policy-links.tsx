import Link from 'next/link';
import type { PolicyLink } from '../types/privacy-policy.types';

/** Buttons to the pages where a right can actually be used, such as the portal's account page. */
export function PolicyLinks({ items }: { items: PolicyLink[] }) {
  return (
    <div className="flex flex-wrap gap-3">
      {items.map((item) => (
        <Link
          key={item.href}
          href={item.href}
          className="inline-flex min-h-11 items-center rounded-[10px] bg-site-teal px-5 text-[15px] font-semibold text-white transition hover:brightness-95"
        >
          {item.label}
        </Link>
      ))}
    </div>
  );
}
