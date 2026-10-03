import Link from 'next/link';
import type { ReactNode } from 'react';

interface SectionCardProps {
  title: string;
  /** "ดูทั้งหมด" and where it goes, at the right of the title. */
  more?: { label: string; href: string };
  /** For a link to this section, like the bell's `#notifications`. */
  id?: string;
  children: ReactNode;
}

/** A white card with a heading: the portal's one building block for a section of a page. */
export function SectionCard({ title, more, id, children }: SectionCardProps) {
  return (
    <section id={id} className="scroll-mt-24 rounded-2xl bg-white p-5 shadow-sm">
      <div className="mb-4 flex items-center justify-between gap-3">
        <h2 className="text-base font-semibold text-ink-900">{title}</h2>
        {more && (
          <Link href={more.href} className="text-sm font-medium text-site-teal hover:underline">
            {more.label}
          </Link>
        )}
      </div>
      {children}
    </section>
  );
}
