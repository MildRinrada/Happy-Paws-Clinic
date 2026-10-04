'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import { updateBookingSettings, updateMedicalRecordSettings, updateQueueSettings } from '../api/clinic-rules';

/**
 * The rules page's three saves. Each returns the settings as stored, so the form can show the
 * new version and, for the queue, the example number the backend formats.
 */

const version = z.number().int().nonnegative();
const whole = z.number().int();

const booking = z.object({
  version,
  values: z.object({
    slot_minutes: whole,
    lead_time_minutes: whole,
    cancellation_cutoff_hours: whole,
    max_advance_days: whole,
    walk_in_slots_per_hour: whole,
  }),
});

export async function updateBookingSettingsAction(payload: z.infer<typeof booking>) {
  const parsed = booking.parse(payload);
  const result = await runAction(() => updateBookingSettings(parsed.version, parsed.values));

  revalidatePath('/settings/rules');
  return result;
}

const queue = z.object({
  version,
  values: z.object({
    number_prefix: z.string().trim().max(3),
    number_digits: whole,
    reset_daily: z.boolean(),
  }),
});

export async function updateQueueSettingsAction(payload: z.infer<typeof queue>) {
  const parsed = queue.parse(payload);
  const result = await runAction(() => updateQueueSettings(parsed.version, parsed.values));

  revalidatePath('/settings/rules');
  return result;
}

const record = z.object({
  version,
  values: z.object({
    required_fields: z.array(z.string().max(40)).max(20),
    examination_systems: z.array(z.string().trim().min(1).max(60)).max(40),
    owner_visible_sections: z.array(z.string().max(40)).max(20),
    signer_must_be_case_vet: z.boolean(),
  }),
});

export async function updateMedicalRecordSettingsAction(payload: z.infer<typeof record>) {
  const parsed = record.parse(payload);
  const result = await runAction(() => updateMedicalRecordSettings(parsed.version, parsed.values));

  revalidatePath('/settings/rules');
  return result;
}
