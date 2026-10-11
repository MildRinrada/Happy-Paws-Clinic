import { useId, type ReactNode } from 'react';
import { SiteContainer } from '@/components/layout/site-container';

export type CtaBandVariant = 'band' | 'slim' | 'inset' | 'muted';

const variants: Record<CtaBandVariant, { section: string; box: string; heading: string; lead: string; actions: string }> = {
  // Full width, white words: the close of the home and about pages.
  band: {
    section: 'bg-site-orange pb-14 pt-9 text-white',
    box: 'gap-x-10 gap-y-7',
    heading: 'text-2xl font-semibold leading-tight sm:text-[30px] sm:leading-tight',
    lead: 'mt-4 text-[15px] font-semibold',
    actions: 'gap-4',
  },
  // The same full-width orange, lower, with a smaller heading: the close of the contact page.
  slim: {
    section: 'bg-site-orange py-9 text-white',
    box: 'gap-6',
    heading: 'text-2xl font-semibold',
    lead: 'mt-2 text-[15px] font-semibold',
    actions: 'gap-4',
  },
  // A rounded orange box inside the page's column, dark words: the close of the services page.
  inset: {
    section: '',
    box: 'gap-x-8 gap-y-5 rounded-[20px] bg-site-orange px-6 py-7 text-ink-900 sm:px-[30px]',
    heading: 'text-[21px] font-bold leading-[30px] sm:text-[23px]',
    lead: 'mt-1.5 text-sm text-ink-800',
    actions: 'gap-3',
  },
  // Full width light grey, dark words: the close of the articles page.
  muted: {
    section: 'bg-site-soft pb-9 pt-10 text-site-forest',
    box: 'gap-x-10 gap-y-6',
    heading: 'text-[22px] font-semibold leading-tight sm:text-[23px]',
    lead: 'mt-1.5 text-[15px] font-light text-ink-700',
    actions: 'gap-3',
  },
};

interface Props {
  heading: ReactNode;
  lead?: ReactNode;
  /** The buttons at the right, usually `CtaLink`s. */
  actions: ReactNode;
  variant?: CtaBandVariant;
  /** Layout only, such as the space around an inset box. */
  className?: string;
  /** Layout only, for the row of buttons, such as a wider gap on a wide screen. */
  actionsClassName?: string;
}

/** The call to action that closes a public page: a heading, a line, and buttons, on orange or grey. */
export function CtaBand({ heading, lead, actions, variant = 'band', className = '', actionsClassName = '' }: Props) {
  const headingId = useId();
  const look = variants[variant];
  const row = `flex flex-wrap items-center justify-between ${look.box}`;

  const content = (
    <>
      <div>
        <h2 id={headingId} className={look.heading}>
          {heading}
        </h2>
        {lead && <p className={look.lead}>{lead}</p>}
      </div>
      <div className={`flex flex-wrap ${look.actions} ${actionsClassName}`}>{actions}</div>
    </>
  );

  return (
    <section aria-labelledby={headingId} className={`${look.section} ${className}`}>
      {variant === 'inset' ? (
        <SiteContainer>
          <div className={row}>{content}</div>
        </SiteContainer>
      ) : (
        <SiteContainer className={row}>{content}</SiteContainer>
      )}
    </section>
  );
}
