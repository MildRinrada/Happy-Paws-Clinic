import { formatBaht } from '@/lib/format/money';
import { headlineTiles } from '../../content/headline-tiles';
import { formatCount, formatMinutes } from '../../lib/format-report';
import type { TodayLinks } from '../../lib/today-links';
import type { TodayDashboard } from '../../types/analytics.types';
import type { LastWeek } from '../../types/today.types';
import { HeadlineTile } from './headline-tile';

interface HeadlineTilesProps {
  today: TodayDashboard;
  lastWeek: LastWeek;
  links: TodayLinks;
}

/**
 * The day in four numbers, each a link to its page. The takings are a tile only when the API sent
 * them, which it does for a session that may see money; the page does not second-guess that.
 *
 * Appointments are compared with the same weekday last week, whole day against whole day. The
 * takings are not: today's are still coming in, so last week's whole day is given as a reference
 * line instead of a percentage that would read as a fall every morning.
 */
export function HeadlineTiles({ today, lastWeek, links }: HeadlineTilesProps) {
  const text = headlineTiles;
  const { appointments, queue, revenue } = today;

  const appointmentHint = (
    <>
      {lastWeek.appointments !== undefined && (
        <span className="block font-medium text-ink-800">{text.vsLastWeek(appointments.total - lastWeek.appointments)}</span>
      )}
      <span className="block">{text.upcoming(formatCount(appointments.upcoming))}</span>
    </>
  );

  return (
    <div className={`grid grid-cols-2 gap-3 ${revenue ? 'lg:grid-cols-4' : 'lg:grid-cols-3'}`}>
      <HeadlineTile
        label={text.appointments}
        value={formatCount(appointments.total)}
        hint={appointmentHint}
        href={links.calendar}
      />
      <HeadlineTile
        label={text.waiting}
        value={formatCount(queue.waiting)}
        hint={queue.longest_wait_minutes != null ? text.longest(formatMinutes(queue.longest_wait_minutes)) : undefined}
        href={links.queue}
      />
      <HeadlineTile
        label={text.inRoom}
        value={formatCount(queue.in_room)}
        hint={text.served(formatCount(queue.served))}
        href={links.queue}
      />
      {revenue && (
        <HeadlineTile
          label={text.revenue}
          value={formatBaht(revenue.net_satang)}
          hint={
            lastWeek.revenueSatang !== undefined
              ? text.lastWeekRevenue(formatBaht(lastWeek.revenueSatang))
              : text.payments(formatCount(revenue.payments))
          }
          href={links.revenue}
        />
      )}
    </div>
  );
}
