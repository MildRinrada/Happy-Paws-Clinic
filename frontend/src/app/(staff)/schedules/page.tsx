import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { getScheduleCalendar } from '@/features/schedules/api/schedules';
import { AddShiftForm } from '@/features/schedules/components/add-shift-form';
import { RosterTable } from '@/features/schedules/components/roster-table';
import { TimeOffForm } from '@/features/schedules/components/time-off-form';
import { WeekNav } from '@/features/schedules/components/week-nav';
import { schedulePage } from '@/features/schedules/content/schedule-page';
import { rosterWeek } from '@/features/schedules/lib/roster-week';
import { listStaffMembers } from '@/features/staff/api/listStaffMembers';
import { optional } from '@/lib/api/api-error';
import { Scope } from '@/lib/auth/scopes';
import { clinicDay } from '@/lib/format/datetime';

export const metadata: Metadata = { title: schedulePage.metaTitle };

interface SchedulesPageProps {
  searchParams: Promise<{ week?: string }>;
}

/** One week of the roster at the branch in view, and the forms that add to it. */
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
  const today = clinicDay(new Date());
  const branch = context.branch;
  const canWrite = context.can(Scope.ScheduleWrite);

  const [calendar, members] = await Promise.all([
    getScheduleCalendar(days[0]!, days[6]!, branch.id),
    // Choosing someone who has no shift yet needs the staff list, which is `staff:read`.
    canWrite && context.can(Scope.StaffRead) ? optional(listStaffMembers(branch.id)) : Promise.resolve(null),
  ]);

  const people = members
    ? members.map((member) => ({ id: member.id, name: member.display_name }))
    : calendar.staff.map((person) => ({ id: person.staff_id, name: person.display_name }));
  const defaultDay = days.includes(today) ? today : days[0]!;

  return (
    <div className="max-w-6xl space-y-6">
      <PageHeader title={schedulePage.title} subtitle={schedulePage.subtitle(branch.name)} actions={<WeekNav days={days} />} />
      <RosterTable days={days} staff={calendar.staff} today={today} />

      {canWrite && (
        <div className="grid gap-6 lg:grid-cols-2">
          <AddShiftForm people={people} branchId={branch.id} defaultDay={defaultDay} />
          <TimeOffForm people={people} defaultDay={defaultDay} />
        </div>
      )}
    </div>
  );
}
