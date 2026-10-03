import type { ReactNode } from 'react';

/** A section's title. One component, so every section's heading is the same size and weight. */
export function SectionHeading({ id, children }: { id: string; children: ReactNode }) {
  return (
    <h2 id={id} className="text-2xl font-semibold leading-tight sm:text-[30px] sm:leading-tight">
      {children}
    </h2>
  );
}
