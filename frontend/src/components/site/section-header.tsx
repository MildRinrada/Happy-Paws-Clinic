import type { ReactNode } from 'react';

interface Props {
  /** The heading's id, for the section's `aria-labelledby`. */
  id: string;
  title: ReactNode;
  /** One line under the title. */
  blurb?: ReactNode;
  /** A short note at the right, level with the blurb, such as "3 บริการ". */
  meta?: ReactNode;
  /** Layout only, such as a margin. */
  className?: string;
}

/** A section's title, a line on what it covers, a note at the right, and a dark rule underneath. */
export function SectionHeader({ id, title, blurb, meta, className = '' }: Props) {
  return (
    <div className={`flex items-end justify-between gap-x-6 gap-y-1 border-b-2 border-site-forest pb-3 ${className}`}>
      <div className="min-w-0">
        <h2 id={id} className="text-2xl font-bold leading-tight text-site-forest sm:text-[26px]">
          {title}
        </h2>
        {blurb && <p className="mt-1 text-sm text-site-muted">{blurb}</p>}
      </div>
      {meta && <div className="shrink-0 text-sm text-ink-600">{meta}</div>}
    </div>
  );
}
