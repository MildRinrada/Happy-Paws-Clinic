import { staffApi } from '@/lib/api/server-client';
import type {
  AppointmentTypeInput,
  AppointmentTypes,
  CancellationPolicy,
  CancellationPolicyInput,
} from '../types/appointment-settings.types';

/**
 * The two appointment rule sets. The contract only writes them (`PUT`, `settings:write`); reading
 * them back is docs/contract-extensions.md § 23. Each answer carries `version`, which goes out as
 * `If-Match` on the next save so a save does not silently replace someone else's.
 */

/** `GET /staff/policies/cancellation` — `settings:read`. */
export function getCancellationPolicy(): Promise<CancellationPolicy> {
  return staffApi().get<CancellationPolicy>('/staff/policies/cancellation');
}

/** `PUT /staff/policies/cancellation` — the whole policy. */
export function replaceCancellationPolicy(policy: CancellationPolicyInput, version?: number): Promise<CancellationPolicy> {
  return staffApi().put<CancellationPolicy>('/staff/policies/cancellation', { body: policy, ifMatch: version });
}

/** `GET /staff/appointment-types` — `appointment:read`, since the booking form reads it too. */
export function getAppointmentTypes(): Promise<AppointmentTypes> {
  return staffApi().get<AppointmentTypes>('/staff/appointment-types');
}

/** `PUT /staff/appointment-types` — the whole set; `regular` must be in it. */
export function replaceAppointmentTypes(types: AppointmentTypeInput[], version?: number): Promise<AppointmentTypes> {
  return staffApi().put<AppointmentTypes>('/staff/appointment-types', { body: { types }, ifMatch: version });
}
