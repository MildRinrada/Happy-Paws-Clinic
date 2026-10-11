import Link from 'next/link';
import type { ReactNode } from 'react';

interface HeadlineTileProps {
  label: string;
  value: ReactNode;
  /** A short line under the value: a comparison or a second figure. */
  hint?: ReactNode;
  /** The page the number comes from. Without one, the tile is plain text. */
  href?: string;
}

/**
 * One of the day's main numbers, larger than a report tile, and a link to the page behind it when
 * the session may open that page. The link's name is the label, the number and the hint together.
 */
export function HeadlineTile({ label, value, hint, href }: HeadlineTileProps) {
  const body = (
    <>
      <span className="block text-xs text-ink-500">{label}</span>
      <span className="block text-2xl font-semibold tabular-nums text-ink-900">{value}</span>
      {hint && <span className="mt-0.5 block text-xs text-ink-600">{hint}</span>}
    </>
  );
  const frame = 'block h-full rounded-xl border border-ink-200 bg-white px-4 py-3';

  return href ? (
    <Link href={href} className={`${frame} transition-colors hover:border-brand-300 hover:bg-brand-50/40`}>
      {body}
    </Link>
  ) : (
    <div className={frame}>{body}</div>
  );
}
