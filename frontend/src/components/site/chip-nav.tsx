import { Children, isValidElement, type ReactNode } from 'react';
import { SiteContainer } from '@/components/layout/site-container';

export type ChipNavLayout = 'bar' | 'inline';

interface Props {
  /** Says what the links are for. Shown before the chips in a bar; read out by screen readers in both. */
  label: string;
  /**
   * `bar`: a full-width strip under a banner, with the label shown and the chips in one row that
   * scrolls sideways on a phone. `inline`: chips that wrap, inside the page's column.
   */
  layout?: ChipNavLayout;
  /** Layout only, such as a margin. */
  className?: string;
  /** The chips, usually `DotChip`s. Each is put in its own list item. */
  children: ReactNode;
}

/** A labelled row of chips that jump down the page. */
export function ChipNav({ label, layout = 'bar', className = '', children }: Props) {
  const items = Children.toArray(children).map((child, index) => (
    <li key={isValidElement(child) && child.key !== null ? child.key : index} className="flex">
      {child}
    </li>
  ));

  if (layout === 'inline') {
    return (
      <nav aria-label={label} className={className}>
        <ul className="flex flex-wrap gap-2">{items}</ul>
      </nav>
    );
  }

  return (
    <nav aria-label={label} className={`border-b border-ink-100 bg-white ${className}`}>
      <SiteContainer className="flex items-center gap-3 py-3">
        <p aria-hidden="true" className="shrink-0 text-sm text-ink-600">
          {label}
        </p>
        {/* On a phone the row runs on past the screen's right edge and scrolls; the page itself never does. */}
        <ul className="-mr-5 flex min-w-0 gap-2 overflow-x-auto py-0.5 pr-5 [scrollbar-width:thin] sm:mr-0 sm:pr-0">
          {items}
        </ul>
      </SiteContainer>
    </nav>
  );
}
