import Link from 'next/link';
import { nextFreeTime } from '../../content/next-free-time';
import { bookServiceHref } from '../../lib/next-free-links';

/** Nothing free today or tomorrow: say so, and send the visitor to pick another day. */
export function NextFreeFull({ serviceId }: { serviceId: string }) {
  return (
    <p className="flex flex-wrap items-center gap-x-3 gap-y-1 text-[15px]">
      <span className="text-site-muted">{nextFreeTime.full}</span>
      <Link href={bookServiceHref(serviceId)} className="font-semibold text-site-teal hover:underline">
        {nextFreeTime.seeAll}
      </Link>
    </p>
  );
}
