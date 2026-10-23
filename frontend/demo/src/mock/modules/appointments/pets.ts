import { onCustomerMerge, recordConsent } from '../customers/store';
import { appointments, tickets, waitingList } from './store';

/**
 * Where booking touches the customers module beyond reading it. A pet booked for the first time
 * (`new_pet` on `POST /portal/appointments`, docs/contract-extensions.md § 27) is registered with
 * that module's `registerPet`; the marketing consent ticked on the guest booking form goes
 * through its `recordConsent`, worded as booking words it.
 */

export function recordMarketingConsent(customerId: string): void {
  recordConsent(customerId, 'marketing', true, { source: 'portal', note: 'ยินยอมตอนจองนัดออนไลน์' });
}

// Two customer records merged: their appointments, tickets and waiting-list entries follow the survivor.
onCustomerMerge((survivingId, duplicateId) => {
  const moved = { appointments: 0, queue_tickets: 0, waiting_list: 0 };
  for (const row of appointments().where((item) => item.customer_id === duplicateId)) {
    appointments().update(row.id, { customer_id: survivingId });
    moved.appointments += 1;
  }
  for (const row of tickets().where((item) => item.customer_id === duplicateId)) {
    tickets().update(row.id, { customer_id: survivingId });
    moved.queue_tickets += 1;
  }
  for (const row of waitingList().where((item) => item.customer_id === duplicateId)) {
    waitingList().update(row.id, { customer_id: survivingId });
    moved.waiting_list += 1;
  }
  return moved;
});
