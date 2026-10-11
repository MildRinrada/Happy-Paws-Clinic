import { todayCards } from '../../content/today-cards';
import { formatCount } from '../../lib/format-report';
import type { TodayLinks } from '../../lib/today-links';
import type { TodayDashboard } from '../../types/analytics.types';
import { HeadlineTile } from './headline-tile';

/**
 * "งานของฉัน", first among the day's numbers: the caller's own appointments, the tickets assigned
 * to them, and the cases they have open. The API sends `mine` to every member of staff, so the
 * strip is drawn only when one of the three is above zero. That is what tells a vet's front page
 * from a counter's without the page asking for a role name: whoever has work of their own today
 * sees it first. Open cases have no list page of their own, so that tile is not a link.
 */
export function MineStrip({ mine, links }: { mine: NonNullable<TodayDashboard['mine']>; links: TodayLinks }) {
  const text = todayCards.mine;

  if (mine.appointments + mine.queue_waiting + mine.open_consultations === 0) {
    return null;
  }

  return (
    <section aria-labelledby="mine-heading" className="space-y-2">
      <h3 id="mine-heading" className="text-sm font-semibold text-ink-900">
        {text.title}
      </h3>
      <div className="grid grid-cols-3 gap-3">
        <HeadlineTile label={text.appointments} value={formatCount(mine.appointments)} href={links.calendar} />
        <HeadlineTile label={text.queueWaiting} value={formatCount(mine.queue_waiting)} href={links.queue} />
        <HeadlineTile label={text.openConsultations} value={formatCount(mine.open_consultations)} />
      </div>
    </section>
  );
}
