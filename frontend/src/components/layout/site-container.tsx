import type { ReactNode } from 'react';

/**
 * The column the public site's content sits in, so every section and the footer share the same
 * left and right edges. The site's overall width is this one `max-w-…` value.
 */
export function SiteContainer({ children, className = '' }: { children: ReactNode; className?: string }) {
  return <div className={`mx-auto w-full max-w-[1040px] px-5 sm:px-6 ${className}`}>{children}</div>;
}
