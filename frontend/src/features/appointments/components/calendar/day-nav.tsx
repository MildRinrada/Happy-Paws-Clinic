import Link from 'next/link';
import { buttonClass } from '@/components/ui/button';
import { appointmentCalendar } from '../../content/appointment-calendar';
import { appointmentPaths } from '../../lib/appointment-paths';
import { nextDay, previousDay } from '../../lib/calendar-day';
import { DateJump } from './date-jump';

/** Back a day, today, forward a day, or straight to a date. The day lives in the address. */
export function DayNav({ day, today }: { day: string; today: string }) {
  const text = appointmentCalendar.nav;

  return (
    <nav aria-label={text.pick} className="flex flex-wrap items-center gap-2">
      <Link href={appointmentPaths.calendar(previousDay(day))} className={buttonClass('secondary', 'sm')} aria-label={text.previous}>
        <span aria-hidden>‹</span>
      </Link>
      <Link
        href={appointmentPaths.calendar(today)}
        aria-current={day === today ? 'date' : undefined}
        className={buttonClass(day === today ? 'primary' : 'secondary', 'sm')}
      >
        {text.today}
      </Link>
      <Link href={appointmentPaths.calendar(nextDay(day))} className={buttonClass('secondary', 'sm')} aria-label={text.next}>
        <span aria-hidden>›</span>
      </Link>
      <DateJump day={day} />
    </nav>
  );
}
