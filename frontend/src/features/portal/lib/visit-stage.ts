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
 * How full the progress bar is, as a fraction. The four labels under it (check-in, waiting, exam,
 * payment) sit at 0, 1/3, 2/3 and 1, and each stage fills to just past its own label.
 */
export const stageProgress: Record<VisitStage, number> = {
  before: 0.04,
  waiting: 0.4,
  called: 0.55,
  in_room: 0.72,
  done: 1,
};
