import Link from 'next/link';
import type { ReactNode } from 'react';

type Variant = 'solid' | 'violet' | 'glass' | 'dark';
type Size = 'md' | 'lg';

const variants: Record<Variant, string> = {
  solid: 'bg-site-orange font-bold text-white hover:brightness-95',
  // The primary button where the section itself is orange.
  violet: 'bg-site-violet font-bold text-white hover:brightness-95',
  // For use on a coloured section: a see-through button that lets the section's colour show.
  glass: 'border border-white/70 bg-white/20 text-white hover:bg-white/30',
  dark: 'bg-black font-medium text-white hover:bg-ink-800',
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

/** The public site's call-to-action: a link drawn as a button. */
export function CtaLink({ href, variant = 'solid', size = 'md', className = '', ariaLabel, children }: Props) {
  return (
    <Link
      href={href}
      aria-label={ariaLabel}
      className={`inline-flex items-center justify-center whitespace-nowrap transition ${variants[variant]} ${sizes[size]} ${className}`}
    >
      {children}
    </Link>
  );
}
