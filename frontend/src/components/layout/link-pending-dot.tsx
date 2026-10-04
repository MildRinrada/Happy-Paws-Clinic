'use client';

import { useLinkStatus } from 'next/link';

/**
 * A small spinner beside a link's label from the click until the next page starts to show.
 * Must sit inside the `<Link>` it reports on.
 */
export function LinkPendingDot() {
  const { pending } = useLinkStatus();

  return (
    <span
      aria-hidden
      className={`ml-2 inline-block size-3 rounded-full border-2 border-current border-r-transparent align-[-1px] ${
        pending ? 'animate-spin opacity-70' : 'opacity-0'
      }`}
    />
  );
}
