import { operationsReport } from '../content/operations-report';
import { formatCount, formatPercent, formatPeriod } from '../lib/format-report';
import type { AppointmentStats } from '../types/analytics.types';
import { BarChart } from './bar-chart';
import { ReportCard } from './report-card';
import { StatTile } from './stat-tile';
import { TileRow } from './tile-row';

/** Appointment volume and the two rates, with appointments per period as bars. */
export function AppointmentStatsReport({ stats }: { stats: AppointmentStats }) {
  const text = operationsReport.appointments;
  const { totals } = stats;

  return (
    <ReportCard title={text.title}>
      <TileRow>
        <StatTile label={text.total} value={formatCount(totals.total)} emphasis />
        <StatTile label={text.completed} value={formatCount(totals.completed)} />
        <StatTile
          label={text.cancellationRate}
          value={formatPercent(totals.cancellation_rate)}
          hint={`${text.cancelled} ${formatCount(totals.cancelled)}`}
        />
        <StatTile
          label={text.noShowRate}
          value={formatPercent(totals.no_show_rate)}
          hint={`${text.noShow} ${formatCount(totals.no_show)} (${text.noShowRateHint})`}
        />
        <StatTile label={text.walkIn} value={formatCount(totals.walk_in)} />
        <StatTile label={text.bookedOnline} value={formatCount(totals.booked_online)} />
      </TileRow>

      <BarChart
        label={text.chartLabel}
        points={stats.series.map((period) => ({
          key: period.period,
          label: formatPeriod(period.period, stats.granularity),
          value: period.total,
          display: formatCount(period.total),
        }))}
      />
    </ReportCard>
  );
}
