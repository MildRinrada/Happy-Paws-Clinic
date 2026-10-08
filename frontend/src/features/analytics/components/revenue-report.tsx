import { formatBaht } from '@/lib/format/money';
import { businessReport } from '../content/business-report';
import { formatCount, formatPeriod } from '../lib/format-report';
import type { RevenueAnalytics } from '../types/analytics.types';
import { BarChart } from './bar-chart';
import { MoneyBreakdownTable } from './money-breakdown-table';
import { ReportCard } from './report-card';
import { StatTile } from './stat-tile';
import { TileRow } from './tile-row';

/** Money taken less money refunded, by period, tender and branch. */
export function RevenueReport({ revenue }: { revenue: RevenueAnalytics }) {
  const text = businessReport.revenue;
  const { totals } = revenue;

  return (
    <ReportCard title={text.title} note={text.basis}>
      <TileRow>
        <StatTile label={text.net} value={formatBaht(totals.net_satang)} emphasis />
        <StatTile label={text.collected} value={formatBaht(totals.collected_satang)} hint={formatCount(totals.payments)} />
        <StatTile label={text.refunded} value={formatBaht(totals.refunded_satang)} hint={formatCount(totals.refunds)} />
        <StatTile
          label={text.invoiced}
          value={formatBaht(totals.invoiced_satang)}
          hint={text.invoicedHint(formatCount(totals.invoices))}
        />
      </TileRow>

      <BarChart
        label={text.chartLabel}
        points={revenue.series.map((period) => ({
          key: period.period,
          label: formatPeriod(period.period, revenue.granularity),
          value: period.net_satang,
          display: formatBaht(period.net_satang),
        }))}
      />

      <div className="grid gap-4 lg:grid-cols-2">
        <MoneyBreakdownTable
          heading={text.byMethod}
          nameColumn={text.columns.name}
          rows={revenue.by_method.map((row) => ({ ...row, key: row.method, name: text.methods[row.method] ?? row.method }))}
        />
        <MoneyBreakdownTable
          heading={text.byBranch}
          nameColumn={text.columns.branch}
          rows={revenue.by_branch.map((row) => ({ ...row, key: row.branch_id, name: row.branch_name ?? row.branch_id }))}
        />
      </div>
    </ReportCard>
  );
}
