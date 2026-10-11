export type DotColor = 'teal' | 'brand' | 'orange' | 'violet' | 'alert' | 'sun';
export type DotChipLook = 'soft' | 'outline';

const dots: Record<DotColor, string> = {
  teal: 'bg-site-teal',
  brand: 'bg-brand-500',
  orange: 'bg-site-orange',
  violet: 'bg-site-violet',
  alert: 'bg-site-alert',
  sun: 'bg-site-sun',
};

const looks: Record<DotChipLook, { size: string; colour: string }> = {
  // Light border, dark green words: the services page's category bar and the team page's departments.
  soft: { size: 'h-10 gap-2 text-sm', colour: 'border-ink-200 text-site-forest' },
  // The site's darker grey border, a little shorter. Not used by a page at the moment.
  outline: { size: 'h-9 gap-1.5 text-sm', colour: 'border-site-border' },
};

/** A chip that is not the one chosen: white, and teal on hover. */
const idle = 'bg-white hover:border-site-teal hover:text-site-teal';

/** The chip for what the page shows now, such as "ทั้งหมด" in a row of filters: filled dark green. */
const chosen = 'border-site-forest bg-site-forest font-medium text-white';

interface Props {
  href: string;
  label: string;
  /** A coloured dot before the label. Without it, the chip is the label alone. */
  dot?: DotColor;
  /** A number after the label, such as how many people or services. */
  count?: number;
  look?: DotChipLook;
  /** The chip for what the page is showing now. Filled dark green, and read out as the current page. */
  current?: boolean;
  /** Layout only. */
  className?: string;
}

/**
 * A rounded link that jumps to a part of the page, or filters it, optionally marked with a
 * coloured dot.
 */
export function DotChip({ href, label, dot, count, look = 'soft', current = false, className = '' }: Props) {
  const { size, colour } = looks[look];

  return (
    <a
      href={href}
      aria-current={current ? 'page' : undefined}
      className={`flex shrink-0 items-center whitespace-nowrap rounded-full border px-4 transition ${size} ${current ? chosen : `${colour} ${idle}`} ${className}`}
    >
      {dot && <span aria-hidden="true" className={`size-2 shrink-0 rounded-full ${dots[dot]}`} />}
      {label}
      {count !== undefined && <span className={current ? 'text-white/75' : 'text-site-muted'}>{count}</span>}
    </a>
  );
}
