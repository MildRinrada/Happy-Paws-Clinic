import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { BusinessTab } from '@/features/analytics/components/business-tab';
import { OperationsTab } from '@/features/analytics/components/operations-tab';
import { RangePicker } from '@/features/analytics/components/range-picker';
import { ReportTabs } from '@/features/analytics/components/report-tabs';
import { reportsPage } from '@/features/analytics/content/reports-page';
import { loadBusinessReports, loadOperationsReports } from '@/features/analytics/lib/load-reports';
import { readReportRange, type ReportSearchParams, type ReportTab } from '@/features/analytics/lib/report-range';
import { ReportExportControl } from '@/features/reporting/components/report-export-control';
import { exportableReports } from '@/features/reporting/lib/exportable-reports';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';
import { clinicDay } from '@/lib/format/datetime';

export const metadata: Metadata = { title: reportsPage.metaTitle };

interface ReportsPageProps {
  searchParams: Promise<ReportSearchParams>;
}

/**
 * The reports for a chosen range: operational statistics on `stats:read`, the business dashboard on
 * `analytics:read` and `analytics:hr`. A tab is drawn only for a session holding its scope, and the
 * API checks the scope again on every read. These lived under today's cards on `/dashboard`; old
 * links to that address are sent here with their query.
 */
export default async function ReportsPage({ searchParams }: ReportsPageProps) {
  const context = await loadStaffContext();
  const can = (scope: string) => context?.can(scope) ?? false;

  const canStats = can(Scope.StatsRead);
  const canAnalytics = can(Scope.AnalyticsRead);
  const canHr = can(Scope.AnalyticsHr);

  const tabs: ReportTab[] = [
    ...(canStats ? (['operations'] as const) : []),
    ...(canAnalytics || canHr ? (['business'] as const) : []),
  ];

  if (!context || tabs.length === 0) {
    return <Alert tone="info">{reportsPage.noAccess}</Alert>;
  }
  if (!context.branch) {
    return <Alert tone="info">{reportsPage.noBranch}</Alert>;
  }

  const branch = context.branch;
  const today = clinicDay(new Date());

  const asked = readReportRange(await searchParams, today);
  const range = tabs.includes(asked.tab) ? asked : { ...asked, tab: tabs[0]! };

  // "ส่งออกรายงาน": only with report:export, and only the reports whose screen this session may read.
  const exportable = exportableReports(can);

  const [operations, business] = await Promise.all([
    range.tab === 'operations' ? authed(() => loadOperationsReports(range, branch.id)) : Promise.resolve(null),
    range.tab === 'business'
      ? authed(() => loadBusinessReports(range, branch.id, { read: canAnalytics, hr: canHr }))
      : Promise.resolve(null),
  ]);

  return (
    <div className="max-w-6xl space-y-5">
      <PageHeader title={reportsPage.title} subtitle={reportsPage.subtitle(branch.name)} />
      <ReportTabs range={range} tabs={tabs} />
      <RangePicker range={range} today={today} />
      {exportable.length > 0 && <ReportExportControl reports={exportable} from={range.from} to={range.to} branchId={branch.id} />}
      {operations && <OperationsTab reports={operations} />}
      {business && <BusinessTab reports={business} canRead={canAnalytics} />}
    </div>
  );
}
