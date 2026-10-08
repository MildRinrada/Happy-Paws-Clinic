import { formatBaht } from '@/lib/format/money';
import { businessReport } from '../content/business-report';
import type { ServiceAnalytics } from '../types/analytics.types';
import { ReportCard } from './report-card';
import { ServiceTable } from './service-table';
import { StatTile } from './stat-tile';
import { TileRow } from './tile-row';

/** What each service earned, and the same money split by line kind. */
export function ServiceReport({ report }: { report: ServiceAnalytics }) {
  const text = businessReport.services;

  return (
    <ReportCard title={text.title} note={text.note}>
      <div className="space-y-1.5">
        <h4 className="text-xs font-semibold text-ink-700">{text.byKind}</h4>
        <TileRow>
          {report.by_kind.map((kind) => (
            <StatTile key={kind.kind} label={text.kinds[kind.kind] ?? kind.kind} value={formatBaht(kind.revenue_satang)} />
          ))}
        </TileRow>
      </div>
      <ServiceTable services={report.services} />
    </ReportCard>
  );
}
