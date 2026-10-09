import type { Metadata } from 'next';
import Link from 'next/link';
import { Alert } from '@/components/ui/alert';
import { buttonClass } from '@/components/ui/button';
import { EmptyState } from '@/components/ui/empty-state';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { CalendarBoard } from '@/features/appointments/components/calendar/calendar-board';
import { DayConflictCheck } from '@/features/appointments/components/calendar/day-conflict-check';
import { DayNav } from '@/features/appointments/components/calendar/day-nav';
import { waitingListText } from '@/features/appointments/content/advanced-booking';
import { appointmentCalendar } from '@/features/appointments/content/appointment-calendar';
import { appointmentPaths } from '@/features/appointments/lib/appointment-paths';
import { calendarDay } from '@/features/appointments/lib/calendar-day';
import { conflictLabels } from '@/features/appointments/lib/conflict-labels';
import { loadCalendar } from '@/features/appointments/lib/load-calendar';
import { isOpen } from '@/features/appointments/lib/status-labels';
import { ApiError } from '@/lib/api/api-error';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';
import { clinicDay, formatLongDate } from '@/lib/format/datetime';

export const metadata: Metadata = { title: appointmentCalendar.metaTitle };

/**
 * One clinic day at the branch in view, one column per vet. The day is in the address
 * (`?date=`), so a day can be reloaded, bookmarked or sent to a colleague.
 */
export default async function AppointmentsPage({ searchParams }: { searchParams: Promise<{ date?: string }> }) {
  const [context, params] = await Promise.all([loadStaffContext(), searchParams]);
  const text = appointmentCalendar;

  if (!context?.branch) {
    return <EmptyState title={text.noBranch} />;
  }
  if (!context.can(Scope.AppointmentRead)) {
    return <Alert tone="info">{text.noAccess}</Alert>;
  }

  const { branch } = context;
  const today = clinicDay(new Date());
  const day = calendarDay(params.date);

  let view;
  try {
    view = await authed(() => loadCalendar(day, branch.id));
  } catch (error) {
    if (error instanceof ApiError && (error.status === 403 || error.status === 422)) {
      return <Alert>{error.message}</Alert>;
    }
    throw error;
  }

  const canWrite = context.can(Scope.AppointmentWrite);
  const live = view.items.filter((item) => isOpen(item.appointment.status)).length;

  return (
    <div className="space-y-5">
      <PageHeader
        title={text.title}
        subtitle={`${branch.name} / ${formatLongDate(day)} / ${text.summary(view.items.length, live)}`}
        actions={
          canWrite && (
            <>
              <Link href={appointmentPaths.waitingList} className={buttonClass('secondary')}>
                {waitingListText.link}
              </Link>
              <Link href={appointmentPaths.book(day)} className={buttonClass()}>
                {text.book}
              </Link>
            </>
          )
        }
      />

      <DayNav day={day} today={today} />

      {view.closedDay && <Alert tone="info">{text.closedDay}</Alert>}

      {view.items.length > 0 && <DayConflictCheck day={day} branchId={branch.id} labels={conflictLabels(view.items)} />}

      <CalendarBoard
        view={view}
        today={today}
        permissions={{
          canWrite,
          canCheckIn: context.can(Scope.AppointmentCheckIn),
          canConsult: context.can(Scope.ConsultWrite),
        }}
      />
    </div>
  );
}
