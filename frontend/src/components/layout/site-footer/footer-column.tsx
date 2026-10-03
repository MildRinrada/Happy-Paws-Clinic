import type { ReactNode } from 'react';

interface Props {
  /** What sits at the top of the column: a title, or the logo in the first one. */
  heading: ReactNode;
  children: ReactNode;
}

/**
 * One column of the footer. Every column's heading gets the same height, so the columns' contents
 * start on one line whether the heading is a word or the logo.
 */
export function FooterColumn({ heading, children }: Props) {
  return (
    <div>
      <div className="flex h-10 items-center">{heading}</div>
      <div className="mt-8">{children}</div>
    </div>
  );
}

/** A column's title, for the columns that have a word rather than the logo. */
export function FooterColumnTitle({ children }: { children: ReactNode }) {
  return <h2 className="text-[15px] font-bold leading-none">{children}</h2>;
}
