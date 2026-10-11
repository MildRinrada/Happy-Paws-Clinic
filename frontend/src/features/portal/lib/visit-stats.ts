import { formatTime } from '@/lib/format/datetime';
import { visitCard } from '../content/visit-card';
import type { VisitCardView } from './load-visit-card';

/** One of the two boxes on today's card. `quiet` is a word rather than a number, set smaller. */
export interface VisitStat {
  label: string;
  value: string;
  unit?: string;
  quiet?: boolean;
}

/** The second box once there is a ticket: how many are ahead while waiting, where the visit is after that. */
function secondStat({ ticket, stage }: VisitCardView): VisitStat {
  const now = visitCard.stageValue[stage];
  if (now) return { label: visitCard.stageLabel, value: now, quiet: true };

  return ticket?.ahead
    ? { label: visitCard.aheadLabel, value: String(ticket.ahead), unit: visitCard.aheadUnit }
    : { label: visitCard.aheadLabel, value: visitCard.aheadNext, quiet: true };
}

/**
 * The two boxes on a card for today: the queue number and how many are still ahead once there is
 * a ticket; before check-in, the appointment's time and the fact that there is no number yet.
 */
export function visitStats(view: VisitCardView): VisitStat[] {
  const { ticket, appointment } = view;

  if (ticket) {
    return [{ label: visitCard.queueLabel, value: ticket.number }, secondStat(view)];
  }

  if (appointment) {
    return [
      { label: visitCard.timeLabel, value: formatTime(appointment.startsAt) },
      { label: visitCard.queueLabel, value: visitCard.noTicket, quiet: true },
    ];
  }

  return [];
}
