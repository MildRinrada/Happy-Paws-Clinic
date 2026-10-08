'use server';

import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import { replaceAppointmentTypes, replaceCancellationPolicy } from '../api/appointmentSettings';

/**
 * The appointment settings page's two saves. Ranges (cutoff 0 to 168 hours, durations in steps of
 * five minutes and so on) are the backend's rules; these only check the shape, so the backend's own
 * Thai sentence is what the person reads when a value is out of range. Nothing on any page reads
 * these settings back, so nothing needs refreshing.
 */

const version = z.number().int().nonnegative().optional();
const whole = z.number().int();

const policy = z.object({
  version,
  values: z.object({
    cutoff_hours: whole,
    fee_satang: whole,
    free_allowance: whole,
    window_days: whole,
  }),
});

export async function replaceCancellationPolicyAction(input: z.infer<typeof policy>) {
  const parsed = policy.parse(input);
  return runAction(() => replaceCancellationPolicy(parsed.values, parsed.version));
}

const types = z.object({
  version,
  types: z
    .array(
      z.object({
        code: z.string().trim().min(1).max(30),
        name: z.string().trim().min(1).max(60),
        duration_minutes: whole.nullable().optional(),
        online_booking: z.boolean(),
        lead_time_minutes: whole.nullable().optional(),
        queue_priority: z.enum(['normal', 'urgent', 'emergency']),
      }),
    )
    .min(1)
    .max(20),
});

export async function replaceAppointmentTypesAction(input: z.infer<typeof types>) {
  const parsed = types.parse(input);
  return runAction(() => replaceAppointmentTypes(parsed.types, parsed.version));
}
