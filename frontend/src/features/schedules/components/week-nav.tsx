import Link from 'next/link';
import { buttonClass } from '@/components/ui/button';
import { addDays, dayChipParts } from '@/features/booking/lib/booking-dates';
import { schedulePage } from '../content/schedule-page';
import { WeekJump } from './week-jump';

/** The week's dates, links to the weeks either side and back to this one, and a date box to jump to any week. */
export function WeekNav({ days }: { days: string[] }) {
  const text = schedulePage.week;
  const first = days[0]!;
  const last = days[6]!;
  const label = `${dayChipParts(first).date} - ${dayChipParts(last).date}`;

  return (
    <div className="flex flex-wrap items-center gap-2">
      <Link href={`/schedules?week=${addDays(first, -7)}`} className={buttonClass('secondary', 'sm')}>
        {text.previous}
      </Link>
      <span className="min-w-36 text-center text-sm font-medium text-ink-800">{label}</span>
      <Link href={`/schedules?week=${addDays(first, 7)}`} className={buttonClass('secondary', 'sm')}>
        {text.next}
      </Link>
      <Link href="/schedules" className={buttonClass('ghost', 'sm')}>
        {text.thisWeek}
      </Link>
      <WeekJump monday={first} />
    </div>
  );
}
