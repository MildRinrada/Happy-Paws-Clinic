import type { ReactNode } from 'react';

/** A section's title, a step smaller than the public site's section headings: this page is for reading. */
export function PolicyHeading({ id, children }: { id: string; children: ReactNode }) {
  return (
    <h2 id={id} className="text-xl font-semibold leading-snug text-ink-900 sm:text-2xl">
      {children}
    </h2>
  );
}
