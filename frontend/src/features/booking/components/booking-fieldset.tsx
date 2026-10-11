import type { ReactNode } from 'react';

/** A labelled group of choices. The legend is always visible, as every label in the portal is. */
export function BookingFieldset({ legend, children }: { legend: string; children: ReactNode }) {
  return (
    <fieldset className="space-y-2">
      <legend className="mb-2.5 text-[15px] font-semibold text-site-forest">{legend}</legend>
      {children}
    </fieldset>
  );
}
