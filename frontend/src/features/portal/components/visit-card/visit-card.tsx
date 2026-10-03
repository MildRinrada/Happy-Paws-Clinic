import { visitCard } from '../../content/visit-card';
import type { VisitCardView } from '../../lib/load-visit-card';
import { visitNotice } from '../../lib/visit-notice';
import { stageProgress } from '../../lib/visit-stage';
import { AppointmentSummary } from './appointment-summary';
import { LiveHeader } from './live-header';
import { QueueStat } from './queue-stat';
import { VisitActions } from './visit-actions';
import { VisitNotice } from './visit-notice';
import { VisitProgress } from './visit-progress';

/**
 * The owner's visit at a glance: what and when, their queue number, how far along it is, what to
 * do now, and the buttons for it.
 */
export function VisitCard({ view }: { view: VisitCardView }) {
  const { appointment, ticket, today, petName } = view;
  const notice = visitNotice(view);
  const pet = petName && `${visitCard.petPrefix}${petName}`;
  const service = [appointment?.serviceName, pet].filter(Boolean).join(' ');

  return (
    <section className="w-full max-w-[420px] space-y-5 rounded-2xl bg-site-teal p-5 shadow-sm">
      <LiveHeader
        title={today ? visitCard.liveTitle : visitCard.upcomingTitle}
        live={today}
        badge={appointment && visitCard.statusBadge[appointment.status]}
      />

      <div className="flex items-start justify-between gap-3">
        {appointment ? (
          <AppointmentSummary
            service={service || visitCard.upcomingTitle}
            vet={appointment.vetName ?? visitCard.vetFallback}
            startsAt={appointment.startsAt}
            showDate={!today}
          />
        ) : (
          <p className="text-sm font-semibold text-white">{[visitCard.walkInTitle, pet].filter(Boolean).join(' ')}</p>
        )}

        {today && (
          <div className="flex shrink-0 gap-2">
            <QueueStat label={visitCard.queueLabel} value={ticket?.number ?? visitCard.noTicket} />
            {ticket && <QueueStat label={visitCard.aheadLabel} value={visitCard.aheadValue(ticket.ahead)} />}
          </div>
        )}
      </div>

      {today && <VisitProgress progress={stageProgress[view.stage]} steps={visitCard.steps} />}

      <VisitNotice lead={notice.lead} text={notice.text} />

      <VisitActions
        appointmentId={appointment?.id}
        canCheckIn={view.canCheckIn}
        canReschedule={view.canReschedule}
      />
    </section>
  );
}
