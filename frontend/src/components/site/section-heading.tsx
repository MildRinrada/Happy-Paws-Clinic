import type { ReactNode } from 'react';

/**
 * A section's title on its own, with no rule under it (for that, `SectionHeader`). The same size
 * and weight on every public page; the colour comes from the section.
 */
export function SectionHeading({ id, children }: { id: string; children: ReactNode }) {
  return (
    <h2 id={id} className="text-2xl font-semibold leading-tight sm:text-[30px] sm:leading-tight">
      {children}
    </h2>
  );
}
