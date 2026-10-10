import type { ReactNode } from 'react';

/** One paragraph of the policy. */
export function PolicyParagraph({ children }: { children: ReactNode }) {
  return <p className="break-words text-[15px] leading-relaxed text-ink-700">{children}</p>;
}
