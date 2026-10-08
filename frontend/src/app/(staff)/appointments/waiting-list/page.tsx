import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { BackLink } from '@/components/ui/back-link';
import { Card, CardBody } from '@/components/ui/card';
import { EmptyState } from '@/components/ui/empty-state';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { WaitingListForm } from '@/features/appointments/components/waiting-list/waiting-list-form';
import { WaitingListHow } from '@/features/appointments/components/waiting-list/waiting-list-how';
import { appointmentCalendar } from '@/features/appointments/content/appointment-calendar';
import { waitingListText } from '@/features/appointments/content/advanced-booking';
import { appointmentPaths } from '@/features/appointments/lib/appointment-paths';
import { loadVets } from '@/features/appointments/lib/load-calendar';
import { listServices } from '@/features/service-catalog/api/listServices';
import { Scope } from '@/lib/auth/scopes';
import { clinicDay } from '@/lib/format/datetime';

export const metadata: Metadata = { title: waitingListText.metaTitle };

/**
 * Adds a pet to the waiting list at the branch in view. The contract only adds: there is no list
 * to show and nothing to withdraw, which the page says before the form.
 */
export default async function WaitingListPage() {
  const context = await loadStaffContext();
  const text = waitingListText;

  if (!context?.branch) {
    return <EmptyState title={appointmentCalendar.noBranch} />;
  }
  if (!context.can(Scope.AppointmentWrite)) {
    return <Alert tone="info">{text.noAccess}</Alert>;
  }

  const [services, vets] = await Promise.all([listServices(context.branch.id).catch(() => []), loadVets(context.branch.id)]);

  return (
    <div className="max-w-2xl space-y-6">
      <div className="space-y-2">
        <BackLink href={appointmentPaths.calendar()}>{text.back}</BackLink>
        <PageHeader title={text.title} subtitle={`${context.branch.name} / ${text.subtitle}`} />
      </div>

      <WaitingListHow />

      <Card>
        <CardBody className="py-5">
          <WaitingListForm branchId={context.branch.id} services={services} vets={vets} today={clinicDay(new Date())} />
        </CardBody>
      </Card>
    </div>
  );
}
