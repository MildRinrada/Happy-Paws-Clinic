import { visitCard } from '../../content/visit-card';
import type { VisitCardView } from '../../lib/load-visit-card';
import { visitNotice } from '../../lib/visit-notice';
import { stageStep } from '../../lib/visit-stage';
import { visitStats } from '../../lib/visit-stats';
import { AppointmentSummary } from './appointment-summary';
import { LiveHeader } from './live-header';
import { VisitActions } from './visit-actions';
import { VisitLine } from './visit-line';
import { VisitNotice } from './visit-notice';
import { VisitProgress } from './visit-progress';
import { VisitStats } from './visit-stats';

/**
 * The owner's visit at a glance: what and for which pet, their queue number and how many are
 * ahead, how far along it is, what to do now, and the buttons for it. A visit on a later day shows
 * its date and time in place of the queue.
 */
export function VisitCard({ view }: { view: VisitCardView }) {
  const { appointment, today, petName } = view;
  const notice = visitNotice(view);
  const pet = petName && `${visitCard.petPrefix}${petName}`;
  const line = appointment
    ? [appointment.serviceName, pet].filter(Boolean).join(' ') || visitCard.upcomingTitle
    : [visitCard.walkInTitle, pet].filter(Boolean).join(' ');
  const vet = appointment && (appointment.vetName ?? visitCard.vetFallback);

  return (
    <section className="w-full rounded-2xl bg-brand-800 p-5">
      <LiveHeader
        title={today ? visitCard.liveTitle : visitCard.upcomingTitle}
        live={today}
        badge={appointment && visitCard.statusBadge[appointment.status]}
      />

      <div className="mt-4">
        {today || !appointment ? (
          <>
            <VisitLine text={line} vet={vet} />
            <div className="mt-3.5">
              <VisitStats stats={visitStats(view)} />
            </div>
            <div className="mt-4">
              <VisitProgress steps={visitCard.steps} current={stageStep[view.stage]} label={visitCard.stepsLabel} />
            </div>
          </>
        ) : (
          <AppointmentSummary service={line} vet={vet ?? visitCard.vetFallback} startsAt={appointment.startsAt} />
        )}
      </div>

      <div className="mt-4">
        <VisitNotice lead={notice.lead} text={notice.text} />
      </div>

      <div className="mt-4">
        <VisitActions
          appointmentId={appointment?.id}
          canCheckIn={view.canCheckIn}
          canReschedule={view.canReschedule}
        />
      </div>
    </section>
  );
}
