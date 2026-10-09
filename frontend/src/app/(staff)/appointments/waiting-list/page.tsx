import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { BackLink } from '@/components/ui/back-link';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { EmptyState } from '@/components/ui/empty-state';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { listWaitingList } from '@/features/appointments/api/advancedBooking';
import { WaitingListEntries } from '@/features/appointments/components/waiting-list/waiting-list-entries';
import { WaitingListForm } from '@/features/appointments/components/waiting-list/waiting-list-form';
import { WaitingListHow } from '@/features/appointments/components/waiting-list/waiting-list-how';
import { appointmentCalendar } from '@/features/appointments/content/appointment-calendar';
import { waitingListText } from '@/features/appointments/content/advanced-booking';
import { appointmentPaths } from '@/features/appointments/lib/appointment-paths';
import { loadVets } from '@/features/appointments/lib/load-calendar';
import type { WaitingListStatus } from '@/features/appointments/types/advanced-booking.types';
import { listServices } from '@/features/service-catalog/api/listServices';
import { ApiError } from '@/lib/api/api-error';
import { Scope } from '@/lib/auth/scopes';
import { clinicDay } from '@/lib/format/datetime';

export const metadata: Metadata = { title: waitingListText.metaTitle };

const PAGE_SIZE = 50;

/**
 * The waiting list at the branch in view: the form that adds a pet, and the entries an opening here
 * would reach (this branch's and those for any branch), with a way to take one off
 * (docs/contract-extensions.md § 23). Which status and which page are in the address.
 */
export default async function WaitingListPage({ searchParams }: { searchParams: Promise<{ status?: string; cursor?: string }> }) {
  const [context, params] = await Promise.all([loadStaffContext(), searchParams]);
  const text = waitingListText;

  if (!context?.branch) {
    return <EmptyState title={appointmentCalendar.noBranch} />;
  }
  if (!context.can(Scope.AppointmentWrite)) {
    return <Alert tone="info">{text.noAccess}</Alert>;
  }

  const branchId = context.branch.id;
  const status: WaitingListStatus = params.status === 'lapsed' ? 'lapsed' : 'waiting';

  let page;
  try {
    page = await listWaitingList({ branchId, status, cursor: params.cursor, limit: PAGE_SIZE });
  } catch (error) {
    // A cursor from an old address, or a branch this session no longer works at.
    if (error instanceof ApiError && (error.status === 403 || error.status === 422)) {
      return <Alert>{error.message}</Alert>;
    }
    throw error;
  }

  const [services, vets] = await Promise.all([listServices(branchId).catch(() => []), loadVets(branchId)]);

  return (
    <div className="max-w-3xl space-y-6">
      <div className="space-y-2">
        <BackLink href={appointmentPaths.calendar()}>{text.back}</BackLink>
        <PageHeader title={text.title} subtitle={`${context.branch.name} / ${text.subtitle}`} />
      </div>

      <WaitingListHow />

      <Card>
        <CardHeader title={text.addHeading} />
        <CardBody className="py-5">
          <WaitingListForm branchId={branchId} services={services} vets={vets} today={clinicDay(new Date())} />
        </CardBody>
      </Card>

      <WaitingListEntries
        entries={page.data}
        status={status}
        nextCursor={page.page.next_cursor}
        paged={Boolean(params.cursor)}
        vetNames={Object.fromEntries(vets.map((vet) => [vet.id, vet.name]))}
        canOpenCustomer={context.can(Scope.CustomerRead)}
      />
    </div>
  );
}
