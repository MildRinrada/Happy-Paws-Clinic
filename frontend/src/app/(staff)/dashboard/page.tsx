import type { Metadata } from 'next';
import { redirect } from 'next/navigation';
import { Alert } from '@/components/ui/alert';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { getTodayDashboard } from '@/features/analytics/api/getTodayDashboard';
import { AttentionBox } from '@/features/analytics/components/today/attention-box';
import { AutoRefresh } from '@/features/analytics/components/today/auto-refresh';
import { DueBackPreview } from '@/features/analytics/components/today/due-back-preview';
import { HeadlineTiles } from '@/features/analytics/components/today/headline-tiles';
import { MineStrip } from '@/features/analytics/components/today/mine-strip';
import { QuickActions } from '@/features/analytics/components/today/quick-actions';
import { TodayDetails } from '@/features/analytics/components/today/today-details';
import { TodaySection } from '@/features/analytics/components/today/today-section';
import { UpcomingVisits } from '@/features/analytics/components/today/upcoming-visits';
import { dashboardPage } from '@/features/analytics/content/dashboard-page';
import { loadAttention } from '@/features/analytics/lib/load-attention';
import { loadDueBackPreview } from '@/features/analytics/lib/load-due-back-preview';
import { loadLastWeek } from '@/features/analytics/lib/load-last-week';
import { loadUpcoming } from '@/features/analytics/lib/load-upcoming';
import { movedReportHref, REPORTS_PATH, type ReportSearchParams } from '@/features/analytics/lib/report-range';
import { todayLinks } from '@/features/analytics/lib/today-links';
import { REFRESH_SECONDS } from '@/features/analytics/lib/today-settings';
import { tolerant } from '@/features/analytics/lib/tolerant-read';
import { getAttendanceRisk } from '@/features/appointments/api/getAttendanceRisk';
import { PrivacyPolicyMissingNotice } from '@/features/privacy/components/staff/privacy-policy-missing-notice';
import { getDailyBrief } from '@/features/staff-ai/api/getDailyBrief';
import { DailyBriefCard } from '@/features/staff-ai/components/brief/daily-brief-card';
import { Scope } from '@/lib/auth/scopes';
import { clinicDay } from '@/lib/format/datetime';

export const metadata: Metadata = { title: dashboardPage.metaTitle };

interface DashboardPageProps {
  searchParams: Promise<ReportSearchParams>;
}

/**
 * Today at the branch in view, for whoever opens the console first thing: what is waiting to be
 * done, who is due in the next two hours, the day's main numbers, the AI's morning brief, every
 * count folded away, and customers to call. The ranged reports are on `/reports`.
 *
 * The page needs `dashboard:read`. Every box past the day's numbers reads with the scope of its own
 * page and is left out when the session lacks it, or when its read fails: one box that cannot be
 * read never takes the page down. The page reads itself again every minute while it is on screen.
 * The brief is safe to read that often: `GET /staff/ai/daily-brief` returns the stored brief and
 * never calls the model or counts a use.
 */
export default async function DashboardPage({ searchParams }: DashboardPageProps) {
  // The reports used to live here, as `/dashboard?tab=&from=...`; those links still work.
  const moved = movedReportHref(await searchParams);
  if (moved) {
    redirect(moved);
  }

  const context = await loadStaffContext();
  const can = (scope: string) => context?.can(scope) ?? false;

  if (!context || !can(Scope.DashboardRead)) {
    if (can(Scope.StatsRead) || can(Scope.AnalyticsRead) || can(Scope.AnalyticsHr)) {
      redirect(REPORTS_PATH);
    }
    return <Alert tone="info">{dashboardPage.noAccess}</Alert>;
  }
  if (!context.branch) {
    return <Alert tone="info">{dashboardPage.noBranch}</Alert>;
  }

  const branch = context.branch;
  const now = new Date();
  const today = clinicDay(now);
  const canAppointments = can(Scope.AppointmentRead);
  const canCustomers = can(Scope.CustomerRead);

  // Read once and shared: the to-do box counts the risky bookings, and the upcoming list takes
  // owner and pet names from it.
  const risk = canAppointments ? tolerant(() => getAttendanceRisk(today, branch.id)) : Promise.resolve(null);

  const [todayView, brief, attention, upcoming, lastWeek, dueBack] = await Promise.all([
    tolerant(() => getTodayDashboard(branch.id)),
    tolerant(() => getDailyBrief(branch.id)),
    loadAttention({ can, branchId: branch.id, today, now, risk }),
    canAppointments
      ? tolerant(() => loadUpcoming(branch.id, today, now, risk, { customers: canCustomers, patients: can(Scope.PatientRead) }))
      : Promise.resolve(null),
    loadLastWeek(branch.id, today, { appointments: can(Scope.StatsRead), revenue: can(Scope.AnalyticsRead) }),
    can(Scope.CrmRead) ? tolerant(() => loadDueBackPreview(branch.id, canCustomers)) : Promise.resolve(null),
  ]);

  const links = todayLinks(can, today);

  return (
    <div className="max-w-6xl space-y-6">
      <AutoRefresh seconds={REFRESH_SECONDS} />
      <PageHeader
        title={dashboardPage.title}
        subtitle={dashboardPage.subtitle(branch.name)}
        actions={<QuickActions can={can} />}
      />

      {/* Owner only, while no privacy notice is published (docs/contract-extensions.md § 35). */}
      <PrivacyPolicyMissingNotice />

      {(attention || upcoming) && (
        <div className={`grid gap-4 ${attention && upcoming ? 'lg:grid-cols-5' : ''}`}>
          {attention && (
            <div className={upcoming ? 'lg:col-span-3' : ''}>
              <AttentionBox view={attention} />
            </div>
          )}
          {upcoming && (
            <div className={attention ? 'lg:col-span-2' : ''}>
              <UpcomingVisits view={upcoming} calendarHref={links.calendar ?? '/appointments'} />
            </div>
          )}
        </div>
      )}

      {todayView && (
        <TodaySection generatedAt={todayView.generated_at}>
          {todayView.mine && <MineStrip mine={todayView.mine} links={links} />}
          <HeadlineTiles today={todayView} lastWeek={lastWeek} links={links} />
        </TodaySection>
      )}

      {/* The AI morning brief (docs/contract-extensions.md § 34), hidden while the assistant is off. */}
      {brief && brief.status !== 'disabled' && <DailyBriefCard brief={brief} />}

      {todayView && <TodayDetails today={todayView} />}

      {dueBack && <DueBackPreview view={dueBack} canOpenCustomer={canCustomers} />}
    </div>
  );
}
