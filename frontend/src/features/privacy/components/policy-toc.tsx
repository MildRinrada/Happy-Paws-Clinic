import { tocLabel } from '../content/privacy-policy';
import type { PolicyLink } from '../types/privacy-policy.types';

/** The list of sections, each a jump link to its heading. Sticky beside the text on a wide screen. */
export function PolicyToc({ items }: { items: PolicyLink[] }) {
  return (
    <nav aria-label={tocLabel} className="rounded-2xl border border-site-border/60 p-5 lg:sticky lg:top-6">
      <p className="text-sm font-semibold text-site-muted">{tocLabel}</p>
      <ol className="mt-3 space-y-2 text-[15px]">
        {items.map((item, index) => (
          <li key={item.href} className="flex gap-2">
            <span className="text-site-muted">{index + 1}.</span>
            <a href={item.href} className="min-w-0 text-site-teal hover:underline">
              {item.label}
            </a>
          </li>
        ))}
      </ol>
    </nav>
  );
}
