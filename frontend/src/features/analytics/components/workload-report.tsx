import { operationsReport } from '../content/operations-report';
import { formatCount, formatMinutes } from '../lib/format-report';
import type { WorkloadStats } from '../types/analytics.types';
import { ReportCard } from './report-card';
import { StatTile } from './stat-tile';
import { TileRow } from './tile-row';
import { WorkloadTable } from './workload-table';

/** Cases per vet, time per case, and the backlog waiting on each of them now. */
export function WorkloadReport({ stats }: { stats: WorkloadStats }) {
  const text = operationsReport.workload;
  const { totals } = stats;

  return (
    <ReportCard title={text.title} note={text.note}>
      <TileRow>
        <StatTile label={text.cases} value={formatCount(totals.cases)} emphasis />
        <StatTile label={text.averageCase} value={formatMinutes(totals.average_case_minutes)} />
        <StatTile label={text.openCases} value={formatCount(totals.open_cases)} />
        <StatTile label={text.unassigned} value={formatCount(totals.unassigned_queue)} />
      </TileRow>
      <WorkloadTable staff={stats.staff} />
    </ReportCard>
  );
}
