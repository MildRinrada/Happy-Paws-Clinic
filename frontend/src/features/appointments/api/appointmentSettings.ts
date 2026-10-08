import { staffApi } from '@/lib/api/server-client';
import type {
  AppointmentTypeInput,
  AppointmentTypes,
  CancellationPolicy,
  CancellationPolicyInput,
} from '../types/appointment-settings.types';

/**
 * The two appointment settings the contract can only write, both on `settings:write`. Neither has a
 * `GET`, so the answer to a `PUT` is the only place the stored values are ever seen. `version`,
 * when known, goes out as `If-Match` so a save does not silently replace someone else's.
 */

/** `PUT /staff/policies/cancellation` — the whole policy. */
export function replaceCancellationPolicy(policy: CancellationPolicyInput, version?: number): Promise<CancellationPolicy> {
  return staffApi().put<CancellationPolicy>('/staff/policies/cancellation', { body: policy, ifMatch: version });
}

/** `PUT /staff/appointment-types` — the whole set; `regular` must be in it. */
export function replaceAppointmentTypes(types: AppointmentTypeInput[], version?: number): Promise<AppointmentTypes> {
  return staffApi().put<AppointmentTypes>('/staff/appointment-types', { body: { types }, ifMatch: version });
}
