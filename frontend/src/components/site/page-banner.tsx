import { useId, type ReactNode } from 'react';
import { SiteContainer } from '@/components/layout/site-container';
import { BannerImage, type BannerImageData, type BannerImageTone } from './banner-image';

export type PageBannerSize = 'compact' | 'feature' | 'flush' | 'wide' | 'urgent';
export type PageBannerLeadWidth = 'default' | 'roomy';
export type PageBannerTone = 'forest' | 'alert' | 'white' | 'soft';

const sizes: Record<PageBannerSize, { section: string; eyebrow: string; heading: string; lead: string; measure: string }> = {
  // The plain band at the top of the contact, privacy and vaccine pages: words only.
  compact: {
    section: 'py-10 lg:py-12',
    eyebrow: 'text-sm font-semibold uppercase tracking-wide',
    heading: 'mt-2 text-3xl font-semibold leading-tight sm:text-[38px]',
    lead: 'mt-4 text-base font-light leading-relaxed',
    measure: 'max-w-[560px]',
  },
  // The taller band with buttons and a picture at the right, as on the services page.
  feature: {
    section: 'pb-12 pt-10 lg:pb-[62px] lg:pt-[53px]',
    eyebrow: 'text-sm font-semibold',
    heading: 'mt-3 text-[32px] font-semibold leading-[1.2] sm:text-[42px] sm:leading-[48px]',
    lead: 'mt-5 text-base font-light leading-[25px]',
    measure: 'max-w-[500px]',
  },
  // A page's heading straight on the page, with no band behind it, as on the articles page. Less
  // room underneath, since the page's own content follows on the same white.
  flush: {
    section: 'pb-8 pt-10 lg:pt-12',
    eyebrow: 'text-[13px] font-bold',
    heading: 'mt-2 text-[32px] font-bold leading-tight sm:text-[38px] sm:leading-[48px]',
    lead: 'mt-1.5 text-[15px] font-light leading-relaxed',
    measure: 'max-w-[560px]',
  },
  // A band of words only, like `compact`, with a bold heading and room for a longer lead across
  // most of the column, as on the reviews page.
  wide: {
    section: 'pb-12 pt-10 lg:pt-[46px]',
    eyebrow: 'text-sm font-bold',
    heading: 'mt-[11px] text-[32px] font-bold leading-tight sm:text-[40px] sm:leading-[50px]',
    lead: 'mt-[9px] text-[15px] font-light leading-7',
    measure: 'max-w-[680px]',
  },
  // The emergency page's band: like `compact`, a little less room above and below on a phone, the
  // heading kept to a short line, and the lead in regular weight so it reads at a glance. The
  // numbers to ring go under it as `children`.
  urgent: {
    section: 'py-9 lg:py-12',
    eyebrow: 'text-sm font-semibold uppercase tracking-wide',
    heading: 'mt-2 max-w-[720px] text-3xl font-semibold leading-tight sm:text-[38px]',
    lead: 'mt-4 text-base leading-relaxed',
    measure: 'max-w-[600px]',
  },
};

/** A wider lead than the size's own, for a longer paragraph: the privacy and vaccine pages. */
const roomyMeasure = 'max-w-[600px]';

const tones: Record<
  PageBannerTone,
  { section: string; eyebrow: Record<PageBannerSize, string>; lead: string; slot: BannerImageTone }
> = {
  forest: {
    section: 'bg-site-forest text-white',
    eyebrow: {
      compact: 'text-site-orange',
      feature: 'text-site-sun',
      flush: 'text-site-sun',
      wide: 'text-site-sun',
      urgent: 'text-site-orange',
    },
    lead: 'text-white/90',
    slot: 'onDark',
  },
  // The emergency page's red.
  alert: {
    section: 'bg-site-alert text-white',
    eyebrow: {
      compact: 'text-white/85',
      feature: 'text-white/85',
      flush: 'text-white/85',
      wide: 'text-white/85',
      urgent: 'text-white/85',
    },
    lead: 'text-white/95',
    slot: 'onDark',
  },
  // No band: dark green words on the page's white.
  white: {
    section: 'bg-white text-site-forest',
    eyebrow: {
      compact: 'text-brand-700',
      feature: 'text-brand-700',
      flush: 'text-brand-700',
      wide: 'text-brand-700',
      urgent: 'text-brand-700',
    },
    lead: 'text-ink-700',
    slot: 'onLight',
  },
  // A very light grey band with the same dark green words, as on the team page.
  soft: {
    section: 'bg-site-soft text-site-forest',
    eyebrow: {
      compact: 'text-brand-700',
      feature: 'text-brand-700',
      flush: 'text-brand-700',
      wide: 'text-brand-700',
      urgent: 'text-brand-700',
    },
    lead: 'text-ink-700',
    slot: 'onLight',
  },
};

interface Props {
  eyebrow?: string;
  /** The page's one `<h1>`. A line break in a string is kept. */
  heading: ReactNode;
  lead?: ReactNode;
  /** Buttons under the lead, usually `CtaLink`s. */
  actions?: ReactNode;
  /** Anything else under the lead, such as a line on when a policy took effect. */
  children?: ReactNode;
  /** A picture at the right on a wide screen, under the words on a narrow one. */
  image?: BannerImageData;
  /** The four circles over the picture's corner. */
  dots?: boolean;
  /**
   * Something else at the right in place of a picture, such as a search box: level with the
   * lead on a wide screen, under the words on a narrow one. Left out when there is a picture.
   */
  aside?: ReactNode;
  size?: PageBannerSize;
  tone?: PageBannerTone;
  /** `roomy` lets the lead run to 600 px, wider than the size's own. */
  leadWidth?: PageBannerLeadWidth;
  /** Layout only, such as hiding the band when printing. */
  className?: string;
}

/**
 * The top of a public page: eyebrow, heading, lead, buttons, and a picture or a box at the right.
 * A coloured band, a light grey one (`tone="soft"`), or (`tone="white"`) the words alone on the page.
 */
export function PageBanner({
  eyebrow,
  heading,
  lead,
  actions,
  children,
  image,
  dots = false,
  aside,
  size = 'compact',
  tone = 'forest',
  leadWidth = 'default',
  className = '',
}: Props) {
  const headingId = useId();
  const look = sizes[size];
  const colour = tones[tone];
  const measure = leadWidth === 'roomy' ? roomyMeasure : look.measure;
  const grid = image
    ? 'grid items-center gap-10 lg:grid-cols-[minmax(0,1fr)_440px] lg:gap-14'
    : aside
      ? 'grid gap-6 lg:grid-cols-[minmax(0,1fr)_auto] lg:items-end lg:gap-12'
      : '';

  return (
    <section aria-labelledby={headingId} className={`${colour.section} ${className}`}>
      <SiteContainer className={`${look.section} ${grid}`}>
        <div className="min-w-0">
          {eyebrow && <p className={`${look.eyebrow} ${colour.eyebrow[size]}`}>{eyebrow}</p>}
          <h1 id={headingId} className={`whitespace-pre-line ${look.heading}`}>
            {heading}
          </h1>
          {lead && <p className={`${look.lead} ${measure} ${colour.lead}`}>{lead}</p>}
          {children}
          {actions && <div className="mt-7 flex flex-wrap gap-3">{actions}</div>}
        </div>

        {image ? <BannerImage image={image} dots={dots} tone={colour.slot} /> : aside}
      </SiteContainer>
    </section>
  );
}
