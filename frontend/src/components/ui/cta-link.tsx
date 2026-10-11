import Link from 'next/link';
import type { ReactNode } from 'react';

type Variant = 'solid' | 'solidInk' | 'violet' | 'glass' | 'outline' | 'outlineForest' | 'dark' | 'forest';
type Size = 'md' | 'lg';

const variants: Record<Variant, string> = {
  // Orange with white words. Left only in the portal's header; the public site uses `solidInk`.
  solid: 'bg-site-orange font-bold text-white hover:brightness-95',
  // The public site's orange button: dark words, as every page design draws it. Every orange
  // button on the public pages is this one, including the header's.
  solidInk: 'bg-site-orange font-bold text-ink-900 hover:brightness-95',
  // The primary button where the section itself is orange.
  violet: 'bg-site-violet font-bold text-white hover:brightness-95',
  // For use on a coloured section: a see-through button that lets the section's colour show.
  glass: 'border border-white/70 bg-white/20 text-white hover:bg-white/30',
  // The quieter see-through button on the dark green banner.
  outline: 'border border-white/70 bg-white/12 text-white hover:bg-white/20',
  // A quiet button on white: dark green outline and words, as "บทความก่อนหน้านี้".
  outlineForest: 'border border-site-forest bg-white font-semibold text-site-forest hover:bg-site-soft',
  dark: 'bg-black font-medium text-white hover:bg-ink-800',
  forest: 'bg-site-forest font-bold text-white hover:bg-site-forest-raised',
};

const sizes: Record<Size, string> = {
  md: 'h-10 rounded-lg px-3.5 text-sm',
  lg: 'h-[52px] min-w-[132px] rounded-[10px] px-5 text-base',
};

interface Props {
  href: string;
  variant?: Variant;
  size?: Size;
  /** For the one-off adjustment, such as a wider button. */
  className?: string;
  /** What a screen reader says when the visible words need the text around them. */
  ariaLabel?: string;
  children: ReactNode;
}

/**
 * How a `CtaLink` is drawn, for a button-shaped label inside a larger link (a whole card that is
 * the link), where a second link would be a link inside a link, and for a real `<button>` that
 * should look the same, such as a form's submit.
 */
export function ctaLinkLook(variant: Variant = 'solid', size: Size = 'md'): string {
  return `inline-flex items-center justify-center whitespace-nowrap transition ${variants[variant]} ${sizes[size]}`;
}

/** The public site's call-to-action: a link drawn as a button. */
export function CtaLink({ href, variant = 'solid', size = 'md', className = '', ariaLabel, children }: Props) {
  return (
    <Link href={href} aria-label={ariaLabel} className={`${ctaLinkLook(variant, size)} ${className}`}>
      {children}
    </Link>
  );
}
