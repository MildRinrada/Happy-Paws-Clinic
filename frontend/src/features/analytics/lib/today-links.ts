import { appointmentPaths } from '@/features/appointments/lib/appointment-paths';
import { Scope } from '@/lib/auth/scopes';
import { reportHref, readReportRange } from './report-range';

/** Where each number of the day leads. A link is given only when the session may open its page. */
export interface TodayLinks {
  calendar?: string;
  queue?: string;
  revenue?: string;
}

/**
 * Today's takings open the business report on `analytics:read`, or the paid bills at the cash desk
 * on `invoice:write`, the two scopes the API shows the money to.
 */
export function todayLinks(can: (scope: string) => boolean, today: string): TodayLinks {
  const revenue = can(Scope.AnalyticsRead)
    ? reportHref(readReportRange({ tab: 'business' }, today))
    : can(Scope.InvoiceWrite)
      ? '/billing?status=paid'
      : undefined;

  return {
    calendar: can(Scope.AppointmentRead) ? appointmentPaths.calendar(today) : undefined,
    queue: can(Scope.QueueRead) ? '/queue' : undefined,
    revenue,
  };
}
