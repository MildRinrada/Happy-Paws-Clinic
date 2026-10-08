import { operationsReport } from '../content/operations-report';
import { formatCount, formatHour, formatMinutes, NO_VALUE } from '../lib/format-report';
import type { QueueStats } from '../types/analytics.types';
import { BarChart } from './bar-chart';
import { ReportCard } from './report-card';
import { StatTile } from './stat-tile';
import { TileRow } from './tile-row';

/** The span of hours worth drawing: the busy ones, widened to a normal clinic day. */
function visibleHours(stats: QueueStats) {
  const busy = stats.peak_hours.filter((hour) => hour.tickets > 0).map((hour) => hour.hour);
  const first = Math.min(8, ...busy);
  const last = Math.max(20, ...busy);

  return stats.peak_hours.filter((hour) => hour.hour >= first && hour.hour <= last);
}

/** Waits, the busiest hour and what never finished, with tickets per hour issued as bars. */
export function QueueStatsReport({ stats }: { stats: QueueStats }) {
  const text = operationsReport.queue;
  const { totals } = stats;

  return (
    <ReportCard title={text.title}>
      <TileRow>
        <StatTile label={text.tickets} value={formatCount(totals.tickets)} emphasis />
        <StatTile label={text.served} value={formatCount(totals.served)} />
        <StatTile label={text.averageWait} value={formatMinutes(totals.average_wait_minutes)} />
        <StatTile label={text.averageRoom} value={formatMinutes(totals.average_room_minutes)} />
        <StatTile
          label={text.peakHour}
          value={totals.peak_hour == null ? NO_VALUE : formatHour(totals.peak_hour)}
        />
        <StatTile label={text.outstanding} value={formatCount(totals.outstanding)} hint={text.outstandingHint} />
      </TileRow>

      <BarChart
        label={text.chartLabel}
        points={visibleHours(stats).map((hour) => ({
          key: String(hour.hour),
          label: formatHour(hour.hour),
          value: hour.tickets,
          display: `${formatCount(hour.tickets)} (${text.averageWait} ${formatMinutes(hour.average_wait_minutes)})`,
        }))}
      />
    </ReportCard>
  );
}
