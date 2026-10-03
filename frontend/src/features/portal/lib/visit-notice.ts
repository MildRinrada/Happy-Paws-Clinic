import { formatTime } from '@/lib/format/datetime';
import { visitCard } from '../content/visit-card';
import type { VisitCardView } from './load-visit-card';

/**
 * The yellow note's two parts for where the visit has got to.
 *
 * Before check-in it gives the time to arrive by: the appointment less a margin for checking in.
 * It does not say when to leave home. That needs the owner's location and live traffic, which this
 * system has neither of, and a made-up travel time would be worse than none.
 */
export function visitNotice(view: VisitCardView): { lead?: string; text: string } {
  const { stage, appointment, ticket } = view;

  if (stage === 'before') {
    if (!appointment) return { text: visitCard.notice.before };

    const arriveBy = new Date(Date.parse(appointment.startsAt) - visitCard.arriveMinutesEarly * 60_000);
    return { lead: formatTime(arriveBy.toISOString()), text: visitCard.notice.before };
  }

  if (stage === 'called') {
    return { lead: ticket?.number, text: visitCard.notice.called };
  }

  return { lead: visitCard.noticeLead[stage], text: visitCard.notice[stage] };
}
