import type { Metadata } from 'next';
import { AppointmentList } from '@/features/portal/components/appointments/appointment-list';
import { PageHeading } from '@/features/portal/components/shared/page-heading';
import { portalAppointments } from '@/features/portal/content/portal-appointments';
import { loadAppointments } from '@/features/portal/lib/load-appointments';

export const metadata: Metadata = { title: `${portalAppointments.title} · Happy Paws Clinic` };

/** The owner's appointments: what is coming up, then what has been. */
export default async function PortalAppointmentsPage() {
  const { upcoming, history } = await loadAppointments();
  const text = portalAppointments;

  return (
    <div className="space-y-6">
      <PageHeading title={text.title} intro={text.intro} />
      <AppointmentList title={text.upcomingTitle} empty={text.upcomingEmpty} rows={upcoming} />
      <AppointmentList title={text.historyTitle} empty={text.historyEmpty} rows={history} />
    </div>
  );
}
