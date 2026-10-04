import type { ReactNode } from 'react';
import { PolicyHeading } from './policy-heading';

interface PolicySectionProps {
  id: string;
  heading: string;
  children: ReactNode;
}

/** One numbered part of the policy: its heading, and the blocks under it. */
export function PolicySection({ id, heading, children }: PolicySectionProps) {
  return (
    <section aria-labelledby={`${id}-heading`} id={id} className="scroll-mt-6">
      <PolicyHeading id={`${id}-heading`}>{heading}</PolicyHeading>
      <div className="mt-4 space-y-4">{children}</div>
    </section>
  );
}
