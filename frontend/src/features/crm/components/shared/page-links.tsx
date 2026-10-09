import Link from 'next/link';

interface PageLinksProps {
  /** The next page, or absent on the last one. */
  nextHref?: string;
  /** Back to the first page, shown once the list has been paged. */
  firstHref?: string;
  nextLabel: string;
  firstLabel: string;
}

/** "Next page" and "back to the first" under a cursor-paged list. Draws nothing on a single page. */
export function PageLinks({ nextHref, firstHref, nextLabel, firstLabel }: PageLinksProps) {
  if (!nextHref && !firstHref) {
    return null;
  }

  return (
    <div className="flex items-center justify-end gap-4 text-sm">
      {firstHref && (
        <Link href={firstHref} className="text-ink-600 hover:underline">
          {firstLabel}
        </Link>
      )}
      {nextHref && (
        <Link href={nextHref} className="text-brand-700 hover:underline">
          {nextLabel}
        </Link>
      )}
    </div>
  );
}
