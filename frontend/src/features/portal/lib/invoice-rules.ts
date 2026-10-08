import type { Invoice } from '../types/invoice.types';

/** Whether a bill still asks the owner for money. A withdrawn or refunded bill never does. */
export function isOwed(invoice: Invoice): boolean {
  return (invoice.status === 'open' || invoice.status === 'partially_paid') && invoice.balance_satang > 0;
}
