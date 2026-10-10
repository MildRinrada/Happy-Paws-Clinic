import { CtaLink } from '@/components/ui/cta-link';
import { formatTime } from '@/lib/format/datetime';
import { nextFreeTime } from '../../content/next-free-time';
import { dayWord } from '../../lib/day-word';
import { bookTimeHref } from '../../lib/next-free-links';
import type { NextFreeSlot } from '../../types/next-free-time.types';

interface NextFreeRowProps {
  slot: NextFreeSlot;
  serviceId: string;
  serviceName: string;
  now: number;
}

/** One branch's earliest free time, and the button that opens the booking page on it. */
export function NextFreeRow({ slot, serviceId, serviceName, now }: NextFreeRowProps) {
  const day = dayWord(slot.startsAt, now);
  const time = formatTime(slot.startsAt);

  return (
    <li className="flex flex-wrap items-center justify-between gap-x-4 gap-y-3 py-3 first:pt-0 last:pb-0">
      <div className="min-w-0">
        <p className="flex items-center gap-2 text-lg font-bold leading-snug">
          <span aria-hidden="true" className="size-2.5 shrink-0 rounded-full bg-stat-green" />
          {nextFreeTime.earliest(day, time)}
        </p>
        <p className="mt-0.5 pl-[18px] text-sm text-site-muted">
          {slot.branchName ? `${slot.branchName} ` : ''}
          {nextFreeTime.forService(serviceName)}
        </p>
      </div>

      <CtaLink
        href={bookTimeHref(serviceId, slot.branchId, slot.startsAt)}
        ariaLabel={nextFreeTime.bookLabel(day, time, slot.branchName)}
      >
        {nextFreeTime.book}
      </CtaLink>
    </li>
  );
}
