import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { getTodayDashboard } from '@/features/analytics/api/getTodayDashboard';
import { BusinessTab } from '@/features/analytics/components/business-tab';
import { OperationsTab } from '@/features/analytics/components/operations-tab';
import { RangePicker } from '@/features/analytics/components/range-picker';
import { ReportTabs } from '@/features/analytics/components/report-tabs';
import { TodayOverview } from '@/features/analytics/components/today-overview';
import { dashboardPage } from '@/features/analytics/content/dashboard-page';
import { loadBusinessReports, loadOperationsReports } from '@/features/analytics/lib/load-reports';
import { PrivacyPolicyMissingNotice } from '@/features/privacy/components/staff/privacy-policy-missing-notice';
import { readReportRange, type ReportSearchParams, type ReportTab } from '@/features/analytics/lib/report-range';
import { ReportExportControl } from '@/features/reporting/components/report-export-control';
import { exportableReports } from '@/features/reporting/lib/exportable-reports';
import { getDailyBrief } from '@/features/staff-ai/api/getDailyBrief';
import { DailyBriefCard } from '@/features/staff-ai/components/brief/daily-brief-card';
import { optional } from '@/lib/api/api-error';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';
import { clinicDay } from '@/lib/format/datetime';

export const metadata: Metadata = { title: dashboardPage.metaTitle };

interface DashboardPageProps {
  searchParams: Promise<ReportSearchParams>;
}

/**
 * Today's cards on top, then the reports for a chosen range: operational statistics on
 * `stats:read`, the business dashboard on `analytics:read` and `analytics:hr`. Each part is drawn
 * only for a session holding its scope, and the API checks the scope again on every read.
 */
export default async function DashboardPage({ searchParams }: DashboardPageProps) {
  const context = await loadStaffContext();
  const can = (scope: string) => context?.can(scope) ?? false;

  const canToday = can(Scope.DashboardRead);
  const canStats = can(Scope.StatsRead);
  const canAnalytics = can(Scope.AnalyticsRead);
  const canHr = can(Scope.AnalyticsHr);

  if (!context || !(canToday || canStats || canAnalytics || canHr)) {
    return <Alert tone="info">{dashboardPage.noAccess}</Alert>;
  }
  if (!context.branch) {
    return <Alert tone="info">{dashboardPage.noBranch}</Alert>;
  }

  const branch = context.branch;
  const today = clinicDay(new Date());
  const tabs: ReportTab[] = [
    ...(canStats ? (['operations'] as const) : []),
    ...(canAnalytics || canHr ? (['business'] as const) : []),
  ];

  const asked = readReportRange(await searchParams, today);
  const range = tabs.includes(asked.tab) || tabs.length === 0 ? asked : { ...asked, tab: tabs[0]! };
  const showTab = tabs.includes(range.tab);

  // "ส่งออกรายงาน": only with report:export, and only the reports whose screen this session may read.
  const exportable = exportableReports(can);

  const [todayView, operations, business, brief] = await Promise.all([
    canToday ? authed(() => optional(getTodayDashboard(branch.id))) : Promise.resolve(null),
    showTab && range.tab === 'operations' ? authed(() => loadOperationsReports(range, branch.id)) : Promise.resolve(null),
    showTab && range.tab === 'business'
      ? authed(() => loadBusinessReports(range, branch.id, { read: canAnalytics, hr: canHr }))
      : Promise.resolve(null),
    // The AI morning brief (docs/contract-extensions.md § 34). Always answers, "disabled" when the AI is off.
    canToday ? authed(() => optional(getDailyBrief(branch.id))) : Promise.resolve(null),
  ]);

  return (
    <div className="max-w-6xl space-y-6">
      <PageHeader title={dashboardPage.title} subtitle={dashboardPage.subtitle(branch.name)} />

      {/* Owner only, while no privacy notice is published (docs/contract-extensions.md § 35). */}
      <PrivacyPolicyMissingNotice />

      {brief && brief.status !== 'disabled' && <DailyBriefCard brief={brief} />}

      {todayView && <TodayOverview today={todayView} />}

      {showTab && (
        <section className="space-y-4">
          <ReportTabs range={range} tabs={tabs} />
          <RangePicker range={range} today={today} />
          {exportable.length > 0 && <ReportExportControl reports={exportable} from={range.from} to={range.to} branchId={branch.id} />}
          {operations && <OperationsTab reports={operations} />}
          {business && <BusinessTab reports={business} canRead={canAnalytics} />}
        </section>
      )}
    </div>
  );
}
