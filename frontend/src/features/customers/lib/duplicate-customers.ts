import type { ActionError } from '@/lib/actions/action-result';
import type { Customer } from '../types/customer.types';

/**
 * The records already on a phone number, from a `409 duplicate_customer`. Empty for any other
 * refusal, so the form can ask `duplicatesIn(error).length` without checking the code first.
 */
export function duplicatesIn(error: ActionError): Customer[] {
  if (error.code !== 'duplicate_customer') {
    return [];
  }

  const existing = error.details?.existing;

  return Array.isArray(existing) ? (existing as Customer[]) : [];
}
