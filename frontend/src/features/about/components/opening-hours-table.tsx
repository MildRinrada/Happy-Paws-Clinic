import { SiteCard } from '@/components/site/site-card';
import { WEEKDAY_LABELS } from '@/features/clinic/lib/weekdays';
import type { DayHours, Weekday } from '@/features/clinic/types/clinic.types';
import { aboutVisit } from '../content/about-page';

interface OpeningHoursTableProps {
  days: DayHours[];
  /** Today's row is marked, so "are you open now" is answered at a glance. */
  today: Weekday;
}

/** The week, Monday first, one row per day. */
export function OpeningHoursTable({ days, today }: OpeningHoursTableProps) {
  return (
    <SiteCard as="dl" tone="whiteSoft" padding="none" className="divide-y divide-site-border/50 overflow-hidden">
      {days.map((day) => {
        const isToday = day.day === today;
        const hours =
          day.closed || !day.opens || !day.closes ? aboutVisit.closed : aboutVisit.hoursRange(day.opens, day.closes);

        return (
          <div
            key={day.day}
            className={`flex items-center justify-between gap-4 px-5 py-3 ${isToday ? 'bg-site-teal/10' : ''}`}
          >
            <dt className={isToday ? 'font-semibold text-ink-900' : 'text-site-muted'}>
              {WEEKDAY_LABELS[day.day]}
              {isToday && (
                <span className="ml-2 rounded-full bg-site-teal px-2 py-0.5 text-xs font-semibold text-white">
                  {aboutVisit.today}
                </span>
              )}
            </dt>
            <dd className={`tabular-nums ${day.closed ? 'text-site-alert' : isToday ? 'font-semibold text-ink-900' : 'text-ink-800'}`}>
              {hours}
            </dd>
          </div>
        );
      })}
    </SiteCard>
  );
}
