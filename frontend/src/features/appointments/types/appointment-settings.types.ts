/** The four values of `PUT /staff/policies/cancellation`. Every one is sent: it replaces the policy. */
export interface CancellationPolicyInput {
  cutoff_hours: number;
  fee_satang: number;
  free_allowance: number;
  window_days: number;
}

/** The policy as stored, with the version to send as `If-Match` next time. */
export interface CancellationPolicy extends CancellationPolicyInput {
  version: number;
}

export type QueuePriority = 'normal' | 'urgent' | 'emergency';

/** One appointment type. Null duration and lead time mean the service's and the clinic's own. */
export interface AppointmentTypeInput {
  code: string;
  name: string;
  duration_minutes?: number | null;
  online_booking: boolean;
  lead_time_minutes?: number | null;
  queue_priority: QueuePriority;
}

/** `PUT /staff/appointment-types` answers with the whole set and its version. */
export interface AppointmentTypes {
  version: number;
  types: AppointmentTypeInput[];
}
