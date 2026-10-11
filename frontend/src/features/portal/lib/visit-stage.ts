import type { AppointmentStatus } from '@/features/appointments/types/appointment.types';
import type { PortalTicketStatus } from '../types/portal.types';

/**
 * Where a visit has got to, from the owner's side of it: not yet checked in, waiting, called,
 * being seen, or finished. Read from the queue ticket when there is one, since that moves first;
 * otherwise from the appointment.
 */
export type VisitStage = 'before' | 'waiting' | 'called' | 'in_room' | 'done';

export function visitStage(ticket: PortalTicketStatus | undefined, appointment: AppointmentStatus | undefined): VisitStage {
  switch (ticket) {
    case 'waiting':
    case 'skipped':
      return 'waiting';
    case 'called':
      return 'called';
    case 'in_room':
      return 'in_room';
    case 'served':
      return 'done';
  }

  switch (appointment) {
    case 'checked_in':
      return 'waiting';
    case 'in_consultation':
      return 'in_room';
    case 'completed':
      return 'done';
    default:
      return 'before';
  }
}

/**
 * Which of the four steps under the card (check-in, waiting, exam, payment) the visit is at, from
 * 0. The steps before it are done. Called counts as the exam step: the owner is on the way in.
 */
export const stageStep: Record<VisitStage, number> = {
  before: 0,
  waiting: 1,
  called: 2,
  in_room: 2,
  done: 3,
};
