import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { Card } from '@/components/ui/card';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { addDays } from '@/features/booking/lib/booking-dates';
import { getDemandForecast } from '@/features/schedules/api/getDemandForecast';
import { getAssignmentRules, getScheduleCalendar } from '@/features/schedules/api/schedules';
import { AssignmentRulesSection } from '@/features/schedules/components/assignment-rules-section';
import { RosterDayView } from '@/features/schedules/components/day-view/roster-day-view';
import { RosterDialogs } from '@/features/schedules/components/dialogs/roster-dialogs';
import { ForecastLink } from '@/features/schedules/components/forecast/forecast-link';
import { RosterHeaderActions } from '@/features/schedules/components/roster-header-actions';
import { RosterTable } from '@/features/schedules/components/roster-table';
import { WeekNav } from '@/features/schedules/components/week-nav';
import { schedulePage } from '@/features/schedules/content/schedule-page';
import { copyPlan } from '@/features/schedules/lib/copy-plan';
import { busyDays, forecastSpan } from '@/features/schedules/lib/forecast-days';
import { loadRuleServices } from '@/features/schedules/lib/load-rule-services';
import { groupRows, rosterRows } from '@/features/schedules/lib/roster-rows';
import { dayCover } from '@/features/schedules/lib/roster-summary';
import { rosterWeek } from '@/features/schedules/lib/roster-week';
import { listStaffMembers } from '@/features/staff/api/listStaffMembers';
import { optional } from '@/lib/api/api-error';
import { Scope } from '@/lib/auth/scopes';
import { clinicDay } from '@/lib/format/datetime';

export const metadata: Metadata = { title: schedulePage.metaTitle };

interface SchedulesPageProps {
  searchParams: Promise<{ week?: string }>;
}

/**
 * One week of the roster at the branch in view, everyone at the branch in department groups, and,
 * for a session with `schedule:write`, dialogs to add, change, remove and copy shifts and to
 * record leave, and the case-assignment rules.
 */
export default async function SchedulesPage({ searchParams }: SchedulesPageProps) {
  const context = await loadStaffContext();

  if (!context?.can(Scope.ScheduleRead)) {
    return <Alert tone="info">{schedulePage.noAccess}</Alert>;
  }
  if (!context.branch) {
    return <Alert tone="info">{schedulePage.noBranch}</Alert>;
  }

  const { week } = await searchParams;
  const days = rosterWeek(week);
  const now = Date.now();
  const today = clinicDay(new Date(now));
  const branch = context.branch;
  const canWrite = context.can(Scope.ScheduleWrite);
  const span = context.can(Scope.StatsRead) ? forecastSpan(days, today) : null;

  const [calendar, members, services, lastWeek, rules, forecast] = await Promise.all([
    getScheduleCalendar(days[0]!, days[6]!, branch.id),
    // Everyone at the branch, so people with nothing planned show as empty rows: `staff:read`.
    context.can(Scope.StaffRead) ? optional(listStaffMembers(branch.id)) : Promise.resolve(null),
    canWrite ? loadRuleServices(branch.id, context.can(Scope.ServiceManage)) : Promise.resolve([]),
    // What "คัดลอกเวรสัปดาห์ก่อน" would copy, said in its dialog before anyone presses it.
    canWrite ? getScheduleCalendar(addDays(days[0]!, -7), addDays(days[0]!, -1), branch.id).catch(() => null) : null,
    // Extensions: a failed read costs the page only that part (§ 48 and § 33).
    canWrite ? getAssignmentRules().catch(() => null) : null,
    span ? getDemandForecast(branch.id, span.from, span.to).catch(() => null) : null,
  ]);

  const rows = rosterRows(calendar.staff, members, context.staffId);
  const groups = groupRows(rows);
  const cover = dayCover(rows, days);
  const busy = busyDays(forecast);
  const people = groups.flatMap((group) => group.rows.map((row) => ({ id: row.staff_id, name: row.display_name })));

  const page = (
    <div className="max-w-6xl space-y-6">
      <PageHeader
        title={schedulePage.title}
        subtitle={schedulePage.subtitle(branch.name)}
        actions={
          <>
            <RosterHeaderActions />
            {context.can(Scope.StatsRead) && <ForecastLink />}
          </>
        }
      />
      <WeekNav days={days} />

      {rows.length === 0 ? (
        <Card>
          <p className="px-5 py-6 text-sm text-ink-500">{schedulePage.empty}</p>
        </Card>
      ) : (
        <>
          <RosterTable days={days} groups={groups} today={today} now={now} cover={cover} busy={busy} />
          <RosterDayView days={days} today={today} now={now} groups={groups} cover={cover} busy={[...busy]} />
        </>
      )}

      {canWrite && <AssignmentRulesSection people={people} services={services} stored={rules} />}
    </div>
  );

  if (!canWrite) {
    return page;
  }

  return (
    <RosterDialogs
      people={people}
      branchId={branch.id}
      defaultDay={days.includes(today) ? today : days[0]!}
      copy={copyPlan(lastWeek, days)}
    >
      {page}
    </RosterDialogs>
  );
}
