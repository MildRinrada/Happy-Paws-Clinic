import type { ReactNode } from 'react';
import { MoreLink } from './more-link';

interface SectionCardProps {
  title: string;
  /** "ดูทั้งหมด" and where it goes, at the right of the title. */
  more?: { label: string; href: string };
  /** Something quieter than a link at the right of the title, such as "6 ครั้งล่าสุด" or a date. */
  aside?: ReactNode;
  /** For a link to this section, like the bell's `#notifications`. */
  id?: string;
  /**
   * A line under the title across the whole card, as the home page draws its lists. `'lg'` draws
   * it from 1024px only, for a card the phone design draws without one.
   */
  ruled?: boolean | 'lg';
  /** 16px inside instead of 20px below 1024px, as the phone design draws the pet page's cards. */
  snug?: boolean;
  children: ReactNode;
}

const layouts = {
  plain: { card: 'p-5', head: 'mb-4', body: '' },
  ruled: { card: '', head: 'border-b border-ink-100 px-4 py-3', body: 'px-4' },
  lg: {
    card: 'p-4 lg:p-0',
    head: 'mb-3 lg:mb-0 lg:border-b lg:border-ink-100 lg:px-4 lg:py-3',
    body: 'lg:px-4',
  },
};

/** A white card with a heading: the portal's one building block for a section of a page. */
export function SectionCard({ title, more, aside, id, ruled = false, snug = false, children }: SectionCardProps) {
  const layout = layouts[ruled === 'lg' ? 'lg' : ruled ? 'ruled' : 'plain'];
  const card = snug && !ruled ? 'p-4 lg:p-5' : layout.card;

  return (
    <section id={id} className={`scroll-mt-24 rounded-2xl border border-ink-200 bg-white ${card}`}>
      <div className={`flex items-center justify-between gap-3 ${layout.head}`}>
        <h2 className="font-display text-base font-semibold leading-6 text-ink-900">{title}</h2>
        {more && <MoreLink label={more.label} href={more.href} />}
        {aside && <div className="shrink-0 text-[13px] leading-5 text-ink-600">{aside}</div>}
      </div>
      {layout.body ? <div className={layout.body}>{children}</div> : children}
    </section>
  );
}
