import { formatTime } from '@/lib/format/datetime';
import { formatBaht } from '@/lib/format/money';
import { todayCards } from '../content/today-cards';
import { formatCount, formatMinutes } from '../lib/format-report';
import type { TodayDashboard } from '../types/analytics.types';
import { StatGroup } from './stat-group';
import { StatTile } from './stat-tile';

/**
 * Today's cards. The takings and "my work" cards come and go with the response: the API adds the
 * money only for a session that may already see it, and the page does not second-guess that.
 */
export function TodayOverview({ today }: { today: TodayDashboard }) {
  const text = todayCards;
  const { appointments, queue, consultations, revenue, mine } = today;

  return (
    <section aria-labelledby="today-heading" className="space-y-3">
      <div className="flex items-baseline justify-between gap-3">
        <h2 id="today-heading" className="font-display text-lg font-semibold text-ink-900">
          {text.heading}
        </h2>
        <p className="text-xs text-ink-500">{text.updatedAt(formatTime(today.generated_at))}</p>
      </div>

      <div className="grid gap-3 lg:grid-cols-2">
        <StatGroup title={text.appointments.title}>
          <StatTile label={text.appointments.total} value={formatCount(appointments.total)} emphasis />
          <StatTile label={text.appointments.upcoming} value={formatCount(appointments.upcoming)} />
          <StatTile label={text.appointments.arrived} value={formatCount(appointments.arrived)} />
          <StatTile label={text.appointments.completed} value={formatCount(appointments.completed)} />
          <StatTile label={text.appointments.cancelled} value={formatCount(appointments.cancelled)} />
          <StatTile label={text.appointments.noShow} value={formatCount(appointments.no_show)} />
        </StatGroup>

        <StatGroup title={text.queue.title}>
          <StatTile label={text.queue.waiting} value={formatCount(queue.waiting)} emphasis />
          <StatTile label={text.queue.inRoom} value={formatCount(queue.in_room)} />
          <StatTile label={text.queue.served} value={formatCount(queue.served)} />
          <StatTile label={text.queue.issued} value={formatCount(queue.issued)} />
          <StatTile label={text.queue.averageWait} value={formatMinutes(queue.average_wait_minutes)} />
          <StatTile label={text.queue.longestWait} value={formatMinutes(queue.longest_wait_minutes)} />
        </StatGroup>

        <StatGroup title={text.consultations.title}>
          <StatTile label={text.consultations.open} value={formatCount(consultations.open)} emphasis />
          <StatTile label={text.consultations.closedToday} value={formatCount(consultations.closed_today)} />
        </StatGroup>

        {revenue && (
          <StatGroup title={text.revenue.title}>
            <StatTile label={text.revenue.net} value={formatBaht(revenue.net_satang)} emphasis />
            <StatTile label={text.revenue.collected} value={formatBaht(revenue.collected_satang)} />
            <StatTile label={text.revenue.refunded} value={formatBaht(revenue.refunded_satang)} />
            <StatTile label={text.revenue.payments} value={formatCount(revenue.payments)} />
          </StatGroup>
        )}

        {mine && (
          <StatGroup title={text.mine.title}>
            <StatTile label={text.mine.appointments} value={formatCount(mine.appointments)} />
            <StatTile label={text.mine.queueWaiting} value={formatCount(mine.queue_waiting)} />
            <StatTile label={text.mine.openConsultations} value={formatCount(mine.open_consultations)} />
          </StatGroup>
        )}
      </div>
    </section>
  );
}
