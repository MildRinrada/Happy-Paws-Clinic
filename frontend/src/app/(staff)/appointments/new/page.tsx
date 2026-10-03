import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { BackLink } from '@/components/ui/back-link';
import { Card, CardBody } from '@/components/ui/card';
import { EmptyState } from '@/components/ui/empty-state';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { NewAppointmentForm } from '@/features/appointments/components/new/new-appointment-form';
import { appointmentCalendar, newAppointment } from '@/features/appointments/content/appointment-calendar';
import { appointmentPaths } from '@/features/appointments/lib/appointment-paths';
import { calendarDay } from '@/features/appointments/lib/calendar-day';
import { loadVets } from '@/features/appointments/lib/load-calendar';
import { listServices } from '@/features/service-catalog/api/listServices';
import { Scope } from '@/lib/auth/scopes';

export const metadata: Metadata = { title: newAppointment.metaTitle };

/** The counter books for a customer, at the branch in view. */
export default async function NewAppointmentPage({ searchParams }: { searchParams: Promise<{ date?: string }> }) {
  const [context, params] = await Promise.all([loadStaffContext(), searchParams]);
  const text = newAppointment;

  if (!context?.branch) {
    return <EmptyState title={appointmentCalendar.noBranch} />;
  }
  if (!context.can(Scope.AppointmentWrite)) {
    return <Alert tone="info">{text.noAccess}</Alert>;
  }

  const day = calendarDay(params.date);
  const [services, vets] = await Promise.all([listServices(context.branch.id).catch(() => []), loadVets(context.branch.id)]);

  return (
    <div className="max-w-2xl space-y-6">
      <div className="space-y-2">
        <BackLink href={appointmentPaths.calendar(day)}>{text.back}</BackLink>
        <PageHeader title={text.title} subtitle={`${context.branch.name} / ${text.subtitle}`} />
      </div>

      <Card>
        <CardBody className="py-5">
          <NewAppointmentForm branchId={context.branch.id} services={services} vets={vets} initialDay={day} />
        </CardBody>
      </Card>
    </div>
  );
}
