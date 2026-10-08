import type { Metadata } from 'next';
import { CurrentAppointment } from '@/features/portal/components/reschedule/current-appointment';
import { RescheduleClosed } from '@/features/portal/components/reschedule/reschedule-closed';
import { RescheduleWizard } from '@/features/portal/components/reschedule/reschedule-wizard';
import { PageHeading } from '@/features/portal/components/shared/page-heading';
import { portalReschedule } from '@/features/portal/content/portal-reschedule';
import { loadReschedule } from '@/features/portal/lib/load-reschedule';

export const metadata: Metadata = { title: `${portalReschedule.title} · Happy Paws Clinic` };

/** Moving one of the owner's appointments to another time, with the booking page's slot picker. */
export default async function PortalReschedulePage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const { appointment, vets } = await loadReschedule(id);
  const text = portalReschedule;

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <PageHeading title={text.title} intro={text.intro} />

      <div className="space-y-6 rounded-2xl bg-white p-5 shadow-sm sm:p-6">
        <CurrentAppointment appointment={appointment} />

        {appointment.canChange ? (
          <>
            <p className="text-xs text-site-muted">{text.cutoffNote}</p>
            <RescheduleWizard appointment={appointment} vetNames={vets} />
          </>
        ) : (
          <RescheduleClosed />
        )}
      </div>
    </div>
  );
}
