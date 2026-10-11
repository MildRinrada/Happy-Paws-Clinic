import { appointmentPaths } from '@/features/appointments/lib/appointment-paths';
import { customerPaths } from '@/features/customers/lib/customer-paths';
import { Scope } from '@/lib/auth/scopes';

export type QuickActionKey = 'issueTicket' | 'book' | 'register' | 'collect';

export interface QuickAction {
  key: QuickActionKey;
  /** An existing page that does the job. */
  href: string;
  /** Everything the page needs to be usable for this job; the shortcut shows only with all of them. */
  scopes: string[];
}

/**
 * The counter's four everyday jobs, each pointing at the screen that already does it. Issuing a
 * walk-in ticket is the dialog on the queue page, which needs the queue on screen as well; taking
 * money starts from the list of bills, and the bill page itself asks for `payment:collect`.
 */
export const quickActions: QuickAction[] = [
  { key: 'issueTicket', href: '/queue', scopes: [Scope.QueueRead, Scope.QueueWrite] },
  { key: 'book', href: appointmentPaths.book(), scopes: [Scope.AppointmentWrite] },
  { key: 'register', href: customerPaths.register, scopes: [Scope.CustomerWrite] },
  { key: 'collect', href: '/billing', scopes: [Scope.InvoiceWrite, Scope.PaymentCollect] },
];
