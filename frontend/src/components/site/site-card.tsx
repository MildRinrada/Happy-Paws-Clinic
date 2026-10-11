import Link from 'next/link';
import type { AriaRole, ReactNode } from 'react';

export type SiteCardTone =
  | 'muted'
  | 'white'
  | 'whiteSoft'
  | 'whiteFaint'
  | 'raised'
  | 'notice'
  | 'forest'
  | 'orange'
  | 'line'
  | 'alert';
export type SiteCardPadding = 'none' | 'sm' | 'md' | 'lg';
export type SiteCardRadius = 'md' | 'lg' | 'xl';

const tones: Record<SiteCardTone, string> = {
  // Light grey, no border: the services page's cards, the reviews, and the review link's messages.
  muted: 'bg-site-soft',
  // White with the site's grey border: service, vet, article and vaccine schedule cards.
  white: 'border border-site-border bg-white',
  // White with a lighter border: the about, contact, emergency and privacy pages' boxes.
  whiteSoft: 'border border-site-border/60 bg-white',
  // White with the pale warm grey border of the filter chips: the reviews page's cards.
  whiteFaint: 'border border-ink-200 bg-white',
  // White with a shadow and no border, standing out on a coloured band: the home hero's "ว่างเร็วสุด"
  // card and the emergency page's numbers.
  raised: 'bg-white shadow-lg',
  // Pale orange: a note to read before acting, such as a medical disclaimer.
  notice: 'border border-site-orange/60 bg-[#fff6e8]',
  // The solid colours, with white words.
  forest: 'bg-site-forest text-white',
  orange: 'bg-site-orange text-white',
  // LINE's own green, for a card that leads to the clinic's LINE account.
  line: 'bg-line-green text-white',
  // The emergency red.
  alert: 'bg-site-alert text-white',
};

const paddings: Record<SiteCardPadding, string> = {
  // For a card whose picture runs to its edges, or whose padding is its own; the content sets it.
  none: '',
  sm: 'p-4 sm:p-5',
  md: 'p-5',
  lg: 'p-5 sm:p-6',
};

const radii: Record<SiteCardRadius, string> = {
  md: 'rounded-[14px]',
  lg: 'rounded-2xl',
  // The large split card at the top of the articles page.
  xl: 'rounded-[20px]',
};

interface Props {
  /** The element the card is. `li` when it sits in a list of cards, `dl` when it is a list of terms. */
  as?: 'div' | 'li' | 'article' | 'section' | 'aside' | 'figure' | 'nav' | 'dl' | 'details';
  /** Makes the whole card a link, which picks up a teal border on hover. */
  href?: string;
  tone?: SiteCardTone;
  padding?: SiteCardPadding;
  radius?: SiteCardRadius;
  id?: string;
  /** Such as `note`, for a card that is a note beside the main text. */
  role?: AriaRole;
  ariaLabel?: string;
  ariaLabelledBy?: string;
  /** Layout only: a column, a gap, a margin, or the padding of a card with `padding="none"`. */
  className?: string;
  children: ReactNode;
}

/** The public site's card: one rounded surface, in one of a few tones. */
export function SiteCard({
  as: Element = 'div',
  href,
  tone = 'white',
  padding = 'md',
  radius = 'lg',
  id,
  role,
  ariaLabel,
  ariaLabelledBy,
  className = '',
  children,
}: Props) {
  const look = `${radii[radius]} ${tones[tone]} ${paddings[padding]} ${className}`;

  if (href) {
    return (
      <Link
        href={href}
        id={id}
        aria-label={ariaLabel}
        aria-labelledby={ariaLabelledBy}
        className={`block transition hover:border-site-teal ${look}`}
      >
        {children}
      </Link>
    );
  }

  return (
    <Element id={id} role={role} aria-label={ariaLabel} aria-labelledby={ariaLabelledBy} className={look}>
      {children}
    </Element>
  );
}
